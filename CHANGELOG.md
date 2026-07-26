# Changelog

All notable changes to `dotnet-standards`.

Versions follow semantic versioning. The version in `.claude-plugin/plugin.json`
is the only signal an installed copy is stale, so it is bumped whenever
components change materially — not only on releases.

---

## [0.3.0] — S7b, 2026-07-26

### Changed
- **`facade-module-architecture` rebuilt from scratch** under the three-way authoring
  process (independent A/B drafts, `skill-arbiter` verdicts with file-verified reasons,
  user adjudication per piece). Evidence base re-founded on the user's per-area canonical
  designations (`ops-service` as the base project for solution/Core/facade-startup/
  composition-root conventions; `apsp-backend` as the production source for modules and
  controllers; `be-booking` for one anti-example only). All six recorded defects of the
  0.2.0 version are fixed, plus rulings made this session: two lifetime markers (no
  singleton marker), four sealed exceptions with no serialization ceremony on new ones,
  unified suffix-partial law (base list only in the suffix-less core file — services and
  controllers alike), `Enums/` unconditional, `<X>Validation.cs` naming, settings follow
  their service, `Facades/Common` fractal growth with reach-not-size placement,
  Expressions write-once, no `Mappings/` folder.
- **New shape:** a ~300-line decision-layer body plus six verbatim-approved
  `references/` files (`solution-layout`, `core-contracts`, `facades`, `modules`,
  `composition-root`, `web-controllers`), replacing the three 0.2.0 references.
  Nine authorized anti-examples live in the references, none in the body.
- **Description voice settled** by the arbiter loading Anthropic's official
  `skill-creator`: third person, under 100 words, trigger-noun "pushy", `Not for:`
  routing list. `docs/02-repo-structure.md` §5 rewritten to match; the shipped
  description is the reference example.

## [Unreleased] — process change, 2026-07-26

**No version bump, deliberately.** No plugin component changed — the version is the only
signal an installed copy is stale, and bumping it would mint a fresh 41 MB cache directory
for a docs-and-tooling change. Everything here is process.

### Added
- `.claude/agents/skill-writer-sp.md` and `.claude/agents/skill-arbiter.md` — the second and
  third authors in the new three-way skill authoring process. **Project tooling, not plugin
  content**: they live in `.claude/agents/`, never in the plugin's `agents/`, because triage
  settled that exactly one agent ships (`ef-core-specialist`, B18). Neither agent may write a
  file; both return draft text so nothing is written before the user approves. **Amended same
  day at the user's direction: all three participants — main session, writer, arbiter — read
  the user-named exemplar files in `reference/projects/` directly, with equal access.** The
  first design fed the agents only material pre-digested by the main session, which made every
  draft inherit one reading of the code and left the arbiter judging two drafts that shared
  one pair of eyes. Equal capability, differing only in loaded methodology; the reading
  discipline (user names the files, widening requires asking, no bulk scans, R7, Bash not
  Glob) binds all three identically.
- `docs/03-session-roadmap.md` — the **three-way authoring process**, mandatory from S7b
  onward. Main session drafts A from the repo's own rules; `skill-writer-sp` drafts B from
  Superpowers' `writing-skills`; `skill-arbiter` decides using Anthropic's official
  `skill-creator`. Piece by piece, explained in Vietnamese, user-approved before any write.

### Changed
- **R7 canonical source re-designated: `apsp-backend`, not `ops-service`.** The user
  identified `ops-service` as a base project rather than production. `apsp-backend`
  **confirms** the Facade/Module architecture — identical project graph, identical `Core`
  shape, identical two-axis split, identical 13-file configuration load order — so **Q1's
  answer stands**. Six details change, and `facade-module-architecture` is queued for rebuild
  in S7b.
- `docs/02-repo-structure.md` §5 — the description **voice** is now marked contested and
  explicitly undecided. Second person (§5) versus third person (the user's own
  `skill-creator` convention). Assigned to the arbiter, to be settled with reasons *after*
  drafts exist. Anti-triggers remain the settled part of the rule.

