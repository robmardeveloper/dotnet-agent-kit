# AGENTS.md: clean .NET rules for AI coding agents

> **Template.** Copy this file to the root of your .NET repository and adapt it.
> Any agent that reads `AGENTS.md` will follow it (Claude Code, Copilot, Codex, Cursor, Gemini CLI and others).

## How to read these rules

- **Every rule applies to every change you make.** If a rule blocks you, stop and ask. Do not work around it.
- **Every rule has an ID** (for example `SOLID-1`). Reviews report violations by ID.
- **Every rule is one imperative line**, followed by **Why** (the reason) and **Based on** (the principle or book it
  applies). The rule is our own concrete, checkable application of that source, not a quotation from it.
- **These rules are recommendations** grounded in recognised sources. Two things have been checked: the reviewer
  finds violations of them (`docs/reviewer-check.md`), and on one task, with a small model, the code written with
  them had two security defaults that the code written without them always lacked (`docs/rules-check.md`). That is a
  narrow result, not proof that the rules make code better in general.

---

## 1. SOLID

**SOLID-1** Give each class a single actor to answer to: never mix business rules with persistence, HTTP, files or email in the same class.
- Why: when code serving two actors lives together, a change requested by one breaks the other, and the class cannot be tested without both.
- Based on: the Single Responsibility Principle, "a module should be responsible to one, and only one, actor". Robert C. Martin, *Clean Architecture* (2017), ch. 7.

**SOLID-2** Add new behaviour by adding a new type behind an existing abstraction (strategy, handler), not by adding another branch to an existing `switch` or `if` chain on a type or kind.
- Why: each new branch edits and risks code that already works; a new type leaves it untouched.
- Based on: the Open-Closed Principle, coined by Bertrand Meyer (1988). Martin, *Clean Architecture*, ch. 8; and Martin, *Clean Code* (1st ed., 2008), heuristic G23, "Prefer polymorphism to if/else or switch/case".

**SOLID-3** Make every implementation usable through its abstraction without the caller knowing which one it got: never throw `NotImplementedException` or `NotSupportedException` for an interface member, and never make an override demand more or guarantee less than its base.
- Why: if callers must check the concrete type, the abstraction is a lie and every caller pays for it.
- Based on: the Liskov Substitution Principle, Barbara Liskov, "Data Abstraction and Hierarchy" (1988). Martin, *Clean Architecture*, ch. 9. The "demand more, guarantee less" test comes from Bertrand Meyer's Design by Contract.

**SOLID-4** Keep interfaces small and shaped by their consumers: no consumer depends on members it does not call. A repository is the exception: one per aggregate (DOM-4), with only the operations its use cases need.
- Why: a fat interface couples unrelated consumers and forces implementations to fake members they do not need. Splitting a repository into one interface per use case would scatter one aggregate's persistence for no gain.
- Based on: the Interface Segregation Principle. Martin, *Clean Architecture*, ch. 10. The repository exception follows Evans, *Domain-Driven Design*, ch. 6 (Repositories, one per aggregate root).

**SOLID-5** Make source code depend on abstractions, never on volatile concrete classes; stable platform types (`string`, `DateOnly`, collections) are the exception.
- Why: concrete code under active development changes often; interfaces change rarely, so depending on them keeps changes local.
- Based on: the Dependency Inversion Principle. Martin, *Clean Architecture*, ch. 11. How the concrete classes get wired in is covered in block 4, Dependency injection.

## 2. Clean code

**CC-1** Use names that state intent: no unexplained abbreviations, no vague names (`Data`, `Info`, `Manager`, `Helper`, `Temp`), and booleans phrased as questions (`IsOverdue`, `HasReservations`).
- Why: code is read far more often than it is written; a good name removes the need for a comment.
- Based on: Martin, *Clean Code* (1st ed.), ch. 2, "Meaningful Names".

**CC-2** Keep methods short and at a single level of abstraction: aim for about 20 lines, and treat more than 3 parameters as needing a reason.
- Why: a short method that does one thing can be named, read and tested on its own.
- Based on: Martin, *Clean Code* (1st ed.), ch. 3, "Functions": small functions, one level of abstraction per function, and function arguments.

