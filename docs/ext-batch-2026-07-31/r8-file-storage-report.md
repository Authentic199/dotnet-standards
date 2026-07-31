# R8 label implementation — `file-storage` (Group 4) — coordinator report

## 1. Status: COMPLETE

All five approved labels of Group 4 (F1, F2, F3, F4, F6) ship. **Nothing dropped.**
Every candidate was re-verified against the corpus by me before drafting, and again by
both authors and the arbiter independently.

Full three-way loop ran on the anti-example set as ONE piece: Author A (`skill-writer-a`,
`a5287d6cc493e1c89`) and Author B (`skill-writer-sp`, `abd77c3d53434bc0f`) drafted the
complete set independently and in parallel; both drafts went to the arbiter
(`skill-arbiter`, `abbcff5106c58ee0f`) **VERBATIM**. The arbiter loaded
`skill-creator:skill-creator` LIVE (no `Unknown skill`). All subagents ran synchronously.

Files written: **only** `skills/file-storage/` + this report. No git, no router, no
manifests, no CHANGELOG, no sibling skills.

## 2. Per-label verification

| # | Label | Verdict | Corpus site I confirmed (sanitized) |
|---|---|---|---|
| F1 | Delete the old object BEFORE the new upload succeeds | **VERIFIED — ships as entry 6** | Six update methods across five projects, all the same shape: inside a transaction's `try`, the existing key is deleted ~3 lines *above* the upload that replaces it; the `catch` compensates only the new key. Two of the six sit in the same project's user-facing and self-service update paths |
| F2 | Checksum computed over UTF-8-decoded binary bytes | **VERIFIED — ships as entry 7** | An external-URL ingest handler buffers the download and hashes `Encoding.UTF8.GetString(bytes)`; the shared MD5 helper it calls then re-encodes with `Encoding.ASCII` before hashing. Two hops, two encodings, two files. The digest is the sole input to a "has it changed?" comparison that decides whether to re-upload |
| F3 | Undisposed `FileStream` + `MemoryStream` | **VERIFIED — ships as entry 8, honest weaker framing** | Same ingest handler: the download handle is in a `using`, but the `FileStream` opened from it and the `MemoryStream` copied into are never disposed. Aggravator verified in the facade itself: `OpenFileStream()` returns a **new** stream without assigning the type's own `Stream` property (never assigned in the constructor either), so the type's `Dispose` stream-branch is unreachable and the handle's `using` closes nothing the call site opened |
| F4 | Hand-built key with no uniqueness component | **VERIFIED — attached to EXISTING entry #1, no parallel entry** | Five interpolation sites across three module services, all `$"{Folder}/{FileName.Sanitized()}"` — no tick component. Census for the "commoner shape" claim: 1 canonical format declaration in the extension layer, 1 hand-rolled const, **5** tick-less interpolations |
| F6 | `_ = DeleteManyAsync(...)` fire-and-forget | **VERIFIED — ships as entry 9** | A bulk-delete flow's `finally` discards the returned task with `_ =`. Four sibling call sites in the same project `await` the identical method, so the discard is the outlier, not a house convention |

### F4 attachment — as instructed

F4 is the concrete site of the skill's existing anti-pattern #1, and the shape fit, so it
is a **13-line addition inside entry #1**, not a new entry. Entry #1's existing text, BAD
block, GOOD block and numbering are untouched; the addition appends after its closing
paragraph. It earns its place because entry #1's shipped BAD block *includes* a tick
component — a reader who hand-rolls a key without one could read entry #1 and conclude it
does not apply. Two consequences ship, both corpus-verified:
(a) same filename → same key → the second upload overwrites the first, and any other row
still pointing at that key serves the wrong object;
(b) the same files' update flows use the **correct** upload → commit → delete-old
ordering with a guard that does *not* compare old key to new — so a replacement under the
same filename makes the two keys equal and the delete-old step removes the object just
uploaded, leaving the committed row pointing at nothing.

## 3. Budget route

