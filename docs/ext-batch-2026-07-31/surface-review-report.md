# WORK PACKAGE B — review-layer activation surface

**Status: COMPLETE. Both pieces applied.**
Coordinator report to the main session. This file is the approval gate.

Scope worked: the four review rubric skills and the six agents under `agents/`.
Nothing else touched — no knowledge skill body, no router, no flow skill, no
description or frontmatter, no git. (`git status` also shows `api-surface`,
`automapper-mapping`, `dotnet-testing`, `ef-core-data-access` and
`module-feature` modified — those are **Work Package A's**, not mine.)

---

## The mechanism, and what actually repaired it

At 0.3.32 the four reviewer agents were given knowledge-skill load lists derived
from which skills their rubric **cites most**. The five skills shipped
0.3.48–0.3.52 were cited zero times, so no reviewer ever loaded one, so all five
skills' doctrine — and the 32 anti-examples shipped at 0.3.54 — was invisible to
the enforcement layer.

**The repair is not the one the brief anticipated, and the measurement is why.**
The brief's move 2 assumed the agent load lists needed the five skills added.
They do not, and adding them would have made things worse. Each agent's list is
"the bodies this rubric cites most", and the counts put every one of the five far
below every incumbent floor:

| Rubric | Preloaded (floor) | Cited *more* but deliberately **not** preloaded | The five |
|---|---|---|---|
| code-review | ef-core 18, error-handling 17, module-feature 15, message-keys **8** | api-surface 13, dotnet-testing 13, distributed-lock 9 | 6, 3, 2, 2, 1 |
| security | auth 28, api-surface 13, error-handling **5** | facade-module-architecture 7 | 3, 2, 1 |
| performance | lock 17, ef-core 14, caching 14, elastic **14** | api-surface 8 | 4, 1, 1 |
| architecture | fma 30, mediatr 5, module-feature **4**, api-surface 4 | — | 2, 1 |

Adding `common-extensions` at 6 to the code reviewer while `api-surface` at 13
and `dotnet-testing` at 13 stay out would invert the list's own stated criterion,
and an agent that preloads everything preloads nothing usefully. **Both authors
reached this independently; I counted it myself and got identical numbers; the
arbiter re-checked the metric and accepted it.**

What carries the five is the *standing* instruction each agent already has —
*"Any other skill a check cites is loaded before a finding citing it is
written."* That sentence is a conditional, and **piece 1's citations are what
make it fire.** The citations are the mechanism; the load lists were a red
herring.

**And then Author B found the real hole in that sentence**, which is the single
best finding in this package: it triggers on *writing a finding*, and piece 1
deliberately created three citations whose entire purpose is to **stop** one
(code-review 4.2, performance 1.3, performance 1.5). In each, the correct
outcome is silence, so the trigger never fires, and the reviewer files a false
positive against house doctrine from memory. *"A suppression taken from memory
is the same defect as a finding taken from memory, in the direction nobody
audits."* All four reviewer agents now carry that clause.

## Citation coverage: 0/5 → 5/5

| Rubric | After both pieces |
|---|---|
| `dotnet-code-review` | common-extensions 6 · http-client-factory 3 · file-storage 2 · list-query-pipeline 2 · excel-miniexcel 1 |
| `dotnet-performance-review` | list-query-pipeline 4 · http-client-factory 1 · excel-miniexcel 1 |
| `dotnet-security-review` | file-storage 3 · http-client-factory 2 · common-extensions 1 |
| `dotnet-architecture-review` | common-extensions 2 · list-query-pipeline 1 |

## Final line counts

| File | Before | After | Note |
|---|---|---|---|
| `dotnet-code-review/SKILL.md` | 268 | 270 | +2 routing rows |
| `dotnet-code-review/references/review-rubric.md` | 868 | 929 | citations + new check 3.11 |
| `dotnet-performance-review/SKILL.md` | 503 | **504** | +1, justified below |
| `dotnet-performance-review/references/performance-checks.md` | 510 | 513 | |
| `dotnet-security-review/SKILL.md` | 508 | **508** | **0 net** |
| `dotnet-security-review/references/security-checks.md` | 457 | 458 | one *Refused* row |
| `dotnet-architecture-review/SKILL.md` | 459 | 465 | |
| `dotnet-architecture-review/references/conformance-checks.md` | 436 | 436 | unchanged |
| `agents/dotnet-code-reviewer.md` | 106 | 110 | |
| `agents/dotnet-security-reviewer.md` | 116 | 119 | |
| `agents/dotnet-performance-reviewer.md` | 117 | 119 | |
| `agents/dotnet-architecture-reviewer.md` | 105 | 107 | |
| both tester agents | — | — | **unchanged, deliberately** |

**The one line over the bar, justified.** `dotnet-performance-review/SKILL.md`
goes 503 → 504. Its routing table sent *"pagination"* to `api-surface`, but
checks 1.4 and 1.5 turn on which `ToPagedList` overload pages in the database and
on the reflection-derived search-field set — **neither fact is in `api-surface`**.
That row actively misrouted a reviewer holding either finding. The row is now
split contract/implementation. The 0-line alternative (one row, two owners)
preserves the misroute in halved form and breaks the table's one-owner-per-row
shape. `dotnet-security-review/SKILL.md` took its equivalent change as a
modify-in-place at **zero** net lines.

---

## THREE FALSE POSITIVES AGAINST SHIPPED HOUSE CODE — found, and closed

Worth more than the citation count. Each is a case where a rubric, run as written
against a project that recreated a shipped skill's canon **as instructed**, files
a finding against house doctrine. The rubrics' own rule: *"Raising settled house
design as a defect is worse than a gap in the report — the author learns the
whole document can be ignored."*

1. **code-review 4.2 vs `list-query-pipeline`.** 4.2 greps
   `"Console.Write\|Debug.WriteLine"`. The shipped canonical listing calls
   `Debug.WriteLine` at `query-expression-extension.md:223, 228, 249, 254`, and
   the *Deviations from corpus* table records that choice as **"Settled"**. Four
   guaranteed hits per correct recreation.
2. **code-review 4.1 vs `list-query-pipeline`.** 4.1 grades a `catch` that does
   not rethrow. The filter stage's catch arm is exactly that, and the owning
   skill says *"all of that is the contract… Do not convert this arm into a
   throw."* The nuance is preserved: the arm **is** a finding, but the defect is
   that it leaves no record, not the drop.
3. **security 3.3 vs `common-extensions`.** 3.3 greps `new Random(`, which
   returns the house shared generator that six of six corpus projects declare.
   The check now carries the shipped discriminator instead — hashable-before-
   storage may come from there, recoverable may not.

**Process note worth keeping:** Author A found #2 and rejected #1; Author B found
#1 and rejected #2. Neither found both. Same class of defect, same shipped file,
split cleanly between two independent drafts — which is the argument for the
two-author loop in one line.

---

## The one new check: `dotnet-code-review` 3.11

**Bar applied: a shipped anti-example with no reviewer anywhere, and a host
format that can carry it.** I walked all 32 anti-examples. Exactly one family
qualified — three anti-examples across three skills, one shape, zero reviewers:
`common-extensions` #12 (`WaitAsync()` without `await`), `file-storage` #9
(discarding the `Task` from a delete), `excel-miniexcel` (an `async` lambda to
`List<T>.ForEach`). Nothing reaches them: 3.2 grades the opposite defect
(blocking on a task), 3.3 needs the literal token `async void`, 4.1 needs a
`catch`, perf 2.6/2.7 are CS1998 and `Task.Run`.

**Smoke-tested against `reference/projects/ops-service`, in the exact form the
check ships:**

```
grep -rn --include=*.cs "^\s*_ = [A-Za-z_][A-Za-z0-9_.]*(" src/
```
→ **2 hits, 2 true positives, 0 false positives.**
`RecoveryPasswordService.cs:83` (`_ = sender.SendAsync(...)`) and
`UserService.cs:187` (`_ = s3AwsFileStorage.DeleteManyAsync(deleteAvatars,
cancellationToken)`) — the second is `file-storage` anti-pattern 9's exact shape,
live in the corpus. `grep "\.ForEach(async"` → **0 hits**, clean. I ran both
before either author reported; all three runs agree.

Three design points survived arbitration and are worth naming:
- **Severity HIGH by default**, not MEDIUM: section 3 is uniformly HIGH across
  3.1–3.10, and `common-extensions` #12 is an unawaited *semaphore acquire* — the
  gate is never taken.
- **The three passes are independent, not a sieve.** `_ =` produces no CS4014,
  and an `async` lambda binding to `Action<T>` produces none either, so a build
  alone misses both shipped shapes. Ordered by measured yield.
- **The `_ = await` filter is mechanical**, not a judgement call — it discards a
  *result*, not a task. Author A's original type-confirmation filter (which A
  self-named its weakest joint) was replaced.

