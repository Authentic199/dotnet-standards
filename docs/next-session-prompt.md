# Next sessions — the three parallel lanes (index)

**S7b is complete.** `facade-module-architecture` v0.3.0 shipped: rebuilt under the
three-way process, 6 references, settled description voice, verified `Skills (1)`.

From here the skill roadmap splits into **three lanes that run as three parallel
sessions**. The user adjudicated the split: lanes may run concurrently because their
skills are content-disjoint; anything that is NOT parallel-safe is excluded from every
lane (see below). Open exactly one of these prompts per session:

| Lane | File | Sessions in order |
|---|---|---|
| **A — Data & Feature Spine** | `next-session-prompt-A.md` | `cqrs-feature-slice` (S8) → `ef-core-data-access` (S9) → `domain-modeling` → `modern-csharp` |
| **B — API & Security Surface** | `next-session-prompt-B.md` | `api-surface` (S12) → `error-handling` (S13) → `auth-and-security` → `observability` |
| **C — Infrastructure Services** | `next-session-prompt-C.md` | `distributed-caching` (S10) → `elasticsearch-search` (S11) → `background-worker` → `http-resilience` |

**Excluded from every lane — run solo AFTER the lanes converge, never in parallel:**
`dotnet-testing` (S14, research variant), `choosing-a-dotnet-skill` (S15 router — needs
every description final), `project-scaffolding`, the four review rubrics, and the
process layer (P4). A lane that finishes its queue STOPS and says so; it does not pull
from this list.

## The parallel protocol (binds all three lanes)

1. **Ownership.** A lane session may write ONLY: `skills/<its-current-skill>/` and its
   own `docs/next-session-prompt-<A|B|C>.md`. It never touches another lane's skill
   folders or prompt file, the router, or TRIAGE rows.
2. **Isolation.** Each lane session works on its own git branch
   (`lane-<x>/<skill-name>`), created from the latest `main` — in its own worktree or
   checkout if sessions run at the same moment in time.
3. **Deferred notes.** Mid-session rulings, deferred requests and canonical-source
   records go into the lane's own prompt file under a `## Lane log` heading — NOT into
   `03-session-roadmap.md` or `TRIAGE.md` mid-flight. A future solo session
   consolidates the lane logs into the roadmap.
4. **Merge & version.** At session end: rebase onto latest `main`, bump the PATCH
   version by +1 relative to whatever `main` then carries (both manifests must agree —
   `claude plugin validate` checks), append the CHANGELOG entry at the top, merge.
   On conflict: keep both CHANGELOG entries, renumber your own version above theirs.
5. **Prove it, one at a time.** After merging: `claude plugin uninstall … --scope local`
   + install + `claude plugin details dotnet-standards` must report `Skills (n+1)`.
   If another lane is mid-install, wait — never two installs concurrently.
6. **Self-perpetuation.** Each session ends by rewriting ONLY its own lane prompt file
   so it opens the lane's next skill, carrying the lane log forward. The
   one-session-one-deliverable rule holds inside every lane.
7. **No duplication.** Each lane prompt lists what the other two lanes own; a lane must
   refuse (and log) any request that belongs to another lane or to the excluded list.
