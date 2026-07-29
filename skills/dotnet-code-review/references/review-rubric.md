# Review rubric — the per-area checks

The checklists behind the priority order in `SKILL.md`. Work them in order: data
access, security, concurrency, integration, correctness, tests, simplicity. Style
and slop are not here — `references/cleanup-checklist.md` owns those.

**Scope, stated once.** The default scope is the change. A solution-wide sweep is
justified only when hunting consumers of a symbol the diff altered. A finding
that is pre-existing and outside the change is INFO, per `SKILL.md`. Do not
re-derive this at each check.

**Tool, stated once.** Every check is a manual instruction: a `grep` to run or a
file to open. No analysis server is assumed. Patterns are written for
`grep -rn --include=*.cs`; paths are written as `src/Modules/<Module>/`,
`src/Web/`, `tests/` — substitute the solution's own shapes, and `<X>` for any
entity or feature name.

**How to read a check.**

> **<n> <title>** — *DEFAULT SEVERITY* · owner
> `Find:` the grep, or the file to open and what to look at.
> Why it is a finding, and the shape of the fix.

The severity shown is the **default**. Escalate when the change scores Critical
blast radius, when the defect is reachable from an unauthenticated path, or when
it can corrupt, lose or expose data. De-escalate to INFO when the finding is
pre-existing. An owner in `code font` is the skill that legislates the rule —
cite it in the finding rather than re-deriving it. `universal` means the check is
a defect in any C# codebase and needs no house citation. Nothing else qualifies
as a check.

**Routed elsewhere, stated once.** Anything whose finding is *"this is slow"* —
N+1, projection cost, index coverage, allocation, round-trip counts — belongs to
`dotnet-performance-review`; note the suspicion, do not grade it. Auth internals
— how a scheme parses, how a policy composes, what a permission constant means —
belong to `auth-and-security`, with deep posture work to `dotnet-security-review`.
The security section below stays at posture level.

## Contents

