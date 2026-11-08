# Review against AGENTS.md

Base commit: 00bfbe04b9e97a49ae830df0da601f18e71c0c89. Files reviewed: 38. None of these files are committed. All are new and untracked: the solution, props files, `.editorconfig`, `.gitignore`, 4 src projects and 2 test projects.

## Findings

| # | File | Line | Rule | What breaks it | Proposal | Effort |
|---|------|------|------|----------------|----------|--------|
| 1 | src/Library.Api/Loans/LoanEndpoints.cs | 17 | SEC-3 | `app.MapPost($"{LoansRoute}/{{id:int}}/return", ReturnAsync);` accepts any int, including 0 and negatives. The `id` is passed to the service without a range check, while `bookId` is checked (`bookId <= 0`, line 27). The input's range is not validated at the boundary. | Use `{id:int:min(1)}` or reject `id <= 0` with a 400 problem response in `ReturnAsync`. | Small |
| 2 | src/Library.Application/LoanService.cs | 24 | DOM-5 | `if (activeLoanCount >= LoanRules.MaxActiveLoansPerMember)` puts the business rule "a member may hold at most 3 loans" in the application service, which should only load, call a domain method and save. | Move the decision into the domain, for example a domain service or policy (`LoanPolicy.EnsureCanBorrow(activeLoanCount)`) or a member aggregate. The application service then only supplies the count and calls it. | Medium |
| 3 | src/Library.Application/LoanService.cs | 66 | SEP-2 | `ReturnAsync` changes state (`loan.Return(...)` and save), then `return Success(loan);` gives back the whole `Loan`. Only commands that create something may return data. The caller (LoanEndpoints.cs:61) only checks it for null. | Return only the result (`LoanError`, or an outcome with no value) from `ReturnAsync` and `ILoanService.ReturnAsync`. | Small |
| 4 | src/Library.Infrastructure/Persistence/EfLoanRepository.cs | 18 | CC-3 | `loan.ReturnedAt == null` copies the "active loan" rule that already lives in `Loan.IsActive` (Loan.cs:21). | Define the rule once, for example `Loan.IsActiveExpression` (`Expression<Func<Loan,bool>>`), and use it here and in `IsActive`. | Small |
| 5 | src/Library.Infrastructure/Persistence/EfLoanRepository.cs | 23 | CC-3 | The same `loan.ReturnedAt == null` rule is copied a second time, in `CountActiveLoansForMemberAsync`. | Use the shared expression from finding 4. | Small |
| 6 | src/Library.Infrastructure/Persistence/EfLoanRepository.cs | 30 | CC-3 | The same `loan.ReturnedAt == null` rule is copied a third time, in `ListActiveLoansForMemberAsync`. | Use the shared expression from finding 4. | Small |
| 7 | src/Library.Application/LoanService.cs | 36 | TEST-1 | The `catch (LoanConflictException)` path, where a concurrent borrow becomes `BookAlreadyOnLoan`, is new behaviour with no test. The same goes for its source, `EfUnitOfWork`, which turns SQLite error 19 into `LoanConflictException`. | Add a unit test where an `IUnitOfWork` fake throws `LoanConflictException` and the result is `BookAlreadyOnLoan`. Add an SQLite test where a second active loan for one book raises `LoanConflictException`. | Medium |
| 8 | src/Library.Infrastructure/Persistence/LibraryDbContext.cs | 17 | CC-4 | `HasMaxLength(200)` is a magic number. | Use a named constant, for example `Book.MaxTitleLength`. | Small |
| 9 | src/Library.Api/Loans/LoanEndpoints.cs | 29 | SEP-3 | `title: "bookId must be a positive integer."` is text shown to users, written in the code. | Move it to a `.resx` file and read it through `IStringLocalizer<LoanEndpoints>`. | Medium |
| 10 | src/Library.Api/Loans/LoanEndpoints.cs | 74 | SEP-3 | `title: "The book does not exist."` is user-facing text written in the code. | Move it to resources and use `IStringLocalizer`. | Small |
| 11 | src/Library.Api/Loans/LoanEndpoints.cs | 75 | SEP-3 | `title: "The loan does not exist."` is user-facing text written in the code. | Move it to resources and use `IStringLocalizer`. | Small |
| 12 | src/Library.Api/Loans/LoanEndpoints.cs | 76 | SEP-3 | `title: "The book is already on loan."` is user-facing text written in the code. | Move it to resources and use `IStringLocalizer`. | Small |
| 13 | src/Library.Api/Loans/LoanEndpoints.cs | 79 | SEP-3 | `title: $"The member already has {LoanRules.MaxActiveLoansPerMember} active loans."` is user-facing text written in the code. | Move it to a resource with a format placeholder and use `IStringLocalizer`. | Small |
| 14 | src/Library.Api/Loans/LoanEndpoints.cs | 80 | SEP-3 | `title: "The loan has already been returned."` is user-facing text written in the code. | Move it to resources and use `IStringLocalizer`. | Small |
| 15 | tests/Library.UnitTests/ArchitectureTests.cs | 22 | TEST-2 | `Domain_ReferencesOnlySystemAssemblies` does not follow `Method_Scenario_ExpectedResult`: it has no scenario part. | Rename, for example `DomainAssembly_ReferencedAssemblies_AreOnlySystemAssemblies`. | Small |
| 16 | tests/Library.UnitTests/ArchitectureTests.cs | 32 | TEST-2 | `Application_ReferencesOnlyDomainAndSystemAssemblies` does not follow `Method_Scenario_ExpectedResult`. | Rename it to the three-part pattern. | Small |
| 17 | tests/Library.UnitTests/ArchitectureTests.cs | 42 | TEST-2 | `Application_DoesNotReferenceInfrastructureOrApi` does not follow `Method_Scenario_ExpectedResult`. | Rename it to the three-part pattern. | Small |
| 18 | tests/Library.UnitTests/ArchitectureTests.cs | 51 | TEST-2 | `Infrastructure_DoesNotReferenceApi` does not follow `Method_Scenario_ExpectedResult`. | Rename it to the three-part pattern. | Small |
| 19 | tests/Library.IntegrationTests/LoanApiTests.cs | 283 | ASYNC-2 | `private async Task<LoanDto> BorrowAsync(string memberHeader, int bookId)` does HTTP I/O but has no `CancellationToken cancellationToken` last parameter. | Add the parameter and pass it to `SendAsync` and `ReadFromJsonAsync`. | Small |
| 20 | tests/Library.IntegrationTests/LoanApiTests.cs | 291 | ASYNC-2 | `ReadLoansAsync(HttpResponseMessage response)` reads the response body without a `CancellationToken` parameter. | Add the parameter and pass it to `ReadFromJsonAsync`. | Small |
| 21 | tests/Library.IntegrationTests/LoanApiTests.cs | 297 | ASYNC-2 | `SendAsync(HttpMethod method, string url, string? memberHeader, object? body = null)` does I/O without a `CancellationToken` parameter. | Add the parameter and pass it on. | Small |
| 22 | tests/Library.IntegrationTests/LoanApiTests.cs | 303 | ASYNC-2 | `SendRawAsync(...)` calls `_client.SendAsync(request)` (line 311) without a token and has no `CancellationToken` parameter. | Add the parameter and pass it to `_client.SendAsync(request, cancellationToken)`. | Small |

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
| CC-3 | Violated | 3 |
| CC-4 | Violated | 1 |
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
| SEP-2 | Violated | 1 |
| SEP-3 | Violated | 6 |
| TEST-1 | Violated | 1 |
| TEST-2 | Violated | 4 |
| TEST-3 | Complies | 0 |
| TEST-4 | Complies | 0 |
| TEST-5 | Complies | 0 |
| SA-1 | Complies | 0 |
| SA-2 | Measured by commands | 0 |
| SA-3 | Measured by commands | 0 |
| SA-4 | Complies | 0 |
| SEC-1 | Complies | 0 |
| SEC-2 | Complies | 0 |
| SEC-3 | Violated | 1 |
| SEC-4 | Complies | 0 |
| SEC-5 | Complies | 0 |
| SEC-6 | Complies | 0 |
| SEC-7 | Measured by commands | 0 |
| LOG-1 | Complies | 0 |
| LOG-2 | Complies | 0 |
| LOG-3 | Complies | 0 |
| LOG-4 | Complies | 0 |
| ASYNC-1 | Complies | 0 |
| ASYNC-2 | Violated | 4 |
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

