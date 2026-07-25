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
│   └── *.sh                          actual hook logic
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

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Write|Edit",
        "hooks": [
          {
            "type": "command",
            "command": "\"${CLAUDE_PLUGIN_ROOT}/hooks/run-hook.cmd\" format.sh"
          }
        ]
      }
    ]
  }
}
```

Available events: `PreToolUse`, `PostToolUse`, `UserPromptSubmit`, `SessionStart`, `SessionEnd`,
`Stop`, `SubagentStop`, `PreCompact`, `Notification`.

## 6. Windows: the polyglot hook wrapper

The user's environment is Windows 11. Claude Code invokes hooks through **`CMD.exe`**, which
cannot execute `.sh` files — it tries to open them in a text editor. The kit's eight hooks are
all `.sh`.

The fix is a single reusable polyglot wrapper, valid in both CMD and POSIX sh:

```cmd
: << 'CMDBLOCK'
@echo off
REM Polyglot wrapper: runs .sh scripts cross-platform
REM Usage: run-hook.cmd <script-name> [args...]
"C:\Program Files\Git\bin\bash.exe" -l "%~dp0%~1"
exit /b
CMDBLOCK

# Unix shell runs from here
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SCRIPT_NAME="$1"
shift
"${SCRIPT_DIR}/${SCRIPT_NAME}" "$@"
```

- CMD reads `:` as a label and ignores the rest of the line, runs `bash.exe`, then `exit /b`
  stops it before the Unix half.
- POSIX sh treats `:` as a no-op and swallows the CMD block as a heredoc.
- `$0` is used instead of `${BASH_SOURCE[0]}` — the latter fails on systems where `/bin/sh` is
  dash.

**Requirements and consequences**
- **Git for Windows must be installed**, at `C:\Program Files\Git\bin\bash.exe`. A different
  install location requires editing the wrapper.
- Quote `${CLAUDE_PLUGIN_ROOT}` in `hooks.json` — on Windows it can contain spaces.
- Hook scripts should stick to bash builtins; external tools (`sed`, `awk`, `grep`) exist in
  Git Bash but require the `-l` login shell for a correct PATH.
- `run-hook.cmd` needs `chmod +x` for Unix.

**This is a real, non-trivial cost.** It is an input to the S4 hook triage decision, not an
afterthought.

## 7. Release hygiene (post-v1)

Semantic versioning in `plugin.json`; changes recorded in `CHANGELOG.md`; a git tag per release
(`git tag v0.2.0`). Not required while the plugin is personal and local, but the version field
should still be bumped whenever skills change materially — it is the only signal that an
installed copy is stale.
