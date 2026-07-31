# SoftDeletes addition — coordinator report (house-laws §8)

## 1. Status: COMPLETE (re-run, 2026-07-31)

First run BLOCKED on the arbiter's `Unknown skill: skill-creator` (its record is
preserved below in §1a). The re-run followed the relaunch addendum: no corpus
re-survey, no author respawn — both preserved drafts were relayed to a fresh
`skill-arbiter` VERBATIM (the agent read `softdeletes-authorA-draft.md` and
`softdeletes-authorB-draft.md` in full) together with the shared-claim checklist
from §6. **skill-creator loaded live on the arbiter's first action.** Verdict
issued, coordinator verification pass run (§9), final text assembled to
**`softdeletes-draft.md`** (section + references file + description delta +
anchor). Nothing under `skills/` was written; the main session assembles at
merge time. Arbiter agent id: `a94b0074c00b2dfdb` (continuable via SendMessage
if the user's R7/R8 rulings require a redraft).

### 1a. First-run blocking record (preserved)

The first arbiter run reported `Unknown skill` for `skill-creator:skill-creator`
(both name forms tried; no self-heal, no cache read, nothing judged). Both
author drafts were complete and were preserved verbatim for exactly this re-run.

## 2. Files written (scratchpad only — nothing under skills/, per my brief)

| File | Content | Lines |
|---|---|---|
| `softdeletes-authorA-draft.md` | Author A full draft, verbatim (first run) | ~590 |
| `softdeletes-authorB-draft.md` | Author B full draft, verbatim (first run) | ~640 |
| `softdeletes-draft.md` | **FINAL deliverable**: anchor + `## Soft delete` section + full `references/soft-deletes.md` + description YAML + routed-upward list | ~560 |
| `softdeletes-report.md` | this report | — |

Assembly math: shipped SKILL.md measured **341 lines** (arbiter said 342 — off
by one); section ≈ 141 lines ⇒ assembled ≈ **483 lines**, inside the <500 bar.

## 3. Description / router material — FINAL

- **Verdict: Author B's delta, verbatim — 99 words by `wc -w`, measured twice**
  (once by the arbiter, once by me post-verdict). Two compressions
  (`IRepositoryWrapper and RepositoryBase` → `IRepositoryWrapper/RepositoryBase`;
  `DatabaseSettings and connection strings` → `DatabaseSettings, connection
  strings`) pay for the insertion `soft delete via ISoftDelete/IHidden,
  DeleteAt/HiddenAt, IgnoreGlobalQueryFilter` after `OnDelete`. `Not for:`
  roster untouched. Full YAML in `softdeletes-draft.md` §4.
- A's delta REJECTED (base miscounted 85 vs measured 96; delta lands over the
  100-word bar).
- No new router row proposed — the addition extends an existing skill; whether
  the router's ef-core row gains a "soft delete" arm is main-session work at
  merge time. Suggested arm if wanted: `soft delete / ISoftDelete / IHidden /
  IgnoreGlobalQueryFilter → ef-core-data-access`.
- Proposed `Not for:` additions to EXISTING siblings: **none**. The reciprocal
  `common-extensions` roster entry stays routed upward (§8.3).

## 4. Proposed CHANGELOG entry (main session renumbers)

> ef-core-data-access: new `## Soft delete` section + `references/soft-deletes.md`
> — interface-based opt-in (`ISoftDelete`/`IHidden`, nullable timestamp stamps,
> BaseEntity carries no flag per facade-module-architecture), repository-level
> automatic filter injection in Find/Count/CountAsync/Any/AnyAsync,
> `IgnoreGlobalQueryFilter` escape hatch (property-matching semantics — a
> caller-written condition on the same stamp is also cleared),
> `ISoftDelete.SqlFilter` partial unique indexes, delete-is-an-update mutation
> flow, root-set-only rule (filtered Includes and computed expressions write the
> stamp check by hand). Ungrounded restore-of-DeleteAt claims stripped
> (provenance law). Description gains soft-delete trigger nouns (96→99 words).

## 5. Verdict log

| Piece | Author A | Author B | Arbiter | Coordinator |
|---|---|---|---|---|
| SKILL.md `## Soft delete` section | drafted | drafted | **MERGE** — B's escape-hatch semantics + framing; A's root-set-only rule (B lacked it); A's neutral stamp-and-UpdateAsync delete example (B's GetByIdAsync delete example found ungrounded by arbiter's own grep) | verification pass §9: PASS |
| `references/soft-deletes.md` | drafted | drafted | **MERGE** — B's structure/prose spine; A's two-conditions-explained visitor prose; cleanup ratified (typo fix, dead branch dropped, shadowed `x` renamed); A's retrofit appendix CUT | verification pass §9: PASS |
| Description delta | drafted | drafted | **B** — only compliant delta (99 words) | re-measured: 99 ✓ |
| Anchor | end of SKILL.md | end of SKILL.md | **confirmed** (both identical) | base measured 341 lines, tail verified ✓ |

