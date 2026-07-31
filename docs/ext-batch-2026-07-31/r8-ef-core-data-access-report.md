# R8 label implementation — `ef-core-data-access` (Group 1: S1, S2, S8)

## 1. Status: COMPLETE

All three approved labels VERIFIED against the corpus and shipped. Nothing
dropped, nothing invented. Three-way loop run in full (both authors → arbiter →
coordinator verification pass). `skill-creator:skill-creator` loaded LIVE on the
arbiter's first action — no `Unknown skill`, no cache read.

Files written: `skills/ef-core-data-access/SKILL.md` (reflow only, net zero
lines) and `skills/ef-core-data-access/references/soft-deletes.md` (three
entries). Nothing else touched — no git, no router, no manifests, no CHANGELOG,
no sibling skill.

Agent ids (continuable): Author A `aac00206a12328f3a`, Author B
`af8d19b8b8c1d5049`, arbiter `ad45417a4a448542e`.

## 2. Per-label disposition

| # | Label | Verdict | Corpus site confirmed (sanitized) |
|---|---|---|---|
| S1 | No-op `.IgnoreQueryFilters()` where no `HasQueryFilter` is registered | **VERIFIED — shipped** | A service method in one reference project composes `Find(...)` → `.IgnoreQueryFilters()` → `ProjectTo` → the list-query call. The entity it queries is declared `: BaseEntity, IHidden` — genuinely stamped, so the intent to read past the stamp is legible. That project registers **zero** `HasQueryFilter`; the corpus's only two `HasQueryFilter` registrations live in a *different* project and are staging-import filters (`ImportSessionId`/`CombineToId`), not stamps. That project's `RepositoryBase.Find` composes both stamps as an ordinary `Where`. |
| S2 | Dead `if (expression == null)` branch in `Find` | **VERIFIED — shipped, honest weaker framing** | Same project's `RepositoryBase<T>.Find`: `expression = ApplySoftDelete(expression).HiddenObject();` then `if (expression == null) { return isAsNoTracking ? …AsNoTracking() : …Set<T>(); }`. Both helpers end `return predicate ?? (_ => true);` **and both are declared to return a non-nullable `Expression<Func<T, bool>>`** — dead by signature, not only by the tails. |
| S8 | `HasCitextUniqueHasFilter` as a second method instead of an optional parameter | **VERIFIED — shipped, honest weaker framing** | One project's shared configuration extension declares `HasCitextUnique(builder, indexExpression)` (no filter path at all) **plus** `HasCitextUniqueHasFilter(builder, indexExpression, string? sql)` whose body **duplicates** the first's and appends `.HasFilter(sql)` unconditionally. The other **five** projects declare the single canonical `HasCitextUnique(builder, indexExpression, string? filter = null)` applying `.HasFilter` only when non-null — the form the shipped SKILL.md already teaches. |

**Severity honesty (brief §2, no inflation).** S2 is **not a live bug** — the
entry opens by saying so; it ships as a trap plus a contract misstatement. S8
**breaks nothing today** — the one call site that needs the filter does pass it;
the entry ships as a split-surface/unsafe-default defect, not a broken index.
Only S1 is a live defect, and even there the shipped text claims no wrong rows
(see catch 1).

**R25 for S8.** Cited by its reasoning only — "A shared configuration extension
is where an existing helper is *extended*, not where a parallel entry point is
added beside it" — with the mechanism spelled out (a trailing optional parameter
breaks no existing call site). No skill named, no rule number quoted, per the
launch message.

## 3. Budget route taken

**Route: no `references/anti-patterns.md`, and net-zero lines in SKILL.md.**

`SKILL.md` measured **499 lines** against a hard bar of **< 500** — zero
headroom, so the brief's "put it in references/" fallback had to go one step
further: even a 4-line pointer block would have crossed the bar. Both authors and
the arbiter converged independently on the same resolution, and it is also the
better fit for this skill:

