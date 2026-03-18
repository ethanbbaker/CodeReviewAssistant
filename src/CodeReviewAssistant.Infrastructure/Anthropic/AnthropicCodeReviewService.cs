using System.Runtime.CompilerServices;
using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using CodeReviewAssistant.Core.Interfaces;
using CodeReviewAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace CodeReviewAssistant.Infrastructure.Anthropic;

/// <summary>
/// <see cref="ICodeReviewService"/> implementation backed by the Anthropic Messages API.
/// </summary>
/// <remarks>
/// The Anthropic SDK already retries 408 / 409 / 429 / 5xx responses automatically
/// (configurable via <see cref="AnthropicClient.MaxRetries"/>; default = 2).
/// On top of that, <see cref="ReviewAsync"/> wraps the full aggregation call in a
/// Polly resilience pipeline for an additional retry layer on transient network
/// failures, with exponential back-off and jitter.
/// </remarks>
public sealed class AnthropicCodeReviewService : ICodeReviewService
{
    private readonly AnthropicClient                        _client;
    private readonly ResiliencePipeline                     _pipeline;
    private readonly ILogger<AnthropicCodeReviewService>    _logger;

    public AnthropicCodeReviewService(
        AnthropicClient                     client,
        ILogger<AnthropicCodeReviewService> logger)
    {
        _client   = client;
        _logger   = logger;
        _pipeline = BuildResiliencePipeline();
    }

    // ── ICodeReviewService ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async IAsyncEnumerable<ReviewChunk> StreamReviewAsync(
        PullRequestContext context,
        ReviewOptions      options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var chunks      = FileChunker.Chunk(context);
        int totalChunks = chunks.Count;

        _logger.LogInformation(
            "Streaming review for PR '{Title}' ({FileCount} files, {ChunkCount} chunk(s))",
            context.Title, context.Files.Count, totalChunks);

        for (int i = 0; i < totalChunks; i++)
        {
            // Emit a progress notification before the first token arrives.
            yield return ReviewChunk.Progress(i, totalChunks);

            // Inter-chunk visual separator in the accumulated text.
            if (i > 0)
                yield return new ReviewChunk("\n\n---\n\n") { ChunkIndex = i, TotalChunks = totalChunks };

            var userMessage = PromptBuilder.BuildUserMessage(
                context, chunks[i], i, totalChunks, options);

            var parameters = BuildParams(options, userMessage);

            await foreach (var ev in _client.Messages.CreateStreaming(parameters, ct))
            {
                if (ev.TryPickContentBlockDelta(out var cbDelta) &&
                    cbDelta.Delta.TryPickText(out var textDelta))
                {
                    yield return new ReviewChunk(textDelta.Text) { ChunkIndex = i, TotalChunks = totalChunks };
                }
            }
        }
    }

    /// <inheritdoc/>
    public async Task<CodeReview> ReviewAsync(
        PullRequestContext context,
        ReviewOptions      options,
        CancellationToken  ct = default)
    {
        var chunks      = FileChunker.Chunk(context);
        int totalChunks = chunks.Count;

        _logger.LogInformation(
            "Reviewing PR '{Title}' ({FileCount} files, {ChunkCount} chunk(s))",
            context.Title, context.Files.Count, totalChunks);

        var reviewText   = new StringBuilder();
        long inputTokens  = 0;
        long outputTokens = 0;
        string lastModel  = options.Model;

        for (int i = 0; i < totalChunks; i++)
        {
            if (i > 0)
                reviewText.AppendLine().AppendLine("---").AppendLine();

            var userMessage = PromptBuilder.BuildUserMessage(
                context, chunks[i], i, totalChunks, options);

            var parameters = BuildParams(options, userMessage);

            var message = await _pipeline.ExecuteAsync(
                async innerCt => await _client.Messages
                    .CreateStreaming(parameters)
                    .Aggregate()
                    .WaitAsync(innerCt),
                ct);

            // Extract text from the response content
            foreach (var block in message.Content)
            {
                if (block.TryPickText(out var tb))
                    reviewText.Append(tb.Text);
            }

            inputTokens  += message.Usage.InputTokens;
            outputTokens += message.Usage.OutputTokens;
            lastModel     = message.Model;

            _logger.LogDebug(
                "Chunk {Chunk}/{Total}: {InputTokens} in / {OutputTokens} out tokens",
                i + 1, totalChunks,
                message.Usage.InputTokens, message.Usage.OutputTokens);
        }

        return new CodeReview
        {
            Text         = reviewText.ToString().Trim(),
            Model        = lastModel,
            InputTokens  = inputTokens,
            OutputTokens = outputTokens,
            WasChunked   = totalChunks > 1,
            ChunkCount   = totalChunks,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static MessageCreateParams BuildParams(ReviewOptions options, string userMessage) =>
        new()
        {
            Model     = options.Model,
            MaxTokens = options.MaxOutputTokens,
            System    = PromptBuilder.SystemPrompt,
            Messages  =
            [
                new MessageParam { Role = Role.User, Content = userMessage },
            ],
        };

    private static ResiliencePipeline BuildResiliencePipeline() =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType      = DelayBackoffType.Exponential,
                UseJitter        = true,
                Delay            = TimeSpan.FromSeconds(2),
                // Retry on network / timeout failures; the SDK already handles
                // 429 and 5xx via its built-in retry, but this catches anything
                // that slips through (e.g. a mid-stream TCP reset).
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(e => e.InnerException is TimeoutException),
            })
            .Build();
}