**Provenance:** Author B's claim that `_ =` is "the sanctioned way to silence
CS4014" was **cut** — corpus-unverifiable compiler recall, refused under the
provenance law. The corpus-grounded observation kept in its place.

---

## A SHARED FALSE CLAIM — all four of us, corrected

In piece 1, both authors, the arbiter **and I** independently called archive
entry-path containment ("zip slip") a genuinely unowned surface, and I queued it
as a new-check candidate. **It is false.** Author A caught it in piece 2 by
opening the listing instead of the prose: `excel-miniexcel/references/
zip-extension.cs:139` takes the archive entry's **last path segment only**
(`Split(Path.AltDirectorySeparatorChar, …)[^1]`), `FormatFileName` (:212–225)
rebuilds `{directory}/{ticks}_{sanitized stem}{ext}`, and `:153` combines that
under the caller's temp root. **The entry's own directory components never reach
the filesystem — the shipped path is safe by construction.** I verified this
myself before accepting the reversal.

No check shipped. Author B's `Find:` extension to security 3.4 was **banked
rather than shipped** — a grep for a defect the house cannot have is pure
false-positive surface, and its 0 hits on ops-service prove only feature-absence.
What shipped instead is one row in `security-checks.md`'s *Refused — and why*
table, which is exactly what that table exists for.

