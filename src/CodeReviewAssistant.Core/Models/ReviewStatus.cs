namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// Final outcome of a persisted review run.
/// </summary>
public enum ReviewStatus
{
    /// <summary>The review completed successfully and produced output.</summary>
    Completed,

    /// <summary>The review failed due to an API or processing error.</summary>
    Failed,

    /// <summary>The review was cancelled by the user before completion.</summary>
    Cancelled,
}
