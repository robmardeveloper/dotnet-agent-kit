# Review against AGENTS.md

Base commit: faeae242a2c415d1f55d8bf7b64b0cffe2e69920. Files reviewed: 52. Every file is new and untracked, because the base commit holds only `AGENTS.md` and the skill. `Library.sln` was only listed, not read.

## Findings

| # | File | Line | Rule | What breaks it | Proposal | Effort |
|---|------|------|------|----------------|----------|--------|
| 1 | src/Library.Application/BorrowBookUseCase.cs | 19 | DOM-5 | `if (activeLoanCount >= LoanPolicy.MaxActiveLoansPerMember)`: the application service applies the loan-limit rule itself. No single `Loan` owns this rule, so it belongs in a domain service. The use case should only load, call, save and return. | Move the limit check into a domain service in `Library.Domain` (for example `LoanEligibility.CanBorrow(activeLoanCount)`, or a policy that returns the failure). The use case then just calls it. | Medium |
| 2 | src/Library.Api/Resources/Messages.cs | 11 | SEP-3 | `private static readonly ResourceManager Manager = new("Library.Api.Resources.Messages", ...)`: user-facing text is read through a static `ResourceManager`, not through `IStringLocalizer<T>`. `LoanEndpoints.cs` lines 26 and 84-88 use the static `Messages`. `IStringLocalizer` is in the ASP.NET Core shared framework, so no package is needed. | Call `AddLocalization()`, inject `IStringLocalizer<Messages>` into the handlers, and delete the static `ResourceManager` wrapper. | Medium |
| 3 | src/Library.Application/ReturnLoanUseCase.cs | 16 | CC-3 | `if (loan.IsReturned) { return Result.Fail(LoanFailure.LoanAlreadyReturned); }` repeats the rule that `Loan.Return` already enforces (`Loan.cs:40`, `if (IsReturned) throw new LoanAlreadyReturnedException(Id)`). The "already returned" rule now lives in two places. | Keep the rule in one place. Either let `Loan.Return` report the outcome (for example return a result or a `bool`) and map that, or catch `LoanAlreadyReturnedException` in the use case. | Small |
| 4 | src/Library.Infrastructure/Persistence/LoanRepository.cs | 13 | CC-3 | The "active loan" predicate `loan.ReturnedAt == null` is written three times: lines 13, 18 and 26. It also exists in the domain as `Loan.IsReturned`. | Extract one `Expression<Func<Loan, bool>> IsActive` (or an `IQueryable<Loan>.Active()` extension) and reuse it in all three queries. | Small |
| 5 | src/Library.Infrastructure/Persistence/UnitOfWork.cs | 18 | TEST-1 | `catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))` turns SQLite error 2067 into `DuplicateActiveLoanException`, but no test makes the real index reject a duplicate. `BorrowBookUseCaseTests` uses `FakeUnitOfWork`. `Borrow_BookAlreadyOnLoan_Returns409` stops at the pre-check and never reaches the index. | Add a SQLite in-memory test: save two active loans for the same book through `UnitOfWork`, and assert `DuplicateActiveLoanException`. | Small |
| 6 | src/Library.Infrastructure/Persistence/LoanRepository.cs | 29 | TEST-1 | `.OrderBy(loan => loan.DueDate).ThenBy(loan => loan.Id)`: the order of `GET /loans` is a behaviour, but no test checks it. `ListActiveLoans_ReturnsOnlyCallersActiveLoans` asserts a single item. | Add a test with two or more active loans that asserts their order. | Small |
| 7 | tests/Library.Tests/Api/LoanApiTests.cs | 135 | TEST-2 | `ListActiveLoans_ReturnsOnlyCallersActiveLoans` has no Scenario part, so it does not follow `Method_Scenario_ExpectedResult`. | Rename, for example `ListActiveLoans_OtherMemberAndReturnedLoans_ReturnsOnlyCallersActiveLoans`. | Small |
| 8 | tests/Library.Tests/Api/LoanApiTests.cs | 209 | ASYNC-2 | `private async Task<int> BorrowAsync(int memberId, int bookId)` makes an HTTP call but has no `CancellationToken cancellationToken` parameter. It passes `CancellationToken.None` instead. | Add a last `CancellationToken cancellationToken` parameter and pass it on. | Small |
| 9 | tests/Library.Tests/Api/LoanApiTests.cs | 220 | ASYNC-2 | `private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)` reads a stream asynchronously but has no `CancellationToken` parameter. It hard-codes `CancellationToken.None` (lines 222-223). | Add a last `CancellationToken cancellationToken` parameter and pass it on. | Small |

