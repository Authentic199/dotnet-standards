---
name: auth-and-security
description: >-
  This skill should be used when working the auth layer of a .NET API: a JWT
  scheme, JwtSettings, signing keys, token lifetimes, access- and refresh-token
  generation and rotation, the scheme-forwarding selector, IJwtUser claims,
  [HasPermission] internals — policy provider, requirement, authorization
  handler — permission codes, granting permissions and roles, ICurrentUser,
  per-request principal verification, API keys, or auth settings and secrets.
  Not for: [HasPermission] usage on endpoints — api-surface; exception shapes —
  error-handling; message text — message-keys; validators, services —
  module-feature; entities, queries, seeding — ef-core-data-access; faking a
  principal — dotnet-testing; placement — facade-module-architecture; Redis —
  distributed-caching; cleanup jobs — background-worker.
---

## Overview

Two questions, answered in two different places. **Who the caller is** comes from a signed token,
settled by the scheme that validated it. **What the caller may do, and whether they are still
allowed in at all**, comes from the database on every request. A token is evidence of a past login;
it is never evidence of current standing.

A third idea joins them: **the signing key is the boundary between client families.** Issuer and
audience are stamped but not checked, so what keeps one family's tokens out of another's endpoints
is that each family signs with its own key.

Those three run through everything here — several client families each with their own scheme and
key; access tokens that identify and refresh tokens that only carry a session; permissions read
from grant rows behind a short-lived cache; and a middleware that re-reads the principal's row
before any endpoint runs.

Three reference files carry the depth. Read the one that matches the work:

- **`references/jwt-and-tokens.md`** — before adding or removing a client scheme, touching the
  forwarding selector, changing a key, lifetime or settings-file layout, or writing or reviewing a
  login, refresh or token-minting flow.
- **`references/permission-internals.md`** — before adding a permission code, changing how a policy
  is built or evaluated, or touching the grant tables and their cache.
- **`references/principal-and-secrets.md`** — before changing what the request pipeline verifies,
  injecting the current principal, adding an auth setting, or gating a machine caller.

Placing `[HasPermission]` on an endpoint is **api-surface**. Faking a principal in a test is
**dotnet-testing**. This skill is the production internals behind both.

## Core Principles

### 1. One client family is one scheme, and every scheme name is a `const` on one class

A scheme name appears in the registration, in the forwarding selector, and in the permission
attribute's scheme argument. A single constants class makes those sites agree at
compile time and makes the whole scheme family readable in one place.

```csharp
public static class JwtScheme
{
    public const string Default = JwtBearerDefaults.AuthenticationScheme;
    public const string Device = nameof(Device);
    public const string Customer = nameof(Customer);
    public const string MultipleScheme = nameof(MultipleScheme);
}
```

`Default` aliases the framework's own name so the primary client keeps it; every other client
family gets its own `nameof` constant. `MultipleScheme` is not a bearer scheme — it is the policy
scheme that dispatches to the others (Principle 3).

### 2. One settings block per scheme, and the settings class owns its derived values

The options root carries one `JwtSettings` property per scheme, named exactly like the scheme
constant. Each family gets its own signing key, refresh key and lifetimes — with issuer and
audience validation off, **the signing key is the boundary between client families**, so sharing
one key means a token minted for one family is accepted for all of them.

`JwtSettings` exposes `GetSecurityKey()`, `GetSecurityRefreshKey()`, `GetAccessTokenExpired()`
and `GetRefreshTokenExpired()`. Callers never re-derive those: an encoding chosen at one call site
and a different encoding at another produce tokens that validate nowhere, and the failure surfaces
as an ordinary 401 with nothing to debug. Expirations are `double`, not `string` — the value is
arithmetic, and typing it as text defers the parse to whoever consumes it.

Adding a scheme is four edits that land together — constant, settings property, registration,
selector arm — plus the configuration block. Miss one and the failure is either a boot failure or
a silent 401 for that client only.

### 3. Several client families dispatch through a policy scheme, not through per-endpoint scheme lists

