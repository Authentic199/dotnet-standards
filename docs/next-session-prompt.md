# Opening prompt — Session S4

> Copy everything below the line into a **fresh** Claude Code session opened in
> `D:\ALTA\Project\dotnet-standards`.

---

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin that holds my .NET knowledge.
It runs alongside **Superpowers** (the process layer) as an independent plugin.
**No Superpowers file may ever be modified.** This is absolute and permanent.

Three-tier architecture: Superpowers = process · `dotnet-standards` = knowledge ·
per-project `CLAUDE.md` = glue.

Reference material, **read-only, never installed as a plugin**:
- `reference/dotnet-claude-kit` — codewithmukesh/dotnet-claude-kit (MIT), pinned at
  commit `cd83d315986c27621da178dad73bd95d503c1540`. Every decision is anchored to that SHA.
- `reference/projects/` — my real .NET projects (gitignored). Contains at least `apsp-backend`
  and `ops-service`. Source of exemplar code for the later `adapt` sessions.
  **Do not open it in S4.** S4 decides process components; no exemplar is relevant.

S0 (planning), S1 (TRIAGE scaffolding), S2 (Group A batch 1) and S3 (Group A batch 2) are
complete. **Group A is closed — 33/33, zero `pending`.**
`docs/TRIAGE.md` holds **94 rows, 33 decided**: Group A 33/33 ✅ · Group B 0/33 · Group C 0/1 ·
Group D 0/27.

## FILES TO READ FIRST

1. `docs/TRIAGE.md` — the Group B rows, the progress block, and **the decision log**. There are
   now 19 entries. Read the **ten S3 entries** carefully: several bind S4 directly, especially
   the ones on A19→B33, the four rubric anchors, and the Group C dependency.
2. `docs/01-triage-rules.md` — §4 (Group B rules and **R5**, the five-item conflict check) and
   §7 (R4, R7, R9). This is the rule set S4 runs on.
3. `docs/00-brainstorm.md` §5 (the four review rubrics) and §6 (the three process-layer gaps I
   identified, plus the Windows hook cost).

`docs/02-repo-structure.md` is needed **only** if a hook row survives — it holds the
`run-hook.cmd` wrapper design. `docs/03-session-roadmap.md` is not needed except to record a
deferred request.

## THE SINGLE DELIVERABLE OF THIS SESSION

**Decide all 33 Group B rows in `docs/TRIAGE.md`. Nothing else.**

| Block | Rows | Count |
|---|---|---|
| B.1 — Meta / workflow skills | B01–B12, plus **B32** `scaffold` and **B33** `health-check` | 14 |
| B.2 — Agents | B13–B22 | 10 |
| B.3 — Hooks | B23–B31 (7 `.sh` scripts + `hooks.json` + `README.md`) | 9 |

For each row fill in: **Superpowers equivalent? · Conflict check (all five R5 items) · Status ·
Reason.**

**Done when:** all 33 rows carry those four fields; every `keep` and `combine` row answers all
five R5 items explicitly; every hook row states the Windows `run-hook.cmd` cost; Q2, Q3 and Q4
are answered and recorded in the decision log; the Group B progress counter reads 33/33;
committed.

## THE RULE THAT MAKES GROUP B DIFFERENT FROM GROUP A

**Never default to `skip`** (`01-triage-rules.md` §4). Group A could skip a row for having no
consumer. Group B may not. Every row is compared against the equivalent Superpowers capability
and assigned one of exactly three outcomes:

- `skip` — Superpowers already does this as well or better.
- `keep` — Superpowers does not have it and I need it.
- `combine` — Superpowers has a base version; the kit's material extends it.

**R5 — the conflict check is mandatory for every `keep` and `combine`.** Five items, each
answered explicitly in the row, not summarised:

1. **Hook events** — same event as a Superpowers hook?
2. **Slash-command names** — collides with Superpowers *or* a Claude Code built-in
   (`/code-review`, `/security-review`, `/review`, `/init`, `/simplify`, `/run`)?
