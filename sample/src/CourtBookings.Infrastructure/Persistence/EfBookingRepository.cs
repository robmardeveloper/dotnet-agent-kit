using CourtBookings.Application.Bookings;
using CourtBookings.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace CourtBookings.Infrastructure.Persistence;

internal sealed class EfBookingRepository(CourtBookingsDbContext dbContext) : IBookingRepository
{
    public async Task<IReadOnlyList<TimeSlot>> ListSlotsEndingAfterAsync(
        int courtId, DateTimeOffset instant, CancellationToken cancellationToken) =>
        await dbContext.Bookings
            .Where(b => b.CourtId == courtId && b.Slot.End > instant)
            .Select(b => b.Slot)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
