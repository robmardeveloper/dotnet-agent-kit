# AGENTS.md: clean .NET rules for AI coding agents

> **Template.** Copy this file to the root of your .NET repository and adapt it.
> Any agent that reads `AGENTS.md` will follow it (Claude Code, Copilot, Codex, Cursor, Gemini CLI and others).

## How to read these rules

- **Every rule applies to every change you make.** If a rule blocks you, stop and ask. Do not work around it.
- **Every rule has an ID** (for example `SOLID-1`). Reviews report violations by ID.
- **Every rule is one imperative line**, followed by **Why** (the reason) and **Based on** (the principle or book it
  applies). The rule is our own concrete, checkable application of that source, not a quotation from it.
- **These rules are recommendations** grounded in recognised sources. Whether they make an agent write better code is
  measured, not assumed: see `docs/experiment.md`.

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

**SOLID-4** Keep interfaces small and shaped by their consumers: no consumer depends on members it does not call.
- Why: a fat interface couples unrelated consumers and forces implementations to fake members they do not need.
- Based on: the Interface Segregation Principle. Martin, *Clean Architecture*, ch. 10.

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

**ARCH-1** Keep four layers, each in its own project (`Domain`, `Application`, `Infrastructure` and the entry point, for example `Api`), with project references pointing only inward: `Api` → `Application` → `Domain`, and `Infrastructure` → `Application`. `Domain` references no other project and no framework package (no EF Core, no ASP.NET Core).
- Why: the business rules stay independent of the database, the web framework and the UI, so those can change without touching them.
- Based on: the Dependency Rule, "source code dependencies must point only inward, toward higher-level policies". Robert C. Martin, *Clean Architecture* (2017), ch. 22.

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

**DOM-1** Put business rules inside the entities: change state only through methods named after domain actions (`loan.Renew()`, `loan.Return()`), with private setters. Services never set an entity's properties to apply a rule.
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

**SEP-2** Make each method either change state or return data, never both (queries have no side effects).
- Why: a caller can then ask a question as often as it likes without changing anything by accident.
- Based on: Command-Query Separation (Bertrand Meyer); Martin, *Clean Code* (1st ed.), ch. 3, "Command query separation"; Fowler, *Refactoring* (2nd ed.): "Separate Query from Modifier".

**SEP-3** Keep every text shown to users out of the code: put it in resource files (`.resx`, one per language) and read it through `IStringLocalizer<T>`.
- Why: wording can change, and languages can be added, without touching or recompiling the logic.
- Based on: Microsoft Learn, *Globalization and localization in ASP.NET Core*.

---

<!-- Next blocks, in review order:
     7. Testing   8. Static analysis   9. Security (OWASP)
     10. Working rules for the agent (who decides, non-negotiables, definition of done, honest reporting) -->
