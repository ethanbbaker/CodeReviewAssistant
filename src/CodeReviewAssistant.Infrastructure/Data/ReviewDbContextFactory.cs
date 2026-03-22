using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CodeReviewAssistant.Infrastructure.Data;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> to create a <see cref="ReviewDbContext"/>
/// when running migrations without needing the Web startup project.
/// </summary>
internal sealed class ReviewDbContextFactory : IDesignTimeDbContextFactory<ReviewDbContext>
{
    public ReviewDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReviewDbContext>()
            .UseSqlite("Data Source=design-time-placeholder.db")
            .Options;

        return new ReviewDbContext(options);
    }
}
