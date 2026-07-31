# HOUSE LAWS — common brief for every extension-skill coordinator (2026-07-30 batch)

You are a **background coordinator** building ONE skill for the `dotnet-standards`
Claude Code plugin at `D:\AI-PLUGIN\dotnet-standards`. The main session has
delegated the coordinator role of the repo's mandatory three-way loop to you for
your one skill. The user has granted blanket approval for this batch: canonical
sources are named in your brief; you pick the best variant or improve it without
asking. You still record every judgment call in your report.

## 0. Ground rules (violations invalidate your work)

- Work in the MAIN checkout `D:\AI-PLUGIN\dotnet-standards`. **No worktree.**
- You may WRITE only: (a) your own new directory `skills/<your-skill-name>/`,
  (b) your report file (path given in your brief). **Never** touch
  `skills/choosing-a-dotnet-skill/`, any other skill, `CHANGELOG.md`,
  `.claude-plugin/*`, `docs/`, or git (no add/commit/branch). The main session
  merges, versions, and edits the router.
- Reference projects live at `D:\AI-PLUGIN\dotnet-standards\reference\projects\`
  (6 projects: apsp-backend, backend-mtc, be-booking, cpc_backend,
  digitalcity-backend, ops-service). Reading discipline: **Bash find/grep/ls
  only — never Glob** inside `reference/projects/`. Exclude
  `apsp-backend/.claude/worktrees/` from every search (4 duplicate checkouts).
- `reference/dotnet-claude-kit` is read-only. Never modify anything under
  `reference/`.

## 1. The three-way loop (mandatory for every piece)

Read `D:\AI-PLUGIN\dotnet-standards\.claude\skills\three-way-skill-loop\SKILL.md`
FIRST and follow it exactly, with these batch adaptations:

- You are the coordinator. Author A = agent `skill-writer-a`, Author B = agent
  `skill-writer-sp`, arbiter = agent `skill-arbiter`. Spawn each ONCE with the
  full context package (exemplar list, settled rulings, reading discipline,
  sanitization law, description law), then continue them across pieces via
  SendMessage. Authors must never see each other's methodology or drafts.
- Pieces, in order: (1) frontmatter/description, (2) Core Principles,
  (3) Patterns, (4) Anti-patterns, (5) Decision Guide, (6) each references/
  file. You may batch pieces 2–5 into one author round if the skill is small,
  but references/ files carrying canonical code get their own round.
- Drafts go to the arbiter **VERBATIM — never summarized**. The arbiter rules
  A/B/MERGE/NEITHER with file-verified reasons.
- Coordinator verification duties on every verdict: diff every rephrasing of a
  settled ruling; check arbiter self-declared additions against the corpus;
  verify SHARED claims of both authors (independent drafts agree on false rules
  at the doctrine's center — it has happened four sessions running); diff
  modality both directions (permission must not drift into obligation, "not
  chosen" must not drift into "banned").
- The loop's step-4 user approval is delegated: your report to the main session
  is the approval gate. Assemble files only after your own verification passes.
- If the arbiter reports `Unknown skill` for `skill-creator:skill-creator`:
  STOP, write the failure into your report, and return — do not self-heal by
  reading any plugin cache (cache reads are banned, CHANGELOG 0.3.31).
- If a writer agent cannot load a skill via the Skill tool, retry ONCE by
  passing the methodology file path directly (known harness gap, CHANGELOG
  0.3.31); record it.

## 2. Description law (docs/02-repo-structure.md §5 — binding)

- Third person, opens `This skill should be used when …`. Never `Use when …`.
- Under 100 words (em-dash-joined words count per `wc -w`). Dense, "pushy"
  concrete trigger nouns so the skill fires without being named.
- No `Covers …` sentence.
- Ends with `Not for:` naming EVERY owning sibling for excluded areas, form
  `<2–3 nouns> — <sibling-skill>`, semicolon-separated. Roster for this batch =
  the 21 shipped skills PLUS the 5 new siblings shipping together:
  `list-query-pipeline`, `file-storage`, `excel-miniexcel`,
  `http-client-factory`, `common-extensions`. Never drop an entry for length —
  cut nouns instead.

## 3. Body format and budget

- **No H1** in skill bodies. Start at `## `.
- SKILL.md target 117–450 lines, hard bar < 500. Do not pad; do not cut content
  merely to chase a sibling's number.
- `references/` files carry the FULL canonical implementation code so that a
  small, weak model can recreate each extension verbatim in a project that
  lacks it. SKILL.md teaches when/why + the decision rules; references/ carry
  the complete `.cs` bodies. This batch's explicit design goal: **a small model
  must be able to do this right.**
- Every skill body must teach the recreate-doctrine: these files are
  accumulated project wisdom, often missing in a new project; when the current
  project lacks the extension, recreate it from this skill's references/ —
  never inline a bespoke copy at call sites, never cite any real project path.

## 4. Sanitization (absolute)

No real project names (apsp, mtc, booking, cpc, digitalcity, ops-service...),
no business-domain nouns (oil change, vouchers, newsletters, facilities,
bookings, payments-provider names...), no real paths, no secrets/keys/bucket
names. Neutral placeholders: `Entity`, `EntityBaseResponse`,
`CreateEntityRequest`, `SearchEntityRequest`, `Wrapper`. Namespace in examples:
`Infrastructure.Facades.Common.…` is fine (structural, not project-identifying).

## 5. Provenance law

Every behavioural claim must be grounded in a corpus file you (or the arbiter)
verified. Anything derived from library documentation instead of the corpus
ships ONLY inside a visibly marked block:
`> **Documentation-derived** — not corpus-verified.` API-recall claims
(ordering, precedence, internal behaviour) that cannot be corpus-checked are
REFUSED, not hedged.

## 6. Variant comparison duty

Your brief lists the same file in several projects. Compare ALL variants
line-by-line where they differ; choose the best or synthesize a corrected
best-version (fix real defects: missing null-guards, in-memory paging,
swallowed exceptions, encoding bugs). Record in your report: which variant won
each method, what you improved, and why. The user pre-authorized this.

## 7. Stack facts (settled — do not relitigate)

.NET 8 · ASP.NET Core Controllers (not Minimal API) · Swashbuckle · NO API
versioning · FluentValidation · AutoMapper v12 · MediatR v12 = in-process
messaging · Redis · Elasticsearch · Hangfire · PostgreSQL + citext · MiniExcel
for Excel · AWS S3 for file storage. Read shipped siblings under
`D:\AI-PLUGIN\dotnet-standards\skills\` as settled baseline — your skill must
not contradict them; where an area is already owned (e.g. entity configuration
in `ef-core-data-access`, `PaginationResponse` contract in `api-surface`),
POINT to the owner, don't re-teach.

## 8. Report file (your deliverable to the main session)

Write it to the path given in your brief, containing:
1. Status: COMPLETE / BLOCKED (+why).
2. File list written under `skills/<name>/` with line counts.
3. Final description text (for router work) + proposed router row(s) for
   `choosing-a-dotnet-skill` (do NOT edit the router yourself) + proposed
   `Not for:` additions to EXISTING sibling skills (list skill + exact
   sentence; do not edit them).
4. Proposed CHANGELOG entry (main session renumbers).
5. Verdict log per piece (A/B/MERGE/NEITHER + one-line reason), coordinator
   catches, every delegated judgment call.
6. Variant-comparison table (which project's file won what, improvements made).
7. Open questions / parked items.
