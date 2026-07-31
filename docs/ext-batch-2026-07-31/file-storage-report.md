# file-storage — coordinator report

## 1. Status: COMPLETE

Re-run succeeded. The first run's blocker (arbiter could not load
`skill-creator:skill-creator`) is resolved — the arbiter loaded it LIVE at
session start and held its methodology through all seven verdicts. The full
three-way loop ran: arbiter-first ping, five carried-over questions ruled, then
piece 1 → pieces 2–5 (batched) → references 6a → references 6b, each with two
independent author drafts relayed VERBATIM (scratchpad files
`file-storage-6a-authorA/B.md`, `file-storage-6b-authorA/B.md`) and a
file-verified arbiter verdict. A mid-session monthly-spend-limit interruption
killed all three agents once; all resumed from transcripts, nothing lost.

Agents: arbiter `ad3d150f0821a6c47` (skill-arbiter), Author A
`a36e83cb7130fb40d` (skill-writer-a), Author B `a1e52665845287960`
(skill-writer-sp). Each spawned once, continued via SendMessage.

## 2. Files written under skills/file-storage/

| File | Lines |
|---|---|
| SKILL.md | 460 |
| references/implementation.md | 860 |
| references/key-generation.md | 191 |
| references/media-downloads.md | 539 |
| references/usage-patterns.md | 315 |

Sanitization sweep clean: no project names, no business-domain nouns, no
credentials (config examples are placeholder-only on `.invalid` hosts; the
corpus filestorage.json files hold REAL credentials and none of it shipped).

**Budget deviation, arbiter-ruled (keep):** SKILL.md ships at 460 lines against
the 117–450 target band, inside the <500 hard bar. Audited against the four
author-declared trim candidates; two were already consumed during arbitration,
and the remaining genuine redundancy totals ~4 lines, which does not reach 450.
Closing the gap would require cutting the URL-method taxonomy, ruled
load-bearing at the Patterns verdict. Progressive disclosure is satisfied by
four references/ files; no section is disproportionate. Banked micro-trims if
the body is ever reopened: Principle 5's two bullets (restated by the two flow
Pattern titles) and the DeleteManyAsync DG row.

## 3. Description, router row, Not-for additions

Final description (98 words by wc -w, em-dashes counted): see SKILL.md
frontmatter — locked at the piece-1 MERGE. Merge-time obligation (S17
no-dangle): if `common-extensions`, `excel-miniexcel`, or `http-client-factory`
fails to ship in the same merge, its Not-for entry must be cut; ~10 words of
headroom held. (`excel-miniexcel` and `http-client-factory` already have router
rows on main as of this writing.)

Proposed router row for `choosing-a-dotnet-skill` (base map, capabilities
neighborhood — do NOT edit the router from this session):

| Storing files in S3: the storage facade and its recreation, uploading IFormFile/Stream/directory, bucket keys, pre-signed vs public vs service URLs, `S3FilePath` on responses, attachment downloads, deleting objects, ingesting an external URL | `file-storage` |

