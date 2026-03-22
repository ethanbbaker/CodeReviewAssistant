using CodeReviewAssistant.Core.Models;

namespace CodeReviewAssistant.Core.Interfaces;

/// <summary>
/// Persistence contract for <see cref="ReviewRecord"/> objects.
/// Implementations are backed by a SQLite database via EF Core.
/// </summary>
public interface IReviewHistoryRepository
{
    /// <summary>Persists a new review record.</summary>
    Task AddAsync(ReviewRecord record, CancellationToken ct = default);

    /// <summary>
    /// Returns the most recent <paramref name="count"/> review records,
    /// ordered newest-first.
    /// </summary>
    Task<IReadOnlyList<ReviewRecord>> GetRecentAsync(int count = 20, CancellationToken ct = default);

    /// <summary>
    /// Returns all review records for a specific repository,
    /// ordered newest-first.
    /// </summary>
    Task<IReadOnlyList<ReviewRecord>> GetByRepositoryAsync(
        string owner, string name, CancellationToken ct = default);

    /// <summary>Returns a single record by its primary key, or <see langword="null"/> if not found.</summary>
    Task<ReviewRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Permanently removes the record with the given <paramref name="id"/>. No-ops if not found.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns the total tokens used (input + output combined) across all reviews
    /// whose <see cref="ReviewRecord.CreatedAt"/> falls on today's UTC date.
    /// Used to enforce the daily spend limit before starting a new review.
    /// </summary>
    Task<long> GetTodaysTotalTokensAsync(CancellationToken ct = default);
}
