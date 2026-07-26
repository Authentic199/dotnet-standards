# Changelog

All notable changes to `dotnet-standards`.

Versions follow semantic versioning. The version in `.claude-plugin/plugin.json`
is the only signal an installed copy is stale, so it is bumped whenever
components change materially — not only on releases.

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
