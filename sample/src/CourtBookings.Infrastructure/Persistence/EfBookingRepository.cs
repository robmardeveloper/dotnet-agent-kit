using CourtBookings.Application.Bookings;
using CourtBookings.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace CourtBookings.Infrastructure.Persistence;

internal sealed class EfBookingRepository(CourtBookingsDbContext dbContext) : IBookingRepository
{
    public Task<Booking?> GetByIdAsync(int bookingId, CancellationToken cancellationToken) =>
        dbContext.Bookings.SingleOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

    public async Task<IReadOnlyList<TimeSlot>> ListConfirmedSlotsEndingAfterAsync(
        int courtId, DateTimeOffset instant, CancellationToken cancellationToken) =>
        await dbContext.Bookings
            .Where(b => b.CourtId == courtId && b.Status == BookingStatus.Confirmed && b.Slot.End > instant)
            .Select(b => b.Slot)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // The booking was loaded through this same DbContext, which tracks its changes.
    public Task UpdateAsync(Booking booking, CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
