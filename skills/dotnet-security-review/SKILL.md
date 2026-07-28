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
*what an attacker could reach, read or forge*, in what order, and how to rank what
it finds. It does not run the review — gathering the diff, dispatching the
reviewer, receiving and triaging the feedback belong to
`superpowers:requesting-code-review` and `superpowers:receiving-code-review`. It
does not apply the fixes either: rotating a key is a configuration change and
closing a gate is an ordinary one.

**It checks conformance; it does not define it.** The auth layer is defined by
`auth-and-security` and its three `references/` files, the HTTP surface and the
document by `api-surface`, the error envelope by `error-handling`, pipeline
position by `facade-module-architecture`. Lines quoted here are the target being
compared against, never a second statement of the rule — same law as
`dotnet-code-review` Principle 5, *The rubric cites the owning skill; it never
re-teaches it*. Two consequences:

- **A check must trace to a shipped skill's body, or be a defect in any
  codebase.** A check that is neither is refused, not softened. Where a whole area
  has no shipped owner, this rubric says so out loud rather than inventing doctrine
  under a security banner — security is exactly the subject where invented rules
  sound most plausible.
- **Cite by number and name** — the check number here, and the owning skill's
  section — so the author argues with the rule rather than with the reviewer.

**House doctrine outranks generic security advice, and the two disagree here.**
Standard hardening guides demand `ValidateIssuer`/`ValidateAudience` be `true` and
treat a bare authenticated-only attribute as a finding. This stack deliberately
does the opposite, for a stated reason. A report that raises settled house design
as a vulnerability is worse than a report with a gap: the author learns the whole
document can be ignored. Read *Not a finding* in layers 2, 4 and 6 before writing
any of them.

**Every check is a manual instruction.** There is no code-analysis server, no
Roslyn tooling and no security scanner in this stack, and no step below may assume
one. Each check is a `grep -rn --include=*.cs` to run, a file to open and read, or
a command whose output you read — and one of them (`dotnet list package`) is the
only automation this rubric has. Where other security material says *"find all
references to `[AllowAnonymous]`"*, here that is a grep, and **the degradation is
not lossless**: a grep sees an attribute written in a file, never one applied by
convention, inherited, or composed at runtime. Check 4.1 compares two counts rather
than reading one list precisely because absence is what a grep is worst at seeing.

**Two modes, and say which** — diff or sweep, scoped exactly as
`dotnet-architecture-review`, *Two modes, and say which*. Diff is the default. A
pre-existing exposure in a touched file is INFO unless the change makes it
reachable or worse; in a sweep everything scores normally. One security-specific
addition: **a secret that was ever committed is never "pre-existing"** — history
does not heal, so it scores full severity in both modes.

**Scope: posture, not breadth.** Reach for this rubric when the change *is* the
security surface — auth wiring, a settings file, a new endpoint's authorization, a
response contract, a logging change, a package bump — or when a release gate or an
inherited codebase needs a sweep. `dotnet-code-review` section 2, *Security
posture*, is the breadth pass; it routes here when security is what the change is
mostly about. **That section is not restated here.** Checks 2.1–2.7 and 1.7 keep
their home and their numbers, and every seam below cites them.

## Core Principles

1. **Every report opens with the honesty rule, verbatim, in these words:** *This is
   static analysis, not a penetration test. It catches known patterns and
   house-doctrine violations; it does not catch business-logic flaws, complex
   authorization bypasses, or anything that only exists at runtime.* A security
   report that does not bound itself gets read as a clearance and the reader stops
   looking — the sentence is the finding the report itself cannot make, which is why
   it is quoted rather than summarised and why report rule 1 forbids moving it.

2. **A missing gate is worth more attention than a weak one.** The house's
   authorization is opt-in: the policy provider returns `null` from
   `GetFallbackPolicyAsync`, so an endpoint with no authorization metadata is
   simply not protected, and nothing logs it (`auth-and-security` →
   `permission-internals.md` §2). Absence is invisible in a diff and invisible at
   runtime. Spend the pass on what is *not there*.

3. **Fail-open is the shape worth hunting.** Most defects announce themselves: a
   request fails, a test goes red. The ones this rubric exists for do the opposite
   — a null the framework accepts as "no policy", a type name that stops resolving,
   a cache entry that never lapses. Nothing throws, nothing logs, the endpoint
   answers 200. When reading auth code the question is never "does this check
   work?" but **"what happens when this check cannot run?"**

4. **A suppression is content, not politeness.** The *Not a finding* blocks below
   are as binding as the checks. Reporting `ValidateIssuer = false` or
   `ClockSkew = TimeSpan.Zero` as a vulnerability is a defect in the review, and it
   costs more than it saves: every real finding in the same report is now read as
   possibly-noise.

5. **Name the exposure and the reach.** "This is insecure" is not a finding; "an
   unauthenticated caller reaching `POST /x` can set `Y`" is. Every finding says
   what an attacker obtains and from what position — unauthenticated, any
   authenticated caller, a caller of another family, an administrator. Severity is
   decided by reachability, so a finding that cannot name who reaches it cannot be
   graded, and an ungraded security finding is an opinion with an alarming
   adjective on it. The remediation is a specific change; for key material it is
   **rotation**, never deletion of the line.

## The layers, in order

