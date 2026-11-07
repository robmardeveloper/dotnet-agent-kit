# Review against AGENTS.md

Base commit: 0a69b98. Files reviewed: 12.

## Findings

| # | File | Line | Rule | What breaks it | Proposal | Effort |
|---|------|------|------|----------------|----------|--------|
| 1 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 22 | SEC-2 | `.FromSqlRaw("SELECT * FROM Bookings WHERE OwnerId = '" + ownerId.Value + "'")` adds the `userId` route value to the SQL text. `UserId.Create` only checks for empty text and length, so a value such as `x' OR '1'='1` gets into the query unchanged (SQL injection). | Use LINQ: `dbContext.Bookings.Where(b => b.OwnerId == ownerId).ToListAsync(cancellationToken)`. | Small |
| 2 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 67 | SEC-4 | `UserId.Create(userId)` takes the owner from the route `/users/{userId}/bookings`. Nothing checks that it matches the logged-in user (`user.GetUserId()`), so any authenticated user can list anyone's bookings. | Use `user.GetUserId()` (for example route `/bookings/upcoming`), or reject with 403/404 when the route user is not the caller. | Small |
| 3 | sample/src/CourtBookings.Api/appsettings.json | 14 | SEC-1 | `"SmtpPassword": "Pista-2026-Smtp!"` puts a credential in a committed configuration file. | Remove it from appsettings. Use User Secrets in development and environment variables or a secret store in production. Rotate the password, because it is now in the working tree. | Small |
| 4 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 28 | SEC-6 | `LogListRequestedBy(email)` writes the user's email address (personal data) to the logs. | Log the user ID only, or remove the message. | Small |
| 5 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 14 | ASYNC-1 | `bookings.ListOwnedByAsync(query.UserId).Result` blocks on a task. | Make `HandleAsync` `async` and `await` the call. | Small |
| 6 | sample/src/CourtBookings.Application/Bookings/IBookingRepository.cs | 14 | ASYNC-2 | `Task<IReadOnlyList<Booking>> ListOwnedByAsync(UserId ownerId);` is an I/O method with no `CancellationToken` parameter. As a result, the handler cannot pass its token on. | Add `CancellationToken cancellationToken` as the last parameter, in the interface and in every implementation. | Small |
| 7 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 20 | ASYNC-2 | `ListOwnedByAsync(UserId ownerId)` takes no token and calls `.ToListAsync()` without one (line 23). | Take `cancellationToken` and pass it to `ToListAsync`. | Small |
| 8 | sample/src/CourtBookings.Domain/Bookings/Booking.cs | 63 | DI-2 | `IsUpcoming() => ... Slot.Start > DateTimeOffset.UtcNow` reads the real clock in Domain. This also contradicts the class comment "the domain never reads the clock". | Change it to `IsUpcoming(DateTimeOffset now)` and pass `timeProvider.GetUtcNow()` from the handler. | Small |
| 9 | sample/tests/CourtBookings.UnitTests/Application/InMemoryBookingRepository.cs | 27 | SOLID-3 | `ListOwnedByAsync(...) => throw new NotImplementedException();` on an interface member. | Implement it over the `Saved` list (filter with `IsOwnedBy`). | Small |
| 10 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | SEP-1 | `CanCancelForFree: b.Start - now >= TimeSpan.FromHours(24)` is a business rule (the free-cancellation window) computed in the endpoint. | Compute it in the domain (for example `Booking.CanCancelForFree(now)`) and carry it in `UpcomingBooking`. | Medium |
| 11 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | CC-3 | The 24-hour free-cancellation rule is written again here. It already exists in `Booking.FreeCancellationNotice` / `Booking.Cancel` (Booking.cs:9, 82). | Use one domain method for both. | Small |
| 12 | sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | CC-4 | `TimeSpan.FromHours(24)` is a magic number. | Use `Booking.FreeCancellationNotice` (or the domain method from #10). | Small |
| 13 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 13 | CC-4 | `query.Days ?? 7` uses a magic number for the default window. | Use a named constant (or `Booking.MaxBookingAhead.Days`). | Small |
| 14 | sample/src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 25 | CC-6 | Lines 25–26 are a commented-out alternative implementation of `ListOwnedByAsync`. | Delete it (and use it as the real implementation, see #1). | Small |
| 15 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 26 | SA-1 | `var email = query.RequestedByEmail!;` uses the null-forgiving operator on a `string?`. | Remove it together with the email log (#4), or handle null explicitly. | Small |
| 16 | sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 16 | LOG-1 | `Console.WriteLine("Listing upcoming bookings for " + query.UserId);` logs through the console instead of the injected `ILogger<T>`. | Remove it, or add a `[LoggerMessage]` method that uses `logger`. | Small |
| 17 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 31 | TEST-1 | The only test asserts inside a `foreach` over the result, so it passes even when the list is empty. Nothing checks which bookings are listed, their order, the `days` window or the 400 for an invalid `days`. In practice the new behaviour has no test. | Assert the expected count, IDs and order, and add cases for `days` and for another user's ID. | Medium |
| 18 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 23 | TEST-2 | The test is named `Test1` instead of `Method_Scenario_ExpectedResult`. | Rename it, for example `ListUpcomingBookings_TwoFutureBookings_ReturnsBothSoonestFirst`. | Small |
| 19 | sample/tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 31 | TEST-2 | `foreach (var booking in bookings) { Assert.True(...) }` puts logic (a loop) in a test. | Use `Assert.All` or `Assert.Collection`, or compare against an expected list. | Small |
| 20 | sample/src/CourtBookings.Api/appsettings.json | 11 | WORK-2 | An `Email` section with credentials was added, but no code in the change uses it. It is outside the task and touches secrets. | Remove the section. | Small |

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
| DOM-3 | Not applicable | 0 |
| DOM-4 | Not applicable | 0 |
| DOM-5 | Complies | 0 |
| SEP-1 | Violated | 1 |
| SEP-2 | Complies | 0 |
| SEP-3 | Not applicable | 0 |
| TEST-1 | Violated | 1 |
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

- sample/src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs:16: the `Console.WriteLine` message is also built by string concatenation, which LOG-2 and LOG-3 forbid for log messages. It is counted once, under LOG-1, so the same line is not counted three times.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:73: computing `CanCancelForFree` outside the entity could also count against DOM-1 (rules inside entities). It is not counted because DOM-1 is mainly about services setting entity state, which does not happen here. It is already reported under SEP-1 and CC-3.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:67: an invalid `userId` (over 200 characters) makes `UserId.Create` throw `ArgumentException`. The only registered exception handler is `BookingRuleExceptionHandler`, so this probably becomes a 500 instead of a 400. The input is still rejected, so SEC-3 is not counted as violated.
- sample/src/CourtBookings.Api/Bookings/BookingEndpoints.cs:54: the endpoint has 6 parameters, and CC-2 says more than 3 needs a reason. Most of them are framework-bound (services, `ClaimsPrincipal`, token), so it is not counted.
- WORK-9: every change since 0a69b98 is uncommitted, so there are no commits to judge. A git command to confirm there are no commits after the base failed with a path error, so this comes only from `git status`, which shows every change as modified or untracked.