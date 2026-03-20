using CodeReviewAssistant.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeReviewAssistant.Infrastructure.Data;

/// <summary>
/// EF Core <see cref="DbContext"/> for the review history SQLite database.
/// </summary>
public class ReviewDbContext(DbContextOptions<ReviewDbContext> options) : DbContext(options)
{
    /// <summary>All persisted review records.</summary>
    public DbSet<ReviewRecord> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<ReviewRecord>(e =>
        {
            e.HasKey(r => r.Id);

            // SQLite has no native TimeSpan column type — store as ticks (long).
            e.Property(r => r.ReviewDuration)
             .HasConversion(
                 ts    => ts.Ticks,
                 ticks => TimeSpan.FromTicks(ticks));

            // Store status as a string so the SQLite file is human-readable.
            e.Property(r => r.Status)
             .HasConversion<string>()
             .HasMaxLength(20);

            e.Property(r => r.RepositoryOwner)
             .IsRequired()
             .HasMaxLength(200);

            e.Property(r => r.RepositoryName)
             .IsRequired()
             .HasMaxLength(200);

            e.Property(r => r.FindingsJson)
             .IsRequired()
             .HasDefaultValue("[]");

            // Composite index for repository-scoped queries.
            e.HasIndex(r => new { r.RepositoryOwner, r.RepositoryName })
             .HasDatabaseName("IX_Reviews_Repository");

            // Index for time-ordered queries.
            e.HasIndex(r => r.CreatedAt)
             .HasDatabaseName("IX_Reviews_CreatedAt");
        });
    }
}
