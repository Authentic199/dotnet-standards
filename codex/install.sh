#!/usr/bin/env bash
#
# Install the parts of dotnet-standards that a Codex plugin cannot carry.
#
# Codex ships `skills/` from the plugin and nothing else: `plugin_hooks` is a
# removed feature flag, and custom agents and prompts are only ever read from
# the two locations below. So the hooks, the six specialist agents and the two
# flow prompts are installed *beside* the plugin rather than inside it.
#
#   hooks    -> $CODEX_HOME/hooks.json      (merged, never clobbered)
#   agents   -> $CODEX_HOME/agents/*.toml
#   prompts  -> $CODEX_HOME/prompts/*.md    (top level only — Codex ignores subdirs)
#
# Usage:
#   bash codex/install.sh                 # user scope: ~/.codex (or $CODEX_HOME)
#   bash codex/install.sh --project PATH  # repo scope: PATH/.codex (agents + hooks only)
#   bash codex/install.sh --dry-run       # print what would change, touch nothing
#   bash codex/install.sh --uninstall     # remove exactly what this script added
#
# Requires: bash, python3. Re-runnable: every step is idempotent.

set -euo pipefail

PLUGIN_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CODEX_HOME="${CODEX_HOME:-$HOME/.codex}"
TARGET=""
PROJECT_MODE=0
DRY_RUN=0
UNINSTALL=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --project)  PROJECT_MODE=1; TARGET="$(cd "$2" && pwd)/.codex"; shift 2 ;;
    --dry-run)  DRY_RUN=1; shift ;;
    --uninstall) UNINSTALL=1; shift ;;
    -h|--help)  sed -n '2,25p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *)          echo "Unknown arg: $1" >&2; exit 2 ;;
  esac
done

[[ -n "$TARGET" ]] || TARGET="$CODEX_HOME"

# On Windows a Python install commonly provides `python` and no `python3`.
PY_BIN=""
for candidate in python3 python py; do
  if command -v "$candidate" >/dev/null 2>&1 && "$candidate" -c "import sys; sys.exit(0 if sys.version_info[0] == 3 else 1)" >/dev/null 2>&1; then
    PY_BIN="$candidate"
    break
  fi
done
[[ -n "$PY_BIN" ]] || { echo "ERROR: no Python 3 on PATH (tried python3, python, py)" >&2; exit 1; }

say() { echo "  $*"; }
run() { [[ $DRY_RUN -eq 1 ]] && return 0; "$@"; }

echo "dotnet-standards -> Codex"
echo "  plugin : $PLUGIN_ROOT"
echo "  target : $TARGET"
[[ $DRY_RUN -eq 1 ]] && echo "  MODE   : dry run, nothing is written"
echo

