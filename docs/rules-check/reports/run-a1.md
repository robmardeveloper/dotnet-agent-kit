# Review against AGENTS.md

Base commit: 00bfbe04b9e97a49ae830df0da601f18e71c0c89. Files reviewed: 16. Every file in the change is untracked; nothing was committed after the base. The 16 files are `.gitignore`, `Library.slnx`, `README.md`, 8 files under `src/Library.Api` and 5 files under `tests/Library.Api.Tests`.

## Findings

| # | File | Line | Rule | What breaks it | Proposal | Effort |
|---|------|------|------|----------------|----------|--------|
| 1 | src/Library.Api/Auth/MemberHeaderAuthentication.cs | 22 | SEC-4 | `services.AddAuthorization();` sets no fallback policy. Only the `/loans` group is protected, through `.RequireAuthorization()` (LoanEndpoints.cs:11), so any endpoint added later is open by default. | Use `AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())` and mark public endpoints `[AllowAnonymous]`. | Small |
| 2 | src/Library.Api/Program.cs | 16 | SEC-5 | The pipeline built after `var app = builder.Build();` has no `UseExceptionHandler` and the services have no `AddProblemDetails`. The endpoints throw `InvalidOperationException` (LoanEndpoints.cs:41, 67) with no handler that turns it into a standard problem response. | Add `builder.Services.AddProblemDetails()` and `app.UseExceptionHandler()` outside Development. | Small |
| 3 | src/Library.Api/Library.Api.csproj | 1 | ARCH-1 | One `Microsoft.NET.Sdk.Web` project holds the domain (`Models`), the application logic (`Loans/LoanService`), the infrastructure (`Data`) and the API. There are no Domain, Application or Infrastructure projects. | Split into `Library.Domain`, `Library.Application`, `Library.Infrastructure` and `Library.Api`, with references pointing only inward. | Large |
| 4 | src/Library.Api/Loans/LoanService.cs | 24 | ARCH-2 | The application logic uses outer-layer names directly: `IDbContextFactory<LibraryDbContext>` and EF Core's `AnyAsync`, `CountAsync` and `SaveChangesAsync` (`using Microsoft.EntityFrameworkCore;` at line 3). | Declare an `ILoanRepository` in Application and implement it in Infrastructure. | Large |
| 5 | src/Library.Api/Loans/LoanService.cs | 24 | SOLID-1 | `LoanService` mixes business rules (limit of 3 at line 53, 14-day due date at line 65, book availability at line 46) with persistence (it creates the `DbContext` and calls `SaveChangesAsync`). | Move the rules into the domain and the persistence behind a repository, so the service only coordinates. | Large |
| 6 | src/Library.Api/Models/Models.cs | 23 | DOM-1 | `Loan` has public setters on every property, for example `public DateTimeOffset? ReturnedAt { get; set; }`. | Use private setters and domain methods such as `Loan.Borrow(...)` and `loan.Return(now)`. | Medium |
| 7 | src/Library.Api/Loans/LoanService.cs | 65 | DOM-1 | The service applies the due-date rule by setting the property itself: `DueDate = borrowedAt.AddDays(LoanPeriodDays)` in an object initializer. | Use a factory method `Loan.Borrow(bookId, memberId, now)` that computes `DueDate`. | Small |
| 8 | src/Library.Api/Loans/LoanService.cs | 111 | DOM-1 | The service sets entity state directly: `loan.ReturnedAt = DateTimeOffset.FromUnixTimeSeconds(...)`. | Call `loan.Return(now)`. | Small |
| 9 | src/Library.Api/Models/Models.cs | 10 | DOM-2 | `Loan` can be created in an invalid state: there is no constructor and nothing checks `BookId > 0`, `MemberId > 0` or `DueDate > BorrowedAt`. | Add a constructor or factory method that checks these invariants. | Medium |
| 10 | src/Library.Api/Models/Models.cs | 3 | DOM-2 | `Book` accepts any `Id` and any `Title`, including an empty one. Only the database limits the length. | Add a constructor that rejects an invalid id or title. | Small |
| 11 | src/Library.Api/Models/Models.cs | 20 | DOM-3 | The loan period is two raw values, `BorrowedAt` and `DueDate` (lines 18 and 20), instead of a date-range value object. | Add a `LoanPeriod` record that checks its own start and end. | Medium |
| 12 | src/Library.Api/Loans/LoanService.cs | 46 | DOM-5 | The borrow rules sit in the application service: the "already on loan" check at line 46 and the "limit reached" check at lines 51–56. The service is not thin. | Move the rules into the domain (an entity or a domain service) and have the service load, call, save and return. | Medium |
| 13 | src/Library.Api/Loans/LoanService.cs | 106 | DOM-5 | The "already returned" rule is in the service: `if (loan.ReturnedAt is not null) return LoanError.AlreadyReturned;`. | Let `loan.Return(...)` reject a second return. | Small |
| 14 | src/Library.Api/Loans/LoanEndpoints.cs | 23 | SOLID-5 | The endpoint depends on the concrete, volatile `LoanService loans` (in `BorrowAsync`). | Depend on an application interface such as `ILoanService`. | Small |
| 15 | src/Library.Api/Loans/LoanEndpoints.cs | 47 | SOLID-5 | Same as #14, in `ListActiveAsync`: `LoanService loans`. | Same as #14. | Small |
| 16 | src/Library.Api/Loans/LoanEndpoints.cs | 57 | SOLID-5 | Same as #14, in `ReturnAsync`: `LoanService loans`. | Same as #14. | Small |
| 17 | src/Library.Api/Loans/LoanService.cs | 24 | SOLID-5 | The service depends on the concrete `LibraryDbContext` through `IDbContextFactory<LibraryDbContext>`. | Depend on a repository abstraction. | Medium |
| 18 | src/Library.Api/Loans/LoanEndpoints.cs | 29 | SEP-3 | A user-facing text is hard-coded: `"Body must be a JSON object with bookId as a positive integer."`. | Move it to a `.resx` file and read it through `IStringLocalizer<T>`. | Medium |
| 19 | src/Library.Api/Loans/LoanEndpoints.cs | 37 | SEP-3 | Hard-coded `"Book does not exist."`. | Same as #18. | Small |
| 20 | src/Library.Api/Loans/LoanEndpoints.cs | 38 | SEP-3 | Hard-coded `"Book is already on loan."`. | Same as #18. | Small |
| 21 | src/Library.Api/Loans/LoanEndpoints.cs | 40 | SEP-3 | Hard-coded `$"Member already has {LoanService.MaxActiveLoansPerMember} active loans."`. | Same as #18, using a format resource. | Small |
| 22 | src/Library.Api/Loans/LoanEndpoints.cs | 65 | SEP-3 | Hard-coded `"Loan does not exist."`. | Same as #18. | Small |
| 23 | src/Library.Api/Loans/LoanEndpoints.cs | 66 | SEP-3 | Hard-coded `"Loan was already returned."`. | Same as #18. | Small |
| 24 | src/Library.Api/Loans/LoanEndpoints.cs | 36 | SA-1 | `outcome.Loan!.Id` uses the null-forgiving operator to silence the nullable warning. | Pattern-match on the outcome (for example `{ Loan: { } loan }`) or reshape `LoanOutcome` so `Loan` is not nullable on success. | Small |
| 25 | tests/Library.Api.Tests/LoanTests.cs | 29 | SA-1 | `root.GetProperty("dueDate").GetString()!`. | Assert not-null first, or use `Assert.NotNull` and keep the result. | Small |
| 26 | tests/Library.Api.Tests/LoanTests.cs | 172 | SA-1 | `only.GetProperty("dueDate").GetString()!`. | Same as #25. | Small |
| 27 | tests/Library.Api.Tests/LoanTests.cs | 233 | SA-1 | `Assert.Single(stillActive!);`. | `Assert.NotNull(stillActive);` then `Assert.Single(stillActive);`. | Small |
| 28 | src/Library.Api/Data/LibraryDatabase.cs | 13 | ASYNC-2 | `InitializeLibraryDatabaseAsync(this IServiceProvider services)` does I/O (`EnsureCreatedAsync`, `ExecuteSqlRawAsync`, `SaveChangesAsync`) but takes no `CancellationToken`. | Add a `CancellationToken cancellationToken` parameter and pass it to every call. | Small |
| 29 | tests/Library.Api.Tests/LibraryApiFactory.cs | 19 | TEST-5 | The integration tests use a SQLite file on disk (`Path.Combine(Path.GetTempPath(), $"library-tests-{Guid.NewGuid():N}.db")`), not SQLite in memory. | Use `Data Source=:memory:` (or a shared in-memory name) with a connection kept open for the factory's lifetime. | Medium |
| 30 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 19 | ARCH-4 | The test project has no architecture-test package (ArchUnitNET or NetArchTest), and no test checks ARCH-1 or ARCH-2. | Add architecture tests once the layers exist. | Medium |
| 31 | src/Library.Api/Loans/LoanService.cs | 34 | CC-2 | `BorrowAsync` runs from line 34 to line 77 (about 44 lines) and mixes locking, queries, rule checks and entity building. | Extract the checks and the loan creation (most of this goes away with DOM-1 and DOM-5). | Medium |
| 32 | src/Library.Api/Loans/LoanEndpoints.cs | 74 | CC-2 | `ReadBookIdAsync` runs from line 74 to line 113 (about 40 lines). | Bind to a request record and validate it, or split parsing from validation. | Small |
| 33 | src/Library.Api/Loans/LoanService.cs | 111 | CC-3 | `DateTimeOffset.FromUnixTimeSeconds(timeProvider.GetUtcNow().ToUnixTimeSeconds())` repeats the calculation already at line 59. | Extract it to one private method, for example `NowToTheSecond()`. | Small |
| 34 | src/Library.Api/Loans/LoanService.cs | 85 | CC-3 | The "active loan of member" predicate `l.MemberId == memberId && l.ReturnedAt == null` repeats line 52. | Put the predicate in one place (an expression or a specification). | Small |
| 35 | src/Library.Api/Program.cs | 9 | CC-4 | A magic default connection string is hard-coded: `?? "Data Source=library.db"`. | Put the default in `appsettings.json` under `ConnectionStrings:Library`. | Small |
| 36 | src/Library.Api/Data/LibraryDbContext.cs | 17 | CC-4 | Magic number in `HasMaxLength(200)`. | Use a named constant such as `Book.MaxTitleLength`. | Small |
| 37 | src/Library.Api/Loans/LoanEndpoints.cs | 95 | CC-4 | Magic string `"bookId"` in the property comparison. | Use a named constant, or bind to a request record. | Small |
| 38 | tests/Library.Api.Tests/LibraryApiFactory.cs | 33 | CC-4 | `"X-Member-Id"` is written as a literal although `MemberHeaderAuthentication.HeaderName` already exists. | Use `MemberHeaderAuthentication.HeaderName`. | Small |
| 39 | src/Library.Api/Loans/LoanEndpoints.cs | 101 | CC-5 | The `if (property.Value.ValueKind == JsonValueKind.Number && ...)` sits three levels deep: `using (document)`, then `foreach`, then `if`. | Use `TryGetProperty` with guard clauses, or bind to a record. | Small |
| 40 | tests/Library.Api.Tests/LoanTests.cs | 112 | TEST-2 | `Fourth_active_loan_returns_409_and_returning_one_frees_a_slot` has a `for` loop (line 112) and several Acts (lines 119, 121, 123). | Split it into separate tests and move setup into Arrange helpers. | Small |
| 41 | tests/Library.Api.Tests/AuthenticationTests.cs | 14 | TEST-2 | `Request_without_member_header_returns_401` has three Acts (lines 14, 15, 16). | Use a `[Theory]` over method and path. | Small |
| 42 | tests/Library.Api.Tests/StartupTests.cs | 36 | TEST-2 | `Books_1_to_20_can_be_borrowed_and_book_21_does_not_exist` has two Acts (lines 36 and 38). | Split into two tests, or one `[Theory]`. | Small |
| 43 | src/Library.Api/Library.Api.csproj | 10 | WORK-3 | `<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.14" />` sets its version in the project file, and there is no `Directory.Packages.props`. | Add `Directory.Packages.props` with `ManagePackageVersionsCentrally` set to `true` and remove `Version` from the project files. | Small |
| 44 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 20 | WORK-3 | `coverlet.collector` with `Version="10.1.0"`. | Same as #43. | Small |
| 45 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 24 | WORK-3 | `Microsoft.AspNetCore.Mvc.Testing` with `Version="10.0.14"`. | Same as #43. | Small |
| 46 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 25 | WORK-3 | `Microsoft.NET.Test.Sdk` with `Version="18.11.0"`. | Same as #43. | Small |
| 47 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 26 | WORK-3 | `xunit` with `Version="2.9.3"`. | Same as #43. | Small |
| 48 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 27 | WORK-3 | `xunit.runner.visualstudio` with `Version="4.0.1"`. | Same as #43. | Small |

