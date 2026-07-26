---
name: error-handling
description: >-
  This skill should be used when throwing or handling errors in a .NET API:
  choosing between BadRequestException, UnAuthorizedException,
  ForbiddenException and InternalServerException (400/401/403/500), returning
  business not-found, deciding to catch, rethrow or wrap with an
  innerException, adding a new sealed exception, how ExceptionHandlerMiddleware
  turns a throw into ErrorResultWrapper — TraceId, diagnostics,
  HiddenProperties production redaction — or reviewing try/catch and error
  building in controllers. Not for: success envelope, ProducesResponseType,
  endpoints — api-surface; message text — message-keys; JWT, policies,
  auth-layer 401/403 — auth-and-security; LockedException, locks,
  ConcurrencyHandler — distributed-lock; exception placement, Core hierarchy —
  facade-module-architecture; validation rules — cqrs-feature-slice.
---

## Overview

Failure has one path through this stack: **application code throws, and the
exception middleware turns the throw into the response.** Nothing in between
shapes an error — no service builds a status code, no controller catches, no
handler returns an error object. A thrown exception is not an escape hatch here;
it is the calling convention for failure.

**One carve-out.** That doctrine governs *thrown* exceptions. Automatic
validation never throws: a request that fails model binding or a validator rule
is rejected before the action runs, and `Web` answers it with a plain
`{ message }` object built by its `InvalidModelStateResponseFactory` — the
exception middleware is never involved. That factory is `Web`'s to own
(`facade-module-architecture`: the only decisions `Web` owns are controller JSON
behavior and the shape of the invalid-model response), and which rules a
validator declares belongs to `cqrs-feature-slice`.

`facade-module-architecture` → `references/core-contracts.md` legislates the
*shape* of the exception hierarchy — the leaves are `sealed`, each takes
`(string? message)` and `(string? message, Exception? innerException)`, none
takes a payload. This skill decides **which one you throw, when you throw
instead of catching, and what the middleware does with it.**

**Stances — settled; do not propose the alternative.**