With more than one bearer scheme, both the default scheme and the default challenge scheme are the
policy scheme, whose `ForwardDefaultSelector` reads the incoming token and forwards to the scheme
matching the principal type stamped in it. Endpoints stay scheme-agnostic — a bare `[Authorize]`
works for every client family, and adding a family touches the auth facade only, never a
controller; the one exception is the permission attribute's scheme argument, which restricts which
families may reach the endpoint at all.

**`references/jwt-and-tokens.md`** — read it before adding or removing a scheme, touching the
selector, or changing a lifetime or the settings-file layout.

### 4. A principal type earns its claims by implementing `IJwtUser` — modules never hand-build a claims list

`IJwtUser` supplies the two claims every token must carry: the principal's id, and the principal's
type. The type claim is the one the scheme selector reads (Principle 3), so a principal that skips
the interface mints a token no scheme can route.

```csharp
public interface IJwtUser : IUser, IGuidIdentify
{
    public IEnumerable<Claim> UseClaims() => GetDefaultClaims(this);

    protected static IEnumerable<Claim> GetDefaultClaims(IJwtUser user)
        => new List<Claim>
        {
            new(JwtTokenPayload.Identification, user.Id.ToString()),
            new(JwtTokenPayload.ModelType, user.GetType().FullName!),
        };
}
```

`UseClaims()` is a virtual default: a principal needing more in its token overrides it and starts
from `GetDefaultClaims(this)`. That override is the only place per-principal claims are decided —
branching on the principal's type inside the token generator puts one client family's payload
rules in a facade shared by all of them. Every claim key is a `const` on one catalogue class; a key
written as a literal on one side and a `const` on the other reads back as `null`.

### 5. A refresh token proves a session, not an identity

An access token says who the caller is. A refresh token carries **only a session id** — who owns
it, when it expires, and whether it is still current all live in a stored row, and that row is the
authority. A stolen refresh token is perfectly well signed, so a valid signature settles nothing.

- Login mints a new session id; refresh reuses it. One login is one session, however often it is
  refreshed.
- Each refresh issues a new token and stores it. The presented token must equal the **latest**
  stored token for that session.
- If it does not, it is a replay of a superseded token: **delete every row for that session** and
  reject. Rejecting only the one request leaves the stolen token usable until it expires on its own.
- Re-check the principal's status on refresh, from the stored row. A principal blocked after login
  must not be able to refresh its way back in — the access token cannot know, only the database can.

Full contracts, the generator API and the annotated flow: **`references/jwt-and-tokens.md`** —
read it before writing or reviewing a login or refresh flow, or adding a claim to a token.

### 6. Authorization is a database read, not a claim read

The token settles *who* the caller is. It does not settle what they may do. When the permission
attribute runs, the handler takes exactly one thing from the principal — its id — and answers from
grant rows in the database, behind a short-lived in-process cache.

- **A grant can be withdrawn without waiting out a token — but it takes effect when the cache entry
  goes, not when the row changes.** Permissions stamped into a token are frozen until it expires;
  rows are not. The entry expires on a *sliding* window, so any write path that changes grants must
  evict the affected keys — the sync verbs do, the single-row verbs do not.
- **The cache is per process.** With more than one instance, a change made on one is stale on the
  others until their entries lapse.
- **A permission claim in a token is decoration.** Never authorize from one, and never assume a
  claim and the rows agree.

Two principals may hold the same code by different paths — one granted directly, one through a role
— and the check does not distinguish them.

Attribute → policy → requirement → handler → grant tables, and what granting means mechanically:
**`references/permission-internals.md`** — read it before adding a permission code, changing how a
policy is built, or touching the grant tables.

### 7. A valid token is not a valid principal

Authentication proves a token was signed by this service and has not expired. It proves nothing
about the account behind it, which may since have been deleted, blocked, or bound to a different
installation. Tokens live for days; that state changes in seconds.

So every authenticated request **re-reads the principal's row** before the endpoint runs, and
rejects when the row is gone, the status is blocked, or the request comes from an installation the
principal is no longer bound to. Endpoints marked anonymous are skipped; nothing else is.

- **Never treat a claim as current state.** A claim records what was true at login. Standing,
  membership and ownership are read from storage — Principle 6 is the authorization half of the
  same rule.