## Rules

| Rule | Status | Violations |
|------|--------|------------|
| SOLID-1 | Violated | 1 |
| SOLID-2 | Not applicable | 0 |
| SOLID-3 | Complies | 0 |
| SOLID-4 | Not applicable | 0 |
| SOLID-5 | Violated | 4 |
| CC-1 | Complies | 0 |
| CC-2 | Violated | 2 |
| CC-3 | Violated | 2 |
| CC-4 | Violated | 4 |
| CC-5 | Violated | 1 |
| CC-6 | Complies | 0 |
| ARCH-1 | Violated | 1 |
| ARCH-2 | Violated | 1 |
| ARCH-3 | Complies | 0 |
| ARCH-4 | Violated | 1 |
| DI-1 | Complies | 0 |
| DI-2 | Complies | 0 |
| DI-3 | Not applicable | 0 |
| DI-4 | Complies | 0 |
| DI-5 | Complies | 0 |
| DI-6 | Complies | 0 |
| DOM-1 | Violated | 3 |
| DOM-2 | Violated | 2 |
| DOM-3 | Violated | 1 |
| DOM-4 | Complies | 0 |
| DOM-5 | Violated | 2 |
| SEP-1 | Complies | 0 |
| SEP-2 | Complies | 0 |
| SEP-3 | Violated | 6 |
| TEST-1 | Complies | 0 |
| TEST-2 | Violated | 3 |
| TEST-3 | Not applicable | 0 |
| TEST-4 | Complies | 0 |
| TEST-5 | Violated | 1 |
| SA-1 | Violated | 4 |
| SA-2 | Measured by commands | 0 |
| SA-3 | Measured by commands | 0 |
| SA-4 | Not applicable | 0 |
| SEC-1 | Complies | 0 |
| SEC-2 | Complies | 0 |
| SEC-3 | Complies | 0 |
| SEC-4 | Violated | 1 |
| SEC-5 | Violated | 1 |
| SEC-6 | Not applicable | 0 |
| SEC-7 | Measured by commands | 0 |
| LOG-1 | Not applicable | 0 |
| LOG-2 | Not applicable | 0 |
| LOG-3 | Not applicable | 0 |
| LOG-4 | Not applicable | 0 |
| ASYNC-1 | Complies | 0 |
| ASYNC-2 | Violated | 1 |
| ASYNC-3 | Complies | 0 |
| WORK-1 | Not reviewable | 0 |
| WORK-2 | Not reviewable | 0 |
| WORK-3 | Violated | 6 |
| WORK-4 | Not applicable | 0 |
| WORK-5 | Not reviewable | 0 |
| WORK-6 | Not reviewable | 0 |
| WORK-7 | Measured by commands | 0 |
| WORK-8 | Not applicable | 0 |
| WORK-9 | Not reviewable | 0 |
| WORK-10 | Not reviewable | 0 |

