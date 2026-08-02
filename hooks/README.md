# Hooks

`dotnet-standards` ships **exactly six hooks**: `post-edit-format`,
`superpowers-check`, `router-nudge`, `test-report-nudge`, `fleet-nudge` and
`process-handback`.

That is not an accident of scope. Nine hook components were triaged against
Superpowers and against the Windows failure mode described below; eight were
refused. `superpowers-check` was added later (Lane D, spec
`docs/superpowers/specs/2026-07-27-process-integration-design.md` §5) and was
admitted only because it passes the same test the eight failed. `router-nudge`
(0.3.27) is one of the original eight, **readmitted because the reason it was
refused was later falsified by observation**. `test-report-nudge` (0.3.44)
descends from another of the eight — `post-test-analyze` — **reshaped so the
refusal's reason keeps holding**: the script summarizes nothing; it nudges the
model to write the report a human reads. `fleet-nudge` and `process-handback`
(2026-08-02) are **the first two hooks admitted on `PreToolUse`**, and they were
admitted for a reason no earlier candidate could claim: the failure they answer
happens at a moment no prompt-level or session-level hook can reach — see their
entries below. The table rows in
[Why only these hooks](#why-only-these-hooks) carry both verdicts and the
evidence between them. If a future session thinks another hook is needed, read
that section first.

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
ships six hooks of the first kind and zero of the other two.**

**All six are Claude Code only.** Codex's plugin manifest accepts `skills`,
`apps`, `mcpServers` and `interface` — `hooks` is rejected outright by its
validator — so a Codex install of this plugin runs none of them. Nothing here
degrades gracefully into that harness: the compensations are written where the
loss lands, in `choosing-a-dotnet-skill` (*When the harness is not Claude Code*)
and in `dotnet-review-flow`'s preflight #3. Do not design a hook whose rule
exists nowhere else.

---

## The six hooks

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

**`router-nudge`** — on the first prompt of a session in a .NET repository, name
the router.

| | |
|---|---|
| Event | `UserPromptSubmit` |
| Command | `"${CLAUDE_PLUGIN_ROOT}/hooks/run-hook.cmd" router-nudge` |
| Mode | synchronous (`"async": false`) |

Reads `session_id` and `cwd` from the `UserPromptSubmit` stdin JSON and emits one
`additionalContext` line pointing at `dotnet-standards:choosing-a-dotnet-skill`,
then stays silent for the rest of that session. Naming a concrete **table
destination** here would make a hook script a second source of truth for routing
the day the router's tables change, so it names none.

**Amended 2026-08-02: the emit also names `/dotnet-feature` and
`/dotnet-review`.** That reasoning holds for table rows and still does — it does
not hold for the one choice the tables cannot express. A row routes a question to
a skill; the 2026-08-02 field failure happened a level above that, in a session
that never chose a process at all and hand-assembled one from another plugin
instead. The commands are this plugin's own entry points and live in this
repository, so they cannot drift out from under the hook the way a table row can.

Two gates, in this order, both must pass:

1. **`cwd` looks like a .NET solution** — a `*.sln`, `*.slnx` or `*.csproj` at
   the root, or a `*.csproj` at depth 2 or 3. Globs, never `find`: this runs on
   every prompt, and a recursive walk of an arbitrary repository is exactly the
   per-turn tax the refusal below was right about.
2. **This session has not been told yet** — a marker under
   `${TMPDIR:-/tmp}/dotnet-standards/` keyed by `session_id`, swept after seven
   days. Emitted context persists in the conversation, so repeating the line
   every turn would buy nothing and cost every turn.

A missing `session_id`, an unwritable temp directory, or a solution nested
deeper than the cap each mean **no output**. Under-firing is the safe direction
and the script prefers it at every branch.

Why it passes the rule below: this hook **guards nothing**. If it silently never
runs, the session is exactly the session that shipped before 0.3.27 — every
skill still reachable by name, every command by slash. That is the whole
difference between it and the guard candidates, whose silent absence invites a
reliance they cannot support.

**Why the pointer hangs off the prompt and not off `SessionStart`.** Because a
session-start pointer was measured failing. In the 2026-07-29 observation that
prompted this hook, Superpowers' own emphatic `SessionStart` block was present
and was ignored on turn 1, while this plugin sat installed and enabled with all
its skill descriptions loaded. Adjacency to the request is the only thing this
hook adds over a slot that was already available and already occupied.

**`test-report-nudge`** — after a `dotnet test` run, have the model keep a
human-readable test report current.

| | |
|---|---|
| Event | `PostToolUse` |
| Matcher | `Bash` |
| Command | `"${CLAUDE_PLUGIN_ROOT}/hooks/run-hook.cmd" test-report-nudge` |
| Mode | synchronous (`"async": false`) |

Extracts `.tool_input.command` from the `PostToolUse` stdin JSON (`jq` if
available, a sed extraction if not); exits 0 unless it contains a
`dotnet test` invocation. On the first match **of each context** — a marker under
`${TMPDIR:-/tmp}/dotnet-standards/` keyed by `session_id` **plus `agent_id`**,
swept after seven days — it emits one `additionalContext`
block: a **standing instruction** to write `test-report.md` at the repository
root whenever a test run settles, overwriting the previous version, in the
**user-approved format** (date/time + command + pass/fail/skip totals, one
section per test class, one plain-language line per test case with PASS/FAIL
and a one-line reason on FAIL, written in the language the user is conversing
in). The instruction persists in the conversation, so later runs in the same
session need no re-emit.

**Once per *context*, not once per session — fixed 2026-08-02, and the bug it
fixes had shipped since 0.3.44.** Under Superpowers'
`subagent-driven-development` every implementer is a `general-purpose` subagent
running its own red-green loop, so the first `dotnet test` of a whole run
reliably fires inside a **throwaway subagent context**. Keyed by `session_id`
alone, that context consumed the session's only emit and then vanished — and
every later run, including the coordinating session's own final full-suite run,
got nothing. The standing instruction had been handed to the one context with no
"rest of the session" to apply it to. The payload distinguishes them: `agent_id`
is present only inside a subagent and absent on the main thread (CLI 2.1.220
schema, which says to use that field and not `agent_type` for exactly this).
**`fleet-nudge` and `process-handback` carry the same keying** — the second of
those had the identical defect on the day it shipped, because
`dotnet-feature-flow:210` orders every implementer subagent to load its skills
with the Skill tool.

**And the two contexts are told different things.** Letting a subagent write
`test-report.md` is worse than letting it write nothing: the file is overwritten
per task, so the last implementer to finish leaves a report that names the whole
run and covers one task. A subagent is told to put its plain-language lines in
the report it hands back and leave the file alone; the main thread, which
survives to the end, owns the file and folds those lines in.

**The script parses no test output — deliberately.** The S6 refusal of the
kit's `post-test-analyze` ("a shell summariser of output the model already
reads in full") is still correct, and this hook is shaped around it: turning
`Name_Should_Not_Exceed_200_Characters` into "tên user không vượt quá 200 ký
tự — PASS" in the user's own language is exactly the work a sed script cannot
do and the model can. The report wording inside the emit is a report rule —
changing it needs the user's approval before it ships (ruling 2026-07-28,
reaffirmed 2026-07-31).

