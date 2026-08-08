---
name: dotnet-review-flow
description: >-
  This skill should be used when running the test-and-review fleet over .NET code:
  /dotnet-review, "review my branch", "final review before merge", "review each
  task as it lands", "review the modules under this folder", "audit
  the existing code in this project" — a branch, a range, or unchanged code:
  every tracked file under given paths, changed or not. Not for: the full feature
  process, brainstorm to commit — dotnet-feature-flow; what each lens checks —
  dotnet-code-review, dotnet-architecture-review, dotnet-security-review,
  dotnet-performance-review; test conventions — dotnet-testing; brainstorming,
  planning, TDD — Superpowers; unclear ownership — choosing-a-dotnet-skill.
---

## Overview

This skill is an **orchestration graph, not a rubric.** It runs a change through
two loops — tests, then review, with a third named unit for when the tiers
produce no signal — using fresh-context subagents, and it decides only *what
runs, in what order, who runs it, and when to stop*.

**It teaches nothing.** Process belongs to Superpowers, which this flow **calls
and never copies**. What each lens checks belongs to the four rubric skills; what
a test looks like belongs to `dotnet-testing`; the .NET conventions under all of
it belong to the knowledge skills. A line of .NET doctrine appearing in this body
is a defect — it becomes a second source of truth the day the owning skill
changes.

### Two modes, one block

| Mode | Entered by | Who fixes | Ends at |
|---|---|---|---|
| **Standalone** | `/dotnet-review`, or this skill triggering on its own description | Nobody, until the user accepts the offer | The final report, plus an **offer** to fix the CONFIRMED findings |
| **Embedded** | `dotnet-feature-flow`, as its PHASES 4–5 | The calling flow's implementer | Return to the caller; the caller owns what happens next |

**How the mode is decided: the caller says so** — and **absent an explicit
statement that this is `dotnet-feature-flow`'s PHASES 4–5, the mode is
standalone.** Default that way deliberately: a standalone run that wrongly thinks
it is embedded edits the tree without asking, while the reverse only asks a
question that was already answered.

State the mode in the first line of the final report. The loops are identical in
both; a reader who does not know which mode ran cannot tell "nothing was fixed"
from "nothing needed fixing".

## Core principles

1. **Judgement comes from fresh context; this session coordinates.** The session
   running this flow **never reviews and never tests.** It computes the diff,
   spawns the agents, verifies specific claims against specific lines, applies
   fixes, and writes the report. A session that has just written the code cannot
   un-know its intent while grading it — that is the whole reason the fleet
   exists.

   **Verifying is not reviewing.** Opening `<file>:<line>` to check whether a
   reported symptom is actually there is a fact check with a stated answer.
   Forming an opinion about code no agent flagged is reviewing, and it is out of
   bounds.

2. **Tests before review, always.** Tests are cheap and mechanical; review is
   expensive. Reviewers never see non-green code, and every review-driven fix
   re-enters the tests anyway — so reviewing first spends the expensive pass on
   code that is about to change.

3. **A finding is verified before it is fixed.** A subagent's CRITICAL is a claim
   about lines this session can open. Reproduce it or downgrade it. Fixing an
   unverified finding edits working code on a subagent's say-so, and no later
   round catches that.

4. **Nothing is silently dropped.** A lens that failed, a tier that was absent, a
   check that could not run, a finding that stayed PLAUSIBLE — each appears in the
   final report by name. Silence reads as a pass, and the distinction between
   *clean* and *unexamined* is this flow's entire output.

## PHASE 0 — Preflight, and STOP on any failure

Four checks, in order. Each failure is a **STOP**: report what is missing, give
the exact remedy, and wait for the user. Do not degrade, do not work around,
and install nothing to get past a failed check here. Standing up a test
environment later, under NO-SIGNAL, is a different act with its own rules.

**1 — Superpowers is present and enabled.** Verify by **actually loading a
Superpowers skill**: invoke the Skill tool on
`superpowers:verification-before-completion`. This flow needs it later anyway, so
the probe costs nothing.

Do **not** verify by reading `installed_plugins.json`. A disabled or stale install
still has a registry entry, so the file answers "is it registered", not "can I
call it". The registry check is the SessionStart hook's job, at warn level; the
only trustworthy check here is the call.

