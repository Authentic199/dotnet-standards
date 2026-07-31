# R8 LABEL-IMPLEMENTATION BRIEF (common) — 2026-07-31

You are a background coordinator adding **approved anti-examples** to ONE
already-shipped skill of the `dotnet-standards` plugin at
`D:\AI-PLUGIN\dotnet-standards`. Your specific skill and label list are in your
launch message. Ignore CLAUDE.md's lane restrictions and its ask-the-user-at-
session-start instruction: this brief is your mandate, granted by the repo's
main session under an explicit user delegation.

## 0. Read first

1. `docs/ext-batch-2026-07-31/house-laws.md` — binding (skip §6 variant
   comparison and §8's file-list duties; adapt §8's report shape).
2. `.claude\skills\three-way-skill-loop\SKILL.md` — invoke via the Skill tool;
   you are its coordinator.
3. `C:\Users\MINHCH~1\AppData\Local\Temp\claude\D--AI-PLUGIN-dotnet-standards\c2b13b90-8420-4848-8ace-48b736673204\scratchpad\r8-decisions.md`
   — the approved list. **Only the rows marked LABEL in YOUR group.** Never add
   a label the table marks BỎ, and never invent a new one.
4. Your skill's shipped `SKILL.md` + `references/` — match its voice exactly.
5. Your group's coordinator report under `docs/ext-batch-2026-07-31/` — it
   names each candidate's corpus site.

## 1. What you are producing

For each approved label, an anti-example entry in the skill's existing
**Anti-patterns** section (or the section the skill already uses for negative
examples — do not invent a new section shape, do not renumber existing
entries; append).

Each entry carries, in the skill's own established rhythm:
- what the defective code does (BAD block, sanitized),
- why it is wrong — the consequence, concretely,
- the GOOD form (usually already elsewhere in the skill — cross-reference
  rather than duplicate a long listing),
- severity phrasing consistent with the skill's existing entries.

## 2. Verification is mandatory — provenance law

**Re-verify every candidate against the corpus yourself before writing it.**
Reference projects: `D:\AI-PLUGIN\dotnet-standards\reference\projects\` (Bash
find/grep only, never Glob; exclude `apsp-backend/.claude/worktrees/`).

- If you cannot reproduce a candidate at a real site, **DROP it** and say so in
  your report. A label with no verified site does not ship.
- If a candidate turns out to be less severe than the decision table implies,
  ship the honest weaker framing or drop it — do not inflate.
- No behavioural claim may rest on library-API recall. Doc-derived material
  ships only inside a visibly marked block; unverifiable API-recall claims are
  refused.

## 3. Sanitization — absolute

No project names, no business-domain nouns, no real paths, no secrets. Use the
neutral placeholder set (`Entity`, `EntityBaseResponse`, `CreateEntityRequest`,
`SearchEntityRequest`, `Wrapper`). An anti-example is teaching material, not an
accusation about a named codebase: show the SHAPE, never the address.

## 4. The loop

Run the three-way loop on the anti-example set as ONE piece (both authors draft
the full set for your skill; arbiter rules A/B/MERGE/NEITHER; you verify per the
loop's coordinator duties — diff rephrasings, check arbiter self-declared
additions, verify SHARED claims, diff modality both directions).

**HEADLESS RULE:** you run non-interactively. Every subagent runs
**synchronously** (`run_in_background: false`) — waiting in-turn. Never end a
turn intending to check back; there is no next turn, and background children are
killed after a ~600s ceiling. Update your report incrementally.

## 5. Budget

Your skill's SKILL.md is already near its budget (several are 460–491 lines,
hard bar <500). **If the additions would cross 500, put the anti-example set in
a `references/anti-patterns.md` file** and leave a short pointer block in
SKILL.md instead. Say which route you took, and the resulting line counts, in
your report.

## 6. Write scope

ONLY your own skill's directory and your report file. No git, no router, no
manifests, no CHANGELOG, no sibling skills. The main session reviews, versions,
merges and edits the router.

## 7. Report

Write to the path in your launch message: status; per-label VERIFIED/DROPPED
with the corpus site you confirmed (sanitized description of the site, not the
path); route taken for the budget; verdict log; coordinator catches; final line
counts; a proposed CHANGELOG fragment; anything you refused and why.