Why it passes the rule below: it guards nothing and parses nothing. If it
silently never runs, test output still appears in the conversation in full —
the session is exactly the session that shipped before 0.3.44; the only loss
is the courtesy report file.

**`fleet-nudge`** — when a subagent is spawned in a .NET repository, say once
that this plugin owns the review and test job.

| | |
|---|---|
| Event | `PreToolUse` |
| Matcher | `Task\|Agent` |
| Command | `"${CLAUDE_PLUGIN_ROOT}/hooks/run-hook.cmd" fleet-nudge` |
| Mode | synchronous (`"async": false`) |

Three gates, in this order: **the session marker**, then **the .NET solution
shape**, then **does this spawn look like review or test work** — the payload
matching `review|audit|tester|test|verify`, or `subagent_type` being
`general-purpose`. On a pass it emits one `additionalContext` block naming
`dotnet-review-flow` — and **no agent**, because that flow owns the roster and a
second copy of it here would drift.

**Why the gate order is the reverse of `router-nudge`'s.** This fires per spawn,
not per prompt, so the cheapest check goes first and the .NET verdict is
memoised: the first invocation of a session writes either an `emitted` or a
`not-applicable` marker, and every later invocation is one `test -e`. A session
in a non-.NET repository pays the solution-shape check exactly once. A spawn that
fails only the third gate writes **no** marker — the next spawn may be a review.

