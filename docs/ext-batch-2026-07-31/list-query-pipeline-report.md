# list-query-pipeline — coordinator report

## 1. Status: COMPLETE

Full three-way loop executed on the re-run (2026-07-31): arbiter gate passed
(`skill-creator:skill-creator` loaded live in every arbiter spawn), piece 1
MERGE, pieces 2–5 MERGE (batched), references round MERGE per file. All
verdicts coordinator-verified; all amendments applied mechanically; skill
assembled and sanitization-swept. One mid-run incident (scratchpad filename
collision with the http-client-factory lane) fully recovered — see §8.

## 2. Files written under skills/list-query-pipeline/

| File | Lines |
|---|---|
| `SKILL.md` | 462 |
| `references/query-expression-extension.md` | 454 |
| `references/pagination-extension.md` | 250 |
| `references/property-info-extension.md` | 195 |

SKILL.md is 12 over the 450 sibling norm, under the <500 hard bar. The
references-round arbiter audited for padding and found none (every candidate
line is corpus-verified and load-bearing); per the S17 precedent it declined to
name cuts to chase 450. references/ files carry the FULL sanitized canonical
implementations (four extension listings — QueryExpressionExtension,
TypeExtension, PaginationExtension + ApplyExtension bundle,
PropertyInfoExtension — plus NotSearchableAttribute), each with a "Deviations
from corpus" table.

## 3. Description / router rows / Not-for additions

### Final description (shipped; `wc -w` = 90)

```yaml
name: list-query-pipeline
description: >-
  This skill should be used when the list-query extensions themselves must be
  written, ported or repaired in a .NET API: QueryExpressionExtension,
  PaginationExtension, ApplyQuery, the $eq/$in/$btw/$ilike/$sw operators and
  the $not prefix, System.Linq.Dynamic.Core predicate strings, np() null
  propagation, GetPropertyRecursive, nested-collection search and sort,
  [NotSearchable]/[NotSearch], CustomFilterBinder, PaginationResponse,
  PageInfo, QueryContainer — or when ApplyFilter/ToPagedListAsync does not
  resolve. Not for: pipeline call sites, repository queries —
  ef-core-data-access; search service methods, validators — module-feature;
  list endpoints, request DTO chains — api-surface; full-text index queries —
  elasticsearch-search; regex, serializer, shared helpers — common-extensions;
  file placement — facade-module-architecture.
```

### Proposed router rows for `choosing-a-dotnet-skill` (main session edits, do not copy blindly — fit the router's row format)

| Trigger | Route |
|---|---|
| the list-query extensions themselves must be written, ported or repaired — `QueryExpressionExtension` / `PaginationExtension` / `ApplyQuery` source, the `$eq…$sw` operator table, `CustomFilterBinder` | `list-query-pipeline` |
| `ApplyFilter` / `ToPagedListAsync` does not resolve; the project has no list-query pipeline | `list-query-pipeline` |

Existing router rows that route "a query"/pagination *usage* to
ef-core-data-access / api-surface stay untouched — this skill fires on
authorship, not usage.

### Proposed `Not for:` additions to EXISTING siblings (exact sentences; main session applies)