> **STOP — Superpowers is not available.**
> This flow calls Superpowers for process and cannot substitute its own.
> Install: `claude plugin install superpowers@claude-plugins-official`
> Then restart Claude Code — a new plugin does not take effect in this session.

**2 — A git repository is present** (`git rev-parse --git-dir`) — both target
shapes need one, to list the files and to stamp the report's provenance. **A
resolvable base is required only of a diff target**; a path scope has none to
resolve. A branch or range that will not resolve is a STOP with what was found.

**3 — This plugin is complete, not a stale or partial install.** The block binds
to five skills and six agents:

| Must be present | Names |
|---|---|
| Rubric skills | `dotnet-code-review`, `dotnet-architecture-review`, `dotnet-security-review`, `dotnet-performance-review` |
| Test conventions | `dotnet-testing` |
| Agents | `dotnet-unit-tester`, `dotnet-integration-tester`, `dotnet-code-reviewer`, `dotnet-architecture-reviewer`, `dotnet-security-reviewer`, `dotnet-performance-reviewer` |

**Check the names against this session's available skills and agent types. Do not
load the rubrics to prove they exist** — five rubric bodies in this session's
context is both expensive and exactly the contamination Principle 1 forbids, and
a name that is absent from the roster is absent from the install. A missing name
means the cached copy is stale or partial: STOP, name what is absent, and give the
update-and-restart remedy.

**One exception, and it is a harness fact rather than a bad install: all six
names absent at once.** Codex is the case in hand — its plugin contract carries
`skills/` and nothing else, so the agents are installed beside the plugin by
`codex/install.sh`, and even then whether they resolve for spawning differs by
Codex surface. Reinstalling the *plugin* will never produce them. Six absent is
that signal — say that the kit may be uninstalled and name the script — while
one or two absent is still a broken install. Then, with the user told which
limit was hit:

- The **five skills** must still all be present. They ship everywhere. If one is
  missing, that is a stale install and the STOP stands.
- Run the four lenses **one at a time, sequentially, in the order listed above**,
  each against the same expanded target, writing its report before the next
  begins. Four passes, never a merged one — the merge is exactly what Principle
  1 forbids, and it is the shortcut this fallback exists to refuse.
- **Say in *Not run* that the lenses shared one context.** They did, and it
  weakens every verdict in a way the report must not hide.

**4 — Determine, expand and state the review target.** The invocation argument
decides the shape. **A diff target** — a branch, a commit range, or no argument,
which defaults to the working tree against the repository's default branch —
expands to the files that changed against its base. **A path scope** — paths with
optional exclusions, as in `src/Modules/` except `src/Modules/Legacy` — expands
through `git ls-files -- <paths> :(exclude)<excluded>` to every tracked file under
them, reviewed whether or not anything changed; tracked only, so build output and
untracked scratch never enter a review. An empty expansion is a STOP.

**Count the expansion, then state the count, the scope label, every exclusion and
any path that expanded to nothing, before anything is spawned** — the fleet's
price scales with a number the user has not seen, a review of the wrong folder
reads exactly like a clean one, and one typo among four paths leaves a plausible
count covering less. Exclusions go into *Not run*: excluded is not reviewed.

**A path scope covering one side of the HTTP boundary warns about the other.**
A scope with the module folders but not the controllers — or the reverse —
reviews half of every request path, and the severest defects are routinely in
the half left out: logic that belongs in a service but sits in a controller is
exactly the code a module-only scope never opens. Name the missing side's path
and offer to widen before spawning; the user may decline, and a declined side
goes under *Not run* like any other exclusion.

## Diff preparation — the spawn contract

**The flow computes the diff exactly once, and every subagent receives the same
four inputs.** No subagent runs git: the four reviewers have no shell at all, and
the two testers have one only to build and test.

1. Resolve the target into a base and a pathspec. A diff target resolves its base
   ref. **A path scope has no base: diff the empty tree against `HEAD` with check
   4's pathspec**, taking the empty tree from `git hash-object -t tree /dev/null`
   — never a hard-coded hash, which differs by object format. Standing code then
   arrives as wholly added lines, exclusions riding inside the pathspec.
2. Write the diff to a file **under the session's scratch area, never inside the
   repository** — a diff file committed by accident is a defect this flow caused.
   Use a path every spawned agent on this host can open — on Windows a real
   Windows path, never a POSIX `/tmp` — and treat an agent reporting the diff
   unreachable as a flow defect to fix, not a fallback to normalize.
