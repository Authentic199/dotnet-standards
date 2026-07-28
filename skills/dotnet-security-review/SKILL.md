---
name: dotnet-security-review
description: >-
  This skill should be used when reviewing .NET or C# code for security: a
  security review or audit, hardcoded secrets and connection strings in
  appsettings, vulnerable packages, injection, mass assignment, a missing
  [HasPermission] or a stray [AllowAnonymous], JWT validation settings, CORS
  pipeline position, or data exposure through DTOs, logs and error responses.
  Not for: blast radius, severity, slop — dotnet-code-review; layering,
  dependency direction — dotnet-architecture-review; N+1, allocation, blocking —
  dotnet-performance-review; the JWT, policy and secret rules themselves —
  auth-and-security; endpoint and DTO shape — api-surface; exception flow,
  redaction — error-handling; the review process —
  superpowers:requesting-code-review, superpowers:receiving-code-review.
---
## Overview

This is a **rubric, not a pipeline.** It says what to check when the question is
*what an attacker could reach, read or forge*, in what order, and how to rank what it
finds. It does not run the review — that belongs to
`superpowers:requesting-code-review` and `superpowers:receiving-code-review` — and it
does not apply the fixes: rotating a key is a configuration change, closing a gate an
ordinary one.

**It checks conformance; it does not define it.** `auth-and-security` defines the auth
layer, `api-surface` the HTTP surface and the document, `error-handling` the error
envelope, `facade-module-architecture` pipeline position. Lines quoted here are the
target compared against, never a second statement of the rule (`dotnet-code-review`
Principle 5, *The rubric cites the owning skill; it never re-teaches it*). So **a check
must trace to a shipped skill's body, or be a defect in any codebase** — one that is
neither is refused, not softened — and **cite by number and name**. Where an area has
no shipped owner this rubric says so out loud; security is the subject where invented
rules sound most plausible.

**House doctrine outranks generic security advice, and the two disagree here.**
Hardening guides demand `ValidateIssuer`/`ValidateAudience` be `true` and treat a bare
authenticated-only attribute as a finding; this stack deliberately does the opposite.
A report that raises settled house design as a vulnerability is worse than one with a
gap — the author learns the whole document can be ignored. Read *Not a finding* in
layers 2, 4 and 6 first.

**Every check is a manual instruction.** No code-analysis server, no Roslyn tooling, no
scanner: each check is a `grep -rn --include=*.cs`, a file to open, or a command whose
output you read, and `dotnet list package` is the only automation here. Where other
material says *"find all references to `[AllowAnonymous]`"*, that is a grep, and **the
degradation is not lossless** — a grep sees an attribute written in a file, never one
applied by convention, inherited, or composed at runtime. Check 4.1 compares two counts
rather than reading one list precisely because absence is what a grep is worst at
seeing.

**Two modes, and say which** — diff or sweep, scoped exactly as
`dotnet-architecture-review`, *Two modes, and say which*. Diff is the default; a
pre-existing exposure in a touched file is INFO unless the change makes it reachable or
worse. One addition: **a secret that was ever committed is never "pre-existing"** —
history does not heal, so it scores full severity in both modes.

**Scope: posture, not breadth.** Reach for this rubric when the change *is* the security
surface — auth wiring, a settings file, a new endpoint's authorization, a response
contract, a logging change, a package bump — or when a release gate or an inherited
codebase needs a sweep. `dotnet-code-review` section 2, *Security posture*, is the
breadth pass and routes here. **That section is not restated:** checks 2.1–2.7 and 1.7
keep their home and their numbers, and every seam below cites them.

## Core Principles

1. **Every report opens with the honesty rule, verbatim, in these words:** *This is
   static analysis, not a penetration test. It catches known patterns and
   house-doctrine violations; it does not catch business-logic flaws, complex
   authorization bypasses, or anything that only exists at runtime.* A report that does
   not bound itself is read as a clearance and the reader stops looking — the sentence
   is the finding the report itself cannot make, which is why it is quoted rather than
   summarised and why report rule 1 forbids moving it.

2. **A missing gate is worth more attention than a weak one.** Authorization is opt-in:
   `GetFallbackPolicyAsync` returns `null`, so an endpoint with no authorization
   metadata is simply not protected and nothing logs it (`auth-and-security` →
   `permission-internals.md` §2). Absence is invisible in a diff and at runtime. Spend
   the pass on what is *not there*.

3. **Fail-open is the shape worth hunting.** Most defects announce themselves; the ones
   this rubric exists for do the opposite — a null the framework reads as "no policy", a
   type name that stops resolving, a cache entry that never lapses. The question is
   never "does this check work?" but **"what happens when it cannot run?"**

4. **A suppression is content, not politeness.** The *Not a finding* blocks bind as hard
   as the checks. Reporting `ValidateIssuer = false` or `ClockSkew = TimeSpan.Zero` as a
   vulnerability is a defect in the review: every real finding in the same report is now
   read as possibly-noise.

5. **Name the exposure and the reach.** "This is insecure" is not a finding; "an
   unauthenticated caller reaching `POST /x` can set `Y`" is. Say what an attacker
   obtains and from what position — unauthenticated, any authenticated caller, another
   family, an administrator. Severity is decided by reachability, so a finding that
   cannot name who reaches it cannot be graded. For key material the remediation is
   **rotation**, never deletion of the line.

