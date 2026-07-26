# Opening prompt — Session S8

> Copy everything below the line into a **fresh** Claude Code session opened in
> `D:\ALTA\Project\dotnet-standards`. Written at the close of S7, 2026-07-26.

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
  S7 opened `ops-service` for the first time. **The reading discipline below is unchanged and is
  not relaxed by S7 having gone first.**

**Phase 1 is closed.** `docs/TRIAGE.md` holds 94 rows, 94 decided, zero `pending`. Triage is
**input**, never a subject for revision.

**Phase 2 shipped the scaffold (S6) and the first knowledge skill (S7).** The plugin installs,
registers one hook, and now carries **one skill** — `facade-module-architecture` — verified via
`claude plugin details`. S8 is the **second** knowledge session.

## WHAT S7 SETTLED — Q1 IS CLOSED, AND IT CHANGES WHAT YOU MAY ASSUME

**Q1 is answered.** My architecture is a three-project chain — `Core` → `Infrastructure` →
`Web`, with `Migrators.<Provider>` between the last two — whose `Infrastructure` project is split
on **two axes**: `Facades/` for technical capabilities and `Modules/` for business ones. Every
facade wires itself via a `Startup.cs` exposing `AddX()`/`UseX()`, composed into one flat fluent
chain. `Program.cs` registers nothing itself.

**It is not Clean Architecture and not Vertical Slice Architecture**, and it is not "close to"
either: `Core` holds no entities, business logic lives *inside* `Infrastructure`, and there is no
`Domain` or `Application` project. A03, A08 and A35 stay skipped — Q1's answer **confirms** those
skips rather than reopening them.

Read `skills/facade-module-architecture/SKILL.md` before designing anything. A CQRS feature has to
land somewhere in that layout, and "where does this file belong?" is already answered.

## THE BLOCKING QUESTION — ANSWER THIS BEFORE ANY OTHER WORK

S8 is `cqrs-feature-slice`, which S2 decided to **`rebuild`** from scratch: the kit's pipeline is
**Mediator** while mine is **MediatR**, and the kit has **no AutoMapper anywhere**. Because it is
`rebuild` and wholly `from-my-code`, the exemplar *is* the entire content of the skill.

**S7 found that `ops-service` contains no MediatR at all** — zero package references, zero
`IRequest<>` / `IRequestHandler` occurrences. `AddInfrastructure` registers `AddFluentValidation()`
and `AddAutoMapper(...)` but no `AddMediatR`. Business logic runs through ordinary services
(`Modules/<Area>/Services/UserService.cs`, `RoleService.cs`, …), split across partial-class files
by a `Name.Suffix.cs` convention.

**So ask me first, and do not proceed past my answer:**

| If | Then |
|---|---|
| **(a)** MediatR is in `apsp-backend` | That becomes S8's R7 canonical source — a different project from S7's, which R7 allows (it is per-skill) but which must be stated in the row |
| **(b)** MediatR is in neither project | S8 becomes `from-research` like A34, not `from-my-code`. Say so and I will decide whether it still runs now |
| **(c)** The service-based shape *is* my real convention | The gateway is renamed and rebuilt around services rather than handlers, and brainstorm §4 #2 changes |

**I name the exemplars either way.** Do not open `apsp-backend` to find out which case applies —
that is exemplar selection, and it is mine alone.

## FILES TO READ FIRST

1. `skills/facade-module-architecture/SKILL.md` — where a feature's files belong. Non-negotiable
   input; S8 does not re-decide layout.
2. `docs/TRIAGE.md` — the **decision log entry for `cqrs-feature-slice`** (S2, the `rebuild`
   entry), the **S7 MediatR entry**, and the **Q1 entry**. Plus rows **A35** (`vertical-slice`,
   `skip` — its Pattern A skeleton was the nearest candidate and was rejected, and the reason
   matters) and **D15** (`skip`). You do not need the other rows.
