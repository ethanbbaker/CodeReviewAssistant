namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// Represents a parsed GitHub URL containing owner, repo, and optional PR number.
/// </summary>
public class GitHubPrReference
{
    public string Owner { get; init; } = string.Empty;
    public string Repo { get; init; } = string.Empty;

    /// <summary>
    /// The pull request number. Null indicates a full-repository review (future phase).
    /// </summary>
    public int? PullRequestNumber { get; init; }

    public bool IsPullRequest => PullRequestNumber.HasValue;

    public override string ToString() =>
        IsPullRequest
            ? $"https://github.com/{Owner}/{Repo}/pull/{PullRequestNumber}"
            : $"https://github.com/{Owner}/{Repo}";
}
