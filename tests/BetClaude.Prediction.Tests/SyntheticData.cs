using BetClaude.Domain;

namespace BetClaude.Prediction.Tests;

/// <summary>Generates reproducible Poisson match data from known team strengths.</summary>
internal static class SyntheticData
{
    public static Dictionary<string, (double Attack, double Defence)> Teams() => new()
    {
        ["Alpha"] = (2.0, 0.7),
        ["Bravo"] = (1.6, 0.8),
        ["Charlie"] = (1.3, 1.0),
        ["Delta"] = (1.1, 1.0),
        ["Echo"] = (0.9, 1.2),
        ["Foxtrot"] = (0.7, 1.4),
    };

    public static List<HistoricalMatch> Generate(
        int seed,
        Dictionary<string, (double Attack, double Defence)> teams,
        double homeAdvantage,
        int rounds)
    {
        var rnd = new Random(seed);
        var names = teams.Keys.ToList();
        var list = new List<HistoricalMatch>();
        var day = new DateTime(2022, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int r = 0; r < rounds; r++)
        {
            foreach (var home in names)
            {
                foreach (var away in names)
                {
                    if (home == away)
                    {
                        continue;
                    }

                    double lambdaHome = homeAdvantage * teams[home].Attack * teams[away].Defence;
                    double lambdaAway = teams[away].Attack * teams[home].Defence;
                    list.Add(new HistoricalMatch(
                        "SYN",
                        "S1",
                        day,
                        home,
                        away,
                        SamplePoisson(rnd, lambdaHome),
                        SamplePoisson(rnd, lambdaAway)));
                    day = day.AddDays(1);
                }
            }
        }

        return list;
    }

    private static int SamplePoisson(Random rnd, double lambda)
    {
        double limit = Math.Exp(-lambda);
        double p = 1.0;
        int k = 0;
        do
        {
            k++;
            p *= rnd.NextDouble();
        }
        while (p > limit);

        return k - 1;
    }
}
