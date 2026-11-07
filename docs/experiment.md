# The experiment: does AGENTS.md make an agent write better code?

**Status: designed, not run yet.** Everything below the "Results" heading is written after the runs; everything above
it was written and committed before them, so that the method cannot be bent to fit the results.

## The question

Given the same codebase and the same task, does an AI coding agent break fewer rules when it has `AGENTS.md` than
when it does not?

## What is compared

| | Run A | Run B |
|---|---|---|
| Code | the repository at commit `0a69b98` (`sample/` as frozen at `493299b`, plus the final `AGENTS.md` and the reviewer; not this file) | the same |
| `AGENTS.md` | removed | present |
| `.claude/skills/` | removed | present |
| Everything else | the same agent, model, version, permission mode and task text | the same |

Both runs see the same code, and that code was written following the rules (its comments even mention rule IDs).
So the experiment does **not** measure "rules against no example". It measures **what the rules file adds on top of
a well-built codebase that the agent can copy**. That is the realistic case, and it makes any difference harder to
get, not easier.

## The task

Both runs get exactly this text as their only message:

> Add a way for users to move one of their bookings to another start time, on the same court and with the same
> duration. It is only allowed if the new time is free and the booking starts in more than 24 hours. The new time
> must follow the same rules as a new booking.

The runs are non-interactive (see "How it is run"), so the agent cannot ask questions and nobody can help it: it has
to decide on its own and say what it decided. Neither run gets help the other did not.

The task is not in `sample/`, touches every layer, and has a trap that has nothing to do with the rules: the
booking being moved must not count as taking its own slot (moving 10:00-11:30 to 10:30-12:00 is allowed).

## What it measures, decided before the runs

Not every rule can be tested by one task. Each rule is in one of four groups, and the groups were fixed before the
runs. A rule the task does not touch is reported as "not applicable", never as "complies".

| Group | Rules | How it is counted |
|---|---|---|
| **The task exercises them** | SOLID-1, SOLID-4, SOLID-5; CC-1 to CC-6; ARCH-1, ARCH-2, ARCH-3; DI-1, DI-2, DI-5, DI-6; DOM-1 to DOM-5; SEP-1, SEP-2, SEP-3; TEST-1 to TEST-5; SA-1, SA-4; SEC-2, SEC-3, SEC-4, SEC-5, SEC-6; LOG-1 to LOG-4; ASYNC-1 to ASYNC-3; WORK-2, WORK-8 | Violations per rule, from the reviewer (see below). This is the main result. |
| **Only if the agent does something not asked for** | SOLID-2, SOLID-3, ARCH-4, DI-3, DI-4, SEC-1, WORK-3, WORK-4 | Counted if they appear, but the task gives no reason to touch them, so "not applicable" is the expected result. |
| **Measured by commands** | SA-2, SA-3, SEC-7, WORK-7 | The five WORK-7 commands, run by hand on each result: pass or fail for each. |
| **About how the agent works, not the code** | WORK-1, WORK-5, WORK-6, WORK-9, WORK-10 | Not counted. Described from the session transcripts (did it say what it decided, did it report what it checked). WORK-9 is here because the results are reviewed as uncommitted changes, so the reviewer cannot see the agent's commits. |

The task does not touch, for example, `HttpClient` (DI-3), secrets (SEC-1), packages (WORK-3) or external services
(WORK-4). Other tasks would be needed for those rules; that is left for a later version.

### Behaviour, checked by hand

Breaking no rule does not mean the feature works. Each result is also checked against these cases, by calling the
API. The cases are fixed now:

1. Moving to a free time, more than 24 hours before the booking starts: accepted.
2. Moving to a time taken by another booking: refused (conflict).
3. Moving when the booking starts in 24 hours or less: refused.
4. Moving someone else's booking: refused without revealing it exists (404).
5. Moving a cancelled booking: refused.
6. Moving to a time that breaks the booking rules (not on the half hour, in the past, more than 7 days ahead): refused (400).
7. Moving to a time that overlaps only the booking's own current slot: accepted.
8. After a move, the old time can be booked by someone else.
9. A request without a user: 401.

## How it is run

1. **Two fresh copies** of the repository at `0a69b98`, in folders **outside** any folder with a `CLAUDE.md`
   (`C:\kit-experiment\run-a` and `C:\kit-experiment\run-b`). This matters: when there is a `CLAUDE.md` in the
   working folder or above it, Claude Code reads that instead of `AGENTS.md` (Claude Code docs, *How Claude
   remembers your project*, "AGENTS.md"). Separate copies, not worktrees, because auto memory is shared across
   worktrees of a repository.
