# Hooks

`dotnet-standards` ships **exactly two hooks**: `post-edit-format` and
`superpowers-check`.

That is not an accident of scope. Nine hook components were triaged against
Superpowers and against the Windows failure mode described below; eight were
refused. `superpowers-check` was added later (Lane D, spec
`docs/superpowers/specs/2026-07-27-process-integration-design.md` §5) and was
admitted only because it passes the same test the eight failed. If a future
session thinks another hook is needed, read
[Why only these hooks](#why-only-these-hooks) first.

---

## The three kinds of hook

Borrowed from the reference kit's own hook documentation, because it is the
distinction that makes "does this collide with another plugin?" answerable at
all:

| Kind | Declared where | Runs when |
|---|---|---|
| **Claude Code hook** | `hooks/hooks.json` in a plugin | Claude Code fires a lifecycle event (`PreToolUse`, `PostToolUse`, `SessionStart`, …) |
| **Git hook** | `.git/hooks/*`, installed by hand per clone | git runs (`pre-commit`, `pre-push`, …) |
| **Utility script** | Nowhere — invoked by a workflow or piped by hand | A human or a skill runs it |

Only the first kind can collide with another plugin's hooks. **`dotnet-standards`
ships two hooks of the first kind and zero of the other two.**

---

## The two hooks

**`post-edit-format`** — after Claude edits or writes a file, format it.

| | |
|---|---|
| Event | `PostToolUse` |
| Matcher | `Edit\|Write` |
| Command | `"${CLAUDE_PLUGIN_ROOT}/hooks/run-hook.cmd" post-edit-format` |
| Mode | synchronous (`"async": false`) |

What it does, in order:

1. Takes the edited file path from `$1`, then `CLAUDE_EDITED_FILE`, then the
   `PostToolUse` stdin JSON (`.tool_input.file_path`) — `jq` if available, a
   `sed` extraction if not.
2. Exits 0 immediately unless the path ends in `.cs` and the file still exists.
3. Normalises Windows backslashes, then walks **up** from the file's directory
   looking for the nearest `*.csproj`, falling back to `*.sln`.
4. Runs `dotnet format <project> --include <file> --no-restore` **from the
   project's own directory, with both paths relative to it**, swallowing all
   output and all failures.

**Scoping to the nearest project is what makes this affordable.** Formatting the
whole solution after every `.cs` edit would be a real tax inside a tight
red-green loop; formatting one file inside one project is not. Swallowing
failures is deliberate for the same reason — a formatter must never be able to
interrupt an edit.

### Step 4 is not written the obvious way, and it cannot be

The reference kit invokes `dotnet format "$PROJECT" --include "$FILE"` with
absolute paths. **On Windows that formats nothing at all**, silently, for two
independent reasons — both measured on .NET SDK 10.0.301:

| Symptom | Cause |
|---|---|
| `Skipping referenced project 'X'.` → `Formatted 0 of 0 files.` | The project path is absolute **with forward slashes**. `dotnet format` compares it against MSBuild's backslash form and never matches. |
| `Formatted 0 of 4 files.` | `--include` only ever matches a path **relative to the current working directory**. Every absolute form, forward slash or backslash, matches zero files. |

The first is not an edge case: step 3 normalises backslashes to forward slashes
precisely so the bash directory walk works, which guarantees the broken form.

Both failures exit 0 and print nothing. Combined with the wrapper's silent
exit-0 below, that gives **two** independent ways for this hook to appear to work
while doing nothing — which is why the only acceptable proof is a before/after
comparison of the file on disk.

The fix is to `cd` into the project directory and pass both the project file and
the `--include` path relative to it. That is what the script does.

**`superpowers-check`** — at session start, warn if Superpowers is missing.

| | |
|---|---|
| Event | `SessionStart` |
| Command | `"${CLAUDE_PLUGIN_ROOT}/hooks/run-hook.cmd" superpowers-check` |
| Mode | synchronous (`"async": false`) |

Reads `${CLAUDE_CONFIG_DIR:-$HOME/.claude}/plugins/installed_plugins.json`; if
no `superpowers@…` entry exists, emits a `systemMessage` warning (with the
exact install command and a restart reminder) plus `additionalContext` telling
Claude the flow skills will hard-stop at their PHASE 0 preflight. **Warn only —
it always exits 0 and never blocks a session**: knowledge-only sessions without
Superpowers are legitimate (spec §5.1).

Why it passes the rule below that killed the guard candidates: its silent
absence is benign **by design**, because it is only the early warning — the
enforcement lives in each flow skill's PHASE 0 hard-stop, which runs with or
without this hook. A machine with no bash loses the courtesy warning and
nothing else.

Nothing else in this plugin registers a Claude Code event.

---

## Windows: `run-hook.cmd` and its cost

The author's environment is Windows. Three facts follow, and all three were
measured rather than assumed.

### 1. Hook scripts are extensionless

Claude Code on Windows **prepends `bash` to any command containing `.sh`**. A
command like `"…/run-hook.cmd" format.sh` therefore becomes
`bash "…/run-hook.cmd" format.sh`, and the reference kit's own
`bash "${CLAUDE_PLUGIN_ROOT}/hooks/post-edit-format.sh"` form double-invokes.

So the script is named `post-edit-format`, with **no extension**, and is passed
to the wrapper as `post-edit-format`. Any script added here must follow the same
rule.

### 2. Everything goes through the polyglot wrapper

`hooks/run-hook.cmd` is valid in both `cmd.exe` and POSIX `sh`. Under `cmd.exe`
the `: << 'CMDBLOCK'` line reads as a label, the batch half runs, and `exit /b`
stops before the Unix half; under `sh` the `:` is a no-op and the batch half is
swallowed as a quoted heredoc.

On Windows it searches for bash in this order:

1. `C:\Program Files\Git\bin\bash.exe`
2. `C:\Program Files (x86)\Git\bin\bash.exe`
3. whatever `where bash` finds (MSYS2, Cygwin, a user-installed Git Bash)

**Git for Windows is therefore a hard dependency of this hook.** Without a bash
on the machine the hook cannot run at all.

The wrapper is a **copy** of the pattern used by Superpowers, not a reference to
it. Pointing at another plugin's internal path would couple the two plugins and
break on the next update. That copy is why `NOTICE` carries a second MIT
attribution.

### 3. When bash is missing, the wrapper exits 0 in silence — on purpose

This is the important line in this file.

If no bash is found, `run-hook.cmd` runs `exit /b 0`. Claude Code sees a clean
exit, the hook never ran, and **nothing anywhere reports that it did not run**.

That behaviour was chosen deliberately, over the alternative of failing loudly,
for one reason: **a hook must never be able to break an edit.** A wrapper that
returned a non-zero exit code on a machine without Git Bash would turn every
single `Edit` and `Write` into a reported hook failure, on a plugin whose entire
value is knowledge rather than tooling. Silence is the correct trade *for this
hook* — the code is still correct, merely unformatted, and
`dotnet format --verify-no-changes` catches the drift before review.

**The rule this creates, and it binds every future hook:**

> A hook may be shipped through `run-hook.cmd` **only if its silent absence is
> benign by design.**

`post-edit-format` passes: unformatted-but-correct code is a cosmetic loss with a
later net to catch it.

A guard hook fails: a `pre-bash-guard` that silently stops guarding **fails
open**. The user keeps believing destructive commands are being blocked while
nothing is blocking them, which is strictly worse than never having installed a
guard — it invites the reliance it cannot support. That asymmetry, not the effort
of writing the wrapper, is why this plugin ships one hook and no guards.

**How to tell whether the hook is actually running.** Because the failure is
silent by design, verify it by observation, not by inspection: edit a `.cs` file
with deliberately bad formatting in a project that has a `.csproj`, and check
whether the file changes on disk. If it does not, run
`hooks/run-hook.cmd post-edit-format <path-to-file>` by hand — that separates
"bash is missing" from "the hook is not registered".

---

## Why only these hooks

The other eight candidates, and why each was refused:

| Candidate | Verdict |
|---|---|
| `pre-bash-guard` | **Refused.** Fails open under the silent exit-0 above; the permission gate already interposes on every Bash call. |
| `post-scaffold-restore` | **Refused.** A synchronous whole-solution `dotnet restore` on every `.csproj` write, for a restore `dotnet build` and `dotnet add package` already perform. |
| `post-test-analyze` | **Refused.** A shell summariser of `dotnet test` output the model already reads in full, with more context. |
| `pre-build-validate` | **Refused as a script.** Its six solution-hygiene checks survive as a checklist Claude performs natively; the script form buys nothing. |
| `pre-commit-antipattern` | **Refused as a gate.** Its four detection patterns survive as knowledge; the blocking form told the user to bypass it with `--no-verify`. |
| `pre-commit-format` | **Refused.** A third layer on a concern `post-edit-format` prevents and `dotnet format --verify-no-changes` verifies. |
| `UserPromptSubmit` skill index | **Refused.** Fires on every prompt — a permanent per-turn token tax against a routing problem solved at zero runtime cost by skill-description discipline. |
| The kit's `hooks.json` as shipped | **Rebuilt, not carried.** Its command form is unusable on Windows and it registers two scripts this plugin does not ship. |

**Adding a hook is not a small change.** It costs a per-event tax on every
matching tool call, and it inherits the silent-failure mode above. The test is
not "is this useful?" but "if this silently never runs, is the user still safe?"

---

## Files

| File | Purpose |
|---|---|
| `hooks.json` | The manifest. Auto-loaded by Claude Code — it must **not** also be declared under `plugin.json`'s `manifest.hooks`, which raises *"Duplicate hooks file detected"*. |
| `run-hook.cmd` | The polyglot CMD/POSIX wrapper. Copied from Superpowers (MIT — see `NOTICE`). |
| `post-edit-format` | The formatting hook. Extensionless. Derived from the reference kit (MIT — see `NOTICE`). |
| `superpowers-check` | The dependency warning hook. Extensionless. Warn-only by design (spec §5). |
