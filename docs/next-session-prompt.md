# Opening prompt — Session S6

> Copy everything below the line into a **fresh** Claude Code session opened in
> `D:\ALTA\Project\dotnet-standards`.

---

## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin that holds my .NET knowledge.
It runs alongside **Superpowers** (the process layer) as an independent plugin.
**No Superpowers file may ever be modified.** This is absolute and permanent.

Three-tier architecture: Superpowers = process · `dotnet-standards` = knowledge ·
per-project `CLAUDE.md` = glue.

Reference material, **read-only, never installed as a plugin**:
- `reference/dotnet-claude-kit` — codewithmukesh/dotnet-claude-kit (MIT), pinned at
  commit `cd83d315986c27621da178dad73bd95d503c1540`. Every decision is anchored to that SHA.
- `reference/projects/` — my real .NET projects (gitignored): `apsp-backend` and `ops-service`.
  Source of exemplar code for the `adapt` sessions. **Do not open it in S6.** S6 writes no skill
  and distils no code; the first session that needs it is S7.

**Phase 1 is over. Triage is closed.** `docs/TRIAGE.md` holds **94 rows, 94 decided** — Group A
33/33 ✅ · Group B 33/33 ✅ · Group C 1/1 ✅ · Group D 27/27 ✅ — with **zero row-level `pending`
in any group**, plus the three un-enumerated components S1 flagged (`mcp-configs/` `skip`, root
`.mcp.json` `combine`, root `.editorconfig` `keep-tweak`). The decision log has **45 entries**
(S1 ×3, S2 ×8, S3 ×10, S4 ×12, S5 ×12).

**S6 is the first Phase 2 session.** Phase 1 decided *what* travels. Phase 2 builds it, and S6
builds the part that has nothing to do with .NET knowledge: **the plugin skeleton that makes
everything else installable.** Nothing S6 produces contains domain content.

## FILES TO READ FIRST

1. `docs/02-repo-structure.md` — **all of it.** This is S6's specification: §1 the directory
   layout, §2 the five verified hard rules, §3 `plugin.json`, §4 the marketplace and the local
   install/restart loop, §5 the component formats, §6 the Windows polyglot hook wrapper.
   **Read §5 and §6 knowing they are partly stale** — see "WHAT S4 AND S5 MEASURED" below; S6
   reconciles them rather than following them blind.
2. `docs/TRIAGE.md` — the **three hook rows only** (B23 `hooks.json`, B24 `post-edit-format.sh`,
   B31 `hooks/README.md`) and the **S5 decision-log entries** on C01 and on the closure of triage.
   You do **not** need the Group A or Group D rows: S6 writes no skill.
3. `docs/01-triage-rules.md` **§7 (R9)** — the MIT attribution obligation, one of the two the
   `NOTICE` must discharge.

`docs/00-brainstorm.md` is not needed except §1 (purpose) for the plugin description string.
`docs/03-session-roadmap.md` is not needed except to record a deferred request.

## THE SINGLE DELIVERABLE OF THIS SESSION

**A `dotnet-standards` plugin that installs from my local marketplace and whose one hook
demonstrably fires. Nothing else.** Six artifacts:

| # | Artifact | Source of truth |
|---|---|---|
| 1 | Repo scaffold — the directories from `02-repo-structure.md` §1 | §1 + §2's hard rules |
| 2 | `.claude-plugin/plugin.json` + `.claude-plugin/marketplace.json` | §3 + §4 |
| 3 | `NOTICE` — **two** MIT obligations | R9 + the S4 Q2 entry |
| 4 | `hooks/run-hook.cmd` — the polyglot wrapper | §6, reconciled with S4's Q2 findings |
| 5 | `hooks/post-edit-format` — **extensionless**, the only hook that survived triage | B24 |
| 6 | `hooks/hooks.json` + `hooks/README.md` | B23 (rebuilt manifest) + B31 |

**Done when:** the plugin installs via `/plugin marketplace add` + `/plugin install`; after a
restart, editing a `.cs` file in a real test solution causes `post-edit-format` to run and the
file to be formatted; `hooks/README.md` documents the one hook and the wrapper's failure mode;
`NOTICE` credits both sources with licence text; committed.

**Verify the hook by running it, not by reading it.** Create a throwaway solution under the
scratchpad (not in this repo, not in `reference/`), edit a `.cs` file in it, and confirm the
formatting actually happened. If the hook silently does not fire, say so plainly — that is the
exact failure mode S4 measured, and a claim that it works without having seen it work is worse
than no hook.

