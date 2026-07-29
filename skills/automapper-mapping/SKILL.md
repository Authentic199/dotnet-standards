---
name: automapper-mapping
description: >-
  This skill should be used when mapping types with AutoMapper in a .NET
  solution: writing or reviewing a Profile class, a CreateMap and its
  ForMember/MapFrom, Ignore, IncludeAllDerived, IncludeMembers, ConvertUsing
  or AfterMap; request-to-entity or entity-to-response mapping; deciding which
  file a CreateMap belongs in or what to name the profile class; checking
  whether a map is safe to reach from a query projection; or wiring
  registration — AddAutoMapper, the marker profile, assembly scanning,
  AddCollectionMappers. Not for: ProjectTo inside a query — ef-core-data-access;
  where a DTO file and its colocated profile sit — api-surface; service and
  validator internals — module-feature; unclear ownership —
  choosing-a-dotnet-skill.
---

## Core Principles

### 1. A profile lives in the file that declares its source type

For a map `Source → Destination`, the `Profile` class is declared in the same file
as `Source`, directly beside it. The one exception: when the source is an entity
and the destination is a response, the profile lives in the **response** file.

Profiles are never collected into a mapping folder. The mapping facade holds
exactly one file — an empty marker profile — and it exists only as the type handed
to the assembly scan. It is an anchor, not a home.

```csharp
// CreateEntityRequest.cs — the request is the source, so the profile lives here
public class CreateEntityRequestMapping : Profile
{
    public CreateEntityRequestMapping() => CreateMap<CreateEntityRequest, Entity>();
}

// EntityDetailResponse.cs — the entity is the source, but the response owns the profile
public class EntityDetailResponseMapping : Profile
{
    public EntityDetailResponseMapping() => CreateMap<Entity, EntityDetailResponse>();
}
```

**Why:** a map is edited when its DTO's shape changes, and the DTO whose shape
drives the map is the one that is not the entity. Keeping the profile in that file
means one edit, one file, and a reviewer sees the property and its mapping in the
same diff. The exception exists because one entity feeds many responses — putting
every response's profile in the entity file would turn it into a junk drawer that
every feature touches.

### 2. A profile class is named `<DtoTypeName>Mapping`

The profile takes the name of the DTO whose file it sits in, plus the `Mapping`
suffix — `CreateEntityRequest` → `CreateEntityRequestMapping`, `EntityBaseResponse`
→ `EntityBaseResponseMapping`. Note that this is the DTO, not the destination: for
a request → entity map the DTO is the *source*, so the name is built from the
request. Do not abbreviate the DTO's suffix away, and never give a profile a name
that points at a different type than the one it maps.

**Why:** the name is the only index there is. Principle 1 scatters profiles across
hundreds of DTO files, so "which file holds this map?" is answered by reading the
class name backwards. A name that drops or alters part of the DTO's name breaks
that inference exactly when someone is hunting the map that is misbehaving.

### 3. A map reachable from a query projection must not use `AfterMap` or `ConvertUsing`

`ProjectTo` compiles a map into an expression tree the database provider must
translate. `AfterMap` and `ConvertUsing` are delegates — they cannot be translated.
The projection either ignores them or fails at runtime, and silent data loss is as
common as an exception.

Reachability is **transitive**. A map is reachable if:

- its destination is projected in a query, **or**
- it is pulled in by `IncludeAllDerived` from a reachable base map, **or**
- it is pulled in by `IncludeMembers` from a reachable map.

Trace the chain before adding a callback. "This map itself is never projected" is
not enough — a base map three levels up may be.

A map that is never reached from a query projection may use `AfterMap` and
`ConvertUsing`.

**Why:** projection safety is a property of the whole reachable graph, not of one
`CreateMap`. The cost is asymmetric — a delegate added to a map that later becomes
projection-reachable breaks at runtime, inside a query, far from the profile that
caused it.

> Writing the `ProjectTo` call in a query — `ef-core-data-access`.

### 4. A map that has configuration to hand down ends with `IncludeAllDerived`

When responses form an inheritance chain, configure shared members once and close
that map with `IncludeAllDerived()`. This applies at every level that has something
to propagate, not only at the root: an intermediate map carries its own delta *and*
`IncludeAllDerived()`. A leaf map declares its delta and omits it.