**CC-3** Do not duplicate logic: if the same rule or calculation appears twice, extract it to one place.
- Why: duplicated logic gets fixed in one copy and stays wrong in the other.
- Based on: the DRY principle ("Don't Repeat Yourself"). Martin, *Clean Code* (1st ed.), ch. 3, and heuristic G5, "Duplication".

**CC-4** Do not use magic numbers or strings: use named constants, enums or configuration options (`IOptions<T>`).
- Why: a name explains what the value means, and changing it happens in one place.
- Based on: Martin, *Clean Code* (1st ed.), heuristic G25, "Replace magic numbers with named constants".

**CC-5** Use guard clauses and early returns instead of nested conditionals; never indent more than two levels inside a method.
- Why: deep nesting hides the normal path of the method under its special cases.
- Based on: Martin Fowler, *Refactoring* (2nd ed., 2018), "Replace Nested Conditional with Guard Clauses"; and Martin, *Clean Code* (1st ed.), ch. 3, "Blocks and indenting".

**CC-6** Leave no commented-out code and no comments that repeat the code; comments explain *why*, never *what*.
- Why: version control keeps old code; comments that repeat the code drift out of date and mislead.
- Based on: Martin, *Clean Code* (1st ed.), ch. 4, "Comments": redundant comments and commented-out code.

## 3. Clean architecture

<!-- Adapt: the project names below are an example. Keep the direction of the references. -->

**ARCH-1** Keep four layers, each in its own project (`Domain`, `Application`, `Infrastructure` and the entry point, for example `Api`), with project references pointing only inward: `Api` → `Application` → `Domain`, and `Infrastructure` → `Application`. The entry point may also reference `Infrastructure`, but only to wire it in the composition root (DI-5). `Domain` references no other project and no framework package (no EF Core, no ASP.NET Core).
- Why: the business rules stay independent of the database, the web framework and the UI, so those can change without touching them.
- Based on: the Dependency Rule, "source code dependencies must point only inward, toward higher-level policies". Robert C. Martin, *Clean Architecture* (2017), ch. 22; the composition-root exception is his "Main" component, the dirtiest one, which depends on everything (ch. 26).

**ARCH-2** Never mention a name from an outer layer in an inner one: when `Application` needs something from outside (database, email, payments), it declares the interface and `Infrastructure` implements it.
- Why: this is how the flow of control can go outward while the source code dependencies still point inward.
- Based on: Martin, *Clean Architecture*, ch. 22 ("Crossing boundaries") and ch. 11 (Dependency Inversion Principle).

**ARCH-3** Never pass domain entities or database types across the API boundary: endpoints receive and return their own request and response types (`record`).
- Why: exposing entities ties the public contract to the internal model, and leaks fields that were never meant to be public.
- Based on: Martin, *Clean Architecture*, ch. 22 ("Which data crosses the boundaries").

**ARCH-4** Check ARCH-1 and ARCH-2 with architecture tests that run in `dotnet test`, so a forbidden reference fails the build.
- Why: a rule that nobody checks erodes one shortcut at a time; a failing test stops the shortcut, including one taken by an agent.
- Based on: making Martin's Dependency Rule (ch. 22) checkable. Libraries: ArchUnitNET or NetArchTest.

## 4. Dependency injection

**DI-1** Receive everything external through the constructor: database, email, payments, file storage, HTTP calls, the clock and random values. Never create them with `new` inside the class that uses them.
- Why: whatever is injected can be replaced by a fake in tests; whatever is created inside is hidden and fixed.
- Based on: Microsoft Learn, *Dependency injection guidelines* (.NET): "Avoid direct instantiation of dependent classes within services".

