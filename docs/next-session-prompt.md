# Opening prompt — Session S5

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
  Source of exemplar code for the later `adapt` sessions.
  **Do not open it in S5.** No Group C or D row needs an exemplar.

S0–S4 are complete. **Group A is closed (33/33). Group B is closed (33/33).**
`docs/TRIAGE.md` holds **94 rows, 66 decided**: Group A 33/33 ✅ · Group B 33/33 ✅ ·
Group C 0/1 · Group D 0/27.

**S5 is the gate into Phase 2.** Triage ends when this session ends — TRIAGE must reach
**zero `pending` rows everywhere**, and the three un-enumerated components must be resolved.

## FILES TO READ FIRST

1. `docs/TRIAGE.md` — the Group C row, all 27 Group D rows, the progress block, and **the
   decision log, which now has 33 entries** (S1 ×3, S2 ×8, S3 ×10, S4 ×12). Read the
   **twelve S4 entries** — several bind S5
   directly: the Q2/Q3/Q4 answers, the B04 memory conflict, the `combine`/`skip` line, the
   B11 token-economics salvage, the B28 correction of record, and the context-discipline entry,
   which lists four Group D rows already carrying known cross-references.
2. `docs/01-triage-rules.md` — **§5 (Group C)** and **§6 (Group D)**, plus §7 (R4, R8, R9).
   This is the rule set S5 runs on. It is short; read all of it.
3. `docs/00-brainstorm.md` §2 (the out-of-scope table that drives R4) and §5 (the four review
   rubrics — several Group D rows are candidate content for them).

`docs/02-repo-structure.md` is needed for the packaging questions at the end (`NOTICE`, the
`run-hook.cmd` wrapper, where a kept MCP config would live). `docs/03-session-roadmap.md` is
not needed except to record a deferred request.

## THE SINGLE DELIVERABLE OF THIS SESSION

**Decide the 1 Group C row and all 27 Group D rows, and resolve the 3 un-enumerated
components. Nothing else.**

| Block | Rows | Count |
|---|---|---|
| Group C — MCP | C01 `mcp/CWM.RoslynNavigator/` | 1 |
| D.1 — `.claude/rules/` | D01–D10 | 10 |
| D.2 — `knowledge/` | D11–D16 | 6 |
| D.3 — `knowledge/decisions/` (ADRs) | D17–D22 | 6 |
| D.4 — `templates/` | D23–D27 | 5 |
| Un-enumerated | `mcp-configs/`, root `.mcp.json` → Group C · root `.editorconfig` → Group D | 3 |

**Every Group D row needs a `Destination`, and it is one of exactly three values** (rules §6):
**skill content** · **project `CLAUDE.md` material** · **drop**. A row without a Destination is
not decided. Note what the middle value means: shipping a per-project `CLAUDE.md` template is
**backlog, not v1** (brainstorm §2, Q6), so "project `CLAUDE.md` material" means *recorded for
tier 3*, never *shipped in v1*.

**Done when:** C01 has a Status and, if kept, its install command and `.mcp.json` shape are
recorded per §5; all 27 Group D rows carry Status + Destination + Reason; the three
un-enumerated components have a disposition; **`grep -c pending docs/TRIAGE.md` returns only
legend and cross-reference hits — zero row-level `pending` in any group**; the progress block
reads 94/94; committed.

## DECIDE C01 FIRST — SEVEN DECIDED ROWS ARE WAITING ON IT

C01 is the highest-leverage row in the file and it is **not** just another disposition. Seven
already-decided rows state a C01 dependency in their Reason, and S5 is where those conditionals
resolve. Do it before Group D, then note the consequence in the decision log.

| Consumer | What it loses without C01 | Survives? |
|---|---|---|
| A02 `arch-check` | project-graph + cycle automation | yes — manual `.csproj` inspection |
| A09 `code-review` | `detect_antipatterns`, `get_diagnostics` | yes — blast-radius + priority tables are the substance |
| A13 `de-sloppify` | `find_dead_code`, `get_type_hierarchy` | yes — the taxonomy is tool-independent |
| A32 `security-scan` | `get_endpoint_map` in Layer 4 only | yes — Layers 1, 2, 5, 6 are CLI scans |
| B06 `outdated` | `get_nuget_packages` inventory | yes — degrades least; read `Directory.Packages.props` |
| B10 verify pipeline | Phases 2 and 3 | yes — Phase 2 → build warnings, Phase 3 → B29's four grep patterns |
| **B18 `ef-core-specialist`** | `find_references`/`find_symbol` — **its whole reason to exist** | **contingent — re-examine this row if C01 is dropped** |

**B18 is the only row in the plugin whose disposition is conditional on C01.** It was kept
because an N+1 hunt across a solution is the "verbose journey, concise answer" case that
justifies a subagent — and that saving comes from the MCP tools. If C01 is dropped, revisit it
and say so in the log.

