using CourtBookings.Domain.Bookings;

namespace CourtBookings.Application.Bookings;

public interface IBookingRepository
{
    /// <summary>The slots of a court's bookings that have not ended by <paramref name="instant"/>.</summary>
    Task<IReadOnlyList<TimeSlot>> ListSlotsEndingAfterAsync(int courtId, DateTimeOffset instant, CancellationToken cancellationToken);

    Task AddAsync(Booking booking, CancellationToken cancellationToken);
}
