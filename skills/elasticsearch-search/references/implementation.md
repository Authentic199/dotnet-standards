# Scaffolding the search capability

Use this file only when the project has no Elasticsearch capability and you are
creating one. If it already has one, extend it in place — the guard below is how you
tell the difference.

## Pre-scaffold guard

Before creating anything, check both:

1. **Does any folder already own `IElasticClient`?** A search capability may sit under
   a different name than the one this file scaffolds, including inside a module.
2. **Does the `AddInfrastructure` chain already contain a client registration** — an
   `AddElasticsearch()`, `AddSearch()` or similarly named line?

Either hit means the capability exists: stop scaffolding and use it in place.

The chain is ordered for readability rather than semantics, which is precisely why a
duplicate survives review in it — **search it by method name, do not skim it.** The
canonical chain this scaffold is taken from carries `.AddElasticsearch()` twice,
eleven lines apart. Nothing fails loudly: both registrations stay in the container and
a plain resolve of `IElasticClient` gets the last one. The exposure is a resolve of
`IEnumerable<IElasticClient>`, which builds both clients and runs the whole mapper
scan against the cluster twice.

## Three prerequisites this scaffold never creates

Each has an owner elsewhere. Verify all three before writing any file.

**1. The persistence facade that owns `DatabaseSettings`.** `ElasticsearchSettings` is
added *into* that class and is bound by the options block that facade already
declares. No persistence facade means no binding to ride, and the settings section
would need a home this scaffold is not entitled to choose.

**2. The validation helper.** `ElasticsearchSettings.Validate` calls
`validationContext.Required()`, an extension in
`Infrastructure/Facades/Common/Extensions/`:

```csharp
public static IEnumerable<ValidationResult> Required(
    this ValidationContext validationContext, params string[] ignoreProperties)
```

It reflects over every public property and reports any that is empty or still at its
type default, so a settings class opts into whole-object validation with one line
instead of per-property attributes. `ignoreProperties` is the escape hatch for a
genuinely optional property.

**3. AutoMapper profile scanning.** The composition root registers AutoMapper with a
marker type, and every `Elk*` document's mapping profile is discovered by that scan.
Without it, each projection gets hand-written at its call site and the same entity
maps two different ways in two different features.

**If any is missing: stop, report, and let the caller choose.**

| Option | Consequence |
|---|---|
| Scaffold the missing piece first, as its own task | Lands in its correct home, reviewed on its own merits. Search work waits. |
| Point at an equivalent already in the project | No new file. Requires confirming the behavior matches — a narrower validation rule or a profile scan over the wrong assembly is a silent divergence, not a detail. |
| Proceed without it | Not offered. Search would own a policy belonging to the whole solution. |

**Separately, a package check that is not a stop:** `NEST` (the client), `Humanizer`
(`Underscore()`, `Camelize()` in the index mapper) and `NewId` (sequential ids on
`ElkBaseEntity`; its types live in the `MassTransit` namespace). Add the references if
they are absent.

## Checklist

Work in this order. Each step is done only when the named artifact exists.

1. Run the guard, then confirm the three prerequisites.
2. Add the NEST client package to the `Infrastructure` csproj, on the version line
   matching the project's target framework. It supplies both the `Nest` and
   `Elasticsearch.Net` namespaces used below. Add `Humanizer` and `NewId` if absent.
3. Create `Facades/Persistence/ElasticSearch/`; write `ElasticSearchWrapper.cs` and
   `ElasticSearchRepositoryBase.cs`.
4. Create `Facades/ElasticSearch/`; write `ElkBaseEntity.cs`,
   `Builders/IIndexSettingsMapper.cs`, `Builders/IndexSettingsMapper.cs`, `Startup.cs`.
5. Add the `ElasticsearchSettings` class **and its property** to the existing
   `Facades/Persistence/DatabaseSettings.cs`. Change nothing else in that file.
6. Add the settings values to the configuration topic that already carries
   `DatabaseSettings`. No new topic, no new binding. Credentials come from the
   environment.
7. Register the wrapper in the persistence facade's `Startup`, beside the repository
   wrapper it mirrors:
   `services.AddScoped(typeof(IElasticSearchWrapper), typeof(ElasticSearchWrapper));`
