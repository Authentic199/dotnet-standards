# excel-miniexcel — coordinator report (RE-RUN, 2026-07-31)

## 1. Status: COMPLETE

Full three-way loop run to completion: arbiter loaded `skill-creator:skill-creator`
LIVE (first-run blocker cleared; no cache reads); first-run author drafts
discarded, authors respawned fresh; two author rounds + verdict rounds + one
arbiter budget pass; all files assembled and verified. Survey spot-checks per
relaunch-common §1 passed (six-way md5 identity; template defect at line 35; all
file locations).

## 2. Files written under skills/excel-miniexcel/

| File | Lines |
|---|---|
| `SKILL.md` | 472 (post budget pass, from 586; hard bar <500) |
| `references/excel-extension.cs` | 59 |
| `references/zip-extension.cs` | 226 |
| `references/import-template-extension.cs` | 186 |
| `references/import-service-pattern.md` | 562 |

Nothing else touched: no router, no manifests, no CHANGELOG, no git, no other
skill.

## 3. Description (final) + proposed router work

### Final description (verdict A, 98 words by `wc -w`, coordinator-verified)

```yaml
---
name: excel-miniexcel
description: >-
  This skill should be used when reading or writing Excel with MiniExcel in a
  .NET service: exporting rows via ExcelExtension.Export or ExportByTemplate,
  importing an uploaded .xlsx through Query<T> with a startCell, filtering
  blank rows, unpacking a zip of workbook plus images, serving or replacing an
  import template file, or staging imported rows for confirm. Not for: row
  validation rules — module-feature; S3 upload, serving — file-storage;
  search, pagination endpoints — list-query-pipeline; PathExtension —
  common-extensions; upload routes, DTOs — api-surface; permission attributes,
  ApiKey — auth-and-security; profiles — automapper-mapping; persistence,
  transactions — ef-core-data-access; exceptions — error-handling; message
  text — message-keys.
---
```

`choosing-a-dotnet-skill` deliberately absent from Not-for (not an owning
sibling of an excluded area — arbiter-concurred).

### Proposed router rows for `choosing-a-dotnet-skill` (main session edits)

| you are asked to… | load |
|---|---|
| export a list as an .xlsx download (plain or via a designed template) | excel-miniexcel |
| import an uploaded Excel file, or a zip of workbook plus images | excel-miniexcel |
| serve or replace the import template users fill in | excel-miniexcel |
| stage imported rows for review/confirm | excel-miniexcel |

### Proposed Not-for additions to EXISTING siblings (main session applies; adapt
each to that description's word budget)

- **api-surface**: append `Excel export/import streams — excel-miniexcel`
- **module-feature**: append `Excel workbook parsing, import flows — excel-miniexcel`
(No other shipped sibling teaches an area this skill now owns; the four batch
siblings' reciprocal entries are their own coordinators' calls.)

## 4. Proposed CHANGELOG entry (main session renumbers)

```
feat(excel-miniexcel): MiniExcel both directions — export, zip import, corrected
template extension, canonical import-service pattern (0.3.XX)

- New skill excel-miniexcel (SKILL.md 472 lines + 4 references/ files, full
  sanitized implementations for verbatim recreation).
- Export: the six-project byte-identical ExcelExtension, reproduced faithfully.
- Zip helper canonical: modern internal corpus variant made public, Archive
  check adopted into IsImages (defect fix), SaveImage decoupled from S3 statics
  (temp root parameterized; corpus FormatFileName shape reproduced privately),
  cross-type nameof corrected; the second corpus variant presented neutrally.
- Templates: ImportTemplateExtension + StaticFileSettings SHIP CORRECTED — the
  verified template-name defect (extension passed as the name argument saves
  the replacement as ".xlsx", unfindable by the StartsWith lookup) fixed per
  pre-authorization, marked visibly in body and reference; GetCurrentDirectory
  vs BaseDirectory anchor divergence surfaced, not fixed, with a marked
  documentation-derived note.
- Import: no corpus extension exists — references/import-service-pattern.md is
  the distilled canon (direct + zip flows, staged/confirm lifecycle as optional,
  session-scoped FindStaged, set-based uniqueness with IgnoreQueryFilters
  grounded in the staging query filter, per-row structural media guard at full
  corpus coverage, bounded upload size gate canonical with the unbounded corpus
  shape recorded neutrally, FluentValidation List<T>+RuleForEach shape with rule
  content routed to module-feature).
- Provenance refusals: un-rewound-stream consequence claims; Hangfire
  transaction-coupling claim; MIME literal in export examples.
```

