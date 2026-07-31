# common-extensions — coordinator report (house-laws §8) — RE-RUN, FINAL

## 1. Status: COMPLETE

Re-run finished 2026-07-31. All pieces went through the full three-way loop
(fresh authors per round, arbiter verified `skill-creator:skill-creator` loaded
at every arbiter spawn — it loaded every time this run). Process incidents, all
recovered: the first re-run arbiter was killed by the headless 600s background
ceiling (lesson applied: every subsequent subagent ran SYNCHRONOUS); a second
arbiter spawn was killed mid-verification by the monthly spend limit (resumed
after the limit was lifted, fresh spawn, no work lost — the relay prompt carried
everything). No cache reads, no worktree, writes confined to
`skills/common-extensions/` + this report.

## 2. Files written under skills/common-extensions/

| File | Lines |
|---|---|
| SKILL.md | 480 |
| references/regex-extension.md | 176 |
| references/validator-extension.md | 181 |
| references/validation-extension.md | 68 |
| references/validator-service.md | 74 |
| references/expression-extension.md | 192 |
| references/serializer-extension.md | 81 |
| references/random-extensions.md | 226 |
| references/password-extension.md | 151 |
| references/action-context-extension.md | 239 |

**SKILL.md is 480 lines — above the 450 soft target, inside the <500 hard bar.**
Coordinator judgment, recorded: the round-2 arbiter delivered the body believing
it ~396 lines; materialized with fences and blank lines it runs longer. Cutting
~30 lines of file-verified doctrine to chase the soft number would violate the
standing don't-cut-content-to-chase-a-number ruling (S17). Main session may trim
if it disagrees; the cheapest candidates are the serializer corpus-divergence
paragraph and one of the two scope-leak mentions.

## 3. Final description + router + sibling Not-for proposals

**Final description (97 words by wc -w, coordinator-verified):**

```
This skill should be used when reaching for a helper, utility, extension
method or attribute in a .NET solution: regex, random string, generated
password, IP address, JSON serialize/deserialize, Expression composition,
reusable existence check or FluentValidation rule method; when adding to
Infrastructure/Facades/Common; before writing an inline helper at a call
site; or when a project lacks an extension it needs. Not for: entity
configuration, repository base — ef-core-data-access; filter, sort,
pagination — list-query-pipeline; S3, file keys, media — file-storage;
Excel, import templates, zip — excel-miniexcel; typed HttpClients —
http-client-factory; API-key filter — auth-and-security; feature-specific
validators, expressions — module-feature.
```

