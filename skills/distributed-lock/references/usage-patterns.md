# Usage patterns

How to consume `IConcurrencyHandler` from a module. Three patterns cover every verified
production case; a use that fits none of them is a design question, not a locking
question, and the default answer is **don't lock it yet**.

Before reaching for a lock, ask whether the store can express the invariant instead — a
unique index, an idempotency key on the request, an atomic conditional update. Those
survive a Redis outage, cost nothing per request, and cannot be forgotten at a new call
site. A distributed lock is the right answer when the invariant spans several statements,
several tables or several stores, and therefore cannot be written as a constraint.

Modules inject `IConcurrencyHandler`. Never `IDistributedLockFactory`, never a semaphore
of their own.

| Situation | Pattern | Key | Provider |
|---|---|---|---|
| One resource; read, check, then write, and the three must not interleave | 1 — single-key guard | one `{Noun}:{id}` | `RedLock` |
| Several resources that must be held together for one operation | 2 — multi-key guard | a `List<string>` of `{Noun}:{id}` | `RedLock` |
| The guarded work writes where a transaction cannot reach it | 3 — guard plus compensation | one `{Noun}:{id}` | `RedLock` |
| Contention you would like to smooth out rather than reject | none — reconsider | — | — |

**Every pattern passes `Provider = ConcurrencyProvider.RedLock` explicitly.** The default
is the in-memory provider, which excludes nothing between instances, and the omission is
invisible at the call site.

---

## Pattern 1 — single-key guard around read, check, write

The canonical shape. A resource is read, business rules are evaluated against what was
read, and a write follows that is only valid if those rules still hold. Two callers
interleaving between the check and the write both pass the check.

The key names the operation and the resource pair it protects. **When the invariant is
"this customer may claim this offer once", the resource is the pair**, and both ids belong
in the key — locking the offer alone serialises every customer against each other for no
reason, and locking the customer alone lets one customer claim two offers concurrently.

```csharp
// IConcurrencyHandler, IRepositoryWrapper, IMapper and IMediator are constructor-injected.

public async Task<RedemptionResponse> RedeemAsync(
    Guid customerId, Guid offerId, CancellationToken cancellationToken = default)
{
    Guid redemptionId = await concurrencyHandler.LockedAsync(
        $"OfferRedeem:{offerId}:{customerId}",
        async () =>
        {
            Offer offer = await repositoryWrapper.Repository<Offer>()
                .Find(x => x.Id == offerId)
                .Include(x => x.Redemptions)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false)
                ?? throw new BadRequestException(Messages<Offer>.NotFound());

            DateTimeOffset now = DateTimeOffset.UtcNow;
            ThrowIfCannotRedeem(offer, customerId, now);

            int availableCredits = await mediator
                .Send(new GetAvailableCreditsQuery(customerId), cancellationToken)
                .ConfigureAwait(false);

            if (availableCredits < offer.CreditsRequired)
            {
                throw new BadRequestException(
                    Messages<Offer>.NotEnoughLength(nameof(Offer.CreditsRequired)));
            }

            await repositoryWrapper.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await mediator
                    .Send(new DeductCreditsCommand(customerId, offer.CreditsRequired), cancellationToken)
                    .ConfigureAwait(false);

                Redemption redemption = await CreateRedemptionAsync(offer, customerId, now, cancellationToken)
                    .ConfigureAwait(false);

                await mediator
                    .Publish(new RedemptionSucceededEvent(redemption.Id, offer.CreditsRequired), cancellationToken)
                    .ConfigureAwait(false);

                await repositoryWrapper.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);

                return redemption.Id;
            }
            catch (Exception ex) when (ex is not BadRequestException)
            {
                await repositoryWrapper.RollbackTransactionAsync(cancellationToken).ConfigureAwait(false);
                Log.Error(Messages.LogErrorTemplate, nameof(RedeemAsync), ex);
                throw new InternalServerException(Messages<Offer>.Action("Redeem"));
            }
        },
        new ConcurrencyHandlerOptions
        {
            Provider = ConcurrencyProvider.RedLock,
        },
        cancellationToken).ConfigureAwait(false);

    return await repositoryWrapper.Repository<Redemption>()
        .Find(x => x.Id == redemptionId, isAsNoTracking: true)
        .ProjectTo<RedemptionResponse>(mapper.ConfigurationProvider)
        .FirstOrDefaultAsync(cancellationToken)
        .ConfigureAwait(false)
        ?? throw new BadRequestException(Messages<Redemption>.NotFound());
}
```