## The layers, in order

Six layers. **Run all six, in order, and report coverage.** The order is by cost, not
risk: layer 1 is one command, layer 6 is judgement on every response the change
touches. A CRITICAL in an early layer never excuses skipping a later one — a patched
CVE list says nothing about a leaking DTO.

| # | Layer | Unit | Answers |
|---|---|---|---|
| 1 | Packages | the restored package graph | Is a known vulnerability already in the build? |
| 2 | Secrets | committed files | Is key material in the repository, or is its contract broken? |
| 3 | Injection and unsafe input | call sites taking caller data | Does request data reach an interpreter or a path? |
| 4 | Auth posture | the auth facade and every endpoint | Is every gate present, ordered, and closed on failure? |
| 5 | CORS | the CORS facade and the pipeline | Is the policy applied where the house says? |
| 6 | Data protection and exposure | responses, logs, the document | What leaves the process, and to whom? |

**Scoping a partial run** — a dependency bump, layer 1; a new or changed endpoint, 4
and 6; an auth change, 4; a configuration change, 2 and 5; a release gate, an inherited
codebase or an incident, all six. Whatever ran, *Layer coverage* says so.

Checks are numbered **per layer and never reused**; `references/security-checks.md`
continues each layer's numbering where this body stops, so `4.11` means one thing in
this skill and nowhere else. Every `Find:` below is the whole instruction — there is no
tool that does it for you. Paths assume the standard layout (`src/Core/`,
`src/Infrastructure/`, `src/Web/`, `tests/`); **resolve the real roots from the `.sln`
once, before the first grep**, because a path that does not exist returns nothing and an
empty result reads exactly like a clean pass. Patterns assume `grep -rn --include=*.cs`
unless stated.

### 1 — Packages

| # | Check | Severity |
|---|---|---|
| 1.1 | **A dependency with a published vulnerability.** `Find:` `dotnet list package --vulnerable --include-transitive`. The transitive flag is not optional: most advisories land on a package no `.csproj` names. Take the severity from the advisory, name the package and the path pulling it in, and give the patched version as the fix; where no patch exists the finding is the risk plus the compensating control. Then answer what the tool cannot — **is the vulnerable path reachable from this service?** A deserialization advisory in a build-only library is INFO; the same one in the request path is its advisory severity or higher · universal | **from the advisory** |

**The body's half of this layer is one command, and the honest limit is that the house
legislates nothing else about packages** — no pinning policy, no approved list, no
supply-chain doctrine. Two adjacent rules belong elsewhere: a new *test* package is
`dotnet-code-review` check 6.8, *A new or unreviewed test package*, and central version
management is `facade-module-architecture`'s. If the command cannot run, say so under
*Layer coverage* rather than reporting a clean layer.

### 2 — Secrets

Deepens `dotnet-code-review` check 2.5, *A secret in source or in a committed settings
file*, whose grep is the entry point — run it first and do not restate it. What follows
is what it does not reach: the fix, and the contract the committed file carries.

| # | Check | Severity |
|---|---|---|
| 2.1 | **Key material in a committed configuration file.** `Find:` open the security configuration topic and every `<topic>.json` / `<topic>.<Environment>.json` beside it; read every value a settings class declares as a key, password or connection string. A UUID, a base64 run, or anything not visibly a `<description>` placeholder is a live key. **The fix is rotation, not deletion** — a key in committed history is compromised regardless of later edits, so a diff removing the line and nothing else restates the finding rather than resolving it. Name the blast radius: an access signing key logs that family out immediately, a refresh key ends sessions at the next refresh · `auth-and-security`, *Don't commit key material* | **CRITICAL** |
| 2.2 | **A committed settings block deleted instead of placeholdered.** `Find:` in the diff, read every removed key in a committed `*.json`. **A placeholder is not a missing value — it is the contract**: the committed file tells a new environment which keys to supply. Emptying the block hides the requirement, and the next deployer discovers it by reading C#. The shape is the key present with a descriptive placeholder and the non-secret lifetimes beside it · `auth-and-security`, `references/principal-and-secrets.md` §3, *The non-commitment rule* | **HIGH** |
| 2.3 | **A settings root bound without startup validation.** `Find:` `grep -rn "AddOptions<" src/Infrastructure/` and check each chain reaches `ValidateDataAnnotationsRecursively()` **and** `ValidateOnStart()` — recursive matters, because the validated property is usually on the nested per-scheme class. A security check, not a configuration one: **placeholder-as-contract and startup validation are one design.** Without `ValidateOnStart`, a placeholder reaching production fails at the first 401 in production instead of at boot, and nobody connects the two events · `auth-and-security`, `references/principal-and-secrets.md` §3, *Settings and secrets* | **HIGH** |
| 2.4 | **Credentials in an infrastructure configuration topic.** `Find:` `grep -rniE "\"(password\|pwd\|username\|apikey\|key\|connectionstring)\" *:" src/Web/Configurations/`. Both owners state the same rule — **credentials never go in this file**; environment variables load last and beat every JSON file, and that is where a deployed username and password come from. Rotate, as in 2.1. Separate from 2.1 because the auth files get looked at and the search and cache topics do not · `elasticsearch-search` + `distributed-caching` | **CRITICAL** |
| 2.5 | **Document-UI credentials in a committed topic.** `Find:` open the document's settings topic and read its credentials block. Outside Development the basic-auth middleware is the only thing between the internet and a complete map of the API surface, so its username and password are the highest-leverage credential the committed tree can hold. The gate itself is 6.6 · `api-surface`, `references/openapi-swashbuckle.md` + universal | **CRITICAL** |

