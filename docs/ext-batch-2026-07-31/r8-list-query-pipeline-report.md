# R8 label implementation — `list-query-pipeline` (Group 2)

## 1. Status: COMPLETE

Six approved labels (L1, L2, L4, L5, L8, L9), all six VERIFIED against the corpus
and shipped. None dropped. One re-framed weaker than the decision table implied
(L5 — see §2). Full three-way loop run: both authors drafted the set
independently, drafts went to the arbiter **verbatim** as files, arbiter ruled
MERGE with file-verified reasons, all six of its self-declared additions
re-verified by me before assembly.

One process incident: the first arbiter spawn was killed mid-run by a monthly
spend limit (infrastructure, not a task failure). It was resumed from its intact
transcript by `SendMessage` after the limit was lifted; the arbiter gate
(`skill-creator:skill-creator` loaded LIVE) had already passed in that same spawn
and no plugin-cache read occurred at any point.

## 2. Per-label verification — all sites reproduced by the coordinator

Reading discipline honoured throughout: Bash `find`/`grep`/`sed` only inside
`reference/projects/`, never Glob; `apsp-backend/.claude/worktrees/` excluded from
every search. Sites below are described, never addressed.

| # | Verdict | Site confirmed (sanitized) |
|---|---|---|
| **L1** | **VERIFIED** | Four unconditional `Console.WriteLine` calls in the filter stage of the three fullest lineages — two on the success path (one per filter arm, printing the composed predicate before each `Where`), two in the catch arms. Extra fact I confirmed and shipped: the three simplest lineages moved the *success-path* line to the debug channel and **left the failure line on the console** — half a fix. |
| **L2** | **VERIFIED** | Both filter arms wrap `Where(query, params)` in `try { … } catch (Exception ex) { Console.WriteLine(…) }`. The queryable is left untouched, the loop continues, the method returns normally. Grounding for "the app logs properly elsewhere": the same solution records errors through a logging abstraction at 137 call sites across 34 files in its own module tree (count verified, deliberately NOT shipped — see §5.3). |
| **L4** | **VERIFIED** | Three lineages, one line inside the recursive search-field walk: the attribute test ORed with `PropertyType == typeof(<a concrete project type>)` **and** a literal property-name `Equals(…, OrdinalIgnoreCase)`. The other three ship the attribute test alone. Consequence confirmed structurally: this walk is the default search-field discovery for every list endpoint that passes no `SearchFields`. |
| **L5** | **VERIFIED — honest weaker framing (latent, not live)** | `TrimEnd(' ','a','n','d','o','r')` at two sites × three lineages; a four-char variant at one site × three lineages; the sibling `TrimEnd(' ','o','r',' ')` in the search stage in **all six**. I traced all ten operator templates through assembly: every composed predicate ends in `)`, which is in neither set, so **nothing is corrupted today**. Shipped as a latent defect with the mechanism and the rule, never as a live failure. |
| **L8** | **VERIFIED** | One lineage's sealed two-generic paged-response subclass declares `public IEnumerable<object> Data { get; internal set; }`. Solution-wide grep: never assigned (the file's only assignment is `PagedData = items`), never read; `internal set` puts it out of reach of any module. Absent from the other five lineages and from the single-generic base — so the two paged shapes of one API disagree. |
| **L9** | **VERIFIED, plus the asymmetry** | `!propertyInfo.GetType().IsGenericType` in the top-level filter arm in **all six** lineages (the leaner three spell it inverted as an early-`continue`). The very next branch guards the real `…PropertyInfo.PropertyType.IsGenericType` in the three richest. `GetType()` returns the reflection object's own type, so the guard has never excluded anything. |

Nothing was inflated. No candidate outside my group's LABEL rows was added, and no
`BỎ` row was touched.

## 3. Budget route

