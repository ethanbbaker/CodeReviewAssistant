using CodeReviewAssistant.Core.Models;
using CodeReviewAssistant.Infrastructure.Anthropic;

namespace CodeReviewAssistant.Tests.Anthropic;

/// <summary>
/// Tests for <see cref="FileChunker.Chunk"/>.
/// </summary>
public class FileChunkerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="FileDiff"/> with a patch of approximately
    /// <paramref name="patchChars"/> characters.
    /// </summary>
    private static FileDiff MakeDiff(string fileName = "file.cs", int patchChars = 100)
    {
        var patch = new string('x', patchChars);
        return new FileDiff(fileName, patch, DiffStatus.Modified, 10, 5);
    }

    private static FileDiff MakeBinaryDiff(string fileName = "image.png")
        => new(fileName, string.Empty, DiffStatus.Binary, 0, 0);

    private static PullRequestContext MakePr(IReadOnlyList<FileDiff> files)
        => new("Test PR", string.Empty, "main", "feature/test", files);

    // Each token ≈ 4 chars; file header overhead is 50 chars ≈ 13 tokens.
    // MaxChunkInputTokens = 140_000; OverheadTokens = 1_500.
    // Usable tokens per chunk ≈ 138_500 ≈ 554_000 chars of patch content.
    private const int MaxPatchCharsPerChunk = (140_000 - 1_500) * 4; // 554_000

    // ── Single-chunk behaviour ────────────────────────────────────────────────

    [Fact]
    public void SmallPr_ProducesSingleChunk()
    {
        var files = new[] { MakeDiff(patchChars: 100), MakeDiff(patchChars: 200) };
        var ctx   = MakePr(files);

        var chunks = FileChunker.Chunk(ctx);

        Assert.Single(chunks);
    }

    [Fact]
    public void EmptyFileList_ProducesSingleEmptyChunk()
    {
        var ctx = MakePr([]);

        var chunks = FileChunker.Chunk(ctx);

        Assert.Single(chunks);
        Assert.Empty(chunks[0]);
    }

    [Fact]
    public void SingleFile_AlwaysInFirstChunk()
    {
        var files = new[] { MakeDiff("only.cs", patchChars: 500) };
        var ctx   = MakePr(files);

        var chunks = FileChunker.Chunk(ctx);

        Assert.Single(chunks);
        Assert.Single(chunks[0]);
        Assert.Equal("only.cs", chunks[0][0].FileName);
    }

    // ── Multi-chunk behaviour ─────────────────────────────────────────────────

    [Fact]
    public void LargePr_ProducesMultipleChunks()
    {
        // Two files each slightly over half the limit → must split into 2 chunks.
        int halfLimit = MaxPatchCharsPerChunk / 2 + 10_000;
        var files     = new[] { MakeDiff("a.cs", halfLimit), MakeDiff("b.cs", halfLimit) };
        var ctx       = MakePr(files);

        var chunks = FileChunker.Chunk(ctx);

        Assert.True(chunks.Count >= 2, $"Expected ≥2 chunks but got {chunks.Count}");
    }

    [Fact]
    public void AllFiles_AppearAcrossChunks()
    {
        // Many medium-sized files to force chunking.
        int filesCount = 20;
        int patchChars = MaxPatchCharsPerChunk / 4; // 4 files per chunk
        var files      = Enumerable.Range(1, filesCount)
                                   .Select(i => MakeDiff($"file{i:D2}.cs", patchChars))
                                   .ToArray();
        var ctx = MakePr(files);

        var chunks   = FileChunker.Chunk(ctx);
        var allNames = chunks.SelectMany(c => c).Select(f => f.FileName).ToHashSet();

        for (int i = 1; i <= filesCount; i++)
            Assert.Contains($"file{i:D2}.cs", allNames);
    }

    [Fact]
    public void TotalFileCount_AcrossAllChunks_MatchesInput()
    {
        int filesCount = 15;
        int patchChars = MaxPatchCharsPerChunk / 3;
        var files      = Enumerable.Range(1, filesCount)
                                   .Select(i => MakeDiff($"f{i}.cs", patchChars))
                                   .ToArray();
        var ctx = MakePr(files);

        var chunks    = FileChunker.Chunk(ctx);
        int totalSeen = chunks.Sum(c => c.Count);

        Assert.Equal(filesCount, totalSeen);
    }

    [Fact]
    public void EachChunk_IsNonEmpty()
    {
        int halfLimit = MaxPatchCharsPerChunk / 2 + 10_000;
        var files     = new[]
        {
            MakeDiff("a.cs", halfLimit),
            MakeDiff("b.cs", halfLimit),
            MakeDiff("c.cs", halfLimit),
        };
        var ctx = MakePr(files);

        var chunks = FileChunker.Chunk(ctx);

        Assert.All(chunks, chunk => Assert.NotEmpty(chunk));
    }

    [Fact]
    public void EachChunk_FileOrder_MatchesInputOrder()
    {
        // Files are large enough to land in separate chunks; order should be preserved.
        int halfLimit = MaxPatchCharsPerChunk / 2 + 10_000;
        var fileNames = new[] { "alpha.cs", "beta.cs", "gamma.cs" };
        var files     = fileNames.Select(n => MakeDiff(n, halfLimit)).ToArray();
        var ctx       = MakePr(files);

        var chunks        = FileChunker.Chunk(ctx);
        var orderedNames  = chunks.SelectMany(c => c).Select(f => f.FileName).ToList();

        Assert.Equal(fileNames, orderedNames);
    }

    // ── Oversized single file ─────────────────────────────────────────────────

    [Fact]
    public void OversizedSingleFile_LandsInOwnChunk()
    {
        // A file larger than the entire limit still gets its own chunk.
        int massive = MaxPatchCharsPerChunk * 2;
        var files   = new[] { MakeDiff("huge.cs", massive) };
        var ctx     = MakePr(files);

        var chunks = FileChunker.Chunk(ctx);

        Assert.Single(chunks);
        Assert.Single(chunks[0]);
        Assert.Equal("huge.cs", chunks[0][0].FileName);
    }

    [Fact]
    public void OversizedFile_DoesNotPreventSubsequentFilesFromBeingIncluded()
    {
        // Huge file first, then two small files — all must appear.
        int massive = MaxPatchCharsPerChunk * 2;
        var files   = new[]
        {
            MakeDiff("huge.cs",  massive),
            MakeDiff("small1.cs", 200),
            MakeDiff("small2.cs", 200),
        };
        var ctx = MakePr(files);

        var chunks   = FileChunker.Chunk(ctx);
        var allNames = chunks.SelectMany(c => c).Select(f => f.FileName).ToHashSet();

        Assert.Contains("huge.cs",   allNames);
        Assert.Contains("small1.cs", allNames);
        Assert.Contains("small2.cs", allNames);
    }

    // ── Binary / empty patch files ────────────────────────────────────────────

    [Fact]
    public void BinaryFiles_AreIncludedInChunks()
    {
        var files = new[]
        {
            MakeDiff("code.cs"),
            MakeBinaryDiff("logo.png"),
        };
        var ctx = MakePr(files);

        var chunks   = FileChunker.Chunk(ctx);
        var allNames = chunks.SelectMany(c => c).Select(f => f.FileName).ToHashSet();

        Assert.Contains("code.cs",   allNames);
        Assert.Contains("logo.png",  allNames);
    }

    [Fact]
    public void BinaryFile_CountsAsSmall_DoesNotCauseSplit()
    {
        // Binary file has no patch → near-zero token cost → shouldn't force a new chunk.
        var files = new[]
        {
            MakeDiff("code.cs",   patchChars: 500),
            MakeBinaryDiff("img.png"),
        };
        var ctx = MakePr(files);

        var chunks = FileChunker.Chunk(ctx);

        Assert.Single(chunks);
    }

    // ── Return-type contract ──────────────────────────────────────────────────

    [Fact]
    public void ReturnValue_IsNeverNull()
    {
        var ctx = MakePr([]);

        var chunks = FileChunker.Chunk(ctx);

        Assert.NotNull(chunks);
    }

    [Fact]
    public void ReturnValue_IsNeverEmpty()
    {
        var ctx = MakePr([]);

        var chunks = FileChunker.Chunk(ctx);

        Assert.True(chunks.Count > 0);
    }
}
