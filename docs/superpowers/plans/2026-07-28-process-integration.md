# Process Integration (Lane D, v1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship the process-integration layer of `dotnet-standards` v1 — two flow skills (`dotnet-feature-flow`, `dotnet-review-flow`), two commands, six specialist agents, a SessionStart `superpowers-check` hook, and the `plugin.json` description rewrite — per the approved spec `docs/superpowers/specs/2026-07-27-process-integration-design.md`.

**Architecture:** Command = deterministic entry, skill = process graph. Flow skills teach nothing: they CALL Superpowers skills for process and POINT subagents at dotnet-standards knowledge/rubric skills for content. Review/test judgment always comes from fresh-context specialist subagents (agent-per-concern, read-only reviewers); the main flow session coordinates and fixes only. Dependency on Superpowers = SessionStart warn + PHASE 0 hard-stop, never auto-install.

**Tech Stack:** Claude Code plugin components only — skill/command/agent markdown, `hooks/hooks.json` + a bash hook script behind the shipped polyglot `run-hook.cmd`. No .NET code is written in this lane.

## Global Constraints

- **No Superpowers file is ever modified** — the relationship is *call*, never *copy*.
- Ownership (Lane D): `skills/dotnet-feature-flow/`, `skills/dotnet-review-flow/`, `commands/`, `agents/`, the `hooks/hooks.json` SessionStart addition + its script, `plugin.json` description, the lane's own docs — **plus the mandatory router (`choosing-a-dotnet-skill`) merge-time rows and CHANGELOG/version bump required by the standing alignment rule** (CHANGELOG 0.3.10; 0.3.14 hotfix precedent).
- Three-way loop (`three-way-skill-loop`) is MANDATORY for the two flow skills and the six agent definitions. Commands, hook script, description rewrite: single-author + `skill-arbiter` sanity pass only.
- Description law (`docs/02-repo-structure.md` §5): third person, `This skill should be used when …`, <100 words, trigger-noun pushy, `Not for:` naming every owning sibling; flow skills also route to each other and to Superpowers for process-only questions.
- **Severity vocabulary is `dotnet-code-review`'s: CRITICAL / HIGH / MEDIUM / INFO.** The spec's "blocking/major" phrasing is superseded (spec §9.3 anticipated this — the rubrics own the scale). Flow loop-exit conditions use: *no CONFIRMED CRITICAL or HIGH findings remain*.
- **Test taxonomy is `dotnet-testing`'s: unit + integration** (`tests/<ProjectName>.UnitTests`, `tests/<ProjectName>.IntegrationTests`) — exactly two tester agents, adding nothing (spec §4).
- Command naming (verified against current docs via claude-code-guide, 2026-07-28): plugin commands are always namespaced `/dotnet-standards:<name>`; the bare `/dotnet-feature`, `/dotnet-review` forms work as fallback when unclaimed (v2.1.216+); plugins CAN shadow built-ins, so the `dotnet-` prefix stays (never claim `/review`).
- Loop caps (user-set, spec §8): review-loop 3 rounds, test-loop 5 rounds; cap hit → halt with status summary and ask. Findings verified before fixed: only CONFIRMED CRITICAL/HIGH force the loop.
- Agents: read-only reviewers get `tools: ["Read", "Grep", "Glob"]` — **no Bash, no Edit/Write**; the flow computes the diff and hands it to them (file list + diff file path in the prompt). Testers get `["Read", "Grep", "Glob", "Bash"]` (run build/test), **no Edit/Write**.
- Hook is warn-only; the hard STOP lives in each flow's PHASE 0. `run-hook.cmd`'s silent-absence failure mode is acceptable ONLY because the hook is warn-only and PHASE 0 backstops it — record this in `hooks/README.md` (the wrapper's own comment demands it).
- Version at merge time: patch +1 vs `main` (0.3.21 if main is still 0.3.20 — re-read the LANE BOARD header at merge; renumber if main moved). Both manifests (`.claude-plugin/plugin.json`, `.claude-plugin/marketplace.json`) must agree. CHANGELOG entry at top.
- Artifact language English. All work in worktree `../dotnet-standards-laned-d1`, branch `lane-d/process-integration`. Merge `main` INTO the branch BEFORE router/CHANGELOG edits (S17 rule).
- `bugfix` flow is v1.5 — NOT this session.

---

### Task 1: Six specialist agent definitions (`agents/*.md`)

**Files:**
- Create: `agents/dotnet-code-reviewer.md`
- Create: `agents/dotnet-architecture-reviewer.md`
- Create: `agents/dotnet-security-reviewer.md`
- Create: `agents/dotnet-performance-reviewer.md`
- Create: `agents/dotnet-unit-tester.md`
- Create: `agents/dotnet-integration-tester.md`
- Delete: `agents/.gitkeep` (directory is no longer empty)

**Interfaces:**
- Consumes: shipped rubric skills `dotnet-standards:dotnet-code-review`, `:dotnet-architecture-review`, `:dotnet-security-review`, `:dotnet-performance-review`; `dotnet-standards:dotnet-testing`.
- Produces: agent names **exactly** `dotnet-code-reviewer`, `dotnet-architecture-reviewer`, `dotnet-security-reviewer`, `dotnet-performance-reviewer`, `dotnet-unit-tester`, `dotnet-integration-tester` — the flow skills (Tasks 2, 4) spawn these by these names. Reviewer output contract: severity-ranked findings using CRITICAL/HIGH/MEDIUM/INFO with `file:line` per finding, in the shipped `dotnet-code-review` report shape. Tester output contract: structured failure list (test name, project, error, `file:line`), never a source edit.

- [ ] **Step 1: Three-way loop, piece = reviewer quartet.** Context package to `skill-writer-a` + `skill-writer-sp`: spec §4 (roster table + read-only ruling + find-vs-fix boundary), the four rubric skills' SKILL.md descriptions, the verified agent-frontmatter field list (`name`, `description`, `tools`, `model`), the tools constraint from Global Constraints, and the requirement that each reviewer's body instruct: load your bound rubric skill FIRST, review ONLY the diff handed to you, report in the rubric's report shape, never propose edits inline. Authors draft all four reviewer files (they differ only in binding + description nouns). Drafts VERBATIM to `skill-arbiter` (which invokes `skill-creator:skill-creator` live — `Unknown skill` → restart parent session).
- [ ] **Step 2: Three-way loop, piece = tester pair.** Same package plus `dotnet-testing`'s taxonomy (unit vs integration conventions, project layout) and the spec ruling: testers run the suite (`dotnet build` / `dotnet test`), report structured failures, NEVER edit source — the fix returns to the implementer.
- [ ] **Step 3: Coordinator verification.** Diff the arbiter's final texts against both drafts (self-declared additions, rephrasings, modality both directions — S12/S13b/S15/S17 discipline). Verify SHARED claims: any claim both authors make about agent frontmatter behavior must match the claude-code-guide findings, not author memory. Verify the six names match the spec roster exactly.
- [ ] **Step 4: Write the six files in the worktree; delete `agents/.gitkeep`.**
- [ ] **Step 5: Validate structure** — each file has frontmatter with `name`, `description`, `tools`; reviewer `tools` exclude Bash/Edit/Write; tester `tools` exclude Edit/Write. Run: `grep -L "^name:" agents/*.md` → expect empty output.
- [ ] **Step 6: Commit** — `git add agents/ && git commit -m "feat(lane-d): six specialist agents (4 read-only reviewers + 2 testers)"`

### Task 2: `dotnet-review-flow` skill (the shared TEST/REVIEW block)

**Files:**
- Create: `skills/dotnet-review-flow/SKILL.md`
- Create (only if the loop so decides): `skills/dotnet-review-flow/references/<name>.md`

**Interfaces:**
- Consumes: the six agent names from Task 1; Superpowers skill names `superpowers:verification-before-completion` (and any the loop adds — call, never copy); severity words CRITICAL/HIGH/MEDIUM/INFO.
- Produces: a **shared TEST-LOOP + REVIEW-LOOP block** that `dotnet-feature-flow` (Task 4) invokes as its PHASES 4–5 — the skill must expose it so the feature flow can say "run dotnet-review-flow's loop block against the working diff" without duplicating text. Standalone entry ends with the findings report; fixing is OFFERED, not automatic (spec §6.2).

- [ ] **Step 1: Three-way loop, piece = frontmatter/description** (description law; `Not for:` routes at minimum to `dotnet-feature-flow` (full feature process), the four rubric skills (the review content itself), and Superpowers for process-only questions).
- [ ] **Step 2: Three-way loop, piece = body.** Context package: spec §6.1 PHASES 4–5 verbatim + §6.2 + §8 (caps, verify-before-fix, subagent-failure retry-once rule, final-report-always rule), PHASE 0 requirements (STOP if Superpowers missing / not a git repo / the four rubric skills or `dotnet-testing` absent from the installed plugin — completeness check), test-before-review ordering + its three recorded reasons, the context-contamination rule (main session never reviews or tests), and the corrected severity vocabulary. Body structure: PHASE 0 preflight → TEST-LOOP (spawn `dotnet-unit-tester` + `dotnet-integration-tester` in parallel; failures → fix → rerun; green or 5 rounds) → REVIEW-LOOP (spawn all 4 reviewers in parallel on the diff; verify findings CONFIRMED vs PLAUSIBLE against the code; fix CONFIRMED CRITICAL/HIGH → back through TEST-LOOP; clean or 3 rounds) → final report (unfixed MEDIUM/INFO, loop counts, suites passed).
- [ ] **Step 3: Loop decides the references-split** (spec §9.4) — single-body unless the arbiter rules the block anatomy belongs in `references/`.
- [ ] **Step 4: Coordinator verification** — same diff discipline; additionally check every named Superpowers skill exists in the installed Superpowers roster and every agent name matches Task 1 exactly; check no Superpowers content was copied in (call, never copy).
- [ ] **Step 5: Write files; validate description** <100 words, third person; no H1 in body.
- [ ] **Step 6: Commit** — `git commit -m "feat(lane-d): dotnet-review-flow skill (shared test/review loop)"`

### Task 3: `/dotnet-review` command (mechanical)

**Files:**
- Create: `commands/dotnet-review.md`

**Interfaces:**
- Consumes: `dotnet-standards:dotnet-review-flow` (Task 2).
- Produces: command file whose body ONLY invokes the flow skill (command = deterministic entry, zero logic).

- [ ] **Step 1: Write the file** (single author — coordinator):

```markdown
---
description: Run the dotnet review workflow — parallel specialist reviewers + testers with verified findings — on an existing diff or branch, per the dotnet-review-flow skill.
argument-hint: [branch, range, or scope — defaults to the working diff]
---

Invoke the dotnet-standards:dotnet-review-flow skill and follow it exactly.
Review target: $ARGUMENTS (if empty, the current working diff against the default branch).
```

- [ ] **Step 2: Arbiter sanity pass** (batch with Task 5's pieces to save a round-trip if convenient).
- [ ] **Step 3: Commit** — `git commit -m "feat(lane-d): /dotnet-review command"`

### Task 4: `dotnet-feature-flow` skill + `/dotnet-feature` command

**Files:**
- Create: `skills/dotnet-feature-flow/SKILL.md`
- Create (only if the loop so decides): `skills/dotnet-feature-flow/references/<name>.md`
- Create: `commands/dotnet-feature.md`

**Interfaces:**
- Consumes: Task 2's shared TEST/REVIEW block (invoked, not duplicated); Superpowers skills `superpowers:brainstorming`, `superpowers:writing-plans`, `superpowers:test-driven-development`, `superpowers:subagent-driven-development`, `superpowers:using-git-worktrees`, `superpowers:finishing-a-development-branch`, `superpowers:verification-before-completion`; knowledge-skill routing via `dotnet-standards:choosing-a-dotnet-skill` for implementation-subagent prompts.
- Produces: the full §6.1 process graph — PHASE 0 preflight (same checks as review-flow + worktree offer), PHASE 1 brainstorm, PHASE 2 plan, GATE 1 (human approves design + plan), PHASE 3 implement (≤3 use-cases → TDD in main session; >3 → subagent-driven-development, task prompts carry knowledge-skill pointers), PHASES 4–5 = dotnet-review-flow's shared block, PHASE 6 git via finishing-a-development-branch, GATE 2 (human approves before any push).

- [ ] **Step 1: Three-way loop, piece = frontmatter/description** (`Not for:` at minimum: standalone review of an existing diff — `dotnet-review-flow`; which knowledge skill owns a convention — `choosing-a-dotnet-skill`; the process skills themselves — Superpowers).
- [ ] **Step 2: Three-way loop, piece = body** (context package: spec §6.1 verbatim + §8 + the two human gates + the ≤3/>3 use-case fork + git-safety rules: no push without GATE 2, worktree removed only after merge, commit messages follow the target repo's convention).
- [ ] **Step 3: Coordinator verification** — as Task 2 Step 4; plus verify the shared-block reference matches what Task 2 actually shipped (exact heading/anchor), and GATE placement matches the spec.
- [ ] **Step 4: Write `commands/dotnet-feature.md`** (single author, arbiter sanity pass):

```markdown
---
description: Run the full dotnet feature workflow — brainstorm, plan, human gate, implement, test loop, review loop, git — per the dotnet-feature-flow skill.
argument-hint: [feature description]
---

Invoke the dotnet-standards:dotnet-feature-flow skill and follow it exactly.
Feature request: $ARGUMENTS
```

- [ ] **Step 5: Write skill files; validate; commit** — `git commit -m "feat(lane-d): dotnet-feature-flow skill + /dotnet-feature command"`

### Task 5: SessionStart `superpowers-check` hook + `hooks/README.md` note

**Files:**
- Create: `hooks/superpowers-check` (extensionless, bash — the shipped convention)
- Modify: `hooks/hooks.json` (add SessionStart beside the existing PostToolUse)
- Modify: `hooks/README.md` (record the second hook + why the wrapper's silent-absence mode stays acceptable)

**Interfaces:**
- Consumes: `${CLAUDE_CONFIG_DIR:-$HOME/.claude}/plugins/installed_plugins.json` — verified shape: `{"plugins": {"<name>@<marketplace>": ...}}`; presence test = key starting `"superpowers@`.
- Produces: warn-only SessionStart output — `systemMessage` (user-visible warning) + `additionalContext` (tells Claude the flows will hard-stop). Exit 0 ALWAYS.

- [ ] **Step 1: Write `hooks/superpowers-check`:**

```bash
#!/usr/bin/env bash
# SessionStart hook: warn (never block) when the Superpowers plugin is absent.
# The hard STOP lives in each flow skill's PHASE 0 — this is the early warning.
# Warn-only is also what makes run-hook.cmd's silent-absence failure mode
# acceptable for this hook: if it never runs, PHASE 0 still guards the flows.
set -euo pipefail

PLUGINS_FILE="${CLAUDE_CONFIG_DIR:-$HOME/.claude}/plugins/installed_plugins.json"

# Missing registry = nothing to say; never block a session from a hook.
[ -f "$PLUGINS_FILE" ] || exit 0

grep -q '"superpowers@' "$PLUGINS_FILE" && exit 0

cat <<'JSON'
{
  "systemMessage": "dotnet-standards: the Superpowers plugin is NOT installed. The /dotnet-feature and /dotnet-review workflows require it and will stop at preflight. Install: claude plugin install superpowers@claude-plugins-official (then restart the session). Knowledge skills work without it.",
  "hookSpecificOutput": {
    "hookEventName": "SessionStart",
    "additionalContext": "Superpowers is not installed. dotnet-standards flow skills (dotnet-feature-flow, dotnet-review-flow) must hard-stop at PHASE 0 and give the install command. Knowledge skills are unaffected."
  }
}
JSON
exit 0
```

- [ ] **Step 2: Modify `hooks/hooks.json`** — final content:

```json
{
  "hooks": {
    "SessionStart": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "\"${CLAUDE_PLUGIN_ROOT}/hooks/run-hook.cmd\" superpowers-check",
            "shell": "bash",
            "async": false
          }
        ]
      }
    ],
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

- [ ] **Step 3: Append to `hooks/README.md`:** the `superpowers-check` entry — what it does, warn-only ruling (spec §5), and the explicit statement that it satisfies the wrapper's "no hook whose silent absence FAILS OPEN" rule because PHASE 0 backstops it.
- [ ] **Step 4: Test the script directly.** Run: `bash hooks/superpowers-check` → expect empty output, exit 0 (Superpowers IS installed here). Then: `PLUGINS_FILE_TEST=1 bash -c 'CLAUDE_CONFIG_DIR=/nonexistent bash hooks/superpowers-check'` → expect empty output, exit 0 (missing registry). Then simulate absence: `CLAUDE_CONFIG_DIR="$(mktemp -d)" ; mkdir -p "$CLAUDE_CONFIG_DIR/plugins" ; echo '{"plugins":{"github@claude-plugins-official":{}}}' > "$CLAUDE_CONFIG_DIR/plugins/installed_plugins.json" ; CLAUDE_CONFIG_DIR="$CLAUDE_CONFIG_DIR" bash hooks/superpowers-check` → expect the JSON warning, exit 0. Validate the emitted JSON parses: pipe through `python -m json.tool`.
- [ ] **Step 5: Arbiter sanity pass** (mechanical piece — single author + arbiter).
- [ ] **Step 6: Commit** — `git commit -m "feat(lane-d): SessionStart superpowers-check hook (warn-only)"`

### Task 6: `plugin.json` description rewrite (two layers)

**Files:**
- Modify: `.claude-plugin/plugin.json` (description field ONLY in this task; version bumps in Task 7)

**Interfaces:**
- Consumes: spec §5.4 — drop "Knowledge only …"; describe the two layers; state workflows sit ON TOP of Superpowers and require it.
- Produces: the description Task 7's CHANGELOG entry cites.

- [ ] **Step 1: Replace the `description` value with:**

```
Two layers. Knowledge: .NET architecture, CQRS pipeline (MediatR + FluentValidation + AutoMapper), EF Core, Redis caching, Elasticsearch, controller-based Web API conventions, worker services, testing, and four review rubrics. Process integration: /dotnet-feature and /dotnet-review closed-loop workflows plus six specialist review/test agents — these sit on top of the Superpowers plugin and require it (brainstorm/plan/TDD process remains Superpowers').
```

- [ ] **Step 2: Arbiter sanity pass** (batch with Task 5).
- [ ] **Step 3: Commit** — `git commit -m "feat(lane-d): plugin description rewrite (two layers, requires Superpowers)"`

### Task 7: Merge-time alignment — router rows, CHANGELOG, version bump

**Files:**
- Modify: `skills/choosing-a-dotnet-skill/SKILL.md` (add routing rows for the two flow skills)
- Modify: `CHANGELOG.md` (new entry at top)
- Modify: `.claude-plugin/plugin.json` + `.claude-plugin/marketplace.json` (version = main's patch +1 at merge time; both agree)

**Interfaces:**
- Consumes: everything shipped in Tasks 1–6; current `main` version (re-read LANE BOARD header at merge).
- Produces: the feat state the standing alignment rule demands — router covers every skill on `main` at merge time, same feat commit family.

- [ ] **Step 1: `git fetch`/`git merge main` INTO `lane-d/process-integration` FIRST** (S17 rule — expect mid-session main movement; CHANGELOG conflict rule: keep both entries, renumber yours above theirs).
- [ ] **Step 2: Router rows.** The router's description says `Not for: process-layer workflow — Superpowers` — the flow skills are dotnet-standards skills and MUST be routed; draft rows for the ownership table: `| Running the whole feature process end to end — brainstorm to push — as one command | dotnet-feature-flow |` and `| Running the standalone test + review loop on an existing diff or branch | dotnet-review-flow |`, placed per the router's existing table structure; check the router's `Not for:` line still reads correctly next to the new rows (if it now misleads, amend it in the same commit — alignment precedent). Router edits go through the arbiter sanity pass (router is a skill body; the piece is small but reviewed).
- [ ] **Step 3: CHANGELOG entry at top** — version, date, the five deliverable groups, the severity-vocabulary correction (spec said blocking/major; shipped rubric ladder CRITICAL/HIGH/MEDIUM/INFO won), the namespacing verification result, the description-promise break (deliberate, user-ruled), rulings made by the arbiter during the loops.
- [ ] **Step 4: Bump BOTH manifests to the same version.** Run: `grep '"version"' .claude-plugin/plugin.json .claude-plugin/marketplace.json` → expect identical values.
- [ ] **Step 5: Commit** — `git commit -m "feat(lane-d): process-integration v1 — flows, commands, agents, hook, description + router alignment"`

### Task 8: Prove it end to end

**Files:** none new (cache surgery + verification only)

- [ ] **Step 1: Merge `lane-d/process-integration` into `main`** (fast-forward or merge commit per house practice).
- [ ] **Step 2: Reinstall:** `claude plugin update dotnet-standards@dotnet-standards-dev` (short name fails — S16 lesson).
- [ ] **Step 3: Verify:** `claude plugin details dotnet-standards@dotnet-standards-dev` shows **20 skills, 2 commands, 6 agents, 2 hook events**; `installed_plugins.json` points at the new cache with `gitCommitSha` = the merge commit (S17 lesson — `details` alone is never proof); both manifests in the cache agree on the version.
- [ ] **Step 4: Delete `reference/` from the new cache dir** (check `installed_plugins.json` before touching ANY cached version dir; leave unreferenced old caches alone).
- [ ] **Step 5: LIVE SMOKE TEST (hard constraint):** in a real session, run `/dotnet-review` on a small real diff and confirm: PHASE 0 passes, testers + reviewers spawn as the named agents, report uses CRITICAL/HIGH/MEDIUM/INFO, loop caps respected. Report failures honestly — a failed smoke test means the lane is NOT done.
- [ ] **Step 6: Also confirm the SessionStart hook fired** in that session (warning absent because Superpowers is installed = correct null result; verify via hook debug output or by the absence of a false warning).

### Task 9: Close-out

**Files:**
- Modify: `CLAUDE.md` (rewrite for Lane D's next session: `bugfix` flow v1.5, carrying the Lane log)
- Modify: `docs/next-session-prompt-D.md` (same rewrite)
- Modify: `docs/next-session-prompt.md` (LANE BOARD: Lane D row + header version/counts; append parked items to the PENDING log)

- [ ] **Step 1: Rewrite the two lane files** — opener: v1 shipped, next = `bugfix` flow (v1.5, spec §6.3, reuses the shared blocks); carry the Lane log forward with this session's rulings, delegation uses, and anything parked.
- [ ] **Step 2: Update the LANE BOARD row + header.**
- [ ] **Step 3: Commit close-out; remove the worktree** after merge is confirmed.

---

## Self-review (done at write time)

1. **Spec coverage:** §3 layout → Tasks 1–6; §4 roster/rulings → Task 1; §5 dependency mechanism → Tasks 5 (warn), 2+4 (PHASE 0 hard-stop + completeness check), 6 (description); §6.1 → Task 4; §6.2 → Tasks 2–3; §6.3 explicitly out (v1.5); §8 → folded into Task 2/4 context packages; §9.1 → resolved (namespacing verified); §9.2 → resolved (unit+integration, two testers); §9.3 → resolved (CRITICAL/HIGH/MEDIUM/INFO); §9.4 → Task 2 Step 3 (loop decides). Gap: none found.
2. **Placeholder scan:** command bodies, hook script, hooks.json, description text are given verbatim; flow-skill/agent bodies are deliberately NOT pre-written — they are the three-way loop's deliverable and pre-writing them would bypass the mandatory process. Their tasks carry the full context package instead. This is the process analogue of test-first, not a placeholder.
3. **Type consistency:** agent names identical across Tasks 1, 2, 4; severity words identical across Tasks 1, 2, 4, 7; skill names `dotnet-feature-flow`/`dotnet-review-flow` consistent; both command bodies reference the namespaced skill names.