3. Derive the changed-file list — a diff target with `git diff --name-only
   <base>...HEAD`, a path scope with check 4's expansion, already counted.
4. Hand every spawned agent these four items, verbatim and identical:

| Input | What it is |
|---|---|
| **Diff file path** | The single computed diff, already on disk |
| **Changed-file list** | The paths the diff touches — the reviewers' scope, and what the testers match stack frames against |
| **Base ref** | What the diff is against, so a finding can say what is new — for a path scope, `the empty tree (standing code)`, where nothing is new |
| **Scope label** | The human-readable target, reused verbatim in every report heading |

One diff, one contract, six agents. Handing two lenses different inputs produces
two reports that cannot be compared, and nobody can tell which one was wrong.

**Chunk a scope the fleet cannot hold.** Split on the scope's own top-level path
boundaries into chunks of at most ~100 files **or ~250 KB of patch, whichever
comes first**: the count is what a user can defer by name, but a hundred generated
files outweigh a thousand hand-written ones. Run the fleet per chunk with that
chunk's own diff and label. **More than one chunk: state the plan and wait** — one
chunk is the ordinary run, several is a bill the user did not name. **Chunking
splits the fleet, not the suite** — TEST-LOOP is not chunked, so only REVIEW-LOOP's
cap is counted per chunk. Findings merge into **one** report for the whole scope,
and any chunk declined or halted by a cap is named under *Not run*.

**A file the change cites as precedent is in scope, even when it is not in the
diff.** When a shape is justified by *"this matches the existing X"*, X is
checked against the skills before the justification is accepted — a reviewer who
reads only the diff cannot see the file the diff is imitating, and imitation is
how one non-conforming file becomes three. **Where existing code contradicts a
loaded skill, the contradiction is the finding**; the code does not become the
rule. If you cannot tell whether the deviation was deliberate, report it and say
so — never resolve it by imitation.

**Recompute the diff after every fix round**, before re-spawning. A reviewer
handed a stale diff reports findings that no longer exist and misses the ones the
fix introduced.

**Then build once, as a gate.** Run `dotnet build` on the solution. On failure,
report the compiler diagnostics and **spawn nobody** — a fleet dispatched at code
that does not compile burns six agents to rediscover one error. On success the
testers still build too, which keeps a build failure reportable separately from a
test failure and costs little against warm outputs — the gate's second benefit,
since two agents compiling the same cold tree in parallel collide on artifacts.

**Read the gate's warning count while it is on the screen.** The knowledge
skills deliberately delegate whole rule families — formatting, using ordering,
nullable flow — to the analyzers, and that delegation holds only
where an analyzer finding can fail a build. A green gate with analyzer warnings
raises one question: what enforces them? Check `TreatWarningsAsErrors` in the
build props and per-rule severity in `.editorconfig`. When neither is set and
the warnings run to the hundreds, the delegated families are enforced by
nobody — the analyzers are catching, everyone is ignoring, and neither this
fleet nor the build will surface what they caught. **Tell the user, with the
count**, recommend raising severity to `error` one rule group at a time rather
than reviewing what the analyzer already flags, and record the fact in the
final report under *Not run*: analyzer-delegated rules, not enforced in this
repository.

This flow owns no worktree and no branch. Worktree lifecycle belongs entirely to
`dotnet-feature-flow`.

## The shared block: TEST-LOOP then REVIEW-LOOP

*This heading is stable. `dotnet-feature-flow` names it verbatim to run this block
as its PHASES 4–5. Do not rename it, and do not fork a second copy of the loops
below into another skill.*

Run **TEST-LOOP** to green, then **REVIEW-LOOP**. Every fix inside REVIEW-LOOP
re-enters TEST-LOOP before the next review round.

**Halting means the loop stops. It never means the deliverable stops.** Every
stop condition below — a cap, a timeout, an unanswered question — ends a loop
and still owes the final report. Fusing those two decisions together is what
once let a blocked test tier consume a whole run and produce nothing.

### TEST-LOOP

**Spawn both testers in parallel**, in one message, each with the spawn contract:

- `dotnet-standards:dotnet-unit-tester`
- `dotnet-standards:dotnet-integration-tester`

Each builds and runs its own tier and returns its own report. **Do not read the
tiers' conventions, re-run a suite, or second-guess a tier from this session** —
the testers own that, and Principle 1 forbids it.

