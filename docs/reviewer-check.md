# Does the reviewer find the violations? A check with planted mistakes

**Result: yes, after one fix.** In the second round, each of the three reviews found all 14 planted violations, at
the right line, with no false alarm. The first round found a real bug in the reviewer (wrong line numbers); it was
fixed in the skill and the whole check was run again. Both rounds are reported here as they came out.

## The question

The skill [`dotnet-review-against-rules`](../.claude/skills/dotnet-review-against-rules/SKILL.md) reviews a change
against the rules in `AGENTS.md`. Before trusting its reports, it has to be shown that it finds violations that are
really there, at the right place, and does not report ones that are not.

The only way to know the right answer in advance is to write the mistakes on purpose. So a change was written with
14 known violations and 5 traps, the answers were written down, and the reviewer was run on it three times.

## The planted change

On top of commit `0a69b98`, a new feature was added to `sample/`: `GET /users/{userId}/bookings`, "list my upcoming
bookings". It touches every layer: endpoint, handler, domain, repository, configuration and tests (12 files). The full
change is in [`reviewer-check/seeded.diff`](reviewer-check/seeded.diff).

It contains:

- **14 violations** of 14 different rules: security (the user ID taken from the route, SQL built by joining strings,
  a password in `appsettings.json`, an email address in the logs), architecture (a business rule in the endpoint, the
  real clock read in the domain), async (`.Result`, no `CancellationToken`), logging (`Console.WriteLine`), code
  (a magic number, commented-out code, `!` on a possible null), a fake that throws `NotImplementedException`, and a
  test named `Test1` with a loop in it. The password is made up.
- **5 decoys**: correct code that looks like a violation, to catch a reviewer that reports anything suspicious. For
  example, logging the user ID (allowed; the email is not), or an endpoint with six parameters that the framework
  fills in.

## The answer key and how it is scored

The [answer key](reviewer-check/answer-key.md) was written before the first review and kept outside the folders the
reviews could read. It gives, for each violation, the expected rule, the rules also accepted, the file and the line.

- A violation is **found** when a review reports it as a violation (not as a doubt) with the expected or an accepted
  rule, at a line within 3 of the one in the key.
- Reporting a decoy as a violation is a **false alarm**. Any other finding is checked by hand against the rule text:
  real, or a false alarm.
- **Pass:** in each of the three reviews, at least 12 of the 14 violations found and at most 1 false alarm.

## How it is run

1. Each review gets **its own new repository** that contains only commit `0a69b98` and its history (fetched alone,
   so no later commit is there), with no remote. The planted change is applied on top and left uncommitted. The
   folders are outside any folder with a `CLAUDE.md`.
2. Each review is a new non-interactive session, one after the other:

   ```text
   claude -p "/dotnet-review-against-rules <base>" --model claude-opus-5-5
     --allowedTools "Bash(git *)" "PowerShell(git *)" --output-format stream-json --verbose < /dev/null
   ```

   Claude Code 2.1.319, paid with an API key so each session reports its cost.
3. After each review, `git status` and `git diff` must be the same as before it (the reviewer is read-only). The
   session transcripts are checked for reads outside the review's own folder.

## Round 1: a bug in the reviewer

| | Review 1 | Review 2 | Review 3 |
|---|---|---|---|
| Violations found (of 14) | 14 | **10** | 14 |
| False alarms | 0 | 0 | 0 |
| Passes | yes | **no** | yes |
| Cost (US dollars) | 0.53 | 0.43 | 0.49 |

Review 2 described all 14 violations correctly, with the right code quoted, but gave the wrong line for 4 of them: for
example, line 182 of `EfBookingRepository.cs`, a file with 37 lines. The transcript shows why. It numbered the files
with `cat -n a.cs b.cs c.cs`, which keeps counting from one file into the next: 22 + 76 + 84 = 182, the real line plus
the lengths of the two files before it. Under the rule fixed before the check, those 4 do not count, so round 1 fails.

The scoring rule was not changed to let it pass. A report that sends you to line 182 of a 37-line file is wrong for
anyone using the kit, so it was treated as a bug in the reviewer.

**The fix** (commit `4bc6d00`), added to the skill's rule that every violation needs evidence:

> The line is the line number **in that file**, counted from 1. Number each file on its own (one file per read, or
> one `cat -n` per file): `cat -n a.cs b.cs` keeps counting from one file into the next and gives wrong lines.
> Before writing the report, check that every line you cite holds the code you quote.

## Round 2: after the fix