8. Append `.AddElasticsearch()` to the `AddInfrastructure` chain — **once**, after the
   duplicate search from the guard above.

Steps 5 through 8 are where these scaffolds get abandoned half-done, and they fail
late. A capability that compiles but is never composed, or is composed but whose
settings section does not exist, breaks at the first request that injects the wrapper
— because that is the first resolve of the lazily built client — not at build.

## `Facades/Persistence/ElasticSearch/ElasticSearchWrapper.cs`

```csharp
using System.Collections;
using Nest;

namespace Infrastructure.Facades.Persistence.ElasticSearch;

public interface IElasticSearchWrapper
{
    public IElasticSearchRepositoryBase<T> Repository<T>()
        where T : class;
}

public class ElasticSearchWrapper : IElasticSearchWrapper
{
    private readonly Hashtable repositories = new();
    private readonly IElasticClient elasticClient;

    public ElasticSearchWrapper(IElasticClient elasticClient) => this.elasticClient = elasticClient;

    public IElasticSearchRepositoryBase<T> Repository<T>()
        where T : class
    {
        if (!repositories.ContainsKey(typeof(T).FullName!))
        {
            repositories.Add(
                typeof(T).FullName!,
                Activator.CreateInstance(
                    typeof(ElasticSearchRepositoryBase<>).MakeGenericType(typeof(T)),
                    elasticClient));
        }

        return (IElasticSearchRepositoryBase<T>)repositories[typeof(T).FullName!]!;
    }
}
```

The repository is constructed reflectively and cached by full type name, which is why
`IElasticSearchRepositoryBase<T>` is never registered in the container — the wrapper is
its only factory. **The check-then-add is not atomic and this `Hashtable` is not
synchronized**, which is the whole reason the wrapper is registered scoped. A singleton
wrapper shared across concurrent requests can race on that pair of lines.

## `Facades/Persistence/ElasticSearch/ElasticSearchRepositoryBase.cs`

