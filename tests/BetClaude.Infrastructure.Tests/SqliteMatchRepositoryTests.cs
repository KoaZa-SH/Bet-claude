using BetClaude.Domain;
using BetClaude.Infrastructure.Storage;

namespace BetClaude.Infrastructure.Tests;

public sealed class SqliteMatchRepositoryTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"betclaude-{Guid.NewGuid():N}.db");
    private readonly SqliteMatchRepository _repository;

    public SqliteMatchRepositoryTests()
    {
        var database = new SqliteDatabase(_path);
        database.Migrate();
        _repository = new SqliteMatchRepository(database);
    }

    public void Dispose()
    {
        foreach (string suffix in new[] { string.Empty, "-wal", "-shm" })
        {
            try
            {
                File.Delete(_path + suffix);
            }
            catch (IOException)
            {
            }
        }
    }

    private static HistoricalMatch Match(int day, string home, string away, int hg, int ag, int? corners = null, int? yellow = null) =>
        new("E0", "2425", new DateTime(2024, 8, day, 15, 0, 0, DateTimeKind.Utc), home, away, hg, ag,
            HomeCorners: corners, HomeYellow: yellow);

    [Fact]
    public async Task Upserting_twice_does_not_duplicate_and_updates_changed_values()
    {
        await _repository.UpsertMatchesAsync(new[] { Match(10, "Fulham", "Leeds", 1, 0), Match(11, "Leeds", "Fulham", 2, 2) }, "test");
        await _repository.UpsertMatchesAsync(new[] { Match(10, "Fulham", "Leeds", 3, 0) }, "test");

        var all = await _repository.GetMatchesAsync("E0");

        Assert.Equal(2, all.Count);
        Assert.Equal(3, all[0].HomeGoals);
        Assert.Equal(2, all[1].HomeGoals);
    }

    [Fact]
    public async Task Missing_statistics_stay_null_and_zero_stays_zero()
    {
        await _repository.UpsertMatchesAsync(new[] { Match(10, "Fulham", "Leeds", 1, 0, corners: null, yellow: 0) }, "test");

        var m = Assert.Single(await _repository.GetMatchesAsync("E0"));

        Assert.Null(m.HomeCorners);
        Assert.Equal((int?)0, m.HomeYellow);
        Assert.Null(m.Referee);
        Assert.Equal(new DateTime(2024, 8, 10, 15, 0, 0, DateTimeKind.Utc), m.KickoffUtc);
    }

    [Fact]
    public async Task Team_names_are_case_insensitive_and_lists_are_available()
    {
        await _repository.UpsertMatchesAsync(new[] { Match(10, "Fulham", "Leeds", 1, 0), Match(11, "fulham", "Leeds", 0, 0) }, "test");

        var teams = await _repository.GetTeamsAsync("E0");
        var competitions = await _repository.GetCompetitionsAsync();

        Assert.Equal(2, teams.Count);
        Assert.Equal(new[] { "E0" }, competitions);
    }

    [Fact]
    public async Task Migrate_is_idempotent()
    {
        var database = new SqliteDatabase(_path);

        database.Migrate();
        database.Migrate();

        Assert.Empty(await _repository.GetMatchesAsync("E0"));
    }
}
