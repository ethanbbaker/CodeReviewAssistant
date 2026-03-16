using CodeReviewAssistant.Core.Models;

namespace CodeReviewAssistant.Core.Interfaces;

public interface IGitHubService
{
    Task<PullRequestInfo> GetPullRequestAsync(string owner, string repo, int prNumber);
    Task<IReadOnlyList<PullRequestFile>> GetPullRequestFilesAsync(string owner, string repo, int prNumber);
    Task<string> GetFileDiffAsync(string owner, string repo, int prNumber);
    Task<RepositoryInfo> GetRepositoryInfoAsync(string owner, string repo);
}
