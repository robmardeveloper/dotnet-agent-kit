# Review against AGENTS.md

Base commit: 0a69b98. Files reviewed: 12.

## Findings

| # | File | Line | Rule | What breaks it | Proposal | Effort |
|---|------|------|------|----------------|----------|--------|
| 1 | sample/src/CourtBookings.Api/appsettings.json | 14 | SEC-1 | `"SmtpPassword": "Pista-2026-Smtp!"` puts a real-looking credential into a committed config file. | Remove the value. Use User Secrets in development and an environment variable or secret store in production. Rotate the password, because it is now in the working tree. | Small |
| 2 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 22 | SEC-2 | `.FromSqlRaw("SELECT * FROM Bookings WHERE OwnerId = '" + ownerId.Value + "'")` joins input into the SQL text. `ownerId` comes straight from the `{userId}` route segment, so this is SQL injection. | Use LINQ: `dbContext.Bookings.Where(b => b.OwnerId == ownerId).ToListAsync(cancellationToken)`. | Small |
| 3 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 67 | SEC-4 | `UserId.Create(userId)` takes the user from the route and never checks it against the authenticated user. Neither the endpoint nor the handler checks ownership, so any logged-in user can list anyone's bookings (`GET /users/bob/bookings` as alice). | Use `user.GetUserId()`. Either drop `{userId}` from the route (`/bookings/upcoming`), or return 403/404 when it differs from the caller. | Small |
| 4 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 28 | SEC-6 | `LogListRequestedBy(email)` (template `"Upcoming bookings requested by {Email}"`, line 36) writes the user's email address, which is personal data, to the logs. | Remove this log. The user ID is already logged at line 27. | Small |
| 5 | sample/src/CourtBookings.Domain/Bookings/Booking.cs | 63 | DI-2 | `IsUpcoming() => ... Slot.Start > DateTimeOffset.UtcNow` reads the real clock in Domain. The handler then ignores its own `TimeProvider`. With the integration factory's fixed time (2026-06-01) every booking counts as past, so the endpoint returns an empty list. | Pass the time in, `IsUpcoming(DateTimeOffset now)`, as `Create` and `Cancel` already do. Call it with `timeProvider.GetUtcNow()`. | Small |
| 6 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 14 | ASYNC-1 | `bookings.ListOwnedByAsync(query.UserId).Result` blocks on a task. | Make `HandleAsync` `async` and `await` the call. | Small |
| 7 | sample/src/CourtBookings.Application/Bookings/IBookingRepository.cs | 14 | ASYNC-2 | `Task<IReadOnlyList<Booking>> ListOwnedByAsync(UserId ownerId);` is an I/O method with no `CancellationToken`, so the handler cannot pass on the one it receives. | Add `CancellationToken cancellationToken` as the last parameter and pass it on from the handler. | Small |
| 8 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 20 | ASYNC-2 | `ListOwnedByAsync(UserId ownerId)` has no token, and `.ToListAsync()` (line 23) is called without one. | Add the parameter and pass it to `ToListAsync(cancellationToken)`. | Small |
| 9 | sample/tests/CourtBookings.UnitTests/Application/InMemoryBookingRepository.cs | 27 | ASYNC-2 | The fake's `ListOwnedByAsync(UserId ownerId)` signature has no `CancellationToken`. | Match the corrected interface. | Small |
| 10 | sample/tests/CourtBookings.UnitTests/Application/InMemoryBookingRepository.cs | 27 | SOLID-3 | `ListOwnedByAsync(...) => throw new NotImplementedException();` on an interface member. | Implement it: filter `Saved` by `IsOwnedBy(ownerId)`. | Small |
| 11 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 26 | SA-1 | `var email = query.RequestedByEmail!;` hides a null that can really happen: the endpoint has no `RequiresEmail` policy and passes `FindFirstValue(ClaimTypes.Email)`, which can be null. | Remove it together with the email log (finding 4), or check for null. | Small |
| 12 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | SEP-1 | `CanCancelForFree: b.Start - now >= TimeSpan.FromHours(24)` is a business rule computed in the endpoint. That is also why the endpoint needs `TimeProvider`. | Add a domain method, for example `Booking.CanCancelForFree(now)` using `FreeCancellationNotice`. Have the handler put the result in `UpcomingBooking`, and have the endpoint only map it. | Medium |
| 13 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | CC-3 | The free-cancellation rule is a second copy of `Booking.Cancel`'s `Slot.Start - now < FreeCancellationNotice` (Booking.cs:82). | Use one domain method, as in finding 12. | Small |
| 14 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | CC-4 | `TimeSpan.FromHours(24)` is a magic number. The constant `Booking.FreeCancellationNotice` already exists. | Use the constant, or the domain method from finding 12. | Small |
| 15 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 13 | CC-4 | `AddDays(query.Days ?? 7)` uses a magic default of 7 days. | Use a named constant, for example `Booking.MaxBookingAhead`, or a named default window. | Small |
| 16 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 25 | CC-6 | Commented-out code: `// public async Task<IReadOnlyList<Booking>> ListOwnedByAsync(...) => ... Where(b => b.OwnerId == ownerId)...` (lines 25-26). | Delete it. Its LINQ version should replace the raw SQL (finding 2). | Small |
| 17 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 16 | LOG-1 | `Console.WriteLine("Listing upcoming bookings for " + query.UserId);` | Remove it, or log through a `[LoggerMessage]` method on the injected `ILogger<ListUpcomingBookingsHandler>`. | Small |
| 18 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsQuery.cs | 5 | DOM-3 | `string? RequestedByEmail` passes an email address as a raw string, although the `EmailAddress` value object exists and `ClaimsPrincipalExtensions.GetEmail` returns it. | Remove the field (it is only used for the log in finding 4), or type it as `EmailAddress`. | Small |
| 19 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 23 | TEST-1 | The only test asserts nothing about the number of results. Because of finding 5 the list is empty, so the `foreach` never runs and the test passes with zero assertions. Ownership, the `days` window, ordering and the 400 response are not tested at all. | Assert on the exact list returned (count, order, flags). Add tests for another user's ID, for `days` out of range, and unit tests for the handler with `FakeTimeProvider`. | Medium |
| 20 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 23 | TEST-2 | The test is named `Test1` instead of `Method_Scenario_ExpectedResult`. | Rename, for example `ListUpcomingBookings_TwoFutureBookings_ReturnsBothCancellableForFree`. | Small |
| 21 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 31 | TEST-2 | `foreach (var booking in bookings) { Assert.True(...); }` is logic inside a test. | Assert on the whole collection, for example `Assert.Collection` or `Assert.Equal` against the expected list. | Small |
| 22 | sample/src/CourtBookings.Api/appsettings.json | 11 | WORK-2 | The new `"Email"` section (lines 11-15) is not used by any code in this change, and it adds a secret. It is a change the listing feature does not need. | Remove the section from this change. | Small |

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
| DOM-1 | Not applicable | 0 |
| DOM-2 | Not applicable | 0 |
| DOM-3 | Violated | 1 |
| DOM-4 | Complies | 0 |
| DOM-5 | Complies | 0 |
| SEP-1 | Violated | 1 |
| SEP-2 | Complies | 0 |
| SEP-3 | Not applicable | 0 |
| TEST-1 | Violated | 1 |
| TEST-2 | Violated | 2 |
| TEST-3 | Not applicable | 0 |
| TEST-4 | Not applicable | 0 |
| TEST-5 | Complies | 0 |
| SA-1 | Violated | 1 |
| SA-2 | Measured by commands | - |
| SA-3 | Measured by commands | - |
| SA-4 | Not applicable | 0 |
| SEC-1 | Violated | 1 |
| SEC-2 | Violated | 1 |
| SEC-3 | Complies | 0 |
| SEC-4 | Violated | 1 |
| SEC-5 | Not applicable | 0 |
| SEC-6 | Violated | 1 |
| SEC-7 | Measured by commands | - |
| LOG-1 | Violated | 1 |
| LOG-2 | Complies | 0 |
| LOG-3 | Complies | 0 |
| LOG-4 | Complies | 0 |
| ASYNC-1 | Violated | 1 |
| ASYNC-2 | Violated | 3 |
| ASYNC-3 | Complies | 0 |
| WORK-1 | Not reviewable | - |
| WORK-2 | Violated | 1 |
| WORK-3 | Not applicable | 0 |
| WORK-4 | Not applicable | 0 |
| WORK-5 | Not reviewable | - |
| WORK-6 | Not reviewable | - |
| WORK-7 | Measured by commands | - |
| WORK-8 | Not applicable | 0 |
| WORK-9 | Not reviewable | - |
| WORK-10 | Not reviewable | - |

Totals: 22 violations in 18 rules; 21 rules comply; 14 not applicable; 5 not reviewable; 4 measured by commands.

## Doubts

- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:67: `UserId.Create(userId)` throws `ArgumentException` for a route value longer than 200 characters. That probably becomes a 500 instead of a 400 (SEC-3). Not counted, because the route value should be removed anyway (finding 3), and I did not confirm how the exception handler maps `ArgumentException`.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:54: the endpoint takes 6 parameters, and CC-2 says more than 3 need a reason. Not counted, because Minimal API handler parameters are bindings, not a call signature. `TimeProvider` would also go away once finding 12 is fixed.
- sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs:16: the `Console.WriteLine` also builds its text by concatenation (LOG-2). Not counted separately, because it is not an `ILogger` message and is already counted under LOG-1.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:4-5: the endpoint uses Domain types (`Booking.MaxBookingAhead`, `UserId`) directly. Not counted under ARCH-1, because `ClaimsPrincipalExtensions` already references `CourtBookings.Domain.Users` from Api, so this is an existing, allowed reference.
- WORK-9: the changes are not committed (no commits after 0a69b98), so commit size and messages cannot be judged.