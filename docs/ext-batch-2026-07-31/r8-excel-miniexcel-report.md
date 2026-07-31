# R8 label implementation — `excel-miniexcel` (Group 3) — coordinator report

## 1. Status: COMPLETE

All five approved labels (E2, E4, E5, E7, E8) re-verified against the corpus and
shipped. Full three-way loop run: arbiter loaded `skill-creator:skill-creator`
LIVE (no `Unknown skill`, no cache reads); both authors drafted the set
independently and synchronously; drafts relayed to the arbiter VERBATIM; verdict
**MERGE** with per-entry sub-verdicts; all arbiter self-declared additions
coordinator-verified against files before writing.

**The loop earned its keep on this piece.** Both authors independently found a
contradiction my brief had missed — see §5 catch 1 — which turned a
five-entry append into a five-entry append *plus* three reconciling edits to the
skill's own canonical reference. Shipping E4 without those edits would have left
the skill condemning a shape its own reference demonstrates.

## 2. Per-label verdicts

| # | Label | Result | Corpus site I confirmed (sanitized) |
|---|---|---|---|
| E2 | Unbounded upload size gate | **VERIFIED — shipped, severity corrected** | Two import endpoints in one solution carry `[DisableRequestSizeLimit]` + `[RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue)]` on the `[HttpPost]`. |
| E4 | Cleanup job scheduled inside the transaction, before commit | **VERIFIED — shipped, consequence re-anchored** | Two flows in one solution's generic import service place the job-schedule call between `AddRangeAsync` and `CommitTransactionAsync`. |
| E5 | `Console.WriteLine` + `Stopwatch` probe in a production import path | **VERIFIED — shipped** | A private per-row media helper in an import service ends with a stopwatch reading written to stdout; the same file routes genuine failures through the project's logging facade in two other places. |
| E7 | `async` lambda in `List<T>.ForEach` inside a `finally` | **VERIFIED — shipped** | One site, corpus-wide: a `finally` disposes a list of request objects holding open file handles via `ForEach(async x => await …)`. |
| E8 | `DateTime.UtcNow.Ticks` as the uniqueness prefix | **VERIFIED — shipped NARROWED** | A shared file-name formatter builds `{directory}/{ticks}_{name}{ext}`, reached from the zip helper's per-entry save, called once per media entry from the import loop. |

Nothing dropped. Two labels shipped at **lower** severity than the decision
table proposed — both corrections are load-bearing, both are recorded below.

## 3. Severity corrections (brief §2: ship the honest weaker framing)

**E2 — the decision table's stated reason is FALSE.** It reads "resource
exhaustion on an anonymous-ish path". Both endpoints carry a permission
attribute on the same method. The shipped entry says so in its own text —
"authenticated and permission-gated, so this is not an open door" — and reframes
the defect as *an absent decision*: the ceiling is now whatever the fronting host
happens to allow, and nothing in the file records a size anyone chose. No
unauthenticated-DoS claim ships.

**E8 — "real collision window under concurrency" is too strong.** The per-run
temp directory already carries a `Guid`, so two concurrent imports cannot reach
each other's files. The real window is *inside one run*: two rows whose media
folders each hold a file with the same leaf name, flattened into one directory.
The shipped entry states that scope explicitly and does not reopen the
cross-request half.

**E4 — the consequence is re-anchored, not asserted as corpus behaviour.** See
catch 2.

## 4. Budget route

**Route taken: `references/anti-patterns.md`** (brief §5), because five entries
inline would have pushed SKILL.md to roughly 600 lines against a hard bar of 500,
and this skill's previous arbiter had already ruled that further compression of
the body costs content.

| File | Before | After |
|---|---|---|
| `skills/excel-miniexcel/SKILL.md` | 472 | **483** (9-line pointer block + separator) |
| `skills/excel-miniexcel/references/anti-patterns.md` | — | **219** (new) |
| `skills/excel-miniexcel/references/import-service-pattern.md` | 562 | **568** (3 reconciling edits) |

The six existing anti-pattern entries in SKILL.md are untouched and unrenumbered;
the new material is appended as a sub-section at the end of `## Anti-patterns`,
immediately before `## Decision Guide`.

## 5. Verdict log

**Piece: the five-entry anti-example set (drafted whole by both authors).
Verdict: MERGE.**

| Entry | Sub-verdict | Reason |
|---|---|---|
| E2 | MERGE (A's frame + 2 of B's sentences) | A's "what is missing is a decision" is the honest post-correction frame; B's "reached for after a legitimate workbook was rejected" and "raise the constant, do not delete it" make it actionable. |
| E4 placement | MERGE — ships **with** reconciling edits | A's argument survives the no-transaction-claim constraint intact; B contributed the decisive sentence ("nothing about the job store has to be known to see that the ordering is right"). |
| E4 predicate | **A** — stays folded, no fix attached | B's fix adds `x.CreatedAt <= stagedBefore` to an interface that declares exactly one member, and rewrites two §7 signatures from inside an anti-example. |
| E5 | MERGE, mostly B | B's `list-query-pipeline` cross-reference verified true and kept; A's "least likely to keep" clause cut. |
| E7 | MERGE | A's two-consequences-from-the-language framing + B's more general closing rule; B's `Task.WhenAll` sentence cut. |
| E8 | **A**, corrected | B's GOOD block threads sheet-supplied text into a file path *and* silently changes a `private` method in shipped canon while claiming no edit is needed. |

