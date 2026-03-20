namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// Mutable state for one PR's AI review, shared between the config bar and the output panel.
/// Stored in <c>IReviewCacheService</c> keyed by PR identity so reviews persist across tab switches.
/// </summary>
public sealed class ReviewSessionState
{
    /// <summary>Options that were (or will be) used for this review.</summary>
    public ReviewOptions Options { get; set; } = ReviewOptions.Default;

    /// <summary>Accumulated markdown text from the streaming API response.</summary>
    public string AccumulatedMarkdown { get; set; } = string.Empty;

    /// <summary>Current lifecycle phase.</summary>
    public ReviewRunStatus Status { get; set; } = ReviewRunStatus.Idle;

    /// <summary>Error message when <see cref="Status"/> is <see cref="ReviewRunStatus.Error"/>.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>0-based index of the chunk currently being streamed.</summary>
    public int CurrentChunk { get; set; }

    /// <summary>Total number of chunks the diff was split into.</summary>
    public int TotalChunks { get; set; } = 1;

    /// <summary>
    /// Cumulative input tokens consumed across all API calls for this review.
    /// Populated from the final <see cref="ReviewChunk.IsUsage"/> chunk.
    /// </summary>
    public long TotalInputTokens { get; set; }

    /// <summary>
    /// Cumulative output tokens produced across all API calls for this review.
    /// Populated from the final <see cref="ReviewChunk.IsUsage"/> chunk.
    /// </summary>
    public long TotalOutputTokens { get; set; }

    /// <summary><see langword="true"/> when the review has produced some text.</summary>
    public bool HasContent => AccumulatedMarkdown.Length > 0;

    /// <summary>Resets all runtime fields, keeping <see cref="Options"/> intact.</summary>
    public void Reset()
    {
        AccumulatedMarkdown = string.Empty;
        Status            = ReviewRunStatus.Idle;
        ErrorMessage      = null;
        CurrentChunk      = 0;
        TotalChunks       = 1;
        TotalInputTokens  = 0;
        TotalOutputTokens = 0;
    }
}
