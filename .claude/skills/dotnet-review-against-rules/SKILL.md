---
name: dotnet-review-against-rules
description: Read-only review of the changes since a given commit against the rules in AGENTS.md. Reports every violation with file, line, rule, proposal and effort, plus a per-rule table. Changes nothing.
argument-hint: <base-commit>
disable-model-invocation: true
context: fork
agent: Explore
---

# Review the changes against the rules

You are a reviewer. You **read and report; you never change anything**: no edits, no new files, no commits, no
`dotnet format` without `--verify-no-changes`, no package restore that rewrites files. Your report is the only output.

## What to review

- The rules: `AGENTS.md` at the repository root. Read it in full before reviewing. If it is missing, stop and say so:
  without the rules there is nothing to review against.
- The code: only what changed since the base commit **$ARGUMENTS**, including uncommitted changes.
  - List the files with `git diff --name-only $ARGUMENTS` plus `git status --porcelain` for new untracked files.
  - Read the diff with `git diff $ARGUMENTS`, then read **each changed file in full**, because a rule can be broken by
    how new code fits with the code around it.
  - Read unchanged files only to understand the changed ones (for example, the interface a new class implements).
    Do not report violations in code the change did not touch: that is not what is being reviewed.
- If no base commit was given, stop and ask for one.

## How to judge

1. Go through **every rule in AGENTS.md, in order**, and decide one status for each:
   - **Violated**: at least one place in the changed code breaks it. Count every place separately.
   - **Complies**: the change does something the rule is about, and does it right.
   - **Not applicable**: the change does nothing the rule is about (for example, SEC-2 when the change runs no query).
     "Not applicable" is not "complies": never count it as a success.
   - **Not reviewable**: the rule is about how the work was done, not about the code, and the diff cannot show it
     (asking before a decision, reporting at the end, not printing secrets in the session). Typically WORK-1, WORK-5,
     WORK-6 and WORK-10.
   - **Measured by commands**: the rule is checked by running the WORK-7 commands, not by reading: SA-2 (builds
     without warnings), SA-3 (style), SEC-7 (vulnerable packages) and WORK-7 itself. Always use this status for
     these four, whatever the change.
2. **Every violation needs evidence**: the file, the line and the code that breaks the rule. If you cannot point at a
   line, it is not a violation. A doubt is not a violation either: write it under "Doubts" with the reason.
3. Judge **what the rule says**, not what you would prefer. A good idea that no rule asks for is not a violation.
4. Do not run the WORK-7 commands to judge build, tests, style or vulnerable packages: those results are measured
   separately by running the commands. Judge from the code only what needs judgement (for example, SA-4: a suppressed
   warning without a reason; TEST-1: a behaviour change without a test; WORK-8: a test weakened to pass).
5. Order the findings by importance: first security (SEC) and anything that makes the behaviour wrong, then
   architecture (ARCH, DI, DOM), then the rest.

## Effort

- **Small**: a few lines in one place.
- **Medium**: several places or one file restructured, no design change.
- **Large**: a design change (a new type, a layer moved, an interface reshaped).

## Report

Write the report in exactly this shape, so that reports can be compared and counted.

```markdown
# Review against AGENTS.md

Base commit: <commit>. Files reviewed: <n>.

## Findings

| # | File | Line | Rule | What breaks it | Proposal | Effort |
|---|------|------|------|----------------|----------|--------|
| 1 | src/.../X.cs | 42 | SEC-4 | <the code and why it breaks the rule> | <the fix> | Small |

## Rules

| Rule | Status | Violations |
|------|--------|------------|
| SOLID-1 | Complies | 0 |
| SOLID-2 | Not applicable | 0 |
| ... every rule in AGENTS.md, in order ... | | |

Totals: <n> violations in <n> rules; <n> rules comply; <n> not applicable; <n> not reviewable; 4 measured by commands.

## Doubts

- <file:line>: <what you were unsure about and why it is not counted>.
```

If there are no findings, keep the "Findings" heading and write "None." under it. Never leave a rule out of the
"Rules" table.