### Known issues in the shipped skill, pending the S7b rebuild
- `references/dependency-injection.md` documents two DI marker interfaces; `apsp-backend` has
  **three** (`ISingletonService` is missing).
- The principle *"`Core` holds primitives only"* is **wrong**. `Core` also holds an exception
  hierarchy and result wrappers.
- The one shipped anti-example — target-framework drift — **does not reproduce in
  `apsp-backend`**, where every project including both test projects targets `net7.0`. It was
  an `ops-service`-only defect and loses its standing unless re-authorised on new evidence.

### Notes
- **Definitions do not load in the session that creates them.** A newly written agent type is
  undispatchable until restart — the same constraint S6 measured for hooks, now confirmed to
  generalise to project-level agents. This is why S7 stopped at defining the process rather
  than exercising it.
- `apsp-backend` ships **eleven skills of its own**, now designated the highest-tier
  `from-my-code` source. `dotnet-standards` generalises an existing personal convention rather
  than writing on a blank page.
- **S8's blocking question is answered by the user's own written rule:** MediatR is
  *in-process messaging, not CQRS read/write separation*. The `cqrs-feature-slice` gateway as
  named describes a pipeline the user does not run.

---

## [0.2.0] — 2026-07-26

The first knowledge session. Q1 — open since S0 — is answered from real code, and
the first skill ships on top of the answer.

### Q1 resolved — the architecture has a name

The architecture is a **three-project chain** — `Core` → `Infrastructure` → `Web`,
with `Migrators.<Provider>` between the last two — whose `Infrastructure` project is
split on **two axes**: `Facades/` for technical capabilities and `Modules/` for
business ones. Every facade wires itself through a `Startup.cs` exposing
`AddX()`/`UseX()`, composed into a single flat fluent chain.

**It is not Clean Architecture and not Vertical Slice Architecture.** `Core` holds no
entities, business logic lives inside `Infrastructure`, and there is no `Domain` or
`Application` project — so it is not "close to" Clean Architecture either. Three
skipped triage rows (A03, A08, A35) are confirmed by this answer rather than reopened.

### Added
- `skills/facade-module-architecture/SKILL.md` — the first skill in the plugin, and
  the gateway that answers "where does this file belong?". Its description carries
  **anti-triggers** naming six sibling skills to use instead.
- `skills/facade-module-architecture/references/solution-layout.md` — solution and
  build files, package-version discipline, and the six solution-hygiene checks
  (TRIAGE A28 + D07 + B28).
- `skills/facade-module-architecture/references/configuration-and-options.md` — the
  Options pattern, startup validation, and the per-capability configuration-file
  convention (TRIAGE A10).
- `skills/facade-module-architecture/references/dependency-injection.md` — lifetimes,
  the captive-dependency bug, keyed services, and where registration lives
  (TRIAGE A14).

### Changed
- Gateway renamed **`solution-architecture ⚠️` → `facade-module-architecture`**
  across TRIAGE rows A10, A14, A28, B28 and D07, and in `00-brainstorm.md` §4.
  Historical decision-log entries keep the old name — the log is append-only.
- `00-brainstorm.md` §8: **Q1 closed**. Roadmap S7 row marked complete.
- TRIAGE **A05** and **A33**: setup sites traced from named-but-unverified to
  concrete paths, and confirmed to exist. Traced only — neither is distilled here.

### Fixed
- **TRIAGE A33 carried a wrong configuration path.** The row claimed Serilog is
  configured from a `Serilog` section in `appsettings*.json`. No such section exists;
  configuration is a strongly-typed POCO bound from a per-capability `logger.json`
  and applied imperatively in code. Corrected in the row and logged.

### Notes
- **One anti-example ships**, adjudicated by the user rather than assumed:
  target-framework drift. Four further divergences from the kit (no central package
  management, no `global.json`, a rules-free `.editorconfig`, classic `.sln`) are
  recorded as **observed conventions — neither endorsed nor faulted**, per R7's
  "label, don't blend".
