using CodeReviewAssistant.Core.Models;
using CodeReviewAssistant.Core.Parsing;

namespace CodeReviewAssistant.Tests.Parsing;

public class DiffParserTests
{
    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    /// <summary>Builds a minimal PullRequestFile with the given properties.</summary>
    private static PullRequestFile MakeFile(
        string  filename,
        string  status,
        int     additions = 0,
        int     deletions = 0,
        int     changes   = 0,
        string? patch     = null,
        string? previous  = null)
        => new()
        {
            Filename         = filename,
            Status           = status,
            Additions        = additions,
            Deletions        = deletions,
            Changes          = changes,
            Patch            = patch,
            PreviousFilename = previous
        };

    /// <summary>Builds a minimal PullRequestInfo for Parse() tests.</summary>
    private static PullRequestInfo MakePrInfo(
        string title   = "My PR",
        string body    = "Description",
        string baseRef = "main",
        string headRef = "feature/test")
        => new()
        {
            Number   = 1,
            Title    = title,
            Body     = body,
            BaseRef  = baseRef,
            HeadRef  = headRef,
            State    = "open"
        };

    /// <summary>Generates a patch with <paramref name="lineCount"/> '+' lines.</summary>
    private static string MakeLargePatch(int lineCount)
    {
        var lines = Enumerable
            .Range(1, lineCount)
            .Select(i => $"+line {i}");
        return "@@ -0,0 +1," + lineCount + " @@\n" + string.Join('\n', lines);
    }

