# R8 labelling pass — decisions (2026-07-31)

**Delegation event.** Groups 1–2 were decided by the user directly. After group
2 the user said *"từ nay cứ theo bạn đề xuất đi"* — R8 labelling for the rest of
this pass is delegated to the coordinator, decisions recorded here with reasons
so any can be vetoed later. The standing R8 carve-out is waived for this pass
only, by explicit user instruction.

LABEL = ships as a named anti-example in the owning skill. BỎ = stays
neutral/untaught, never framed as an anti-pattern.

**Standing exclusions applied across every group** (from the user's own group-1
calls): misspellings/typos are never labelled; a merely-divergent variant that
lost a canonical pick is never labelled; dead-but-harmless code is not labelled.

## Group 1 — soft delete → `ef-core-data-access` (user-decided)

| # | Candidate | Decision |
|---|---|---|
| S1 | No-op `.IgnoreQueryFilters()` where no `HasQueryFilter` is registered | **LABEL** |
| S2 | Dead `if (expression == null)` branch in `Find` (helpers never return null) | **LABEL** |
| S3 | `Where(_ => true)` on entities that never opted in | BỎ |
| S4 | `HiddenEntension` misspelling | BỎ |
| S5 | `HiddenObject` public / `ApplySoftDelete` private asymmetry | BỎ |
| S6 | Dead `FindNotDeleted` family | BỎ |
| S7 | Copy-pasted XML comment on both stamps | BỎ |
| S8 | `HasCitextUniqueHasFilter` as a second method instead of an optional parameter | **LABEL** (cites R25) |

## Group 2 — list pipeline → `list-query-pipeline` (user-decided)

| # | Candidate | Decision |
|---|---|---|
| L1 | `Console.WriteLine` diagnostics inside `ApplyFilter` (4 sites) | **LABEL** |
| L2 | `catch` that swallows a filter failure — unfiltered rows returned as 200 | **LABEL** (label the *silence*, not the fallback) |
| L3 | The `Any()` probes | BỎ — shipped canon + perf cost model depend on it |
| L4 | Shared reflection helper hardcoding `S3FilePath` + a literal `"LanguageCode"` | **LABEL** |
| L5 | `TrimEnd(' ','a','n','d','o','r')` eating trailing letters (`Ordered` → `Ordere`) | **LABEL** |
| L6 | `*Extention` filename misspellings | BỎ |
| L7 | Duplicate `NotSearchAttribute` / `NotSearchableAttribute` | BỎ |
| L8 | Dead untyped `Data` member on the paged response (JSON contract) | **LABEL** (api-surface co-owned) |
| L9 | `propertyInfo.GetType().IsGenericType` — always true; meant `PropertyType` | **LABEL** |
| L10 | `Current` violation reported under the `PageSize` key | BỎ — could not reproduce in corpus |

## Group 3 — Excel → `excel-miniexcel` (delegated)

| # | Candidate | Decision | Reason |
|---|---|---|---|
| E1 | The zip variant that lost the canonical pick | BỎ | mere divergence |
| E2 | Unbounded upload size gate | **LABEL** | resource exhaustion on an anonymous-ish path |
| E3 | `ValidateXxx` inline-validation ladder | BỎ | ownership is module-feature's; weak here |
| E4 | Auto-clean job scheduled inside the transaction, before commit | **LABEL** | job can fire on rows that roll back |
| E5 | `Console.WriteLine` + `Stopwatch` probe in a production import path | **LABEL** | same rule as L1 — keep the pair consistent |
| E6 | `HanldePhotos` / `HanlLargeFile` misspellings | BỎ | typo family |
| E7 | `async` lambda in `List.ForEach` inside a `finally` | **LABEL** | fire-and-forget; exceptions unobservable |
| E8 | `DateTime.UtcNow.Ticks` as the uniqueness prefix | **LABEL** | real collision window under concurrency |

## Group 4 — S3 / files → `file-storage` (delegated)

