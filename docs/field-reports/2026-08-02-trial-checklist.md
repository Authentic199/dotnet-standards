# Field trial checklist — v0.3.62 process handback

**What this measures.** Whether the two `PreToolUse` hooks shipped at 0.3.62
fire in a real consumer session, and whether the session acts on them. Nothing
else. The design that produced them is
`docs/superpowers/specs/2026-08-02-process-handback-design.md`; the failure they
answer is `2026-08-02-skill-routing-failure.md` in this folder.

**Who fills this in: the user, not the trial session.** This file lives in the
plugin repository, so a session working in a consumer repository will not see it
— keep it that way. A session told it is being measured on whether it loads
`dotnet-review-flow` will load `dotnet-review-flow`, and the trial will have
measured nothing. **Do not paste this checklist, the CHANGELOG entry, or the
words "hook", "fleet-nudge" or "process-handback" into the trial session.** Give
it an ordinary task in ordinary words.

**Two independent signals, and they are not equally trustworthy.**

| Signal | How it is read | Trust |
|---|---|---|
| **Did the hook script run?** | A marker file on disk, written by the script itself | **Hard.** Independent of anything the model says |
| **Did the hook reach the model?** | The session transcript records the emitted text verbatim | **Hard** |
| **Did the session act on it?** | The same transcript: every `Skill` load and every subagent spawn, as tool calls | **Hard** |
| **What the session says it did** | Its own summary | **Not evidence.** This is precisely what failed on 2026-08-02 |

**Nothing has to be measured while the run is in progress.** Claude Code writes
each session to `~/.claude/projects/<encoded-repo-path>/<session-id>.jsonl`, one
record per event, as it happens — so it survives context compaction and needs no
cooperation from the session. `trial-extract.py` in this folder reads it:

```
python docs/field-reports/trial-extract.py <consumer-repo-path>
```

It prints which hooks fired (identified by the text they emitted, not by event
name), every skill load in order, every spawn with its `subagent_type`, and a
verdict block. Verified against real transcripts before it shipped.

---

## PHASE 0 — Preflight. Skip any of this and the trial measures nothing

- [ ] **The consumer project is running 0.3.62.** As of 2026-08-02 the installed
      copy was **0.3.58**, so this is not optional.

      ```
      # in the consumer project directory, e.g. D:\ALTA\Project\TWOH\ops-service
      claude plugin update dotnet-standards@dotnet-standards-dev --scope project
      ```

- [ ] **Verify the install, do not trust `details` alone.** Confirm the registry
      points at the new cache:

      ```
      python -c "import json,io;d=json.load(io.open(r'C:\Users\MinhChanh\.claude\plugins\installed_plugins.json',encoding='utf-8'));print(d['plugins']['dotnet-standards@dotnet-standards-dev'])"
      ```

      Expect `"version": "0.3.62"` and an `installPath` ending in `0.3.62`.

- [ ] **Six hook scripts are in the new cache.** The event count `details` prints
      is not the script count.

      ```
      ls "C:\Users\MinhChanh\.claude\plugins\cache\dotnet-standards-dev\dotnet-standards\0.3.62\hooks"
      ```

      Expect: `post-edit-format`, `superpowers-check`, `router-nudge`,
      `test-report-nudge`, `fleet-nudge`, `process-handback` (plus `hooks.json`,
      `run-hook.cmd`, `README.md`).

- [ ] **The scripts have LF endings, not CRLF.** A CRLF hook dies on line 1 and
      dies *silently* — which would read as "the hook did not fire".

      ```
      file "C:\Users\...\0.3.62\hooks\fleet-nudge"
      ```

      Expect *Bourne-Again shell script … text executable*, with no mention of
      `CRLF line terminators`.

- [ ] **Delete `reference/` from the new cache directory** (house protocol —
      it carries the real projects).

- [ ] **Clear the marker directory before starting**, so the markers found
      afterwards belong to this trial and nothing else:

      ```
      rm -f /tmp/dotnet-standards/fleet-nudge-* /tmp/dotnet-standards/process-handback-*
      ```

      (`/tmp` in Git Bash is `C:\Users\<user>\AppData\Local\Temp`; both paths are
      the same directory.)

- [ ] **The consumer repository looks like a .NET solution at the depth cap** — a
      `*.sln`/`*.csproj` at the root, or a `*.csproj` at depth 2 or 3. Deeper than
      that and both hooks stay silent by design, and the trial is void.

---

## RUN A — the original prompt, from scratch, on a fresh branch

**The strongest form of this trial, and the one to run:** a new branch cut from
the commit *before* the feature existed, and the **original feature request,
verbatim**, in a new session. Same input, known-bad baseline, both incidents
reproduced in one run — the design phase and the review phase — and both hook
paths exercised (`process-handback` on `brainstorming`, `fleet-nudge` at the
first review spawn).

**The baseline it is measured against**, from the 2026-08-02 report: 20+ review
rounds, every one `general-purpose`; zero review skills loaded; zero specialist
agents; the performance lens never applied.

