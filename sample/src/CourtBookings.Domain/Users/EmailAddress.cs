using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;

namespace CourtBookings.Domain.Users;

public sealed record EmailAddress
{
    private EmailAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static EmailAddress Create(string value) =>
        TryCreate(value, out var email) ? email : throw new ArgumentException("Not a valid email address.", nameof(value));

    /// <summary>Accepts a plain address only (<c>alice@example.com</c>), not one with a display name.</summary>
    public static bool TryCreate(string? value, [NotNullWhen(true)] out EmailAddress? email)
    {
        email = MailAddress.TryCreate(value, out var address) && address.Address == value ? new EmailAddress(value) : null;
        return email is not null;
    }

    public override string ToString() => Value;
}
