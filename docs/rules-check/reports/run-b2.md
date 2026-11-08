# Review against AGENTS.md

Base commit: c330410ace53703bb0a4faa31d7ebd74018b0b77. Files reviewed: 45. Nothing is committed since the base commit, so every reviewed file is a new untracked file.

## Findings

| # | File | Line | Rule | What breaks it | Proposal | Effort |
|---|------|------|------|----------------|----------|--------|
| 1 | src/Library.Infrastructure/Persistence/EfUnitOfWork.cs | 18 | TEST-1 | `catch (DbUpdateException exception) when (IsConstraintViolation(exception))` turns SQLite error 19 into a `ConflictException` (409). No test reaches this path. The service checks for an active loan first, so the unique-index branch never runs in the existing tests. | Add an Infrastructure test on SQLite in memory: insert two active loans for the same book through `EfLoanRepository`/`EfUnitOfWork` and assert `ConflictException`. | Small |
| 2 | src/Library.Application/Loans/LoanService.cs | 26 | DOM-5 | `if (activeLoanCount >= Loan.MaxActiveLoansPerMember) throw new ConflictException(...)`: the three-loan business rule is decided in the application service. It is not in the domain, even though the constant lives in `Loan`. | Move the decision into the domain, either as a domain service (for example `LoanPolicy.EnsureCanBorrow(activeLoanCount, isBookOnLoan)`) or as a parameter checked inside `Loan.Borrow`. The service then only loads the data, calls the domain and saves. | Medium |
| 3 | src/Library.Application/Loans/LoanService.cs | 20 | DOM-5 | `if (await loans.HasActiveLoanForBookAsync(...)) throw new ConflictException(...)`: the "one active loan per book" rule is also decided in the application service. | Move it into the same domain policy as finding 2. | Medium |
| 4 | src/Library.Api/Loans/LoanEndpoints.cs | 32 | SEP-3 | `[BookIdField] = ["bookId must be a positive integer."]` is text for users, written in the code. | Move it to a `.resx` file and read it through `IStringLocalizer<T>`. | Small |
| 5 | src/Library.Api/Errors/ErrorHandlingServiceCollectionExtensions.cs | 10 | SEP-3 | `NotFoundDetail = "The requested resource was not found."` is sent to the client as the problem `detail`. | Read it from a `.resx` file through `IStringLocalizer<T>`, for example by resolving it in the handler when the response is written. | Small |
| 6 | src/Library.Api/Errors/ErrorHandlingServiceCollectionExtensions.cs | 11 | SEP-3 | `ConflictDetail = "The request conflicts with the current state of the resource."` is sent to the client. | Same as finding 5. | Small |
| 7 | src/Library.Api/Errors/ErrorHandlingServiceCollectionExtensions.cs | 12 | SEP-3 | `MalformedRequestDetail = "The request body is not valid for this endpoint."` is sent to the client. | Same as finding 5. | Small |
| 8 | src/Library.Infrastructure/Persistence/EfLoanRepository.cs | 13 | CC-3 | The "active loan" rule `loan.ReturnedAt == null` is written three times (lines 13, 16 and 20). It also exists as `Loan.IsActive`. | Put it in one `static readonly Expression<Func<Loan, bool>> IsActive` (or an `IQueryable<Loan>` extension `WhereActive()`) and reuse it in all three queries. | Small |
| 9 | src/Library.Infrastructure/Persistence/LibraryDbContext.cs | 18 | CC-4 | `HasMaxLength(200)` is a magic number. | Use a named constant, for example `Book.MaxTitleLength`. | Small |
| 10 | src/Library.Infrastructure/Persistence/LibraryDbContext.cs | 29 | CC-4 | `.HasFilter("\"ReturnedAt\" IS NULL")` is a magic string that repeats a column name. | Use a named constant built from `nameof(Loan.ReturnedAt)`. | Small |
| 11 | tests/Library.Tests/Api/LoansApiTests.cs | 187 | SA-1 | `Assert.Single(loans!)`: the `!` silences a nullable warning. | Call `Assert.NotNull(loans);` first, then use `loans`. | Small |
| 12 | tests/Library.Tests/Api/LoansApiTests.cs | 202 | SA-1 | `Assert.Empty(loans!)` uses `!` to silence a nullable warning. | Call `Assert.NotNull(loans)` first. | Small |
| 13 | tests/Library.Tests/Api/LoansApiTests.cs | 235 | SA-1 | `Assert.Single(loans!)` uses `!` to silence a nullable warning. | Call `Assert.NotNull(loans)` first. | Small |
| 14 | tests/Library.Tests/Api/LoansApiTests.cs | 266 | SA-1 | `return loan!;` uses `!` to silence a nullable warning. | Call `Assert.NotNull(loan);` and then `return loan;`. | Small |
| 15 | tests/Library.Tests/Architecture/ArchitectureTests.cs | 100 | CC-5 | `if (forbidden.Any(...))` sits inside a `for`, which sits inside a `foreach`: three levels of nesting in `FindSourceLinesContaining`. | Extract the line scan into a method (for example `FindViolationsInFile(file, forbidden)`) or use LINQ `SelectMany`. | Small |
| 16 | tests/Library.Tests/Architecture/ArchitectureTests.cs | 12 | TEST-2 | `Domain_ReferencesNoProjectsPackagesOrFrameworks` has two parts, not `Method_Scenario_ExpectedResult`. | Rename it with three parts, for example `DomainProject_References_HasNoProjectsPackagesOrFrameworks`. | Small |
| 17 | tests/Library.Tests/Architecture/ArchitectureTests.cs | 22 | TEST-2 | `Application_ReferencesOnlyDomain` has two parts. | Rename it with three parts. | Small |
| 18 | tests/Library.Tests/Architecture/ArchitectureTests.cs | 32 | TEST-2 | `Infrastructure_ReferencesOnlyApplication` has two parts. | Rename it with three parts. | Small |
| 19 | tests/Library.Tests/Architecture/ArchitectureTests.cs | 40 | TEST-2 | `Api_ReferencesOnlyInnerLayersAndInfrastructureForComposition` has two parts. | Rename it with three parts. | Small |
| 20 | tests/Library.Tests/Architecture/ArchitectureTests.cs | 50 | TEST-2 | `Source_DoesNotUseOuterLayersOrFrameworkNamespaces` has two parts. | Rename it with three parts. | Small |
| 21 | tests/Library.Tests/Architecture/ArchitectureTests.cs | 68 | TEST-2 | `Source_DoesNotReadSystemClockOrCreateRandomValues` has two parts. | Rename it with three parts. | Small |

