# WORK PACKAGE A — body pointers into the five new skills

**Status: COMPLETE.** 5 pointers applied across 5 files, +25 lines. 11 candidate
sites rejected. No git run; nothing staged or committed. No description,
frontmatter, review skill, flow skill, agent, router or new-skill file touched.

Three-way loop run in full: `skill-writer-a` (A) and `skill-writer-sp` (B)
drafted independently, both drafts went to `skill-arbiter` **verbatim**, and the
arbiter's self-declared additions were verified by me against the shipped files
before anything was written. `skill-creator:skill-creator` loaded LIVE at user
scope — no `Unknown skill`.

Body-pointer coverage moves from **1 of 5 skills** (`common-extensions` only,
via `ef-core-data-access`) to **4 of 5**. `excel-miniexcel` stays at zero, by
ruling — see §4.

---

## 1. Pointers applied

### P1 — `skills/module-feature/references/validation-rules.md` → `common-extensions`

**Anchor:** §"The facade's rule helpers come first", after the paragraph ending
`copy is a second one, and the copies drift the day the rule changes.` (was
line 396).

```
`common-extensions` owns that file — the reuse → promote → inline ladder that decides
whether a rule deserves a helper at all, and the canonical form to recreate
`ValidatorExtension.cs` from when the project has none to open.
```

**Justification:** the host already sends the reader out to a bare
`Facades/Common/Extensions/ValidatorExtension.cs` with no owner named, and its
instruction ("open the file, find the rule") collapses silently when the project
has no such file — which is precisely the case the target skill covers.
**Restates:** `module-feature`'s own shipped `Not for:` — "reusable rule methods,
existence-check extensions — common-extensions". **449 → 453.**

### P2 — `skills/api-surface/references/request-response-dtos.md` → `list-query-pipeline`

**Anchor:** §"Search requests", after the paragraph ending `needs its own guard,
because the contract will not supply one.` (was line 185).

```
The contract is this skill's; the code behind it is not — `list-query-pipeline` owns
`QueryContainer`, the model binder that reads `filter.` off the raw query string and
the operator set in the table above. Go there when an operator behaves unexpectedly,
or when the project has no `QueryContainer` for this request to derive from.
```

**Justification:** the reader is deriving a request from a type another skill
declares, and the section reproduces that type's members and its whole operator
set without saying who maintains them. **Restates:** `api-surface`'s shipped
`Not for:` — "pipeline implementation — list-query-pipeline". **405 → 410.**

### P3 — `skills/ef-core-data-access/references/query-conventions.md` → `list-query-pipeline`

**Anchor:** §"The search shape", after the last design bullet
`- \`ToPagedListAsync\` last, so paging applies to the filtered, sorted result.`
(was line 36).

```
The call sites above are this skill's; the extensions behind them are not —
`list-query-pipeline` owns `QueryExpressionExtension` and `PaginationExtension`, and
is where to go when `ApplyFilter` or `ToPagedListAsync` does not resolve, or when one
of them has to be ported into a project that lacks it or repaired where it misbehaves.
```

**Justification:** the whole file is a call-site chain over four extensions it
does not declare — it even budgets their round-trip cost — and a reader hitting a
missing or misbehaving extension had no next step. **Restates:**
`ef-core-data-access`'s shipped `Not for:` — "query-extension internals —
list-query-pipeline". Placed in `references/` because SKILL.md is at 499.
**109 → 114.**

### P4 — `skills/automapper-mapping/SKILL.md` → `file-storage`

**Anchor:** §"Entity → response: computed and wrapped members", appended to the
`**Why:**` paragraph ending `the response carries the type the caller should see,
and the conversion exists once.` (was line 254).

```
Where that wrapper is the storage-key type a response exposes as a pre-signed URL,
`file-storage` owns it — the type, its JSON converter, and what the boolean second
argument decides.
```

**Justification:** the host teaches `MapFrom(src => new Wrapper(src.Reference,
true))` and leaves the boolean argument unexplained — deliberately, because its
meaning is not mapping mechanics. Corpus-verified as the real shape: 15+ sites of
`MapFrom(src => new S3FilePath(src.<column>, true))` across two projects.
**Restates:** `file-storage`'s shipped `Not for:` — "CreateMap, MapFrom —
automapper-mapping". This is the reciprocal banked in
`docs/ext-batch-2026-07-31/file-storage-report.md`. **454 → 458** (bar clear).

### P5 — `skills/dotnet-testing/references/unit-testing.md` → `http-client-factory`

**Anchor:** §"An outbound third-party dependency", after the paragraph ending
`additional package, and no container for something that is not yours to run.`
(was line 250).