- [1. Data access](#1-data-access)
- [2. Security posture](#2-security-posture)
- [3. Concurrency](#3-concurrency)
- [4. Integration](#4-integration)
- [5. Correctness](#5-correctness)
- [6. Tests](#6-tests)
- [7. Simplicity and over-build](#7-simplicity-and-over-build)

## 1. Data access

**1.1 Data access that bypasses the wrapper** — *HIGH* · `ef-core-data-access`
`Find:` `grep -rn --include=*.cs "ApplicationDbContext\|DbSet<" src/Modules/` and
`grep -rn --include=*.cs "interface I[A-Z][A-Za-z]*Repository" src/`
A context, a `DbSet`, or a hand-written per-entity repository injected into a
service is a second data-access convention living beside the first. The fix is
`repositoryWrapper.Repository<T>()`.

**1.2 A read path that does not declare its tracking** — *MEDIUM* ·
`ef-core-data-access`
`Find:` `grep -rn --include=*.cs "\.Find(" src/Modules/`
A read composing to `FirstOrDefaultAsync`/`ToListAsync` without
`isAsNoTracking: true`, or passing it positionally as a bare `true`. The flag is
the query's read/write declaration and reviewers read it as one; a bare `true`
tells the next reader nothing. Tracking stays on only when the loaded entity will
be mutated and saved.

**1.3 An `Include` chain in front of a projecting read** — *MEDIUM* ·
`ef-core-data-access`
`Find:` `grep -rn -A3 --include=*.cs "\.Include(" src/Modules/`, then look for
`.ProjectTo<` in the following lines.
The projection generates the joins it needs, so the `Include` changes nothing
about the SQL. It is dead code standing in front of the real path, and it teaches
the next author that includes are required here. `Include` is for materializing
entities with their children.

**1.4 A multi-step mutation with no transaction** — *HIGH* · `ef-core-data-access`
`Find:` open every changed service method and count calls to
`AddAsync|AddRangeAsync|UpdateAsync|UpdateRangeAsync|DeleteAsync|DeleteRangeAsync`.
Two or more in one operation without `BeginTransactionAsync` means the operation
is not atomic: a failure between them leaves half the change committed.

**1.5 A transaction that cannot unwind** — *HIGH; CRITICAL when the half-state is
observable* · `ef-core-data-access` + `error-handling`
`Find:` `grep -rn -A6 --include=*.cs "BeginTransactionAsync" src/`, then read each
hit to the end of its method.
Any of — no `catch`; a `catch` that does not call `RollbackTransactionAsync`; a
`catch` that rolls back and *returns*, reporting success for work that was undone;
a `Begin`/`Commit`/`Rollback` call missing the cancellation token; a `return`
inside the `try` that skips the commit. Rank CRITICAL when the uncommitted half is
observable — a row written without its lines, a status advanced without its
ledger entry. The compensating `catch` is one of the two catches that earn their
place, and the failure must keep travelling.

**1.6 A `SaveChangesAsync` at a call site** — *HIGH* · `ef-core-data-access`
`Find:` `grep -rn --include=*.cs "SaveChangesAsync" src/Modules/ src/Web/`
Each mutation saves itself, so one repository call is one unit of work and there
is nothing left for an outer save to flush. A stray save either does nothing or
commits a second, unintended unit — and the two are indistinguishable from the
call site. Read the surrounding method: which one it is changes the fix.

**1.7 Raw SQL carrying request data** — *CRITICAL* · universal
`Find:` `grep -rn --include=*.cs "FromSqlRaw\|ExecuteSqlRaw" src/`
Any interpolation or concatenation of caller-supplied values into command text is
injection. Parameterise, or use the interpolated overload, which parameterises for
you. Note that this pair is an escape hatch in the first place — its parameter
array comes first and neither member takes a cancellation token — so every call is
a choice that must be justified. That it is an exception at all is
`ef-core-data-access`'s ruling.

**1.8 A new entity without its configuration** — *MEDIUM* · `ef-core-data-access`
`Find:` for each entity class added in the diff, grep its own file for
`IEntityTypeConfiguration<`.
One entity, one file, its configuration beside it, opening with
`HasBaseEntity().UnderscoreTable()`. There are no `DbSet` properties, so an entity
with no configuration is simply not in the model.

**1.9 A destructive migration** — *CRITICAL* · universal
`Find:` open every file added under the migrators project and grep it for
`DropColumn\|DropTable\|RenameColumn\|AlterColumn\|nullable: false`.
A drop or a rename destroys data on deploy; a new non-nullable column with no
default fails against existing rows. Migrations are applied programmatically at
startup when the initialisation flag is on, so the review is the gate that
actually exists. Read the generated SQL, not just the builder calls. A migration
is Critical blast radius by definition.

## 2. Security posture

**2.1 An action with no explicit authorization decision** — *CRITICAL* ·
`api-surface` + `auth-and-security`
`Find:` per changed controller,
`grep -c "\[Http" src/Web/Controllers/<X>Controller*.cs` and
`grep -c "\[HasPermission\|\[AllowAnonymous\|\[ApiKey" src/Web/Controllers/<X>Controller*.cs`
— the counts must match.
An unmarked action inherits whatever the controller or pipeline default happens to
be, and that default can change without the action's file being touched. Every
action states its own answer, including "anonymous" — otherwise a deliberate
anonymous endpoint and a forgotten one are indistinguishable. Omission is
invisible in a diff. `[ApiKey]` is the third explicit answer — the machine-caller
filter (`auth-and-security`); an action carrying it needs no principal-based
attribute, and pairing it with `[HasPermission]` is its own finding.

**2.2 `[HasPermission]` written positionally** — *CRITICAL* · `api-surface`
`Find:` `grep -rn --include=*.cs "\[HasPermission(" src/Web/` and flag every call
whose first argument is neither `permissions:` nor `schemes:` **and** which is not
the full three-argument positional form.
The constructor is
`HasPermissionAttribute(string[] schemes = default!, params string[] permissions)`
— `params` sits second, so `[HasPermission(SomePermission)]` binds the string to
`schemes` and authorizes nothing. It compiles, it looks protective, and it is not.
The named argument is mandatory in the scheme-only and permission-only shapes;
only the fully positional three-argument shape may omit names. This is the
highest-value single grep in the rubric.

**2.3 A request value used before it is validated** — *HIGH* · `module-feature` +
`api-surface`
`Find:` for each request class in the diff, grep its own file for
`AbstractValidator<`; then compare the validator's rules against the properties
the service dereferences, parses, or uses to build a key or a path.
The contract, its validator and its mapping profile live in one file. Automatic
validation rejects a bad request before the action runs, so an unvalidated
property is a value the service trusts on the caller's word — and nothing reports
it.

**2.4 An operation not scoped to the caller** — *HIGH* · `api-surface`
`Find:` `grep -rn --include=*.cs "RangeGuidValidator<" src/` — is the optional
filter expression passed? — and `grep -rn --include=*.cs "\"me/" src/Web/` — does
the route also take an owner id?
A bulk operation over ids with no filter to the caller's own rows lets a caller
name someone else's. `me` means the caller identified by the token, so `me/{id}`
is a contradiction; addressing someone else's resource is a different permission.

**2.5 A secret in source or in a committed settings file** — *CRITICAL* · universal
`Find:` `grep -rniE "password=|pwd=|apikey|api_key|secret|bearer [A-Za-z0-9._-]{20,}|BEGIN (RSA|PRIVATE)" --include=*.cs --include=*.json src/`
A credential in the repository is compromised the moment it is pushed, and the fix
is rotation plus configuration — never deletion of the line alone.

**2.6 Sensitive data in a log line or in a message a caller reads** — *HIGH* ·
universal + `error-handling`
`Find:` `grep -rn --include=*.cs "Log[A-Z][a-z]*(" src/` and read every
interpolated argument; then
`grep -rn --include=*.cs "throw new .*Exception(\$\"" src/`
Tokens, credentials, whole request bodies and personal data do not belong in
either. Logs are retained longer, replicated wider and read by more people than
the database is — a log line is an export. Note the redaction limit: production
redaction blanks envelope properties, but the `Message` of an *unhandled*
exception is that exception's own text, which is one more reason to throw a leaf
carrying a written key rather than let an infrastructure exception describe the
system to a stranger.

**2.7 An entity reaching the wire** — *HIGH* · `api-surface` + `module-feature`
`Find:` `grep -rn --include=*Service.cs "Task<" src/Modules/` and flag return
types that are not a `…Response`, a `PaginationResponse<…>` or a primitive; then
`grep -rn --include=*.cs "class [A-Za-z]*Response *: " src/` and flag any response
whose base is a **domain entity**.
An entity's shape is a persistence decision; publishing it makes every column a
public contract, and a column added for internal reasons becomes an unplanned
disclosure the day it is added. Every operation returns a response type — a write
returns by re-reading through the projection. **A response family rooting at the
shared base type is correct** — see 5.12 for the defect that actually lives here.

## 3. Concurrency

**3.1 A dropped `CancellationToken`** — *HIGH; CRITICAL only when it corrupts* ·
`ef-core-data-access` + universal
`Find:` three passes.
`grep -rnE "(SaveChangesAsync|BeginTransactionAsync|CommitTransactionAsync|RollbackTransactionAsync|ToListAsync|FirstOrDefaultAsync|SingleOrDefaultAsync|AnyAsync|CountAsync|AddAsync|AddRangeAsync|UpdateAsync|DeleteAsync)\(\)" --include=*.cs src/`
— the empty parentheses are the tell; then
`grep -rn --include=*.cs "CancellationToken.None" src/`; then open each method that
*accepts* a token and read whether every awaited call inside receives it.
The work keeps running after the caller has gone, burning a connection and a
thread for a response nobody will read. **Escalate to CRITICAL only where the
un-cancelled work corrupts or exposes** — inside a transaction, or between two
writes a disconnect can leave half-applied. A slow report finishing into the void
is HIGH, not CRITICAL.

**3.2 Sync-over-async** — *HIGH* · universal
`Find:` `grep -rnE "\.Result\b|\.Wait\(\)|GetAwaiter\(\)\.GetResult\(\)" --include=*.cs src/`
and, inside `async` methods,
`grep -rnE "\.(Any|Count|First|FirstOrDefault|Single|ToList)\(" --include=*.cs src/`
Blocking on a task inside a request risks thread-pool starvation and deadlock, and
under load the pool starves before anything else fails — so the symptom appears
far from the cause. The second grep catches the quieter shape: a synchronous
`Any()`/`Count()` over an `IQueryable` inside an async chain both blocks **and**
silently drops the token, because the sync overload has nowhere to put it. The
seeding path is where this hides most often. The async overload exists at every
one of these call sites.

**3.3 `async void`** — *HIGH* · universal
`Find:` `grep -rn --include=*.cs "async void" src/`
An exception thrown from an `async void` method cannot be caught by the caller and
takes the process down. Return `Task`.

**3.4 A captive dependency** — *HIGH; CRITICAL when the captive is the context or
the wrapper* · universal
`Find:` `grep -rn --include=*.cs "AddSingleton\|AddHostedService\|: BackgroundService" src/`,
then open each registered type and read every constructor parameter's registered
lifetime.
A scoped dependency — the context, the repository wrapper, anything marked as a
per-request service — captured by a singleton is resolved once and held forever.
For the context that means one non-thread-safe change tracker shared by every
concurrent request: stale reads, cross-request state, a tracker that grows without
bound, and exceptions that reproduce only under load. Fix shape: inject
`IServiceScopeFactory` and open a scope per unit of work. Where the registration
belongs is `facade-module-architecture`'s.

**3.5 Mutable state on a singleton** — *HIGH* · universal
`Find:` `grep -rn --include=*.cs "static" src/` inside facade and handler types;
read every field that is neither `const` nor `readonly`, plus every `readonly`
collection that is not a concurrent type.
A field on a singleton is shared by every concurrent request. A plain
`Dictionary`/`List` mutated from two requests corrupts internally and can hang.

**3.6 A read-check-write with no lock, or work outside the lock** —
*HIGH; CRITICAL when the guarded work runs outside an acquired lock* ·
`distributed-lock`
`Find:` `grep -rn -A12 --include=*.cs "FirstOrDefaultAsync\|AnyAsync" src/Modules/`
for a subsequent `AddAsync`/`UpdateAsync` conditioned on what was read; then
`grep -rn -B6 -A12 --include=*.cs "LockedAsync" src/`
Two shapes, one bug. Unlocked: the gap between the read and the write is the whole
defect — two requests both pass the check. Locked but leaking: everything the lock
protects must run **inside** the delegate — the check that decides whether the work
may happen, the mutation, and the write. A check performed before the call or a
write issued after it returns races exactly as the unlocked code did, and the
symptom is the double processing the lock was added to prevent, with nothing
logged. Whether a given resource can actually be named twice is a domain
judgement: raise the shape as a question, not a verdict.

**3.7 A lock call site relying on the provider default** — *HIGH* ·
`distributed-lock`
`Find:` `grep -rn --include=*.cs "LockedAsync" src/` and check each call passes
options with the distributed provider set explicitly.
The options default to the in-memory provider, which is scoped to one process. Two
instances behind a load balancer each acquire their own semaphore, both believe
they hold the lock, and both run the guarded work. Nothing throws and nothing logs;
the duplicate appears later as duplicate data.

**3.8 Nested single-key locks instead of the multi-key overload** — *HIGH* ·
`distributed-lock`
`Find:` in the same grep as 3.7, look for a `LockedAsync` inside another
`LockedAsync` delegate.
The multi-key overload sorts the keys before acquiring and releases in reverse;
that single global ordering is what stops two callers each holding what the other
needs. Nesting acquires in whatever order each call site was written — a permanent
deadlock on the in-memory provider, two exhausted waits and a 423 on the
distributed one. It appears only under concurrency, never under test.

**3.9 A hand-rolled lock** — *HIGH* · `distributed-lock`
`Find:` `grep -rnE "new SemaphoreSlim|lock *\(|static readonly object|Interlocked\." --include=*.cs src/Modules/`
A semaphore field on a service, a `lock` on a static object, or a dictionary of
in-flight ids is invisible to the next instance of the process. Mutual exclusion
has one owner; call it. If the solution has no lock capability at all, that is a
scaffolding decision rather than a review fix — say so and route.

**3.10 Registry cleanup interleaving** — *HIGH, and report it as a shape* ·
`distributed-lock`
`Find:` `grep -rn --include=*.cs "GetOrAdd\|TryRemove\|TryAdd" src/`
A shared concurrent registry that removes an entry when its last visible user
finishes can interleave: A has already fetched the entry, B removes it, C adds a
fresh one, and A and C now hold different objects for the same key while both
believe they are excluded. A concurrent dictionary makes each operation atomic,
not the sequence. The shipped lock registry carries exactly this window,
documented, and made benign by storing lazy values — **so the finding is not
"this is broken", it is "this is the known window; show me why it is benign
here."** Name the interleaving and ask for the invariant; do not prescribe the fix
at review time.

## 4. Integration

**4.1 A swallowed failure** — *HIGH; CRITICAL when the caller is told it
succeeded* · `error-handling`
`Find:` `grep -rn -A5 --include=*.cs "catch" src/` and read every block that does
not end in `throw`.
A `catch` that logs and continues, returns `null`, or returns an empty result makes
the request report success while nothing happened — and the 500 that would have
been logged with a trace id never was. The default is not to catch: an exception
reaching the middleware uncaught becomes a 500 carrying a trace id, the exception
text, its source type, method and line, and an error log entry. A `catch` that
adds nothing *removes* all of that.

**4.2 `Console.WriteLine` as logging** — *MEDIUM; HIGH when it is the only record
of a swallowed failure* · universal
`Find:` `grep -rn --include=*.cs "Console.Write\|Debug.WriteLine" src/`
It has no level, no scope, no trace id and no sink, so it is invisible in exactly
the environment where it would have mattered. When it pairs with 4.1 that is one
defect, not two — report it once, at the higher severity.

**4.3 A catch filter that downgrades an exception already carrying its status** —
*HIGH* · `distributed-lock` + `error-handling`
`Find:` `grep -rn --include=*.cs "catch (Exception" src/` and, for each, establish
what status the block produces, which exceptions can reach it, and — decisively —
whether the `try` **wraps** the lock acquisition or sits **inside** the locked
delegate.
The rule: any handler that converts an exception into a different status must
exclude the exceptions that already carry the right one.

```csharp
catch (Exception ex) when (ex is not BadRequestException and not LockedException)
```

The canonical miss is a compensating `catch` **wrapping** a lock call that
excludes only the rule-violation exception. It runs the undo for work that never
started and converts a retryable 423 into a 500 — telling the caller the server is
broken when the correct answer was "try again shortly", so the retry that would
have succeeded never happens. A filter **inside** the delegate needs no such
exclusion: acquisition already succeeded, the exception cannot occur there, and
adding it would be noise. Settle the inside/outside question before writing the
finding.

**4.4 A second producer of error responses** — *HIGH* · `error-handling`
`Find:` `grep -rn --include=*.cs "ErrorResultWrapper\|Response.StatusCode *=" src/`
outside the middleware.
The exception middleware alone shapes an error response. A `catch` that builds the
wrapper or sets the status by hand means two places decide one contract, and they
drift.

**4.5 A rethrow that adds nothing, or a `try` inside an action** — *MEDIUM* ·
`error-handling` + `api-surface`
`Find:` `grep -rn --include=*.cs "Exception(ex.Message" src/` and
`grep -rn --include=*.cs "try" src/Web/Controllers/`
A rethrow carrying the same text and the same inner exception adds a stack frame
and no context; the middleware would have produced an equivalent response with
better diagnostics from the untouched exception. If a `catch` cannot say something
the exception did not, delete the `catch`. An action is a single delegating
expression, so there is nowhere in it to put a `try` — which is the point; the
work belongs in the module service.

**4.6 Two logging policies in one pipeline** — *MEDIUM* · `error-handling`
`Find:` `grep -rn -A4 --include=*.cs "catch" src/` and flag blocks that log
unconditionally before rethrowing or wrapping.
The shared path logs as an error only at `StatusCode >= 500`, deliberately: a 400
is a normal outcome of a public API, and logging every rejected request as an
error trains people to ignore the error log. A `catch` that logs everything it
sees double-logs the 500s and adds the 400s back in, defeating the gate for the
whole service. If one particular 400 is genuinely worth recording, log it where it
is thrown and say why.

**4.7 A mutation that leaves the cache or the index stale** — *HIGH* ·
`distributed-caching` / `elasticsearch-search`
`Find:` for each entity written in the diff,
`grep -rn --include=*.cs "<X>" src/` across the cache and search facades to see
whether it is cached or indexed at all.
A write that does not invalidate the entry it invalidated, or reindex the document
it changed, serves stale data until the TTL expires — and a TTL is not an
invalidation strategy. It reproduces for some callers and not others, and never
for the developer who just wrote the row.

**4.8 An outbound call with no timeout or no client lifetime** — *HIGH* ·
universal + `error-handling`
`Find:` `grep -rn --include=*.cs "new HttpClient(" src/` and grep the typed-client
registrations for `Timeout`.
A client constructed per call exhausts sockets under load and never picks up DNS
changes; a call with no timeout hangs the request thread on a dependency that has
stopped answering, and the default timeout is long enough to exhaust the request
pool before it fires. A dependency's own exception type also means nothing to your
caller — wrap it in a leaf with a written message and the original as the inner
exception, which is the other catch that earns its place.

**4.9 Configuration read past the bound options object** — *MEDIUM* ·
`facade-module-architecture`
`Find:` `grep -rn --include=*.cs "IConfiguration\|configuration\[\|GetSection(" src/Modules/ src/Facades/`
Settings bind once through the options chain — `AddOptions<T>()` →
`BindConfiguration(nameof(T))` → `ValidateDataAnnotationsRecursively()` →
`ValidateOnStart()` — so bad configuration fails the process at startup instead of
a request at first use. A service reading a raw key per request skips the
validation, the section-name convention and the typed contract, turning a startup
failure into an intermittent one. The bound type is unwrapped to `.Value` in the
constructor; no method sees the wrapper. Binding belongs in the owner's own
settings registration.

## 5. Correctness

**5.1 An exception leaf that does not pin its status in every constructor** —
*HIGH* · `error-handling`
`Find:` `grep -rn --include=*.cs ": HttpCustomException" src/`, open each, and
count constructors against assignments of `StatusCode`.
A constructor that forgets the assignment — usually the inner-exception overload —
leaves `StatusCode` at its default `0`, and the middleware copies that straight
onto the response: an exception named for a 400 emitting an invalid status.
Pinning is the leaf's only job. It stays latent exactly until someone uses the
unpinned overload, so check the diff for new call sites too.

**5.2 A leaf on the non-HTTP base, or one carrying a payload** — *HIGH* ·
`error-handling`
`Find:` `grep -rn --include=*.cs ": CustomException" src/` and read each type's
members.
A leaf on the non-HTTP base has no `StatusCode`, so the middleware treats it as
unknown and answers 500 whatever the name promised. A leaf carrying state must be
*taught* to the middleware — a dedicated `catch` and a dependency injected for no
other reason. The contract is two members: the status and the message. Structure a
caller actually needs is a *response*, on the success path.

**5.3 A member that nothing reads** — *MEDIUM* · `error-handling` + universal
`Find:` for each member added or touched, `grep -rn --include=*.cs "\.<Member>\b" src/`
across the solution and count the reads.
An unread member is worse than unused code: the next author reads it as a channel
that works and builds on it. The middleware consults the status and the message
and nothing else, so any other member on an exception is dead by construction —
the base type's own unread payload member is the precedent, and is not an
invitation. State plainly that nothing reads it; removal itself is `/simplify`'s
job, behind the safe-delete checks in `cleanup-checklist.md`.

**5.4 The wrong leaf for a not-found** — *HIGH* · `error-handling`
`Find:` `grep -rn --include=*.cs "NotFoundException\|StatusCodes.Status404\|NotFound()" src/`
Business not-found is a 400, and no not-found leaf gets added. 404 already has one
owner — routing, where a malformed typed id dies before any code runs. A
well-formed id matching no row is a lookup *result*, and answering it the same way
as a malformed URL leaves the caller unable to tell the two apart. The single
carve-out is the current principal's own record, which is a 401.

**5.5 A validator message typed to the entity** — *MEDIUM* · `message-keys`
`Find:` `grep -rn --include=*Validator*.cs "WithMessage(Messages<" src/`
Requests type validator messages; entities type outcome messages. A rule about the
request's own property reads `Messages<TRequest>.X(x => x.Prop)`, with the display
attribute on that request keeping the module segment right. The entity-typed form
emits the same key and is **superseded** — a finding in new code, not in old. The
one exception is a rule asserting that a *different* entity exists, whose message
is that entity's to own. **Where older material disagrees, `message-keys` governs
— verify against it before writing the finding, not against memory.**

**5.6 A wrong or hand-made key** — *MEDIUM* · `message-keys`
`Find:` `grep -rn --include=*.cs "WithMessage(\"" src/`;
`grep -rn --include=*.cs "throw new \w*Exception(\"" src/`;
`grep -rn --include=*.cs "\"Mes\." src/`;
`grep -rn --include=*.cs "WithMessage(MessagesType\." src/`; and for each request
named inside `Messages<…>`, grep its declaration for the display attribute.
A literal bypasses the key grammar entirely and ships as itself. A hand-typed key
is worse, because it looks correct while drifting silently the day a class is
renamed. A request with no display attribute falls back to its type name for the
module segment, so the transport type leaks into every key — nothing fails, the
key is simply wrong and differs from every other key for the same entity. The
legacy extension form emits an identical key: recognise it when reading, never
write it new.

**5.7 A message that blames the wrong property** — *MEDIUM* · universal, with the
message itself `message-keys`
`Find:` read each `RuleFor(x => x.A)` chain in the diff and confirm every
`WithMessage` selector in it names `x.A`; then
`grep -rn -A6 --include=*.cs "ThrowIf\|IsExist" src/Modules/`
The caller is told to fix a value that was never wrong, which is worse than no
message — it sends them confidently in the wrong direction. The common shape is a
shared helper that hardcodes one property name and is then reused for a second. It
is invisible to every test that asserts only the status code.

**5.8 A file in `Services/` that is not a service part** — *MEDIUM* ·
`module-feature`, *When a service outgrows one file*
`Find:` `ls src/Modules/<Module>/Services/`
The full test and the destinations live in `dotnet-architecture-review` check 4.9,
which owns `Services/` folder shape as a placement question. Note the hit here,
cite that check, and do not re-derive the rule.

**5.9 A base list on a non-core partial part** — *MEDIUM* · `module-feature` +
`api-surface`
`Find:` `grep -rn --include=*.cs "partial class .*:" src/`
The suffix-less core file is the only part carrying a base list, for a service and
for a controller alike. Repeating the lifetime marker or the base controller on a
second part compiles — the compiler merges them silently — which is exactly why it
is a review finding: every part now claims to be the core file, and the lifetime
and the contract have no single home. The prefix-named form scatters one type
across the alphabet for the same reason.

**5.10 A message handler that does not delegate** — *HIGH* · `module-feature`
`Find:` `grep -rn -A15 --include=*.cs "IRequestHandler<\|INotificationHandler<" src/`
Every handler delegates to the owning module's service and its body is one line. A
handler that injects the repository, queries it and hands back an entity has moved
one module's behaviour into a file another module reads through: the service stops
being the module's whole surface, and an entity crosses the boundary that
responses exist to hold.

**5.11 A guard that returns, or a predicate that throws** — *MEDIUM* ·
`module-feature`
`Find:` `grep -rn --include=*.cs "ThrowIf\|IsExist" src/Modules/*/Validations/`
The boundary is symmetric: a guard never returns `bool`, a predicate never throws.
A validator *asks* and turns `false` into its own message; a service *demands* and
leaves the caller nothing to decide. Also flag a service re-checking a rule its
validator already ran — that is one rule with two homes and two messages.

**5.12 A response that is a sibling instead of a rung** — *MEDIUM* ·
`api-surface` + `automapper-mapping`
`Find:` `grep -rn --include=*.cs "class \w+Response : " src/Modules/`
Two responses in one family both deriving from the shared base instead of one
deriving from the other; a base rung nothing derives from; a response re-declaring
properties the rung above already gives it. The family is a ladder, not a flat
set — that is what stops a property added to one rung silently missing on the
next, and what lets the derived profiles inherit their member configuration
through `IncludeAllDerived` on the base rung's profile. **Rooting the family at
the shared base type is correct**; the defect is the missing rung between two of
them, or the orphan rung with nothing below it.

**5.13 A nullability warning the change introduced** — *HIGH* · universal
`Find:` build before and after the change and diff the warning lists; flag every
new `CS86xx`.
The compiler is stating that a reference it cannot prove non-null is being
dereferenced. That is a `NullReferenceException` with a date on it, not a
tidiness issue — which is why it is a correctness finding and not slop, even
though it surfaces during the cleanup sweep. A suppression added instead of a fix
is the same finding: `grep -rn --include=*.cs "#pragma warning disable" src/`.

## 6. Tests

Which tier a scenario belongs to is `dotnet-testing`'s Decision Guide. This pass
asks only whether the changed behaviour is covered, and whether the tests that
exist can fail.

**6.1 Changed behaviour with no test at the right tier** — *HIGH* ·
`dotnet-testing`
`Find:` `grep -rn "<X>Service\|<X>Controller" tests/` for each changed type; the
projects are `tests/<ProjectName>.UnitTests` and
`tests/<ProjectName>.IntegrationTests`.
Name the specific scenario that is missing, not "add tests". The tiers are not
interchangeable: query and persistence behaviour is proven against a real
database; guards, branches and what gets thrown are proven with the facade
boundary substituted. A test at the wrong tier passes while proving nothing.

**6.2 A projecting read covered only by a unit test** — *MEDIUM* · `dotnet-testing`
`Find:` for each read composing a projection, check which tier covers it.
Nothing but a real database proves the projection translates to SQL. A substituted
queryable will happily evaluate in memory what the provider cannot translate.

**6.3 An in-memory or re-registered database in a test project** — *HIGH* ·
`dotnet-testing`
`Find:` `grep -rn "UseInMemoryDatabase\|InMemory\|Sqlite" tests/`
It enforces no unique index, honours no transaction, and generates none of the SQL
the real provider does — so it passes exactly the tests that matter most. Flag the
neighbouring shape too: a fixture that removes and re-registers the context rather
than overriding the configuration keys, which makes the suite exercise a context
the application never builds.

**6.4 A test that cannot fail** — *HIGH* · `dotnet-testing`
`Find:` `grep -rn -A12 "\[Fact\]\|\[Theory\]" tests/` and flag every body with no
assertion call.
"It did not throw, so it works" passes for every reason including the wrong ones,
and reports coverage it does not have — which is worse than no test, because it
stops anyone writing the real one. Also flag a test asserting a validation failure
against the error envelope: a request rejected before the action never throws, so
there is no envelope, every field deserializes to null, and the assertion passes
for the wrong reason.

**6.5 A call verified where a real outcome exists** — *MEDIUM* · `dotnet-testing`
`Find:` `grep -rn "Received(\|DidNotReceive(" tests/`
Verify a call only where its presence or absence is the entire observable — a
guard's promise that nothing was written. On a path that produced state, assert
the state; the test then survives the refactor that changed how it got there.

**6.6 A validator test asserting the entity-typed message** — *MEDIUM* ·
`message-keys` + `dotnet-testing`
`Find:` `grep -rn "Messages<" tests/`
The mirror of 5.5: an assertion typed to the entity passes against a validator
that should have been request-typed, so the superseded form survives the one test
that would have caught it.

**6.7 A flow spread across ordered tests** — *HIGH* · `dotnet-testing`
`Find:` `grep -rn "static .*_.*;\|TestCaseOrderer\|IClassFixture" tests/` and look
for step-shaped method names or static fields carrying ids between tests.
Each test gets a fresh class instance and a reset database, so the state the second
step expects is gone before it runs. One test owns the whole sequence.

**6.8 A new or unreviewed test package** — *HIGH* · `dotnet-testing`
`Find:` `grep -rn "PackageReference" tests/**/*.csproj`
FluentAssertions v8 and later require a paid commercial licence, so introducing it
is a purchasing decision rather than a package bump; Shouldly is the house
assertion library and NSubstitute the house double library. Moq is not used. A new
test package of any kind is a decision, not a detail — name it in the review.

## 7. Simplicity and over-build

> The ladder runs after you understand the problem, not instead of it.
> Read fully, then be lazy.

Three checks, in order: does the task in front of you need this code, does it
already exist, and is this the smallest shape the owning skills allow. A reviewer
who has not read the whole method cannot tell deliberate structure from slop, and
this is the one area where that mistake costs the author real work for nothing.

**Capped at MEDIUM, and the cap is absolute.** The escalation clause in *How to
read a check* does not reach this area: it produces candidates for `/simplify`,
not merge blockers. A finding here is written like any other — `file:line`, what
is wrong, why, the fix — and lands in the MEDIUM or INFO section with *candidate
for `/simplify`* as its fix. The `Cleanup candidates` section stays
`cleanup-checklist.md`'s. The scope rule bites harder here than anywhere else: a
shape the change merely touched is pre-existing, therefore INFO, and usually not
worth the line — and at **Low** blast radius this area is not reached at all,
because a rename is not an invitation to redesign.

**Where the simpler shape is itself a shipped convention, the finding already has
a number and an owner.** Cite it by number and name and stop — one defect, one
grader. A hand-rolled mutual exclusion is 3.9 *(A hand-rolled lock)* at HIGH. A
second lock, cache or search capability beside the existing one is ruled by its
own skill — `distributed-lock`, `distributed-caching` and `elasticsearch-search`
each state *"if you find one, use it in place"*. A package added where a
referenced one would do is the repository `CLAUDE.md`'s new-dependency rule where
it carries one (`claude-md-builder` R16), and in a test project it is 6.8 *(A new
or unreviewed test package)* at HIGH. None of these is re-graded here.

**Never a simplification candidate, whatever it costs:** validator rules at a
trust boundary (`module-feature`); the exception flow and the error envelope
(`error-handling`); `CancellationToken` declaration and propagation (3.1);
authorization attributes and everything `dotnet-security-review` grades; message
keys (`message-keys`); migration safety (1.9); the structural families named in
7.1; and anything the user explicitly asked for. Proposing one of these as
over-build is itself the defect in the finding.

**7.1 Code written for a need the task does not have** — *MEDIUM* · universal,
with folders owned by `dotnet-architecture-review`
`Find:` `git diff main...HEAD` for what the change adds; then for each type,
member, parameter, overload or interface it introduces,
`grep -rn --include=*.cs "<SymbolName>" src/ tests/` and count the hits outside
its own declaration. Read every new `bool` or enum parameter and every new `if`
or `switch` arm against the guards and the validator that run before it. Then
read the task, or the plan the change implements, and name the requirement each
addition serves.
A parameter nothing passes, an overload nothing calls, a flag with one value, an
arm for a state a validator already rejected, an interface written for a second
implementation the task does not have — each is a promise the next author must
keep, and when the need finally arrives it arrives in a different shape, so the
code is rewritten anyway, having cost twice. Fix: cut it from the change while
cutting is still free, and say what would justify writing it later — name the
missing requirement, never that the code "feels speculative".
**Zero compile-time references proves nothing on its own.** A service registered
through its lifetime marker has no references by design; run the safe-delete
checks in `cleanup-checklist.md` before proposing any removal.
**Does not apply to** structure a shipped skill mandates. Facades-axis
infrastructure built ahead of need — *"a technical capability many projects
reuse"*, where reach decides and not size (`facade-module-architecture`) — is
sanctioned structure, not over-build. So are the module's file family and its
response tiers (`module-feature`), a thin request or notification envelope and
its handler (`mediatr-messaging`), and a marker type an assembly scan resolves
(`facade-module-architecture`). A type that exists because the convention says
the module has one is not "code nothing calls", and a finding against one of
these is wrong rather than merely low-value.
**Two neighbours own their own halves.** Code the change *orphaned* rather than
added is `cleanup-checklist.md` category 3 *(Dead code)*, behind the same
safe-delete checks. A folder created in advance of its trigger is
`dotnet-architecture-review` 4.2, at INFO. Cite them; do not re-grade them.

**7.2 A helper written where one already exists** — *MEDIUM* · universal
`Find:` search for what the new code *does*, never for what it is called — a
duplicate that shared a name would have collided at compile time. For each
helper, extension method, converter, mapper or private method the diff adds, grep
the words of its name one at a time —
`grep -rni --include=*.cs "<NameWord>" src/` — and read every declaration among
the hits; then `grep -rn --include=*.cs "static class \w*Extensions" src/` for the
ones in the same module or facade, and `ls src/Facades/ src/Facades/Common/` to
read the capability list against what the new code does.
Two implementations of one job do not stay identical. A fix lands on one, the
other keeps the bug, and the caller that got the stale one is the report filed
weeks later against the wrong file — so both survive and a third gets written
beside them. Fix: call what exists, and extend it in place if it is one argument
short of the job.
**Does not apply to** a per-module copy the convention requires — each module
having its own service, validator and profile is the shape, not duplication — nor
to a capability duplicate, which the paragraph above routes to its owner rather
than grading here. Collapsing a prescribed repetition into one generic that
serves every case is a 7.3 finding in the opposite direction, not a saving.

**7.3 A shape more elaborate than the owning skill requires** — *MEDIUM* · the
skill owning the area + `dotnet-testing`
`Find:` open the skill that owns whatever the change lands in and compare the
diff against the shape it prescribes; count the hops from the controller action
to the code that does the work; and for every interface or abstract type the diff
adds, `grep -rn --include=*.cs ": I<Name>\b" src/` to count the implementations
and `grep -rn "<Name>" tests/` to see whether anything substitutes it.
The floor is the shipped convention, never fewer lines than it. Above that floor,
an abstraction earns its place when a shipped skill mandates it, or when a test
must stand in for it — unit tests substitute at the facade boundary, and an
extension method cannot be substituted because NSubstitute configures only
members the interface declares (`dotnet-testing`, `references/unit-testing.md`).
Counting implementations settles nothing on its own: the repository wrapper is
what unit tests substitute at the facade boundary, and it earns its place
whatever the count. A wrapper around a wrapper, a strategy with one strategy, a
generic parameter one caller ever binds, an indirection that only forwards, a
layer added so the code would "be ready" for a second implementation nobody has
asked for — each adds a hop the next reader must trace and buys nothing back. Say
which of the two tests the shape fails.
**The same defect from the other side.** A simplification that merges two
concerns into one method, inlines the seam a test substitutes, collapses a guard
chain into an expression that hides which rule fired, or drops the invalidation
4.7 *(A mutation that leaves the cache or the index stale)* requires scores here
exactly as over-build does. Clarity beats brevity, and a candidate that cannot be
applied without changing behaviour is not a candidate — drop it and say why.
Fewer lines than the convention is not simpler; it is off-convention, and the
next author restores the convention by hand.
**Does not apply to** the shapes the owning skills mandate — the request,
validator and profile file family and the response tiers (`module-feature`), the
exception leaf's two members (`error-handling`), the per-capability `Startup.cs`
and the four-call options binding (`facade-module-architecture`), a thin envelope
and its one-line handler (`mediatr-messaging`). "Fewer files" is not a house
value.
