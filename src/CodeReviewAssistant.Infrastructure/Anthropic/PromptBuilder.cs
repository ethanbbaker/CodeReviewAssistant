using System.Text;
using CodeReviewAssistant.Core.Models;
using CodeReviewAssistant.Core.Parsing;

namespace CodeReviewAssistant.Infrastructure.Anthropic;

/// <summary>
/// Builds the system and user prompts for code-review API requests.
/// </summary>
internal static class PromptBuilder
{
    // ── System prompt ─────────────────────────────────────────────────────────

    internal const string SystemPrompt =
        """
        You are an expert software engineer performing a thorough pull-request code review.

        Your goal is to produce a clear, actionable review that helps the author improve the change.
        Structure your response in Markdown using these sections (omit any section with nothing to say):

        ## Summary
        One or two sentences describing what the PR does and your overall impression.

        ## Issues
        Numbered list of bugs, logic errors, security vulnerabilities, or correctness problems.
        Include the file name and, where possible, the relevant line or snippet.

        ## Suggestions
        Numbered list of non-blocking improvements: readability, performance, naming, test coverage, etc.

        ## Nitpicks (optional)
        Minor style or formatting observations.

        Guidelines:
        - Be specific: quote code or line ranges rather than vague references.
        - Be constructive: explain *why* something is a problem and suggest a concrete fix.
        - Distinguish blocking issues (must fix) from suggestions (nice to have).
        - If a file is binary or has no diff, skip it.
        - If the PR is small and looks good, say so briefly rather than inventing issues.
        """;

    // ── User message builder ───────────────────────────────────────────────────

    /// <summary>
    /// Builds the user-turn message for a single chunk of files within a PR.
    /// </summary>
    /// <param name="context">Pull-request metadata and all file diffs.</param>
    /// <param name="files">The subset of files to include in this request.</param>
    /// <param name="chunkIndex">0-based index of this chunk (for multi-chunk preamble).</param>
    /// <param name="totalChunks">Total number of chunks (1 for a single request).</param>
    /// <param name="options">Review options (focus areas, etc.).</param>
    internal static string BuildUserMessage(
        PullRequestContext         context,
        IReadOnlyList<FileDiff>    files,
        int                        chunkIndex,
        int                        totalChunks,
        ReviewOptions              options)
    {
        var sb = new StringBuilder(capacity: 8 * 1024);

        // ── PR header ────────────────────────────────────────────────────────
        sb.AppendLine($"## Pull Request: {context.Title}");
        sb.AppendLine();
        sb.AppendLine($"**Branch:** `{context.HeadBranch}` → `{context.BaseBranch}`");
        sb.AppendLine($"**Changes:** +{context.TotalAdditions} / -{context.TotalDeletions} across {context.Files.Count} file(s)");

        if (!string.IsNullOrWhiteSpace(context.Description))
        {
            sb.AppendLine();
            sb.AppendLine("**Description:**");
            sb.AppendLine(context.Description.Trim());
        }

        // ── Focus areas ──────────────────────────────────────────────────────
        if (options.FocusAreas.Count > 0)
        {
            sb.AppendLine();
            sb.Append("**Review focus:** ");
            sb.AppendLine(string.Join(", ", options.FocusAreas));
        }

        // ── Chunking preamble ────────────────────────────────────────────────
        if (totalChunks > 1)
        {
            sb.AppendLine();
            sb.AppendLine($"**Note:** This PR has been split across {totalChunks} review requests " +
                          $"due to its size. You are reviewing chunk {chunkIndex + 1} of {totalChunks} " +
                          $"({files.Count} file(s) in this chunk).");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // ── File diffs ───────────────────────────────────────────────────────
        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];

            sb.AppendLine($"### `{file.FileName}` [{file.Status}]");

            if (file.IsBinary)
            {
                sb.AppendLine("*(binary file — no diff available)*");
                sb.AppendLine();
                continue;
            }

            if (string.IsNullOrWhiteSpace(file.Patch))
            {
                sb.AppendLine("*(no changes)*");
                sb.AppendLine();
                continue;
            }

            sb.AppendLine($"+{file.Additions} / -{file.Deletions}");

            if (file.PreviousFileName is not null)
                sb.AppendLine($"*(renamed from `{file.PreviousFileName}`)*");

            sb.AppendLine();
            sb.AppendLine("```diff");
            sb.AppendLine(file.Patch);
            sb.AppendLine("```");

            if (file.IsTruncated)
                sb.AppendLine($"> ⚠️ Diff truncated — only the first {DiffParser.MaxPatchLines} lines shown.");

            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("Please review the diff above and provide your structured feedback.");

        return sb.ToString();
    }
}
