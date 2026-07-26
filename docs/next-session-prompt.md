# Opening prompt — Session S7

> Copy everything below the line into a **fresh** Claude Code session opened in
> `D:\ALTA\Project\dotnet-standards`. Written at the close of S6, 2026-07-26.

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
- `reference/projects/` — my real .NET projects (gitignored): `apsp-backend` and `ops-service`.
  **S7 is the first session allowed to open this.** It is not a licence to browse — see the
  reading discipline below.

**Phase 1 is closed.** `docs/TRIAGE.md` holds 94 rows, 94 decided, zero `pending`, with a
45-entry decision log. Triage is **input** to S7, never a subject for it.

**Phase 2 opened with S6, which shipped the scaffold.** The plugin installs, registers exactly
one hook, and that hook demonstrably formats C# files. No skill exists yet. **S7 is the first
knowledge session** — the first session whose output is .NET content rather than plumbing.

## STATUS: THE LIVE-HOOK GAP IS CLOSED

S6 could prove the hook worked by running it directly, but not that it fires *inside a live
Claude Code session* — the session that installs a plugin predates its hooks. **That gap is now
closed**, confirmed 2026-07-26 in a separate test project with a project-scoped install
(`--scope local`) and a fresh session after restart: a `.cs` file written through Claude's Write
tool with irregular indentation came back correctly formatted. See `CHANGELOG.md`. **S7 does not
need to repeat this.**

If the plugin is not yet installed in *this* environment (a different project, or the same one
in a new working tree):

```
claude plugin marketplace add D:/ALTA/Project/dotnet-standards --scope local
claude plugin install dotnet-standards@dotnet-standards-dev --scope local
# restart, then delete reference/ from the cache copy — see below
```

`--scope local` writes to `.claude/settings.local.json` (gitignored) rather than the shared
project config — the right default while this is still a personal test loop.

## FILES TO READ FIRST

1. `docs/TRIAGE.md` — **five rows and one skip.** The rows are **A28** (`project-structure`,
   `keep-tweak`), **A10** (`configuration`, `keep-tweak`), **A14** (`dependency-injection`,
   `keep-tweak`), **D07** (`packages.md`, `combine` — merges into A28's file), and **B28**
   (`pre-build-validate`, `combine` — six solution-hygiene checks that ship beside A28). Plus
   the **A08** row, which is `skip` and explains *why* Q1 is still open. You do not need the
   other 88 rows.
2. `docs/00-brainstorm.md` **§4 row 1** — the `solution-architecture ⚠️` gateway definition, and
   **§3** for the three packaging mechanisms (gateway + `references/`, description discipline
   with anti-triggers, the router).
3. `docs/01-triage-rules.md` **§3 and §7** — R7 (one canonical source per skill, never average
   two conventions) and R8 (record anti-examples). Both bite in S7.
4. `docs/03-session-roadmap.md` — **the five-step adapt session structure.** It applies here
   without exception, and step 3 (distil + sanitize) and step 4 (reverse-check) are the two that
   are easiest to skip and most expensive to skip.
5. `docs/02-repo-structure.md` **§5** for the skill format only. §1–§4 and §6 are S6's business
   and are already implemented.

## THE SINGLE DELIVERABLE OF THIS SESSION

**Q1 resolved, and the `solution-architecture` gateway skill built on the answer.**

**Q1 is:** *what is my actual architecture?* It has been deliberately unanswered since S0 and it
has already cost three rows — **A03** (`architecture-advisor`), **A08** (`clean-architecture`)
and **A35** (`vertical-slice`) were all skipped rather than let them pre-empt this decision, and
**B07** and **B32** lost their Step 1 / Step 2 for the same reason. Nothing downstream may
assume Clean Architecture, VSA or anything else until this lands. **The answer comes from
reading `ops-service`, not from picking a named pattern off a shelf** — and if the real answer is
"a pragmatic layering that matches no textbook", that is a valid answer and must be written down
as what it is, not rounded to the nearest famous name.

Four artifacts:

| # | Artifact | Source |
|---|---|---|
| 1 | **The Q1 answer**, written into `docs/TRIAGE.md`'s decision log | `ops-service`, read under the discipline below |
| 2 | `skills/solution-architecture/SKILL.md` | Q1 + brainstorm §4 row 1 + §3's description rules |
| 3 | `skills/solution-architecture/references/solution-layout.md` | A28 `keep-tweak` + D07 merged + B28's six checks |
| 4 | `…/references/configuration-and-options.md` and `…/references/dependency-injection.md` | A10 and A14 — both `keep-tweak`, both framework fact, both architecture-neutral |

**Done when:** Q1 has a written answer with the exemplar paths that justify it; the gateway skill
exists with a description that states its **anti-triggers** (what it is *not* for, and which
sibling skill to use instead — mechanism C); the three `references/` files exist and contain no
path into a real project; the skill's rules and its distilled code agree line by line (step 4,
the reverse-check); the plugin reinstalls and `claude plugin details dotnet-standards` reports
`Skills (1)`; committed.