### Coordinator catches (all file-verified by me)

1. **SHARED FINDING, TRUE, and my brief missed it.** Both authors independently
   reported that `references/import-service-pattern.md` places
   `ScheduleAutoClean();` *inside* the `try`, before the commit, at §5 and §6 —
   i.e. E4 would have condemned this skill's own shipped canon. I confirmed both
   sites directly. Resolution: E4 ships **together with** three reconciling edits
   (below). The alternative — withholding a verified rule to protect a reference
   that teaches the condemned shape — is the worse trade.

2. **Author B's separate discovery is TRUE but B stated it imprecisely, and the
   imprecision mattered.** B reported that the corpus sweep lacks
   `IgnoreQueryFilters()`. Verified: it does lack it — but B implied the service
   has no such accessor, whereas the service carries a *private* query helper
   that does apply it and the sweep simply bypasses it. Since the entity filters
   are mutually exclusive with the sweep's predicate, **the corpus sweep selects
   nothing**. Consequence for E4: the "a second import is swept early" outcome is
   **not reproducible in the corpus** — it is masked there by a different defect.
   It *is* a true consequence of the flow this skill prescribes, because §7's own
   `AutoCleanAsync` carries `IgnoreQueryFilters()` with the user-id predicate
   (verified). The shipped text therefore anchors the claim explicitly — "on the
   flow this skill prescribes … (`references/import-service-pattern.md` §7)" —
   and never presents it as observed corpus behaviour. B's entry text had
   asserted it flat; A's had too.

3. **B's E4 fix is not grounded in this skill's canon.** `IImportable` declares
   exactly one member, `Guid? ImportSessionId` — no timestamp. B's `stagedBefore`
   fix adds a column to shipped canon from inside an anti-example. Cut.

4. **A's objection to B's E8 fix is well-founded and I upheld it.** B threads the
   row's media-folder name — text read off the uploaded sheet — into a file path.
   A refused that on the grounds it introduces a defect. Additionally I verified
   B's GOOD block shows a three-argument formatter, while the shipped
   `FormatFileName` is `private` with two parameters — so B's "no edit to
   `zip-extension.cs` is strictly required" is inconsistent with B's own code. A's
   fix ships: it uses the directory parameter `SaveImage` already exposes, and
   `zip-extension.cs` genuinely needs no edit.

5. **Provenance handling of `FileMode.Create` differed between authors.** B
   marked its overwrite-not-throw semantics as documentation-derived; A asserted
   it inline as plain fact. B's handling is the compliant one and is applied to
   A's winning text.

6. **Verified the arbiter's own additions** rather than trusting them. Two were
   substantive: (a) it replaced A's `Path.Combine` with
   `string.Join(Path.AltDirectorySeparatorChar, …)` — I confirmed the format
   string is `/`-based and the leaf is recovered by an
   `AltDirectorySeparatorChar` split, so `Path.Combine`'s `\` on Windows would
   have been spliced into a `/`-formatted path; the `string.Join` form is the
   house idiom at two verified sites. (b) It added "`zip-extension.cs` needs no
   edit for this" — I confirmed `SaveImage` takes the directory as a parameter
   and its `Directory.CreateDirectory` call creates intermediate segments, so a
   nested run segment works unchanged.

7. **Verified the arbiter's reconciling-edit text matched the file byte-for-byte
   before applying it** (it had reproduced surrounding comment lines from its
   read). It did. I also confirmed after editing that §6's braces still close
   correctly: the schedule now sits after the inner transaction block but still
   inside the outer `try` whose `finally` clears the temp directory — correct,
   since the inner `catch` rethrows and the schedule is unreachable on failure.

8. **Arbiter flagged one claim of Author A it could not confirm** — A's rationale
   described a corpus flow calling the folder-clear helper inside the `try` and
   again in the `catch`; in the file the arbiter read it is in the `finally`. Not
   used by either draft, so nothing shipped depends on it. Recorded, not chased.

### Reconciling edits applied to `references/import-service-pattern.md`

Three, all inside this skill's own directory (within write scope):

1. **§5** — `ScheduleAutoClean();` moved out of the `try`, to below the `catch`.
2. **§6** — same move; it now sits after the inner transaction block, still
   inside the outer `try`/`finally` that clears the temp directory.
3. **§7** — three-line prose note added explaining *why* the call sits after the
   transaction block, pointing at `references/anti-patterns.md`. Placed
   immediately before the existing `background-worker` line, which is untouched.

`AutoCleanAsync`'s predicate is **not** edited — the marker is the settled
staging design; only the placement changed.

## 6. Refusals and things deliberately not claimed

