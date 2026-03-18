using CodeReviewAssistant.Core.Models;

namespace CodeReviewAssistant.Infrastructure.Anthropic;

/// <summary>
/// Splits the file list of a pull request into chunks that each fit within
/// Claude's context window, based on a rough character-to-token estimate.
/// </summary>
internal static class FileChunker
{
    // Claude's advertised context window is 200 K tokens.
    // We leave generous headroom for the system prompt, user-message overhead,
    // and the model's output tokens.
    private const int MaxChunkInputTokens = 140_000;

    // Rough heuristic: 4 characters ≈ 1 token (standard BPE estimate).
    private const int CharsPerToken = 4;

    // Estimated overhead for system prompt + PR header + boilerplate per request.
    private const int OverheadTokens = 1_500;

    /// <summary>
    /// Partitions <paramref name="context"/>'s files into one or more groups
    /// that each fit within <see cref="MaxChunkInputTokens"/>.
    /// Returns a single-element list when the whole diff fits in one request.
    /// </summary>
    internal static IReadOnlyList<IReadOnlyList<FileDiff>> Chunk(PullRequestContext context)
    {
        var files     = context.Files;
        var chunks    = new List<IReadOnlyList<FileDiff>>();
        var current   = new List<FileDiff>();
        int usedTokens = OverheadTokens;

        foreach (var file in files)
        {
            int fileTokens = EstimateTokens(file);

            // If even a single file exceeds the limit on its own, include it anyway
            // in its own chunk — the API will handle truncation.
            if (current.Count > 0 && usedTokens + fileTokens > MaxChunkInputTokens)
            {
                chunks.Add(current.ToArray());
                current    = [file];
                usedTokens = OverheadTokens + fileTokens;
            }
            else
            {
                current.Add(file);
                usedTokens += fileTokens;
            }
        }

        if (current.Count > 0)
            chunks.Add(current.ToArray());

        return chunks.Count > 0 ? chunks : [Array.Empty<FileDiff>()];
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int EstimateTokens(FileDiff file)
    {
        // File header line (~50 chars) + patch content
        int chars = 50 + (file.Patch?.Length ?? 0);
        return chars / CharsPerToken;
    }
}
