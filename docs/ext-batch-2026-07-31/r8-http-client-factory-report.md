# R8 label implementation — `http-client-factory` (Group 5: H1, H4, H6, H9)

## 1. Status: COMPLETE

All four approved labels VERIFIED against the corpus and shipped. Nothing dropped,
nothing weakened below the decision table except where the table's stated reason
was itself too strong (H1 — see §3). Full three-way loop ran: both authors drafted
the set as ONE piece, the arbiter ruled MERGE per entry, I verified every
self-declared addition against the shipped files before assembling.

The arbiter loaded `skill-creator:skill-creator` LIVE on its first tool use — no
`Unknown skill`, no cache read, no blocker. Both authors loaded their methodology
on the first attempt (no harness gap this run).

## 2. Files written (write scope respected)

| File | Lines | State |
|---|---|---|
| `skills/http-client-factory/SKILL.md` | **498** (was 491) | modified — +7 lines, pointer block only |
| `skills/http-client-factory/references/anti-patterns.md` | **216** | new |

`git status --porcelain` shows exactly these two paths and nothing else. No git
commands run, no router, no manifests, no CHANGELOG, no sibling skills, no
`reference/` writes. Counts are blank-inclusive (`awk 'END{print NR}'`), per the
house-laws lesson about `Measure-Object -Line`.

## 3. Per-label verification

**All four VERIFIED. None dropped.**

**H1 — sync-over-async eager read — VERIFIED.** Reproduced in the divergent sender
lineage (one project, the one that carries no AutoMapper map and its own
`IDisposable` result type): the result type's **constructor** assigns the body with
`ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult()` inside a
`try`, whose `catch` sets the status to 500 and writes the exception dump into the
same property callers read as the body.

> **Honest weakening applied, per brief §2.** The decision table's reason says
> "classic deadlock/thread-starvation shape". `ConfigureAwait(false)` **is**
> present, so the classic synchronization-context deadlock claim is not supportable
> and was REFUSED by both authors and the arbiter at my instruction. The shipped
> entry teaches only what the code supports — an un-skippable, un-cancellable,
> unbounded buffering read that parks a thread-pool thread for the duration of a
> network read on every call — and says in the body, visibly, that this is *not*
> the deadlock. That refusal is written into the skill text so a later reader
> cannot "restore" the stronger claim.

**H4 — the `IFormFile` loop bug — VERIFIED.** Reproduced in the multipart builder of
one project's facade copy: inside `foreach (object? item in (dynamic)value)` the
guard is `if (value is IFormFile fileInCollection)` — it tests the collection, not
the loop variable, so it can never match and every file in a collection property
goes out as `item.ToString()`. The corrected form is the shipped canonical
(`references/content-extensions.md`), which tests `item` and additionally sets
`stream.Headers.ContentType`; the before/after the decision table expected already
existed, so the entry cross-references rather than duplicates.

**H6 — builder-state carryover / `UseClient` stickiness — VERIFIED, and stronger
than the table implies.** Two independent corpus facts:
- The canonical sender's `RequestBuilder` is a readonly field and **nothing** on
  the send path resets it — Method, Uri, Headers, Content, CustomClient and
  UseLogging all survive `SendAsync`. `UseClient` sets `UseLogging = false` and
  never restores it.
- A real consumer holds one injected `IHttpClientSender` in a class registered
  **singleton** (marker interface scanned with `.WithSingletonLifetime()` — I read
  the registration block to confirm the lifetime, not just the marker), and builds
  its chain with `WithHeaders` **inside an `if`**.

So the entry teaches both halves: the conditional link (which leaves the previous
call's headers, credential included, on the instance) and the captive long-lived
capture (one builder shared by every concurrent call — precisely what the shipped
registration section says `AddTransient` exists to prevent).

> **Provenance guard I imposed.** The consumer that shows the conditional-link
> shape lives in a lineage whose sender *does* rebuild its request message each
> send, so that particular project does not leak headers today. I checked both
> drafts for drift into "a real deployment is leaking headers" — neither drifted —
> and had the arbiter add the explicit anchor "Measured against the facade this
> skill ships". The entry is a claim about the canonical contract, not about any
> running system.

**H9 — readers that swallow a failure into a 500 — VERIFIED.** The canonical readers
catch everything, set `StatusCode = InternalServerError` and return `default`. The
consuming shape is reproduced in **three** projects (near-identical): after a
*correct* status branch, the code reads the error body and dereferences the result
on the very next line, so a body that is not the documented JSON becomes a
`NullReferenceException` from a line that mentions no HTTP — the remote answered
400 and the service answers its own caller 500.

Scope held on both constraints the table implies:
- Labelled the **silence**, not the `!` operator (H10 is BỎ). The entry closes with
  "The point is the silence, not the operator" and names `.Value`, plain member
  access and pass-to-a-mapper as the same defect, so the ruling cannot be re-read
  as a `!`-lint.
- Kept **distinct from shipped anti-pattern 3**, which is about reading *before* the
  status check. H9 opens by disclaiming #3 explicitly.

## 4. Budget route

**references route.** SKILL.md was at 491 against the hard bar of <500; four full
entries would have landed it near 650. Per brief §5 the set went to a new
`references/anti-patterns.md` (216 lines) with a 7-line pointer block appended to
the existing Anti-patterns section. **Final SKILL.md: 498 — 2 lines of margin.**

Existing entries 1–4 were **not** renumbered and not touched; the pointer is
`### 5–8.`, continuing the section's own outline so 5–8 exist in it. No new section
shape was invented. I declined a fourth row in SKILL.md's References table (both
the arbiter and Author A concurred): that table is prefaced "Open these when writing
the facade itself, not when calling it", which is false of entries 7–8 — the
budget was the third reason, not the first.

