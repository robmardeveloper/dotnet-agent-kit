# Review against AGENTS.md

Base commit: 79a32f8. Files reviewed: 12.

## Findings

| # | File | Line | Rule | What breaks it | Proposal | Effort |
|---|------|------|------|----------------|----------|--------|
| 1 | sample/src/CourtBookings.Api/appsettings.json | 14 | SEC-1 | `"SmtpPassword": "Pista-2026-Smtp!"`: a password is committed to the repository. | Remove the value. Use User Secrets in development and environment variables or a secret store in production. The leaked password must also be changed. | Small |
| 2 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 22 | SEC-2 | `.FromSqlRaw("SELECT * FROM Bookings WHERE OwnerId = '" + ownerId.Value + "'")`: the SQL is built by joining strings with a user id that comes from the route, so it is open to SQL injection. | Use LINQ: `dbContext.Bookings.Where(b => b.OwnerId == ownerId).ToListAsync(cancellationToken)`. | Small |
| 3 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 67 | SEC-4 | `new ListUpcomingBookingsQuery(UserId.Create(userId), ...)`: the user id comes from the route `/users/{userId}/bookings` (line 23) and is never checked against the logged-in user. Any authenticated user can list anyone's bookings. | Use `user.GetUserId()`, or reject the request when the route id is not the caller's id. Better still, change the route to `/bookings/upcoming`. | Small |
| 4 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 28 | SEC-6 | `LogListRequestedBy(email)` uses the template `"Upcoming bookings requested by {Email}"` (line 36), so it writes the user's email address, which is personal data, to the logs. | Remove this log or log only the user id. | Small |
| 5 | sample/src/CourtBookings.Domain/Bookings/Booking.cs | 63 | DI-2 | `Slot.Start > DateTimeOffset.UtcNow` reads the real clock in Domain. The integration test's `FakeTimeProvider` sets the time to 2026-06-01, so the result depends on today's real date. | Pass the time in, as `Cancel` does: `IsUpcoming(DateTimeOffset now)`. The handler then passes `timeProvider.GetUtcNow()`. | Small |
| 6 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 14 | ASYNC-1 | `bookings.ListOwnedByAsync(query.UserId).Result` blocks on a task. | Make `HandleAsync` `async` and `await` the call. | Small |
| 7 | sample/src/CourtBookings.Application/Bookings/IBookingRepository.cs | 14 | ASYNC-2 | `Task<IReadOnlyList<Booking>> ListOwnedByAsync(UserId ownerId);` does I/O but has no `CancellationToken` parameter. Because of this, the handler cannot pass its token on (handler line 14). | Add `CancellationToken cancellationToken` as the last parameter and pass it in the handler. | Small |
| 8 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 20 | ASYNC-2 | `ListOwnedByAsync(UserId ownerId)` has no token, and `.ToListAsync()` on line 23 is called without one. | Take a `CancellationToken` and pass it to `ToListAsync(cancellationToken)`. | Small |
| 9 | sample/tests/CourtBookings.UnitTests/Application/InMemoryBookingRepository.cs | 27 | SOLID-3 | `public Task<IReadOnlyList<Booking>> ListOwnedByAsync(UserId ownerId) => throw new NotImplementedException();` throws `NotImplementedException` for an interface member. | Implement it over `Saved`: `Saved.Where(b => b.IsOwnedBy(ownerId))`. | Small |
| 10 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 26 | SA-1 | `var email = query.RequestedByEmail!;` uses `!` to silence a nullable warning. The email claim can be missing, because this endpoint has no `RequiresEmail` policy. | Remove the email from the query (see #4), or check it for null. | Small |
| 11 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | SEP-1 | `CanCancelForFree: b.Start - now >= TimeSpan.FromHours(24)`: the endpoint works out the free-cancellation business rule itself. | Move the rule into `Booking` (for example `CanCancelForFree(now)` using `FreeCancellationNotice`) and return the result in `UpcomingBooking` from the handler. | Medium |
| 12 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsQuery.cs | 5 | DOM-3 | `string? RequestedByEmail` passes an email as a raw string, although the `EmailAddress` value object already exists. | Use `EmailAddress`, or drop the field (see #4). | Small |
| 13 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | CC-3 | `b.Start - now >= TimeSpan.FromHours(24)` repeats the free-cancellation rule that `Booking.Cancel` already implements (`Booking.cs` line 82, `FreeCancellationNotice`). | Keep one place for it in `Booking` (same fix as #11). | Small |
| 14 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | CC-4 | `TimeSpan.FromHours(24)` is a magic number. | Use `Booking.FreeCancellationNotice`. | Small |
| 15 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 13 | CC-4 | `query.Days ?? 7` is a magic number. | Use a named constant, or `Booking.MaxBookingAhead`. | Small |
| 16 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 25 | CC-6 | Lines 25-26 are commented-out code: `// public async Task<IReadOnlyList<Booking>> ListOwnedByAsync(...) => ...`. | Delete them. | Small |
| 17 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 16 | LOG-1 | `Console.WriteLine("Listing upcoming bookings for " + query.UserId);` logs through the console instead of the injected `ILogger`. | Remove it, or replace it with a `[LoggerMessage]` method. | Small |
| 18 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 23 | TEST-2 | `public async Task Test1()` does not follow the `Method_Scenario_ExpectedResult` name pattern. | Rename it, for example `ListUpcomingBookings_TwoFutureBookings_ReturnsBothCancellableForFree`. | Small |
| 19 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 31 | TEST-2 | `foreach (var booking in bookings) { Assert.True(...) }` puts logic (a loop) in the test. With an empty list it passes without checking anything. | Assert the expected count and values explicitly, for example with `Assert.Collection`. | Small |
| 20 | sample/src/CourtBookings.Api/appsettings.json | 11 | WORK-2 | The new `"Email": { ... SmtpPassword ... }` section adds secrets that the task (listing upcoming bookings) does not need. | Revert the change to appsettings.json. | Small |

## Rules

| Rule | Status | Violations |
|------|--------|------------|
| SOLID-1 | Complies | 0 |
| SOLID-2 | Not applicable | 0 |
| SOLID-3 | Violated | 1 |
| SOLID-4 | Complies | 0 |
| SOLID-5 | Complies | 0 |
| CC-1 | Complies | 0 |
| CC-2 | Complies | 0 |
| CC-3 | Violated | 1 |
| CC-4 | Violated | 2 |
| CC-5 | Complies | 0 |
| CC-6 | Violated | 1 |
| ARCH-1 | Not applicable | 0 |
| ARCH-2 | Complies | 0 |
| ARCH-3 | Complies | 0 |
| ARCH-4 | Not applicable | 0 |
| DI-1 | Complies | 0 |
| DI-2 | Violated | 1 |
| DI-3 | Not applicable | 0 |
| DI-4 | Not applicable | 0 |
| DI-5 | Complies | 0 |
| DI-6 | Complies | 0 |
| DOM-1 | Complies | 0 |
| DOM-2 | Not applicable | 0 |
| DOM-3 | Violated | 1 |
| DOM-4 | Not applicable | 0 |
| DOM-5 | Complies | 0 |
| SEP-1 | Violated | 1 |
| SEP-2 | Complies | 0 |
| SEP-3 | Not applicable | 0 |
| TEST-1 | Complies | 0 |
| TEST-2 | Violated | 2 |
| TEST-3 | Complies | 0 |
| TEST-4 | Not applicable | 0 |
| TEST-5 | Complies | 0 |
| SA-1 | Violated | 1 |
| SA-2 | Measured by commands | 0 |
| SA-3 | Measured by commands | 0 |
| SA-4 | Not applicable | 0 |
| SEC-1 | Violated | 1 |
| SEC-2 | Violated | 1 |
| SEC-3 | Complies | 0 |
| SEC-4 | Violated | 1 |
| SEC-5 | Not applicable | 0 |
| SEC-6 | Violated | 1 |
| SEC-7 | Measured by commands | 0 |
| LOG-1 | Violated | 1 |
| LOG-2 | Complies | 0 |
| LOG-3 | Complies | 0 |
| LOG-4 | Complies | 0 |
| ASYNC-1 | Violated | 1 |
| ASYNC-2 | Violated | 2 |
| ASYNC-3 | Complies | 0 |
| WORK-1 | Not reviewable | 0 |
| WORK-2 | Violated | 1 |
| WORK-3 | Not applicable | 0 |
| WORK-4 | Not applicable | 0 |
| WORK-5 | Not reviewable | 0 |
| WORK-6 | Not reviewable | 0 |
| WORK-7 | Measured by commands | 0 |
| WORK-8 | Not applicable | 0 |
| WORK-9 | Not reviewable | 0 |
| WORK-10 | Not reviewable | 0 |

Totals: 20 violations in 17 rules; 22 rules comply; 14 not applicable; 5 not reviewable; 4 measured by commands.

## Doubts

- sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs:22 (TEST-1): a test exists, so TEST-1 is marked as complying. But the test likely proves nothing. `IsUpcoming` reads the real clock (finding #5), and the bookings are dated around the fake "now" of 2026-06-01, so the list probably comes back empty and the `foreach` checks nothing. Nothing tests the `days` filter, ownership, or the new handler at unit level. Not counted, because the rule's wording ("ship with a test") is met.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:67 (SEC-3): `UserId.Create(userId)` validates the route value only by throwing `ArgumentException`. That probably reaches the client as a 500 error, not as a rejected input (400). The email claim is also read as a raw string without validation. Not counted, because the input is rejected, if badly, and the right fix is to drop the route parameter (finding #3).
- sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs:16 (LOG-2): the console message is built by string concatenation. I counted it only under LOG-1, because it does not go through a logger.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:54 (CC-2): `ListUpcomingBookings` has 6 parameters. Minimal API handlers receive their services this way, which is a plausible reason, so it is not counted.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:58 (SOLID-5): the endpoint depends on the concrete `ListUpcomingBookingsHandler`. The existing endpoints do the same for every handler, so I did not treat it as a new violation.
- WORK-9: all the changes are uncommitted, so the diff cannot show how they would be split into commits.