**DI-2** Never read the clock or generate random values directly in `Domain` or `Application` (`DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, `new Random()`, `Random.Shared`): inject `TimeProvider` for time and an interface of your own for randomness.
- Why: code that reads the real clock cannot be tested for "overdue tomorrow" or "expired last year"; with `TimeProvider` the test sets the time (`FakeTimeProvider`).
- Based on: Microsoft Learn, `TimeProvider` class (.NET 8 and later) and its test double `FakeTimeProvider`.

**DI-3** Never create an `HttpClient` per call: use `IHttpClientFactory` (typed clients) or a single long-lived client with `PooledConnectionLifetime` set.
- Why: each new client opens new connections, and under load the operating system can run out of ports.
- Based on: Microsoft Learn, *HttpClient guidelines for .NET*.

**DI-4** Do not use the service locator: never inject `IServiceProvider` or call `GetService`/`GetRequiredService` outside the composition root, and never keep the container in a static field.
- Why: a class that fetches its own dependencies hides them from its constructor, so neither the reader nor the tests can see what it needs.
- Based on: Microsoft Learn, *Dependency injection guidelines* (.NET): "Avoid using the service locator pattern" and "Avoid static access to services".

**DI-5** Wire everything in one place, the composition root (`Program.cs`, or one `AddXxx` extension method per layer called from it).
- Why: the rest of the code stays free of the container; how the system is assembled can be read in a single place.
- Based on: Martin, *Clean Architecture*, ch. 26 ("The Main Component": dependencies are injected in Main and distributed from there).

**DI-6** Never let a longer-lived service hold a shorter-lived one (a singleton must not depend on a scoped service such as a `DbContext`), and keep scope validation on.
- Why: the singleton keeps the scoped service alive for the whole application, sharing state between requests (a "captive dependency").
- Based on: Microsoft Learn, *Dependency injection guidelines* (.NET): "Captive dependency" (term by Mark Seemann) and scope validation.

## 5. Rich domain

**DOM-1** Put business rules inside the entities: change state only through methods named after domain actions (`booking.Cancel()`, `booking.CheckIn()`), with private setters. Services never set an entity's properties to apply a rule.
- Why: if the rules live in services, any service can forget or contradict them; inside the entity they are applied every time.
- Based on: Martin Fowler, "AnemicDomainModel" (martinfowler.com, 2003); Vaughn Vernon, *Implementing Domain-Driven Design* (2013), ch. 1 ("Anemia and Memory Loss").

**DOM-2** Never let an entity exist in an invalid state: constructors and factory methods check the invariants, and every method rejects a change that would break one.
- Why: if an object can be invalid, every piece of code that uses it has to check it again, and some will not.
- Based on: Eric Evans, *Domain-Driven Design* (2003), ch. 6 (Aggregates: invariants are maintained at every change); Vernon, *Implementing Domain-Driven Design*, ch. 10 ("Model true invariants in consistency boundaries").

**DOM-3** Model concepts defined by their value (money, date range, ISBN, email address) as immutable value objects (`record`) that validate themselves, instead of passing raw `string`, `int` or `decimal`.
- Why: a raw `string` accepts any text; an `Isbn` type can only hold a valid ISBN, and the check lives in one place.
- Based on: Evans, *Domain-Driven Design*, ch. 5 (Value Objects, designed as immutable); Fowler, *Refactoring* (2nd ed.): "Primitive Obsession" and "Replace Primitive with Object".

**DOM-4** Group entities that must stay consistent together into an aggregate: code outside it holds references only to the root, other aggregates are referenced by ID, and a single transaction changes a single aggregate.
- Why: the root is the only door in, so it can enforce every invariant; small aggregates linked by ID keep transactions short and avoid loading half the database.
- Based on: Evans, *Domain-Driven Design*, ch. 6 (Aggregates); Vernon, *Implementing Domain-Driven Design*, ch. 10 ("Design small aggregates", "Reference other aggregates by identity").

**DOM-5** Keep application services thin: they load the aggregate, call a domain method, save and return. A domain service is only for a rule that does not belong to any single entity.
- Why: logic that slips into services leaves the entities as empty data holders, which is the anemic model of DOM-1.
- Based on: Evans, *Domain-Driven Design*, ch. 5 (Services "should be used judiciously and not allowed to strip the entities and value objects of all their behavior"); Vernon, *Implementing Domain-Driven Design*, ch. 7 and ch. 14.

## 6. Separation of concerns

**SEP-1** Keep endpoints, controllers and view models free of business rules: they translate the input, call the application layer and translate the result.
- Why: logic in the presentation layer is hard to test and gets duplicated when a second entry point (another endpoint, a job, a UI) needs it.
- Based on: Robert C. Martin, *Clean Architecture* (2017), ch. 22 (interface adapters) and ch. 23 ("Presenters and Humble Objects").

**SEP-2** Make each method either change state or return data, never both (queries have no side effects). The one exception: a command that creates something may return what it created (its ID and the values it stored), and nothing else.
- Why: a caller can then ask a question as often as it likes without changing anything by accident. Without the exception, the caller could not find what it has just created.
- Based on: Command-Query Separation (Bertrand Meyer); Martin, *Clean Code* (1st ed.), ch. 3, "Command query separation"; Fowler, *Refactoring* (2nd ed.): "Separate Query from Modifier". The exception is our own working practice.

**SEP-3** Keep every text shown to users out of the code: put it in resource files (`.resx`, one per language) and read it through `IStringLocalizer<T>`.
- Why: wording can change, and languages can be added, without touching or recompiling the logic.
- Based on: Microsoft Learn, *Globalization and localization in ASP.NET Core*.

---

## 7. Testing

**TEST-1** Ship every change of behaviour with a test. To fix a bug, first write a test that reproduces it and fails, then make it pass.
- Why: the test proves the change works and keeps the bug from coming back; seeing it fail first proves it really catches the bug.
- Based on: Martin, *Clean Code* (1st ed.), ch. 9 ("The three laws of TDD"; F.I.R.S.T.: tests are written in time, before the production code); Microsoft Learn, *Best practices for writing unit tests* (.NET): "Protection against regression".

**TEST-2** Write each test as Arrange, Act, Assert, with a single Act and no logic (`if`, loops); use `[Theory]` for several inputs. Name it `Method_Scenario_ExpectedResult` (for example `Cancel_LessThan24HoursBefore_ChargesPenalty`).
- Why: a test must be readable at a glance and obviously correct; when it fails, its name alone says which behaviour broke.
- Based on: Microsoft Learn, *Best practices for writing unit tests*: "Follow test naming standards", "Arrange your tests", "Avoid multiple Act tasks", "Avoid coding logic in unit tests".

**TEST-3** Keep unit tests fast, independent and repeatable: no real clock, network, file system or shared database, and no order between tests. Control time with `FakeTimeProvider` (see DI-2).
- Why: a test that depends on the time of day or on another test passes or fails at random, and then nobody trusts the suite.
- Based on: Martin, *Clean Code*, ch. 9 (F.I.R.S.T.: Fast, Independent, Repeatable, Self-validating, Timely); Microsoft Learn, *Best practices for writing unit tests*: "Avoid infrastructure dependencies", "Handle stub static references with seams".

**TEST-4** Test behaviour through the public API: never test private methods directly and never open up members (`public`, `internal`) only so a test can reach them.
- Why: tests tied to the inside of a class break on every refactoring even when the behaviour is still correct.
- Based on: Microsoft Learn, *Best practices for writing unit tests*: "Validate private methods with public methods".

**TEST-5** Cover each API endpoint with integration tests on `WebApplicationFactory<Program>`, replacing only what is external (database, outside services) and using SQLite in memory for the database, not the EF Core in-memory provider.
- Why: unit tests do not prove that routing, DI, serialization and persistence work together; the SQLite provider behaves like a real relational database.
- Based on: Microsoft Learn, *Integration tests in ASP.NET Core* (`WebApplicationFactory`, `ConfigureTestServices`, SQLite recommended for in-memory testing).

## 8. Static analysis

**SA-1** Keep nullable reference types on (`<Nullable>enable</Nullable>`) and do not use the null-forgiving operator `!` to silence a warning: add a check or fix the type.
- Why: each nullable warning is a possible `NullReferenceException`; each `!` is a place where the compiler can no longer protect you.
- Based on: Microsoft Learn, *Nullable reference types* (C#): "Use `!` sparingly".

**SA-2** Build with warnings as errors and with the .NET analyzers raised above the default (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<AnalysisLevel>latest-Recommended</AnalysisLevel>`), set once for the whole solution in `Directory.Build.props`.
- Why: a warning that does not stop the build gets ignored and piles up; one shared setting keeps every project to the same standard.
- Based on: Microsoft Learn, *Code analysis in .NET* ("Enable additional rules", "Treat warnings as errors").

