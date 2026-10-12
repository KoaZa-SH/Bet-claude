using Microsoft.Data.Sqlite;

namespace BetClaude.Infrastructure.Storage;

/// <summary>Local SQLite database with versioned migrations (PRAGMA user_version).</summary>
public sealed class SqliteDatabase
{
    // Each entry is one schema version. Never edit an old entry; append a new one.
    private static readonly string[] Migrations =
    {
        // v1: teams and matches. Missing statistics are NULL, never 0.
        @"
CREATE TABLE teams(
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE COLLATE NOCASE
);

CREATE TABLE matches(
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    source TEXT NOT NULL,
    competition TEXT NOT NULL,
    season TEXT NOT NULL,
    kickoff_utc TEXT NOT NULL,
    home_team_id INTEGER NOT NULL REFERENCES teams(id),
    away_team_id INTEGER NOT NULL REFERENCES teams(id),
    home_goals INTEGER NOT NULL,
    away_goals INTEGER NOT NULL,
    referee TEXT,
    home_shots INTEGER,
    away_shots INTEGER,
    home_shots_on_target INTEGER,
    away_shots_on_target INTEGER,
    home_corners INTEGER,
    away_corners INTEGER,
    home_yellow INTEGER,
    away_yellow INTEGER,
    home_red INTEGER,
    away_red INTEGER,
    home_fouls INTEGER,
    away_fouls INTEGER,
    fetched_utc TEXT NOT NULL,
    UNIQUE(competition, kickoff_utc, home_team_id, away_team_id)
);

CREATE INDEX ix_matches_competition_kickoff ON matches(competition, kickoff_utc);
",
    };

    public SqliteDatabase(string path)
    {
        string? dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            Pooling = false,
        }.ToString();
    }

    public string ConnectionString { get; }

    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    public void Migrate()
    {
        using var connection = Open();

        using (var wal = connection.CreateCommand())
        {
            wal.CommandText = "PRAGMA journal_mode=WAL;";
            wal.ExecuteScalar();
        }

        long version;
        using (var read = connection.CreateCommand())
        {
            read.CommandText = "PRAGMA user_version;";
            version = Convert.ToInt64(read.ExecuteScalar() ?? 0L);
        }

        for (int i = (int)version; i < Migrations.Length; i++)
        {
            using var transaction = connection.BeginTransaction();

            using (var script = connection.CreateCommand())
            {
                script.Transaction = transaction;
                script.CommandText = Migrations[i];
                script.ExecuteNonQuery();
            }

            using (var setVersion = connection.CreateCommand())
            {
                setVersion.Transaction = transaction;
                setVersion.CommandText = $"PRAGMA user_version = {i + 1};";
                setVersion.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }
}
