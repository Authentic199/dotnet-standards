# Process handback — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A session working in a .NET repository cannot run a whole feature or a whole review through Superpowers alone without this plugin's layers being named at the moment each choice is made.

**Architecture:** Three layers, none of which touches a Superpowers file. Layer 1 — four self-gating static rules in every generated consumer `CLAUDE.md`, which Superpowers' own precedence rule ranks above process skills. Layer 2 — two new `PreToolUse` hooks that inject context at the two moments the failures happened: a subagent spawn, and a Superpowers process skill being loaded. Layer 3 — description and body text on this side so the flows are findable by the words people actually type.

**Tech Stack:** Markdown skill files and extensionless bash hook scripts in a Claude Code plugin. No compiled code. Verification is by `git diff`, `grep`, line counts, synthetic hook payloads on stdin, plugin validation, and a real install check.

**Spec:** `docs/superpowers/specs/2026-08-02-process-handback-design.md`

## Global Constraints

- **Artifact language is English.** Talk to the user in Vietnamese; every file written is English.
- **No Superpowers file is read-modify-written, patched, or copied.** Reading its cache for evidence is fine; changing anything under `plugins/cache/claude-plugins-official/superpowers/` is a hard stop. A marketplace update erases local edits, so a design resting on one silently expires.
- **Two wordings need the user's explicit approval before they ship:** the emit text of both new hooks, and the final text of R28–R31. Both are report-rule-class changes. Show the wording, get a yes, then write it.
- **Description law binds** (`docs/02-repo-structure.md` §5): third person, **under 100 words**, trigger-noun pushy, `Not for:` naming every owning sibling from the shipped roster. **Both flow descriptions are already at 90 words** — every word added must be paid for by a word cut. Measure with `sed -n '/^description:/,/^---/p' <file> | sed '$d' | wc -w`.
- **Hook doctrine, from `hooks/README.md`, applies unchanged to both new hooks:** extensionless script through `run-hook.cmd`; emit **once per session**; gated to .NET solutions; silent absence benign — *"if this silently never runs, is the user still safe?"* must answer yes; and the script names **no destination another file already owns** (`dotnet-review-flow` owns its agent roster; do not repeat the six names in a hook).
- **`jq` is not installed in this environment.** Every field extraction needs the `sed` fallback path, and that is the path that actually runs. Copy the `json_string_field` helper from `hooks/router-nudge` rather than inventing a second one.
- **Gate order differs from `router-nudge`'s** and this is deliberate: these hooks fire per tool call, so the session marker is checked **first**, and the first invocation memoises the .NET verdict as either an `emitted` or a `not-applicable` marker.
- **`updatedInput` is not used.** No hook rewrites `subagent_type`. Refused in the spec with reasons; do not reintroduce it as a convenience.
- **Counting lines in PowerShell:** `(Get-Content <file>).Count`. `Measure-Object -Line` skips blank lines and under-reports.
- **Version is read off `main` at merge time, never off the branch.** Two same-number collisions have already shipped from parallel sessions. `main` is at 0.3.61 as this plan is written; the shipping number is whatever `main` says at merge.
- **Commit style:** end every commit message with `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>`. Multi-line messages go through a file: write to the scratchpad and `git commit -F <path>`. PowerShell here-strings mangle these — do not use them.
- **Working directory:** the worktree `D:\AI-PLUGIN\dotnet-standards-laned-handback`, branch `lane-d/process-handback`, created off `main`. Merge `main` into the branch **before** touching `hooks/hooks.json` or any shared file.

---

## File Structure

