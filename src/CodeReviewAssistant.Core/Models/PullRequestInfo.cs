namespace CodeReviewAssistant.Core.Models;

public class PullRequestInfo
{
    public int Number { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? MergedAt { get; init; }
    public string BaseRef { get; init; } = string.Empty;
    public string HeadRef { get; init; } = string.Empty;
    public int Additions { get; init; }
    public int Deletions { get; init; }
    public int ChangedFiles { get; init; }
    public bool IsMerged { get; init; }
}