Note on the filename: no *shipped* sibling had a `references/anti-patterns.md` when I
started. By the time I finished, the `excel-miniexcel` and `file-storage` coordinators
had independently created files of the same name in this same batch — so the convention
is converging on its own. Flagging for the main session only in case the house wants a
different name applied uniformly across all three.

## 5. Verdict log

| Piece | Verdict | One line |
|---|---|---|
| Anti-example set (one piece) | **MERGE** — 5 A-led, 6 A-led, 7 MERGE with A's fix **overruled**, 8 MERGE | A's prose precision + B's structural discipline; A's remedy for the header half was factually broken and was cut in all three places it appeared |
| SKILL.md pointer block | **B** | B's `### 5–8.` continues the section's numbering; A's bare bold prose left 5–8 invisible in the outline. Arbiter added B's missing when-to-open trigger |

## 6. Coordinator catches (beyond relaying)

1. **The authors contradicted each other at the doctrine's centre, and A was wrong.**
   Author A's GOOD block shipped `.WithHeaders(headers ?? new Dictionary<string, string>())`
   with prose "the fix is to run the link every time and pass an empty collection",
   and a table row repeating it. I read the shipped canonical `WithHeaders`: it
   iterates the supplied pairs and assigns into the dictionary — **no `Clear()`, no
   removal, no path that runs on an empty collection**. An empty call is inert on a
   reused instance. I put this to the arbiter as a finding; it confirmed and cut all
   three instances, keeping "set every link on every chain" as the rule (correct for
   Method/Uri/Content, which are overwritten) while stating plainly that for headers
   the cure is a fresh instance. **A broken remedy would otherwise have shipped.**
2. **`(dynamic)` must stay unlabelled — verified.** Author A refused to lint the
   `(dynamic)` cast in entry 6 because the same construct appears in the *shipped
   canonical* flattener. I confirmed it at `references/content-extensions.md` l.509.
   The arbiter then also cut A's softer "instead of a `(dynamic)` cast" clause as a
   miniature not-chosen→banned drift. Correct call: labelling it would have made the
   skill contradict its own reference file.
3. **B's self-declared addition to entry 8 is corpus-true.** `ReadAsByteArrayAsync`
   returns `Array.Empty<byte>()`, so its swallowed failure is a zero-length payload,
   not a null — a null-check is the *wrong* branch for that reader. Verified;
   it partially restates existing shipped prose but changes what a reader writes, so
   it earned its one sentence.
4. **Verified all eight arbiter self-declared additions before assembling**, not
   after: the entry-6 `else` line is byte-exact against the canonical at
   `content-extensions.md` l.153; `UseClient` really does set `UseLogging = false`
   with no restore; `LogException(ex)` is the canonical single-argument private
   helper; the insertion point (447 prose / 448 blank / 449 `## Retry…`) was
   confirmed before the edit; the H1-vs-`##` ruling rests on a census I re-ran
   myself — H1 dominates the shipped `references/` files and both of *this skill's*
   existing reference files use it, with `api-surface` the lone `##` outlier.
5. **Line-count discipline.** Author B reported SKILL.md as 492 lines; the
   authoritative blank-inclusive count is 491. Had 492 been trusted with a 7-line
   block the result would have been miscomputed against a hard bar. Measured before
   and after: 491 → 498.

## 7. Delegated judgment calls (each recorded)

- Kept the arbiter's invented call-site helper `BuildHeaders(settings)` in entry 7's
  GOOD block over the alternative it offered (`new { x_api_key = settings.AccessKey }`,
  a form already shipped in SKILL.md). Reason accepted: the anonymous-object form
  quietly implies headers are always static, which is exactly the case where the
  offending `if` never appears. It is visibly a call-site helper, not claimed as
  facade API.
- Shipped at **498** rather than trimming the pointer's final sentence to reach 497.
  The sentence is the when-to-open trigger, which skill-creator names explicitly as
  a requirement for reference pointers.