**Not findings here.** A **placeholder** in a committed file — `<signing key>`,
`changeme`, an empty string — is the contract working as designed, and `ValidateOnStart`
turns one reaching a real environment into a boot failure. A **`UserSecretsId` in a
`.csproj`** is the fix, not the problem. A **non-secret** — a lifetime, a node URI, an
index prefix — belongs there; lifetimes in particular are reviewable decisions the
owning skill commits on purpose. And a value in **`appsettings.Development.json` or a
test fixture** is not HIGH; see *Severity calibration*.

### 3 — Injection and unsafe input

**3.1 Raw SQL reachable from request data** — *CRITICAL* · `dotnet-code-review` check
1.7, *Raw SQL carrying request data* (universal)

`Find:` `grep -rn "FromSqlRaw\|ExecuteSqlRaw\|SqlQueryRaw" src/ tests/`

Check 1.7 owns the rule and the fix. **Cite it; do not restate or re-grade it.** Four
sites it does not reach are this layer's:

- **Widen the grep past the module folders** to the whole solution including migrations
  and seeding — 1.7 runs on the change, this layer on the surface.
- **Which endpoint reaches each surviving call, and whether it is anonymous.** Record
  surviving sites as INFO with their reachability even when they parameterise; a correct
  call is still where the next edit introduces concatenation.
- **A column or sort direction built from request text.** Parameters cannot parameterise
  an identifier, so the fix is an allow-list mapping the caller's token to a constant. A
  reviewer who says "parameterise it" here is wrong.
- **A `LIKE` pattern assembled from caller input** is not injection but a
  denial-of-service surface; rank it MEDIUM and say which of the two it is.

| # | Check | Severity |
|---|---|---|
| 3.2 | **Insecure deserialization.** `Find:` `grep -rn "BinaryFormatter\|TypeNameHandling\|SoapFormatter\|LosFormatter\|new JavaScriptSerializer" src/`. A payload that names its own type turns deserialization into arbitrary object construction. No shipped body legislates a serializer, so this is stamped universal and the finding is the reachability: caller-supplied bytes, or bytes from a store a caller can write · universal | **CRITICAL** |
| 3.3 | **A weak or hand-rolled cryptographic primitive.** `Find:` `grep -rn "MD5\|SHA1\|DESCryptoServiceProvider\|CipherMode.ECB\|new Random(" src/`. Flag only where the value is a *security* value — a password, token, signature or nonce. **A weak hash used as a cache key, an ETag or a shard selector is not a finding**; say which you found before writing it up. The house prescribes no cryptography beyond the JWT layer, so name the exposure and ask for the algorithm decision rather than dictating one · universal | **HIGH** |
| 3.4 | **A path or process argument built from request data.** `Find:` `grep -rn "Path.Combine\|File.Open\|File.Read\|File.Write\|new FileStream\|Process.Start" src/` and read each argument back to its source. A request-supplied filename containing `..` escapes the intended directory, and `Path.Combine` discards everything before a rooted argument — `/etc/x` silently becomes the whole path. The same shape reaching a process argument is command execution. The fix is a generated name plus a resolved-path containment check, not a blacklist of `..` · universal | **HIGH; CRITICAL when the path is read or written without further checks** |
| 3.5 | **A request value used before it is validated.** Run `dotnet-code-review` check 2.3, *A request value used before it is validated*, as written. The security escalation: when the unvalidated property is the one reaching 3.1, 3.4 or 6.1, it is the same finding as that one — report it once, at the higher severity, and name both · `dotnet-code-review` 2.3 | **HIGH** |

> **Out of scope, honestly.** Generic material checks **XSS and output encoding in
> server-rendered views** (`@Html.Raw`, Razor, Blazor). **This stack renders no
> server-side views** — `api-surface` settles Controllers returning JSON wrappers — so
> the defect has no engine to live in, is not checked here, and its absence from a
> report is not a gap. If a response field carries caller-supplied markup a browser
> client renders, the escaping decision belongs to that client; the API-side finding is
> 6.5, not XSS. **Access-control defects at the query level are `dotnet-code-review`
> check 2.4, *An operation not scoped to the caller*.** Run it, cite it, do not renumber
> it here.

### 4 — Auth posture

The deepest layer, and where fail-open lives. It deepens two checks it does not own: run
`dotnet-code-review` check **2.2, *`[HasPermission]` written positionally*** first — it
compiles, looks protective, and authorizes nothing — and check **2.1, *An action with no
explicit authorization decision***, whose count-comparison is the entry point for 4.1.
Cite both by number and name; neither is repeated or re-graded here.