Why this shape holds:

- **The entity is re-read inside the delegate, not passed in from outside.** This is the
  whole point and the easiest thing to get wrong. State loaded before the lock is a
  snapshot from before the previous holder's write; deciding on it inside the lock is the
  original race with extra steps. If a value must be read before the lock to *choose* the
  key — as in Pattern 2 — read it again inside for the decision.
- **The delegate returns an id, not a response.** The projection that builds the response
  runs after the lock is released. It is a pure read of a row that is now final, so holding
  the lock through a `ProjectTo` and a second query only lengthens every other caller's
  wait.
- **The transaction opens and closes inside the delegate.** See *Lock outside, transaction
  inside* below — this ordering is doctrine, not style.
- **The rollback happens before the exception leaves the delegate**, and therefore before
  the lock is released. A rollback that ran after release would let the next holder read
  rows that are about to disappear.
- **The catch filter passes `BadRequestException` through untouched.** A rule violation is
  the caller's answer and must not be relabelled as a server fault. Everything else is
  logged and wrapped. This filter needs no `LockedException` exclusion — it sits *inside*
  the delegate, which only runs once acquisition has already succeeded. Pattern 3's filter
  wraps the `LockedAsync` call itself, and that one does need it.
- **The success event is published inside the transaction, before commit — that is the
  canonical ordering, and it constrains what a handler may do.** Handlers that write
  through the same repository join the open transaction and are rolled back with it;
  anything a handler does outside the database — mail, push, an external call — has
  already happened and no rollback reaches it. Publish pre-commit only for handlers whose
  effects are database writes. Compare Pattern 3, where the event is published after the
  lock because there is no transaction to join.

---

## Pattern 2 — multi-key guard

One operation touches several resources, each of which some *other* operation guards on
its own. Locking only the primary resource leaves the attached ones unprotected — a
concurrent operation holding an attached resource's key proceeds happily.

Which keys are needed is data-dependent, so the key list is built first, from a cheap
projection that reads nothing but the attached ids.

```csharp
// IConcurrencyHandler and IOptions<ConcurrencySettings>.Value are constructor-injected.

public async Task<PaymentResponse> PayAsync(Guid orderId, PaymentRequest request)
{
    var attached = await repositoryWrapper.Repository<Order>()
        .Find(x => x.Id == orderId, isAsNoTracking: true)
        .Select(x => new
        {
            x.RedemptionId,
            x.CreditGrantId,
            x.AdjustmentId,
        })
        .FirstOrDefaultAsync();

    if (attached is null)
    {
        return await DoPayAsync(orderId, request);
    }

    List<string> keys = new()
    {
        OrderPayLock(orderId),
    };

    if (attached.RedemptionId is Guid redemptionId)
    {
        keys.Add(RedemptionPayLock(redemptionId));
    }

    if (attached.CreditGrantId is Guid creditGrantId)
    {
        keys.Add(CreditGrantPayLock(creditGrantId));
    }

    if (attached.AdjustmentId is Guid adjustmentId)
    {
        keys.Add(AdjustmentPayLock(adjustmentId));
    }

    return await concurrencyHandler.LockedAsync(
        keys,
        () => DoPayAsync(orderId, request),
        new()
        {
            Provider = ConcurrencyProvider.RedLock,
            WaitTime = TimeSpan.FromSeconds(concurrencySettings.WaitTime),
        });
}

private static string OrderPayLock(Guid orderId)
    => $"OrderPayment:{orderId}";

private static string RedemptionPayLock(Guid redemptionId)
    => $"RedemptionPayment:{redemptionId}";

private static string CreditGrantPayLock(Guid creditGrantId)
    => $"CreditGrantPayment:{creditGrantId}";

private static string AdjustmentPayLock(Guid adjustmentId)
    => $"AdjustmentPayment:{adjustmentId}";
```

*(This canonical path threads no `CancellationToken`, and it is reproduced as it stands.
Nothing about the lock prevents one — thread it through in new code.)*

Why this shape holds:

- **One `LockedAsync` call with a list, never two nested calls.** The handler sorts the
  keys before acquiring, so every caller in the system takes them in one global order and
  no pair can deadlock. Nesting two calls acquires in whatever order each call site
  happened to write — under the in-memory provider that deadlocks permanently, because
  nothing there times out; under the distributed provider both callers burn their full
  `WaitTime` and answer `423`. Neither is acceptable and only one of them is even visible.