**SA-3** Enforce the code style of `.editorconfig` in the build (`<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`), with the important style rules set to `warning`.
- Why: by default style is only checked inside the IDE; in the build it is checked for every author, human or agent.
- Based on: Microsoft Learn, *Code analysis in .NET* ("Code-style analysis", "Enable on build").

**SA-4** Never suppress a warning without a written reason: use `[SuppressMessage]` with `Justification`, or a comment next to `#pragma warning disable`, scoped to the smallest possible piece of code. Never disable a rule for the whole project just to make the build pass.
- Why: an unexplained suppression hides a real problem, and the next reader cannot tell whether it is still safe.
- Based on: Microsoft Learn, *Suppress code analysis warnings* (`SuppressMessageAttribute` and its `Justification` property).

---

## 9. Security

**SEC-1** Never put secrets (passwords, connection strings with credentials, API keys) in code, in `appsettings*.json` or anywhere in the repository: use User Secrets in development and environment variables or a secret store in production.
- Why: anything committed stays in the history and reaches everyone who can read the repository, even after it is deleted.
- Based on: Microsoft Learn, *Safe storage of app secrets in development in ASP.NET Core* ("Never store passwords or other sensitive data in source code or configuration files").

**SEC-2** Query the database through LINQ or the parameterized `FromSql`/`ExecuteSql`; never build SQL by joining strings with input, and never pass input to `FromSqlRaw` or `ExecuteSqlRaw`.
- Why: a value joined into the SQL text can end the statement and run another one (SQL injection); a parameter is always treated as data.
- Based on: OWASP Top 10:2025, A05 Injection; Microsoft Learn, *SQL Queries* (EF Core): "Passing parameters".

