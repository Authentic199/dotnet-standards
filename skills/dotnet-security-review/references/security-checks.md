# Security checks — the long tail

The checks behind the six layers in `SKILL.md`. The body carries each layer's
decisive checks; this file carries the rest, plus the comparison data those checks
are run against — the shipped pipeline order, the sites one identifier occupies,
and which gate answers which question.

**Numbering continues the body's.** Layer 1 resumes at 1.2, layer 2 at 2.6, layer 3
at 3.6, layer 4 at 4.10, layer 5 at 5.4, layer 6 at 6.7. A number is never reused,
so `check 4.14` needs no file named beside it.

**Scope, tool and severity are the body's and are not repeated here.** Diff mode
versus sweep, the never-pre-existing rule for a secret that was ever committed,
`grep -rn --include=*.cs` as the default instrument with no analysis server and no
scanner assumed, and the CRITICAL / HIGH / MEDIUM / INFO calibration all hold
unchanged. So does the honesty rule: whatever runs, the report says **static
analysis, not a penetration test**, verbatim.

**How to read a check.**

> **<n.m> <title>** — *SEVERITY* · owner
> `Find:` the grep to run, or the file to open and what to read in it.
> Why it is a finding, and the change that closes it.

The owner is the skill and section that legislates the rule — cite it in the finding
rather than re-deriving it. `universal` means the check is a defect in any codebase
and needs no house citation; that is a citation too, and it is written out. Anything
presented as a table or a listing is **comparison data, not a check**: it carries no
number and no severity, because it is what you compare against.

**Cite across, never re-grade.** Where a check here deepens one in the body or in
another rubric, it names it by number and title and adds only what is new. One
defect earns one severity in one place.

## Contents

