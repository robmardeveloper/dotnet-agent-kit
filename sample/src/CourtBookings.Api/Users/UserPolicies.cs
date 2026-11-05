namespace CourtBookings.Api.Users;

public static class UserPolicies
{
    /// <summary>The user's token carries an email address, needed to send them the booking confirmation.</summary>
    public const string RequiresEmail = nameof(RequiresEmail);
}
