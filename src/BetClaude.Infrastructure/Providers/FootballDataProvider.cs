using System.Text.RegularExpressions;
using BetClaude.Domain;
using BetClaude.Services;

namespace BetClaude.Infrastructure.Providers;

/// <summary>
/// football-data.co.uk: free CSV files with results, shots, corners, cards, fouls and referee for major leagues.
/// Files are downloaded to the user's own machine only. Check the site's terms before redistributing data.
/// </summary>
public sealed class FootballDataProvider : IHistoricalDataProvider
{
    public const string BaseUrl = "https://www.football-data.co.uk/";

    private static readonly Regex CodePattern = new("^[A-Za-z0-9]{1,6}$", RegexOptions.Compiled);
    private static readonly Regex SeasonPattern = new("^[0-9]{4}$", RegexOptions.Compiled);

    private readonly HttpClient _http;

    public FootballDataProvider(HttpClient http)
    {
        _http = http;
    }

    public string Id => "football-data.co.uk";

    public static string HistoryUrl(string competitionCode, string season)
        => $"{BaseUrl}mmz4281/{season}/{competitionCode}.csv";

    public async Task<IReadOnlyList<HistoricalMatch>> FetchHistoryAsync(string competitionCode, string season, CancellationToken ct = default)
    {
        if (!CodePattern.IsMatch(competitionCode))
        {
            throw new ArgumentException("Competition code must be 1-6 letters or digits, for example E0.", nameof(competitionCode));
        }

        if (!SeasonPattern.IsMatch(season))
        {
            throw new ArgumentException("Season must be four digits, for example 2425.", nameof(season));
        }

        string csv = await _http.GetStringAsync(HistoryUrl(competitionCode, season), ct).ConfigureAwait(false);
        return FootballDataCsvParser.ParseHistory(csv, competitionCode.ToUpperInvariant(), season);
    }

    public async Task<IReadOnlyList<Fixture>> FetchFixturesAsync(CancellationToken ct = default)
    {
        string csv = await _http.GetStringAsync(BaseUrl + "fixtures.csv", ct).ConfigureAwait(false);
        return FootballDataCsvParser.ParseFixtures(csv);
    }
}