## Rules

| Rule | Status | Violations |
|------|--------|------------|
| SOLID-1 | Complies | 0 |
| SOLID-2 | Complies | 0 |
| SOLID-3 | Complies | 0 |
| SOLID-4 | Complies | 0 |
| SOLID-5 | Complies | 0 |
| CC-1 | Complies | 0 |
| CC-2 | Complies | 0 |
| CC-3 | Violated | 1 |
| CC-4 | Violated | 2 |
| CC-5 | Violated | 1 |
| CC-6 | Complies | 0 |
| ARCH-1 | Complies | 0 |
| ARCH-2 | Complies | 0 |
| ARCH-3 | Complies | 0 |
| ARCH-4 | Complies | 0 |
| DI-1 | Complies | 0 |
| DI-2 | Complies | 0 |
| DI-3 | Not applicable | 0 |
| DI-4 | Complies | 0 |
| DI-5 | Complies | 0 |
| DI-6 | Complies | 0 |
| DOM-1 | Complies | 0 |
| DOM-2 | Complies | 0 |
| DOM-3 | Not applicable | 0 |
| DOM-4 | Complies | 0 |
| DOM-5 | Violated | 2 |
| SEP-1 | Complies | 0 |
| SEP-2 | Complies | 0 |
| SEP-3 | Violated | 4 |
| TEST-1 | Violated | 1 |
| TEST-2 | Violated | 6 |
| TEST-3 | Complies | 0 |
| TEST-4 | Complies | 0 |
| TEST-5 | Complies | 0 |
| SA-1 | Violated | 4 |
| SA-2 | Measured by commands | 0 |
| SA-3 | Measured by commands | 0 |
| SA-4 | Complies | 0 |
| SEC-1 | Complies | 0 |
| SEC-2 | Complies | 0 |
| SEC-3 | Complies | 0 |
| SEC-4 | Complies | 0 |
| SEC-5 | Complies | 0 |
| SEC-6 | Complies | 0 |
| SEC-7 | Measured by commands | 0 |
| LOG-1 | Complies | 0 |
| LOG-2 | Complies | 0 |
| LOG-3 | Complies | 0 |
| LOG-4 | Complies | 0 |
| ASYNC-1 | Complies | 0 |
| ASYNC-2 | Complies | 0 |
| ASYNC-3 | Complies | 0 |
| WORK-1 | Not reviewable | 0 |
| WORK-2 | Complies | 0 |
| WORK-3 | Complies | 0 |
| WORK-4 | Not applicable | 0 |
| WORK-5 | Not reviewable | 0 |
| WORK-6 | Not reviewable | 0 |
| WORK-7 | Measured by commands | 0 |
| WORK-8 | Not applicable | 0 |
| WORK-9 | Not reviewable | 0 |
| WORK-10 | Not reviewable | 0 |

