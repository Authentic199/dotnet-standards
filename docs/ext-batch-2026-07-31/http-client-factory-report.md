# http-client-factory — coordinator report (RE-RUN, 2026-07-31)

## 1. Status: COMPLETE

Re-run succeeded. The first run's blocker (arbiter could not load
`skill-creator:skill-creator`) is gone: the arbiter loaded it LIVE on its
context ping, confirmed BEFORE any author dispatch (relaunch addendum §2
honored). Full three-way loop ran: piece 1 (frontmatter), pieces 2–5 batched
(body), piece 6 (three references files), final QA pass over the assembled
files on disk — PASS, with a mandatory budget cut applied and re-verified.
First-run drafts were discarded; authors respawned fresh (addendum §3/§8).

## 2. Files written under skills/http-client-factory/

| File | Lines |
|---|---|
| SKILL.md | 491 (frontmatter 18 + body; hard bar < 500 cleared) |
| references/sender-and-result.md | 580 |
| references/content-extensions.md | 615 |
| references/registration-and-settings.md | 345 |

Line counts by `\n` count on disk. Shipped-references precedent allows >500
(largest shipped is 840; arbiter verified). Nothing else in the repo was
touched: no router, no manifests, no CHANGELOG, no git.

## 3. Final description + proposed router work

### 3.1 Final description (98 words by wc -w, arbiter-MERGE, coordinator-verified)

```
This skill should be used when .NET code calls out over HTTP: reaching a
third-party or integration API, any new HttpClient(), injecting
IHttpClientSender, chaining
UseClient/UseMethod/WithUri/WithHeaders/WithContent into SendAsync, reading
an HttpResult, JSON, form-urlencoded or multipart via
ToStringContent/ToFormUrlEncodedContent/ToFormDataContent, an outbound file
upload, [FormName] flattening, an HttpClientSettings partial or
httpclient.json, registering AddHttpClientSender or typed AddHttpClient, or
recreating the sender facade where none exists. Not for: object storage,
media download workflows — file-storage; inbound endpoints, DTOs —
api-surface; exception types, ExceptionHandlerMiddleware — error-handling;
JWT, secret storage — auth-and-security; client file placement —
facade-module-architecture; utility extensions — common-extensions; faking
the sender — dotnet-testing.
```

### 3.2 Proposed router rows for choosing-a-dotnet-skill (do NOT let me edit it)

Base map row:

```
| Calling out over HTTP: the IHttpClientSender chain and HttpResult, content built via ToStringContent/ToFormUrlEncodedContent/ToFormDataContent and [FormName], HttpClientSettings partials and httpclient.json, typed AddHttpClient clients, or recreating the sender facade | `http-client-factory` |
```

"When two skills both look right" rows:

```
| HTTP | an inbound route, controller or DTO — `api-surface`; the outbound call through the sender facade — `http-client-factory` |
| a file over the wire | uploading it to a third-party API (ToFormDataContent) or pulling bytes through the sender — `http-client-factory`; object storage and the download-then-store workflow — `file-storage` |
| retry / timeout | not the facade's — module-owned settings consumed by that module's own client; the settings-shape example — `http-client-factory` (which teaches only the boundary) |
```

(Main session trims to house rhythm; the first two matter most.)

### 3.3 Proposed Not-for additions to sibling skills (I did not edit them)

- **file-storage** (batch sibling, its coordinator/main session applies):
  add `the outbound HTTP call itself — http-client-factory`. Arbiter-flagged
  as load-bearing: two corpus module services pull binary media THROUGH the
  sender; without the reciprocal entry, "download the partner's media file"
  routes to a skill that does not teach the transport.
- **api-surface** (optional, needs an api-surface-owning session): a
  reciprocal `outbound HTTP, the sender facade — http-client-factory` would
  close the inbound/outbound axis from both sides. Not urgent — this skill's
  own Not-for already draws it.

## 4. Proposed CHANGELOG entry (main session renumbers)