| Question | Answer |
|---|---|
| What produces an error response? | The exception middleware, alone. A controller **throws**; it never builds `ErrorResultWrapper` |
| How does an exception carry data? | It doesn't. `(message)` and `(message, innerException)` are the only constructors |
| Is there a `NotFoundException`? | **No, and none gets added.** Business not-found is `BadRequestException` 400 |
| Where do 404s come from? | Routing only — a malformed id dies on the `{id:guid}` constraint (`api-surface`'s law) |
| What does a service do with an exception it cannot handle? | Nothing. Let it bubble |
| Where does the message text come from? | `Messages<T>` — its grammar belongs to `message-keys` |

Answer day-to-day questions from this file; open
`references/middleware-behavior.md` only when you are changing the middleware
itself, debugging an envelope that came out wrong, or configuring redaction.

## Choosing the exception

The four leaves are not four equal options. Real usage is lopsided, and the
proportion *is* the guidance — they are listed most-thrown first:

| Exception | Status | Throw when |
|---|---|---|
| `BadRequestException` | 400 | **The workhorse.** The request cannot be honoured as sent — a value is missing, malformed, expired, already used, in the wrong state, or names something that does not exist |
| `InternalServerException` | 500 | An invariant **the code owns** is broken — a required registration or setting is absent, a persistence step failed, a dependency answered nonsense |
| `UnAuthorizedException` | 401 | Identity cannot be established, or the authenticated principal's own record is gone, blocked, or no longer valid |
| `ForbiddenException` | 403 | Reserved — and never thrown in practice. See below |

The dividing line between the top two: **if the caller can fix it by changing
the request, it is a 400, not a 500.**

**The message argument comes from `Messages<T>`.** Its key grammar belongs to
`message-keys`; load that skill before writing the argument, not this one.

### Business not-found is a 400

**Anything the caller asked for that does not exist is `BadRequestException`.**
There is no `NotFoundException` and none gets added.

```csharp
Order order = await repository.FindAsync(id, cancellationToken)
    ?? throw new BadRequestException(Messages<Order>.NotFound());
```

The reason is that **404 already has an owner: routing.** `{id:guid}` means a
malformed id dies in the route table as a 404 before any code runs
(`api-surface`'s law). If a well-formed id that simply matches no row also
answered 404, the caller could not tell "you sent nonsense" from "you sent a
valid id for a row that isn't there" — and the second is a lookup result, not a
routing outcome.

**The one exception is the caller's own identity.** When the *current
principal's* record cannot be found, the token describes someone who no longer
exists and the caller must re-authenticate:

```csharp
User user = await repository.FindAsync(currentUser.Id, cancellationToken)
    ?? throw new UnAuthorizedException(Messages<User>.NotFound());
```

### `ForbiddenException` is reserved, and honestly unused

Zero sites throw it. That is the true state of the codebase, not an oversight to
go and correct.

**Real 403s come from the authorization layer, and they are not enveloped.** The
exception middleware is registered *before* authentication and authorization, so
it wraps them and anything they **throw** is caught — that is exactly how a
JWT-verify failure arrives as a proper 401 envelope. But a policy denial does not
throw. It writes a bare 403 and short-circuits the pipeline, and a response that
was never an exception never becomes an `ErrorResultWrapper`. The same is true of
an authentication challenge for a missing or expired token.

Two consequences:

- **Do not document a `[HasPermission]` denial as the enveloped shape.** How the
  policy denies is `auth-and-security`'s story.
- **`ForbiddenException` is the leaf for an enveloped 403 you decide yourself** —
  a caller who *is* authenticated and *did* pass the policy, but still may not
  perform this act for a reason only the domain knows, and who should get a
  message explaining it. Rare by design.

## Catch or bubble

**Default: do not catch.** An exception that reaches the middleware uncaught
becomes a 500 carrying a trace id, the exception text, its source type, method
and line, and an error log entry. A `catch` that adds nothing *removes* all of
that.

**Wrap only when the catch states something the exception cannot.** Two shapes
earn it:

```csharp
// 1. A transaction that must be unwound before the failure surfaces.
await repositoryWrapper.RollbackTransactionAsync(cancellationToken);
throw new InternalServerException(Messages<Order>.Create(false));

// 2. A dependency whose own exception means nothing to the caller.
throw new InternalServerException(Messages<PaymentGateway>.Action("Verify", false), ex);
```

In the second shape the inner exception is not decoration: on an
`InternalServerException` **that carries an inner exception**, the middleware
builds its diagnostics from the *inner* one. The caller reads your message; the
log points at the real fault.

**`throw new InternalServerException(ex.Message, ex)` is not house style.** It
re-raises the same text with the same inner exception and one more stack frame,
and the middleware would have produced an equivalent 500 — with better
diagnostics — had the exception been left alone. If a catch block cannot say
something the exception did not already say, delete the catch block.

| Shape | Why it is wrong |
|---|---|
| `catch` → build an `ErrorResultWrapper`, or set `Response.StatusCode` by hand | A second producer of error responses. Now two places decide the contract, and they drift |
| `catch` → swallow (log and continue, or return `null`) | The request reports success while nothing happened, and the 500 that would have been logged never was |
| `catch (Exception ex) { throw new InternalServerException(ex.Message, ex); }` | Adds a frame and no context |
| `try`/`catch` inside a controller action | The action is one expression (`api-surface`) — there is nowhere to put a `try`, which is the point |

## The error envelope

Every enveloped failure is an `ErrorResultWrapper`, serialized with
default-valued members omitted — so an absent field is absent, not `null`.

| Path | Status | Message | Diagnostics |
|---|---|---|---|
| Any `HttpCustomException` | its pinned `StatusCode` | its `Message` | none |
| `InternalServerException` **with** an inner exception | 500 | its `Message` | from the **inner** exception |
| Any other exception | 500 | the exception's own message | from that exception |

Diagnostics means `TraceId`, `SupportMessage` (a fixed template quoting the trace
id back to the caller), `Exception`, `Source`, `Method` and `Line`.

**The contract between a leaf and the middleware is two members: `StatusCode`
and `Message`.** Nothing else about a leaf is consulted to shape the response —
which is why the growth rule below costs nothing.

**Only `StatusCode >= 500` is logged as an error.** A 400 is a normal outcome of
a public API; logging every rejected request as an error trains people to ignore
the error log. If a particular 400 is worth recording, log it where you throw it.

**Production redaction is configuration, not code.**
`ErrorResponseSettings.HiddenProperties` names envelope properties to blank
before serialization; because blanked members are then omitted entirely, a
redacted field disappears rather than showing up empty. The base configuration
hides nothing; the Production overlay hides `Source`, `Method`, `Exception` and
`Line`, leaving the caller a message, a status, a trace id and the sentence
telling them to quote it. Debugging a stripped production error starts at that
setting, not in code. Note what redaction does **not** cover: the `Message` of an
unhandled exception is the exception's own text — one more reason to throw a leaf
with a written message rather than let an infrastructure exception describe your
system to a stranger.

**Read `references/middleware-behavior.md` when** changing the middleware,
adding or configuring `HiddenProperties`, or working out why a response came back
with the wrong shape, status or missing fields — it carries the full handler
walkthrough, the pipeline position and its consequences, and the redaction
mechanics.

## Adding a new exception

**A leaf that pins a status is free.** Derive from `HttpCustomException`, set
`StatusCode` in every constructor, add nothing else. The middleware matches on
the base type and reads `StatusCode` and `Message`, so the new exception is
handled the day it is written — no middleware change, no registration, no
`catch`. `core-contracts.md` carries the worked example (HTTP 423, added when a
locking facade arrived) and the file shape: `sealed`, two constructors, and no
`[Serializable]`/`SerializationInfo` ceremony, which is obsolete on modern .NET.
Where the file lives is `facade-module-architecture`'s law. **What that 423
means — when a lock is taken, how long it is held, what a caller should do about
it — belongs to `distributed-lock`.**

Three questions before you add one:

1. **Does it pin a status the existing leaves do not express?** If not, throw an
   existing leaf with a better message.
2. **Does it derive from `HttpCustomException`?** A leaf deriving from
   `CustomException` has no `StatusCode`, so the middleware treats it as an
   unknown exception and answers 500 — whatever the name promised.
3. **Does it need the middleware to do anything for it?** If yes, stop. That is
   the boundary, and the next two sections are why.

### Pin the status in *every* constructor

```csharp
public sealed class BadRequestException : HttpCustomException
{
    public BadRequestException(string? message)
        : base(message)
    {
        StatusCode = HttpStatusCode.BadRequest;   // pinned
    }

    public BadRequestException(string? message, Exception? innerException)
        : base(message, innerException)
    {
        // StatusCode never assigned — defect
    }
}
```

**A constructor that forgets the assignment leaves `StatusCode` at its default
`0`, and the middleware copies that straight onto the response.** The exception
is named for a 400 and produces an invalid status. This exists in a real
codebase and is latent only because nothing calls that overload yet — one
well-intentioned use of the inner-exception constructor would ship it. Pinning is
the *only* job of a leaf; a constructor that skips it has no job.

One trap while you are in that file: `HttpCustomException` also declares an
`object? Value` member, and the base constructors populate it. **It is not a
payload channel** — the middleware never reads it. Per the settled law there is
no payload channel at all; do not infer one from that member.

### A leaf may not carry a payload

**No exception takes a data payload, and `ErrorResultWrapper` has no `Data`
property.** An error response carries diagnostics, not a result. Structure the
caller needs — which of fifty submitted rows failed, and why — is a *response*,
returned on the success path and designed as such; a 400 says the request was
rejected. Both this and the wrapper's shape are settled in `core-contracts.md`;
do not reopen either from the exception side.

The counter-example that shows why the growth sanction has a boundary is a real
file-upload exception:

```csharp
public sealed class FileUploadException : CustomException   // not HttpCustomException
{
    public ICollection<string>? AddedKeys { get; set; }     // a payload
}
```

Three failures compound. It derives from `CustomException`, so it pins no status.
It carries state, so the middleware must be *taught* about it — a dedicated
`catch`, plus a storage dependency injected into the middleware for no other
reason. And as shipped, that branch builds an envelope, logs it, deletes the
uploaded files — and then returns without ever writing a response, so the caller
gets an empty reply that is not an envelope at all.

**Read this as a defect of the shape, not of one team's usage** — the same
`catch` exists unchanged in both reference codebases, which is exactly how a
convention-shaped mistake propagates. Compensating logic belongs in the operation
that knows about it (upload inside a scope that cleans up on failure, then throw
a plain leaf), never in the middleware.

## Not this skill

The success envelope, `ProducesResponseType` documentation of the error shape,
and everything about writing an endpoint — `api-surface`. The wording of any
message, success or error — `message-keys`. JWT schemes, policies, and how the
authorization layer produces its own 401s and 403s — `auth-and-security`. The 423
lock exception's meaning, lock acquisition and the concurrency handler —
`distributed-lock`. Where an exception file lives and the shape of the `Core`
hierarchy — `facade-module-architecture`. Which rules a validator declares —
`cqrs-feature-slice`.