| # | Candidate | Decision | Reason |
|---|---|---|---|
| F1 | Delete the old object BEFORE the new upload succeeds | **LABEL** | data loss on upload failure; strongest of the group |
| F2 | Checksum computed over UTF-8-decoded binary bytes | **LABEL** | silently wrong for any non-text file |
| F3 | Undisposed `FileStream` + `MemoryStream` | **LABEL** | handle/memory leak on a hot path |
| F4 | Hand-built key with no uniqueness component | **LABEL** | the concrete site of the skill's anti-pattern #1 |
| F5 | `AmazonS3Client` + pre-sign body duplicated in converter and service | BỎ | duplication only |
| F6 | `_ = DeleteManyAsync(...)` fire-and-forget | **LABEL** | unobserved task; failures vanish |
| F7 | `PutObjectAsync` + multipart escaping the class's own bool contract | BỎ | too subtle to teach cleanly |
| F8 | Five never-assignable get-only auto-properties | BỎ | dead-but-harmless |
| F9 | `GetPreSignedUrl` bucketName parameter asymmetry | BỎ | shape nit |
| F10 | Format-const re-declaring module file | BỎ | already carries an S17 label on other grounds; double-labelling confuses |

## Group 5 — outbound HTTP → `http-client-factory` (delegated)

| # | Candidate | Decision | Reason |
|---|---|---|---|
| H1 | Sync-over-async eager read in the divergent sender lineage | **LABEL** | classic deadlock/thread-starvation shape |
| H2 | Non-partial settings class | BỎ | divergence |
| H3 | Mixed registration helper | BỎ | divergence |
| H4 | The `IFormFile` loop bug the canonical sender fixed | **LABEL** | real bug, before/after already exists |
| H5 | Uncalled settings-wiring half | BỎ | dead-but-harmless (S6 precedent) |
| H6 | Builder-state carryover between calls (`UseClient` stickiness) | **LABEL** | cross-request contamination |
| H7 | Typed-client double registration | BỎ | divergence |
| H8 | `Extentions` / `Atrribute` filename typos | BỎ | typo family |
| H9 | Readers that swallow a failure into a 500 | **LABEL** | same rule as L2 |
| H10 | `!`-dereference after read | BỎ | weak |
| H11 | A request property defaulted to `Random.Shared.Next` | BỎ | off-topic here; stays rubric feed |

## Group 6 — utilities → `common-extensions` (delegated)

| # | Candidate | Decision | Reason |
|---|---|---|---|
| C1 | 169-line module-contaminated `ExpressionExtension` | **LABEL** | the user's own "most confused point", with live evidence |
| C2 | The 22-line `ActionContextExtension` (three defects in one file) | **LABEL** | already the user-named poor variant |
| C3 | `Service<T>()` itself | BỎ | already taught as anti-pattern 4 in shipped text |
| C4 | Inline `CreateScope`-without-`using` one-liner | BỎ | same ground as C3 |
| C5 | Serializer service: options on serialize, none on deserialize | **LABEL** | round-trip asymmetry, silent |
| C6 | `SemaphoreSlim` unawaited `WaitAsync` in the sync overload | **LABEL** | real concurrency defect |
| C7 | `RegexExtension`'s own inline `new Regex` sites | BỎ | corrected in canon; the law already teaches it |
| C8 | `A-z` character class (matches `[ \ ] ^ _ ` `) | **LABEL** | classic latent regex bug |
| C9 | `IsValidAllPhoneNumber` — permits almost anything | **LABEL** | validation that validates nothing |
| C10 | `GetRemoteIpAddr` dead guards + `string.Empty`-not-null | BỎ | already documented neutrally |
| C11 | `Guid.Parse` instead of `TryParse` on a route value | **LABEL** | malformed input → 500 instead of 400 |
| C12 | Per-call options clone with null configs | BỎ | weak |
| C13 | Middleware re-implementing the IP chain inline | **LABEL** | the exact violation R25 exists to prevent |
| C14 | Body-buffering variant with a no-op seek | BỎ | needs an API-behaviour check provenance forbids |
| C15 | `RandomRangeNotRepeat` double enumeration | BỎ | perf nit |
| C16 | `TickCount`-seeded static `Random` reachable from password generation | **LABEL** | predictable secrets — strongest security item of the pass |

## Totals

32 LABEL · 31 BỎ, across 63 candidates and 6 owning skills.
