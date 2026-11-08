# Review against AGENTS.md

Base commit: faeae242a2c415d1f55d8bf7b64b0cffe2e69920. Files reviewed: 19.

There is no committed diff since the base. Every file under review is new and untracked: `.gitignore`, `Library.slnx`, 10 files in `src/Library.Api` and 7 files in `tests/Library.Api.Tests`. Where one finding covers several places, the row lists every line, and each place is counted in the Rules table.

## Findings

| # | File | Line | Rule | What breaks it | Proposal | Effort |
|---|------|------|------|----------------|----------|--------|
| 1 | src/Library.Api/Program.cs | 18 | SEC-4 | `builder.Services.AddAuthorization();` sets no fallback policy. Only the `/loans` group is protected, by `.RequireAuthorization()` at LoanEndpoints.cs:11, so any endpoint added later without it will be open to anyone. | Use `AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())`, and mark public endpoints with `AllowAnonymous`. | Small |
| 2 | src/Library.Api/Program.cs | 24 | SEC-5 | The pipeline (`app.UseAuthentication(); app.UseAuthorization(); ...`) has no `UseExceptionHandler`, and `AddProblemDetails` is never registered. LoanEndpoints.cs:30 and :44 throw `InvalidOperationException`, which nothing turns into a standard problem response. | Add `builder.Services.AddProblemDetails()` and `app.UseExceptionHandler()` outside Development. | Small |
| 3 | src/Library.Api/Library.Api.csproj | 3 | ARCH-1 | Everything is in one project (`Library.Api`): entities (`Models/`), rules (`Loans/LoanService.cs`), EF Core (`Data/`) and endpoints. There are no `Domain`, `Application` or `Infrastructure` projects, and nothing enforces the direction of references. | Split into `Library.Domain`, `Library.Application`, `Library.Infrastructure` and `Library.Api`, with references pointing inward. | Large |
| 4 | src/Library.Api/Loans/LoanService.cs | 12 | ARCH-2 | The application service `LoanService(IDbContextFactory<LibraryDbContext> dbFactory)` names the EF Core context and uses `Microsoft.EntityFrameworkCore` directly, instead of an interface it declares. | Declare `ILoanRepository` / `IBookRepository` in Application, and implement them in Infrastructure with EF Core. | Large |
| 5 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 10 | ARCH-4 | The test project references no ArchUnitNET or NetArchTest package (lines 10-22), and no test checks the direction of references. | Add architecture tests for ARCH-1 and ARCH-2 once the layers exist. | Medium |
| 6 | src/Library.Api/Loans/LoanService.cs | 12 | SOLID-1 | One class holds both the loan rules (`MaxActiveLoansPerMember`, `LoanPeriod`, the availability check) and the persistence (DbContext creation, `SaveChangesAsync`, `ExecuteUpdateAsync`, catching `DbUpdateException`). | Move the rules into the `Loan` entity or a domain type, and keep persistence behind a repository. | Large |
| 7 | src/Library.Api/Loans/LoanEndpoints.cs | 14 (also 34, 37) | SOLID-5 | Every handler takes the concrete `LoanService service`, a volatile class, rather than an abstraction. | Depend on an interface, such as `ILoanService` or one handler per use case. | Medium |
| 8 | src/Library.Api/Loans/LoanService.cs | 12 | SOLID-5 | The class depends on the concrete `LibraryDbContext` (through `IDbContextFactory<LibraryDbContext>`). | Depend on a repository interface. | Medium (comes with #4) |
| 9 | src/Library.Api/Loans/LoanService.cs | 44, 92 | DI-2 | `var borrowedAt = DateTimeOffset.UtcNow;` and `SetProperty(l => l.ReturnedAt, DateTimeOffset.UtcNow)` read the real clock in the service. | Inject `TimeProvider` and call `GetUtcNow()`. Use `FakeTimeProvider` in tests. | Small |
| 10 | src/Library.Api/Models/Loan.cs | 5 | DOM-1 | Every property has `{ get; set; }` (lines 5-16). The entity has no domain methods, such as `Loan.Borrow(...)` or `loan.Return(...)`. | Use private setters, a factory `Loan.Start(bookId, memberId, now)` and a method `Return(now)`. | Medium |
| 11 | src/Library.Api/Models/Book.cs | 5 | DOM-1 | `public int Id { get; set; }` and `public required string Title { get; set; }` (line 7) are public setters. | Use private setters and a constructor. | Small |
| 12 | src/Library.Api/Loans/LoanService.cs | 45 | DOM-1 | The service builds the loan by setting its properties directly (`new Loan { BookId = ..., MemberId = ..., BorrowedAt = ..., DueDate = borrowedAt + LoanPeriod }`). It applies the due-date rule itself. | Call a domain factory that computes `DueDate`. | Small |
| 13 | src/Library.Api/Loans/LoanService.cs | 92 | DOM-1 | Returning a loan sets the entity's state from the service: `setters.SetProperty(l => l.ReturnedAt, DateTimeOffset.UtcNow)`. | Load the loan, call `loan.Return(now)`, then save. | Medium |
| 14 | src/Library.Api/Models/Loan.cs | 3 | DOM-2 | `Loan` has no constructor or check. It can exist with `BookId = 0`, `MemberId = 0`, or `DueDate` before `BorrowedAt`. | Add a constructor or factory that checks the invariants. | Small |
| 15 | src/Library.Api/Models/Book.cs | 7 | DOM-2 | `required string Title` accepts `""`, and nothing checks it. | Add a constructor that rejects an empty or blank title. | Small |
| 16 | src/Library.Api/Loans/LoanService.cs | 37 | DOM-5 | `BorrowAsync` holds the business rules itself: the active-loan count against `MaxActiveLoansPerMember` (lines 37-42), the "already on loan" check (line 32) and the due-date calculation (line 50). It is not just load, call, save. | Move the rules into the domain, such as a `Member`/loan-policy method or the `Loan` factory. | Large |
| 17 | src/Library.Api/Loans/LoanService.cs | 89 | DOM-5 | `ReturnAsync` applies the "only an active loan can be returned" rule in a query predicate (`l.ReturnedAt == null` in an `ExecuteUpdateAsync`). | Put the rule in `Loan.Return()`, which rejects a loan that is already returned. | Medium |
| 18 | src/Library.Api/Loans/LoanEndpoints.cs | 18, 25, 26, 29, 42, 43 | SEP-3 | User-facing texts are written in the code: `"bookId must be a positive integer."`, `"Book not found."`, `"Book is already on loan."`, `$"Member already has {...} active loans."`, `"Loan not found."`, `"Loan was already returned."`. | Move them to `.resx` files and read them through `IStringLocalizer<T>`. | Medium |
| 19 | src/Library.Api/Auth/ClaimsPrincipalExtensions.cs | 10 | SA-1 | `user.FindFirstValue(ClaimTypes.NameIdentifier)!` uses the null-forgiving operator to silence the nullable warning. | Check for null and throw a clear exception, or return `int?`. | Small |
| 20 | tests/Library.Api.Tests/ApiTestBase.cs | 30, 37 | SA-1 | `(await response.Content.ReadFromJsonAsync<...>(JsonOptions))!` | Use `Assert.NotNull` and return the checked value. | Small |
| 21 | tests/Library.Api.Tests/LoanTests.cs | 22, 25 | SA-1 | `.GetString()!` and `JsonSerializer.Deserialize<LoanResponse>(json, JsonOptions)!` | Use `Assert.NotNull` before using the value. | Small |
| 22 | src/Library.Api/Loans/LoanService.cs | 20 | CC-2 | `BorrowAsync` is about 50 lines (20-70). It mixes locking, queries, rule checks, entity creation and handling of the save error. | Extract the rule checks and the save into separate methods, or into the domain. | Medium |
| 23 | src/Library.Api/Loans/LoanEndpoints.cs | 9 | CC-2 | `MapLoanEndpoints` is about 40 lines (9-49), because all three handlers are inline lambdas. | Move each handler into a named static method. | Small |
| 24 | src/Library.Api/Loans/LoanService.cs | 38 | CC-3 | The "active loan" rule `l.ReturnedAt == null` (with `l.MemberId == memberId`) is written again at lines 32, 38, 78 and 90. | Put it in one place, such as an `IsActive` expression or a domain property. | Small |
| 25 | src/Library.Api/Loans/LoanEndpoints.cs | 60 | CC-4 | The JSON property name is a magic string: `element.TryGetProperty("bookId", ...)`. | Use a named constant, or a request record (see the doubt on ARCH-3). | Small |
| 26 | tests/Library.Api.Tests/ApiTestBase.cs | 16 | CC-4 | `"X-Member-Id"` is written as a literal, although `MemberHeaderAuthenticationHandler.HeaderName` exists. | Use `MemberHeaderAuthenticationHandler.HeaderName`. | Small |
| 27 | tests/Library.Api.Tests/AuthenticationTests.cs | 28, 43 | CC-4 | `"X-Member-Id"` is written as a literal twice. | Use `MemberHeaderAuthenticationHandler.HeaderName`. | Small |
| 28 | tests/Library.Api.Tests/LoanTests.cs | 15, 49, 64, 74, 85, 96, 109, 122, 139, 154, 162, 174, 186, 198, 210, 218 | TEST-2 | None of the 16 test names follow `Method_Scenario_ExpectedResult` (for example `Fourth_active_loan_is_409`, `Returning_twice_is_409`). | Rename them, for example `Borrow_FourthActiveLoan_Returns409`. | Small |
| 29 | tests/Library.Api.Tests/AuthenticationTests.cs | 16, 25, 40, 51 | TEST-2 | None of the 4 names follow `Method_Scenario_ExpectedResult` (for example `Valid_header_is_authenticated`). | Rename them, for example `Authenticate_ValidHeader_Returns200`. | Small |
| 30 | tests/Library.Api.Tests/DatabaseInitializationTests.cs | 13, 28 | TEST-2 | `Creates_missing_file_with_books_1_to_20` and `Second_initialization_keeps_existing_books` do not follow the pattern. | Rename them, for example `InitializeAsync_MissingFile_CreatesTwentyBooks`. | Small |
| 31 | tests/Library.Api.Tests/AuthenticationTests.cs | 58 | TEST-2 | The theories run their Act through a `switch` on `method` (lines 58-64), which puts logic in the tests. | Pass an `HttpRequestMessage` or a delegate in the theory data, or split into separate theories. | Small |
| 32 | tests/Library.Api.Tests/LoanTests.cs | 17 | TEST-3 | `var before = DateTimeOffset.UtcNow;` reads the real clock, and line 29 then asserts within ±1 minute. | Replace `TimeProvider` with `FakeTimeProvider` in the factory and assert the exact due date. | Small (after #9) |
| 33 | tests/Library.Api.Tests/LibraryApiFactory.cs | 18 | TEST-5 | The integration tests use an SQLite file in the temp directory (`$"Data Source={DatabasePath}"`, path built at line 12), not SQLite in memory. | Use a shared open `SqliteConnection("DataSource=:memory:")` and register it in `ConfigureTestServices`. | Medium |
| 34 | src/Library.Api/Library.Api.csproj | 10 | WORK-3 | `<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.14" />` puts the version in the project file. There is no `Directory.Packages.props`. | Add `Directory.Packages.props` with `ManagePackageVersionsCentrally`, and remove `Version` from the project. | Small |
| 35 | tests/Library.Api.Tests/Library.Api.Tests.csproj | 11, 15, 16, 17, 18 | WORK-3 | Five `PackageReference` lines carry `Version="..."`: coverlet.collector, Mvc.Testing, Test.Sdk, xunit, xunit.runner.visualstudio. | Move the versions to `Directory.Packages.props`. | Small |

## Rules

| Rule | Status | Violations |
|------|--------|------------|
| SOLID-1 | Violated | 1 |
| SOLID-2 | Not applicable | 0 |
| SOLID-3 | Complies | 0 |
| SOLID-4 | Not applicable | 0 |
| SOLID-5 | Violated | 2 |
| CC-1 | Complies | 0 |
| CC-2 | Violated | 2 |
| CC-3 | Violated | 1 |
| CC-4 | Violated | 4 |
| CC-5 | Complies | 0 |
| CC-6 | Complies | 0 |
| ARCH-1 | Violated | 1 |
| ARCH-2 | Violated | 1 |
| ARCH-3 | Complies | 0 |
| ARCH-4 | Violated | 1 |
| DI-1 | Complies | 0 |
| DI-2 | Violated | 2 |
| DI-3 | Not applicable | 0 |
| DI-4 | Complies | 0 |
| DI-5 | Complies | 0 |
| DI-6 | Complies | 0 |
| DOM-1 | Violated | 4 |
| DOM-2 | Violated | 2 |
| DOM-3 | Not applicable | 0 |
| DOM-4 | Complies | 0 |
| DOM-5 | Violated | 2 |
| SEP-1 | Complies | 0 |
| SEP-2 | Complies | 0 |
| SEP-3 | Violated | 6 |
| TEST-1 | Complies | 0 |
| TEST-2 | Violated | 23 |
| TEST-3 | Violated | 1 |
| TEST-4 | Complies | 0 |
| TEST-5 | Violated | 1 |
| SA-1 | Violated | 5 |
| SA-2 | Measured by commands | - |
| SA-3 | Measured by commands | - |
| SA-4 | Not applicable | 0 |
| SEC-1 | Complies | 0 |
| SEC-2 | Complies | 0 |
| SEC-3 | Complies | 0 |
| SEC-4 | Violated | 1 |
| SEC-5 | Violated | 1 |
| SEC-6 | Not applicable | 0 |
| SEC-7 | Measured by commands | - |
| LOG-1 | Not applicable | 0 |
| LOG-2 | Not applicable | 0 |
| LOG-3 | Not applicable | 0 |
| LOG-4 | Not applicable | 0 |
| ASYNC-1 | Complies | 0 |
| ASYNC-2 | Complies | 0 |
| ASYNC-3 | Complies | 0 |
| WORK-1 | Not reviewable | - |
| WORK-2 | Complies | 0 |
| WORK-3 | Violated | 6 |
| WORK-4 | Not applicable | 0 |
| WORK-5 | Not reviewable | - |
| WORK-6 | Not reviewable | - |
| WORK-7 | Measured by commands | - |
| WORK-8 | Not applicable | 0 |
| WORK-9 | Not reviewable | - |
| WORK-10 | Not reviewable | - |

Totals: 67 violations in 20 rules; 21 rules comply; 12 not applicable; 5 not reviewable; 4 measured by commands.

## Doubts

- src/Library.Api/Loans/LoanService.cs:44, 92: DI-1 also names "the clock" among the things to receive through the constructor. I counted these lines only under DI-2, the more specific rule, so they are not counted twice. DI-1 otherwise complies: the DbContext factory is injected.
- src/Library.Api/Loans/LoanEndpoints.cs:14: ARCH-3 asks endpoints to receive their own request `record`. The POST endpoint takes a raw `JsonElement?` instead, with a reason given in the comment at line 13. It is neither an entity nor a database type, so I did not count it. The response type `LoanDto` is declared in LoanService.cs:105 rather than by the endpoint, which is also borderline.
- src/Library.Api/Loans/LoanService.cs:99: `ownedByCaller` is a boolean not phrased as a question (`isOwnedByCaller`, CC-1). It is a local variable whose name reads clearly, so I did not count it.
- src/Library.Api/Program.cs:10: the fallback `"Data Source=library.db"` and the name `"Library"` are inline strings. They are a default for a value read from configuration, so I did not count them under CC-4.
- tests/Library.Api.Tests/LoanTests.cs:28: `before.AddDays(14)` repeats `LoanService.LoanPeriod`. Writing the expected value as a literal is usual in tests, so I did not count it under CC-4 or CC-3.
- src/Library.Api/Models/Loan.cs:11-13: `BorrowedAt`/`DueDate` together form a date range, and `MemberId`/`BookId` are raw `int`s. They could be value objects under DOM-3, but the rule's examples do not clearly cover IDs, so I marked DOM-3 not applicable.
- tests/Library.Api.Tests/LoanTests.cs:133: `ListAsync(client)` in the Assert section is a second call to the API, which could be read as a second Act under TEST-2. Not counted.
- tests/Library.Api.Tests/DatabaseInitializationTests.cs:10: these tests use the real file system (TEST-3). What they test is the creation of the database file, so it cannot be avoided. Not counted.
- src/Library.Api/Program.cs:31: `public partial class Program` is opened up for the tests (TEST-4). This is the pattern `WebApplicationFactory<Program>` requires, and TEST-5 asks for it, so it is not counted.
- tests/Library.Api.Tests/ApiTestBase.cs:23-38: the test helpers do I/O without a `CancellationToken` parameter (ASYNC-2). They are test code that no request calls. Not counted.
- ASYNC-2 and LOG-2 also ask for CA2016 and CA2254 to be raised to `warning` in `.editorconfig`, and the change adds no `.editorconfig`. With no line to point at, this is left to the WORK-7 measurements.
- src/Library.Api/Program.cs:22: `LibraryDatabase.InitializeAsync(...)` is called without a token. None is in scope in top-level startup, so I did not count it.
- src/Library.Api/Auth/MemberHeaderAuthenticationHandler.cs:13: any caller can claim any member ID through the `X-Member-Id` header. The doc comment calls it a stand-in for a real identity provider, so I did not count it under SEC-4. It must not ship as real authentication.
- src/Library.Api/Auth/MemberHeaderAuthenticationHandler.cs:32: the `Fail` message is hard-coded text. ASP.NET Core does not send it to the client by default, so I did not count it under SEP-3.
- src/Library.Api/Loans/LoanService.cs:84: `ReturnAsync` changes state and returns a `ReturnResult` (SEP-2). That is an outcome status, not queried data, so I did not count it.
- src/Library.Api/Loans/LoanService.cs:88: the comment "Conditional update: only an active loan that belongs to the caller is changed." partly restates the query (CC-6), but it also states the intent. Not counted.
- WORK-9: nothing has been committed after the base. All the work is uncommitted, so the commits cannot be judged.