# dotnet-agent-kit: clean .NET rules for AI coding agents

An `AGENTS.md` with 62 concrete, checkable rules for writing .NET code with an AI coding agent, a read-only skill
that reviews a change against those rules, and two documented checks of how far each one can be trusted.

Built with AI coding agents, designed and reviewed by robmardeveloper.

## Why this exists

When I worked with coding agents, my instructions were generic: "apply SOLID, use dependency injection, follow good
practices". They were too vague to check: I could not tell whether the agent had followed them, and neither could
the agent. So I wrote them down as rules that can be checked, built a reviewer that checks them, and then tested
both instead of assuming they work.

## What is in it

| Path | What it is |
|---|---|
| [`AGENTS.md`](AGENTS.md) | The rules, in 12 blocks: SOLID, clean code, clean architecture, dependency injection, rich domain, separation of concerns, testing, static analysis, security, logging, async code, and working rules for the agent. Each rule is one imperative line with an ID (`SEC-4`), a **Why** and a **Based on** (the book or official guidance it applies). |
| [`.claude/skills/dotnet-review-against-rules/`](.claude/skills/dotnet-review-against-rules/SKILL.md) | A Claude Code skill that reviews the changes since a given commit against `AGENTS.md`. It only reads: it reports every violation with file, line, rule, a proposed fix and the effort, and changes nothing. |
| [`sample/`](sample/) | `CourtBookings`, a small .NET 10 Minimal API for booking sports courts, written to follow the rules: domain, application, infrastructure and API layers, an injected clock, a fake email sender, unit, integration and architecture tests. It shows what the rules look like in real code. |
| [`docs/`](docs/) | The two checks below, with their design, raw reports and costs. |

## How to use it

1. Copy `AGENTS.md` to the root of your .NET repository and adapt it: remove what does not apply, change what your
   team does differently. Any agent that reads `AGENTS.md` will follow it (Claude Code, Copilot, Codex, Cursor,
   Gemini CLI and others).
2. With Claude Code, also copy `.claude/skills/dotnet-review-against-rules/`. Then review a change with:

   ```
   /dotnet-review-against-rules <base-commit>
   ```

   The skill runs only when you call it; the agent does not start it by itself.

To build and test the sample (needs the .NET 10 SDK):

```
cd sample
dotnet build -warnaserror
dotnet test
```

## What has been checked, and what has not

The rules come from recognised sources, but a source does not prove that a rule helps an agent. Two things were
checked, each designed and committed before it was run, and both reported as they came out:

**1. Does the reviewer find violations?** ([`docs/reviewer-check.md`](docs/reviewer-check.md))
A change was written with 14 violations planted on purpose and 5 traps (correct code that looks wrong). In the second
round, each of three reviews found all 14, at the right line, with no false alarm. The first round found a bug in the
reviewer itself (wrong line numbers), which was fixed and the whole check run again.

**2. Do the rules make an agent write better code?** ([`docs/rules-check.md`](docs/rules-check.md))
A small model (Claude Haiku 5.5) built the same API from an empty folder six times: three times without the rules,
three times with them. A reviewer that did not know which was which reviewed all six.
- All six worked: every behaviour case passed, all built without warnings and passed their tests.
- With the rules, every run had two security defaults that every run without them lacked: authentication required by
  default, and a central error handler. These are protections for future code, not holes that could be exploited
  today.
- With the rules, the agent worked about twice as long and cost 3 to 7 times more, still cents per task.

This is a narrow result: one task, one small model, three runs per side. It shows a consistent effect on that task,
not that the rules make code better in general, and nothing about larger models.

## Cost of the checks

The two checks cost about 9.5 US dollars in API usage in total (3.13 for the reviewer check, 6.38 for the rules
check), plus trial runs while designing them. The figures for each run are in the documents.

## License

[MIT](LICENSE).