```csharp
using Elasticsearch.Net;
using Nest;
using System.Reflection;

namespace Infrastructure.Facades.Persistence.ElasticSearch;

public interface IElasticSearchRepositoryBase<T>
    where T : class
{
    // Write
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    Task<T> AddAsync(T entity, Refresh refresh, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, Refresh refresh, CancellationToken cancellationToken = default);

    // Read by id
    T? GetById(object id);

    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> GetByIdManyAsync<TId>(IEnumerable<TId> ids, string? index = null, CancellationToken cancellationToken = default)
        where TId : notnull;

    // Read by query
    // WARNING: `terminateAfter` is wrong whenever more than one document can match.
    // Two matching documents, terminateAfter = 1, sorted descending: Elasticsearch stops
    // as soon as it has one hit (it behaves like a limit), so "first" is whichever shard
    // answered first — not the first in your sort order. Pass null when the sort decides
    // the answer.
    Task<T?> FirstOrDefaultAsync(Func<SearchDescriptor<T>, ISearchRequest> selector, int? terminateAfter = 1, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> SearchAsync(Func<SearchDescriptor<T>, ISearchRequest> selector, string? scrollTime = null, Func<ScrollDescriptor<T>, IScrollRequest>? scrollselector = null, CancellationToken cancellationToken = default);

    Task<long> CountAsync(Func<CountDescriptor<T>, ICountRequest> selector, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(object id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Func<SearchDescriptor<T>, ISearchRequest> selector, CancellationToken cancellationToken = default);

    // Update
    Task UpdateAsync(string id, T entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    Task UpdateRangeAsync(IEnumerable<T> entities, Refresh refresh = Refresh.False, CancellationToken cancellationToken = default);

    Task UpsertRangeAsync(IEnumerable<T> entities, Refresh refresh = Refresh.False, CancellationToken cancellationToken = default);

    Task UpdateByQueryAsync(Func<UpdateByQueryDescriptor<T>, IUpdateByQueryRequest> selector, CancellationToken cancellationToken = default);

    // Delete
    Task DeleteAsync(object id, CancellationToken cancellationToken = default);

    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

    Task DeleteRangeAsync(IEnumerable<T> entities, IndexName? indexName = null, CancellationToken cancellationToken = default);

    Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    Task DeleteByQueryAsync(Func<DeleteByQueryDescriptor<T>, IDeleteByQueryRequest> selector, CancellationToken cancellationToken = default);

    // Point in time
    Task<OpenPointInTimeResponse> GetPointInTimeAsync(string indexName, string keepAlive);

    Task ClosePointInTimeAsync(string pitId);
}

public class ElasticSearchRepositoryBase<T> : IElasticSearchRepositoryBase<T>
    where T : class
{
    private readonly IElasticClient elasticClient;

    public ElasticSearchRepositoryBase(IElasticClient elasticClient)
    {
        this.elasticClient = elasticClient;
    }

    public async Task<T> AddAsync(T entity, Refresh refresh, CancellationToken cancellationToken = default)
    {
        ThrowIfFailure(await elasticClient.IndexAsync(CheckIdValue(entity), i => i.Refresh(refresh), cancellationToken));
        return entity;
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        return await AddAsync(entity, Refresh.WaitFor, cancellationToken);
    }

    public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        return await AddRangeAsync(entities, Refresh.WaitFor, cancellationToken);
    }

    public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, Refresh refresh, CancellationToken cancellationToken = default)
    {
        IEnumerable<Task<T>> tasks = entities.Select(entity => Task.Run(() => CheckIdValue(entity)));
        IEnumerable<T> validEntities = await Task.WhenAll(tasks);
        ThrowIfFailure(await elasticClient.BulkAsync(x => x.CreateMany(validEntities).Refresh(refresh), cancellationToken));

        return entities;
    }

    public T? GetById(object id)
    {
        GetResponse<T> response = elasticClient.Get<T>(id.ToString(), x => x.Realtime(true));
        if (!response.Found)
        {
            return null;
        }

        ThrowIfFailure(response);

        return response.Source;
    }

    public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        GetResponse<T> response = await elasticClient.GetAsync<T>(id.ToString(), x => x.Realtime(true), cancellationToken);
        if (!response.Found)
        {
            return null;
        }

        ThrowIfFailure(response);

        return response.Source;
    }

    public Task<IEnumerable<T>> GetByIdManyAsync<TId>(IEnumerable<TId> ids, string? index = null, CancellationToken cancellationToken = default)
        where TId : notnull
    {
        IEnumerable<string> castIds = ids.Cast<string>();
        return elasticClient.SourceManyAsync<T>(castIds, index, cancellationToken);
    }

    public async Task<T?> FirstOrDefaultAsync(Func<SearchDescriptor<T>, ISearchRequest> selector, int? terminateAfter = 1, CancellationToken cancellationToken = default)
    {
        // See the warning on the interface member: terminateAfter makes "first" mean
        // "first found", not "first in sort order".
        ISearchResponse<T> response = await elasticClient.SearchAsync<T>(selector(FirstOrDefaultDescriptor(terminateAfter)), cancellationToken);
        ThrowIfFailure(response);
        return response.Documents.FirstOrDefault();
    }

    public async Task<IEnumerable<T>> SearchAsync(Func<SearchDescriptor<T>, ISearchRequest> selector, string? scrollTime = null, Func<ScrollDescriptor<T>, IScrollRequest>? scrollselector = null, CancellationToken cancellationToken = default)
    {
        List<T> entities = new();
        ISearchResponse<T> response = await elasticClient.SearchAsync(selector, cancellationToken);

        if (scrollTime != null)
        {
            while (response.Documents.Count > 0)
            {
                ThrowIfFailure(response);
                entities.AddRange(response.Documents);
                response = await elasticClient.ScrollAsync(scrollTime, response.ScrollId, scrollselector, cancellationToken);
            }
        }
        else
        {
            ThrowIfFailure(response);
            entities.AddRange(response.Documents);
        }

        return entities;
    }

    public async Task<long> CountAsync(Func<CountDescriptor<T>, ICountRequest> selector, CancellationToken cancellationToken = default)
    {
        CountResponse response = await elasticClient.CountAsync<T>(selector, cancellationToken);
        ThrowIfFailure(response);
        return response.Count;
    }

    public async Task<bool> ExistsAsync(object id, CancellationToken cancellationToken = default)
    {
        ExistsResponse response = await elasticClient.DocumentExistsAsync<T>(id.ToString(), x => x.Realtime(true), cancellationToken);
        ThrowIfFailure(response);
        return response.Exists;
    }

    public async Task<bool> ExistsAsync(Func<SearchDescriptor<T>, ISearchRequest> selector, CancellationToken cancellationToken = default)
    {
        ISearchResponse<T> response = await elasticClient.SearchAsync<T>(selector(FirstOrDefaultDescriptor()), cancellationToken);
        ThrowIfFailure(response);
        return response.Documents.Any();
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        ThrowIfFailure(await elasticClient.UpdateAsync<T>(entity, x => x.Doc(entity).Refresh(Refresh.WaitFor), cancellationToken));
    }

    public async Task UpdateAsync(string id, T entity, CancellationToken cancellationToken = default)
    {
        ThrowIfFailure(await elasticClient.UpdateAsync<T>(id, x => x.Doc(entity).Refresh(Refresh.WaitFor), cancellationToken));
    }

    public async Task UpdateRangeAsync(IEnumerable<T> entities, Refresh refresh = Refresh.False, CancellationToken cancellationToken = default)
    {
        ThrowIfFailure(await elasticClient.BulkAsync(x => x.UpdateMany(entities, (x, entity) => x.Doc(entity)).Refresh(refresh), cancellationToken));
    }

    public async Task UpsertRangeAsync(IEnumerable<T> entities, Refresh refresh = Refresh.False, CancellationToken cancellationToken = default)
    {
        ThrowIfFailure(await elasticClient.BulkAsync(x => x.UpdateMany(entities, (x, entity) => x.Doc(entity).Upsert(entity)).Refresh(refresh), cancellationToken));
    }

    public async Task UpdateByQueryAsync(Func<UpdateByQueryDescriptor<T>, IUpdateByQueryRequest> selector, CancellationToken cancellationToken = default)
    {
        ThrowIfFailure(await elasticClient.UpdateByQueryAsync(selector, cancellationToken));
    }

    public async Task DeleteAsync(object id, CancellationToken cancellationToken = default)
    {
        ThrowIfFailure(await elasticClient.DeleteAsync<T>(id.ToString(), x => x.Refresh(Refresh.WaitFor), cancellationToken));
    }

    public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        ThrowIfFailure(await elasticClient.DeleteAsync<T>(entity, x => x.Refresh(Refresh.WaitFor), cancellationToken));
    }

    public async Task DeleteRangeAsync(IEnumerable<T> entities, IndexName? indexName = null, CancellationToken cancellationToken = default)
    {
        ThrowIfFailure(await elasticClient.DeleteManyAsync<T>(entities, indexName, cancellationToken));
    }

    public async Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await DeleteRangeAsync(entities, null, cancellationToken);
    }

    public async Task DeleteByQueryAsync(Func<DeleteByQueryDescriptor<T>, IDeleteByQueryRequest> selector, CancellationToken cancellationToken = default)
    {
        ThrowIfFailure(await elasticClient.DeleteByQueryAsync(selector, cancellationToken));
    }

    public async Task<OpenPointInTimeResponse> GetPointInTimeAsync(string indexName, string keepAlive)
        => await elasticClient.OpenPointInTimeAsync(indexName, p => p.KeepAlive(keepAlive));

    public async Task ClosePointInTimeAsync(string pitId)
    {
        ThrowIfFailure(await elasticClient.ClosePointInTimeAsync(p => p.Id(pitId)));
    }

    private static void ThrowIfFailure(IResponse response)
    {
        if (!response.IsValid)
        {
            throw response.OriginalException ?? new InvalidOperationException(response.DebugInformation);
        }
    }

    private static SearchDescriptor<T> FirstOrDefaultDescriptor(int? terminateAfter = 1)
    {
        SearchDescriptor<T> descriptor = new();
        return descriptor
            .Size(1)                        // one document is all FirstOrDefault needs
            .TerminateAfter(terminateAfter) // see the warning on the interface member
            .TrackTotalHits(false);         // skip total-hit accounting; it is overhead nobody reads here
    }

    private object? GetIdValue(T entity) => GetKeyInfo().GetValue(entity);

    private PropertyInfo GetKeyInfo()
    {
        Type entityType = typeof(T);
        ElasticsearchTypeAttribute? attribute = entityType.GetCustomAttribute<ElasticsearchTypeAttribute>();
        if (attribute == null)
        {
            throw new InvalidOperationException($"missing key in {nameof(ElasticsearchTypeAttribute)}");
        }

        PropertyInfo? propertyInfo = entityType.GetProperty(attribute.IdProperty);
        if (propertyInfo == null)
        {
            throw new InvalidOperationException($"{attribute.IdProperty} does not match any property name on {typeof(T).Name}");
        }

        return propertyInfo;
    }

    private bool IsDefaultKey(T entity)
    {
        object? keyVal = GetIdValue(entity);
        Type type = GetKeyInfo().PropertyType;
        object? defaultVal = type.IsValueType ? Activator.CreateInstance(type) : null;
        if (keyVal == null)
        {
            return defaultVal == null;
        }

        return keyVal.Equals(defaultVal);
    }

    private T CheckIdValue(T entity)
    {
        if (IsDefaultKey(entity))
        {
            PropertyInfo key = GetKeyInfo();
            switch (key.PropertyType.FullName)
            {
                case "System.Guid":
                    key.SetValue(entity, Guid.NewGuid());
                    break;

                case "System.String":
                    key.SetValue(entity, Guid.NewGuid().ToString());
                    break;

                default:
                    throw new InvalidOperationException($"key of type {key.PropertyType.FullName} is not default value");
            }
        }

        return entity;
    }
}
```

