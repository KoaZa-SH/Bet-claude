using BetClaude.Domain;
using BetClaude.Prediction;

namespace BetClaude.Services;

public sealed record AnalysisResult(
    MatchProbabilities? Probabilities,
    DataQualityLevel Quality,
    ValidationStatus Validation,
    BacktestResult? Backtest,
    double DecayXi,
    double Rho,
    int HomeMatches,
    int AwayMatches,
    int TrainingMatches,
    IReadOnlyList<string> WarningKeys);

/// <summary>
/// Produces probabilities for one match from imported history. When a team has too little data, no probabilities
/// are returned at all (Insufficient) instead of a precise-looking number.
/// </summary>
public sealed class AnalysisService
{
    public const int MinTeamMatches = 8;
    public const int LowTeamMatches = 20;
    public const int MinBacktestMatches = 300;
    public const double DefaultDecayXi = 0.002;

    private static readonly double[] DecayGrid = { 0.0005, 0.001, 0.002, 0.004 };
    private static readonly double[] RhoGrid = { -0.1, -0.05, 0.0 };

    private readonly IMatchRepository _repository;
    private readonly Dictionary<string, (int Count, BacktestResult? Result)> _cache = new();
    private readonly object _gate = new();

    public AnalysisService(IMatchRepository repository)
    {
        _repository = repository;
    }

    public async Task<AnalysisResult> AnalyzeAsync(
        string competition,
        string home,
        string away,
        DateTime asOfUtc,
        CancellationToken ct = default)
    {
        var matches = await _repository.GetMatchesAsync(competition, ct).ConfigureAwait(false);
        return await Task.Run(() => Analyze(matches, competition, home, away, asOfUtc), ct).ConfigureAwait(false);
    }

    public AnalysisResult Analyze(
        IReadOnlyList<HistoricalMatch> matches,
        string cacheKey,
        string home,
        string away,
        DateTime asOfUtc)
    {
        var warnings = new List<string>();

        if (string.Equals(home, away, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("WarnSameTeam");
            return new AnalysisResult(null, DataQualityLevel.Insufficient, ValidationStatus.NoBacktest, null, DefaultDecayXi, 0.0, 0, 0, 0, warnings);
        }

        var played = matches.Where(m => m.KickoffUtc < asOfUtc).ToList();
        var backtest = GetBacktest(played, cacheKey);
        double xi = backtest?.DecayXi ?? DefaultDecayXi;
        double rho = backtest?.Rho ?? 0.0;

        var model = GoalModelFitter.Fit(played, asOfUtc, xi);
        int homeMatches = model.MatchCount(home);
        int awayMatches = model.MatchCount(away);
        int fewest = Math.Min(homeMatches, awayMatches);

        if (!model.Knows(home) || !model.Knows(away))
        {
            warnings.Add("WarnTeamUnknown");
        }

        DataQualityLevel quality = fewest < MinTeamMatches
            ? DataQualityLevel.Insufficient
            : fewest < LowTeamMatches ? DataQualityLevel.Low : DataQualityLevel.Adequate;

        ValidationStatus validation;
        if (backtest is null)
        {
            validation = ValidationStatus.NoBacktest;
            warnings.Add("WarnNoBacktest");
        }
        else if (backtest.BeatsBaseline)
        {
            validation = ValidationStatus.BeatsBaseline;
        }
        else
        {
            validation = ValidationStatus.NotValidated;
            warnings.Add("WarnNotBeatingBaseline");
        }

        if (quality == DataQualityLevel.Insufficient)
        {
            return new AnalysisResult(null, quality, validation, backtest, xi, rho, homeMatches, awayMatches, model.TrainingMatches, warnings);
        }

        if (quality == DataQualityLevel.Low)
        {
            warnings.Add("WarnFewMatches");
        }

        var (lambdaHome, lambdaAway) = model.ExpectedGoals(home, away);
        var matrix = ScoreMatrix.Build(lambdaHome, lambdaAway, rho);
        var probabilities = MarketCalculator.Compute(matrix);

        return new AnalysisResult(probabilities, quality, validation, backtest, xi, rho, homeMatches, awayMatches, model.TrainingMatches, warnings);
    }

    private BacktestResult? GetBacktest(IReadOnlyList<HistoricalMatch> played, string key)
    {
        if (played.Count < MinBacktestMatches)
        {
            return null;
        }

        lock (_gate)
        {
            if (_cache.TryGetValue(key, out var cached) && cached.Count == played.Count)
            {
                return cached.Result;
            }
        }

        var result = Backtester.SelectBest(played, DecayGrid, RhoGrid);

        lock (_gate)
        {
            _cache[key] = (played.Count, result);
        }

        return result;
    }
}
