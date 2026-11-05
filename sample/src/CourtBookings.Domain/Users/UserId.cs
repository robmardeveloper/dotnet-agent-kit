namespace CourtBookings.Domain.Users;

/// <summary>Who a user is, as the identity provider names them (the token's subject).</summary>
public sealed record UserId
{
    public const int MaxLength = 200;

    private UserId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static UserId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength);
        return new UserId(value);
    }

    public override string ToString() => Value;
}
