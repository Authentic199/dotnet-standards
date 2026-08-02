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
| **Did the hook fire?** | A marker file on disk, written by the script itself | **Hard.** Independent of anything the model says |
| **Did the session act on it?** | The transcript — which agents were spawned, which skills were loaded | **Soft.** Read the tool calls, never the session's summary of them |

A session's own account of what it loaded is exactly the evidence that failed on
2026-08-02: that session believed it had covered the review properly until the
transcript was checked.

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

## RUN A — a feature built through subagent-driven development

This is the run that reproduces the original failure. **Word the request the way
it was worded on 2026-08-02** — an ordinary feature request, no process
instructions, no mention of reviews or agents.

- [ ] Give the session a feature large enough to route to
      `subagent-driven-development` (more than three use-cases), in a .NET repo.
- [ ] Let it run to the end, including whatever review it decides to do.
- [ ] **Do not intervene**, even when it is visibly about to repeat the failure.
      An interrupted run answers a different question.

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

### Then read the transcript — tool calls, not prose

- [ ] Was `dotnet-feature-flow` or `/dotnet-feature` entered at any point?
      (`yes` / `no`)
- [ ] Which skills were loaded, in order? List them. Specifically: were the
      knowledge skills loaded **before** the design or plan step that used them,
      or after?
- [ ] How many subagents were spawned, and with which `subagent_type`? Count
      `general-purpose` spawns separately.
- [ ] Was `dotnet-review-flow` loaded before any review round?
- [ ] Were any of the six specialist agents spawned? Which?
- [ ] Was the final review done by `../requesting-code-review/code-reviewer.md`,
      or by this plugin's fleet?

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
2. The skill-load and subagent-spawn sequence from Run A, as tool calls.
3. The Run B delta, by lens.
4. One sentence on what *actually* changed in how the session worked — or that
   nothing did.

Anything that turns out to be a plugin defect goes to the PENDING log on the
LANE BOARD with its evidence, the same way this trial got there.
