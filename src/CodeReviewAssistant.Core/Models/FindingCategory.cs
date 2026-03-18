namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// Categories of code review findings used to filter and group results.
/// </summary>
public enum FindingCategory
{
    Security        = 0,
    Performance     = 1,
    Correctness     = 2,
    Maintainability = 3,
    TestCoverage    = 4,
    Style           = 5,
}
