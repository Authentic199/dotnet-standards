## OpenAPI — the Swashbuckle facade

One facade, one `Startup.cs`, two extension methods. Endpoints contribute only
their XML `<summary>` and their `ProducesResponseType` lines; everything else on
this page is facade-level and written once.

### Registration — `AddOpenApiDocumentation`

```csharp
namespace Infrastructure.Facades.OpenAPI;

internal static class Startup
{
    internal static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SwaggerSettings>()
            .BindConfiguration(nameof(SwaggerSettings))
            .ValidateDataAnnotationsRecursively()
            .ValidateOnStart();

        SwaggerSettings swaggerSettings = configuration.GetSection(nameof(SwaggerSettings)).Get<SwaggerSettings>()!;
        if (swaggerSettings.Enable)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(swaggerSettings.DefaultInfos?.DocKey, new OpenApiInfo
                {
                    Version = swaggerSettings.DefaultInfos?.Version,
                    Title = swaggerSettings.DefaultInfos?.Title,
                    Description = swaggerSettings.DefaultInfos?.Description,
                });

                options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    BearerFormat = "JWT",
                    Description = "Input your Bearer token to access this API",
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                });

                options.SupportNonNullableReferenceTypes();

                options.OperationFilter<SecurityRequirementsOperationFilter>();

                options.SchemaFilter<EnumTypesSchemaFilter>(
                    Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));

                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetEntryAssembly()?.GetName().Name}.xml"));
            });

            services.AddFluentValidationRulesToSwagger();
        }

        return services;
    }
}
```

The parts that are decisions rather than boilerplate:

- **The options-pattern four-call chain** (`AddOptions` → `BindConfiguration` →
  `ValidateDataAnnotationsRecursively` → `ValidateOnStart`) runs first and
  unconditionally, so a malformed topic fails at boot even when the document is
  switched off.
- **The settings are then read a second time, directly from `IConfiguration`.**
  Deliberate, not redundant: the `Enable` gate has to be known *while
  registering*, and no `IOptions<T>` can be resolved yet at that point.
- **`Enable` gates the entire block.** When false nothing is registered — no
  document, no UI, no generator cost. That is the switch for an environment that
  must not publish its API surface; it is configuration, not a code change.
- **`SupportNonNullableReferenceTypes()`** makes C# nullability the source of
  truth for `required` in the schema. It is the reason DTO properties should be
  honestly nullable: `string?` and `string` now mean different things to clients.
- **Two `IncludeXmlComments` calls, two assemblies** — the executing assembly
  (Infrastructure, holding the DTOs and enums) and the entry assembly (Web,
  holding the controllers and their endpoint summaries). Drop the second and
  every operation description disappears while the schemas stay documented; drop
  the first and the reverse. **Both are required.**
- **`AddFluentValidationRulesToSwagger()`** projects validator rules into the
  schema, so constraints are documented from the rules that actually run instead
  of being restated by hand and going stale.

### Pipeline — `UseOpenApiDocumentation`

```csharp
internal static IApplicationBuilder UseOpenApiDocumentation(this IApplicationBuilder app)
{
    SwaggerSettings swaggerSettings = app.ApplicationServices.GetRequiredService<IOptions<SwaggerSettings>>().Value;

    if (swaggerSettings.Enable)
    {
        if (!(app as WebApplication)!.Environment.IsDevelopment())
        {
            app.UseSwaggerUIBasicAuthMiddleware();
        }

        app.UseSwagger();
        app.UseSwaggerUI(configure =>
        {
            configure.ConfigObject.PersistAuthorization = true;
            configure.RoutePrefix = swaggerSettings.DefaultInfos?.RoutePrefix;
            configure.SwaggerEndpoint($"/swagger/{swaggerSettings.DefaultInfos?.DocKey}/swagger.json", swaggerSettings.DefaultInfos?.DocName);
            configure.EnableDeepLinking();
            configure.DocExpansion(DocExpansion.None);
        });
    }

    return app;
}
```

- **The basic-auth middleware is registered before the UI and only outside
  Development.** Order matters: registered after `UseSwaggerUI`, it never runs.
  The middleware reads `DefaultInfos.RoutePrefix` from configuration and matches
  `Request.Path.StartsWithSegments("/{routePrefix}")`, so changing the prefix
  moves the protection with the UI automatically. It compares the decoded
  `Basic` header against `Credentials.UserName`/`Password` and answers 401 with a
  `WWW-Authenticate` challenge otherwise.
- **`PersistAuthorization = true`** keeps the entered bearer token across
  reloads — the difference between a usable and an unusable UI.
- **`DocExpansion.None`** collapses every group on load; `EnableDeepLinking()`
  makes an operation's URL shareable. Both are ergonomics of a document with
  hundreds of operations, not preferences.
- `RoutePrefix`, `DocKey` and `DocName` all come from settings — **no literal
  path here.** The `SwaggerEndpoint` path is built from `DocKey`, so the key used
  at registration and the key used here can never disagree.

### Settings and its configuration topic

```csharp
public class SwaggerSettings : IValidatableObject
{
    public bool Enable { get; set; }

    public SwaggerDefaultInfos DefaultInfos { get; set; } = new();

    public SwaggerCredentials Credentials { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required(nameof(Enable));
}

public class SwaggerDefaultInfos : ISwaggerInfos, IValidatableObject
{
    public string Title { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string DocKey { get; set; } = default!;
    public string DocName { get; set; } = default!;
    public string RoutePrefix { get; set; } = default!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required();
}

public class SwaggerCredentials : IValidatableObject
{
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required();
}
```