Totals: 21 violations in 8 rules; 41 rules comply; 4 not applicable; 5 not reviewable; 4 measured by commands.

## Doubts

- tests/Library.Tests/Support/TestClock.cs:3: TEST-3 (and DI-2) say to control time with `FakeTimeProvider`, but the tests use a hand-written `TestClock : TimeProvider`. Not counted: no test reads the real clock, so the point of the rule is met. Using `FakeTimeProvider` would also mean adding the `Microsoft.Extensions.TimeProvider.Testing` package, which WORK-3 says needs the person's approval.
- tests/Library.Tests/Infrastructure/LibraryDatabaseInitializerTests.cs:12: these tests write a real SQLite file in the temp folder, and TEST-3 forbids the file system. Not counted: they check that the database file is created (line 22), so they are integration tests of Infrastructure rather than the unit tests TEST-3 covers.
- tests/Library.Tests/Api/LoansApiTests.cs:201 and 234, and tests/Library.Tests/Infrastructure/LibraryDatabaseInitializerTests.cs:33-34: each of these tests makes a second call after the Act (a GET to check the result, or a second `InitializeAsync`), which could be read as two Acts under TEST-2. Not counted: the GET only observes the result, and running twice is the scenario being tested.
- tests/Library.Tests/Application/Fakes.cs:35-37: the fake sets the private `Loan.Id` through reflection. Not counted under TEST-4: no member was opened up and no private method is tested; this copies what the database does on insert, and the comment explains why.
- src/Library.Application/Loans/LoanService.cs:7-11: the constructor takes 4 parameters and CC-2 asks for a reason above 3. Not counted: they are injected dependencies, and the rule says "needing a reason", not "forbidden".
- src/Library.Domain/Book.cs:12: the database limits `Title` to 200 characters, but the `Book` constructor does not check that limit (DOM-2). Not counted: the limit is only declared in persistence, so it is not clear that the domain has this invariant.
- src/Library.Api/Loans/LoanEndpoints.cs:17: `{loanId:int}` accepts zero and negative ids, which return 404 instead of being rejected as 400 (SEC-3). Not counted: the input is still rejected and nothing unsafe follows.
- tests/Library.Tests/Api/LoansApiTests.cs:260: the helper `BorrowAsync` does HTTP I/O with no `CancellationToken` parameter (ASYNC-2). Not counted: it is a private test helper, and xUnit 2 test methods have no token to pass down.
- Book and member ids are plain `int` (DOM-3). Not counted: identifiers are not among the value concepts the rule names (money, date range, ISBN, email).