namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// The complete, accumulated result of a pull-request code review.
/// Returned by <see cref="ICodeReviewService.ReviewAsync"/> once the
/// full response has been collected from the streaming API.
/// </summary>
public sealed class CodeReview
{
    /// <summary>Full review text in Markdown.</summary>
    public required string Text { get; init; }

    /// <summary>Model that generated the review (echoed from the API response).</summary>
    public string Model { get; init; } = string.Empty;

    /// <summary>Total input tokens consumed across all API requests.</summary>
    public long InputTokens { get; init; }

    /// <summary>Total output tokens generated across all API requests.</summary>
    public long OutputTokens { get; init; }

    /// <summary>
    /// <see langword="true"/> when the diff was too large for a single request
    /// and was split into multiple chunks reviewed separately.
    /// </summary>
    public bool WasChunked { get; init; }

    /// <summary>Number of individual API requests made (≥ 1).</summary>
    public int ChunkCount { get; init; } = 1;

    /// <summary>UTC timestamp when the review completed.</summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}