Two S4 findings feed this decision and are recorded for exactly this moment:

- **The quantified case (salvaged from B11).** A typical `.cs` file costs 500–2000 tokens; a
  Roslyn MCP query costs 30–150. Understanding a type via four MCP calls costs ~310 tokens
  against ~2900 for reading the four files. Until now the C01 case was a dependency list with
  no numbers.
- **The wiring gap (salvaged from B12).** Roslyn MCP tools need the solution located before
  they work — "find `.slnx`/`.sln`: current dir, then parents, then children". S4 refused to
  build a hook for it (Q4). If C01 is kept, that belongs in C01's `references/` wiring notes.

The §5 default is **`keep` as an externally installed dotnet tool, not copied into the plugin** —
it conflicts with nothing. If you keep it, §5 requires recording the **install command** and the
**`.mcp.json` shape** in the destination skill's `references/`, so a future project can wire it
up without rediscovery. `mcp-configs/` and the root `.mcp.json` are the un-enumerated components
that belong to this decision — raise them here.

## APPLY THE R4 SHORT-CIRCUITS FIRST, BEFORE ANY READING

Two Group D rows are R4 (`01-triage-rules.md` §7 — subject area excluded by brainstorm §2, set
to `skip`, Reason `out-of-scope v1`, **no deep reading**):

- **D23 `templates/blazor-app/`** — Blazor is in the §2 exclusion table.
- **D25 `templates/modular-monolith/`** — modular monolith is in the §2 exclusion table.

Do these first; it is what freed the context budget in S3 and S4. **They are the only two.**
Group A produced 4 R4 skips and Group B produced 1 — do not stretch R4 to cover a row whose
subject merely overlaps an excluded area. Everything else that ends up `skip`/`drop` is a
**reasoned** skip and its Reason must say so, so a later session cannot mistake it for a
short-circuit.

## THE AUTO-INJECTION IS NOW THE DELIVERABLE, NOT A WIDENING

In S2, S3 and S4 the harness auto-injected the kit's root `CLAUDE.md` and all ten
`.claude/rules/*.md` whenever a kit skill file was read, and all three sessions had to record
that they had seen D01–D10 without deciding them. **In S5 that inversion ends: those ten files
*are* rows D01–D10.** Seeing them is now legitimate and expected. Two consequences:

1. You may already have D01–D10 in context before you deliberately open anything. Use it.
2. **The remaining 17 Group D rows are not auto-injected** — `knowledge/*.md` (D11–D16), the six
   ADRs (D17–D22) and the five templates (D23–D27) must be read deliberately. Budget for that,
   and skip D23 and D25 per R4 above.

## WHAT S2–S4 DECIDED THAT BINDS S5

Settled. Apply them, do not re-litigate them.

**1 — My stack.** Controllers (MVC), **not** Minimal API · Swagger UI / Swashbuckle, **not**
Scalar · **no** API versioning · MediatR + FluentValidation + AutoMapper · Redis · Elasticsearch.
I am **staying on MediatR** — not migrating to Mediator or Wolverine. This has now killed or
rewritten five rows across three sessions (A27, A35, part of B05, part of B06, and it shaped the
`cqrs-feature-slice` `rebuild`). It bears directly on **D15** and **D16** below.

**2 — Q1 is still open and S7 owns it.** My real architecture is explicitly **not** Clean
Architecture, and it stays unnamed until S7. Any Group D row that *prescribes* a layering or
selects between architectures pre-empts Q1 and cannot be inherited. This has already killed A03,
A08, A35, B07 and B17. **D02 and D17 and D21 are the rows where it fires again.**

**3 — Four Group D rows already carry known cross-references**, flagged by S3 and S4 for exactly
this session. These are flags, not decisions:
- **D06 `hooks.md`** — contains a "never `--no-verify`" rule that **B29's own script contradicts**
  by telling the user to bypass with it. B29 shipped only the four detection patterns, not the
  blocking gate. Only one hook survives S4 (`post-edit-format`), so most of this file documents
  hooks that do not exist here.
- **D08 `performance.md`** — contains the **HybridCache default** that both **A06** and **B19**
  diverge from, because my stack is Redis (§2). Strong candidate content for the
  `dotnet-performance-review` rubric, which S4 gave its only anchor.
- **D10 `testing.md`** — covers the same ground as **A34** (`from-kit + from-research`) and
  **B09**'s combine. Do not ship the same material twice; decide which file owns it.
- **D14 `dotnet-whats-new.md`** — overlaps **A24 `modern-csharp`**, which already carries the
  C# 14 / .NET 10 feature material. Same instruction: one owner.