**Route taken: `references/anti-patterns.md`** (the brief's over-500 route), with a
short pointer appended to SKILL.md's existing `## Anti-patterns` section and one
bullet in `## Going deeper`. No existing anti-pattern bullet was renumbered,
rewritten or reordered; no new section shape was invented in SKILL.md.

| File | Before | After |
|---|---|---|
| `skills/list-query-pipeline/SKILL.md` | 462 | **474** (+12, hard bar <500) |
| `skills/list-query-pipeline/references/anti-patterns.md` | — | **244** (new) |
| `references/query-expression-extension.md` | 454 | 454 (untouched) |
| `references/pagination-extension.md` | 250 | 250 (untouched) |
| `references/property-info-extension.md` | 195 | 195 (untouched) |

244 lines sits mid-range against the siblings (195 / 250 / 454). Nothing
load-bearing was cut to chase a number (S17 precedent); what was cut was padding —
see §5.6/§5.7.

## 4. Verdict log

| Piece | Verdict | Reason |
|---|---|---|
| The six-entry anti-example set + SKILL.md pointer (one piece) | **MERGE** | B's skeleton (ordering, entry structure, "Write instead" cross-references, the exact `compose` phrasing) + A's L9 ruling and two foreclosing paragraphs, with four corrections neither author made. |

Contested points, as ruled:

1. **L9 wording → A's single forbid**, not B's new-code/shipped-copy split. B's "write `PropertyType` in new code" recommends the exact expression the shipped listing refuses *on the merits*; the split would have shipped two rules where one is true.
2. **L2's GOOD block → B.** A's `LogFilterTermDropped(...)` was an invented house API (A self-flagged it). Cut. L2 ships no fabricated code at all — it cross-references the listing and states the upgrade in prose.
3. **Two claims tightened.** "writes neither line to standard output" → "calls `Console.WriteLine` in neither arm" (textual fact, no `Debug.WriteLine` behaviour claim). "hundreds of call sites" → "the logging abstraction its own modules already use" (no count ships).
4. **L5 phrasing → B's exact form** ("every predicate the shipped operator templates **compose** ends in `)`"); A's "all ten templates close with `)`" was inaccurate. A's search-side observation kept — it is the sharper teaching and the shipped copying note does not cover it.
5. **L4 placeholders → A's** (`SomeConcreteType` / `"SomeLiteralPropertyName"`). B's `typeof(Wrapper)` collides with the house meaning of `Wrapper`.
6. **B's six-row scan table → cut** as padding; two of its cost phrasings folded into entries.
7. **A's intro claim narrowed** — "a test" cut (the corpus has no test projects); ships as "none of them fails a build or a request".
8. **Heading level** — my ruling, applied: `# ` title matching the three siblings.
9. **Length** ≈ mid-range; cuts were the table, the invented GOOD block, the census sentence, and a paragraph each of restatement in L1 and L4.

## 5. Coordinator catches and verification beyond the arbiter's own

1. **The shared-blind-spot check fired, and it went against my own brief.** BOTH
   authors independently overrode my brief's statement that "the intended
   expression is `propertyInfo.PropertyType`". I verified them and **they are
   right**: the shipped `references/query-expression-extension.md` refuses that
   swap twice — a copying note and a deviation row ("Changing it to `PropertyType`
   would exclude all `Nullable<T>` properties from filtering"). Had the brief been
   followed literally, the new file would have contradicted a shipped listing in
   the same directory. The label survives; only its prescription changed.
2. **All six arbiter self-declared additions re-verified** against files, not
   accepted on assertion: (a) the intro naming all three listings — the pagination
   deviation row it depends on exists; (b) L5's deviation-table citation — exists,
   "Provably latent given the shipped template set"; (c) "the very next branch of
   the same method" — confirmed the correct guard is the immediately following
   branch, with one intervening assignment (both authors' "two lines/branches
   further down" were wrong; the arbiter's correction is version-stable);
   (d) the composed L9 consequence sentence — both halves grounded; (e) L4's
   two-bullet restructure — the `common-extensions` boundary claim survives the
   fold; (f) pointer as a bullet, not a paragraph — matches the section, which is
   a bullet list.
3. **Refused to ship a corpus census.** B's draft carried "hundreds of call
   sites"; my count is 137 across 34 files — "hundreds" overstates it, and a
   precise count is a fingerprinting risk. Ships unquantified.
4. **Every anchor claim spot-checked before editing**: the four `Debug.WriteLine`
   lines and the `Console.WriteLine` deviation row in the shipped listing; the
   null-safe search term ending `== true or `; the two guards; both SKILL.md
   insertion anchors.
5. **Modality diffed both directions.** L9's forbid is absolute on the *blind
   swap* but explicitly leaves a deliberate redesign open ("if a type exclusion is
   genuinely wanted here, name the types to exclude") — a "not chosen" that did
   not drift into "banned". L2's entry opens by protecting the fallback, so a
   permission (drop the term, answer 200) could not drift into a prohibition.
6. **Coordinator formatting amendment (mechanical):** the arbiter's entry headings
   came back at `###` under a `#` title; the three siblings run `#` → `##`. I
   normalized the six entry headings to `##`. No wording changed.
7. **Sanitization sweep run over both written files** for project names, business
   nouns, the two real identifiers behind L4, real paths and corpus paths — **zero
   hits**.

## 6. Files written (write scope honoured)

- `D:\AI-PLUGIN\dotnet-standards\skills\list-query-pipeline\references\anti-patterns.md` (new, 244 lines)
- `D:\AI-PLUGIN\dotnet-standards\skills\list-query-pipeline\SKILL.md` (two insertions, 462 → 474)
- this report

Nothing else. No git operation, no router edit, no manifest, no CHANGELOG, no
sibling skill. Scratchpad working copies (verbatim author drafts, extracted
verdict) are namespaced `r8-lqp-*` per the batch's filename-collision lesson.

## 7. Proposed CHANGELOG fragment (main session renumbers)

> **0.3.NN — feat(list-query-pipeline): the R8 anti-example set.** Six
> user-approved labels ship as `references/anti-patterns.md` (244 lines) with a
> pointer bullet in SKILL.md's `## Anti-patterns` and one in `## Going deeper`
> (462 → 474 lines; no existing bullet renumbered): console diagnostics in the
> filter loop; the catch arm that leaves no record (the **silence** is labelled —
> the wide-result fallback is Principle 3 and is explicitly protected); a
> character set standing in for a suffix strip (shipped as **latent, not live** —
> every composed predicate ends in `)`); a guard evaluated on the `PropertyInfo`
> object instead of the property, with the intra-method asymmetry as the teaching;
> domain knowledge welded into the shared reflection walk; a dead untyped member
> on the two-generic paged envelope (taught from the pipeline side, contract
> question routed to `api-surface`). Loop rulings: L9 ships ONE rule — *inherit the
> expression as it stands* — because `references/query-expression-extension.md`
> refuses the `PropertyType` swap on the merits (it would exclude every
> `Nullable<T>` from filtering on every list endpoint); BOTH authors caught this
> against the coordinator's own brief. Author A's invented logging API cut; a
> precise corpus call-site count cut as a fingerprinting risk; a scan table cut as
> padding. Nothing dropped, nothing inflated.

## 8. Refusals, residual risks and parked items

**Refused / declined:**
- **No API-recall claims shipped.** Nothing in the file says what
  `Debug.WriteLine` compiles to or where it writes, and nothing says how any
  serializer renders an unassigned member. L1's "Write instead" is phrased as a
  textual fact about the listing; L8 makes no serialization claim at all. No
  documentation-derived block was needed, so none ships.
- **L5 not inflated to a live bug.** The decision table's `Ordered` → `Ordere`
  illustration is shipped explicitly as a conditional about future templates, not
  as a sighting, because I could not reproduce corruption in any lineage.
- **L8 does not re-decide the wire contract** — it teaches the pipeline-side rule
  and routes the member question to `api-surface`.
- **No seventh label.** Both authors surfaced further candidates (the duplicated
  `try`/`catch` block; the `Any()` probes; `ParsingConfig.Default` mutated as a
  shared static). All three are R8 decisions that are the user's, and the latter
  two are explicitly declined in the shipped deviation table. None shipped.

**Residual risks for the main session:**
1. `SomeConcreteType` / `SomeLiteralPropertyName` are not in the registered house
   placeholder set — the set has no placeholder meaning "a domain type that should
   not be here". Deliberate, but it sets a precedent worth recording.
2. "four of these" in L1 is the only count in the file. It is a shape count, not a
   codebase census, but it is the one number a sanitization pass would question.
3. L2 ships no GOOD code block by design (no invented API, no duplication of the
   listing). Structural asymmetry with L5's GOOD block is intentional.
4. `Console.WriteLine` is the file's most-repeated code token, appearing in three
   BAD blocks. It is the corpus shape and the labelled defect, so it must.

**Parked (out of my write scope, flagged not fixed):**
- The shipped copying note in `references/query-expression-extension.md` says
  "every operator **template** ends in `)`". Strictly the `$null` template is
  `" == null "`; only the *assembled* predicate closes with `)`. The new
  anti-example states it precisely, so the two files now differ in precision on
  the same fact. Substance is identical (both conclude latent-safe) — worth one
  word in a future pass, but editing shipped prose was outside this mandate.
