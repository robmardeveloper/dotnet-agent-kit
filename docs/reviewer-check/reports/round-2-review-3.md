# Review against AGENTS.md

Base commit: 4debb47. Files reviewed: 12.

## Findings

| # | File | Line | Rule | What breaks it | Proposal | Effort |
|---|------|------|------|----------------|----------|--------|
| 1 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 22 | SEC-2 | `.FromSqlRaw("SELECT * FROM Bookings WHERE OwnerId = '" + ownerId.Value + "'")`. The `userId` from the route is joined into the SQL text, which allows SQL injection. | Use LINQ: `dbContext.Bookings.Where(b => b.OwnerId == ownerId).ToListAsync(cancellationToken)`. | Small |
| 2 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 67 | SEC-4 | `new ListUpcomingBookingsQuery(UserId.Create(userId), ...)` takes the user from the route `/users/{userId}/bookings` and never checks it against the logged-in user. Any logged-in user can list another user's bookings. | Use `user.GetUserId()` like the other endpoints. Drop the route parameter or reject it when it does not match the logged-in user. | Small |
| 3 | sample/src/CourtBookings.Api/appsettings.json | 14 | SEC-1 | `"SmtpPassword": "Pista-2026-Smtp!"` puts a credential in a committed config file. | Remove it. Use User Secrets in development and environment variables or a secret store in production. Rotate the password, because it is now in the working tree. | Small |
| 4 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 28 | SEC-6 | `LogListRequestedBy(email)` writes the requester's email address (personal data) to the logs. The template is at line 36: `"Upcoming bookings requested by {Email}"`. | Log the user ID only, or remove this log message. | Small |
| 5 | sample/src/CourtBookings.Domain/Bookings/Booking.cs | 63 | DI-2 | `IsUpcoming() => ... && Slot.Start > DateTimeOffset.UtcNow` reads the real clock in Domain. The handler and the API use `TimeProvider`, so the two disagree. Under the integration factory's `FakeTimeProvider` (2026-06-01), bookings made for June 2026 are filtered out because the real date is later. | Pass the time in: `IsUpcoming(DateTimeOffset now)`. The handler gives it `timeProvider.GetUtcNow()`, as `Create` and `Cancel` already do. | Small |
| 6 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 14 | ASYNC-1 | `bookings.ListOwnedByAsync(query.UserId).Result` blocks on a task. | Make `HandleAsync` `async` and `await` the call. | Small |
| 7 | sample/src/CourtBookings.Application/Bookings/IBookingRepository.cs | 14 | ASYNC-2 | `Task<IReadOnlyList<Booking>> ListOwnedByAsync(UserId ownerId);` is an I/O method without a `CancellationToken cancellationToken` parameter. As a result, the handler cannot pass on the token it receives. | Add `CancellationToken cancellationToken` as the last parameter. Pass it from the handler and in every implementation. | Small |
| 8 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 20 | ASYNC-2 | `ListOwnedByAsync(UserId ownerId)` has no token, and `.ToListAsync()` on line 23 is called without one. | Accept `cancellationToken` and pass it to `ToListAsync(cancellationToken)`. | Small |
| 9 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 26 | SA-1 | `var email = query.RequestedByEmail!;` uses the null-forgiving operator. The value comes from `FindFirstValue(ClaimTypes.Email)`, which can be null. | Remove the email from this path (see #4), or check it for null explicitly. | Small |
| 10 | sample/tests/CourtBookings.UnitTests/Application/InMemoryBookingRepository.cs | 27 | SOLID-3 | `public Task<IReadOnlyList<Booking>> ListOwnedByAsync(UserId ownerId) => throw new NotImplementedException();` throws for an interface member. | Implement it on `Saved`: `Task.FromResult<IReadOnlyList<Booking>>([.. Saved.Where(b => b.IsOwnedBy(ownerId))])`. | Small |
| 11 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | SEP-1 | `CanCancelForFree: b.Start - now >= TimeSpan.FromHours(24)` computes a business rule (free cancellation) in the endpoint. | Compute it in the domain or application layer (for example a `Booking` method that uses `FreeCancellationNotice`), return it in `UpcomingBooking`, and have the endpoint only map it. | Medium |
| 12 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | SOLID-1 | The same line puts the free-cancellation business rule inside the HTTP endpoint class. | Same fix as #11. | Medium |
| 13 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | CC-3 | `b.Start - now >= TimeSpan.FromHours(24)` repeats the rule already in `Booking.Cancel`: `Slot.Start - now < FreeCancellationNotice` (Booking.cs:82). | Keep one domain method and call it from both places. | Small |
| 14 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | CC-4 | `TimeSpan.FromHours(24)` is a magic number. `Booking.FreeCancellationNotice` already exists. | Use the named constant, or better, the domain method from #11. | Small |
| 15 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 13 | CC-4 | `query.Days ?? 7` uses the magic number `7` for the default window. | Use a named constant, for example `Booking.MaxBookingAhead.Days` or a `DefaultWindowDays` const. | Small |
| 16 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 25 | CC-6 | Lines 25-26 are commented-out code: `// public async Task<IReadOnlyList<Booking>> ListOwnedByAsync(...) => ...`. | Delete it. The LINQ version should be the real implementation (see #1). | Small |
| 17 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 16 | LOG-1 | `Console.WriteLine("Listing upcoming bookings for " + query.UserId);` writes a log line outside `ILogger<T>`, built by concatenation. | Remove it, or add a `[LoggerMessage]` method on the injected logger. | Small |
| 18 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 11 | TEST-1 | The new behaviour has no test that can fail. This covers the window filter, the ordering, `IsUpcoming`, `days` validation returning BadRequest, and the ownership check. The handler cannot be unit-tested because the fake throws (#10). The only test (ListUpcomingBookingsEndpointTests.cs:31-34) passes on an empty list, and #5 makes the list empty. | Add handler unit tests (with `FakeTimeProvider`), `Booking.IsUpcoming` domain tests, and integration tests for the expected list, out-of-range `days`, and another user's ID. | Medium |
| 19 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 23 | TEST-2 | `public async Task Test1()` does not follow `Method_Scenario_ExpectedResult`. | Rename, for example `ListUpcomingBookings_TwoFutureBookings_ReturnsBothCancellableForFree`. | Small |
| 20 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 31 | TEST-2 | `foreach (var booking in bookings) { Assert.True(booking.CanCancelForFree); }` puts a loop in the test, and the test passes when the list is empty. | Assert the expected count and contents directly, for example `Assert.Collection(...)` or `Assert.All` after `Assert.Equal(2, bookings.Count)`. | Small |
| 21 | sample/src/CourtBookings.Api/appsettings.json | 11 | WORK-2 | The `"Email": { SmtpHost, SmtpUser, SmtpPassword }` section is not used by any code in the change or in `src`. It is outside the task and it touches secrets. | Remove the section from this change. | Small |

## Rules

| Rule | Status | Violations |
|------|--------|------------|
| SOLID-1 | Violated | 1 |
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
| DOM-1 | Not applicable | 0 |
| DOM-2 | Not applicable | 0 |
| DOM-3 | Complies | 0 |
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
| WORK-2 | Violated | 1 |
| WORK-3 | Not applicable | 0 |
| WORK-4 | Not applicable | 0 |
| WORK-5 | Not reviewable | 0 |
| WORK-6 | Not reviewable | 0 |
| WORK-7 | Measured by commands | 0 |
| WORK-8 | Not applicable | 0 |
| WORK-9 | Not reviewable | 0 |
| WORK-10 | Not reviewable | 0 |

Totals: 21 violations in 18 rules; 22 rules comply; 13 not applicable; 5 not reviewable; 4 measured by commands.

## Doubts

- sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs:16: the `Console.WriteLine` message is built by concatenation, which LOG-2 also forbids. I counted it only under LOG-1 because it does not go through a logger template. LOG-2 is marked "Complies" because the `[LoggerMessage]` templates (lines 33, 36) use PascalCase placeholders.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:73: findings #11 and #12 are the same line seen through SEP-1 and SOLID-1. Both rules state it literally, so both are counted. A reader who sees this as double counting can subtract one.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:54: the handler takes 6 parameters, which CC-2 says needs a reason. These are Minimal API binding parameters, as in the existing endpoints, so I did not count it.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:67: `UserId.Create(userId)` checks blank and over-long route values only by throwing `ArgumentException`, which probably becomes a 500 rather than a 400. SEC-3 asks for rejection at the boundary. I did not count it because the value is rejected, and the real fix is #2 (do not take the user ID from the route).
- sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs:28: the test uses `FakeTimeProvider`, but its result depends on the real clock through `Booking.IsUpcoming` (#5). TEST-3 is marked "Complies" because the test code itself uses no real clock, and the cause is counted under DI-2.
- sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs:22: `SELECT * FROM Bookings` may not map cleanly onto the owned `Slot` columns with SQLite. This was not checked by running it, so it is not counted beyond SEC-2.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:58: the endpoint depends on the concrete `ListUpcomingBookingsHandler` and not an interface. This follows the existing handler pattern, so I did not count it under SOLID-5.