# Performance checks — the long tail

The body carries each area's decisive checks — the ones that decide whether a read path, a
lock or an index call is affordable. This file carries the rest: rarer shapes, shapes that
need more than a table cell, the checks worth running on a sweep or when a slow endpoint
has survived the body's pass, and the comparison data those checks are counted against.

**Numbering continues each area where the body stops, and no number is reused.** Area 1
resumes at 1.7, area 2 at 2.5, area 3 at 3.6, area 4 at 4.6, area 5 at 5.5. A citation of
`3.7` therefore means one check in this skill and nowhere else, whichever file it came
from, and cross-references name **number and name** in both directions.

**Everything the body says still binds here** and is not repeated: diff mode versus sweep,
the rule that a shape whose cost scales with data is not pre-existing when the data grew,
`grep -rn --include=*.cs` as the default instrument with no profiler and no benchmark
harness assumed, the CRITICAL / HIGH / MEDIUM / INFO calibration, and the honesty rule —
whatever runs, the report says **static inspection of code shape, not a measurement**,
verbatim, and carries no number it did not read out of the code. So does the cross-area
rule the body's preamble sets: **cost that grows with something the code does not bound is
HIGH; cost the code itself bounds is MEDIUM.**

**How to read a check.**

> **<n.m> <title>** — *SEVERITY* · owner
> `Find:` the grep to run, or the file to open and what to read in it.
> Why it costs, and the change that closes it.

The owner is the skill and section that legislates the rule — cite it in the finding rather
than re-deriving it. `universal` means the check is a defect in any codebase and needs no
house citation; that is a citation too, and it is written out. Where a shape is already
graded by a sibling rubric the check reads **graded by `<skill>` <n.n>, *<name>*** and
**carries no severity of its own** — cite the owning check and add only the cost. Where one
check covers two arms and only one is graded elsewhere, the severity attaches to the named
arm and to nothing else (2.10 is the only such row). Anything
presented as a table or a listing is **comparison data, not a check**: no number, no
severity, because it is what you compare against.

## 1 — Query shape and round trips

**1.7 A read path that does not declare its tracking** — *graded by `dotnet-code-review`
1.2, "A read path that does not declare its tracking"* · `ef-core-data-access`, *Find is
the query gate*
`Find:` `grep -rn --include=*.cs "\.Find(" src/` and read each composition for
`isAsNoTracking: true`.

Already graded as a convention finding; cite it and do not re-grade it. The cost half it
does not carry: a tracked read makes the change tracker build and hold a snapshot of
**every row it materializes** for the life of the request, so memory and fixup cost scale
with the page while the response does not change — worst exactly where it is least visible,
on a list endpoint that only projects. On a read that is never saved the tracker is pure
overhead paid per row per request. Where the same endpoint also shows check 1.1, *A query
executed once per row*, report them together: the tracker cost is a multiplier on a count
that is already wrong. The shape the rule exempts — a single row fetched to be mutated and
saved — is not a finding.

**1.8 An aggregate answered by a second query instead of the page's companion payload** —
*MEDIUM* · `api-surface`, `references/request-response-dtos.md`, *`MoreInfo`*
`Find:` `grep -rn --include=*.cs "PaginationResponse<\|ToPagedListAsync(" src/Infrastructure/Modules/`
and, per module, check whether a second service member or endpoint computes a total, a sum
or a count over the same filter.

`MoreInfo` carries *"a companion object the same search already computed"* — a summary row,
totals, a recommendation derived from the same query — and exists so a search that also
needs an aggregate adds a payload alongside the page *"instead of a second endpoint"*. A
separate call re-runs the filter composition from the start: the client pays two full round
trips plus the probe overhead twice, and the two halves can disagree because they ran at
different instants. The fix is the `moreInfo` overload of the terminal call, not a cache in
front of the second endpoint. The owning rule states the boundary as a design question
rather than a cost one — if the extra data is not a by-product of this search, it *is* a
second endpoint — so report the round-trip count and route the shape decision to the owner.

**1.9 A projection carrying an aggregate over a collection navigation** — *INFO — report
the shape* · `automapper-mapping`
`Find:` open every profile on the endpoint's projection path and read each `MapFrom` for
`Count(`, `Sum(`, `Any(` or `Where(` over a navigation property.