**Proposed router rows for `choosing-a-dotnet-skill` (do NOT let this session
edit the router — main session places and phrases per the router's format):**

- a helper, utility or extension method (regex, random string, generated
  password, client IP, JSON serialize/deserialize, expression composition,
  existence check, reusable FluentValidation rule) → `common-extensions`
- adding to or searching `Infrastructure/Facades/Common/` (Extensions,
  Services, Attributes) → `common-extensions`
- a project is missing a house extension / about to inline a helper at a call
  site → `common-extensions`

**Proposed `Not for:` additions to EXISTING siblings (exact sentences; main
session applies — an ownership boundary the shipped files now need in reverse):**

- `module-feature`: `reusable rule methods, existence-check extensions, shared
  helpers — common-extensions`
  (module-feature's description claims "FluentValidation rules, IsExist
  predicates" — the reciprocal boundary of this skill's seventh Not-for entry.)
- No other existing sibling needs one: ef-core-data-access, auth-and-security,
  message-keys, elasticsearch-search boundaries are all drawn by pointer rows
  inside this skill's body and their existing descriptions do not overlap it.

## 4. Proposed CHANGELOG entry (main session renumbers)

> feat(common-extensions): umbrella extensions skill — lookup-first doctrine
> over Infrastructure/Facades/Common (search by capability + both legacy
> spellings), the reuse → promote → inline ladder, recreate-from-references
> doctrine, base-vs-feature mechanical test (module-namespace using), one home
> per shape, 9-file canon with full recreate-ready code (regex, expression,
> serializer, random, password, action-context, validation, validator,
> validator-service) + catalogue of the remaining Common/ recurrences with
> ownership pointers. Corrected canon shipped, all visibly marked: ValidatorService
> disposing corpus shape (2 of 4 corpus variants leak); regex-law hoists —
> ValidatorExtension inline literals and the NotSpecialCharacter template moved
> into RegexExtension fields/builder; ReplaceVnCountryCode and
> SpecialCharacterRemoving use compiled instances; RandomExtensions type/file
> name corrected from the corpus-wide RamdomExtentions misspelling (members
> deliberately kept). Full verdict log in the batch coordinator report.

## 5. Verdict log, coordinator catches, delegated calls

**Piece 1 (description): MERGE.** Arbiter fixed two SHARED author defects — the
path token (both wrote `.../Common/Extensions` where the doctrine and two canon
items span `Common/`) and zip/import-template routing swallowed by a bare
"Excel" noun. Three self-declared arbiter additions all coordinator-verified as
executions of the mandate's ownership map. Coordinator independently verified:
wc -w = 97; module-feature's shipped description claims FluentValidation rules
(made the seventh Not-for entry mandatory); 21 shipped skills on disk.

**Pieces 2–5 (body): MERGE.** Three SHARED-false claims caught and cut:
(1) AndJoin/OrJoin/ToPredicate absent from the canonical ExpressionExtension —
only in the 169-line contaminated variant (coordinator re-verified, zero grep
hits); (2) ValidatorService is a FOUR-variant family, 2 disposing / 2 leaking —
"corrected synthesis" reframed to "select the disposing corpus shape" (stronger
provenance); (3) R7 member-averaging (RandomPercentage, WhenHttpMethod,
ForwardSlash..., IsValidAllPhoneNumber initially cut). Coordinator re-verified
arbiter additions: ConfigurationExtension 6/6 present 4/6 byte-identical (row →
ef-core-data-access); IsExistByIds null-branch (ValidationExtension.cs:22-26).
Author A self-corrected the brief's serializer claim (per-call allocation is not
the distinguishing defect; per-method inline options + no-options deserialize
is) — arbiter verified and upheld.

**Round 3a (regex/validator/validation/validator-service): MERGE×3 +
NEITHER(validator-service — both authors shipped a false leak census; corrected
four-shape census shipped).** NotSpecialCharacter hoist (A) upheld on file
evidence (the template mechanism at RegexExtension.cs:14 is the file's own
idiom); B's code-site provenance comments adopted; corpus-derived accessibility
rule (Regex fields public / templates+partials private) settled naming
divergences; arbiter caught A's Regex.Escape claim wrong on `^` (Escape DOES
escape it; `]`/`-` it does not) — shipped as a marked documentation-derived
note.

**Round 3b (expression/serializer/random/password/action-context): MERGE×5.**
Fifth SHARED-false claim of the run: both authors gave the password tail-shuffle
a security rationale the code does not support (every char is already inserted
at a random position — 5 chars.Insert sites, coordinator-verified); corrected to
the faithful-reproduction rationale. Arbiter REFUSED two unverifiable API-recall
claims (query-provider translatability in the reference file;
JsonSerializerOptions metadata caching). Byte-verified everything else incl.
Vietnamese enum comments (Và/Hoặc — coordinator re-verified), 41-line
serializer, md5 identity of the random core.

**Delegated calls (standing batch delegation; carve-outs respected):**
1. Regex law ruled ABSOLUTE within shipped canon (SHARED author question) —
   validator literals hoist into RegexExtension; **banked for user review at
   merge** (it changes shipped canon beyond the pre-named ValidatorService fix).
2. AndJoin/OrJoin/ToPredicate ship in references with provenance note (§6
   method-level synthesis; survey recorded them canon-worthy) + body bullet.
3. WhenHttpMethod restored (module-free, arbiter-endorsed); ForwardSlashNot-
   SpecialCharacter + IsValidAllPhoneNumber DECLINED (contaminated-variant-only,
   the latter validates almost nothing) — banked.
4. ValidationContextExtension (DataAnnotations Required helper sharing the
   corpus ValidatorExtension.cs) EXCLUDED — both authors + coordinator agree;
   candidate for its own future file.
5. Extra self-consistency corrections accepted (ReplaceVnCountryCode ToString
   round-trip at RegexExtension.cs:42; per-call new Regex at :78) — authorized
   by the brief's self-consistency clause, corpus-verified, visibly marked.
6. schemeSet local rename accepted unmarked; UtcNow kept (no TimeProvider note);
   Distinct-over-entities sentence verified (RepositoryBase.cs:30) and added;
   timestamp-before-extension sentence added (arbiter recommendation executed).
7. **Arbiter overruled the coordinator's lean on fixing "cant not be emty"** —
   kept verbatim: the thrown text is greppable surface exactly like the member
   names; one rule ships (file/type name corrected, nothing inside the type).
   Coordinator accepted the overrule as better-reasoned.
8. Member spellings (lenght, RandomLowwerCase) kept per the same rule; the body
   itself lists RandomLowwerCase.