Six layers, each catching a different class. **Run all six, in order, and report
coverage.** The order is by cost, not by risk: layer 1 is one command, layer 6 is
judgement on every response the change touches. A CRITICAL in an early layer never
excuses skipping a later one — a patched CVE list says nothing about a leaking DTO.

| # | Layer | Unit | Answers |
|---|---|---|---|
| 1 | Packages | the restored package graph | Is a known vulnerability already in the build? |
| 2 | Secrets | committed files | Is key material in the repository, or is its contract broken? |
| 3 | Injection and unsafe input | call sites taking caller data | Does request data reach an interpreter or a path? |
| 4 | Auth posture | the auth facade and every endpoint | Is every gate present, ordered, and closed on failure? |
| 5 | CORS | the CORS facade and the pipeline | Is the policy applied where the house says? |
| 6 | Data protection and exposure | responses, logs, the document | What leaves the process, and to whom? |

**Scoping a partial run** — after a dependency bump, layer 1; a new or changed
endpoint, 4 and 6; an auth change, 4; a configuration change, 2 and 5; a release
gate, an inherited codebase or an incident, all six. Whatever ran, *Layer coverage*
says so; a two-layer review is useful, a two-layer review that does not say so is
misleading.

Checks are numbered **per layer and never reused**; `references/security-checks.md`
continues each layer's numbering where the body stops, so `4.11` means one thing in
this skill and nowhere else. Paths are written for the standard layout
(`src/Core/`, `src/Infrastructure/`, `src/Web/`, `tests/`); resolve the real roots
from the `.sln` before the first grep, because a path that does not exist returns
nothing and an empty result reads exactly like a clean pass. Patterns assume
`grep -rn --include=*.cs` unless stated.

### 1 — Packages

**1.1 A dependency with a published vulnerability** — *severity from the advisory*
· universal
`Find:` `dotnet list package --vulnerable --include-transitive`

The flag is not optional: most advisories land on a package no `.csproj` names, so
a clean direct list is not a clean solution. Take the severity from the advisory
rather than inventing one, name the package and the path that pulls it in, and give
the patched version as the fix. Where no patch exists, the finding is the risk plus
the compensating control, and it is still a finding. Then answer the one question
the tool cannot: **is the vulnerable code path reachable from this service?** A
deserialization advisory in a library used only by a build task is INFO; the same
advisory in the request path is its advisory severity or higher.

**The body's half of this layer is one command, and the honest limit is that the
house legislates nothing else about packages.** There is no shipped pinning policy, no
approved-package list, no supply-chain doctrine. Two adjacent rules exist and
belong to their owners: a new *test* package is a decision, not a detail —
`dotnet-code-review` check 6.8, *A new or unreviewed test package* — and central
version management is `facade-module-architecture`'s. Do not grow this layer past
what a shipped body actually says. If the command cannot run (no restore, no feed),
say so under *Layer coverage* rather than reporting a clean layer.

### 2 — Secrets

Deepens `dotnet-code-review` check 2.5, *A secret in source or in a committed
settings file*, whose grep is the entry point — run it first and do not restate it.
What follows is what that check does not reach: the fix, and the contract the
committed file is supposed to carry.

**2.1 Key material in a committed configuration file** — *CRITICAL* ·
`auth-and-security`, *Don't commit key material*
`Find:` open the security configuration topic and every `<topic>.json` /
`<topic>.<Environment>.json` beside it; read every value a settings class declares
as a key, password or connection string. A UUID, a base64 run, or any string that
is not visibly a `<description>` placeholder is a live key.
**The fix is rotation, not deletion.** A key present in committed history is
compromised regardless of later edits, so a diff that removes the line and nothing
else is the finding restated, not resolved — say so explicitly, and name the
rotation's blast radius: rotating an access signing key logs that family out
immediately, a refresh key ends their sessions at the next refresh.

**2.2 A committed settings block deleted instead of placeholdered** — *HIGH* ·
`auth-and-security`, `references/principal-and-secrets.md` §3, *The non-commitment
rule*
`Find:` in the diff, read every removed key in a committed `*.json`.
**A placeholder is not a missing value — it is the contract**: the committed file
is what tells a new environment which keys to supply. Emptying the block hides the
requirement, and the next deployer discovers it by reading C#. The shape is the key
present with a descriptive placeholder and the non-secret lifetimes beside it.

**2.3 A settings root bound without startup validation** — *HIGH* ·
`auth-and-security`, `references/principal-and-secrets.md` §3, *Settings and
secrets*
`Find:` `grep -rn "AddOptions<" src/Infrastructure/` and check each chain reaches
`ValidateDataAnnotationsRecursively()` **and** `ValidateOnStart()`; recursive
matters, because the validated property is usually on the nested per-scheme class,
not the root.
This is a security check, not a configuration one: **placeholder-as-contract and
startup validation are one design.** Without `ValidateOnStart`, a placeholder that
reaches production fails at the first 401 in production instead of at boot — and
nobody connects the two events.

**2.4 Credentials in an infrastructure configuration topic** — *CRITICAL* ·
`elasticsearch-search` + `distributed-caching`
`Find:` `grep -rniE "\"(password|pwd|username|apikey|key|connectionstring)\" *:" src/Web/Configurations/`
Both owners state the same rule in their own words — **credentials never go in this
file**; environment variables load last and beat every JSON file, and that is where
a deployed username and password come from. Same remediation as 2.1: rotate. This
is separate from 2.1 because the auth files get looked at and the search and cache
topics do not.