3. **Skill names** — collides with a Superpowers skill name?
4. **Instructions** — contradicts the brainstorm → plan → TDD → review flow?
5. **Agent names** — collides with an existing agent?

**An unresolvable conflict downgrades the row to `skip`.** And the golden rule: any extension
lives inside `dotnet-standards` as a new skill or hook. No Superpowers file is ever modified.

To answer items 2, 3 and 5 you need the actual Superpowers inventory — list its skills, commands
and agents at the start of the session. That is an announced widening and it is expected.

## OPEN QUESTIONS S4 MUST RESOLVE

| # | Question | Rows it decides |
|---|---|---|
| **Q2** | Can a `.cs` format hook coexist with Superpowers' hooks, and is the `run-hook.cmd` cost worth it? | B24, and the shape of B23 |
| **Q3** | Which of the kit's 10 agents (if any) survive Superpowers' review flow? | B13–B22 |
| **Q4** | Should the deferred `UserPromptSubmit` skill-index hook (mechanism E) be built? | A **new** component — not a kit row. Record it as such. |

Record each answer in the decision log, not only in the row Reasons.

## WHAT S2 AND S3 DECIDED THAT BINDS S4

Settled. Apply them, do not re-litigate them.

**1 — My stack.** Controllers (MVC), **not** Minimal API · Swagger UI / Swashbuckle, **not**
Scalar · **no** API versioning · MediatR + FluentValidation + AutoMapper · Redis · Elasticsearch.
I am **staying on MediatR** — not migrating to Mediator or Wolverine. Any Group B row that
assumes otherwise is wrong about my project, not right about my future.

**2 — I write no tests today.** This is a deliberate gap I want filled from kit + research
(A34, provenance `from-kit + from-research`). It bears directly on **B09 `tdd`** and
**B22 `test-engineer`**: do not treat the absence of tests as evidence that testing tooling
should be skipped. S3 flagged that B09's .NET substance (xUnit v3, `WebApplicationFactory`,
Testcontainers, Verify) **may belong in A34's reference file rather than in a Group B skill** —
that is a live option for B09, and choosing it is a `combine`, not a `skip`.

**3 — Two ⇄ rows arrived in Group B from Group A, and both carry salvage notes.**
- **B32 `scaffold`** (was A30, moved in S2) — its durable knowledge is the 9-item
  feature-completeness checklist; its CQRS substance lives in
  `references/architecture-patterns.md`, which no session has read yet.
- **B33 `health-check`** (was A19, moved in S3) — **read its Reason column before deciding it.**
  Its Step 2.5 **Triage Gate** and `references/grading-rubric.md` are cross-rubric quality
  material that S7 must harvest **even if you set B33 to `skip`.** A `skip` here must preserve
  that, the way S2's A08 `skip` preserved four anti-pattern blocks in the decision log.

**4 — The four review rubrics stayed in Group A and are not up for re-decision.** A02
`arch-check`, A09 `code-review`, A13 `de-sloppify`, A32 `security-scan` ship as **rubrics with no
slash-command name**, which is what defuses the collisions with the built-in `/code-review` and
`/security-review`. This matters for S4 because their Group B counterparts — **B15
`code-reviewer`**, **B20 `refactor-cleaner`**, **B21 `security-auditor`**, **B19
`performance-analyst`** — are the *agents* that would consume them. Deciding an agent `keep`
while its rubric already exists in Group A is coherent; deciding it `keep` as a competing review
*workflow* is not.

**5 — Group C is still undecided until S5, and three Group B rows depend on it.** B06
`outdated`, B15 `code-reviewer` and B20 `refactor-cleaner` all consume the Roslyn MCP server
(C01). As in S3: **the dependency belongs in the Reason, and it is not by itself a reason to
skip.** State how the row degrades without C01.

**6 — `combine` is real here, unlike in Group A.** S2 rejected `combine` for Group A in favour
of 1-1 traceability. That decision was about `references/*.md` file mapping and **does not carry
over to Group B** — §4 of the rules names `combine` as one of the three legitimate Group B
outcomes, and brainstorm §6 already proposes it for `dotnet-build-loop` and the `.cs` format
hook. Do not import the Group A policy here.