| # | Check | Severity |
|---|---|---|
| 4.1 | **An endpoint with no authorization metadata at all.** `Find:` per controller, compare `grep -c "\[Http"` against `grep -c "\[HasPermission\|\[AllowAnonymous\|\[ApiKey"`, then read the actions wherever the counts differ. Check 2.1 says every action states its own answer; **this is why it fails open** — `GetFallbackPolicyAsync` returns `null`, so no policy applies to an unattributed endpoint. Protection is visible on the endpoint or it does not exist, and forgetting the attribute produces a working public endpoint with no error, no log line and a 200. In a sweep, compare every controller. A change making the provider return a policy is a house-wide behavioural change and its own finding: it silently protects endpoints that were deliberately anonymous · `auth-and-security`, `references/permission-internals.md` §2 | **CRITICAL** |
| 4.2 | **Several permissions on one attribute read as a conjunction.** `Find:` `grep -rn "\[HasPermission(" src/Web/` and flag every call listing two or more permission codes. The handler calls `HasAnyPermission…`: **several codes mean ANY, not ALL.** An author writing two codes to mean "both" has authorized strictly more than intended, it compiles, and it tests green for anyone holding either. A conjunction is not expressible through this attribute; it needs a different gate · `auth-and-security`, `references/permission-internals.md` §3 | **HIGH** |
| 4.3 | **Per-request principal verification that can silently stop running.** `Find:` `grep -rn "Type.GetType(" src/` and read the branch that follows. The principal's type is recovered by name from the token, and an unqualified name resolves only inside the assembly that asks — so if a principal entity moves, resolution yields `null`, the guarded block is **skipped entirely**, and every deleted or blocked principal is admitted with no exception and no log line. The remedy is to fail closed; a guard that merely skips is the finding. **Escalate any diff that moves or renames a principal entity on its own:** one string identifies a family across the minted claim, the selector arm, the grant rows and this resolution, nothing enforces agreement, and the change is a data migration whose fix names the row rewrite · `auth-and-security`, *Don't let a type name be the only thing holding the system together* + `references/principal-and-secrets.md` §2 | **CRITICAL** |
| 4.4 | **A grant write path that evicts nothing.** `Find:` `grep -rn "ModelPermission\|RolePermission\|ModelRole" src/` for write calls, and check each path either uses the sync verb or removes the affected key. Permission answers come from a cache with a **sliding** expiry — "unused for N minutes", not "at most N minutes old" — so an active principal refreshes the entry on every request and for the busiest account a revocation may never take effect. **The sync verb validates the whole set and evicts its key; revoke validates only that the grant row exists, and evicts nothing.** A privilege-retention defect, not a caching one: a revocation that reports success and does nothing. Name the key that must be evicted — the principal's for a direct grant, the role's for a role's · `auth-and-security`, *Don't revoke a grant without evicting its cache* + `references/permission-internals.md` §7 | **HIGH** |
| 4.5 | **The auth stages out of order in the pipeline.** `Find:` open `src/Infrastructure/Startup.cs` and read `UseInfrastructure` as a sequence; in diff mode, `git diff <base>...HEAD -- src/Infrastructure/Startup.cs`. The shipped order is **static files → routing → APM → CORS → exception handler → authentication → current-user → principal verification → authorization**, with health, jobs and document stages after it. Each stage depends on the one above: before routing the anonymous check has no endpoint metadata and silently never applies; verification before the current-user seam has nothing to verify; after authorization, a blocked principal's permissions are evaluated before anyone notices. A diff that *moves* a line here is behavioural, not cleanup — that ruling is `dotnet-architecture-review` check 5.3, which names CORS explicitly. Ask for the intended order, never for a revert · `auth-and-security`, `references/principal-and-secrets.md` §2 *Ordering* + `facade-module-architecture`, `references/composition-root.md` | **CRITICAL when a stage moves above the one it depends on; HIGH otherwise** |
| 4.6 | **An anonymous endpoint that trusts the current principal.** `Find:` `grep -rn "\[AllowAnonymous\]" src/Web/`, then read the service each action calls for uses of the current-principal seam. **Anonymous endpoints leave the principal unset by design**, even when a valid token was sent: population is skipped when the endpoint carries anonymous metadata, so the accessors answer with an empty id. Code branching on that is not reading "no user" — it is reading a value matching no row, and any path treating the empty case as permissive is open to everyone. On a **new** anonymous endpoint, record the decision as INFO even when it passes · `auth-and-security` Principle 7 + `references/principal-and-secrets.md`, *Populating it* | **HIGH** |
| 4.7 | **Authorization or standing decided from a claim.** `Find:` `grep -rn "FindFirst\|HasClaim\|User\.Claims" src/` and discard the hits inside the auth facade's own claim-reading extension. **A permission claim in a token is decoration.** A token is evidence of a past login, never of current standing: what the caller may do comes from grant rows, whether they are allowed in at all from the principal's row. Reading a claim for either decision authorizes against a snapshot that may be days old and cannot be revoked · `auth-and-security` Principles 6 and 7 | **HIGH; CRITICAL when it is the only gate** |
| 4.8 | **A signing key shared between client families.** `Find:` open the security configuration and compare every scheme's `Key` and `RefreshKey` for equality; in a diff, check whether a new scheme copied an existing block. Then `grep -rn "AddJwtBearer(" src/Infrastructure/` and check each registration passes its own settings block. With issuer and audience validation deliberately off, **the signing key is the entire boundary between client families** — two families sharing one means a token minted for the low-privilege family validates on the high-privilege one, a cross-family bypass no test will show because both tokens are valid. Also flag a new scheme missing any of its sites (constant, settings property, configuration block, bearer registration, selector arm): miss the selector arm and that family's tokens go to the default scheme, a silent 401 for one client only · `auth-and-security` Principle 2 + `references/jwt-and-tokens.md` §3 | **CRITICAL** |
| 4.9 | **`[ApiKey]` misapplied.** `Find:` `grep -rn "\[ApiKey" src/Web/` and, per hit, check the same action for `[HasPermission]`; then open the filter and read its comparison. Three shapes in one grep. It is an **MVC authorization filter, not a scheme**: it establishes no principal, so `[HasPermission]` beside it has nothing to check and authorizes nothing while looking as if it does — and for the same reason it cannot gate a caller acting on behalf of a person, since only a token produces a principal. And the comparison must be `CryptographicOperations.FixedTimeEquals`, never `==`; a naive comparison leaks the key's prefix through timing, with the known limit that unequal lengths return immediately, so length stays distinguishable · `auth-and-security`, `references/principal-and-secrets.md` §4 | **HIGH; CRITICAL when paired with `[HasPermission]` or when it is the only gate on a person-acting caller** |

