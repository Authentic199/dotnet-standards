# Opening prompt — Session S7b (rebuild under the three-way process)

> Copy everything below the line into a **fresh** Claude Code session opened in
> `D:\ALTA\Project\dotnet-standards`. A fresh session is **mandatory**, not a convention:
> the two authoring agents were defined at the close of S7 and are undispatchable until a
> restart. Written at the close of S7, 2026-07-26.

---

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin that holds my .NET knowledge.
It runs alongside **Superpowers** (the process layer) as an independent plugin.
**No Superpowers file may ever be modified.** This is absolute and permanent.

Three-tier architecture: Superpowers = process · `dotnet-standards` = knowledge ·
per-project `CLAUDE.md` = glue.

Reference material, **read-only, never installed as a plugin**:
- `reference/dotnet-claude-kit` — codewithmukesh/dotnet-claude-kit (MIT), pinned at
  `cd83d315986c27621da178dad73bd95d503c1540`. Every decision is anchored to that SHA.
- `reference/projects/` — my real .NET projects (gitignored): **`apsp-backend`** (real,
  in production — **the canonical source**) and `ops-service` (a base project, **comparison
  only**).

**Phase 1 is closed.** `docs/TRIAGE.md`: 94 rows, 94 decided. Triage is **input**, never a
subject for revision.

## WHY THIS SESSION EXISTS

S7 answered Q1 and shipped the first skill, `facade-module-architecture` — **but distilled it
from `ops-service`, which I then told it is a base project, not the real one.** R7 requires one
canonical source per skill, chosen by me, and forbids averaging. So the source is re-designated
and the skill is rebuilt.

**Q1's answer is NOT reopened.** `apsp-backend` *confirms* the architecture: identical project
graph, identical `Core` shape, identical `Facades/` × `Modules/` split, identical self-wiring
`Startup.cs` convention, identical 13-file configuration load order. `ops-service` is evidently
a base extracted from `apsp-backend`. Only the evidence base and six details change.

S7 also **defined a new authoring process but could not run it**, because an agent definition
does not load in the session that creates it. That is the other reason we restart.

## THE ARCHITECTURE — SETTLED, DO NOT RE-DERIVE

```
Core  ←  Infrastructure  ←  Migrators.<Provider>  ←  Web
     ←───────────────────────────────────────────────┘
```

- **`Core`** — contracts and base types. `Bases/` (`BaseEntity<TId>`, `IGuidIdentify`),
  `Common/Exceptions/` (`CustomException` → `HttpCustomException` → five concrete types, plus
  `SuccessResultWrapper<TData>` / `ErrorResultWrapper`), `Common/Interfaces/` (`IScopedService`,
  `ISingletonService`, `ITransientService`, `ICode`), `Helpers/`. **No entities, no business
  rules.**
- **`Infrastructure`** — references only `Core`, and **owns the business logic**. Split on two
  axes: **`Facades/`** (~21 technical capabilities) × **`Modules/`** (22 business capabilities,
  each internally `Entities/ Requests/ Responses/ Services/` plus optional
  `Mappings/ Validations/ Seeders/ Settings/`).
- **`Migrators.<Provider>`** — provider-specific EF Core migration assemblies only.
- **`Web`** — `Program.cs`, `Controllers/` (one folder per module, all inheriting
  `BaseController`), `Configurations/`, host assets. Registers nothing itself.

**The signature convention:** every facade owns a `Startup.cs` exposing `AddX()`/`UseX()`;
`Infrastructure/Startup.cs` composes them into a **single flat fluent chain**
(`AddInfrastructure` / `UseInfrastructure`); **order inside `UseInfrastructure` IS the
middleware pipeline order**. Modules have **no** `Startup.cs` — they implement a marker
interface from `Core` and a Scrutor `services.Scan(...)` in `Facades/Common/Startup.cs`
registers them, so implementing the marker *is* the lifetime decision.

**It is not Clean Architecture and not VSA**, and not "close to" either.

## THE SINGLE DELIVERABLE

**`facade-module-architecture`, rebuilt from `apsp-backend` under the three-way process.**

Six known defects in the shipped version that the rebuild must fix:

| # | Defect |
|---|---|
| 1 | `references/dependency-injection.md` documents **two** DI marker interfaces; there are **three** — `ISingletonService` is missing |
| 2 | The principle *"`Core` holds primitives only"* is **wrong** — `Core` also holds the exception hierarchy and result wrappers |
| 3 | The shipped anti-example (target-framework drift) **does not reproduce in `apsp-backend`** — every project including both test projects targets `net7.0`. It was `ops-service`-only. Do not carry it unless I re-authorise it |
| 4 | `Facades/Identity/` is **separate** from `Facades/Auth/` — A05's recorded boundary is drawn in the wrong place |
| 5 | `Facades/Persistence/` contains `RepositoryBase` / `IRepositoryWrapper` — I **do** use a repository abstraction over EF Core, which the kit forbids outright. Note it and point at `ef-core-data-access`; do not resolve it here |
| 6 | `Web/Controllers/` has a `BaseController` and one folder per module — a placement rule this gateway owns |

**Done when:** every piece has been through the three-way loop and I approved it; nothing
contains a path into a real project or a business-domain name; rules and distilled code agree
line by line; the plugin reinstalls and `claude plugin details dotnet-standards` reports
`Skills (1)`; committed.

## THE THREE-WAY AUTHORING PROCESS — MANDATORY, READ `03-session-roadmap.md` FOR THE FULL RULE

| Author | Loads (the only thing that differs) |
|---|---|
| **A — you, the main session** | `docs/02-repo-structure.md` §5, `docs/00-brainstorm.md` §3, the kit's skill format — **not** `superpowers:writing-skills`, so A and B do not share a methodology |
| **B — `skill-writer-sp`** | `superpowers:writing-skills` |
| **Arbiter — `skill-arbiter`** | Anthropic's official `skill-creator` |

**Equal source access.** All three of you read the **same exemplar files I name** in
`reference/projects/` — directly, each with your own eyes. Do not feed B or the arbiter your
summary in place of the code; the whole point is three independent readings. The reading
discipline binds all three identically: I name the files, widening requires asking, no bulk
scans, `apsp-backend` is canonical, Bash not Glob. When the arbiter finds the drafts disagree
about a fact in the code, it opens the file and checks rather than guessing which author read
it right.

**Per piece, not per skill:**

1. **You explain first, in Vietnamese** — what you intend to write, why you decided that way,
   what is good about it, how it combines with the other pieces. I comment.
2. **A and B draft the same piece independently.** Both return text. **Neither writes a file.**
3. **The arbiter decides** — `A`, `B`, `MERGE` or `NEITHER`, never "either is fine" — naming
   the specific property that decided it, and what it cut and why.
4. **I review the verdict and the reasons and approve.** Only then do you write the file.
5. Repeat until the skill is complete.

**Structure is an output, not an input.** Do not fix the section list, or choose between the
knowledge shape and my workflow-shaped template, before drafts exist. Deciding that in advance
without analysis or perspective is over-engineering — that is my ruling and it stands.

**You still own the agents' prompts.** Equal access means they read the named exemplars
themselves — not that they can find the rule sets alone. Every agent prompt must carry: the
exemplar file list I named, the two live conflicts below, and the fact that
`apsp-backend/skills/` is the highest-tier `from-my-code` source. Skipping any of these
silently turns a three-way decision into a two-way one.