**2.5 Document-UI credentials in a committed topic** — *CRITICAL* · `api-surface`,
`references/openapi-swashbuckle.md` + universal
`Find:` open the document's settings topic and read its credentials block.
Outside Development the basic-auth middleware is the only thing standing between
the internet and a complete map of the API surface, so its username and password
are the highest-leverage credential the committed tree can hold. The gate itself is
check 6.6.

**Not findings here.** A **placeholder** in a committed file — `<access-token
signing key>`, `changeme`, an empty string — is the contract working as designed,
and `ValidateOnStart` turns one reaching a real environment into a boot failure. A
**`UserSecretsId` in a `.csproj`** is the fix, not the problem. A **non-secret in
the committed file** — a lifetime, a node URI, an index prefix — belongs there;
lifetimes in particular are reviewable decisions the owning skill puts in the
committed file on purpose. And a value in **`appsettings.Development.json` or a
test fixture** is not HIGH; see *Severity calibration*.

### 3 — Injection and unsafe input

**3.1 Raw SQL reachable from request data** — *CRITICAL* · `dotnet-code-review`
check 1.7, *Raw SQL carrying request data* (universal)
`Find:` `grep -rn "FromSqlRaw\|ExecuteSqlRaw\|SqlQueryRaw" src/ tests/`
Check 1.7 owns the rule and the fix — parameterise, or use the interpolated
overload. **Cite it; do not restate it and do not re-grade it.** Four sites it does
not reach, which are this layer's:
- **Widen the grep past the module folders** to the whole solution including
  migrations and seeding — 1.7 runs on the change, this layer runs on the surface.
- **Which endpoint reaches each surviving call, and whether that endpoint is
  anonymous.** Record surviving call sites as INFO with their reachability even
  when they parameterise correctly; that inventory is what makes the next review
  cheap, because a parameterised call is still the site where the next edit
  introduces concatenation.
- **A column or sort direction built from request text.** Parameters cannot
  parameterise an identifier, so the only fix is an allow-list mapping the caller's
  token to a constant. A reviewer who says "parameterise it" here is wrong, and the
  author will discover that after merging.
- **A `LIKE` pattern assembled from caller input** is not injection but is a
  denial-of-service surface; rank it MEDIUM and say which of the two it is.

**3.2 Insecure deserialization** — *CRITICAL* · universal
`Find:` `grep -rn "BinaryFormatter\|TypeNameHandling\|SoapFormatter\|LosFormatter\|new JavaScriptSerializer" src/`
A payload that names its own type turns deserialization into arbitrary object
construction. No shipped body legislates a serializer, so this is stamped universal
and the finding is the reachability: caller-supplied bytes, or bytes from a store a
caller can write.

**3.3 A weak or hand-rolled cryptographic primitive** — *HIGH* · universal
`Find:` `grep -rn "MD5\|SHA1\|DESCryptoServiceProvider\|CipherMode.ECB\|new Random(" src/`
Flag only where the value is a *security* value — a password, a token, a signature,
a nonce. **A weak hash used as a cache key, an ETag or a shard selector is not a
finding**; say which one you found before writing it up. The house prescribes no
cryptography beyond the JWT layer, so the finding names the exposure and asks for
the algorithm decision rather than dictating one.

**3.4 A path or process argument built from request data** — *HIGH; CRITICAL when
the path is read or written without further checks* · universal
`Find:` `grep -rn "Path.Combine\|File.Open\|File.Read\|File.Write\|new FileStream\|Process.Start" src/`
and read each argument back to its source.
A request-supplied filename containing `..` escapes the intended directory, and
`Path.Combine` discards everything before an argument that is rooted — a value like
`/etc/x` or `C:\x` silently becomes the whole path. The same shape reaching a
process argument is command execution. The fix is a generated name plus a
resolved-path containment check, not a blacklist of `..`.

**3.5 A request value used before it is validated** — *HIGH* · `dotnet-code-review`
check 2.3, *A request value used before it is validated*
Run it as written. The security-specific escalation: when the unvalidated property
is the one that reaches 3.1, 3.4 or 6.1, it is the same finding as that one —
report it once, at the higher severity, and name both.

> **Out of scope, honestly.** Generic .NET security material checks **XSS and
> output encoding in server-rendered views** (`@Html.Raw`, Razor, Blazor). **This
> stack renders no server-side views** — `api-surface` settles Controllers
> returning JSON wrappers, and there is no view engine for the defect to live in.
> It is therefore not checked here and its absence from a report is not a gap. If a
> response field carries caller-supplied markup that a browser client renders, the
> escaping decision belongs to that client; the API-side finding is 6.5, an
> unfiltered value on the wire, not XSS. **Access-control defects at the query
> level — an operation not scoped to the caller — are `dotnet-code-review` check
> 2.4, *An operation not scoped to the caller*.** Run it, cite it, do not renumber
> it into this layer.

### 4 — Auth posture