```
Where the path reaches that API through the house's sender facade rather than holding
a client of its own, the seam is the facade: substitute `IHttpClientSender` and return
the `HttpResult` the path expects. `http-client-factory` describes that result —
including that the sender catches transport failures and returns a `500` instead of
throwing, which is the behaviour a faithful double reproduces.
```

**Justification:** the host's existing advice ("hand its typed client an
`HttpMessageHandler` stub") describes a seam this house's canonical outbound code
does not have — `http-client-factory` shows even a typed client is handed to the
sender with `.UseClient(httpClient)`, so the call site never sends. A double that
throws where the real sender returns a `500` is an unfaithful double.
**Restates:** `http-client-factory`'s shipped `Not for:` — "faking the sender —
dotnet-testing" (see the directional note in §5). **268 → 274.**

---

## 2. Verdict log

| Site | Verdict | One-line reason |
|---|---|---|
| 1 — `validation-rules.md` → common-extensions | **B** | A dragged `ValidationExtension` into an anchor about hand-rolled regexes — true but off-anchor, and it implies existence checks are all documented there, which `IsExistByUnique` disproves. |
| 2 — `module-feature/SKILL.md` → common-extensions (B only) | **NEITHER — rejected** | B's sentence misattributes `module-feature`'s **own** canonical file to a sibling. See §3. |
| 3 — `request-response-dtos.md` → list-query-pipeline | **MERGE** | B names the owned type (`QueryContainer` — a named token is what a reader opens); A supplies the repair trigger B lacked. |
| 4 — `query-conventions.md` → list-query-pipeline | **MERGE** | B names the two extension files; A's "the call sites above" is unambiguous where B's "All four" mis-counts a six-item bullet list. |
| 5 — `automapper-mapping` → file-storage | **B**, A's placement overruled | A's insert severed the code block from its own `**Why:**` paragraph; A also taught the boolean's answer, duplicating the target. |
| 6 — `unit-testing.md` → http-client-factory | **A**, arbiter rewrite | B's rejection refuted by the target's own body; A self-flagged this as its weakest pointer and on the evidence it is the most load-bearing. |

---

## 3. Coordinator catches

1. **`IsExistByUnique` — a provenance trap both I and Author A hit from
   different sides.** A rejected a candidate at `validation-rules.md`:122 because
   `common-extensions` does not document `IsExistByUnique` — true of the skill
   body (zero hits across SKILL.md and all 10 reference files). I then checked the
   corpus: `IsExistByUnique` is declared in
   `Facades/Common/Extensions/RepositoryBaseExtentions.cs` in **all six**
   projects — a `Common/Extensions/` file `common-extensions` simply has not
   documented. A's rejection stands either way; the gap is banked in §8.

2. **Author B's P2 rejected on fact, not on sprinkling.** B proposed a second
   `module-feature` pointer asserting *"The predicate extension the validator
   calls **is** `ValidationExtension`"*. I flagged the definite article as
   over-narrowing; the arbiter verified and went further — at that anchor
   (`module-feature/SKILL.md`:233) "a predicate extension on the repository
   abstraction" is the module's **own** `<X>Validation.cs`
   (`validation-rules.md`:33 table row "Extension method? yes — on
   `IRepositoryWrapper`", listing at :48). The pointer would have handed
   module-feature's own canonical file to a sibling. Loosening the wording could
   not save it. Ruling upheld; B's own tiebreak ("keep P1") was correct.

3. **Shared blind spot the convergence hid.** Both authors accepted four of five
   sites, which is exactly the pattern this repo has been burned by. The arbiter's
   convergence audit found the one they both missed from opposite directions: the
   `dotnet-testing` host section teaches an `HttpMessageHandler` seam this
   house's outbound code does not use. A accepted the site for the wrong reason
   (and called it its weakest); B rejected it for a reason that reads the
   `Not for:` as an ownership assignment, which the settled principle explicitly
   forbids.

4. **Arbiter's self-declared additions — all verified by me against shipped
   files, none taken on trust:**
   - *Site 5's catch-not-throw fact* (the addition the arbiter flagged as most
     worth checking) — confirmed verbatim at `http-client-factory/SKILL.md`:56-60,
     §"3. The sender returns; it does not throw".
   - *`.UseClient(httpClient)` for typed clients* — confirmed at :173, :194, :397,
     :474.
   - *"the bullet list has six items, not four"* — confirmed (Find, ProjectTo,
     ApplyFilter, ApplySearch, ApplySort, ToPagedListAsync).
   - *Sites 2 and 3's grafted repair clauses* — A's substance on B's file names;
     both file names confirmed exact against
     `list-query-pipeline/SKILL.md`:31-32.
   - `IHttpClientSender`, `HttpResult`, `QueryContainer`, `QueryExpressionExtension`,
     `PaginationExtension`, `ValidatorExtension` — every token used in an inserted
     line appears in the target skill's shipped body.

