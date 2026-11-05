using CourtBookings.Domain.Users;

namespace CourtBookings.UnitTests;

internal static class TestUsers
{
    public static readonly UserId Alice = UserId.Create("alice");

    public static readonly EmailAddress AliceEmail = EmailAddress.Create("alice@example.com");

    public static readonly UserId Bob = UserId.Create("bob");
}