- All version-specific content is dated **2026-07-26**. The stack targets **.NET 8**,
  not the kit's .NET 10. Next R10 trigger: **.NET 11 GA, 2026-11-10**.
- `NOTICE` unchanged — no new *kind* of artifact carries kit material; the three
  `references/` files are derived components already covered.

---

## [0.1.0] — 2026-07-26

The scaffold. No .NET knowledge ships in this version; this is the plumbing that
makes everything else installable.

### Added
- `.claude-plugin/plugin.json` and `.claude-plugin/marketplace.json` — the plugin
  manifest and the local development marketplace (`dotnet-standards-dev`).
- `hooks/post-edit-format` — the one hook that survived triage. Runs
  `dotnet format` scoped to the nearest `.csproj` after every `.cs` edit.
  Extensionless by necessity: Claude Code on Windows prepends `bash` to any
  command containing `.sh`.
- `hooks/run-hook.cmd` — polyglot CMD/POSIX wrapper. Copied in pattern from
  Superpowers, never referenced across plugins.
- `hooks/hooks.json` — rebuilt manifest, one `PostToolUse` entry. Auto-loaded, so
  it is deliberately **not** declared under `plugin.json`'s `manifest.hooks`.
- `hooks/README.md` — the three-kinds hook taxonomy, the Windows cost, and the
  rule that a hook may ship only if its silent absence is benign.
- `NOTICE` — two MIT attributions: `codewithmukesh/dotnet-claude-kit` at the
  pinned commit, and the wrapper pattern from Superpowers.
- Empty `skills/` and `agents/` directories for the components S7–S8 will build.

### Fixed
- `post-edit-format` — the reference kit's `dotnet format "$PROJECT" --include "$FILE"` call
  formats **nothing** on Windows, silently. Two independent causes, both measured on
  .NET SDK 10.0.301: an absolute project path with forward slashes triggers
  *"Skipping referenced project"*, and `--include` only matches paths relative to
  the current working directory. Now runs from the project directory with
  relative paths. See `hooks/README.md`.
- `post-edit-format` — the project walk now recognises `.slnx`, the `dotnet new sln`
  default since .NET 10, alongside `.sln`.

### Verified
- **Live confirmation, closing the one gap S6 could not close itself.** S6 proved
  the hook worked by running `run-hook.cmd` directly; it could not prove the
  hook fires *inside a live Claude Code session*, because the session that
  installs a plugin predates its hooks. Confirmed 2026-07-26 in a separate test
  project, project-scoped install (`--scope local`), fresh session after
  restart: writing a `.cs` file with irregular indentation through Claude's
  Write tool triggered `post-edit-format` and the file came back re-indented
  to 4-space / brace-on-own-line convention. **The hook fires end to end.**

### Notes
- **Installing this plugin copies the whole source directory and ignores
  `.gitignore`** — including `reference/`, which holds the kit clone and the
  author's real projects. First install copied 39 MB against a ~330 KB plugin.
  No exclusion mechanism exists for a `directory` marketplace source. Two
  candidate fixes are recorded in `docs/02-repo-structure.md` §4; neither is
  chosen yet because both change what §1 specifies. Until then, delete
  `reference/` from the cache copy after each install.
- **Install copies, it does not link.** Editing this repository changes nothing in
  the installed plugin until uninstall → install → restart.
- **No `mcpServers` block in `plugin.json`.** `CWM.RoslynNavigator` is kept as an
  externally installed dotnet tool, not bundled. Its install command and
  `.mcp.json` shape become a `references/` file in a later version.
- **No `commands/` directory**, by design — see `README.md`.
- **This plugin's knowledge layer carries dated content.** .NET/C# version
  guidance, breaking-change notes, package versions and commercial-licence
  boundaries all expire. The nearest known expiry is **.NET 11 GA,
  2026-11-10**. Treat a stale "current as of" line in `README.md` as a defect,
  not as cosmetics.