- **The pre-read projects ids only, `isAsNoTracking`, and decides nothing.** It selects the
  key list. Every value the payment logic actually depends on is read again inside
  `DoPayAsync`, under the lock.
- **The pre-read is allowed to be stale, and you must decide whether yours may be.** If an
  attachment appears between the projection and the acquisition, this call locks a set that
  no longer matches the row. The canonical accepts that window because attaching and
  settling do not race in its flow. If yours can, either compute the key set inside a
  single-key lock on the primary resource, or re-verify the set inside the delegate.
- **The no-attachments path skips the lock entirely and calls the same method.** One
  implementation, two entry conditions — the guarded path adds a lock, it does not add
  behaviour. When the row does not exist at all there is nothing to serialise, and
  `DoPayAsync` still produces the not-found answer.
- **Key helpers are `private static` on the consuming service.** Locks that only this
  service takes belong to this service. They are private for the same reason there is no
  central factory: a second call site importing them would be silently signing up to an
  invariant it has not read.
- **The keys name the operation, not just the entity.** `OrderPayment:{id}` and
  `RedemptionPayment:{id}` guard payment specifically; a bare `Order:{id}` would collide
  with any other lock anyone ever takes on an order and serialise unrelated work.
- **This is the one sanctioned read of `ConcurrencySettings`.** `WaitTime` is an `int` in
  seconds, converted here with `TimeSpan.FromSeconds(...)`. It is read *here* because a
  payment path is where an operator plausibly needs to retune the wait under load.
  Everywhere else the default is the answer; a settings read at every call site is noise
  that hides the one place it matters.

---

## Pattern 3 — guard plus compensation

The guarded work writes somewhere the database transaction cannot reach — a search index,
an external system, a second store. A rollback undoes the rows and leaves the rest
standing, so the undo has to be written by hand.

```csharp
CompletionRecord record = mapper.Map<CompletionRecord>(request);
PointBatch? pointBatch = null;

try
{
    await concurrencyHandler.LockedAsync(
        SessionCompletionLock(request.WorkSessionId),
        async () =>
        {
            await ThrowIfAlreadyCompletedAsync(request.WorkSessionId);
            DateTimeOffset confirmedAt = DateTimeOffset.UtcNow;

            WorkSession session = await repositoryWrapper.Repository<WorkSession>()
                .Find(x => x.Id == request.WorkSessionId, isAsNoTracking: true)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false)
                ?? throw new BadRequestException(Messages<WorkSession>.NotFound());

            pointBatch = await pointService
                .CreateBatchAsync(
                    new CreatePointBatchParameters
                    {
                        CustomerId = customerId,
                        RecordId = record.Id,
                        ExpiryUnit = session.PointExpiryUnit,
                        ExpiryValue = session.PointExpiryValue,
                        Points = session.Points,
                        ConfirmedAt = confirmedAt,
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            record.Points = pointBatch?.Points ?? 0;

            return await searchWrapper.Repository<ElkCompletionRecord>()
                .AddAsync(record, cancellationToken);
        },
        new() { Provider = ConcurrencyProvider.RedLock },
        cancellationToken).ConfigureAwait(false);

    await mediator
        .Publish(new CompletionRecordCreatedEvent(record.Id, request.ResourceId), cancellationToken)
        .ConfigureAwait(false);
}
catch (Exception ex) when (ex is not BadRequestException and not LockedException)
{
    if (pointBatch is not null)
    {
        await repositoryWrapper.Repository<PointBatch>()
            .DeleteAsync(pointBatch, cancellationToken)
            .ConfigureAwait(false);
    }

    await DeleteIfExistAsync(record, cancellationToken);

    await mediator
        .Publish(new CompletionRecordFailedEvent(record.Id, request.ResourceId), cancellationToken)
        .ConfigureAwait(false);

    LogExtension.Error(nameof(CreateAsync), ex, nameof(CompletionRecordService));
    throw new InternalServerException(Messages<ElkCompletionRecord>.Create(false));
}

return record;

// …

private static string SessionCompletionLock(Guid workSessionId)
    => $"SessionCompletion:{workSessionId}";
```

