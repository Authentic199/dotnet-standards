# 02 — Plugin Repository Structure

> Source of truth: the `superpowers-developing-for-claude-code:developing-claude-code-plugins`
> skill and its `references/plugin-structure.md` + `references/polyglot-hooks.md`, read during S0.
> Nothing here is recalled from memory.
>
> **No file described here is created in S0.** This document is the specification that S6 implements.

---

## 1. Directory layout

```
dotnet-standards/                     <- plugin root (this repository)
├── .claude-plugin/
│   ├── plugin.json                   REQUIRED — plugin manifest
│   └── marketplace.json              personal dev marketplace
├── skills/                           at ROOT, never inside .claude-plugin/
│   └── <skill-name>/
│       ├── SKILL.md                  required for every skill
│       ├── references/               sub-topic docs (packaging mechanism A)
│       ├── assets/                   distilled + sanitized exemplar code
│       └── scripts/                  optional executable helpers
├── agents/
│   └── <agent-name>.md               only if Group B triage says keep
├── commands/
│   └── <command-name>.md             likely unused — see §5
├── hooks/
│   ├── hooks.json                    auto-loaded by Claude Code
│   ├── run-hook.cmd                  polyglot wrapper — REQUIRED on Windows
│   └── <hook-name>                   actual hook logic — EXTENSIONLESS, see §6
├── docs/                             planning docs (this directory)
├── reference/                        gitignored — kit clone + exemplar projects
├── LICENSE
├── NOTICE                            MIT attribution to dotnet-claude-kit (rule R9)
└── README.md
```

## 2. Hard rules (verified, not assumed)

1. **`.claude-plugin/` contains manifests only** — `plugin.json` and optionally
   `marketplace.json`. Skills, commands, agents and hooks live at the **plugin root**. Putting
   components inside `.claude-plugin/` breaks loading.
2. **Use `${CLAUDE_PLUGIN_ROOT}` for every path in config files.** Never hard-code an absolute
   path — it makes the plugin non-portable and breaks hooks and MCP servers.
3. **Paths in `plugin.json` are relative and start with `./`.**
4. **`hooks/hooks.json` is loaded automatically.** Do **not** also declare it under
   `plugin.json`'s `manifest.hooks` field — that produces a
   *"Duplicate hooks file detected"* error.
5. **Scripts must be executable** (`chmod +x`) for hooks and MCP servers.

## 3. `plugin.json`

Minimal form:

```json
{
  "name": "dotnet-standards",
  "version": "0.1.0",
  "description": "Personal .NET knowledge layer: architecture, CQRS pipeline, EF Core, caching, search, API conventions, testing.",
  "author": { "name": "<user>" }
}
```

Optional fields available when needed: `homepage`, `repository`, `license`, `keywords`,
`mcpServers`.

**`mcpServers`** is where `CWM.RoslynNavigator` would be wired *if* Group C triage ever changes
from "external tool" to "bundled". The S0 default is external — see `01-triage-rules.md` §5 —
so `plugin.json` ships without an `mcpServers` block.

## 4. Personal marketplace and the local test loop

`.claude-plugin/marketplace.json`:

```json
{
  "name": "dotnet-standards-dev",
  "description": "Personal development marketplace",
  "owner": { "name": "<user>" },
  "plugins": [
    {
      "name": "dotnet-standards",
      "description": "Personal .NET knowledge layer",
      "version": "0.1.0",
      "source": "./",
      "author": { "name": "<user>" }
    }
  ]
}
```

Install / iterate:

```
/plugin marketplace add D:/ALTA/Project/dotnet-standards
/plugin install dotnet-standards@dotnet-standards-dev
# restart Claude Code

# after changes:
/plugin uninstall dotnet-standards@dotnet-standards-dev
/plugin install dotnet-standards@dotnet-standards-dev
# restart Claude Code
```

A restart is required for changes to take effect. "It didn't work" is usually a missing restart.

**Two things S6 measured about this loop, both of which change how it is used:**

