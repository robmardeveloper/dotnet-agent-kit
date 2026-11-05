using CourtBookings.Application.Courts;
using CourtBookings.Application.Notifications;
using CourtBookings.Domain.Bookings;
using Microsoft.Extensions.Logging;

namespace CourtBookings.Application.Bookings;

/// <summary>Books a court: loads what the rule needs, lets the domain decide, saves (DOM-5), and emails the confirmation.</summary>
public sealed partial class BookCourtHandler(
    ICourtRepository courts,
    IBookingRepository bookings,
    IEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<BookCourtHandler> logger)
{
    public async Task<BookingConfirmation> HandleAsync(BookCourtCommand command, CancellationToken cancellationToken)
    {
        if (!await courts.ExistsAsync(command.CourtId, cancellationToken))
        {
            throw new BookingRuleException(BookingError.CourtNotFound);
        }

        var slot = TimeSlot.Create(command.Start, command.Duration);
        var now = timeProvider.GetUtcNow();
        var takenSlots = await bookings.ListConfirmedSlotsEndingAfterAsync(command.CourtId, now, cancellationToken);

        var booking = Booking.Create(command.UserId, command.CourtId, slot, takenSlots, now);
        await bookings.AddAsync(booking, cancellationToken);
        LogBookingConfirmed(booking.Id, booking.CourtId, booking.Slot.Start);

        var confirmation = new BookingConfirmation(booking.Id, booking.CourtId, booking.Slot.Start, booking.Slot.End);
        await emailSender.SendBookingConfirmationAsync(command.Email, confirmation, cancellationToken);
        return confirmation;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Booking {BookingId} confirmed for court {CourtId} at {StartTime}")]
    private partial void LogBookingConfirmed(int bookingId, int courtId, DateTimeOffset startTime);
}
