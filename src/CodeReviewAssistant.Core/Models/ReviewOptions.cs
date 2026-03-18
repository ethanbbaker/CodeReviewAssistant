namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// Configuration that governs how a pull-request review is performed.
/// </summary>
public sealed class ReviewOptions
{
    /// <summary>Shared default instance with sensible defaults.</summary>
    public static readonly ReviewOptions Default = new();

    /// <summary>
    /// Anthropic model ID to use.
    /// Defaults to <c>claude-opus-4-6</c> per project guidelines.
    /// </summary>
    public string Model { get; init; } = "claude-opus-4-6";

    /// <summary>
    /// Maximum number of output tokens Claude may generate per API request.
    /// For large PRs that are chunked, this limit applies per chunk.
    /// </summary>
    public int MaxOutputTokens { get; init; } = 4096;

    /// <summary>
    /// Optional list of focus areas to emphasise in the review prompt,
    /// e.g. <c>["security", "performance", "test coverage"]</c>.
    /// When empty the prompt requests a general review.
    /// </summary>
    public IReadOnlyList<string> FocusAreas { get; init; } = [];
}
