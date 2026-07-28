---
name: claude-md-builder
description: >-
  This skill should be used when creating, rebuilding, refreshing or trimming a
  CLAUDE.md for a .NET repository: bootstrapping project memory, capturing the
  exact build, test and EF migration commands, recording hard rules and do-not
  rules, cutting a bloated CLAUDE.md back under 200 lines, or turning a mistake
  Claude repeated into a new rule. Not for: what a convention should say — the
  knowledge skills; which skill owns an area — choosing-a-dotnet-skill;
  reviewing changed code — dotnet-code-review; authoring a plugin skill —
  superpowers:writing-skills; running a feature end to end — dotnet-feature-flow.
---

## Overview

This skill writes the **tier-3 file**: the per-project `CLAUDE.md` that says
which conventions apply to *this* codebase. It does not decide what a convention
should be — the knowledge skills own that, and they load on demand. `CLAUDE.md`
is loaded into **every** session instead, which is the whole constraint: every
line spends context on every turn, forever.

So the file is not documentation. It is the shortest possible list of things
Claude would otherwise get wrong.

Two modes. **Create** when the repository has no `CLAUDE.md`. **Update** when it
has one — never regenerate over a file a human has tuned.

Cutting across both, one branch: a repository that has no business code yet.
There the codebase answers nothing, and the only available source is whatever
documentation the user hands over. That branch is narrower, not looser — see
*PHASE 1b* and principle 8.

## Core Principles

1. **Every line earns its place or is cut.** The test, applied per line: *would
   removing this cause Claude to make a mistake?* If not, delete it. There is no
   third answer, and "it is nice context" is not one.

2. **Never write what Claude can read off the codebase.** A directory listing, a
   dependency inventory, an architecture essay — Claude derives all of it from
   the repo in seconds. Spending permanent context on derivable facts is the
   single most common way a `CLAUDE.md` goes bad.

3. **Never duplicate an analyzer.** If StyleCop, SonarAnalyzer, Roslynator, an
   `.editorconfig` or `dotnet.ruleset` already enforces a rule, the rule does not
   go in `CLAUDE.md`. Scan step 3 exists to build this exclusion list *before*
   drafting. Never send an LLM to do a linter's job.

4. **Every line is project-specific or an approved static rule.** Those are the
   only two admissible sources. Generic advice the model already carries — "write
   clean code", "handle errors", "follow SOLID" — is banned outright.

5. **Commands are copy-pasteable and verified, never reconstructed from
   memory.** A command in `CLAUDE.md` that does not run is worse than no command:
   Claude will trust it and waste a turn discovering it is wrong. Take commands
   from the CI config and the solution layout, and write them with every switch
   spelled out.

6. **Do not run `/init` and do not consume its output as truth.** `/init`
   produces exactly the derivable content principle 2 forbids. Where an
   `/init`-generated file already exists, it is the *subject* of update mode — a
   draft to cut down, not a source to trust.

7. **Under 200 lines, always.** Official guidance targets under 200 lines per
   `CLAUDE.md`; past that, adherence drops and Claude starts ignoring the rules
   that matter. Depth goes into the documents `CLAUDE.md` points at, never into
   `CLAUDE.md` itself.

8. **A document states intent; only the codebase states fact.** A spec, a design
   note or an ERD is evidence about what the project *means to be*. It is never
   evidence that a command runs, that a folder exists, or that a framework was
   chosen — those are settled by the repository or not at all. Everything taken
   from a document is provisional, marked as such, and re-checked once code
   exists.

## The workflow

Run the phases in order. Phases 5 and 6 are not optional and not merged.

### PHASE 0 — Pick the mode

`CLAUDE.md` at the repository root, or `.claude/CLAUDE.md`? Present → **update
mode** (see *Update mode* below). Absent → continue here in create mode.

### PHASE 1 — Scan

Read `references/scan-map.md` and work its table top to bottom. It names each
file to open, what to infer from it, and what to do when the file is missing.

Three rules hold across the whole scan:

- **Exclude `**/worktrees/**`, `bin/`, `obj/`, `node_modules/`, `.git/`.** A
  repository with git worktrees checked out inside it will otherwise inflate
  every count several times over.
- **Read the key structure of config files, never their values.** Knowing that
  `RedisSettings` exists is the inference; the connection string is not.
- **Record where each inference came from.** A fact with no file behind it does
  not go in the draft.

**Then check the gate.** Did the scan produce enough to fill the two required
sections — *Project overview* and *Commands*? Concretely: a solution or project
file was found, **and** the repository holds business code beyond a template
skeleton. Yes → go to PHASE 2. No → this is a greenfield repository; go to
PHASE 1b first.

### PHASE 1b — Documents as a source

Run this whenever the gate failed. Run it **optionally** in any other case, when
the user offers documents: they are always welcome, and only ever mandatory here.

Ask the user to name the documents — spec, requirements, design note, ERD, API
contract, agreed decisions — **by path**. Read only what was named. Never go
hunting through the repository for something document-shaped; an unnamed file is
not a source.

What a document may produce:

| From documents | Never from documents |
|---|---|
| What the project is and who uses it | Any build, test or migration command |
| Domain glossary — terms an outsider misreads | The directory tree, or any path |
| Intended module and capability boundaries | The target framework or package list |
| Hard business constraints, forbidden actions | Anything stated as already existing |

**Mark every document-derived line** with a block-level HTML comment naming the
source: `<!-- source: docs/spec.md, unverified -->`. Those comments are stripped
before the file reaches context, so they cost nothing, and the next update knows
exactly which lines still need checking against real code.

