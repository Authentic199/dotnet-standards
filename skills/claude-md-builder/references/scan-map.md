# Scan map

Read at PHASE 1. Work the table top to bottom. Every row states what to open,
what may be inferred from it, and what happens when it is absent.

Exclude `**/worktrees/**`, `bin/`, `obj/`, `node_modules/` and `.git/` from every
search. Use `find` / `grep`; a repository with worktrees checked out inside it
inflates every count several times over otherwise.

## The table

| # | Open | Infer | If absent |
|---|---|---|---|
| 1 | `*.sln` at the root | Solution name → the build command. Project list and each project's role. | Fall back to the set of `*.csproj`; build per project. |
| 2 | every `*.csproj` | `<TargetFramework>` → the .NET version. `ProjectReference` graph → the **real** dependency direction, which becomes the layering section. `PackageReference` → key packages. | Cannot proceed; there is no .NET project here. |
| 3 | `Directory.Build.props`, `dotnet.ruleset`, `.editorconfig`, `stylecop.json` | Which analyzers are active, and which rules are suppressed. This produces the **exclusion list**: topics that must not appear in `CLAUDE.md`. | No exclusion list; style rules are then admissible but still must be project-specific. |
| 4 | `global.json` | Pinned SDK version. | Take the SDK version from the CI image (row 10) instead. |
| 5 | startup project `Program.cs`, `Startup.cs`, `Configurations/` | Config load order, listening port, Swagger and health-check paths. | Omit the endpoints line rather than guessing a port. |
| 6 | `appsettings*.json` and any `Configurations/*.json` — **key structure only** | Which infrastructure is wired: Redis, Elasticsearch, Hangfire, object storage, mail. Whether a **tracked** config file carries values that look like real credentials → this is the trigger for question 2. | Infer infrastructure from `PackageReference` instead. |
| 7 | `**/Migrations/*.cs`, and the project owning that folder | The `-p` value for EF commands. **Zero migration files → drop the whole EF migration block**, rules included. | Drop the EF block. |
| 8 | the `DbContext` class | The `-c` value for EF commands. More than one → name them; the `-c` switch stops being optional. | Drop the `-c` switch from the commands. |
| 9 | `tests/**/*.csproj` | Test projects, their names and test framework → the test command. | No test command; the test policy question still gets asked. |
| 10 | `.gitlab-ci.yml`, `.github/workflows/*`, `azure-pipelines.yml` | **Verified** build, test, publish commands and the SDK image. This is the most trustworthy command source in the repository, because CI actually runs it. | Derive commands from the solution layout and mark them unverified to the user. |
| 11 | `docker-compose*.yml`, `env*.Example`, `.env.example` | Services needed to run locally; the environment-variable override form. | Omit the local-run section. |
| 12 | `README.md`, `CONVENTION.md`, `WORKFLOW.md`, `AGENTS.md`, `BUSINESS-RULES.md`, `docs/**` | **Pointer targets.** List them and say when to read each. Never copy their content in. | No pointers section. |
| 13 | `CLAUDE.md`, `.claude/CLAUDE.md`, `CLAUDE.local.md` | Presence decides create versus update mode. | Create mode. |

## The greenfield gate

After the table, ask whether rows 1, 2 and 10 produced enough to fill *Project
overview* and *Commands*, and whether the repository holds business code beyond a
template skeleton. If not, this is a greenfield repository: go to the skill's
PHASE 1b and ask **Q0** below before anything else.

A greenfield repository still runs rows 1–4 and 13 — a solution skeleton, a
pinned SDK or an existing `CLAUDE.md` are all real findings. What it cannot
produce is a command, a module path or a package list, and none of those may be
supplied from a document.

## Compare against the owning skill before writing anything down

Layout and convention findings — rows 2, 5, 6, 7, 8 and 9 — do not go straight
into the draft. Each is first compared against the `dotnet-standards` skill that
owns the area (see the skill's principle 8 table):

- **Matches the skill** → not a finding. It becomes a pointer to that skill, and
  the detail is dropped. Most findings end here.
- **Contradicts the skill** → **never resolved by the scan alone.** It goes on
  the PHASE 1c report and the user classifies it: deliberate → into the file
  under *Where this repository differs*; a defect → reported, and kept out of the
  file entirely.
- **The skill assumes something this repository does not have** → a real finding,
  and the most valuable one. Straight into *Where this repository differs*.
- **The skill has no answer** → a real finding, usually a domain rule. Keep it.

A contradiction is easy to mistake for a match, because both are *about* a topic
the skill covers. Read what the skill actually requires before deciding — a
controller declaring its own route prefix is not "the routing convention", it is
the opposite of it.

Two extra suppressions:

- **Never emit a directory tree that merely mirrors the canonical shape.** It is
  doctrine restated, and it goes stale the first time a folder is added.
- **A repository still taking shape gets no tree at all.** Signals: few modules,
  a layout that is obviously partial, or a `Planned, not yet built` section in
  play. A snapshot of an unfinished layout reads as the intended final shape, so
  Claude stops creating what is missing — the opposite of what the file is for.

## What is never inferred

Test policy, deliberate oddities, and whether committed credentials are
intentional. These have no signal in the file tree — a repository can hold two
fully-built test projects and still forbid writing tests. Guessing them produces
a confidently wrong rule, which is worse than an absent one.

## Questions

Ask Q1–Q3 in **one batch**. Say that skipping is fine. A skipped question
produces no section — never a guess. Do not add a fourth to that batch.

**Q0 — Documents.** *This repository has no business code yet. Are there
documents I should read — a spec, requirements, a design note, an ERD, an API
contract? Name them by path.*
Asked **only** on the greenfield branch, and asked **before** Q1–Q3, because the
answers change what the later questions are worth asking about. Only the named
paths are read. No answer → the file is built from the skeleton alone and stays
very short, which is the correct outcome, not a failure.

**Q1 — Test policy.** *Does this repository write automated tests? If yes: are
they required before a change is considered done, or optional? If no: is writing
tests forbidden here?*
Always asked. The presence of test projects proves nothing about the policy.

**Q2 — Committed credentials.** *Config file `<path>` is tracked and appears to
hold real credentials. Should writing real credentials into tracked config be
forbidden, or is it deliberate here?*
Asked **only** when row 6 tripped. The two answers select opposite rules —
see `static-rules.md` §Secrets, R12a and R12b.

**Q3 — Deliberate oddities.** *Is there anything in this repository that looks
like a bug or a bad practice but is intentional — something Claude should not
"fix"?*
Always asked. Each answer becomes one line under *Gotchas*.