The deepest layer, the one with the most shipped doctrine behind it, and the one
where fail-open lives. It deepens two checks it does not own: run
`dotnet-code-review` check **2.2, *`[HasPermission]` written positionally*** first —
it is the highest-value single grep in either rubric, it compiles, it looks
protective, and it authorizes nothing — and check **2.1, *An action with no
explicit authorization decision***, whose count-comparison is the entry point for
4.1. Cite both by number and name; neither is repeated or re-graded here. What
follows is the mechanism behind them and the seams neither reaches.

**4.1 An endpoint with no authorization metadata at all** — *CRITICAL* ·
`auth-and-security`, `references/permission-internals.md` §2
`Find:` per controller, compare `grep -c "\[Http"` against
`grep -c "\[HasPermission\|\[AllowAnonymous\|\[ApiKey"`, then read the actions in
any file where the counts differ.
Check 2.1 says every action states its own answer. **This is why it fails open:**
`GetFallbackPolicyAsync` returns `null`, so the framework applies no policy to an
unattributed endpoint. Protection is visible on the endpoint or it does not exist —
forgetting the attribute produces a working, public endpoint with no error, no log
line and a 200 response. In a sweep, run the comparison over every controller, not
only changed ones. If a change makes the provider return a policy instead, that is
a house-wide behavioural change and its own finding: it silently protects endpoints
that were deliberately anonymous.

**4.2 Several permissions on one attribute read as a conjunction** — *HIGH* ·
`auth-and-security`, `references/permission-internals.md` §3
`Find:` `grep -rn "\[HasPermission(" src/Web/` and flag every call listing two or
more permission codes.
The handler calls `HasAnyPermission…`: **several codes mean ANY, not ALL.** An
author writing two codes to mean "both" has authorized strictly more than intended,
it compiles, and it tests green for anyone holding either. A conjunction is not
expressible through this attribute; it needs a different gate.

**4.3 Per-request principal verification that can silently stop running** —
*CRITICAL* · `auth-and-security`, *Don't let a type name be the only thing holding
the system together* + `references/principal-and-secrets.md` §2
`Find:` `grep -rn "Type.GetType(" src/` and read the branch that follows.
The principal's type is recovered by name from the token. An unqualified name
resolves only inside the assembly that asks, so if a principal entity ever moves,
resolution yields `null`, the guarded block is **skipped entirely**, and every
deleted or blocked principal is admitted — 200, no exception, no log line. The
shipped remedy is to fail closed: an authenticated request whose principal type
cannot be resolved is rejected, not waved through. A guard that merely skips is the
finding. **Escalate any diff that moves or renames a principal entity on its own:**
one string identifies a family across the minted claim, the selector arm, the grant
rows and this resolution, nothing enforces agreement, and the change is a data
migration whose fix names the row rewrite.

**4.4 A grant write path that evicts nothing** — *HIGH* · `auth-and-security`,
*Don't revoke a grant without evicting its cache* + `references/permission-internals.md` §7
`Find:` `grep -rn "ModelPermission\|RolePermission\|ModelRole" src/` for write
calls, and check each path either uses the sync verb or removes the affected key.
Permission answers come from a cache with a **sliding** expiry — "unused for N
minutes", not "at most N minutes old" — so an active principal refreshes the entry
on every request and for the busiest account a revocation may never take effect at
all. **The sync verb validates the whole set and evicts its key; revoke validates
only that the grant row exists, and evicts nothing.** This is a privilege-retention
defect, not a caching one, and it is a revocation that reports success and does
nothing. Name the key that must be evicted — the principal's for a direct grant,
the role's for a role's.

**4.5 The auth stages out of order in the pipeline** — *CRITICAL when a stage moves
above the one it depends on; HIGH otherwise* · `auth-and-security`,
`references/principal-and-secrets.md` §2 *Ordering* + `facade-module-architecture`,
`references/composition-root.md`
`Find:` open `src/Infrastructure/Startup.cs` and read `UseInfrastructure` as a
sequence; in diff mode, `git diff <base>...HEAD -- src/Infrastructure/Startup.cs`.
The shipped order is **static files → routing → APM → CORS → exception handler →
authentication → current-user → principal verification → authorization**, and the
health, jobs and document stages after it. Each stage depends on the one above:
before routing the anonymous check has no endpoint metadata and silently never
applies; verification placed before the current-user seam has nothing to verify;
placed after authorization, a blocked principal's permissions are evaluated before
anyone notices they are blocked. A diff that *moves* a line here is a behavioural
change, not a cleanup — that ruling is `dotnet-architecture-review` check 5.3, which
names CORS explicitly; cite it. Ask for the intended order, never for a revert.

**4.6 An anonymous endpoint that trusts the current principal** — *HIGH* ·
`auth-and-security` Principle 7 + `references/principal-and-secrets.md`, *Populating
it*
`Find:` `grep -rn "\[AllowAnonymous\]" src/Web/`, then read the service each action
calls for uses of the current-principal seam.
**Anonymous endpoints leave the principal unset by design**, even when a valid
token was sent: the middleware skips population when the endpoint carries anonymous
metadata, so the accessors answer with an empty id. Code that branches on that
value is not reading "no user" — it is reading a value that matches no row, and any
path treating the empty case as permissive is open to everyone. On a **new**
anonymous endpoint, record the deliberate decision as INFO even when it passes; an
anonymous endpoint decided silently is the one nobody revisits.

