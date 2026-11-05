using CourtBookings.Application.Bookings;
using CourtBookings.Application.Notifications;
using CourtBookings.Domain.Users;
using Microsoft.Extensions.Logging;

namespace CourtBookings.Infrastructure.Notifications;

/// <summary>
/// The fake email sender (WORK-4): it sends nothing and only logs that an email would have gone out.
/// The log names the booking, not the recipient, because an email address is personal data (SEC-6).
/// </summary>
internal sealed partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendBookingConfirmationAsync(EmailAddress recipient, BookingConfirmation booking, CancellationToken cancellationToken)
    {
        LogConfirmationNotSent(booking.BookingId);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Confirmation email for booking {BookingId} not sent: this is the fake sender")]
    private partial void LogConfirmationNotSent(int bookingId);
}