Each tester ends on one of six verdict strings. Branch on them:

| Verdict | Do this |
|---|---|
| `GREEN` | This tier is done |
| `tier absent — nothing run` | Does not block the loop, and **is not a pass.** Enter **NO-SIGNAL** (`references/failure-branches.md`), then carry the tier into the final report under *Not run* |
| `RED — tests failed` | Fix and rerun — see who fixes, below |
| `RED — build failed` | Fix the build; no test result exists to interpret yet |
| `RED — environment` | **Enter NO-SIGNAL — open `references/failure-branches.md` now. This does not consume a round.** No container runtime, an unreachable image, an artifact lock: there is nothing in the code to fix, so an identical rerun is not the answer — repair, or record and continue, is |
| `RED — timed out` | Report the command and the budget; a run killed at a limit is not a failing suite. Retry once with a larger budget, then halt the loop — **the report is still owed.** This does not enter NO-SIGNAL: unlike a blocked or absent tier, a timeout can be the code's own fault |

**Who fixes:** embedded mode, the calling flow's implementer — the loop reruns
to green. Standalone mode, **nobody**: record the failing tier and **continue
to REVIEW-LOOP**, carrying the RED verdict into the report — the offer after
the report covers the failures alongside the findings. Principle 2 still held:
the tiers ran first, and its economy argument — don't spend review on code
about to change — has no force when nothing changes until the user answers. A
standalone run that stopped at a red tier would ship no review of exactly the
code that most needs one.

**Cap: 5 rounds.** On the fifth, halt with a status summary — which tier, which
tests, what changed between rounds, how many rounds ran — and ask the user. Never
a sixth, and never relax the green bar to escape the cap.

#### A tier that varies is not a tier that failed

**Compare the failing set across rounds, not the count.** A tier whose failing
tests change between two rounds without a fix aimed at them — or whose totals
move at all on an unchanged tree — is **non-deterministic**, and that is a
different condition from `RED — tests failed` even though it arrives wearing the
same verdict string. `dotnet-testing` Principle 7 owns why; this flow owns what
happens next, in this order:

1. **Confirm it, and confirming means repeating.** Re-spawn that tester with the
   identical contract against the unchanged tree, two or three times, and compare
   the failing sets. **These re-spawns do not consume a round** — nothing was
   fixed between them, and a cap that counts them punishes the only evidence that
   settles the question. One green run never confirms a variance is gone either:
   the same repetition is what closes it.
2. **Stabilizing comes first, and it is the fix of that round.** Fixing
   individual reds under a varying tier spends rounds on tests that may be
   reporting the fixture. Who fixes is unchanged — the calling flow's implementer
   embedded, nobody standalone.
3. **Then re-triage every outstanding failure, one at a time.** Never carry
   forward a list assembled while the tier varied — including a list this run
   inherited as "known", "pre-existing" or "out of scope". That list was built
   under conditions that made classification unreliable, and the re-triage is
   what separates fixture artifacts from real defects.
4. **Say it in the report.** The tier's row carries `non-deterministic` beside its
   verdict, with the runs compared and what the re-triage found. A varying tier
   reported as an ordinary red hands the reader a number that does not mean what
   it looks like.

**While a tier varies, its whole-suite totals are not evidence** — not for
"green", not for "these N are known", not for verification-before-completion.
Only a filtered run of a named test is, and the report says which kind it quotes.

### NO-SIGNAL and a subagent that dies — `references/failure-branches.md`

Two contingency branches live in that file, and **each is entered on an event you
cannot miss**: a tester returning `RED — environment` or `tier absent — nothing
run`, and a spawned subagent that errors or returns nothing. **Open the file the
moment you read either one** — before deciding anything about the run.

Carry these three without opening it, because they change what you do *before*
you get there:

- **A tier with no signal never halts the flow.** NO-SIGNAL may end in a question;
  it may never end in nothing delivered. REVIEW-LOOP runs and the report is
  produced either way.
- **If NO-SIGNAL changed the tree, re-enter TEST-LOOP and recompute the diff
  before REVIEW-LOOP** — reviewers handed a stale diff are grading something
  nobody will ship.
- **Classify a subagent failure before retrying.** A deterministic environment
  failure is never retried; a transient one is retried once with the identical
  prompt. Retrying a deterministic failure burns the round and changes nothing.