**4.7 Authorization or standing decided from a claim** — *HIGH; CRITICAL when it is
the only gate* · `auth-and-security` Principles 6 and 7
`Find:` `grep -rn "FindFirst\|HasClaim\|User\.Claims" src/` and discard the hits
inside the auth facade's own claim-reading extension.
**A permission claim in a token is decoration.** A token is evidence of a past login
and never evidence of current standing: what the caller may do comes from grant
rows, and whether they are still allowed in at all comes from the principal's row.
Code that reads a claim to make either decision authorizes against a snapshot that
may be days old and cannot be revoked.

**4.8 A signing key shared between client families** — *CRITICAL* ·
`auth-and-security` Principle 2 + `references/jwt-and-tokens.md` §3
`Find:` open the security configuration and compare every scheme's `Key` and
`RefreshKey` for equality; in a diff, check whether a new scheme copied an existing
block. Then `grep -rn "AddJwtBearer(" src/Infrastructure/` and check each
registration passes its own settings block.
With issuer and audience validation deliberately off, **the signing key is the
entire boundary between client families.** Two families sharing a key means a token
minted for the low-privilege family validates on the high-privilege one — a
cross-family authentication bypass no test will show, because both tokens are valid.
Also flag a new scheme missing any of its sites (constant, settings property,
configuration block, bearer registration, selector arm): miss the selector arm and
the new family's tokens are handed to the default scheme, which is a silent 401 for
that client only.

**4.9 `[ApiKey]` misapplied** — *HIGH; CRITICAL when paired with `[HasPermission]`
or when it is the only gate on a person-acting caller* · `auth-and-security`,
`references/principal-and-secrets.md` §4
`Find:` `grep -rn "\[ApiKey" src/Web/` and, per hit, check the same action for
`[HasPermission]`; then open the filter and read its comparison.
Three shapes in one grep. It is an **MVC authorization filter, not a scheme**: it
establishes no principal, so `[HasPermission]` beside it has nothing to check and
authorizes nothing while looking as if it does. For the same reason it cannot gate a
caller acting on behalf of a person — only a token produces a principal. And the
comparison must be `CryptographicOperations.FixedTimeEquals`, never `==`; a naive
comparison leaks the key's prefix through timing, and the known limit is that
unequal lengths return immediately, so length stays distinguishable.

**Four things that look like findings here and are not.** Each is house law, and
reporting one costs the reader's trust in every other finding in the report:

- **`ValidateIssuer = false` and `ValidateAudience = false`.** Deliberate: these
  tokens are minted and consumed by the same service, and the separation between
  client families is carried by a **distinct signing key per scheme**, not by a
  claim — which is what makes 4.8 the real check. Turning them on protects nothing
  here. Do not recommend tightening one side alone: turning validation on while
  live tokens carry a different issuer invalidates every session at once.
- **`ClockSkew = TimeSpan.Zero`.** House law, and **stricter** than the generic
  advice that flags it. The framework default grants five minutes of grace, which
  makes a short access-token lifetime meaningless and expiry tests flaky. Raising it
  would be the finding; the zero is not, and neither is a resulting "token expired
  early" report.
- **`[HasPermission(schemes: …)]` with no permission codes.** A sanctioned call
  shape (`api-surface`), meaning *this family may reach this endpoint*. It is not
  the "bare authenticated-only attribute" generic guidance flags.
- **A bare 401 with no error envelope from the API-key filter.** Documented:
  rejection short-circuits and does not pass through the exception middleware, so
  the body differs from every other error in the API. Worth knowing before putting
  the filter on a public surface, but it is **not** an `error-handling` defect —
  authorization short-circuiting is not error handling. The findings that do live
  there are the three in 4.9.

### 5 — CORS

**Say the limit before the checks.** The house legislates exactly two things about
CORS — where the policy is registered and where it runs. **No shipped body states
which origins are allowed, whether a wildcard is ever acceptable, which methods or
headers may be exposed, or how development and production policies should differ.**
This layer therefore checks placement, pairing and pipeline position plus one
universal defect, and an origin list is reported as **INFO with the question stated**
— never as a finding. Inventing the missing policy inside a review is exactly what
`dotnet-code-review` Principle 5 forbids.

**5.1 `UseCorsPolicy()` at the wrong position** — *HIGH* ·
`facade-module-architecture`, `references/composition-root.md` + `error-handling`,
`references/middleware-behavior.md` + `dotnet-architecture-review` check 5.3
`Find:` read the `UseInfrastructure` chain (same file as 4.5). As shipped,
`UseCorsPolicy()` sits after routing and APM and **directly above**
`UseExceptionHandlerMiddleware()`.
Two distinct failures. **Below the endpoint stages**, the preflight response is
produced by code the browser never reaches correctly. **Below the exception
handler**, CORS moves *inside* that handler's `try` — and the shipped design is the
opposite: the exception middleware itself and CORS run **outside** the `try`, and a
failure there is the host's to answer. Moving it changes which failures come back as
the standard envelope, which is a contract change disguised as a reordering. Check
5.3 owns the ruling that such a move is behavioural and names CORS explicitly.

**5.2 CORS configured outside the composition root** — *MEDIUM* ·
`facade-module-architecture`, `references/composition-root.md` +
`dotnet-architecture-review` check 3.3
`Find:` `grep -rn "AddCors\|AddPolicy\|WithOrigins" src/Web/ src/Infrastructure/`
and discard the hits inside the CORS facade and its call sites in
`Infrastructure/Startup.cs`.
The policy is registered once, in the facade, reached through `AddCorsPolicy` in the
`AddInfrastructure` chain. A second policy declared beside a controller means two
places decide one browser-visible contract, and only one of them is where anyone
looks.