```csharp
// EntityBaseResponse.cs — root of the chain
CreateMap<Entity, EntityBaseResponse>()
    .ForMember(des => des.Computed, opt => opt.MapFrom(/* ... */))
    .IncludeAllDerived();

// EntityDetailResponse.cs — intermediate: its own delta, and still hands down
CreateMap<Entity, EntityDetailResponse>()
    .ForMember(des => des.Extra, opt => opt.MapFrom(/* ... */))
    .IncludeAllDerived();

// EntitySummaryResponse.cs — leaf: nothing derives from it
CreateMap<Entity, EntitySummaryResponse>();
```

**Why:** without it, every derived response repeats the base's `ForMember` calls,
and the copies drift the moment the base changes. `IncludeAllDerived` is a tool this
codebase reaches for, not an obligation — a derived map that needs none of the
base's configuration is free to stand alone.

`IncludeMembers(x => x.Nested)` is the same idea applied to composition rather than
inheritance: it flattens a nested member's map into the destination instead of
re-declaring its members.

### 5. One registration call, anchored by the marker profile, discovery by scan

AutoMapper is registered once at the composition root. The call names the marker
profile type as its assembly anchor and enables collection mappers:

```csharp
services.AddAutoMapper(cfg => cfg.AddCollectionMappers(), typeof(MappingProfile));
```

Every profile in that assembly is then found automatically — which is what makes
principle 1 affordable. Profiles are colocated because registration never has to
know where they are, and adding a profile is never a two-file edit. A profile that
does not apply is nearly always in the wrong assembly, not missing from a list.

`AddCollectionMappers()` enables equivalency-based collection mapping: where a map
declares how two items are considered the same, the collection is updated in place
rather than replaced wholesale. It is enabled globally and opted into per map, so it
belongs in the registration, stated once.

**Why one call:** a second `AddAutoMapper` anywhere means two configurations, and
which one resolves depends on registration order — a bug with no compiler signal.

> Asserting the configuration is valid in a test — `dotnet-testing`.

## Patterns

### Request → entity: conditional and ignored members

Shared members are configured on the base request map; a derived request map then
carries only what the derived request adds.

```csharp
// EntityRequest.cs — the base request owns the shared configuration
public class EntityRequestMapping : Profile
{
    public EntityRequestMapping()
    {
        CreateMap<EntityRequest, Entity>()
            .ForMember(des => des.Secret, opt =>
            {
                opt.Condition(src => src.Secret != null);
                opt.MapFrom(src => Protect(src.Secret));
            })
            .ForMember(des => des.Attachment, opt => opt.Ignore())
            .IncludeAllDerived();
    }
}

// CreateEntityRequest.cs — inherits everything above
public class CreateEntityRequestMapping : Profile
{
    public CreateEntityRequestMapping() => CreateMap<CreateEntityRequest, Entity>();
}
```

**Why:** without `Condition`, an absent member overwrites a stored value with `null`
on update — the map cannot tell "not supplied" from "cleared". Pairing `Condition`
with a transforming `MapFrom` also keeps the transform from running on a value that
was never sent. `Ignore` is a statement, not a silence: it records that the member
is filled deliberately by another step, so a reviewer does not read the omission as
an oversight.

**And a statement earns its line.** **AutoMapper copies only members it finds on
the source**, so a destination member the source type does not declare is never
written — with or without the `Ignore`. Ignoring it changes no behaviour; it is
noise, and noise buries the deliberate ignores a reviewer must trust.

Two things follow, and the second one is a trap:

- **Check for the configuration test before deleting any of them.** Where
  `AssertConfigurationIsValid` runs, every destination member must be mapped or
  ignored, so those lines are load-bearing and none of them is noise. Grep the
  test sources — a hit inside `bin/` or `obj/` is the AutoMapper assembly, not
  a test, and proves nothing.
- **An `Ignore` is not a mass-assignment control.** What stops a caller writing
  `CreatedBy` is the request type not declaring it. Ignoring server-owned
  members on a request map is neither a safety measure to credit nor, where it
  is absent, an exposure to report.

Delegate-based options are safe on a request map because it runs against
materialized objects and its destination is an entity, not a projected shape. The
gate is still principle 3's reachability test, not the map's direction.

### Entity → response: computed and wrapped members

Three shapes, chosen by reuse.

**Inline LINQ**, for a computation used once:

```csharp
CreateMap<Entity, EntityBaseResponse>()
    .ForMember(des => des.LatestValue, opt => opt.MapFrom(
        src => src.Children.OrderByDescending(x => x.CreatedAt).FirstOrDefault()))
    .IncludeAllDerived();
```

**A static expression on the entity**, for a computation used more than once. The
entity declares it once as a `static readonly` expression field; every map passes it
to `MapFrom` by name:

```csharp
public class Entity : BaseEntity
{
    internal static readonly Expression<Func<Entity, int>> ActiveCount = x
        => x.Children.Count(c => c.IsActive);
}
```

```csharp
// in any response file that needs the same number
CreateMap<Entity, EntityBaseResponse>()
    .ForMember(des => des.ActiveCount, opt => opt.MapFrom(Entity.ActiveCount));
```

`internal` is sufficient because the entities and the response profiles that consume
them live in the same assembly — the expression is shared across modules, not
exposed beyond them.

**Constructing a wrapper**, where the response exposes a raw stored value as a
richer type:

```csharp
CreateMap<Entity, EntityBaseResponse>()
    .ForMember(des => des.Reference, opt => opt.MapFrom(
        src => new Wrapper(src.Reference, true)));
```

**Why:** all three stay inside expression trees, so all three survive query
projection — which is why none of them reaches for a delegate. The static form is
how this codebase shares computed logic: the rule lives once on the entity that owns
it, and every response mapping the same value gets the same answer. Duplicating the
same LINQ into three response maps is how they drift apart. Wrapper construction
belongs in the map for the same reason — the entity keeps the storage-level value,
the response carries the type the caller should see, and the conversion exists once.

### `IncludeMembers`: flattening a nested member

When a destination's members come partly from a nested member of the source, declare
that member's own map and pull it in:

```csharp
CreateMap<NestedEntity, EntityBaseResponse>();

CreateMap<Entity, EntityBaseResponse>()
    .IncludeMembers(x => x.Nested);
```

Members the outer map does not resolve fall through to the included member's map.
More than one member may be flattened into the same destination:

```csharp
CreateMap<Entity, EntityBaseResponse>()
    .IncludeMembers(x => x.Primary, x => x.Secondary);
```

**Why:** it removes a `ForMember` per flattened property, and the nested map stays
reusable on its own. Where `IncludeAllDerived` hands configuration *down* an
inheritance chain, `IncludeMembers` pulls it *in* from a composed member — the same
instinct, applied to the other axis.

### Maps outside query projection

A map that is never reached from a query projection may use delegate-based
configuration.

`ConvertUsing`, translating one whole type to another:

```csharp
CreateMap<SourceState, TargetState>()
    .ConvertUsing((src, des) => src switch
    {
        SourceState.First => TargetState.Pending,
        SourceState.Second => TargetState.Rejected,
        _ => TargetState.Pending,
    });
```

`AfterMap`, for fixup that needs the finished destination in hand:

```csharp
CreateMap<Entity, EntityDetailResponse>()
    .AfterMap((src, des, context) => { /* fill members the map could not */ });
```

`opt.PreCondition` also exists, gating a member before its source value is resolved.
It is uncommon in this codebase and `Condition` covers the same need in almost every
case.

**Why:** these run as compiled delegates against materialized objects. That is what
makes them expressive and what makes them untranslatable — which is the whole of the
reachability rule in principle 3. Before adding one, establish that no query
projection can reach the map, including through `IncludeAllDerived` or
`IncludeMembers`.

### Opting a map into equivalency updating

Collection mappers are enabled globally at registration, but a map opts *in* to
equivalency updating by declaring how two elements are recognised as the same one:

```csharp
CreateMap<NestedEntity, NestedEntity>()
    .EqualityComparison((src, des) => src.Id == des.Id);
```

**Why:** without a comparison the destination collection is replaced wholesale —
every element is new, and anything tracking those elements sees a full
delete-and-insert. With one, matching elements are updated in place and only
genuinely new ones are added. This is what a synchronisation or seeding map needs; a
read-side response map does not, which is why the behaviour is opt-in per map rather
than global.

> On AutoMapper v12 the configuration takes a single argument, and
> `AddCollectionMappers()` comes from the collection extensions package.

## Anti-patterns

### Delegate configuration on a projection-reachable map

A map reached from a query projection — directly, or through `IncludeAllDerived` or
`IncludeMembers` — must not use `AfterMap` or `ConvertUsing`. The callback cannot be
translated, and the value silently arrives unset.

```csharp
// BAD — this base map is reached from a query projection
CreateMap<Entity, EntityBaseResponse>()
    .AfterMap((src, des) => des.ActiveCount = src.Nested.Count(x => x.IsActive))
    .IncludeAllDerived();

// GOOD — an expression the provider can translate
CreateMap<Entity, EntityBaseResponse>()
    .ForMember(des => des.ActiveCount, opt => opt.MapFrom(Entity.ActiveCount))
    .IncludeAllDerived();
```

