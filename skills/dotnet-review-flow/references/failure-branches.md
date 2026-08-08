# Failure branches — a tier with no signal, and a subagent that dies

Two contingency paths lifted out of the shared block. Neither runs on a normal
pass: the first is entered only when a tester reports `RED — environment` or
`tier absent — nothing run`, the second only when a spawned subagent errors or
returns nothing. **Both are entered on an unmistakable event, which is why they
live here and the loops themselves do not.**

## NO-SIGNAL

Entered when a tester returns `RED — environment` or `tier absent — nothing
run`. Both mean one thing — **no evidence about the code under review, and
nothing in the code to fix** — so from here the flow treats them identically.
Splitting them is what let one of them deliver a report and the other deliver
nothing.

> **NO-SIGNAL may end in a question. It may never end in nothing delivered.**
> Whether repair succeeds, fails, or waits on an answer, REVIEW-LOOP still runs
> and the report is still produced. The lenses never depended on the tiers.

**1 — State it so the user can act on it.** Name what is missing and why, in
words that support a decision. A verdict string and an error code are a
symptom, not a diagnosis. A user who cannot tell what is being asked does not
answer, and an unanswered question is exactly how a run ends with nothing.

**2 — Measure before offering. Numbers, not adjectives.**

| Entry | Measure |
|---|---|
| `RED — environment` | What is blocking, taken from the tester's *Environment* section; whether it is repairable here; which rung of the table below it falls on |
| `tier absent — nothing run` | How many types in scope have no test, which tiers exist versus are empty, and whether the missing tier needs infrastructure stood up — that last one changes the size of the job by an order of magnitude |

"This would be a large job" is unusable. "Module X: 14 types, 0 tests" is a
decision input.

**3 — Repair, at most twice.** One question classifies every action: **does it
acquire something over the network?**

| Do it | Ask first | Never |
|---|---|---|
| Start containers whose images are already local | **Anything acquired over the network** — a missing package, an image not yet pulled | Anything irreversible on the user's machine |
| Re-run the pair **serially** — unit first, then integration — on an artifact lock, and note the serialization | Install software on the machine | Anything needing administrator rights |
| Re-run a diagnostic command, read configuration — never a test suite; the tiers are re-run only by re-spawning the testers | Edit project files, change ports, delete build caches | Anything governed by policy the user does not own |
| | | Edit a test to dodge a failure — the testers' ban, and it does not loosen because the coordinator is the one holding the pen |

**Two attempts, then explain and ask.** One attempt is one repair pass
followed by one re-spawn of the testers, however many individual actions
that pass contained — the cap governs the reruns, not the actions inside
them. Every other loop here is capped; an uncapped repair loop spends a
session invisibly. An ordinary build restoring its own packages is building,
not repairing, and this table does not govern it — a build that **fails
because acquisition failed** is what enters here.

**4 — Offer options built from the measurement. Never a bare yes/no.** The list
is generated from what step 2 counted; it is not written down here, because a
fixed menu cannot know what was measured. It always includes *do nothing,
record it in the report*, and it includes a partial option whenever the
measurement decomposes into parts — one module rather than four, the unit tier
rather than both. Yes/no forces a user who has an hour to choose between
nothing and everything.

If the user accepts writing tests, **this session writes them** — the same
mechanism as the end-of-report offer, on the same authority: the user's answer.
What a test looks like belongs to `dotnet-testing`; none of it is taught here.
**This offer is standalone only.** Embedded under `dotnet-feature-flow`, tests
are written as the feature is built and the calling flow owns that. The repair
ladder above applies in both modes.

**If NO-SIGNAL changed the tree — tests written, a project file edited — re-enter
TEST-LOOP and recompute the diff before REVIEW-LOOP.** Reviewers handed a diff
that predates the tests just written are grading something nobody will ship.

**Then continue to REVIEW-LOOP regardless.** Every tier that produced no signal
goes into *Not run* with what was attempted and what the user chose.


## When a subagent fails

A subagent fails in one of two ways, and they are handled differently —
**classify before retrying**:

**A deterministic environment failure is never retried.** An error naming a
missing tool (`No such tool available`), a skill that would not load, or an
agent that could not start at all: the same spawn meets the same defect every
time, so the retry proves nothing — and a retry that "succeeds" is worse than
one that fails, because success means the agent improvised around the defect in
a way this flow never sees and cannot audit. The same command then yields
different runs depending on whether an agent chose to improvise. **STOP and
surface it to the user**: the defect is in the install or in an agent's
definition, and it is fixed there, not by respawning.

**A transient failure is retried once, with the identical prompt.** A timeout,
an API error, an empty return, or a return that is not the report shape: retry
once. Same contract, same wording — a reworded retry tests a different thing
and its result is not comparable.

Still failing: **surface it to the user by name**, say what is now unknown, and
list it under *Not run*. **Never silently drop a lens or a tier, and never
substitute this session's judgement for one that did not run.** A four-lens report
missing one lens is a three-lens report, and a report missing the security lens
looks exactly like a report where security found nothing — the most expensive
ambiguity this flow can ship.