- Kept the property name `Content` in entry 5's BAD block (B asked whether to rename
  it `Body`). Recognizability wins: someone repairing a divergent copy must see the
  shape they actually have; the prose disambiguates.
- Entry 5's BAD block shows a one-parameter constructor where the corpus original
  takes four. The extra parameters are irrelevant to the defect and would leak
  lineage detail — a sanitization-positive simplification, recorded here because it
  is a fidelity trade.
- No cross-reference from entry 6 to shipped anti-pattern 2: #2 is about drift risk
  in a hand-built copy, #6 is a live wrong-output bug in one. Linking them would
  file the bug under the softer heading.

## 8. Refused / not shipped

- **The deadlock claim on H1** (see §3) — refused, and the refusal is written into
  the skill body.
- **Any claim about `(dynamic)`'s runtime binding behaviour** (e.g. `RuntimeBinderException`
  on a non-enumerable) — not corpus-checkable, refused by both authors, banked.
- **B's "no request log showing the extra header"** — narrowed by the arbiter to "none
  of it appears in a request log", because the stronger form additionally depends on
  how `HttpRequestMessage.ToString()` renders headers — API recall, not corpus-checked.
- **B's "Singleton is faster" rationalization row** — an unverifiable performance claim
  that is not the defect. Dropped.
- **A's `Failure(...)` result-wrapper vocabulary** in entry 8 — invents types the skill
  does not have. Replaced with plain returns matching SKILL.md's *Branching on the result*.
- **No documentation-derived block was needed** anywhere in the file: every behavioural
  claim rests on a corpus shape I verified or on the skill's own shipped contract.

## 9. New R8 bank items surfaced during this pass (NOT labelled — user's call)

1. `HttpResult.ToString()` in the shipped canonical appends the literal `"', Duration: "`
   — an unbalanced quote inherited from the base type, in every response log line.
   Cosmetic; deliberately absent from the shipped text.
2. `[AllowNull] public new HttpResponseHeaders Headers` in the canonical result shadows
   the base member, so a caller holding a base-typed reference sees different headers.
   **This is shipped canonical and the AutoMapper map depends on it** — noted only so a
   future session does not "discover" it as a defect.
3. `request.Method!` / `request.Url!` / `request.Data!` — three null-forgiving operators
   in one chain on inbound-shaped data, visible in entry 7's BAD block (kept verbatim,
   uncommented). Both authors independently declined to teach it here: it is a validation
   concern, likely `api-surface`'s or `error-handling`'s, and commenting on it would
   smuggle back the `!`-lint that H10 rejected.
4. `new StringContent(item?.ToString(), Encoding.UTF8)` passing a possibly-null first
   argument, in the same defective loop. Not taught.

## 10. Proposed CHANGELOG fragment (main session renumbers)

```
## 0.3.NN — http-client-factory (R8 labels)

feat(http-client-factory): four approved anti-examples land as entries 5–8 in a
new references/anti-patterns.md, with a 7-line pointer block continuing SKILL.md's
Anti-patterns outline (entries 1–4 untouched, not renumbered). H1 sync-over-async
eager body read in a result constructor — shipped with the deadlock claim REFUSED
in-body (ConfigureAwait(false) is present; taught as thread-pool starvation,
unbounded buffering and no cancellation, per the honest-weaker-framing rule).
H4 the multipart collection loop that tests the collection instead of the item
(files in a collection go out as their type name; single-file properties still
work, which is why it survives testing) — corrected form cross-referenced to the
canonical, not duplicated. H6 builder-state carryover: the conditional With… link
plus the captive singleton capture, anchored "measured against the facade this
skill ships" so no live deployment is accused. H9 using a reader's return value
with no branch for what it returns on failure — labelled as the SILENCE, explicitly
not a `!`-operator lint (H10 BỎ) and explicitly distinct from anti-pattern 3.
Coordinator catch: both authors' shared "set every link unconditionally" remedy is
INERT for headers — WithHeaders writes and never removes, so an empty call clears
nothing; the broken fix was cut in all three places and replaced with "only a fresh
instance is clean". `(dynamic)` left unlabelled: the shipped canonical flattener
uses it. SKILL.md 491 → 498 (hard bar <500); references/anti-patterns.md 216.
```

## 11. Open items for the main session

1. **Filename convention** — `references/anti-patterns.md`. Two sibling coordinators in
   this batch (`excel-miniexcel`, `file-storage`) landed the same filename independently,
   so the convention is self-consistent across the batch. Cheap to rename all three
   together if the house prefers something else.
2. **2 lines of margin** on SKILL.md (498/500). Any future addition to this skill's
   body needs the references route too.
3. **No router work done or needed** — this pass adds no new skill and changes no
   description, so `choosing-a-dotnet-skill` is untouched by design.
4. Four new bank items in §9 await R8 labels (the user's carve-out).
