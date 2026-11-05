using System.Security.Claims;
using CourtBookings.Domain.Users;

namespace CourtBookings.Api.Users;

/// <summary>Reads the user from the token's claims. The JWT handler maps "sub" and "email" to these claim types.</summary>
public static class ClaimsPrincipalExtensions
{
    // The fallback policy only lets authenticated users in, so a missing claim is a token
    // the API should never have accepted: a server error, not the client's fault.
    public static UserId GetUserId(this ClaimsPrincipal user) =>
        UserId.Create(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The authenticated user has no subject claim."));

    // Only called on endpoints with the RequiresEmail policy, which checks the claim is there.
    public static EmailAddress GetEmail(this ClaimsPrincipal user) =>
        EmailAddress.Create(user.FindFirstValue(ClaimTypes.Email)
            ?? throw new InvalidOperationException("The authenticated user has no email claim."));
}
