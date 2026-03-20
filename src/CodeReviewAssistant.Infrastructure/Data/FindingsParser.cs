using System.Text.RegularExpressions;
using CodeReviewAssistant.Core.Models;

namespace CodeReviewAssistant.Infrastructure.Data;

/// <summary>
/// Extracts structured <see cref="FindingRecord"/> objects from a Claude review response.
/// </summary>
/// <remarks>
/// The review prompt instructs Claude to format each finding as:
/// <code>
///   **[SEVERITY]** `path/to/file.ext` — concise description
/// </code>
/// Both the em-dash (—) and a plain hyphen (-) are accepted as the separator.
/// </remarks>
internal static partial class FindingsParser
{
    // Matches: **[SEVERITY]** `path/to/file` — description
    // Groups: severity, file, desc
    [GeneratedRegex(
        @"\*\*\[(?<severity>[A-Z]+)\]\*\*\s+`(?<file>[^`]+)`\s+[—\-]\s+(?<desc>.+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex FindingPattern();

    /// <summary>
    /// Parses <paramref name="markdown"/> and returns one <see cref="FindingRecord"/>
    /// per matched finding line.  Returns an empty list when the input is empty or
    /// contains no recognisable findings.
    /// </summary>
    internal static List<FindingRecord> Parse(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return [];

        var results = new List<FindingRecord>();

        foreach (Match m in FindingPattern().Matches(markdown))
        {
            results.Add(new FindingRecord(
                Severity:    m.Groups["severity"].Value,
                FilePath:    m.Groups["file"].Value,
                Description: m.Groups["desc"].Value.Trim()));
        }

        return results;
    }
}