### REVIEW-LOOP

Entered **only with both tiers green** — or with a tier that produced no signal,
once NO-SIGNAL has recorded it, or, standalone, with a RED tier recorded per
*Who fixes* above. A blocked tier is not a failing tier, and the lenses never
depended on either.

**Spawn all four reviewers in parallel**, in one message, with the same spawn
contract:

- `dotnet-standards:dotnet-code-reviewer`
- `dotnet-standards:dotnet-architecture-reviewer`
- `dotnet-standards:dotnet-security-reviewer`
- `dotnet-standards:dotnet-performance-reviewer`

All four, every round. One agent per lens, no sharing: no reviewer inherits
another's context. On a harness with no plugin agent types, the four run
sequentially under preflight #3's exception — still four, still one lens each. **Never collapse the four into fewer spawns, and never drop one
because "this change is not about security"** — the lenses grade different things,
a merged pass reliably drops one, and a lens dropped by judgement is a lens whose
absence nobody notices.

Collect the four reports as returned. Each carries its own rubric's sections and
verdict; **do not merge, rewrite or re-grade them.**

Then verify (below), and:

1. **Fix only CONFIRMED CRITICAL and HIGH findings**, then re-enter **TEST-LOOP**,
   recompute the diff, and re-spawn all four reviewers.
2. **Stop when no CONFIRMED CRITICAL or HIGH findings remain.**

**MEDIUM and INFO are never chased.** They go to the final report unfixed and
unargued. Chasing them turns a bounded loop into open-ended cleanup, and
`/simplify` owns cleanup.

Severity words are `dotnet-code-review`'s ladder — CRITICAL / HIGH / MEDIUM /
INFO — cited by every rubric, consumed here, defined here never.

**Cap: 3 rounds.** On the third, halt with a status summary — which findings
survive, which were fixed, how many rounds ran — and ask. Never a fourth, never
lower a severity to clear the gate.

### Verifying findings — CONFIRMED vs PLAUSIBLE

Every CRITICAL and HIGH finding is verified before it forces a fix. Open the cited
`<file>:<line>` and answer one question: **is the reported symptom actually there,
as described?**

| Verdict | Meaning | Consequence |
|---|---|---|
| **CONFIRMED** | Reproduced from the cited lines | Forces a fix (CRITICAL and HIGH only) |
| **PLAUSIBLE** | Could not be reproduced from the cited lines — they do not show it, the citation is stale, or it depends on context the reviewer could not see | Reported with the reason, never fixed, never deleted |

Three rules keep this honest:

- **PLAUSIBLE is not "wrong".** It means the evidence handed over did not
  establish it here. It survives into the final report in the reviewer's own
  wording, so the user can judge it. Never upgrade a PLAUSIBLE by argument.
- **Verify the claim, not the code.** Read the cited lines and their immediate
  context. A verification pass that wanders into a general opinion has become the
  review this session may not perform.
- **Do not verify MEDIUM or INFO.** They force nothing, so verification buys
  nothing and costs a read of every file in the change.

For triaging what comes back — arguing with a finding, telling a technical
disagreement from a performative one — load
`superpowers:receiving-code-review` rather than improvising.

Before declaring the block complete, invoke
`superpowers:verification-before-completion` and follow it. This flow's output is
a claim that the suite is green and the lenses are clean; that skill turns the
claim into evidence. "Green" means counts read out of a tester's report, not an
impression of one.

## The final report

**Always produced** — in both modes, when everything passed, when a cap halted
the run, and when NO-SIGNAL ended in an unanswered question. **There is no path through the shared block that ends without the report.**
PHASE 0 and the pre-build gate stop *before* the block and hand back diagnostics
instead; everything after them owes this report. Every section appears; write
`None.` when empty.

**Report language.** Write the report in the language the reviewed project's
`CLAUDE.md` sets for talking to the user. If it sets none, write in the language
the user is using in this session. Identifiers, paths, commands, file names and
quoted code stay in English. **The field labels below are English because this
skill is written in English — they are field names, not fixed strings. Translate
them.** A report in the wrong language is a defect even when every finding in it
is correct.

**The header table is not optional and every row appears.** A row that cannot
apply carries `—`; a blank cell is a defect.

