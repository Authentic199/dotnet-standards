## Endpoint anatomy — worked controllers

Two controllers, complete: the standard single-file form, and the same
controller after it grew into partials. Everything the SKILL body states as a
rule is applied here rather than restated. The domain is neutral — substitute
your own.

### The standard form — one file

```csharp
using Infrastructure.Facades.Auth.Jwt;
using Infrastructure.Facades.Common.Responses;
using Infrastructure.Modules.Orders.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers.Orders;

public class OrdersController : BaseController
{
    private readonly IOrderService orderService;

    public OrdersController(IOrderService orderService)
    {
        this.orderService = orderService;
    }

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

    /// <summary>
    /// Search orders.
    /// </summary>
    [HttpGet]
    [HasPermission(permissions: Permissions.Orders + Operations.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResultWrapper), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SuccessResultWrapper<PaginationResponse<OrderDefaultResponse>>>> SearchAsync(
        [FromQuery] SearchOrderRequest request,
        CancellationToken cancellationToken)
        => OkWrapper(await orderService.SearchAsync(request, cancellationToken), Messages<Order>.Search());

    /// <summary>
    /// View one order.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission(permissions: Permissions.Orders + Operations.ViewDetail)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResultWrapper), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SuccessResultWrapper<OrderResponse>>> GetDetailAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
        => OkWrapper(await orderService.GetDetailAsync(id, cancellationToken), Messages<Order>.Detail());

    /// <summary>
    /// Update an order.
    /// </summary>
    [HttpPut("{id:guid}")]
    [HasPermission(permissions: Permissions.Orders + Operations.Update)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResultWrapper), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SuccessResultWrapper<OrderResponse>>> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateOrderRequest request,
        CancellationToken cancellationToken)
        => OkWrapper(await orderService.UpdateAsync(id, request, cancellationToken), Messages<Order>.Update());

    /// <summary>
    /// Delete multiple orders.
    /// </summary>
    [HttpPost("BulkDelete")]
    [HasPermission(permissions: Permissions.Orders + Operations.Delete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResultWrapper), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SuccessResultWrapper<MultipleIdentiferResponse>>> DeleteRangeAsync(
        [FromBody] DeleteRangeOrderRequest request,
        CancellationToken cancellationToken)
        => OkWrapper(await orderService.DeleteRangeAsync(request, cancellationToken), Messages<Order>.Delete());
}
```

Read down the left edge: every endpoint is summary → verb → permission → two
`ProducesResponseType` → wrapped signature → one expression. The uniformity is
the point — a missing line is visible without reading the code.

- **The file-scoped `namespace …;` is the form.** A block-scoped `namespace { }`
  indents the whole file and marks it as pre-convention.
- `SearchOrderRequest` is a `QueryContainer` subclass and arrives `[FromQuery]`;
  the paging, filter and sort contract is in `references/request-response-dtos.md`.
- The search return type is
  `SuccessResultWrapper<PaginationResponse<OrderDefaultResponse>>` — two
  envelopes, both mandatory, neither invented at the endpoint.
- **`MultipleIdentiferResponse` is spelled exactly that way** — the shared facade
  type carries a typo in its name. Use it verbatim. Do not "correct" it at a call
  site: the name is a shared contract, and renaming it is a facade-wide change
  outside this skill's scope.

### The grown form — suffix partials

```
Web/Controllers/Orders/
  OrdersController.cs             # the file above: base list, field, constructor
  OrdersController.Fulfilment.cs  # public partial class OrdersController
  OrdersController.Self.cs        # public partial class OrdersController
```

```csharp
namespace Web.Controllers.Orders;

public partial class OrdersController     // no base list, no fields, no constructor
{
    /// <summary>
    /// The caller's own orders.
    /// </summary>
    [HttpGet("me")]
    [HasPermission(schemes: new[] { JwtScheme.Default })]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResultWrapper), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SuccessResultWrapper<PaginationResponse<OrderDefaultResponse>>>> SearchMyOrdersAsync(
        [FromQuery] QueryContainer request,
        CancellationToken cancellationToken)
        => OkWrapper(await orderService.SearchMyOrdersAsync(request, cancellationToken), Messages<Order>.Search());

    /// <summary>
    /// List the calling account's own shipments.
    /// </summary>
    [HttpGet("me/Shipments")]
    [HasPermission(schemes: new[] { JwtScheme.Default })]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResultWrapper), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SuccessResultWrapper<PaginationResponse<ShipmentDefaultResponse>>>> SearchMyShipmentsAsync(
        [FromQuery] QueryContainer request,
        CancellationToken cancellationToken)
        => OkWrapper(await orderService.SearchMyShipmentsAsync(request, cancellationToken), Messages<Shipment>.Search());

    /// <summary>
    /// Order totals across the caller's visible scope.
    /// </summary>
    [HttpGet("Overview")]
    [HasPermission(
        new[] { JwtScheme.Default },
        Permissions.Orders + Operations.View,
        Permissions.Orders + SubResources.Shipments + Operations.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResultWrapper), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SuccessResultWrapper<OrderOverviewResponse>>> OverviewAsync(CancellationToken cancellationToken)
        => OkWrapper(await orderService.OverviewAsync(cancellationToken), Messages<Order>.Search());
}
```

`orderService` is not redeclared — it is the core file's field, and the part uses
it directly. A part that needs a second service adds a parameter to the **core
file's** constructor.

`OverviewAsync` is the one inline signature in either file: its only parameter is
the token. Every other endpoint has two or more parameters and wraps them one per
line.

### The `[HasPermission]` shapes, in context