**SEC-3** Validate every input on the server, at the boundary, before using it: accept only the expected type, format, length and range (allowlist), check that it makes sense for the business, and reject anything else. Never "clean" bad input instead of rejecting it.
- Why: what the client checks can be skipped; only the server is under your control. Validation does not replace SEC-2: it is a second layer.
- Based on: OWASP *Input Validation Cheat Sheet* (allowlist, syntactic and semantic validation, server side).

**SEC-4** Require authorization by default: set a fallback policy that requires an authenticated user, mark as anonymous only the endpoints that are public on purpose, and check that the user may act on the specific resource requested, not just that they are logged in.
- Why: an endpoint that someone forgets to protect is open; with a default policy, forgetting closes it instead. Being logged in does not mean being allowed to cancel another user's booking.
- Based on: OWASP Top 10:2025, A01 Broken Access Control; Microsoft Learn, *Create an ASP.NET Core app with user data protected by authorization* ("Require authenticated users": fallback policy and `[AllowAnonymous]`).

**SEC-5** Never send internal error details to the client: the developer exception page only in `Development`; elsewhere `UseExceptionHandler` with `AddProblemDetails`, so the client gets a standard problem response without stack traces or exception messages.
- Why: stack traces and exception messages reveal code, libraries and data that help an attacker.
- Based on: OWASP Top 10:2025, A10 Mishandling of Exceptional Conditions; Microsoft Learn, *Handle errors in ASP.NET Core* ("Do not serve sensitive error information to clients").