- [ ] Cut the branch from the commit **before** the feature — not from
      `feature/access-control-core`, or there is nothing to build.
- [ ] **New session.** Markers are keyed by session id; a session that already
      has one gets no emit.
- [ ] Give it the original request, in the original words. **Nothing else.** No
      `/dotnet-feature`, no "use subagents", no "review carefully", no mention of
      skills, hooks, or that anything is being measured. Every one of those
      answers the question on the session's behalf.
- [ ] Let it run to the end, including whatever review it decides to do.
- [ ] **Do not intervene**, even when it is visibly about to repeat the failure.
      An interrupted run answers a different question.
- [ ] Measure nothing during the run. There is nothing to catch in flight — the
      transcript on disk holds all of it.

*A cheaper variant exists — copy the finished plan into a new branch and say
"execute this plan" — but it measures less: brainstorming never runs, so the
design-phase incident cannot reproduce, and the plan already carries the skill
pointers the corrected session wrote into it, which biases the implementation
half. Use it only if a full rebuild is too expensive, and say so in the report.*

### Read the hard signal first

```
ls -la /tmp/dotnet-standards/
```

| What is there | What it means |
|---|---|
| `process-handback-<session-id>` | Hook B fired — a Superpowers process skill was loaded and the composition note was injected |
| `fleet-nudge-<session-id>` | Hook A fired — a review- or test-shaped subagent spawn was seen |
| `*-na-<session-id>` | The hook ran and decided **this is not a .NET repository**. If the repo *is* .NET, the glob gate or the `cwd` field is wrong — that is a defect, report it |
| Neither, for this session id | The hook never ran at all: `hooks.json` not loaded, no bash, CRLF, or the matcher name is wrong on this CLI |

### Then run the extractor — it reads the tool calls, so nobody has to

```
python docs/field-reports/trial-extract.py <consumer-repo-path>
```

Save its whole output; it is the report's core. Then answer by hand the two
questions it cannot:

- [ ] **Ordering.** Were the knowledge skills loaded *before* the design or plan
      step that used them, or after the code was already written? The skill list
      is printed in order — compare it against when the spec was produced.
- [ ] **Who did the final review.** Superpowers'
      `requesting-code-review/code-reviewer.md`, or this plugin's fleet? The
      spawn descriptions in the output usually say.

**Also worth one question at the very end of the trial session** — after
everything is finished, so it changes nothing: *"trong phiên này bạn đã load
skill nào và spawn agent loại gì?"* Compare its answer to the extractor's.
**The gap between the two is itself a finding**, and it is the gap that hid this
whole failure for a full session on 2026-08-02.

### The verdict for Run A

| Outcome | What it means | What follows |
|---|---|---|
| Hooks fired **and** the flow or the agents were used | The remedy works | Record it; close the trial |
| Hooks fired, **nothing changed in behaviour** | The nudge is heard and ignored — the same class of result as the 2026-07-29 SessionStart measurement | Escalate: `permissionDecision: "ask"` on a review spawn naming `general-purpose`. The ladder is in the design §Risks |
| Hooks **did not fire** | A plumbing defect, not a persuasion defect | Fix the plumbing first; the behaviour question stays unanswered |
| Fired but the session never spawned a review subagent at all | Inconclusive for Hook A | Run B still answers the rubric question |

---

## RUN B — `/dotnet-review` on the branch that was reviewed by hand

This one quantifies what the improvised review missed. It needs no hooks: the
command enters the flow directly.

- [ ] Run `/dotnet-review` on `feature/access-control-core`.
- [ ] Compare its report against the five findings the hand-rolled final review
      produced.

Record, one line each:

- [ ] **Findings the fleet caught that the hand-rolled review did not** — and for
      each, which lens caught it.
- [ ] **Performance-lens findings specifically.** This is the lens that never ran
      once in the original session, so anything here is pure delta. The known
      candidate: `HasJsonbDictionary` serialising three times per comparison on
      `access_decision`, on the write path.
- [ ] **Findings the hand-rolled review caught that the fleet did not.** A real
      possibility, and worth more than the reverse — it names a gap in the
      rubrics, which is fixable content.
- [ ] **Anything both produced.** Confirms the rubric was not needed there.

**A clean fleet report is a result, not a disappointment.** It would mean the
improvised review happened to cover the ground — worth knowing, and worth
writing down, because it bounds how much this whole change was worth.

---

## What to hand back

One file, in the consumer repository or pasted into a plugin session, carrying:

1. The marker listing, verbatim.
2. `trial-extract.py`'s full output for Run A, verbatim.
3. The gap between what the session said it did and what the extractor found.
4. The Run B delta, by lens.
5. One sentence on what *actually* changed in how the session worked — or that
   nothing did.

Anything that turns out to be a plugin defect goes to the PENDING log on the
LANE BOARD with its evidence, the same way this trial got there.