Five properties of this shape are contract, not accident:

- **`[ElasticsearchType(IdProperty = …)]` is mandatory on every document type.** The
  private helpers read it reflectively to find and, if unset, to fill the id. A
  document without it throws on the first write, not at compile time.
- **Single-document writes and `AddRangeAsync` default to `Refresh.WaitFor`;
  `UpdateRangeAsync` and `UpsertRangeAsync` default to `Refresh.False`.** A read
  straight after a single write sees it; a read straight after `UpdateRangeAsync`
  may not. Callers that need a freshly bulk-updated document visible pass a
  `Refresh` value deliberately.
- **Only `Guid` and `string` ids can be auto-filled.** Any other key type still at its
  default throws rather than guessing.
- **Every response goes through `ThrowIfFailure`, with one gap:**
  `GetByIdManyAsync` returns the client's task directly and does not validate. It is
  the one member whose failure mode differs from the rest.
- **A missing document is `null`, not an exception.** `GetById`/`GetByIdAsync` check
  `Found` before validating, so a miss returns `null` and composes with `??`.

*Deviation, deliberate — two members are omitted from this scaffold:* the canonical
also carries a synchronous `Search(…, out ISearchResponse<T>?, …)` and a `BulkAll(…)`
that blocks a thread on a `CountdownEvent`. Both block inside request paths. If you
meet them in an existing project, leave them alone and use the async members; do not
add them to a new scaffold. `usage-patterns.md` carries the call-site treatment.