With no code, the `Commands` section stays **empty rather than invented**. Say so
in the file, in one line.

### PHASE 2 — Ask, once

Ask the three questions in `references/scan-map.md` §Questions as **one batch**,
and say plainly that skipping is fine. Question 2 is asked only if the scan
tripped its condition; Q0 belongs to PHASE 1b and is asked there, not here.
Never add a fourth question to this batch, and never ask anything the scan could
have answered.

Skipped questions produce **no** section — they do not produce a guess.

### PHASE 3 — Select the static rules

Open `references/static-rules.md`. Each rule carries an **Applies when**
condition. Ship a rule only when the scan proved its condition holds; drop it
silently otherwise. A repository with no `Migrations/` folder does not receive
the EF migration rules, and forcing them in is how a generic file gets built.

Rules with a `<slot>` take the value the scan found. A slot the scan could not
fill means the rule does not ship — never leave a placeholder in the output.

### PHASE 4 — Draft

Follow `references/template.md`: the section order, which sections are required
versus conditional, and the line budget for each. Fill it with scan findings and
selected static rules only.

Write the `Commands` section first and the prose sections last. If a section has
nothing project-specific to say, omit the section — an empty heading is bloat
with a title.

On the greenfield branch, `Commands` and `Project structure` are deferred rather
than required, and the capped `Planned, not yet built` section becomes available.
Read that section's expiry rule in `references/template.md` before using it.

### PHASE 5 — Trim

Walk `references/checklist.md` against the draft. Cut in the order it gives.
Then count lines. Over 200 → keep cutting; the checklist order is also the
cutting order, so the least valuable content goes first.

Never buy space by dropping a command or a verification probe.

### PHASE 6 — Self-verify

Answer these **using only the drafted file**, with the repository out of view:

1. *What are the exact build and test commands for this project?*
2. *Which project do I pass to `-p` for a migration?* (skip if no migrations)
3. *Where does a new domain module go?*

On the greenfield branch those probes have no answers to have. Use these instead:

1. *What is this project, and who uses it?*
2. *What must never be done in this repository?*
3. *Which parts of this file have to be rewritten once code exists?*

Any answer that requires guessing is a failure of the file, not of the reader.
Add what was missing, then trim something else to stay under 200 — and re-run
this phase, because the trim may have broken another probe.

### PHASE 7 — Write and hand over

Write `CLAUDE.md` at the repository root. Then **show the user what changed and
propose the commit — do not commit and do not push.** State the line count and
which of the three probes the file answers.

Finally, tell the user the two facts that make the file work: run `/context` to
confirm it loaded, and add to it whenever Claude repeats a mistake.

## Update mode

An existing `CLAUDE.md` was written or tuned by a human. Treat it as authoritative
about intent and unreliable about facts.

1. **Scan first anyway** (PHASE 1) — commands, layout and packages drift; the
   file does not.
2. **Diff, do not regenerate.** Produce a change list: lines that are now factually
   wrong, lines that duplicate an analyzer, lines Claude can derive from the code,
   and genuine gaps. Show it before editing.
3. **Ask the update question:** *"What mistake did Claude repeat recently?"* Every
   answer is a candidate rule — this is the highest-yield source of real rules
   there is, because it is evidence rather than prediction. Add each as one
   falsifiable imperative line.
4. **Preserve deliberate oddities.** A rule that looks wrong may be a decision.
   Ask before removing anything that reads like a hard constraint; never delete a
   rule merely because its reason is not written down.
5. **Settle the provisional content first.** Any `Planned, not yet built` section
   and any line carrying an `unverified` source comment is now due: check it
   against the code that exists. Each one is either deleted (it happened, and the
   code says so better), promoted to a rule (it was decided), or re-marked (still
   not built). Nothing stays provisional across two updates without being said
   out loud.
6. **Trim and verify** (PHASES 5–6) exactly as in create mode, then hand over.

## Hard constraints

These are not preferences. A draft violating any of them is rejected and redrafted.

- **Never paste generic content the model already knows.** If the sentence would
  be equally true in a Node repository, it does not belong here.
- **Never duplicate a linter or analyzer rule.** Formatting, naming casing, using
  ordering, XML-doc presence and nullable warnings are all out of scope by
  construction.
- **Every line is project-specific or an approved static rule.** No third source.
- **Never invent a command.** A command ships only when the CI config, the
  solution layout or the user supplied it. A document is not one of those three.
- **Never present planned content as existing.** Anything taken from a document
  reads as intended, is marked with its source, and is re-checked once there is
  code to check it against.
- **Never write a secret into the file**, and never quote a config *value* — key
  names only. This holds even where the repository intentionally commits real
  credentials.
- **Never exceed 200 lines**, and never reach the limit by deleting commands.
- **Do not write `CLAUDE.md` files into directories the user did not name**, and
  do not commit. Proposing the commit is the last step; approving it is the
  user's.

## Resources

- **`references/scan-map.md`** — read at PHASE 1. The scan table (file → inference
  → fallback) and the three user questions.
- **`references/static-rules.md`** — read at PHASE 3. The approved static rules,
  grouped by topic, each with its `Applies when` condition and slots.
- **`references/template.md`** — read at PHASE 4. Section skeleton, ordering,
  required versus conditional, per-section line budgets.
- **`references/checklist.md`** — read at PHASE 5. The anti-pattern checklist, in
  cutting order.
