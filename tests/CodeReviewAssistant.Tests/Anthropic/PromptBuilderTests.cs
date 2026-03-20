using CodeReviewAssistant.Core.Models;
using CodeReviewAssistant.Infrastructure.Anthropic;

namespace CodeReviewAssistant.Tests.Anthropic;

/// <summary>
/// Tests for <see cref="PromptBuilder.BuildUserMessage"/>.
/// </summary>
public class PromptBuilderTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FileDiff MakeDiff(
        string     fileName         = "src/Foo.cs",
        string     patch            = "@@ -1,3 +1,4 @@\n context\n+added line\n context",
        DiffStatus status           = DiffStatus.Modified,
        int        additions        = 1,
        int        deletions        = 0,
        bool       isTruncated      = false,
        string?    previousFileName = null)
        => new(fileName, patch, status, additions, deletions)
        {
            IsTruncated      = isTruncated,
            PreviousFileName = previousFileName,
        };

    private static PullRequestContext MakePr(
        string                  title       = "My PR",
        string                  description = "",
        string                  baseBranch  = "main",
        string                  headBranch  = "feature/test",
        IReadOnlyList<FileDiff>? files      = null)
        => new(title, description, baseBranch, headBranch,
               files ?? [MakeDiff()]);

    private static ReviewOptions DefaultOptions() => new()
    {
        EnabledCategories    = new HashSet<FindingCategory>(Enum.GetValues<FindingCategory>()),
        MinimumSeverity      = FindingSeverity.Low,
        IncludeSuggestedFixes = true,
        FocusAreas           = null,
    };

    // ── PR header ─────────────────────────────────────────────────────────────

    [Fact]
    public void IncludesPrTitle()
    {
        var ctx    = MakePr(title: "Fix the critical bug");
        var result = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 0, 1, DefaultOptions());

        Assert.Contains("Fix the critical bug", result);
    }

    [Fact]
    public void IncludesBranchNames()
    {
        var ctx    = MakePr(baseBranch: "main", headBranch: "feature/xyz");
        var result = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 0, 1, DefaultOptions());

        Assert.Contains("feature/xyz", result);
        Assert.Contains("main",        result);
    }

    [Fact]
    public void IncludesDescription_WhenPresent()
    {
        var ctx    = MakePr(description: "This PR fixes a nasty race condition.");
        var result = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 0, 1, DefaultOptions());

        Assert.Contains("This PR fixes a nasty race condition.", result);
    }

    [Fact]
    public void OmitsDescriptionSection_WhenEmpty()
    {
        var ctx    = MakePr(description: "");
        var result = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 0, 1, DefaultOptions());

        Assert.DoesNotContain("**Description:**", result);
    }

    [Fact]
    public void OmitsDescriptionSection_WhenWhitespaceOnly()
    {
        var ctx    = MakePr(description: "   \n  ");
        var result = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 0, 1, DefaultOptions());

        Assert.DoesNotContain("**Description:**", result);
    }

    // ── Review instructions ───────────────────────────────────────────────────

    [Fact]
    public void IncludesCategoryLabels()
    {
        var options = new ReviewOptions
        {
            EnabledCategories = new HashSet<FindingCategory> { FindingCategory.Security, FindingCategory.Performance },
        };
        var ctx    = MakePr();
        var result = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 0, 1, options);

        Assert.Contains("Security",    result);
        Assert.Contains("Performance", result);
    }

    [Fact]
    public void IncludesMinimumSeverity_AsUppercase()
    {
        var options = new ReviewOptions { MinimumSeverity = FindingSeverity.High };
        var ctx     = MakePr();
        var result  = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 0, 1, options);

        Assert.Contains("HIGH", result);
    }

    [Fact]
    public void IncludeSuggestedFixes_Yes()
    {
        var options = new ReviewOptions { IncludeSuggestedFixes = true };
        var ctx     = MakePr();
        var result  = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 0, 1, options);

        Assert.Contains("Suggested fixes:** Yes", result);
    }

    [Fact]
    public void IncludeSuggestedFixes_No()
    {
        var options = new ReviewOptions { IncludeSuggestedFixes = false };
        var ctx     = MakePr();
        var result  = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 0, 1, options);

        Assert.Contains("Suggested fixes:** No", result);
    }

    [Fact]
    public void IncludesFocusAreas_WhenSet()
    {
        var options = new ReviewOptions { FocusAreas = "SQL injection, null checks" };
        var ctx     = MakePr();
        var result  = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 0, 1, options);

        Assert.Contains("SQL injection, null checks", result);
        Assert.Contains("Special focus:",              result);
    }

    [Fact]
    public void OmitsFocusAreaSection_WhenNull()
    {
        var options = new ReviewOptions { FocusAreas = null };
        var ctx     = MakePr();
        var result  = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 0, 1, options);

        Assert.DoesNotContain("Special focus:", result);
    }

    [Fact]
    public void OmitsFocusAreaSection_WhenWhitespaceOnly()
    {
        var options = new ReviewOptions { FocusAreas = "   " };
        var ctx     = MakePr();
        var result  = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 0, 1, options);

        Assert.DoesNotContain("Special focus:", result);
    }

    // ── Chunking preamble ─────────────────────────────────────────────────────

    [Fact]
    public void OmitsChunkingPreamble_ForSingleChunk()
    {
        var ctx    = MakePr();
        var result = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 0, 1, DefaultOptions());

        Assert.DoesNotContain("split across", result);
        Assert.DoesNotContain("chunk 1 of",   result);
    }

    [Fact]
    public void IncludesChunkingPreamble_ForMultipleChunks()
    {
        var ctx    = MakePr();
        var result = PromptBuilder.BuildUserMessage(ctx, ctx.Files, 1, 3, DefaultOptions());

        Assert.Contains("split across 3 review requests", result);
        Assert.Contains("chunk 2 of 3",                    result);
    }

    [Fact]
    public void ChunkingPreamble_ShowsCorrectFileCount()
    {
        var files  = new[] { MakeDiff("a.cs"), MakeDiff("b.cs") };
        var ctx    = MakePr(files: files);
        var result = PromptBuilder.BuildUserMessage(ctx, files, 0, 2, DefaultOptions());

        Assert.Contains("2 file(s) in this chunk", result);
    }

    // ── File diff rendering ───────────────────────────────────────────────────

    [Fact]
    public void IncludesFileName_InDiffSection()
    {
        var files  = new[] { MakeDiff("src/MyClass.cs") };
        var ctx    = MakePr(files: files);
        var result = PromptBuilder.BuildUserMessage(ctx, files, 0, 1, DefaultOptions());

        Assert.Contains("src/MyClass.cs", result);
    }

    [Fact]
    public void IncludesPatch_InFencedBlock()
    {
        const string patch = "@@ -1,2 +1,3 @@\n context\n+new line";
        var files  = new[] { MakeDiff(patch: patch) };
        var ctx    = MakePr(files: files);
        var result = PromptBuilder.BuildUserMessage(ctx, files, 0, 1, DefaultOptions());

        Assert.Contains("```diff",  result);
        Assert.Contains(patch,      result);
        Assert.Contains("```",      result);
    }

    [Fact]
    public void BinaryFile_ShowsNoDiffAvailable()
    {
        var files  = new[] { new FileDiff("image.png", string.Empty, DiffStatus.Binary, 0, 0) };
        var ctx    = MakePr(files: files);
        var result = PromptBuilder.BuildUserMessage(ctx, files, 0, 1, DefaultOptions());

        Assert.Contains("binary file — no diff available", result);
        Assert.DoesNotContain("```diff",                    result);
    }

    [Fact]
    public void EmptyPatch_ShowsNoChanges()
    {
        var files  = new[] { MakeDiff(patch: "") };
        var ctx    = MakePr(files: files);
        var result = PromptBuilder.BuildUserMessage(ctx, files, 0, 1, DefaultOptions());

        Assert.Contains("*(no changes)*", result);
    }

    [Fact]
    public void RenamedFile_ShowsPreviousFileName()
    {
        var files  = new[] { MakeDiff("new/path.cs", previousFileName: "old/path.cs", status: DiffStatus.Renamed) };
        var ctx    = MakePr(files: files);
        var result = PromptBuilder.BuildUserMessage(ctx, files, 0, 1, DefaultOptions());

        Assert.Contains("renamed from", result);
        Assert.Contains("old/path.cs",  result);
    }

    [Fact]
    public void TruncatedFile_ShowsTruncationWarning()
    {
        var files  = new[] { MakeDiff(isTruncated: true) };
        var ctx    = MakePr(files: files);
        var result = PromptBuilder.BuildUserMessage(ctx, files, 0, 1, DefaultOptions());

        Assert.Contains("Diff truncated", result);
    }

    [Fact]
    public void MultipleFiles_AllAppearInOutput()
    {
        var files = new[]
        {
            MakeDiff("alpha.cs"),
            MakeDiff("beta.ts"),
            MakeDiff("gamma.py"),
        };
        var ctx    = MakePr(files: files);
        var result = PromptBuilder.BuildUserMessage(ctx, files, 0, 1, DefaultOptions());

        Assert.Contains("alpha.cs", result);
        Assert.Contains("beta.ts",  result);
        Assert.Contains("gamma.py", result);
    }
}