**Four things that look like findings here and are not.** Each is house law, and
reporting one costs the reader's trust in every other finding:

- **`ValidateIssuer = false` and `ValidateAudience = false`.** Deliberate: these tokens
  are minted and consumed by the same service, and the separation between families is
  carried by a **distinct signing key per scheme**, not by a claim — which is what makes
  4.8 the real check. Do not recommend tightening one side alone: turning validation on
  while live tokens carry a different issuer invalidates every session at once.
- **`ClockSkew = TimeSpan.Zero`.** House law, and **stricter** than the advice flagging
  it: the framework default grants five minutes of grace, which makes a short
  access-token lifetime meaningless and expiry tests flaky. Raising it would be the
  finding; the zero is not, and neither is a resulting "token expired early" report.
- **`[HasPermission(schemes: …)]` with no permission codes.** A sanctioned call shape
  (`api-surface`) meaning *this family may reach this endpoint* — not the "bare
  authenticated-only attribute" generic guidance flags.
- **A bare 401 with no error envelope from the API-key filter.** Documented: rejection
  short-circuits past the exception middleware, so the body differs from every other
  error in the API. Worth knowing before putting the filter on a public surface, but
  **not** an `error-handling` defect — authorization short-circuiting is not error
  handling. The findings that live there are 4.9's.

### 5 — CORS

**Say the limit before the checks.** The house legislates exactly two things about CORS
— where the policy is registered and where it runs. **No shipped body states which
origins are allowed, whether a wildcard is acceptable, which methods or headers may be
exposed, or how development and production policies should differ.** This layer checks
placement, pairing and pipeline position plus one universal defect; an origin list is
**INFO with the question stated**, never a finding. Inventing the missing policy inside
a review is exactly what `dotnet-code-review` Principle 5 forbids.

| # | Check | Severity |
|---|---|---|
| 5.1 | **`UseCorsPolicy()` at the wrong position.** `Find:` read the `UseInfrastructure` chain (same file as 4.5). As shipped, `UseCorsPolicy()` sits after routing and APM and **directly above** `UseExceptionHandlerMiddleware()`. Two failures. **Below the endpoint stages**, the preflight response is produced by code the browser never reaches correctly. **Below the exception handler**, CORS moves *inside* that handler's `try` — and the shipped design is the opposite: the exception middleware itself and CORS run **outside** the `try`, and a failure there is the host's to answer. Moving it changes which failures return the standard envelope: a contract change disguised as a reordering · `facade-module-architecture`, `references/composition-root.md` + `error-handling`, `references/middleware-behavior.md` + `dotnet-architecture-review` check 5.3 | **HIGH** |
| 5.2 | **CORS configured outside the composition root.** `Find:` `grep -rn "AddCors\|AddPolicy\|WithOrigins" src/Web/ src/Infrastructure/` and discard the hits inside the CORS facade and its call sites in `Infrastructure/Startup.cs`. The policy is registered once, in the facade, reached through `AddCorsPolicy` in the `AddInfrastructure` chain. A second policy beside a controller means two places decide one browser-visible contract, and only one is where anyone looks · `facade-module-architecture`, `references/composition-root.md` + `dotnet-architecture-review` check 3.3 | **MEDIUM** |
| 5.3 | **An origin wildcard combined with credentials.** `Find:` `grep -rn "AllowAnyOrigin\|SetIsOriginAllowed" src/` and read the same policy builder for `AllowCredentials`. **The two forms are not the same defect and must not be graded together.** `SetIsOriginAllowed(_ => true)` with `AllowCredentials()` reflects the caller's origin back, the browser accepts it, and any site on the internet can make credentialed requests as the logged-in user — and it is the form that actually ships, because it is what an author reaches for when the literal wildcard "stopped working". The literal `AllowAnyOrigin()` with `AllowCredentials()` is rejected by browsers per the CORS specification, so it grants nothing: report that one as a **misunderstanding to resolve** — an explicit origin list, or drop credentials — not as an exploit · universal | **CRITICAL for a reflected origin; MEDIUM for the literal wildcard** |

### 6 — Data protection and exposure

**6.1 Mass assignment — a request surface wider than the operation** — *HIGH; CRITICAL
when the extra property decides ownership, price, status or permission* · `api-surface`,
*Binding sources* (shape a) + universal (shape b)

`Find:` open each action touched by the diff and read every parameter; then, per request
type, list its properties and `grep -rn "CreateMap<<X>Request" src/` to read what the
entity receives.