**Why a `PreToolUse` hook exists at all, after S6 refused per-call hooks.** On
2026-08-02 a consumer session ran more than twenty subagent review rounds — the
final whole-branch review among them — without loading one of the five review
skills or spawning one of the six agents, so the performance lens was never
applied. `router-nudge` could not have caught it: those rounds ran inside one
autonomous turn of Superpowers' `subagent-driven-development`, so the transition
from writing code to reviewing it was **decided by the model, not typed by the
user**. There was no prompt to hang a nudge on. The spawn is the only moment that
exists, and that is the whole justification for the per-call tax.

**What it deliberately does not do.** The `PreToolUse` schema also carries
`permissionDecision` and `updatedInput`. This hook uses neither. Rewriting
`subagent_type` from a hook would make the transcript disagree with what was
spawned; blocking a spawn would turn a nudge into a gate. Both were refused in
the design (`docs/superpowers/specs/2026-08-02-process-handback-design.md`), and
a hard gate is the *next* escalation only if the field trial shows the nudge
being ignored.

Why it passes the rule below: it guards nothing. If it silently never runs, every
agent is still spawnable and every skill still loadable by name — the session is
exactly the session that shipped before this hook.

**`process-handback`** — when a Superpowers process skill is loaded in a .NET
repository, say once that the two layers compose.

| | |
|---|---|
| Event | `PreToolUse` |
| Matcher | `Skill` |
| Command | `"${CLAUDE_PLUGIN_ROOT}/hooks/run-hook.cmd" process-handback` |
| Mode | synchronous (`"async": false`) |

Same marker mechanism. The middle gate is the skill name: one of
`superpowers:brainstorming`, `writing-plans`, `subagent-driven-development`,
`test-driven-development`, `executing-plans`, `requesting-code-review`. Anything
else exits silently **and memoises nothing**.

It answers the other half of the same field failure: an architecture
specification written from memory during `brainstorming`, whose summary line
reads *"Do NOT invoke any other skill"* while the two fuller statements of that
same ban scope it to **implementation** skills by name. The emit states the
composition — Superpowers owns brainstorming, planning and TDD; this plugin owns
which convention governs each step and who reviews the result — and it changes no
Superpowers file, because none may be changed and a marketplace update would
erase the change anyway.

Why it passes the rule below: it guards nothing and forbids nothing. Silent
absence returns the session to exactly what shipped before it.

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

`router-nudge` passes more cheaply still: it guards nothing, so its silence
hands back exactly the behaviour that shipped before it existed.

A guard hook fails: a `pre-bash-guard` that silently stops guarding **fails
open**. The user keeps believing destructive commands are being blocked while
nothing is blocking them, which is strictly worse than never having installed a
guard — it invites the reliance it cannot support. That asymmetry, not the effort
of writing the wrapper, is why this plugin ships no guards at all.

**How to tell whether the hook is actually running.** Because the failure is
silent by design, verify it by observation, not by inspection: edit a `.cs` file
with deliberately bad formatting in a project that has a `.csproj`, and check
whether the file changes on disk. If it does not, run
`hooks/run-hook.cmd post-edit-format <path-to-file>` by hand — that separates
"bash is missing" from "the hook is not registered".

---

## Why only these hooks

The other eight candidates — plus one later proposal — and why each was refused:

