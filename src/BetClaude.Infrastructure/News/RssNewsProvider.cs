using System.Net;
using BetClaude.Domain;
using BetClaude.Services;

namespace BetClaude.Infrastructure.News;

/// <summary>
/// Fetches one RSS feed after checking the site's robots.txt (404/410 = no restrictions; any other failure = do not fetch).
/// A source's terms of use are separate from robots.txt: keep only title, link and short summary, and always link back.
/// </summary>
public sealed class RssNewsProvider : INewsProvider
{
    private readonly HttpClient _http;
    private readonly Uri _feed;
    private readonly string _sourceId;
    private readonly string _language;

    public RssNewsProvider(HttpClient http, Uri feed, string sourceId, string language)
    {
        _http = http;
        _feed = feed;
        _sourceId = sourceId;
        _language = language;
    }

    public async Task<IReadOnlyList<NewsItem>> FetchAsync(CancellationToken ct = default)
    {
        var robots = await LoadRobotsAsync(ct).ConfigureAwait(false);
        if (!robots.IsAllowed(_feed.PathAndQuery))
        {
            throw new InvalidOperationException("The site's robots.txt does not allow fetching this feed.");
        }

        string xml = await _http.GetStringAsync(_feed, ct).ConfigureAwait(false);
        return RssParser.Parse(xml, _sourceId, _language, DateTime.UtcNow);
    }

    private async Task<RobotsTxtRules> LoadRobotsAsync(CancellationToken ct)
    {
        var robotsUri = new Uri(_feed, "/robots.txt");
        using var response = await _http.GetAsync(robotsUri, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Gone)
        {
            return RobotsTxtRules.AllowAll();
        }

        if (!response.IsSuccessStatusCode)
        {
            return RobotsTxtRules.DisallowAll();
        }

        string text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return RobotsTxtRules.Parse(text);
    }
}