**Two live conflicts the arbiter inherits:**
- **Description voice** — `02-repo-structure.md` §5 says second person (`Use when …`); my
  `apsp-backend/skills/skill-creator/SKILL.md` says **third person** (*"This skill should be
  used when…"*), "pushy", **under 100 words**, explicit trigger phrases. The shipped
  description follows §5 and exceeds 100 words.
- **Body shape** — knowledge shape (Core Principles → Patterns → Anti-patterns → Decision
  Guide) versus my template (Prerequisites → Steps → Conventions → Examples → Common Mistakes).

**Announce every agent use, and relay progress at natural milestones** — start, blocker, phase
completion, completion. Agents cannot interrupt mid-run; they end with a `## QUESTIONS`
section. Answer what you can, escalate only genuine decisions to me, then continue the same
agent with `SendMessage` so its context survives. **Run agents in the current working directory
— no worktree.**

## READING `reference/projects/` — THE DISCIPLINE, NOT A SUGGESTION

1. **I name the exemplars. You never select them for me.** If you need files I have not named,
   **ask** — do not go looking.
2. **No bulk scanning.** Any widening is a *targeted lookup* — grep or glob for a specific
   symbol — announced up front with **what** you are seeking and **why**.
3. **R7 — one canonical source.** `apsp-backend` for this skill. `ops-service` is comparison
   only. **Never average two conventions.**
4. **`apsp-backend/skills/` is the highest-tier `from-my-code` source** — eleven skills I wrote
   myself, in my own words, above your reading of the code. Read the counterpart before
   drafting, cross-check it against the code, and **report divergence rather than silently
   preferring one.**
5. **R8 — anti-examples are code *I* point at.** If you find something yourself, **ask before
   labelling it "avoid this"**. S7 found seven candidates, asked, and shipped one — and that
   one turned out not to reproduce in the real project.
6. **Sanitize.** No connection strings, no secrets, no internal package names, no
   business-domain names, and **never a path into a real project**.

## WHAT S6 AND S7 MEASURED — IT OVERRIDES THE DOCS WHERE THEY DISAGREE

**1 — A definition does not load in the session that creates it.** Agents, hooks and skills all
require a restart before they are usable. Plan for it.

**2 — Install copies the directory; it does not link.** Editing this repo changes nothing in
the installed plugin. **Write the whole skill, then install once.** No live-reload.

**3 — The copy ignores `.gitignore` and pulls in `reference/`** — 39 MB against a ~330 KB
plugin. Cheapest remedy: `git archive HEAD | tar -x -C <cache-dir>`. The permanent fix is still
unowned — two candidates in `02-repo-structure.md` §4, neither chosen. **S7b does not own it.**

**4 — The plugin cache is shared across projects.** `~/.claude/plugins/installed_plugins.json`
maps each *project path* to its own *installPath*. S7 deleted a version directory believing it
was a stale duplicate; it belonged to another project and had to be restored. **Never delete a
cache version directory without reading that file first.**

**5 — `Glob` is unreliable inside `reference/projects/`.** It reported "no files found" for
git-tracked files that exist. **Use Bash `find`/`ls` there, and never report a negative `Glob`
result as a finding about my code.**

**6 — `dotnet format` on Windows fails silently in two ways:** an absolute project path with
**forward slashes** yields *"Skipping referenced project"*, and **`--include` only matches paths
relative to the cwd**. Both exit 0 and print nothing. See `hooks/README.md`.

**7 — `claude plugin` is a CLI.** Especially **`details`**, which prints the inventory the
harness actually parsed — the fastest way to catch a frontmatter error, which otherwise fails
**silently**. `uninstall` needs `--scope local`; `list` enumerates every project's local
install, so two rows for one plugin is normal.

## WHAT IS SETTLED AND MUST NOT BE RE-LITIGATED

**1 — Triage is closed.** A row's Status, Destination and Reason are inputs. If a row looks
wrong while building, **say so and record it** — do not silently rewrite it.

**2 — My stack.** Controllers (MVC), **not** Minimal API · Swashbuckle, **not** Scalar · **no**
API versioning · FluentValidation + AutoMapper · Redis · Elasticsearch · Hangfire.
**MediatR is in-process messaging, NOT CQRS read/write separation** — my own written rule,
confirmed by the code. `apsp-backend` targets **`net7.0`**, not the kit's .NET 10.

**3 — Q1 is closed.** The architecture above. Nothing reopens it.

**4 — The scaffold is done.** One hook, and only one. No `commands/`. No `mcpServers` block.
`NOTICE` needs no change unless a **new kind** of artifact carries kit material.

**5 — Agents live in `.claude/agents/`, never the plugin's `agents/`.** They are tooling for
building the plugin; triage settled that exactly one agent ships (`ef-core-specialist`, B18).

**6 — Open questions S7b does not own:** repository-over-EF-Core (now with real evidence — see
defect 5), HybridCache-vs-Redis, the commercial-licence three-way choice, and the
`cqrs-feature-slice` refounding (S8 — the gateway as named describes a pipeline I do not run).

## HARD CONSTRAINTS

**1 — One session, one deliverable.** The rebuilt `facade-module-architecture` only. Do **not**
write the router, touch the review rubrics, build a second gateway, or restructure the repo. If
I ask for more mid-session, refuse and record it in `docs/03-session-roadmap.md` under
**"Requests deferred out of S7b"**.

**2 — Prove it, do not assert it.** Reinstall, confirm `claude plugin details dotnet-standards`
reports `Skills (1)`, and say what actually happened including failures. A frontmatter error
fails **silently** — "I wrote the file" is not evidence.

**3 — Artifact language is English. Talk to me in Vietnamese.**

**4 — End-of-session ritual.** Commit, bump `version` in **both** `.claude-plugin/plugin.json`
and `.claude-plugin/marketplace.json` (they must agree — `claude plugin validate` checks it),
add a `CHANGELOG.md` entry, then rewrite this file so it opens **S8** — the
`cqrs-feature-slice` refounding.

Start by confirming you understand the constraints, then **verify that `skill-writer-sp` and
`skill-arbiter` are dispatchable** before doing anything else. If they are not, stop and tell
me — the whole process depends on them.
