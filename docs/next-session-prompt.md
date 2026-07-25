# Opening prompt — Session S3

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
  Source of exemplar code for later `adapt` sessions. **Do not open it in S3** — see R6 below.

S0 (planning), S1 (TRIAGE scaffolding) and S2 (Group A batch 1) are complete.
`docs/TRIAGE.md` holds **94 rows, 11 decided**: Group A 11/34 · Group B 0/32 · Group C 0/1 ·
Group D 0/27.

## FILES TO READ FIRST

1. `docs/TRIAGE.md` — the rows, the progress block, and **the S2 decision log entries**
   (eight of them; they contain policy that binds S3, not just history)
2. `docs/01-triage-rules.md` — the six status values and rules R1–R9
3. `docs/00-brainstorm.md` §2 (scope) and §4 (the 15 gateway skills) — the destinations rows land in

`docs/02-repo-structure.md` and `docs/03-session-roadmap.md` are **not** needed for S3.

## THE SINGLE DELIVERABLE OF THIS SESSION

**Decide the remaining 23 Group A rows in `docs/TRIAGE.md`. Nothing else.**

That is every Group A row except the eleven S2 closed (A01, A03, A06, A08, A17, A18, A23, A25,
A28, A31, A35):

| # | Row | # | Row |
|---|---|---|---|
| A02 ⇄ | `arch-check` | A16 ⇄ | `dotnet-init` |
| A04 | `aspire` | A19 ⇄ | `health-check` |
| A05 | `authentication` | A20 | `httpclient-factory` |
| A07 | `ci-cd` | A21 | `logging` |
| A09 ⇄ | `code-review` | A22 | `messaging` |
| A10 | `configuration` | A24 | `modern-csharp` |
| A11 | `container-publish` | A26 | `opentelemetry` |
| A12 | `ddd` | A27 | `project-setup` |
| A13 ⇄ | `de-sloppify` | A29 | `resilience` |
| A14 | `dependency-injection` | A32 ⇄ | `security-scan` |
| A15 | `docker` | A33 | `serilog` |
| | | A34 | `testing` |

For each row fill in: **Status · Provenance · Destination · Reason**.
Leave Canonical source, Anti-examples and Sanitized empty unless R6 is satisfied.

**Done when:** all 23 rows carry Status + Provenance + Destination + Reason; the four R4 rows are
short-circuited without deep reading; Q5 is answered and recorded; the Group A progress counter
reads 34/34; committed.

## WHAT S2 DECIDED THAT BINDS S3

These are settled. Apply them, do not re-litigate them.

**1 — `combine` is off the table for Group A. 1-1 mapping is the policy.**
I rejected every R3 combine proposal in S2 in favour of traceability: one kit skill → exactly one
`references/*.md` file, so any decision audits back to its source at the pinned SHA. Many kit
skills still land in the same gateway skill — they just keep separate files. This directly affects
the combine candidates still flagged in the table: **A20 `httpclient-factory` + A29 `resilience`**,
and **A21 `logging` + A33 `serilog` + A26 `opentelemetry`**. Give each its own destination file;
do not merge them.

**2 — My stack, recorded in S2.** Controllers (MVC), **not** Minimal API · Swagger UI /
Swashbuckle, **not** Scalar · **no** API versioning · MediatR + FluentValidation + AutoMapper.
Two of these are live divergences from both the kit and the .NET 10 defaults. **S3 does not
migrate my toolchain by triage** — those are R7 questions owed to S7. Flag divergences in the
Reason column and move on.

**3 — `cqrs-feature-slice` is `rebuild`.** No kit component supplies its skeleton. A35 is `skip`.
Consequence S3 must handle: **D15 `mediatr-to-mediator-migration.md` is now less relevant, not
more** — I am staying on MediatR, not migrating away. D15 is a Group D row, so do not decide it,
but do not let A22 `messaging` inherit an assumption that I am moving to Mediator or Wolverine.

**4 — A30 `scaffold` moved to Group B as B32.** Precedent for the ⇄ rows below.

**5 — The kit is multi-architecture by design; this plugin is not.** Three of the four batch-1
architecture rows were skipped for that reason. Expect the same pressure on A12 `ddd` and
A27 `project-setup`, both of which carry multi-architecture framing.

