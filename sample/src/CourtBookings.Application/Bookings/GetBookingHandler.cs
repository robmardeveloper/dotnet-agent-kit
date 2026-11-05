using CourtBookings.Domain.Bookings;

namespace CourtBookings.Application.Bookings;

/// <summary>Reads one booking. A query: it changes nothing (SEP-2).</summary>
public sealed class GetBookingHandler(IBookingRepository bookings)
{
    public async Task<BookingDetails> HandleAsync(GetBookingQuery query, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(query.BookingId, cancellationToken);

        // Someone else's booking gets the same answer as a missing one, so its existence is not revealed (SEC-4).
        if (booking is null || !booking.IsOwnedBy(query.UserId))
        {
            throw new BookingRuleException(BookingError.BookingNotFound);
        }

        return new BookingDetails(
            booking.Id, booking.CourtId, booking.Slot.Start, booking.Slot.End, booking.Status, booking.HasCancellationPenalty);
    }
}
