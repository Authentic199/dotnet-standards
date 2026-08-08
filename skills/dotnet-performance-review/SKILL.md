---
name: dotnet-performance-review
description: >-
  This skill should be used when reviewing .NET or C# code for performance: a
  performance review, a slow endpoint or query, N+1 and round-trip counts,
  unbounded page size, missing index coverage, blocking or sync-over-async,
  cache TTL and staleness, lock hold time and contention, or Elasticsearch
  scroll, refresh and terminateAfter cost. Not for: blast radius, severity,
  slop — dotnet-code-review; layering, dependency direction —
  dotnet-architecture-review; secrets, injection, data exposure —
  dotnet-security-review; the query, cache, lock and search rules themselves —
  ef-core-data-access, distributed-caching, distributed-lock,
  elasticsearch-search; pagination contracts — api-surface; synchronous
  validator predicates — module-feature; permission cache internals —
  auth-and-security; the review process —
  superpowers:requesting-code-review, superpowers:receiving-code-review.
---
## Overview

This is a **rubric, not a pipeline.** It says what to check when the question is *what this
code costs per call and what happens to that cost under load*, in what order, and how to rank
what it finds. It does not run the review — that belongs to `superpowers:requesting-code-review`
and `superpowers:receiving-code-review` — and it does not apply the fixes: adding a projection,
an index or a guard is an ordinary change, made normally.

**This is static inspection, not a measurement.** Every check reads the *shape* of the code —
round trips per request, what a lock holds, what a cache promises, what bounds a result set —
and predicts cost from it. Nothing here profiles, benchmarks, samples or observes production,
so **no finding may carry a number it did not read out of the code**. *"This is roughly 40%
slower"* is refused outright, not softened; *"this endpoint issues one query per row of the
page, so the round-trip count scales with `PageSize`"* is the same finding, honestly stated. A
rubric that guesses magnitudes is worse than one that names shapes, because the guess is the
part the reader repeats.

**It checks conformance; it does not define it.** `ef-core-data-access` defines query shape,
`distributed-caching` cache policy, `distributed-lock` what belongs inside a lock,
`elasticsearch-search` search cost, `api-surface` the pagination contract. Lines quoted here
are the target compared against, never a second statement of the rule (`dotnet-code-review`
Principle 5, *The rubric cites the owning skill; it never re-teaches it*). So **a check must
trace to a shipped skill's body, or be a defect in any codebase** — one that is neither is
refused out loud, by number and name, and `references/performance-checks.md` carries the
*Refused — and why* table so the boundary reads as drawn rather than forgotten. Performance is
the subject where invented rules sound most expert, and where a review can burn an afternoon
on a constant factor nobody would have noticed.

**Generic performance advice outranks nothing here, and the two disagree often.** Optimization
guides demand a measurement before every finding and a benchmark harness beside every claim,
and prescribe `HybridCache`, compiled queries, `TimeProvider`, `ValueTask`, `sealed`,
`Span<T>`/pooling and injecting `DbContext` directly. **None of these is house doctrine**,
several contradict shipped rulings, and none may appear as a finding. Raising settled house
design as a defect is worse than a gap in the report: the author learns the whole document can
be ignored. Read *Not a finding* in each area before grading, and say what you suppressed.

**Every check is a manual instruction.** No profiler, no benchmark harness, no code-analysis
server, no query plan: each check is a `grep -rn --include=*.cs`, a file to open and read
against a shipped rule, or a build whose diagnostics you read. Where other material says
*"find the hot path"*, this rubric cannot — it can only find the *shape* that is expensive
wherever it runs. Paths assume the standard layout (`src/Core/`, `src/Infrastructure/`,
`src/Web/`, `tests/`); **resolve the real roots from the `.sln` once, before the first grep**,
because a path that does not exist returns nothing and an empty result reads exactly like a
fast, clean pass.

**Two modes, and say which** — diff or sweep, scoped exactly as `dotnet-architecture-review`,
*Two modes, and say which*. Diff is the default; a pre-existing cost in a touched file is INFO
unless the change makes it hotter, wider or reachable from a new route. One addition specific
to cost: **a shape whose cost scales with data is not pre-existing when the data grew** — a
query that was fine at a thousand rows and is now on a table of millions scores normally in
either mode, and the finding says which of the two changed.

**Scope: cost, not correctness.** Reach for this rubric when the change *is* the cost surface
— a new query or search endpoint, a list or export path, a cache or lock added or widened, an
index or entity configuration touched — or when a slow endpoint, a load-test result or an
inherited codebase needs a sweep. `dotnet-code-review` is the breadth pass and routes here
explicitly: its `references/review-rubric.md` rules that *"anything whose finding is 'this is
slow' — N+1, projection cost, index coverage, allocation, round-trip counts — belongs to
`dotnet-performance-review`; note the suspicion, do not grade it."* **This is where those get
graded**, and the only place they do. `dotnet-security-review` also routes rate-limiting and
DoS questions here; what this rubric can and cannot say about them is settled in *Refused —
and why*, not silently dropped.

Checks are numbered **per area and never reused**; `references/performance-checks.md` continues
each area's numbering where this body stops, so `1.9` means one thing in this skill and
nowhere else. Patterns assume `grep -rn --include=*.cs` unless stated.

## Core Principles

1. **Every report opens with the honesty rule, verbatim, in these words:** *This is
   static inspection of code shape, not a measurement. It predicts cost from structure —
   round trips, hold time, unbounded sets — and does not profile, benchmark or observe
   production. It finds shapes that are expensive wherever they run; it cannot tell you
   which of them your traffic actually reaches.* A performance report that does not bound
   itself is read as a profiling result, and the first number in it becomes the number
   everyone quotes. The sentence is the finding the report cannot make, which is why it
   is quoted rather than summarised.

2. **Count round trips and waits; ignore instructions.** In a service whose work is a
   database, a cache, a lock and a search cluster, cost is dominated by how many times a
   request leaves the process and how long each caller waits — not by how many objects a
   method allocates. So the areas run in that order, and a finding that cannot say *"this adds
   N round trips"*, *"this holds the lock for the length of X"* or *"this transfers M rows to
   discard most of them"* is usually a micro-optimization wearing a review's clothes.
   Allocation is last for the same reason, and is mostly refused: no shipped body legislates it.

