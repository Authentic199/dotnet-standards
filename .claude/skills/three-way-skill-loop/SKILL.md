---
name: three-way-skill-loop
description: >-
  This skill should be used when producing any piece of a dotnet-standards
  plugin skill (frontmatter/description, Core Principles, Patterns,
  Anti-patterns, Decision Guide, or a references/ file) — it defines the
  mandatory three-way drafting loop and the coordinator role of the main
  session. Not for: what a skill's content should say — the installed
  dotnet-standards skills and the session's CLAUDE.md brief own that.
---

# The three-way skill loop

The main session is the **coordinator**. It never drafts skill text itself.
Per user directive (2026-07-27, S15): drafting is done by agents; the main
session dispatches, relays, verifies, and works with the user.

## Roles

| Role | Agent | Methodology it loads |
|---|---|---|
| Coordinator | main session | this skill + the lane CLAUDE.md brief |
| Author A | `skill-writer-a` | `docs/02-repo-structure.md` §5 + `docs/00-brainstorm.md` §3 + the kit's skill structure |
| Author B | `skill-writer-sp` | `superpowers:writing-skills` |
| Arbiter | `skill-arbiter` | `skill-creator:skill-creator`, invoked LIVE |

The two authors must never load each other's methodology — independence is the
point. All agents run in the current working directory, no worktree, and are
continued across pieces via SendMessage with context intact.

## Per-piece sequence (never reordered, never skipped)

1. **Explain first.** The coordinator explains the piece plan to the user in
   Vietnamese; the user comments. No drafting before this.
2. **Independent drafts.** Dispatch Author A and Author B in parallel with
   identical piece scope, the same settled-rulings package, and equal source
   access. Neither writes files; both return text with rationale, self-flagged
   weaknesses, and `## QUESTIONS`.
3. **Verdict.** Forward BOTH drafts to the arbiter **verbatim — never
   summarized** (a summarized draft invalidates the verdict; S13b P3 needed a
   second round for exactly this). The arbiter rules A/B/MERGE/NEITHER with
   file-verified reasons and returns the final text.
4. **User approval.** The coordinator verifies the verdict (below), presents
   it to the user in Vietnamese, and only after approval may any file be
   written — by the coordinator, at skill-assembly time.

## Coordinator verification duties on every verdict

- **Diff every rephrasing** of a settled ruling against the original (S12: the
  arbiter can introduce errors while reformulating).
- **Check the arbiter's self-declared additions** like any author claim (S13b:
  the arbiter may add content; it must self-declare, you must verify).
- **Expect shared blind spots.** Independent drafts can agree on a false rule
  at the doctrine's center (S13b: request-typed success keys; S15: entity-typed
  validator assertions — the same class of error in mirror image). The arbiter
  must verify shared claims, not just disagreements; the coordinator checks
  that it did.
- **Diff modality, not just facts** (S13b: a user permission drifted into an
  obligation; S15: "not chosen" drifted into "banned").
- **Answer agent `## QUESTIONS` by delegate-on-recommendation**: execute a
  clear recommendation and report it; escalate to the user only the genuinely
  undecidable. Record every delegated call in the Lane log.

## Standing rules that bind every piece

- Ping each agent once per session before relying on it; the ping doubles as
  the context-package load. If the arbiter reports `Unknown skill` for
  skill-creator, restart the parent session — subagent skill rosters snapshot
  at parent startup.
- Agent prompts must carry: the user-named exemplar list, all relevant settled
  rulings, and the reading discipline for `reference/projects/`.
- Announce every agent use to the user; relay milestones.
- Artifact language English; sanitized (no real paths, project names,
  business-domain names, secrets). Anti-examples are labelled only by the user
  (R8). One canonical source per area, user-designated (R7) — never average.
