using CodeReviewAssistant.Core.Models;

namespace CodeReviewAssistant.Components.Services;

/// <summary>
/// Persists AI review state per PR across sidebar tab switches.
/// Keyed by owner/repo/PR-number so reviews survive navigation.
/// </summary>
public interface IReviewCacheService
{
    /// <summary>
    /// Returns the <see cref="ReviewSessionState"/> for the given PR reference,
    /// creating a fresh one if none exists yet.
    /// </summary>
    ReviewSessionState GetOrCreate(GitHubPrReference reference);

    /// <summary>Fires whenever any review state changes (used to trigger Blazor re-renders).</summary>
    event Action? OnChange;

    /// <summary>Raises <see cref="OnChange"/>; call this after mutating a session state.</summary>
    void NotifyChanged();
}

/// <inheritdoc cref="IReviewCacheService"/>
public sealed class ReviewCacheService : IReviewCacheService
{
    private readonly Dictionary<string, ReviewSessionState> _cache = new(StringComparer.OrdinalIgnoreCase);

    public event Action? OnChange;

    public ReviewSessionState GetOrCreate(GitHubPrReference reference)
    {
        var key = $"{reference.Owner}/{reference.Repo}/{reference.PullRequestNumber}";
        if (!_cache.TryGetValue(key, out var state))
        {
            state       = new ReviewSessionState();
            _cache[key] = state;
        }
        return state;
    }

    public void NotifyChanged() => OnChange?.Invoke();
}