The same check from the start: three new repositories, the same planted change, the same key and the same pass rule.
The only difference is the fixed skill, committed in each repository on top of `0a69b98` and used as the review base,
so that the change under review is exactly the same 12 files as in round 1.

| | Review 1 | Review 2 | Review 3 |
|---|---|---|---|
| Violations found (of 14) | **14** | **14** | **14** |
| False alarms | 0 | 0 | 0 |
| Passes | **yes** | **yes** | **yes** |
| Cost (US dollars) | 0.58 | 0.50 | 0.60 |

Every line cited was checked against the files: all correct. All three reviews read each file on its own.

### Each violation, in each review

✓ found; ✗ not found under the scoring rule.

| # | Rule | What was planted | R1·1 | R1·2 | R1·3 | R2·1 | R2·2 | R2·3 |
|---|---|---|---|---|---|---|---|---|
| 1 | SEC-4 | User ID from the route: anyone can list anyone's bookings | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 2 | SEP-1 | "Can cancel for free" (24 hours) decided in the endpoint | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 3 | DI-2 | `DateTimeOffset.UtcNow` in the domain | ✓ | ✗ line | ✓ | ✓ | ✓ | ✓ |
| 4 | CC-4 | Bare `7` as the default number of days | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 5 | ASYNC-1 | `.Result` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 6 | ASYNC-2 | New I/O method without `CancellationToken` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 7 | SEC-2 | SQL built by joining the user ID, in `FromSqlRaw` | ✓ | ✗ line | ✓ | ✓ | ✓ | ✓ |
| 8 | LOG-1 | `Console.WriteLine` instead of the logger | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 9 | SEC-6 | Email address in the log | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 10 | SEC-1 | Password in `appsettings.json` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 11 | SA-1 | `!` on a possible null | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 12 | CC-6 | Commented-out code | ✓ | ✗ line | ✓ | ✓ | ✓ | ✓ |
| 13 | SOLID-3 | Fake that throws `NotImplementedException` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| 14 | TEST-2 | Test named `Test1`, with a `foreach` | ✓ | ✗ line | ✓ | ✓ | ✓ | ✓ |

**Decoys:** none of the 5 was reported as a violation in any of the six reviews. The six-parameter endpoint was
written under "Doubts" every time, with the right reason (the parameters are bound by the framework).

### Findings that were not planted

The reviews also reported violations that are not in the key. Each was checked against the rule text:

| Rule | Where | What | Decision |
|---|---|---|---|
| CC-3, CC-4 | The endpoint, same line as violation 2 | The 24 hours repeat `Booking.FreeCancellationNotice`, as a bare number | Real; accepted rules for violation 2, counted once |
| DOM-3 | `ListUpcomingBookingsQuery` | The email travels as `string?` although the `EmailAddress` value object exists | Real. Not planted on purpose: it came with the planted email log |
| WORK-2 | `appsettings.json` | A configuration section with a secret that no code uses | Real: the rule forbids touching secrets the task does not need |
| SOLID-1 | The endpoint, same line as violation 2 | A business rule in an HTTP class | Real: the rule says so literally. It is the same mistake seen through two rules; the review says so itself |
| TEST-1 | The new test | The only test cannot fail: the domain reads the real clock, so the list is empty and the loop never runs | Counted as real: the change of behaviour has no test that checks it. A stricter reading ("a test exists") would make it a false alarm; that gives at most 1 per review, so the result is the same |

## Cost

| | US dollars |
|---|---|
| Round 1 (3 reviews) | 1.45 |
| Round 2 (3 reviews) | 1.68 |
| **Total** | **3.13** |

## Limits

- **The same author wrote the mistakes and the reviewer.** The planted mistakes are the kind the rules describe, so
  this shows that the reviewer finds clear violations of its own rules. It does not show how it does on subtle ones,
  or on code written by someone else.
- **One change, three reviews per round.** Three out of three is consistent, not a statistic.
- **The key was not committed before the reviews.** It was written first and kept outside the review folders (no
  transcript mentions it), but this repository cannot prove when it was written.
- **What the reviewer does not do:** it does not build or run tests. That is measured by the WORK-7 commands; here
  the compiler would also have caught violation 7 (EF Core's analyzer warning `EF1003`).

## Files

- [`reviewer-check/seeded.diff`](reviewer-check/seeded.diff): the planted change.
- [`reviewer-check/answer-key.md`](reviewer-check/answer-key.md): the answer key, as written before the reviews.
- [`reviewer-check/reports/`](reviewer-check/reports/): the six reports, as they came out of the reviewer.