**1 — Install *copies* the directory; it does not link to it.** The plugin lands at
`~/.claude/plugins/cache/<marketplace>/<plugin>/<version>/` as a full copy. Editing a file in this
repository therefore changes **nothing** in the installed plugin until the uninstall/install/restart
cycle is run. There is no live-reload.

**2 — The copy ignores `.gitignore`, and that is a problem this repository specifically has.**
The whole source directory is copied, including `reference/` — the kit clone *and*
`reference/projects/`, the user's real .NET projects. The first install copied **39 MB**, of which
essentially all was `reference/`, into the plugin cache. The plugin itself is ~330 KB.

`claude plugin marketplace add` offers `--sparse` for git sources, but this marketplace is
registered as a `directory` source and no exclusion mechanism applies to it. Two candidate fixes,
**neither decided in S6 because both change what §1 specifies**: register the marketplace as a
*git* source so the checkout only contains tracked files, or move the plugin root into a
subdirectory so `docs/` and `reference/` sit outside it. Until one is chosen, the mitigation is
manual: delete `reference/` from the cache copy after each install. It is dead weight there —
no shipped component references it.

**Also verified in S6:** `claude plugin` exposes the whole loop as CLI subcommands
(`marketplace add`, `install`, `list`, `details`, `validate`, `uninstall`), so the loop can be
driven without the interactive `/plugin` commands. `claude plugin details dotnet-standards`
prints the component inventory the harness actually loaded, which is the fastest way to confirm a
manifest was parsed — it reported `Hooks (1) PostToolUse` and no duplicate-hooks error.

Later distribution options, if the plugin is ever shared: direct GitHub
(`/plugin marketplace add <org>/<repo>`), a separate marketplace repository, or
`extraKnownMarketplaces` in team settings. Not needed for personal use.

## 5. Component formats

### Skill — `skills/<name>/SKILL.md`

```markdown
---
name: skill-name
description: Use when [triggering conditions] - [what it does]
---
```

Per rule **C** from `00-brainstorm.md` §3, every description in this plugin also states its
**anti-triggers**: what it is *not* for, and which sibling skill to use instead.

> **⚠️ The voice above is contested and is NOT settled — do not treat it as decided.**
> This section prescribes the **second person** (`Use when …`). The user's own authoring
> convention, `apsp-backend/skills/skill-creator/SKILL.md`, prescribes the **third person**
> (*"This skill should be used when…"*, explicitly not *"Use this skill when…"*), "pushy"
> phrasing, **under 100 words**, and explicit trigger phrases — and S7 designated that file the
> highest-tier `from-my-code` source. The already-shipped `facade-module-architecture`
> description follows this section and exceeds 100 words.
>
> **Resolution is owned by the `skill-arbiter` agent**, which loads Anthropic's official
> `skill-creator`, and it must decide **with stated reasons** after competing drafts exist —
> not before. See the three-way authoring process in `03-session-roadmap.md`. Whichever voice
> wins, this section is rewritten to match it and the shipped description is brought into line.
> Until then, **anti-triggers are the only part of this rule that is settled.**

### Agent — `agents/<name>.md`

```markdown
---
description: What this agent specializes in
capabilities: ["capability1", "capability2"]
---
```

### Command — `commands/<name>.md`

```markdown
---
description: Brief description
---
```

**Deliberately avoided in v1.** Slash-command names collide easily with Claude Code built-ins
(`/code-review`, `/security-review`, `/review`, `/init`). The four review rubrics are shipped as
**skills**, not commands, precisely to sidestep this. A command is only added if S4 triage proves
one is needed and clears conflict-check item 2.

### Hook — `hooks/hooks.json`

