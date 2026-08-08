# Field report — the search-field contract, and why references get skipped

**Source.** A consumer repository running dotnet-standards 0.3.68, with the full
`QueryExpressionExtension` + `PaginationExtension` pipeline and
`NotSearchableAttribute` in place. The consumer session's own write-up is
`docs/code-review/2026-08-07-plugin-feedback-search-fields.md` in that
repository; this file is the plugin-side record.

The report does not ask for a convention to change. Every convention involved
was already correct and already written down. It reports a *gap between a correct
convention and the code that shipped anyway*, and then reports — from the inside
— why the convention did not reach the author.

## Part 1 — the defect, and what shipped

Three call sites passed a field array written at the call site as
`ApplySearch`'s second argument instead of `request.SearchFields`.

**Corpus check run before acting.** Across the four projects under
`reference/projects/`, excluding the duplicate worktree checkouts: 96
`ApplySearch(` occurrences, of which the only ones not passing a request's own
`SearchFields` are (a) `QueryExpressionExtension.cs` itself, where the
`IEnumerable` overload forwards its parameter to the `IQueryable` one, and (b)
one unit test of the extension, which supplies a literal set on purpose. In
production code the convention is universal — zero counter-examples in four
projects. That is what makes 1.12's grep near-zero-false-positive, and it is why
the check is scoped to `src/`.

**What no review layer caught.** The consumer ran 9 per-task reviews plus 4
whole-branch lenses — the breadth lens on the strongest available model — over
the three wrong call sites. None reported anything. Several quoted the
hard-coded array back as a valid description of the endpoint's behaviour. The
reason is structural, not a model failure: **no rubric check existed**, so there
was nothing to fail.

Shipped in 0.3.70:

- `dotnet-code-review` rubric **1.12**, with the grep and the two supported
  narrowings named so the finding carries its own fix.
- The rule lifted into `ef-core-data-access`'s **body**, not left in
  `references/query-conventions.md` alone.
- `list-query-pipeline`'s decision-guide row split, because it said *never* about
  something that is not a hard stop — see below.

## The `[NotSearchable]` overclaim

`references/property-info-extension.md` states it correctly: the attribute is
consulted only where `ApplySearch` derives the field set itself
(`searchFields ??= …GetPropertyRecursiveWithMaxDeep(1, typeof(JsonIgnoreAttribute), typeof(NotSearchableAttribute))`).
A caller that names the property explicitly in `SearchFields` still reaches it.

The decision-guide row in `list-query-pipeline/SKILL.md` nonetheless read *"must
**never** be swept by free-text search → `[NotSearchable]`"*. In the consumer
repository that reading came close to closing a credential-probing concern that
was in fact still open. A body that is right and a lookup table that overclaims
is worse than either alone: the table is what gets read under time pressure.

Fixed by splitting the row in two — the derived-set case keeps the attribute, and
the *must never be reachable* case routes to a decision in the owning gate's
service.

## Part 2 — why the reference was not opened

The consumer session's own account of the miss, recorded here because it is
first-person data on a failure mode this plugin has otherwise only guessed at.
Four conditions, and they compound:

1. **A concrete example was already in context.** The strongest of the four. The
   reference went unopened because a sibling service's `SearchAsync` was already
   open. Nothing felt missing, so nothing was fetched.
2. **The trigger sentence sits at the end of a long section.** By the time it is
   reached, the summary has been absorbed and attention has moved on.
3. **The need arose mid-writing, not at task start.** `SKILL.md` loads at minute
   zero; the `ApplySearch` question arrives at minute ten, inside the flow of
   writing code.
4. **The body's summary was good enough to feel sufficient.** A good summary
   produces the feeling of having understood — exactly when the reference
   demotes itself to *further reading*.

**The common thread:** references get opened on felt uncertainty, and the failure
mode is *false certainty* — confident and wrong, because a precedent was at hand.

### Directions proposed (not decided)

Recorded verbatim in substance; none is a ruling.

- **A — trigger on the token about to be typed, not on a felt category.**
  *"Before typing `.ApplySearch(`, open this file"* is checkable; *"when working
  on search"* requires a self-assessment that cannot be checked.
- **B — invert body and reference.** Canonical shape in the body, rationale and
  variants in `references/`. Then a skipped reference costs the *why*, never the
  *how*. (0.3.70 applies this to exactly one rule, as a probe.)
- **C — name repository precedent as an adversary.** *Precedent in the repo is
  evidence about the repo's history, not evidence about the convention.* Skills
  implicitly assume the reader takes conventions from the skill; the reader
  actually takes them from the nearest open file.
- **D — a convention with no check is documentation.** Generalizes Part 1: build
  a convention ↔ rubric-check coverage matrix, and treat every syntactically
  detectable convention with no owning check as plugin debt.
- **E — a reference routing table at the top of each `SKILL.md`**, the way
  `choosing-a-dotnet-skill` routes between skills. *You are about to do X → open
  Y*, seen before the work rather than after the summary.
- **F — plans must paste the shape, not name the file.** `dotnet-feature-flow`
  already requires plans to name `references/` files; a step reading *"add the
  search endpoint (ef-core-data-access)"* hides the omission, whereas a step that
  should hold ten canonical lines and is empty shows it at GATE 1.
- **G — make copying self-declare.** Extend the conformance sweep with a step
  requiring the implementer to list every structure copied from an existing file
  and the skill clause permitting it.
- **H — measure it.** Per skill: give an agent only `SKILL.md`, hide
  `references/`, place it in a repo carrying a wrong precedent, and check whether
  the canonical shape still comes out. A skill that fails is one storing
  load-bearing information in the wrong place — which turns *what belongs in the
  body* from an argument into a measurement.
