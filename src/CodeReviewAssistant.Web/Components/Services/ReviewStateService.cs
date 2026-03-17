using CodeReviewAssistant.Core.Models;

namespace CodeReviewAssistant.Components.Services;

/// <summary>
/// Shared state for pull-request review sessions.
/// Supports multiple open PR tabs plus a "Search" (home) state.
/// Scoped per Blazor circuit so layout and page components stay in sync.
/// </summary>
public interface IReviewStateService
{
    /// <summary>All open PR sessions, in the order they were added.</summary>
    IReadOnlyList<(PullRequestContext Context, GitHubPrReference Reference)> Sessions { get; }

    /// <summary>
    /// Index of the currently active session, or <c>null</c> when the Search tab is active.
    /// </summary>
    int? ActiveIndex { get; }

    /// <summary>Context for the active session, or <c>null</c> on the Search tab.</summary>
    PullRequestContext? Context  { get; }

    /// <summary>Reference for the active session, or <c>null</c> on the Search tab.</summary>
    GitHubPrReference? Reference { get; }

    event Action? OnChange;

    /// <summary>
    /// Opens a new PR tab (or activates it if already open) and makes it the active tab.
    /// </summary>
    void AddSession(PullRequestContext context, GitHubPrReference reference);

    /// <summary>Switches the active tab.  Pass <c>null</c> to show the Search tab.</summary>
    void SetActiveIndex(int? index);

    /// <summary>Closes the tab at <paramref name="index"/>, activating the Search tab if it was active.</summary>
    void RemoveSession(int index);

    /// <summary>Switches to the Search tab without removing any sessions.</summary>
    void ClearReview();

    /// <summary>Backwards-compatible alias for <see cref="AddSession"/>.</summary>
    void SetReview(PullRequestContext context, GitHubPrReference reference);
}

public sealed class ReviewStateService : IReviewStateService
{
    private readonly List<(PullRequestContext Context, GitHubPrReference Reference)> _sessions = new();

    public IReadOnlyList<(PullRequestContext Context, GitHubPrReference Reference)> Sessions => _sessions;

    public int?                ActiveIndex { get; private set; }
    public PullRequestContext? Context     => ActiveIndex.HasValue ? _sessions[ActiveIndex.Value].Context   : null;
    public GitHubPrReference?  Reference   => ActiveIndex.HasValue ? _sessions[ActiveIndex.Value].Reference : null;

    public event Action? OnChange;

    public void AddSession(PullRequestContext context, GitHubPrReference reference)
    {
        // If this PR is already open, just switch to it.
        for (int i = 0; i < _sessions.Count; i++)
        {
            var r = _sessions[i].Reference;
            if (r.Owner             == reference.Owner  &&
                r.Repo              == reference.Repo   &&
                r.PullRequestNumber == reference.PullRequestNumber)
            {
                ActiveIndex = i;
                OnChange?.Invoke();
                return;
            }
        }

        _sessions.Add((context, reference));
        ActiveIndex = _sessions.Count - 1;
        OnChange?.Invoke();
    }

    public void SetActiveIndex(int? index)
    {
        ActiveIndex = index;
        OnChange?.Invoke();
    }

    public void RemoveSession(int index)
    {
        if (index < 0 || index >= _sessions.Count) return;

        _sessions.RemoveAt(index);

        if (!ActiveIndex.HasValue)
        {
            // Already on Search tab — nothing to adjust.
        }
        else if (ActiveIndex.Value == index)
        {
            // Closed the active tab → fall back to Search.
            ActiveIndex = null;
        }
        else if (ActiveIndex.Value > index)
        {
            // Shift active index down to account for removed entry.
            ActiveIndex--;
        }

        OnChange?.Invoke();
    }

    public void ClearReview()
    {
        ActiveIndex = null;
        OnChange?.Invoke();
    }

    // Backwards-compatible shim.
    public void SetReview(PullRequestContext context, GitHubPrReference reference)
        => AddSession(context, reference);
}