    // ---------------------------------------------------------------
    // ResolveStatus — status mapping
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("added",    DiffStatus.Added)]
    [InlineData("modified", DiffStatus.Modified)]
    [InlineData("deleted",  DiffStatus.Deleted)]
    [InlineData("renamed",  DiffStatus.Renamed)]
    [InlineData("copied",   DiffStatus.Copied)]
    public void ResolveStatus_KnownStatusStrings_MapCorrectly(
        string rawStatus, DiffStatus expected)
    {
        var file   = MakeFile("foo.cs", rawStatus, patch: "@@ @@\n+x");
        var result = DiffParser.ResolveStatus(file);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveStatus_UnknownStatusString_ReturnsUnknown()
    {
        var file   = MakeFile("foo.cs", "weird-status", patch: "@@ @@\n+x");
        var result = DiffParser.ResolveStatus(file);
        Assert.Equal(DiffStatus.Unknown, result);
    }

    [Theory]
    [InlineData("ADDED")]
    [InlineData("Modified")]
    [InlineData("DELETED")]
    public void ResolveStatus_StatusStrings_AreCaseInsensitive(string rawStatus)
    {
        var file   = MakeFile("foo.cs", rawStatus, patch: "@@ @@\n+x");
        var result = DiffParser.ResolveStatus(file);
        Assert.NotEqual(DiffStatus.Unknown, result);
    }

    // ---------------------------------------------------------------
    // ResolveStatus — binary file detection
    // ---------------------------------------------------------------

    [Fact]
    public void ResolveStatus_NullPatchWithChanges_ReturnsBinary()
    {
        // GitHub omits the patch for binary files but still reports changes
        var file   = MakeFile("logo.png", "modified", changes: 1, patch: null);
        var result = DiffParser.ResolveStatus(file);
        Assert.Equal(DiffStatus.Binary, result);
    }

    [Fact]
    public void ResolveStatus_NullPatchZeroChanges_UsesStatusString()
    {
        // Newly-added empty file: patch is null, changes is 0 → not binary
        var file   = MakeFile("empty.txt", "added", changes: 0, patch: null);
        var result = DiffParser.ResolveStatus(file);
        Assert.Equal(DiffStatus.Added, result);
    }

    [Fact]
    public void ResolveStatus_BinaryFile_IsBinaryPropertyIsTrue()
    {
        var file  = MakeFile("data.bin", "modified", changes: 5, patch: null);
        var diffs = DiffParser.ParseFiles(new[] { file });
        Assert.True(diffs[0].IsBinary);
    }

    // ---------------------------------------------------------------
    // TrimPatch — truncation
    // ---------------------------------------------------------------

    [Fact]
    public void TrimPatch_NullPatch_ReturnsEmptyStringNotTruncated()
    {
        var (patch, truncated) = DiffParser.TrimPatch(null);
        Assert.Equal(string.Empty, patch);
        Assert.False(truncated);
    }

    [Fact]
    public void TrimPatch_ShortPatch_ReturnsUnchangedNotTruncated()
    {
        var raw            = "@@ -1,2 +1,3 @@\n context\n-old\n+new";
        var (patch, trunc) = DiffParser.TrimPatch(raw);
        Assert.Equal(raw, patch);
        Assert.False(trunc);
    }

    [Fact]
    public void TrimPatch_PatchExceedsMaxLines_TruncatesAndFlagsTrue()
    {
        var raw            = MakeLargePatch(DiffParser.MaxPatchLines + 50);
        var (patch, trunc) = DiffParser.TrimPatch(raw);

        Assert.True(trunc);
        Assert.Equal(DiffParser.MaxPatchLines, patch.Split('\n').Length);
    }

    [Fact]
    public void TrimPatch_PatchAtExactlyMaxLines_NotTruncated()
    {
        // MakeLargePatch(N) produces 1 header line + N content lines = N+1 total.
        // To get exactly MaxPatchLines lines we therefore pass MaxPatchLines - 1.
        var raw        = MakeLargePatch(DiffParser.MaxPatchLines - 1);
        var (_, trunc) = DiffParser.TrimPatch(raw);
        Assert.False(trunc);
    }

    // ---------------------------------------------------------------
    // ParseFiles — renamed files
    // ---------------------------------------------------------------

    [Fact]
    public void ParseFiles_RenamedFile_SetsPreviousFileName()
    {
        var file  = MakeFile("new/path.cs", "renamed",
            additions: 0, deletions: 0, changes: 0,
            patch: "@@ @@\n context", previous: "old/path.cs");

        var result = DiffParser.ParseFiles(new[] { file });

        Assert.Equal("old/path.cs", result[0].PreviousFileName);
        Assert.Equal(DiffStatus.Renamed, result[0].Status);
    }

    [Fact]
    public void ParseFiles_NonRenamedFile_PreviousFileNameIsNull()
    {
        var file   = MakeFile("src/Foo.cs", "modified", patch: "@@ @@\n+x");
        var result = DiffParser.ParseFiles(new[] { file });
        Assert.Null(result[0].PreviousFileName);
    }

    [Fact]
    public void ParseFiles_EmptyPreviousFilename_PreviousFileNameIsNull()
    {
        var file   = MakeFile("src/Foo.cs", "modified", patch: "@@ @@\n+x", previous: "");
        var result = DiffParser.ParseFiles(new[] { file });
        Assert.Null(result[0].PreviousFileName);
    }

    // ---------------------------------------------------------------
    // ParseFiles — additions / deletions / NetChange
    // ---------------------------------------------------------------

    [Fact]
    public void ParseFiles_AddedFile_MapsAdditionsAndDeletions()
    {
        var file   = MakeFile("New.cs", "added", additions: 10, deletions: 0, patch: "@@ @@\n+x");
        var result = DiffParser.ParseFiles(new[] { file });

        Assert.Equal(10,               result[0].Additions);
        Assert.Equal(0,                result[0].Deletions);
        Assert.Equal(10,               result[0].NetChange);
        Assert.Equal(DiffStatus.Added, result[0].Status);
    }

    [Fact]
    public void ParseFiles_DeletedFile_MapsCorrectly()
    {
        var file   = MakeFile("Old.cs", "deleted", additions: 0, deletions: 5, patch: "@@ @@\n-x");
        var result = DiffParser.ParseFiles(new[] { file });

        Assert.Equal(DiffStatus.Deleted, result[0].Status);
        Assert.Equal(-5,                 result[0].NetChange);
    }

    [Fact]
    public void ParseFiles_ModifiedFile_MapsCorrectly()
    {
        const string patch = "@@ -1,3 +1,4 @@\n context\n-old\n+new\n+extra";
        var file   = MakeFile("Foo.cs", "modified", additions: 2, deletions: 1, patch: patch);
        var result = DiffParser.ParseFiles(new[] { file });

        Assert.Equal("Foo.cs",           result[0].FileName);
        Assert.Equal(DiffStatus.Modified, result[0].Status);
        Assert.Equal(patch,              result[0].Patch);
        Assert.False(result[0].IsTruncated);
    }

    // ---------------------------------------------------------------
    // ParseFiles — large diff truncation end-to-end
    // ---------------------------------------------------------------

    [Fact]
    public void ParseFiles_LargeDiff_TruncatesToMaxPatchLines()
    {
        var file   = MakeFile("Big.cs", "modified",
            patch: MakeLargePatch(DiffParser.MaxPatchLines + 100));
        var result = DiffParser.ParseFiles(new[] { file });

        Assert.True(result[0].IsTruncated);
        Assert.Equal(DiffParser.MaxPatchLines, result[0].Patch.Split('\n').Length);
    }

    [Fact]
    public void ParseFiles_SmallDiff_NotTruncated()
    {
        var file   = MakeFile("Small.cs", "modified", patch: "@@ -1 +1 @@\n-a\n+b");
        var result = DiffParser.ParseFiles(new[] { file });
        Assert.False(result[0].IsTruncated);
    }

    // ---------------------------------------------------------------
    // ParseFiles — binary files
    // ---------------------------------------------------------------

    [Fact]
    public void ParseFiles_BinaryFile_EmptyPatchNotTruncated()
    {
        var file   = MakeFile("image.png", "modified", changes: 1, patch: null);
        var result = DiffParser.ParseFiles(new[] { file });

        Assert.Equal(DiffStatus.Binary, result[0].Status);
        Assert.Equal(string.Empty,      result[0].Patch);
        Assert.False(result[0].IsTruncated);
        Assert.True(result[0].IsBinary);
    }

    // ---------------------------------------------------------------
    // ParseFiles — empty list
    // ---------------------------------------------------------------

    [Fact]
    public void ParseFiles_EmptyList_ReturnsEmptyCollection()
    {
        var result = DiffParser.ParseFiles(Array.Empty<PullRequestFile>());
        Assert.Empty(result);
    }

    // ---------------------------------------------------------------
    // Parse — PullRequestContext assembly
    // ---------------------------------------------------------------

    [Fact]
    public void Parse_WithPrInfoAndFiles_BuildsContextCorrectly()
    {
        var prInfo = MakePrInfo(
            title:   "Fix null ref",
            body:    "Closes #123",
            baseRef: "main",
            headRef: "fix/null-ref");

        var files = new[]
        {
            MakeFile("A.cs", "modified", additions: 2, deletions: 1, patch: "@@ @@\n+x"),
            MakeFile("B.cs", "added",    additions: 5, deletions: 0, patch: "@@ @@\n+y")
        };

        var ctx = DiffParser.Parse(prInfo, files);

        Assert.Equal("Fix null ref", ctx.Title);
        Assert.Equal("Closes #123", ctx.Description);
        Assert.Equal("main",        ctx.BaseBranch);
        Assert.Equal("fix/null-ref",ctx.HeadBranch);
        Assert.Equal(2,             ctx.Files.Count);
        Assert.Equal(7,             ctx.TotalAdditions);
        Assert.Equal(1,             ctx.TotalDeletions);
    }

    [Fact]
    public void Parse_WithNoFiles_BuildsContextWithEmptyFiles()
    {
        var ctx = DiffParser.Parse(MakePrInfo(), Array.Empty<PullRequestFile>());
        Assert.Empty(ctx.Files);
        Assert.Equal(0, ctx.TotalAdditions);
        Assert.Equal(0, ctx.TotalDeletions);
    }

    // ---------------------------------------------------------------
    // PullRequestContext — aggregate helpers
    // ---------------------------------------------------------------

    [Fact]
    public void PullRequestContext_TruncatedFileCount_CountsOnlyTruncatedFiles()
    {
        var largePatch = MakeLargePatch(DiffParser.MaxPatchLines + 10);
        var files = new[]
        {
            MakeFile("Big.cs",   "modified", patch: largePatch),
            MakeFile("Small.cs", "modified", patch: "@@ @@\n+x")
        };

        var ctx = DiffParser.Parse(MakePrInfo(), files);
        Assert.Equal(1, ctx.TruncatedFileCount);
    }

    [Fact]
    public void PullRequestContext_BinaryFileCount_CountsOnlyBinaryFiles()
    {
        var files = new[]
        {
            MakeFile("logo.png", "modified", changes: 1, patch: null),
            MakeFile("Foo.cs",   "modified", patch: "@@ @@\n+x")
        };

        var ctx = DiffParser.Parse(MakePrInfo(), files);
        Assert.Equal(1, ctx.BinaryFileCount);
        Assert.Equal(0, ctx.TruncatedFileCount);
    }

    // ---------------------------------------------------------------
    // FileDiff — IsBinary convenience property
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(DiffStatus.Added,    false)]
    [InlineData(DiffStatus.Modified, false)]
    [InlineData(DiffStatus.Deleted,  false)]
    [InlineData(DiffStatus.Renamed,  false)]
    [InlineData(DiffStatus.Binary,   true)]
    [InlineData(DiffStatus.Unknown,  false)]
    public void FileDiff_IsBinary_OnlyTrueForBinaryStatus(DiffStatus status, bool expectedBinary)
    {
        var diff = new FileDiff("f.cs", string.Empty, status, 0, 0);
        Assert.Equal(expectedBinary, diff.IsBinary);
    }
}