## `Facades/ElasticSearch/ElkBaseEntity.cs`

```csharp
using MassTransit;
using Nest;

namespace Infrastructure.Facades.ElasticSearch;

public class ElkBaseEntity
{
    [Keyword]
    public Guid Id { get; set; } = NewId.Next().ToGuid();

    public virtual DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

`[Keyword]` on `Id` is the point of the base class: an id must be matched exactly, and
a `text`-mapped id is analysed into tokens and stops matching. `NewId` (namespace
`MassTransit`, from its own standalone package) generates sequential guids, which keep
index locality where random guids fragment it. `CreatedAt` is `virtual` so a document
can override the mapping without redeclaring the property.

*(Deviation, deliberate: the canonical declares this base inside one module's
`ElkEntities/` folder and every other module imports it from there; the facade owns it
here.)*

## `Facades/ElasticSearch/Builders/IIndexSettingsMapper.cs`

```csharp
using Nest;

namespace Infrastructure.Facades.ElasticSearch.Builders;

public interface IIndexSettingsMapper
{
    string IndexPrefix { get; set; }

    void Map(ConnectionSettings connectionSettings);
}
```

Non-generic on purpose: the startup scan needs one type it can find and instantiate
without knowing any document type.

## `Facades/ElasticSearch/Builders/IndexSettingsMapper.cs`

```csharp
using Humanizer;
using Nest;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Infrastructure.Facades.ElasticSearch.Builders;

