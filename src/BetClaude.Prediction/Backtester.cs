using BetClaude.Domain;

namespace BetClaude.Prediction;

public sealed record BacktestResult(
    double DecayXi,
    double Rho,
    int Evaluated,
    double LogLoss,
    double Brier,
    double BaselineLogLoss)
{
    public const int MinEvaluated = 100;

    /// <summary>True only when enough matches were evaluated and the model's log loss is below the frequency baseline.</summary>
    public bool BeatsBaseline => Evaluated >= MinEvaluated && LogLoss < BaselineLogLoss;
}

/// <summary>
/// Walk-forward backtest on 1X2 probabilities. For each match day in the evaluation tail, the model is
/// fitted using only matches played before that day, so no future information leaks into the prediction.
/// The baseline predicts the outcome frequencies observed in the same training data.
/// </summary>
public static class Backtester
{
    public static BacktestResult? SelectBest(
        IReadOnlyList<HistoricalMatch> matches,
        IReadOnlyList<double> decayGrid,
        IReadOnlyList<double> rhoGrid,
        double evalFraction = 0.25,
        int minTrain = 100)
    {
        var ordered = matches.OrderBy(m => m.KickoffUtc).ToList();
        int n = ordered.Count;
        int start = Math.Max(minTrain, (int)(n * (1.0 - evalFraction)));
        if (n - start < 30)
        {
            return null;
        }

        var evalDays = ordered
            .Skip(start)
            .GroupBy(m => m.KickoffUtc.Date)
            .OrderBy(g => g.Key)
            .ToList();

        BacktestResult? best = null;

        foreach (double xi in decayGrid)
        {
            var acc = new Acc[rhoGrid.Count];
            for (int r = 0; r < acc.Length; r++)
            {
                acc[r] = new Acc();
            }

            foreach (var day in evalDays)
            {
                DateTime dayStart = DateTime.SpecifyKind(day.Key, DateTimeKind.Utc);
                var train = ordered.TakeWhile(m => m.KickoffUtc < dayStart).ToList();
                if (train.Count < minTrain)
                {
                    continue;
                }

                var model = GoalModelFitter.Fit(train, dayStart, xi);
                var baseline = OutcomeFrequencies(train);

                foreach (var match in day)
                {
                    if (!model.Knows(match.HomeTeam) || !model.Knows(match.AwayTeam))
                    {
                        continue;
                    }

                    var (lambdaHome, lambdaAway) = model.ExpectedGoals(match.HomeTeam, match.AwayTeam);
                    int actual = match.HomeGoals > match.AwayGoals ? 0 : match.HomeGoals == match.AwayGoals ? 1 : 2;
                    double baseP = Math.Max(baseline[actual], 1e-12);

                    for (int r = 0; r < acc.Length; r++)
                    {
                        var matrix = ScoreMatrix.Build(lambdaHome, lambdaAway, rhoGrid[r]);
                        var (ph, pd, pa) = MarketCalculator.Result1X2(matrix);
                        var probs = new[] { ph, pd, pa };

                        double brier = 0;
                        for (int k = 0; k < 3; k++)
                        {
                            double o = k == actual ? 1.0 : 0.0;
                            brier += (probs[k] - o) * (probs[k] - o);
                        }

                        acc[r].N++;
                        acc[r].LogLoss += -Math.Log(Math.Max(probs[actual], 1e-12));
                        acc[r].Brier += brier;
                        acc[r].BaseLogLoss += -Math.Log(baseP);
                    }
                }
            }

            for (int r = 0; r < acc.Length; r++)
            {
                if (acc[r].N == 0)
                {
                    continue;
                }

                var result = new BacktestResult(
                    xi,
                    rhoGrid[r],
                    acc[r].N,
                    acc[r].LogLoss / acc[r].N,
                    acc[r].Brier / acc[r].N,
                    acc[r].BaseLogLoss / acc[r].N);

                if (best is null || result.LogLoss < best.LogLoss)
                {
                    best = result;
                }
            }
        }

        return best;
    }

    private static double[] OutcomeFrequencies(IReadOnlyList<HistoricalMatch> train)
    {
        double home = 1, draw = 1, away = 1; // Laplace smoothing
        foreach (var m in train)
        {
            if (m.HomeGoals > m.AwayGoals)
            {
                home++;
            }
            else if (m.HomeGoals == m.AwayGoals)
            {
                draw++;
            }
            else
            {
                away++;
            }
        }

        double total = home + draw + away;
        return new[] { home / total, draw / total, away / total };
    }

    private sealed class Acc
    {
        public int N;
        public double LogLoss;
        public double Brier;
        public double BaseLogLoss;
    }
}