```markdown
# Review: <scope label> — <one line on what it covers>

| | |
|---|---|
| **Date** | <yyyy-MM-dd> |
| **Branch** | `<branch>` (<n> commits) |
| **Base** | `<sha>` on `<base branch>`, or `the empty tree (standing code)` for a path scope |
| **Worktree** | `<path>`, or `—` |
| **Scope** | <n> files — <what they are> |
| **Excluded** | <paths or shapes left out, or `—`> |
| **Method** | <which lenses and testers ran, in independent contexts; and that every CONFIRMED CRITICAL/HIGH was re-checked at the cited `file:line`> |

### Verdicts
| Lens | Verdict | CONFIRMED CRITICAL/HIGH | Unfixed MEDIUM/INFO |
|---|---|---|---|
| Code | PASS / FAIL | n | n |
| Architecture | PASS / FAIL | n | n |
| Security | PASS / FAIL | n | n |
| Performance | PASS / FAIL | n | n |

### Tests
| Tier | Verdict | Passed | Failed | Skipped |
|---|---|---|---|---|
<counts exactly as the testers reported them, with their verdict strings. A tier
that reported `tier absent — nothing run` or `RED — environment` still gets its
row, with the verdict spelled out and dashes in the counts — never omitted, never
merged into another row, never written as green. It appears again under *Not run*
with what NO-SIGNAL attempted. A tier whose failing set moved between rounds
carries `non-deterministic` beside its verdict and its counts are quoted as one
run's, never as the tier's state.>

### CONFIRMED findings
**<id> · <lens> · <SEVERITY> · `<file>:<line>` · fixed (what changed) / outstanding (why)**
- **Defect:** <what is wrong>
- **Failure:** <the concrete input or state, then the wrong result>
- **Fix:** <the correct shape, or who decides when the call is not this flow's>

### PLAUSIBLE findings
**<id> · <lens> · <SEVERITY> · `<file>:<line>`**
- **Claim:** <the reviewer's finding, in the reviewer's own wording>
- **Not reproduced:** <what verification tried and what it saw instead>

### Unfixed MEDIUM and INFO
<by lens, unranked, carried through untouched>

### Not run
<tag each entry **not examined** — excluded paths, declined chunks, a layer no
coverage line reached — or **attempted, no result** — a lens that failed twice, a
tier with no signal, a chunk a cap halted. Name each one: untagged, "chose not to
look" reads as "found nothing".>

### Run
TEST-LOOP <n> of 5 · REVIEW-LOOP <n> of 3 · Chunks <n> · NO-SIGNAL <attempted,
what the user chose, or "not entered"> · <cap hit? say so> · <commands the flow ran>
```

Five rules for the report:

1. **A green run reports the numbers.** "All clean" with no counts is
   indistinguishable from a suite that discovered nothing and a fleet that never
   spawned.
2. **Carry the subagents' own coverage sections through.** Each rubric mandates a
   coverage line — audits, layers, areas — and each tester reports what did not
   run. Fold them into *Not run*; do not summarize them away. What the fleet did
   not examine is the most perishable thing it learned.
3. **Nothing a subagent learned is dropped — and nothing is padded either.** The
   user paid for six fresh-context passes, so a summary keeping only the blockers
   throws away most of what was bought. But keeping a finding means keeping its
   **Defect / Failure / Fix**, not its reviewer's prose: **each of those lines is
   at most two sentences, and a finding has no fourth line.** Anything longer is
   an appendix — put it under `## Notes` at the end and reference it by the
   finding's id. Dropping a finding and inflating one are both failures of this
   rule, and the second is the one that actually happens.
4. **Three things never appear in a finding**, because each is process narration
   rather than information. **How the review reached it** — no *"the reviewer
   asked for…"*, no *"verified by running…"*; state the conclusion and put
   evidence the reader must re-check in `## Notes`. **A paragraph arguing the
   severity** — the severity is on the header line and the `Failure:` line is its
   whole argument. **Anything already in the header table** — the branch, the base
   and the scope are stated once, at the top, and never again.
5. **The report is written to a file, not only to the chat.** Write it to
   `docs/code-review/<yyyy-MM-dd>-<scope-label>.md` inside the reviewed
   repository, creating the folder if absent. The chat shows the same content;
   the file is the durable copy.

## Standalone mode — the offer

The standalone run ends at the report. Then, and only then, **offer**:

> N CONFIRMED findings remain (X CRITICAL, Y HIGH). Fix them now?

