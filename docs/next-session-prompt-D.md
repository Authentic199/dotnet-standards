# Lane D — Process Integration · Session D1: closed-loop workflows (feature + review)

> Copy everything below the line into a fresh Claude Code session in
> `D:\ALTA\Project\dotnet-standards`. **DO NOT OPEN THIS SESSION until the four
> review rubrics AND `dotnet-testing` have shipped** — the deliverables bind to
> them (see the spec §7). Written at the close of S14, 2026-07-27, from the
> approved design spec.

---

## CONTEXT

I am building `dotnet-standards`. This lane adds the **process-integration
layer**: closed-loop agentic workflows that combine Superpowers (process) with
this plugin's knowledge skills, so one command walks brainstorm → plan →
implement → test-loop → review-loop → git without me invoking each Superpowers
skill by hand. **The full approved design is
`docs/superpowers/specs/2026-07-27-process-integration-design.md` — read it
FIRST; it is the source of truth for this lane and this file does not repeat
it.** The user chose to build this INSIDE `dotnet-standards` (the "Knowledge
only" description promise is deliberately broken and must be rewritten).
**No Superpowers file may ever be modified** — the relationship is *call*,
never *copy*.

**You own ONLY:** `skills/dotnet-feature-flow/`, `skills/dotnet-review-flow/`,
`commands/`, `agents/` (the six specialist agents), the `hooks/hooks.json`
SessionStart addition + its script, the `plugin.json` description rewrite, and
this file. Refuse and log anything else.

**START IN YOUR OWN WORKTREE:**
`git worktree add ../dotnet-standards-laned-d1 -b lane-d/process-integration main`

## THE DELIVERABLE — v1 per the spec

1. `dotnet-feature-flow` skill + `/dotnet-feature` command (spec §6.1).
2. `dotnet-review-flow` skill + `/dotnet-review` command (spec §6.2 — the
   shared TEST/REVIEW block extracted).
3. Six specialist agents (spec §4): 4 read-only rubric reviewers + 2 testers
   whose roster mirrors the shipped `dotnet-testing` taxonomy exactly.
4. SessionStart `superpowers-check` hook (spec §5 — warn only; the hard STOP
   lives in each flow's PHASE 0). Reuse the shipped polyglot `run-hook.cmd`
   convention: script name without extension.
5. `plugin.json` description rewrite (two layers; requires Superpowers).

`bugfix` flow is v1.5 — NOT this session (one session, one deliverable set; the
spec already records its shape).

## OPEN ITEMS TO RESOLVE FIRST (spec §9)

Before writing anything: (1) verify plugin-command namespacing/collision
behavior with the `claude-code-guide` agent — do not guess; (2) read the
shipped rubrics' severity vocabulary and `dotnet-testing`'s test taxonomy —
the flows consume their vocabulary, never invent one; (3) decide
references-split per flow skill through the loop.

## PROCESS

The three-way process (author A + `skill-writer-sp` + `skill-arbiter`,
per piece) applies to the two FLOW SKILLS and the six AGENT definitions.
Commands, the hook script and the description rewrite are mechanical — single
author, arbiter sanity pass only. The arbiter MUST invoke
`skill-creator:skill-creator` live (restart the parent session if it reports
`Unknown skill` — subagent rosters snapshot at parent start; user ruling S14).
**STANDING DELEGATION (LAW, memory `delegate-on-recommendation`):** execute
recommendations, report done; ask only when genuinely undecidable. Carve-out:
naming canonical sources/exemplars stays with the user. Verification target for
the arbiter here = the spec + the installed Superpowers/rubric/testing skill
bodies (not `reference/projects/`).

## HARD CONSTRAINTS

1. Prove it end to end: validate + reinstall + `claude plugin details` shows
   the new commands/agents/hook counts; then ONE live smoke test —
   `/dotnet-review` on a small real diff — before calling the lane done.
   Report failures honestly.
2. Merge/version protocol as always (patch +1 vs main at merge time, both
   manifests agree, CHANGELOG at top, one install at a time; delete
   `reference/` from the new cache copy).
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol (branch `lane-d/process-integration`, merge into
   main), then rewrite THIS file to open Lane D's next session (`bugfix` flow,
   v1.5), carrying the Lane log forward.

## Lane log

- **D0 (2026-07-27, S14 session):** design brainstormed under
  `superpowers:brainstorming` and approved section-by-section; spec committed
  (`docs/superpowers/specs/2026-07-27-process-integration-design.md`, commit
  c6d2a73). Key user rulings captured there: v1 = feature + review, bugfix
  v1.5; approach 2 (inside dotnet-standards, promise deliberately broken); two
  human gates (design/plan approval; pre-push) + loop caps (review 3, test 5);
  test-before-review ordering; agent-per-concern roster (4 reviewers + 2+
  testers), review/test ALWAYS subagents (context-contamination rule);
  implementation via superpowers:subagent-driven-development when > 3
  use-cases; dependency = warn-early (SessionStart) + hard-stop (PHASE 0), no
  auto-install. Deferred as recorded tech debt: PM workflow (BA requirements /
  ad-hoc → task skeleton + overview), project-setup workflow (per-project
  CLAUDE.md).
- **First action of D1:** invoke `superpowers:writing-plans` on the spec to
  produce the implementation plan, then run the plan through the process above.