The provider translates each of these into a **correlated subquery evaluated per row**, so
the projection's cost is page size × the navigation's size — invisible in the C# and
invisible in the response. **This is the shipped correct form**: the owning skill's own
example maps an active count exactly this way, because the alternative is a delegate the
provider cannot translate at all. So it is never a defect — name the aggregate, name the
page size it multiplies against, and stop. The adjacent rule, that a map reachable from a
query projection must not use `AfterMap` or `ConvertUsing`, is a **correctness** rule owned
entirely by `automapper-mapping` (reachability is transitive through `IncludeAllDerived`
and `IncludeMembers`); it is not this rubric's to grade, and it is why the "fix" for a slow
projection must never be a callback.

**1.10 A repository mutation reached once per row** — *HIGH* · `ef-core-data-access`,
*Saving is the repository's job*
`Find:` `grep -rn -A6 --include=*.cs "foreach (\|for (" src/` and read each body for
`AddAsync`, `UpdateAsync` or `DeleteAsync`. There is no separate `SaveChanges` to grep for
here, because saving is the repository's job — *"one repository call is one committed
change"*. **A literal `SaveChangesAsync` at a call site is a different shape and is
`dotnet-code-review` check 1.6, *A SaveChangesAsync at a call site*** — report that one
there, not here.

That design is what makes the loop expensive: *N* rows are *N* round trips and *N* commits,
each with its own transaction overhead, and there is no batching step that would quietly
rescue it — a failure at row *k* leaves *k* committed. The range members — `AddRangeAsync`,
`UpdateRangeAsync`, `DeleteRangeAsync` — are the one-call form. Where the operation
genuinely needs each row committed separately, say so and record the count. Where the loop
is a multi-step mutation rather than a bulk one, the finding is atomicity and belongs to
`dotnet-code-review` check 1.4, *A multi-step mutation with no transaction*; say which of
the two you mean.

**1.11 A seeding or initialization path built from per-row reads** — *MEDIUM* ·
`ef-core-data-access`, *DbInitializer seeding* + universal
`Find:` open the database initializer and every seeding path it calls, then read each loop
body for a per-item existence probe or a per-item save.

Seeding runs before the application serves, so the cost is startup and deployment latency
rather than request latency, and it is bounded by the seed set the code itself owns —
MEDIUM by the preamble rule, not HIGH. It earns a check anyway because seeding is where the
per-row round trip is written most freely (the author is thinking about correctness and the
row count is small in development) and where it is most likely to be copied: **the shape
graduates into a request path the first time someone reuses the method.** Say that the
deployment, not the request, is what pays. Where the reads are also synchronous, that half
is body check 2.4, *A synchronous repository read outside a validator predicate* — cite
one, not both.

## 2 — Blocking and async cost

**2.5 `async void`** — *graded by `dotnet-code-review` 3.3, "`async void`"* · universal
`Find:` `grep -rn --include=*.cs "async void" src/`

Already graded, and graded on the right grounds: the method cannot be awaited, so nothing
bounds it, nothing cancels it, and an exception inside it does not surface at the call site
— it reaches the process. There is no separate cost finding. It is listed here so a
performance sweep that greps for it knows the answer is already written and does not file a
second one.

**2.6 An `async` method that never awaits** — *MEDIUM* · universal
`Find:` build and read the diagnostics for **CS1998** (*async method lacks `await`
operators*); do not grep for this — the compiler already knows.

The method allocates and drives a state machine to produce a result it already had. On a
cold path this is invisible and not worth a line in a report; on a per-request seam called
several times per request it is real, and the fix is `Task.FromResult` or
`ValueTask.CompletedTask` in the signature the caller already expects. **Report it only
where you can name the hot path** — otherwise it is exactly the micro-optimization the
calibration rules out, and it belongs in a cleanup list rather than a report. The
counterpart shape, a non-`async` method returning a task directly, is deliberate where it
appears in the shipped capabilities and is **not** a finding; see area 4's *Not findings*.

**2.7 `Task.Run` used to escape a synchronous API** — *MEDIUM; HIGH on a request path* ·
universal
`Find:` `grep -rn --include=*.cs "Task\.Run(" src/`

`Task.Run` around a blocking call does not make it asynchronous. On a server it does not
reduce the threads the request consumes — it adds a hop and a second thread while the first
waits, so under load the pool is drained by exactly the calls that were supposed to release
it. **This is body check 2.2, *Sync-over-async*, wearing a disguise, and it is worse than
the plain form because it reads as a fix.** Where the wrapped call is itself a `.Result` or
a `.Wait()`, that inner call is `dotnet-code-review` 3.2 and is reported once, there. The
honest answer is almost always that an async member already exists at the call site; where
one genuinely does not, say so and grade the call site rather than the wrapper — and where
no asynchronous member exists at all, the finding is that the work does not belong on a
request path.