3. **Growth is the finding; today's magnitude is not.** An N+1 over three rows and an N+1 over
   three million are the same defect, because the row count is data and the shape is code.
   State what the cost is a function of — rows returned, page size, concurrent callers,
   retries, documents indexed — and the reader can decide the magnitude for their own data
   without either of you inventing a number. Name the path the shape sits on when you know it;
   not knowing it lowers the rung and is stated, it does not block the finding.

4. **A known cost is a shape to report, not a defect to grade.** Several expensive shapes here
   are shipped, deliberate and documented by their owning skill: the canonical search chain
   costs five round trips, not two (`ef-core-data-access`, *Know the cost*); the pagination
   contract's default page size is unbounded on purpose (`api-surface`); the four Redis
   connection values are *policy, not tuning knobs* (`distributed-caching`). Report these as a
   shape with its cost named and its owner cited — never as a violation. Grading house design
   as a defect spends the reader's trust on the one finding that was never in doubt.

5. **A suppression is content, not politeness.** The *Not a finding* blocks bind as hard as
   the checks. FluentValidation predicates using the repository's **synchronous** reads are
   doctrine, not sync-over-async (`module-feature`, `references/validation-rules.md`,
   *Predicates are synchronous*); the connection values above are not tuning targets;
   `RetryTime` is not one either. Report one of these and every real finding in the same
   report is now read as possibly-noise. Say what you suppressed, in the report, every time.

6. **Grade once.** Several shapes this rubric deepens are already graded by
   `dotnet-code-review` — *Routing* names the full set by number and is the authority, so
   this principle does not duplicate the list. Cite each by number and name and add only the
   cost the breadth pass does not carry. This rubric re-grades nothing and renumbers nothing:
   one shape, one severity, in whichever report the reader opens.

## Severity calibration

The four words and their general meanings are `dotnet-code-review`'s — Principle 3, *One
severity vocabulary, four words*, and its *Severity ladder*. This rubric does not restate
them; it calibrates them, because performance findings have no natural ceiling — everything is
faster if you work at it — and left uncalibrated they either all argue their way to CRITICAL
while the service stays up, or all deflate into "could be optimized". The ladder's HIGH rung
already carries this rubric's load-bearing clause: **"fails predictably under load or on a
second request."** That sentence is the default rung here, and the table below says what
moves off it.

| Severity | In a performance finding |
|---|---|
| **CRITICAL** | The shape takes the process or a shared dependency **down**, rather than making it slow: sync-over-async on a request path exhausting the thread pool (3.2, universal), an unbounded fetch that can exhaust process memory (universal), a transaction opened *before* a lock is acquired — which holds a connection for the full `WaitTime` for every caller queued on that key and presents as a database outage (`distributed-lock`, *Lock outside, transaction inside*). The test is not how slow it is but whether the failure mode is *stopped* rather than *sluggish*. Cost alone is never CRITICAL. |
| **HIGH** | Fails predictably under load or on a second request — the ladder's own words. Cost that **grows** with something the code does not bound: a per-row round trip, a filter with no index behind it on a table that grows, a lock lengthened by work that need not be inside it, a page size the endpoint does not cap. |
| **MEDIUM** | Bounded waste, or an availability cost with no correctness consequence: a constant number of extra round trips, a dead `Include` in front of a projection, a cache whose keys diverge so it always misses, a cost real only at a scale this endpoint does not see. **Say plainly what it is** — a cost, not corruption and not an exposure (the phrasing `dotnet-security-review` check 2.6 uses for the same move). |
| **INFO** | A shipped shape's known cost recorded so the next reader does not re-raise it, a hot-path suspicion with nothing to compare it against, a question no shipped body settles — every allocation question lands here — a pre-existing cost noticed in diff mode, or a cost decision made well. |

Five calibrations settle the arguments this rubric actually gets:

- **HIGH is the home rung, not the consolation.** The ladder's HIGH already describes a
  performance defect; CRITICAL is the exception that has to earn itself. If a report's
  findings are mostly CRITICAL, the calibration failed, not the code.
- **A finding that needs a precondition is HIGH, not CRITICAL.** Needs load, needs
  concurrency, needs a large table, needs a particular page size, needs a second defect —
  the same precondition test `dotnet-security-review` applies. Reserve CRITICAL for what
  is already happening on today's traffic, and state the precondition in the finding.
- **Degrading to correct-but-slower de-escalates, and the finding says so.** The cache is
  never the source of truth, so a miss, a divergent key or an unevicted entry costs latency
  and load, not answers: drop one rung and name the fallback. Silence here reads as
  corruption and gets the finding fixed at the wrong priority — or quietly re-escalated by
  the next reviewer.
- **A shape `dotnet-code-review` already grades keeps its grade.** Cite the number and the
  name, add the cost, and do not restate the severity — a second number for one shape is
  how a reader ends up with two reports that disagree, and two tracker items for one fix.
- **Severity is consequence, not effort** — `dotnet-code-review`'s rule, unchanged, with
  one performance-specific corollary: **and never a benchmark number**. "20% faster" is not
  a severity argument, it is a measurement this rubric did not take. A one-line `.Select`
  that removes a per-row query stays HIGH; a quarter-long rewrite that would shave a
  bounded cost is MEDIUM. A style or micro-optimization preference — `sealed`, `ValueTask`,
  a compiled query, a `Span<T>` rewrite — is never a finding at any rung.

## The areas, in order

Five areas. **Run them in order and report coverage.** The order is by where the money
is: a round trip to another process costs more than everything area 5 measures, and
allocation — the thing generic material starts with — is last and mostly refused, because
no shipped body legislates it. A HIGH in area 1 never excuses skipping area 4; a fast
query behind a lock nobody can enter is still a slow endpoint.

