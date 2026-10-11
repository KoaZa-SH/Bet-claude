namespace BetClaude.Prediction.Tests;

public class ScoreMatrixTests
{
    [Fact]
    public void ProbabilitiesSumToOne()
    {
        var m = ScoreMatrix.Build(1.4, 1.1, -0.05);
        double sum = 0;
        for (int h = 0; h <= m.MaxGoals; h++)
        {
            for (int a = 0; a <= m.MaxGoals; a++)
            {
                sum += m[h, a];
            }
        }

        Assert.InRange(sum, 0.999999, 1.000001);
    }

    [Fact]
    public void OneXTwoSumsToOne()
    {
        var m = ScoreMatrix.Build(1.7, 0.9, -0.1);
        var p = MarketCalculator.Compute(m);
        Assert.Equal(1.0, p.HomeWin + p.Draw + p.AwayWin, 9);
    }

    [Fact]
    public void DoubleChanceAndDrawNoBetAreConsistentWith1X2()
    {
        var m = ScoreMatrix.Build(1.5, 1.2, -0.05);
        var p = MarketCalculator.Compute(m);

        Assert.Equal(p.HomeWin + p.Draw, p.HomeOrDraw, 10);
        Assert.Equal(p.HomeWin + p.AwayWin, p.HomeOrAway, 10);
        Assert.Equal(p.Draw + p.AwayWin, p.DrawOrAway, 10);
        Assert.Equal(1.0, p.DrawNoBetHome + p.DrawNoBetAway, 10);
    }

    [Fact]
    public void OverLinesAreMonotonicallyDecreasing()
    {
        var m = ScoreMatrix.Build(1.6, 1.3, 0.0);
        var p = MarketCalculator.Compute(m);

        Assert.True(p.OverLines[0.5] >= p.OverLines[1.5]);
        Assert.True(p.OverLines[1.5] >= p.OverLines[2.5]);
        Assert.True(p.OverLines[2.5] >= p.OverLines[3.5]);
        Assert.True(p.OverLines[3.5] >= p.OverLines[4.5]);
    }

    [Fact]
    public void SymmetricExpectedGoalsGiveSymmetricResult()
    {
        var m = ScoreMatrix.Build(1.3, 1.3, -0.08);
        var p = MarketCalculator.Compute(m);
        Assert.Equal(p.HomeWin, p.AwayWin, 9);
    }

    [Fact]
    public void ZeroRhoMatchesIndependentPoisson()
    {
        double lh = 1.2;
        double la = 0.8;
        var m = ScoreMatrix.Build(lh, la, 0.0);
        double expected = Math.Exp(-lh) * Math.Exp(-la);
        Assert.Equal(expected, m[0, 0], 5);
    }

    [Fact]
    public void StrongerHomeSideHasHigherWinProbability()
    {
        var strong = MarketCalculator.Compute(ScoreMatrix.Build(2.2, 0.8, 0.0));
        var even = MarketCalculator.Compute(ScoreMatrix.Build(1.3, 1.3, 0.0));
        Assert.True(strong.HomeWin > even.HomeWin);
        Assert.True(strong.AwayWin < even.AwayWin);
    }

    [Fact]
    public void TopScoresAreSortedByProbability()
    {
        var p = MarketCalculator.Compute(ScoreMatrix.Build(1.4, 1.0, 0.0), topScores: 5);
        Assert.Equal(5, p.TopScores.Count);
        for (int i = 1; i < p.TopScores.Count; i++)
        {
            Assert.True(p.TopScores[i - 1].Probability >= p.TopScores[i].Probability);
        }
    }
}