**The gateway name is provisional** (`⚠️` in brainstorm §4). If Q1's answer makes a better name
obvious, rename it now — this is the last cheap moment, before 14 sibling skills and a router
point at it.

## READING `reference/projects/` — THE DISCIPLINE, NOT A SUGGESTION

This is the first session with access, and the rules exist because a real codebase contains both
good code and technical debt.

1. **I name the exemplars. You never select them for me.** If you need files I have not named,
   ask — do not go looking.
2. **No bulk scanning.** Any widening beyond what I name is a *targeted lookup* — grep or glob
   for a specific symbol, or a Roslyn MCP query — announced up front with **what** you are
   seeking and **why**.
3. **R7 — one canonical source.** `solution-architecture` draws from **`ops-service`**.
   `apsp-backend` is for comparison only. Where the two diverge, ask me *"which one from now
   on?"* and record the answer. **Never average two conventions** — averaging produces a
   convention that exists in no real codebase.
4. **R8 — record anti-examples.** If I point at code I do *not* want repeated, the skill must
   say "avoid this", not only "do this".
5. **Sanitize.** The finished skill is self-contained. No connection strings, no secrets, no
   internal package names, no business-domain names, and **never a path into a real project**.

## A BOUNDED SECOND TASK — TRACING, NOT DISTILLING

Two decided rows say in their own text that **tracing is an S7 task**:

- **A05** `authentication` (`adapt`, `from-my-code`) → `ops-service`
  `src/Infrastructure/Facades/Auth/` plus its setup sites (`Program.cs`, DI extension methods).
- **A33** `serilog` (`adapt`, `from-my-code`) → `ops-service`
  `src/Infrastructure/Facades/Logging/` plus its setup sites (`Program.cs` bootstrap, the
  `Serilog` section of `appsettings*.json`).

Both rows say the setup sites are *named but not yet traced* because S3 could not open
`reference/projects/`. **S7 traces them and writes the concrete paths into the two rows. S7 does
not distil them into content** — A05 belongs to the `auth-and-security` session and A33 to
`observability`, and building either here would be three deliverables in one session.

> **Note on a wording drift, flagged rather than silently resolved.** The S6 prompt said S7
> "promotes A05 and A33 from recorded paths to distilled content". TRIAGE's own rows say
> *tracing* is the S7 task, and the one-deliverable rule agrees with TRIAGE. This prompt follows
> TRIAGE. **If you actually meant full distillation, say so in your first message** and I will
> drop `solution-architecture` to a later session rather than run two knowledge deliverables at
> once.

## WHAT S6 MEASURED — IT OVERRIDES THE DOCS WHERE THEY DISAGREE

All four are already written into `02-repo-structure.md`; they are repeated because they change
how S7 works, not just what it knows.

**1 — Install copies the directory; it does not link to it.** Editing this repo changes nothing
in the installed plugin. Every verification needs the full `uninstall → install → restart`
cycle. There is no live-reload, so **write the whole skill, then install once** rather than
installing to check each edit.

**2 — The copy ignores `.gitignore`, and it copies `reference/`.** The first install pulled
**39 MB** — the kit clone and both real project checkouts — into `~/.claude/plugins/cache/`,
against a ~330 KB plugin. Delete `reference/` from the cache copy after each install.
**Two candidate fixes are recorded in `02-repo-structure.md` §4 and neither is chosen**, because
both change what §1 specifies. **S7 does not own this** — do not let it become a reason to
restructure the repo mid-session. If it needs deciding, it gets its own session.

