using CodeReviewAssistant.Core.Models;

namespace CodeReviewAssistant.Core.Interfaces;

/// <summary>
/// Performs AI-powered code reviews against a pull request diff via the Anthropic API.
/// </summary>
public interface ICodeReviewService
{
    /// <summary>
    /// Streams the review as a sequence of <see cref="ReviewChunk"/> fragments.
    /// Chunks are yielded in order as Claude produces them; accumulate <see cref="ReviewChunk.Text"/>
    /// to reconstruct the full review.
    /// </summary>
    /// <remarks>
    /// When the diff exceeds the single-request context limit the implementation
    /// chunks the files and streams each chunk sequentially. The caller receives
    /// a seamless, concatenated stream regardless of chunking.
    /// </remarks>
    IAsyncEnumerable<ReviewChunk> StreamReviewAsync(
        PullRequestContext context,
        ReviewOptions      options,
        CancellationToken  ct = default);

    /// <summary>
    /// Fully evaluates the review and returns the complete <see cref="CodeReview"/> once done.
    /// Internally calls <see cref="StreamReviewAsync"/> and accumulates the result.
    /// </summary>
    Task<CodeReview> ReviewAsync(
        PullRequestContext context,
        ReviewOptions      options,
        CancellationToken  ct = default);
}