**5.3 An origin wildcard combined with credentials** — *CRITICAL for a reflected
origin; MEDIUM for the literal wildcard* · universal
`Find:` `grep -rn "AllowAnyOrigin\|SetIsOriginAllowed" src/` and read the same
policy builder for `AllowCredentials`.
**The two forms are not the same defect and must not be graded together.**
`SetIsOriginAllowed(_ => true)` with `AllowCredentials()` reflects the caller's
origin back, the browser accepts it, and any site on the internet can make
credentialed requests as the logged-in user — CRITICAL, and it is the form that
actually ships, because it is what an author reaches for when the literal wildcard
"stopped working". The literal `AllowAnyOrigin()` with `AllowCredentials()` is
rejected by browsers per the CORS specification, so it grants nothing and exploits
nothing: report it as a **misunderstanding to resolve** — the author must choose an
explicit origin list or drop credentials — not as an exploit.

### 6 — Data protection and exposure

**6.1 Mass assignment — a request surface wider than the operation** — *HIGH;
CRITICAL when the extra property decides ownership, price, status or permission* ·
`api-surface`, *Binding sources* (shape a) + universal (shape b)
`Find:` open each action touched by the diff and read every parameter; then, for
each request type, list its properties and `grep -rn "CreateMap<<X>Request" src/` to
read what the entity receives.
Two shapes, and their grounding differs — say which one you found.
- **(a) A parameter with no binding-source attribute.** `api-surface` requires every
  parameter to state its source — `[FromRoute]`, `[FromQuery]`, `[FromBody]`,
  `[FromForm]` — with `CancellationToken` the only exception. Without it the binder
  decides, and the accepted surface becomes whatever inference produces rather than
  what the signature reads as. **Older endpoints relying on inference are
  pre-existing and score INFO; new ones do not.**
- **(b) An entity, or a request carrying properties the caller must not set — an
  owner id, a status, a role, a price, an `Id` — bound from the body; or one shared
  request class serving create and update.** Stamped **universal**: no shipped body
  forbids over-posting in those words, and it is rare here precisely because the
  house shape is a purpose-built request type per operation, so a property the
  caller must not set simply is not on the type. The finding is the departure from
  that shape, and the fix is a narrower request type — not ignoring the property in
  the profile, which leaves the next author's `CreateMap` to re-introduce it
  silently, and not a guard added downstream, which the next author will forget.

**6.2 A response property hidden by a comment** — *HIGH; CRITICAL when the value is
a credential, a token or another caller's data* · universal + `api-surface`
`Find:` `grep -rn -B3 --include=*Response*.cs "internal\|do not return\|not returned\|hidden" src/`
and read each hit against the property it annotates; independently, serialize one
response and compare the JSON against the class.