Totals: 48 violations in 21 rules; 19 rules comply; 12 not applicable; 6 not reviewable; 4 measured by commands.

## Doubts

- src/Library.Api/Loans/LoanEndpoints.cs:34 and :62: the new `switch` on `LoanError` maps outcomes to HTTP results. SOLID-2 is about adding a branch to an existing switch, and these switches are new, so they are not counted.
- src/Library.Api/Data/LibraryDatabase.cs:15: `services.GetRequiredService<IDbContextFactory<LibraryDbContext>>()` runs in a startup extension that only Program.cs calls (Program.cs:18). It is treated as composition-root code, so it is not counted under DI-4.
- tests/Library.Api.Tests/LoanTests.cs:19 and :21: `DateTimeOffset.UtcNow` reads the real clock, and the assertion depends on a ±1 s tolerance. TEST-3 covers unit tests, and these are integration tests, so it is not counted. A `FakeTimeProvider` replaced through `ConfigureTestServices` would make the test repeatable.
- Test names: several do not follow `Method_Scenario_ExpectedResult`. Examples are `Concurrent_borrows_of_one_book_let_exactly_one_through` (LoanTests.cs:127), `Missing_header_is_rejected_before_body_validation` (AuthenticationTests.cs:46) and `Restarting_on_an_existing_database_keeps_its_loans` (StartupTests.cs:43). Whether a domain verb counts as the "Method" of an HTTP-level test is a matter of judgement, so these are not counted under TEST-2.
- src/Library.Api/Loans/LoanService.cs:91: `ReturnAsync` changes state and returns a `LoanError` status. That reads as a command result rather than query data, so it is not counted under SEP-2.
- No `.editorconfig` raises CA2254 (LOG-2) or CA2016 (ASYNC-2), and there is no `Directory.Build.props` (SA-2). No changed line can be cited for these gaps. The SA-2 part is measured by commands; the rest is not counted.
- src/Library.Api/Data/LibraryDatabase.cs:21: `ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;")` is a constant with no input, so it is not counted under SEC-2.
- src/Library.Api/Loans/LoanService.cs:29-32: the in-process `SemaphoreSlim` gate is documented as correct only for a single API process. No rule covers this, so it is not counted.