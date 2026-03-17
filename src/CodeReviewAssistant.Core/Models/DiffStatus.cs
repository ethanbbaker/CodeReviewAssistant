namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// Represents the change status of a single file in a pull request diff.
/// </summary>
public enum DiffStatus
{
    /// <summary>File did not previously exist.</summary>
    Added,

    /// <summary>File already existed and was changed.</summary>
    Modified,

    /// <summary>File was removed.</summary>
    Deleted,

    /// <summary>File was moved (and optionally modified). <see cref="FileDiff.PreviousFileName"/> will be set.</summary>
    Renamed,

    /// <summary>File was copied from another path. <see cref="FileDiff.PreviousFileName"/> will be set.</summary>
    Copied,

    /// <summary>
    /// File is binary. GitHub omits the patch text for binary files; the diff
    /// will be an empty string and <see cref="FileDiff.IsTruncated"/> will be <c>false</c>.
    /// </summary>
    Binary,

    /// <summary>Status string returned by the API was not recognised.</summary>
    Unknown
}
