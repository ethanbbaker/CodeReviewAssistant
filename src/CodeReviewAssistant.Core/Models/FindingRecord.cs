namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// A single structured finding parsed from a Claude review response.
/// Used for serialisation into <see cref="ReviewRecord.FindingsJson"/> and
/// for counting findings by severity.
/// </summary>
/// <param name="Severity">Severity label exactly as produced by the model — CRITICAL, HIGH, MEDIUM, LOW, or INFO.</param>
/// <param name="FilePath">Repository-relative path of the file this finding applies to.</param>
/// <param name="Description">Concise description of the issue.</param>
public record FindingRecord(string Severity, string FilePath, string Description);