```
## 0.3.NN — http-client-factory

feat(http-client-factory): new skill — the mandatory outbound-HTTP facade.
IHttpClientSender fluent chain (no verb shortcuts; UseMethod is data),
HttpResult return-don't-throw contract (readers swallow to 500 — check
status before reading), builder-state-persists teaching (headers/URI/
content/UseClient all outlive a send; UseClient is sticky and silences
logging), content builders + [FormName]/PropertyFlatten support set carried
in full (compile-standalone, incl. two ReflectionHelper methods and a
minimal ValidationContextExtension), empty base HttpClientSettings partial
canonical (per-integration partials own properties and validation; apsp's
IValidatableObject base verified functionally inert — reference-equality
comparison never fires), split registration canonical (5-of-6; composition
root must call AddHttpClientSender AND AddClientSetting — cpc/ops corpus
half-wiring found and taught as a check, not labelled), ReHackt
.Extensions.Options.Validation 7.0.1 named as the registration
prerequisite, leading-slash route convention (4-of-4 corpus files),
resilience = module-owned settings only (HiveStack-shape keys shown in one
JSON section; zero doc-recall recipes). ZERO-new-HttpClient is user
doctrine (corpus: zero occurrences outside the facade's static client).
Corpus-divergent lineages (mtc sync-over-async eager read; non-partial
settings) presented neutrally, unlabelled (R8).
```

## 5. Verdict log, coordinator catches, delegated calls

### Verdicts

| Piece | Verdict | One line |
|---|---|---|
| 1 frontmatter | MERGE (98 w) | A's spine + B's tokens; arbiter cut "webhook" (both authors shared it; corpus webhook surface is inbound — grep-verified); dotnet-testing entry included on auth-and-security's shipped precedent, NOT B's S15 reading; automapper-mapping omitted on correctness (references carry HttpResultProfile — disclaiming Profiles would misroute); A's "outbound tokens — auth-and-security" rejected on code (credential attachment is this skill's) |
| 2–5 body | MERGE (now 471 body lines after budget) | B's builder-state principle + A's why-paragraphs; arbiter found the shared claim UNDERSTATED (Uri/Method/Content/CustomClient/UseLogging all persist; UseClient sticky) and B's registration form the 1-of-6 outlier (R7 → split form + composition-root warning); A's scoped underscore-hyphen claim FALSE (applies to every parsed key); GET/query-string row corpus-grounded and added; Outcome.* invented types removed |
| 6 references | MERGE ×3 (base = A all three) | Transcriptions byte-identical between authors (mechanical diff); B's File 3 had two functional defects the arbiter caught (empty-string credentials fail ValidateOnStart boot; missing leading slashes vs 4-of-4 corpus routes); A's false justification for an unused using caught and replaced; empty base partial per my delegated ruling |
| Final QA | PASS conditional | 21-point assembly verification all green; anchors/contradictions/sanitization clean; mandatory 31-line cut list applied → 491 lines; arbiter self-diagnosed its 389-line miscount (Measure-Object -Line skips blanks — use (Get-Content).Count) |

### Coordinator catches (beyond relaying)

1. **Arbiter's round-2 line count wrong**: claimed 389, actual 502 → total 521
   vs hard bar <500. Caught at assembly by my own count; forced the budget
   pass. Lesson recorded: count lines with a blank-line-inclusive method.
2. **B's resilience-keys grep wrong twice**: B reported
   RetryCount/RetryDelayMilliseconds/TimeoutSeconds absent from all .cs; my
   direct grep found them at HiveStackSettings.cs ll.33–37 + consumer
   HiveStackClient.cs ll.62–65. Resolved with file:line both times.
3. **Base-partial censuses conflicting proposals**: authors agreed on facts,
   proposed opposite canonicals. My tie-breaking code-reading — apsp's
   IValidatableObject base is DOUBLY inert (fresh-instance reference
   equality never true; null section short-circuits) — arbiter traced and
   confirmed. Empty base ruled canonical (delegated best-variant authority).
