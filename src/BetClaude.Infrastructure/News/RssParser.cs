using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using BetClaude.Domain;

namespace BetClaude.Infrastructure.News;

/// <summary>
/// Parses RSS 2.0. Feed content is untrusted data: DTDs are prohibited, HTML is stripped, text is truncated,
/// and only absolute http(s) links are kept. Only title, link and a short summary are retained.
/// </summary>
public static class RssParser
{
    private const int MaxTitle = 200;
    private const int MaxSummary = 300;

    private static readonly Regex Tags = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

    public static IReadOnlyList<NewsItem> Parse(string xml, string source, string language, DateTime fetchedUtc, int maxItems = 50)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using var stringReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(stringReader, settings);
        var doc = XDocument.Load(xmlReader);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var items = new List<NewsItem>();

        foreach (var item in doc.Descendants("item"))
        {
            string? link = ((string?)item.Element("link"))?.Trim();
            if (!IsHttpUrl(link) || !seen.Add(link!))
            {
                continue;
            }

            string title = Clean((string?)item.Element("title"), MaxTitle);
            if (title.Length == 0)
            {
                continue;
            }

            string summary = Clean((string?)item.Element("description"), MaxSummary);

            DateTime? published = null;
            string? pub = (string?)item.Element("pubDate");
            if (pub is not null && DateTimeOffset.TryParse(pub, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            {
                published = dto.UtcDateTime;
            }

            items.Add(new NewsItem(source, title, link!, summary, published, fetchedUtc, language));
            if (items.Count >= maxItems)
            {
                break;
            }
        }

        return items;
    }

    private static bool IsHttpUrl(string? value)
        => value is not null
           && Uri.TryCreate(value, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static string Clean(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string plain = WebUtility.HtmlDecode(Tags.Replace(text, " "));
        plain = Spaces.Replace(plain, " ").Trim();
        return plain.Length <= max ? plain : plain[..max].TrimEnd() + "…";
    }
}
