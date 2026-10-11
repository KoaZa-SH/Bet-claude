namespace BetClaude.Prediction;

/// <summary>
/// Joint probability of the final score, built from two Poisson distributions with an optional
/// Dixon-Coles correction for low scores. Every goal market is derived from this single matrix.
/// </summary>
public sealed class ScoreMatrix
{
    private readonly double[,] _p;

    private ScoreMatrix(double[,] p, int maxGoals, double lambdaHome, double lambdaAway, double rho)
    {
        _p = p;
        MaxGoals = maxGoals;
        LambdaHome = lambdaHome;
        LambdaAway = lambdaAway;
        Rho = rho;
    }

    public int MaxGoals { get; }

    public double LambdaHome { get; }

    public double LambdaAway { get; }

    public double Rho { get; }

    public double this[int homeGoals, int awayGoals] => _p[homeGoals, awayGoals];

    public static ScoreMatrix Build(double lambdaHome, double lambdaAway, double rho = 0.0, int maxGoals = 10)
    {
        if (lambdaHome <= 0 || double.IsNaN(lambdaHome))
        {
            throw new ArgumentOutOfRangeException(nameof(lambdaHome));
        }

        if (lambdaAway <= 0 || double.IsNaN(lambdaAway))
        {
            throw new ArgumentOutOfRangeException(nameof(lambdaAway));
        }

        if (maxGoals < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(maxGoals));
        }

        var ph = Poisson(lambdaHome, maxGoals);
        var pa = Poisson(lambdaAway, maxGoals);
        var m = new double[maxGoals + 1, maxGoals + 1];
        double sum = 0;

        for (int h = 0; h <= maxGoals; h++)
        {
            for (int a = 0; a <= maxGoals; a++)
            {
                double v = ph[h] * pa[a] * Tau(h, a, lambdaHome, lambdaAway, rho);
                if (v < 0)
                {
                    v = 0;
                }

                m[h, a] = v;
                sum += v;
            }
        }

        // Normalise so truncation at maxGoals and the correction do not leak probability mass.
        for (int h = 0; h <= maxGoals; h++)
        {
            for (int a = 0; a <= maxGoals; a++)
            {
                m[h, a] /= sum;
            }
        }

        return new ScoreMatrix(m, maxGoals, lambdaHome, lambdaAway, rho);
    }

    private static double[] Poisson(double lambda, int max)
    {
        var r = new double[max + 1];
        r[0] = Math.Exp(-lambda);
        for (int k = 1; k <= max; k++)
        {
            r[k] = r[k - 1] * lambda / k;
        }

        return r;
    }

    private static double Tau(int h, int a, double lh, double la, double rho)
    {
        if (h == 0 && a == 0)
        {
            return 1.0 - lh * la * rho;
        }

        if (h == 0 && a == 1)
        {
            return 1.0 + lh * rho;
        }

        if (h == 1 && a == 0)
        {
            return 1.0 + la * rho;
        }

        if (h == 1 && a == 1)
        {
            return 1.0 - rho;
        }

        return 1.0;
    }
}
