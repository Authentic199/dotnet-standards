# dotnet-standards on Codex

Codex installs this plugin's **skills** and only its skills. Everything else the
plugin ships — hooks, the six specialist agents, the two flow commands — has a
Codex equivalent, but Codex reads each one from a location outside the plugin.
`install.sh` puts them there.

```bash
codex plugin marketplace add Authentic199/dotnet-standards
codex plugin add dotnet-standards@dotnet-standards-dev
bash codex/install.sh          # from a checkout of this repo
```

## Why anything has to be installed separately

| Component | Claude Code | Codex |
|---|---|---|
| Skills | plugin `skills/` | plugin `skills/` — **the same directory, nothing copied** |
| Hooks | plugin `hooks/hooks.json` | `~/.codex/hooks.json` or `<repo>/.codex/hooks.json`. Plugin-bundled hooks were a feature (`plugin_hooks`) and it is **removed** — `codex features list` shows it, and the plugin manifest validator rejects a `hooks` field outright. |
| Agents | plugin `agents/*.md` | `~/.codex/agents/*.toml` or `<repo>/.codex/agents/*.toml` |
| Commands | plugin `commands/*.md` | `~/.codex/prompts/*.md` — user scope only, top level only (Codex ignores subdirectories) |

`agents/*.md` and `commands/*.md` remain the single source of truth.
`sync-from-plugin.py` projects them into the two shapes above; `--check` fails
when they have drifted, which is the shape to run after editing either source.

## What was measured, and what was not

Measured on 2026-08-02 against Codex CLI 0.144.1, in a directory holding a
`.csproj`:

- **Hooks fire.** A `codex exec` turn created this plugin's own session marker
  and the router text reached the model — found in the session rollout.
- **One Windows fact cost the first attempt, and it is the reason
  `commandWindows` is on every entry: Codex launches a hook command without a
  shell**, so a bare `.cmd` path is not executable and every hook silently never
  ran. `cmd /c <path> <arg>` runs. A silent no-op is exactly what a wrong
  command looks like here — nothing reports it.
- **All 26 skills are visible to the model** as `dotnet-standards:<skill>`, with
  their descriptions, confirmed through `codex debug prompt-input`.

Not verified, and stated as such:

- **The custom agents.** They are installed in the documented location and their
  TOML parses, but `codex exec` exposes no spawn tool at all, so nothing could be
  observed from a headless run — and upstream reports the same names resolving
  differently across Codex surfaces. Check it in an interactive session before
  relying on the fleet. `dotnet-review-flow` preflight #3 already treats "all six
  absent" as a harness fact and falls back to four sequential lenses, so the flow
  degrades honestly rather than pretending it ran a fleet.
- **The two prompts in the slash menu.** They are files in the documented
  directory; the menu itself needs an interactive session to see.

## After installing

1. **Start a new session.** Skills, agents, prompts and hooks all load at
   session start.
2. **Run `/hooks` and trust them.** Codex records trust against each hook's
   hash and skips untrusted hooks **silently** — an untrusted hook is
   indistinguishable from a broken one.
3. Re-run `bash codex/install.sh` after moving this checkout: the installed
   hook commands carry its absolute path.

`bash codex/install.sh --uninstall` removes exactly what was installed, leaving
any other tool's entries in `hooks.json` untouched.