- **Logging someone out is a data change, not a token change.** Block the row and the next request
  fails, whatever token the caller holds.
- Unlike the permission read, this one is **not cached**. Stale standing is precisely what it
  exists to prevent, so it stays one indexed, no-tracking read per authenticated request.

**Read `references/principal-and-secrets.md`** before changing what the pipeline verifies,
injecting the current principal, or touching auth configuration.

## Decision Guide

| Scenario | Recommendation |
|---|---|
| A new client family needs its own tokens | Five edits that land together: the scheme const, the settings property of the same name, the config block, the bearer registration, the selector arm. `references/jwt-and-tokens.md` |
| A token should live longer or shorter | Change the lifetime in committed configuration, never in code. Never compute an expiry at a call site — the settings class's expiry helpers are the single source. Access changes apply at the next login, refresh changes at the next refresh |
| A token must carry an extra value | Add a const to the claim-key catalogue, then override `UseClaims()` on that principal starting from `GetDefaultClaims(this)`. Never branch on principal type inside the generator. Add a claim reader only if request code needs it |
| Read the caller's id, family or installation | Inject `ICurrentUser`. Never `IHttpContextAccessor`, never `FindFirst` on a raw claim string |
| A new capability needs protecting | Resource/action constants → catalogue definition with its implied codes and guards → seed the row → put the code on the endpoint → grant it. Skip the seed and every sync naming it throws |
| Changing what a principal or role holds | Prefer the **sync** verb: it validates the whole set before deleting anything and evicts its cache key. Single-row give/revoke evicts nothing — if you use it, evict explicitly |
| Protect an endpoint | **api-surface** owns the attribute's placement and argument shapes. This skill owns what happens after it |
| A machine caller with no account | `[ApiKey]` — an MVC filter, not a scheme. It establishes no principal, so `[HasPermission]` has nothing to check. Never pair the two on one endpoint |
| Rotate a signing key, or a key was committed | A configuration change per environment, never a code change. Rotating an access key logs that family out at once; a refresh key ends sessions at the next refresh. A key in committed history is compromised — rotate it; deleting the line changes nothing |
| A principal entity is renamed or moved | Treat it as a **data migration**. The type name is the routing claim, the grant rows' family column, and the per-request type resolution — rewrite the rows and check the selector arm in the same change. Moving assembly also silently disables per-request verification |
| Block an account, effective now | Change the row. The next request fails whatever token the caller holds — do not wait out a lifetime or invent a revocation list |
| A permission check returns the wrong answer after a grant change | Suspect the cache before the code: a sliding expiry plus a write path that evicts nothing means an active principal's entry may never lapse |
| Auth needs a new configuration value | Add it inside the bound security envelope so it inherits recursive validation and `ValidateOnStart`. A separate root section only for a concern that is not JWT auth |

## Anti-patterns

### Don't let a type name be the only thing holding the system together

A principal's type name is stamped into tokens, compared in selector arms, written into grant rows,
and resolved back into a `Type` per request. Nothing enforces agreement between those four sites,
and one of them fails **open**.

```csharp
// BAD — the same string, produced four different ways, agreeing by luck.
new Claim(JwtTokenPayload.ModelType, user.GetType().FullName!);   // minted into the token
_ when type == typeof(Customer).FullName => JwtScheme.Customer,   // scheme selection
ModelType = typeof(T).FullName!,                                  // persisted grant row

Type? type = Type.GetType(currentUser.GetModelType());            // per-request verification
if (type?.IsAssignableTo(typeof(IJwtUser)) == true && /* … */)
{
    // …the row check, the status check and the installation check ALL live in here,
    //   and silently never run when `type` is null
}
```

An unqualified type name only resolves inside the assembly that asks for it. Move the entity to
another assembly and resolution returns `null`, the block is skipped, and every deleted or blocked
principal is admitted — no exception, no log line, a 200 response. The grant rows go stale in the
same commit, and live tokens stop routing to their scheme.

