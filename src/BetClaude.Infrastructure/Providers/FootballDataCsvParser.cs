using System.Globalization;
using System.Text;
using BetClaude.Domain;

namespace BetClaude.Infrastructure.Providers;

/// <summary>
/// Parses football-data.co.uk CSV files by header name. Missing or empty cells become null, never 0.
/// Kickoff times in these files are local to the league; they are stored as given and treated as UTC
/// (the time of day is therefore approximate, the date is what matters for modelling).
/// </summary>
public static class FootballDataCsvParser
{
    private static readonly string[] DateFormats = { "dd/MM/yyyy", "dd/MM/yy", "d/M/yyyy", "d/M/yy" };

    public static IReadOnlyList<HistoricalMatch> ParseHistory(string csv, string competition, string season)
    {
        var result = new List<HistoricalMatch>();

        foreach (var row in Rows(csv))
        {
            string? home = row.Get("HomeTeam");
            string? away = row.Get("AwayTeam");
            int? homeGoals = row.GetInt("FTHG");
            int? awayGoals = row.GetInt("FTAG");

            if (home is null || away is null || homeGoals is null || awayGoals is null)
            {
                continue;
            }

            if (!TryParseKickoff(row.Get("Date"), row.Get("Time"), out DateTime kickoff))
            {
                continue;
            }

            string comp = !string.IsNullOrWhiteSpace(competition) ? competition : row.Get("Div") ?? "UNKNOWN";

            result.Add(new HistoricalMatch(
                comp,
                season,
                kickoff,
                home,
                away,
                homeGoals.Value,
                awayGoals.Value,
                row.Get("Referee"),
                row.GetInt("HS"),
                row.GetInt("AS"),
                row.GetInt("HST"),
                row.GetInt("AST"),
                row.GetInt("HC"),
                row.GetInt("AC"),
                row.GetInt("HY"),
                row.GetInt("AY"),
                row.GetInt("HR"),
                row.GetInt("AR"),
                row.GetInt("HF"),
                row.GetInt("AF")));
        }

        return result;
    }

    public static IReadOnlyList<Fixture> ParseFixtures(string csv)
    {
        var result = new List<Fixture>();

        foreach (var row in Rows(csv))
        {
            string? home = row.Get("HomeTeam");
            string? away = row.Get("AwayTeam");
            string? div = row.Get("Div");

            if (home is null || away is null || div is null)
            {
                continue;
            }

            if (!TryParseKickoff(row.Get("Date"), row.Get("Time"), out DateTime kickoff))
            {
                continue;
            }

            result.Add(new Fixture(div, kickoff, home, away));
        }

        return result;
    }

    internal static List<string> SplitLine(string line)
    {
        var cells = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                cells.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        cells.Add(sb.ToString());
        return cells;
    }

    private static IEnumerable<CsvRow> Rows(string csv)
    {
        var lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        if (lines.Length == 0)
        {
            yield break;
        }

        var header = SplitLine(lines[0].TrimStart('﻿'));
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Count; i++)
        {
            columns.TryAdd(header[i].Trim(), i);
        }

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            yield return new CsvRow(columns, SplitLine(lines[i]));
        }
    }

    private static bool TryParseKickoff(string? date, string? time, out DateTime kickoff)
    {
        kickoff = default;
        if (date is null)
        {
            return false;
        }

        if (!DateTime.TryParseExact(date, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime day))
        {
            return false;
        }

        TimeSpan span = TimeSpan.Zero;
        if (time is not null && TimeSpan.TryParse(time, CultureInfo.InvariantCulture, out TimeSpan parsed))
        {
            span = parsed;
        }

        kickoff = DateTime.SpecifyKind(day.Date + span, DateTimeKind.Utc);
        return true;
    }

    private sealed class CsvRow
    {
        private readonly Dictionary<string, int> _columns;
        private readonly List<string> _cells;

        public CsvRow(Dictionary<string, int> columns, List<string> cells)
        {
            _columns = columns;
            _cells = cells;
        }

        public string? Get(string name)
        {
            if (_columns.TryGetValue(name, out int index) && index < _cells.Count)
            {
                string value = _cells[index].Trim();
                return value.Length == 0 ? null : value;
            }

            return null;
        }

        public int? GetInt(string name)
        {
            string? value = Get(name);
            return value is not null && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : null;
        }
    }
}