| # | Area | Unit | Answers |
|---|---|---|---|
| 1 | Query shape and round trips | each read path | How many times does this request leave the process, and what bounds each one? |
| 2 | Blocking and async cost | each call site on a request path | Does anything here hold a thread instead of awaiting it? |
| 3 | Cache and staleness cost | each cache read, write and key | Is the cache paying for itself, or paying twice? |
| 4 | Lock and contention | each `LockedAsync` delegate | What does one caller make every other caller wait for? |
| 5 | Search-infrastructure cost | each search and index call | What does the cluster do that nobody asked for? |

**Scoping a partial run** — a new or changed read path, areas 1 and 2; a cache or lock added
or widened, 3 and 4; a search or indexing change, 5 and 2; a slow endpoint, an inherited
codebase or a load-test result, all five in order. Whatever ran, *Area coverage* says so.

**One severity rule runs across every area.** Cost that grows with something the code does not
bound is HIGH; the same shape where the code itself bounds the input — a catalogue table, a
fixed-length loop — is MEDIUM. The row count is data; the bound is code.

### 1 — Query shape and round trips

The database is the most expensive thing this service talks to and the easiest to talk to
too often. `ef-core-data-access` owns both read shapes and `api-surface` the request
contract that decides how much a caller may ask for. This area counts round trips and
rows; whether the query is *correct* is `dotnet-code-review` section 1.

