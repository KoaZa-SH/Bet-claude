using BetClaude.Domain;

namespace BetClaude.Prediction.Tests;

public class GoalModelTests
{
    [Fact]
    public void FitRecoversTeamStrengthOrdering()
    {
        var matches = SyntheticData.Generate(seed: 7, SyntheticData.Teams(), homeAdvantage: 1.25, rounds: 10);
        var asOf = matches.Max(m => m.KickoffUtc).AddDays(1);

        var model = GoalModelFitter.Fit(matches, asOf, decayXi: 0.0);

        // Alpha has the best attack and Foxtrot the worst defence: Alpha at home to Foxtrot must score far more
        // than Foxtrot at home to Alpha.
        var alphaHome = model.ExpectedGoals("Alpha", "Foxtrot");
        var foxtrotHome = model.ExpectedGoals("Foxtrot", "Alpha");
        Assert.True(alphaHome.Home > foxtrotHome.Home);
        Assert.True(alphaHome.Home > alphaHome.Away);
        Assert.True(model.HomeAdvantage > 1.0);
    }

    [Fact]
    public void UnknownTeamsFallBackToLeagueAverage()
    {
        var matches = SyntheticData.Generate(seed: 3, SyntheticData.Teams(), homeAdvantage: 1.2, rounds: 4);
        var asOf = matches.Max(m => m.KickoffUtc).AddDays(1);
        var model = GoalModelFitter.Fit(matches, asOf, decayXi: 0.001);

        Assert.False(model.Knows("Nobody"));
        Assert.Equal(0, model.MatchCount("Nobody"));

        var (home, away) = model.ExpectedGoals("Nobody", "Nowhere");
        Assert.InRange(home, 0.05, 6.0);
        Assert.InRange(away, 0.05, 6.0);
    }

    [Fact]
    public void FitIgnoresMatchesAfterTheCutoff()
    {
        var matches = SyntheticData.Generate(seed: 5, SyntheticData.Teams(), homeAdvantage: 1.2, rounds: 4);
        var cutoff = matches[10].KickoffUtc;
        var model = GoalModelFitter.Fit(matches, cutoff, decayXi: 0.0);
        Assert.Equal(10, model.TrainingMatches);
    }

    [Fact]
    public void FitWithNoMatchesDoesNotThrow()
    {
        var model = GoalModelFitter.Fit(Array.Empty<HistoricalMatch>(), DateTime.UtcNow, 0.002);
        Assert.Equal(0, model.TrainingMatches);
        var (home, away) = model.ExpectedGoals("A", "B");
        Assert.True(home > 0 && away > 0);
    }

    [Fact]
    public void BacktestProducesFiniteMetricsAndBeatsFrequencyBaselineOnStructuredData()
    {
        var matches = SyntheticData.Generate(seed: 11, SyntheticData.Teams(), homeAdvantage: 1.25, rounds: 30);

        var result = Backtester.SelectBest(matches, new[] { 0.0, 0.001 }, new[] { 0.0 });

        Assert.NotNull(result);
        Assert.True(result!.Evaluated > 100);
        Assert.False(double.IsNaN(result.LogLoss) || double.IsInfinity(result.LogLoss));
        Assert.False(double.IsNaN(result.Brier));
        Assert.True(result.LogLoss < result.BaselineLogLoss);
        Assert.True(result.BeatsBaseline);
    }

    [Fact]
    public void BacktestReturnsNullWhenThereIsTooLittleData()
    {
        var matches = SyntheticData.Generate(seed: 2, SyntheticData.Teams(), homeAdvantage: 1.2, rounds: 2);
        var result = Backtester.SelectBest(matches, new[] { 0.001 }, new[] { 0.0 });
        Assert.Null(result);
    }
}