| File | Responsibility | Change |
|---|---|---|
| `skills/claude-md-builder/references/static-rules.md` | The rule catalogue | New group *Process ownership*, R28–R31 |
| `skills/claude-md-builder/references/template.md` | Section order and budgets | Where the new group lands in §8 |
| `skills/claude-md-builder/references/checklist.md` | Trim guard | New group added to *Never cut* |
| `hooks/fleet-nudge` | **New** | `PreToolUse`, matcher `Task\|Agent` |
| `hooks/process-handback` | **New** | `PreToolUse`, matcher `Skill` |
| `hooks/hooks.json` | Hook manifest | Two registrations |
| `hooks/router-nudge` | Existing nudge | One sentence in the emit; header doctrine amendment |
| `hooks/README.md` | Hook doctrine | Inventory 4 → 6; two rows in *Why only these hooks*; the `router-nudge` amendment recorded |
| `skills/choosing-a-dotnet-skill/SKILL.md` | The router | New section + one table row |
| `skills/dotnet-feature-flow/SKILL.md` | Feature flow | Description only |
| `skills/dotnet-review-flow/SKILL.md` | Review flow | Description only |
| `docs/field-reports/2026-08-02-skill-routing-failure.md` | Field evidence | Moved from the repository root |
| `CHANGELOG.md`, `.claude-plugin/plugin.json`, `.claude-plugin/marketplace.json`, `docs/next-session-prompt.md` | Release record | Per the close protocol |

---

### Task 1: File the field report where evidence lives

**Files:**
- Move: `2026-08-02-skill-routing-failure-postmortem.md` → `docs/field-reports/2026-08-02-skill-routing-failure.md`

**Step 1**
- [ ] `git mv` is wrong here — the file is untracked. Create `docs/field-reports/`, move the file, and `git add` it at the branch's first commit.
- [ ] Add a two-line header stating it is a consumer-session report, quoted against Superpowers 6.2.0, and naming the spec that answers it.

**Verification:** the repository root holds no loose report; `docs/field-reports/` holds exactly one file; the spec's *Origin* line resolves.

---

### Task 2: Layer 1 — the four static rules

**Files:**
- Modify: `skills/claude-md-builder/references/static-rules.md` — new group after *Communication and language* (ends at line 168 today)
- Modify: `skills/claude-md-builder/references/template.md` §8 (line 113 today)
- Modify: `skills/claude-md-builder/references/checklist.md` *Never cut* (line 67 today)

**Step 1 — get the wording approved.** Show R28–R31 exactly as the spec states them. Do not proceed on inference; this is one of the two frozen wordings.

**Step 2 — write the group.** Header `## Process ownership`, an **Applies when** line reading *always — self-gating, exactly like R23: the rule states its own condition, so it ships whether or not the scan can detect Superpowers in the repository.* Each rule carries its `*Prevents:*` line citing the 2026-08-02 field failure, and R29 carries the `*Note:*` that it names no agent because `dotnet-review-flow` owns the roster.

**Step 3 — template placement.** In §8's paragraph, state that the `### Process` sub-heading ships **immediately after the hard-constraint block and before every other group**, with its reason: it governs how the rest of the file is read. Four lines against the 55-line budget.

**Step 4 — checklist.** Add one *Never cut* bullet: the `### Process` group. It reads like meta-commentary, so a trimming pass reaches for it first — and it is the one group whose absence is invisible until a whole session has run the wrong way.

**Verification:**
- [ ] `grep -c "^\*\*R2[89]\|^\*\*R3[01]" references/static-rules.md` returns 4.
- [ ] No rule names an agent, a rubric skill, or a table row that lives elsewhere.
- [ ] Update mode still reaches them: `references/static-rules.md` is the file PHASE 3 opens and *Update mode* step 3 re-runs against — confirm no new gating was introduced that a re-run would skip.

---

### Task 3: Layer 3 — the router gains a composition section

**Files:**
- Modify: `skills/choosing-a-dotnet-skill/SKILL.md` (158 lines today) — new section after the planning section (ends line 67); one row in the shared-token table (lines 108–130)

**Step 1** — Add `## Composing with Superpowers process skills`, three bullets, no more than 14 lines total:
- The ban's scope — mirroring R30, with the `brainstorming:13`/`:61` versus `:132` reasoning compressed to one clause.
- The phase re-route rule — mirroring R31: this file is a lookup table, re-read at each phase change.
- The spawn rule — mirroring R29: a review or test subagent is `dotnet-review-flow`'s, not `general-purpose`.

**Step 2** — Add one row to *When two skills both look right*:
`| spawning a subagent | one that reviews or tests .NET code — `dotnet-review-flow` and the agents it names; one that writes code — an ordinary Superpowers subagent whose prompt orders the load of the knowledge skills this table names |`