public abstract class IndexSettingsMapper<T> : IIndexSettingsMapper
    where T : class
{
    [AllowNull]
    public string IndexPrefix { get; set; }

    public void Map(ConnectionSettings connectionSettings)
    {
        Type type = typeof(T);
        string mappingName = (IndexPrefix + (type.GetCustomAttribute<ElasticsearchTypeAttribute>()?.RelationName ?? type.Name)).Underscore();
        connectionSettings.DefaultMappingFor<T>(x => x.IndexName(mappingName));

        ElasticClient elasticClient = new(connectionSettings);

        if (!elasticClient.Indices.Exists(mappingName).Exists)
        {
            elasticClient.Indices.Create(mappingName, x => x.Map<T>(Configure));
        }
        else
        {
            IEnumerable<string> propertyNames = typeof(T).GetProperties()
                .Where(x => x.GetCustomAttribute<IgnoreAttribute>() == null)
                .Select(x => x.Name.Camelize());
            IEnumerable<string> mappingProperies = elasticClient.Indices.GetMapping<T>()
                .Indices[mappingName].Mappings.Properties.Keys.Select(x => x.Name);

            if (mappingProperies.Union(propertyNames).Count() > mappingProperies.Count())
            {
                elasticClient.Indices.PutMapping<T>(OnUpdateMapping);
            }
        }
    }

    public abstract ITypeMapping Configure(TypeMappingDescriptor<T> descriptor);

    public virtual IPutMappingRequest OnUpdateMapping(PutMappingDescriptor<T> descriptor)
    {
        return descriptor.AutoMap();
    }
}
```

The index name is `(IndexPrefix + RelationName-or-type-name).Underscore()`, computed in
one place: prefix `app_`, type `ElkOrder` → `app_elk_order`. `RelationName` comes from
`[ElasticsearchType(RelationName = …)]` when the index name should not follow the C#
type name — renaming the class then does not rename the index. The prefix is the
multi-tenant and multi-environment separator: one cluster, many applications.

**What this does at startup, stated plainly, because it is not obvious from the
signature:**

- `Map` receives the `ConnectionSettings` object the caller is **still configuring**,
  registers the default index name for `T` on it, and then **builds a second
  `ElasticClient` from it**. Every mapper builds its own client, capturing the settings
  as they stood at that moment.
- `Indices.Exists`, `Indices.Create`, `GetMapping` and `PutMapping` are all **blocking,
  synchronous** calls. They run while the container is composing the singleton, so
  composition waits on the cluster. That is the canonical design: provisioning happens
  before the first request, and a wrong cluster address fails the first resolve rather
  than a user request.
- **The diff detects added properties only.** It compares property *names* and updates
  when the document has more of them than the live index. A changed field type, a
  changed analyzer, a removed property and a renamed property all produce no diff and
  no update. Elasticsearch cannot change an existing field's type in place anyway —
  those need a new index and a reindex, which nothing here performs or warns about.
- Properties marked `[Ignore]` are excluded from the comparison, so they must also be
  excluded from `Configure`, or the diff and the live mapping drift permanently.

## `Facades/ElasticSearch/Startup.cs`

```csharp
using Elasticsearch.Net;
using Infrastructure.Facades.ElasticSearch.Builders;
using Infrastructure.Facades.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nest;
using System.Reflection;

namespace Infrastructure.Facades.ElasticSearch;

public static class Startup
{
    public static IServiceCollection AddElasticsearch(this IServiceCollection services)
    {
        services.AddSingleton<IElasticClient>(LazyAddElasticsearchClient);
        return services;
    }