3. `docs/01-triage-rules.md` §3 and §7 — **R7** (one canonical source, never average two
   conventions) and **R8** (record anti-examples). Both bit hard in S7.
4. `docs/03-session-roadmap.md` — the **five-step adapt session**. Step 3 (distil + sanitize) and
   step 4 (reverse-check) are the two that are easiest to skip and most expensive to skip. S7's
   step-4 pass caught a wrong claim before it shipped; run it.
5. `docs/02-repo-structure.md` §5 — skill format only.

## THE SINGLE DELIVERABLE OF THIS SESSION

**The `cqrs-feature-slice` gateway skill**, built on whichever answer you get to the blocking
question above, plus its `references/` files.

**Done when:** the skill exists with a description stating its **anti-triggers** (what it is *not*
for and which sibling to use instead — mechanism C, and note that `facade-module-architecture` now
exists as a real cross-reference target); the `references/` files contain no path into a real
project and no business-domain names; the skill's rules and its distilled code agree line by line
(step 4, the reverse-check); the plugin reinstalls and `claude plugin details dotnet-standards`
reports **`Skills (2)`**; committed.

## READING `reference/projects/` — THE DISCIPLINE, NOT A SUGGESTION

1. **I name the exemplars. You never select them for me.** If you need files I have not named,
   **ask** — do not go looking.
2. **No bulk scanning.** Any widening is a *targeted lookup* — grep or glob for a specific
   symbol — announced up front with **what** you are seeking and **why**.
3. **R7 — one canonical source.** Never average two conventions; averaging produces a convention
   that exists in no real codebase. Where projects diverge, ask *"which one from now on?"* and
   record the answer.
4. **R8 — record anti-examples.** An anti-example is code **I point at**. If you find something
   yourself, **ask me before labelling it "avoid this"** — S7 established this and it matters:
   labelling your own judgement as my convention is the failure R7 exists to prevent. S7 found
   seven candidates, asked, and shipped exactly one.
5. **Sanitize.** No connection strings, no secrets, no internal package names, no business-domain
   names, and **never a path into a real project**.

## WHAT S6 AND S7 MEASURED — IT OVERRIDES THE DOCS WHERE THEY DISAGREE

**1 — Install copies the directory; it does not link to it.** Editing this repo changes nothing in
the installed plugin. **Write the whole skill, then install once.** There is no live-reload.

**2 — The copy ignores `.gitignore` and copies `reference/`** — 39 MB against a ~330 KB plugin.
Delete `reference/` from the cache copy after each install. **S7's cheaper method:**
`git archive HEAD | tar -x -C <cache-dir>` reproduces the cache copy without `reference/` at all.
**The fix is still unowned** — two candidates are recorded in `02-repo-structure.md` §4 and neither
is chosen, because both change what §1 specifies. **S8 does not own this.**

**3 — The plugin cache is shared across projects, and a version directory can belong to another
project.** `~/.claude/plugins/installed_plugins.json` maps each *project path* to its own
*installPath*. S7 deleted a version directory believing it was a stale duplicate; it was the
install backing a different project, and had to be restored from `git archive`. **Never delete a
cache version directory without reading `installed_plugins.json` first.**

**4 — `Glob` is unreliable inside `reference/projects/`.** It reported "no files found" for files
that exist and are git-tracked — the directory is gitignored *by this repo*, which is the likely
cause. **Use Bash `find`/`ls` there, and never report a negative `Glob` result as a finding about
my code.** S7 did exactly that once and had to retract it.

**5 — `dotnet format` on Windows fails silently in two ways:** an absolute project path with
**forward slashes** yields *"Skipping referenced project"* and formats 0 of 0 files, and
**`--include` only matches paths relative to the current working directory**. Both exit 0 and
print nothing. Documented in `hooks/README.md`.

**6 — `claude plugin` is a CLI.** `install`, `uninstall`, `list`, `validate`, and especially
**`details`**, which prints the component inventory the harness actually parsed — the fastest way
to catch a frontmatter error, which otherwise fails **silently**. Note `uninstall` needs
`--scope local` to match a local-scope install, and `list` enumerates *every* project's local
install, so two rows for one plugin is normal rather than a duplicate.