## WHAT S4 AND S5 MEASURED — THIS OVERRIDES `02-repo-structure.md` WHERE THEY DISAGREE

`02-repo-structure.md` was written in S0 from documentation. S4 measured the Windows behaviour
and S5 measured the MCP server. **Four points where the doc is now stale or incomplete:**

**1 — Hook scripts must be extensionless.** Claude Code on Windows **prepends `bash` to any
command containing `.sh`**, which makes the kit's own `bash "…/x.sh"` command form unusable and
breaks the wrapper's argument. §5's example command is
`"${CLAUDE_PLUGIN_ROOT}/hooks/run-hook.cmd" format.sh` — **the `.sh` must go.** Name the script
`post-edit-format` with no extension and pass it as `post-edit-format`. Fix §5 in the doc as part
of this session, since a stale spec is what caused this.

**2 — The wrapper exits 0 silently when no bash is found, and that is *accepted* here, not
overlooked.** Superpowers' own `run-hook.cmd` behaves this way. Q2 decided a hook may ship on
Windows **only if its silent absence is benign by design** — for `post-edit-format` it is (code
is unformatted but correct, and B10 Phase 6 catches the drift before review), which is precisely
why `pre-bash-guard` was refused: for a guard the same silence is fail-open. **§6's draft wrapper
has no such fallback** — it would hard-fail instead. Decide deliberately which behaviour to ship,
and document the choice in `hooks/README.md`. It is a real decision, not boilerplate.

**3 — The wrapper is copied in *pattern*, never referenced across plugins.** Depending on
Superpowers' internal path would couple the plugins and is forbidden by the golden rule of
`combine`. That copy is what creates the **second MIT obligation**.

**4 — C01 is an external tool, and S5 confirmed there is nothing for S6 to build for it.**
`plugin.json` ships **without an `mcpServers` block** — §3 called this an S0 default; it is now a
decided disposition. And do not build solution detection: S5 found that the S4-recorded "wiring
gap" **is not a gap** — the server resolves the solution itself in four steps, including one-shot
MCP roots discovery, and the kit's own `.mcp.json` passes no arguments at all. **S6 owes the MCP
server zero code and zero config.** Its install command and `.mcp.json` shape are already
recorded in the C01 row and become a `references/` file in **S7**, not now.

## THE `NOTICE` FILE — TWO OBLIGATIONS, NOT ONE

Get this right; it is the one legal artifact in the project.

1. **R9 — codewithmukesh/dotnet-claude-kit (MIT).** Anything `keep`/`keep-tweak`/`adapt`-ed is a
   derivative work, and after triage that is **30 rows** (Group A: 2 `adapt` + 20 `keep-tweak`;
   Group B: 5 `keep`; Group D: 3 `keep-tweak`), plus **22 `combine` rows** (10 in Group B, 12 in
   Group D) that carry kit material into new files.
   Credit the project, reproduce the MIT text, and **name the pinned SHA** — the attribution
   should say *what* was derived from, not just *who*. `reference/dotnet-claude-kit/LICENSE` is
   the text to reproduce.
2. **Superpowers (MIT) — the `run-hook.cmd` polyglot pattern.** Added by S4 because the wrapper is
   copied rather than referenced. Credit it separately and say what was copied.

**Do not invent a third obligation for C01.** Nothing of `CWM.RoslynNavigator` is copied — it is
an external MIT NuGet dependency that this plugin *documents*, and it lives inside the same kit
repository obligation 1 already covers. A sentence noting it is documented, not vendored, is
correct; a separate licence block is not.

## WHAT IS SETTLED AND MUST NOT BE RE-LITIGATED

**1 — The five hard rules of `02-repo-structure.md` §2 are verified, not assumed.** In particular:
`.claude-plugin/` holds **manifests only** (skills, agents, hooks live at the plugin **root** —
putting them inside breaks loading); `hooks/hooks.json` is **auto-loaded**, so it must **not**
also be declared under `plugin.json`'s `manifest.hooks` field, which produces a *"Duplicate hooks
file detected"* error; every path in a config file uses **`${CLAUDE_PLUGIN_ROOT}`, quoted**, never
an absolute path; scripts need `chmod +x`.

