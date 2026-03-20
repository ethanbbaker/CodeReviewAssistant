namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// A single fragment emitted by <see cref="ICodeReviewService.StreamReviewAsync"/>.
/// </summary>
/// <remarks>
/// Three kinds of chunk are emitted during a review:
/// <list type="bullet">
///   <item><description>
///     <b>Text chunks</b> — <see cref="IsProgress"/> and <see cref="IsUsage"/> are both
///     <see langword="false"/>; <see cref="Text"/> contains a streaming text fragment to accumulate.
///   </description></item>
///   <item><description>
///     <b>Progress chunks</b> — <see cref="IsProgress"/> is <see langword="true"/>; <see cref="Text"/>
///     is empty. Emitted before each API request so the UI can show "Analysing chunk N of M".
///   </description></item>
///   <item><description>
///     <b>Usage chunks</b> — <see cref="IsUsage"/> is <see langword="true"/>; <see cref="Text"/>
///     is empty. Emitted once after all chunks complete with cumulative token counts.
///   </description></item>
/// </list>
/// </remarks>
public sealed record ReviewChunk(string Text)
{
    /// <summary>
    /// When <see langword="true"/> this is a progress notification — <see cref="Text"/> is empty.
    /// </summary>
    public bool IsProgress { get; init; }

    /// <summary>0-based index of the chunk currently being processed.</summary>
    public int ChunkIndex { get; init; }

    /// <summary>Total number of chunks the diff was split into.</summary>
    public int TotalChunks { get; init; }

    /// <summary>
    /// When <see langword="true"/> this is a token-usage summary emitted once after all chunks
    /// complete. <see cref="Text"/> is empty; read <see cref="InputTokens"/> and
    /// <see cref="OutputTokens"/> instead.
    /// </summary>
    public bool IsUsage { get; init; }

    /// <summary>
    /// Total input tokens consumed across all API calls.
    /// Only meaningful when <see cref="IsUsage"/> is <see langword="true"/>.
    /// </summary>
    public long InputTokens { get; init; }

    /// <summary>
    /// Total output tokens produced across all API calls.
    /// Only meaningful when <see cref="IsUsage"/> is <see langword="true"/>.
    /// </summary>
    public long OutputTokens { get; init; }

    /// <summary>Convenience factory for a progress-only chunk.</summary>
    public static ReviewChunk Progress(int chunkIndex, int totalChunks) =>
        new(string.Empty) { IsProgress = true, ChunkIndex = chunkIndex, TotalChunks = totalChunks };

    /// <summary>Convenience factory for a token-usage summary chunk.</summary>
    public static ReviewChunk Usage(long inputTokens, long outputTokens) =>
        new(string.Empty) { IsUsage = true, InputTokens = inputTokens, OutputTokens = outputTokens };
}
