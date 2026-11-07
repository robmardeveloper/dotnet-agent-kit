# Answer key for the reviewer check (written before the reviews)

The seeded change adds `GET /users/{userId}/bookings` ("list my upcoming bookings") on top of commit `0a69b98`.
Paths are relative to `sample/`. A seed is **found** when a review reports it as a violation (in the Findings
table, not under Doubts) with the expected rule or an accepted alternative, at a line within 3 of the one given.
A seed reported several times counts once.

## Seeds (14)

| # | Expected rule | Also accepted | File | Line(s) | What is wrong |
|---|---|---|---|---|---|
| 1 | SEC-4 | SEC-3 | src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 23, 67 | The user ID comes from the route, not from the authenticated user: anyone can list anyone's bookings |
| 2 | SEP-1 | CC-3, CC-4, DOM-1 | src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 73 | The endpoint decides "can cancel for free" with its own 24 hours: a business rule in the endpoint |
| 3 | DI-2 | DI-1 | src/CourtBookings.Domain/Bookings/Booking.cs | 63 | `DateTimeOffset.UtcNow` read in the domain |
| 4 | CC-4 | CC-3 | src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 13 | Bare `7` as the default number of days |
| 5 | ASYNC-1 | | src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 14 | `.Result` blocks on the task |
| 6 | ASYNC-2 | | src/CourtBookings.Application/Bookings/IBookingRepository.cs | 14 (or its implementations: EfBookingRepository.cs 20, InMemoryBookingRepository.cs 27) | New I/O method without `CancellationToken` |
| 7 | SEC-2 | | src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 22 | SQL built by concatenating the user ID, passed to `FromSqlRaw` |
| 8 | LOG-1 | LOG-2 | src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 16 | `Console.WriteLine` instead of the injected logger |
| 9 | SEC-6 | | src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 28, 36 | The user's email address is written to the log |
| 10 | SEC-1 | | src/CourtBookings.Api/appsettings.json | 14 | An SMTP password in `appsettings.json` (made up) |
| 11 | SA-1 | | src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 26 | `!` silences a possible null |
| 12 | CC-6 | | src/CourtBookings.Infrastructure/Persistence/EfBookingRepository.cs | 25-26 | Commented-out code left behind |
| 13 | SOLID-3 | | tests/CourtBookings.UnitTests/Application/InMemoryBookingRepository.cs | 27 | The fake throws `NotImplementedException` for an interface member |
| 14 | TEST-2 | | tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 23, 31 | Test named `Test1`, with a `foreach` in it |

## Decoys (5): correct code that looks suspicious

Reporting one of these as a violation, with the rule given, is a **false alarm**.

| # | Rule it seems to break | File | Line(s) | Why it is correct |
|---|---|---|---|---|
| S1 | SEC-6 | src/CourtBookings.Application/Bookings/ListUpcomingBookingsHandler.cs | 27, 33 | It logs the user ID, an identifier, which SEC-6 allows |
| S2 | DI-2 | ListUpcomingBookingsHandler.cs 13; BookingEndpoints.cs 70 | | The time comes from the injected `TimeProvider` |
| S3 | SEP-1 | src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 62-65 | Checking the range of an input at the boundary is what SEC-3 asks; it returns a plain 400 with no text |
| S4 | CC-2 | src/CourtBookings.Api/Bookings/BookingEndpoints.cs | 54-60 | Six parameters, all bound by the framework (route, query, user, two services, token) |
| S5 | CC-4 | tests/CourtBookings.IntegrationTests/ListUpcomingBookingsEndpointTests.cs | 10 | `60` is given a name, `OneHour` |

## Other findings

A violation that is neither a seed nor a decoy is checked by hand against the rule text: if it is real, it is listed
as "real, not seeded" and is not a false alarm; if it is not, it is a false alarm.

## Pass criterion

The reviewer passes if, in **each** of the 3 reviews, it finds at least **12 of the 14** seeds and reports at most
**1** false alarm.

## Known before the reviews

- Seed 7 is also caught by the compiler: EF Core's analyzer reports `EF1003`, which fails the build with warnings as
  errors. The reviewer does not build (WORK-7 is run apart), so it has to find it by reading.
