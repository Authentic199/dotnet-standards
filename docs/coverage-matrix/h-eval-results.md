# Direction H, first run — does the text beat the precedent?

**Run:** 2026-08-08, against v0.3.74.
**Question:** when a repository contains a plausible-but-wrong precedent, does a
skill's text still produce the canonical shape — and does it depend on which
version of the text, and on which model?

**11 agent runs, one task, graded by script.** No judgement in the grading: six
grep assertions, pass/fail.

## The fixture

A minimal .NET tree in the plugin's own anonymous vocabulary. Two deliberately
seeded **non-conforming controllers that look modern** — file-scoped namespace,
expression-bodied actions, `{orderId:guid}` constraints, complete
`ProducesResponseType`, XML summaries — while each declares its own `[Route]`,
carries a two-module name, and injects a foreign module's service. Two of them,
so the wrong shape reads as *the house pattern*.

The correct envelope (`SearchInvoicesByOrderQuery`, `internal sealed`) and the
foreign service method already exist, so the right answer requires no invention.
**No `mediator.Send` example exists anywhere in the tree** — the only visible
precedent is the wrong one.

Task: *add `GET /api/Orders/{id}/Invoices`*, framed as "task 3 of 6, moving
quickly" rather than "explore the repository first".

Assertions: no new `[Route]` · the endpoint is a partial of the parent's
controller · no foreign service in the controller constructor · the parent's
service `Send`s the envelope · no second envelope invented · no direct foreign
call from the controller.

## Wave 1 was thrown away, and that is the pilot working

The first fixture kept a **correct sibling** (`OrdersController.Shipments.cs` +
`OrderService.Shipments.cs`) and the prompt said *"explore the repository
first"*. All four configurations passed — **including the control with no
doctrine at all**. Per the acceptance rule the field report shipped with these
cases, *a case that passes before the fix does not reproduce the defect and is
rewritten, never counted as good news.* Wave 1 was rewritten, not reported.

## Results

| Model | Doctrine | Verdict |
|---|---|---|
| Opus 5 | none (control) | **FAIL** |
| Opus 5 | v0.3.72 — before the 0.3.73 fixes | PASS |
| Opus 5 | v0.3.74 — current | PASS |
| Sonnet 5 | v0.3.74 — current | PASS |
| Sonnet 5 | v0.3.74 + a 4-question gate at the top of `api-surface` | PASS |
| Haiku 4.5 | none (control) | **FAIL** |
| Haiku 4.5 | v0.3.72 | **FAIL** |
| Haiku 4.5 | v0.3.74 | **FAIL** |
| Haiku 4.5 | v0.3.74 + the gate (×2 runs) | **FAIL, FAIL** |

Every failure is the same file: `OrderInvoicesController.cs`, its own
`[Route("api/Orders/{orderId:guid}/Invoices")]`, `IInvoiceService` injected,
service called directly. The agents said so themselves — *"mirrors
`OrderPaymentsController` and `OrderRefundsController` exactly"*.

## What this establishes

**1. Doctrine works, and the effect is real.** At Opus, the control fails and
both doctrine versions pass. With no conventions file, a strong model copies the
wrong neighbour and describes the copy as consistency.

**2. The 0.3.73 text changes are still unverified.** v0.3.72 — with the
*"two or three is normal"* contradiction intact and the section still called
*Pre-convention files* — passed exactly like v0.3.74. One Opus run even called
the seeded controllers *"pre-convention"*, using the old section name, and
refused them anyway. **The fixes may be right; this eval cannot say so.**

**3. Below a model threshold, the text does not participate at all.** Five Haiku
runs, three doctrine variants, one identical wrong answer. 781 lines of
conventions lost to one neighbouring file, every time.

**4. Prose could not move that threshold.** A four-question stop-gate — countable,
imperative, at the very top of the file, each question ending in **stop** — was
written specifically against this failure. Haiku failed with it, twice. Sonnet
passed without it. **It was reverted**, unshipped: keeping text that failed its
only test is the "documentation, not enforcement" pattern this plugin exists to
argue against. *H's first payoff was a deletion.*

**5. So the enforcement is the reviewer, not the author.** `dotnet-architecture-review`
check **3.6**, shipped hours earlier at 0.3.74, catches every one of these five
Haiku outputs with a one-line grep and no comprehension required. For a
weak-model session the review layer is not a safety net behind the knowledge
layer — **it is the only control that fires.**

## What this does not establish

- **The eval does not reproduce the field failure's cause.** The incident happened
  at a strong model *with the v0.3.72 text loaded*, and here that same text
  passes. The difference is attention budget across a six-task session, not
  information. A one-task eval cannot manufacture that, and no fixture change
  will fix it.
- **One task, one skill pair.** `api-surface` + `module-feature` only. Seven other
  skills have greppable canonical shapes and are untested.
- **One or two runs per cell.** Enough to see a threshold, not enough for a rate.
  The Haiku cells are the exception: five runs, five identical failures.
- **Assertion A5 was wrong in wave 1** and was rewritten between waves — it
  counted envelope *files* rather than envelope *records*, so it flagged a legal
  handler placement as a second envelope.

## Reproducing

Fixture, doctrine bundles, run directories and graders live in the session
scratchpad, not in this repository — they are throwaway. What is worth keeping is
the recipe: two non-conforming precedents that look modern, no correct sibling,
the right envelope already present, a "task 3 of 6" framing, and six grep
assertions. Rebuild from that description; it took under an hour.