    private static ElasticClient LazyAddElasticsearchClient(IServiceProvider provider)
    {
        ElasticsearchSettings settings = provider
            .GetRequiredService<IOptions<DatabaseSettings>>().Value.ElasticsearchSettings;

        ConnectionSettings connectionSettings = GetConnectionSettings(settings);

        connectionSettings.ThrowExceptions(alwaysThrow: true);

        connectionSettings.BasicAuthentication(settings.Username, settings.Password);

        connectionSettings.RequestTimeout(TimeSpan.FromMinutes(10));

        // Runs every index mapper. Blocking cluster calls happen here.
        ScanElasticSearchSettings(connectionSettings, settings);

        return new ElasticClient(connectionSettings);
    }

    private static ConnectionSettings GetConnectionSettings(ElasticsearchSettings settings)
    {
        string[] nodes = settings.Nodes.ToArray();

        if (nodes.Length == 1)
        {
            Uri uri = new(nodes[0]);
            return new(uri);
        }
        else
        {
            Uri[] uris = new Uri[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                uris[i] = new Uri(nodes[i]);
            }

            StaticConnectionPool staticConnectionPool = new(uris);
            return new(staticConnectionPool);
        }
    }

    private static void ScanElasticSearchSettings(
        ConnectionSettings connectionSettings, ElasticsearchSettings settings)
    {
        ICollection<Type> mappingSettings =
            Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(IIndexSettingsMapper)))
            .ToArray();

        foreach (Type mappingType in mappingSettings)
        {
            ConstructorInfo? constructorInfo = mappingType.GetConstructor(Type.EmptyTypes);

            if (constructorInfo != null)
            {
                IIndexSettingsMapper instance = (IIndexSettingsMapper)Activator.CreateInstance(mappingType)!;
                instance.IndexPrefix = settings.IndexPrefix;
                instance.Map(connectionSettings);
            }
            else
            {
                throw new InvalidOperationException($"{mappingType.Name} is missing a public parameterless constructor");
            }
        }
    }
}
```

Three things this file makes true, and one it does not:

- **`AddElasticsearch()` takes no argument, and registers only the client.** Settings
  are read at *resolve* time through `IOptions<DatabaseSettings>`, never at
  registration time through `IConfiguration` — which is why the line's position in the
  `AddInfrastructure` chain is free. The wrapper registers separately, in the
  persistence facade's `Startup`, beside the repository wrapper it mirrors.
- **The scan is `Assembly.GetExecutingAssembly()`** — the assembly this `Startup` is
  compiled into. Mappers in any other assembly are silently not found: no error, no
  index, and the first write creates one with a dynamic mapping instead of yours.
- **Every mapper needs a public parameterless constructor.** A mapper that takes a
  dependency throws at composition.
- **It does not make provisioning safe to repeat under concurrency.** The scan runs
  once per process, on first resolve, and several instances starting together each run
  it against the same cluster.

## The `ElasticsearchSettings` block

Add both pieces to the existing `Facades/Persistence/DatabaseSettings.cs`. Change
nothing else in that file.

```csharp
// 1) a property on the existing DatabaseSettings class
public ElasticsearchSettings ElasticsearchSettings { get; set; } = new();

// 2) a class in the same file
public class ElasticsearchSettings : IValidatableObject
{
    public List<string> Nodes { get; set; } = new();

    public string Username { get; set; } = default!;

    public string Password { get; set; } = default!;

    public string IndexPrefix { get; set; } = default!;