**SEC-6** Never write to logs passwords, access tokens, session IDs, keys or sensitive personal data; log identifiers and events instead, and mask whatever must appear.
- Why: logs are read by more people and kept longer than the data they come from, so a secret in a log is a leaked secret.
- Based on: OWASP *Logging Cheat Sheet* ("Data to exclude"); OWASP Top 10:2025, A09 Security Logging & Alerting Failures.

**SEC-7** Never leave a package with a known vulnerability: keep NuGet Audit on (it runs on every restore), treat its warnings `NU1901`-`NU1904` as work to fix before the change is done, and check with `dotnet list package --vulnerable --include-transitive`. Suppress an advisory only with `NuGetAuditSuppress` and a written reason (see SA-4).
- Why: most of the code that runs is in your dependencies, and a known vulnerability is the first one attackers try.
- Based on: OWASP Top 10:2025, A03 Software Supply Chain Failures; Microsoft Learn, *Auditing package dependencies for security vulnerabilities* (NuGet).

---

## 10. Logging

**LOG-1** Log only through an `ILogger<T>` received by constructor injection, where `T` is the class that writes the log; never through `Console.WriteLine`, `Debug.WriteLine` or a logger factory created by hand.
- Why: the host decides where logs go, at what level and with what filters; the category (the class name) tells you which code wrote each line.
- Based on: Microsoft Learn, *Logging in C#* ("Integration with hosts and dependency injection" and "Log category").

**LOG-2** Write log messages as constant templates with named placeholders in PascalCase (`"Booking {BookingId} confirmed for {StartTime}"`); never build them with string interpolation or concatenation. Raise rule CA2254 to `warning` in `.editorconfig` so the build catches it.
- Why: with a template, each value is stored as a separate field that can be searched and filtered (structured logging); an interpolated string turns everything into plain text and is built even when the level is off.
- Based on: Microsoft Learn, *Logging in C#* ("Log message template"); *CA2254: Template should be a static expression*.

**LOG-3** Declare each log message once, as a `partial` method marked with `[LoggerMessage]` (level, message and, where useful, event ID), and call that method instead of `LogInformation`, `LogWarning` and the like.
- Why: the message is generated and checked at compile time, values are not boxed, and the messages of a class are listed in one place instead of being spread as strings through the code.
- Based on: Microsoft Learn, *High-performance logging* (source generation with `LoggerMessageAttribute`).

**LOG-4** Choose the level by what happened: `Information` for the normal flow, `Warning` for something unexpected the operation survives, `Error` for a failure of the current operation, `Critical` for one that needs immediate attention; `Debug` and `Trace` only for diagnosis. When logging an exception, pass the exception object itself, not just `ex.Message`. What must never be logged is in SEC-6.
- Why: levels are what lets production keep only what matters; the exception object keeps its type and stack trace, which the message alone loses.
- Based on: Microsoft Learn, *Logging in C#* ("Log level" table and "Log exceptions").

---

## 11. Asynchronous code

**ASYNC-1** Use `await` all the way up: never block on a task with `.Result`, `.Wait()` or `GetAwaiter().GetResult()`, never use `Thread.Sleep` (use `await Task.Delay`), and never write `async void` except for event handlers.
- Why: blocking a thread while it waits for a task can deadlock and wastes a thread the server needs for other requests; an exception thrown in `async void` cannot be caught by the caller.
- Based on: Microsoft Learn, *Asynchronous programming scenarios* ("Yield for tasks in a nonblocking manner", "Return 'async void' only from event handlers" and "Synchronous access to asynchronous operations").

**ASYNC-2** Give every asynchronous method that does I/O a last parameter `CancellationToken cancellationToken`, and pass it on to every call that accepts one; endpoints take it as a parameter so it comes from the request. Raise rule CA2016 to `warning` in `.editorconfig`.
- Why: when the client gives up, the work it started (queries, HTTP calls) stops too instead of running for nobody.
- Based on: Microsoft Learn, *Task-based Asynchronous Pattern (TAP)* ("Cancellation"); *CA2016: Forward the CancellationToken parameter to methods that take one*; *Parameter binding in Minimal API apps* (`CancellationToken` is bound to `HttpContext.RequestAborted`).

