# 01 — Triage Rules

> Governs how every component of `codewithmukesh/dotnet-claude-kit` (pinned at
> `cd83d315986c27621da178dad73bd95d503c1540`) is dispositioned into `dotnet-standards`.
> These rules were refined during S0. The six status values are **fixed** — refinements
> add dimensions and constraints around them, they do not replace them.

---

## 1. Status values

| Status | Meaning |
|---|---|
| `keep` | Take the kit's component essentially as-is. |
| `keep-tweak` | Take it, with edits — wording, examples, package versions, .NET version. |
| `adapt` | Keep the kit's skill skeleton, replace the substance with the user's real code and conventions. |
| `rebuild` | Write from scratch. The kit's version is unusable or absent. |
| `skip` | Do not carry it over. |
| `combine` | Merge with something else — a Superpowers capability, or another kit component. |

Every non-`pending` row **must** carry a Reason.

## 2. Provenance (rule R1)

`provenance` is a **separate axis** from status. Status answers *"what do we do with the kit's
component?"*. Provenance answers *"where does the knowledge come from?"*. Conflating them is
what hid the testing gap during S0.

| Provenance | Meaning |
|---|---|
| `from-my-code` | Distilled from exemplar files the user names. |
| `from-kit` | The reference kit is the source of truth. |
| `from-research` | Built from web research (docs, established practice). Sources must be cited in the skill. |
| `mixed` | Two or more of the above. State which parts come from where. |

Canonical example: `dotnet-testing` is `keep-tweak` (status) with provenance
`from-kit + from-research` — the user writes no tests today, so there is nothing to adapt, but
the topic is in scope and wanted.

---

## 3. Group A — Knowledge skills

Kit examples: `ef-core`, `minimal-api`, `caching`, `clean-architecture`, `serilog`, `resilience`.

**Base rule**
- The user **has** exemplar code / a convention → `adapt`. Keep the kit's skeleton, replace the
  substance.
- The user **has no** exemplar code → `keep` or `keep-tweak`, with a quality note. Mark
  `upgrade candidate` so it can be promoted to `adapt` once exemplars exist.

**R2 — `Destination` column is mandatory.**
Because of packaging mechanism **A** (gateway skills + `references/`), the relationship is
**many kit skills → one gateway skill**. Every Group A row must record which gateway skill it
lands in and which `references/*.md` file it becomes. A decision without a destination is
incomplete.

**R3 — `combine` is available to Group A.**
Previously `combine` was Group-B-only. Folding the kit's `caching` and Redis material into a
single `distributed-caching` gateway skill *is* a combine. Reuse the existing status; do not
invent a new one.

**R6 — `adapt` is gated on exemplars.**
A row may not be set to `adapt` until the user has named specific exemplar files. Without named
exemplars the correct value is `keep-tweak` + `upgrade candidate`. This prevents planning work
that cannot be executed.

**R8 — Record anti-examples.**
TRIAGE carries an `Anti-examples` column. When the user supplies code they explicitly do **not**
want repeated, it is recorded and the resulting skill must say "avoid this" — not only "do this".
A skill that only shows the good path does not prevent the bad one.

---

## 4. Group B — Process layer

Covers agents, workflow commands, meta-skills and hooks.

**Never default to `skip`.** Each component is compared against the equivalent Superpowers
capability and assigned one of:

- `skip` — Superpowers already does this as well or better.
- `keep` — Superpowers does not have it and the user needs it.
- `combine` — Superpowers has a base version; the kit's material extends it.

**R5 — Conflict check is mandatory for every `keep` and `combine`.** Five items, all answered
explicitly in the TRIAGE row:

1. **Hook events** — does it register on the same event as a Superpowers hook?
2. **Slash-command names** — does it collide with Superpowers *or* with a Claude Code built-in
   (`/code-review`, `/security-review`, `/review`, `/init`)?
3. **Skill names** — does it collide with a Superpowers skill name?
4. **Instructions** — does it contradict the brainstorm → plan → TDD → review flow?
5. **Agent names** — does it collide with an existing agent?

