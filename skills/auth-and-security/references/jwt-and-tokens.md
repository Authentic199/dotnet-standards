# JWT and tokens

Scheme constants, per-scheme settings and registration, then token generation and the refresh
flow.

- [1. Where it lives](#1-where-it-lives)
- [2. The settings classes](#2-the-settings-classes)
- [3. Registration](#3-registration)
- [4. The forwarding selector](#4-the-forwarding-selector)
- [5. Adding a client scheme](#5-adding-a-client-scheme)
- [6. Configuration file shape](#6-configuration-file-shape)
- [7. Token generation](#7-token-generation)
  - [The claim-key catalogue](#the-claim-key-catalogue)
  - [The two contracts](#the-two-contracts)
  - [The generator](#the-generator)
  - [Wiring](#wiring)
- [8. The login and refresh flow](#8-the-login-and-refresh-flow)

## 1. Where it lives

```
Infrastructure/Facades/Auth/Jwt/
  JwtScheme.cs           # the scheme constants, nothing else
  JwtSettingOptions.cs   # JwtSettingOptions + JwtSettings
  Startup.cs             # internal static AddJwtAuth(this IServiceCollection, IConfiguration)
```

`AddJwtAuth` is `internal`, so only the auth facade's own composition root can call it — the Web
project cannot reach `AddAuthentication` even by accident. Placement rules:
**facade-module-architecture**.

## 2. The settings classes

```csharp
public class JwtSettingOptions : IValidatableObject
{
    public JwtSettings Default { get; set; } = new();

    public JwtSettings Device { get; set; } = new();

    public JwtSettings Customer { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required();
}

public class JwtSettings : IValidatableObject
{
    public string Key { get; set; } = default!;

    public string RefreshKey { get; set; } = default!;

    public double AccessTokenExpirationInMinutes { get; set; }

    public double RefreshTokenExpirationInDays { get; set; }

    public string Issuer { get; set; } = default!;

    public string IsAudience { get; set; } = default!;

    public SymmetricSecurityKey GetSecurityKey()
        => new(Encoding.UTF8.GetBytes(Key));

    public SymmetricSecurityKey GetSecurityRefreshKey()
        => new(Encoding.UTF8.GetBytes(RefreshKey));

    public DateTime GetAccessTokenExpired()
        => DateTime.UtcNow.AddMinutes(AccessTokenExpirationInMinutes);

    public DateTime GetRefreshTokenExpired()
        => DateTime.UtcNow.AddDays(RefreshTokenExpirationInDays);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required(nameof(Issuer), nameof(IsAudience));
}
```

- **One property per scheme on the options root.** The property name is both the binding key and
  the scheme constant, so `JwtSettingOptions.Device` ↔ `JwtScheme.Device` is a one-line check. A
  dictionary keyed by scheme name would also bind, but loses the compile-time property and the
  per-scheme validation.
- **`double` expirations**, because they are fed straight to `AddMinutes` / `AddDays`. Note that
  `Required` treats a type default as missing, so a lifetime left at `0` fails at startup rather
  than minting already-expired tokens.
- **The four `Get*` helpers are the only place encoding and clock policy are decided.** Never write
  `Encoding.*.GetBytes(settings.Key)` at a call site — a key built with one encoding at
  registration and another at signing rejects every token whose key contains a non-ASCII byte, and
  the failure looks like a generic 401.
- **`Required(...)` takes the properties to *skip*.** `Required()` with no arguments asserts the
  whole object is populated; `Required(nameof(Issuer), nameof(IsAudience))` asserts everything
  **except** those two, which makes them optional. That is why a configuration file can omit them
  and still pass `ValidateOnStart`. What they are for is covered under token generation below.
- This is `IValidatableObject`, so an unset signing key is a startup failure rather than a 401 on
  the first login of the day.

## 3. Registration

```csharp
internal static IServiceCollection AddJwtAuth(
    this IServiceCollection services, IConfiguration configuration)
{
    services.AddOptions<SecuritySettings>()
        .BindConfiguration(nameof(SecuritySettings))
        .ValidateDataAnnotationsRecursively()
        .ValidateOnStart();

    SecuritySettings securitySettings = configuration
        .GetRequiredSection(nameof(SecuritySettings))
        .Get<SecuritySettings>()!;
    JwtSettingOptions jwt = securitySettings.JwtSettingOptions;

    services.AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = JwtScheme.MultipleScheme;
        options.DefaultScheme = JwtScheme.MultipleScheme;
    })
    .AddJwtBearer(JwtScheme.Default,  o => o.Configure(jwt.Default))
    .AddJwtBearer(JwtScheme.Device,   o => o.Configure(jwt.Device))
    .AddJwtBearer(JwtScheme.Customer, o => o.Configure(jwt.Customer))
    .AddPolicyScheme(JwtScheme.MultipleScheme, JwtScheme.MultipleScheme, o =>
    {
        o.ForwardDefaultSelector = ForwardByPrincipalType;
    });

    return services;
}

private static void Configure(this JwtBearerOptions options, JwtSettings settings)
{
    options.SaveToken = true;
    options.TokenValidationParameters = new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = settings.GetSecurityKey(),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero,
    };
}
```

Every bearer registration is identical apart from its settings block, so it is written once and
applied per scheme. Existing code inlines this block once per scheme instead; prefer the shared
extension in new work, because a validation change that has to be made three times gets made twice.

- **The settings are read twice, deliberately.** `AddOptions<…>().BindConfiguration(…)` registers
  them for injection *later*; `GetRequiredSection(…).Get<…>()` materialises them *now*, because
  `AddJwtBearer` needs the signing key during registration, before any `IOptions<T>` can be
  resolved. `GetRequiredSection` turns a missing section into a startup failure.
- **`ValidateDataAnnotationsRecursively` + `ValidateOnStart`.** Recursive, because the validation
  that matters is on the nested per-scheme class, not on the root. On start, because a process that
  boots with an unset key is a process that 401s every request in production.
- **`ClockSkew = TimeSpan.Zero`.** The framework default grants five minutes of grace, which makes
  a short access-token lifetime meaningless and makes expiry tests flaky.
- **`ValidateIssuer` and `ValidateAudience` are off.** These tokens are minted and consumed by the
  same service, and the separation between client families is carried by a distinct signing key per
  scheme, not by a claim.

## 4. The forwarding selector

```csharp
private static string ForwardByPrincipalType(HttpContext context)
{
    string? authorization = context.Request.Headers[HeaderNames.Authorization];
    if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer "))
        return JwtScheme.Default;

    string token = authorization["Bearer ".Length..].Trim();
    JwtSecurityTokenHandler handler = new();
    if (!handler.CanReadToken(token))
        return JwtScheme.Default;

    string? type = handler.ReadJwtToken(token).Claims
        .FirstOrDefault(x => x.Type == JwtTokenPayload.ModelType)?.Value;

    return type switch
    {
        _ when type == typeof(User).FullName     => JwtScheme.Default,
        _ when type == typeof(Device).FullName   => JwtScheme.Device,
        _ when type == typeof(Customer).FullName => JwtScheme.Customer,
        _ => JwtScheme.Default,
    };
}
```

Three properties of this method matter:

1. **It reads, it does not validate.** `ReadJwtToken` parses an unverified token purely to choose a
   scheme; the chosen scheme then does the real signature check. Nothing read here may be trusted
   for anything else.
2. **Every unknown or unreadable input falls through to the default scheme.** The fallback rejects
   rather than accepts — the default scheme's key will not match a foreign token — and it keeps the
   failure an ordinary 401 from a real bearer handler instead of a selector-thrown 500.
3. **The claim it switches on is the principal type**, written at token generation as the entity's
   `FullName`. The selector and the generator are two halves of one contract: change the claim on
   one side and the other must change in the same commit.

Keep an explicit arm per scheme even where it duplicates the fallback — the arm list is how the
next person adding a client family sees what to add.

## 5. Adding a client scheme

1. Add the `const` to `JwtScheme`.
2. Add a `JwtSettings` property of the same name to `JwtSettingOptions`.
3. Add the block to the base configuration file (and to the environment override only if the local
   value differs).
4. Add one `.AddJwtBearer(JwtScheme.X, o => o.Configure(jwt.X))` line.
5. Add the selector arm, keyed on the principal type the generator stamps.

Miss step 3 and startup fails — `ValidateOnStart` reports the empty key. Miss step 5 and the new
client's tokens are silently handed to the default scheme, which validates them against the wrong
key, so every request from that client 401s while everything else works.

## 6. Configuration file shape

Auth settings live in their own configuration file rather than in the general application settings
— one file per concern, so the key-bearing file is the one you protect. The section name matches
the settings class name and each nested key matches a scheme property; rename either and binding
silently yields defaults, which `ValidateOnStart` then reports.

```jsonc
{
  "SecuritySettings": {
    "JwtSettingOptions": {
      "Default": {
        "Key": "<access-token signing key>",
        "RefreshKey": "<refresh-token signing key>",
        "AccessTokenExpirationInMinutes": 10080,
        "RefreshTokenExpirationInDays": 90
      },
      "Device": {
        "Key": "<access-token signing key>",
        "RefreshKey": "<refresh-token signing key>",
        "AccessTokenExpirationInMinutes": 259200,
        "RefreshTokenExpirationInDays": 360
      }
    }
  }
}
```

The committed file carries the shape and the non-secret lifetimes; key values are placeholders
replaced per environment — see **`references/principal-and-secrets.md`**. Configuration layers per
key, base file first and environment override second, so the override repeats **only** the scheme
blocks whose local values differ and inherits the rest.

## 7. Token generation

```
Infrastructure/Facades/
  Definitions/JwtTokenPayload.cs   # every claim key, as consts
  Identity/
    Base/{IJwtUser,IRefreshToken}.cs
    JwtToken/{JwtTokenGenerator,Startup}.cs
```

### The claim-key catalogue

```csharp
public static class JwtTokenPayload
{
    public const string ModelType = "modelType";
    public const string Identification = "identification";
    public const string Session = "session";
    public const string ApplicationId = "applicationId";
    // … one const per claim the service ever writes or reads
}
```

One file, one const per claim, camel-cased values. The writer, the scheme selector and every reader
of the principal all go through these constants, so a rename is a compile error rather than a
silent `null` at runtime.

| Claim | Token | Written by | Purpose |
|---|---|---|---|
| `identification` | access | `IJwtUser.GetDefaultClaims` | the principal's id |
| `modelType` | access | `IJwtUser.GetDefaultClaims` | selects the scheme — see the selector above |
| `session` | refresh | the calling service | ties the token to one stored session row |
| family-specific | access | the principal's `UseClaims` override | payload one client family needs |

`modelType` is the contract between minting and the scheme selector: the generator writes the
principal's `GetType().FullName`, the selector compares against `typeof(X).FullName`. **They change
together** — rename on one side and that client family routes silently to the default scheme, whose
key will not validate its tokens.

### The two contracts

```csharp
public interface IUser
{
    public OperationStatus Status { get; }
}

public interface IJwtUser : IUser, IGuidIdentify
{
    public IEnumerable<Claim> UseClaims() => GetDefaultClaims(this);

    protected static IEnumerable<Claim> GetDefaultClaims(IJwtUser user) => …;
}

public interface IRefreshToken
{
    public string? Token { get; set; }

    public DateTime ExpireTime { get; set; }

    public Guid SessionId { get; set; }
}
```

- **`IJwtUser` is what makes an entity loginable.** It requires a Guid id and an operation status,
  because both the login path and the per-request re-check need them. The status is two-valued —
  allowed, or blocked; anything richer belongs to the business module, not here.
- **`IRefreshToken` is a persistence contract**, implemented by one refresh-token entity per
  principal family, which adds the foreign key to its principal. The entity, its configuration and
  its queries are **ef-core-data-access**; purging expired rows is **background-worker**.

An entity needing more in its token overrides `UseClaims`, starting from the default set:

```csharp
public IEnumerable<Claim> UseClaims()
    => IJwtUser.GetDefaultClaims(this)
        .Append(new Claim(JwtTokenPayload.ApplicationId, ApplicationId));
```

Start from `GetDefaultClaims(this)` rather than re-listing the default claims by hand — existing
overrides re-list them, so a change to the default set reaches every principal except those.

### The generator

```csharp
public interface IJwtTokenGenerator
{
    JwtSettings GetSettingByScheme(string? scheme = null);

    string GenerateAccessToken(JwtSettings settings, IJwtUser user);

    string GenerateRefreshToken(JwtSettings settings, params Claim[] claims);

    SecurityToken? ValidateRefreshToken(JwtSettings settings, string refreshToken);

    string? GetClaimsValue(string? token, string claimType)
    {
        try
        {
            return new JwtSecurityTokenHandler()
                .ReadJwtToken(token).Claims
                .FirstOrDefault(x => x.Type == claimType)?.Value;
        }
        catch
        {
            return null;
        }
    }

    protected static string GenerateToken(
        SecurityKey key, string issuer, string audience,
        DateTime? expiresAt, IEnumerable<Claim> claims)
    {
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(issuer, audience, claims,
            expires: expiresAt, signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

`GetClaimsValue` is a default interface method and `GenerateToken` a `protected static` one: both
are behaviour belonging to the contract rather than to any implementation, so neither is duplicated
per implementation and `GenerateToken` is never exposed to callers.

- **`GenerateToken` takes a `SecurityKey`, not a key string.** Callers pass `GetSecurityKey()` or
  `GetSecurityRefreshKey()`, which keeps the settings helpers the only place encoding is ever
  decided. Accepting a string here would put a second `Encoding.*.GetBytes` in the codebase, and a
  signing/validating encoding mismatch shows up only as "everything 401s", with nothing in the logs.
- **Access and refresh differ in three things**: which key signs them, how long they live, and what
  claims they carry. A refresh token presented as a bearer token therefore fails the scheme's
  signature check outright.
- **`GetClaimsValue` reads an unverified token** — the same posture as the scheme selector. It
  returns `null` on anything unreadable rather than throwing, because its callers reject with their
  own domain error. Nothing it returns may be trusted; it locates, it does not authorise.

```csharp
public class JwtTokenGenerator : IJwtTokenGenerator
{
    public JwtTokenGenerator(IOptions<SecuritySettings> settingOption)
        => AllSettings = settingOption.Value.JwtSettingOptions;

    public JwtSettingOptions AllSettings { get; }

    public JwtSettings GetSettingByScheme(string? scheme = null)
        => (JwtSettings?)AllSettings.GetType()
               .GetProperty(scheme ?? nameof(JwtSettingOptions.Default))?.GetValue(AllSettings)
           ?? throw new InvalidOperationException(
               $"The scheme is not declared on {typeof(JwtSettingOptions).FullName}.");

    public string GenerateAccessToken(JwtSettings settings, IJwtUser user)
        => IJwtTokenGenerator.GenerateToken(
            settings.GetSecurityKey(), settings.Issuer, settings.IsAudience,
            settings.GetAccessTokenExpired(), user.UseClaims());

    public string GenerateRefreshToken(JwtSettings settings, params Claim[] claims)
        => IJwtTokenGenerator.GenerateToken(
            settings.GetSecurityRefreshKey(), settings.Issuer, settings.IsAudience,
            settings.GetRefreshTokenExpired(), claims);

    public SecurityToken? ValidateRefreshToken(JwtSettings settings, string refreshToken)
    {
        JwtSecurityTokenHandler tokenHandler = new();
        TokenValidationParameters validationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = settings.GetSecurityRefreshKey(),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
        };

        try
        {
            tokenHandler.ValidateToken(refreshToken, validationParameters, out SecurityToken? validated);
            return validated;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
```

- **`GetSettingByScheme` resolves a scheme name to a settings property by reflection**, defaulting
  to the default scheme. This is why the `JwtSettingOptions` property must be named exactly like the
  `JwtScheme` const: a mismatch is a runtime throw at mint time, not a compile error. The throw is
  an `InvalidOperationException` deliberately — an unknown scheme is a wiring bug that must fail at
  the call site, not a 401 handed to a client. Exception families are **error-handling**.
- **Expiries come from `JwtSettings`**, so "how long is a token valid" has one answer, shared by the
  token being signed and the row being persisted.
- **`Issuer` and `IsAudience` are stamped, not verified.** They become the token's `iss` and `aud`,
  but the bearer handlers run with `ValidateIssuer`/`ValidateAudience` off, so they are descriptive
  payload — which is why both are optional in configuration. The boundary is the per-scheme signing
  key. Do not tighten one side alone: turning validation on while live tokens carry a different
  issuer invalidates every session at once.
- **Lifetime validation keeps the framework default `RequireExpirationTime`.** Every token this
  service mints stamps an expiry, so requiring one rejects nothing legitimate and rejects a
  crafted token that omits it.
- **Validation failure returns `null`, not an exception.** The caller decides what a bad refresh
  token means and which message it carries — **message-keys** owns the wording.

`ValidateRefreshToken` is called from the refresh request's own validator, not from the service: the
validator resolves its family's settings via `GetSettingByScheme(JwtScheme.X)`, checks the
signature, and rejects before the service ever runs its stored-row comparison. Two independent
gates — signature at the edge, currency in the database. Validator structure is **module-feature**.

> **Honesty note.** The taught shape diverges from existing code in four places, all consequences of
> the settings decree in the previous section: expiry maths moved off the generator onto
> `JwtSettings` (existing code parses string settings on the generator); both mint calls and the
> refresh validation now derive keys through the settings helpers (existing code passes raw key
> strings and builds the validation key inline with a different encoding); the inert `ValidIssuer`/
> `ValidAudience` assignments are dropped; and `RequireExpirationTime = false` is dropped.

### Wiring

```csharp
internal static class Startup
{
    internal static IServiceCollection AddJwtTokenService(this IServiceCollection services)
    {
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        // the expired-token cleanup hosted service is registered here too — background-worker
        return services;
    }
}
```

Scoped, registered by the Identity facade's own `Startup`, never from the Web project.

## 8. The login and refresh flow

Seen from a module service. Persistence is **ef-core-data-access**, message text is
**message-keys**, exception types are **error-handling**.

```csharp
public async Task<AuthUserResponse> AuthenticateAsync(
    AuthUserRequest request, CancellationToken cancellationToken = default)
{
    User user = await repositoryWrapper.Repository<User>()
        .Find(x => x.Username == request.Username || x.Email == request.Username)
        .FirstOrDefaultAsync(cancellationToken)
        ?? throw new BadRequestException(Messages<User>.NotFound());

    if (user.Status == OperationStatus.Lock)
    {
        throw new BadRequestException(Messages<User>.Blocked());
    }

    if (!VerifyPassword(request.Password, user.Password))
    {
        throw new BadRequestException(Messages<User>.Invalid(x => x.Password));
    }

    return await AuthenticateAsync(user, cancellationToken);
}

public async Task<AuthUserResponse> RefreshAsync(
    UserRefreshTokenRequest request, CancellationToken cancellationToken = default)
{
    string sessionIdRaw = tokenGenerator.GetClaimsValue(request.RefreshToken, JwtTokenPayload.Session)
        ?? throw new BadRequestException(Messages<UserRefreshToken>.Invalid(x => x.Token));
    Guid sessionId = Guid.Parse(sessionIdRaw);

    IQueryable<UserRefreshToken> sessionTokens = repositoryWrapper.Repository<UserRefreshToken>()
        .Find(x => x.SessionId == sessionId)
        .Include(x => x.User)
        .OrderByDescending(x => x.CreatedAt);

    UserRefreshToken current = await sessionTokens.FirstOrDefaultAsync(cancellationToken)
        ?? throw new BadRequestException(Messages<UserRefreshToken>.Invalid(x => x.Token));

    if (current.Token != request.RefreshToken)
    {
        await repositoryWrapper.Repository<UserRefreshToken>()
            .DeleteRangeAsync(sessionTokens, cancellationToken);

        throw new BadRequestException(Messages<UserRefreshToken>.WasUsed());
    }

    if (current.User!.Status == OperationStatus.Lock)
    {
        throw new BadRequestException(Messages<User>.Blocked());
    }

    return await AuthenticateAsync(current.User, cancellationToken, current.SessionId);
}

private async Task<AuthUserResponse> AuthenticateAsync(
    User user, CancellationToken cancellationToken, Guid? sessionId = null)
{
    sessionId ??= NewId.Next().ToGuid();
    JwtSettings setting = tokenGenerator.GetSettingByScheme();

    string accessToken = tokenGenerator.GenerateAccessToken(setting, user);
    string refreshToken = tokenGenerator.GenerateRefreshToken(
        setting, new Claim(JwtTokenPayload.Session, sessionId.Value.ToString()));

    await repositoryWrapper.Repository<UserRefreshToken>().AddAsync(
        new UserRefreshToken
        {
            Token = refreshToken,
            SessionId = sessionId.Value,
            UserId = user.Id,
            ExpireTime = setting.GetRefreshTokenExpired(),
        },
        cancellationToken);

    return new(accessToken, refreshToken);
}
```

Why it is shaped this way:

- **One private mint step, two public entrances.** Login and refresh differ only in how they reach a
  trusted principal; everything after — session id, both tokens, the stored row — is identical, and
  duplicating it is how the two paths drift apart.
- **`sessionId ??=` is the entire rotation mechanism.** Login starts a session, refresh continues
  it. The session id is what groups a chain, which is what makes replay detectable and revocation
  possible at session granularity.
- **The session id is a sequential Guid**, not `Guid.NewGuid()` — it is an indexed persisted column,
  and random Guids fragment that index.
- **`OrderByDescending(...).FirstOrDefaultAsync(...)` is what "the latest stored token" means.**
  Comparing against any other row in the chain would accept a superseded token.
- **`DeleteRangeAsync` before throwing is the reuse response**, not an optimisation. Drop it and a
  detected replay becomes a shrug.
- **The session claim locates, the stored string authorises.** `GetClaimsValue` does not validate,
  so the claim may only be used as a lookup key.
- **The response carries the two tokens and nothing else.** Lifetimes already live in the access
  token's `exp`; restating them in the envelope creates a second source of truth. Response shapes
  are **api-surface**.
