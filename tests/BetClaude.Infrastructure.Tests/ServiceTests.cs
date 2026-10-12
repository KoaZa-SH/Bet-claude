using BetClaude.Domain;
using BetClaude.Services;

namespace BetClaude.Infrastructure.Tests;

public class ServiceTests
{
    private sealed class FakeProvider : IHistoricalDataProvider
    {
        private readonly IReadOnlyList<HistoricalMatch> _matches;

        public FakeProvider(IReadOnlyList<HistoricalMatch> matches) => _matches = matches;

        public string Id => "fake";

        public Task<IReadOnlyList<HistoricalMatch>> FetchHistoryAsync(string competitionCode, string season, CancellationToken ct = default)
            => Task.FromResult(_matches);

        public Task<IReadOnlyList<Fixture>> FetchFixturesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Fixture>>(Array.Empty<Fixture>());
    }

    private sealed class FakeRepository : IMatchRepository
    {
        public List<HistoricalMatch> Stored { get; } = new();

        public string? LastSource { get; private set; }

        public Task<int> UpsertMatchesAsync(IReadOnlyCollection<HistoricalMatch> matches, string source, CancellationToken ct = default)
        {
            Stored.AddRange(matches);
            LastSource = source;
            return Task.FromResult(matches.Count);
        }

        public Task<IReadOnlyList<HistoricalMatch>> GetMatchesAsync(string competition, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<HistoricalMatch>>(Stored);

        public Task<IReadOnlyList<string>> GetCompetitionsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public Task<IReadOnlyList<string>> GetTeamsAsync(string competition, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    private static List<HistoricalMatch> SmallLeague()
    {
        string[] teams = { "A", "B", "C", "D" };
        var list = new List<HistoricalMatch>();
        var day = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int round = 0; round < 4; round++)
        {
            foreach (string home in teams)
            {
                foreach (string away in teams)
                {
                    if (home != away)
                    {
                        list.Add(new HistoricalMatch("T1", "2324", day, home, away, (round + home.Length) % 3, round % 2));
                        day = day.AddDays(1);
                    }
                }
            }
        }

        return list;
    }

    [Fact]
    public async Task Import_stores_matches_with_the_provider_id_and_returns_the_count()
    {
        var repo = new FakeRepository();
        var service = new DataImportService(repo);

        int written = await service.ImportHistoryAsync(new FakeProvider(SmallLeague()), "T1", "2324");

        Assert.Equal(48, written);
        Assert.Equal("fake", repo.LastSource);
    }

    [Fact]
    public async Task Import_of_an_empty_result_writes_nothing()
    {
        var repo = new FakeRepository();
        var service = new DataImportService(repo);

        int written = await service.ImportHistoryAsync(new FakeProvider(Array.Empty<HistoricalMatch>()), "T1", "2324");

        Assert.Equal(0, written);
        Assert.Empty(repo.Stored);
    }

    [Fact]
    public void Same_team_on_both_sides_gives_no_probabilities()
    {
        var service = new AnalysisService(new FakeRepository());

        var result = service.Analyze(SmallLeague(), "T1", "A", "a", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Null(result.Probabilities);
        Assert.Contains("WarnSameTeam", result.WarningKeys);
    }

    [Fact]
    public void A_team_with_no_history_is_reported_as_insufficient_data_not_a_precise_number()
    {
        var service = new AnalysisService(new FakeRepository());

        var result = service.Analyze(SmallLeague(), "T1", "A", "Ghost FC", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Null(result.Probabilities);
        Assert.Equal(DataQualityLevel.Insufficient, result.Quality);
        Assert.Contains("WarnTeamUnknown", result.WarningKeys);
        Assert.Equal(0, result.AwayMatches);
    }
}
