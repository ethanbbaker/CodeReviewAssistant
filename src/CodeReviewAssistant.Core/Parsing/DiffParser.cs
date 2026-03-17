using CodeReviewAssistant.Core.Models;

namespace CodeReviewAssistant.Core.Parsing;

/// <summary>
/// Converts raw GitHub API pull-request file data into structured
/// <see cref="FileDiff"/> / <see cref="PullRequestContext"/> domain models.
/// </summary>
public static class DiffParser
{
    /// <summary>
    /// Maximum number of unified-diff lines kept per file.
    /// GitHub already caps patches at 65,536 bytes server-side; this
    /// secondary limit keeps AI context windows manageable.
    /// </summary>
    public const int MaxPatchLines = 500;

    // ----------------------------------------------------------------
    // Public API
    // ----------------------------------------------------------------

    /// <summary>
    /// Builds a <see cref="PullRequestContext"/> from pull-request metadata and
    /// the raw list of changed files returned by the GitHub API.
    /// </summary>
    public static PullRequestContext Parse(
        PullRequestInfo               prInfo,
        IReadOnlyList<PullRequestFile> files)
    {
        return new PullRequestContext(
            Title:       prInfo.Title,
            Description: prInfo.Body,
            BaseBranch:  prInfo.BaseRef,
            HeadBranch:  prInfo.HeadRef,
            Files:       ParseFiles(files));
    }

    /// <summary>
    /// Converts a list of raw <see cref="PullRequestFile"/> objects into
    /// structured <see cref="FileDiff"/> domain models.
    /// </summary>
    public static IReadOnlyList<FileDiff> ParseFiles(
        IReadOnlyList<PullRequestFile> files)
        => files.Select(ToFileDiff).ToList();

    // ----------------------------------------------------------------
    // Core conversion
    // ----------------------------------------------------------------

    private static FileDiff ToFileDiff(PullRequestFile file)
    {
        var status               = ResolveStatus(file);
        var (patch, isTruncated) = TrimPatch(file.Patch);

        return new FileDiff(
            FileName:  file.Filename,
            Patch:     patch,
            Status:    status,
            Additions: file.Additions,
            Deletions: file.Deletions)
        {
            PreviousFileName = NullIfEmpty(file.PreviousFilename),
            IsTruncated      = isTruncated
        };
    }

    // ----------------------------------------------------------------
    // Status resolution
    // ----------------------------------------------------------------

    /// <summary>
    /// Maps the GitHub API status string to a <see cref="DiffStatus"/> value.
    /// </summary>
    /// <remarks>
    /// Binary detection: GitHub omits the patch text for binary files, but still
    /// reports a non-zero change count.  A file with <c>Patch == null</c> and
    /// <c>Changes > 0</c> is therefore treated as <see cref="DiffStatus.Binary"/>.
    /// A newly-added empty file has <c>Patch == null</c> AND <c>Changes == 0</c>
    /// and is handled by the normal "added" status path below.
    /// </remarks>
    public static DiffStatus ResolveStatus(PullRequestFile file)
    {
        if (file.Patch is null && file.Changes > 0)
            return DiffStatus.Binary;

        return file.Status?.ToLowerInvariant() switch
        {
            "added"    => DiffStatus.Added,
            "modified" => DiffStatus.Modified,
            "deleted"  => DiffStatus.Deleted,
            "renamed"  => DiffStatus.Renamed,
            "copied"   => DiffStatus.Copied,
            _          => DiffStatus.Unknown
        };
    }

    // ----------------------------------------------------------------
    // Patch truncation
    // ----------------------------------------------------------------

    /// <summary>
    /// Trims a unified-diff patch to at most <see cref="MaxPatchLines"/> lines.
    /// </summary>
    /// <returns>
    /// A tuple of the (possibly shortened) patch string and a flag indicating
    /// whether any lines were removed.
    /// </returns>
    public static (string Patch, bool IsTruncated) TrimPatch(string? rawPatch)
    {
        if (rawPatch is null)
            return (string.Empty, false);

        // Split once; avoid LINQ to keep allocations low on large diffs.
        var lines = rawPatch.Split('\n');

        if (lines.Length <= MaxPatchLines)
            return (rawPatch, false);

        var trimmed = string.Join('\n', lines[..MaxPatchLines]);
        return (trimmed, true);
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrEmpty(value) ? null : value;
}
