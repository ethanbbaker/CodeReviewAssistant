using CodeReviewAssistant.Core.Exceptions;
using CodeReviewAssistant.Core.Interfaces;
using CodeReviewAssistant.Core.Models;
using CodeReviewAssistant.Core.Parsing;
using Octokit;
using PullRequestFile = CodeReviewAssistant.Core.Models.PullRequestFile;
using RepositoryInfo  = CodeReviewAssistant.Core.Models.RepositoryInfo;

namespace CodeReviewAssistant.Infrastructure.GitHub;

public class GitHubService : IGitHubService
{
    private readonly IGitHubClient _client;

    public GitHubService(IGitHubClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<PullRequestInfo> GetPullRequestAsync(string owner, string repo, int prNumber)
    {
        try
        {
            var pr = await _client.PullRequest.Get(owner, repo, prNumber);

            return new PullRequestInfo
            {
                Number       = pr.Number,
                Title        = pr.Title,
                Body         = pr.Body ?? string.Empty,
                State        = pr.State.StringValue,
                Author       = pr.User?.Login ?? string.Empty,
                CreatedAt    = pr.CreatedAt,
                MergedAt     = pr.MergedAt,
                BaseRef      = pr.Base?.Ref ?? string.Empty,
                HeadRef      = pr.Head?.Ref ?? string.Empty,
                Additions    = pr.Additions,
                Deletions    = pr.Deletions,
                ChangedFiles = pr.ChangedFiles,
                IsMerged     = pr.Merged
            };
        }
        catch (RateLimitExceededException ex)
        {
            throw new GitHubRateLimitException(ex.Reset);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PullRequestFile>> GetPullRequestFilesAsync(
        string owner, string repo, int prNumber)
    {
        try
        {
            var files = await _client.PullRequest.Files(owner, repo, prNumber);

            return files.Select(f => new PullRequestFile
            {
                Filename         = f.FileName,
                Status           = f.Status,
                Additions        = f.Additions,
                Deletions        = f.Deletions,
                Changes          = f.Changes,
                Patch            = f.Patch,
                PreviousFilename = f.PreviousFileName
            }).ToList();
        }
        catch (RateLimitExceededException ex)
        {
            throw new GitHubRateLimitException(ex.Reset);
        }
    }

    /// <inheritdoc />
    public async Task<string> GetFileDiffAsync(string owner, string repo, int prNumber)
    {
        try
        {
            // Retrieve all file patches and concatenate them into a unified diff string.
            var files = await _client.PullRequest.Files(owner, repo, prNumber);

            return string.Join(
                Environment.NewLine + Environment.NewLine,
                files
                    .Where(f => f.Patch is not null)
                    .Select(f => $"--- {f.PreviousFileName ?? f.FileName}\n+++ {f.FileName}\n{f.Patch}"));
        }
        catch (RateLimitExceededException ex)
        {
            throw new GitHubRateLimitException(ex.Reset);
        }
    }

    /// <inheritdoc />
    public async Task<RepositoryInfo> GetRepositoryInfoAsync(string owner, string repo)
    {
        try
        {
            var repository = await _client.Repository.Get(owner, repo);

            return new RepositoryInfo
            {
                Owner          = repository.Owner?.Login ?? owner,
                Name           = repository.Name,
                FullName       = repository.FullName,
                Description    = repository.Description,
                DefaultBranch  = repository.DefaultBranch,
                IsPrivate      = repository.Private,
                StargazersCount = repository.StargazersCount,
                ForksCount     = repository.ForksCount
            };
        }
        catch (RateLimitExceededException ex)
        {
            throw new GitHubRateLimitException(ex.Reset);
        }
    }

    /// <inheritdoc />
    public async Task<PullRequestContext> GetPullRequestContextAsync(
        string owner, string repo, int prNumber)
    {
        try
        {
            // Fetch PR metadata and file list concurrently to minimise latency.
            var prTask    = _client.PullRequest.Get(owner, repo, prNumber);
            var filesTask = _client.PullRequest.Files(owner, repo, prNumber);

            await Task.WhenAll(prTask, filesTask);

            var pr    = await prTask;
            var files = await filesTask;

            var prInfo = new PullRequestInfo
            {
                Number       = pr.Number,
                Title        = pr.Title,
                Body         = pr.Body ?? string.Empty,
                State        = pr.State.StringValue,
                Author       = pr.User?.Login ?? string.Empty,
                CreatedAt    = pr.CreatedAt,
                MergedAt     = pr.MergedAt,
                BaseRef      = pr.Base?.Ref ?? string.Empty,
                HeadRef      = pr.Head?.Ref ?? string.Empty,
                Additions    = pr.Additions,
                Deletions    = pr.Deletions,
                ChangedFiles = pr.ChangedFiles,
                IsMerged     = pr.Merged
            };

            var coreFiles = files.Select(f => new PullRequestFile
            {
                Filename         = f.FileName,
                Status           = f.Status,
                Additions        = f.Additions,
                Deletions        = f.Deletions,
                Changes          = f.Changes,
                Patch            = f.Patch,
                PreviousFilename = f.PreviousFileName
            }).ToList();

            return DiffParser.Parse(prInfo, coreFiles);
        }
        catch (RateLimitExceededException ex)
        {
            throw new GitHubRateLimitException(ex.Reset);
        }
    }
}
