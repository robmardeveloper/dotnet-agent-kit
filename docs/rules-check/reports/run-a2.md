# Review against AGENTS.md

Base commit: 00bfbe04b9e97a49ae830df0da601f18e71c0c89. Files reviewed: 18. Nothing changed in tracked files; all 18 files are new and untracked.

## Findings

| # | File | Line | Rule | What breaks it | Proposal | Effort |
|---|------|------|------|----------------|----------|--------|
| 1 | src/Library.Api/Program.cs | 20 | SEC-4 | `builder.Services.AddAuthorization();` sets no fallback policy. Protection depends on each group remembering `.RequireAuthorization()` (LoanEndpoints.cs:12), so a new endpoint is open by default. | `AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())`, and mark only public endpoints `[AllowAnonymous]` | Small |
| 2 | src/Library.Api/Program.cs | 22 | SEC-5 | The pipeline after `var app = builder.Build();` has no `UseExceptionHandler`, and there is no `AddProblemDetails()` in services. | Add `builder.Services.AddProblemDetails()` and `app.UseExceptionHandler()` outside Development | Small |
| 3 | Library.slnx | 3 | ARCH-1 | One project, `src/Library.Api/Library.Api.csproj`, holds endpoints, the loan rules, EF entities and the DbContext. There are no `Domain`, `Application` or `Infrastructure` projects. | Split into Domain / Application / Infrastructure / Api projects with references pointing inward | Large |
| 4 | src/Library.Api/Loans/LoanService.cs | 20 | ARCH-2 | `LoanService(IDbContextFactory<LibraryDbContext> dbFactory)`: the application logic names EF Core and the concrete `LibraryDbContext` directly. It does not use an interface it declares itself. | Declare `ILoanRepository` in Application and implement it in Infrastructure | Large |
| 5 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 10 | ARCH-4 | The test project's packages (lines 10-22) include no ArchUnitNET or NetArchTest, and no test checks the layer references. | Add architecture tests that run in `dotnet test` | Medium |
| 6 | src/Library.Api/Loans/LoanEndpoints.cs | 24 | SOLID-5 | `LoanService loans`: the endpoint depends on a volatile concrete class. | Inject an interface (for example `ILoanService`, or one handler interface per use case) | Medium |
| 7 | src/Library.Api/Loans/LoanEndpoints.cs | 57 | SOLID-5 | `ListActiveAsync(..., LoanService loans, ...)` depends on the concrete class. | Same as #6 | Small |
| 8 | src/Library.Api/Loans/LoanEndpoints.cs | 60 | SOLID-5 | `ReturnAsync(..., LoanService loans, ...)` depends on the concrete class. | Same as #6 | Small |
| 9 | src/Library.Api/Loans/LoanService.cs | 20 | SOLID-1 | `LoanService` mixes the business rules (loan limit at line 47, 14-day due date at line 58) with persistence (DbContext queries, `SaveChangesAsync`, `ExecuteUpdateAsync`, catching `DbUpdateException`). | Move the rules into the domain and the persistence behind a repository | Large |
| 10 | src/Library.Api/Data/LibraryDatabase.cs | 49 | DI-1 | `await using var db = new LibraryDbContext(options);`: the class creates the database dependency with `new`. | Receive `IDbContextFactory<LibraryDbContext>` (or a seeder service) through DI and resolve it in the composition root | Medium |
| 11 | src/Library.Api/Loans/LoanService.cs | 52 | DI-2 | `var now = DateTimeOffset.UtcNow;` reads the real clock in application logic. | Inject `TimeProvider` and use `GetUtcNow()` | Small |
| 12 | src/Library.Api/Loans/LoanService.cs | 95 | DI-2 | `var now = DateTimeOffset.UtcNow;` reads the real clock when a loan is returned. | Same as #11 | Small |
| 13 | src/Library.Api/Data/Entities.cs | 11 | DOM-1 | Every `Loan` property (lines 11-18) has a public `set;`, and there are no domain methods such as `Return()`. | Use private setters plus `Loan.Borrow(...)` and `loan.Return(now)` | Medium |
| 14 | src/Library.Api/Loans/LoanService.cs | 53 | DOM-1 | The service builds a `Loan` by setting its properties and applies the due-date rule itself: `DueDate = now.AddDays(LoanPeriodDays)`. | Use a factory `Loan.Borrow(bookId, memberId, now)` that computes `DueDate` | Medium |
| 15 | src/Library.Api/Loans/LoanService.cs | 98 | DOM-1 | `s.SetProperty(l => l.ReturnedAt, now)`: the service applies the "return" rule by setting the property directly. | Load the loan, call `loan.Return(now)` and save (keep atomicity with a concurrency token) | Medium |
| 16 | src/Library.Api/Data/Entities.cs | 9 | DOM-2 | `Loan` has no constructor or factory checks, so it can exist with `BookId = 0`, `MemberId = 0`, or `DueDate` before `BorrowedAt`. | Use a constructor or factory that validates IDs and dates | Medium |
| 17 | src/Library.Api/Loans/LoanService.cs | 29 | DOM-5 | `BorrowAsync` contains the borrowing rules (book exists, not already on loan, member's limit, due date). It is not "load, call domain method, save". | Move the limit and availability rules to the domain (entity or domain service) | Large |
| 18 | src/Library.Api/Loans/LoanService.cs | 90 | DOM-5 | `ReturnAsync` decides "already returned" against "not found" in the service (lines 100-108). | Let the entity reject a second return, and keep the service thin | Medium |
| 19 | src/Library.Api/Loans/LoanEndpoints.cs | 37 | SA-1 | `Results.Created($"/loans/{result.Loan!.Id}", result.Loan)` uses the null-forgiving `!`. | Make the result type guarantee `Loan` on success (separate success/failure types or pattern match `{ Loan: { } loan }`) | Small |
| 20 | src/Library.Api/Loans/LoanService.cs | 29 | CC-2 | `BorrowAsync` runs lines 29-78, about 50 lines. It mixes locking, queries, rule checks, entity construction and exception handling. | Extract the checks and the save into separate methods | Medium |
| 21 | src/Library.Api/Loans/LoanService.cs | 41 | CC-3 | The "active loan" rule `l.ReturnedAt == null` is repeated at lines 41, 46, 84 and 97. | Use one `IsActive` expression or specification, or a domain property | Small |
| 22 | src/Library.Api/Program.cs | 10 | CC-4 | `builder.Configuration["Library:DatabasePath"]` reads configuration with a magic key string, not `IOptions<T>`. | Bind a `LibraryOptions` class with a named section constant | Small |
| 23 | src/Library.Api/Loans/LoanEndpoints.cs | 37 | CC-4 | `$"/loans/{...}"` repeats the route literal `"/loans"` from line 12. | Use a named route constant or `Results.CreatedAtRoute` | Small |
| 24 | src/Library.Api/Loans/LoanEndpoints.cs | 30 | SEP-3 | `title: "bookId must be a positive integer."` is user-facing text written in the code. | Move it to a `.resx` file and read it through `IStringLocalizer<T>` | Small |
| 25 | src/Library.Api/Loans/LoanEndpoints.cs | 38 | SEP-3 | `title: "Book not found."` is hardcoded. | Same as #24 | Small |
| 26 | src/Library.Api/Loans/LoanEndpoints.cs | 39 | SEP-3 | `title: "Book is already on loan."` is hardcoded. | Same as #24 | Small |
| 27 | src/Library.Api/Loans/LoanEndpoints.cs | 42 | SEP-3 | `title: $"A member can have at most {...} active loans."` is hardcoded. | Same as #24 | Small |
| 28 | src/Library.Api/Loans/LoanEndpoints.cs | 67 | SEP-3 | `title: "Loan has already been returned."` is hardcoded. | Same as #24 | Small |
| 29 | src/Library.Api/Loans/LoanEndpoints.cs | 68 | SEP-3 | `title: "Loan not found."` is hardcoded. | Same as #24 | Small |
| 30 | src/Library.Api/Program.cs | 25 | LOG-3 | `app.Logger.LogInformation("Using database {DatabasePath}", databasePath);` calls `LogInformation` directly instead of a `[LoggerMessage]` partial method. | Declare a `[LoggerMessage]` method and call it | Small |
| 31 | src/Library.Api/Data/LibraryDatabase.cs | 41 | ASYNC-2 | `InitializeAsync(string databasePath)` does I/O (`EnsureCreatedAsync`, `SaveChangesAsync`) but takes no `CancellationToken`. | Add `CancellationToken cancellationToken` and pass it on | Small |
| 32 | src/Library.Api/Loans/LoanService.cs | 29 | ASYNC-2 | The parameter is named `ct`, but the rule requires `cancellationToken`. | Rename it to `cancellationToken` | Small |
| 33 | src/Library.Api/Loans/LoanService.cs | 80 | ASYNC-2 | `GetActiveLoansAsync(..., CancellationToken ct)` has the wrong parameter name. | Rename it | Small |
| 34 | src/Library.Api/Loans/LoanService.cs | 90 | ASYNC-2 | `ReturnAsync(..., CancellationToken ct)` has the wrong parameter name. | Rename it | Small |
| 35 | src/Library.Api/Loans/LoanEndpoints.cs | 25 | ASYNC-2 | Endpoint `BorrowAsync(..., CancellationToken ct)` has the wrong parameter name. | Rename it | Small |
| 36 | src/Library.Api/Loans/LoanEndpoints.cs | 57 | ASYNC-2 | Endpoint `ListActiveAsync(..., CancellationToken ct)` has the wrong parameter name. | Rename it | Small |
| 37 | src/Library.Api/Loans/LoanEndpoints.cs | 60 | ASYNC-2 | Endpoint `ReturnAsync(..., CancellationToken ct)` has the wrong parameter name. | Rename it | Small |
| 38 | tests/Library.Api.Tests/LibraryApiFactory.cs | 22 | TEST-5 | `builder.UseSetting("Library:DatabasePath", DatabasePath);` runs the integration tests on a SQLite file in a temp directory, not SQLite in memory. | Replace the DbContext registration in `ConfigureTestServices` with an open in-memory `SqliteConnection` | Medium |
| 39 | tests/Library.Api.Tests/DatabaseInitializationTests.cs | 9 | TEST-3 | The tests create and read real files under `Path.GetTempPath()` (lines 9, 13, 29). | Test the seeding against in-memory SQLite, or move these to integration tests | Medium |
| 40 | tests/Library.Api.Tests/LoanApiTests.cs | 45 | TEST-2 | `Requests_without_the_member_header_are_unauthorized` has three Acts (lines 45, 46, 47). | Make it a `[Theory]` over method and path, with one Act | Small |
| 41 | tests/Library.Api.Tests/LoanApiTests.cs | 144 | TEST-2 | `A_member_with_three_active_loans_gets_409_for_a_fourth` has a second Act and assertion (member 2 borrows). | Split it into its own test | Small |
| 42 | tests/Library.Api.Tests/LoanApiTests.cs | 155 | TEST-2 | `Returning_a_loan_frees_a_slot_and_the_book` has three Acts (lines 155, 157, 158). | Split it into separate tests | Small |
| 43 | tests/Library.Api.Tests/LoanApiTests.cs | 171 | TEST-2 | `GET_lists_only_the_callers_active_loans` has two Acts (lines 171, 172). | One Act per test | Small |
| 44 | tests/Library.Api.Tests/LoanApiTests.cs | 195 | TEST-2 | `Returning_a_loan_returns_204_and_removes_it_from_the_active_list` adds a second Act (GET) inside the Assert. | Split it, or move the GET to a separate test | Small |
| 45 | tests/Library.Api.Tests/LoanApiTests.cs | 219 | TEST-2 | `Returning_someone_elses_loan_is_404_and_leaves_it_active` adds a second Act (GET) inside the Assert. | Split it | Small |
| 46 | tests/Library.Api.Tests/LoanApiTests.cs | 253 | TEST-2 | `When_one_member_races_for_several_books_the_limit_still_holds` adds a second Act (GET) inside the Assert. | Split it | Small |
| 47 | tests/Library.Api.Tests/LoanApiTests.cs | 41 | TEST-2 | The name `Requests_without_the_member_header_are_unauthorized` does not follow `Method_Scenario_ExpectedResult`. | For example `GetLoans_WithoutMemberHeader_Returns401` | Small |
| 48 | tests/Library.Api.Tests/LoanApiTests.cs | 57 | TEST-2 | The name `Requests_with_an_empty_or_invalid_member_header_are_unauthorized` does not follow the naming pattern. | Rename it | Small |
| 49 | tests/Library.Api.Tests/LoanApiTests.cs | 68 | TEST-2 | The name `Borrowing_returns_201_with_an_id_and_a_due_date_14_days_away_with_an_offset` does not follow the naming pattern. | Rename it | Small |
| 50 | tests/Library.Api.Tests/LoanApiTests.cs | 102 | TEST-2 | The name `Borrowing_with_an_invalid_body_is_400` does not follow the naming pattern. | Rename it | Small |
| 51 | tests/Library.Api.Tests/LoanApiTests.cs | 113 | TEST-2 | The name `Borrowing_a_book_that_does_not_exist_is_404` does not follow the naming pattern. | Rename it | Small |
| 52 | tests/Library.Api.Tests/LoanApiTests.cs | 123 | TEST-2 | The name `Borrowing_a_book_already_on_loan_to_someone_else_is_409` does not follow the naming pattern. | Rename it | Small |
| 53 | tests/Library.Api.Tests/LoanApiTests.cs | 133 | TEST-2 | The name `A_member_with_three_active_loans_gets_409_for_a_fourth` does not follow the naming pattern. | Rename it | Small |
| 54 | tests/Library.Api.Tests/LoanApiTests.cs | 148 | TEST-2 | The name `Returning_a_loan_frees_a_slot_and_the_book` does not follow the naming pattern. | Rename it | Small |
| 55 | tests/Library.Api.Tests/LoanApiTests.cs | 162 | TEST-2 | The name `GET_lists_only_the_callers_active_loans` does not follow the naming pattern. | Rename it | Small |
| 56 | tests/Library.Api.Tests/LoanApiTests.cs | 179 | TEST-2 | The name `GET_returns_an_empty_list_when_the_member_has_no_loans` does not follow the naming pattern. | Rename it | Small |
| 57 | tests/Library.Api.Tests/LoanApiTests.cs | 187 | TEST-2 | The name `Returning_a_loan_returns_204_and_removes_it_from_the_active_list` does not follow the naming pattern. | Rename it | Small |
| 58 | tests/Library.Api.Tests/LoanApiTests.cs | 199 | TEST-2 | The name `Returning_a_loan_twice_is_409` does not follow the naming pattern. | Rename it | Small |
| 59 | tests/Library.Api.Tests/LoanApiTests.cs | 211 | TEST-2 | The name `Returning_someone_elses_loan_is_404_and_leaves_it_active` does not follow the naming pattern. | Rename it | Small |
| 60 | tests/Library.Api.Tests/LoanApiTests.cs | 225 | TEST-2 | The name `Returning_a_loan_that_does_not_exist_is_404` does not follow the naming pattern. | Rename it | Small |
| 61 | tests/Library.Api.Tests/LoanApiTests.cs | 233 | TEST-2 | The name `When_several_members_race_for_one_book_exactly_one_wins` does not follow the naming pattern. | Rename it | Small |
| 62 | tests/Library.Api.Tests/LoanApiTests.cs | 244 | TEST-2 | The name `When_one_member_races_for_several_books_the_limit_still_holds` does not follow the naming pattern. | Rename it | Small |
| 63 | tests/Library.Api.Tests/DatabaseInitializationTests.cs | 23 | TEST-2 | The name `A_missing_database_is_created_with_20_books_numbered_1_to_20` does not follow the naming pattern. | Rename it | Small |
| 64 | tests/Library.Api.Tests/DatabaseInitializationTests.cs | 33 | TEST-2 | The name `An_existing_database_is_not_seeded_again` does not follow the naming pattern. | Rename it | Small |
| 65 | src/Library.Api/Library.Api.csproj | 10 | WORK-3 | `<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.14" />` sets its version in the project, and there is no `Directory.Packages.props`. | Add `Directory.Packages.props` with `ManagePackageVersionsCentrally`, and remove `Version` here | Small |
| 66 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 11 | WORK-3 | `coverlet.collector` sets `Version="10.1.0"` in the project. | Move the version to central package management | Small |
| 67 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 15 | WORK-3 | `Microsoft.AspNetCore.Mvc.Testing` sets `Version="10.0.14"` in the project. | Same as #66 | Small |
| 68 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 16 | WORK-3 | `Microsoft.NET.Test.Sdk` sets `Version="18.11.0"` in the project. | Same as #66 | Small |
| 69 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 17 | WORK-3 | `xunit` sets `Version="2.9.3"` in the project. | Same as #66 | Small |
| 70 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 18 | WORK-3 | `xunit.runner.visualstudio` sets `Version="4.0.1"` in the project. | Same as #66 | Small |

## Rules

| Rule | Status | Violations |
|------|--------|------------|
| SOLID-1 | Violated | 1 |
| SOLID-2 | Not applicable | 0 |
| SOLID-3 | Complies | 0 |
| SOLID-4 | Not applicable | 0 |
| SOLID-5 | Violated | 3 |
| CC-1 | Complies | 0 |
| CC-2 | Violated | 1 |
| CC-3 | Violated | 1 |
| CC-4 | Violated | 2 |
| CC-5 | Complies | 0 |
| CC-6 | Complies | 0 |
| ARCH-1 | Violated | 1 |
| ARCH-2 | Violated | 1 |
| ARCH-3 | Complies | 0 |
| ARCH-4 | Violated | 1 |
| DI-1 | Violated | 1 |
| DI-2 | Violated | 2 |
| DI-3 | Not applicable | 0 |
| DI-4 | Complies | 0 |
| DI-5 | Complies | 0 |
| DI-6 | Complies | 0 |
| DOM-1 | Violated | 3 |
| DOM-2 | Violated | 1 |
| DOM-3 | Not applicable | 0 |
| DOM-4 | Complies | 0 |
| DOM-5 | Violated | 2 |
| SEP-1 | Complies | 0 |
| SEP-2 | Complies | 0 |
| SEP-3 | Violated | 6 |
| TEST-1 | Complies | 0 |
| TEST-2 | Violated | 25 |
| TEST-3 | Violated | 1 |
| TEST-4 | Complies | 0 |
| TEST-5 | Violated | 1 |
| SA-1 | Violated | 1 |
| SA-2 | Measured by commands | 0 |
| SA-3 | Measured by commands | 0 |
| SA-4 | Not applicable | 0 |
| SEC-1 | Complies | 0 |
| SEC-2 | Complies | 0 |
| SEC-3 | Complies | 0 |
| SEC-4 | Violated | 1 |
| SEC-5 | Violated | 1 |
| SEC-6 | Complies | 0 |
| SEC-7 | Measured by commands | 0 |
| LOG-1 | Complies | 0 |
| LOG-2 | Complies | 0 |
| LOG-3 | Violated | 1 |
| LOG-4 | Complies | 0 |
| ASYNC-1 | Complies | 0 |
| ASYNC-2 | Violated | 7 |
| ASYNC-3 | Complies | 0 |
| WORK-1 | Not reviewable | 0 |
| WORK-2 | Complies | 0 |
| WORK-3 | Violated | 6 |
| WORK-4 | Not applicable | 0 |
| WORK-5 | Not reviewable | 0 |
| WORK-6 | Not reviewable | 0 |
| WORK-7 | Measured by commands | 0 |
| WORK-8 | Not applicable | 0 |
| WORK-9 | Not reviewable | 0 |
| WORK-10 | Not reviewable | 0 |

Totals: 70 violations in 23 rules; 23 rules comply; 7 not applicable; 5 not reviewable; 4 measured by commands.

## Doubts

- src/Library.Api/Program.cs:25: `app.Logger` is the host logger, not an `ILogger<T>` received through a constructor. Top-level `Program` has no constructor, so LOG-1 cannot be applied literally there. Not counted under LOG-1; the same line is counted under LOG-3.
- src/Library.Api/Auth/MemberHeaderAuthenticationHandler.cs:16: it takes `ILoggerFactory`, but the `AuthenticationHandler<T>` base constructor requires that. Not counted under LOG-1.
- src/Library.Api/Auth/MemberHeaderAuthenticationHandler.cs:38: the hardcoded message in `AuthenticateResult.Fail($"{HeaderName} must be a positive integer.")` is not sent to the client by default, so it may not be user-facing text. Not counted under SEP-3.
- src/Library.Api/Loans/LoanEndpoints.cs:21: the endpoint `BorrowAsync` is about 25 lines (21-45), slightly above "about 20". Too close to call for CC-2.
- src/Library.Api/Loans/LoanEndpoints.cs:51: `"bookId"` is a JSON property name written as a literal. It could be a CC-4 magic string, but it is the wire contract and appears only once. Not counted.
- src/Library.Api/Loans/LoanEndpoints.cs:22: the request comes in as `JsonElement`, not a request `record` (ARCH-3). The response `LoanDto` is defined in the service file, not by the endpoint. Neither one exposes an entity or database type, which is what the rule forbids. Not counted.
- src/Library.Api/Data/Entities.cs:14-15: `BorrowedAt` and `DueDate` together form a date range, which DOM-3 lists as an example. Member and book IDs are raw `int`. It is a stretch to treat these as value-defined concepts, so DOM-3 is marked not applicable.
- src/Library.Api/Data/Entities.cs:5-6: `Book` also has public setters and no invariant checks, but it carries no business behaviour in this change. Not counted under DOM-1 or DOM-2.
- src/Library.Api/Loans/LoanService.cs:90: `ReturnAsync` changes state and returns a `LoanFailure` code. That is a command reporting its outcome, not a query result, so it is not counted under SEP-2.
- src/Library.Api/Program.cs:9: the comment partly describes *what* the next line does (the default file name). It also documents behaviour that is not obvious (relative to the working directory). Not counted under CC-6.
- tests/Library.Api.Tests/LoanApiTests.cs:71: the test reads `DateTimeOffset.UtcNow`, a real clock. TEST-3 is about unit tests, and this is a `WebApplicationFactory` integration test, so it is not counted. It would go away once DI-2 (#11, #12) is fixed and `FakeTimeProvider` is used.
- tests/Library.Api.Tests/LoanApiTests.cs:18, 24, 31 and DatabaseInitializationTests.cs:44: these async test helpers do I/O without a `CancellationToken`. xUnit 2 gives the test no token to pass on, so they are not counted under ASYNC-2.
- Repository root: there is no `.editorconfig`, so CA2254 (LOG-2) and CA2016 (ASYNC-2) are not raised to `warning`. A missing file has no line to cite, so this is not counted. The same goes for the missing `Directory.Build.props` (SA-2 and SA-3, measured by commands).
- WORK-9: nothing has been committed yet (every file is untracked), so the diff cannot show the commit structure.