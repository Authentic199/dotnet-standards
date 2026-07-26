# Changelog

All notable changes to `dotnet-standards`.

Versions follow semantic versioning. The version in `.claude-plugin/plugin.json`
is the only signal an installed copy is stale, so it is bumped whenever
components change materially — not only on releases.

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
