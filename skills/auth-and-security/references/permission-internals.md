# Permission internals

Placing `[HasPermission]` on an endpoint — constructor shapes, argument order — is **api-surface**.
This file is what happens after the attribute is placed.

- [Where it lives](#where-it-lives)
- [The request path](#the-request-path)
- [1. From attribute to policy name](#1-from-attribute-to-policy-name)
- [2. From policy name to policy](#2-from-policy-name-to-policy)
- [3. From requirement to decision](#3-from-requirement-to-decision)
- [4. The permission catalogue](#4-the-permission-catalogue)
  - [Implied permissions](#implied-permissions)
  - [Adding a permission](#adding-a-permission)
- [5. Where grants live](#5-where-grants-live)
- [6. The grant services](#6-the-grant-services)
- [7. The per-request read and its cache](#7-the-per-request-read-and-its-cache)
- [8. Registration](#8-registration)

## Where it lives

```
Infrastructure/Facades/
  Auth/Permissions/{HasPermissionAttribute,PermissionPolicyProvider,
                    PermissionRequirement,PermissionAuthorizationHandler}.cs
  Auth/Startup.cs                    # registers the provider and the handler
  Definitions/AppPermissions.cs      # the catalogue: every permission that exists
  Identity/GrantPermission/
    Entities/{Permission,Role,RolePermission,ModelPermission,ModelRole}.cs
    Services/{GpModelPermissionService,GpModelRoleService,GpRolePermissionService}.cs
    Startup.cs
```

## The request path

1. The attribute turns its permission arguments into a **policy name** — one string.
2. The policy provider recognises that name and builds a policy carrying one requirement that holds
   the same string.
3. The handler splits the string back into codes and asks the grant service whether the current
   principal holds **any** of them.
4. The grant service reads direct and role-derived grants through an in-process cache, expands them
   by their implied permissions, and answers.

Nothing in that chain reads a claim except the principal's id. There is no policy registration
anywhere — policies are **manufactured on demand from their own names**, which is the only way an
attribute can accept arbitrary permission codes without a startup registration per combination.

## 1. From attribute to policy name

```csharp
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string[] schemes = default!, params string[] permissions)
    {
        Policy = string.Join(",", permissions.Select(x => AppPermissions.PrePermissions + x));

        AuthenticationSchemes = schemes?.Length > 0
            ? string.Join(",", schemes)
            : JwtBearerDefaults.AuthenticationScheme;
    }
}
```

The attribute is a **name builder and nothing else**. Two encodings happen here:

- Each code is prefixed with a marker constant and the results joined with commas into one name. The
  prefix is what the provider keys on; the comma is what the handler splits on. **Neither may appear
  inside a permission code.**
- Schemes are joined into `AuthenticationSchemes`, defaulting to the default bearer scheme. This is
  the only place a scheme reaches authorization — a principal established under an unlisted scheme
  is rejected before any permission is examined.

Because the framework treats a policy name as opaque, the whole permission set travels as one string
and is not decoded until the handler.

## 2. From policy name to policy

```csharp
internal class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        => FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(AppPermissions.PrePermissions, StringComparison.OrdinalIgnoreCase))
        {
            AuthorizationPolicyBuilder builder = new();
            builder.AddRequirements(new PermissionRequirement(policyName));
            return Task.FromResult<AuthorizationPolicy?>(builder.Build());
        }

        return FallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => FallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => Task.FromResult<AuthorizationPolicy?>(null);
}
```

- **Delegate, don't replace.** The custom provider intercepts only names carrying the marker;
  everything else goes to the wrapped default provider, so conventional named policies keep working.
- **The requirement receives the undecoded name.** Splitting and stripping is the handler's job, so
  the encoding is applied in one place and undone in one place.
- **`GetFallbackPolicyAsync` returns `null`, so an endpoint with no authorization metadata is not
  protected.** The fallback policy is what the framework applies to *unattributed* endpoints;
  returning `null` means protection is visible on the endpoint or it does not exist. That is the
  convention here — every protected endpoint says so. The consequence is that forgetting the
  attribute fails open, and nothing will tell you.

The requirement is a value carrier, no logic:

```csharp
internal class PermissionRequirement : IAuthorizationRequirement
{
    public string Permissions { get; }

    public PermissionRequirement(string permissions) => Permissions = permissions;
}
```

## 3. From requirement to decision

```csharp
internal class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IGpModelPermissionService gpModelPermissionService;
    private readonly ICurrentUser currentUser;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        IEnumerable<string> permissionCodes = requirement.Permissions
            .Split(",")
            .Select(x => x.Replace(AppPermissions.PrePermissions, string.Empty,
                                   StringComparison.OrdinalIgnoreCase));

        if (gpModelPermissionService.HasAnyPermissionWithCache<User>(
                currentUser.GetUserId(), permissionCodes))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

Four facts to carry away:

1. **Several permissions on one attribute mean ANY, not ALL.** The check is `HasAnyPermission…`.
   A conjunction is not expressible through this attribute.
2. **The principal supplies only an id.** Its claims are not consulted — Principle 6. An anonymous
   request yields an empty id, which matches no row, so the handler denies rather than throws.
3. **Failure is silence.** The handler never calls `context.Fail()`; it simply does not succeed,
   leaving other handlers free to satisfy the requirement. `Fail()` would veto them.
4. **The default principal family is the one that is permission-checked.** The grant tables are
   polymorphic, but this handler resolves grants for one principal type. Other client families are
   gated by scheme selection alone.

## 4. The permission catalogue

Every code that exists is declared in one static class; a code not declared there cannot be checked.

```csharp
public record PermissionDefinition(
    string Resource,
    string Action,
    string ModelType,
    string Guards = AppPermissions.AdminGuard,
    params string[] RelatePermissions)
{
    public string Code => Resource + Action;

    public string Name => $"{Resource} {Action}";
}

private static readonly PermissionDefinition[] Orders =
{
    new(AppResource.Orders,
        AppAction.Create,
        typeof(User).FullName!,
        JoinGuards(AdminGuard, BasicGuard),
        AppResource.Orders + AppAction.View,
        AppResource.Orders + AppAction.ViewDetail),

    new(AppResource.Orders,
        AppAction.View,
        typeof(User).FullName!,
        JoinGuards(AdminGuard, BasicGuard)),
};

public static IReadOnlyList<PermissionDefinition> All { get; } = GetAllValue();
```

- **`Code` is derived, never typed.** `Resource + Action` — optionally with a sub-resource constant
  between them — is the only way a code comes into existence, which is what lets the same constants
  appear in the catalogue and on the endpoint.
- **One array per resource, concatenated into `All`.** The aggregator is the single point of
  failure: a new array that nobody concatenates is silently absent from the catalogue.
- **`ModelType` records which principal family the permission is meant for.**
- **`Guards` are grant *presets*, not a per-request check.** Each definition lists the tiers allowed
  to hold it, separator-joined; the class exposes filtered views — note these filter by guard tier
  **and** by principal family, so they are single-family presets. They drive seeding
  (**ef-core-data-access**), never authorization.

### Implied permissions

```csharp
private static readonly Dictionary<string, PermissionDefinition> PermissionsValue
    = All.ToDictionary(x => x.Code, x => x);

public static IEnumerable<string> GetAllPermission(IEnumerable<string> permissionCodes)
{
    List<string> permissions = new();
    foreach (string permissionCode in permissionCodes)
    {
        if (!PermissionsValue.TryGetValue(permissionCode, out PermissionDefinition? definition))
        {
            throw new InvalidOperationException($"{permissionCode} not declared in {nameof(All)}");
        }

        permissions.Add(permissionCode);
        permissions.AddRange(definition.RelatePermissions);
    }

    return permissions;
}
```

- **The lookup dictionary must be `static readonly`, not a computed property.** This runs on every
  authorized request, once per held code. A `=>` property here rebuilds the whole dictionary on each
  access — and this method touches it twice per code.
- **Implication lives in the catalogue, not in the grant rows.** Granting every implied code
  explicitly at grant time would work, but it makes the implication invisible in the data and
  impossible to change afterwards — every existing principal would keep the old expansion.
- **Expansion is one level deep.** An implied permission is not itself expanded, so implications do
  not chain. Declare the full set on each definition.
- **A granted row whose code is not in the catalogue throws — during authorization.** Deleting a
  code without deleting its rows turns every request by an affected principal into a server error.
  Remove rows first.

### Adding a permission

1. Add the resource/action constants if the vocabulary lacks them.
2. Add the definition to that resource's array, with its `RelatePermissions` and `Guards`.
3. Make sure the permission table receives the new row — seeding is **ef-core-data-access**.
4. Put it on the endpoint — **api-surface**.
5. Grant it to whatever roles need it.

Miss step 3 and every sync naming the new code throws. Miss step 2 while the code is already granted
and the expansion throws on the next check for anyone holding it.

## 5. Where grants live

| Entity | Key | Meaning |
|---|---|---|
| `Permission` | `Code` (string PK) | the code exists; carries name, family, resource, guards |
| `Role` | id (`BaseEntity`, `ICode`) | a named bundle, scoped to one family |
| `RolePermission` | role id + code | the role holds the permission |
| `ModelPermission` | principal id + family + code | a principal holds the permission **directly** |
| `ModelRole` | principal id + family + role id | a principal holds the **role** |

- **`ModelId` + `ModelType` is a polymorphic owner** — no foreign key to any principal table,
  because any principal type can be the owner. That is what lets one grant schema serve every client
  family, and it is why **deleting a principal does not cascade its grants**. Orphan rows are the
  caller's problem.
- **`Permission.Code` is the primary key**, so the catalogue and the table share one identifier — no
  surrogate id, no mapping layer. Consequently `Permission` is reference data, not a lifecycle
  entity. `Role` is a full entity, because roles are created and edited at runtime.
  Deleting the permission **row** cascades to its grants — they are removed, not orphaned. Deleting
  the **catalogue entry** in code does nothing to the database, and is the dangerous one: the rows
  survive and the next check for anyone holding them throws. Two different deletions, one safe
  order — rows first, then the code.
- Table naming, composite keys and cascade behaviour are **ef-core-data-access**. What matters here:
  a grant is a row, not a claim.

## 6. The grant services

```csharp
internal static IServiceCollection AddGrantPermission(this IServiceCollection services)
    => services
        .AddScoped<IGpModelRoleService, GpModelRoleService>()
        .AddScoped<IGpRolePermissionService, GpRolePermissionService>()
        .AddScoped<IGpModelPermissionService, GpModelPermissionService>();
```

| Service | Owns |
|---|---|
| `IGpModelPermissionService` | direct grants; **all** read and check queries, including the cached one |
| `IGpModelRoleService` | which roles a principal holds |
| `IGpRolePermissionService` | which permissions a role carries |

Each is generic in the principal type, and that generic argument is the only thing that fills the
family column. Each offers the same verb set — give / revoke / **sync** / ask / read.

```csharp
Permission permission = await repositoryWrapper.Repository<Permission>()
    .Find(x => x.Code == permissionCode)
    .FirstOrDefaultAsync(cancellationToken)
    ?? throw new InvalidOperationException($"{nameof(permissionCode)} not found '{permissionCode}'.");

await repositoryWrapper.Repository<ModelPermission>().AddAsync(
    new ModelPermission
    {
        ModelId = modelId,
        ModelType = typeof(T).FullName!,
        PermissionCode = permission.Code,
    },
    cancellationToken);
```

- **The lookup before the insert is the integrity check** — a grant for an undeclared code is
  refused at write time rather than exploding at authorization time. It throws
  `InvalidOperationException`, not a client-facing exception: an unknown code is a bug, not user
  input. Exception families are **error-handling**.
- **Prefer the sync verb.** It validates the whole set against the permission table *before*
  deleting anything, replaces the set in one pass, and evicts its cache key. Revoke validates only
  that the grant row exists, and evicts nothing.
- **Read helpers return `IQueryable`**, so callers compose paging or projection rather than
  receiving a materialised list; the all-permissions read unions direct and role-derived grants and
  de-duplicates by code.

## 7. The per-request read and its cache

```csharp
public bool HasAnyPermissionWithCache<T>(Guid modelId, IEnumerable<string> permissionCodes)
{
    List<string> held = new();
    held.AddRange(CacheModelPermission<T>(modelId));

    IEnumerable<Guid> roleIds = repositoryWrapper.Repository<ModelRole>()
        .Find(x => x.ModelId == modelId && x.ModelType == typeof(T).FullName)
        .Select(x => x.RoleId)
        .ToArray();
    held.AddRange(CacheRolePermission(roleIds));

    return AppPermissions.GetAllPermission(held)
        .Distinct()
        .Any(code => permissionCodes.Contains(code));
}
```

- **Two kinds of cache entry, not one**: direct grants keyed by principal, role permissions keyed by
  role. Caching the union per principal would mean editing a role invalidates nothing; keying by
  role means one eviction covers every principal holding it. Cache keys are derived from the type
  name plus the id, so principal and role entries cannot collide.
- **The principal→role lookup is not cached** and runs on every authorized request — so adding or
  removing a role on a principal takes effect on the very next request, while changing the
  permissions *behind* a role does not until that role's entry is evicted or lapses.
- **Implied permissions are expanded after the cache**, so changing the implication lists in code
  takes effect on deploy with no eviction.
- **Entries use a sliding expiry from configuration, in-process by design.** This is a hot
  per-request lookup with a bounded staleness window, not shared state — Redis conventions are
  **distributed-caching**, and moving this there would trade a bounded window for a network hop on
  every authorized request. With more than one instance, each holds its own copy and they expire
  independently. A busy principal's entry never lapses on its own — which is why the eviction rule
  below is not optional.
- **Any write path that changes grants must evict the affected key.** Each service evicts only its
  own: the principal-permission sync evicts the principal's key, the role-permission sync evicts the
  role's key. A new write path that skips this leaves the change invisible.

## 8. Registration

```csharp
private static IServiceCollection AddPermissions(this IServiceCollection services)
    => services
        .AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>()
        .AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

**Singleton provider, scoped handler — this pairing is not a style choice.** The framework resolves
the policy provider once, from the root container, so it must not depend on anything scoped; it
depends only on the authorization options and builds a stateless policy per name. The handler is
where per-request dependencies live — the current principal, repositories — and it is resolved per
request. Registering the handler as a singleton would capture a scoped repository in the root
container and fail at startup, or worse, leak one request's state into the next.

Placement and composition-root rules are **facade-module-architecture**.

> **One string identifies a principal type in five places.** `typeof(T).FullName` is the
> `modelType` claim stamped into a token, the comparison in the scheme selector, the `ModelType`
> column on every grant row, the `ModelType` on permission and role rows, and the `ModelType` field
> on every catalogue definition. Nothing enforces agreement. Move a principal entity to another
> namespace and its tokens route to the wrong scheme *and* its grants stop matching — no compile
> error, no failed migration, no log line. Relocating or renaming a principal entity is a data
> change: plan the row rewrite in the same commit.