**2.8 Independent awaits issued in sequence** — *MEDIUM* · universal
`Find:` open the changed service methods and read runs of consecutive `await` statements for
a later one that does not use an earlier one's result.

Two independent round trips awaited in sequence cost their sum; the same two started
together cost the longer. The rule is narrow and the exception is not optional: **do not
report this against calls that share the scoped data context.** The change tracker is not
thread-safe — the reason a captive one is graded as severely as it is by
`dotnet-code-review` check 3.4, *A captive dependency* — so concurrent repository calls on
one request's context are a correctness defect, not an optimization. The finding is
therefore limited to calls that leave through **different** capabilities: a search-cluster
read beside a cache read, an outbound call beside a database read. Where they share the
context, say so and close the finding; when in doubt, report the sequence and ask rather
than prescribing concurrency.

**2.9 A captive scoped dependency, cost side** — *graded by `dotnet-code-review` 3.4, "A
captive dependency"* · `dotnet-code-review`
`Find:` `grep -rn --include=*.cs "AddSingleton\|AddHostedService\|: BackgroundService" src/`,
then open each registered type and read every constructor parameter's registered lifetime.

Already graded, as a correctness defect, because that is what it is — 3.4 owns the shape,
the severity and the fix. Two cost consequences belong in that same finding rather than in a
second one. A scoped context captured by a singleton is *"one non-thread-safe change tracker
shared by every concurrent request: stale reads, cross-request state, a tracker that grows
without bound, and exceptions that reproduce only under load"* — and a tracker growing
without bound presents as a memory leak and a gradual slowdown, so a performance
investigation that finds one should recognise it and route it, not re-diagnose it. The
search wrapper is the mirror image: it caches one repository per document type in a
non-synchronized structure whose check-then-add is not atomic, which is why **do not promote
it to singleton** is a shipped instruction rather than a tuning opinion.

**2.10 A process-wide client constructed per call** — *graded by `dotnet-code-review` 4.8,
"An outbound call with no timeout or no client lifetime"* **for an HTTP client**; *HIGH* for
a capability client the house ships as a singleton · `http-client-factory`, *The
sender is the only way out of the process* (the HTTP arm) + `distributed-lock`,
`references/implementation.md` + `elasticsearch-search`, *Registration*
`Find:` `grep -rn --include=*.cs "new HttpClient(\|ConnectionMultiplexer.Connect\|new ElasticClient(\|RedLockFactory.Create" src/`
and read the lifetime of whatever holds each one.

The HTTP arm is already graded; cite it and add the cost — a per-call client exhausts
sockets under load and never picks up DNS changes, and the failure arrives as a connection
error under concurrency rather than as a slow response, which is why it survives every
low-traffic test. Name the shipped destination, not the generic one: the house answer
is the facade's single pooled client with a bounded `PooledConnectionLifetime`, never
a `static readonly` field at the call site. The capability clients are this rubric's, because 4.8 does not cite them:
the lock factory *"is a singleton, and it is constructed eagerly … One multiplexer per
process is the point; a per-request one exhausts connections under load"*, and the search
client is *"Singleton from a factory — one client per process."* Connection exhaustion does
not present as a client bug; it presents as timeouts on whatever else shares the server. The
inverse mistake has its own owner — the search **wrapper** is scoped deliberately (2.9) — so
read the registration before proposing either direction.

## 3 — Cache and staleness cost

**3.6 A cache write inside a transaction that has not committed** — *MEDIUM* ·
`distributed-caching`, `references/usage-patterns.md`
`Find:` `grep -rn --include=*.cs -A10 "BeginTransactionAsync" src/Infrastructure/` and read
each block for a cache write before the commit.

*"Note that the cache write is not part of the database transaction. If a surrounding
transaction rolls back after `Cached` ran, the cache holds a value the database never
accepted; when the mutation participates in a transaction, invalidate after it commits."*
The entry is then wrong rather than merely stale, and every reader gets a fast wrong answer
because the fallback that would have corrected it never runs. **That consequence is
correctness and belongs to its owners** — `dotnet-code-review` check 4.7, *A mutation that
leaves the cache or the index stale*, and check 1.5, *A transaction that cannot unwind*,
where the un-unwindable step is the concern. What belongs here is the ordering cost: an
invalidation moved after the commit is free, and an author who "fixes" this by shortening
the TTL has bought a window instead of closing one — body check 3.3, *A TTL lengthened to
improve the hit rate*, from the other side.