## Rules

| Rule | Status | Violations |
|------|--------|------------|
| SOLID-1 | Complies | 0 |
| SOLID-2 | Not applicable | 0 |
| SOLID-3 | Complies | 0 |
| SOLID-4 | Complies | 0 |
| SOLID-5 | Complies | 0 |
| CC-1 | Complies | 0 |
| CC-2 | Complies | 0 |
| CC-3 | Violated | 2 |
| CC-4 | Complies | 0 |
| CC-5 | Complies | 0 |
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
| DOM-5 | Violated | 1 |
| SEP-1 | Complies | 0 |
| SEP-2 | Complies | 0 |
| SEP-3 | Violated | 1 |
| TEST-1 | Violated | 2 |
| TEST-2 | Violated | 1 |
| TEST-3 | Complies | 0 |
| TEST-4 | Complies | 0 |
| TEST-5 | Complies | 0 |
| SA-1 | Complies | 0 |
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
| ASYNC-2 | Violated | 2 |
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

Totals: 9 violations in 6 rules; 42 rules comply; 5 not applicable; 5 not reviewable; 4 measured by commands.

## Doubts

- tests/Library.Tests/Support/TestTimeProvider.cs:6: TEST-3 says to control time with `FakeTimeProvider`. The change uses its own `TimeProvider` subclass, and the README says this is because of a package restriction (WORK-3: no package without asking). The clock is still controlled, so this is not counted.
- tests/Library.Tests/Infrastructure/LibraryDatabaseInitializerTests.cs:11: these tests use a real temp directory and a SQLite file (`File.Exists` on line 27). TEST-3 forbids the file system in *unit* tests. These read as infrastructure integration tests, and the scenario is about a new database file, so this is not counted.
- tests/Library.Tests/Application/BorrowBookUseCaseTests.cs:95: the `for` loop in the `AddActiveLoans` helper is test logic (TEST-2). It sits in an Arrange helper, not in a test body, so it is not counted. A `[Theory]` or explicit setup would remove it.
- src/Library.Api/Endpoints/LoanEndpoints.cs:16 and :53: the handlers take 5 parameters, and CC-2 says more than 3 needs a reason. Minimal API binds request and injected services through handler parameters, so the reason is implied by the framework. Not counted.
- src/Library.Application/BorrowBookUseCase.cs:11: `ExecuteAsync` is about 31 lines against the roughly 20 lines that CC-2 aims for. It stays at one level of abstraction, so it is not counted.
- src/Library.Api/Endpoints/LoanEndpoints.cs:20: LOG-1 says "constructor injection". The logger comes in as a handler parameter, because static minimal-API handlers have no constructor. The category is still the writing class, so this is not counted.
- src/Library.Api/Endpoints/LoanEndpoints.cs:82: the `switch` on `LoanFailure` in `ToProblem` is new code, not a branch added to an existing switch, so SOLID-2 was marked Not applicable. The next failure type will need another branch here.
- src/Library.Api/Endpoints/LoanEndpoints.cs:19: endpoints depend on the concrete use case classes (`BorrowBookUseCase` and the others) rather than abstractions (SOLID-5). This is a common choice for application services and they are not clearly "volatile", so it is not counted.
- src/Library.Domain/Loan.cs:38: `Return(DateTimeOffset returnedAt)` does not reject a `returnedAt` earlier than `BorrowedAt`, which could be an invariant (DOM-2). The rules do not state that invariant, so it is not counted.
- src/Library.Domain/Loan.cs:23: member, book and loan IDs are raw `int`, and the borrowed-to-due period is two separate `DateTimeOffset`s. DOM-3 lists money, date ranges, ISBNs and emails, not IDs. The loan period could be read as a date range, but no range type is passed around, so DOM-3 was marked Not applicable.
- src/Library.Api/Program.cs:56: `app.Run()` blocks after the earlier `await`; `await app.RunAsync()` would be consistent. It is not one of the patterns ASYNC-1 lists, so it is not counted.
- src/Library.Api/Program.cs:59: `public partial class Program` exists only for `WebApplicationFactory<Program>` (TEST-4: opening members for tests). TEST-5 asks for this pattern, so it is not counted.
- WORK-9: nothing has been committed; all files are untracked. Whether commits were asked for cannot be seen, so this was marked Not reviewable.