**Verification:**
- [ ] The section teaches no .NET convention and names no rubric content — the router routes, it does not teach (`SKILL.md:35-37`).
- [ ] Line count stays under 180.
- [ ] The three bullets say the same thing as R28–R31 without contradicting them; diff the wording deliberately.

---

### Task 4: Layer 3 — both flow descriptions

**Files:**
- Modify: `skills/dotnet-feature-flow/SKILL.md` frontmatter (90 words today)
- Modify: `skills/dotnet-review-flow/SKILL.md` frontmatter (90 words today)

**Step 1 — `dotnet-feature-flow`.** Add that it **replaces** hand-assembling Superpowers brainstorming + plan + subagent-driven development for a .NET change, plus the typed phrasings *"execute this plan"*, *"implement the plan with subagents"*. Pay for it by cutting from the phase list — the `Not for:` roster is not cuttable.

**Step 2 — `dotnet-review-flow`.** Add *"final review before merge"* and *"review each task as it lands"*. Pay for it the same way.

**Verification:**
- [ ] Both word counts under 100, measured with the command in Global Constraints.
- [ ] Every shipped sibling that owns a neighbouring area still appears in each `Not for:`.
- [ ] `grep -n "dotnet-feature-flow\|dotnet-review-flow" skills/*/SKILL.md` — no sibling's `Not for:` pointer is invalidated by the rewording.

---

### Task 5: Layer 2 — `hooks/fleet-nudge`

**Files:**
- Create: `hooks/fleet-nudge` (extensionless)

