namespace CodeReviewAssistant.Core.Exceptions;

/// <summary>
/// Thrown when the GitHub API rate limit has been exceeded.
/// </summary>
public class GitHubRateLimitException : Exception
{
    public DateTimeOffset? ResetAt { get; }

    public GitHubRateLimitException(DateTimeOffset? resetAt = null)
        : base(BuildMessage(resetAt))
    {
        ResetAt = resetAt;
    }

    public GitHubRateLimitException(string message, Exception innerException)
        : base(message, innerException) { }

    private static string BuildMessage(DateTimeOffset? resetAt) =>
        resetAt.HasValue
            ? $"GitHub API rate limit exceeded. Limit resets at {resetAt:HH:mm:ss zzz}."
            : "GitHub API rate limit exceeded. Please try again later.";
}