2. In run A, `AGENTS.md` and `.claude/` are deleted. Then each copy gets **a new git repository with one commit,
   "Initial commit"**, and no history and no remote. Without this, run A could read the rules in the history
   (`git log`, `git show`), and the agents could push to GitHub. `sample/` is checked to be identical in both.
3. **A dry run first.** In each clone, a short non-interactive session asks the agent which instruction files it has
   loaded and to run `dotnet build`. In run B it must name `AGENTS.md`; in run A, no instruction file. This checks the
   setup without spending a real run.
4. **The runs go one after the other**, on the same day, with the same Claude Code version (checked before each run)
   and the same model. Each one is a new non-interactive session started in the clone's root:

   ```text
   claude -p "<the task text>" --model claude-opus-5-5 --permission-mode acceptEdits
     --allowedTools "Bash(dotnet *)" "Bash(git *)" "PowerShell(dotnet *)" "PowerShell(git *)"
     --output-format stream-json --verbose < /dev/null
   ```

   The `PowerShell(...)` rules were added after the first dry run: on Windows the agent runs commands with the
   PowerShell tool, so with only the `Bash(...)` rules `dotnet build` was denied.

   The agent can read and edit files and run `dotnet` and `git` commands; anything else is denied without asking. A
   denied attempt (for example, installing a tool) is not a failure of the run: it is recorded, and it is a result.

   The sessions use an API key (set as `ANTHROPIC_API_KEY` only for these sessions, never written in the repository)
   instead of a subscription, so that each session reports its exact cost.
5. Recorded for each run: the full session output (`stream-json`, which is the transcript), the Claude Code
   version, the model, the tokens and cost reported at the end, and the final diff (`git add -A` and
   `git diff --cached` against the copy's "Initial commit"). If the agent made commits, the diff includes them.
6. **A run that fails because of the setup** (a wrong path, a missing permission, a command that does not start) is
   not a result. It is thrown away, the setup is fixed, and the run is repeated from a new clone. Every thrown-away
   attempt and its reason are written in the results. A run where the agent simply does the task badly is never
   thrown away.

## How it is judged

1. Each result's diff is applied to a **new clone at `0a69b98`**, which has `AGENTS.md` and the reviewer, and left
   uncommitted (the reviewer reviews uncommitted changes). Both results then look the same to the reviewer, with no
   commit history to tell them apart, and the clones are named `review-1` and `review-2` so the name does not say
   which run is which. Which is which is written down only after judging.
2. The reviewer skill `dotnet-review-against-rules` is run **twice on each result**, with base `0a69b98`, each time
   in a new non-interactive session (`claude -p "/dotnet-review-against-rules 0a69b98"`, same model and key as the
   runs). After each review, `git status` and `git diff` must be the same as before it. The reviewer is read-only,
   needs a line as evidence for every violation and gives every rule a status (see its `SKILL.md`).
3. **A violation counts only if both reviews report it** (same rule, same place). One that appears in only one review
   is checked by hand against the rule text, and the decision and its reason are written in the results.
4. The WORK-7 commands and the behaviour cases are run by hand on each result.
5. The reviewer's reports are kept as they came out, next to this file, so anyone can check the counting.

## Cost

Running agents costs money even when nothing is paid on top: here it is paid from API credit. The work is two short
dry runs, two task runs and four reviews (one review took about 90K tokens when the reviewer was tried). Before the
runs the total was estimated at 10 to 20 US dollars at the API price of the model; the measured cost of each session
is reported in the results.

## Limits, said up front

- **One run per condition.** An agent does not do the same thing twice, so one run of each is an example, not a
  statistic. A difference of one or two violations can be chance.
- **The reviewer is also a model.** It can miss violations or see ones that are not there; that is why there are two
  reviews per result, a line as evidence for each violation, and a manual check of every disagreement.
- **One task exercises 45 of the 62 rules.** The rest are not tested here, and that is shown in the table,
  not hidden.
- **Same author.** The rules, the example, the task and the reviewer were all written by the same person with the
  same agent; the method above is what keeps that from deciding the result.

If the rules do not make a difference, or make it worse, that is the result and it is published as it is.

## Results

*Not run yet.*