| # | Check | Severity |
|---|---|---|
| 1.1 | **A query executed once per row.** `Find:` `grep -rn -A8 "foreach (" src/Infrastructure/Modules/` and read each block for a repository call — `Repository<`, `Find(`, `GetByIdAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `CountAsync`; also read every `.Select(` whose lambda is `async`, and every mapping profile the projection reaches for a `MapFrom` that calls back into a repository. One round trip becomes one per row, so latency tracks `PageSize` and the endpoint degrades exactly when it is most used. The fix is one composed query — the house shape projects with `ProjectTo` and lets the projection generate its joins — not a cache in front of the loop, which moves the cost rather than removing it. **Where staging is in use**, the composed replacement also carries `IgnoreQueryFilters()` — the staging filter hides rows another session has already staged, so without it the single query is a cheaper way to be wrong. Name the multiplier: *"one round trip per returned row"* is the whole argument · `ef-core-data-access`, *Query conventions* + `excel-miniexcel`, *Checking the database once per row* + universal | **HIGH** |
| 1.2 | **An `Include` chain in front of a projecting read.** `Find:` `grep -rn -A4 "\.Include(" src/Infrastructure/Modules/`, then look for `.ProjectTo<` in the following lines. Already graded as dead code — cite it. The cost half it does not carry: the `Include` changes the SQL. The projection would have generated exactly the joins the response needs, and the chain in front of it drags whole child collections across the wire to be discarded. Invisible on a get-single site; on a list endpoint it multiplies the payload by the child count · `dotnet-code-review` check 1.3, *An `Include` chain in front of a projecting read* | **graded by `dotnet-code-review` 1.3** |
| 1.3 | **A list endpoint with no page-size ceiling of its own.** `Find:` open every request type reaching a paged search and read its `PageSize` handling; `grep -rn "PageSize" src/` for any bound at all. The contract's default is `int.MaxValue / 2` — a caller that sends nothing gets everything — and the owning skill states the unbounded default *"is deliberate but sharp: an endpoint that must never return everything needs its own guard, because the contract will not supply one."* **So this is never a finding against the contract; it is a finding against this endpoint** for not carrying the guard it was told to carry. Cost is unbounded in rows, in projection work and in response bytes, and one caller sending no page size is enough — which is also the one house-grounded answer to a rate-limiting or DoS question routed here from `dotnet-security-review`. A present `QueryContainer` validator is **not** that guard: it rejects `0` and anything strictly above the `int.MaxValue / 2` default and permits the default itself, so it caps nothing this check is about · `api-surface`, `references/request-response-dtos.md`, *The search contract* + `list-query-pipeline`, *`QueryContainer` validates its own paging* | **HIGH; MEDIUM where the table is bounded by design** |
| 1.4 | **A terminal call made too early.** `Find:` two greps. `grep -rn -A5 "ToListAsync(" src/Infrastructure/Modules/` and read what follows each result for `.Where(`, `.OrderBy(`, `.Skip(`, `.Take(`, `.Count(`; and `grep -rn "ToPagedList(" src/` — no `Async` — for the overload trap. Both read shapes *"start at `Find`, project immediately, and never materialize an entity"*, and `ToPagedListAsync` goes **last** *"so paging applies to the filtered, sorted result."* A predicate applied after the list arrived ran on every row the database already sent: the transfer is paid in full and the page size bounds nothing. The overload is the same defect by a different route — `ToPagedListAsync` on `IQueryable` pages in the database, while `ToPagedList` on `IEnumerable` *"enumerates and counts the whole sequence."* Both compile and both return the same shape, so neither is visible in the response · `ef-core-data-access`, `references/query-conventions.md` + `api-surface`, `references/request-response-dtos.md`, *Pagination* + `list-query-pipeline`, *Paging after materialising* | **HIGH** |
| 1.5 | **A filter, search or sort field with no index behind it.** `Find:` read each endpoint's `SearchFields` and filterable properties, then open the entity's `IEntityTypeConfiguration` and check for a `HasIndex`, a composite `HasIndex(x => new { … })` or a `HasCitextUnique` covering them. An endpoint naming no `SearchFields` does not search nothing — the set is then derived by reflection over the element type's `string` properties to one navigation level, minus `[JsonIgnore]`, `[NotSearchable]` and `[NotMapped]`, so read the element type rather than the request. Every filter, keyword and sort field is client-supplied, so the caller chooses the query plan: an unindexed column turns a paged read into a scan at whatever size the table has reached, and the code never changes — which is why it reads as fine in review and fails a year later. `HasCitextUnique` sets the column type and its index together, so a `citext` column declared with `HasColumnType` alone is searchable and unindexed. Report the column and the query that reaches it, and **name the index you want**; whether a composite index is ordered one way or the other is the owning skill's call, not this rubric's · universal, with the declaration mechanism from `ef-core-data-access`, `references/entity-configuration.md` and the searched-field set from `list-query-pipeline`, *Keyword search and the default field set* | **HIGH; MEDIUM where the table is bounded by design** |
| 1.6 | **A count or existence question answered by fetching rows.** `Find:` `grep -rnE "ToListAsync\(\)\.(Count\|Any)\|\.ToList\(\)\.(Count\|Any)" src/` and, more widely, read every `ToListAsync` whose result is only measured. The rows cross the wire, are materialized, and are thrown away to produce one integer or one boolean; `CountAsync`/`AnyAsync` answer at the database. Bounded waste — real, but it does not grow a round trip · universal | **MEDIUM** |

**Two shapes to report, not to grade.** The **canonical search chain costs five round
trips, not two** — `ApplyFilter`, `ApplySearch` and `ApplySort` *"each open by evaluating
`entities.Any()`"* whether or not the client supplied anything, so the total is *"three
probes, the page, and the count"*, and the owning skill says *"Budget for that on hot list
endpoints."* On a hot endpoint record the five with its citation as INFO, note that three
are unconditional probes, and stop: it is not a defect and there is no local fix. And the
**`int.MaxValue / 2` default** is the contract working as designed — 1.3 grades the missing
guard, never the default.

*Continued in `references/performance-checks.md` at 1.7:* tracking left undeclared on a read
path (graded by `dotnet-code-review` 1.2); an aggregate answered by a second query instead of
the page's companion payload; a projection carrying an aggregate over a collection
navigation; a repository mutation reached once per row; a seeding path built from per-row
reads.

### 2 — Blocking and async cost

Everything here fails the same way: a thread is held instead of released, and under
concurrency the whole application slows while each individual call looks fine.
`dotnet-code-review` section 3 owns two of these shapes and they are cited, never
re-graded; the two this rubric owns are the search capability's synchronous surface and
the synchronous read outside a validator.

| # | Check | Severity |
|---|---|---|
| 2.1 | **A synchronous search or bulk member on a request path.** `Find:` `grep -rn "\.Search(\|\.BulkAll(" src/` and read each hit's path back to a controller or handler — the tell is a repository member with no `Async` suffix. `Search(…, out …)` calls the synchronous client and loops synchronously when given a scroll time; `BulkAll` *"subscribes to an observable and waits on a `CountdownEvent` until the entire bulk finishes."* Each blocked call holds a thread-pool thread for a full round trip, and *"thread-pool starvation does not present as a search problem — it presents as the whole application slowing down."* `Search` also *"takes no `CancellationToken` at all, so a client that walks away cannot stop it."* The owning skill ships the replacement end to end, including the batch loop that replaces the observable — cite it rather than restating it. Both members are omitted from the scaffold, so a hit means an older surface · `elasticsearch-search`, `references/usage-patterns.md`, *Anti-pattern: the blocking pair* | **HIGH; CRITICAL on a request path** |
| 2.2 | **Sync-over-async.** `Find:` `grep -rn "\.Result\b\|\.Wait()\|GetAwaiter()\.GetResult()" src/`. Already graded, universal — cite it. The cost half it does not carry is the failure mode: this is the one shape in the rubric whose consequence is *stopped* rather than *sluggish*, which is why the CRITICAL rung exists at all · `dotnet-code-review` check 3.2, *Sync-over-async* | **graded by `dotnet-code-review` 3.2** |
| 2.3 | **A dropped `CancellationToken` on a multi-round-trip path.** `Find:` `grep -rnE "(SaveChangesAsync\|ToListAsync\|FirstOrDefaultAsync\|AnyAsync\|CountAsync)\(\)" src/` — the empty parentheses are the tell — then read each async signature that accepts a token and does not pass it on. Already graded — cite it. The cost half is arithmetic this rubric can actually count: on the canonical search chain an abandoned request still costs five round trips, not one, and on a scroll or bulk loop it costs the whole drain, holding a pooled connection for a response nobody will read. Say how many round trips survive the disconnect. Where an endpoint is both slow and cancellable this is the cheapest fix in the report; say that too · `dotnet-code-review` check 3.1, *A dropped `CancellationToken`* | **graded by `dotnet-code-review` 3.1** |
| 2.4 | **A synchronous repository read outside a validator predicate.** `Find:` `grep -rnE "\.(Any\|Count\|First\|FirstOrDefault\|Single)\(" src/Infrastructure/Modules/` and **discard every hit inside a `<X>Validation.cs`** — those are doctrine, below. What remains is a synchronous database call on an async path: not sync-over-async (nothing is blocked on a `Task`, so 2.2 does not reach it), but a full round trip taken on the caller's thread, once per iteration when it sits in a loop or a seeder. Count these into the endpoint's area-1 total as well as reporting the block. The fix is the `Async` member and the token · universal, boundary from `module-feature`, `references/validation-rules.md`, *Predicates are synchronous* | **MEDIUM; HIGH inside a loop or on a request path** |

**Not a finding here.** A **FluentValidation `.Must(...)` predicate using the repository's
synchronous reads** — `Any`, `Count`, `GetById`, `Find(...).FirstOrDefault()` — is house
doctrine: *"A `.Must(...)` rule runs synchronously … there is no asynchronous operation to
cancel."* The same sentence forbids the workaround a reviewer is most likely to propose —
*"Do not reach for `.GetAwaiter().GetResult()` on an async repository call to fit this
shape."* **Report the round trips a validator makes** if a request runs several, as a count
under area 1 — never the synchronicity. Reporting a synchronous predicate is how a report
loses its reader for 2.2, which is real.

*Continued in `references/performance-checks.md` at 2.5:* `async void` (graded by
`dotnet-code-review` 3.3); an `async` method that never awaits; `Task.Run` used to escape a
synchronous API; independent awaits issued in sequence; a captive scoped dependency's cost
side (graded by 3.4); a process-wide client constructed per call (graded by 4.8 for an HTTP
client).

### 3 — Cache and staleness cost

A cache that is not hit costs strictly more than no cache: the read, the miss, and the
source-of-truth query. `distributed-caching` rules that *"Losing Redis loses nothing.
Every read degrades to the authoritative query — slower, not wrong"*, which is why almost
everything here is MEDIUM and why the failure worth hunting is the cache that runs, pays
a round trip, and never returns a hit.

| # | Check | Severity |
|---|---|---|
| 3.1 | **One entry, two key factories.** `Find:` `grep -rn "CacheKey<\|CachePrefix" src/` and check that every hit resolves to the facade's own public static member; a module-local key helper is the shape. Identical strings today is not the question — divergence tomorrow is. The day one factory gains a segment or a separator, *"producers and consumers silently stop meeting at the same key. The failure is not an error but a miss … The cache appears to work while being entirely useless, and nothing in the logs says so."* Every read then pays a Redis round trip **and** the source query. **Grade MEDIUM because it degrades to correct, and say that in the finding** — which is also why this is a review finding and never a monitoring one. Fix by deleting the local helper · `distributed-caching`, `references/usage-patterns.md`, *Key construction* | **MEDIUM** |
| 3.2 | **A read-once handoff read with `GetAsync`.** `Find:` `grep -rn "GetAsync<" src/Infrastructure/` and check each against the pattern its key belongs to. A handoff entry *"has exactly one legitimate reader. Removing on read frees the memory immediately instead of holding it for the rest of the TTL"* — so `GetAsync` leaves a superseded snapshot resident and servable for the rest of the window. `GetRemoveAsync` removes only when a value was found, so the fix costs nothing and is one method name · `distributed-caching`, `references/usage-patterns.md`, *Pattern A* | **MEDIUM** |
| 3.3 | **A TTL lengthened to improve the hit rate.** `Find:` in diff mode, read every changed expiration constant; in a sweep, read each TTL against the pattern it implements. **The TTL is the janitor, not the contract** — it *"only bounds the entries whose consumer never arrived. Do not lengthen it to 'improve the hit rate': that widens the window in which a superseded snapshot can be served."* A hit-rate argument is a performance argument, which is exactly why refusing it belongs to this rubric. A raised constant with no accompanying pattern change is the finding. The mirror image is a no-TTL entry whose three conditions do not all hold — one row, one writer, unconditional invalidation — where *"A stale no-TTL entry is served forever"* · `distributed-caching`, `references/usage-patterns.md`, *No TTL is earned, not assumed* | **MEDIUM** |
| 3.4 | **A static catalogue rebuilt on every access.** `Find:` `grep -rnE "=>\s*[A-Za-z_.]+\.(ToDictionary\|ToList\|ToArray)\(" src/` — an expression-bodied member is a method call, so each access builds a fresh collection over the whole catalogue. The shipped rule: the lookup *"must be `static readonly`, not a computed property. This runs on every authorized request, once per held code. A `=>` property here rebuilds the whole dictionary on each access — and this method touches it twice per code."* The shape generalizes, and the `=>` is the only visible difference from the correct form. Cost scales with request rate and catalogue size · `auth-and-security`, `references/permission-internals.md` §4, *Implied permissions* | **HIGH on a per-request path; MEDIUM elsewhere** |
| 3.5 | **A mutation that leaves the entry or the document stale.** `Find:` for each entity written in the diff, search the cache and search facades for it to see whether it is cached or indexed at all. Already graded — cite it. The cost half runs in the other direction from 4.7's correctness concern: an invalidation written **inside a loop** pays one Redis or cluster round trip per row, and one that removes a broad prefix instead of a key discards entries every other caller must rebuild. A TTL is not an invalidation strategy — waiting it out means every caller in the window pays the miss *and* the source query. Report the round-trip count and the eviction breadth; the staleness stays with 4.7 · `dotnet-code-review` check 4.7, *A mutation that leaves the cache or the index stale* | **graded by `dotnet-code-review` 4.7** |

**Three things that look like findings here and are not.** The **four connection values** —
`AbortOnConnectFail = false`, `ConnectTimeout`, `ConnectRetry`, `KeepAlive` — are *"policy,
not tuning knobs … Do not override them per call site"*, and the first is what makes a
cache outage a slowdown instead of an outage: do not propose tuning them, and do not grade
a cache-path defect as though the fallback did not exist. **A cache-aside entry with no
TTL** is earned, not forgotten, where one row has one writer that always invalidates
through one helper; the finding, if any, is a mutating path that bypasses the helper, and
that is 3.5's. And the **permission cache's in-process sliding expiry is deliberate** — *"a
hot per-request lookup with a bounded staleness window, not shared state … moving this
there would trade a bounded window for a network hop on every authorized request."*
Proposing Redis for it is a performance regression dressed as a convention fix. Its real
defect — a grant write path that evicts nothing — is a privilege-retention finding graded
by `dotnet-security-review` check 4.4. Route it; do not re-grade it here.

*Continued in `references/performance-checks.md` at 3.6:* a cache write inside a transaction
that has not committed; a fallback that can return something the cache would not have; a
large object graph stored as one entry; a cache in front of a read that fits neither shipped
pattern; a cache client reached outside the facade.

### 4 — Lock and contention

The only area where one caller's cost is paid by every other caller. `distributed-lock`
owns the delegate's contents, the options and the ordering rules; this area owns the wait.
**Every finding here names the key and who else queues on it.** This area ships no BAD/GOOD
pair — read the owning skill's patterns for the shape being compared against.

| # | Check | Severity |
|---|---|---|
| 4.1 | **A transaction opened before the lock is acquired.** `Find:` `grep -rn -B6 "LockedAsync" src/` and read for `BeginTransactionAsync` above the call, then `grep -rn -A12 "LockedAsync" src/` to confirm the transaction opens and commits **inside** the delegate. Doctrine is *"Acquire the lock first, open the transaction inside the delegate … Never the reverse."* A transaction opened before acquisition *"holds a database connection and its row locks for the entire wait — up to the full `WaitTime` — for every caller queued on that key. Under contention that turns lock queueing into connection-pool pressure, and the outage presents as a database problem rather than a lock-contention one."* The misdiagnosis is half the damage: the symptom points at the database and the cause is in the lock. **This is CRITICAL rather than a precondition case** — the queue is created by the lock's own existence, not by a condition outside the code; nobody locks an uncontended key. Report the second failure with it: two call sites taking the two locks in opposite orders *"deadlock across two systems, where neither one's deadlock detector can see the cycle"*. **Where the same call site also has the racing read-check-write, that half is `dotnet-code-review` 3.6, *A read-check-write with no lock, or work outside the lock*; report the pool consequence once, here** · `distributed-lock`, `references/usage-patterns.md`, *Lock outside, transaction inside* | **CRITICAL** |
| 4.2 | **Work inside the delegate that passes the interleaving test.** `Find:` `grep -rn "LockedAsync" src/Infrastructure/` and open every delegate. Per statement, ask the owning skill's question: *"if a second caller executed this same statement between mine and my next one, would my outcome still be correct?"* What would still be correct belongs outside, because *"every statement inside is time every other caller spends waiting."* Request validation, request mapping, response projection, compensating work and any external call that does not touch the guarded state belong outside — and the response projection is the instance that survives review, because it looks like part of the operation while adding a whole second query to the held region: the shipped shape has *"The delegate returns an id, not a response."* Name the statements to move and the wait they add for every queued caller — never that the lock is "too long" · `distributed-lock`, `references/usage-patterns.md`, *What belongs inside the delegate — the interleaving test* | **HIGH** |
| 4.3 | **`ExpiryTime` sized to the typical case.** `Find:` `grep -rn "ExpiryTime\|ConcurrencyHandlerOptions" src/` and compare each value against the **worst-case** duration of everything inside the delegate, including any outbound call's own timeout. *"Size `ExpiryTime` against the worst-case duration of the guarded work, not its typical duration."* When work outruns it *"the session expires, a second caller acquires legitimately, and two callers process one resource with no error anywhere."* If the honest worst case is uncomfortably large the fix is 4.2 — *"moving work out of the locked region, not shortening the expiry"* — and do not rely on lock renewal you have not verified in the shipped client version. **Say plainly what this is:** a duration decision with a correctness consequence, which is why the consequence itself routes to `distributed-lock` · `distributed-lock`, *Sizing the options* | **HIGH** |
| 4.4 | **`WaitTime` raised to reduce errors.** `Find:` `grep -rn "WaitTime" src/` and read the diff for an increase. Exhausting `WaitTime` throws, and **that is a feature**: it *"converts unbounded queueing into a fast, retryable answer."* *"A caller waiting a minute for a lock has already blown its own timeout, and the queue behind it keeps growing"* — so raising the value converts a visible fast failure into an invisible slow one. The finding is the raise; the fix is 4.2 or a queue · `distributed-lock`, *Sizing the options* | **MEDIUM** |
| 4.5 | **A lock path with no bound at all.** `Find:` `grep -rn "AddConcurrencyHandler\|SemaphoreAsync\|ConcurrencyProvider" src/` and read which provider the call sites actually resolve in each environment. **`SemaphoreAsync` takes no options object at all** — no `WaitTime`, no `ExpiryTime`, no `RetryTime` — *"a caller waits on the semaphore for as long as it takes, and this path therefore never throws `LockedException`. The same call site produces two different observable behaviours depending on the provider: a `423` under one, an unbounded queue under the other."* Name the provider the environment runs; the owning skill notes the in-memory provider is not the one production call sites select, so the finding is about a request path that actually resolves it. A call site that passes **no options object at all** is `dotnet-code-review` 3.7, *A lock call site relying on the provider default* — cite it there; 4.3, 4.4 and this check grade values that are passed and sized or bounded wrongly · `distributed-lock`, `references/implementation.md`, *The dispatch* | **HIGH where a request path resolves the semaphore provider** |

**Four things that look like findings here and are not.** A **lock spanning an outbound
call or a report** is ruled *"Correct, and it converts a resource invariant into a
throughput ceiling — every caller for that key now waits on someone else's network."*
Report the ceiling and the two questions the owning skill attaches — does `ExpiryTime`
exceed the call's worst-case timeout (4.3's legitimate finding), and does this operation
want a queue instead — but do not report the shape as a defect. **`RetryTime`** is *"the
polling interval inside that window … the default is not a tuning target."* The lock
factory's **eager connect at composition** is the deliberate opposite of the cache's
policy, because *"a cache has a correct degraded mode — slower but right — and a lock has
none"*; a per-request factory would be the finding. And the **semaphore registry's cleanup
race** is shipped knowingly — *"not presented as a defect to fix in passing"* — so where it
is raised at all it is `dotnet-code-review` check 3.10, *Registry cleanup interleaving*, and
this rubric adds nothing to it.

*Continued in `references/performance-checks.md` at 4.6:* a multi-key lock taken as nested
single-key calls (graded by `dotnet-code-review` 3.8); a key coarser than the resource it
guards; a key composed from a value read outside the delegate (graded by 3.6); a
`LockedException` caught and retried inside a module; the registry cleanup window's cost side
(graded by 3.10) — and, after them, the three shapes that look like contention and are not.

### 5 — Search-infrastructure cost

Cost paid on the cluster rather than in the process, which is why it survives a profiler
run against the API. `elasticsearch-search` owns the repository surface, the descriptor and
the connection policy; this area asks only what each call waits on.

| # | Check | Severity |
|---|---|---|
| 5.1 | **A scroll on a request path.** `Find:` `grep -rn "scrollTime\|GetPointInTimeAsync" src/` and read which path each hit sits on. `SearchAsync(scrollTime: "…")` *"drains a scroll into one list — correct for an export or an index rebuild, wrong for a request path"*: the caller waits for every batch and holds the whole result in memory, and both costs grow with the matching set, which is data. **Large result sets are a background-job concern** — route the fix rather than tuning it, or use `GetPointInTimeAsync`/`ClosePointInTimeAsync` where pagination must stay consistent across pages · `elasticsearch-search`, *Large result sets are a background-job concern* | **HIGH** |
| 5.2 | **Documents fetched to answer a question about documents.** `Find:` `grep -rn -A4 "SearchAsync(" src/` and look for a result used only as `.Count`, `.Any()`, `.FirstOrDefault()` or a null check. `CountAsync` returns *"a `long`, no documents transferred"* and `ExistsAsync` is *"cheaper than fetching and null-checking."* The materialized list also carries whatever size the descriptor names, so the transferred volume is set by a number nobody revisits. The same rule runs before a delete-by-query, where the owning skill already requires a count first whenever the predicate is not trivially exact. Area 1.6's shape, one process further out · `elasticsearch-search`, *The repository surface* | **MEDIUM** |
| 5.3 | **A refresh paid per document.** `Find:` `grep -rn -B6 "AddAsync\|AddRangeAsync\|UpsertRangeAsync\|UpdateRangeAsync" src/` and check whether a single-document write sits inside a loop. Single writes and `AddRangeAsync` complete with `Refresh.WaitFor`, so each call waits for the index to make its write searchable — affordable once, and the whole cost the default exists to avoid when it runs per row. `UpdateRangeAsync` and `UpsertRangeAsync` default to `Refresh.False` — *"that is what makes bulk indexing affordable."* The fix is a batch loop with an explicit chunk size, refreshing deliberately on the final batch only if something queries straight after · `elasticsearch-search`, *Refresh is a default, and sometimes the wrong one* | **HIGH; MEDIUM where the loop length is bounded by the code** |
| 5.4 | **A request-path lookup under the ten-minute ceiling.** `Find:` open the search facade's connection policy to confirm `RequestTimeout`, then list the request-path call sites reaching the cluster. `RequestTimeout` is `TimeSpan.FromMinutes(10)`, *"Sized for bulk indexing and reindexing … It applies to every request, so a single lookup against a hung cluster also holds its caller for ten minutes."* The connection values are policy and are not overridable per call site, so **there is no local fix and this is reported as a shape, not a defect**: name the call sites that inherit the ceiling and hand the question of a caller-side bound to whoever owns the endpoint's own timeout · `elasticsearch-search`, `references/implementation.md`, *Connection policy* | **INFO — report the shape** |

**Two shapes to report, not to grade.** **`terminateAfter`**: the default of `1` kept where
at most one document can match is the cheap, correct choice, and where a **sort decides
which document is right** doctrine already requires `terminateAfter: null` — the full
search that costs is the correct trade. Never report paying it as a cost finding, never
propose restoring the default for speed, and note that the shipped sentence permits keeping
the default without obliging it, so a correct `null` is not a finding either. **First
resolve of the client is not free**: it *"scans the Infrastructure assembly for
`IIndexSettingsMapper` implementations and calls each one — blocking cluster calls that
create or amend indices"*, so composition waits on the cluster. The owning skill states the
trade — *"That cost is the trade for never querying an index that does not exist"* — making
it a startup shape to record, not a finding.

*Continued in `references/performance-checks.md` at 5.5:* an index write inline with the
database write; a descriptor with no explicit size; a by-id fetch issued per id; a
read-modify-write loop where a query-scoped update belongs; startup work proportional to the
corpus.

## The report

If this review produces a report, write it to a file under `docs/code-review/`
in the reviewed repository (create the folder if absent) — the file, not the
chat copy, is the deliverable.

One report, the severity words as headings, always in this order. **Every section appears
every time**; write `None.` when a section is empty, because an absent section is ambiguous
between *checked, found nothing* and *did not check* — and in a cost review that ambiguity
is what lets an unexamined area read as a fast one.

```markdown
## Performance review: <scope>

> This is static inspection of code shape, not a measurement. It predicts cost from
> structure — round trips, hold time, unbounded sets — and does not profile, benchmark or
> observe production. It finds shapes that are expensive wherever they run; it cannot tell
> you which of them your traffic actually reaches.

### Summary
<mode (diff or sweep) · areas run · PASS / FAIL and the findings that decide it>

### CRITICAL
- **<title>** — `<file>:<line>` · check <n.n>
  <the shape> · <what the cost grows with> · <the path it sits on: request, job, startup> · <the change that bounds it> · <owning skill, or universal>

### HIGH
- **<title>** — `<file>:<line>` · check <n.n> …

### MEDIUM
- **<title>** — `<file>:<line>` · check <n.n> …

### INFO
- **<title>** — `<file>:<line>` · check <n.n> …

### Area coverage
1 Query shape and round trips · 2 Blocking and async cost · 3 Cache and staleness cost ·
4 Lock and contention · 5 Search-infrastructure cost
<ran / skipped and why, per area>

### Suppressions applied
<deliberate costs and house-doctrine shapes seen and not reported, one line each — or `None.`>

### What's Good
- <the cost decisions worth repeating>
```

Four rules for the findings themselves:

1. **The honesty rule is verbatim and it is not moved.** It sits above the Summary, before
   the first finding, in the words Core Principle 1 fixes. A performance report that buries
   it has published a measurement it did not take; one that paraphrases it has softened the
   only claim it is obliged to make.
2. **Name what the cost grows with, and the path it sits on.** "This is slow" is not a
   finding; "one round trip per returned row, so latency tracks `PageSize`, on the public
   list endpoint at `<file>:<line>`" is. The two clauses are separate on purpose: a reader
   who accepts the shape and disputes the path is arguing about severity, not about the
   defect — and **where the path is unknown, say so and drop a rung rather than dropping
   the finding.**
3. **Cite the check number and the name, or say `universal`.** The author then argues with
   the rule rather than with the reviewer, and a stale rule surfaces as a contradiction
   rather than as a second opinion. **A finding citing nothing is this rubric inventing
   doctrine under a performance banner**, the one thing it may not do. Where a shape is
   already graded by a sibling rubric, cite that check by number and name and **carry no
   second severity** — one shape, one grade, in whichever report the reader opens.
4. **No number the reviewer did not read out of the code.** Round trips, rows per
   iteration, batches in a loop, hold times, page bounds, seconds of a configured timeout:
   countable from the source, so count them. Percentages, milliseconds, allocation sizes
   and throughput figures: not countable from shape, so they do not appear — not in a
   finding, not in a severity argument, not as an illustration. A single invented one turns
   the whole report into a measurement it never was.

**`FAIL` is decided by CRITICAL and HIGH only** — the rule `dotnet-architecture-review`
uses. MEDIUM and INFO do not fail a verdict, or every report would FAIL and the verdict
would stop meaning anything. A MEDIUM-only report is `PASS`, and the Summary says how many
cost findings it carries; that is a real distinction and it needs no new word.

**Area coverage is not optional**, and neither is *Suppressions applied* when an area ran.
A review that ran only areas 1 and 2 is a useful report; one that ran only areas 1 and 2
and does not say so is a misleading one, because a reader takes a silent area for a cheap
one. And naming the shapes you deliberately did not report — the five round trips, the
unbounded `PageSize` default, the connection values, `RetryTime`, the lock that spans an
outbound call, `terminateAfter`, the ten-minute ceiling, first resolve — tells the reader
you opened those files and *decided*. Otherwise the next reviewer raises them, and the one
after that.

## Routing

**Deep dives — sibling rubrics.** This rubric owns cost. When the change's risk lives
elsewhere, load that one instead of stretching this.

| The change is mostly about | Load |
|---|---|
| Blast radius, severity of behavioural findings, slop; the shapes it already grades and this rubric only deepens — 1.2, 1.3, 3.1, 3.2, 3.3, 3.4, 3.6, 3.8, 3.10, 4.7 and 4.8 | `dotnet-code-review` |
| Layering, dependency direction, placement, the composition root's shape | `dotnet-architecture-review` |
| Secrets, injection, authorization gates, mass assignment, data exposure — including a grant write path that evicts nothing, check 4.4 | `dotnet-security-review` |

**Doctrine — the owning knowledge skill.** This rubric notices that a shape is expensive;
what the shape should be lives here.

| The finding is about | Owning skill |
|---|---|
| Query shape, tracking, projection, includes, pagination mechanics, transactions, entity configuration and its indexes, seeding | `ef-core-data-access` |
| The search and pagination request **contract** — `PageSize` defaults, response and `MoreInfo` shape | `api-surface` |
| The **stages** behind that contract — `ApplyFilter`/`ApplySearch`/`ApplySort`, the reflection-derived search-field set, `ToPagedList` vs `ToPagedListAsync`, `QueryContainer` | `list-query-pipeline` |
| Profiles, `CreateMap`, and what a projection-reachable map may contain | `automapper-mapping` |
| Cache keys, TTL, the two patterns, invalidation, connection policy, what may be cached at all | `distributed-caching` |
| Lock keys, the options, what belongs inside the delegate, provider choice | `distributed-lock` |
| The repository surface, descriptors, refresh, scroll, index writes, the connection policy | `elasticsearch-search` |
| Validator predicates and their synchronous reads, service and handler structure | `module-feature` |
| The permission catalogue and its lookup, the permission cache's internals | `auth-and-security` |
| Where an event handler lives and how it is dispatched — the destination for an index write moved off a request path | `mediatr-messaging` |
| Unsure which of the above owns it | `choosing-a-dotnet-skill` |

**Process.** Requesting the review and triaging what comes back belong to
`superpowers:requesting-code-review` and `superpowers:receiving-code-review`.
**Execution:** adding a projection, an index, a page-size guard or a batch member is an
ordinary code change, made normally; only the cleanup one leaves behind — the now-dead
`Include`, the unused helper — goes to `/simplify`.

## References

**Read `references/performance-checks.md` when** running a sweep, when a slow endpoint has
survived this body's pass, or when a finding needs the long tail behind an area. It
continues each area's numbering from where this body stops, with no number reused, so a
citation is unambiguous about which file it came from. It also carries the round-trip
comparison table the counting checks are run against, and closes with *Refused — and why*:
the real performance topics this rubric deliberately does not check, and where the reader
goes instead.

## Decision Guide

| Situation | Do this |
|---|---|
| Asked for "a performance review" with no scope | Declare the mode and the areas in the Summary; run all five in order unless the scope narrows them |
| Asked why an endpoint is slow | Say first that this rubric reads shape, not traffic. Then count that path's round trips (area 1) before reading a line for allocation or style |
| A finding feels CRITICAL | Check what bounds it. If a precondition — a page size, a second caller, a cold cache, a large table — is needed to make it hurt, it is HIGH |
| The cost is real but nothing degrades except speed | Drop a rung and name the fallback out loud. Slower-but-correct is an availability finding and says the words |
| A shape is expensive and shipped on purpose — the five round trips, the `PageSize` default, the connection values, the ten-minute ceiling, the lock that blocks composition | Report the shape with its citation and stop. It is a suppression, and *Suppressions applied* is where it goes |
| A shape `dotnet-code-review` already grades | Cite the check by number and name, add only the cost, and leave the severity to it |
| A generic optimization is the obvious answer — `HybridCache`, compiled queries, `TimeProvider`, sealing, `Span<T>`, pooling, `ValueTask`, `DbContext` directly | Not a finding here. See *Refused — and why*; none is house doctrine, several reverse a shipped decision, and a review is not where one becomes doctrine |
| The question is rate limiting or a DoS surface, arriving from `dotnet-security-review` | The one house-grounded bound is check 1.3, a per-endpoint page-size ceiling. Name it, and state the policy question as unowned rather than inventing an answer |
| The cost is in a background job or its scheduling | The job's body is ordinary code — run the five areas on it. The scheduling around it has no owning skill yet; say it is unowned |
| An allocation question, or any cost question no shipped body settles | INFO with the question stated. This rubric has no doctrine of its own, and the one shipped sentence about allocation forbids a change rather than describing a defect |
| The only remaining finding is style, naming or a missing comment | Say nothing — `dotnet-code-review` Principle 4, *Style is reviewed last, or not at all* |
| Everything passes | Say PASS, write `None.` into each empty section, keep the honesty rule, keep *Area coverage* and *Suppressions applied* honest, and name the cost decisions the code got right |
