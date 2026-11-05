using CourtBookings.Application.Bookings;
using CourtBookings.Application.Notifications;
using CourtBookings.Domain.Users;

namespace CourtBookings.UnitTests.Application;

/// <summary>Hand-written fake that keeps the emails instead of sending them (TEST-3).</summary>
internal sealed class InMemoryEmailSender : IEmailSender
{
    public List<(EmailAddress Recipient, BookingConfirmation Booking)> Sent { get; } = [];

    public Task SendBookingConfirmationAsync(EmailAddress recipient, BookingConfirmation booking, CancellationToken cancellationToken)
    {
        Sent.Add((recipient, booking));
        return Task.CompletedTask;
    }
}
