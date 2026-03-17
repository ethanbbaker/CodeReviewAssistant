using CodeReviewAssistant.Core.Models;

namespace CodeReviewAssistant.Components.Services;

/// <summary>
/// Shared state for the active pull-request review session.
/// Scoped per Blazor circuit so the layout and page components stay in sync.
/// </summary>
public interface IReviewStateService
{
    PullRequestContext? Context   { get; }
    GitHubPrReference? Reference  { get; }
    event Action?      OnChange;

    void SetReview(PullRequestContext context, GitHubPrReference reference);
    void ClearReview();
}

public sealed class ReviewStateService : IReviewStateService
{
    public PullRequestContext? Context   { get; private set; }
    public GitHubPrReference? Reference  { get; private set; }
    public event Action?      OnChange;

    public void SetReview(PullRequestContext context, GitHubPrReference reference)
    {
        Context   = context;
        Reference = reference;
        OnChange?.Invoke();
    }

    public void ClearReview()
    {
        Context   = null;
        Reference = null;
        OnChange?.Invoke();
    }
}
