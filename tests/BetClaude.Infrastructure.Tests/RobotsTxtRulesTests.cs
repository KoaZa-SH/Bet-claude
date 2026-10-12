using BetClaude.Infrastructure.News;

namespace BetClaude.Infrastructure.Tests;

public class RobotsTxtRulesTests
{
    // robots.txt files supplied by the user on 2026-10-11.
    private const string Varzesh3 =
        "User-agent: *\nDisallow: /about\nDisallow: /profile\nDisallow: /developer-tools\n" +
        "Disallow: /bulletins\nDisallow: /login\nDisallow: *?*\nSitemap: https://www.varzesh3.com/sitemap.xml\n";

    private const string Fbtv3 =
        "User-agent: *\nDisallow: /admin/\nDisallow: /login/\nDisallow: /register/\n" +
        "Disallow: /checkout/\nDisallow: /vendor/\n\nAllow: /\n\nSitemap: https://fbtv3.ir/sitemap.xml\n";

    [Fact]
    public void Varzesh3_feed_is_allowed_but_query_urls_and_login_are_not()
    {
        var rules = RobotsTxtRules.Parse(Varzesh3);

        Assert.True(rules.IsAllowed("/rss/all"));
        Assert.False(rules.IsAllowed("/rss/all?x=1"));
        Assert.False(rules.IsAllowed("/login"));
        Assert.False(rules.IsAllowed("/bulletins"));
    }

    [Fact]
    public void Fbtv3_allows_news_but_not_admin()
    {
        var rules = RobotsTxtRules.Parse(Fbtv3);

        Assert.True(rules.IsAllowed("/news/123"));
        Assert.False(rules.IsAllowed("/admin/panel"));
    }

    [Fact]
    public void Longest_match_wins_and_allow_wins_ties()
    {
        var rules = RobotsTxtRules.Parse("User-agent: *\nDisallow: /a\nAllow: /a/b\nDisallow: /c\nAllow: /c\n");

        Assert.True(rules.IsAllowed("/a/b/page"));
        Assert.False(rules.IsAllowed("/a/x"));
        Assert.True(rules.IsAllowed("/c"));
    }

    [Fact]
    public void Dollar_anchor_matches_only_the_end_of_the_path()
    {
        var rules = RobotsTxtRules.Parse("User-agent: *\nDisallow: /*.pdf$\n");

        Assert.False(rules.IsAllowed("/files/a.pdf"));
        Assert.True(rules.IsAllowed("/files/a.pdf.html"));
    }

    [Fact]
    public void Rules_for_other_crawlers_are_ignored()
    {
        var rules = RobotsTxtRules.Parse("User-agent: SomeOtherBot\nDisallow: /\n");

        Assert.True(rules.IsAllowed("/anything"));
    }

    [Fact]
    public void AllowAll_and_DisallowAll()
    {
        Assert.True(RobotsTxtRules.AllowAll().IsAllowed("/x"));
        Assert.False(RobotsTxtRules.DisallowAll().IsAllowed("/x"));
    }
}
