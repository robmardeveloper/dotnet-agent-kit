using CourtBookings.Domain.Bookings;
using CourtBookings.Domain.Users;

namespace CourtBookings.Application.Bookings;

public static class BookingRepositoryExtensions
{
    /// <summary>
    /// Loads a booking for the user who owns it. Someone else's booking gets the same answer as a
    /// missing one, so its existence is not revealed (SEC-4). Every use case that acts on one booking goes through here.
    /// </summary>
    public static async Task<Booking> GetOwnedByAsync(
        this IBookingRepository bookings, UserId userId, int bookingId, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(bookingId, cancellationToken);

        if (booking is null || !booking.IsOwnedBy(userId))
        {
            throw new BookingRuleException(BookingError.BookingNotFound);
        }

        return booking;
    }
}
