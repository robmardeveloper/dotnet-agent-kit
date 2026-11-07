# Review against AGENTS.md

Base commit: 0a69b98. Files reviewed: 12.

## Findings

| # | File | Line | Rule | What breaks it | Proposal | Effort |
|---|------|------|------|----------------|----------|--------|
| 1 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 182 | SEC-2 | `.FromSqlRaw("SELECT * FROM Bookings WHERE OwnerId = '" + ownerId.Value + "'")` builds SQL by joining strings and passes the input to `FromSqlRaw`. The value comes from the route `userId`, so anyone can inject SQL. | Use LINQ: `dbContext.Bookings.Where(b => b.OwnerId == ownerId).ToListAsync(cancellationToken)`. | Small |
| 2 | sample/src/CourtBookings.Api/appsettings.json | 14 | SEC-1 | `"SmtpPassword": "Pista-2026-Smtp!"` puts a credential in the repository. It stays in git history. | Remove it, use User Secrets in development and an environment variable or secret store in production, and rotate the password. | Small |
| 3 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 67 | SEC-4 | `new ListUpcomingBookingsQuery(UserId.Create(userId), ...)` takes the owner from the route `/users/{userId}/bookings`. Neither the endpoint nor the handler checks that it matches the caller, so any logged-in user can list another user's bookings. | Use `user.GetUserId()`, as the other endpoints do, and drop the route parameter (for example `/bookings/upcoming`). Or reject the request when the route id is not the caller's. | Small |
| 4 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 28 (template at 36) | SEC-6 | `LogListRequestedBy(email)` with `"Upcoming bookings requested by {Email}"` writes the user's email address, which is personal data, to the logs. | Remove this log, or log the user id only. | Small |
| 5 | sample/src/CourtBookings.Domain/Bookings/Booking.cs | 139 | DI-2 | `IsUpcoming() => ... Slot.Start > DateTimeOffset.UtcNow` reads the real clock in Domain. The handler filters with the injected `TimeProvider`, so the two use different clocks and the result depends on the real time. | Pass the time in: `IsUpcoming(DateTimeOffset now)`, called with `timeProvider.GetUtcNow()`, as `Create` and `Cancel` already do. | Small |
| 6 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 14 | ASYNC-1 | `bookings.ListOwnedByAsync(query.UserId).Result` blocks on a task. | Make `HandleAsync` `async` and `await` the call. | Small |
| 7 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 26 | SA-1 | `var email = query.RequestedByEmail!;` uses the null-forgiving operator on a value that is null when the token has no email claim. | Remove the value together with the log in finding 4, or check for null. | Small |
| 8 | sample/tests/CourtBookings.UnitTests/Application/InMemoryBookingRepository.cs | 27 | SOLID-3 | `ListOwnedByAsync(...) => throw new NotImplementedException();` on an interface member. | Implement it over the in-memory list, for example `Saved.Where(b => b.IsOwnedBy(ownerId)).ToList()`. | Small |
| 9 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | SEP-1 | `CanCancelForFree: b.Start - now >= TimeSpan.FromHours(24)` puts the free-cancellation rule in the endpoint, and the endpoint reads the clock through `TimeProvider` to do it. | Compute it in the domain (for example a `Booking` method using `FreeCancellationNotice`), carry it on `UpcomingBooking`, and have the endpoint only map it. | Medium |
| 10 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsQuery.cs | 3 | DOM-3 | `string? RequestedByEmail` is a raw string, but the `EmailAddress` value object already exists. | Use `EmailAddress?`, or remove the field (it only feeds the log in finding 4). | Small |
| 11 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | CC-3 | `TimeSpan.FromHours(24)` repeats the rule already in `Booking.FreeCancellationNotice` and `Booking.Cancel` (Booking.cs:85, 158). | Keep the rule in one place, the domain (see finding 9). | Small |
| 12 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | CC-4 | Magic number `24` in `TimeSpan.FromHours(24)`. | Use `Booking.FreeCancellationNotice` (or fix through finding 9). | Small |
| 13 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 13 | CC-4 | Magic number `7` in `query.Days ?? 7`. | Use a named constant, or `Booking.MaxBookingAhead`. | Small |
| 14 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 185-186 | CC-6 | Commented-out code: `// public async Task<IReadOnlyList<Booking>> ListOwnedByAsync(...` | Delete it. The LINQ version should be the live code (finding 1). | Small |
| 15 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 16 | LOG-1 | `Console.WriteLine("Listing upcoming bookings for " + query.UserId);` logs through the console. | Remove it, or add a `[LoggerMessage]` method on the injected `ILogger<ListUpcomingBookingsHandler>`. | Small |
| 16 | sample/src/CourtBookings.Application/Bookings/IBookingRepository.cs | 14 | ASYNC-2 | `Task<IReadOnlyList<Booking>> ListOwnedByAsync(UserId ownerId);` is an I/O method with no `CancellationToken`. | Add `CancellationToken cancellationToken` as the last parameter and pass it from the handler. | Small |
| 17 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 180, 183 | ASYNC-2 | `ListOwnedByAsync(UserId ownerId)` has no token, and `.ToListAsync()` gets none. | Add the parameter and pass it to `ToListAsync(cancellationToken)`. | Small |
| 18 | sample/tests/CourtBookings.UnitTests/Application/InMemoryBookingRepository.cs | 27 | ASYNC-2 | `ListOwnedByAsync(UserId ownerId)` does not match the I/O signature with a token. | Add the parameter, to match the interface. | Small |
| 19 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 72 | TEST-2 | The test is named `Test1`, not `Method_Scenario_ExpectedResult`. | Rename it, for example `ListUpcomingBookings_TwoBookingsMoreThan24HoursAhead_ReturnsBothFreeToCancel`. | Small |
| 20 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 80-83 | TEST-2 | `foreach (var booking in bookings) { Assert.True(...); }` puts a loop in the test. Combined with `?? []`, the test also passes when the list is empty. | Assert the expected list directly (count, ids, `CanCancelForFree`) with no loop. | Small |

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
| ASYNC-2 | Violated | 3 |
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

