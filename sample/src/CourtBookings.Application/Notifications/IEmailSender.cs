using CourtBookings.Application.Bookings;
using CourtBookings.Domain.Users;

namespace CourtBookings.Application.Notifications;

/// <summary>Sends emails to users. An interface because email is external (WORK-4): tests and local runs use a fake.</summary>
public interface IEmailSender
{
    Task SendBookingConfirmationAsync(EmailAddress recipient, BookingConfirmation booking, CancellationToken cancellationToken);
}
