## What Core contains

`Core` is the **contract layer**, not a domain layer and not a bag of primitives.
The test for putting a type here is not "is it small?" — it is **"must two or more
layers name this type?"**

`Core` references **no project**: it sits at the bottom of the chain and everything
else points down at it. It carries exactly **two packages** — `Humanizer` and
`NewId`. (Analyzer packages come from the solution-level build props, not from this
project; a `PackageReference Update` entry is not a Core dependency.)

```
Core/
├── Bases/                 # entity base types
└── Common/
    ├── Exceptions/        # exception hierarchy + result wrappers
    └── Interfaces/        # DI lifetime markers, shared contracts
```

### `Bases/` — entity base types

```csharp
public abstract class BaseEntity : BaseEntity<Guid>, IGuidIdentify
{
    protected BaseEntity() => Id = NewId.Next().ToGuid();
}

public abstract class BaseEntity<TId> : IEntity
{
    public TId Id { get; set; } = default!;

    public virtual DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public interface IIdentify<T> { public T Id { get; set; } }

public interface IGuidIdentify : IIdentify<Guid> { }
```

- Guid keys are **sequential** (`NewId.Next().ToGuid()`), never `Guid.NewGuid()`:
  sequential values keep index inserts append-only, and the entity has its id
  before it is ever persisted. `NewId`'s types live in the `MassTransit`
  namespace even though the package is `NewId`, so the file carries
  `using MassTransit;` — expected, not a stray dependency.
- Non-Guid keys derive from `BaseEntity<TId>` directly (`BaseEntity<int>`), which
  gives `Id` + `CreatedAt` and nothing more.
- `IEntity` is an **empty marker** — it exists to be named by generic constraints
  and assembly scans, not to declare members.
- The base carries no audit user, no soft-delete flag, no domain-event list.
  Entities that need those declare them themselves.

### `Common/Interfaces/` — lifetime markers and `ICode`

There are **exactly two** lifetime markers, both empty:

| Marker | Registered as |
|---|---|
| `IScopedService` | scoped |
| `ITransientService` | transient |

The marker goes on the **service interface**, not on the implementation:

```csharp
public interface IReportService : IScopedService { }
```

**Implementing the marker *is* the lifetime decision.** A startup assembly scan
registers every implementation with the matching lifetime — there is no
`AddScoped` line to write, and no registration file to keep in sync with the
services. (Scan mechanics: see the Facades section.)

**There is deliberately no singleton marker.** A shared instance is a decision
with real consequences, so singletons are registered explicitly in the owning
facade's `Startup.cs`, where the configuration and the ordering are visible at
the call site. Every singleton in the system is one deliberate line of code. Do
not add a third marker.

`ICode` is the one other contract here:

```csharp
public interface ICode
{
    public string? Code { get; }
}
```

It marks types that carry a unique business code, and it exists so that a
**single shared persistence configuration** can be constrained to
`where T : class, ICode` and applied to all of them. That is why a one-member
interface belongs at the bottom of the graph: the types that implement it and
the code that configures them live in different places.

### `Common/Exceptions/` — the hierarchy and the wrappers

```
Exception
└── CustomException                  // base for everything thrown on purpose
    └── HttpCustomException          // + HttpStatusCode StatusCode, object? Value
        ├── BadRequestException      // 400
        ├── UnAuthorizedException    // 401
        ├── ForbiddenException       // 403
        └── InternalServerException  // 500
```

Concrete types are **`sealed`** and do exactly one thing: pin the status code.
Each takes `(string? message)` and `(string? message, Exception? innerException)` —
**no constructor takes an `object data` payload.** Structure a caller wants to
convey belongs in the message or in a purpose-built response, not smuggled
through the exception.

The two response wrappers live in the same folder:

| Wrapper | Members | Produced by |
|---|---|---|
| `SuccessResultWrapper<TData>` | `Message`, `Data` | the Web layer's base controller |
| `ErrorResultWrapper` | `TraceId`, `Exception`, `Source`, `Method`, `Line`, `Message`, `SupportMessage`, `StatusCode` | the exception-handling middleware, in Infrastructure |

`ErrorResultWrapper` has no `Data` property — an error response carries
diagnostics, not a payload.

These sit in `Core` for the same reason the exceptions do: **the thrower and the
shaper are different layers.** Infrastructure services throw, Web controllers
wrap successes, the middleware shapes failures. A contract consumed on both
sides of a project boundary belongs at the bottom of the graph.

### Growing `Core`: add leaves, never reshape

Extend `Core` by adding a **new leaf under an existing contract**, never by
bending a contract every layer already depends on.

Worked example — a production system needed HTTP 423 when concurrency locking
arrived. That was one new file, in the house shape: `sealed`, two constructors,
and no `[Serializable]`/`SerializationInfo` ceremony — that serialization path
is obsolete on modern .NET (SYSLIB0051), and new exceptions do not carry it:

```csharp
using System.Net;

namespace Core.Common.Exceptions
{
    public sealed class LockedException : HttpCustomException
    {
        public LockedException(string? message)
            : base(message)
        {
            StatusCode = HttpStatusCode.Locked;
        }

        public LockedException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            StatusCode = HttpStatusCode.Locked;
        }
    }
}
```

`HttpCustomException` was untouched, the four existing exceptions were untouched,
and the error middleware needed no change: it matches on `HttpCustomException`
and reads `StatusCode` and `Message`, so a new subclass is handled the day it is
written. That is what a leaf costs.

Reshaping is the opposite, and it forces every layer to re-agree: a third
lifetime marker, a `Data` property on `ErrorResultWrapper`, an `object data`
constructor on `HttpCustomException`, extra members on `IEntity`. Don't.

### If you are unsure whether a type belongs in `Core`

Any one of these means it belongs in a facade or a module instead:

- It needs a `using` for EF Core, ASP.NET Core, or any package beyond
  `Humanizer` and `NewId`. Adding a `PackageReference` to `Core` is an
  architecture change, not a convenience.
- Only one layer will ever reference it.
- Its name states a business concept.