```csharp
// BAD — the only thing hiding this property is a sentence. The serializer does
// not read comments, and every response carries the value to every caller.
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
developer, and the two are not the same mechanism. The gap is invisible in review
precisely because the comment reads as authoritative. Treat any "internal", "not
exposed" or "do not return" annotation on a public response member as a finding, and
check the whole response *family* — see 6.5. The fix is that the property does not
belong on a response type at all: if an internal step needs the value, that step's
type is not the response. A serialization-ignore attribute would also suppress it,
but it leaves the exposure one attribute-deletion away and **no shipped body
legislates it** — raise it as the author's decision, not as the recommended fix.

**6.3 Redaction relied on as a security control** — *MEDIUM; HIGH when the
Production overlay is missing entirely* · `error-handling`,
`references/middleware-behavior.md`, *Redaction — `ErrorResponseSettings`*
`Find:` open the error-response settings in the base configuration and in every
environment overlay and read the hidden-property list; then check each configured
name against the wrapper's actual property names, character for character.
Three failures live here. An environment whose overlay carries **no** hidden set
returns full diagnostics — source, method, exception text, line — to every caller on
every 500. A configured name that **does not match** a wrapper property fails
silently: the settings are read straight from `IConfiguration` inside the `catch` and
the class is never registered with the options pattern, so there is no binding and no
validation on start — the reflection lookup simply finds no match, nothing fails at
boot, and the field ships unredacted. And decisively: **`Message` is never in the
hidden set, and on the unexpected-exception path it is the raw exception's own
text** — redaction removes the stack and the type name, never the sentence. A
driver's error string describing the schema reaches the caller through the one field
redaction cannot touch, which is why the fix is upstream: throw a leaf carrying a
written message, not a longer hidden list.

**6.4 A credential or personal datum written to a log** — *HIGH* ·
`dotnet-code-review` check 2.6, *Sensitive data in a log line or in a message a
caller reads*
`Find:` run 2.6's greps, then add
`grep -rn "GetTokenAsync\|Request.Headers\|HttpContext.User\b" src/` and read what
each result reaches.
Check 2.6 owns the rule — **a log line is an export**, retained longer, replicated
wider and read by more people than the database is, so a token or a request body in
a log has a larger audience than the row it came from. One house-specific route it
does not name: **the bearer registrations set `SaveToken = true`** (universal
consequence of a shipped setting), so the raw token is retained on the authenticated
request. Anything that logs the authentication properties, dumps the principal, or
serializes incoming headers wholesale is exporting a live credential, not merely a
user identifier.

**6.5 A property that reaches the wire because of where it sits** — *HIGH* ·
`dotnet-code-review` check 2.7, *An entity reaching the wire* + `api-surface`
`Find:` run 2.7's greps; then, for every response type touched in the diff,
`grep -rn "class \w*Response *: <ThatType>" src/` and read the responses that
inherit from it.
Check 2.7 covers the entity itself. This layer's addition is the **inherited**
disclosure: the response family is a ladder, so a property added to a rung appears on
every response below it — including ones the author never opened, in modules they do
not own, on endpoints reachable by other client families. A one-property diff to a
base rung is a multi-endpoint disclosure, and reviewing the changed file alone cannot
see it. Check the ladder, not the file.

**6.6 The API document published or unguarded** — *HIGH; CRITICAL when the UI is
reachable unauthenticated in a deployed environment* · `api-surface`,
`references/openapi-swashbuckle.md`
`Find:` read the `Enable` value in each environment's effective configuration; then
open the document facade's pipeline extension and read the order of the basic-auth
registration against the UI registration.
Three failures, one file. **`Enable` gates the entire block** — document and UI alike
— and it is the switch for an environment that must not publish its API surface;
it is configuration, not a code change. The **basic-auth middleware registered after
`UseSwaggerUI` never runs**, which looks correct in the diff and protects nothing,
and it must be guarded by `!Environment.IsDevelopment()`. And a **`RoutePrefix`
changed without the middleware's matching prefix** moves the UI out from behind its
own gate — the shipped middleware reads the prefix from the same settings for exactly
this reason, so a hand-written prefix anywhere is the finding. The credentials
themselves are check 2.5. A published document is not a vulnerability by itself; it
is a complete, current map of every endpoint, parameter and schema handed to whoever
asks.

**Not findings here.** A **response family rooting at the shared base type** is
correct — `dotnet-code-review` check 2.7 says so explicitly, and check 5.12, *A
response that is a sibling instead of a rung*, owns the defect that actually lives in
that family. An **empty hidden-property list in the *base* configuration** is by
design: full diagnostics are wanted outside production and the overlay carries the
set. A **4xx with no trace id or support message** is by design — diagnostics exist
only on the 500 paths. And a **test fixture, seed value or
`appsettings.Development.json` value** is not production exposure; see the
calibration below.

## Severity calibration

The four words and their general meanings are `dotnet-code-review`'s — Principle 3,
*One severity vocabulary, four words*, and its *Severity ladder*. This rubric does
not restate them; it calibrates them, because security findings are argued from
category rather than consequence and, left uncalibrated, every one of them argues its
way to CRITICAL and the word stops carrying information.

| Severity | In a security finding |
|---|---|
| **CRITICAL** | Exploitable **now**, by someone not already trusted with the result: an authorization gate that does not run, key material an attacker can read, caller data reaching an interpreter, a cross-family authentication bypass, a fail-open seam on the authenticated path. The test is not how bad the outcome is but whether anything today stands between an attacker and it. |
| **HIGH** | A real weakening with a precondition attached — it needs a valid token, an internal position, a specific environment, or a second defect. Also: the correct control present but placed where it cannot run. |
| **MEDIUM** | Hardening, or a control whose failure mode is confusing rather than exploitable — an unverifiable redaction name, a duplicated policy registration, a wildcard the browser already rejects. |
| **INFO** | A surviving call site recorded for the next review, a deliberate anonymous endpoint documented, a pre-existing exposure noticed in diff mode, a question no shipped body settles — every CORS origin question lands here — or a protection done well. |

Four calibrations settle the arguments this rubric actually gets:

- **Reachability escalates; unreachability does not delete.** A defect reachable
  from an **unauthenticated** path moves up one rung. A defect with no current
  caller moves down one rung and is still reported — reachability is a property of
  today's routes, and routes change in one line. State the path in the finding.
- **Test fixtures, seed data and `appsettings.Development.json` values are not
  HIGH.** They are INFO, or MEDIUM where the same value is also used in a deployed
  environment. The one exception is a **real** credential parked in any of them:
  that is 2.1 at full severity, because the repository does not know which file it
  is in. Grading development values HIGH is how a security report loses its reader
  for the findings that matter.
- **Severity is consequence, not effort** — `dotnet-code-review`'s rule, unchanged.
  A one-character fix to a missing gate is CRITICAL; a quarter-long
  secret-management migration whose absence exposes nothing today is MEDIUM.
- **A missing XML comment is never a security finding.** Nor is naming, casing,
  formatting, or a missing `ProducesResponseType`. `api-surface` owns the
  documentation rule and `dotnet-code-review` Principle 4, *Style is reviewed last,
  or not at all*, owns the rest. A style nit inside a security report reads as
  padding and discredits the findings above it.

## The report

One report, the severity words as headings, always in this order. **Every section
appears every time**; write `None.` when a section is empty, because an absent
section is ambiguous between *checked, found nothing* and *did not check* — and in a
security review that ambiguity is the whole distinction.

```markdown
## Security review: <scope>

