namespace CodeReviewAssistant.Core.Models;

/// <summary>
/// Lifecycle state of a streaming AI code review.
/// </summary>
public enum ReviewRunStatus
{
    /// <summary>No review has been started yet.</summary>
    Idle,

    /// <summary>A review is actively streaming from the API.</summary>
    Running,

    /// <summary>The review completed successfully.</summary>
    Complete,

    /// <summary>The user cancelled the review mid-stream.</summary>
    Cancelled,

    /// <summary>The review failed with an error.</summary>
    Error,
}