Two shapes, and their grounding differs — say which you found.

- **(a) A parameter with no binding-source attribute.** `api-surface` requires every
  parameter to state its source — `[FromRoute]`, `[FromQuery]`, `[FromBody]`,
  `[FromForm]` — with `CancellationToken` the only exception. Without it the binder
  decides, and the accepted surface becomes whatever inference produces rather than what
  the signature reads as. **Older endpoints relying on inference are pre-existing and
  score INFO; new ones do not.**
- **(b) An entity, or a request carrying properties the caller must not set — an owner
  id, a status, a role, a price, an `Id` — bound from the body; or one shared request
  class serving create and update.** Stamped **universal**: no shipped body forbids
  over-posting in those words, and it is rare here because the house shape is a
  purpose-built request type per operation. The finding is the departure from that
  shape; the fix is a narrower request type — not ignoring the property in the profile,
  which leaves the next author's `CreateMap` to re-introduce it, and not a downstream
  guard, which the next author will forget.

**6.2 A response property hidden by a comment** — *HIGH; CRITICAL when the value is a
credential, a token or another caller's data* · universal + `api-surface`

`Find:` `grep -rn -B3 --include=*Response*.cs "internal\|do not return\|not returned\|hidden" src/`
and read each hit against the property it annotates; independently, serialize one
response and compare the JSON against the class.

```csharp
// BAD — the only thing hiding this property is a sentence. The serializer does not
// read comments, and every response carries the value to every caller.
public class EntityBaseResponse
{
    public Guid Id { get; set; }

    /// <summary>Internal only — not returned in JSON.</summary>
    public string InternalReference { get; set; } = default!;
}
```

```csharp
// GOOD — the wire contract carries only what is on the wire.
public class EntityBaseResponse
{
    public Guid Id { get; set; }
}
```

A response type **is** the published contract; a comment is a note to the next
developer, and the two are not the same mechanism — the gap is invisible in review
precisely because the comment reads as authoritative. Treat any "internal", "not
exposed" or "do not return" annotation on a public response member as a finding, and
check the whole response *family* (see 6.5). The fix is that the property does not
belong on a response type at all: if an internal step needs the value, that step's type
is not the response. A serialization-ignore attribute would also suppress it, but it
leaves the exposure one attribute-deletion away and **no shipped body legislates it** —
raise it as the author's decision, not as the recommended fix.

| # | Check | Severity |
|---|---|---|
| 6.3 | **Redaction relied on as a security control.** `Find:` open the error-response settings in the base configuration and in every environment overlay and read the hidden-property list; then check each configured name against the wrapper's actual property names, character for character. Three failures. An environment whose overlay carries **no** hidden set returns full diagnostics — source, method, exception text, line — to every caller on every 500. A configured name that **does not match** a wrapper property fails silently: the settings are read straight from `IConfiguration` inside the `catch` and the class is never registered with the options pattern, so there is no binding and no validation on start — the reflection lookup finds no match, nothing fails at boot, and the field ships unredacted. And decisively: **`Message` is never in the hidden set, and on the unexpected-exception path it is the raw exception's own text** — redaction removes the stack and the type name, never the sentence. A driver's error string describing the schema reaches the caller through the one field redaction cannot touch, which is why the fix is upstream: throw a leaf carrying a written message, not a longer hidden list · `error-handling`, `references/middleware-behavior.md`, *Redaction — `ErrorResponseSettings`* | **MEDIUM; HIGH when the Production overlay is missing entirely** |
| 6.4 | **A credential or personal datum written to a log.** `Find:` run `dotnet-code-review` check 2.6's greps, then add `grep -rn "GetTokenAsync\|Request.Headers\|HttpContext.User\b" src/` and read what each result reaches. Check 2.6, *Sensitive data in a log line or in a message a caller reads*, owns the rule — **a log line is an export**, retained longer, replicated wider and read by more people than the database, so a token or request body in a log has a larger audience than the row it came from. One house-specific route it does not name: **the bearer registrations set `SaveToken = true`** (universal consequence of a shipped setting), so the raw token is retained on the authenticated request. Anything logging the authentication properties, dumping the principal, or serializing incoming headers wholesale is exporting a live credential, not merely a user identifier · `dotnet-code-review` 2.6 | **HIGH** |
| 6.5 | **A property that reaches the wire because of where it sits.** `Find:` run `dotnet-code-review` check 2.7's greps; then, for every response type touched in the diff, `grep -rn "class \w*Response *: <ThatType>" src/` and read the responses inheriting from it. Check 2.7, *An entity reaching the wire*, covers the entity itself. This layer's addition is the **inherited** disclosure: the response family is a ladder, so a property added to a rung appears on every response below it — including ones the author never opened, in modules they do not own, on endpoints reachable by other client families. A one-property diff to a base rung is a multi-endpoint disclosure, and reviewing the changed file alone cannot see it. Check the ladder, not the file · `dotnet-code-review` 2.7 + `api-surface` | **HIGH** |
| 6.6 | **The API document published or unguarded.** `Find:` read the `Enable` value in each environment's effective configuration; then open the document facade's pipeline extension and read the order of the basic-auth registration against the UI registration. Three failures, one file. **`Enable` gates the entire block** — document and UI alike — and is the switch for an environment that must not publish its API surface; it is configuration, not a code change. The **basic-auth middleware registered after `UseSwaggerUI` never runs**, which looks correct in the diff and protects nothing, and it must be guarded by `!Environment.IsDevelopment()`. And a **`RoutePrefix` changed without the middleware's matching prefix** moves the UI out from behind its own gate — the shipped middleware reads the prefix from the same settings for exactly this reason, so a hand-written prefix anywhere is the finding. The credentials themselves are 2.5. A published document is not a vulnerability by itself; it is a complete, current map of every endpoint, parameter and schema handed to whoever asks · `api-surface`, `references/openapi-swashbuckle.md` | **HIGH; CRITICAL when the UI is reachable unauthenticated in a deployed environment** |