*Normalized:* **the catch filter excludes `LockedException` as well as
`BadRequestException`.** The canonical filter excludes only `BadRequestException`, so a
rejected acquisition — work that never started — runs the entire compensation and is
rethrown as a server fault, turning a retryable `423` into a `500`. This is the corrected
form; see *Do not catch `LockedException` in a module*.

Why this shape holds:

- **The duplicate check and the write are both inside the lock.** `ThrowIfAlreadyCompletedAsync`
  is the entire reason the lock exists: it is a read whose answer must still be true when
  the write lands three statements later. Outside the lock it is decoration.
- **The compensation is outside, deliberately.** By the time it runs the lock is gone.
  Compensation touches only records this operation created, which no one else can reach,
  and it may be slow; holding the key through it would make every waiting caller pay for
  this caller's failure.
- **`pointBatch` is a compensation handle, not a result.** It is assigned inside the
  delegate and read outside — normally the exact bug a lock exists to prevent, since a
  value read after release may be stale. It is sound *only* because it is read on the
  failure path, only to undo something this call created, and never to decide anything
  about shared state. **Do not widen this into "capture the result in a local instead of
  returning it."** When you want the outcome, return it from the delegate, as Patterns 1
  and 2 do — here the delegate returns the index write's result, which satisfies
  `Task<TResult>` without inventing a placeholder.
- **The success event is published outside the lock.** Handlers may be slow, may call out
  of process, and do not participate in the invariant. Compare Pattern 1, where the event
  is inside because it is inside a transaction whose rollback the handler's database
  writes would share.
- **The key uses a `private static` helper**, as Pattern 2 does — the corrected form of the
  key convention the body describes.

---

## What belongs inside the delegate — the interleaving test

For each statement, ask: **if a second caller executed this same statement between mine and
my next one, would my outcome still be correct?** Statements that fail go inside the lock.
Everything else stays outside, because every statement inside is time every other caller
spends waiting.

| Inside | Outside |
|---|---|
| Re-reading the state the decision depends on | Request validation and mapping the request |
| The duplicate / eligibility / balance check | Projecting the response |
| The writes those checks authorise | Publishing events that are not inside a transaction |
| Begin / commit / rollback of the transaction covering those writes | Compensating for a failure |
| — | Any call to an external system that does not touch the guarded state |

Two failure modes this test catches:

- **Reading before, deciding inside.** The read is outside, the `if` is inside, and it
  looks locked. The value was captured before the previous holder committed.
- **A lock that spans an HTTP call or a report.** Correct, and it converts a resource
  invariant into a throughput ceiling — every caller for that key now waits on someone
  else's network. If the external call must be inside, `ExpiryTime` must exceed its
  worst-case timeout, and you should ask whether the operation wants a queue instead.

## Lock outside, transaction inside

**Acquire the lock first, open the transaction inside the delegate, commit or roll back
before the delegate returns.** Never the reverse.

A transaction opened before acquisition holds a database connection and its row locks for
the entire wait — up to the full `WaitTime` — for *every* caller queued on that key. Under
contention that turns lock queueing into connection-pool pressure, and the outage presents
as a database problem rather than a lock-contention one. Worse, two call sites that take
the database lock and the distributed lock in opposite orders deadlock across two systems,
where neither one's deadlock detector can see the cycle.

Committing before release matters just as much: the next holder acquires the moment the
delegate returns, and if the commit had not landed it would read state the previous holder
is still about to write.

## Do not catch `LockedException` in a module

A failed acquisition is already the correct answer: the caller is told the resource is busy
and can retry. **The retry has already happened** — `WaitTime` and `RetryTime` mean the
handler polled for the lock throughout the wait window before giving up. Catching it to
retry stacks a second wait window on an exhausted first one; catching it to return success
reports work that never ran.

Let it propagate. The middleware maps it to `423`.

The one legitimate local move is **excluding it from a catch filter you already have**. A
compensating `catch` that wraps the `LockedAsync` call and does not exclude it runs the
undo for work that never started, and — because it rethrows as a server fault — turns a
retryable `423` into a `500`:

```csharp
catch (Exception ex) when (ex is not BadRequestException and not LockedException)
{
    // compensate, log, rethrow as a server fault
}
```

A filter *inside* the delegate needs no such exclusion: acquisition already succeeded
before the delegate ran, so `LockedException` cannot arrive there.

The rule generalises: **any handler that converts an exception into a different status must
exclude the exceptions that already carry the right one.**
