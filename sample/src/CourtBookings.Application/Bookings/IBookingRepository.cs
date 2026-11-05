using CourtBookings.Domain.Bookings;

namespace CourtBookings.Application.Bookings;

/// <summary>The bookings aggregate's repository: one per aggregate, with what its use cases need (DOM-4).</summary>
public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int bookingId, CancellationToken cancellationToken);

    /// <summary>The slots of a court's confirmed bookings that have not ended by <paramref name="instant"/>.</summary>
    Task<IReadOnlyList<TimeSlot>> ListConfirmedSlotsEndingAfterAsync(int courtId, DateTimeOffset instant, CancellationToken cancellationToken);

    Task AddAsync(Booking booking, CancellationToken cancellationToken);

    Task UpdateAsync(Booking booking, CancellationToken cancellationToken);
}
