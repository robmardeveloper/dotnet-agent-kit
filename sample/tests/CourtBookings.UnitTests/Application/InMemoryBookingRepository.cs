using CourtBookings.Application.Bookings;
using CourtBookings.Domain.Bookings;

namespace CourtBookings.UnitTests.Application;

/// <summary>
/// Hand-written fake of the database, so the handler tests touch no file or server (TEST-3).
/// It does not assign IDs (only the database does), so every booking it holds has ID 0.
/// </summary>
internal sealed class InMemoryBookingRepository : IBookingRepository
{
    public List<Booking> Saved { get; } = [];

    public int UpdateCount { get; private set; }

    public Task<Booking?> GetByIdAsync(int bookingId, CancellationToken cancellationToken) =>
        Task.FromResult(Saved.FirstOrDefault(b => b.Id == bookingId));

    public Task<IReadOnlyList<TimeSlot>> ListConfirmedSlotsEndingAfterAsync(int courtId, DateTimeOffset instant, CancellationToken cancellationToken)
    {
        IReadOnlyList<TimeSlot> slots =
            [.. Saved.Where(b => b.CourtId == courtId && b.Status == BookingStatus.Confirmed && b.Slot.End > instant).Select(b => b.Slot)];
        return Task.FromResult(slots);
    }

    public Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        Saved.Add(booking);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Booking booking, CancellationToken cancellationToken)
    {
        UpdateCount++;
        return Task.CompletedTask;
    }
}
