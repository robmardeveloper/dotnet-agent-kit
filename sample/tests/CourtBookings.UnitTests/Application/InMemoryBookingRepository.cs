using CourtBookings.Application.Bookings;
using CourtBookings.Domain.Bookings;

namespace CourtBookings.UnitTests.Application;

/// <summary>Hand-written fake of the database, so the handler tests touch no file or server (TEST-3).</summary>
internal sealed class InMemoryBookingRepository : IBookingRepository
{
    public List<Booking> Saved { get; } = [];

    public Task<IReadOnlyList<TimeSlot>> ListSlotsEndingAfterAsync(int courtId, DateTimeOffset instant, CancellationToken cancellationToken)
    {
        IReadOnlyList<TimeSlot> slots = [.. Saved.Where(b => b.CourtId == courtId && b.Slot.End > instant).Select(b => b.Slot)];
        return Task.FromResult(slots);
    }

    public Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        Saved.Add(booking);
        return Task.CompletedTask;
    }
}
