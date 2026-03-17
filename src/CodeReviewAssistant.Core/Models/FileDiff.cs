namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// Structured representation of a single file's changes within a pull request.
/// </summary>
/// <param name="FileName">Destination file path (relative to repository root).</param>
/// <param name="Patch">
///   Unified-diff patch text as returned by the GitHub API.
///   Empty string for binary files or when the API omits the patch.
/// </param>
/// <param name="Status">How the file changed.</param>
/// <param name="Additions">Number of lines added.</param>
/// <param name="Deletions">Number of lines removed.</param>
public record FileDiff(
    string     FileName,
    string     Patch,
    DiffStatus Status,
    int        Additions,
    int        Deletions)
{
    /// <summary>
    /// Original file path before a rename or copy. <c>null</c> for all other statuses.
    /// </summary>
    public string? PreviousFileName { get; init; }

    /// <summary>
    /// <c>true</c> when the patch was truncated because it exceeded the maximum line
    /// budget (<see cref="DiffParser.MaxPatchLines"/>).
    /// GitHub itself already caps patches at 65,536 bytes; files that surpass that
    /// limit arrive with a <c>null</c> patch and are represented here with an empty
    /// <see cref="Patch"/> and <c>IsTruncated = false</c> (they are <see cref="DiffStatus.Binary"/>
    /// or simply too large).
    /// </summary>
    public bool IsTruncated { get; init; }

    /// <summary>Convenience property — <c>true</c> for binary files.</summary>
    public bool IsBinary => Status == DiffStatus.Binary;

    /// <summary>Net line change (additions minus deletions).</summary>
    public int NetChange => Additions - Deletions;
}
