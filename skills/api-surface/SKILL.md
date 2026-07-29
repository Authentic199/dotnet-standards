---
name: api-surface
description: >-
  This skill should be used when shaping the HTTP surface of a .NET API:
  adding a route or endpoint, writing a controller action — expression-bodied
  bodies, segment casing, id constraints, [HasPermission],
  [ProducesResponseType], XML summaries, partial-file splits — request and
  response DTO base-class chains, colocated validator and mapping profile,
  pagination and search contracts, API versioning, or Swashbuckle/OpenAPI
  setup. Not for: where a controller file goes — facade-module-architecture;
  handler and service internals, validation rules — module-feature;
  entities, DbContext, queries — ef-core-data-access; JWT, policies,
  permission internals — auth-and-security; exception flow, error responses —
  error-handling; success and error message text — message-keys.
---

## Overview

One HTTP surface, decided in four places: the route template, the endpoint
signature, the request/response DTO pair, and the OpenAPI document generated
from them. This skill owns the *shape* of all four. **Where** a controller or
DTO file lives is `facade-module-architecture`'s law; **what goes inside it**
is decided here. What an endpoint or a validator *does* belongs to the module
service — `module-feature`.

**Stack stances — settled; do not propose the alternative.**

| Question | Answer |
|---|---|
| Minimal API or Controllers? | **Controllers** inheriting `BaseController`. No `MapGet`, no endpoint groups, no `IEndpointRouteBuilder` extensions |
| How is the API versioned? | **It is not.** See *Versioning* below |
| Swashbuckle, Scalar, or the built-in generator? | **Swashbuckle** (`AddSwaggerGen` + `UseSwaggerUI`) |
| What shapes a success response? | `OkWrapper` / `CreatedWrapper` / `AcceptedWrapper` → `SuccessResultWrapper<TData>` |
| What shapes an error response? | The exception middleware, alone. A controller **throws**; it never builds `ErrorResultWrapper` |

Answer day-to-day questions from this file; open a `references/` file only when
writing or reviewing the files that section covers.

## Routes

`BaseController` carries `[Route("api/[controller]")]` and `[ApiController]`.
**No other controller declares a `[Route]`** — the URL prefix is decided in one
file, so a controller's own attributes only ever name the verb and the tail.

| Shape | Template |
|---|---|
| Collection | `[HttpGet]`, `[HttpPost]` |
| One item | `[HttpGet("{id:guid}")]`, `[HttpPut("{id:guid}")]` |
| Named action on the collection | `[HttpPost("BulkDelete")]` |
| Named action on one item | `[HttpPost("{id:guid}/Activate")]` |
| Sub-resource of one item | `[HttpGet("{id:guid}/Shipments")]` |
| The authenticated caller's own scope | `[HttpGet("me")]`, `[HttpGet("me/Shipments")]` |
| A second identifier | `[HttpPut("me/Shipments/{shipmentId:guid}")]` |

- **`{id:guid}` always — a bare `{id}` is an anti-pattern.** The constraint is
  what keeps `api/Orders/BulkDelete` and `api/Orders/{id}` from competing for
  the same request, a malformed id dies in routing as a 404 instead of reaching
  model binding, and the document shows `format: uuid`.
- **Every Guid route parameter carries `:guid`, including the second one — and
  a second identifier is named `<thing>Id`, never a bare `id`.** Two parameters
  called `id` in one template cannot be told apart by a reader or a client
  generator.
- **Literal segments are PascalCase** (`BulkDelete`, `Activate`,
  `PaymentTransactions`) — matching the C# member they expose.
- **`me` is the one lowercase segment.** It means *the caller identified by the
  token*, so the endpoint takes no owner id at all — `me/{id}` is a
  contradiction. If a caller may address someone else's resource, that is the
  `{id:guid}` form, and it is a different permission.
- **Bulk delete is `POST BulkDelete`**, taking a range request in the body — not
  `DELETE` with a body, which clients and proxies handle inconsistently, and not
  a repeated query parameter.