Proposed Not-for additions to EXISTING siblings (exact sentences; owning
sessions/main decide):
- api-surface: `file fields, pre-signed URLs, S3FilePath — file-storage`
- automapper-mapping: `S3FilePath in MapFrom, IsSystem — file-storage`
- http-client-factory (batch sibling — its coordinator's call): `media download pipeline, IMediaManager — file-storage`

## 4. Proposed CHANGELOG entry (main session renumbers)

feat(file-storage): S3 file-storage facade as recreatable canon (0.3.xx)
- New skill `file-storage` (SKILL.md 460 ln + 4 references, 2,365 ln total):
  facade file set (S3FilePath, S3FilePathConverter, S3AwsSettings,
  IS3AwsFileStorageService + service, S3FileUploadException, Startup),
  S3AwsExtensions key law `{Folder}/{Ticks}_{SanitizedFileName}{Extension}`,
  MediaDownloads external-URL ingest pipeline, wiring + call-site flows.
- Arbiter-ruled syntheses: converter Write gains the IsSystem branch (external
  URLs emitted verbatim; every corpus site passes true, so zero behaviour
  change); GetPreSignedUrl gains optional responseContentDisposition;
  DirectoryUploadAsync merged; Stream extension overload + public
  FormatFileName(fileName, folder); FileStorage.cs ships as a marked optional
  appendix (consumed nowhere in the corpus).
- Corrections shipped, each recorded in a per-file "Normalizations at a glance"
  table: converter Read (corpus re-parses the token as JSON — throws on every
  real payload); dead `Path.GetExtension(...) is null` guard →
  IsNullOrEmpty; AddMediaManager → AddHttpClient<IMediaManager, MediaManager>
  (corpus AddScoped resolves only by accident; transient-lifetime delta noted
  in prose); create/update flows call BeginTransactionAsync (all six corpus
  upload+transact sites do); misspelled GetTempFileNameWithoutDicrectory
  member + constant renamed; namespace and CS1572 XML-doc placement fixes.
- Refused: AutoCloseStream=false (wrong overload); digitalcity's
  ServiceUrl-derived protocol (defect); "AutomaticCloseOnDispose" behavioural
  correction (ships verbatim with the caller-dispose contract stated in
  prose); every unverifiable API-recall claim.
- Excluded: KeysGenerationExtension.cs (RSA crypto, not S3).

## 5. Verdict log, coordinator catches, delegated calls

### Verdicts (all file-verified by the arbiter)

- Carried-over Q1–Q5: SYNTHESIZE / MERGE / SPLIT / MERGE / appendix — detail in
  §6 notes below and in the arbiter transcript.
- Piece 1 (frontmatter): MERGE — A's opener + B's trigger set; B's AddHttpClient
  Not-for entry load-bearing (literal-token precedent); module-feature stays
  out (S15 pointer rule); list-query-pipeline omission verified (no
  list/enumerate surface on the facade).
- Pieces 2–5 (batched by coordinator per house-laws §1): MERGE — A's URL-method
  taxonomy + BAD/GOOD form + sibling DG rows; B's recreate-doctrine argument,
  wiring compression, update-ordering argument; both authors' non-compiling
  snippets replaced; FormatFileName order normalized (4 sites in A).
- Piece 6a (implementation + key-generation): MERGE ×2 — A's guard/checklist/
  normalizations devices + B's package table with ValidateDataAnnotations
  fallback, filename-vs-classname warning, config split, .invalid hosts. H1
  ruled IN for references files (27/37 house majority; my "no H1" instruction
  was an over-generalization). Converter's private GetPreSignedUrl confirmed
  3-parameter (both authors independently + arbiter).
- Piece 6b (media-downloads + usage-patterns): MERGE ×2 — A's scaffolding
  devices + dispose-contract chain; B's FileShare.Delete/using consequences,
  transaction shape, diagnostic symptom line; A's silent ExceptionMessage
  rewrite REJECTED (unrecorded change); B's untested "options binding runs
  twice" cut; converter-duplicate direction compile-verified (FIRST registered
  wins) and rewritten.

### Coordinator catches

1. FormatFileName argument order: A's body snippets reversed the settled
   (fileName, folder) signature — normalized at verdict.
2. Mapping-site count: my package said 13; both authors counted 12; arbiter
   confirmed 12 (loose grep had matched the ctor declaration). No count ships.
