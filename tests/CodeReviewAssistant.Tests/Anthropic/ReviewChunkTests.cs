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
}
