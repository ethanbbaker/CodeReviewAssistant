using CodeReviewAssistant.Core.Models;

namespace CodeReviewAssistant.Tests.Anthropic;

/// <summary>
/// Tests for <see cref="ReviewChunk"/> and <see cref="ReviewSessionState"/>.
/// </summary>
public class ReviewChunkTests
{
    // ── ReviewChunk: text chunks ──────────────────────────────────────────────

    [Fact]
    public void TextChunk_HasIsProgressFalse()
    {
        var chunk = new ReviewChunk("some text");

        Assert.False(chunk.IsProgress);
    }

    [Fact]
    public void TextChunk_PreservesText()
    {
        var chunk = new ReviewChunk("## Security\n1. **[HIGH]** foo — bar");

        Assert.Equal("## Security\n1. **[HIGH]** foo — bar", chunk.Text);
    }

    [Fact]
    public void TextChunk_DefaultChunkIndexIsZero()
    {
        var chunk = new ReviewChunk("text");

        Assert.Equal(0, chunk.ChunkIndex);
        Assert.Equal(0, chunk.TotalChunks);
    }

    // ── ReviewChunk.Progress factory ──────────────────────────────────────────

    [Fact]
    public void Progress_HasIsProgressTrue()
    {
        var chunk = ReviewChunk.Progress(0, 3);

        Assert.True(chunk.IsProgress);
    }

    [Fact]
    public void Progress_TextIsEmpty()
    {
        var chunk = ReviewChunk.Progress(1, 4);

        Assert.Equal(string.Empty, chunk.Text);
    }

    [Fact]
    public void Progress_SetsChunkIndex()
    {
        var chunk = ReviewChunk.Progress(2, 5);

        Assert.Equal(2, chunk.ChunkIndex);
    }