3. A's unrecorded ExceptionMessage rewrite — caught by corpus grep, rejected.
4. AWSSDK.S3 3.7.205.10 verified in Infrastructure.csproj before the package
   name shipped (A's own request).
5. AddAsync + transaction members verified against RepositoryWrapper.cs and a
   live service call site before the flow snippets shipped (B's request).
6. common-extensions ships NO hashing helper (checked its coordinator's
   report) — both drafts' attribution removed; ComputeChecksumAsync ships as a
   declared placeholder with a raw-bytes hedge.
7. MediaDownloads exists only in 2 of 6 projects (arbiter; my context package
   had overclaimed) — the reference file says so honestly.

### Arbiter self-corrections (its own settled fragments)

CS1572: `<param>` tags moved from the struct to the constructor
(compile-verified warnings); the fragment (h) self-import using dropped.

### Delegated judgment calls (standing delegation; all recorded)

1. Converter Read corrected (+ response-only note). 2. AddMediaManager →
AddHttpClient (comment restricted to the constructor fact; transient note).
3. S3FileUploadException as-is + batch note; NOT rewired. 4. Misspelled member
+ constant corrected (S17 API-name sanitization; cosmetic). 5. MediaDownloadInfo
VERBATIM — dead Stream property kept; caller-dispose contract in prose; inert
argument pass-through acknowledged (semantic vs cosmetic distinction).
6. FileShare.Delete prose states verified facts only, no platform mechanism.
7. Pieces 2–5 batched into one round. 8. References file plan fixed at four
files (house shape per distributed-caching). 9. BeginTransactionAsync post-lock
correction to the body AUTHORIZED (corpus-mandated: 25 sites, all six
upload+transact services) — the one finding that reached back into approved
text. 10. Budget ruling (a): keep 460, deviation recorded. 11. Header-injection
sentence cut per B's altitude recommendation; the bucket-key-is-not-a-filename
sentence kept. 12. .invalid placeholder TLD over .example (non-resolvable).

## 6. Variant-comparison table

| File | Findings / winner |
|---|---|
| S3FilePath.cs | apsp wins (string?, ctor-param isSystem, get-only IsSystem). digitalcity's settable-IsSystem shape refused (default(S3FilePath) yields false). |
| FileResponseConverter.cs | apsp=be-booking=cpc=ops byte-identical; final = apsp shape + digitalcity's IsSystem Write branch + corrected Read. digitalcity's ServiceUrl-protocol line refused (defect). Read broken verbatim in all five. Converter's own GetPreSignedUrl stays 3-parameter (serializer has no request to take a disposition from). |
| S3AWSSettings.cs | Identical in 5; transcribed (file renamed S3AwsSettings.cs — casing normalization). |
| S3AWSFileStorageService.cs | apsp base (only variant with the GetServiceUrl pair — MediaDownloadExtension depends on it) + be-booking's responseContentDisposition (verbatim-grounded, 1 real consumer) + cpc's DirectoryUploadAsync (1 real consumer). cpc's AutoCloseStream=false refused (wrong overload). mtc multi-tenant fork not canon. defaultS3CannedAcl = PublicRead identical in all six; named in the reference. Multipart + PutObjectAsync ship verbatim with the exception-escaping asymmetry documented, not "fixed". |
| S3FileUploadException.cs | Identical in 5; thrown only in digitalcity — ships with the batch-rollback note; extension keeps InternalServerException (4/5 canon). |
| FileStorage.cs | Dormant everywhere (zero type references in 5 projects); ships as marked optional appendix with the IsSystem-inexpressible note. |
| Startup.cs (facade) | Byte-identical in four; transcribed with the Options using kept (corpus text; the omitting variant differs in two ways at once). |
| S3AwsExtensions.cs | apsp key rule + cpc's Stream overload + FormatFileName (corrected guard, cpc's namespace). digitalcity's `-` separator + legacy zip members excluded; mtc excluded. |
| MediaDownloads (5 files) | apsp canonical — exists ONLY in apsp + digitalcity. Corrected Startup (AddHttpClient). MediaDownloadInfo verbatim incl. inert Stream/AutomaticCloseOnDispose; ExceptionMessage dashed format verbatim; misspellings corrected; Vietnamese docs → English; Console writes removed. digitalcity's Stream-assigning OpenReadStream fork noted, not adopted. |
| Consumer flows | Create/update transaction shape from the corpus's six upload+transact sites (all call BeginTransactionAsync; upload-before vs inside the transaction is genuinely split — no doctrine manufactured). Ingest flow from the single real consumer (download → hash → compare → skip-or-upload → key+checksum in one ExecuteUpdateAsync), with its defective UTF-8-decode-then-hash checksum ABSTRACTED out, not transcribed and not labelled. |
| Packages (csproj-verified) | AWSSDK.S3 3.7.205.10, ReHackt.Extensions.Options.Validation 7.0.1, Downloader 3.0.6, MimeTypesMap 1.0.9. |

## 7. Open questions / parked items

R8 label candidates — banked, user's alone, none labelled in shipped text:
1. Corpus delete-old-BEFORE-upload update ordering (Profile-user service site —
   the exact ordering the skill warns against; strongest candidate).
2. The consumer's checksum over UTF-8-decoded binary bytes (reason the helper
   is abstracted).
3. Its undisposed FileStream + MemoryStream.
4. A tick-less hand-built key `$"{Dir}/{Name.SpecialCharacterRemoving()}"` at a
   product-service site — anti-pattern #1's concrete site.
5. Converter/service AmazonS3Client + pre-sign body duplication.
6. `_ = DeleteManyAsync(...)` fire-and-forget site.
7. PutObjectAsync + multipart members escaping the class's own bool contract.
8. FileStorage.cs's five never-assignable get-only auto-properties.
9. GetPreSignedUrl's bucketName parameter asymmetry.
10. The format-const re-declaring module file already carries an S17 label on
    OTHER grounds (placement) — the user may want the labels reconciled.

Other parked:
- Facades/Medias/ (MediaUploadRequest + validators, 5 projects) remains
  unowned — was outside this brief's scope; flag to the user.
- ComputeChecksumAsync has no owner file anywhere in the plugin; if a hashing
  helper ever ships (common-extensions or auth-and-security), both flow files
  should adopt its real name.
- The description's merge-time obligation (§3) binds the merging session.
- The main session may want the arbiter's banked micro-trims (§2) only if the
  body is reopened for substantive reasons.
