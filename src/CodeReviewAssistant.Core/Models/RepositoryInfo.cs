namespace CodeReviewAssistant.Core.Models;

public class RepositoryInfo
{
    public string Owner { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string DefaultBranch { get; init; } = string.Empty;
    public bool IsPrivate { get; init; }
    public int StargazersCount { get; init; }
    public int ForksCount { get; init; }
}
