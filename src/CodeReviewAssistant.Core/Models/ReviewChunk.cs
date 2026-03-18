namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// A single fragment emitted by <see cref="ICodeReviewService.StreamReviewAsync"/>.
/// </summary>
/// <remarks>
/// Most chunks carry streaming text to accumulate (<see cref="IsProgress"/> is <see langword="false"/>).
/// Progress-only chunks (no text) are emitted before each API request so the UI
/// can display "Analysing chunk N of M" without waiting for text to arrive.
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

    /// <summary>Convenience factory for a progress-only chunk.</summary>
    public static ReviewChunk Progress(int chunkIndex, int totalChunks) =>
        new(string.Empty) { IsProgress = true, ChunkIndex = chunkIndex, TotalChunks = totalChunks };
}