Arbiter's explicit dispositions (all five checklist items + open items ruled;
full detail in its returned verdict):
- Restore/un-delete claims **STRIPPED both sides** (not even doc-marked —
  provenance law refuses, doesn't hedge).
- A's corpus-false "touches nothing else" escape-hatch sentence NOT shipped;
  B's property-matching semantics ship in both places.
- Automatic injection canonical — arbiter re-derived independently on four
  structural grounds, did not lean on the authors' convergence. R7 half routed
  to the user (§8.1).
- Retrofit appendix CUT; its one groundable consequence survives as the
  pre-scaffold-guard paragraph (arbiter addition #3).
- Migration/backfill guidance REFUSED under provenance law; only the checklist
  clause "a migration carries it" ships. Knowing gap, stated as such (§8.4).
- Fidelity: `HiddenEntension`→`HiddenExtension` fixed; unreachable null branch
  dropped with an explanatory note; shadowed inner `x` renamed;
  `CheckExpression`/`Filter` event kept OUT with the wiring reframed as "the
  soft-delete portion of the five members" (only the four SoftDeletes/ files
  carry the verbatim promise).
- B's semantic table re-grounded as observation ("Nothing clears the stamp" —
  zero `DeleteAt = null` sites) rather than intent-inference; no softening
  marker needed.

Delegated judgment calls exercised (blanket batch approval, all logged):
- First run: canonical substrate = apsp+mtc byte-identical four files;
  both consumption models fed neutrally to both authors; A's shipped-text edit
  routed upward; R8 labelling withheld. (Preserved from first-run report.)
- Re-run: relayed drafts by having the arbiter read the preserved verbatim
  files (bytes identical to the author outputs — satisfies VERBATIM without a
  lossy re-paste); accepted the arbiter's five self-declared additions after
  corpus-checking each (§9); accepted the 341-vs-342 base-count discrepancy as
  immaterial (assembled ≈483 < 500) and recorded the corrected number in the
  draft's anchor statement.

## 6. First-run coordinator catches (all relayed to the arbiter and disposed)

1. A's description base count wrong (85 vs 96) — CONFIRMED by arbiter; A's
   delta rejected.
2. A's section "touches nothing else" escape-hatch sentence corpus-false —
   CONFIRMED against `RemoveGlobalQueryFilterNodeVisitor.cs`; not shipped.
3. Shared blind spot: restore/un-delete of ISoftDelete ungrounded
   (`DeleteAt = null`: zero corpus hits; only `Hidden(false)` reverses) —
   STRIPPED both sides.
4. B's supporting claims verified (UnderscoreTable table-name-only; no index
   filter on HiddenAt; `Messages<T>` precedent — now moot, example cut).
5. Shared code claims verified (dead null branch unreachable; `HiddenEntension`
   typo in both projects; 3 escape sites all `typeof(IHidden)`; no
   `HasQueryFilter` anywhere).
6. Both authors' convergence on automatic injection treated as a shared claim —
   arbiter re-derived independently; holds.

## 7. Variant comparison (unchanged from first run; pre-authorized best-or-synthesize)

| Item | apsp-backend | backend-mtc | digitalcity | Winner / synthesis |
|---|---|---|---|---|
| ISoftDelete.cs | DeleteAt + SqlFilter const | byte-identical | reduced (no SqlFilter), 0 consumers | apsp/mtc (identical) |
| IHidden.cs + HiddenObject | present | byte-identical | absent | apsp/mtc; typo `HiddenEntension`→`HiddenExtension` fixed (arbiter-ratified) |
| GlobalQueryFilterExtension.cs | present | identical (BOM only) | absent | apsp/mtc |
| RemoveGlobalQueryFilterNodeVisitor.cs | present | byte-identical | absent | apsp/mtc; shadowed inner lambda param renamed (arbiter-ratified) |
| Consumption model | opt-in extensions — 0 entities, 0 call sites | automatic injection in RepositoryBase, 5 entities, 3 escape sites | none | mtc's wiring (arbiter-ratified on independent grounds; R7 ratification routed to user) |
| Partial unique index | `HasCitextUnique(expr, string? filter = null)` | + separate `HasCitextUniqueHasFilter` | n/a | apsp single-method form (matches shipped SKILL.md) |
| Dead null-branch in mtc `Find` | n/a | present, unreachable | n/a | dropped, with explanatory note (arbiter-ratified) |
| `CheckExpression` / `Filter` event in mtc `Find` | n/a | present | n/a | **kept OUT** (arbiter finding — unrelated per-repository filter mechanism; wiring reframed as soft-delete portion only) |