**2 — Exactly one hook ships, and the other eight are decided.** B24 `post-edit-format` is the
only `keep`. It runs `dotnet format <project> --include <file> --no-restore`, scoped to the
nearest `.csproj`, and swallows all failures — that scoping is what makes the per-edit cost
acceptable inside a tight red-green loop. **Do not add a second hook.** `pre-bash-guard` was
refused on the fail-open reasoning above, and the `UserPromptSubmit` skill-index hook was refused
outright by Q4. If a new hook seems necessary, that is a signal to re-read the Q2 and Q4 entries,
not to write it.

**3 — S6 writes no skill, no agent, no command, and no `references/` file.** The 15 gateway
skills, the four review rubrics and every `references/*.md` named in TRIAGE belong to **S7–S8**.
S6 creates the *directories* they will live in and nothing inside them. A scaffold with empty
`skills/` is the correct output; a scaffold with one hand-written skill is scope creep.

**4 — Open questions S6 inherits and does not own.** **Q1** (my architecture, unnamed — S7), the
**repository-over-EF-Core** question (four contradictory sources, two of them inside the kit),
the **HybridCache-vs-Redis** resolution (four rows, one S7 decision), the **commercial-licence
three-way choice** (see below), and the **AutoMapper-vs-projection** R7 question (S8). Q2, Q3, Q4
and Q5 are closed. **None of these blocks S6** — S6 touches no domain content — so do not open
them, and do not let one become a reason to stall.

**5 — My stack, for the description string only.** Controllers (MVC), not Minimal API · Swagger
UI / Swashbuckle, not Scalar · no API versioning · MediatR + FluentValidation + AutoMapper ·
Redis · Elasticsearch. Web API and Worker service project shapes.

## TWO S5 FINDINGS THAT TOUCH S6 IN PASSING

Neither is S6's deliverable. Both are recorded so they are not lost between sessions.

**Perishable knowledge now has a home in the repo.** S5 found four kept items with hard expiry
dates — the nearest being **.NET 11 GA on 2026-11-10**, which will stale D11's breaking-changes
material and D14's "do not generate `net11.0`/C# 15" guardrail. S6 should make that survivable
rather than solve it: put the **pinned kit SHA and an "as of" date** in `README.md`, and note in
`CHANGELOG.md` that the knowledge layer carries dated content. The re-pin trigger in
`01-triage-rules.md` §7 now has a second form — *the .NET release train moved past what we
recorded* — and that sentence belongs in the rules file if it is not already there.

**Three packages in my live stack are commercially licensed.** MediatR from v13, AutoMapper from
v15, FluentAssertions from v8. This is an S7 matter (it lands in B06's licence table) and **not**
a licensing question about this plugin — `dotnet-standards` ships none of them. Mentioned only so
S6 does not confuse it with the `NOTICE` work, which is about the kit and Superpowers alone.

## HARD CONSTRAINTS

**1 — One session, one deliverable.** S6 builds the six artifacts above. Do **not** write a skill
or a `references/` file; do not install `CWM.RoslynNavigator`; do not add a second hook; do not
re-open a TRIAGE row — triage is closed and its rows are the input to S7, not a subject for S6.
Do not touch `reference/projects/`. Do not modify any Superpowers file. If I ask for more
mid-session, refuse and record the request in `docs/03-session-roadmap.md` under a
"Requests deferred out of S6" heading. This is a design constraint, not a suggestion.

**2 — Prove it, do not assert it.** This is the first session whose output either runs or does
not. Install the plugin, restart, trigger the hook on a throwaway solution in the scratchpad, and
report what actually happened including the failures. "A restart is required for changes to take
effect" (§4) — a missing restart is the usual cause of "it didn't work", so rule that out before
concluding anything is broken.

**3 — Artifact language is English.** All generated files — docs, TRIAGE, `NOTICE`, `README.md`,
and later skills and descriptions — are written in English. Talk to me in Vietnamese.

**4 — End-of-session ritual.** Commit with a clear message, then rewrite
`docs/next-session-prompt.md` so it opens **S7**. S7 is the first knowledge session: it resolves
**Q1** by naming my real architecture from the `ops-service` exemplars, promotes A05 and A33 from
recorded paths to distilled content, and starts building the gateway skills — which means S7 is
the first session allowed to open `reference/projects/`. Before writing that prompt, confirm the
scaffold genuinely works: **plugin installs, hook fires, verified by running it.**

Start by confirming you understand the constraints, then read the files listed above.
