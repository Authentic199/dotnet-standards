# dotnet-standards

A personal Claude Code plugin holding one thing: **how I write .NET.**

It runs alongside [Superpowers](https://github.com/obra/superpowers) as an
independent plugin. No Superpowers file is ever modified.

## The three tiers

| Tier | Owner | Responsibility |
|---|---|---|
| 1. Process | **Superpowers** | brainstorm → plan → TDD → review |
| 2. Knowledge | **`dotnet-standards`** (this repo) | What the code looks like: architecture, in-process messaging pipeline, EF Core, caching, search, API surface, testing |
| 3. Context | Per-project `CLAUDE.md` | Which conventions apply to *this* codebase — generated and maintained by `claude-md-builder` |

Since the process-integration layer (Lane D, spec
`docs/superpowers/specs/2026-07-27-process-integration-design.md`), this plugin
also ships closed-loop workflows (`/dotnet-feature`, `/dotnet-review`) that sit
ON TOP of tier 1: they call Superpowers skills and this repo's knowledge skills;
they copy neither. The old "does not own workflow" promise was deliberately
revised by that spec. Project-specific context still belongs to tier 3.

## Status

**v0.3.26 — knowledge layer + process-integration layer shipped, plus the
tier-3 generator.**

| Component | State |
|---|---|
| `hooks/` | ✅ six hooks, `post-edit-format` + `superpowers-check` + `router-nudge` + `test-report-nudge` + `fleet-nudge` + `process-handback` — see [`hooks/README.md`](hooks/README.md) |
| `skills/` | ✅ knowledge skills, four review rubrics, the router, two flow skills, and `claude-md-builder` — the tier-3 `CLAUDE.md` generator |
| `agents/` | ✅ six specialist agents — four read-only reviewers, two testers |
| `commands/` | ✅ `/dotnet-feature`, `/dotnet-review` — thin entries into the flow skills; the `dotnet-` prefix avoids built-in collisions (namespacing verified against current docs, Lane D) |
| Codex | ✅ `.codex-plugin/plugin.json` + `.agents/plugins/marketplace.json` for the skills, `codex/` for the hooks, agents and prompts Codex reads from outside a plugin — see [Install → Codex](#codex) |

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

### Codex

The same repository is also a Codex plugin — `.codex-plugin/plugin.json` plus a
marketplace descriptor at `.agents/plugins/marketplace.json`. Nothing is
duplicated: Codex loads the *same* `skills/` directory.

```
codex plugin marketplace add Authentic199/dotnet-standards
codex plugin add dotnet-standards@dotnet-standards-dev
```

Then **start a new thread** — an installed plugin is picked up at thread start,
not mid-conversation. Verify with `codex plugin list`.

Working on this checkout, point the marketplace at the directory instead
(`codex plugin marketplace add /absolute/path/to/dotnet-standards`), and after
each edit re-run `codex plugin add …`; Codex caches by version, so bump the
version (or append a `+codex.<token>` cachebuster) when the version has not moved.

> ⚠️ **The Codex cache is built from git HEAD, not from the working tree.** An
> uncommitted edit installs as the previous commit's content and says nothing
> about it — commit first, then install.

**A Codex plugin carries `skills/` and nothing else** — `hooks`, `commands` and
`agents` are rejected manifest fields. Every one of them still has a Codex
equivalent; Codex just reads it from outside the plugin. One script installs
them:

```
bash codex/install.sh
```

| Component | On Codex | Installed to |
|---|---|---|
| `skills/` | ✅ from the plugin, unchanged | — |
| `hooks/` | ✅ same six, same scripts | `~/.codex/hooks.json` (`plugin_hooks` is a removed feature) |
| `agents/` | ✅ projected to Codex's TOML form | `~/.codex/agents/*.toml` |
| `commands/` | ✅ as custom prompts, same `/dotnet-feature`, `/dotnet-review` | `~/.codex/prompts/*.md` |

`agents/` and `commands/` stay the single source of truth;
`codex/sync-from-plugin.py --check` fails when the projections have drifted.
What was measured and what was not is written down in
[`codex/README.md`](codex/README.md) — read it before trusting the agent fleet
there.

Validate the Codex manifest with Codex's own validator, which also checks every
skill's frontmatter:

```
python3 ~/.codex/skills/.system/plugin-creator/scripts/validate_plugin.py .
```

### The instructions file, on both harnesses

Claude Code reads `CLAUDE.md`; Codex reads `AGENTS.md`. **The rules live in
`CLAUDE.md` on both** — `AGENTS.md` is a pointer at it with no rules of its own,
written by `claude-md-builder` (PHASE 7) alongside the file it points at. A
project with no `CLAUDE.md` gets one built first, then the pointer. This repo's
own [`AGENTS.md`](AGENTS.md) is that pointer.

> ⚠️ **The copy ignores `.gitignore`.** `reference/` — the kit clone and the real
> project checkouts — is copied along with everything else, turning a ~330 KB
> plugin into a 39 MB one. Delete `reference/` from the cache copy after each
> install. A proper fix is recorded in
> [`docs/02-repo-structure.md`](docs/02-repo-structure.md) §4 and is not yet chosen.

### Requirements

- **Git for Windows** — every hook runs through a polyglot CMD/POSIX wrapper
  that needs a bash. Without it the hooks silently never run; see
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
