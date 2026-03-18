namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// A single streaming text fragment emitted by <see cref="ICodeReviewService"/>.
/// Accumulate these to build the full <see cref="CodeReview.Text"/>.
/// </summary>
public sealed record ReviewChunk(string Text);
