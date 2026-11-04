using CourtBookings.Api.Bookings;
using CourtBookings.Application;
using CourtBookings.Infrastructure;

// Composition root (DI-5): the only place that knows every layer.
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("CourtBookings")
    ?? throw new InvalidOperationException("Connection string 'CourtBookings' is missing.");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

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

app.MapBookingEndpoints();

await app.Services.EnsureDatabaseCreatedAsync(CancellationToken.None);
await app.RunAsync();

// Lets the integration tests start the app with WebApplicationFactory<Program> (TEST-5).
public partial class Program;
