using CodeReviewAssistant.Core.Parsing;

namespace CodeReviewAssistant.Tests.Parsing;

public class GitHubUrlParserTests
{
    // ---------------------------------------------------------------
    // Pull-request URLs
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("https://github.com/octocat/Hello-World/pull/42")]
    [InlineData("https://github.com/octocat/Hello-World/pull/42/files")]
    [InlineData("https://github.com/octocat/Hello-World/pull/42#issuecomment-1")]
    [InlineData("HTTP://GITHUB.COM/octocat/Hello-World/pull/42")]   // case-insensitive scheme
    public void TryParse_ValidPrUrl_ReturnsPrReference(string url)
    {
        var result = GitHubUrlParser.TryParse(url);

        Assert.NotNull(result);
        Assert.True(result.IsPullRequest);
        Assert.Equal("octocat",     result.Owner);
        Assert.Equal("Hello-World", result.Repo);
        Assert.Equal(42,            result.PullRequestNumber);
    }

    [Fact]
    public void TryParse_PrUrl_ExtractsCorrectPrNumber()
    {
        var result = GitHubUrlParser.TryParse("https://github.com/microsoft/vscode/pull/99999");

        Assert.NotNull(result);
        Assert.Equal("microsoft", result.Owner);
        Assert.Equal("vscode",    result.Repo);
        Assert.Equal(99999,       result.PullRequestNumber);
    }

    // ---------------------------------------------------------------
    // Repository-only URLs
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("https://github.com/octocat/Hello-World")]
    [InlineData("https://github.com/octocat/Hello-World/")]
    [InlineData("https://github.com/octocat/Hello-World?tab=readme")]
    public void TryParse_RepoUrl_ReturnsRepoReferenceWithNoPrNumber(string url)
    {
        var result = GitHubUrlParser.TryParse(url);

        Assert.NotNull(result);
        Assert.False(result.IsPullRequest);
        Assert.Null(result.PullRequestNumber);
        Assert.Equal("octocat",     result.Owner);
        Assert.Equal("Hello-World", result.Repo);
    }

    // ---------------------------------------------------------------
    // Invalid / unsupported URLs
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("https://gitlab.com/owner/repo/merge_requests/1")]
    [InlineData("https://github.com/octocat")]          // only owner — no repo
    [InlineData("https://github.com/")]
    public void TryParse_InvalidUrl_ReturnsNull(string? url)
    {
        var result = GitHubUrlParser.TryParse(url);
        Assert.Null(result);
    }

    // ---------------------------------------------------------------
    // IsPullRequestUrl helper
    // ---------------------------------------------------------------

    [Fact]
    public void IsPullRequestUrl_WithPrUrl_ReturnsTrue()
    {
        Assert.True(GitHubUrlParser.IsPullRequestUrl(
            "https://github.com/dotnet/runtime/pull/12345"));
    }

    [Fact]
    public void IsPullRequestUrl_WithRepoUrl_ReturnsFalse()
    {
        Assert.False(GitHubUrlParser.IsPullRequestUrl(
            "https://github.com/dotnet/runtime"));
    }

    [Fact]
    public void IsPullRequestUrl_WithNull_ReturnsFalse()
    {
        Assert.False(GitHubUrlParser.IsPullRequestUrl(null));
    }

    // ---------------------------------------------------------------
    // ToString round-trip
    // ---------------------------------------------------------------

    [Fact]
    public void ToString_PrReference_ProducesCanonicalUrl()
    {
        var result = GitHubUrlParser.TryParse(
            "https://github.com/octocat/Hello-World/pull/42/files#diff-0");

        Assert.Equal("https://github.com/octocat/Hello-World/pull/42", result!.ToString());
    }
}