**3.7 A fallback that can return something the cache would not have** — *HIGH* ·
`distributed-caching`, `references/usage-patterns.md`
`Find:` `grep -rn --include=*.cs "GetAsync<\|GetRemoveAsync<" src/Infrastructure/` and read
each `??` fallback expression against the write that populates the same key.

*"The fallback must return the same logical value the cache would have. If the two paths can
disagree, the flow is non-deterministic and no test will reliably catch it."* A different
filter, a different projection, a different default: the same request returns one answer
warm and another cold, and no test catches it because the test decides which path runs.
**This is the one cache finding that does not de-escalate** — everywhere else in this area
the worst case is slower-but-correct, and here it is a different answer, which is why it
sits a rung above body check 3.1 rather than beside it. Say that explicitly in the finding,
so the next reviewer does not "correct" it back down. It is graded here rather than routed
because the divergence is almost always introduced for a cost reason — the fallback written
as the cheap query rather than as the same query — and because it is invisible while the hit
rate is high. Report both paths and ask which one is the definition.

**3.8 A large object graph stored as one entry** — *MEDIUM* · `distributed-caching`,
`references/usage-patterns.md` + universal
`Find:` `grep -rn --include=*.cs "SetAsync<\|Cached(" src/Infrastructure/` and read the type
argument of each write; open the type and follow its collection properties.

Every read pays serialization, transfer and deserialization of the whole entry whatever the
caller needed from it, so a graph cached to save one query can cost more than the query
saved. The shipped rule that bites here is the compatibility one: *"Treat a cached payload
as a wire format: changing its shape is a compatibility decision, not a refactor"* — a
renamed or removed property *"reads back as `null`/`0` with no error."* A large graph is a
large wire format, and it is the shape that turns the next author's refactor into a silent
data defect. Report the size and the read frequency together — a large entry read rarely is
not the same finding as one read per request — and note that the fix is usually a narrower
cached type.

**3.9 A cache in front of a read that fits neither pattern** — *MEDIUM* ·
`distributed-caching`, *When to cache*
`Find:` for each cached read, decide which of the two shipped patterns it is — a **handoff**
(produced by one stage, consumed once by the next, correlated by an id) or a **cache-aside
configuration-like row** (read on nearly every request, written rarely, one writer that
always invalidates). Anything that is neither is the finding.

The shipped decision table's third row is explicit: anything else is **"don't cache yet."**
The reason is cost, not purity — a cache is a round trip to Redis plus a fallback, so in
front of a single-row indexed read it is a net addition on a miss and a wash on a hit, while
adding a key to maintain, an invalidation to forget and a payload to keep compatible, and
one whose writers do not all funnel through a single invalidation helper pays that on every
write as well. State the finding precisely: not *"caching is wrong here"* but that **no
shipped pattern covers this shape**. This is the one check in the rubric whose fix is
*remove the optimization*, and it is a decision this rubric can frame but not make.

**3.10 A cache client reached outside the facade** — *MEDIUM* · `distributed-caching`
`Find:` `grep -rn --include=*.cs "IDistributedCache\|ConnectionMultiplexer\|IDatabase" src/`
and discard the hits inside the cache facade's own folder.

**Nothing else in the solution touches `IDistributedCache` or a Redis client directly.** A
second reach means a second connection policy, and the four shipped values are what make an
outage degrade to slower-but-correct: the owning skill states that *"Leaving
`AbortOnConnectFail` at its default turns a cache outage into an application outage"*, which
is precisely what a hand-rolled client gets. Where the second reach is also a second **key**
convention, the finding is body check 3.1, *One entry, two key factories* — cite one. The
**placement** half — a capability reached from outside its facade — belongs to
`dotnet-architecture-review`; report the cost here and route the shape there.

## 4 — Lock and contention

**4.6 A multi-key lock taken as nested single-key calls** — *graded by `dotnet-code-review`
3.8, "Nested single-key locks instead of the multi-key overload"* · `distributed-lock`
`Find:` `grep -rn --include=*.cs -A12 "LockedAsync" src/` and read each delegate for a second
`LockedAsync`.

