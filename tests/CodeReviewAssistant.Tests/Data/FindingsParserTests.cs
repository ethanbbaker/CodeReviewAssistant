using CodeReviewAssistant.Infrastructure.Data;

namespace CodeReviewAssistant.Tests.Data;

/// <summary>
/// Tests for <see cref="FindingsParser.Parse"/>.
/// </summary>
public class FindingsParserTests
{
    // ── Empty / null input ────────────────────────────────────────────────────

    [Fact]
    public void EmptyString_ReturnsEmptyList()
    {
        var result = FindingsParser.Parse(string.Empty);
        Assert.Empty(result);
    }

    [Fact]
    public void WhitespaceOnly_ReturnsEmptyList()
    {
        var result = FindingsParser.Parse("   \n  \t  ");
        Assert.Empty(result);
    }

    [Fact]
    public void NoFindings_ReturnsEmptyList()
    {
        const string markdown = """
            ## Summary
            This looks like a solid change with no major issues.
            """;

        var result = FindingsParser.Parse(markdown);
        Assert.Empty(result);
    }

    // ── Single finding ────────────────────────────────────────────────────────

    [Fact]
    public void SingleFinding_EmDash_ParsedCorrectly()
    {
        const string markdown = "1. **[HIGH]** `src/Auth.cs` — Missing null check on user input.";

        var result = FindingsParser.Parse(markdown);

        Assert.Single(result);
        Assert.Equal("HIGH",          result[0].Severity);
        Assert.Equal("src/Auth.cs",   result[0].FilePath);
        Assert.Equal("Missing null check on user input.", result[0].Description);
    }

    [Fact]
    public void SingleFinding_PlainHyphen_ParsedCorrectly()
    {
        const string markdown = "1. **[MEDIUM]** `src/Parser.cs` - Potential off-by-one error.";

        var result = FindingsParser.Parse(markdown);

        Assert.Single(result);
        Assert.Equal("MEDIUM",        result[0].Severity);
        Assert.Equal("src/Parser.cs", result[0].FilePath);
        Assert.Equal("Potential off-by-one error.", result[0].Description);
    }

    // ── All severity levels ───────────────────────────────────────────────────

    [Theory]
    [InlineData("CRITICAL")]
    [InlineData("HIGH")]
    [InlineData("MEDIUM")]
    [InlineData("LOW")]
    [InlineData("INFO")]
    public void AllSeverityLevels_ParsedCorrectly(string severity)
    {
        var markdown = $"1. **[{severity}]** `file.cs` — Some description.";

        var result = FindingsParser.Parse(markdown);

        Assert.Single(result);
        Assert.Equal(severity, result[0].Severity);
    }

    // ── Multiple findings ─────────────────────────────────────────────────────

    [Fact]
    public void MultipleFindings_AllParsed()
    {
        const string markdown = """
            ## Security
            1. **[CRITICAL]** `src/Sql.cs` — SQL injection vulnerability.
            2. **[HIGH]** `src/Auth.cs` — Weak password hashing algorithm.

            ## Performance
            3. **[MEDIUM]** `src/Cache.cs` — Cache never invalidated.
            """;

        var result = FindingsParser.Parse(markdown);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void MultipleFindings_PreserveOrder()
    {
        const string markdown = """
            1. **[CRITICAL]** `a.cs` — First.
            2. **[HIGH]** `b.cs` — Second.
            3. **[LOW]** `c.cs` — Third.
            """;

        var result = FindingsParser.Parse(markdown);

        Assert.Equal("a.cs", result[0].FilePath);
        Assert.Equal("b.cs", result[1].FilePath);
        Assert.Equal("c.cs", result[2].FilePath);
    }

    // ── CRITICAL counting ─────────────────────────────────────────────────────

    [Fact]
    public void CriticalCount_OnlyCountsCritical()
    {
        const string markdown = """
            1. **[CRITICAL]** `a.cs` — Critical issue.
            2. **[CRITICAL]** `b.cs` — Another critical issue.
            3. **[HIGH]** `c.cs` — High but not critical.
            4. **[INFO]** `d.cs` — Informational.
            """;

        var result         = FindingsParser.Parse(markdown);
        int criticalCount  = result.Count(f => f.Severity == "CRITICAL");

        Assert.Equal(4, result.Count);
        Assert.Equal(2, criticalCount);
    }

    [Fact]
    public void NoFindings_CriticalCountIsZero()
    {
        var result = FindingsParser.Parse("## Summary\nLooks good!");
        Assert.Equal(0, result.Count(f => f.Severity == "CRITICAL"));
    }

    // ── Mixed content (headers, prose, code blocks) ───────────────────────────

    [Fact]
    public void FindingsInFullReview_ExtractedFromAmidstProse()
    {
        const string markdown = """
            ## Security

            The PR modifies the authentication flow. Here are the issues found:

            1. **[HIGH]** `src/Services/AuthService.cs` — Password stored in plain text.
            2. **[LOW]** `src/Models/User.cs` — Unused import.

            ## Summary

            Overall the PR has a critical security concern that must be addressed before merging.
            """;

        var result = FindingsParser.Parse(markdown);

        Assert.Equal(2, result.Count);
        Assert.Equal("src/Services/AuthService.cs", result[0].FilePath);
        Assert.Equal("src/Models/User.cs",           result[1].FilePath);
    }

    [Fact]
    public void CodeBlock_ContentNotMistakenlyScrapped()
    {
        // Code blocks can contain patterns that look like findings — must not be matched.
        const string markdown = """
            ## Correctness / Bugs
            1. **[MEDIUM]** `src/Foo.cs` — Wrong return type.

            ```csharp
            // This is just code, not a finding
            var x = **[HIGH]** something;
            ```
            """;

        var result = FindingsParser.Parse(markdown);

        // The inline code in the ``` block does not match the full finding pattern,
        // so only the real finding should be returned.
        Assert.Single(result);
        Assert.Equal("src/Foo.cs", result[0].FilePath);
    }

    // ── File path variety ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("src/Foo.cs")]
    [InlineData("tests/Unit/BarTests.cs")]
    [InlineData("some-dir/sub/file.ts")]
    [InlineData("README.md")]
    public void VariousFilePaths_ParsedCorrectly(string filePath)
    {
        var markdown = $"1. **[LOW]** `{filePath}` — Some issue.";

        var result = FindingsParser.Parse(markdown);

        Assert.Single(result);
        Assert.Equal(filePath, result[0].FilePath);
    }

    // ── Description trimming ──────────────────────────────────────────────────

    [Fact]
    public void Description_IsTrimmed()
    {
        const string markdown = "1. **[INFO]** `file.cs` —   Has leading/trailing spaces.   ";

        var result = FindingsParser.Parse(markdown);

        Assert.Equal("Has leading/trailing spaces.", result[0].Description);
    }
}