| Seen above | Meaning |
|---|---|
| `[HasPermission(permissions: Permissions.Orders + Operations.Create)]` | Default scheme; caller must hold the permission |
| `[HasPermission(schemes: new[] { JwtScheme.Terminal })]` | This scheme's token is the whole check — no permission required |
| `[HasPermission(new[] { JwtScheme.Default }, perm1, perm2)]` | Both: named scheme **and** permissions |

The third shape may be written fully positionally as above, or with both
arguments named —
`[HasPermission(schemes: new string[] { JwtScheme.Default }, permissions: …)]`.
Both are the same shape; naming them is never wrong.

**The trap.** The constructor is
`HasPermissionAttribute(string[] schemes = default!, params string[] permissions)`,
so the first positional argument is *schemes*:

```csharp
[HasPermission(Permissions.Orders + Operations.Delete)]   // ✗ compiles; authorizes nothing
```

The permission string binds to `schemes`, `permissions` is empty, and the
endpoint is gated only by whatever scheme name that string accidentally is. Use
the `permissions:` name whenever the scheme is default. Multi-permission
semantics and policy enforcement belong to `auth-and-security`.

### Anti-example (authorized, real) — the pre-convention file

Every annotation below is real and comes from one shipped controller, sanitized:

```csharp
namespace Web.Controllers.Categories                // ✗ block-scoped namespace
{
    public class CategoriesController : BaseController
    {
        private readonly ICategoryService categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            this.categoryService = categoryService;
        }

        [HttpPost]                                  // ✗ no <summary>
                                                    // ✗ no [HasPermission] — endpoint is open
                                                    // ✗ no ProducesResponseType at all
        public async Task<ActionResult<SuccessResultWrapper<CategoryResponse>>> CreateAsync([FromForm] CreateCategoryRequest request)
        {                                           // ✗ block body
            return OkWrapper(await categoryService.CreateAsync(request), Messages<Category>.Create());
        }                                           // ✗ no CancellationToken anywhere in the file

        [HttpPut("{id}")]                           // ✗ bare {id} — no :guid constraint
        public async Task<ActionResult<SuccessResultWrapper<CategoryResponse>>> UpdateAsync(Guid id, [FromForm] UpdateCategoryRequest request)
        {                                           // ✗ id has no [FromRoute]; ✗ 2 params unwrapped
            return OkWrapper(await categoryService.UpdateAsync(id, request), Messages<Category>.Update());
        }

        [HttpPost("BulkDelete")]
        public async Task<ActionResult<SuccessResultWrapper<MultipleIdentiferResponse>>> DeleteMany(DeleteManyCategoryRequest request)
        {                                           // ✗ no Async suffix
            return OkWrapper(await categoryService.DeleteManyAsync(request), Messages<Category>.Delete());
        }                                           // ✗ request has no [FromBody]

        [HttpGet]
        public async Task<ActionResult<SuccessResultWrapper<PaginationResponse<CategoryResponse>>>> SearchAysnc([FromQuery] SearchCategoryRequest request)
        {                                           // ✗ misspelled: SearchAysnc
            return OkWrapper(await categoryService.SearchAsync(request), Messages<Category>.Search());
        }
    }
}
```

`[FromForm]` on the create and update requests is **not** a defect — a request
carrying an upload binds from the form. Everything marked `✗` is.

Eleven defects, one file, and it compiles and serves traffic. **The danger is
proximity:** an agent adding a twelfth endpoint here will match the file and
reproduce all eleven. Match this reference instead.

Fix what you touch — the spelling, the missing attributes, the token, the
wrapping — all of which are safe, because no route template contains an action
name. **Leave `{id}` alone unless you are shipping the route change with its
clients.** Note that the samples above deliberately write `{id:guid}` even where
the file they were drawn from wrote bare `{id}`; the constraint is the rule going
forward, not a licence to retro-fit legacy routes casually.

### Anti-example (authorized, real) — naming drift

A *modern* controller, correct in every structural respect, drifting on names:

```csharp
namespace Web.Controllers.Accounts
{                                                   // ✗ block-scoped namespace
    [Authorize]                                     // ✗ redundant: HasPermissionAttribute : AuthorizeAttribute
    public class AccountsController : BaseController
    {
        /// <summary>
        /// Create an account.
        /// </summary>
        [HttpPost]
        [HasPermission(schemes: new string[] { JwtScheme.Default }, permissions: Permissions.Accounts + Operations.Create)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResultWrapper), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SuccessResultWrapper<AccountDetailResponse>>> CreateAsync(CreateAccountRequest request, CancellationToken cancellation)
            => OkWrapper(await accountService.CreateAsync(request, cancellation), Messages<Account>.Create());
            // ✗ token named `cancellation`, not `cancellationToken`
            // ✗ `request` unattributed — no [FromBody]
            // ✗ two parameters on one line — not wrapped

        /// <summary>
        /// Delete multiple accounts.
        /// </summary>
        [HttpPost("BulkDelete")]
        [HasPermission(schemes: new string[] { JwtScheme.Default }, permissions: Permissions.Accounts + Operations.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResultWrapper), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SuccessResultWrapper<MultipleIdentiferResponse>>> DeleteRangeAsync(DeleteAccountRangeRequest request, CancellationToken cancellation)
        {                                           // ✗ block body in an otherwise expression-bodied file
            return OkWrapper(await accountService.DeleteRangeAsync(request, cancellation), Messages<Account>.Delete());
        }
    }
}
```

Each defect is individually harmless, which is why the file passed review. Two
matter more than they look:

- **`cancellation` instead of `cancellationToken`** — the name is what the next
  author greps for and what the next endpoint copies; one renamed token becomes a
  file convention within three endpoints.
- **The single block body** is the crack. A file that is expression-bodied
  everywhere except one method has already established that a block is
  permissible, and the next `if` goes inside it.