Totals: 22 violations in 9 rules; 39 rules comply; 5 not applicable; 5 not reviewable; 4 measured by commands.

## Doubts

- src/Library.Api/Program.cs:24: `LoanService` needs `TimeProvider`, but neither `Program.cs` nor `AddLibraryInfrastructure` registers one. The app relies on a framework registration, which I believe `AddAuthentication` provides on .NET 8 and later. If that is wrong, `ValidateOnBuild` would fail at startup outside the tests. The integration tests could not show it, because they register their own clock (LibraryApiFactory.cs:30). Not counted because I could not confirm it from the code.
- src/Library.Application/LoanService.cs:11: `BorrowAsync` runs to about 33 lines, against the "about 20" that CC-2 aims for. It has three test helpers (LoanApiTests.cs:297, 303) take 4 parameters with no written reason. Not counted because CC-2 is worded as a target ("aim for about"), and the method stays at one level of abstraction.
- src/Library.Application/LoanService.cs:58: `if (!loan.IsActive)` repeats the guard that `Loan.Return` (Loan.cs:39) already applies. This could count under CC-3 or DOM-5. Not counted because it turns the domain rule into a result instead of an exception, rather than applying the rule a second time.
- src/Library.Infrastructure/Persistence/LibraryDbContext.cs:27: `HasFilter("\"ReturnedAt\" IS NULL")` writes the column name as a string. It is SQL with no input in it, so not SEC-2. It might count under CC-4, where `nameof(Loan.ReturnedAt)` would be the fix. Not counted.
- src/Library.Infrastructure/Persistence/LibraryDbContext.cs:17 and src/Library.Domain/Book.cs:12: the 200-character title limit is only set in the database. `Book` itself accepts any length. This may be a missing DOM-2 invariant, but no rule says title length is a business invariant.
- src/Library.Api/Loans/LoanEndpoints.cs:24: `ILogger<LoanEndpoints>` arrives as a handler parameter, not through a constructor. The class is static-style, so there is no constructor. Minimal APIs inject parameters this way, so I did not count it under LOG-1.
- src/Library.Api/Authentication/MemberHeaderAuthenticationHandler.cs:21: anyone can claim any member ID by setting `X-Member-Id`. The doc comment says this is a stand-in for a real identity provider. SEC-4's fallback policy and the ownership check (`FindOwnedByMemberAsync`) are in place, and no rule covers how strong the authentication scheme is. Not counted.
- src/Library.Api/Authentication/MemberHeaderAuthenticationHandler.cs:30: the `AuthenticateResult.Fail` message is an English string in the code. By default it does not reach the client (the 401 has no body), so I did not count it under SEP-3.
- tests/Library.IntegrationTests/LoanApiTests.cs:232-233 and 244-245: each test sends the return request, then a list request. The second request could count as a second Act under TEST-2. I read it as observing state for the Assert. LibraryDatabaseSeederTests.cs:42-43 calls `InitializeAsync` twice, which is the scenario itself. Not counted.
- tests/Library.IntegrationTests/LibraryApiFactory.cs:44: the tests use a hand-written `FixedTimeProvider` rather than the `FakeTimeProvider` that TEST-3 names. Time is still controlled, and adding the package would need the person's permission (WORK-3). Not counted.
- tests/Library.IntegrationTests/LoanApiTests.cs:13: `"X-Member-Id"` repeats `MemberHeaderAuthenticationDefaults.HeaderName`. It may be deliberate, so the test pins the public contract. Not counted under CC-3.
- DOM-3 (marked not applicable): the change has no money, ISBN or email concept. `BorrowedAt`/`DueAt` could be read as a date range value object, but that is a design preference rather than a clear case for the rule.
- WORK-9 (marked not reviewable): nothing has been committed yet. The whole change is untracked, so commit size and messages cannot be judged.