- **Accepted:** this session fixes them directly, re-enters the shared block from
  TEST-LOOP, and reissues the report — same block, same caps.
- **Declined, or no answer:** stop. The report is the deliverable.
- **Never fix first and report after.** A standalone review that edited the tree
  before the user saw the findings has taken a decision nobody offered — and that
  boundary is exactly the one the read-only fleet exists to draw.

The offer covers CONFIRMED CRITICAL and HIGH only. MEDIUM and INFO are not offered
and not chased.

## Routing

**Sibling flow.** The full feature process — brainstorm, plan, implement, this
block, then git — is `dotnet-feature-flow`. It calls the shared block above; it
does not reimplement it. **Content.** What each lens checks lives in
`dotnet-code-review`, `dotnet-architecture-review`, `dotnet-security-review` and
`dotnet-performance-review`; what a test looks like lives in `dotnet-testing`.
This flow loads none of them — the agents do. **Process.** Brainstorming,
planning, TDD, worktrees, finishing a branch, and the request/receive review
discipline belong to Superpowers, which this flow calls and never copies.
Executing cleanup candidates belongs to `/simplify`. **Unclear ownership.**
`choosing-a-dotnet-skill`.

## Decision Guide

| Situation | Do this |
|---|---|
| Invoked with no argument | Diff the working tree against the default branch; state the scope label back before running |
| Invoked with paths, with or without exclusions | A path scope, not a diff — no base to resolve. Expand with `git ls-files`, state the count, the label, every exclusion and any path that expanded to nothing, then review every tracked file, changed or not |
| Superpowers will not load | STOP with the install command and the restart note. Never substitute a hand-rolled process |
| A rubric skill or an agent name is missing from the roster | STOP — the install is stale or partial. Name what is absent, give the update-and-restart remedy |
| The pre-build gate fails | Report the diagnostics, spawn nobody, hand it back |
| The gate passes but a tester reports `RED — build failed` | A test project does not compile and sits outside what the solution build covered. Treat it as a build failure for that tier, not a test failure |
| The repository has no test projects at all | Both tiers absent. NO-SIGNAL: measure the gap, offer options built from the count, then REVIEW-LOOP either way |
| The same tier reports different failures in two rounds, or moving totals on an unchanged tree | Non-deterministic, not merely red. Re-spawn to confirm (those re-spawns cost no round), stabilize before fixing individual tests, then re-triage every outstanding failure one at a time — and stop quoting whole-suite totals until it is settled |
| A failure list arrives labelled "known noise" or "pre-existing" | Trust it only if the tier is deterministic. Otherwise it is an unclassified list, and re-triaging it is where a hidden defect surfaces |
| A tester reports `RED — environment` | NO-SIGNAL — `references/failure-branches.md`. Does not consume a round, and never halts the run — the report is owed regardless |
| A repair inside NO-SIGNAL would download something | Ask first. That single question — does this acquire over the network — is the whole classifier |
| A reviewer's CRITICAL cannot be reproduced at its `file:line` | PLAUSIBLE. Report it with the reason; do not fix and do not delete |
| A reviewer returns a finding with no `file:line` | PLAUSIBLE by definition — there is nothing to verify against |
| Two lenses report the same defect | Verify once, fix once, report once naming both lenses. Never carry two severities for one shape |
| A CONFIRMED HIGH is real but outside this change's scope | It still blocks the loop, or the user decides it does not. Ask; never silently demote it |
| Every line in the diff reads as added | An artifact of the empty-tree base, not a fact about the code. A reviewer's "newly introduced" is wording, and "outside this change's scope" has no referent when the scope is the file set — never move a severity for either |
| A subagent errors naming a missing tool or an unloadable skill | Deterministic — never retried. STOP and surface it: the defect is in the install or the agent definition, and a "successful" retry only means the agent improvised around it |
| A path scope covers modules without controllers, or the reverse | Warn: half of every request path is out of scope. Name the missing side, offer to widen; declined goes under *Not run* |
| A lens fails twice | Surface it by name, list it under *Not run*, continue with the rest. Never review that lens yourself |
| The user asks what a finding means | Point at the owning rubric or knowledge skill; do not re-explain the rule here |
| Asked to also plan, brainstorm or commit | Not this skill. `dotnet-feature-flow` owns the full process; Superpowers owns each process step |
