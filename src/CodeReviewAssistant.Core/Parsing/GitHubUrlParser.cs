using System.Text.RegularExpressions;
using CodeReviewAssistant.Core.Models;

namespace CodeReviewAssistant.Core.Parsing;

public static partial class GitHubUrlParser
{
    // Matches: https://github.com/{owner}/{repo}/pull/{number}[anything]
    [GeneratedRegex(
        @"^https?://github\.com/(?<owner>[A-Za-z0-9_.\-]+)/(?<repo>[A-Za-z0-9_.\-]+)/pull/(?<number>\d+)",
        RegexOptions.IgnoreCase)]
    private static partial Regex PullRequestUrlRegex();

    // Matches: https://github.com/{owner}/{repo}[/][?anything]
    [GeneratedRegex(
        @"^https?://github\.com/(?<owner>[A-Za-z0-9_.\-]+)/(?<repo>[A-Za-z0-9_.\-]+)(?:/|$|\?)",
        RegexOptions.IgnoreCase)]
    private static partial Regex RepositoryUrlRegex();

    /// <summary>
    /// Tries to parse a GitHub URL into a <see cref="GitHubPrReference"/>.
    /// Returns <c>null</c> if the URL is not a recognised GitHub URL.
    /// </summary>
    public static GitHubPrReference? TryParse(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        url = url.Trim();

        // Try PR URL first (more specific)
        var prMatch = PullRequestUrlRegex().Match(url);
        if (prMatch.Success)
        {
            return new GitHubPrReference
            {
                Owner = prMatch.Groups["owner"].Value,
                Repo  = prMatch.Groups["repo"].Value,
                PullRequestNumber = int.Parse(prMatch.Groups["number"].Value)
            };
        }

        // Fall back to repo-only URL
        var repoMatch = RepositoryUrlRegex().Match(url);
        if (repoMatch.Success)
        {
            return new GitHubPrReference
            {
                Owner = repoMatch.Groups["owner"].Value,
                Repo  = repoMatch.Groups["repo"].Value,
                PullRequestNumber = null
            };
        }

        return null;
    }

    /// <summary>
    /// Returns <c>true</c> when the URL is a valid GitHub pull-request URL.
    /// </summary>
    public static bool IsPullRequestUrl(string? url) =>
        TryParse(url)?.IsPullRequest == true;
}
