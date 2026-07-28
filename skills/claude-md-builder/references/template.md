# CLAUDE.md section skeleton

Read at PHASE 4. This is a skeleton, not a draft: it fixes the section order,
says which sections are required, and gives each one a line budget. It contains
no prose to copy — every line of the output is written from scan findings or
selected static rules.

**Total budget: 165 lines, hard ceiling 200.** The gap is deliberate headroom for
the probes in PHASE 6. Section 9b does not raise the total: it only exists on the
greenfield branch, where sections 3 and 4 are deferred and give back far more.

Omit any conditional section with nothing project-specific to say. An empty
heading is bloat with a title.

---

## Order and budgets

| # | Section | Required? | Budget | Ships when |
|---|---|---|---|---|
| 1 | Title + one-line purpose | required | 2 | always |
| 2 | Project overview | required | 8 | always |
| 3 | Commands | required | 20 | always — **deferred on the greenfield branch** |
| 4 | Project structure | conditional | 25 | layout is not obvious from the solution file; deferred on the greenfield branch |
| 5 | Architecture and layering | conditional | 15 | the `ProjectReference` graph shows a real direction to protect |
| 6 | Conventions — pointers only | conditional | 12 | scan row 12 found convention documents |
| 7 | Configuration and secrets | conditional | 10 | scan rows 5–6 found a non-obvious config story |
| 7b | Communication | required | 4 | always |
| 8 | Rules | required | 55 | always |
| 9 | Gotchas | conditional | 15 | question 3 answered, or the scan found a trap |
| 9b | Planned, not yet built | conditional | 10 | greenfield branch only |
| 10 | When unsure | required | 5 | always |

---

## What each section holds

**1 — Title + purpose.** The file's own name and one line saying what the
repository is for. Nothing else. No meta-commentary about `CLAUDE.md` itself.

**2 — Project overview.** Stack facts Claude cannot guess and will get wrong:
target framework, database, cache, search, background jobs, listening port,
Swagger and health paths. Dense lines, not paragraphs.
*Never:* a marketing description, a feature list, project history.

**3 — Commands.** Copy-pasteable, every switch spelled out, taken from the CI
config or the solution layout. Cover: build, run, test, and EF migration when the
migration block applies. Group them in one fenced block per concern.
*Never:* a command that was not verified against a real source.

**4 — Project structure.** A tree **no deeper than two levels**, annotated only
where the name does not explain the role. The purpose is to say where new code
goes, not to inventory what exists.
*Never:* a full recursive listing — Claude reads that from disk faster than from
here, and it goes stale the same day.

**5 — Architecture and layering.** The dependency direction as the
`ProjectReference` graph actually shows it, plus any rule that protects it. State
the *why* in one clause where the direction is not self-evident.
*Never:* an essay on clean architecture.

**6 — Conventions — pointers only.** A list of the convention documents found,
each with one clause saying when to read it. Static rule R20 lives here.
*Never:* the content of those documents, summarised or otherwise. Copying it here
is what pushes a file past 200 lines.

**7 — Configuration and secrets.** How configuration resolves, which override
form wins, and the R12 arm the user selected. Key names only.
*Never:* a configuration value, of any kind, secret or not.

**7b — Communication.** Static rules R22 and R23: which language the user is
addressed in, and the brainstorm/plan split. Four lines, near the top of the
rules rather than buried — it governs every reply, so a reader who stops early
must still have seen it.
*Never:* tone or verbosity preferences that no one will check.

**8 — Rules.** The selected static rules plus any project-specific rule the scan
or the questions produced. One imperative line each, falsifiable, grouped under
short sub-headings. Put hard constraints — the ones whose violation costs data or
a broken environment — first and mark them plainly.
*Never:* a rule an analyzer already enforces. Never a rule that cannot be checked.

**9 — Gotchas.** Deliberate oddities from question 3, and traps the scan found:
the thing that looks wrong and must not be "fixed", the step that fails in a
non-obvious way.
*Never:* a bug report. This section records intent, not defects.

**9b — Planned, not yet built.** Greenfield only, **10 lines maximum**. What the
documents say the project will be: intended module boundaries, the domain
glossary, business constraints already agreed. Every line carries its source
comment.
*Never:* a command, a path, a package, or anything phrased as though it exists.
**Expiry rule — this section is the one part of the file with a deadline.** At
the next update it must be emptied: each line is deleted because the code now
says it better, promoted into a rule because it was decided, or deliberately
re-marked as still unbuilt. A `Planned` section that survives two updates
unchanged has become the *historical archive* anti-pattern and is cut on sight.

**10 — When unsure.** Static rule R18, plus who or what to consult. Three to five
lines.

---

## Formatting rules for the output

- **The generated file is written in Vietnamese.** Headings, prose and rule lines
  in Vietnamese; commands, paths, identifiers, package names and code always in
  English. The rules in `static-rules.md` are held in canonical English — restate
  them in Vietnamese when writing them into the output, and change nothing about
  what they require. Two of the three reference `CLAUDE.md` files in this
  plugin's corpus are already written this way.
- Markdown headings and bullets, no dense paragraphs — structure is what makes
  the file scannable.
- One rule per line, imperative mood, present tense.
- Fenced code blocks for every command, so they can be copied without editing.
- Reserve emphasis (`IMPORTANT`, `NEVER`) for the hard constraints in section 8.
  Emphasis everywhere is emphasis nowhere.
- Notes meant for human maintainers go in block-level HTML comments — those are
  stripped before the file reaches context, so they cost nothing.
