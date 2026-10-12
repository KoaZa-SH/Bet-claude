using BetClaude.Domain;

namespace BetClaude.Services;

public interface IMatchRepository
{
    /// <summary>Inserts new matches and updates existing ones (same competition, kickoff and teams). Returns rows written.</summary>
    Task<int> UpsertMatchesAsync(IReadOnlyCollection<HistoricalMatch> matches, string source, CancellationToken ct = default);

    Task<IReadOnlyList<HistoricalMatch>> GetMatchesAsync(string competition, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetCompetitionsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetTeamsAsync(string competition, CancellationToken ct = default);
}

/// <summary>
/// A source of historical results and upcoming fixtures. New providers implement this interface and never touch the models.
/// </summary>
public interface IHistoricalDataProvider
{
    string Id { get; }

    Task<IReadOnlyList<HistoricalMatch>> FetchHistoryAsync(string competitionCode, string season, CancellationToken ct = default);

    Task<IReadOnlyList<Fixture>> FetchFixturesAsync(CancellationToken ct = default);
}

public interface INewsProvider
{
    Task<IReadOnlyList<NewsItem>> FetchAsync(CancellationToken ct = default);
}

/// <summary>Stores API keys and other secrets. Never writes them in clear text.</summary>
public interface ISecretStore
{
    void Save(string name, string secret);

    string? TryLoad(string name);

    void Delete(string name);
}