- **A sub-resource nests under its owner** (`{id:guid}/Shipments`,
  `me/Shipments`), and **the class that hosts those routes is the owner's
  controller** — the controller named for the module whose resource that
  leading `{id:guid}` identifies, the parent. It hosts them as a suffix
  partial: `OrdersController.Shipments.cs`. **A full CRUD surface on the
  sub-resource does not change this** — five nested actions are still five
  routes beginning with the parent's id, so they stay on the parent's
  controller. Two names are wrong here and both compile: a concatenated
  `OrderShipmentsController`, and a top-level `ShipmentsController` hosting
  routes that open with the parent's id.
- **The child earns its own top-level controller only when its routes stop
  nesting** — when it is addressable without the parent's id (`api/Shipments`,
  `api/Shipments/{id:guid}`). That is a routing change, not a rename: decide it
  from the route templates, never from how many actions there are.
- Verbs are conventional: `POST` create, `GET` read/search, `PUT` full update of
  the addressed thing, `DELETE` single-id delete.

## Endpoint anatomy

```csharp
/// <summary>
/// Create an order.
/// </summary>
[HttpPost]
[HasPermission(permissions: Permissions.Orders + Operations.Create)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResultWrapper), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<SuccessResultWrapper<OrderResponse>>> CreateAsync(
    [FromBody] CreateOrderRequest request,
    CancellationToken cancellationToken)
    => OkWrapper(await orderService.CreateAsync(request, cancellationToken), Messages<Order>.Create());
```

Seven parts, every endpoint, no exceptions:

1. An XML `<summary>` — it is the OpenAPI operation description, so an endpoint
   without one ships an undocumented operation.
2. The verb attribute and its route tail.
3. `[HasPermission]`.
4. `[ProducesResponseType(StatusCodes.Status200OK)]`.
5. `[ProducesResponseType(typeof(ErrorResultWrapper), StatusCodes.Status400BadRequest)]`
   — the error envelope documented beside the success one.
6. An `Async`-suffixed name, spelled correctly, saying what the operation does —
   the route already says where. A misspelled name is a readability defect, not
   a contract defect: no route template contains an action name, so fixing the
   spelling is safe and is not a breaking change.
7. `CancellationToken cancellationToken` as the **last** parameter, passed into
   the service call.

**`CancellationToken` never takes `= default` on an action.** MVC always
supplies it from `HttpContext.RequestAborted`, so the default is unreachable;
what it actually communicates is that the author treated cancellation as
optional.

**The return type is always `Task<ActionResult<SuccessResultWrapper<T>>>`** —
including when `T` is a `PaginationResponse<…>` or an identifier response.
Never `IActionResult`, never a bare DTO.

The success message comes from `Messages<T>`; its key grammar belongs to
`message-keys` — load that skill before writing a message argument.

### Body style — expression-bodied only

**The body is one delegating call, so it is written as one expression.**

```csharp
    => OkWrapper(await orderService.SearchAsync(request, cancellationToken), Messages<Order>.Search());
```

A block body (`{ return OkWrapper(...); }`) is an anti-pattern, and a file mixing
both styles is the worse one — the mix is what makes a second statement look
permissible. The expression body is a structural guarantee: there is nowhere to
put the `if`, the mapping or the `try` that does not belong in a controller, and
scope creep cannot be added without reshaping the method, so it shows up in the
diff. If an endpoint cannot be written as one expression, the work belongs in
the module service.

> This resolves the point `facade-module-architecture` leaves open when it says
> body style "is not legislated here". It is legislated here.

### Signature wrapping — a countable rule

**Count every parameter, `CancellationToken` included. Two or more → one per
line, with `=>` on its own line below them; only a token-only signature stays
inline.** No character threshold, nothing to estimate.

```csharp
public async Task<ActionResult<SuccessResultWrapper<OrderResponse>>> UpdateAsync(
    [FromRoute] Guid id,
    [FromBody] UpdateOrderRequest request,
    CancellationToken cancellationToken)
    => OkWrapper(await orderService.UpdateAsync(id, request, cancellationToken), Messages<Order>.Update());
```