**Not findings here.** A **response family rooting at the shared base type** is correct —
`dotnet-code-review` check 2.7 says so explicitly, and check 5.12, *A response that is a
sibling instead of a rung*, owns the defect that actually lives in that family. An
**empty hidden-property list in the *base* configuration** is by design: full
diagnostics are wanted outside production and the overlay carries the set. A **4xx with
no trace id or support message** is by design — diagnostics exist only on the 500 paths.
And a **test fixture, seed value or `appsettings.Development.json` value** is not
production exposure; see below.

## Severity calibration

The four words and their general meanings are `dotnet-code-review`'s — Principle 3, *One
severity vocabulary, four words*, and its *Severity ladder*. This rubric does not restate
them; it calibrates them, because security findings are argued from category rather than
consequence and, left uncalibrated, every one argues its way to CRITICAL and the word
stops carrying information.

| Severity | In a security finding |
|---|---|
| **CRITICAL** | Exploitable **now**, by someone not already trusted with the result: an authorization gate that does not run, key material an attacker can read, caller data reaching an interpreter, a cross-family authentication bypass, a fail-open seam on the authenticated path. The test is not how bad the outcome is but whether anything today stands between an attacker and it. |
| **HIGH** | A real weakening with a precondition attached — it needs a valid token, an internal position, a specific environment, or a second defect. Also: the correct control present but placed where it cannot run. |
| **MEDIUM** | Hardening, or a control whose failure mode is confusing rather than exploitable — an unverifiable redaction name, a duplicated policy registration, a wildcard the browser already rejects. |
| **INFO** | A surviving call site recorded for the next review, a deliberate anonymous endpoint documented, a pre-existing exposure noticed in diff mode, a question no shipped body settles — every CORS origin question lands here — or a protection done well. |

Four calibrations settle the arguments this rubric actually gets:

- **Reachability escalates; unreachability does not delete.** A defect reachable from an
  **unauthenticated** path moves up one rung; one with no current caller moves down one
  rung and is still reported, because reachability is a property of today's routes and
  routes change in one line. State the path in the finding.
- **Test fixtures, seed data and `appsettings.Development.json` values are not HIGH.**
  They are INFO, or MEDIUM where the same value is also used in a deployed environment.
  The one exception is a **real** credential parked in any of them: 2.1 at full severity,
  because the repository does not know which file it is in. Grading development values
  HIGH is how a report loses its reader for the findings that matter.
- **Severity is consequence, not effort** — `dotnet-code-review`'s rule, unchanged. A
  one-character fix to a missing gate is CRITICAL; a quarter-long secret-management
  migration whose absence exposes nothing today is MEDIUM.
- **A missing XML comment is never a security finding.** Nor is naming, casing,
  formatting, or a missing `ProducesResponseType`. `api-surface` owns the documentation
  rule and `dotnet-code-review` Principle 4, *Style is reviewed last, or not at all*, the
  rest. A style nit inside a security report reads as padding and discredits the findings
  above it.

## The report

One report, the severity words as headings, always in this order. **Every section appears
every time**; write `None.` when a section is empty, because an absent section is
ambiguous between *checked, found nothing* and *did not check* — and in a security review
that ambiguity is the whole distinction.

```markdown
## Security review: <scope>

> This is static analysis, not a penetration test. It catches known patterns and
> house-doctrine violations; it does not catch business-logic flaws, complex
> authorization bypasses, or anything that only exists at runtime.

### Summary
<mode (diff or sweep) · layers run · PASS / FAIL and the findings that decide it>

### CRITICAL
- **<title>** — `<file>:<line>` · check <n.n>
  <what is exposed> · <the reach: unauthenticated / any caller / another family / admin> · <why it matters> · <the change that closes it> · <owning skill>

### HIGH
- **<title>** — `<file>:<line>` · check <n.n> …

### MEDIUM
- **<title>** — `<file>:<line>` · check <n.n> …

### INFO
- **<title>** — `<file>:<line>` · check <n.n> …

### Layer coverage
1 Packages · 2 Secrets · 3 Injection · 4 Auth posture · 5 CORS · 6 Data protection
<ran / skipped and why, per layer>

### Suppressions applied
<house-doctrine patterns seen and deliberately not reported, one line each — or `None.`>

### What's Good
- <the protections worth repeating>
```

Five rules for the findings themselves:

1. **The disclaimer is verbatim and it is not moved.** It sits above the Summary, before
   the first finding, in the words above. A report that buries it has published a
   clearance; one that paraphrases it has softened the only claim it is obliged to make.
