using BetClaude.Domain;

namespace BetClaude.Prediction;

public static class MarketCalculator
{
    public static readonly double[] DefaultLines = { 0.5, 1.5, 2.5, 3.5, 4.5 };

    public static (double Home, double Draw, double Away) Result1X2(ScoreMatrix m)
    {
        double home = 0, draw = 0, away = 0;
        for (int h = 0; h <= m.MaxGoals; h++)
        {
            for (int a = 0; a <= m.MaxGoals; a++)
            {
                double p = m[h, a];
                if (h > a)
                {
                    home += p;
                }
                else if (h == a)
                {
                    draw += p;
                }
                else
                {
                    away += p;
                }
            }
        }

        return (home, draw, away);
    }

    public static MatchProbabilities Compute(ScoreMatrix m, IEnumerable<double>? lines = null, int topScores = 5)
    {
        double home = 0, draw = 0, away = 0, btts = 0, expectedHome = 0, expectedAway = 0;
        var all = new List<CorrectScore>();

        for (int h = 0; h <= m.MaxGoals; h++)
        {
            for (int a = 0; a <= m.MaxGoals; a++)
            {
                double p = m[h, a];
                if (h > a)
                {
                    home += p;
                }
                else if (h == a)
                {
                    draw += p;
                }
                else
                {
                    away += p;
                }

                if (h > 0 && a > 0)
                {
                    btts += p;
                }

                expectedHome += h * p;
                expectedAway += a * p;
                all.Add(new CorrectScore(h, a, p));
            }
        }

        var over = new SortedDictionary<double, double>();
        foreach (double line in lines ?? DefaultLines)
        {
            double sum = 0;
            for (int h = 0; h <= m.MaxGoals; h++)
            {
                for (int a = 0; a <= m.MaxGoals; a++)
                {
                    if (h + a > line)
                    {
                        sum += m[h, a];
                    }
                }
            }

            over[line] = sum;
        }

        double decisive = home + away;
        double dnbHome = decisive > 0 ? home / decisive : 0.5;
        double dnbAway = decisive > 0 ? away / decisive : 0.5;

        var top = all.OrderByDescending(s => s.Probability).Take(topScores).ToList();

        return new MatchProbabilities(
            home,
            draw,
            away,
            home + draw,
            home + away,
            draw + away,
            dnbHome,
            dnbAway,
            btts,
            over,
            expectedHome,
            expectedAway,
            top);
    }
}
