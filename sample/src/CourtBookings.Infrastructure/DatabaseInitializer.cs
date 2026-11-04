using CourtBookings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CourtBookings.Infrastructure;

public static class DatabaseInitializer
{
    /// <summary>
    /// Creates the database and its seed courts if they do not exist. The sample has no migrations
    /// yet; a real application would apply migrations here instead.
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CourtBookingsDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