## THINGS S4 MUST HANDLE

**The Windows hook cost is a decision input, not an implementation detail.** The kit's hooks are
`.sh`. Claude Code on Windows runs hooks through `CMD.exe`, which cannot execute them — it opens
them in an editor. Keeping *any* kit hook requires shipping a polyglot `run-hook.cmd` wrapper and
depends on Git for Windows being installed. **Every hook row's Reason must state this cost**, and
Q2 is partly a question about whether the cost is worth paying at all.

**B16 `devops-engineer` is an R4 short-circuit.** Its entire subject area — Docker, GitHub
Actions / Azure DevOps pipelines, Aspire orchestration — is excluded by brainstorm §2, and the
matching Group A rows (A04, A07, A11, A15) are already R4 skips. Set it to `skip`, Reason
`out-of-scope v1`, no deep reading. It is the only R4 row in Group B; do not stretch R4 to cover
rows whose subject is merely overlapping.

**B04 `instinct-system` needs conflict-check item 4 read carefully.** It writes to
`.claude/instincts.md`, `MEMORY.md` and `.claude/learning-log.md`. Check what it would do to the
memory conventions this environment already has before deciding.

**B07 `plan`, B08 `spec`, B10 `verify`, B11 `workflow-mastery`, B12 `wrap-up` are the
head-on collisions.** Each overlaps a Superpowers skill by name or by function
(`writing-plans` / plan mode, `brainstorming`, `verification-before-completion`, several at once,
`finishing-a-development-branch`). These are where R5 item 3 and item 4 earn their place — I
would rather have five well-reasoned `skip`s here than five skills that fight Superpowers.

**Do not decide Group C or Group D rows.** Reading kit files will again auto-inject the kit's
root `CLAUDE.md` and all ten `.claude/rules/*.md` into context — this happened in both S2 and S3
and is now known to be deterministic. Seeing D01–D10 is not permission to decide them.
**D06 `.claude/rules/hooks.md` will be especially tempting while you work on B23–B31 — leave it
`pending`.** S3 also flagged D10 (testing) and D14 (`dotnet-whats-new`) as overlapping decided
Group A rows; those flags are for S5.

## HARD CONSTRAINTS

**1 — One session, one deliverable.** S4 decides Group B rows only. Do **not** decide Group C or
D rows, do not write a skill or a hook, do not create `plugin.json`, do not touch
`reference/projects/`, do not modify any Superpowers file. If I ask for more mid-session, refuse
and record the request in `docs/03-session-roadmap.md` under a "Requests deferred out of S4"
heading. This is a design constraint, not a suggestion.

**2 — Context discipline.** Apply the B16 R4 short-circuit first, before any reading. Then read
only what a decision actually needs: the kit's 10 agent files are short, the 7 hook scripts are
short, and `hooks.json` decides conflict-check item 1 for every hook row — read that early. For
the 14 meta/workflow skills, the `SKILL.md` is enough; do not open their `references/` unless a
specific decision turns on it. Announce any widening up front: what you are looking for, and why.
Listing the Superpowers inventory is a required widening, not an optional one.

**3 — Artifact language is English.** All generated files — docs, TRIAGE, and later skills and
descriptions — are written in English. Talk to me in Vietnamese.

**4 — End-of-session ritual.** Commit with a clear message, then rewrite
`docs/next-session-prompt.md` so it opens **S5** (Group C + Group D — the MCP server, the 10
kit rules, 6 knowledge files, 6 ADRs and 5 templates; every Group D row needs a Destination of
*skill content* · *project `CLAUDE.md` material* · *drop*; plus the three components S1 recorded
as un-enumerated: `mcp-configs/`, root `.mcp.json`, root `.editorconfig`). **S5 is the gate into
Phase 2** — TRIAGE must reach zero `pending` rows everywhere.

Start by confirming you understand the constraints, then read the files listed above.