4. **First-run survey corrections found this run**: support set is five files
   + two ReflectionHelper methods (ReflectionHelper was a missed compile
   dependency); ValidateDataAnnotationsRecursively is NuGet ReHackt
   7.0.1, NOT corpus extension code (first-run §6 boundary note was wrong);
   be-booking consumer path is src/Infrastructure/Modules/... (old path 404s).
5. Verified the arbiter's shipped-precedent claims myself (ExceptionHandlerMiddleware
   is error-handling's own vocabulary; "faking a principal — dotnet-testing"
   in auth-and-security, line-wrapped); spot-checked the builder-state line
   citations directly in the canonical sender before accepting round 2.

### Delegated judgment calls (standing delegation; each recorded when made)

- Arbiter-first spawn order (addendum); authors only after skill-creator load confirmed.
- ReflectionHelper two-method carry; ValidationContextExtension + IsNullableType
  minimal carry with dedup notes (compile-standalone doctrine).
- file-storage Not-for entry ships the brief-compliant "object storage, media
  download workflows — file-storage" (brief's gloss honored; arbiter's
  corpus tension recorded — see §7).
- Corrected filenames in references (HttpClientExtensions.cs /
  FormNameAttribute.cs) + one variance note + "don't rename as a drive-by".
- ToFormDataContent loop defect ships CORRECTED (be-booking/ops body is the
  canonical and already fixes it); swallow-to-500 taught neutrally with
  check-status-first; constructed negatives ruled NOT R8; log format strings
  kept verbatim (no project identity; recreate-verbatim wins).
- Resilience: no Not-for entry (no shipped owner); boundary-only teaching;
  the three key names shown in ONE JSON section as the corpus-shaped example.
- No Not-for entries for list-query-pipeline / excel-miniexcel (both authors
  + arbiter: no confusable boundary).
- automapper-mapping: prose sentence at the registration section, NO Decision
  Guide row (shipped precedent: routing rows mirror the description roster).
  **This is the single body mention of a skill absent from the description —
  cheap veto if the user dislikes it.**
- Accepted 491 lines; refused further cuts (S17 no-number-chasing).
- Added the missing TOC row in sender-and-result.md (arbiter's non-blocking
  note; one line).

## 6. Variant-comparison table (what won, what was improved)