**ASYNC-3** End the name of every method that returns `Task`, `Task<T>`, `ValueTask` or `ValueTask<T>` with `Async`, and never add `Async` to a synchronous method. Endpoint handlers and test methods are the exception: no code of yours calls them, and test names follow TEST-2.
- Why: the caller sees from the name alone that it has to `await` the call; a method nobody calls has no caller to warn.
- Based on: Microsoft Learn, *Task-based Asynchronous Pattern (TAP)* ("Naming, parameters, and return types").

---

## 12. Working rules for the agent

These rules are about how you work, not about the code. They are our own practice; where a tool is involved, the
source is its documentation.

**WORK-1** The person decides product, money, technology (frameworks, packages, architecture) and anything that leaves this machine; you build and recommend. When there is a choice to make, show the options in a short table with one firm recommendation, then wait for the answer.
- Why: those decisions have consequences you cannot see from the code: cost, the team, the client.
- Based on: our own working practice.

**WORK-2** Change only what the task needs, inside this repository. Never touch secrets, `appsettings.Production.json`, generated files or a migration that has already been applied, unless the task asks for it.
- Why: a change outside the task is a change nobody asked for and nobody will review.
- Based on: our own working practice.

**WORK-3** Never add, remove or update a package without asking. Package versions live only in `Directory.Packages.props` (`ManagePackageVersionsCentrally` set to `true`); project files reference packages without `Version`.
- Why: every package is code you did not write and must keep up to date (see SEC-7); with central versions, all projects use the same version and an update is one line.
- Based on: Microsoft Learn, *Central Package Management* (NuGet).

**WORK-4** Put every external service (email, payments, third-party APIs) behind an interface (ARCH-2) with a fake implementation, and use the fake in development and in tests. Never call a real paid or external service without permission.
- Why: the code can be built, run and tested by anyone, at no cost and without side effects such as a real email being sent.
- Based on: ARCH-2, DI-1 and TEST-3 in this file.

**WORK-5** Ask before anything that cannot be undone or that leaves this machine: pushing, publishing, deleting, sending, paying or creating accounts. Each confirmation covers one action. If a permission is denied, do not look for another way to do the same thing.
- Why: these are the actions where a mistake cannot be fixed with another commit.
- Based on: our own working practice.

**WORK-6** Never read or print secret values: check that a secret exists without showing it, and never pass one on the command line (it stays in the shell history and in logs).
- Why: a secret you have read can end up in your output, a log or a commit (see SEC-1).
- Based on: our own working practice; SEC-1 in this file.

**WORK-7** A change is done only when all of these pass, run in this order from the repository root, and no test was deleted, skipped or weakened to get there:
  ```
  dotnet restore
  dotnet build -warnaserror
  dotnet test
  dotnet format --verify-no-changes
  dotnet list package --vulnerable --include-transitive
  ```
- Why: each command checks something different (compiles cleanly, behaves as before, follows the style, has no known vulnerable package), and together they can be run by anyone, so "done" means the same for everybody.
- Based on: Microsoft Learn, *dotnet format* (`--verify-no-changes` "terminates with a non zero exit code if any files would have been formatted"); SA-2, TEST-1 and SEC-7 in this file.

**WORK-8** When a test fails, fix the code, not the test. If you believe the test itself is wrong, stop and explain why instead of changing it.
- Why: a test changed to pass no longer protects anything, and the reason it failed is lost.
- Based on: our own working practice; TEST-1 in this file.

**WORK-9** Make small commits, one logical change each, with a message that explains why the change was made.
- Why: a small commit can be reviewed and reverted on its own; the code already shows what changed, only the message can say why.
- Based on: our own working practice.

**WORK-10** End every task with a short report: what you did, what is left, and how you checked it (the commands and their result). If something failed, say so and show the output; if something was not tested, say so and why; if something you said turns out to be false, correct it.
- Why: the person decides on the basis of your report, so a report that hides a failure leads to a wrong decision.
- Based on: our own working practice.
