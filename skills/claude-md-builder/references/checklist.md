# Trim and verify checklist

Read at PHASE 5. Walk the draft against every item below, **in order**. The order
is also the cutting order: when the file is over budget, item 1 goes before item
2, and so on. Nothing below item 11 may be cut to buy space.

Applied per line, the whole checklist reduces to one question:
**would removing this line cause Claude to make a mistake?** If not, cut it.

## Cut in this order

**1 — Derivable content.** Anything Claude reads off the codebase in seconds:
directory listings deeper than two levels, dependency inventories, file-by-file
descriptions, architecture overviews that restate the project graph.

**2 — Doctrine a `dotnet-standards` skill already owns.** Project layout,
dependency direction, the composition root, DI lifetime markers, module folder
shape, repository and query conventions, route and DTO shape, message keys.
Replace each with a pointer to the owning skill. Check this against principle 8's
table, and check it twice for anything the user rejected as a static rule — a
rejected rule returning as a "scan finding" is the same rule wearing a different
hat. Also cut any directory tree of a repository still taking shape: it reads as
the intended final layout and stops Claude creating what is missing.
**Cut only what the repository does the same way as the skill.** A line recording
that the repository does the *opposite* is not doctrine — it is the finding, and
it is never cut here. If unsure which one a line is, it goes on the PHASE 1c
report, not into the bin.

**3 — Analyzer duplicates.** Any rule already enforced by StyleCop,
SonarAnalyzer, Roslynator, `.editorconfig` or the ruleset: formatting, naming
casing, using ordering, XML-doc presence, nullable warnings. Cross-check against
the exclusion list built in scan row 3.

**4 — Generic model knowledge.** Anything equally true of a Node or Python
repository: "write clean code", "handle errors properly", "follow SOLID", "add
comments where helpful", explanations of what `async` means.

**5 — Non-falsifiable rules.** Any line that cannot be checked. "Keep the code
maintainable" cannot; "every I/O method declares a `CancellationToken`" can. If
you cannot describe the violation, it is not a rule.

**6 — Philosophy and mission.** Values, principles, team culture, the reason the
project exists beyond one line in the overview.

**7 — Historical archive.** Approaches that were abandoned, migrations already
finished, "we used to do X". An agent reads these as live options.

**7b — Stale provisional content.** On an update, any `Planned, not yet built`
line and any line marked `unverified` that the code has since made real, made
wrong, or left untouched for a second update. Delete it, promote it to a rule, or
re-mark it deliberately — never leave it drifting.

**8 — Meta-sections.** Rules about `CLAUDE.md` itself, notes on how the file was
generated, instructions for maintaining it. Those belong in a comment or outside
the file.

**9 — Task-specific instructions.** Anything scoped to one feature or one ticket.
It belongs in the prompt or a plan document, not in every future session.

**10 — Long explanations.** Tutorials, API documentation, multi-paragraph
rationale. Replace with a pointer to the document that owns it.

**11 — Duplicates and contradictions.** The same rule stated twice under two
headings; two rules that cannot both be satisfied. Contradiction is worse than
either rule alone — Claude picks one arbitrarily.

## Never cut

- A command, or any switch of one.
- A rule the user answered a question to produce.
- The `When unsure` section.
- Anything the PHASE 6 probes depend on.
- **The `### Process` group under section 8 (R28–R31).** It reads like
  meta-commentary about tooling rather than a rule about this repository, so a
  trimming pass reaches for it early. Its absence is the one that cannot be
  noticed from inside the file: a session that never reads it runs the whole
  task through another plugin's process, and nothing in the output looks wrong
  until a review that never applied a rubric comes back clean.
- **The R27 preamble under section 6b, and the *load it first* clause on any
  capability-absent bullet.** Both read like rationale, so items 6 and 10 will
  reach for them first; neither may be cut. Without them the section states that
  a capability is missing and nothing more, which is read as the owning skill
  being switched off — the exact misreading R27 exists to prevent, and it does
  its damage at the moment the capability is being introduced.

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
- [ ] Every line describing something not yet built carries its source mark.
- [ ] Section 6b opens with R27, and every bullet recording an absent capability
      says to load that skill when the capability is introduced.
- [ ] Every contradiction found was reported and classified — none was cut
      silently, and none classified as a defect reached the file.