- All three entries land in the existing `references/soft-deletes.md`, **woven
  beside the positive form each one inverts** — not gathered into a new section.
  This skill has **no `## Anti-patterns` section** in either file; it teaches
  negatives inline in bolded-lead prose ("…is the drift this pattern exists to
  remove"; "an unfiltered one is the defect that surfaces weeks later as 'this
  code is taken'"). A gathered block would have been the only structure in the
  file with no positive anchor, and would have forced each entry to restate the
  GOOD listing it inverts.
- SKILL.md gains discoverability through an **exactly-8-line replacement** of the
  shipped paragraph at lines 370–377, so the file stays at 499.

**Insertion points** (all three verified against the file by me, then again by
the arbiter): after line 141 (end of `## GlobalQueryFilterExtension.cs`), after
line 239 (the "no null branch to write after the chain" contract sentence), after
line 293 (end of `## The entity's opt-in`).

### Final line counts

| File | Before | After | Bar |
|---|---|---|---|
| `SKILL.md` | 499 | **499** | < 500 ✓ (net zero, as required) |
| `references/soft-deletes.md` | 308 | **374** (+66) | no bar; entries are 23 + 21 + 22 incl. separators |
| `references/query-conventions.md` | 109 | 109 (untouched) | — |

Width check: every line of inserted **prose** is ≤ 79 columns. One inserted
**code** line runs to 83 (`return isAsNoTracking ? dbContext.Set<T>()…`), copied
from the shape under discussion; the file's existing code listings already run to
176 columns, so this is in-norm. The reflowed SKILL.md paragraph measures
79/80/65/76/77/63/74/79 — lines 1–6 byte-identical to shipped, and the 80 on line
2 is shipped text unchanged.

## 4. Verdict log

| Piece | Author A | Author B | Arbiter | Coordinator |
|---|---|---|---|---|
| Entry S1 | drafted | drafted (+ an "alternate" marked block) | **MERGE**, conclusion arbiter-rewritten — B's lead and code terminator, A's precise `###` cross-reference anchor, A's no-duplicate-marker argument; B's alternate block dropped | 1 further correction (catch C2 below) |
| Entry S2 | drafted | drafted | **MERGE** + 1 arbiter addition — B's bolded lead and anaphora, A's severity-first framing, arbiter's non-nullable-return evidence | 1 correction (catch C1 below) |
| Entry S8 | drafted | drafted | **MERGE** — B's lead + severity closing, A's two-call code block (B had no code) | accepted as written |
| SKILL.md reflow | drafted | drafted | **B, verbatim** — A hard-codes the count "three" (rots on a fourth entry) and loses the imperative "Read" | 1 correction (catch C3 below) |
| Placement (3 slots) | proposed | proposed (identical) | checked, not ratified; all three confirmed | independently verified against the file ✓ |

## 5. Coordinator catches

### Pre-arbitration (raised in my catch list, all nine ruled on)

1. **SHARED BLIND SPOT — the two independent drafts agreed on a provenance
   violation at the doctrine's centre.** Both stated S1's no-op *conclusion* as
   bare fact outside any marker — A: "What is not legible is that nothing
   happens"; B: "that call moves no row" — then pointed at the marked block.
   That step follows only from the doc-derived premise about what
   `IgnoreQueryFilters()` reaches, which SKILL.md 404–411 marks as
   not-corpus-verified. **Arbiter upheld against both**; the shipped S1 asserts
   only the two corpus facts (nothing registered through `HasQueryFilter`; the
   check is an ordinary `Where`) and routes the effect to the marker. Fifth
   session running that independent drafts have converged on a false or
   unlicensed claim — the pattern is holding.
2. **Pointer vs duplicated marked block** → pointer. One doc-derived claim, one
   place; a second copy in `references/` can drift when someone edits SKILL.md.
   B's alternate dropped.
3. **Cross-reference accuracy** → A's `### The filter belongs to the repository`
   ships. Verified: the marked block sits at 404–411, inside that `###`
   (379–418); B's `## Soft delete` anchor opens at 347 and would make the reader
   scan ~57 lines.
4. **Sanitizing `HasCitextUniqueHasFilter`** → **KEEP** (my recommendation,
   arbiter upheld). The S17 `LogExtension.Error` precedent does not bind: there
   the API name was incidental to an example whose substance lay elsewhere. Here
   the *name is the defect* (two names for one job), and the shipped skill
   already prints `HasCitextUnique`, `HiddenObject`, `ApplySoftDelete`,
   `IgnoreGlobalQueryFilter` as house vocabulary. It is not a project name, a
   path, or a business-domain noun.
5. **Reflow** → B's, corrected (see C3).
6. **Modality, both directions** — no drift found after the fixes. S2 leads with
   "is not a live bug" (no inflation). S8's "Nothing breaks the day the second
   method is written" reads as severity, not permission, because the next clause
   names what does break; B self-flagged this and the arbiter judged the
   self-flag over-cautious — I agree, the closing sentence carries it. Nothing
   is framed as "banned" where the corpus shows only "not chosen": S8's
   prescription restates the form 5 of 6 projects and the shipped skill already
   teach.
7. **Placement convergence treated as a shared claim, not corroboration** — all
   three line numbers re-verified against the file. One correction to B's
   *reasoning*: it called line 239 "the closing paragraph of that section", but
   239 is mid-section (the section runs to 246). The slot is still right — after
   246 an intervening paragraph would break the anaphora both entries depend on.
8. **Checklist NOT extended** (arbiter's ruling, A's recommendation). Items 3
   and 5 already carry the positive rules S2 and S8 invert; the checklist is a
   verification list keyed to positives, and S1 has no natural item, so a partial
   extension would be asymmetric. Cheap to revisit — see §7.
9. **Budget** — 66 added lines, inside the 45–70 band. Nothing cut to chase a
   lower number.

### Post-verdict (my corrections to the arbiter's own final text)

- **C1 — cut an unverifiable compiler-behaviour claim (S2).** The arbiter's
  self-declared addition read "…declared to return a non-nullable expression,
  **so the compiler already knows it**." The first half is verifiable and shipped
  (both signatures return a non-nullable `Expression<Func<T, bool>>` — confirmed
  in the shipped listings at lines 97/220 *and* in the corpus). The trailing
  clause implies a compiler diagnostic that I cannot verify and that a reader
  would reasonably take as "you get a warning" — which is not what happens for
  `== null` against a not-null flow state. **Cut**; the verifiable half carries
  the argument on its own.
- **C2 — a residual leak of the very claim catch 1 removed (S1).** The arbiter's
  closing read "the damage of the other one **is not measured in rows
  returned**" — which still asserts the row effect it had just stripped from both
  authors. Replaced with "and what the other one leaves behind is a call site
  announcing a bypass that no later reader can evaluate without going to check
  what the entity registers" — the legibility cost, asserting nothing about
  behaviour.
- **C3 — kept the shipped prohibition subordinate (reflow).** The arbiter shipped
  B verbatim and disclosed that `…lacks them; do not write a local variant.`
  becomes a standalone capitalized sentence — same words, promoted. Its offered
  alternative introduced a false causal link ("all compile, **so** do not write a
  local variant" — the near-misses compiling is not the reason). I shipped a
  third form that keeps the shipped clause exactly as shipped, semicolon-joined
  and lowercase, with no causal claim:

  > `lacks them; the near-misses it names all compile; do not write a local variant.`

  No promotion, no false causality, 8 lines, 79 columns.
- **Author A's self-flagged weakness #6, closed.** A admitted it had not checked
  whether `IgnoreGlobalQueryFilter(typeof(IHidden))` — the form S1 prescribes —
  actually appears in that project. **I verified it: three call sites in the same
  project, all `typeof(IHidden)`.** So the prescribed form is corpus-real and the
  project itself does it right elsewhere; S1's "the hatch that reaches the stamp"
  is an observation, not invented doctrine. I did **not** add this census to the
  text (the file's voice does not do project counts) — recording it here as the
  grounding.

## 6. Proposed CHANGELOG fragment (main session renumbers)

> **ef-core-data-access — three R8 anti-examples (soft delete).** `references/soft-deletes.md`
> gains three labelled negatives, each woven beside the positive form it inverts
> rather than gathered into a section this skill deliberately does not keep:
> (1) `IgnoreQueryFilters()` reached for as the escape hatch on a stamped entity
> where nothing is registered through `HasQueryFilter` — the entry states only
> the corpus facts and routes the API effect to the existing
> documentation-derived marker, which is where that claim already ships once;
> (2) the dead `if (expression == null)` branch after the helper chain in `Find`
> — shipped as a trap and a contract misstatement, not a bug, since both helpers
> are *declared* to return a non-nullable expression; (3) a second
> `HasCitextUniqueHasFilter` method instead of the canonical trailing
> `string? filter = null` — the extend-what-exists reasoning, with the honest
> note that nothing breaks the day it is written. `SKILL.md` is unchanged in
> length (499 lines, hard bar < 500): the paragraph at 370–377 was reflowed in
> place to point at the new material ("the near-misses it names all compile").
> `references/soft-deletes.md` 308 → 374.

## 7. Refused, parked, and open

**Refused (would have violated a standing law):**

- Any statement of what `IgnoreQueryFilters()` clears, outside the existing
  marked block. Both authors wrote one; both were cut, and so was the arbiter's
  softer residue (C2). The claim ships **once**, in the marker already on
  SKILL.md.
- "…so the compiler already knows it" (C1) — unverifiable compiler-diagnostic
  claim, refused rather than hedged.
- B's factual error, caught by the arbiter: "the deleted **and hidden** ones stay
  excluded" — the corpus entity declares `IHidden` **only**, so `ApplySoftDelete`
  hands its predicate straight back. Removed (and the whole sentence went anyway
  under catch 1).

**Anti-example candidates banked — NOT labelled, not in the text (R8 is the
user's):**

1. **`CheckExpression(expression)`** in the same `Find`, between the dead branch
   and the `Where`. The arbiter read it (I had not): it invokes an optional
   per-repository `Filter?.Invoke()` delegate and ANDs it on via
   `Expression.Invoke` over a fresh parameter. Two things a reviewer might care
   about — it opens a *second, mutable* filter channel beside the two this
   pattern owns, and `Expression.Invoke` inside a predicate is not translatable
   by every provider. Single project, uncompared; would need its own pass.
2. **A second by-key overload `GetByIdAsync(params object[] keyValues)`** beside
   the `GetById<TId>` the skill teaches — arguably S8's split-surface shape in a
   different member. A's sighting; unverified across the corpus.
3. **`FindPrimaryKey()` and the raw-SQL pair on the public `IRepositoryBase<T>`**
   — B's sighting; unverified by anyone.

**Open questions for the main session / user:**

1. **Checklist hooks.** Ruled no (§5 item 8). If a reviewer-facing hook is wanted
   anyway, the preferred shape is extending two existing items rather than adding
   new ones — item 3 gains "…and no null branch follows the chain", item 5 gains
   "…written through the helper's filter parameter, not a second helper" — which
   leaves S1 unrepresented. Costs 0–2 lines in `references/`, SKILL.md untouched
   either way.
2. **The reflow touches shipped prose.** It is the only way to point at the new
   material at 499/500. Lines 1–6 of the paragraph are byte-identical; only the
   tail changed, and the shipped prohibition survives verbatim and subordinate
   (C3). Trivially revertible if the main session would rather ship the entries
   unsignposted.
3. **SKILL.md has no headroom left.** It sits at exactly 499 against a hard bar
   of < 500. Any future addition to this skill needs either a reflow like this
   one or a real cut. Worth a board note.
