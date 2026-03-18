namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// Severity levels for code-review findings, ordered from most to least severe.
/// </summary>
public enum FindingSeverity
{
    Critical = 0,
    High     = 1,
    Medium   = 2,
    Low      = 3,
    Info     = 4,
}