```csharp
// GOOD — resolution failure on an authenticated request is a fault, not a pass
if (currentUser.IsAuthenticated() && NotAnonymousEndpoint(httpContext))
{
    Type? type = Type.GetType(currentUser.GetModelType());

    if (type?.IsAssignableTo(typeof(IJwtUser)) != true)
        throw new UnAuthorizedException(/* unresolvable principal type */);

    // …the three checks now always run
}

// GOOD — for a NEW discriminator, a stable constant instead of a runtime type name
public static class PrincipalFamily
{
    public const string User = nameof(User);
    public const string Customer = nameof(Customer);
}

new Claim(JwtTokenPayload.ModelType, PrincipalFamily.Customer);   // survives any move
```

Two rules follow. **Fail closed** — an authenticated request whose principal type cannot be
resolved is rejected, not waved through; that is the remedy that applies to code as it stands.
And **when introducing a new discriminator, use a stable constant per client family**, so a
namespace stays a code concern and the wire format is not. Where the type name is already the
discriminator, relocating a principal entity is a data migration: the routing arm, the live tokens
and the grant rows move together. Do not retrofit the constant onto an existing family — the
constant and the type name would disagree for every token already issued.

### Don't decide key encoding at the call site

```csharp
// BAD — one class signs with one encoding and validates with another
SecurityKey signing = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.RefreshKey));
// …elsewhere in the same file
byte[] validating = Encoding.ASCII.GetBytes(settings.RefreshKey);
```

```csharp
// GOOD — the settings class is the only place encoding is decided
IssuerSigningKey = settings.GetSecurityRefreshKey();

protected static string GenerateToken(SecurityKey key, /* … */);   // takes a key, not a string
```

The two encodings agree for every ASCII key — which is exactly why this survives testing and fails
on the first key containing a non-ASCII byte. Then every refresh fails, the two byte arrays differ
by one character nobody can see, and the symptom is an ordinary 401 with nothing in the logs.
Taking a `SecurityKey` rather than a `string` makes the mistake unwriteable: no call site is left
that could choose an encoding.

### Don't revoke a grant without evicting its cache

```csharp
// BAD — the row is gone and the check still passes
await repositoryWrapper.Repository<ModelPermission>().DeleteAsync(grant, cancellationToken);
// no eviction — while the cache that answers every request renews itself on each read:
cacheEntry.SlidingExpiration = expired;
```

```csharp
// GOOD — the sync verb validates the whole set, replaces it in one pass, and evicts
await gpModelPermissionService.SyncPermissionsAsync<User>(userId, remainingCodes, cancellationToken);

// GOOD — or, on any hand-written write path, evict the key you just invalidated
memoryCache.Remove(CacheKeys.GetKeyByModel<User>(userId));
```

Sliding expiry means "unused for N minutes", not "at most N minutes old". An active principal
refreshes the entry on every request, so the busier the account, the longer the revoked access
survives — for the caller who matters most, the revocation may never take effect at all. Every path
that writes a grant row must evict the affected key: the principal's key for a direct grant, the
role's key for a role's permissions. Prefer the sync verb precisely because it does this for you,
and consider an absolute expiry, which bounds the damage when someone forgets.

### Don't commit key material

```jsonc
// BAD — a live key in source control. It is compromised the moment it is pushed,
// and it stays compromised in history after the file is edited.
{
  "SecuritySettings": {
    "JwtSettingOptions": {
      "Default": { "Key": "c8b41f27-6d0a-4a19-9f52-3ce7b18d40aa" }
    }
  }
}
```

```jsonc
// GOOD — the committed file is the contract: every key that must be supplied,
// with the non-secret lifetimes that are worth reviewing in a diff
{
  "SecuritySettings": {
    "JwtSettingOptions": {
      "Default": {
        "Key": "<access-token signing key>",
        "RefreshKey": "<refresh-token signing key>",
        "AccessTokenExpirationInMinutes": 10080,
        "RefreshTokenExpirationInDays": 90
      }
    }
  }
}
```

The placeholder is not a missing value — it is how a new environment learns what to supply.
`ValidateOnStart` is what makes it safe: a placeholder that reaches a real environment fails at boot
rather than at the first 401 in production. The non-commitment rule and the startup validation are
one design, not two. If a key has ever been committed, rotate it; deleting the line changes nothing.