## 5. Verdict log, coordinator catches, delegated calls

### Verdicts per piece

| Piece | Verdict | One-line reason |
|---|---|---|
| 1 frontmatter/description | **A** | B's roster omitted three owners its own body routed to; B's DisableRequestSizeLimit trigger falsified; A = 10 entries, house-symbol triggers, 98 words |
| 2–5 body (batched) | **MERGE** | A's skeleton (BAD/GOOD blocks, two-folder split, defect-as-mechanism) + B's orientation table, BuildEmptyFilter rationale, open-before-writing, zip-trust anti-pattern, staging hedge; 3 verified arbiter corrections |
| R2-1 excel-extension.cs | **MERGE** | A's documented frame + B's package versions; shared CopyToOutputDirectory claim reduced to instruction-only; PathExtension justification corrected to observed behaviour |
| R2-2 zip-extension.cs | **MERGE** | A's XML docs + B's corpus-faithful FormatFileName ({dir}/{ticks}_{name}{ext} + ReplaceSpecialCharacters); A's reordered inline builder cut; navigation-helper exclusion confirmed (authors converged independently) |
| R2-3 import-template-extension.cs | **MERGE** | A's all-C# frame (B's correction note was a markdown blockquote inside a .cs) + B's endpoint/request/validator block; arbiter R7 correction: local AllowedExtensions (canonical project has no zip helper) |
| R2-4 import-service-pattern.md | **MERGE** | A's spine + B's settings names/decomposition/checklist; A's coupling claim refused; B's unscoped FindStaged replaced (security); per-row media guard restored at corpus coverage |
| Budget pass | 19 cuts, 586 → 472 | All cuts deduplicate against references/; stopped above 450 deliberately — remaining routes cost content (S17 precedent) |

### Coordinator catches (all file-verified)

1. Shared blind spot: both authors asserted the un-rewound-stream consequence
   ("empty download, no exception") — unverifiable framework recall; CUT.
2. Size-gate blind spot (both drafts): corpus has TWO gate shapes — unbounded
   (`DisableRequestSizeLimit` + int.MaxValue) and bounded
   (`RequestSizeLimit`/`RequestFormLimits` + 1 GB const). A's "every corpus
   import endpoint" claim FALSE; B's description trigger FALSE.
3. A's soft-delete rationale for IgnoreQueryFilters VERIFIED FALSE (arbiter
   catch, coordinator-confirmed): the global filters are the staging filters
   (`ImportSessionId == null`; one entity adds `CombineToId == null`).
4. B's "ValidateXxx ladder" anti-pattern cut as an R8 silhouette of a specific
   corpus file.
5. Staging grounded in ONE project (zero ImportSessionId hits in the other) —
   "addition, not a requirement" modality won.
6. Auth attributes differ per project — kept generic, routed.
7. A's R2-4 provenance violation ("scheduled inside the transaction so it
   exists only if the import committed") — REFUSED; corpus call order
   reproduced with no coupling claim.
8. B's R2-4 FindStaged dropped the session-ownership scoping — A's corpus form
   kept (the predicate IS the authorization).
9. A's inline SaveImage name builder diverged from the corpus FormatFileName
   (reordered, dropped ReplaceSpecialCharacters, moved the large marker) — B's
   faithful shape shipped.
10. ARBITER OVERTURNED coordinator note 5 with file evidence: the apsp
    template-update validator genuinely has the null guard + ToLowerInvariant
    (I had compared against the direct-import validator). Overturn verified and
    accepted; recorded as the loop working as designed.
11. Budget-pass consistency catch: body's `settings.MaxFileSize` vs the ruled
    `MaxSizeImage` settings name — fixed during compression.

### Delegated judgment calls (user's standing authorization; recorded)

- Zip helper canonical = modern internal variant as base, made public, Archive
  check adopted in IsImages; loser presented neutrally (label banked).
- Bounded upload gate canonical (house-laws §6 defect-fix authority); unbounded
  corpus shape one neutral mention in body + one in the pattern doc.
- Row-validation style = FluentValidation; rule content → module-feature.
- Import skeleton = staged generic structure; staging optional per catch 5.
- SaveImage decoupled from S3 statics; seam = (tempRootPath, tempDirectoryName,
  markLargeFileSize) → relative path (both authors converged); cross-type
  nameof corrected as a mechanical consequence, noted in the file header only.
- Corpus FormatFileName reproduced privately in the zip helper with the
  must-match-the-storage-formatter warning.
- PathExtension.Combine kept with common-extensions routing (arbiter-corrected
  justification: empty/rooted-segment handling, not separator form).