- [Layer 1 — packages](#layer-1--packages) — 1.2
- [Layer 2 — secrets and the key path](#layer-2--secrets-and-the-key-path) — 2.6–2.7
- [Layer 3 — parsing and untrusted values](#layer-3--parsing-and-untrusted-values) — 3.6–3.8
- [Layer 4 — the token flow and the grant tables](#layer-4--the-token-flow-and-the-grant-tables) — 4.10–4.17
- [Comparison data — the shipped pipeline order](#comparison-data--the-shipped-pipeline-order)
- [Comparison data — one identifier, five sites](#comparison-data--one-identifier-five-sites)
- [Comparison data — which gate answers what](#comparison-data--which-gate-answers-what)
- [Layer 5 — CORS](#layer-5--cors) — 5.4
- [Layer 6 — what the store and the document publish](#layer-6--what-the-store-and-the-document-publish) — 6.7–6.8
- [Refused — and why](#refused--and-why)

## Layer 1 — packages

**1.2 A vulnerability silenced rather than patched** — *HIGH* · universal
`Find:` `grep -rn "NU19[0-9][0-9]\|NoWarn" --include=*.csproj --include=*.props src/ tests/`
and the repository-root build files.
A suppressed advisory warning reports a clean scan for a solution that is not clean,
and the next reviewer inherits the false pass rather than the risk — which makes
body check **1.1, A dependency with a published vulnerability** answer the wrong
question. The same finding covers a package deliberately held below its patched
version with no recorded reason. The fix is the bump, or an explicit recorded
exception naming the compensating control — not a quieter build. Suppression
*placement* is `dotnet-architecture-review` check **1.11, A rule suppression inside
a csproj**; cite it and do not grade it twice — what is graded here is the advisory
being silenced, not where the silencing sits.

Run the layer's command against a **restored** graph. An unrestored solution reports
nothing and reads exactly like a clean pass.

## Layer 2 — secrets and the key path

**Run `dotnet-architecture-review` check 5.9, *`AddEnvironmentVariables()` not last,
or absent*, as part of this layer.** It is owned there and graded there — cite it by
number and name, do not renumber it into this file. What a placement review does not
state is the consequence this layer cares about: environment variables are how key
material reaches a deployed environment, so an environment source that is not last
means a committed placeholder — or a stale committed value — can override the real
secret, and the service boots with the wrong key. Startup validation does not catch
it, because it asserts that a value is *present*, never that it is the right one.

**2.6 Key encoding decided at a call site** — *MEDIUM* · `auth-and-security`,
*Don't decide key encoding at the call site* + `references/jwt-and-tokens.md` §2,
*The settings classes*
`Find:` `grep -rnE "Encoding\.[A-Za-z]+\.GetBytes\(" src/` and
`grep -rn "new SymmetricSecurityKey(" src/`, discarding the hits inside the settings
class's own helpers.
**The four `Get*` helpers are the only place encoding and clock policy are decided.**
A key built with one encoding at registration and another at signing validates
nowhere — and the two encodings agree for every ASCII key, which is exactly why this
survives testing and fails on the first key containing a non-ASCII byte. The symptom
is an ordinary 401 with nothing in the logs.
**Say plainly what this is: an availability defect on the key path, not an
exposure.** It belongs in this layer because the key path is this layer's unit, and
because the shape it produces — a raw key string travelling between call sites — is
what puts key material somewhere a log or an exception message can reach it. The fix
is structural rather than local: a method that takes a `SecurityKey` rather than a
`string` leaves no call site that *could* choose an encoding. Flag any new helper
that accepts a key as text.

**2.7 Issuer or audience validation tightened on one side only** — *HIGH* ·
`auth-and-security` › `references/jwt-and-tokens.md` §7, *The generator*
`Find:` `grep -rn "ValidateIssuer\|ValidateAudience\|ValidIssuer\|ValidAudience" src/`
and compare every hit against what the generator actually stamps.
The issuer and audience values are **stamped, not verified** — descriptive payload
on a token the bearer handlers never check them against, which is why both are
optional in configuration and may be absent or blank in a live token. Turning
validation on while live tokens carry a different issuer — or none — **invalidates
every session at once**: every holder of every family's token is logged out on
deploy, and the rollback is another deploy.
This is the finding to write where generic advice wants a change. The body's *Not a
finding* block rules `ValidateIssuer = false` itself out of scope; this check is what
to write instead when a diff tries to "fix" it. If the tightening is genuinely
wanted, it is a coordinated change — stamp first, ship, wait out the access-token
lifetime, then validate — and the review's job is to say so rather than to approve
or reject the line.

## Layer 3 — parsing and untrusted values

**3.6 An authorization-header prefix stripped by replacement** — *MEDIUM; HIGH where
the parsed value is used for anything but scheme selection* · universal
`Find:` `grep -rnE "Replace\(\"Bearer|Replace\(\"bearer|Substring\(7\)" src/` and
read each hit's surrounding guard.
`Replace` removes the substring **wherever it appears**, not a prefix, and it returns
the input unchanged when the prefix is absent — so a header that never carried the
prefix is handed onward as though it were a token. A bare `Substring(7)` throws on
any shorter header, converting a malformed request into a 500. The shipped scheme
selector is the correct shape and the contrast to cite: guard with
`StartsWith("Bearer ")`, slice by the prefix length, trim (`auth-and-security`,
`references/jwt-and-tokens.md` §4).
**Be honest about the consequence.** In the shipped selector a malformed value simply
fails the readability check and falls through to a scheme that rejects — it fails
closed, which is why this is MEDIUM. It becomes HIGH the moment the same parse feeds
something that is not a scheme choice.

**3.7 Reflection or type resolution over unsigned request data** — *HIGH* · universal
`Find:` `grep -rn "Type.GetType(\|GetProperty(\|GetMethod(\|Activator.CreateInstance(" src/`
and trace each name argument back to its source.
A name arriving in a header, a query string or a body and reaching a type, property
or method lookup lets the caller choose which code path runs. The shipped uses are
the safe contrast — the scheme-to-settings lookup resolves a **constant** — so the
finding is the departure, not the mechanism. Two neighbours that are **not** this
check: resolution of the principal type from a signed token claim is body check
**4.3, Per-request principal verification that can silently stop running**, and
type information taken from a serialized payload is body check **3.2, Insecure
deserialization**. Grading any of the three twice inflates one defect into several.

**3.8 A value read from an unverified token used for more than a lookup** —
*HIGH; CRITICAL when it decides identity, ownership or authorization* ·
`auth-and-security` › `references/jwt-and-tokens.md` §4, *The forwarding selector*
and §8, *The login and refresh flow*
`Find:` `grep -rn "ReadJwtToken\|GetClaimsValue\|CanReadToken" src/` and trace every
returned value to its use.
Two shipped readers parse a token **whose signature has not been checked**, and both
carry the same stated posture: the selector *reads, it does not validate*, and the
claim reader *locates, it does not authorise*. A caller-supplied token is a
caller-supplied string. Using its claim as a database key is fine, because the stored
row is then the authority — the shipped refresh flow is the model, where the session
claim locates the row and **the stored string authorises**. Using it as the
principal's id, as a tenant, as a permission, or to decide a branch is authentication
by assertion. The fix is always the same shape: look the value up, and let what comes
back decide.

## Layer 4 — the token flow and the grant tables

The body covers the gates at the edge of a request. This section covers the flow that
mints the credential and the tables that answer for it — the two places where a
defect grants standing rather than merely failing to check it.

**4.10 A detected refresh-token replay answered by rejecting one request** — *HIGH* ·
`auth-and-security` Principle 5, *A refresh token proves a session, not an identity*
+ `references/jwt-and-tokens.md` §8
`Find:` open the refresh flow and read the branch taken when the presented token does
not equal the stored one.
The presented token must equal **the latest** stored token for that session, and the
response to a mismatch is to **delete every row for that session** before rejecting —
a mismatch means a superseded token was replayed, so the session is already
compromised. **Deleting the range before throwing is the reuse response, not an
optimisation:** drop it and a detected replay becomes a shrug, leaving the stolen
copy usable until it expires on its own, which for a refresh token is the longest
lifetime the service issues. Flag equally a refresh path that overwrites the row
instead of failing.
HIGH rather than CRITICAL because the attacker must already hold a superseded token:
this is a failure to revoke after a compromise, not a grant of access.

**4.11 A refresh path that skips the currency checks** — *HIGH* ·
`auth-and-security` Principle 5 + `references/jwt-and-tokens.md` §7, *The generator*,
and §8, *The login and refresh flow*
`Find:` in the same read as 4.10, check three things — the chain is ordered
newest-first before the comparison; the principal's status is re-read from the stored
row; the session id is carried through rather than minted afresh.
An ordered read taking the newest row **is** what "the latest stored token" means;
comparing against any other row in the chain accepts a superseded token, which
silently disables 4.10. Re-reading status matters because a principal blocked after
login must otherwise be able to **refresh its way back in** indefinitely — the access
token cannot know it was blocked, and only the row can answer, so on the refresh path
the status is read through the stored session's own principal and never from anything
in the presented token. And carrying the session id forward is the entire rotation
mechanism: minting a new one on refresh breaks the chain into unlinked singletons,
after which no replay is detectable at all.
**Signature and currency are two independent gates** — the refresh request's own
validator resolves its family's settings, checks the signature and rejects at the
edge, before the service ever runs its stored-row comparison. A diff that removes
either has halved the gate while both call sites still read as present, which is why
the check reads both ends rather than either alone. Validator structure itself is
`module-feature`'s.

**4.12 A selector fallback that does not reject** — *CRITICAL* ·
`auth-and-security` › `references/jwt-and-tokens.md` §4, *The forwarding selector*
`Find:` open the forwarding selector and read every arm, its early returns and its
default arm.
Every unknown or unreadable input falls through to a scheme whose key will not match
a foreign token, so **the fallback rejects rather than accepts** — that property is
what makes an unrecognised token an ordinary 401 instead of an admission. A fallback
edited to point at a more permissive family inverts it, and an unrecognised token
becomes a routing success: the same cross-family bypass body check **4.8, A signing
key shared between client families** grades, which is why this carries the same
severity. Two neighbouring shapes are findings for the same reason: a selector that
**throws** turns an unauthenticated request into a 500 from the wrong layer and moves
an admission decision into a method that has verified no signature; and a **missing
arm** for a live family hands that family's tokens to a scheme that rejects them — an
outage for one client while everything else works. Keep an explicit arm per scheme
even where it duplicates the fallback; the arm list is how the next person sees what
to add. A missing arm on a *newly added* scheme is 4.8's missing-site sweep — do not
grade it twice.

**4.13 A new gate that returns null on failure with nothing rejecting downstream** —
*HIGH; CRITICAL when the caller is a gate* · `auth-and-security` ›
`references/jwt-and-tokens.md` §7, *The generator*
`Find:` `grep -rn -A6 "catch" src/` inside the auth facade and any new validation
helper, and flag every block returning `null`, `false`, `default` or an empty
sequence; then open every caller and read what it does with that value.
The shipped validators return `null` rather than throwing, deliberately — but the
sentence that makes that safe is that **the caller decides what a bad token means and
rejects with its own domain error**. The shape is therefore safe by contract, not by
construction, and the contract is enforced nowhere. Copied into a new gate whose
caller treats `null` as "no constraint", it is a fail-open gate that logs nothing.
**Check the caller, not the helper: the finding is a null that flows on, never the
null itself.** This is the same failure mode as body check **4.1, An endpoint with no
authorization metadata at all** (a `null` fallback policy) and body check **4.3,
Per-request principal verification that can silently stop running** (a skipped
resolution) — three fail-open seams, one habit. Grade this one only where the caller
is new or changed; the two shipped seams are 4.1's and 4.3's.

**4.14 Grants left orphaned by a principal deletion** — *MEDIUM* ·
`auth-and-security` › `references/permission-internals.md` §5, *Where grants live*
`Find:` `grep -rn "DeleteAsync\|DeleteRangeAsync" src/` for principal entities and
check the same operation for a matching grant cleanup.
The grant owner is polymorphic — a principal id plus a family name, with **no foreign
key to any principal table**. That is what lets one grant schema serve every client
family, and it is why deleting a principal does **not** cascade its grants. The rows
survive as orphans. They are inert while nothing holds that id and they become a live
privilege grant the moment something does — a re-created principal, a restored
backup, a seeded fixture, an id arriving through an import. Say which cleanup is owed
and where; the entity and cascade mechanics are `ef-core-data-access`.

**4.15 A permission code removed from the catalogue while its rows remain** —
*HIGH* · `auth-and-security` › `references/permission-internals.md` §4, *Implied
permissions*, and §5
`Find:` for every constant or definition deleted in the diff, grep the codebase for
the resulting code string and check the change also removes the rows.
Deleting the permission **row** cascades to its grants; deleting the **catalogue
entry in code** does nothing to the database, and is the dangerous one. A granted row
whose code is not in the catalogue **throws — during authorization** — so every
request by every principal still holding that code becomes a server error, on a path
with no fallback. There is one safe order and it is the reverse of the intuitive one:
**rows first, then the code.** The same finding runs in the other direction: a new
code declared but never seeded makes every sync naming it throw.

**4.16 The authorization registrations re-lifetimed** — *HIGH* · `auth-and-security`
› `references/permission-internals.md` §8, *Registration*
`Find:` `grep -rn "IAuthorizationPolicyProvider\|IAuthorizationHandler" src/` and read
the registration verb on each.
**Singleton provider, scoped handler — this pairing is not a style choice.** The
framework resolves the policy provider once from the root container, so it must
depend on nothing scoped; the handler is where the per-request dependencies live —
the current principal, repositories — and it is resolved per request. Registering the
handler as a singleton either fails at startup or, worse, **leaks one request's state
into the next**: an authorization decision made for one caller answering for another.
That is a wrong-answer gate rather than a lifetime nit, which is why it is graded
here; cite `dotnet-code-review` check **3.4, A captive dependency** for the general
shape and grade it once, here.

**4.17 Login rejections that distinguish an unknown account from a wrong password** —
*MEDIUM* · universal; the message text is `message-keys`
`Find:` open the login flow and compare the message key each failure branch carries;
then check whether either branch returns before any password verification runs.
Two distinguishable rejections let an unauthenticated caller enumerate which accounts
exist — a directory the service did not mean to publish, and the reconnaissance step
before every credential-stuffing run.
**Report this as a decision, not as a defect.** The shipped login flow distinguishes
the two branches, so a review that grades this CRITICAL is grading the house's own
example, and it must not be written as a doctrine violation. What the review owes is
the trade-off, named: a distinguishable message is better for a caller who mistyped
their address and on an internal or invitation-only surface that is the right trade;
a single indistinguishable message costs support clarity and buys enumeration
resistance. Report MEDIUM with the surface named and the choice put to the author,
and escalate only where the login is public and self-service.
Two things travel with it. **Timing is part of the same answer** — an early return
before any hash verification is distinguishable whatever the text says. And the
distinction usually survives on the **password-reset and resend paths** after the
login itself is fixed, so check those in the same pass. The wording belongs to
`message-keys`; do not propose literal text here.

### Comparison data — the shipped pipeline order

Compare `UseInfrastructure` against this. It is the target for body checks **4.5, The
auth stages out of order in the pipeline** and **5.1, `UseCorsPolicy()` at the wrong
position** — not a second statement of them.

```
UseStaticFiles()
UseRouting()                   # endpoint metadata becomes available
UseApm()
UseCorsPolicy()                # outside the exception handler's try, by design
UseExceptionHandlerMiddleware()
UseAuthentication()            # a scheme establishes the principal
UseCurrentUser()               # copy it into the request-scoped seam
UseVerifyJwtUserMiddleware()   # the row still exists, still allowed, still this installation
UseAuthorization()             # permissions, from the grant tables
```

Each auth stage depends on the one above it. Before routing, the anonymous check has
no metadata to read. Verification placed before the principal layer has nothing to
verify; placed after authorization, a blocked principal's permissions are evaluated
before anyone notices they are blocked.

### Comparison data — one identifier, five sites

One string identifies a principal family in five places and nothing enforces
agreement between them. **This is comparison data, not a check** — use it to make a
finding's grep complete.

| # | Site | What holds the string |
|---|---|---|
| 1 | the minted token | the principal-type claim the generator stamps |
| 2 | the forwarding selector | the arm compared against each family's type name |
| 3 | every grant row | the family column beside the principal id |
| 4 | permission and role rows | the same family column on each |
| 5 | catalogue definitions | each definition's recorded family |

Move a principal entity and tokens route to the wrong scheme, grants stop matching,
and guard presets target a family that no longer exists — no compile error, no failed
migration, no log line. Relocating or renaming a principal entity is a **data
change**: plan the row rewrite in the same commit.

**A sixth site sits outside this list and fails open.** Per-request verification
resolves the same name back into a type, and when resolution returns nothing the
guarded block is skipped rather than rejecting. That one is body check **4.3,
Per-request principal verification that can silently stop running**, graded there;
it is named here only so a rename review does not stop at five.

### Comparison data — which gate answers what

Use this when a report needs to say which gate is missing rather than that
"authorization is weak". Four gates, four questions.

| Gate | Answers | Fails by |
|---|---|---|
| Scheme selection | which client family is this? | routing to a scheme whose key rejects |
| Signature validation | did this service sign it, and is it live? | 401 from the bearer handler |
| Principal verification | does the account still exist and is it still allowed? | the rejection family, per request |
| Permission evaluation | may this caller do this? | the requirement is simply not satisfied |

Two consequences worth carrying into a finding. **Permission failure is silence** —
the handler never fails the requirement explicitly, it merely does not succeed — so
nothing in a log distinguishes "denied" from "no handler ran". And **the default
principal family is the one that is permission-checked**; other families are gated by
scheme selection alone, which is why body check **4.8, A signing key shared between
client families** is CRITICAL rather than a configuration nit.

## Layer 5 — CORS

**This layer has almost no tail, and that is the finding about the layer.** The house
legislates where the policy is registered and where it runs, and nothing about
origins, credentials, methods, headers or environment separation. One structural
check follows; everything else is in *Refused — and why*.

**5.4 The CORS registration and the pipeline call not paired** — *MEDIUM* ·
`facade-module-architecture` › `references/composition-root.md`
`Find:` `grep -n "AddCorsPolicy\|UseCorsPolicy" src/Infrastructure/Startup.cs`
Both lines are shipped — one in the service chain, one in the pipeline — and the
facade contributes both halves or neither. One half alone is a policy configured and
never applied, or a pipeline stage with no policy behind it; either way the
browser-visible behaviour is not what the code reads as. Report the missing half by
name. MEDIUM and not higher because it **fails closed**: the absent headers make the
browser refuse the cross-origin call, which is a broken client rather than an
exposure. Where the policy runs is body check **5.1, `UseCorsPolicy()` at the wrong
position**; a second policy declared elsewhere is **5.2, CORS configured outside the
composition root**.

## Layer 6 — what the store and the document publish

**6.7 A caller-supplied credential persisted in plain form** — *HIGH* · universal
`Find:` `grep -rniE "class .*(Credential|Secret|Token|ApiKey|Password)" src/` over
entity types, then read the columns each entity configuration maps, and check whether
the value is ever reachable through a response type.
An integration key, an external account password or a third-party token stored as an
ordinary column turns any single read of that table — a backup, a support query, a
logged query, a read replica — into credential disclosure. Its blast radius is not
this service but the *other* system the credential opens, which is why it outranks
its line count and why "the database is internal" does not soften it.
**Note what is not in scope:** the service's own refresh tokens are stored
deliberately, and the stored row is the authority that makes replay detectable at all
(check 4.10). This finding is about credentials belonging to **someone else's**
system.
**Name the exposure and ask for the protection decision; do not prescribe a
mechanism.** `auth-and-security` prescribes no secret store — how values reach and
rest in an environment is a deployment decision — so a review that mandates a
specific cipher, vault or provider is inventing doctrine. The finding states what is
stored, who can read it and what it opens; the answer may be encryption at rest, an
external store, or not storing it at all. Two shapes worth flagging in the same pass:
a credential column reachable through a response family (body check **6.5, A property
that reaches the wire because of where it sits**), and a credential that only needs
to be *verified* rather than *replayed* — that one should not be reversible at all.

**6.8 An XML summary that documents the inside of the system** — *INFO; MEDIUM where
it names a system, a credential holder or an unfixed weakness* · universal,
reinforced by `api-surface` › `references/openapi-swashbuckle.md`
`Find:` `grep -rn -B6 "\[Http" src/Web/Controllers/` and read the `<summary>` and
`<remarks>` on each action; then read the comments on DTO members and enum values.
Documentation comments here are not internal notes. Two `IncludeXmlComments` calls
publish endpoint summaries as operation descriptions and DTO summaries as schema
descriptions, and the enum schema filter folds each member's comment into the schema
alongside its numeric value — all of it served to whoever can reach the document. A
sentence naming an internal table, a downstream vendor, a queue, a feature flag, a
bug ticket or a workaround is a public statement about the architecture, written by
someone who believed they were writing to the next developer.
**This does not contradict the body's calibration that a missing XML comment is never
a security finding** — the finding here is a comment that says too much, not one that
is absent. The fix is to rewrite the comment for its real audience, never to delete
it: an undocumented operation is its own defect, and `api-surface` owns it.

**Not findings here.** A **missing padlock in the document UI, or a padlock on every
operation**, reflects the endpoint's own authorization metadata and never the
document configuration — treat it as a fast readout for body check **4.1, An endpoint
with no authorization metadata at all**, and write the finding there, against the
endpoint. And **bare integers where an enum should be described**, or bare operations
and schemas generally, almost always mean the documentation XML file was not produced
by the build: the filter degrades silently — no file, no filter, no error. That is a
build-configuration defect with its own diagnostic table in
`references/openapi-swashbuckle.md`, and it is nobody's security finding.

## Refused — and why

Recorded so the next session does not re-derive them, and so a reader can see the
boundary was drawn deliberately rather than forgotten. Each of these is a real
security topic; none has a shipped owner or a universal footing, and this rubric does
not invent doctrine under a security banner.

| Candidate | Why it is not here |
|---|---|
| CORS origin, header, method and credentials policy, and development-versus-production separation | The house legislates **only** where the policy is registered and where it runs. Body check 5.3 covers the one universal defect; everything past it would be invented. An origin list is INFO with the question stated. |
| XSS and server-rendered output encoding | No view layer — Controllers returning JSON. Refused in the body and not revisited here. |
| Token-lifetime ceilings | The shipped configuration carries deliberately long lifetimes that differ per client family, and no shipped body sets a maximum. A "lifetime too long" check would manufacture findings against the shipped values. A lifetime left at zero cannot ship — startup validation treats a type default as missing. |
| A scheme constant disagreeing with its settings property name | Real, but it is an availability defect: the mismatch fails at boot or produces a silent 401 for one family. It exposes nothing, so it is not this rubric's. |
| A numbered test-posture check — the test authentication handler, a seeded high-privilege principal | `dotnet-testing` owns the test scheme and the body's routing table points there; test coverage of a security behaviour is `dotnet-code-review` section 6's. No shipped sentence makes a *production* finding of it. Banked. |
| Rate limiting, lockout, brute-force resistance on login | No shipped body states a policy. 4.17 covers the part that is universal — distinguishable answers — and the rest is a product decision. |
| Archive entry-path containment ("zip slip") on an uploaded archive | Real elsewhere, and **not** a gap in the shipped path: the house zip helper takes an entry's last path segment only, sanitizes the stem and regenerates the name under a caller-supplied temp root, so the entry's own directory components never reach the filesystem. That is body check 3.4's "generated name" arrived at incidentally — no shipped body states it is the property doing the work, and the upload gates (archive type, entry counts, per-entry size) are not among the things that guard it. A check here would legislate a rule no body carries, and 3.4's grep does not reach an extraction call. Revisit if a shipped body ever states the property. |
| Password hashing algorithm, work factor, salting | The login flow shows a verification call; no shipped body legislates the primitive behind it. Refused rather than guessed. |
| Security response headers — HSTS, CSP, frame options | No shipped owner, and no view layer to protect. |
| A secret store, vault or encryption-at-rest mechanism for 6.7 | `auth-and-security` explicitly prescribes none. Naming one would make this rubric the owner of a decision it does not hold. |
| Central package management, a pinned SDK, or a package-source allowlist | `dotnet-architecture-review` check 1.12 owns central package management and the SDK pin. Neither is a security question here. |