9. ActionContextExtension = union of the two corpus files (mandate-specified);
   the 22-line file confirmed as BOTH the merge donor and the user-labelled poor
   IP variant (one file, three members, one excluded).
10. Assembly-time consistency edits (recorded): 3b H1s normalized to class-name
    form matching 3a; serializer provenance blockquote flattened to a plain
    paragraph (3a convention: no blockquote when nothing deviates); references
    line counts unconstrained by the SKILL.md budget.
11. SKILL.md at 480 lines accepted (see §2).

## 6. Variant-comparison outcomes (survey table from run 1 confirmed; outcomes)

| File | Winner / synthesis shipped |
|---|---|
| RegexExtension | apsp skeleton + digitalcity IdentifierNumber/ColorCode/compiled SpecialCharacters + hoisted validator literals + AcceptedCharacters template/builder; VNMotobikePlate excluded (business-domain) |
| ExpressionExtension | apsp 114-line base + mtc's three generic wrappers (only); contaminated variant grounds anti-pattern 3 |
| SerializerExtension | cpc file verbatim (options cached, clone per call, TryDeserialize); service variants' real defect = per-method inline options + no-options deserialize |
| RandomExtensions | 139-line byte-identical core + apsp RandomAlphaNumericUpperCase + digitalcity RandomPercentage; type/file name corrected, members kept |
| PasswordExtension | 2-of-3 byte-identical form with `[]`; PasswordOptions project-defined, ships in-file |
| ActionContextExtension | apsp Payments-module full variant (proxy-aware chain) + mtc HttpMethod()/Guid RouteValue; namespace normalized; TryGetFromString companion shipped in Dependencies |
| ValidationExtension | apsp 31-line file verbatim + null-caveat + Distinct-cost notes |
| ValidatorExtension | apsp 158-line generic set + WhenHttpMethod; all patterns via RegexExtension (corrected canon) |
| ValidatorService | cpc disposing shape verbatim (interface + primary ctor + using scope); 4-variant census in the file's note |

Catalogue rows shipped: PathExtension, PropertyInfoExtension, TypeExtension,
EnumExtension, ConsoleExtentions, SemaphoreSlimExtension,
ServiceScopeFactoryExtension (leak caveat), ConfigurationExtension (→
ef-core-data-access), RepositoryBaseExtentions (→ ef-core-data-access),
ValidatorMessageExtention (→ message-keys), BatchExtension (→
elasticsearch-search). Ownership-map rulings executed: RepositoryBaseExtentions
= catalogue row + pointer only; ImportTemplateExtension + Zip* → excel-miniexcel
(Not-for nouns); JsonNamingPolicyExtension single-project, silent; Crypto/ +
KeysGenerationExtension PARKED (unowned ground — needs a user decision; also
KeysGenerationExtension has an unbounded non-thread-safe static Dictionary memo
if ever canonized).

## 7. Open questions / banked items (R7/R8 — user's alone)

1. **Regex-law hoist as corrected canon** (delegated call #1) — flag at merge
   review; easily reverted per-file if vetoed.
2. **Banked anti-example candidates** (all verified, none labelled, none taught
   negatively beyond the four sanctioned): the 169-line module-contaminated
   ExpressionExtension (strongest); the 22-line ActionContextExtension
   (three defects in one file); Service<T>() itself; the inline
   CreateScope-no-using one-liner; JsonSerializerService no-options deserialize
   asymmetry; SemaphoreSlimExtension unawaited WaitAsync in the sync overload +
   process-wide single gate; RegexExtension's own inline new Regex sites
   (now corrected in canon); `A-z` character-class bug (variant; canonical
   already fixed — ready-made before/after pair); IsValidAllPhoneNumber
   (declined, validates almost nothing); GetRemoteIpAddr dead result guards +
   string.Empty-not-null return (documented neutrally); Guid.Parse-not-TryParse
   RouteValue; per-call Options clone with null configs; corpus middleware
   re-implementing the IP chain inline; a body-buffering variant with a no-op
   seek (seeks to current position, never rewinds); RandomRangeNotRepeat double
   enumeration; TickCount-seeded public static Random reachable from
   PasswordExtension (options if the user wants action: caveat is already in
   both files' notes as security-posture prose; a corrected-canon
   RandomNumberGenerator variant would be a user call).
3. **ValidationContextExtension** — excluded; candidate for a future
   references file or module-feature ground.
4. **SKILL.md 480 lines** — main session may trim (candidates in §2).
5. Sibling-batch consistency: this report's Not-for text assumes all five batch
   skills merge together (names frozen); re-diff every batch description if any
   name changes or ships separately.
