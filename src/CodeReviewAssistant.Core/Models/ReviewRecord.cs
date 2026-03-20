namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// Persisted record of a completed (or failed) code review run.
/// Stored by <see cref="IReviewHistoryRepository"/> and backed by a SQLite table.
/// </summary>
public class ReviewRecord
{
    /// <summary>Primary key — set to <see cref="Guid.NewGuid"/> on creation.</summary>
    public Guid Id { get; set; }

    /// <summary>GitHub organisation or user that owns the repository.</summary>
    public string RepositoryOwner { get; set; } = string.Empty;

    /// <summary>Repository name (without owner prefix).</summary>
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>Pull-request number, or <see langword="null"/> if the review was not linked to a PR.</summary>
    public int? PullRequestNumber { get; set; }

    /// <summary>UTC timestamp when the review record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Final outcome of the review run.</summary>
    public ReviewStatus Status { get; set; }

    /// <summary>Total number of findings extracted from the review output.</summary>
    public int TotalFindings { get; set; }

    /// <summary>Number of findings rated CRITICAL severity.</summary>
    public int CriticalFindings { get; set; }

    /// <summary>
    /// JSON-serialised array of <see cref="FindingRecord"/> objects parsed from the review markdown.
    /// Empty array (<c>[]</c>) when the review produced no parseable findings or failed.
    /// </summary>
    public string FindingsJson { get; set; } = "[]";

    /// <summary>
    /// Wall-clock time from review start to completion.
    /// Stored as ticks in SQLite via an EF value converter.
    /// </summary>
    public TimeSpan ReviewDuration { get; set; }

    /// <summary>
    /// Approximate total tokens consumed (input + output) across all API requests.
    /// Zero when token data is unavailable (streaming path).
    /// </summary>
    public int TokensUsed { get; set; }
}
