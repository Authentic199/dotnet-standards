# Registration and settings

How the sender is registered, how each integration contributes its settings, and what the
settings file looks like.

## Contents

| Section | What it holds |
|---|---|
| [Prerequisite package](#prerequisite-package) | `ReHackt.Extensions.Options.Validation` |
| [Startup.cs](#startupcs) | The facade's two registration methods |
| [Wiring both from the composition root](#wiring-both-from-the-composition-root) | The one mistake this design invites |
| [The base settings partial](#the-base-settings-partial) | `HttpClientSettings` |
| [A settings partial an integration contributes](#a-settings-partial-an-integration-contributes) | The per-integration file |
| [Typed-client registration](#typed-client-registration) | `AddHttpClient<TInterface, TImpl>` |
| [httpclient.json](#httpclientjson) | Full sanitized settings-file shape |
| [Where the file is loaded](#where-the-file-is-loaded) | Configuration wiring |
| [ValidationContextExtension](#validationcontextextension) | The `Required()` helper the settings classes call |

## Prerequisite package

`ValidateDataAnnotationsRecursively()` is **not** in the BCL. It comes from:

```xml
<PackageReference Include="ReHackt.Extensions.Options.Validation" Version="7.0.1" />
```

referenced from the `Infrastructure` project. Without it the registration below does not
compile. The BCL's `ValidateDataAnnotations()` is not a drop-in replacement here: every
integration's settings live in a nested section object, and each of those sections implements
`IValidatableObject` itself (see below) — the recursive call is what gives those nested
implementations something to run at startup.

## Startup.cs

`Infrastructure/Facades/Common/HttpClients/Startup.cs` — two separate methods:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Facades.Common.HttpClients
{
    internal static class Startup
    {
        internal static IServiceCollection AddHttpClientSender(this IServiceCollection services)
        {
            services.AddTransient<IHttpClientSender, HttpClientSender>();
            return services;
        }

        internal static IServiceCollection AddClientSetting(this IServiceCollection services)
        {
            services
                .AddOptions<HttpClientSettings>()
                .BindConfiguration(nameof(HttpClientSettings))
                .ValidateDataAnnotationsRecursively()
                .ValidateOnStart();

            return services;
        }
    }
}
```

`AddTransient` is not interchangeable with a singleton: `HttpClientSender` holds a mutable
request builder as instance state (see `sender-and-result.md`), so a shared instance would
interleave concurrent chains.

`BindConfiguration(nameof(HttpClientSettings))` is what ties the class name to the root key in
the settings file — rename the class and the configuration key must move with it.

## Wiring both from the composition root

The composition root must call **both** methods:

```csharp
services
    .AddHttpClientSender()
    .AddClientSetting();
```

This is the one mistake the split invites, and it is present in the reference solutions: two of
them define `AddClientSetting()` and never call it from the composition root. The sender still
resolves and calls still go out, but `HttpClientSettings` is never bound and never validated —
so every settings section is default-constructed, `ValidateOnStart()` proves nothing, and a
missing host surfaces as a failed request instead of a failed boot.
Concretely: every client that injects `IOptions<HttpClientSettings>` gets an unbound instance
with empty hosts and routes.

A variant exists in which `AddHttpClientSender()` calls `AddClientSetting()` internally, so the
composition root has one call to make. Either shape is fine; what is not fine is a settings
registration nobody invokes. If you are in an existing project, check the composition root
before assuming the options are bound.

## The base settings partial

`Infrastructure/Facades/Common/HttpClients/HttpClientSettings.cs`:

```csharp
namespace Infrastructure.Facades.Common.HttpClients;

public partial class HttpClientSettings
{
}
```

Empty by design. The root class exists so `BindConfiguration(nameof(HttpClientSettings))` has
a name to bind, and so each integration can add its own property from its own file without any
two integrations editing the same one.

Two rules the base file carries:

- **It declares no integration sections.** Sections arrive from the integrations' own partial
  files. A base file that names a specific integration makes the shared facade depend on a
  module, which is backwards — that dependency direction belongs to
  `facade-module-architecture`, and it is a layering violation there too.
- **`partial`, always.** A non-partial `HttpClientSettings` forces every integration to edit
  one shared file, which is the merge-conflict shape this design exists to avoid.

*Variances in existing projects:* one puts `: IValidatableObject` with a `Required()` body on
the base; another declares its integration properties directly on the base instead of in
per-module partials. Neither is what a new recreation copies. Validation belongs on the
per-integration settings classes, where the properties are — and on an all-object base
`Required()` compares each section against a freshly constructed instance, which is reference
inequality, so it never reports anything.

## A settings partial an integration contributes

One file per integration, next to that integration's code, holding the partial that adds the
section plus the section's own settings class:

```csharp
using Infrastructure.Facades.Common.Extensions;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Facades.Common.HttpClients;

public partial class HttpClientSettings
{
    public ThirdPartyApiSettings ThirdPartyApi { get; set; } = new();
}

public class ThirdPartyApiSettings : IValidatableObject
{
    public string Scheme { get; set; } = default!;

    public string Host { get; set; } = default!;

    public string EntityRoute { get; set; } = default!;

    public string CreateEntityRoute { get; set; } = default!;

    public string ClientId { get; set; } = default!;

    public string ClientSecret { get; set; } = default!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required();
}
```

`Required()` with no arguments demands every property; pass property names to exempt the
optional ones: `validationContext.Required(nameof(ClientSecret))`. When a section needs a rule
beyond presence, write the `Validate` body out and `yield return` the extra `ValidationResult`
alongside the `Required()` results.

Note the namespace: the partial must be declared in
`Infrastructure.Facades.Common.HttpClients` even though the file lives beside the integration.

## Typed-client registration

For an integration with one stable base address, in that integration's own `Startup.cs`:

```csharp
using Infrastructure.Facades.Common.HttpClients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Facades.ThirdPartyApi.Settings
{
    internal static class Startup
    {
        internal static IServiceCollection AddThirdPartyApiClient(this IServiceCollection services)
        {
            services.AddScoped<IThirdPartyApiClient, ThirdPartyApiClient>();
            services.AddHttpClient<IThirdPartyApiClient, ThirdPartyApiClient>((provider, cfg) =>
            {
                ThirdPartyApiSettings setting = provider
                    .GetRequiredService<IOptions<HttpClientSettings>>().Value.ThirdPartyApi;

                cfg.BaseAddress = new Uri(UriHelper.BuildAbsolute(setting.Scheme, new HostString(setting.Host)));
            });

            return services;
        }
    }
}
```

The consumer injects both the typed `HttpClient` and `IHttpClientSender`, then opens the chain
with `.UseClient(httpClient)`.

`IThirdPartyApiClient` / `ThirdPartyApiClient` are your own types — the client class shape is
in the skill body, not in these files.

## httpclient.json

The whole file is one `HttpClientSettings` object — the key matches
`BindConfiguration(nameof(HttpClientSettings))` — with one nested object per integration whose
name matches the property added by that integration's partial.

```json
{
  "HttpClientSettings": {
    "ThirdPartyApi": {
      "Scheme": "https",
      "Host": "api.example.test",
      "EntityRoute": "/v1/entities",
      "CreateEntityRoute": "/v1/entities",
      "ClientId": "<supplied per environment>",
      "ClientSecret": "<supplied per environment>"
    },
    "NotificationApi": {
      "Scheme": "https",
      "Host": "notify.example.test",
      "SendRoute": "/v1/messages",
      "ApplicationId": "<supplied per environment>",
      "ApiKey": "<supplied per environment>"
    },
    "PartnerApi": {
      "Scheme": "https",
      "Host": "partner.example.test",
      "SyncRoute": "/v2/sync",
      "AccessToken": "<supplied per environment>",
      "RetryCount": 2,
      "RetryDelayMilliseconds": 500,
      "TimeoutSeconds": 30
    }
  }
}
```

Four things this shape shows:

- **Scheme and host are separate keys**, because clients build absolute URIs from them
  (`UriHelper.BuildAbsolute(scheme, host, route)`) rather than storing a base URL string.
- **Route values start with `/`.** They are passed to `UriHelper.BuildAbsolute(scheme, host,
  route)`, whose path argument is a `PathString`; every route key in the reference solutions
  carries the leading slash.
- **Routes are keys, not literals.** Every path a client calls has a named key.
- **The third section carries `RetryCount` / `RetryDelayMilliseconds` / `TimeoutSeconds`.** That
  is what module-owned resilience looks like: the keys live in that integration's own section
  and are read by that integration's own client code. The facade reads none of them and has no
  retry, timeout or circuit-breaker policy of its own. Do not add these keys to a section whose
  client does not implement them — a settings key that nothing reads is worse than absent.

Never commit real credentials. The reference solutions keep placeholders in the committed file
and supply real values per environment.

## Where the file is loaded

Settings are split into several per-concern JSON files rather than one `appsettings.json`, each
loaded as a base file plus an optional environment override:

```csharp
builder.Configuration
    .AddJsonFile($"{configurationsDirectory}/httpclient.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"{configurationsDirectory}/httpclient.{environmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
```

The base file is **not** optional — a missing `httpclient.json` is a startup failure by design.
How the configuration files are organised as a whole belongs to
`facade-module-architecture`; what matters here is that the outbound-HTTP settings have their
own file and their own environment override.

Adding an integration is therefore three edits that land together: the settings partial, the
section in `httpclient.json`, and the registration call in the composition root.

## ValidationContextExtension

The settings classes above call `validationContext.Required()`. It is a shared house helper —
**most projects already have it**, in a general validation-extensions file. Check first;
extend the existing one rather than adding a second. The general-purpose extension file itself
is `common-extensions` territory. Carried here only so a project without it can compile the
settings classes:

```csharp
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Infrastructure.Facades.Common.Extensions;

public static class ValidationContextExtension
{
    public static IEnumerable<ValidationResult> Required(this ValidationContext validationContext, params string[] ignoreProperties)
    {
        foreach (PropertyInfo propertyInfo in validationContext.ObjectType.GetProperties())
        {
            if (ignoreProperties.Contains(propertyInfo.Name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            Type propertyType = propertyInfo.PropertyType;
            object? propValue = propertyInfo.GetValue(validationContext.ObjectInstance);
            object? defaultVal;
            string message = $"{propertyInfo.Name} of {validationContext.ObjectType.FullName} is required";
            if (propertyType == typeof(string))
            {
                if (string.IsNullOrEmpty(propValue?.ToString()))
                {
                    yield return new ValidationResult(
                    message,
                    new[] { propertyInfo.Name });
                }
            }
            else
            {
                defaultVal = propertyType.IsNullableType() ? null : Activator.CreateInstance(propertyType);

                if (propValue?.Equals(defaultVal) == true)
                {
                    yield return new ValidationResult(
                        message,
                        new[] { propertyInfo.Name });
                }
            }
        }
    }
}
```

And the one type helper it calls, which likewise usually already exists:

```csharp
namespace Infrastructure.Facades.Common.Extensions;

internal static class TypeExtension
{
    internal static bool IsNullableType(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
}
```