> **Corrected in S6.** The S0 draft of this example passed `format.sh` to the wrapper. That is
> wrong on Windows and was the stale spec that S4's measurement caught: Claude Code prepends
> `bash` to any command containing `.sh`. **The script name passed to the wrapper carries no
> extension**, and the script file on disk carries none either. This is the shipped form.

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [
          {
            "type": "command",
            "command": "\"${CLAUDE_PLUGIN_ROOT}/hooks/run-hook.cmd\" post-edit-format",
            "shell": "bash",
            "async": false
          }
        ]
      }
    ]
  }
}
```

`"shell": "bash"` and `"async": false` are copied from Superpowers' working manifest.
**Verified in S6:** the wrapper produces identical, correct results whether Claude Code invokes it
through `cmd.exe` or through `bash`, both with an explicit path argument and with the `PostToolUse`
stdin JSON. `jq` is **not** installed in this environment and the script's `sed` fallback extracts
the Windows path correctly, escaped backslashes and all.

Available events: `PreToolUse`, `PostToolUse`, `UserPromptSubmit`, `SessionStart`, `SessionEnd`,
`Stop`, `SubagentStop`, `PreCompact`, `Notification`.

## 6. Windows: the polyglot hook wrapper

The user's environment is Windows 11. Claude Code invokes hooks through **`CMD.exe`**, which
cannot execute `.sh` files — it tries to open them in a text editor. The kit's eight hooks are
all `.sh`.

The fix is a single reusable polyglot wrapper, valid in both CMD and POSIX sh.

> **Superseded in S6.** The sketch below was written in S0 from documentation. It has a single
> hard-coded bash path and no fallback, so it hard-fails on any machine where Git is installed
> elsewhere. **The shipped wrapper is `hooks/run-hook.cmd`** — read that file, not this sketch.
> It follows the pattern Superpowers actually uses: three discovery attempts, then a silent
> `exit /b 0`. The two structural facts below still hold and are why the pattern works at all.

- CMD reads `:` as a label and ignores the rest of the line, runs `bash.exe`, then `exit /b`
  stops it before the Unix half.
- POSIX sh treats `:` as a no-op and swallows the CMD block as a quoted heredoc.
- `$0` is used instead of `${BASH_SOURCE[0]}` — the latter fails on systems where `/bin/sh` is
  dash.

**Requirements and consequences**
- **A bash must be installed.** The shipped wrapper looks in `C:\Program Files\Git\bin\bash.exe`,
  then `C:\Program Files (x86)\Git\bin\bash.exe`, then whatever `where bash` finds.
- **If none is found the wrapper exits 0 in silence** — the hook never runs and nothing reports
  it. That behaviour is deliberate, not an oversight; it is the whole substance of **Q2**, and it
  is why a hook may ship only when its silent absence is benign by design. See `hooks/README.md`.
- Quote `${CLAUDE_PLUGIN_ROOT}` in `hooks.json` — on Windows it can contain spaces.
- Hook scripts should stick to bash builtins where possible. **Measured in S6:** `jq` is absent in
  this environment; `sed` and `find` are present and work without a `-l` login shell.
- `run-hook.cmd` and every hook script need `chmod +x` for Unix.

**A third Windows fact, measured in S6 — `dotnet format` path handling.** This one is not about
the wrapper; it broke the hook silently and cost the most to find.

- An **absolute project path containing forward slashes** makes `dotnet format` log
  *"Skipping referenced project"* and report *"Formatted 0 of 0 files"*. Since a hook script under
  Git Bash normalises backslashes to forward slashes so its own directory walk works, this is the
  default state, not an edge case.
- **`--include` only ever matches a path relative to the current working directory.** Every
  absolute form — forward slash or backslash — silently matches zero files.
- Both failures exit 0 and print nothing, so the hook looks like it is working.
- **The fix that works:** run from the project's own directory and pass both the project and the
  `--include` path relative to it.

**This is a real, non-trivial cost.** It is an input to the S4 hook triage decision, not an
afterthought.

## 7. Release hygiene (post-v1)

Semantic versioning in `plugin.json`; changes recorded in `CHANGELOG.md`; a git tag per release
(`git tag v0.2.0`). Not required while the plugin is personal and local, but the version field
should still be bumped whenever skills change materially — it is the only signal that an
installed copy is stale.