## THINGS S3 MUST HANDLE

**R4 short-circuit — apply it first, before any reading.** A04 `aspire`, A07 `ci-cd`,
A11 `container-publish` and A15 `docker` are all in areas excluded by brainstorm §2. Set them to
`skip`, Reason `out-of-scope v1`, **no deep reading**. Doing this first frees the context budget
for the rows that need it. Also check A16 `dotnet-init` and A22 `messaging`: A16 generates tier-3
`CLAUDE.md` templates, which are backlog rather than v1 — that is a scope call, not R4. A22 is
in scope (§2 lists background workers) but its Modular Monolith / saga framing is not.

**Q5 — resolve it.** *Do `auth-and-security` and `observability` have usable exemplars, or do they
fall back to `from-kit`?* This is the session that answers it. Four rows depend on the answer:
A05 `authentication`, A21 `logging`, A26 `opentelemetry`, A33 `serilog`. **Ask me** — see R6.
Record the answer in the decision log, not only in the row Reasons.

**Six ⇄ rows are still in Group A.** A02 `arch-check`, A09 `code-review`, A13 `de-sloppify`,
A16 `dotnet-init`, A19 `health-check`, A32 `security-scan`. Four of them are the review-rubric
anchors named in brainstorm §5, so they are not straightforwardly Group B — a rubric is knowledge
supplied *to* the Superpowers review process, which is exactly Group A. But A19 `health-check` is
a project-assessment workflow and A16 is an init workflow. Decide each on its merits; moving one
to Group B costs nothing and S2 set the precedent with A30. Note that A02, A09, A19 and A32 all
depend on the Group C Roslyn MCP server, which is undecided until S5 — that dependency belongs in
the Reason, and it is not by itself a reason to skip.

**R6 gates `adapt`.** A row may not be set to `adapt` until I have named specific exemplar files.
Without named exemplars the correct value is `keep-tweak` + `upgrade candidate`. **Ask me for
exemplar paths when a row looks like an `adapt` — do not go looking for them yourself, and do not
open `reference/projects/`.** In S2 I named none, so batch 1 produced zero `adapt` rows; do not
treat that as the expected outcome for S3, but do not manufacture `adapt` rows either.

**A34 `testing` is the deliberate gap.** I write no tests, so there is nothing to adapt. Expected
disposition is `keep-tweak` with provenance `from-kit + from-research` — this is the canonical
example in `01-triage-rules.md` §2. It is also the row where B09 `tdd`'s .NET substance may
eventually land; note that, do not decide B09.

**One new component S2 surfaced, with no kit row behind it:** `api-surface` needs a
`references/controller-conventions.md` written from my code, because A23 `minimal-api` describes
a shape I do not use. Record it somewhere durable in S3 — it must not be lost before S7.

## HARD CONSTRAINTS

**1 — One session, one deliverable.** S3 decides the remaining Group A rows only. Do **not**
decide Group B, C or D rows, do not write a skill, do not create `plugin.json`, do not touch
`reference/projects/`. If I ask for more mid-session, refuse and record the request in
`docs/03-session-roadmap.md`. This is a design constraint, not a suggestion.

**2 — Context discipline.** After the R4 short-circuit removes four rows, read the `SKILL.md` of
**only the remaining Group A rows** — at most 19 files, fewer if a row resolves without deep
reading. Nothing else from the kit. Any widening must be announced up front: what you are looking
for, and why. **Known cost, discovered in S2:** reading any file under `reference/dotnet-claude-kit/`
causes the harness to auto-inject the kit's root `CLAUDE.md` and all ten `.claude/rules/*.md`
files into context. Budget for it, and remember that seeing D01–D10 is not permission to decide
them.

**3 — Artifact language is English.** All generated files (docs, TRIAGE, and later skills and
descriptions) are written in English. Talk to me in Vietnamese.

**4 — End-of-session ritual.** Commit with a clear message, then rewrite
`docs/next-session-prompt.md` so it opens **S4** (Group B — the process layer: 12 meta/workflow
skills, 10 agents, 9 hook rows, plus B32 `scaffold`, each with a mandatory R5 conflict check,
resolving open questions Q2, Q3 and Q4).

Start by confirming you understand the constraints, then read the files listed above.
