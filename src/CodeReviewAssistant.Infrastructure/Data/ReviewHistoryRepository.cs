using CodeReviewAssistant.Core.Interfaces;
using CodeReviewAssistant.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeReviewAssistant.Infrastructure.Data;

/// <summary>
/// EF Core implementation of <see cref="IReviewHistoryRepository"/> backed by SQLite.
/// </summary>
public sealed class ReviewHistoryRepository(ReviewDbContext db) : IReviewHistoryRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(ReviewRecord record, CancellationToken ct = default)
    {
        db.Reviews.Add(record);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ReviewRecord>> GetRecentAsync(
        int count = 20, CancellationToken ct = default)
        => await db.Reviews
                   .AsNoTracking()
                   .OrderByDescending(r => r.CreatedAt)
                   .Take(count)
                   .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ReviewRecord>> GetByRepositoryAsync(
        string owner, string name, CancellationToken ct = default)
        => await db.Reviews
                   .AsNoTracking()
                   .Where(r => r.RepositoryOwner == owner && r.RepositoryName == name)
                   .OrderByDescending(r => r.CreatedAt)
                   .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<ReviewRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Reviews
                   .AsNoTracking()
                   .FirstOrDefaultAsync(r => r.Id == id, ct);

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var rows = await db.Reviews
                           .Where(r => r.Id == id)
                           .ExecuteDeleteAsync(ct);

        // No-op if the record didn't exist.
        _ = rows;
    }

    /// <inheritdoc/>
    public async Task<long> GetTodaysTotalTokensAsync(CancellationToken ct = default)
    {
        var startOfDayUtc = DateTime.UtcNow.Date;
        return await db.Reviews
                       .Where(r => r.CreatedAt >= startOfDayUtc)
                       .SumAsync(r => (long)r.TokensUsed, ct);
    }
}