**Step 1 — get the emit wording approved** (frozen wording #2, with Task 6's).

**Step 2 — write the script.** Structure, in this order:
1. Header comment in the house style: why it exists, what it costs, why silent absence is benign, and why it names `dotnet-review-flow` rather than the six agents.
2. Read stdin; empty → exit 0.
3. `json_string_field` helper copied from `router-nudge`; extract `session_id`, `cwd`. Extract `tool_input.prompt`, `tool_input.description`, `tool_input.subagent_type` — falling back to matching the raw payload when extraction fails, which is the benign direction.
4. **Marker check first.** `emitted` or `not-applicable` marker present → exit 0.
5. .NET solution gate — the glob ladder from `router-nudge`, unchanged. Fail → write the `not-applicable` marker, exit 0.
6. Review/test gate: the extracted text matches `review|reviewer|audit|test|tester|verify`, **or** `subagent_type` is `general-purpose`. No match → exit 0 **without** writing a marker (a later spawn may be a review).
7. No `session_id`, or unwritable `TMPDIR` → exit 0.
8. Emit the approved JSON; sweep week-old markers.

**Verification (synthetic payloads, before any commit):**
- [ ] .NET dir + review-shaped prompt → emits once; second identical call → silent.
- [ ] .NET dir + implementation prompt with no keyword → silent, and a later review call still emits.
- [ ] Non-.NET dir → silent, and the second call short-circuits on the `not-applicable` marker (prove it: `stat` the marker).
- [ ] Missing `session_id` → silent, exit 0.
- [ ] `TMPDIR` pointed at an unwritable path → silent, exit 0.
- [ ] Emitted JSON parses (`python -c "import json,sys;json.load(sys.stdin)"` or equivalent).

---

### Task 6: Layer 2 — `hooks/process-handback`

**Files:**
- Create: `hooks/process-handback` (extensionless)

**Step 1** — Same structure as Task 5, with these differences: the gate reads `tool_input.skill` and matches the six in-scope Superpowers process skills; there is no keyword gate; a skill outside the set exits 0 without writing a marker.

**Step 2** — The emit is the approved text from the spec.

**Verification (synthetic payloads):**
- [ ] .NET dir + `superpowers:subagent-driven-development` → emits once.
- [ ] .NET dir + `superpowers:brainstorming` → emits (fresh session marker).
- [ ] `dotnet-standards:mediatr-messaging` → silent, and a later in-scope skill still emits.
- [ ] Non-.NET dir → silent; second call short-circuits.
- [ ] Missing `session_id`, unwritable `TMPDIR` → silent, exit 0.

---

### Task 7: Register the hooks, amend the doctrine

**Files:**
- Modify: `hooks/hooks.json` — a `PreToolUse` block with two matcher entries
- Modify: `hooks/router-nudge` — one sentence in the emit; header amendment
- Modify: `hooks/README.md` — three places

**Step 1 — `hooks.json`.** Add `PreToolUse` with matcher `Task|Agent` → `fleet-nudge` and matcher `Skill` → `process-handback`, in the same command form as the existing four (`run-hook.cmd`, `shell: bash`, `async: false`).

**Step 2 — `router-nudge` emit.** One added sentence: when the whole task is a .NET feature or a .NET review, the entry points are `/dotnet-feature` and `/dotnet-review`. Nothing else changes.

**Step 3 — `router-nudge` header.** Amend `IT NAMES THE ROUTER AND NOTHING ELSE` per the spec: the reasoning holds for table rows and still does; it does not hold for the choice the router cannot express — a process for the whole session. Record it as an amendment with its date and reason, in the 0.3.27 style.

**Step 4 — `README.md`.** Update *The four hooks* → six; add both to the *Files* table; and add two rows to *Why only these hooks* recording what each new hook was measured against, so a future session sees why the per-tool-call tax was accepted here after S6 refused it for prompts.

**Verification:**
- [ ] `hooks.json` parses; matchers are exactly `Task|Agent` and `Skill`.
- [ ] `plugin.json` does **not** also declare `manifest.hooks` — that raises *Duplicate hooks file detected*.
- [ ] A real session restart shows both hooks firing (transcript or `claude --debug`), not just synthetic payloads.
- [ ] `README.md` states a count that matches `ls hooks/` minus `README.md`, `hooks.json`, `run-hook.cmd`.

---

### Task 8: Version, changelog, install proof, board

**Files:**
- Modify: `.claude-plugin/plugin.json`, `.claude-plugin/marketplace.json`, `CHANGELOG.md`, `docs/next-session-prompt.md`

**Step 1** — Read the version off `main` at merge time and bump both manifests to the same number.

**Step 2 — CHANGELOG entry** carrying: the two incidents with their evidence; the two findings the field report did not reach (no Superpowers edits possible; the write→review transition is model-initiated, so `UserPromptSubmit` cannot catch it); the `PreToolUse`/`additionalContext` measurement with the CLI version it came from; the three layers; and the two refusals (`updatedInput`, hard gates) with reasons.

**Step 3** — `claude plugin validate`; then the real install path: `claude plugin update dotnet-standards@dotnet-standards-dev --scope project`, confirm `installed_plugins.json` points at the new cache and its `gitCommitSha` matches the merge commit, delete `reference/` from the new cache dir.

**Step 4 — board.** Update the Lane D row and the header; append a PENDING entry for the field trial (spec *Verification* items 3 and 4), which is the only thing that settles whether this worked.

**Verification:**
- [ ] Both manifests agree on the version.
- [ ] `claude plugin details` shows the new version and unchanged skill count (no skill added).
- [ ] The board names the trial and what it measures.

---

## Self-Review

**Spec coverage:** Layer 1 → Task 2. Layer 2 hook A → Task 5, hook B → Task 6, registration and doctrine → Task 7. Layer 3 router → Task 3, descriptions → Task 4. Report filing → Task 1. Release → Task 8. Field trial → parked on the board in Task 8 Step 4, because it runs in a consumer repository and cannot be executed from this tree.

**Frozen wordings:** two, and both gate their tasks — R28–R31 (Task 2 Step 1) and the two hook emits (Task 5 Step 1, Task 6 Step 1). No task writes either before approval.

**What this plan deliberately does not do:** touch any Superpowers file; fork `subagent-driven-development`; use `updatedInput`; ship a `permissionDecision` gate; add a skill; change a rubric.

**Known risk carried forward:** `dotnet-feature-flow` has never been run end to end in the field. This plan routes more traffic at it. The mitigation is the trial, not a smaller change.
