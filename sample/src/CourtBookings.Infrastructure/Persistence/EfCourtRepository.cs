using CourtBookings.Application.Courts;
using Microsoft.EntityFrameworkCore;

namespace CourtBookings.Infrastructure.Persistence;

internal sealed class EfCourtRepository(CourtBookingsDbContext dbContext) : ICourtRepository
{
    public Task<bool> ExistsAsync(int courtId, CancellationToken cancellationToken) =>
        dbContext.Courts.AnyAsync(c => c.Id == courtId, cancellationToken);
}
