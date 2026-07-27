# Principal, request verification and secrets

Who the caller is, how that survives past the authentication handler, what is re-checked per
request, and how the settings behind it are bound and protected.

- [1. The current principal](#1-the-current-principal)
  - [Reading claims](#reading-claims)
  - [Populating it](#populating-it)
  - [Registering it — the part that is easy to get wrong](#registering-it--the-part-that-is-easy-to-get-wrong)
- [2. Verifying the principal each request](#2-verifying-the-principal-each-request)
  - [Ordering](#ordering)
- [3. Settings and secrets](#3-settings-and-secrets)
  - [The non-commitment rule](#the-non-commitment-rule)
- [4. API keys — the secondary gate](#4-api-keys--the-secondary-gate)

```
Infrastructure/Facades/
  Auth/{CurrentUser,CurrentUserMiddleware,ClaimsPrincipalExtension,SecuritySettings,Startup}.cs
  Auth/ApiKey/{ApiKeyAttribute,ApiKeySettings,Startup}.cs
  Middleware/VerifyJwtUserMiddleware.cs
```

## 1. The current principal

Application code never reads claims from the HTTP context. It injects one scoped service — that
service is the single seam between the authenticated request and everything downstream, and reading
a claim through the context instead re-implements the seam, couples business code to the web host,
and skips the anonymous rule the middleware applies.

```csharp
public interface ICurrentUser
{
    string? Name { get; }

    Guid GetUserId();

    string GetModelType();

    bool IsAuthenticated();

    string? GetApplicationId();
}

public interface ICurrentUserInitializer
{
    void SetCurrentUser(ClaimsPrincipal user);
}
```

**Two interfaces, one class.** Reading the principal is available everywhere; *setting* it is a
separate capability only the middleware needs. Split this way, no handler can overwrite the current
principal — the write method is not on the interface it injects.

```csharp
public class CurrentUser : ICurrentUser, ICurrentUserInitializer
{
    private ClaimsPrincipal? user;

    public string? Name => user?.Identity?.Name;

    public bool IsAuthenticated() => user?.Identity?.IsAuthenticated is true;

    public Guid GetUserId()
        => IsAuthenticated() ? Guid.Parse(user?.GetUserId() ?? Guid.Empty.ToString()) : Guid.Empty;

    public string GetModelType()
        => IsAuthenticated() ? user?.GetModelType() ?? string.Empty : string.Empty;

    public string? GetApplicationId()
        => IsAuthenticated() ? user?.GetApplicationId() ?? string.Empty : string.Empty;

    public void SetCurrentUser(ClaimsPrincipal user) => this.user ??= user;
}
```

- **The accessors are total** — they answer for an anonymous request instead of throwing. Code that
  must not run anonymously checks `IsAuthenticated()`; it does not null-guard the id. A consequence
  worth knowing: an anonymous request yields an empty id, which matches no row, so downstream checks
  — including the permission handler — deny rather than throw.
- **`??=` makes the principal write-once per request.** The first writer wins; nothing later in the
  pipeline can substitute a different principal.
- **Claim keys are never spelled here.** Each getter delegates to a `ClaimsPrincipal` extension that
  reads a payload constant — the same constants the token generator wrote
  (`references/jwt-and-tokens.md`).

### Reading claims

```csharp
public static class ClaimsPrincipalExtension
{
    public static string? GetUserId(this ClaimsPrincipal principal)
        => principal.FindFirstValue(JwtTokenPayload.Identification);

    public static string? GetModelType(this ClaimsPrincipal principal)
        => principal.FindFirstValue(JwtTokenPayload.ModelType);

    public static string? GetApplicationId(this ClaimsPrincipal principal)
        => principal.FindFirstValue(JwtTokenPayload.ApplicationId);

    private static string? FindFirstValue(this ClaimsPrincipal principal, string claimType)
        => principal is null
            ? throw new ArgumentNullException(nameof(principal))
            : principal.FindFirst(claimType)?.Value;
}
```

One extension per claim the application actually consumes, each named for the concept rather than
the claim, over one private lookup. Adding a claim means a constant, a reader here, and — if it
belongs to the request identity — a member on the current-user interface. A raw
`FindFirst("someString")` anywhere else is the thing this file exists to prevent. This is the read
side of the contract whose write side is `IJwtUser.UseClaims`.

### Populating it

```csharp
public class CurrentUserMiddleware
{
    private readonly RequestDelegate next;

    public CurrentUserMiddleware(RequestDelegate next) => this.next = next;

    public async Task Invoke(HttpContext httpContext, ICurrentUserInitializer currentUserInitializer)
    {
        if (httpContext.User.Identity is not null
            && httpContext.User.Identity.IsAuthenticated
            && httpContext.GetEndpoint()?.Metadata?.GetMetadata<IAllowAnonymous>() is null)
        {
            currentUserInitializer.SetCurrentUser(httpContext.User);
        }

        await next(httpContext);
    }
}
```

- **It copies an already-established principal; it never authenticates.** Two ordering requirements
  follow, and both are real: it must run **after authentication**, or the context principal is
  anonymous; and **after endpoint routing**, or `GetEndpoint()` is `null` and the anonymous check
  silently never applies.
- **The scoped dependency is a method parameter, not a constructor parameter.** Middleware instances
  are effectively singletons; a scoped service in the constructor captures the first request's
  instance for the life of the process. Per-invocation parameters are resolved per request.
- **Anonymous endpoints leave the principal unset by design**, even when a valid token was sent. An
  endpoint that does not need a principal must behave identically for every caller.

### Registering it — the part that is easy to get wrong

```csharp
internal static IServiceCollection AddCurrentUser(this IServiceCollection services)
    => services
        .AddScoped<ICurrentUser, CurrentUser>()
        .AddScoped(sp => (ICurrentUserInitializer)sp.GetRequiredService<ICurrentUser>());

internal static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app)
    => app.UseMiddleware<CurrentUserMiddleware>();
```

The second line resolves the **first registration and casts it**, so one instance per request serves
both interfaces. Registering the initializer with its own concrete registration compiles, runs, and
produces a second object: the middleware populates one nobody reads, and every injected reader
reports anonymous — with no error to explain it.

## 2. Verifying the principal each request

```csharp
public async Task InvokeAsync(
    HttpContext httpContext, ICurrentUser currentUser, IRepositoryWrapper repositoryWrapper)
{
    Type? type = Type.GetType(currentUser.GetModelType());

    if (type?.IsAssignableTo(typeof(IJwtUser)) == true
        && httpContext.User.Identity?.IsAuthenticated == true
        && httpContext.GetEndpoint()?.Metadata?.GetMetadata<IAllowAnonymous>() is null)
    {
        // resolve the repository for `type`, read the row by primary key, no tracking
        IJwtUser? user = /* … */;

        if (user is null)
            throw new UnAuthorizedException(/* not-found message for `type` */);

        if (user.Status is OperationStatus.Lock)
            throw new UnAuthorizedException(/* blocked message for `type` */);

        // the installation check, below
        if (/* principal is installation-bound and the request is not from its installation */)
            throw new UnAuthorizedException(/* invalid-installation message */);
    }

    await next(httpContext);
}
```

> The body above is **elided**: the real implementation resolves the repository, the query and the
> message class for `type` by reflection, and that plumbing is not the lesson. What matters is the
> contract — three checks, three rejections, one exception family.

| Check | Rejects | Why the token cannot answer it |
|---|---|---|
| the row exists | a principal deleted since the token was issued | the token cannot know it was deleted |
| status is not blocked | a principal blocked since the token was issued | blocking must take effect immediately |
| installation matches | an installation-bound token replayed elsewhere | the claim says which installation; only the row says which is current |

1. **It does not re-authenticate.** Signature and expiry were settled upstream; this settles
   *existence and standing*. The principal is already populated, so it runs after the principal
   layer and before authorization.
2. **The principal's own type drives the lookup.** The type name from the token is resolved to a
   `Type`, checked to be a token-bearing principal, and used to open that entity's repository — so
   one middleware serves every client family without a switch.
3. **The read is by primary key and no-tracking.** It happens on every authenticated request; it
   must not enlist in the change tracker. Query conventions are **ef-core-data-access**.
4. **The installation check is family-scoped and opt-outable.** It applies only to principals
   implementing the installation-bound contract, and the helper that evaluates it also checks for an
   opt-out attribute on the endpoint — so the predicate is "no opt-out **and** the ids differ". The
   opt-out exists because the endpoint that re-binds an installation must be reachable by exactly
   the caller whose installation does not yet match.
5. **A fourth check would apply to every client family**, because the middleware is type-agnostic.
   A rule for one family must be expressed the way the installation check is: a contract test plus
   an opt-out.

All three rejections are one exception family carrying a message keyed to the principal type. The
family and the response envelope are **error-handling**; the text is **message-keys**.

> **Hazard.** The type is recovered with `Type.GetType(name)` from a name stamped into the token. An
> unqualified name only resolves inside the assembly that asks. If a principal entity ever moves
> assembly, resolution returns `null`, the whole block is skipped, and the middleware **silently
> stops verifying anything** — it fails open, with no error anywhere. This is the fourth and most
> consequential site of the same type-name-as-data pattern: the claim the generator stamps, the
> selector arm that routes on it, the grant rows keyed by it, and this resolution. Nothing enforces
> agreement between them. Treat relocating a principal entity as a data migration.

### Ordering

```
UseRouting()                   # endpoint metadata becomes available
…
UseExceptionHandlerMiddleware()
UseAuthentication()            # a scheme establishes the principal
UseCurrentUser()               # copy it into the request-scoped seam
UseVerifyJwtUserMiddleware()   # the row still exists, still allowed, still this installation
UseAuthorization()             # permissions, from the grant tables
```

Each stage depends on the one above it. Before routing, the anonymous check has no metadata to read.
Placing verification before the principal layer gives it nothing to verify; placing it after
authorization means a blocked principal's permissions are evaluated before anyone notices they are
blocked. The exception middleware sits above all of them so the rejections come back in the standard
envelope.

## 3. Settings and secrets

```csharp
public class SecuritySettings : IValidatableObject
{
    public JwtSettingOptions JwtSettingOptions { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required();
}
```

This is the bound root for the security configuration file. Its job is to give auth configuration
one named section and one validated root — binding is recursive and validated at startup, so a
malformed block fails the process at boot. The mechanics are in `references/jwt-and-tokens.md`; do
not duplicate them.

New auth settings belong **inside** this envelope as a property, so they inherit that boot-time
validation. The only reason to bind a separate root section is a concern that is not JWT auth at all
— the API key below is the example, and it binds from a root section in the general application
settings file rather than the security file.

### The non-commitment rule

**Committed configuration carries shape and non-secret values. Key material is supplied per
environment and is never committed.**

- **A placeholder is not a missing value — it is the contract.** The committed file is what tells a
  new environment which keys must be supplied. Omit the block and the next deployer has to read the
  C# to discover what to configure.
- **Startup validation is what makes this safe.** A placeholder reaching a real environment fails at
  boot, not at the first 401 in production. The non-commitment rule and the validation rule are one
  design, not two.
- **Environment overrides carry only what differs**; unspecified keys layer through from the base.
- **Lifetimes belong in the committed file.** They are reviewable decisions, and a lifetime that
  differs silently between environments produces bugs that surface only hours later.
- **This skill prescribes no secret store.** How values reach an environment is a deployment
  decision. What is fixed: the value in source control is never a live key, and rotating a key is a
  configuration change, never a code change.

Two operational facts follow from the per-scheme settings design:

- Keys are separate per scheme and per token kind, so rotation scope is bounded. Rotating a signing
  key logs out every holder of that family's access tokens immediately; rotating a refresh key ends
  their sessions at the next refresh.
- **A key present in committed history is compromised regardless of later edits.** Rotation, not
  deletion, is the fix.

## 4. API keys — the secondary gate

Some callers are machines with no account: webhooks, internal jobs, integration callbacks. They are
gated by a shared key, not a token.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ApiKeyAttribute : Attribute, IAuthorizationFilter
{
    private const string ApiKeyHeader = "X-API-Key";
    private readonly string keyName;

    public ApiKeyAttribute(string keyName = nameof(ApiKeySettings.Default)) => this.keyName = keyName;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!IsApiKeyValid(GetConfiguredKey(context.HttpContext), GetSubmittedKey(context.HttpContext)))
        {
            context.Result = new UnauthorizedResult();
        }
    }
}

public class ApiKeySettings : IValidatableObject
{
    public string Default { get; set; } = default!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required();
}
```

- **It is an MVC authorization filter, not an authentication scheme.** It establishes no principal,
  so the current-user seam stays anonymous and the permission attribute has nothing to check. Do not
  combine the two on one endpoint. Use it for machine-to-machine callers only; anything acting on
  behalf of a person needs a token, because only a token produces a principal.
- **Keys are named.** The constructor argument selects a property on the settings class, defaulting
  to the primary one, so a second caller gets its own key without a second attribute type. Note the
  selection is by name at runtime, so a typo surfaces as a failure on the first request rather than
  at startup.
- **Compare with `CryptographicOperations.FixedTimeEquals`, never `==`.** A naive comparison leaks
  the key's prefix through timing. Know its one limit: it returns `false` immediately when the
  lengths differ, so length remains distinguishable — the contents are what must be defended.
- **Same settings shape, same non-commitment rule.** The key is bound from configuration, validated
  on start, and never committed.
- **Rejection short-circuits with a bare 401** — it does not pass through the exception middleware,
  so the body differs from every other error in the API. See **error-handling** for the standard
  envelope; know the difference before putting this on a public surface.
