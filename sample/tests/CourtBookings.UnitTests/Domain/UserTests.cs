using CourtBookings.Domain.Users;
using Xunit;

namespace CourtBookings.UnitTests.Domain;

public sealed class UserTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UserIdCreate_Blank_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => UserId.Create(value));
    }

    [Fact]
    public void UserIdCreate_LongerThanMaxLength_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UserId.Create(new string('a', UserId.MaxLength + 1)));
    }

    [Theory]
    [InlineData("not an email")]
    [InlineData("Alice <alice@example.com>")]
    [InlineData("")]
    public void EmailAddressCreate_NotJustAnAddress_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => EmailAddress.Create(value));
    }

    [Fact]
    public void EmailAddressCreate_PlainAddress_KeepsIt()
    {
        Assert.Equal("alice@example.com", EmailAddress.Create("alice@example.com").Value);
    }
}
