using System.Net;
using CodeReviewAssistant.Core.Exceptions;
using CodeReviewAssistant.Infrastructure.GitHub;
using Moq;
using Octokit;

namespace CodeReviewAssistant.Tests.GitHub;

public class GitHubServiceTests
{
    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static Mock<IGitHubClient> CreateClientMock() => new(MockBehavior.Strict);

    /// <summary>
    /// Creates a <see cref="RateLimitExceededException"/> using a mocked IResponse with
    /// a properly-populated <see cref="ApiInfo"/>. Octokit 14 reads ApiInfo.RateLimit
    /// inside the constructor, so a null ApiInfo causes a NullReferenceException.
    /// </summary>
    private static RateLimitExceededException CreateRateLimitException(int minutesUntilReset = 30)
    {
        var epochReset = DateTimeOffset.UtcNow.AddMinutes(minutesUntilReset).ToUnixTimeSeconds();
        var rateLimit  = new RateLimit(5000, 0, epochReset);
        var apiInfo    = new ApiInfo(
            new Dictionary<string, Uri>(),
            new List<string>(),
            new List<string>(),
            etag: "test-etag",
            rateLimit,
            serverTimeDifference: TimeSpan.Zero);

        var responseMock = new Mock<IResponse>();
        responseMock.SetupGet(r => r.ApiInfo).Returns(apiInfo);
        responseMock.SetupGet(r => r.StatusCode).Returns(HttpStatusCode.Forbidden);
        responseMock.SetupGet(r => r.Body).Returns("{}");
        responseMock.SetupGet(r => r.ContentType).Returns("application/json");
        // Headers is IReadOnlyDictionary; pass an empty one to satisfy any fallback reads
        responseMock.SetupGet(r => r.Headers)
                    .Returns(new Dictionary<string, string>());

        return new RateLimitExceededException(responseMock.Object);
    }

    // ---------------------------------------------------------------
    // GetPullRequestAsync — rate limiting
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetPullRequestAsync_WhenRateLimited_ThrowsGitHubRateLimitException()
    {
        var rateLimitEx = CreateRateLimitException(minutesUntilReset: 30);

        var prsMock = new Mock<IPullRequestsClient>(MockBehavior.Strict);
        prsMock.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
               .ThrowsAsync(rateLimitEx);

        var clientMock = CreateClientMock();
        clientMock.SetupGet(c => c.PullRequest).Returns(prsMock.Object);

        var sut = new GitHubService(clientMock.Object);

        await Assert.ThrowsAsync<GitHubRateLimitException>(
            () => sut.GetPullRequestAsync("octocat", "Hello-World", 42));
    }

    [Fact]
    public async Task GetPullRequestAsync_WhenRateLimited_ExceptionCarriesResetTime()
    {
        var rateLimitEx = CreateRateLimitException(minutesUntilReset: 45);

        var prsMock = new Mock<IPullRequestsClient>(MockBehavior.Strict);
        prsMock.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
               .ThrowsAsync(rateLimitEx);

        var clientMock = CreateClientMock();
        clientMock.SetupGet(c => c.PullRequest).Returns(prsMock.Object);

        var sut = new GitHubService(clientMock.Object);
        var ex  = await Assert.ThrowsAsync<GitHubRateLimitException>(
            () => sut.GetPullRequestAsync("octocat", "Hello-World", 42));

        Assert.NotNull(ex.ResetAt);
        Assert.True(ex.ResetAt!.Value > DateTimeOffset.UtcNow);
    }

    // ---------------------------------------------------------------
    // GetPullRequestFilesAsync — mapping + rate limiting
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetPullRequestFilesAsync_ReturnsMappedFiles()
    {
        // PullRequestFile(sha, fileName, status, additions, deletions, changes,
        //                 blobUrl, rawUrl, contentsUrl, patch, previousFileName)
        var octoFiles = new List<PullRequestFile>
        {
            new("sha1", "src/Foo.cs", "modified", 3, 1, 4,
                "blob1", "raw1", "contents1", "@@ -1 +1 @@\n-old\n+new", ""),
            new("sha2", "README.md",  "added",    5, 0, 5,
                "blob2", "raw2", "contents2", "@@ -0,0 +1,5 @@",        "")
        };

        var prsMock = new Mock<IPullRequestsClient>(MockBehavior.Strict);
        prsMock.Setup(p => p.Files("octocat", "Hello-World", 42))
               .ReturnsAsync(octoFiles);

        var clientMock = CreateClientMock();
        clientMock.SetupGet(c => c.PullRequest).Returns(prsMock.Object);

        var sut    = new GitHubService(clientMock.Object);
        var result = await sut.GetPullRequestFilesAsync("octocat", "Hello-World", 42);

        Assert.Equal(2,            result.Count);
        Assert.Equal("src/Foo.cs", result[0].Filename);
        Assert.Equal("modified",   result[0].Status);
        Assert.Equal(3,            result[0].Additions);
        Assert.Equal(1,            result[0].Deletions);
        Assert.Equal("README.md",  result[1].Filename);
        Assert.Equal("added",      result[1].Status);
        Assert.Equal(5,            result[1].Additions);
    }

    [Fact]
    public async Task GetPullRequestFilesAsync_WhenRateLimited_ThrowsGitHubRateLimitException()
    {
        var prsMock = new Mock<IPullRequestsClient>(MockBehavior.Strict);
        prsMock.Setup(p => p.Files(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
               .ThrowsAsync(CreateRateLimitException());

        var clientMock = CreateClientMock();
        clientMock.SetupGet(c => c.PullRequest).Returns(prsMock.Object);

        var sut = new GitHubService(clientMock.Object);

        await Assert.ThrowsAsync<GitHubRateLimitException>(
            () => sut.GetPullRequestFilesAsync("octocat", "Hello-World", 42));
    }

    // ---------------------------------------------------------------
    // GetFileDiffAsync — rate limiting
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetFileDiffAsync_WhenRateLimited_ThrowsGitHubRateLimitException()
    {
        var prsMock = new Mock<IPullRequestsClient>(MockBehavior.Strict);
        prsMock.Setup(p => p.Files(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
               .ThrowsAsync(CreateRateLimitException());

        var clientMock = CreateClientMock();
        clientMock.SetupGet(c => c.PullRequest).Returns(prsMock.Object);

        var sut = new GitHubService(clientMock.Object);

        await Assert.ThrowsAsync<GitHubRateLimitException>(
            () => sut.GetFileDiffAsync("octocat", "Hello-World", 42));
    }

    // ---------------------------------------------------------------
    // GetRepositoryInfoAsync — rate limiting
    // ---------------------------------------------------------------

    [Fact]
    public async Task GetRepositoryInfoAsync_WhenRateLimited_ThrowsGitHubRateLimitException()
    {
        var repoClientMock = new Mock<IRepositoriesClient>(MockBehavior.Strict);
        repoClientMock.Setup(r => r.Get(It.IsAny<string>(), It.IsAny<string>()))
                      .ThrowsAsync(CreateRateLimitException());

        var clientMock = CreateClientMock();
        clientMock.SetupGet(c => c.Repository).Returns(repoClientMock.Object);

        var sut = new GitHubService(clientMock.Object);

        await Assert.ThrowsAsync<GitHubRateLimitException>(
            () => sut.GetRepositoryInfoAsync("octocat", "Hello-World"));
    }
}
