using CourtBookings.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace CourtBookings.IntegrationTests;

/// <summary>
/// Runs the real API, replacing only what is external (TEST-5): the database becomes SQLite in
/// memory, the clock becomes a <see cref="FakeTimeProvider"/> and the user comes from
/// <see cref="TestAuthHandler"/> instead of a signed token.
/// </summary>
public sealed class CourtBookingsApiFactory : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset Now = new(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);

    // An in-memory SQLite database lives as long as its connection is open, so the factory keeps it open.
    private readonly SqliteConnection connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<CourtBookingsDbContext>>();
            services.AddDbContext<CourtBookingsDbContext>(options => options.UseSqlite(connection));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now));

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
        });
    }

    /// <summary>A client that calls the API as <paramref name="userId"/>, whose token carries <paramref name="email"/> if given.</summary>
    public HttpClient CreateClientFor(string userId, string? email = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userId);
        if (email is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email);
        }

        return client;
    }

    public HttpClient CreateClientForAlice() => CreateClientFor("alice", "alice@example.com");

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await connection.DisposeAsync();
    }
}
