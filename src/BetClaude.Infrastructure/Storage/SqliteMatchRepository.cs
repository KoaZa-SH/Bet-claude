using System.Globalization;
using BetClaude.Domain;
using BetClaude.Services;
using Microsoft.Data.Sqlite;

namespace BetClaude.Infrastructure.Storage;

public sealed class SqliteMatchRepository : IMatchRepository
{
    private const string TimeFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private readonly SqliteDatabase _database;

    public SqliteMatchRepository(SqliteDatabase database)
    {
        _database = database;
    }

    public Task<int> UpsertMatchesAsync(IReadOnlyCollection<HistoricalMatch> matches, string source, CancellationToken ct = default)
        => Task.Run(() => Upsert(matches, source, ct), ct);

    public Task<IReadOnlyList<HistoricalMatch>> GetMatchesAsync(string competition, CancellationToken ct = default)
        => Task.Run(() => ReadMatches(competition, ct), ct);

    public Task<IReadOnlyList<string>> GetCompetitionsAsync(CancellationToken ct = default)
        => Task.Run(() => ReadStrings("SELECT DISTINCT competition FROM matches ORDER BY competition;", null, ct), ct);

    public Task<IReadOnlyList<string>> GetTeamsAsync(string competition, CancellationToken ct = default)
        => Task.Run(
            () => ReadStrings(
                "SELECT DISTINCT t.name FROM teams t JOIN matches m ON t.id = m.home_team_id OR t.id = m.away_team_id " +
                "WHERE m.competition = $c ORDER BY t.name COLLATE NOCASE;",
                competition,
                ct),
            ct);

    private int Upsert(IReadOnlyCollection<HistoricalMatch> matches, string source, CancellationToken ct)
    {
        using var connection = _database.Open();
        using var transaction = connection.BeginTransaction();
        string fetched = Iso(DateTime.UtcNow);
        int written = 0;

        foreach (var m in matches)
        {
            ct.ThrowIfCancellationRequested();

            long homeId = TeamId(connection, transaction, m.HomeTeam);
            long awayId = TeamId(connection, transaction, m.AwayTeam);

            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
INSERT INTO matches(
    source, competition, season, kickoff_utc, home_team_id, away_team_id, home_goals, away_goals, referee,
    home_shots, away_shots, home_shots_on_target, away_shots_on_target, home_corners, away_corners,
    home_yellow, away_yellow, home_red, away_red, home_fouls, away_fouls, fetched_utc)
VALUES(
    $source, $competition, $season, $kickoff, $home, $away, $hg, $ag, $referee,
    $hs, $as, $hsot, $asot, $hc, $ac,
    $hy, $ay, $hr, $ar, $hf, $af, $fetched)
ON CONFLICT(competition, kickoff_utc, home_team_id, away_team_id) DO UPDATE SET
    source = excluded.source,
    season = excluded.season,
    home_goals = excluded.home_goals,
    away_goals = excluded.away_goals,
    referee = excluded.referee,
    home_shots = excluded.home_shots,
    away_shots = excluded.away_shots,
    home_shots_on_target = excluded.home_shots_on_target,
    away_shots_on_target = excluded.away_shots_on_target,
    home_corners = excluded.home_corners,
    away_corners = excluded.away_corners,
    home_yellow = excluded.home_yellow,
    away_yellow = excluded.away_yellow,
    home_red = excluded.home_red,
    away_red = excluded.away_red,
    home_fouls = excluded.home_fouls,
    away_fouls = excluded.away_fouls,
    fetched_utc = excluded.fetched_utc;";

            Add(cmd, "$source", source);
            Add(cmd, "$competition", m.Competition);
            Add(cmd, "$season", m.Season);
            Add(cmd, "$kickoff", Iso(m.KickoffUtc));
            Add(cmd, "$home", homeId);
            Add(cmd, "$away", awayId);
            Add(cmd, "$hg", m.HomeGoals);
            Add(cmd, "$ag", m.AwayGoals);
            Add(cmd, "$referee", m.Referee);
            Add(cmd, "$hs", m.HomeShots);
            Add(cmd, "$as", m.AwayShots);
            Add(cmd, "$hsot", m.HomeShotsOnTarget);
            Add(cmd, "$asot", m.AwayShotsOnTarget);
            Add(cmd, "$hc", m.HomeCorners);
            Add(cmd, "$ac", m.AwayCorners);
            Add(cmd, "$hy", m.HomeYellow);
            Add(cmd, "$ay", m.AwayYellow);
            Add(cmd, "$hr", m.HomeRed);
            Add(cmd, "$ar", m.AwayRed);
            Add(cmd, "$hf", m.HomeFouls);
            Add(cmd, "$af", m.AwayFouls);
            Add(cmd, "$fetched", fetched);

            written += cmd.ExecuteNonQuery();
        }

        transaction.Commit();
        return written;
    }

    private IReadOnlyList<HistoricalMatch> ReadMatches(string competition, CancellationToken ct)
    {
        using var connection = _database.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT m.competition, m.season, m.kickoff_utc, th.name, ta.name, m.home_goals, m.away_goals, m.referee,
       m.home_shots, m.away_shots, m.home_shots_on_target, m.away_shots_on_target,
       m.home_corners, m.away_corners, m.home_yellow, m.away_yellow, m.home_red, m.away_red,
       m.home_fouls, m.away_fouls
FROM matches m
JOIN teams th ON th.id = m.home_team_id
JOIN teams ta ON ta.id = m.away_team_id
WHERE m.competition = $c
ORDER BY m.kickoff_utc, m.id;";
        Add(cmd, "$c", competition);

        var result = new List<HistoricalMatch>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ct.ThrowIfCancellationRequested();
            result.Add(new HistoricalMatch(
                reader.GetString(0),
                reader.GetString(1),
                ParseIso(reader.GetString(2)),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt32(5),
                reader.GetInt32(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                NullableInt(reader, 8),
                NullableInt(reader, 9),
                NullableInt(reader, 10),
                NullableInt(reader, 11),
                NullableInt(reader, 12),
                NullableInt(reader, 13),
                NullableInt(reader, 14),
                NullableInt(reader, 15),
                NullableInt(reader, 16),
                NullableInt(reader, 17),
                NullableInt(reader, 18),
                NullableInt(reader, 19)));
        }

        return result;
    }

    private IReadOnlyList<string> ReadStrings(string sql, string? parameter, CancellationToken ct)
    {
        using var connection = _database.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        if (parameter is not null)
        {
            Add(cmd, "$c", parameter);
        }

        var result = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ct.ThrowIfCancellationRequested();
            result.Add(reader.GetString(0));
        }

        return result;
    }

    private static long TeamId(SqliteConnection connection, SqliteTransaction transaction, string name)
    {
        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = "INSERT OR IGNORE INTO teams(name) VALUES($n);";
            Add(insert, "$n", name);
            insert.ExecuteNonQuery();
        }

        using var select = connection.CreateCommand();
        select.Transaction = transaction;
        select.CommandText = "SELECT id FROM teams WHERE name = $n;";
        Add(select, "$n", name);
        return Convert.ToInt64(select.ExecuteScalar());
    }

    private static void Add(SqliteCommand cmd, string name, object? value)
        => cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);

    private static int? NullableInt(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    private static string Iso(DateTime value)
    {
        DateTime utc = value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return utc.ToString(TimeFormat, CultureInfo.InvariantCulture);
    }

    private static DateTime ParseIso(string value)
        => DateTime.ParseExact(
            value,
            TimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
}