**Why:** delegates are not expression trees. The projection either fails or silently
drops the member, and it does so in the query, far from the profile that caused it.
Trace upward before adding a callback — a base map three levels above may be the one
that is projected.

### `ForMember` on a computed get-only property

```csharp
// BAD — Latest has no setter; this configuration never runs
public NestedResponse? Latest
    => Nested?.OrderByDescending(x => x.CreatedAt).FirstOrDefault();

CreateMap<Entity, EntityDetailResponse>()
    .ForMember(des => des.Latest, opt => opt.MapFrom(
        src => src.Nested.OrderByDescending(x => x.CreatedAt).FirstOrDefault()));

// GOOD — map the member the property computes from, and let it compute
CreateMap<Entity, EntityDetailResponse>()
    .ForMember(des => des.Nested, opt => opt.MapFrom(src => src.Nested));

// GOOD — or, if the value must come from the source, give it a setter and map it
public NestedResponse? Latest { get; set; }
```

**Why:** the configuration is dead, and dead in a way that reads as live — a reader
assumes the mapped expression is what produces the value, changes it, and nothing
happens. Do not configure a member that cannot be assigned; either map what it
derives from, or make it assignable.

### A profile name that points at a different type

```csharp
// BAD — name says Default, map produces Detail
public class EntityDefaultResponseMapping : Profile
{
    public EntityDefaultResponseMapping()
        => CreateMap<Entity, EntityDetailResponse>();
}

// GOOD
public class EntityDetailResponseMapping : Profile
{
    public EntityDetailResponseMapping()
        => CreateMap<Entity, EntityDetailResponse>();
}
```

Abbreviating the suffix away — `EntityBaseMapping` for `EntityBaseResponse` — fails
the same way.

**Why:** profiles are scattered across DTO files by design and found by scan, so the
class name is the only index into them. A name that points at a sibling type sends
the next reader to the wrong file, and one that drops part of the DTO's name breaks
the search that would have found it.

### Assigning to the `dest` parameter inside `ConvertUsing`

```csharp
// BAD — the assignment is not what AutoMapper uses
CreateMap<SourceState, TargetState>()
    .ConvertUsing((src, des) => des = src switch
    {
        SourceState.First => TargetState.Pending,
        _ => TargetState.Rejected,
    });

// GOOD — return the value
CreateMap<SourceState, TargetState>()
    .ConvertUsing((src, des) => src switch
    {
        SourceState.First => TargetState.Pending,
        _ => TargetState.Rejected,
    });
```

**Why:** AutoMapper takes the delegate's **return value**. An assignment expression
happens to evaluate to the assigned value, so the bad form produces the right answer
by accident and teaches a false model — that writing to `des` is what sets the
result. Someone who believes that will write a multi-statement body that assigns to
`des` and returns something else, and it will not work.

## Decision Guide

| Scenario | Recommendation |
|---|---|
| Adding a `CreateMap` — which file? | The file that declares the **source** type. Exception: entity → response goes in the **response** file. |
| Profiles piling up — collect them in a mapping folder? | No. Colocation is the rule; the marker profile stays empty and is only the assembly-scan anchor. |
| Naming the profile class | `<DtoTypeName>Mapping`, matching the DTO whose file it sits in — for a request map that is the request, which is the source. |
| Computed member, used by one map | Inline LINQ inside `MapFrom`. |
| Computed member, used by more than one map | `internal static readonly Expression<Func<Entity, T>>` field on the entity, passed as `MapFrom(Entity.Field)`. |
| Response exposes a stored value as a richer type | Construct the wrapper inside `MapFrom`. |
| Destination members come from a nested member | `IncludeMembers(x => x.Nested)` plus that member's own `CreateMap`. |
| Base and derived responses share configuration | Configure at each level that has something to hand down and end with `IncludeAllDerived()`; the leaf omits it. |
| Map needs post-processing, and no query projection can reach it | `AfterMap` / `ConvertUsing` may be used. |
| Map needs post-processing, but a projection can reach it | Not available. Express the value as an expression instead. |
| Collection should be updated in place rather than replaced | Opt in with `.EqualityComparison((src, des) => src.Id == des.Id)`. |
| `ReverseMap` considered | Rare in this codebase; no house ruling. |
| Writing the `ProjectTo` call in a query | `ef-core-data-access`. |
| Asserting the mapping configuration is valid | `dotnet-testing`. |
| Unsure which skill owns the question | `choosing-a-dotnet-skill`. |
