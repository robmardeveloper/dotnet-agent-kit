# Review against AGENTS.md

Base commit: 1646b96. Files reviewed: 12.

## Findings

| # | File | Line | Rule | What breaks it | Proposal | Effort |
|---|------|------|------|----------------|----------|--------|
| 1 | sample/src/CourtBookings.Api/appsettings.json | 14 | SEC-1 | `"SmtpPassword": "Pista-2026-Smtp!"` puts a real credential in a committed config file. | Remove the value from the repository and rotate the password, since it is now exposed. Read it from User Secrets in development and from environment variables or a secret store in production. | Small |
| 2 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 22 | SEC-2 | `.FromSqlRaw("SELECT * FROM Bookings WHERE OwnerId = '" + ownerId.Value + "'")` joins the `userId` from the route into the SQL text. This allows SQL injection. | Use LINQ: `dbContext.Bookings.Where(b => b.OwnerId == ownerId).ToListAsync(cancellationToken)`. | Small |
| 3 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 67 | SEC-4 | `new ListUpcomingBookingsQuery(UserId.Create(userId), ...)` takes the owner from the route `/users/{userId}/bookings` and never checks it against the caller. Any logged-in user can list anyone else's bookings. | Use `user.GetUserId()` (for example, route `/bookings/upcoming`). Otherwise, reject the request when the route `userId` is not the caller's ID. | Small |
| 4 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 28 | SEC-6 | `LogListRequestedBy(email)` writes the user's email address (personal data) to the log. The template is at line 36. | Drop that log message. The user ID is already logged at line 27. | Small |
| 5 | sample/src/CourtBookings.Domain/Bookings/Booking.cs | 63 | DI-2 | `Slot.Start > DateTimeOffset.UtcNow` reads the real clock in Domain. The behaviour is also wrong under the fake clock: the integration test's `FakeTimeProvider` is at 2026-06-01, so every booking looks past and the list comes back empty. | Use `IsUpcoming(DateTimeOffset now)` and pass `timeProvider.GetUtcNow()` from the handler, as `Create` and `Cancel` already do. | Small |
| 6 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 14 | ASYNC-1 | `bookings.ListOwnedByAsync(query.UserId).Result` blocks on a task. | Make `HandleAsync` `async` and `await` the call. | Small |
| 7 | sample/src/CourtBookings.Application/Bookings/IBookingRepository.cs | 14 | ASYNC-2 | `Task<IReadOnlyList<Booking>> ListOwnedByAsync(UserId ownerId);` is an I/O method with no `CancellationToken` parameter. | Add `CancellationToken cancellationToken` as the last parameter and pass it from the handler. | Small |
| 8 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 20 | ASYNC-2 | `ListOwnedByAsync(UserId ownerId)` takes no token, so `.ToListAsync()` at line 23 cannot be cancelled. | Accept `CancellationToken cancellationToken` and pass it to `ToListAsync`. | Small |
| 9 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 16 | LOG-1 | `Console.WriteLine("Listing upcoming bookings for " + query.UserId);` logs through the console instead of the injected `ILogger`. | Remove the line. If it is needed, use a `[LoggerMessage]` method. | Small |
| 10 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 26 | SA-1 | `var email = query.RequestedByEmail!;` silences a nullable warning. The email claim is optional on this endpoint because it has no `RequiresEmail` policy. | Remove it together with finding 4. If the value is needed, check it for null or make the type non-nullable. | Small |
| 11 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | SEP-1 | `CanCancelForFree: b.Start - now >= TimeSpan.FromHours(24)` decides a business rule (free cancellation) inside the endpoint. | Move the rule into `Booking` (for example `CanCancelForFree(now)`) and have the handler return the result in `UpcomingBooking`. | Medium |
| 12 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | CC-3 | The same free-cancellation calculation already exists in `Booking.Cancel` (`Slot.Start - now < FreeCancellationNotice`, Booking.cs:82). | Use one domain method for both, as in finding 11. | Medium |
| 13 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | CC-4 | `TimeSpan.FromHours(24)` is a magic value. The named constant `Booking.FreeCancellationNotice` already exists. | Use `Booking.FreeCancellationNotice` (fixed anyway by finding 11). | Small |
| 14 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 13 | CC-4 | `AddDays(query.Days ?? 7)` uses a magic default of 7. | Use a named constant, for example `Booking.MaxBookingAhead`. | Small |
| 15 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsQuery.cs | 5 | DOM-3 | `string? RequestedByEmail` passes an email address as a raw string. The `EmailAddress` value object already exists. | Remove the field (finding 4). If it is needed, type it as `EmailAddress?`. | Small |
| 16 | sample/tests/CourtBookings.UnitTests/Application/InMemoryBookingRepository.cs | 27 | SOLID-3 | `ListOwnedByAsync(UserId ownerId) => throw new NotImplementedException();` breaks the fake as a substitute for `IBookingRepository`. | Implement it over `Saved` (filter by `IsOwnedBy(ownerId)`). | Small |
| 17 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 31 | TEST-2 | `foreach (var booking in bookings) { Assert.True(...) }` puts logic in a test. It also passes when the list is empty, which is what happens now (see finding 5). | Assert the exact expected list (count, IDs, order, `CanCancelForFree`) with no loop. | Small |
| 18 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 23 | TEST-2 | `public async Task Test1()` does not follow `Method_Scenario_ExpectedResult`. | Rename it, for example `ListUpcomingBookings_TwoFutureBookings_ReturnsBothSoonestFirst`. | Small |
| 19 | sample/src/CourtBookings.Domain/Bookings/Booking.cs | 63 | TEST-1 | The new `IsUpcoming()` and the new `ListUpcomingBookingsHandler` have no unit tests. The one integration test cannot fail on an empty result, so nothing shows the behaviour works. | Add unit tests for `IsUpcoming` (confirmed or cancelled, past or future) and for the handler (the days window, the ordering). Make the integration test assert concrete results. | Medium |
| 20 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 25 | CC-6 | Lines 25-26 hold commented-out code: `// public async Task<IReadOnlyList<Booking>> ListOwnedByAsync(...) => ... Where(b => b.OwnerId == ownerId)...`. | Delete it, and use that LINQ version as the real code (finding 2). | Small |

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
| ARCH-1 | Complies | 0 |
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
| DOM-4 | Complies | 0 |
| DOM-5 | Complies | 0 |
| SEP-1 | Violated | 1 |
| SEP-2 | Complies | 0 |
| SEP-3 | Not applicable | 0 |
| TEST-1 | Violated | 1 |
| TEST-2 | Violated | 2 |
| TEST-3 | Complies | 0 |
| TEST-4 | Complies | 0 |
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
| WORK-2 | Not reviewable | 0 |
| WORK-3 | Not applicable | 0 |
| WORK-4 | Not applicable | 0 |
| WORK-5 | Not reviewable | 0 |
| WORK-6 | Not reviewable | 0 |
| WORK-7 | Measured by commands | 0 |
| WORK-8 | Not applicable | 0 |
| WORK-9 | Not reviewable | 0 |
| WORK-10 | Not reviewable | 0 |