    public int DefaultSize { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => validationContext.Required();
}
```

It needs `using System.ComponentModel.DataAnnotations;` and the namespace holding
`Required()`; both are usually already in that file.

- **No `AddOptions<ElasticsearchSettings>()`, no new configuration topic, no new
  binding.** It rides the `AddOptions<DatabaseSettings>()` block the persistence facade
  already declares.
- **The nested `Validate` runs because that block chains
  `ValidateDataAnnotationsRecursively()`** — the house binding idiom throughout the
  solution. The non-recursive variant stops at the root and never descends, so a
  completely unconfigured search section would pass; if the project you are in binds
  non-recursively, that is what to fix, not this class.
- **`Required()` reports any property still at its type default**, so `DefaultSize`
  must be configured to a non-zero value — it is not optional by omission. Use
  `Required(nameof(Username), nameof(Password))` if the target cluster genuinely has no
  authentication.

*(Deliberate divergence from this plugin's `distributed-caching` skill, which extracts
its settings on the rule that settings follow their service: search keeps the canonical
nesting, and pays for it with a section name that no longer equals the type name.)*

Configuration lives in the **database topic**, not a topic of its own, because the
section is `DatabaseSettings`. Shape only, placeholders only:

```json
{
  "DatabaseSettings": {
    "ElasticsearchSettings": {
      "Nodes": [ "<scheme>://<host>:<port>" ],
      "Username": "<from environment>",
      "Password": "<from environment>",
      "IndexPrefix": "<application-key>",
      "DefaultSize": 20
    }
  }
}
```

- `Nodes` holds full URIs. `DefaultSize` is the intended default page size; the
  canonical declares and validates it but no call site reads it yet — wire it in when
  a paging call site needs a default, rather than hard-coding one.
- **Credentials never go in this file.** Environment variables load last and beat every
  JSON file — that is where a deployed username and password come from.
- `IndexPrefix` must differ per environment when environments share a cluster; two
  environments with the same prefix write into the same indices.

## Connection policy

Four values, set on every client this capability builds. They are policy, not tuning
knobs — do not override them per call site.

| Setting | Value | Why |
|---|---|---|
| `ThrowExceptions` | `alwaysThrow: true` | A rejected request raises instead of returning an invalid response object. Without it, a cluster error deserializes into an empty result set and reads as "no matches". |
| `BasicAuthentication` | from settings | Credentials arrive through configuration, so they can come from the environment and never from source. |
| `RequestTimeout` | `TimeSpan.FromMinutes(10)` | Sized for bulk indexing and reindexing, which legitimately run for minutes. It applies to **every** request, so a single lookup against a hung cluster also holds its caller for ten minutes. |
| node count | 1 → direct `Uri`; more → `StaticConnectionPool` | One node needs no pool. A static pool round-robins across a fixed membership list; it does not sniff, so a node added to the cluster later is invisible until the settings change. |

Because `ThrowExceptions` is on, the repository's own `ThrowIfFailure` is a second line
of defence rather than the only one. Both stay: the client setting covers transport and
server errors, `ThrowIfFailure` covers a response the client considers delivered but
invalid.

## The two wiring lines

```csharp
// Facades/Persistence/Startup.cs — beside the repository-wrapper registration
services.AddScoped(typeof(IElasticSearchWrapper), typeof(ElasticSearchWrapper));
```

```csharp
// Infrastructure/Startup.cs — one line in the AddInfrastructure chain
services
    // … existing facades …
    .AddElasticsearch();
```

The wrapper is registered where the surface lives and the client where the wiring
lives, so a scaffold that stops after the chain line compiles and then fails to resolve
`IElasticSearchWrapper` at the first injection. **Append the chain line exactly once** —
run the duplicate search from the guard before adding it.

## Normalizations at a glance

| Spot | Canonical | This scaffold | Reason |
|---|---|---|---|
| Wrapper / repository namespace | `Infrastructure.Persistence.Repositories.ElasticSearch` | `Infrastructure.Facades.Persistence.ElasticSearch` | Matches the folder and the rest of the solution. |
| `UpdateByQuerryAsync` / `DeleteByQuerryAsync` | spelled `Querry` | `UpdateByQueryAsync` / `DeleteByQueryAsync` | Typo. Grep for `Querry` when working in an older project — it is what you will find there. |
| Blocking-sync members | `Search(…, out …)` and `BulkAll(…)` present | omitted | Both block a thread inside request paths; see `usage-patterns.md`. |
| `ElkBaseEntity` home | one module's `ElkEntities/` folder | `Facades/ElasticSearch/` | A shared base belongs to the capability, not to whichever module needed it first. |
| Error message operand | `nameof(T)` (yields the literal `"T"`) | `typeof(T).Name` | The message named no type. |
| Mapper-scan error message | `"missing default contructor"` | names the mapper type, spelled correctly | The message did not say which mapper failed. |
| Source comments | mixed language | English | Substance preserved, including the `terminateAfter` warning. |

Everything else — bodies, defaults, policy values, registration sites — is canonical
and unchanged.