An unresolvable conflict downgrades the row to `skip`.

**Golden rule of `combine`:** the extension always lives inside `dotnet-standards` as a new
skill or hook. **No Superpowers file is ever modified.** This is absolute.

**Windows hook cost.** The kit's hooks are `.sh`. On Windows, Claude Code runs hooks through
`CMD.exe`, which cannot execute `.sh`. Keeping any kit hook requires shipping the polyglot
`run-hook.cmd` wrapper and depends on Git for Windows being installed. Record this cost in the
row's Reason — it is part of the decision, not an implementation detail.

---

## 5. Group C — MCP server

`CWM.RoslynNavigator`: default `keep` as an **external tool** — a separately installed dotnet
tool. It is not copied into the plugin and conflicts with nothing.

If kept, record the install command and the `.mcp.json` shape in the destination skill's
`references/`, so a future project can wire it up without rediscovery.

---

## 6. Group D — Rules and project templates

Evaluated individually. A rule that is worth keeping becomes **content inside a skill** or
material for a tier-3 project `CLAUDE.md`. The kit's own rules mechanism is not preserved by
default.

Each Group D row records a `Destination`: skill content · project `CLAUDE.md` material · drop.

---

## 7. Cross-cutting rules

**R4 — Out-of-scope short-circuit.**
Any component belonging to an area excluded in `00-brainstorm.md` §2 (Blazor, modular monolith /
microservices, CI/CD, Docker, Kubernetes, container publishing, Aspire) is set to `skip`
immediately, Reason = `out-of-scope v1`. **No deep reading.** This exists to protect the triage
sessions' context budget.

**R7 — One canonical source per skill.**
Each skill draws from exactly **one** project, chosen by the user. Other projects are for
comparison only. When conventions diverge, ask the user *"which one do you want from now on?"*
and record the answer. **Never average two conventions.** Averaging produces a convention that
exists in no real codebase.

**R9 — MIT attribution.**
The reference kit is MIT-licensed. Anything `keep`/`keep-tweak`/`adapt`-ed is a derivative work.
The plugin must ship a `NOTICE` file crediting `codewithmukesh/dotnet-claude-kit` and reproducing
the MIT license text. This is a legal obligation, not a courtesy.

**Pinning.** Every triage decision is anchored to the pinned SHA. Re-pinning to a newer kit
commit is a deliberate act recorded in the TRIAGE decision log, and it invalidates nothing
automatically — but rows touching changed files must be revisited.

**R10 — Two re-pin triggers, not one.** The second was added in S6 after S5 found four kept items
with hard expiry dates.

1. **The kit moved.** A file a decided row depends on changed upstream. Revisit that row.
2. **The .NET release train moved past what we recorded.** Version guidance, breaking-change
   notes, package-version advice and "do not generate X yet" guardrails all go stale on a
   schedule, independently of whether the kit changed a line. The nearest known date is
   **.NET 11 GA on 2026-11-10**.

Trigger 2 obliges a check even when the kit is untouched, and it can force a re-pin *or* a
straight rewrite of the affected content from current sources. `README.md` carries both the pinned
SHA and an **"as of" date** so the second trigger is checkable at a glance; a stale "as of" line is
a defect, not cosmetics.

---

## 8. TRIAGE row schema

**Group A**

| Field | Notes |
|---|---|
| Path | Kit path at the pinned SHA |
| Summary | One line |
| Status | one of the six |
| Provenance | R1 |
| Destination | R2 — gateway skill + `references/` file |
| Canonical source | R7 — project → feature/paths (required for `adapt`) |
| Anti-examples | R8 |
| Sanitized? | ticked after distillation review |
| Reason / Notes | required |
| Upgrade candidate? | R6 |

**Group B**

| Field | Notes |
|---|---|
| Path · Summary · Superpowers equivalent? | |
| Conflict check | R5 — all five items answered |
| Status · Reason | required |

**Group C** — Component · Status · Notes
**Group D** — Path · Summary · Status · Destination · Reason
**Decision log** (append-only) — Date · Session · Component · Decision · Why