## WHAT IS SETTLED AND MUST NOT BE RE-LITIGATED

**1 — Triage is closed.** A row's Status, Destination and Reason are inputs. If a row looks wrong
while building, **say so and record it** — do not silently rewrite it. S7 did exactly this twice
(A33's config path, A14's Scrutor hedge) and both became log entries, not row rewrites.

**2 — My stack.** Controllers (MVC), **not** Minimal API · Swagger UI / Swashbuckle, **not**
Scalar · **no** API versioning · MediatR + FluentValidation + AutoMapper · Redis · Elasticsearch.
Web API and Worker project shapes. Where the kit contradicts these, my stack wins and the
divergence is already recorded in the row. **Caveat from S7:** the MediatR half of that line is
exactly what the blocking question above is about.

**3 — Q1 is closed.** See above. Nothing may reopen it.

**4 — The scaffold is done and is not S8's subject.** One hook, and only one. No `commands/`
directory. No `mcpServers` block — `CWM.RoslynNavigator` is an external dotnet tool this plugin
*documents*, and its install command and `.mcp.json` shape become a `references/` file inside
`project-scaffolding`. `NOTICE` needs no change unless S8 carries kit material into a **new kind**
of artifact; it already covers all 52 derived components, and S7 confirmed that three new
`references/` files did not require an update.

**5 — Open questions S8 does not own** (beyond its own): the **repository-over-EF-Core** question
(four contradictory sources, two inside the kit), the **HybridCache-vs-Redis** resolution (four
rows), and the **commercial-licence three-way choice** (MediatR v13+, AutoMapper v15+,
FluentAssertions v8+ — it lands in B06's licence table, a later session). Q1–Q5 are all closed.
**S8 does own the AutoMapper-vs-projection R7 question**, which S2 owed to this session.

**6 — This repo has no `LICENSE`, deliberately.** Personal and unpublished, so "all rights
reserved" by default; `NOTICE` covers the third-party obligations regardless.

**7 — The knowledge layer carries dated content.** `README.md` holds the pinned SHA and an **"as
of" date**, and a stale date is a defect. **R10** gives two re-pin triggers: the kit moved, **or**
the .NET release train moved past what we recorded. Nearest expiry: **.NET 11 GA on 2026-11-10**.
**S7 established that my stack targets .NET 8, not the kit's .NET 10** — so anything
version-specific carries a date *and* states which framework version it targets.

## HARD CONSTRAINTS

**1 — One session, one deliverable.** `cqrs-feature-slice` only. Do **not** write the router
(`choosing-a-dotnet-skill` is S15, deliberately, so its decision table has real targets), do
**not** touch the four review rubrics, do **not** build a second gateway skill, do **not**
restructure the repo. If I ask for more mid-session, refuse and record the request in
`docs/03-session-roadmap.md` under a **"Requests deferred out of S8"** heading. This is a design
constraint, not a suggestion.

**2 — Prove it, do not assert it.** A skill either loads or does not. Reinstall, confirm
`claude plugin details dotnet-standards` reports **`Skills (2)`**, and say what actually happened
including failures. A frontmatter error fails **silently** — the skill simply is not there — so
"I wrote the file" is not evidence.

**3 — Artifact language is English.** All generated files — docs, TRIAGE, skills, descriptions.
Talk to me in Vietnamese.

**4 — End-of-session ritual.** Commit with a clear message, bump `version` in
`.claude-plugin/plugin.json` **and** `.claude-plugin/marketplace.json` (they must agree —
`claude plugin validate` checks this), add a `CHANGELOG.md` entry, then rewrite
`docs/next-session-prompt.md` so it opens **S9** — `ef-core-data-access`.

Start by confirming you understand the constraints, then **ask me the blocking question** before
reading anything from `reference/projects/`.