### Binding sources

**Every parameter states its binding source explicitly** — `[FromRoute]`,
`[FromQuery]`, `[FromBody]`, `[FromForm]` — except `CancellationToken`, which
never carries an attribute. The source decides how the request is read and how
the parameter appears in the document; `[FromQuery]` on a search DTO in
particular is what keeps a filter object out of the body. Older endpoints omit
`[FromRoute]` on the route id and rely on `[ApiController]` inference; new
endpoints do not.

### `[HasPermission]` — one constructor, three call shapes

```csharp
public HasPermissionAttribute(string[] schemes = default!, params string[] permissions)
```

One constructor, not an overload set. One shape per endpoint:

| Intent | Call |
|---|---|
| Default scheme, one or more permissions | `[HasPermission(permissions: Permissions.Orders + Operations.View)]` |
| A non-default scheme, no permission check | `[HasPermission(schemes: new[] { JwtScheme.Terminal })]` |
| A specific scheme **and** permissions | `[HasPermission(new[] { JwtScheme.Default }, Permissions.Orders + Operations.View, Permissions.Orders + Operations.ViewDetail)]` |

**The named argument is mandatory in the first two shapes.** `params` sits
second, so `[HasPermission(SomePermission)]` binds the string to `schemes` and
silently authorizes nothing — it compiles, it looks protective, and it is not.
Only the third, fully positional shape may omit the names.

Permission constants are composed by concatenation: resource + optional
sub-resource + action. **How the policy is parsed and enforced belongs to
`auth-and-security`**; this skill fixes only the call shape at the endpoint.

### Pre-convention files

Older controllers predate these rules and typically break four at once:
block-scoped `namespace { }`, block-bodied endpoints, bare `{id}`, and no
`<summary>` or `ProducesResponseType` — sometimes with a misspelled action name.
**Do not take the neighbouring endpoint as the template.** Match this file, not
the file you are editing. Fix surrounding endpoints only when you are already
changing them; a name or attribute fix is safe, but changing a *route* is a
breaking change (see *Versioning*).

**Read `references/endpoint-anatomy.md` when** writing or reviewing an endpoint,
choosing a route for something that is not plain CRUD, or picking a
`[HasPermission]` shape — it carries the full worked controller and the
annotated pre-convention file.

## Controller partials

A controller splits by functional role into **suffix-named** partials, under the
same law as a module service:

```
Terminals/
  TerminalsController.cs            # core: base list, fields, constructor
  TerminalsController.Auth.cs       # public partial class TerminalsController
  TerminalsController.Profile.cs    # public partial class TerminalsController
```

- **`: BaseController` appears on the suffix-less core file and nowhere else.**
- **The constructor injects service interfaces and nothing else** — no
  repository, no `DbContext`, no mapper, no unit of work. "Nothing else" bars
  non-service dependencies; it does not mean exactly one service. A controller
  whose route family spans modules injects each module's service, and two or
  three is normal.
- **Fields and the single constructor live in the core file.** A role part that
  needs another service does not declare a second constructor — the core file's
  constructor takes it.
- The route never changes, so the split is invisible to API consumers.

**A controller is named for one module; a two-module name is a defect.** There
is no `OrderShipmentsController`: a route family about another module's concept
under this module's resource — an order's shipments — belongs to **the parent's
controller**, the module whose resource roots the route, as a suffix part:
`OrdersController.Shipments.cs`, never a controller named for the child. The
operations behind it live in that module's service the same way — a suffix
part, `OrderService.Shipments.cs`, whose only reach into the foreign module is
a `Send` of that module's envelope; the shipment logic itself stays in the
shipment module's service (`module-feature`, *Call the service, or send a
message?*).

Four anti-patterns, all of which compile:

| Anti-pattern | Why it is wrong |
|---|---|
| `Profile.TerminalsController.cs` (prefix-named) | Sorts away from its own controller; the core file is no longer first or identifiable by name |
| Base list on a role part instead of the core file | The core file stops being the one declaration point; reviewers cannot tell which file is core |
| Base list repeated on every part | Legal C# — the compiler merges them silently — but now every part claims to be the core file |
| `OrderShipmentsController` (two-module name) | Neither module owns it. The route family is `OrdersController.Shipments.cs`; its operations are `OrderService.Shipments.cs`, reaching the foreign module only by `Send` |

## Request DTOs

**Every public property of a request, a response — and the entity behind them —
carries an XML `<summary>`.** `IncludeXmlComments` publishes property docs into
the OpenAPI schema exactly as it publishes operation docs, so a bare property
ships an undocumented field whose meaning every client guesses from the name.
This is the property-level face of the endpoint's own `<summary>` law above.

**Look up the module's existing requests before defining one.** Requests are a
mandatory inheritance chain, not a flat set: shared shape moves into a base
class, and `Create`/`Update` derive from it. Creating that base, or lightly
reshaping it to fit a second request, is the expected move — not a refactor to
be avoided.

```csharp
public abstract class OrderRequest                  // shared shape; [MessageDisplay] here
{
    public Guid? ProductId { get; set; }
}

public class CreateOrderRequest : OrderRequest      // derive to extend
{
    public string? Code { get; set; }
}

public class UpdateOrderRequest : OrderRequest { }  // an empty derived class is correct
```

**One file holds the contract, its `AbstractValidator<T>`, and its AutoMapper
`Profile`, in that order.** There is no `Validators/` folder and no `Mappings/`
folder. An empty derived class is not a smell — it exists to give the endpoint a
named contract and its own validator.

- **A derived validator starts with `Include(new OrderRequestValidator(...))`**,
  then adds only what its own properties need.
- **`[MessageDisplay(nameof(Order))]` sits on the base request**, once — it
  renames the `Messages<T>` key prefix for every derived request.
- **A base request declares a `Profile` only when its map needs member
  customization** — a conditional `ForMember`, an ignored member, a hashed
  value. That map ends `.IncludeAllDerived()`, which is the whole point: it
  pushes the customization down instead of making every derived map repeat it.
  **When the base map would be plain, the base declares no `Profile` at all and
  each derived request maps itself.** Do not add an empty base profile just to
  have one.

| Request kind | Base |
|---|---|
| Paged / filtered list | `QueryContainer` — usually an empty subclass named for the endpoint; brings filter operators, paging, search fields and sort |
| Bulk operation over ids | `RangeGuidRequest`, validated by `Include(new RangeGuidValidator<Order>(repositoryWrapper, filter))` |
| Everything else | the module's own base request |

`RangeGuidValidator` checks existence and duplicates in one `Include`; its
optional filter expression is how a bulk operation is scoped to the caller's own
rows — pass it whenever ownership matters.

**Which rules a validator declares, and how a rule reaches the database, belong
to `module-feature`.** This skill fixes only the file's shape and the
inheritance obligation.

## Response DTOs

**Every response family roots at `BaseEntity`** — every entity does, so the
response mapped from one does too, and `Id` and `CreatedAt` are then present
everywhere without redeclaration. Search documents root at `ElkBaseEntity`. The
family grows as a ladder, each rung deriving from the one above:

```csharp
public class OrderBaseResponse : BaseEntity            // identity + the few universal fields
public class OrderDefaultResponse : OrderBaseResponse  // the list projection
public class OrderResponse : OrderDefaultResponse      // the detail projection, with relations
```

Rung names in use: `<X>BaseResponse` → `<X>DefaultResponse` → `<X>Response` /
`<X>RelationResponse` / `<X>DetailResponse`.

- **Sibling duplication is the anti-pattern**: two responses both declaring
  `: BaseEntity` and re-listing the same properties. They drift, and a property
  added to one silently disappears from the other's payload. The moment two
  responses share a property, that property belongs on a common base.