**Route taken: `references/anti-patterns.md` + a pointer block**, per brief §5. SKILL.md
was already at 460 of a <500 hard bar; four full BAD/GOOD entries inline would have been
~120 lines. The arbiter grounded this in skill-creator's own progressive-disclosure rule
("if you're approaching this limit, add an additional layer of hierarchy along with clear
pointers"), so it is house doctrine rather than a budget dodge.

| File | Before | After | Change |
|---|---|---|---|
| `skills/file-storage/SKILL.md` | 460 | **485** | +25 (F4 attachment 13, pointer block 11, references-table row 1) |
| `skills/file-storage/references/anti-patterns.md` | — | **171** | new file (entries 6–9) |

485 of the 500 hard bar, 15 lines of headroom. No existing entry renumbered; no section
shape invented. Sanitization sweep over both files returns zero hits for project names,
business-domain nouns, real paths or credentials. No H1 in SKILL.md; the references file
uses the `#` title / `##` entries shape the other four references files already use.

## 4. Verdict log

One piece, four deliverables, ruled separately.

| Deliverable | Verdict | Reason |
|---|---|---|
| 1 — F4 attachment to entry #1 | **B** (editorial trim) | A inserted the second BAD block *between* entry #1's existing BAD and GOOD, splitting its own explanatory sentence from the block it names and making the shipped paragraph read as commentary on a defect it was not written about — a rewrite by adjacency. B appends a self-contained lead-in → BAD → consequence unit and leaves entry #1 literally untouched |
| 2 — pointer block | **B** (two cells corrected) | Table with a cost column: same line count as A's bullets, and each row gives a stand-alone consequence so the reader can decide whether to open the file. Rows 6 and 9's cost cells rewritten off a framing the arbiter disproved |
| 3 — `references/anti-patterns.md` | **MERGE**, with **NEITHER** on entry 9's GOOD block and prose | B's structure and entries 6/8; A's `usage-patterns.md` §7 citation grafted into entry 7; entry 9 redrafted by the arbiter after both authors' shared claim was disproved |
| 4 — references-table row | **MERGE** | A's coverage phrasing, B's `Task` precision |

Sub-rulings I handed the arbiter:
- **(i) F4 placement** → after entry #1's closing paragraph (B), on the "do not rewrite
  existing entries" constraint.
- **(ii) Entry 9's GOOD block** → **keep the `Count > 0` guard**. Both authors dropped it
  as redundant; disproved (see catch 1). The GOOD is now a minimal diff of the BAD —
  `_ =` → `await`, one keyword — which is also what makes the entry persuasive.
- **(iii) Heading level** → `#` title, `##` for entries 6–9, matching the house shape in
  all four shipped references files. Numbering continuity is carried by the numerals and
  the opening sentence, not by heading depth.
- Entry 6 ships with **no GOOD block** — the GOOD listing already exists twice (SKILL.md's
  Update Pattern and `usage-patterns.md` §5, the latter with both guards). A third copy is
  precisely the drift entry #1 warns about. Verified both targets exist before allowing it.
- `ct` vs `cancellationToken` → `cancellationToken` throughout the new file, so entries 6,
  7 and 9 diff cleanly against the `usage-patterns.md` sections they are negative images of.

## 5. Coordinator catches and verification duties

**Shared-blind-spot catches (both independent drafts agreed on a false rule — the fifth
session running).** The arbiter found both; I re-verified both myself against the shipped
`references/implementation.md`, not just the corpus, because these entries constrain code
readers will *recreate* from this skill:

1. **"The `Count > 0` guard is redundant."** A: *"guarding something that was never a
   problem"*; B: *"the count guard is not needed either"*. Both wrong. The shipped
   `DeleteManyAsync` **logs** on the empty branch, and the corpus method's failure path
   empties the list before the `finally` runs — so the guard is what stops a spurious
   "nothing to delete" log on every rollback. Cut from both; the final entry now tells the
   reader explicitly to leave the guard alone and why.
2. **"No log reader can tell."** Both drafts claimed a failed fire-and-forget delete is
   invisible to logs. The shipped service catches and `Log.Error`s. The honest, narrower
   loss shipped instead: nothing ties that logged failure to the request that caused it,
   and the request reports success either way.

**A claim in my own brief that I withdrew.** My F3 package told both authors the temp file
survives "until finalization" because its removal is tied to the handle closing. That
**contradicts shipped `references/media-downloads.md` §"Why the file survives long enough
to read"** — the handle's own `Dispose` deletes the path, guarded, and the share/option
flags exist precisely so the path can be deleted while the handle is open. Both authors
refused the claim independently and were right. F3 therefore ships the honest weaker
framing the brief's §2 asks for: the **OS handle** is the load-bearing leak, the
`MemoryStream` is the lesser half, and `DeleteOnClose` contributes nothing on this path.

**Other verification duties discharged:**
- *Rephrasings of settled rulings diffed.* Entry 7's `Seek`-back-to-0 rationale turned out
  to be shipped text in `usage-patterns.md` §7 nearly verbatim ("Reading the stream to hash
  it leaves the position at the end, and the upload would send zero bytes") — a
  restatement, not a new behavioural claim.
- *Arbiter self-declared additions checked* (six). Entry 9's guard sentence and corrected
  cost statement verified against the shipped `DeleteManyAsync` body (empty-branch
  `Log.Information`, catch-branch `Log.Error`, no rethrow). Its removal of A's routing of a
  hash helper to **common-extensions** verified and correct: SKILL.md routes *filename
  sanitization* there, not hashing — an unverified ownership assignment, rightly cut.
- *Convergence checked rather than assumed.* `UploadAsync(folder, fileName, Stream)` is a
  real published overload (`key-generation.md`), `DeleteManyAsync` really does no-op on an
  empty list, the extension overload really does throw on failure, and
  `usage-patterns.md` §5 really does carry both `!= previousKey` guards.
- *Modality diffed both directions.* The arbiter converted both authors' "the guard is
  redundant" into "leave the guard alone" — permission → obligation, but in the restraining
  direction and fully grounded, so accepted. Entry 8's buffering advice stays permissive
  ("buffer only when…"), which is correct: buffering is not banned, it is unnecessary here.
- *A's `ops-service` gap.* A's filename-scoped grep hit four projects, not five. I read the
  fifth site myself and confirmed the identical shape. No shipped text makes a numeric
  claim, so nothing depended on it.

## 6. Refused / cut, and why

- **The temp-file-survival claim** — contradicts shipped text (above). Withdrawn by me.
- **"Released only when the finalizer gets to it"** (both drafts, entry 8) — framework
  API-recall that cannot be corpus-checked. Provenance law says refuse, not hedge. Replaced
  with the pure-C# fact: the handle is not released at the end of the block that opened it,
  and nothing later in the flow releases it.
- **UTF-8 / ASCII substitution semantics** (U+FFFD, `?`) — deliberately never asserted. The
  entry states only the structural fact: a byte → string → byte round trip through two
  different encodings is not lossless. **Zero `> Documentation-derived` blocks were needed
  in the shipped text**, which is the outcome to prefer.
- **Cancellation-token, DI-scope and `UnobservedTaskException` semantics** for F6 — barred
  in my author package, and neither draft reached for them.
- **A's re-argument of the lossiness rule at length** in entry 7 — `usage-patterns.md` §7
  already owns that rule; the entry now points at it and contributes only what is new (the
  two-file, two-encoding shape and the change-detection consequence).
- **A's restatement of the dispose mechanics** in entry 8 — `media-downloads.md` §"The
  dispose contract" already states those three facts; the entry points instead.

## 7. Proposed CHANGELOG fragment (main session renumbers)

```
feat(file-storage): R8 anti-examples — five approved labels (0.3.xx)
- Group 4 of the R8 labelling pass ships in `file-storage`: F1, F2, F3, F4, F6.
  All five re-verified in the corpus by the coordinator, both authors and the
  arbiter; none dropped.
- New `references/anti-patterns.md` (171 ln) carries entries 6–9 continuing
  SKILL.md's numbering: 6 deleting the old object before the new upload succeeds
  (6 sites / 5 projects — the negative example the Update Pattern lacked);
  7 taking a checksum over a text decoding of the bytes (two files, two
  encodings, and the change-detection then skips a genuine update);
  8 leaving the streams around an ingest undisposed (the handle's `using` closes
  nothing the call site opened — `OpenFileStream` never assigns `Stream`);
  9 discarding the `Task` returned by a delete.
- F4 attached to EXISTING anti-pattern #1 rather than duplicating it: the
  tick-less `$"{Folder}/{FileName}"` shape (5 sites / 3 module services), whose
  second consequence is that a same-named replacement makes the new key equal
  the old, so a correct delete-old step removes the object just uploaded.
- SKILL.md 460 → 485 (<500 bar): F4 attachment, a 4-row pointer block, one
  references-table row. No existing entry renumbered or rewritten.
- Shared-blind-spot corrections: both authors called the `Count > 0` guard
  redundant (the shipped DeleteManyAsync logs on the empty branch and the
  failure path empties the list — guard kept, GOOD block is now a one-keyword
  diff) and both claimed a failed fire-and-forget delete is invisible to logs
  (the service Log.Errors it; what is lost is the join to the request).
- Refused: the coordinator's own "temp file survives until finalization" F3
  claim (contradicts shipped media-downloads.md), finalizer timing, encoding
  substitution semantics, cancellation/scope semantics, and an unverified
  routing of a hash helper to common-extensions.
```

## 8. Parked for the main session / user

1. **A sixth candidate both authors surfaced independently, not labelled** (outside my
   approved rows): the same three module services as F4 treat a failed upload as "no file"
   — `if (await UploadAsync(file, key)) { entity.Image = key; }` — so the request returns
   200 having persisted nothing. It is a *distinct* failure mode from shipped entry #5
   (which persists a key for an object never written). If the user wants it, it needs its
   own BAD/GOOD: ~14 lines in the references file + 1 pointer row = 486, still under the
   bar. My recommendation: bank it — four entries plus an attachment is the right size for
   one ship, and it is a new label, which is the user's call.
2. **A create path in one of those services uploads outside any transaction with no
   compensating delete at all** — the negative case for the shipped "Create: upload first,
   compensate if the row fails" Pattern, which also has no negative example today. (A only.)
3. **The ingest handler also re-declares the `Format` const** — a literal live instance of
   shipped entry #1's first BAD block, in the same file as F2 and F3. No new entry needed;
   it confirms entry #1 was never hypothetical. Note this is the same file family as
   decision-table row F10, which the table marks BỎ to avoid double-labelling.
4. **Possible *positive* callout** (B flagged, arbiter verified as correct design): at the
   F6 site the rollback path clears the delete list before the `finally`, so a rolled-back
   request deletes nothing. Entry 9 now tells readers not to touch that guard, but whether
   it deserves a positive example somewhere is the user's call.
5. **Token-name inconsistency now spans the pointer**: SKILL.md's anti-patterns 1–5 use
   `ct`, the new 6–9 use `cancellationToken` (chosen so they diff cleanly against
   `usage-patterns.md` §5/§7, which they are negative images of). Pre-existing across the
   references set; flagged, not fixed.
