# Trim and verify checklist

Read at PHASE 5. Walk the draft against every item below, **in order**. The order
is also the cutting order: when the file is over budget, item 1 goes before item
2, and so on. Nothing below item 10 may be cut to buy space.

Applied per line, the whole checklist reduces to one question:
**would removing this line cause Claude to make a mistake?** If not, cut it.

## Cut in this order

**1 — Derivable content.** Anything Claude reads off the codebase in seconds:
directory listings deeper than two levels, dependency inventories, file-by-file
descriptions, architecture overviews that restate the project graph.

**2 — Analyzer duplicates.** Any rule already enforced by StyleCop,
SonarAnalyzer, Roslynator, `.editorconfig` or the ruleset: formatting, naming
casing, using ordering, XML-doc presence, nullable warnings. Cross-check against
the exclusion list built in scan row 3.

**3 — Generic model knowledge.** Anything equally true of a Node or Python
repository: "write clean code", "handle errors properly", "follow SOLID", "add
comments where helpful", explanations of what `async` means.

**4 — Non-falsifiable rules.** Any line that cannot be checked. "Keep the code
maintainable" cannot; "every I/O method declares a `CancellationToken`" can. If
you cannot describe the violation, it is not a rule.

**5 — Philosophy and mission.** Values, principles, team culture, the reason the
project exists beyond one line in the overview.

**6 — Historical archive.** Approaches that were abandoned, migrations already
finished, "we used to do X". An agent reads these as live options.

**6b — Stale provisional content.** On an update, any `Planned, not yet built`
line and any line marked `unverified` that the code has since made real, made
wrong, or left untouched for a second update. Delete it, promote it to a rule, or
re-mark it deliberately — never leave it drifting.

**7 — Meta-sections.** Rules about `CLAUDE.md` itself, notes on how the file was
generated, instructions for maintaining it. Those belong in a comment or outside
the file.

**8 — Task-specific instructions.** Anything scoped to one feature or one ticket.
It belongs in the prompt or a plan document, not in every future session.

**9 — Long explanations.** Tutorials, API documentation, multi-paragraph
rationale. Replace with a pointer to the document that owns it.

**10 — Duplicates and contradictions.** The same rule stated twice under two
headings; two rules that cannot both be satisfied. Contradiction is worse than
either rule alone — Claude picks one arbitrarily.

## Never cut

- A command, or any switch of one.
- A rule the user answered a question to produce.
- The `When unsure` section.
- Anything the PHASE 6 probes depend on.

## Final gate

Answer yes to all of these, or keep working:

- [ ] Line count is under 200.
- [ ] Every command is copy-pasteable and came from a verified source.
- [ ] Every line is project-specific or an approved static rule.
- [ ] No configuration value appears anywhere in the file — key names only.
- [ ] No secret appears anywhere in the file.
- [ ] No two lines contradict each other.
- [ ] Each of the three PHASE 6 probes is answerable from the file alone.
- [ ] No section heading stands empty.