Each nested settings class is itself `IValidatableObject`, which is what makes
`ValidateDataAnnotationsRecursively()` meaningful — a blank `RoutePrefix` fails
at boot instead of publishing the UI at the application root. Settings live
beside the facade, not in a central settings folder.

`Web/Configurations/openapi.json` — one topic, one file:

```json
{
  "SwaggerSettings": {
    "Enable": true,
    "DefaultInfos": {
      "Title": "API Documentations",
      "Version": "1.0.0",
      "Description": "API connection specification document.",
      "DocKey": "default",
      "DocName": "Default",
      "RoutePrefix": "docs"
    },
    "Credentials": {
      "UserName": "replace-me",
      "Password": "replace-me"
    }
  }
}
```

**`Version` here is document metadata, not API versioning.** It labels the
document; it never appears in a route. The API is unversioned — see *Versioning*
in the SKILL body.

**Credentials in the committed topic file are placeholders.** Real values arrive
from the environment overlay or the environment variables loaded last; secret
handling itself belongs to `auth-and-security`.

### The two filters

**`SecurityRequirementsOperationFilter`** — puts the padlock only where it belongs:

```csharp
internal class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<AuthorizeAttribute>().Any())
        {
            return;
        }

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme,
                },
            }] = Array.Empty<string>(),
        });
    }
}
```

The early return is the whole design: an operation with no `AuthorizeAttribute`
in its endpoint metadata gets no security requirement, so anonymous endpoints are
not falsely marked as locked. **`HasPermissionAttribute` derives from
`AuthorizeAttribute`, so every endpoint carrying `[HasPermission]` is detected**
— no separate registration, and adding a permission to an endpoint updates the
document by itself. If a padlock is missing, check the attribute, not the filter.

**`EnumTypesSchemaFilter`** — folds each enum member's XML comment into the
schema description:

```csharp
internal class EnumTypesSchemaFilter : ISchemaFilter
{
    private readonly XDocument? xmlComments;

    public EnumTypesSchemaFilter(string xmlPath)
    {
        if (File.Exists(xmlPath))
        {
            xmlComments = XDocument.Load(xmlPath);
        }
    }

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (xmlComments != null && schema.Enum?.Count > 0 && context.Type?.IsEnum == true)
        {
            StringBuilder schemaDescription = new("<p>Members:</p><ul>");

            foreach (object enumMemberValue in Enum.GetValues(context.Type))
            {
                string fullEnumMemberName = $"F:{context.Type.FullName}.{enumMemberValue}";

                string? enumMemberComment = xmlComments
                    .XPathEvaluate($"normalize-space(//member[@name = '{fullEnumMemberName}']/summary/text())") as string;

                schemaDescription.Append("<li><i>")
                    .Append(Convert.ChangeType(enumMemberValue, Enum.GetUnderlyingType(context.Type)).ToString())
                    .Append(" - ").Append(enumMemberValue).Append("</i>: ")
                    .Append(enumMemberComment?.Trim())
                    .Append("</li> ");
            }

            schema.Description = schemaDescription.Append("</ul>").ToString();
        }
    }
}
```

It XPaths the documentation file for `F:<Namespace>.<Enum>.<Member>` and emits an
HTML list of *numeric value – name – comment*, which is what tells a client
developer that `2` means what it means. **This is why enum members carry XML
`<summary>` comments.** Its constructor argument is the path of the assembly
holding the enums — the same path passed to the first `IncludeXmlComments`. It
degrades silently: no file, no filter, no error. **An enum showing bare integers
almost always means the XML file was not produced, not that the filter is
broken.**

### Prerequisite — the documentation file

Both `IncludeXmlComments` calls and the enum filter read a `.xml` next to the
assembly. That file exists only if the build produces it:

```xml
<!-- Directory.Build.props at the repository root -->
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

**One line at the root, inherited by every project** — never repeated per csproj.
`Directory.Build.targets` then points `DocumentationFile` at
`$(OutputPath)$(AssemblyName).xml`, which is what puts the file where
`AppContext.BaseDirectory` + `{AssemblyName}.xml` will find it. Both files are
`facade-module-architecture`'s territory.

Turning documentation generation on makes the compiler warn (CS1591) on every
public member without a comment. **That suppression belongs in `dotnet.ruleset`,
never in a csproj** — again `facade-module-architecture`.

### Where this is composed

`AddOpenApiDocumentation(configuration)` is one line in the `AddInfrastructure`
chain, and `UseOpenApiDocumentation()` one line in the `UseInfrastructure`
pipeline at the position the UI must occupy. **Both belong to
`facade-module-architecture`** — never call either from `Program.cs` directly.

### Debugging the document

| Symptom | Where to look |
|---|---|
| No document or UI at all | `Enable` is false in the effective configuration |
| Schemas documented, operations bare | the entry-assembly `IncludeXmlComments` call |
| Operations documented, schemas bare | the executing-assembly `IncludeXmlComments` call |
| Everything bare | `GenerateDocumentationFile` at the root, or the `.xml` not landing beside the assembly |
| Enums show bare integers | the same missing `.xml` — the filter degrades silently — then member-level `<summary>` comments |
| Padlock missing, or on everything | the endpoint's `[HasPermission]`/`[Authorize]`; never the filter |
| Constraints missing from a schema | the validator exists and is registered; `AddFluentValidationRulesToSwagger()` |
| Token lost on every reload | `PersistAuthorization` |
| UI reachable unauthenticated in a deployed environment | basic-auth middleware registered after `UseSwaggerUI`, or a `RoutePrefix` that no longer matches the UI's |