- **Pick the lowest rung that carries what the endpoint needs.** A caller
  wanting one extra field gets a new rung, not a parallel class.
- **The `Profile` lives in the same file below the class**, and — unlike the
  request side — the base rung does carry it, ending `.IncludeAllDerived()` so
  derived maps inherit its member configuration.

**Lists return `PaginationResponse<T>`** — `PagedData` plus `PageInfo`
(`TotalCount`, `PageSize`, `Current`) — built by `ToPagedListAsync`. **Never
invent a new list envelope**: no `Items`/`Total` shape, no bare array, no custom
paging DTO. Clients read one list contract across the whole API.

`PaginationResponse<T, TMoreInfo>` adds exactly one member, `MoreInfo`, and is
for **a companion object the same search already computed** — a summary, totals,
or a derived recommendation beside the page. The service supplies it through the
`ToPagedListAsync(…, moreInfo, …)` overload; the controller never assembles it.
It is not a slot for unrelated payload: if the extra data is not computed by
this search, it is a second endpoint.

**Read `references/request-response-dtos.md` when** adding a request or response
type, extending an existing family, or reviewing a DTO's inheritance and its
colocated validator and profile.

## Versioning — there is none

The route is `api/[controller]` and it does not grow a version segment. **No
`/v1/`, no `Asp.Versioning` package, no `?api-version`, no version header or
media-type negotiation, no `OrdersV2Controller`, and no second Swagger document
per version.**

A breaking change is therefore a coordinated client-and-server release, which
makes the additive form the default instinct: a new optional response property,
a new optional request property whose absence preserves the previous behaviour,
a new endpoint, a new PascalCase sub-resource. Removing or renaming a property,
tightening validation, changing a route or a status code is breaking, and it
ships with its clients.

When someone asks "should we add v2?", the answer is no — extend the resource or
add a sibling endpoint. When the additive form is genuinely impossible, what
changes is the release schedule, not the URL.

## OpenAPI — Swashbuckle

**Configuration is facade-level; endpoints contribute nothing but their
attributes.** An endpoint's whole OpenAPI contribution is its `<summary>` and
its two `ProducesResponseType` lines — no `[SwaggerOperation]`, no per-endpoint
document ceremony.

Settings bind from an `openapi.json` configuration topic through the options
pattern — `SwaggerSettings`: `Enable`, `DefaultInfos` (title, version,
description, doc key, doc name, route prefix) and `Credentials`. **The whole
facade no-ops when `Enable` is false**, so a deployment can turn the document
off.

When enabled, the generator registers:

- one `SwaggerDoc` keyed from settings;
- a **JWT bearer security definition**, plus an operation filter that attaches
  the security requirement only to operations carrying `[Authorize]` — so
  unauthenticated endpoints are not falsely padlocked;
- `SupportNonNullableReferenceTypes()`;
- a **schema filter that folds enum members' XML comments** into the schema
  description;
- `IncludeXmlComments` for **both** assemblies — the one holding the DTOs and
  the entry assembly holding the controllers. One alone leaves half the document
  bare, and both need `GenerateDocumentationFile` in the csproj;
- `AddFluentValidationRulesToSwagger()`, so validator rules surface as schema
  constraints instead of being documented a second time by hand.

Outside Development the UI sits behind basic-auth middleware, with credentials
from the same settings class.

**Read `references/openapi-swashbuckle.md` when** configuring the OpenAPI
facade, adding a filter, changing the document's info or route prefix, or
debugging a missing description, enum or security padlock.

## Not this skill

Which project or folder a controller or DTO file belongs in —
`facade-module-architecture`. Handler, service and validator rule internals —
`module-feature`. Entities, `DbContext`, queries and how a paged query is
executed — `ef-core-data-access`. JWT schemes, policy handlers and permission
internals — `auth-and-security`. Exception types, middleware and how the error
envelope is built — `error-handling`. The wording of success and error messages
— `message-keys`.
