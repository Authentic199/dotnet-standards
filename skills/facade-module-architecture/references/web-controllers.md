## Web — Controllers

`Web/Controllers/` holds controllers and nothing else: `BaseController.cs` at the root,
then one folder per module that exposes HTTP endpoints, named after the module.

```
Web/Controllers/
  BaseController.cs
  Orders/OrdersController.cs           # standard form — one file per module
  Terminals/                           # grown form — split by functional role
    TerminalsController.cs
    TerminalsController.Auth.cs
    TerminalsController.Profile.cs
```

### `BaseController` — the only base

```csharp
[Route("api/[controller]")]
[ApiController]
public class BaseController : ControllerBase
{
    // bodies elided — each wraps the payload in SuccessResultWrapper
    protected ActionResult<SuccessResultWrapper<TData>> OkWrapper<TData>(TData? data = default, string? message = default);
    protected ActionResult<SuccessResultWrapper<TData>> CreatedWrapper<TData>(string uri, TData? data = default, string? message = default);
    protected ActionResult<SuccessResultWrapper<TData>> AcceptedWrapper<TData>(TData? data = default, string? message = default);
}
```

Every controller inherits `BaseController`, never `ControllerBase` directly, and none
declares its own `[Route]` — the URL shape is decided in exactly one file. These three
wrappers are the only success path: **controllers wrap successes; the exception middleware
shapes failures** (see *The composition root & configuration*). A controller never builds
an error response — it throws, and the middleware answers.

### Endpoint anatomy

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

The constructor injects the module's service interface and nothing else. Every endpoint
carries an XML `<summary>`, its routing attribute, `[HasPermission]`, and two
`ProducesResponseType` lines — the second with `typeof(ErrorResultWrapper)` for 400, so the
error envelope is documented next to the success one. Every endpoint takes a
`CancellationToken` and passes it down, and the body is a single delegating call wrapped in
`OkWrapper` with a message from `Messages<T>` (Definitions facade). Body style —
expression-bodied or block-bodied — is not legislated here.

### When a controller grows

Split by functional role into SUFFIX-named partials, exactly as a module service splits
(see *Infrastructure — the Modules axis*): the core file carries **no suffix**
(`OrdersController.cs`) and is the one that declares `: BaseController`; every other part
declares only `public partial class OrdersController`, with no base list. The route is
unchanged, so the split is invisible to API consumers.

- No business logic in a controller: no repository, no `DbContext`, no rule evaluation —
  the module service owns all of it.
- DTO shapes, versioning and OpenAPI detail belong to the api-surface skill; permission
  internals belong to the auth-and-security skill.