Already graded; cite it and do not re-grade it. The contention cost it does not carry: the
multi-key overload *"sorts the keys before acquiring and releases in reverse. That global
ordering is what stops two callers holding one another's keys"*, and nesting discards it, so
**each caller holds its first lock for the entire wait on its second — the contention cost
is paid twice even when the two callers never deadlock at all.** Both failure outcomes are
named by the owner and neither is acceptable: on the in-memory provider *"it is a permanent
deadlock"*, on the distributed provider *"both callers exhaust `WaitTime` and answer
`423`."* And *"it appears only under concurrency and never under test"*, which is why a
static reviewer is the only one who will ever see it. The fix is the list overload, not a
hand-written ordering convention.

**4.7 A key coarser than the resource it guards** — *HIGH* · `distributed-lock`, *Key
discipline*
`Find:` `grep -rn --include=*.cs "LockedAsync" src/` and read the key expression at each call
site against the operation the delegate performs.

*"Name the operation, not just the entity"*: two operations on one entity are two locks, and
one key serialising both is *"either too coarse or — worse — accidentally shared with a third
feature that meant something else by it."* The throughput consequence is what this rubric
grades and why it is HIGH: every unrelated caller of that entity now queues behind work that
could not have interfered with theirs, so **queue depth becomes the endpoint's whole traffic
rather than the contention on one row** — it grows with load rather than with conflict, and
nothing in the code bounds it. Report the two operations you would separate; it costs nothing
to fix at the call site, because *"There is no central key factory and none should be
introduced."* Two neighbouring defects in the same grep are **not** cost findings and route
to the owner: a key too narrow to exclude the caller it must exclude (*"they do not exclude
each other … the most common real failure and it is invisible"*), and a bare identifier as a
key, which *"is a genuine collision risk against every other key in the server"* because lock
keys receive none of the cache's prefix.

**4.8 A key composed from a value read outside the delegate** — *graded by
`dotnet-code-review` 3.6, "A read-check-write with no lock, or work outside the lock"* ·
`distributed-lock`, `references/usage-patterns.md`
`Find:` at each `LockedAsync` call site, read the statements above it that produce the key and
ask whether the delegate re-reads them.

Already graded: deciding on a value read before the lock is the check-half of a
read-check-write sitting outside it. Reading a value in order to *choose* the key is
sanctioned — the key must exist before there is a lock to take, *"as in Pattern 2 — read it
again inside for the decision"* — and what is not sanctioned is deciding on that snapshot,
which *"is a snapshot from before the previous holder's write; deciding on it inside the lock
is the original race with extra steps."* It is written up in a performance rubric because the
shape is produced by a **cost instinct**: the author moved the read out of the delegate to
shorten the held region, which is body check 4.2, *Work inside the delegate that passes the
interleaving test*, applied one statement too far. Report it as the interleaving test's other
edge — the re-read is one of the statements that must stay inside — and route the race itself
to 3.6.

**4.9 `LockedException` caught and retried inside a module** — *MEDIUM* · `distributed-lock`,
*Do not catch `LockedException` in a module*
`Find:` `grep -rn --include=*.cs "LockedException" src/` and discard the hits in the
exception's own declaration and in the middleware.

*"A failed acquisition is already the correct answer: the caller is told the resource is busy
and can retry. The retry has already happened"* — `WaitTime` and `RetryTime` are that retry. A
module that catches it either swallows a real conflict or, worse, retries in a loop: the
caller's wait is multiplied invisibly and the endpoint's latency ceiling becomes the product
of two retry policies, one of which is written down nowhere, while the key that was already
contended takes sustained pressure. **This is a contention finding rather than an
error-handling one for that reason** — the catch is where a bounded fast failure becomes an
unbounded slow one, the same failure mode body check 4.4, *`WaitTime` raised to reduce
errors*, describes from the other end. Where the catch simply swallows without retrying, that
is `dotnet-code-review` check 4.1, *A swallowed failure*. The fix is to let it travel.

**4.10 The registry cleanup window, cost side** — *graded by `dotnet-code-review` 3.10,
"Registry cleanup interleaving"* · `distributed-lock`
`Find:` run check 3.10 as written.

Already graded, **and graded as a shape rather than as a defect to fix in passing** — the
shipped registry carries this window deliberately and the owning skill says so. Do not
prescribe a fix: 3.10's own ruling is that *"the finding is not 'this is broken', it is 'this
is the known window; show me why it is benign here.'"* The reason it appears in a performance
rubric at all is that **the tempting optimization is exactly what breaks it** — removing the
lazy value to avoid an allocation, or removing the cleanup to avoid a dictionary write,
changes what makes the window benign. If a change under review touches a shared registry for
a cost reason, say that the benign-ness is the property being traded and route the judgement
to 3.10. There is no separate cost finding: a concurrent dictionary makes each operation
atomic, and contention on it is not what makes the endpoint slow.

**Not findings: three shapes that look like contention and are not.** Recorded so a sweep
does not re-derive them, and because each has been raised before. **The lock factory is
constructed eagerly and composition blocks on Redis** — one multiplexer per process is the
point, a per-request one exhausts connections under load, and an unreachable server surfacing
at startup rather than at the first lock is the deliberate opposite of the cache's policy,
because a cache has a correct degraded mode and **a lock has none**; expect this when
debugging a startup hang and do not report it. **The lock and cache connections are separate
on purpose** — same server, two connections, each owned by its capability; sharing one would
tie a cache restart to the lock's availability, so do not propose the consolidation. **The
dispatch is deliberately non-`async`** — both overloads return the provider's task directly,
so it adds no state machine and no extra allocation, and the shipped instruction is to keep
it that way; a reviewer applying 2.6 here has it exactly backwards.

## 5 — Search-infrastructure cost

**5.5 An index write inline with the database write** — *MEDIUM; HIGH where a user-facing
write path inherits the ten-minute ceiling* · `elasticsearch-search`, the re-index pattern
`Find:` `grep -rn --include=*.cs -A8 "AddAsync(\|UpdateAsync(\|DeleteAsync(" src/Infrastructure/Modules/`
and read for an indexing call in the same method.

Re-indexing belongs in *"a handler reacting to a domain event, not inline in the code that
just wrote the row"*, and that handler *"re-reads the source of truth, tolerates a missing
document, and overwrites idempotently."* Inline, the caller pays a cluster round trip inside
its own latency budget — including the refresh wait — and a cluster that is slow or down
makes the database write path slow or fail with it. One round trip is bounded, which is why
this is MEDIUM by default; it escalates where the path is user-facing, because the client
sets a ten-minute timeout for every request (body check **5.4, *A request-path lookup under
the ten-minute ceiling***) and a hung cluster then turns a write endpoint into a ten-minute
hold. The cost and the coupling are one finding; name both. The fix is the move — a
`try`/`catch` around the inline call fixes the failure and keeps the cost.

**5.6 A descriptor with no explicit size** — *MEDIUM* · `elasticsearch-search`
`Find:` `grep -rn --include=*.cs "SearchAsync(" src/` and read each descriptor for a `.Size(`
or a `.From(`/`.Size(` pair.

*"Page size always comes from the descriptor you write; `DefaultSize` is configured and
validated but no call site reads it, so it is not a fallback you can code against."* A
descriptor naming no size therefore inherits the cluster's own default — a number nobody in
this solution chose — and the call quietly transfers and materializes that many documents
whatever the caller needed, so the transferred volume is set by a value that appears in no
file. State the size the call site should name, and **name `DefaultSize` in the finding**:
that is what stops the next author "fixing" it by configuring a value no code reads.

**5.7 A by-id fetch issued per id** — *MEDIUM* · `elasticsearch-search`, *the repository
surface*
`Find:` `grep -rn --include=*.cs -A6 "foreach (" src/` and look for `GetByIdAsync` in the
body; `grep -rn --include=*.cs "GetByIdManyAsync" src/` for the member that replaces it.

One round trip per id where the surface offers a member that takes the whole sequence — the
area's version of body check **1.1, *A query executed once per row***, one process further
out. Two shipped properties of that member belong in the same finding rather than in a
separate one, because a reviewer reading the loop will meet both: it takes *"string ids only
— it casts the sequence unconditionally, so a `Guid` list throws at runtime"*, and it is
*"the one member that skips `ThrowIfFailure`, so a rejected request can come back empty
instead of throwing."* The second is also why a per-id loop can look like it is working: each
rejected call returns empty and the caller reads a miss. The fix is the batched member **plus
an explicit empty-result check** — recommending the member alone trades a slow path for a
silent one.

**5.8 A read-modify-write loop where a query-scoped update belongs** — *MEDIUM* ·
`elasticsearch-search`, *the repository surface*
`Find:` `grep -rn --include=*.cs "UpdateAsync\|UpsertRangeAsync" src/` inside loops, and
`grep -rn --include=*.cs "UpdateByQueryAsync\|DeleteByQueryAsync" src/` for the members that
replace them.

*"Flip a field on every matching document, in place"* is `UpdateByQueryAsync` — *"a script,
not a document"* — one request instead of a fetch, a materialization and one write per
document. Two constraints decide whether it applies and must travel with the recommendation:
the script uses *"camelCase field names, numeric enums"* and is easy to get wrong in ways
that fail silently; and a query matching more than you meant updates or deletes more than you
meant — `DeleteByQueryAsync` *"deletes exactly what the query matches"* — which is why the
owning skill requires running *"the same predicate through `CountAsync` first whenever it is
not trivially exact."* Report the round-trip arithmetic and the count-first precaution
together, and hand the script to the owner: a reviewer who proposes the query-level form
without the precaution has proposed a cheaper way to be wrong.

**5.9 Startup work proportional to the corpus** — *INFO; MEDIUM where the work is unbounded* ·
`elasticsearch-search`, *Registration*
`Find:` open the search facade's registration and every `IIndexSettingsMapper` implementation
in the solution; read each for anything beyond settings and mappings — a query, a count, a
document write.

First resolve of the client is not lazy in the way the registration suggests: it *"scans the
Infrastructure assembly for `IIndexSettingsMapper` implementations and calls each one —
blocking cluster calls that create or amend indices"*, and *"Composition waits on
Elasticsearch at that moment."* **That cost is the shipped trade** — *"the trade for never
querying an index that does not exist"* — and is not a finding (body, *Two shapes to report,
not to grade*): record how many mappers run and what each does, noting that startup time here
grows with the number of document types the solution has, not with traffic. What **is** a
finding is a mapper that does more than declare an index: every statement added there runs
inside composition, before the process serves anything, on a path with no timeout of its own
beyond the client's ten minutes. INFO where that work is bounded; MEDIUM where it reads the
corpus.

