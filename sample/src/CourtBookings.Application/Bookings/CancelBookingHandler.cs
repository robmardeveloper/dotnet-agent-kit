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
        var booking = await bookings.GetOwnedByAsync(command.UserId, command.BookingId, cancellationToken);

        booking.Cancel(timeProvider.GetUtcNow());
        await bookings.UpdateAsync(booking, cancellationToken);

        LogBookingCancelled(booking.Id, booking.HasCancellationPenalty);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Booking {BookingId} cancelled, penalty: {HasCancellationPenalty}")]
    private partial void LogBookingCancelled(int bookingId, bool hasCancellationPenalty);
}
