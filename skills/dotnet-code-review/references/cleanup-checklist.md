# Cleanup checklist — the slop taxonomy and the safe-delete checks

Priority row 7 of `review-rubric.md`. This file collects **candidates** and
nothing else: the review lists what is safe to remove and why, and `/simplify`
executes with its own verification. A review never deletes.

**Conventions are `review-rubric.md`'s.** The tool is
`grep -rn --include=*.cs "<pattern>" src/`, severities are the CRITICAL / HIGH /
MEDIUM / INFO ladder, and the scope rule stated there — the default scope is the
change; pre-existing findings are INFO at most — holds here unchanged. Where an
entry overlaps a rubric check it is cross-referenced by number **and** name, and
not restated. Every entry either cites a shipped skill or is a defect in any C#
codebase.

**The compiler's own output counts as manual inspection.** Building the solution
and reading its diagnostics is reading a file the toolchain wrote; the rule the
body states bans assuming an analysis *server*, not reading compiler output. Two
categories below rely on it, and they are the two where grep genuinely cannot
answer the question.

Slop is reviewed **last**, after the six areas. Nothing in this file may be
raised above a finding from any of them.

## The slop taxonomy

Five categories. Anything not on this list is either a real finding for one of
the six areas or a preference — and preferences belong to the formatter and the
analyzers.

**1. Unused usings** — *INFO* · universal
`Find:` build the solution and read the `IDE0005` diagnostics; the compiler is
the only reliable detector. `grep -rn --include=*.cs "^using " src/` enumerates
candidates but proves nothing on its own.
**Qualifies:** a `using` no symbol in the file resolves through.
**Does not qualify:** a global using, or one made redundant by an implicit-usings
setting — that is a project-file question, not a per-file edit; a `using` only a
source-generated or conditionally-compiled member needs; a `using` inside an `#if`
branch, which must be read first; an alias directive that documents intent.
Lowest-value category by a wide margin. Report it as a count, never line by line.

**2. New analyzer warnings** — *MEDIUM; the nullability carve-out below is HIGH
and leaves this file* · universal
`Find:` build before and after the change and diff the two warning lists. Then
`grep -rn --include=*.cs "#pragma warning disable\|SuppressMessage" src/` for the
other shape — a warning silenced rather than fixed.
**Qualifies:** any warning the change introduced, and any suppression added
without a comment saying why the analyzer is wrong.
**Does not qualify:** the solution's pre-existing warning count. Reporting it
buries the change's own warnings, which is the failure mode this whole rubric
exists to prevent.
Triage by kind, compiler warnings first: an obsolete-API warning is a migration;
an IDE suggestion is optional and often noise.
**The nullability carve-out.** A `CS86xx` warning is the compiler saying a
reference it cannot prove non-null is being dereferenced. That is a
`NullReferenceException` with a date on it, not a tidiness issue. **Report it as
`review-rubric.md` 5.13 *(A nullability warning the change introduced)* at HIGH,
under correctness — not here as slop.**

**3. Dead code** — *MEDIUM* · universal
`Find:` for each type, member or branch the change touched or orphaned, run the
**safe-delete checks** below. Never propose a removal from a grep alone.
**Qualifies:** code nothing reaches, once all four safe-delete checks pass.
**Does not qualify:** anything failing even one of them. A symbol with one
compile-time reference is a *candidate*, not a conclusion — and a class
registered by an assembly scan is not dead because nothing constructs it.

Four shapes recur, and each is easier to recognise than to find by counting
references:

| Shape | How it reads | Cross-reference |
|---|---|---|
| An unread member on an exception type | A property the type declares and its constructors populate, that nothing downstream consults. Worse than unused: the next author reads it as a working channel and builds on it — which is how the shape spreads. | `review-rubric.md` 5.3 *(A member that nothing reads)* — owner `error-handling` |
| An `Include` chain in front of a projecting read | Compiles, runs, and changes nothing about the generated SQL. Dead code standing directly in front of the real path, documenting an intention the code does not have. | `review-rubric.md` 1.3 *(An `Include` chain in front of a projecting read)* — owner `ef-core-data-access` |
| A branch behind guards that already excluded it | A null check after a guard clause that throws on null; an arm for a state a validator rejects before the method runs. Not merely unreachable — it tells the next reader the case is possible. | universal |
| A marker whose work already shipped | A `TODO` describing the very thing the diff under review just implemented. Dead code's twin: a comment describing a state of the world that ended. | category 4 below |

**4. Stale TODO / HACK / FIXME** — *INFO; MEDIUM when the marker actively
misdescribes current behaviour* · universal
`Find:` `grep -rn --include=*.cs "TODO\|HACK\|FIXME\|XXX" src/`
**Qualifies:** a marker whose work is done, whose premise is gone, or that names
no owner, no issue and no condition for its own removal.
**Does not qualify:** a marker carrying a tracked issue reference — that is a
working index entry, and removing it loses the link. Nor a marker in code the
change did not touch, unless the change directly contradicts it.
**Why the MEDIUM case:** a `HACK` describing a workaround for a condition that no
longer exists does not merely clutter — it actively teaches the next reader that
the code is compensating for something, and they will preserve the compensation.
A marker that outlives its work makes the markers that *do* matter unreadable by
volume.
Each stale marker gets one of three dispositions in the candidate list: **fix
now** (small and self-contained), **file it** (real work — it needs an issue
reference in the comment), or **delete** (the work already happened). A marker
carried forward untouched for a third review is documentation of neglect, not a
plan.

