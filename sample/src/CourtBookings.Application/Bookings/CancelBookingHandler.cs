using CourtBookings.Domain.Bookings;
using Microsoft.Extensions.Logging;

namespace CourtBookings.Application.Bookings;

/// <summary>Cancels a booking. It returns nothing (SEP-2): the caller reads the result with <see cref="GetBookingHandler"/>.</summary>
public sealed partial class CancelBookingHandler(
    IBookingRepository bookings,
    TimeProvider timeProvider,
    ILogger<CancelBookingHandler> logger)
{
    public async Task HandleAsync(CancelBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetByIdAsync(command.BookingId, cancellationToken);

        // Someone else's booking gets the same answer as a missing one, so its existence is not revealed (SEC-4).
        if (booking is null || !booking.IsOwnedBy(command.UserId))
        {
            throw new BookingRuleException(BookingError.BookingNotFound);
        }

        booking.Cancel(timeProvider.GetUtcNow());
        await bookings.UpdateAsync(booking, cancellationToken);

        LogBookingCancelled(booking.Id, booking.HasCancellationPenalty);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Booking {BookingId} cancelled, penalty: {HasCancellationPenalty}")]
    private partial void LogBookingCancelled(int bookingId, bool hasCancellationPenalty);
}
