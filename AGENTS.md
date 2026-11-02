# AGENTS.md: clean .NET rules for AI coding agents

> **Template.** Copy this file to the root of your .NET repository and adapt it.
> Any agent that reads `AGENTS.md` will follow it (Claude Code, Copilot, Codex, Cursor, Gemini CLI and others).

## How to read these rules

- **Every rule applies to every change you make.** If a rule blocks you, stop and ask. Do not work around it.
- **Every rule has an ID** (for example `SOLID-1`). Reviews report violations by ID.
- **Every rule is one imperative line**, followed by **Why** (the reason) and **Based on** (the principle or book it
  applies). The rule is our own concrete, checkable application of that source, not a quotation from it.
- **Every rule has a tag:**
  - `[Applied]`: applied in practice in a real .NET project built with several AI agents.
  - `[Recommended]`: considered good practice, but not yet proven in that project.

---

## 1. SOLID

**SOLID-1** `[Applied]` Give each class a single actor to answer to: never mix business rules with persistence, HTTP, files or email in the same class.
- Why: when code serving two actors lives together, a change requested by one breaks the other, and the class cannot be tested without both.
- Based on: the Single Responsibility Principle, "a module should be responsible to one, and only one, actor". Robert C. Martin, *Clean Architecture* (2017), ch. 7.

**SOLID-2** `[Applied]` Add new behaviour by adding a new type behind an existing abstraction (strategy, handler), not by adding another branch to an existing `switch` or `if` chain on a type or kind.
- Why: each new branch edits and risks code that already works; a new type leaves it untouched.
- Based on: the Open-Closed Principle, coined by Bertrand Meyer (1988). Martin, *Clean Architecture*, ch. 8; and Martin, *Clean Code* (1st ed., 2008), heuristic G23, "Prefer polymorphism to if/else or switch/case".

**SOLID-3** `[Applied]` Make every implementation usable through its abstraction without the caller knowing which one it got: never throw `NotImplementedException` or `NotSupportedException` for an interface member, and never make an override demand more or guarantee less than its base.
- Why: if callers must check the concrete type, the abstraction is a lie and every caller pays for it.
- Based on: the Liskov Substitution Principle, Barbara Liskov, "Data Abstraction and Hierarchy" (1988). Martin, *Clean Architecture*, ch. 9. The "demand more, guarantee less" test comes from Bertrand Meyer's Design by Contract.

**SOLID-4** `[Applied]` Keep interfaces small and shaped by their consumers: no consumer depends on members it does not call.
- Why: a fat interface couples unrelated consumers and forces implementations to fake members they do not need.
- Based on: the Interface Segregation Principle. Martin, *Clean Architecture*, ch. 10.

**SOLID-5** `[Applied]` Make source code depend on abstractions, never on volatile concrete classes; stable platform types (`string`, `DateOnly`, collections) are the exception.
- Why: concrete code under active development changes often; interfaces change rarely, so depending on them keeps changes local.
- Based on: the Dependency Inversion Principle. Martin, *Clean Architecture*, ch. 11. How the concrete classes get wired in is covered in block 4, Dependency injection.

## 2. Clean code

**CC-1** `[Applied]` Use names that state intent: no unexplained abbreviations, no vague names (`Data`, `Info`, `Manager`, `Helper`, `Temp`), and booleans phrased as questions (`IsOverdue`, `HasReservations`).
- Why: code is read far more often than it is written; a good name removes the need for a comment.
- Based on: Martin, *Clean Code* (1st ed.), ch. 2, "Meaningful Names".

**CC-2** `[Applied]` Keep methods short and at a single level of abstraction: aim for about 20 lines, and treat more than 3 parameters as needing a reason.
- Why: a short method that does one thing can be named, read and tested on its own.
- Based on: Martin, *Clean Code* (1st ed.), ch. 3, "Functions": small functions, one level of abstraction per function, and function arguments.

**CC-3** `[Applied]` Do not duplicate logic: if the same rule or calculation appears twice, extract it to one place.
- Why: duplicated logic gets fixed in one copy and stays wrong in the other.
- Based on: the DRY principle ("Don't Repeat Yourself"). Martin, *Clean Code* (1st ed.), ch. 3, and heuristic G5, "Duplication".

**CC-4** `[Applied]` Do not use magic numbers or strings: use named constants, enums or configuration options (`IOptions<T>`).
- Why: a name explains what the value means, and changing it happens in one place.
- Based on: Martin, *Clean Code* (1st ed.), heuristic G25, "Replace magic numbers with named constants".

**CC-5** `[Recommended]` Use guard clauses and early returns instead of nested conditionals; never indent more than two levels inside a method.
- Why: deep nesting hides the normal path of the method under its special cases.
- Based on: Martin Fowler, *Refactoring* (2nd ed., 2018), "Replace Nested Conditional with Guard Clauses"; and Martin, *Clean Code* (1st ed.), ch. 3, "Blocks and indenting".

**CC-6** `[Recommended]` Leave no commented-out code and no comments that repeat the code; comments explain *why*, never *what*.
- Why: version control keeps old code; comments that repeat the code drift out of date and mislead.
- Based on: Martin, *Clean Code* (1st ed.), ch. 4, "Comments": redundant comments and commented-out code.

---

<!-- Next blocks, in review order:
     3. Clean architecture   4. Dependency injection   5. Rich domain   6. Separation of concerns
     7. Testing              8. Static analysis        9. Security (OWASP)
     10. Working rules for the agent (who decides, non-negotiables, definition of done, honest reporting) -->