- Template defect fix shipped per pre-authorization (marked in body header +
  inline + reference); anchor divergence surfaced not fixed + approved
  doc-derived marked note.
- choosing-a-dotnet-skill out of Not-for; MIME literal refused
  (ExcelContentType placeholder + api-surface routing); version pinning: state
  corpus versions, stop.
- ExcelExtension field/class collision NOT renamed (R7 — user's) + clarifying
  sentence in Principle 4; EntryExcelCastTo un-rewound read shipped VERBATIM +
  Principle 2 scoped to the export direction (both banked, §7).
- Auto-clean scheduling: corpus order reproduced, no behavioural claim either
  way (only the template defect was pre-authorized as a correction).
- Per-row media guard restored as a single structural non-ladder guard at full
  corpus coverage; settings = corpus names (MaxObject/MaxImage/MaxSizeImage/
  TimeAutoClean); corpus entry-arithmetic `(MaxImage+1)*MaxObject+3` excluded
  as project-specific.
- Template-flow validator keeps its own local AllowedExtensions (arbiter R7
  correction, coordinator-confirmed) — R2-3 independently recreatable.
- R2-4 assumed-types routing table added (arbiter-drafted, approved); B's
  checklist kept; file-scoped namespaces normalized; shared
  CopyToOutputDirectory inference reduced to instruction-only.
- Budget pass stopped at 472 (not 450): remaining cuts cost content.

### Process notes

- Loop integrity: arbiter pinged FIRST per relaunch-common §2, skill-creator
  load confirmed live before any author dispatch. All drafts relayed VERBATIM.
  Shared claims verified every round (three caught: rewind consequence,
  size-gate shape, copy-to-output). Arbiter self-declared additions all
  coordinator-verified against files (HasQueryFilter sites, FormatFileName,
  IsValidQuantityImages, ImportSettings, the note-5 overturn, PathExtension).
- The run was interrupted twice by a spend limit (after import-template
  reference assembly); resumed with context intact both times; no re-work.

## 6. Variant comparison (final)

| Area | Winner | Improvements made |
|---|---|---|
| Export extension | no contest (6 byte-identical) | none to code; doc header added |
| Zip helper | cpc-style modern variant | made public; digitalcity's Archive-attr IsImages adopted; SaveImage decoupled from S3 statics with corpus FormatFileName reproduced privately; cross-type nameof corrected; Vietnamese comments → English; navigation helpers excluded on usage test |
| Upload size gate | cpc bounded pair | canonical; unbounded digitalcity shape recorded neutrally |
| Import flow skeleton | digitalcity staged generic structure | staging made explicitly optional; cpc's FluentValidation List<T>+RuleForEach style; session-scoped FindStaged kept; per-row media guard restored non-ladder; B's flatter HandleMedia decomposition; corpus settings names |
| Template flow | apsp (only source) | line-35 GetTemplateName defect CORRECTED per pre-authorization; validator kept apsp's self-contained AllowedExtensions |
| Request file-gates | unanimous corpus | whitelist (direct) / IsZipFile (zip), reproduced |

## 7. Open questions / parked items (banked for the user — non-blocking)

1. **R8 label candidates (all presented neutrally or omitted; none labelled):**
   digitalcity zip variant (canonical-pick loser); unbounded size gate;
   ValidateXxx inline-validation ladder; auto-clean job scheduled inside the
   transaction before commit; Console.WriteLine+Stopwatch probe in a production
   import path (cpc); misspelled `HanldePhotos`/`HanlLargeFile` (cpc); async
   lambda in List.ForEach inside finally (cpc); DateTime.UtcNow.Ticks
   uniqueness prefix (collision window — rubric candidate).
2. **R7 rename question:** `ZipExtension.ExcelExtension` string[] field
   collides with the `ExcelExtension` export class — user's call whether canon
   renames it (one token in references/zip-extension.cs + 3 body call sites);
   shipped as-is with do-not-fix notes.
3. **EntryExcelCastTo queries its MemoryStream without Seek(0) after Extract**
   — corpus-unanimous (both variants), shipped verbatim; Principle 2 scoped so
   the body doesn't contradict it; whether it is a real defect needs an
   API-behaviour check outside provenance.
4. Coordination point with the common-extensions sibling: this skill routes
   `PathExtension`, `RegexExtension.ReplaceSpecialCharacters` and the settings
   `Required()` extension there — that skill should own all three.
5. Body ships at 472 lines (over the 450 sibling ceiling, under the <500 hard
   bar) — arbiter ruled further cuts cost content; user may override.