| Candidate | Verdict |
|---|---|
| `pre-bash-guard` | **Refused.** Fails open under the silent exit-0 above; the permission gate already interposes on every Bash call. |
| `post-scaffold-restore` | **Refused.** A synchronous whole-solution `dotnet restore` on every `.csproj` write, for a restore `dotnet build` and `dotnet add package` already perform. |
| `post-test-analyze` | **Refused — a changed form shipped at 0.3.44 as `test-report-nudge`.** The S6 verdict read: *"A shell summariser of `dotnet test` output the model already reads in full, with more context."* That reason was never falsified and the shipped hook preserves it: the script still summarises nothing. What changed is the deliverable — on 2026-07-31 the user asked for a persistent plain-language report **file for a human reader**, which raw test output is not and a shell parser cannot write in the user's language. The shipped form nudges the model to write that file (format user-approved the same day) instead of parsing anything in shell. |
| `pre-build-validate` | **Refused as a script.** Its six solution-hygiene checks survive as a checklist Claude performs natively; the script form buys nothing. |
| `pre-commit-antipattern` | **Refused as a gate.** Its four detection patterns survive as knowledge; the blocking form told the user to bypass it with `--no-verify`. |
| `pre-commit-format` | **Refused.** A third layer on a concern `post-edit-format` prevents and `dotnet format --verify-no-changes` verifies. |
| `UserPromptSubmit` skill index | **Refused in S6 — shipped at 0.3.27 as `router-nudge`.** The S6 verdict read: *"Fires on every prompt — a permanent per-turn token tax against a routing problem solved at zero runtime cost by skill-description discipline."* Observation falsified the premise, not the arithmetic. On 2026-07-29, a session in a consumer .NET repository — this plugin installed and enabled at project scope, every skill description loaded — answered a review request by going straight to `find`, twice, and loaded no skill at all. The token objection was then answered rather than waved off: the shipped hook emits **once per session**, behind a solution-file check, not once per prompt. **Refusing a component is not permanent. Refusing it for a reason that later stops holding is a defect, and correcting it belongs in this file, in the same change that ships the component.** |
| The kit's `hooks.json` as shipped | **Rebuilt, not carried.** Its command form is unusable on Windows and it registers two scripts this plugin does not ship. |
| A `PreToolUse` hook on subagent spawns and on skill loads | **Admitted 2026-08-02 as `fleet-nudge` and `process-handback`** — the first per-tool-call hooks in this plugin, and the bar they had to clear was S6's own: *is the per-call tax worth it, and if the hook silently never runs is the user still safe?* Yes to both, for one reason no earlier candidate could claim. The failure they answer — a consumer session that wrote a specification with no knowledge skill loaded and ran 20+ review rounds with no rubric behind them — happened at moments **no prompt-level or session-level hook can reach**: the write→review transition was decided by the model inside one autonomous `subagent-driven-development` turn, so no `UserPromptSubmit` fired, and `SessionStart` injection has already been measured being ignored on turn 1 (0.3.27). The tax was then answered rather than waved off: the session marker is checked **first**, the .NET verdict is memoised on the first call either way, and every later call in the session is one `test -e`. Both guard nothing, forbid nothing, and use neither `permissionDecision` nor `updatedInput` — a nudge that rewrites what was spawned makes the transcript lie. |
| `ponytail` as a third plugin — an ambient simplicity ruleset riding `SessionStart` + `UserPromptSubmit` | **Refused 2026-07-29 — distilled into house pieces instead.** Two independent grounds, either sufficient: (1) its delivery mechanism is ambient session-start injection, the channel this repository has already measured being ignored on turn 1 (the observation behind `router-nudge`, CHANGELOG 0.3.27); (2) a generic YAGNI voice cannot distinguish sanctioned structure — the module file family, thin envelopes, Facades-axis infrastructure built ahead of need (a user ruling, 2026-07-29) — from slop, and it ships no repo-level exception mechanism to be taught the difference. What transfers shipped as house components: `dotnet-code-review` rubric area 7, `dotnet-feature-flow`'s PHASE 2 ladder and cleanup offer, and static rule R24 (`claude-md-builder`). **The refusal stops holding only if BOTH become true:** a measurement *in this environment* shows session-start injection heeded on turn 1, **and** ponytail or a successor ships a repo-level exception mechanism able to express "structure mandated by a shipped skill is exempt". Decision record: `docs/superpowers/specs/2026-07-29-write-simple-code-ownership-design.md` §3–§4. |

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
| `router-nudge` | The routing pointer hook. Extensionless. Once per session, .NET repositories only (0.3.27). |
| `test-report-nudge` | The test-report instruction hook. Extensionless. Once per session, `dotnet test` Bash calls only (0.3.44). |
| `fleet-nudge` | The review-fleet pointer hook. Extensionless. Once per session, review- or test-shaped subagent spawns in .NET repositories only (2026-08-02). |
| `process-handback` | The plugin-composition hook. Extensionless. Once per session, Superpowers process-skill loads in .NET repositories only (2026-08-02). |