**5. Dropped `CancellationToken`** — *HIGH* · `ef-core-data-access` + universal
`Find and rule:` see `review-rubric.md` 3.1 *(A dropped `CancellationToken`)*,
which owns this entirely — the three sweeps, the HIGH default, and the narrow
escalation to CRITICAL when the un-cancelled work can corrupt or expose.
**Why it is listed here at all:** it is the one item that is both a concurrency
finding and a cleanup candidate, because propagating a token through a chain is
mechanical work `/simplify` can carry, usually across many files at once.
**The severity does not drop because the work is batched.** Report it under
concurrency at its real severity and mention it here only as work to hand off; if
the cleanup pass is deferred, this row is extracted and reported as a rubric
finding in its own right.

**Not in this taxonomy: sealing classes that nothing inherits.** It is not a
house convention, it is not enforced anywhere in the shipped skills, and a review
that raises it is importing an outside opinion. Do not add it.

## The safe-delete checks

Grep sees compile-time references. A symbol can be used by name, by convention or
by configuration and appear in none of them — so **all four checks must come back
clean before any deletion is proposed.** A symbol that fails one is not dead: it
is used by a mechanism the compiler cannot see, and deleting it produces a
runtime failure with no compile-time warning.

**(a) Compile-time references.**
`grep -rn --include=*.cs "<SymbolName>" src/ tests/`
One hit means only the declaration exists. Search the test projects too — a
symbol kept alive only by its own tests is a separate conversation, not a clean
pass.

**(b) String-based and convention-based use.** Four sub-checks, all required:

- **`nameof` and string literals** —
  `grep -rn --include=*.cs "nameof(<SymbolName>)\|\"<SymbolName>\"" src/`.
  Attributes, policy names, log templates and message keys all name types and
  properties as text.
- **Reflection** —
  `grep -rn --include=*.cs "Type.GetType\|Activator.CreateInstance\|GetMethod(\|GetProperty(" src/`.
  If any of these exist near the area, the symbol may be resolved by name at
  runtime.
- **Convention-based registration.** This is the one that catches people.
  Services register by an assembly scan over a **lifetime marker interface**, so
  a class whose interface carries the scoped or transient marker is registered
  and resolved **even though nothing in the solution ever constructs it or names
  its concrete type**. Zero compile-time references is the *expected* state for a
  correctly written service. Entity configurations and mapping profiles are found
  the same way — by assembly scan, with no registration step and no reference
  anywhere — as are contributor sets discovered by their interface, such as seed
  contributors. Read the type's base list and check the composition root for a
  scan-based registration before concluding anything:
  `grep -rn --include=*.cs "AddClasses(\|FromAssembly\|ApplyConfigurationsFromAssembly\|AddScoped(typeof" src/`.
  Owner: `facade-module-architecture`.
- **Serialization and model binding** —
  `grep -rn --include=*.cs "JsonDerivedType\|JsonPolymorphic\|JsonConverter\|JsonPropertyName\|\[ModelBinder" src/`.
  A polymorphic payload names its subtypes in an attribute on the **base**, not
  at any call site.

**(c) Configuration files.**
`grep -rn --include=*.json "<SymbolName>" src/`
Settings classes bind by section name, and per-capability configuration files
name types and sections in JSON — a bound settings class whose section exists is
live regardless of how it looks in C#. Check environment-specific overlays too: a
type used only by a production overlay looks dead in a development checkout.

**(d) Public API consumed outside the solution.**
A symbol that is public on an assembly another repository references, part of the
HTTP contract, or consumed by a generated client cannot be evaluated from inside
this solution. **Stop** — removing it is a breaking change that ships with its
consumers, not with a cleanup pass.

**The ruling.** Propose removal only when all four pass, and record which ones
you ran. If any is inconclusive — a dynamic string that *might* be this symbol, a
scan whose reach you could not confirm — the answer is "keep, and say why", not
"probably fine". **Execution is `/simplify`'s, with its own verification;** a
cleanup that breaks something is worse than the mess it was fixing.

## Reporting the candidates

Candidates go under the `Cleanup candidates` heading in the report shape the body
defines — unranked, and separate from the severity-ranked findings. Mixing them is
how a CRITICAL ends up two screens below a list of unused usings. If there are
none, the section says `None.` like every other.

Group by category, and give a count and a file list rather than one line per
occurrence: name the top few candidates and summarize the rest. A hundred-line
cleanup list is not read, so it is not a finding.

Mark every dead-code candidate with its safe-delete result — `4/4 clear`, or the
specific check that blocked it. **A candidate with no recorded safe-delete result
is not a candidate, it is a guess** — and the tool that executes it will not
repeat the checks the reviewer skipped.

## What this file does not do

It is a checklist, not a pipeline. There is **no ordered step sequence, no
commit-per-step protocol, no build-and-test loop between phases, and no cleanup
agent** here. That machinery belongs to an execution tool, and execution is owned
by `/simplify` and by the Superpowers review process, which carry their own
verification. Importing a pipeline into a rubric would create a second, competing
cleanup workflow that nobody asked for and nothing keeps in sync.
