using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CourtBookings.Api.Bookings;
using CourtBookings.Api.Users;
using CourtBookings.Application;
using CourtBookings.Infrastructure;

// Composition root (DI-5): the only place that knows every layer.
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("CourtBookings")
    ?? throw new InvalidOperationException("Connection string 'CourtBookings' is missing.");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

// Tokens are JWTs; in development, "dotnet user-jwts create" issues them and keeps the signing key
// in User Secrets, outside the repository (SEC-1). The issuer and audiences come from configuration.
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(UserPolicies.RequiresEmail, policy => policy.RequireClaim(ClaimTypes.Email));

builder.Services.AddLocalization();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BookingRuleExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseRequestLocalization(options => options
    .AddSupportedCultures("en", "es")
    .AddSupportedUICultures("en", "es")
    .SetDefaultCulture("en"));
app.UseAuthentication();
app.UseAuthorization();

app.MapBookingEndpoints();

await app.Services.EnsureDatabaseCreatedAsync(CancellationToken.None);
await app.RunAsync();

// Lets the integration tests start the app with WebApplicationFactory<Program> (TEST-5).
public partial class Program;