**4 — Two more rows are pre-flagged by earlier decisions:**
- **D15 `mediatr-to-mediator-migration.md`** — S2 recorded this is **less** relevant, not more,
  because I am staying on MediatR. Both B05 and B06 had to have their pointers to it stripped.
- **D12 `common-antipatterns.md`** — strong **R8** anti-example material for the review rubrics,
  and it is referenced by rows that were skipped (B15, B19). If it is kept, A09/A13/A32 are its
  consumers, not an agent.

**5 — `.editorconfig` has a consumer now.** B28's `combine` carries a six-item solution-hygiene
checklist into `solution-architecture ⚠️`, and one of the six checks is "`.editorconfig`
present". B03 `convention-learner` (kept) also treats `.editorconfig` and
`Directory.Build.props` as **always winning** over both kit and plugin defaults. So the
un-enumerated root `.editorconfig` is not a loose end — decide it as a Group D row with a real
consumer.

**6 — The `combine` / `skip` line, adopted in S4.** A row is `combine` when named material ships
to a **named destination**; it is `skip`/`drop` + a decision-log entry when the salvage is real
but has no single destination. Group D's Destination column enforces the same discipline by
construction — which is why a Destination is mandatory and "pending" is not one of the values.

**7 — Two attribution obligations, not one.** **R9** requires a `NOTICE` crediting
codewithmukesh/dotnet-claude-kit and reproducing the MIT text, since anything
`keep`/`keep-tweak`/`adapt`-ed is a derivative work. S4 added a **second**: `dotnet-standards`
ships its own copy of the polyglot `run-hook.cmd` pattern, copied from Superpowers (MIT), because
referencing another plugin's internal path is forbidden. Both are **S6 work** — record them, do
not create files.

## THINGS S5 MUST HANDLE

**An S1 summary is an enumeration artifact, not evidence.** S4 found that B28's summary
described a check the script does not perform, and that error had been carried since S1 and
would have produced the wrong disposition. **Where a Group C or D decision turns on what a file
actually contains, open the file.** This matters most for D11–D16 and the six ADRs, whose
one-line summaries were written without reading them.

**The ADRs are the kit author's decisions, not mine.** D17–D22 are explicitly flagged in TRIAGE
as such, and three are known to diverge: **D17** (VSA as default — pre-empts Q1), **D20**
(HybridCache over `IDistributedCache` — my stack is Redis), **D21** (multi-architecture support —
`dotnet-standards` targets exactly one architecture, mine). The kit being multi-architecture *by
design* is the single largest cause of skips in this project; D21 is the ADR that states that
design. Judge each ADR on whether it matches my real conventions, not on whether it is
well-argued.

**Templates are backlog, and two of the five are in-scope shapes.** D26 `web-api/` and D27
`worker-service/` match my project shapes (§2), but shipping a per-project `CLAUDE.md` template
was declined for v1 (Q6) — that is a **deferral, not an exclusion**, which is exactly why A16
was a *reasoned* skip and not R4. Decide D24, D26 and D27 on that basis; D23 and D25 are R4.

**Do not decide Group A or Group B rows.** Both groups are closed. If S5 surfaces something that
looks like it changes a decided row — and the C01 decision may genuinely do this for **B18** —
**record it in the decision log as a flag for S6/S7; do not edit the decided row's Status.**
The one exception is B18, and only because its Reason already states the contingency in advance.

## HARD CONSTRAINTS

**1 — One session, one deliverable.** S5 decides Group C and Group D rows and the three
un-enumerated components. Do **not** write a skill, a hook, a `NOTICE` or a `plugin.json`; do not
install the MCP server; do not touch `reference/projects/`; do not modify any Superpowers file.
If I ask for more mid-session, refuse and record the request in `docs/03-session-roadmap.md`
under a "Requests deferred out of S5" heading. This is a design constraint, not a suggestion.

**2 — Context discipline.** Apply the D23/D25 R4 short-circuits first, before any reading. Then
decide C01, then D01–D10 (likely already injected), then read D11–D22 deliberately. The rules
files are ≤100 lines each by the kit's own budget; the knowledge files and ADRs are longer.
Announce any widening up front: what you are looking for, and why.

**3 — Artifact language is English.** All generated files — docs, TRIAGE, and later skills and
descriptions — are written in English. Talk to me in Vietnamese.

**4 — End-of-session ritual.** Commit with a clear message, then rewrite
`docs/next-session-prompt.md` so it opens **S6**. S6 is the first Phase 2 session: repo scaffold,
`plugin.json`, the `NOTICE` file for **both** MIT obligations, the `run-hook.cmd` wrapper, and
the `post-edit-format` hook plus its manifest — the only hook that survived triage. Before
writing that prompt, confirm the triage phase is genuinely closed: **94/94, zero row-level
`pending`.**

Start by confirming you understand the constraints, then read the files listed above.
