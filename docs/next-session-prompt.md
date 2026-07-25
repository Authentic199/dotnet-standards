# Opening prompt — Session S2

> Copy everything below the line into a **fresh** Claude Code session opened in
> `D:\ALTA\Project\dotnet-standards`.

---

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin that holds my .NET knowledge.
It runs alongside **Superpowers** (the process layer) as an independent plugin.
**No Superpowers file may ever be modified.**

Three-tier architecture: Superpowers = process · `dotnet-standards` = knowledge ·
per-project `CLAUDE.md` = glue.

Reference material, **read-only, never installed as a plugin**:
- `reference/dotnet-claude-kit` — codewithmukesh/dotnet-claude-kit (MIT), pinned at
  commit `cd83d315986c27621da178dad73bd95d503c1540`. Every decision is anchored to that SHA.
- `reference/projects/` — my real .NET projects (gitignored). Currently one: `apsp-backend`.
  Source of exemplar code for later `adapt` sessions. **Do not open it in S2** — see R6 below.

Sessions S0 (planning) and S1 (TRIAGE scaffolding) are complete.
`docs/TRIAGE.md` now holds **94 rows, all `pending`**: Group A 35 · Group B 31 · Group C 1 ·
Group D 27. S1 made no dispositions. **S2 is the first session that decides anything.**

## FILES TO READ FIRST

1. `docs/TRIAGE.md` — the 94 rows, the enumeration conventions, and the decision log
2. `docs/01-triage-rules.md` — the six status values and rules R1–R9
3. `docs/00-brainstorm.md` §2 (scope) and §4 (the 15 gateway skills) — the destinations rows land in

`docs/02-repo-structure.md` and `docs/03-session-roadmap.md` are **not** needed for S2.

## THE SINGLE DELIVERABLE OF THIS SESSION

**Decide the Group A batch-1 rows in `docs/TRIAGE.md`. Nothing else.**

Batch 1 is the six core knowledge areas: architecture, CQRS/MediatR, EF Core, caching,
API surface, error handling. Mapped onto the S1 rows, that is **12 rows** — challenge this
mapping if you disagree with it:

| Area | Rows |
|---|---|
| Architecture | A03 `architecture-advisor` · A08 `clean-architecture` · A28 `project-structure` · A35 `vertical-slice` |
| CQRS / MediatR | A30 `scaffold` — **the kit has no dedicated CQRS or MediatR skill**; see the note below |
| EF Core | A17 `ef-core` |
| Caching | A06 `caching` |
| API surface | A01 `api-versioning` · A23 `minimal-api` · A25 `openapi` · A31 `scalar` |
| Error handling | A18 `error-handling` |

For each row, fill in: **Status · Provenance · Destination · Reason**.
Leave Canonical source, Anti-examples and Sanitized empty unless R6 is satisfied (below).

**Done when:** all 12 batch-1 rows carry Status + Provenance + Destination + Reason; every
`adapt` row names real exemplar files (R6); the Group A progress counter is updated; committed.

## THINGS S1 SURFACED THAT S2 MUST HANDLE

**The CQRS gap.** The kit ships no `cqrs` or `mediatr` skill. The nearest material is
A35 `vertical-slice` (handler patterns for Mediator/Wolverine), A30 `scaffold` (feature-slice
generation) and D15 `knowledge/mediatr-to-mediator-migration.md`. My gateway skill
`cqrs-feature-slice` therefore has no kit skeleton to keep. Decide whether it is `rebuild`, or
`adapt` built on the vertical-slice skeleton — and record that reasoning, because it drives the
whole S8 session. D15 and A22 `messaging` sit adjacent to this area but are **not** in batch 1.

**Q1 is still open.** My real architecture is **not** Clean Architecture, and its name is not
decided until S7. Group A destinations may therefore point at the placeholder
`solution-architecture ⚠️`. Do not let A08 or A35 quietly become the answer to Q1.

**R6 gates `adapt`.** A row may not be set to `adapt` until I have named specific exemplar files.
If I have not named any for an area, the correct value is `keep-tweak` + `upgrade candidate`.
**Ask me for exemplar paths when a row looks like an `adapt` — do not go looking for them
yourself, and do not open `reference/projects/`.**

**R3 combine candidates already flagged in S1.** A25 `openapi` + A31 `scalar` → one `api-surface`
gateway. A06 `caching` folds into `distributed-caching`. These are proposals recorded by S1, not
decisions — confirm or reject them explicitly.

**Borderline group markers (⇄).** Rows marked ⇄ in TRIAGE sit on the Group A / Group B boundary.
A30 `scaffold` carries one. Moving a ⇄ row to another group is allowed and costs nothing.

## HARD CONSTRAINTS

**1 — One session, one deliverable.** S2 decides batch-1 Group A rows only. Do **not** decide
Group B, C or D rows, do not write a skill, do not create `plugin.json`, do not touch
`reference/projects/`. If I ask for more mid-session, refuse and record the request in
`docs/03-session-roadmap.md`. This is a design constraint, not a suggestion.

**2 — Context discipline.** Read the `SKILL.md` of **only the kit skills in batch 1** — at most
12 files. Nothing else from the kit. Any widening must be announced up front: what you are
looking for, and why.

**3 — Artifact language is English.** All generated files (docs, TRIAGE, and later skills and
descriptions) are written in English. Talk to me in Vietnamese.

**4 — End-of-session ritual.** Commit with a clear message, then rewrite
`docs/next-session-prompt.md` so it opens **S3** (Group A decisions, batch 2 — all remaining
Group A rows, applying the R4 out-of-scope short-circuit, and resolving open question Q5).

Start by confirming you understand the constraints, then read the files listed above.