**The generalisable lesson, and it caused two separate errors in this package:**
all four of us reasoned from a skill's *prose about* a mechanism instead of from
the mechanism's own listing. A `references/*.cs` file is ground truth for a claim
about what code does; the SKILL.md around it is not. The arbiter's own security
2.4 error (below) has the same root.

---

## Verdict log

**Piece 1** — arbiter loaded `skill-creator:skill-creator` live, no `Unknown skill`.

| Check | Verdict |
|---|---|
| CR 1.5 · file-storage | A |
| CR 3.4 · http-client-factory | A — a citation, not a scope change: 3.4's own `Find:` surfaces the transient sender, then the prose narrates scoped-only |
| CR 4.1 · list-query-pipeline | A (false positive #2) |
| CR 4.2 · list-query-pipeline | B (false positive #1); A's rejection falsified |
| CR 4.8 · http-client-factory | MERGE |
| CR 5.3 | REJECTED — routes an `api-surface` question under an `error-handling` check |
| CR 5.14 · common-extensions | B, owner only |
| CR 5.16 · common-extensions | MERGE |
| CR 7.2 · common-extensions | A, owner only |
| PERF 1.1 · excel-miniexcel | A, condition fronted |
| PERF 1.3 · list-query-pipeline | B — A's trap-fear answered by B's own wording |
| PERF 1.4 · list-query-pipeline | B — *Paging after materialising* states the defect; A's section states none |
| PERF 1.5 · list-query-pipeline | MERGE + correction |
| PERF-checks 2.10 · http-client-factory | MERGE |
| SEC 2.4 | MERGE — **coordinator overrode the arbiter** |
| SEC 3.3 · common-extensions | MERGE (false positive #3) |
| SEC 3.4 · file-storage | A |
| ARCH 2.1 | MERGE, A's owner set |

**Piece 2**

| Item | Verdict |
|---|---|
| Code-review routing rows (A: 4, B: 2) | **B, extended** — a routing row asserts an area the rubric *checks*. `list-query-pipeline`'s code-review citations are a silent catch arm and a console write; `file-storage`'s is a transaction shape. Neither is that skill's own subject. Arbiter extended B's reasoning to kill the `file-storage` row B had left unaddressed. **2 rows.** |
| Performance routing form | **B** — the existing row actively misrouted; A's append left the misroute in place |
| Security routing row | **B** — 0 lines both; B's wording quotes check 2.4's own title |
| Architecture routing row | **MERGE** — A's idiom, B's path (`Facades/Common/`) |
| New check 3.11 | **MERGE** — B's severity, owner clause and 3.3-overlap line; arbiter's pass order and filter |
| Zip containment check | **NEITHER — banked**; A's *Refused* row shipped instead |
| Undisposed `CreateScope()` | **DROPPED** — see banked #3 |
| Agent load lists | **Null result, accepted by A, B, the arbiter and me** — no additions |
| Agent standing-load repair | **Both, complementary** — A's open-list fix (one file) + B's suppression clause (all four) |
| Tester agents | **No change**, confirmed by both authors independently |

## Coordinator catches

1. **A shared blind spot at the doctrine's centre.** Both authors wrote the
   reflection-derived search set as *"minus `[NotSearchable]`"*. The shipped call
   is `GetPropertyRecursiveWithMaxDeep(1, typeof(JsonIgnoreAttribute),
   typeof(NotSearchableAttribute))` and SKILL.md:182 adds *"The walk always
   appends `[NotMapped]`."* **Three exclusion attributes, not one** — a reviewer
   using either draft over-counts the columns needing an index. Arbiter caught
   it; I verified it at the line before shipping.
2. **I overrode the arbiter on security 2.4, on evidence it had not read.** It
   dropped B's `http-client-factory` owner after a lookup found the settings
   partial carries only `Scheme`/`Host`/`EntityRoute`. Incomplete:
   `http-client-factory/SKILL.md:250` fills `ClientId`/`ClientSecret` from the
   settings section and anti-pattern 4 ships `settings.AccessKey`. The
   `httpclient` topic holds credentials exactly as `filestorage` does. **B was
   right; owner reinstated.** The arbiter accepted the override and named its own
   error: *"I stopped at the settings-partial example and generalised from it."*
3. **The shared zip claim, falsified** — section above.
4. **Every arbiter self-declared addition verified against the files:** the
   citation form; the `[NotMapped]` correction; the 1.3 rewording; the 3.4
   shortening; the 1.1 condition fronting; the arch repair-paragraph split;
   3.11's pass order, filter and severity floor.
5. **Citation form settled and applied uniformly:** `` `skill`,
   `references/file.md`, *Heading verbatim* `` — the form already shipping in
   these checks. B's invented `#9` dropped (the heading already begins "9.").
   `universal` is never deleted, only `reinforced by`.

---

## Considered and NOT changed — with reasons

| Candidate | Why not |
|---|---|
| **PERF *Comparison data — round trips*, search row** | `query-expression-extension.md:452` explicitly declines the cost model: *"the cost model is published by `ef-core-data-access` and graded by `dotnet-performance-review`."* A citation would assert the opposite of the cited file. Also, comparison data carries no owner by design. |
| **PERF-checks 1.8 (`MoreInfo`)** | `list-query-pipeline` defers the envelope's members to `api-surface` in writing. The existing owner is correct and complete. |
| **CR 5.3 (a member nothing reads)** | Same reason — the dead-envelope-member question is routed to `api-surface` by its own skill. |
| **CR 3.5 (mutable state on a singleton)** | The shape is a `readonly IHttpClientSender` field, which 3.5's grep cannot return. Citation only pays with a grep change. Went onto 3.4 instead, whose grep catches it unchanged. |
| **SEC 6.1 (mass assignment) / `file-storage` anti-pattern 2** | `S3FilePath` bound inbound is a wrong-**type** defect; 6.1(b) is about properties the caller must not **set**. Would misdescribe. Banked. |
| **ARCH `conformance-checks.md` 4.15** | Grades a **folder**; `common-extensions` anti-pattern 5 grades a **file**. Different unit, different `Find:`. |
| **ARCH *base facade set* listing** | Comparison data — carries no owner by the file's own rule. `FileStorage` already in the set. |
| **`excel-miniexcel` routing row anywhere** | 1 citation in performance (a fix-shaping condition), 1 in code-review. A doctrine row asserting an area no check reaches is a pointer with nothing behind it. |
| **`list-query-pipeline` / `file-storage` routing rows in code-review** | Their citations there are incidental to those skills' subjects (a catch arm, a console write, a transaction shape). |
| **`common-extensions` routing row in security; `list-query-pipeline` in architecture** | 1 citation each; the check's own owner clause already names file and heading inline. Not worth a line, one on an over-bar file. |
| **Additions to any agent preload list** | The measurement, above. Every one of the five sits below every incumbent floor. |
| **The two tester agents** | They load `dotnet-testing`, discover projects by suffix, build, run, and report from a closed verdict vocabulary. **They grade nothing**, so no citation reaches them and no new skill changes what they run. Both authors read both files and confirmed independently. |
| **The other 13 uncovered anti-examples** | Each is one shape in one skill with no general form (`TrimEnd` character set, the `GetType()` dead guard, the serializer read/write asymmetry, the hand-rolled key format, the tick-format conflation, the discarded upload `bool`, the checksum over decoded text, the undisposed ingest streams, the missing `startCell`, the template path composed by hand, the missing size ceiling, the sweep armed before commit, the clock-only file name). Thirteen checks the rubrics cannot carry, and all thirteen are now reachable through the owner clauses piece 1 added. |

---

## BANKED — found, evidenced, deliberately not shipped

1. **Security 2.4's grep cannot reach the credentials it is about.** The pattern
   `"(password|pwd|username|apikey|key|connectionstring)" *:` anchors the
   alternation in quotes, so `"AccessKey":`, `"SecretKey":` and `"ClientSecret":`
   do **not** match — the storage topic's two secret fields and the HTTP-client
   topic's. The citation now sends the reviewer to the right file; the grep still
   will not find the field there. One-token widening, but a `Find:` change needs
   its own smoke test and was outside this mandate.
2. **Code-review 3.5's grep cannot see a stateful reference-typed field on a
   singleton.** Both authors reached this independently. Widening: a pass for
   reference-typed `readonly` fields on singleton-registered types.
3. **An undisposed `CreateScope()` has no reviewer** (`common-extensions` #4).
   Smoke-tested: 7 hits on ops-service, 3 correctly `using`-scoped, 4 needing
   case-by-case reading — one holds the scope in a field whose disposer must be
   hunted. **Dropped rather than shipped**: a grep whose hits are judgement calls
   costs the check its credibility (the 0.3.56 lesson). Evidence recorded.
4. **Zip entry-path containment** — recorded in the *Refused* table, not checked.
   Revisit only if a shipped body ever states the containment property.
5. **`file-storage` anti-pattern 2 (`S3FilePath` on a request)** fits no check.
   Author B's placement instinct: `dotnet-code-review` section 5, near 5.21.
6. **The agents' preload criterion is factually false in three of four.** Each
   says "the bodies this rubric cites most", but code-review omits `api-surface`
   (13) and `dotnet-testing` (13) while preloading `message-keys` (8); security
   omits `facade-module-architecture` (7); performance omits `api-surface` (8).
   **Pre-existing, and untouched deliberately** — rebalancing changes what every
   review loads unconditionally and deserves its own evidence pass. Fold in
   `mediatr-messaging`'s missing code-review routing row (2 citations, no row);
   same class of omission. **Recommended as its own small solo item.**

---

## Proposed CHANGELOG fragment (main session renumbers)

```markdown
### 0.3.5x — the review layer can see the five new skills

The four rubrics cited the skills shipped at 0.3.48–0.3.52 **zero** times, and
the reviewer agents load what their rubric cites — so 32 anti-examples and five
skills' doctrine were invisible to enforcement. 0/5 → 5/5.

- **18 citations added** across the four rubrics' `· owner` clauses. Citation
  form settled: `` `skill`, `references/file.md`, *Heading verbatim* ``;
  `universal` is never deleted, only `reinforced by`.
- **Three false positives against shipped house code closed.** code-review 4.2
  greps `Debug.WriteLine`, which the canonical list-query listing calls 4× as a
  *Settled* deviation; code-review 4.1 grades the filter stage's catch arm, whose
  drop is the published contract; security 3.3 greps `new Random(`, which returns
  the house generator six of six corpus projects declare.
- **New check `dotnet-code-review` 3.11**, *A task started and never awaited* —
  the only anti-example family that was one shape across three skills with zero
  reviewers. Three independent passes (none subsumes another); smoke-tested at
  2 hits / 2 true positives / 0 false on the reference solution.
- **Routing tables**: 2 rows in code-review, 1 in architecture; performance's
  pagination row split contract/implementation because it *misrouted*; security's
  credentials row widened in place at zero net lines.
- **The agent repair is not a longer load list.** Measured: every new skill sits
  below every rubric's incumbent preload floor, and skills cited *more*
  (`api-surface` 13, `dotnet-testing` 13) are already excluded — so the citations
  are what make the standing conditional load fire. The real hole was that the
  standing sentence triggers only on *writing* a finding, while three of the new
  citations exist to **suppress** one. All four reviewer agents now load the
  cited body before relying on it to stay silent.
- **Refused, with the evidence**: archive entry-path containment. The shipped zip
  helper takes an entry's last path segment only and regenerates the name — safe
  by construction. Recorded in *Refused — and why* rather than checked.
- Two over-budget bodies: security **0 net lines**, performance **+1**, argued as
  a misrouting fix rather than findability.
```

---

## PROCESS VIOLATION — self-reported

**In the piece-2 arbiter round I compressed both authors' drafts instead of
forwarding them verbatim, and labelled the compression as verbatim.** The loop's
law is explicit — drafts go to the arbiter *verbatim, never summarized* — and
S13b needed a second round for exactly this failure. Piece 1 was forwarded
correctly and in full; piece 2 was not.

Mitigating, but not excusing: the compression preserved every proposal's exact
text — row wording, check wording, anchors, severities, greps, hit counts, both
authors' self-flagged weaknesses and both authors' questions — and the arbiter
ruled against each author on points its own compressed rendering had preserved
(it reversed A on the routing-row count and on pass ordering, and cut B's
provenance claim), which is evidence it had enough to rule. But I cannot claim
the verdict is as sound as piece 1's, and the main session should weigh piece 2's
rulings accordingly. Recording it rather than letting it pass silently.

## Also worth flagging

The item-3 agent edits change **what a reviewer must do before staying silent**,
which is close enough to report discipline that Author B asked whether project
memory's *"never edit a skill's report rules without showing the wording first"*
applies. The arbiter ruled it does not — that memory governs what a report
*says*, and these are agent files governing when a body is *loaded*. I agree, and
have shown the exact shipped wording above regardless, so the main session can
veto it cheaply.

## Refused

Nothing. No item in the brief was declined.
