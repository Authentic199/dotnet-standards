# ValidatorService

**When:** something needs FluentValidation to run on an object that never went through model binding — a background job payload, a webhook body, an object assembled inside a service.

**Why a service and not `IValidator<T>` injected directly:** the caller is generic over `T` and often long-lived (a singleton, a job handler). Injecting `IValidator<T>` would force the caller to know `T` at construction time, and injecting a scoped validator into a longer-lived object captures it. This service takes one scope factory and resolves the right validator per call.

> **Corrected canon.** Resolve inside a `using` scope, as below. Three other shapes of this service exist in the corpus and two of them leak the scope: one resolves through a `Service<T>()` helper that creates a scope and returns the service without disposing it, and one calls `serviceProvider.CreateScope().ServiceProvider.GetRequiredService<T>()` inline with nothing holding the scope. The third disposes correctly but takes `IServiceProvider` instead of `IServiceScopeFactory`, carries no marker interface, and drops the `CancellationToken`. Recreate **this** form.

```csharp
using Core.Common.Exceptions;
using Core.Common.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Facades.Common.Services;

public interface IValidatorService : IScopedService
{
    Task ValidateAsync<T>(T instance, CancellationToken ct = default)
        where T : class;
}

public class ValidatorService(IServiceScopeFactory serviceScopeFactory) : IValidatorService
{
    public async Task ValidateAsync<T>(T instance, CancellationToken ct = default)
        where T : class
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IValidator<T> validator = scope.ServiceProvider.GetRequiredService<IValidator<T>>();
        ValidationResult validationResult = await validator.ValidateAsync(instance, ct);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException(validationResult.Errors[0].ErrorMessage);
        }
    }
}
```

## Notes

- **`IServiceScopeFactory`, not `IServiceProvider`.** The injected provider belongs to whatever scope constructed this service — which may be the root, if the caller is a singleton. The factory always produces a genuinely new scope regardless of who is calling.
- **A fresh scope per call is deliberate**, not an oversight to optimise away. Validators frequently depend on a `DbContext`; sharing one across calls in a background job is exactly the leak this shape avoids. The `using` is the load-bearing token — without it the scope and everything it resolved stay alive until GC, which for a `DbContext` means a held connection.
- **`GetRequiredService`, not `GetService`.** A missing validator registration is a wiring bug that should surface at the first call with a clear message, not a silent null.
- **First error only.** This throws on `Errors[0]`, so the caller sees one message rather than a list. That is the contract; change it deliberately if the caller needs all of them. `Errors[0]` is safe because FluentValidation reports `IsValid == false` only when the error list is non-empty.
- **`ct` reaches `ValidateAsync`.** This is the parameter most often dropped when this class is retyped from memory; async validators that hit the database will not observe cancellation without it.
- **Primary constructors need C# 12.** On an older language version, use a conventional constructor and a `private readonly IServiceScopeFactory` field — behaviour is identical.

## Dependencies and registration

Packages: `FluentValidation`, `Microsoft.Extensions.DependencyInjection.Abstractions`. Needs a `BadRequestException` (a 400-mapped exception type) and the `IScopedService` marker.

Registration is by marker scan — the class is picked up automatically because its interface derives from `IScopedService`:

```csharp
services.Scan(scan => scan
    .FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
    .AddClasses(filter => filter.AssignableTo<IScopedService>())
        .AsImplementedInterfaces()
        .WithScopedLifetime()
);
```

If the project has no marker-interface convention, drop `: IScopedService` from the interface and register explicitly:

```csharp
services.AddScoped<IValidatorService, ValidatorService>();
```

Either way the **validators themselves** must also be registered, or `GetRequiredService<IValidator<T>>` throws:

```csharp
services.AddValidatorsFromAssembly(typeof(SomeValidator).Assembly);
```
