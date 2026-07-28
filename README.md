# dotnet-standards

A personal Claude Code plugin holding one thing: **how I write .NET.**

It runs alongside [Superpowers](https://github.com/obra/superpowers) as an
independent plugin. No Superpowers file is ever modified.

## The three tiers

| Tier | Owner | Responsibility |
|---|---|---|
| 1. Process | **Superpowers** | brainstorm → plan → TDD → review |
| 2. Knowledge | **`dotnet-standards`** (this repo) | What the code looks like: architecture, in-process messaging pipeline, EF Core, caching, search, API surface, testing |
| 3. Context | Per-project `CLAUDE.md` | Which conventions apply to *this* codebase |

Since the process-integration layer (Lane D, spec
`docs/superpowers/specs/2026-07-27-process-integration-design.md`), this plugin
also ships closed-loop workflows (`/dotnet-feature`, `/dotnet-review`) that sit
ON TOP of tier 1: they call Superpowers skills and this repo's knowledge skills;
they copy neither. The old "does not own workflow" promise was deliberately
revised by that spec. Project-specific context still belongs to tier 3.

## Status

**v0.3.21 — knowledge layer + process-integration layer shipped.**

| Component | State |
|---|---|
| `hooks/` | ✅ two hooks, `post-edit-format` + `superpowers-check` — see [`hooks/README.md`](hooks/README.md) |
| `skills/` | ✅ knowledge skills, four review rubrics, the router, and two flow skills |
| `agents/` | ✅ six specialist agents — four read-only reviewers, two testers |
| `commands/` | ✅ `/dotnet-feature`, `/dotnet-review` — thin entries into the flow skills; the `dotnet-` prefix avoids built-in collisions (namespacing verified against current docs, Lane D) |

Triage of the reference kit is complete: 94 components decided across four
groups. The decisions live in [`docs/TRIAGE.md`](docs/TRIAGE.md); the rules that
produced them live in [`docs/01-triage-rules.md`](docs/01-triage-rules.md).

## Install

The plugin is served from a marketplace declared in this same repository
(`.claude-plugin/marketplace.json`, name `dotnet-standards-dev`). Point the
marketplace at the GitHub remote — no local checkout required on the target
machine:

```
/plugin marketplace add Authentic199/dotnet-standards
/plugin install dotnet-standards@dotnet-standards-dev
```

Requires the [Superpowers](https://github.com/obra/superpowers) plugin —
install it first (or alongside):

```
/plugin marketplace add anthropics/claude-plugins-official
/plugin install superpowers@claude-plugins-official
```

Working on this repo itself and want the marketplace to track your local
checkout instead of GitHub, use a directory source:

```
/plugin marketplace add /absolute/path/to/dotnet-standards
/plugin install dotnet-standards@dotnet-standards-dev
```

**Then restart Claude Code.** Changes to a plugin — including a fresh install —
do not take effect in a running session. "It didn't work" is almost always a
missing restart; rule that out before concluding anything is broken.

**Install copies this directory — it does not link to it.** The plugin lands in
`~/.claude/plugins/cache/`. Editing a file here changes nothing in the installed
plugin until you run the full cycle:

```
/plugin uninstall dotnet-standards@dotnet-standards-dev
/plugin install dotnet-standards@dotnet-standards-dev
# restart
```

The same commands exist as CLI subcommands (`claude plugin install …`,
`claude plugin details dotnet-standards`, `claude plugin validate .`), which is
useful for scripting and for confirming what the harness actually parsed.

> ⚠️ **The copy ignores `.gitignore`.** `reference/` — the kit clone and the real
> project checkouts — is copied along with everything else, turning a ~330 KB
> plugin into a 39 MB one. Delete `reference/` from the cache copy after each
> install. A proper fix is recorded in
> [`docs/02-repo-structure.md`](docs/02-repo-structure.md) §4 and is not yet chosen.

### Requirements

- **Git for Windows** — the one hook runs through a polyglot CMD/POSIX wrapper
  that needs a bash. Without it the hook silently never runs; see
  [`hooks/README.md`](hooks/README.md) for why that is accepted.
- **.NET SDK** on `PATH`, for `dotnet format`.

## Provenance and dates

This plugin derives material from
[`codewithmukesh/dotnet-claude-kit`](https://github.com/codewithmukesh/dotnet-claude-kit)
(MIT) and copies one wrapper pattern from Superpowers (MIT). Both obligations are
discharged in [`NOTICE`](NOTICE).

> **Reference kit pinned at commit `cd83d315986c27621da178dad73bd95d503c1540`.**
> **Knowledge in this plugin is current as of 2026-07-26.**

Both lines matter, and the second is the one that rots. **The knowledge layer
carries dated content**: .NET and C# version guidance, breaking-change notes,
package-version advice and licence boundaries all have a shelf life. The nearest
known expiry is **.NET 11 GA on 2026-11-10**, which will stale the .NET 10
migration material and the "do not generate `net11.0` / C# 15" guardrail.

Re-pinning the reference kit to a newer commit is a deliberate act, recorded in
the TRIAGE decision log. There are two triggers for it: the kit changed a file a
decided row depends on, **or** the .NET release train moved past what was
recorded here.

## Licence

This repository carries no licence of its own — it is personal and unpublished,
so it is "all rights reserved" by default. `NOTICE` covers the third-party
material regardless. Choosing a licence is deferred until there is a reason to
publish.