- **No claim, in either direction, about whether a job-store enqueue is enrolled
  in the database transaction.** This skill's previous coordinator refused that
  claim as unverifiable library-API recall; I held the line. E4's teaching is
  built entirely on the dependency being unresolvable *from the code*, which
  holds whichever way the job store behaves.
- **No claim about system-clock resolution or granularity** in E8. The entry is
  grounded structurally: a wall-clock read is the only distinguishing component
  the code offers.
- **No claim that `DisposeAsync` on any particular stream type suspends** in E7.
  The wording is that `ForEach` *can* return with disposals outstanding; the
  temp-directory point is phrased as ordering established against nothing.
- **`Task.WhenAll(… .AsTask())`** cut from E7 — B self-flagged it as untested
  prose with no corpus site.
- **"the one output a deployed host is least likely to keep"** cut from E5 —
  A self-flagged it as environment recall.
- **"The body is buffered and parsed before a single row is seen"** cut from E2 —
  framework recall, and the ordering is wrong for the packaged flow anyway.
- **No route to `background-worker`** anywhere in new text — it is not a shipped
  skill. The three pre-existing dangling references to it in
  `import-service-pattern.md` are left exactly as they were; I added no fourth.
- **The corpus's missing-`IgnoreQueryFilters` sweep is not mentioned in any
  shipped artifact.** It is a real, verified defect but is not one of the five
  approved labels, and labelling is R8.

## 7. Sanitization

Swept the new file and SKILL.md for every project name, business-domain noun,
corpus facade type name and real path in the exclusion list — **clean, zero
hits**. Placeholders used: `Entity`, `ImportEntityData`, `EntityMedia`,
`MediaRequest`, `ImportEntityRequest`, `SuccessResultWrapper`. The stdout probe
literal is neutralized to `"[resize]"` (neither author's literal, and not the
corpus's). The logging facade is referred to only as "the project's logging
facade", never by type name.

## 8. Proposed CHANGELOG fragment (main session renumbers)

```
feat(excel-miniexcel): five approved R8 anti-examples + reference reconciliation (0.3.XX)

- New references/anti-patterns.md (219 lines): unbounded upload size gate;
  cleanup sweep armed before the commit; async lambda in List<T>.ForEach inside
  a finally; Console.WriteLine+Stopwatch probe in the import path; a clock read
  as the sole distinguishing component of a saved media file name. SKILL.md
  gains a 9-line pointer block (472 -> 483, hard bar <500); the six existing
  entries are untouched and unrenumbered.
- Two severity corrections against the decision table, both shipped honest:
  E2's "anonymous-ish path" premise is FALSE (both endpoints are
  permission-gated) - reframed as an absent decision, not a DoS; E8's
  "collision window under concurrency" is too strong (the run directory already
  carries a Guid) - narrowed to within-one-run leaf-name collision.
- E4 ships WITH three reconciling edits to references/import-service-pattern.md
  (§5 and §6 move ScheduleAutoClean() below the transaction block; §7 gains a
  three-line why). Both authors independently caught that the reference
  demonstrated the shape the anti-example condemns. AutoCleanAsync's predicate
  is NOT changed - the marker is settled staging design.
- E4's blast-radius consequence is anchored to §7's prescribed sweep, not
  asserted as corpus behaviour: the corpus sweep bypasses the service's own
  IgnoreQueryFilters accessor and selects nothing.
- Provenance refusals: job-store/transaction enrolment (either direction);
  clock resolution; DisposeAsync completion semantics; Task.WhenAll disposal
  form; buffered-parse ordering. FileMode.Create overwrite semantics ships
  inside a marked documentation-derived block.
```

## 9. Open / banked for the user (non-blocking)

1. **A sixth-entry candidate the arbiter argued for, R8 — the user's call.** The
   corpus staging sweep queries through the repository directly with no
   `IgnoreQueryFilters()`, while the entity's global filters are mutually
   exclusive with its predicate — so it deletes nothing and abandoned rows are
   never collected. What makes it a *shape* rather than an omission: the same
   class carries a private accessor that does apply the call, and the sweep
   simply bypasses it. It needs no new fix text — §7's `FindStaged` already ships
   the corrected accessor. Not shipped; not on the approved five.
2. Two misspelled private helper names in the same import service — typo family,
   BỎ per the standing exclusions; noted only because both authors saw them.
3. Author A described a corpus flow clearing the temp folder inside the `try` and
   again in the `catch`; the arbiter could not confirm it in the file it read
   (there it is in the `finally`). If the user wants it examined, the file needs
   naming.
4. `references/import-service-pattern.md` still routes twice to
   `background-worker`, which is not a shipped skill — pre-existing dangle, out
   of this piece's scope, for whichever session owns that file next.

## 10. Write scope honoured

Written: `skills/excel-miniexcel/references/anti-patterns.md` (new),
`skills/excel-miniexcel/SKILL.md` (pointer block appended),
`skills/excel-miniexcel/references/import-service-pattern.md` (3 reconciling
edits), and this report. **No git, no router, no manifests, no CHANGELOG, no
sibling skill touched.** `git status` shows only this skill's files plus the
unrelated pre-existing `skills/http-client-factory/references/anti-patterns.md`
from a sibling coordinator.
