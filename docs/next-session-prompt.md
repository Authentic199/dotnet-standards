# Opening prompt — Session S1

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
  commit `cd83d315986c27621da178dad73bd95d503c1540`.
- `reference/projects/` — my real .NET projects (gitignored). Currently one:
  `apsp-backend`. Source of exemplar code for later `adapt` sessions. **Not used in S1.**

Session S0 (planning) is complete. Its output is in `docs/`.

## FILES TO READ FIRST

1. `docs/00-brainstorm.md` — plugin goal, scope, the 15 gateway skills + 4 review rubrics,
   open questions
2. `docs/01-triage-rules.md` — the triage rules, including R1–R9
3. `docs/03-session-roadmap.md` — the S1 entry and the end-of-session ritual
4. `docs/TRIAGE.md` — the existing skeleton you will be extending

`docs/02-repo-structure.md` is **not** needed for S1.

## THE SINGLE DELIVERABLE OF THIS SESSION

**Populate `docs/TRIAGE.md` with rows — and make no decisions.**

Concretely:
1. Extend the table schemas to match `01-triage-rules.md` §8 — add the `Provenance` (R1),
   `Destination` (R2) and `Anti-examples` (R8) columns.
2. Enumerate **every** component of the reference kit as exactly one row, filed under
   Group A / B / C / D, each with a one-line summary and status `pending`.
   Components live under: `skills/` (46), `agents/` (10), `hooks/` (8 scripts + `hooks.json`),
   `knowledge/` (6 files + `decisions/`), `templates/` (5), `mcp/` (1), `.claude/rules/`.
3. Write the pinned SHA into the TRIAGE header.
4. Fill in the Progress section denominators.
5. Commit.

**Done when:** every kit component has exactly one row; every status is still `pending`;
the SHA is in the header; the Progress denominators are real numbers; committed.

## HARD CONSTRAINTS

**1 — One session, one deliverable.** S1 produces row *scaffolding* only. Do **not** decide any
status, do not write a skill, do not create `plugin.json`. If I ask for more mid-session, refuse
and record the request in `docs/03-session-roadmap.md`. This is a design constraint, not a
suggestion.

**2 — Context discipline.** Do **not** read the contents of the reference kit's files in S1.
Directory listings (`ls`/`tree`) and file names are enough to build rows — deciding is a later
session's job. Do not open `reference/projects/` at all this session.
Any widening must be announced: what you are looking for, and why.

**3 — Artifact language is English.** All generated files (docs, TRIAGE, and later skills and
descriptions) are written in English. Talk to me in Vietnamese.

**4 — End-of-session ritual.** Commit with a clear message, then rewrite
`docs/next-session-prompt.md` so it opens **S2** (Group A decisions, batch 1).

Start by confirming you understand the constraints, then read the four files listed above.