> This is static analysis, not a penetration test. It catches known patterns and
> house-doctrine violations; it does not catch business-logic flaws, complex
> authorization bypasses, or anything that only exists at runtime.

### Summary
<mode (diff or sweep) · layers run · PASS / FAIL and the findings that decide it>

### CRITICAL
- **<title>** — `<file>:<line>` · check <n.n>
  <what is exposed> · <the reach: unauthenticated / any caller / another family / admin> · <why it matters> · <the specific change that closes it> · <owning skill>

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

1. **The disclaimer is verbatim and it is not moved.** It sits above the Summary,
   before the first finding, in the words above. A report that buries it at the
   bottom has published a clearance, and a report that paraphrases it has softened
   the one claim it is obliged to make.
2. **Name the exposure and the reach, and carry `file:line`.** "Insecure
   configuration" is not a finding; "an unauthenticated caller can read every field
   of the error envelope including the exception text, at `<file>:<line>`" is. The
   two clauses are separate because a reader who agrees with the first and disagrees
   with the second is arguing about severity, not about the defect. Evidence is
   `file:line`, or the exact command and the line of its output.
3. **Cite the check number and the owning skill.** The author argues with the rule
   rather than with the reviewer, and a rule that turns out to be stale surfaces as a
   contradiction rather than as a second opinion. A finding citing nothing is this
   rubric inventing doctrine under a security banner, which is the one thing it may
   not do. Where a check is `universal`, say so — that is a citation too.
4. **`FAIL` is decided by CRITICAL and HIGH only** — the same rule
   `dotnet-architecture-review` uses, and for the same reason: if MEDIUM failed the
   gate, every report would FAIL and the verdict would stop meaning anything. A
   MEDIUM-only report is `PASS`, and the Summary says how many hardening findings it
   carries.
5. **`Suppressions applied` is not optional when the layer ran.** Naming the
   house-doctrine patterns you deliberately did not report is what tells the reader
   you opened the JWT block and *decided*, rather than never opening it — otherwise
   the next reviewer raises them, and the one after that.

**Layer coverage is not optional.** A review that ran only layers 1 and 2 is a
useful report; one that ran only layers 1 and 2 and does not say so is a misleading
one, because a reader will take a silent layer for a clean one.

## Routing

**Deep dives — sibling rubrics.** This rubric owns exposure. When the change's risk
lives somewhere else, load that one instead of stretching this.

| The change is mostly about | Load |
|---|---|
| Blast radius, severity of behavioural findings, slop; the posture checks 2.1–2.7 and raw SQL 1.7 | `dotnet-code-review` |
| Layering, dependency direction, placement, the composition root's shape | `dotnet-architecture-review` |
| Query cost, allocation, blocking, missing indexes — including a rate-limiting or DoS question | `dotnet-performance-review` |

**Doctrine — the owning knowledge skill.** This rubric notices the disagreement; the
rule itself lives here.

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
**Execution:** rotating a key is a configuration change per environment and closing a
gate is an ordinary code change — both made normally, neither here; only the cleanup
they leave behind goes to `/simplify`.

## References

**Read `references/security-checks.md` when** running a sweep or a release gate, or
when a finding needs the long tail behind a layer — it continues each layer's
numbering from where this body stops, with no number reused, so a citation is
unambiguous about which file it came from.

## Decision Guide

| Situation | Do this |
|---|---|
| Asked for "a security review" with no scope | Ask whether this is the change or the solution, declare the mode and the layers in the Summary, and run all six unless the scope narrows them |
| A generic checklist flags `ValidateIssuer = false` or `ClockSkew = TimeSpan.Zero` | Not a finding. House law, and the skew is stricter than the advice flagging it. Check 4.8 is the real one — do two schemes share a key? |
| A secret is found in a committed file | CRITICAL, and the fix is **rotation**. A diff that only deletes the line has closed nothing — say so |
| A secret is in git history but not in the working tree | Still CRITICAL. History does not heal, and "pre-existing" does not apply |
| An endpoint has no authorization attribute | CRITICAL — the fallback policy is `null`, so it is public. Do not assume a global default protects it |
| One `[HasPermission]` lists two permission codes | HIGH by 4.2 — it means ANY, not ALL. Ask which was intended |
| An anonymous endpoint reads the caller's identity | HIGH — the principal was never populated, so the accessor answers with an empty id, not with a real caller |
| A finding is about which origins CORS allows | INFO with the question stated. No shipped body settles it; only 5.3 is universal |
| A defect exists but nothing currently routes to it | Report it one rung lower. Never drop it — reachability changes in one line |
| The value is in a test fixture or a development overlay | INFO or MEDIUM, unless it is a real credential, which is 2.1 at full severity |
| A finding could be graded here or by `dotnet-code-review` section 2 | Grade it once. If 2.x owns the shape, cite it by number and name and add only the deepening |
| A layer cannot run | Say so under *Layer coverage*. Never let a skipped layer read as a clean one |
| Generic advice suggests a check with no shipped owner — XSS in views, a prescribed secret store, a CORS origin policy | Not a finding here. Say what is not covered rather than inventing the rule |
| A convention the code breaks is stated in no shipped body | INFO with the question stated. This rubric has no doctrine of its own |
| Everything passes | Say PASS, write `None.` into each empty section, keep the disclaimer, keep *Layer coverage* honest, and name what the code got right |
