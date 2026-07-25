# TRIAGE — dotnet-claude-kit → dotnet-standards

> Reference repo: codewithmukesh/dotnet-claude-kit (MIT)
> Pinned commit SHA: `<fill in Session 1>`
> Status values: `pending` | `keep` | `keep-tweak` | `adapt` | `rebuild` | `skip` | `combine`
> Rule: every non-pending row MUST have a Reason. Group B rows with `keep`/`combine` MUST have a Conflict-check note.

## Progress
- Group A (knowledge skills): 0/？ decided
- Group B (agents / workflow commands / meta-skills / hooks): 0/？ decided
- Group C (MCP): 0/1 decided
- Group D (rules / templates): 0/？ decided

## Group A — Knowledge skills
> For `adapt`/`rebuild` rows: fill Canonical source (which project/feature the exemplar comes from) and tick Sanitized after distillation review.

| Path | Summary (1 line) | Status | Canonical source (project → feature/paths) | Sanitized? | Reason / Notes | Upgrade candidate? |
|---|---|---|---|---|---|---|
| skills/ef-core/... | | pending | | | | |

## Group B — Process layer (compare against Superpowers per component)
| Path | Summary | Superpowers equivalent? | Conflict check (hooks/commands/instructions) | Status | Reason |
|---|---|---|---|---|---|
| agents/... | | | | pending | |

## Group C — MCP
| Component | Status | Notes |
|---|---|---|
| CWM.RoslynNavigator | pending | default: keep as external dotnet tool, not copied into plugin |

## Group D — Rules & templates
| Path | Summary | Status | Destination (skill content / project CLAUDE.md template / drop) | Reason |
|---|---|---|---|---|

## Decision log (append-only)
| Date | Session | Component | Decision | Why |
|---|---|---|---|---|