# ---------------------------------------------------------------------------
# Agents
# ---------------------------------------------------------------------------
if [[ $UNINSTALL -eq 1 ]]; then
  for src in "$PLUGIN_ROOT"/codex/agents/*.toml; do
    dest="$TARGET/agents/$(basename "$src")"
    [[ -f "$dest" ]] && { say "remove agent  $dest"; run rm -f "$dest"; }
  done
else
  run mkdir -p "$TARGET/agents"
  for src in "$PLUGIN_ROOT"/codex/agents/*.toml; do
    dest="$TARGET/agents/$(basename "$src")"
    say "agent   $(basename "$src")"
    run cp "$src" "$dest"
  done
fi

# ---------------------------------------------------------------------------
# Prompts — user scope only. Codex reads $CODEX_HOME/prompts, never a repo's.
# ---------------------------------------------------------------------------
if [[ $PROJECT_MODE -eq 1 ]]; then
  echo
  say "prompts skipped — Codex reads custom prompts only from \$CODEX_HOME/prompts;"
  say "run this script without --project to get /dotnet-feature and /dotnet-review."
else
  if [[ $UNINSTALL -eq 1 ]]; then
    for src in "$PLUGIN_ROOT"/codex/prompts/*.md; do
      dest="$CODEX_HOME/prompts/$(basename "$src")"
      [[ -f "$dest" ]] && { say "remove prompt $dest"; run rm -f "$dest"; }
    done
  else
    run mkdir -p "$CODEX_HOME/prompts"
    for src in "$PLUGIN_ROOT"/codex/prompts/*.md; do
      say "prompt  /$(basename "$src" .md)"
      run cp "$src" "$CODEX_HOME/prompts/$(basename "$src")"
    done
  fi
fi

# ---------------------------------------------------------------------------
# Hooks — merged into an existing hooks.json, never written over one.
#
# Entries this script owns are recognised by their command path, so uninstall
# removes exactly what was added and a re-run replaces it rather than appending
# a duplicate. Another tool's hooks in the same file are left untouched.
# ---------------------------------------------------------------------------
echo
HOOKS_TEMPLATE="$PLUGIN_ROOT/codex/hooks.json"
HOOKS_TARGET="$TARGET/hooks.json"

PLUGIN_ROOT="$PLUGIN_ROOT" HOOKS_TEMPLATE="$HOOKS_TEMPLATE" HOOKS_TARGET="$HOOKS_TARGET" \
DRY_RUN="$DRY_RUN" UNINSTALL="$UNINSTALL" "$PY_BIN" <<'PY'
import json, os, pathlib

TAG = "dotnet-standards"
plugin_root = os.environ["PLUGIN_ROOT"]
# Windows form for `commandWindows`: Codex runs hook commands without a shell,
# so the wrapper is launched through `cmd /c` with a native path.
plugin_root_win = plugin_root
if plugin_root_win.startswith("/") and len(plugin_root_win) > 2 and plugin_root_win[2] == "/":
    plugin_root_win = plugin_root_win[1] + ":" + plugin_root_win[2:]
plugin_root_win = plugin_root_win.replace("/", "\\")


def json_escape(value):
    """The replacement lands inside an already-encoded JSON string."""
    return json.dumps(value)[1:-1]


template_path = pathlib.Path(os.environ["HOOKS_TEMPLATE"])
target_path = pathlib.Path(os.environ["HOOKS_TARGET"])
dry_run = os.environ["DRY_RUN"] == "1"
uninstall = os.environ["UNINSTALL"] == "1"

template = json.loads(template_path.read_text(encoding="utf-8"))
existing = {}
if target_path.is_file():
    try:
        existing = json.loads(target_path.read_text(encoding="utf-8"))
    except json.JSONDecodeError:
        raise SystemExit(f"ERROR: {target_path} is not valid JSON — fix or move it first")

hooks = existing.setdefault("hooks", {})


def is_ours(entry):
    # Identified by the command itself, not by an ownership field: Codex's hook
    # schema is fixed, and an unknown key risks the whole file being rejected.
    # Both tokens are required — Superpowers ships a run-hook.cmd of its own.
    return any(
        TAG in h.get("command", "") and "run-hook.cmd" in h.get("command", "")
        for h in entry.get("hooks", [])
    )


removed = added = 0
for event, groups in list(hooks.items()):
    kept = [g for g in groups if not is_ours(g)]
    removed += len(groups) - len(kept)
    if kept:
        hooks[event] = kept
    else:
        del hooks[event]

if not uninstall:
    for event, groups in template["hooks"].items():
        for group in groups:
            raw = json.dumps(group)
            raw = raw.replace("__PLUGIN_ROOT_WIN__", json_escape(plugin_root_win))
            raw = raw.replace("__PLUGIN_ROOT__", json_escape(plugin_root))
            group = json.loads(raw)
            hooks.setdefault(event, []).append(group)
            added += 1

if not hooks:
    existing.pop("hooks", None)

print(f"  hooks   {added} installed, {removed} previous entr{'y' if removed == 1 else 'ies'} replaced")
print(f"  file    {target_path}")

if not dry_run:
    target_path.parent.mkdir(parents=True, exist_ok=True)
    target_path.write_text(json.dumps(existing, indent=2) + "\n", encoding="utf-8")
PY

echo
if [[ $UNINSTALL -eq 1 ]]; then
  echo "Removed. Start a new Codex session for it to take effect."
  exit 0
fi

cat <<'NEXT'
Done. Three things now, in this order:

  1. Install the plugin itself, if you have not:
       codex plugin marketplace add Authentic199/dotnet-standards
       codex plugin add dotnet-standards@dotnet-standards-dev
  2. Start a NEW Codex session — agents, prompts and hooks all load at start.
  3. Run /hooks in that session and TRUST these hooks. Codex records trust
     against each hook's hash and skips untrusted ones silently, so an
     untrusted hook looks exactly like a broken one.

Then check: /dotnet-review should appear in the slash menu, and asking Codex to
"spawn the dotnet-code-reviewer agent" should resolve the name.
NEXT
