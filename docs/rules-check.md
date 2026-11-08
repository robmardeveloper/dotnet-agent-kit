# Do the rules make an agent write better code? A check from an empty folder

**Result: the decision rule was met, with a modest difference.** All six results work the same: 10 of 10 behaviour
cases and every WORK-7 command passing. In the objective score, each run with the rules scored 0 and each run
without them scored 2. The difference is the same two security defaults every time, not a hole that could be
exploited today. See [Results](#results).

**Status: design, written and committed before any run.** Results are added below this design without changing
it. If something in the design has to change after the first run, the change is written here with the reason and
the date, and the runs before it are reported as they were.

## The question

`AGENTS.md` claims to make a coding agent write safer, cleaner .NET code. [The reviewer check](reviewer-check.md)
showed that the reviewer finds violations of the rules. This check asks the other question: **with the rules in its
folder, does the agent make fewer of those violations than without them?**

The task starts from an empty folder, so the agent cannot copy good or bad habits from existing code: whatever it
writes comes from the task, the model and, on one side, the rules.

## The two sides

| | Side A (without the rules) | Side B (with the rules) |
|---|---|---|
| Starting folder | A new git repository with one empty commit, "Initial commit" | A new git repository with one commit holding `AGENTS.md` and `.claude/skills/` exactly as in this repository at the commit of this design |
| Task | The same text, word for word | The same text, word for word |
| Model, tools, permissions | The same | The same |

Claude Code reads `AGENTS.md` when there is no `CLAUDE.md` in the folder or above it (Claude Code docs, *How Claude
remembers your project*). So every run folder is under `C:\kit-experiment\`, outside any folder with a `CLAUDE.md`,
and there is no global `~/.claude/CLAUDE.md`. Each run gets its own repository, not a worktree, because auto memory is
shared across worktrees of a repository.

## The task

The text below is given to both sides unchanged. It fixes the contract, so that one program can check the behaviour
of every result, and it settles the decisions an agent with the rules would otherwise stop to ask about (WORK-1,
WORK-3): in a non-interactive session nobody can answer.

```text
Build a small library loans API in .NET 10, from this empty repository.

Members borrow books and give them back.

- The API is the project src/Library.Api and must start with:
  dotnet run --project src/Library.Api --urls http://localhost:5199
- Store the data in SQLite, in a file library.db in the current working directory. If the file does not exist,
  create it on startup with 20 books, IDs 1 to 20.
- Who is calling: the member ID comes in the X-Member-Id request header. Treat it as a stand-in for a real identity
  provider: read it in an ASP.NET Core authentication scheme, not in the endpoints. A request without the header, or
  with an empty one, gets 401.
- POST /loans with the body {"bookId": <int>} borrows a book for the caller. The loan lasts 14 days.
  - 201 with {"id", "bookId", "dueDate"}; dueDate is ISO 8601 with an offset.
  - 400 if bookId is missing or not a positive integer.
  - 404 if the book does not exist.
  - 409 if the book is already on loan, or if the caller already has 3 active loans.
- GET /loans: 200 with the caller's own active loans, in the same shape.
- POST /loans/{id}/return gives a loan back: 204. 404 if the loan does not exist or is not the caller's. 409 if it
  was already returned.
- All IDs are integers.
- Write tests.

Packages: you may use Microsoft.EntityFrameworkCore.Sqlite, Microsoft.EntityFrameworkCore.Design,
Microsoft.AspNetCore.Mvc.Testing, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio and coverlet.collector,
in their latest stable versions. No other package.

Nobody can answer questions during this task. When there is a decision to make, make it yourself and go on. At the
end, say what you decided and why, and what you checked.
```

## How it is run

1. **Order:** A, B, A, B, A, B, one at a time, each in a new session.
2. **Command** (Claude Code version and model fixed for all runs, written in the results):

   ```text
   claude -p "<task>" --model claude-haiku-5-5 --permission-mode acceptEdits
     --allowedTools "Bash(dotnet *)" "Bash(git *)" "PowerShell(dotnet *)" "PowerShell(git *)"
     --output-format stream-json --verbose < /dev/null
   ```

   Paid with an API key, so each session reports its cost.
3. **Dry runs first**, one per side, not counted: they check that the command works, that side B's session loaded
   `AGENTS.md`, and what one run costs. If one run costs more than about 1.5 US dollars, the check stops and the
   design is looked at again.
4. **Discarded runs:** a run is discarded and repeated only for a reason outside the agent (the session fails to
   start, the API returns an error, the machine runs out of disk). Every discarded run is listed with its reason. A
   run where the agent writes bad code, gives up or does not finish is a result, not a discard.

## What is measured

Each result is everything the agent left in its folder, compared with the starting commit: its commits, if it made
any, and what it left uncommitted.

### 1. Behaviour

[`rules-check/behaviour-check.cs`](rules-check/behaviour-check.cs) checks the contract with 10 cases: borrowing,
a book already on loan, the limit of 3, an unknown book, returning, returning someone else's loan, returning twice,
the list showing only the caller's active loans, the 401s and the 400s. Before any run it was checked against a
throwaway implementation of the contract (10 of 10 pass) and against a copy with two bugs put in on purpose (no owner
check, a limit of 4): exactly cases 3 and 6 fail.

For each result: delete `library.db`, start the API with the command in the task, run
`dotnet run behaviour-check.cs -- http://localhost:5199`, stop the API. If the API does not start, all 10 cases fail.

### 2. Build and tests

For each result, the WORK-7 commands are run from the repository root and each outcome is written down: restore,
build with `-warnaserror`, tests (how many, how many pass), `dotnet format --verify-no-changes`, vulnerable packages.
They are reported on their own and are not part of the score: on side A nobody asked for `-warnaserror` or for
`dotnet format`, so counting them would measure whether the agent knew about the rules, not whether its code is
better.

### 3. Rule violations, through the reviewer

Each result gets one review with the skill `dotnet-review-against-rules`, as in the reviewer check, with the same
model and the same read-only command. To hide which side a result comes from:

- Each review runs in a new repository. Its base commit holds `AGENTS.md` and `.claude/skills/` as in this
  repository (the reviewer needs them); the result is put on top, uncommitted. If the agent changed or deleted those
  files, they are put back to the originals in the review copy: the review is about the code, and the changes are
  reported apart.
- The review folders are named by a random letter, not by side.

The blinding has limits: a result written with the rules may look like it (rule IDs in comments, a
`Directory.Packages.props`). That is written down where it happens.

Every finding in every review is checked by hand against the rule text and the code, as in the reviewer check: a
real violation, or a false alarm. False alarms are removed and listed. Only the checked findings count.

## The score and the decision rule

The rules are split in two groups, fixed now:

- **The objective group:** rules whose violation is a defect whatever the coding style: SEC-1 to SEC-6, ASYNC-1,
  DI-3, DI-6 and SOLID-3. Each checked violation of one of these counts 1 (the same mistake repeated in several
  places counts once per rule and file). Each failed behaviour case also counts 1.
- **The convention group:** all other rules (naming, layout, logging style, tests style, packages and so on).
  These are reported per run, as they come out, but they do not decide anything: side B was told about these
  conventions and side A was not, so fewer of them on side B is expected and says little.

**The objective score** of a run is its objective violations plus its failed behaviour cases. Lower is better.

**Decision rule, fixed before the runs:** the check says the rules helped only if **every** side B run scores lower
than **every** side A run (the highest B below the lowest A). If the rules made no difference, the chance of that
with 3 runs per side is 1 in 20. Any other outcome is described as it is, with the numbers, and this repository
will not claim that the rules make the code better.

If a bug is found in the behaviour check or in the scoring after runs have started, it is fixed, every result is
scored again with the fixed version, and both the bug and the change are reported.

## Why this model

The runs use `claude-haiku-5-5`, a small, cheap model, on purpose. Rules are meant to help where the agent would
otherwise get things wrong; a strong model may write safe code with or without them, and then the check could not
tell. So the check asks the question where an answer is most useful: **does the kit make a cheaper model write
better code?**

This was decided before any run, and only this model is run. Trying another model after seeing the results would be
picking the one that gives the answer we want. The result says something about this model and this task only, not
about stronger models.

The reviews are done with `claude-opus-5-5`, the model that passed [the reviewer check](reviewer-check.md), so the
judge is the one that was checked and is the same for every result.

## Cost

Estimated before the runs, in US dollars:

| | Estimate |
|---|---|
| 2 dry runs and 6 runs with `claude-haiku-5-5` | 1 to 4 |
| 6 reviews with `claude-opus-5-5` | about 3.5 (the reviewer check cost 0.50 to 0.60 per review) |
| **Total** | **5 to 8**, stop at about 12 |

The real cost of each session is written in the results.

## Limits

- **Three runs per side.** Enough to see a clear difference, not to measure a small one.
- **One task.** A small web API with a database and authentication: it touches the security and async rules, not
  all of them (no `HttpClient`, so DI-3 can only be broken by writing one where none is needed).
- **The same person wrote the rules, the task, the behaviour check and the scoring**, and the reviewer is a model of
  the same family as the agent.
- **The blinding is partial** (see above).
- **The task text already asks for some good practice** (authentication in a scheme, status codes), on both sides.
  This makes the behaviour check possible, and it means the check measures what the rules add on top of a clear task.

## Changes to the design

- **2026-11-07, after the two dry runs, before any counted run: the behaviour check now sends integer member IDs.**
  It sent member IDs such as `c1`. The task says "All IDs are integers", so both dry runs accepted only positive
  integers in `X-Member-Id` and answered 401 to everything: 0 of 10 on both sides, a bug in the check, not in the
  results. The member IDs are now `101`, `201` and so on; nothing else changed. With the fix both dry runs pass 10
  of 10, the throwaway correct implementation still passes 10 of 10, and the broken one still fails exactly cases
  3 and 6. The task text did not change.
- **Discarded dry run:** the first attempt at dry run B was started by mistake in the folder above the run folders.
  It was stopped after three commands, all of them reads (it listed the folders and read the task file); it wrote
  nothing. It was run again in its own folder.

## Results

The six runs were made on 2026-11-07, in the order A1, B1, A2, B2, A3, B3, with Claude Code 2.1.319 and
`claude-haiku-5-5`. None was discarded.

### What can be measured by a machine: no difference

| | A1 | A2 | A3 | B1 | B2 | B3 |
|---|---|---|---|---|---|---|
| Behaviour cases passed (of 10) | 10 | 10 | 10 | 10 | 10 | 10 |
| `dotnet build -warnaserror` | pass | pass | pass | pass | pass | pass |
| Tests (all passing) | 37 | 33 | 38 | 45 | 60 | 40 |
| `dotnet format --verify-no-changes` | pass | pass | pass | pass | pass | pass |
| Vulnerable packages | none | none | none | none | none | none |

Every result does what the task asks. The task was clear enough for the small model to get it right on both
sides.

### The objective score: the rule is met

| | A1 | A2 | A3 | B1 | B2 | B3 |
|---|---|---|---|---|---|---|
| SEC-4: no fallback policy requiring an authenticated user | 1 | 1 | 1 | 0 | 0 | 0 |
| SEC-5: no `UseExceptionHandler` and `AddProblemDetails` | 1 | 1 | 1 | 0 | 0 | 0 |
| Other objective violations | 0 | 0 | 0 | 0 (see below) | 0 | 0 |
| Failed behaviour cases | 0 | 0 | 0 | 0 | 0 | 0 |
| **Objective score** | **2** | **2** | **2** | **0** | **0** | **0** |

The highest B score (0) is below the lowest A score (2), so the decision rule fixed before the runs is met.

**What the difference is, and what it is not.** Both violations were checked in the code of every result: the three
A results protect each endpoint by hand with `RequireAuthorization()` and have no exception handler; the three B
results set a fallback policy and use `UseExceptionHandler` with problem details. Neither is a hole that can be used
today: the behaviour check shows every endpoint answering 401 without a member, and outside `Development`
ASP.NET Core does not send error details to the client anyway. They are defaults that protect the code written
next: a new endpoint that forgets `RequireAuthorization()`, an error that should reach the client as a standard
problem response. So what this shows is: **with the rules, the small model put in those two defaults every time;
without them, never.** It does not show that the code without the rules is unsafe.

**Checked by hand.** Every finding in the objective group was checked against the rule text and the code. One was
counted as a false alarm: in B1, SEC-3 for the loan ID in `POST /loans/{id}/return` not being checked as positive.
The task asks for 404 when a loan does not exist, which is what a loan ID of 0 gets; the review of B2 came to the
same conclusion about the same code (it lists it under doubts), and the three A results have the same route without
it being reported. Counting it would give B1 a score of 1, still below 2, so the result is the same. The six results
were also searched for the patterns of the objective rules the reviews could have missed (`.Result`, `.Wait()`,
`GetAwaiter().GetResult()`, `Thread.Sleep`, `async void`, `NotImplementedException`, raw SQL, secrets in
configuration, `new HttpClient`, singletons holding a `DbContext`): none was found.

### The convention group: reported, does not decide

Violations reported by each review, as the reviewer gave them:

| | A1 | A2 | A3 | B1 | B2 | B3 |
|---|---|---|---|---|---|---|
| All violations in the report | 48 | 70 | 67 | 22 | 21 | 9 |
| Of them, in the objective group | 2 | 2 | 2 | 1 (false alarm) | 0 | 0 |

The biggest difference is the structure: the three B results split the code into Domain, Application,
Infrastructure and Api projects with architecture tests, and keep package versions in one place; the three A results
put everything in one project. The rest are rules A was never told about (test names, texts in resource files,
`CancellationToken` names, central package versions). As said in the design, side B was told these conventions
and side A was not, so this difference is expected and is not a result.

**Not checked one by one:** the design says every finding is checked by hand. That was done for the objective
group, which decides the result. The convention findings (about 240) are reported as the reviewer gave them,
without checking each one, because they do not decide anything. Read them as the reviewer's count, not as a
checked number.

### What the rules cost

| | A1 | A2 | A3 | B1 | B2 | B3 |
|---|---|---|---|---|---|---|
| Cost (US dollars) | 0.06 | 0.06 | 0.15 | 0.42 | 0.38 | 0.42 |
| Turns | 50 | 58 | 70 | 111 | 113 | 144 |
| Files written | 16 | 19 | 19 | 38 | 45 | 52 |
| Commits | 0 | 0 | 0 | 5 | 6 | 6 |

With the rules the model works much more: about twice the turns, 3 to 7 times the cost, two to three times the
files. On a small model this is still cents per task, but it is not free.

### Cost of the check

| | US dollars |
|---|---|
| Two dry runs (the stopped one reported no cost; it ran three read-only commands) | 0.56 |
| 6 runs with `claude-haiku-5-5` | 1.48 |
| 6 reviews with `claude-opus-5-5` | 4.34 |
| **Total** | **6.38** |

### Limits seen in the results

- **The blinding did not hide the side.** The B results have two to three times as many files, several projects
  and a `Directory.Packages.props`; a reviewer can tell. The reviewer was not told the side, and both counted
  violations are the kind a reader can check in a few lines of `Program.cs`.
- **The difference is narrow.** Two rules, the same two in every run. Three runs per side and one task: this shows a
  consistent effect on this task and this model, not a size of effect, and nothing about stronger models.

## Files

- [`rules-check/behaviour-check.cs`](rules-check/behaviour-check.cs): the behaviour cases, run unchanged on every
  result.
- [`rules-check/reports/`](rules-check/reports/): the six review reports, as they came out of the reviewer, named by
  run. The reviews themselves ran in folders named by random letters.
