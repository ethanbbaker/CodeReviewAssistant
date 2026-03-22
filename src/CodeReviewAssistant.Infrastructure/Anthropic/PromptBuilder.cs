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

        ## HARD RULE — Minimum Severity
        Every user message contains a "Minimum severity to report" field.
        Severity scale from highest to lowest: CRITICAL, HIGH, MEDIUM, LOW, INFO.
        You MUST omit every finding whose severity is below that threshold — do not mention it,
        summarise it, or reference it in any way.  This rule applies to every section and every
        category in your response, without exception.

        ## Output format
        Your goal is to produce a clear, actionable review that helps the author improve the change.
        Structure your response in Markdown.  Include only the sections relevant to the categories
        you have been asked to cover (listed in the user message).

        For each section use this format:

        ## <Section Name>
        Numbered list of findings.  For each finding state:
        - **[SEVERITY]** `path/to/file.ext` — concise description (explain *why* it is a problem).
        - If suggested fixes are requested, follow with a fenced code block showing the fix.

        Severity levels (use exactly these labels): CRITICAL · HIGH · MEDIUM · LOW · INFO

        ## Summary
        Always include this section last.  One or two sentences: what the PR does and your overall
        impression.  If the PR looks good, say so briefly rather than inventing issues.

        ## Other guidelines
        - Be specific: quote code or line ranges rather than vague references.
        - Be constructive: explain *why* something is a problem and suggest a concrete fix.
        - Distinguish blocking issues from non-blocking suggestions.
        - Skip binary files or files with no diff.
        """;

    // ── User message builder ───────────────────────────────────────────────────

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

        // ── Review instructions ───────────────────────────────────────────────
        sb.AppendLine();
        sb.AppendLine("## Review Instructions");

        // Categories
        var enabledNames = options.EnabledCategories
            .OrderBy(c => (int)c)
            .Select(CategoryLabel);
        sb.AppendLine($"**Categories to cover:** {string.Join(", ", enabledNames)}");

        // Minimum severity — be explicit about ordering and use a hard rule
        var minSev     = options.MinimumSeverity.ToString().ToUpperInvariant();
        var sevOrder   = "CRITICAL (highest) → HIGH → MEDIUM → LOW → INFO (lowest)";
        var suppressed = options.MinimumSeverity > FindingSeverity.Info
            ? $" Do NOT mention any finding rated below {minSev} — omit it entirely."
            : string.Empty;
        sb.AppendLine($"**Minimum severity to report:** {minSev}");
        sb.AppendLine($"**Severity scale:** {sevOrder}");
        sb.AppendLine($"**IMPORTANT:** Only include findings rated {minSev} or higher.{suppressed}");

        // Suggested fixes
        sb.AppendLine(options.IncludeSuggestedFixes
            ? "**Suggested fixes:** Yes — include a fenced code-block fix for each issue where practical."
            : "**Suggested fixes:** No — describe the problem only, do not include code fixes.");

        // Optional free-text focus
        if (!string.IsNullOrWhiteSpace(options.FocusAreas))
        {
            sb.AppendLine();
            sb.AppendLine($"**Special focus:** {options.FocusAreas.Trim()}");
        }

        // ── Chunking preamble ─────────────────────────────────────────────────
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

        // ── File diffs ────────────────────────────────────────────────────────
        foreach (var file in files)
        {
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
        sb.AppendLine("Please review the diff above following the instructions and provide your structured feedback.");

        return sb.ToString();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string CategoryLabel(FindingCategory c) => c switch
    {
        FindingCategory.Security        => "Security",
        FindingCategory.Performance     => "Performance",
        FindingCategory.Correctness     => "Correctness / Bugs",
        FindingCategory.Maintainability => "Maintainability",
        FindingCategory.TestCoverage    => "Test Coverage",
        FindingCategory.Style           => "Style / Nitpicks",
        _                               => c.ToString(),
    };
}