    [Fact]
    public void Progress_SetsTotalChunks()
    {
        var chunk = ReviewChunk.Progress(0, 7);

        Assert.Equal(7, chunk.TotalChunks);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 3)]
    [InlineData(4, 5)]
    public void Progress_RoundTrips_IndexAndTotal(int index, int total)
    {
        var chunk = ReviewChunk.Progress(index, total);

        Assert.Equal(index, chunk.ChunkIndex);
        Assert.Equal(total, chunk.TotalChunks);
    }

    // ── ReviewChunk record equality ───────────────────────────────────────────

    [Fact]
    public void Progress_ChunksWithSameValues_AreEqual()
    {
        var a = ReviewChunk.Progress(1, 3);
        var b = ReviewChunk.Progress(1, 3);

        Assert.Equal(a, b);
    }

    [Fact]
    public void TextChunks_WithSameText_AreEqual()
    {
        var a = new ReviewChunk("hello");
        var b = new ReviewChunk("hello");

        Assert.Equal(a, b);
    }

    // ── ReviewSessionState ────────────────────────────────────────────────────

    [Fact]
    public void NewState_HasIdleStatus()
    {
        var state = new ReviewSessionState();

        Assert.Equal(ReviewRunStatus.Idle, state.Status);
    }

    [Fact]
    public void NewState_HasNoContent()
    {
        var state = new ReviewSessionState();

        Assert.False(state.HasContent);
    }

    [Fact]
    public void HasContent_IsTrueWhenMarkdownAccumulated()
    {
        var state = new ReviewSessionState
        {
            AccumulatedMarkdown = "## Review\nLooks good."
        };

        Assert.True(state.HasContent);
    }

    [Fact]
    public void Reset_ClearsAccumulatedMarkdown()
    {
        var state = new ReviewSessionState
        {
            AccumulatedMarkdown = "## Some review content",
            Status = ReviewRunStatus.Complete,
        };

        state.Reset();

        Assert.Equal(string.Empty, state.AccumulatedMarkdown);
    }

    [Fact]
    public void Reset_SetsStatusToIdle()
    {
        var state = new ReviewSessionState { Status = ReviewRunStatus.Error };

        state.Reset();

        Assert.Equal(ReviewRunStatus.Idle, state.Status);
    }

    [Fact]
    public void Reset_ClearsErrorMessage()
    {
        var state = new ReviewSessionState
        {
            Status       = ReviewRunStatus.Error,
            ErrorMessage = "Something went wrong",
        };

        state.Reset();

        Assert.Null(state.ErrorMessage);
    }

    [Fact]
    public void Reset_ResetsChunkCounters()
    {
        var state = new ReviewSessionState
        {
            CurrentChunk = 3,
            TotalChunks  = 5,
        };

        state.Reset();

        Assert.Equal(0, state.CurrentChunk);
        Assert.Equal(1, state.TotalChunks);
    }

    [Fact]
    public void Reset_PreservesOptions()
    {
        var opts = new ReviewOptions
        {
            Model                = "claude-opus-4-6",
            MinimumSeverity      = FindingSeverity.Critical,
            IncludeSuggestedFixes = false,
        };
        var state = new ReviewSessionState
        {
            Options             = opts,
            AccumulatedMarkdown = "old content",
            Status              = ReviewRunStatus.Complete,
        };

        state.Reset();

        Assert.Same(opts,                state.Options);
        Assert.Equal("claude-opus-4-6",  state.Options.Model);
        Assert.Equal(FindingSeverity.Critical, state.Options.MinimumSeverity);
        Assert.False(state.Options.IncludeSuggestedFixes);
    }

    [Fact]
    public void HasContent_IsFalseAfterReset()
    {
        var state = new ReviewSessionState
        {
            AccumulatedMarkdown = "Some content",
        };

        state.Reset();

        Assert.False(state.HasContent);
    }

    // ── ReviewChunk.Usage factory ─────────────────────────────────────────────

    [Fact]
    public void Usage_HasIsUsageTrue()
    {
        var chunk = ReviewChunk.Usage(1_000, 500);

        Assert.True(chunk.IsUsage);
    }

    [Fact]
    public void Usage_HasIsProgressFalse()
    {
        var chunk = ReviewChunk.Usage(1_000, 500);

        Assert.False(chunk.IsProgress);
    }

    [Fact]
    public void Usage_TextIsEmpty()
    {
        var chunk = ReviewChunk.Usage(1_000, 500);

        Assert.Equal(string.Empty, chunk.Text);
    }

    [Fact]
    public void Usage_SetsInputTokens()
    {
        var chunk = ReviewChunk.Usage(12_345, 0);

        Assert.Equal(12_345, chunk.InputTokens);
    }

    [Fact]
    public void Usage_SetsOutputTokens()
    {
        var chunk = ReviewChunk.Usage(0, 6_789);

        Assert.Equal(6_789, chunk.OutputTokens);
    }

    [Theory]
    [InlineData(0,      0)]
    [InlineData(1_000,  500)]
    [InlineData(50_000, 12_000)]
    public void Usage_RoundTrips_TokenCounts(long input, long output)
    {
        var chunk = ReviewChunk.Usage(input, output);

        Assert.Equal(input,  chunk.InputTokens);
        Assert.Equal(output, chunk.OutputTokens);
    }

    [Fact]
    public void Usage_ChunksWithSameValues_AreEqual()
    {
        var a = ReviewChunk.Usage(1_000, 500);
        var b = ReviewChunk.Usage(1_000, 500);

        Assert.Equal(a, b);
    }

    [Fact]
    public void TextChunk_IsUsageIsFalse()
    {
        var chunk = new ReviewChunk("some text");

        Assert.False(chunk.IsUsage);
    }

    [Fact]
    public void TextChunk_TokenCountsDefaultToZero()
    {
        var chunk = new ReviewChunk("some text");

        Assert.Equal(0, chunk.InputTokens);
        Assert.Equal(0, chunk.OutputTokens);
    }

    // ── ReviewSessionState: token counters ────────────────────────────────────

    [Fact]
    public void NewState_HasZeroTokenCounts()
    {
        var state = new ReviewSessionState();

        Assert.Equal(0, state.TotalInputTokens);
        Assert.Equal(0, state.TotalOutputTokens);
    }

    [Fact]
    public void Reset_ClearsTokenCounts()
    {
        var state = new ReviewSessionState
        {
            TotalInputTokens  = 10_000,
            TotalOutputTokens = 3_000,
        };

        state.Reset();

        Assert.Equal(0, state.TotalInputTokens);
        Assert.Equal(0, state.TotalOutputTokens);
    }
}