5. **Modality diffed both directions.** No inserted sentence uses
   must/never/always; each opens on a conditional ("Where…", "Go there when…").
   Nothing converts "not chosen" into "banned" — P5 in particular does **not**
   say the `HttpMessageHandler` stub is wrong, only that the house's seam differs.
   No permission drifted into obligation.

6. **`S3FilePath` deliberately not introduced** into `automapper-mapping`, whose
   body runs entirely on neutral placeholders. Author A raised it as a question
   and recommended against; the arbiter concurred; I concur. The pointer names the
   owning skill, which is enough to find the token.

---

## 4. Rejected candidates

| Site | Why it fails the bar |
|---|---|
| `module-feature/SKILL.md`:237 (B-P2) | Misattributes module-feature's own `<X>Validation.cs` to a sibling — see §3.2. |
| `validation-rules.md` review-checklist bullet (~448) | A checklist recaps rules already stated 55 lines above; a pointer there is duplication, not routing. |
| `validation-rules.md`:122 (`IsExistByIds`, `IsExistByUnique`) | `common-extensions` does not document `IsExistByUnique`. Refused on provenance. |
| `module-feature/references/service-growth.md`:81 | The `ApplyFilter…ToPagedListAsync` chain is incidental scenery for a section about splitting a service into partial parts. |
| `request-response-dtos.md` §"Pagination" (~363) | Same boundary as P2, same file. The reader there is choosing between two shipped overloads — a call site, explicitly *not* `list-query-pipeline`'s. |
| api-surface, any file-field or pre-signed-URL site | Searched all 14 in-scope skills for `IFormFile`/`S3`/bucket/pre-signed: **no such site exists in any api-surface body.** Nothing to point from. |
| `api-surface/references/endpoint-anatomy.md`:238 (`[FromForm]`) | Closest miss. The reader is inside an anti-example's line-by-line defect diagnosis about binding source, not deciding where an upload goes. |
| `facade-module-architecture/references/facades.md`:108-119 | `HttpClients/` is an illustration of folder shape; `http-client-factory`'s own `Not for:` assigns "client file placement" **to** this host. A pointer would invert the boundary. |
| `facade-module-architecture/references/composition-root.md`:145 | `.AddS3AwsFileStorage()` / `.AddHttpClientSender()` are two lines in a fifteen-line chain whose point is "every line is a call into a facade". Singling out two misreads the section. |
| `dotnet-testing/SKILL.md`:249 Decision-Guide row | Same boundary as P5; doubling a pointer inside one skill is the sprinkle the discipline forbids. |
| `elasticsearch-search` scroll/export (SKILL.md:281, usage-patterns.md:256) | "Export" there means draining a scroll into a list, not producing a workbook. A keyword collision, not a boundary. |
| `auth-and-security`, `distributed-caching`, `distributed-lock`, `error-handling`, `message-keys`, `mediatr-messaging`, `claude-md-builder` | Swept; no body site puts a reader in front of any of the five capabilities. |

### `excel-miniexcel` — deliberately zero pointers

Judged, not skipped. I grepped `excel|xlsx|MiniExcel|workbook` across all 14
in-scope skill bodies; both authors independently repeated it; the arbiter
repeated it a third time. **Zero body occurrences.** The token appears only in two
`Not for:` description entries (`api-surface`, `module-feature`). No pre-existing
body puts a reader in front of a workbook, so there is no site to hang a pointer
on, and manufacturing one would be exactly the sprinkling the brief forbids.
**Coverage is not the goal — none added.**

---

## 5. Directional note (the one judgment call worth your veto)

P4 (`automapper-mapping` → `file-storage`) is the only pointer whose boundary is
drawn by the **target's** `Not for:` rather than the host's —
`automapper-mapping`'s own description names none of the five new siblings. The
arbiter ruled a shipped reciprocal `Not for:` is still a shipped `Not for:`, and I
accepted: the host leaves a boolean argument unexplained on the page, and the
reader writing that map next needs the persist-the-key/serve-the-URL rule, which
is not mapping mechanics. **If you want host-side boundaries only, drop P4** —
it is a clean single-paragraph revert — and bank an `automapper-mapping`
description edit instead. I recommend keeping it.

P5's host (`dotnet-testing`) likewise names no HTTP sibling in its own
description; its boundary comes from `http-client-factory`'s "faking the sender —
dotnet-testing", which disclaims the *technique* while owning the *contract* the
pointer names. Same ruling, same veto available.

---

## 6. Final line counts

