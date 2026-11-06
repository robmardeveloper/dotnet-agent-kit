using CourtBookings.Domain.Bookings;

namespace CourtBookings.Application.Bookings;

/// <summary>Reads one booking. A query: it changes nothing (SEP-2).</summary>
public sealed class GetBookingHandler(IBookingRepository bookings)
{
    public async Task<BookingDetails> HandleAsync(GetBookingQuery query, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetOwnedByAsync(query.UserId, query.BookingId, cancellationToken);

        return new BookingDetails(
            booking.Id, booking.CourtId, booking.Slot.Start, booking.Slot.End, booking.Status, booking.HasCancellationPenalty);
    }
}
