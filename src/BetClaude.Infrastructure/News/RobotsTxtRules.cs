using System.Text;
using System.Text.RegularExpressions;

namespace BetClaude.Infrastructure.News;

/// <summary>
/// Minimal robots.txt evaluator (longest matching rule wins, Allow wins ties, * and $ supported).
/// Rules from the "*" group and from a group naming this crawler are both applied, which is slightly stricter than needed.
/// </summary>
public sealed class RobotsTxtRules
{
    private readonly List<Rule> _rules = new();

    public static RobotsTxtRules AllowAll() => new();

    public static RobotsTxtRules DisallowAll() => Parse("User-agent: *\nDisallow: /");

    public static RobotsTxtRules Parse(string text, string agentToken = "betclaude")
    {
        var rules = new RobotsTxtRules();
        bool groupApplies = false;
        bool lastWasAgent = false;

        foreach (string raw in text.Split('\n'))
        {
            string line = raw;
            int hash = line.IndexOf('#');
            if (hash >= 0)
            {
                line = line[..hash];
            }

            line = line.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            int colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            string key = line[..colon].Trim().ToLowerInvariant();
            string value = line[(colon + 1)..].Trim();

            if (key == "user-agent")
            {
                if (!lastWasAgent)
                {
                    groupApplies = false;
                }

                lastWasAgent = true;
                string agent = value.ToLowerInvariant();
                if (agent == "*" || (agent.Length > 0 && agentToken.Contains(agent)))
                {
                    groupApplies = true;
                }
            }
            else
            {
                lastWasAgent = false;
                if ((key == "allow" || key == "disallow") && groupApplies && value.Length > 0)
                {
                    rules._rules.Add(new Rule(key == "allow", ToRegex(value), value.Length));
                }
            }
        }

        return rules;
    }

    public bool IsAllowed(string pathAndQuery)
    {
        string path = string.IsNullOrEmpty(pathAndQuery) ? "/" : pathAndQuery;
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        Rule? best = null;
        foreach (var rule in _rules)
        {
            if (!rule.Pattern.IsMatch(path))
            {
                continue;
            }

            if (best is null || rule.Length > best.Length || (rule.Length == best.Length && rule.Allow && !best.Allow))
            {
                best = rule;
            }
        }

        return best is null || best.Allow;
    }

    private static Regex ToRegex(string pattern)
    {
        bool anchored = pattern.EndsWith('$');
        if (anchored)
        {
            pattern = pattern[..^1];
        }

        var sb = new StringBuilder("^");
        foreach (char c in pattern)
        {
            if (c == '*')
            {
                sb.Append(".*");
            }
            else
            {
                sb.Append(Regex.Escape(c.ToString()));
            }
        }

        if (anchored)
        {
            sb.Append('$');
        }

        return new Regex(sb.ToString(), RegexOptions.CultureInvariant);
    }

    private sealed record Rule(bool Allow, Regex Pattern, int Length);
}