| File | Before | After | Bar |
|---|---|---|---|
| `module-feature/references/validation-rules.md` | 449 | **453** | references/ — no bar |
| `api-surface/references/request-response-dtos.md` | 405 | **410** | references/ — no bar |
| `ef-core-data-access/references/query-conventions.md` | 109 | **114** | references/ — no bar |
| `automapper-mapping/SKILL.md` | 454 | **458** | <500 ✓ (42 to spare) |
| `dotnet-testing/references/unit-testing.md` | 268 | **274** | references/ — no bar |
| `ef-core-data-access/SKILL.md` | 499 | **499** | untouched, as instructed |

`git status` shows exactly these five files modified, nothing else.

---

## 7. Proposed CHANGELOG fragment (main session renumbers)

```markdown
## [0.3.5X] — 2026-07-31

### Changed — activation surface, part A: body pointers into the five new skills

The five skills shipped at 0.3.48–0.3.52 had router rows and reciprocal
`Not for:` entries, but those fire only at skill-SELECTION time. A session
already inside `module-feature` writing a validator, or inside `api-surface`
writing a list endpoint, was never told they exist. Body-pointer coverage was
1 of 5; it is now 4 of 5.

- `module-feature/references/validation-rules.md` — "The facade's rule helpers
  come first" now names `common-extensions` as the owner of
  `ValidatorExtension.cs`, the reuse → promote → inline ladder, and the
  recreate-from-canon path for a project that has no such file to open.
- `api-surface/references/request-response-dtos.md` — "Search requests" now
  separates the published contract from the code behind it: `QueryContainer`,
  the `filter.` model binder and the operator set are `list-query-pipeline`'s.
- `ef-core-data-access/references/query-conventions.md` — the
  ApplyFilter/ApplySearch/ApplySort/ToPagedListAsync chain now says the call
  sites are this skill's and `QueryExpressionExtension`/`PaginationExtension`
  are `list-query-pipeline`'s, including the port-and-repair case. Placed in
  `references/` because SKILL.md is at the 499-line bar.
- `automapper-mapping/SKILL.md` — the "constructing a wrapper" shape now routes
  the storage-key type, its JSON converter and its unexplained boolean argument
  to `file-storage`. This closes the reciprocal banked in the file-storage
  coordinator report.
- `dotnet-testing/references/unit-testing.md` — "An outbound third-party
  dependency" now names the house's actual seam: substitute `IHttpClientSender`
  and return the `HttpResult`, which the sender returns rather than throws even
  on transport failure — the behaviour a faithful double must reproduce.

**`excel-miniexcel` gets no body pointer, by ruling.** No pre-existing skill body
mentions Excel, xlsx or workbooks outside two `Not for:` entries, so there is no
site where a reader is already doing what it owns. Coverage is not the goal.

**Rejected, recorded:** a second `module-feature` pointer (it would have
misattributed `module-feature`'s own `<X>Validation.cs` to `common-extensions`);
the `facade-module-architecture` folder-shape and composition-chain sites (the
reader there is designing a facade, not making a call); the elasticsearch scroll
sites (a keyword collision on "export"); a `validation-rules.md` pointer naming
`IsExistByUnique` (`common-extensions` does not document that symbol); eight
others.
```

---

## 8. Parked / open items

1. **`common-extensions` has an undocumented file.** `IsExistByUnique` lives in
   `Facades/Common/Extensions/RepositoryBaseExtentions.cs` in all six corpus
   projects, and `module-feature/references/validation-rules.md`:122 already cites
   it as "the facade's own helper" — but `common-extensions` documents neither the
   symbol nor the file. That is a gap in a new skill, so it is outside this work
   package (I may not edit the five). It cost one otherwise-good pointer site.
   **Recommend a follow-up to add `RepositoryBaseExtentions` to
   `common-extensions/references/`**, after which the `validation-rules.md`:122
   site becomes pointable.
2. **Reciprocal description edits, banked not made** (descriptions are out of
   scope for this package): `automapper-mapping`'s `Not for:` names no new
   sibling and could gain "S3 keys, pre-signed URLs — file-storage";
   `dotnet-testing`'s could gain "the sender's contract, `HttpResult` —
   http-client-factory". Neither is required for the pointers to stand.
3. **R8 candidate, not acted on — the user's call alone.** The arbiter observed
   that `dotnet-testing/references/unit-testing.md`:248-250's
   `HttpMessageHandler`-stub advice describes a seam no call site in this house
   uses. It is not wrong in general, and P5 supplements rather than contradicts
   it. Narrowing or labelling that sentence belongs to a `dotnet-testing`-owning
   session and to the user.
4. **Nothing refused.** Every part of the mandate was executed.