Totals: 20 violations in 17 rules; 24 rules comply; 11 not applicable; 6 not reviewable; 4 measured by commands.

## Doubts

- sample/src/CourtBookings.Api/appsettings.json:11: the `Email` section is not used by the "list upcoming bookings" change, so it may be outside the task (WORK-2). The task statement is not available, so I did not count it. The secret in it is already counted under SEC-1.
- sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs:16: the `Console.WriteLine` message is also built by concatenation, which is what LOG-2 forbids. LOG-2 is about `ILogger` templates, and this line is already counted under LOG-1, so I did not count it twice.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:67: `UserId.Create(userId)` rejects an over-long or blank route value by throwing `ArgumentException`. That probably ends up as a 500 instead of a 400. The input is still rejected, so I did not count it under SEC-3. Fixing SEC-4 removes this path anyway.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:54: the endpoint handler has 6 parameters, and CC-2 asks for a reason above 3. They are all bound by DI or the framework, as in the existing endpoints, so I did not count it.
- sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs:21: the handler filters by date window and orders in memory. That is query shaping rather than a domain rule, so I did not count it under DOM-5.
- sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs:23: the test's result depends on the real date, because `IsUpcoming` reads `DateTimeOffset.UtcNow`. That makes it unrepeatable (TEST-3). The cause is in production code and is counted under DI-2, so I did not count it again.