using System.Xml;
using BetClaude.Infrastructure.News;

namespace BetClaude.Infrastructure.Tests;

public class RssParserTests
{
    private static readonly DateTime Fetched = new(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc);

    private static string Feed(string items) =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><rss version=\"2.0\"><channel><title>t</title>" + items + "</channel></rss>";

    [Fact]
    public void Html_is_stripped_and_dates_are_converted_to_utc()
    {
        string xml = Feed(
            "<item><title>Star <b>out</b></title><link>https://example.com/a</link>" +
            "<description><![CDATA[<p>Injured &amp; out</p>]]></description>" +
            "<pubDate>Mon, 12 Oct 2026 07:00:00 GMT</pubDate></item>");

        var items = RssParser.Parse(xml, "src", "en", Fetched);

        var item = Assert.Single(items);
        Assert.Equal("Star out", item.Title);
        Assert.Equal("Injured & out", item.Summary);
        Assert.Equal("https://example.com/a", item.Url);
        Assert.Equal(new DateTime(2026, 10, 12, 7, 0, 0), item.PublishedUtc);
        Assert.Equal(Fetched, item.FetchedUtc);
        Assert.Equal("en", item.Language);
    }

    [Fact]
    public void Non_http_links_and_duplicate_links_are_dropped()
    {
        string xml = Feed(
            "<item><title>Bad</title><link>javascript:alert(1)</link></item>" +
            "<item><title>One</title><link>https://example.com/1</link></item>" +
            "<item><title>One again</title><link>https://example.com/1</link></item>");

        var items = RssParser.Parse(xml, "src", "en", Fetched);

        var item = Assert.Single(items);
        Assert.Equal("One", item.Title);
    }

    [Fact]
    public void Long_text_is_truncated()
    {
        string longTitle = new('a', 500);
        string xml = Feed($"<item><title>{longTitle}</title><link>https://example.com/x</link></item>");

        var item = Assert.Single(RssParser.Parse(xml, "src", "en", Fetched));

        Assert.True(item.Title.Length <= 201);
        Assert.EndsWith("…", item.Title);
    }

    [Fact]
    public void Feeds_with_a_dtd_are_rejected()
    {
        string xml = "<?xml version=\"1.0\"?><!DOCTYPE rss [<!ENTITY x \"y\">]><rss><channel>" +
                     "<item><title>&x;</title><link>https://example.com/1</link></item></channel></rss>";

        Assert.Throws<XmlException>(() => RssParser.Parse(xml, "src", "en", Fetched));
    }

    [Fact]
    public void Persian_titles_are_kept()
    {
        string xml = Feed("<item><title>مصدومیت ستاره تیم</title><link>https://example.com/fa</link></item>");

        var item = Assert.Single(RssParser.Parse(xml, "varzesh3", "fa", Fetched));

        Assert.Equal("مصدومیت ستاره تیم", item.Title);
        Assert.Equal("fa", item.Language);
    }
}