2. **Name the exposure and the reach, and carry `file:line`.** "Insecure configuration"
   is not a finding; "an unauthenticated caller can read every field of the error
   envelope including the exception text, at `<file>:<line>`" is. The two clauses are
   separate because a reader who agrees with the first and disagrees with the second is
   arguing about severity, not about the defect.
3. **Cite the check number and the owning skill.** The author argues with the rule rather
   than the reviewer, and a stale rule surfaces as a contradiction rather than a second
   opinion. A finding citing nothing is this rubric inventing doctrine under a security
   banner, the one thing it may not do. Where a check is `universal`, say so — that is a
   citation too.
4. **`FAIL` is decided by CRITICAL and HIGH only** — the rule
   `dotnet-architecture-review` uses: if MEDIUM failed the gate, every report would FAIL
   and the verdict would stop meaning anything. A MEDIUM-only report is `PASS`, and the
   Summary says how many hardening findings it carries.
5. **`Suppressions applied` is not optional when the layer ran.** Naming the
   house-doctrine patterns you deliberately did not report tells the reader you opened
   the JWT block and *decided* — otherwise the next reviewer raises them, and the one
   after that.

**Layer coverage is not optional.** A review that ran only layers 1 and 2 is a useful
report; one that ran only layers 1 and 2 and does not say so is misleading, because a
reader takes a silent layer for a clean one.

## Routing

**Deep dives — sibling rubrics.** This rubric owns exposure. When the change's risk lives
elsewhere, load that one instead of stretching this.

| The change is mostly about | Load |
|---|---|
| Blast radius, severity of behavioural findings, slop; the posture checks 2.1–2.7 and raw SQL 1.7 | `dotnet-code-review` |
| Layering, dependency direction, placement, the composition root's shape | `dotnet-architecture-review` |
| Query cost, allocation, blocking, missing indexes — including a rate-limiting or DoS question | `dotnet-performance-review` |

**Doctrine — the owning knowledge skill.** This rubric notices the disagreement; the rule
itself lives here.

| The finding is about | Owning skill |
|---|---|
| JWT schemes, signing keys, token flows, permission internals, the principal seam, API keys, auth settings and secrets | `auth-and-security` |
| Endpoint attributes, binding sources, request and response DTO shape, the OpenAPI document | `api-surface` |
| The error envelope, redaction, which exception carries which status | `error-handling` |
| Pipeline position, where a policy is registered, the composition root | `facade-module-architecture` |
| Raw SQL as an escape hatch, entity configuration, migrations, seeding | `ef-core-data-access` |
| Validation rules and guards behind an unvalidated value | `module-feature` |
| Faking a principal, the test authentication handler | `dotnet-testing` |
| The text of a message that reaches a caller | `message-keys` |
| Cache and search credentials and their settings files | `distributed-caching`, `elasticsearch-search` |
| Unsure which of the above owns it | `choosing-a-dotnet-skill` |

**Process.** Requesting the review and triaging what comes back belong to
`superpowers:requesting-code-review` and `superpowers:receiving-code-review`.
**Execution:** rotating a key is a configuration change per environment and closing a gate
an ordinary code change — both made normally, neither here; only the cleanup they leave
behind goes to `/simplify`.

## References

**Read `references/security-checks.md` when** running a sweep or a release gate, or when a
finding needs the long tail behind a layer — it continues each layer's numbering from
where this body stops, with no number reused, so a citation is unambiguous about which
file it came from.

## Decision Guide

| Situation | Do this |
|---|---|
| Asked for "a security review" with no scope | Declare the mode and the layers in the Summary; run all six unless the scope narrows them |
| A checklist flags `ValidateIssuer = false` or `ClockSkew = TimeSpan.Zero` | Not a finding. House law, and the skew is stricter than the advice flagging it. Check 4.8 is the real one |
| A secret is found in a committed file | CRITICAL, and the fix is **rotation**. A diff that only deletes the line has closed nothing — say so |
| A secret is in git history but not in the working tree | Still CRITICAL. History does not heal, and "pre-existing" does not apply |
| An endpoint has no authorization attribute | CRITICAL — the fallback policy is `null`, so it is public. Do not assume a global default protects it |
| One `[HasPermission]` lists two permission codes | HIGH by 4.2 — it means ANY, not ALL. Ask which was intended |
| An anonymous endpoint reads the caller's identity | HIGH — the principal was never populated, so the accessor answers with an empty id, not a real caller |
| A finding is about which origins CORS allows | INFO with the question stated. No shipped body settles it; only 5.3 is universal |
| A defect exists but nothing currently routes to it | Report it one rung lower. Never drop it — reachability changes in one line |
| The value is in a test fixture or a development overlay | INFO or MEDIUM, unless it is a real credential, which is 2.1 at full severity |
| A finding could be graded here or by `dotnet-code-review` section 2 | Grade it once. If 2.x owns the shape, cite it by number and name and add only the deepening |
| A layer cannot run | Say so under *Layer coverage*. Never let a skipped layer read as a clean one |
| Generic advice suggests a check with no shipped owner — XSS in views, a secret store, a CORS origin policy | Not a finding here. Say what is not covered rather than inventing the rule |
| A convention the code breaks is stated in no shipped body | INFO with the question stated. This rubric has no doctrine of its own |
| Everything passes | Say PASS, write `None.` into each empty section, keep the disclaimer, keep *Layer coverage* honest, and name what the code got right |
