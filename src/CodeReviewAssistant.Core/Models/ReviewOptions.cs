namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// Configuration that governs how a pull-request review is performed.
/// </summary>
public sealed class ReviewOptions
{
    /// <summary>Shared default instance — all categories enabled, minimum severity Low.</summary>
    public static readonly ReviewOptions Default = new();

    // ── Technical settings ────────────────────────────────────────────────────

    /// <summary>Anthropic model ID to use.</summary>
    public string Model { get; init; } = "claude-haiku-4-5";

    /// <summary>
    /// Maximum number of output tokens Claude may generate per API request.
    /// Applies per chunk when the diff is split.
    /// </summary>
    public int MaxOutputTokens { get; init; } = 4096;

    // ── User-facing review configuration ─────────────────────────────────────

    /// <summary>
    /// Which finding categories to include in the review.
    /// Defaults to all categories.
    /// </summary>
    public IReadOnlySet<FindingCategory> EnabledCategories { get; init; } =
        new HashSet<FindingCategory>(Enum.GetValues<FindingCategory>());

    /// <summary>
    /// Only report findings at or above this severity.
    /// Defaults to <see cref="FindingSeverity.Low"/> (everything except Info-level noise).
    /// </summary>
    public FindingSeverity MinimumSeverity { get; init; } = FindingSeverity.Low;

    /// <summary>
    /// When <see langword="true"/>, Claude will include a suggested code fix for each issue.
    /// </summary>
    public bool IncludeSuggestedFixes { get; init; } = true;

    /// <summary>
    /// Optional free-text focus instruction appended to the prompt,
    /// e.g. "focus on SQL injection risks".
    /// <see langword="null"/> means no additional focus directive.
    /// </summary>
    public string? FocusAreas { get; init; }
}
