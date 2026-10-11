namespace BetClaude.Domain;

/// <summary>
/// A finished match in the internal, provider-independent format.
/// Statistics a provider did not supply are <c>null</c>, never 0.
/// </summary>
public sealed record HistoricalMatch(
    string Competition,
    string Season,
    DateTime KickoffUtc,
    string HomeTeam,
    string AwayTeam,
    int HomeGoals,
    int AwayGoals,
    string? Referee = null,
    int? HomeShots = null,
    int? AwayShots = null,
    int? HomeShotsOnTarget = null,
    int? AwayShotsOnTarget = null,
    int? HomeCorners = null,
    int? AwayCorners = null,
    int? HomeYellow = null,
    int? AwayYellow = null,
    int? HomeRed = null,
    int? AwayRed = null,
    int? HomeFouls = null,
    int? AwayFouls = null);

/// <summary>An upcoming match.</summary>
public sealed record Fixture(string Competition, DateTime KickoffUtc, string HomeTeam, string AwayTeam);

/// <summary>
/// A news item. Only title, link and a short summary are kept; the link always points to the original source.
/// </summary>
public sealed record NewsItem(
    string Source,
    string Title,
    string Url,
    string Summary,
    DateTime? PublishedUtc,
    DateTime FetchedUtc,
    string Language);

public sealed record CorrectScore(int HomeGoals, int AwayGoals, double Probability);

/// <summary>Market probabilities derived from one shared score matrix, so they are mutually consistent.</summary>
public sealed record MatchProbabilities(
    double HomeWin,
    double Draw,
    double AwayWin,
    double HomeOrDraw,
    double HomeOrAway,
    double DrawOrAway,
    double DrawNoBetHome,
    double DrawNoBetAway,
    double BothTeamsScore,
    IReadOnlyDictionary<double, double> OverLines,
    double ExpectedHomeGoals,
    double ExpectedAwayGoals,
    IReadOnlyList<CorrectScore> TopScores);

public enum DataQualityLevel
{
    Insufficient,
    Low,
    Adequate,
}

public enum ValidationStatus
{
    /// <summary>Too little data to run a backtest at all.</summary>
    NoBacktest,

    /// <summary>A backtest ran but the model did not beat the simple baseline.</summary>
    NotValidated,

    /// <summary>The model beat the simple baseline in a walk-forward backtest. This is not a guarantee of future accuracy.</summary>
    BeatsBaseline,
}