**3 — `dotnet format` on Windows fails silently in two ways**, worth knowing because they will
reappear in `dotnet-testing` and in the review rubrics: an absolute project path with **forward
slashes** produces *"Skipping referenced project"* and formats 0 of 0 files, and **`--include`
only matches paths relative to the current working directory**. Both exit 0 and print nothing.
Documented in `hooks/README.md`.

**4 — `claude plugin` is a CLI, not only slash commands.** `install`, `uninstall`, `list`,
`validate`, and especially **`details`**, which prints the component inventory the harness
actually parsed. That is the fastest way to confirm a `SKILL.md` was loaded — and the fastest
way to catch a frontmatter error, which otherwise fails silently.

## WHAT IS SETTLED AND MUST NOT BE RE-LITIGATED

**1 — Triage is closed.** 94 rows decided. A row's Status, Destination and Reason are inputs. If
a row looks wrong while building, say so and record it — do not silently rewrite it.

**2 — My stack.** Controllers (MVC), **not** Minimal API · Swagger UI / Swashbuckle, **not**
Scalar · **no** API versioning · MediatR + FluentValidation + AutoMapper · Redis · Elasticsearch.
Web API and Worker service project shapes. The kit contradicts several of these on purpose —
where it does, my stack wins, and the divergence is already recorded in the row.

**3 — The scaffold is done and is not S7's subject.** One hook, and only one — `pre-bash-guard`
and the `UserPromptSubmit` skill-index hook are refused, with reasons, in the Q2 and Q4 log
entries. No `commands/` directory. No `mcpServers` block: `CWM.RoslynNavigator` is an external
dotnet tool that this plugin *documents*, and its install command and `.mcp.json` shape become a
`references/` file inside `project-scaffolding`, not here. `NOTICE` discharges both MIT
obligations and needs no change unless S7 carries kit material into a **new kind** of artifact —
it already covers all 52 derived components.

**4 — Open questions S7 does not own.** The **repository-over-EF-Core** question (four
contradictory sources, two inside the kit), the **HybridCache-vs-Redis** resolution (four rows),
the **commercial-licence three-way choice** (MediatR v13+, AutoMapper v15+, FluentAssertions v8+
— it lands in B06's licence table, a later session), and the **AutoMapper-vs-projection** R7
question. Q2, Q3, Q4 and Q5 are closed. **Q1 is the one S7 owns.**

**5 — This repo has no `LICENSE`, deliberately.** Personal and unpublished, so "all rights
reserved" by default; `NOTICE` covers the third-party obligations regardless. Revisit only if
there is a reason to publish.

**6 — The knowledge layer carries dated content.** `README.md` holds the pinned SHA and an
**"as of" date**, and a stale date is a defect. **R10** in `01-triage-rules.md` §7 gives two
re-pin triggers: the kit moved, **or** the .NET release train moved past what we recorded. The
nearest expiry is **.NET 11 GA on 2026-11-10**. If S7 writes anything version-specific, it
carries a date.

## HARD CONSTRAINTS

**1 — One session, one deliverable.** Q1 plus the `solution-architecture` gateway, plus the
bounded A05/A33 *tracing*. Do **not** build a second gateway skill, do **not** write the router
(`choosing-a-dotnet-skill` is S15, deliberately, so its decision table has real targets), do
**not** touch the four review rubrics, do **not** restructure the repo. If I ask for more
mid-session, refuse and record the request in `docs/03-session-roadmap.md` under a
**"Requests deferred out of S7"** heading. This is a design constraint, not a suggestion.

**2 — Prove it, do not assert it.** A skill either loads or does not. Reinstall, restart,
confirm `claude plugin details dotnet-standards` reports the skill, and say what actually
happened including failures. A frontmatter error fails **silently** — the skill simply is not
there — so "I wrote the file" is not evidence.

**3 — Artifact language is English.** All generated files — docs, TRIAGE, skills, descriptions.
Talk to me in Vietnamese.

**4 — End-of-session ritual.** Commit with a clear message, bump `version` in
`.claude-plugin/plugin.json` **and** `.claude-plugin/marketplace.json` (they must agree —
`claude plugin validate` checks this), add a `CHANGELOG.md` entry, then rewrite
`docs/next-session-prompt.md` so it opens **S8** — `cqrs-feature-slice`, the gateway S2 decided
to `rebuild` from scratch because the kit's pipeline is Mediator while mine is MediatR, and the
kit has no AutoMapper anywhere.

Start by confirming you understand the constraints, then read the files listed above.