Totals: 20 violations in 16 rules; 24 rules comply; 12 not applicable; 6 not reviewable; 4 measured by commands.

## Doubts

- **TEST-1**, ListUpcomingBookingsEndpointTests.cs:72: the new endpoint has an integration test, so I counted TEST-1 as complying. But that test passes even with an empty list. There is also no unit test for `ListUpcomingBookingsHandler` or `Booking.IsUpcoming`, and no test for the `days` validation or the ownership check. Whether one weak test is enough for TEST-1 is a judgement call.
- **SEC-3**, BookingEndpoints.cs:67: `UserId.Create(userId)` turns a route value longer than 200 characters into an `ArgumentOutOfRangeException`. That is probably a 500 rather than a clean rejection. I did not count it because I did not check how the exception handler maps that exception, and fixing SEC-4 (taking the id from the token) removes the input anyway. `days` is checked against an allowlisted range, which is why SEC-3 is "Complies".
- **CC-2**, BookingEndpoints.cs:54-60: `ListUpcomingBookings` has 6 parameters. Most are values the framework injects into minimal API handlers, as in the existing endpoints. Only `TimeProvider` has no good reason, and it goes away with the SEP-1 fix, so I did not count it.
- **LOG-2**, ListUpcomingBookingsHandler.cs:16: the `Console.WriteLine` builds its message by concatenation. I counted it once, under LOG-1, rather than also as a LOG-2 message template.
- **DOM-5**, ListUpcomingBookingsHandler.cs:20-23: the handler filters by the date window and sorts in memory. This is query shaping rather than a business rule taken from the entity, so I did not count it.
- **WORK-2**, appsettings.json:11-15: an SMTP section has nothing to do with listing upcoming bookings, so this looks like a change outside the task. The task statement was not available to me, so I could not judge it. The secret itself is counted under SEC-1.