## Comparison data — round trips in the shipped shapes

Not a check: this is what checks **1.1**, **1.4**, **1.10** and **2.3** are counted against.
Each row is the shipped shape's own cost as its owning skill states it; a call path that
exceeds it is where the finding is. Two rows are marked as **compositions** — arithmetic over
two shipped facts rather than a number any body states — and nothing here is a measurement.

| Shipped shape | Round trips | Stated by |
|---|---|---|
| The search shape, end to end | **five** — three probes (`ApplyFilter`, `ApplySearch`, `ApplySort` each open with `entities.Any()`), the page, the count | `ef-core-data-access`, `references/query-conventions.md`, *Know the cost* |
| The search shape with a companion payload | *composition:* the five above, plus the companion object's own composition in the same query | `api-surface`, `references/request-response-dtos.md`, *`MoreInfo`* |
| One repository mutation | one — *"one repository call is one committed change"* | `ef-core-data-access`, *Saving is the repository's job* |
| A multi-step mutation | *composition:* one per repository call, plus begin and commit | same |
| A handoff cache read | one Redis read, plus the source-of-truth query on a miss | `distributed-caching`, `references/usage-patterns.md`, *Pattern A* |
| A locked operation | the delegate's own round trips, serialized behind every other caller of that key | `distributed-lock`, *the interleaving test* |

