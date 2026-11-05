using System.Net.Mail;

namespace CourtBookings.Domain.Users;

public sealed record EmailAddress
{
    private EmailAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static EmailAddress Create(string value)
    {
        if (!MailAddress.TryCreate(value, out var address) || address.Address != value)
        {
            throw new ArgumentException("Not a valid email address.", nameof(value));
        }

        return new EmailAddress(value);
    }

    public override string ToString() => Value;
}
