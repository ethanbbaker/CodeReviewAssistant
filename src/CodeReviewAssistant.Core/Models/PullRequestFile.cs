namespace CodeReviewAssistant.Core.Models;

public class PullRequestFile
{
    public string Filename { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Additions { get; init; }
    public int Deletions { get; init; }
    public int Changes { get; init; }
    public string? Patch { get; init; }
    public string? PreviousFilename { get; init; }
}