**Read the table as a floor, not a budget.** Five round trips is what the canonical search
*costs*, not what it is *allowed*; a sixth is a finding only when a check names it.

## Refused — and why

Recorded so the next session does not re-derive them, and so a reader can see the boundary was
drawn deliberately rather than forgotten. Each of these is a real performance topic; none has
a shipped owner or a universal footing, and **this rubric does not invent doctrine under a
performance banner** — the temptation is stronger here than anywhere else, because every one
of them is defensible advice somewhere. Where a refusal still leaves the reader somewhere to
go, the row says where.

| Candidate | Why it is not here |
|---|---|
| **Rate limiting, request quotas, lockout policy, brute-force cost** | No shipped body states a policy, and a limit invented in a review becomes the de facto contract. `dotnet-security-review` refuses it for the same reason, routes *"a rate-limiting or DoS question"* here, and grades one adjacent shape itself — its check 3.1 ranks a caller-assembled `LIKE` pattern MEDIUM as a denial-of-service surface — so the route is real and the answer is thin. **The one house-grounded mitigation is body check 1.3, *A list endpoint with no page-size ceiling of its own***: the pagination contract's own guard is the only bound this stack ships against a caller who asks for too much. Name 1.3, and state the policy question as unowned rather than answering it. |
| **Hangfire, background jobs and their scheduling cost** | **No `background-worker` skill has shipped.** Job frequency, concurrency limits, retry cost, queue depth and a job overlapping its own next run have no sentence to be compared against, so every check would be invented. A job's *body* is ordinary code: run the five areas against it and cite their numbers. Revisit when the owning skill ships. |
| **Benchmark and profiling methodology** | BenchmarkDotNet setup, "measure first", flame graphs, sampling intervals and what makes a valid comparison are all real and none is house doctrine. They are also the opposite of what this rubric is — Core Principle 1 says the report *"does not profile, benchmark or observe production."* Recommending that someone take a measurement is always legitimate and is not a finding; specifying how to run one is somebody else's document, and a reviewer who takes one is no longer running this rubric. |
| **`HybridCache`** | **Considered and not adopted**, in those words — evaluated for its L1/L2 layering, stampede protection and tag-based invalidation, ruled out on framework-version grounds, with the shipped facade named as the standard until a framework upgrade. Recommending it reopens a settled decision from below; stampede protection is the argument most likely to be re-raised here. Revisit in `distributed-caching`, not in a review. |
| **Compiled queries, and query-construction cost generally** | No shipped body mentions `EF.CompileQuery` or `CompileAsyncQuery`, so there is no house position to conform to and no site the rubric could call deficient — a check would manufacture a finding on every query in the solution. The same refusal covers *"this queryable should be hoisted or reused"*: the canonical read composes a fresh queryable per call **by design**, because that composition is what makes the filter, search and sort contract work. |
| **`Span<T>`, `stackalloc`, object pooling, `ValueTask` preference, struct choices** | The house legislates **one** sentence about allocation and it forbids a change rather than describing a defect: the lock dispatch stays non-`async` because it *"adds no state machine and no extra allocation. Keep it that way."* One protected call site is not a foundation for an allocation area. Allocation findings here are limited to what other checks already produce — 1.7's per-row tracker, 2.6's needless state machine, 3.8's payload — and nothing in a report should read as a general allocation audit. Everything else is an outside opinion with a number attached, and the number would be one this rubric did not measure. |
| **`TimeProvider` and injectable clocks** | Kit doctrine, and a testability argument rather than a cost one. `ef-core-data-access` defaults `CreatedAt` to `DateTimeOffset.UtcNow` in the shipped base entity, so a rubric raising this grades house design as a defect; `dotnet-testing` owns test seams. Do not import it through a performance report. |
| **Sealing classes that nothing inherits** | A standing exclusion, already written down: `dotnet-code-review`, `references/cleanup-checklist.md` — *"Not in this taxonomy … It is not a house convention, it is not enforced anywhere in the shipped skills, and a review that raises it is importing an outside opinion. Do not add it."* No more a performance finding here than a cleanup one there. |
| **Using `DbContext` directly instead of the repository wrapper** | Kit doctrine, and the direct opposite of house law — the wrapper is the shipped data-access surface (`ef-core-data-access`) and lives in a sanctioned facade (`dotnet-architecture-review`). A "fewer layers is faster" argument here is an architecture rewrite proposed inside a review. |
| **`ClockSkew = TimeSpan.Zero` and clock-drift tolerance** | The zero is house law and *"stricter than the advice flagging it"*; `dotnet-security-review` records it as a suppression. The cost framing — that a strict skew produces more refreshes and refreshes cost round trips — has no shipped sentence setting a tolerance or a refresh budget, so a rubric inventing one would be arguing for a security-relevant relaxation on performance grounds. Refused there, refused here. |
| **Connection-pool sizing, context pooling, provider-level tuning** | The connection policies this stack ships are the cache's, the lock's and the search client's, and all are declared **policy, not tuning knobs**. Nothing shipped states a database pool size or endorses context pooling, so both would be invented — and body check **4.1, *A transaction opened before the lock is acquired***, is the one pool-pressure finding with a house sentence behind it. A report answering pool pressure with "raise the pool size" has replaced that finding with a setting. |
| **Projection-safety of a mapping profile** — `AfterMap`, `ConvertUsing`, `IncludeMembers` on a projection-reachable map | Real, shipped and owned by `automapper-mapping`, but its consequence is **correctness**, not cost — the projection either ignores the callback or fails at runtime. Check 1.9 records what a reviewer needs to know about it; the grading is not this rubric's. The one cost-shaped outcome is the workaround: where an author replaced a failing `ProjectTo` with materialize-then-map, that is body check **1.4, *A terminal call made too early***, and it is reported there. |