| Area | Winner | Improvement over corpus |
|---|---|---|
| HttpClientSender.cs | be-booking/cpc/ops lineage (byte-identical trio; RequestBuilder + Uri guard + TryAddWithoutValidation + null/whitespace header skip) | None needed; behavioural contract documented (builder-state persistence, UseClient stickiness, Duration semantics, the one throw); 2 cosmetic comment fixes (dropped upstream #119 comment, ISO typo) |
| HttpResult + HttpResultProfile | The 5-of-6 AutoMapper shape | Taught with check-status-first consequence; OnError subscribe-before-read |
| HttpClientExtentions.cs | be-booking/ops body (fixes apsp's item/value IFormFile loop bug; ContentType from file.ContentType) + apsp's ToFormUrlEncodedContent(useSnakeCase) overload | Synthesis exists in no single corpus file; XML doc written for the undocumented overload; overload-divergence caution added |
| HttpPropertyFlattener + support set | apsp PropertyFlattener base ([JsonIgnore]-skip) + byte-identical rest + ReflectionHelper 2 methods | Single-use flattener note; protected-ctor explanation |
| HttpClientSettings base | EMPTY partial (cpc/digitalcity/ops; 3-of-5) | apsp's IValidatableObject base verified inert (arbiter-traced); be-booking's properties-on-base = layering violation; both demoted to variance notes |
| Startup registration | Split form (apsp/ops/cpc/digitalcity; 5-of-6) | Composition-root-must-call-both warning (cpc/ops half-wiring found: AddClientSetting defined, never called); ReHackt 7.0.1 prerequisite named |
| Typed client | The corpus AddScoped + AddHttpClient<TInterface,TImpl> pairing with BaseAddress from IOptions | Reproduced without asserting DI resolution order (unverifiable) |
| httpclient.json | Invented from settings-class shapes (never opened corpus values into final text) | Boot-safe placeholders ("<supplied per environment>" — empty strings fail Required()+ValidateOnStart, an arbiter catch); leading-slash routes (4-of-4 corpus); resilience keys in one section only |

## 7. Open questions / parked items (for the main session / user)

1. **file-storage reciprocal Not-for** — "the outbound HTTP call itself —
   http-client-factory" (§3.3). The corpus tension behind it: my brief said
   "downloading media is theirs even though it uses HTTP", but two corpus
   module services pull binary bodies through this sender. The shipped entry
   ("media download workflows") honors the brief while narrowing the
   misroute; the reciprocal entry closes the rest.
2. **R8 bank (labels are the user's alone; all shipped NEUTRALLY):** mtc
   sync-over-async eager-read lineage; non-partial settings (mtc);
   AddSystemClient mixed registration (mtc); apsp IFormFile loop bug
   (superseded by canonical); the cpc/ops uncalled-AddClientSetting
   half-wiring; UseClient stickiness / builder-state carryover sites; the
   typed-client double registration; the corpus `Extentions`/`Atrribute`
   filename typos; swallow-to-500 readers; `!`-dereference after read in a
   corpus client; a request property defaulted to Random.Shared.Next
   (off-topic, banked for rubrics).
3. **Resilience owner**: still none on the shipped roster. Whichever session
   ever ships a resilience/observability owner should add reciprocal
   pointers; this skill teaches only the module-owned boundary.
4. **Nothing was compiled.** All code was corpus-verified by transcription
   diff (the two authors' independent transcriptions were byte-identical);
   the one block worth pasting into a scratch project is the synthesized
   ToFormUrlEncodedContent overload pair (unambiguous by inspection —
   different arities).
5. **automapper-mapping prose mention** (§5, delegated calls) — cheap veto.
6. **Process lessons for house-laws**: (a) arbiter-first ordering confirmed
   its worth; (b) count lines blank-inclusive — PowerShell
   `Measure-Object -Line` silently skips blank lines and produced a
   389-vs-502 miscount that nearly shipped a hard-bar violation; (c) agent
   task .output files are JSONL transcripts — extract the final assistant
   message for verbatim draft relay rather than re-typing through
   entity-escaping transport.
7. **Scratchpad artifacts kept for audit**: draft-a-refs.md, draft-b-refs.md
   (byte-verbatim round-3 drafts), final-frontmatter.yaml, final-body.md
   (pre-cut round-2 body), assemble.py, cuts.py (the exact assembly and
   budget edits, fail-loud anchored).

## 8. First-run corpus survey (retained for provenance — corrections in §5 apply)

All six projects hold `src/Infrastructure/Facades/Common/HttpClients/` and
`src/Web/Configurations/httpclient.json`; apsp worktrees excluded everywhere.
Sender lineages: (a) apsp — mutable request field reset in finally,
Headers.Add, no Uri guard; (b) be-booking = cpc_backend = ops-service —
byte-identical RequestBuilder form (CANONICAL, chosen); (c) digitalcity —
options + Reset() + using var request; (d) backend-mtc — no AutoMapper,
HttpResult:IDisposable, ctor eager sync-over-async read (corpus-divergent,
unlabelled). Extensions: be-booking = ops best (IFormFile fix); apsp adds
snake_case overload (merged in). Flattener byte-identical in 5 (absent mtc);
apsp base has the [JsonIgnore]-skip. Settings partial mechanism in 5 of 6;
mtc single non-partial class. Registration split form in 5; mtc
AddSystemClient mixes concerns. Typed clients in 4 projects (5 sites).
Doctrine grounding: `new HttpClient(` zero hits outside the facade across all
six (apsp constructs via target-typed `new(Handler)`). Consumers census and
resilience findings as in §5–§6. Live-looking credentials exist in corpus
httpclient.json files — none opened into final text this run.