## 8. Open questions / parked items (for the main session / user)

1. **R7 ratification:** the taught shape (automatic injection) is the corpus's
   only functional wiring but lives in backend-mtc, not apsp (which carries the
   unconsumed opt-in family). All four parties (A, B, arbiter, coordinator)
   rule the code compels the automatic shape. Formal ratification is the
   user's; the arbiter offered to redraft if overruled (agent id in §1).
2. **A's one-line cross-reference in the shipped `### The surface` section** —
   edits shipped text, routed upward. Arbiter's opinion: add it; sentence text
   preserved in `softdeletes-draft.md` §5.1.
3. **Reciprocal `common-extensions` `Not for:` roster entry** — would bust the
   100-word bar without further compression; also batch-ordering-dependent.
   Routed upward. (If common-extensions slips the batch, the body pointer
   dangles harmlessly — the `Join` signature is spelled out in the references
   file, so a reader is not blocked.)
4. **Migration/backfill gap** — deliberately absent under provenance law. If
   the user wants it covered, it needs a user-approved documentation-derived
   block (arbiter declined to write one under standing delegation).
5. **Anti-example candidates banked (R8 — none labelled).** Strongest, newly
   surfaced by the arbiter: **the live no-op `.IgnoreQueryFilters()` call**
   (mtc TariffPackageService.cs:389 — no `HasQueryFilter` registered, so it
   changes nothing, in a query relying on the repository-injected filter;
   matches this coordinator's first-run finding independently). Also banked:
   dead null-branch in `Find`; `Where(_ => true)` on unfiltered entities;
   `HiddenEntension` typo; ApplySoftDelete/HiddenObject public-private
   asymmetry; the opt-in family as a shape; copy-pasted identical XML comment
   on both stamps; `HasCitextUniqueHasFilter` two-method split.
6. **B's semantic distinction (ISoftDelete permanent vs IHidden reversible)** —
   shipped as usage-census observation, not intent. A one-line user
   confirmation would still upgrade it to doctrine.

## 9. Re-run coordinator verification pass (post-verdict, all corpus-checked)

Arbiter's five self-declared additions, each verified:

1. **Filtered-`Include` form added to root-set-only section** — grep across
   backend-mtc (worktrees excluded): 13 hand-written `DeleteAt == null` /
   `HiddenAt == null` sites outside the SoftDeletes folder, **5 inside
   `Include(`** — matches the claim (arbiter said 12 total; 13 measured — the
   discrepancy is one site, immaterial to the rule). VERIFIED.
2. **Doc-derived block's "a call to it here changes nothing"** — the single
   live `.IgnoreQueryFilters()` call confirmed at
   `Infrastructure/Modules/TariffPackages/Services/TariffPackageService.cs:389`;
   zero `HasQueryFilter` corpus-wide. Corroborates my independent first-run
   finding. VERIFIED.
3. **Pre-scaffold-guard sentence** (wiring changes every existing
   Find/Count/Any at once) — direct structural consequence of the generic
   composition; modality kept as "worth announcing", not a ban. VERIFIED.
4. **`Hidden(bool)` fence compressed to prose citing the `SetCustomer` shape**
   — `SetCustomer` confirmed in shipped SKILL.md at lines 238 and 339; the
   full setter code remains in the references file, so nothing is lost.
   VERIFIED.
5. **Line budget** — shipped base measured 341 lines (not 342); assembled ≈483,
   inside <500 and honest about the top of the 117–450 norm. VERIFIED with the
   one-line correction recorded in the draft's anchor.

Arbiter's own new corpus finding checked: `CheckExpression` + `Filter` event
exist in mtc `RepositoryBase` (lines 13/57/133/174/176) — both authors had
silently dropped them; the arbiter's keep-out ruling with the "soft-delete
portion" reframing is honest and prevents a false verbatim promise. VERIFIED.

Modality diff (both directions): the `typeof(ISoftDelete)` escape hatch stays
"argue about before writing" (permission, not ban — corpus never does it, no
user ban exists); recreate-doctrine obligations ("do not write a local
variant") match house-laws §3's intended modality; no corpus code is framed as
a defect (R8 clean — B-style neutral design arguments only). Rephrasing diff:
the BaseEntity boundary sentence preserves the shipped doctrine's content
("carries no soft-delete flag" → "carries neither stamp, and it never gains
one" — same claim, both interfaces covered). Description re-measured at 99
words. Sanitization scan of the final text: no project names, no
business-domain nouns, no real paths; Order/Lines/Code placeholders match the
shipped skill's own example vocabulary. PASS.
