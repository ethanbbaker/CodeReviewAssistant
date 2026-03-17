namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// Full context for a pull request review: metadata plus the structured file diffs.
/// </summary>
/// <param name="Title">PR title.</param>
/// <param name="Description">PR body / description (may be empty).</param>
/// <param name="BaseBranch">The branch being merged into (e.g. <c>main</c>).</param>
/// <param name="HeadBranch">The branch containing the proposed changes.</param>
/// <param name="Files">Structured diff for every changed file.</param>
public record PullRequestContext(
    string                     Title,
    string                     Description,
    string                     BaseBranch,
    string                     HeadBranch,
    IReadOnlyList<FileDiff>    Files)
{
    /// <summary>Total lines added across all files.</summary>
    public int TotalAdditions => Files.Sum(f => f.Additions);

    /// <summary>Total lines removed across all files.</summary>
    public int TotalDeletions => Files.Sum(f => f.Deletions);

    /// <summary>Number of files that were truncated due to size.</summary>
    public int TruncatedFileCount => Files.Count(f => f.IsTruncated);

    /// <summary>Number of binary files (patch text unavailable).</summary>
    public int BinaryFileCount => Files.Count(f => f.IsBinary);
}
