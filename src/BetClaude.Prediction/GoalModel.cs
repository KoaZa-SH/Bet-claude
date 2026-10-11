using BetClaude.Domain;

namespace BetClaude.Prediction;

/// <summary>
/// Multiplicative team-strength model:
/// lambda_home = gamma * attack[home] * defence[away], lambda_away = attack[away] * defence[home].
/// Defence factors are normalised to a mean of 1; attack absorbs the league scoring level.
/// </summary>
public sealed class FittedGoalModel
{
    private const double MinLambda = 0.05;
    private const double MaxLambda = 6.0;

    private readonly Dictionary<string, int> _index;
    private readonly double[] _attack;
    private readonly double[] _defence;
    private readonly int[] _counts;

    internal FittedGoalModel(
        Dictionary<string, int> index,
        double[] attack,
        double[] defence,
        int[] counts,
        double homeAdvantage,
        double leagueAttack,
        int trainingMatches)
    {
        _index = index;
        _attack = attack;
        _defence = defence;
        _counts = counts;
        HomeAdvantage = homeAdvantage;
        LeagueAttack = leagueAttack;
        TrainingMatches = trainingMatches;
    }

    public double HomeAdvantage { get; }

    public double LeagueAttack { get; }

    public int TrainingMatches { get; }

    public bool Knows(string team) => _index.ContainsKey(team);

    public int MatchCount(string team) => _index.TryGetValue(team, out var idx) ? _counts[idx] : 0;

    public (double Home, double Away) ExpectedGoals(string home, string away)
    {
        double attackHome = AttackOf(home);
        double defenceHome = DefenceOf(home);
        double attackAway = AttackOf(away);
        double defenceAway = DefenceOf(away);

        double lambdaHome = HomeAdvantage * attackHome * defenceAway;
        double lambdaAway = attackAway * defenceHome;
        return (Clamp(lambdaHome), Clamp(lambdaAway));
    }

    private double AttackOf(string team) => _index.TryGetValue(team, out var idx) ? _attack[idx] : LeagueAttack;

    private double DefenceOf(string team) => _index.TryGetValue(team, out var idx) ? _defence[idx] : 1.0;

    private static double Clamp(double v) => Math.Min(MaxLambda, Math.Max(MinLambda, v));
}

public static class GoalModelFitter
{
    /// <summary>
    /// Fits the model on matches played before <paramref name="asOfUtc"/>. Older matches get weight
    /// exp(-decayXi * ageInDays); decayXi should be chosen by backtest (see <see cref="Backtester"/>), not by taste.
    /// <paramref name="priorStrength"/> shrinks teams with little data toward the league average.
    /// </summary>
    public static FittedGoalModel Fit(
        IEnumerable<HistoricalMatch> matches,
        DateTime asOfUtc,
        double decayXi,
        double priorStrength = 2.0,
        int iterations = 40)
    {
        var list = matches.Where(m => m.KickoffUtc < asOfUtc).ToList();
        int n = list.Count;
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        int IndexOf(string team)
        {
            if (!index.TryGetValue(team, out var found))
            {
                found = index.Count;
                index[team] = found;
            }

            return found;
        }

        var home = new int[n];
        var away = new int[n];
        var homeGoals = new double[n];
        var awayGoals = new double[n];
        var weight = new double[n];
        double sumWeight = 0;
        double sumGoals = 0;

        for (int i = 0; i < n; i++)
        {
            var m = list[i];
            home[i] = IndexOf(m.HomeTeam);
            away[i] = IndexOf(m.AwayTeam);
            homeGoals[i] = m.HomeGoals;
            awayGoals[i] = m.AwayGoals;
            double ageDays = Math.Max(0.0, (asOfUtc - m.KickoffUtc).TotalDays);
            weight[i] = Math.Exp(-decayXi * ageDays);
            sumWeight += weight[i];
            sumGoals += weight[i] * (m.HomeGoals + m.AwayGoals);
        }

        int teams = index.Count;
        double league = sumWeight > 0 ? sumGoals / (2.0 * sumWeight) : 1.3;
        if (league < 0.2)
        {
            league = 0.2;
        }

        var counts = new int[teams];
        for (int i = 0; i < n; i++)
        {
            counts[home[i]]++;
            counts[away[i]]++;
        }

        if (n == 0)
        {
            return new FittedGoalModel(index, Array.Empty<double>(), Array.Empty<double>(), counts, 1.2, league, 0);
        }

        var attack = new double[teams];
        var defence = new double[teams];
        for (int t = 0; t < teams; t++)
        {
            attack[t] = league;
            defence[t] = 1.0;
        }

        double gamma = 1.2;
        var num = new double[teams];
        var den = new double[teams];

        for (int iter = 0; iter < iterations; iter++)
        {
            // Attack update.
            Array.Clear(num, 0, teams);
            Array.Clear(den, 0, teams);
            for (int i = 0; i < n; i++)
            {
                num[home[i]] += weight[i] * homeGoals[i];
                den[home[i]] += weight[i] * defence[away[i]] * gamma;
                num[away[i]] += weight[i] * awayGoals[i];
                den[away[i]] += weight[i] * defence[home[i]];
            }

            for (int t = 0; t < teams; t++)
            {
                attack[t] = (num[t] + priorStrength * league) / (den[t] + priorStrength);
            }

            // Defence update.
            Array.Clear(num, 0, teams);
            Array.Clear(den, 0, teams);
            for (int i = 0; i < n; i++)
            {
                num[home[i]] += weight[i] * awayGoals[i];
                den[home[i]] += weight[i] * attack[away[i]];
                num[away[i]] += weight[i] * homeGoals[i];
                den[away[i]] += weight[i] * attack[home[i]] * gamma;
            }

            for (int t = 0; t < teams; t++)
            {
                defence[t] = (num[t] + priorStrength * league) / (den[t] + priorStrength * league);
            }

            // Home advantage update.
            double observed = 0;
            double expected = 0;
            for (int i = 0; i < n; i++)
            {
                observed += weight[i] * homeGoals[i];
                expected += weight[i] * attack[home[i]] * defence[away[i]];
            }

            if (expected > 0)
            {
                gamma = Math.Min(2.0, Math.Max(0.5, observed / expected));
            }

            // Identifiability: keep the mean defence at 1 and let attack absorb the scale.
            double meanDefence = 0;
            for (int t = 0; t < teams; t++)
            {
                meanDefence += defence[t];
            }

            meanDefence /= teams;
            if (meanDefence > 0)
            {
                for (int t = 0; t < teams; t++)
                {
                    defence[t] /= meanDefence;
                    attack[t] *= meanDefence;
                }
            }
        }

        return new FittedGoalModel(index, attack, defence, counts, gamma, league, n);
    }
}