- `ef-core-data-access`: add `; query-extension internals — list-query-pipeline`
  (its description currently disclaims services/validators and file placement
  but nothing routes a reader who wants to modify ApplyFilter's source).
- `api-surface`: add `; pipeline implementation — list-query-pipeline`
  (it owns the PaginationResponse/QueryContainer wire contract; the source
  listing that recreates those types is ours).
- No addition needed for elasticsearch-search, module-feature, or
  facade-module-architecture (their existing boundaries already point the
  right way, and our description carries the reciprocal entries).
- NEW sibling `common-extensions` (same batch, main session coordinates): it
  should carry `recursive property lookup — list-query-pipeline` ONLY IF it
  does not ship its own PropertyInfoExtension listing; if it does ship one
  (the batch plan says it owns the file), instead confirm the two listings
  agree on shared members and add no entry. See §7 open question 1.

## 4. Proposed CHANGELOG entry (main session renumbers)

> **0.3.NN — feat(list-query-pipeline): the implementation side of the list-API
> query pipeline.** New skill (22nd): recreatable canonical source for
> `QueryExpressionExtension` (ApplyFilter/ApplySearch/ApplySort, ten-operator
> grammar + `$not`, Dynamic LINQ predicates, `np()` overload families),
> `PaginationExtension` (ToPagedListAsync, PaginationResponse, QueryContainer,
> CustomFilterBinder), the optional `ApplyQuery` bundle (corrected spelling
> `ApplyExtension.cs`), `PropertyInfoExtension` slice + `NotSearchableAttribute`.
> Canonical base = the DataHolder lineage, corrected per pre-authorized classes:
> CountAsync in async paging, `CurrentInvalid` message fix, `PageSize <= 0`,
> offset-overflow guard (message-keys-compliant key), depth-1 attribute-passed
> search discovery, `$in` OR-chain with `.ToString()` coercion, Debug.WriteLine
> channel, skip-list de-domained, PopulateKeys/dead-Data members dropped,
> AutoMapper.Internal replaced by local TypeExtension. Loop rulings: ApplyQuery
> stays corpus-faithful (no CancellationToken, no searchFieldExcepts — the
> bundle is the documented short form); ParsingConfig kept in corpus tokens but
> set unconditionally outside the checkNull branch (corpus placement never
> covers the Max/Min sort it exists for); `$null` predicate drops the lone
> `it.` alias; Any() probes KEPT (five-trips cost model in
> dotnet-performance-review / ef-core-data-access stays true). Description 90
> words; module-feature added to Not-for on file evidence; both attribute
> spellings ([NotSearchable]/[NotSearch]) are triggers.

## 5. Verdict log, coordinator catches, delegated calls

### Verdicts

| Piece | Verdict | One-line reason |
|---|---|---|
| 1 — frontmatter/description | MERGE | B's authorship-situation trigger + symptom clause; A's literal type names; arbiter added `module-feature` Not-for (file-evidenced, overruling A-Q1) and the `[NotSearch]` second spelling (both drafts had the mtc facts wrong) |
| 2–5 — body (batched) | MERGE | B's grammar layer (four porting rules incl. `$ilike` no-ToLower) + A's structure/modality (ApplyQuery as permission, "change here, once" locus rule); 7+7 principles merged to 5; both drafts' centrepiece defects kept out (A: `IsClass()` substitution, ParsingConfig false causation, unverifiable `$null` claim, H1; B: phantom `ApplyQuery(request, cancellationToken)` call, "default form" modality, PageInfo member reprint) |
| 6 — references (3 files) | MERGE per file | File 1 base B, file 2 base B, file 3 base A; 14 enumerated amendments applied mechanically; both authors' shared `CancellationToken` addition and shared unverified `new ParsingConfig()` construction OVERRULED |

### Coordinator catches (things verified beyond the arbiter's own checks)

- Piece 1: both arbiter self-declared additions re-verified against files
  (service-growth.md:74–83 chain; request-response-families.md:182;
  attribute file census incl. be-booking's dead duplicate).
- Pieces 2–5: arbiter's four self-declared additions re-verified (QEE:258
  `IsNullableType() || Type.IsClass` no-parens; ParsingConfig absent from apsp
  while Max/Min present; ApplyQuery has no token overload;
  PropertyInfoExtension:96–98 non-attribute guard). The
  preference→imperative shift on "pass the exclusion attributes in" reviewed
  and accepted as canonical-form teaching (apsp form, 4/6), not modality drift.
- References round: arbiter's four load-bearing citations re-verified
  (message-keys rule 1 verbatim at SKILL.md:27; query-conventions.md:10–16 =
  the four-stage `SearchAsync` chain WITH `Filter?.Keys.ToArray()` and
  `cancellationToken:`; rubric 3.1's three Find passes none of which cover a
  sync-signature bundle; corpus-wide grep shows `RestrictOrderByToPropertyOrField`
  assigned only `false`, only at mtc/booking:121 inside `if (checkNull)`).
- SHARED-blind-spot discipline paid off twice: the piece-1 attribute facts
  (both authors wrong about mtc), and the references-round `new ParsingConfig()`
  construction (both authors emitted the same unverified ctor; overruled to
  corpus tokens per provenance law).

### Delegated judgment calls (standing delegation; all recorded, user-vetoable)

1. **Boundary:** `common-extensions` owns `PropertyInfoExtension` as a utility;
   this skill's references/ carries the pipeline slice anyway (recreatability);
   `NotSearchableAttribute` is ours. Description triggers on method names only
   so it dangles under neither resolution.
2. **ApplyQuery bundle stays corpus-faithful** — no CancellationToken, no
   searchFieldExcepts (arbiter ruling accepted; A's rubric-3.1 citation was
   verified overstated; settled body text corroborated by query-conventions).
3. **ParsingConfig**: corpus two-line form (`ParsingConfig.Default` + flag
   assignment) moved out of the `checkNull` branch, set once; shared-static
   cost stated in the header note (only-value-ever-assigned fact grepped).
4. **`$null` `it.`-prefix dropped** — a correction OUTSIDE the pre-authorized
   list, upheld on structural evidence (lone alias among ten templates;
   `it.np(Prop)` on the checkNull path; cpc lineage already prefix-free).
   **Veto path if the user disagrees:** in
   `references/query-expression-extension.md`, change
   `$"({key} {FilterOperators[FilterOperator.Null]}) {suffix}"` to
   `$"(it.{key} {FilterOperators[FilterOperator.Null]}) {suffix}"` and delete
   the corresponding Deviations row. Nothing else moves.
5. Offset-guard kept with `CurrentInvalid` key (guard is arithmetically
   necessary — both operands at the `int.MaxValue/2` bound overflow int by
   ~5×10⁸×; message-keys rule 1 forbids B's prose literal).
6. `GetPropertyFromExpression` omitted from the slice (zero pipeline call
   sites; lineages disagree on its body; completeness objection fixed by the
   "pipeline's slice, not the whole file" header instead).
7. Attribute target stays `AttributeTargets.Property` (canonical, honest).
8. Two SKILL.md recipe corrections applied post-verdict (step 1 now names all
   FOUR extension listings incl. TypeExtension; step 4's "only edit" absolute
   widened to the three real project anchors) — both fix sentences the shipped
   references falsified; the round's genuine defect find.
9. SKILL.md ships at 462 lines (hard bar <500); no content cut to chase 450.

## 6. Variant-comparison table (which project won what)

Base lineages: apsp = canonical (DataHolder collection support); mtc = most
advanced paging; cpc/digitalcity/ops = byte-identical simplest lineage;
be-booking = modernized apsp minus the `$in` coercion (full diff run in the
references round — it holds NO refinement the canonical listings drop).

| Decision | Winner | Notes |
|---|---|---|
| QueryExpressionExtension body | apsp | with corrections below |
| `$in` strategy | apsp/mtc OR-chain + `.ToString()` coercion | cpc-trio `Contains(it.…)` documented as porting note only |
| `$null` predicate | cpc trio (no `it.` prefix) | the round's one minority-lineage win; vetoable (§5.4) |
| ParsingConfig | mtc/booking tokens, placement corrected | corpus sets flag in a branch that never covers the Max/Min sort |
| Async paging count | mtc `CountAsync(ct)` | all others sync `Count()` in async methods |
| Offset-overflow guard | mtc (idea), key corrected | prose literal → `CurrentInvalid` per message-keys |
| `PageSize <= 0` | apsp | mtc loosened to `< 0`; api-surface text settles it |
| Validation message members | corrected (all six shared the `PageSizeInvalid` copy-paste) | |
| Search discovery | apsp: depth 1, attributes passed in from ApplySearch | mtc/booking depth-2 baked-in `NotSearchAttribute` route documented as the porting hazard |
| Attribute name | `NotSearchableAttribute` (4/6 wiring) | `[NotSearch]` kept as description trigger + body note |
| Bundle file | mtc (only source), spelling corrected to `ApplyExtension.cs` | argument shape kept corpus-faithful |
| Logging channel | cpc trio `Debug.WriteLine` | replaces `Console.WriteLine` |
| TypeExtension | apsp, trimmed to the 2 called members | replaces AutoMapper.Internal dependency |
| Skip-list | de-domained (attribute-only) | sanitization-mandatory |

## 7. Open questions / parked items

1. **PropertyInfoExtension dual listing (the batch's flagged boundary).**
   Resolution recommended and shipped: common-extensions owns the utility
   view; our references/ carries the pipeline slice with an explicit
   "slice, not the whole file" header pointing at common-extensions. MAIN
   SESSION: at merge, confirm the two skills' listings agree on shared member
   bodies, and settle the reciprocal Not-for per §3.
2. **Any() probes — kept; drop banked as a named follow-up.** All three loop
   roles agree dropping is the better engineering answer but the blast radius
   is two shipped skills. If a future session drops them: edit
   `ef-core-data-access/references/query-conventions.md` (the "five round
   trips, not two" cost note, ~line 108), `dotnet-performance-review`'s
   five-trips rows + suppression example, and remove the three
   `if (!entities.Any())` blocks from our references — same commit.
   **No perf-skill edit is needed NOW** — the shipped shape matches the
   published cost model (the mandate's flag condition did not trigger).
3. **`it.`-prefix ruling** — user-vetoable, revert instruction in §5.4.
4. R8 banked anti-example candidates (flagged by authors/arbiters across all
   rounds, NONE labelled, nothing blocked on them): Console.WriteLine
   diagnostics in ApplyFilter; swallowed catch (two-sided — also the
   total-contract mechanism); Any() probes; S3FilePath/"LanguageCode"
   skip-list leakage; character-set TrimEnd (proven latent); misspelled
   `ApplyExtention.cs` + sibling `*Extention` files; be-booking's dead
   duplicate NotSearchableAttribute.cs; mtc's dead untyped `Data` member on
   the paged response (api-surface co-owned); the always-true
   `propertyInfo.GetType().IsGenericType` guard (with the verified asymmetry:
   the DataHolder branch guards real PropertyType, so collection-element
   Nullable<T> filters ARE excluded while top-level ones are not); the
   `Current` violation reported under the PageSize key (fixed in canonical).
5. **Process/incident items for the main session:** (a) the scratchpad
   filename-collision lesson — coordinators should namespace ALL draft files
   (`<skill>-<piece>-<author>.md`); worth one line in house-laws for future
   batches. (b) The http-client-factory lane's references drafts were
   rescued to `http-client-factory-refs-authorA-RESCUED.md` /
   `-authorB-RESCUED.md` in this scratchpad (the un-namespaced
   `draft-a-refs.md`/`draft-b-refs.md` now hold that lane's only other copy);
   route them to that lane. They contain unruled QUESTIONS incl. an R7
   canonical-source call (base-settings shape) that is the USER's alone.
6. Dynamic LINQ version note shipped as "targets 1.7.x, two-line 1.3.x
   fallback" — corpus pins are 1.3.5 (apsp/cpc/digitalcity/ops) and 1.7.1
   (mtc/booking); no behavioural claims about either version were made.

## 8. Process log (for the record)

- First run (2026-07-30): survey + variant diffs complete; piece-1 author
  drafts preserved; BLOCKED at the arbiter (`Unknown skill:
  skill-creator:skill-creator`, plugin bound to a stale path).
- Re-run (2026-07-31): arbiter-first gate passed; piece 1 MERGE accepted;
  pieces 2–5 batched, MERGE accepted; SKILL.md assembled (458 lines);
  references round dispatched; both author drafts received and preserved.
- Spend-limit interruption after preserving the references drafts; resumed
  same day under sync-subagent policy.
- Incident: the un-namespaced `draft-a-refs.md`/`draft-b-refs.md` were
  overwritten by the http-client-factory lane before the references arbiter
  read them (mtime 07:54). The arbiter refused to rule on wrong-skill drafts
  (correctly — no reconstruction-from-summary). Coordinator re-emitted both
  drafts verbatim from its intact transcript to namespaced paths
  (`list-query-pipeline-refs-authorA/B.md`, 4-line banner noting the
  re-emission), safety-copied the other lane's content, and re-ran the
  arbiter synchronously. Verdict rendered on the true drafts; all citations
  spot-verified; amendments applied; SKILL.md recipe edits applied (458→462);
  sanitization sweep clean.
- Every arbiter spawn in this run loaded `skill-creator:skill-creator` live;
  no plugin-cache reads occurred; no files outside
  `skills/list-query-pipeline/`, this report, and the scratchpad draft/rescue
  copies were written; no git operations performed.
