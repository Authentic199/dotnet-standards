# Convention ↔ check coverage — direction D, first run

**Run:** 2026-08-08, against v0.3.73.
**Claim under test:** *a convention that lives only in a knowledge skill, with no
corresponding rubric check, has nothing enforcing it* — stated in the 2026-08-07
field report after nine per-task reviews and four whole-branch lenses read three
wrong call sites and reported nothing, because no check existed to fail.

## Method

Static. No agent runs. Three scripts in this folder, re-runnable:

| Script | What it does |
|---|---|
| `d_inventory.py` | Extracts every numbered check from the four review skills and their reference files, in both shipped forms — bold `**n.n Title**` and table `\| n.n \| **Title.**`. Then collects every token appearing inside a `Find:` instruction. |
| `d_gaps.py` | Walks the 18 knowledge skills, drops code blocks, keeps paragraphs carrying a normative marker (*never, always, must, do not, is a defect, no other, exactly one…*) **and** at least one backticked code-shaped token, and reports those whose tokens appear in **no** `Find:` instruction. |
| `d_rank.py` | Narrows to the house's bold-imperative rule form. |

**The join is on tokens, not on prose.** A rule is a candidate gap when nothing a
rubric tells a reviewer to grep for overlaps the tokens the rule names. That is a
coarse instrument in both directions, and the numbers below should be read as a
worklist, not a score.

## Inventory

**195 numbered checks**, more than double the ~90 assumed when D was queued:

| Skill | Body | Reference file |
|---|---:|---:|
| `dotnet-code-review` | — | 73 (`review-rubric.md`) |
| `dotnet-security-review` | 29 | 17 (`security-checks.md`) |
| `dotnet-performance-review` | 24 | 27 (`performance-checks.md`) |
| `dotnet-architecture-review` | 2 + 23 in tables | 23 (`conformance-checks.md`) |

**354 distinct tokens** appear inside `Find:` instructions.

## Result

- **196** candidate uncovered rules across the 18 knowledge skills.
- **111** survive the bold-imperative filter.
- **6** were triaged as genuine, greppable, uncovered, and worth a check. They
  shipped in 0.3.74.
- The rest were read and rejected — see *What was rejected*.

Candidates concentrated in `dotnet-testing` (20), `file-storage` (19),
`common-extensions` (17), `api-surface` (16), `module-feature` (16). Density
tracks how much of a skill is *procedure* rather than *rule*: a skill that
mostly describes how to build something produces normative sentences that no
grep could ever check.

## What shipped (0.3.74)

Every grep was smoke-tested against two real projects before shipping — the rule
since 0.3.58 — and two of the six changed as a result.

| Check | Rule it enforces | Smoke test |
|---|---|---|
| `dotnet-architecture-review` **3.6** — a `[Route]` on any controller but the base | `api-surface`, *Routes* | canonical project **0**; the consumer project **2**, both real, one of which no report had named |
| `dotnet-code-review` **5.24** — a `CancellationToken` defaulted on an action | `api-surface` | canonical **3** real actions; consumer **0** |
| **5.25** — an action parameter with no binding source | `api-surface` | no grep possible — an *absent* attribute; written as a read, bounded by the diff |
| **5.26** — an action returning `IActionResult` or a bare type | `api-surface` | canonical **1** — a redirect endpoint |
| **5.27** — a regex built at a call site | `common-extensions` | canonical **3** after the fix below; consumer **0** |
| **6.9** — more than one `WebApplicationFactory` in a test assembly | `dotnet-testing` | consumer **1**, correct |

**Two things the smoke test caught, and neither was visible from the text:**

1. **5.26's only canonical hit is legitimate** — a redirect endpoint, which the
   envelope was never built to carry. `api-surface` states *never `IActionResult`*
   with **no exception**, and the corpus offers no grounding for how a redirect or
   a file stream is meant to be returned instead. The check therefore reports that
   shape rather than demanding a fix, and says explicitly that whether the rule
   grows an exception is the owning skill's call. **Open question for the owner.**
2. **5.27's first grep was wrong.** `Regex\.IsMatch\(` matches
   `SomeNameRegex.IsMatch(...)` — the *correct* usage through a generated member —
   so it produced three false positives on the canonical project. The shipped
   pattern anchors on a non-identifier character before `Regex.`, and the check
   names that false-positive shape as an explicit non-finding.

## What was rejected, and why

| Candidate | Verdict |
|---|---|
| An entity handed to the search wrapper (`elasticsearch-search`) | **Too noisy to ship.** Every grep tried matched legitimate EF repository calls inside search *services* — 9 hits on the canonical project, 4 on the consumer, essentially all false. Real rule, no honest pattern yet. **Kept as debt.** |
| An enum declared outside `Enums/` | **Already covered** — `dotnet-architecture-review` 4.5. |
| A controller named for two modules | **Already covered** — `dotnet-architecture-review` 3.5. |
| `ExpiryTime` sized against typical rather than worst-case work | Not syntactically detectable. A number is a number. |
| Test naming `MethodName_Scenario_ExpectedResult` | Style; an analyzer's job, not a reviewer's. |
| Most of `file-storage`, `excel-miniexcel`, `common-extensions` reference material | Scaffolding instructions — *"recreate these four files"* — normative in form, but they describe building something, and there is nothing to grep in a project that has not built it. |

## Honest limits

- **The join is token-based.** A rule whose tokens happen to appear in an
  unrelated check's grep is scored covered when it is not; a rule phrased without
  backticks is invisible to the scan entirely. Both directions of error are
  present and neither is quantified.
- **Triage was by reading, not exhaustive.** All 111 strong candidates were read
  once. A second pass by someone else would not produce the same six.
- **Nothing here is verified behaviourally.** That a check exists is not evidence
  a reviewer runs it. Direction H is the instrument for that, and the three eval
  cases logged in the board's PENDING log are its first concrete instance.
- **This matrix goes stale the moment a skill changes.** The scripts are here so
  the next run costs minutes, not a session. Re-run it after any release that adds
  rules.
