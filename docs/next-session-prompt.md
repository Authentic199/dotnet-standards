# Next sessions — the three parallel lanes (index)

**S7b is complete.** `facade-module-architecture` v0.3.0 shipped: rebuilt under the
three-way process, 6 references, settled description voice, verified `Skills (1)`.

From here the skill roadmap splits into **three lanes that run as three parallel
sessions**. The user adjudicated the split: lanes may run concurrently because their
skills are content-disjoint; anything that is NOT parallel-safe is excluded from every
lane (see below). Open exactly one of these prompts per session:

> **REPRIORITIZED 2026-07-27 (S14 close, explicit user direction — ship the lean
> plugin first).** `dotnet-testing` and `choosing-a-dotnet-skill` are PROMOTED out
> of the excluded list into lanes B and C respectively. Everything else still
> unshipped is **PENDING**: `auth-and-security`, `observability`,
> `background-worker`, `http-resilience`, `domain-modeling`, `modern-csharp`,
> `project-scaffolding`, and Lane A's queue beyond `ef-core-data-access` (in
> flight — it finishes; nothing new starts after it). Rules those pending skills
> would have owned get folded into the review phase later if still wanted. After
> the two promoted skills ship → **the four review rubrics run next**, per the
> user's order.

| Lane | File | Sessions in order |
|---|---|---|
| **A — Data & Feature Spine** | `next-session-prompt-A.md` | `cqrs-feature-slice` (S8) → `ef-core-data-access` (S9, in flight — finishes, then lane STOPS) → ~~`domain-modeling` → `modern-csharp`~~ (pending) |
| **B — API & Security Surface** | `next-session-prompt-B.md` | `api-surface` (S12) → `error-handling` (S13) → `message-keys` (S13b) → **`dotnet-testing` (B4, promoted)** → ~~`auth-and-security` → `observability`~~ (pending) |
| **C — Infrastructure Services** | `next-session-prompt-C.md` | `distributed-caching` (S10) → `elasticsearch-search` (S11) → `distributed-lock` (S14) → **`choosing-a-dotnet-skill` (S15, promoted router)** → ~~`background-worker` → `http-resilience`~~ (pending) |

**Excluded from every lane — run solo, never in parallel:** `project-scaffolding`
(pending) and the four review rubrics (NEXT after the two promoted skills — one per
session). A lane that finishes its queue STOPS and says so; it does not pull
from this list.

**Lane D — Process Integration (NEW, designed 2026-07-27 in S14):**
`next-session-prompt-D.md`. Closed-loop workflows (feature + review, bugfix
v1.5) + specialist agents + the Superpowers dependency check, per the approved
spec `docs/superpowers/specs/2026-07-27-process-integration-design.md`.
**Runs ONLY after the four rubrics AND `dotnet-testing` ship** — its agents bind
to them. Full order from here: promoted pair (`dotnet-testing` in B ∥ router in
C) → four rubrics (solo, sequential) → Lane D.

**Parallel-run caveat for the promoted pair:** the router ships a decision-table
row for `dotnet-testing`; if the two run concurrently, the router lane aligns that
row's wording against the actually-shipped `dotnet-testing` description at merge
time (the standard cross-skill alignment rule).

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
