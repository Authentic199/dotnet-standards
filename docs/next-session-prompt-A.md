## CONTEXT

I am building `dotnet-standards`, a personal Claude Code plugin holding my .NET
knowledge, alongside Superpowers (process layer). **No Superpowers file may ever be
modified.** `reference/dotnet-claude-kit` is read-only (pinned SHA
`cd83d315986c27621da178dad73bd95d503c1540`); `reference/projects/` holds my real
projects (gitignored): `ops-service` (reusable base), `apsp-backend` (production,
canonical for this lane), `be-booking` (anti-example quarry only). Triage
(`docs/TRIAGE.md`) is closed input.

**This is Lane A of three parallel lanes.** You own ONLY
`skills/ef-core-data-access/` and this file. Lane B owns `api-surface` (shipped
v0.3.2), `error-handling` (S13, may be in flight), `auth-and-security`,
`observability`. Lane C owns `distributed-caching` (shipped v0.3.1),
`elasticsearch-search` (S11 — files may exist uncommitted in the tree; NEVER stage
them), `background-worker`, `http-resilience`. The router, testing, scaffolding
and review rubrics are excluded from all lanes. Refuse and log anything outside
your ownership. **Lanes share one working tree: before every commit run
`git status` and stage ONLY your own paths.**

## THE DELIVERABLE — `ef-core-data-access` (S9)

The data-access gateway. It owns: **repository-over-EF-Core** with the real
`RepositoryBase` / `IRepositoryWrapper` evidence (the kit's "never wrap DbContext
in a repository" stance was explicitly overruled by the codebase — document the
wrapper as the law); `DbContext`; entities (BaseEntity/BaseEntity<TId>, ICode);
entity configurations including `HasCode<T>` / `ICode`; migrations (the
`Migrators.<Provider>` contract is placement — the *workflow* is yours); query
conventions (`Find(isAsNoTracking:)`, `ProjectTo`, includes, pagination
internals, transactions — `module-feature` P4 explicitly handed the
where-a-transaction-begins-and-ends lane to you). Known traps to carry:
`GetByIdAsync(params object[])` has NO ct overload on the relational repository
(documented in `module-feature`'s `references/service-growth.md` — cite, don't
re-teach); `IsExistByUnique`'s `object uniqueValue` + property-type restriction
(documented in `references/validation-rules.md`).
**Not this skill:** service/feature internals (`module-feature`), placement
(`facade-module-architecture`), HTTP (Lane B), Lane C areas.

## THE THREE-WAY PROCESS — MANDATORY

Per piece, not per skill: (1) you explain first in Vietnamese, I comment; (2) you
(author A: loads `docs/02-repo-structure.md` §5 + `docs/00-brainstorm.md` §3 + the
kit's skill format — NOT superpowers:writing-skills) and `skill-writer-sp` (author
B) draft independently, neither writes files; (3) `skill-arbiter` verdicts
A/B/MERGE/NEITHER with file-verified reasons; (4) I approve; only then write.
Both agents exist in `.claude/agents/` and are dispatchable — verify with a ping
before relying on them. Agent prompts must carry: the exemplar list I name, all
relevant settled rulings, and equal-source-access discipline. Announce every agent
use; relay milestones; agents end with `## QUESTIONS`; continue them via
SendMessage. Run agents in the current working directory — no worktree for
subagents.

## READING DISCIPLINE

I name the exemplars at session start — ask me for the list before reading
anything in `reference/projects/`; never select them yourself. Widening = targeted
lookup, announced (what/why). No bulk scans. Bash find/ls/grep, never Glob, inside
`reference/projects/`. R7: one canonical source per area, I designate. R8:
anti-examples are code I point at; ask before labelling. Sanitize: no project
names, no business-domain names (S8 used an `Order` domain — continue it for
cross-skill sample continuity), no real paths, no secrets.

## SETTLED — DO NOT RELITIGATE

- Everything shipped in `facade-module-architecture` v0.3.0+, `api-surface`
  v0.3.2, and **`module-feature` v0.3.3** (read all installed bodies + references
  as baseline; your skill must not contradict them). Key S8 rulings that touch
  data access: ct mandatory on every service operation (`= default` last param);
  handlers/services return responses never entities; `IsExist…` predicate naming;
  `Find(…).FirstOrDefaultAsync(ct)` over `GetByIdAsync` when a token matters;
  Expressions/ owns business-computed values.
- Description law (§5): third person `This skill should be used when…`, <100
  words by wc -w (measure it — S8's author A self-counted 97 and was actually
  111), pushy nouns, `Not for:` naming every owning sibling.
- The `references/` mechanism: body ≤~300 lines decision layer; depth in
  references with conditional "Read X when" pointers; the split itself goes
  through the three-way loop.
- My stack: Controllers not Minimal API, Swashbuckle, no API versioning,
  FluentValidation + AutoMapper, Redis, Elasticsearch, Hangfire, PostgreSQL
  primary (MySQL migrator exists).

## HARD CONSTRAINTS

1. One session, one deliverable: `ef-core-data-access` only. Extra requests → log
   under `## Lane log` below and refuse.
2. Prove it: validate + reinstall + `claude plugin details` shows the new skill;
   report failures honestly. Merge/version protocol: patch bump (0.3.3 is taken;
   check `plugin.json` AND the marketplace entry — both must match), CHANGELOG at
   top, one install at a time (S8 hit a stray local-scope install; check scope).
3. Artifact language English; talk to me in Vietnamese.
4. End: commit per protocol (stage only your paths), then rewrite THIS file so it
   opens Lane A's next session (S10+ per roadmap — after S9 the lane's remaining
   queue is set at consolidation), carrying the Lane log forward.

## Lane log

- **S8 — cross-lane alignment + one OPEN conflict (for the consolidation
  session and S13b `message-keys`):** `api-surface` v0.3.2 shipped mid-S8 and
  owns the DTO chain law (base request `Profile` only when customized, ending
  `IncludeAllDerived()`; plain base = no profile; abstract base requests;
  response rungs always carry the profile). `module-feature`'s request/response
  piece was amended to match before shipping — shipped siblings win. **OPEN:**
  api-surface's base-request `[MessageDisplay(nameof(Entity))]` law sits
  uneasily beside S8's user ruling R-s8 (strongly-typed
  `Messages<T>.Required(x => x.Prop)` is THE validator-message standard).
  `module-feature` ships the R-s8 form and stays silent on `[MessageDisplay]`;
  the `message-keys` skill (S13b) must reconcile the two into one law.
  (api-surface's own `request-response-dtos.md` already acknowledges both forms
  exist and defers — the reconciliation has a landing place.)
- **S8 (2026-07-26) — user orders two new catalog skills, not built:**
  (1) a standalone **AutoMapper/mapping skill** (`automapper-mapping`,
  provisional name) — sources: be-booking's `ca-automapper` plugin skill as
  *untrusted* reference, verified against real apsp-backend code (CouponResponse
  + Expressions/, DeviceRequest base-class `IncludeAllDerived` inheritance,
  Devices complex responses); (2) a standalone **MediatR skill**
  (`mediatr-messaging`, provisional) — in-process messaging only, no behaviors,
  no CQRS (exemplar: apsp Vouchers). `module-feature`,
  `facade-module-architecture` and `api-surface` route to them by these names —
  renaming them later means a ripple. Catalog placement/session numbers to be
  consolidated into the roadmap at a solo session.
- **S8 — rename executed:** `cqrs-feature-slice` → `module-feature` (user
  ruling). Ripple applied to `facade-module-architecture` (2 sites) and
  `api-surface` (5 sites incl. its reference; the "validation rules" hand-back
  phrase preserved). Historical docs (TRIAGE, roadmap) intentionally untouched.
- **S8 — "Services/ is not a dumping ground" repetition ruling:** carried at
  every stage that creates or reviews files in `Services/` — architecture
  (shipped), `module-feature` (shipped, with both authorized inventories), and
  the future review rubrics (`dotnet-code-review`, `dotnet-architecture-review`)
  must include it as a checklist item so the user never re-flags it. Carry this
  note until the rubric sessions consume it.
- **S8 — session ruling ledger** lives in the S8 conversation's scratchpad
  (`piece0-settled.md`, R-s1…R-s23); the durable subset is restated in
  `module-feature`'s CHANGELOG entry (0.3.3). If S9 needs a ruling's exact
  wording, the CHANGELOG entry is the surviving record.
- **S8 — anti-example candidates surfaced but NOT taken** (available if ever
  wanted): second bypass query (search variant, repo+mapper in handler);
  `CancellationToken.None` at 3 dispatch sites in one foreign service;
  `EInvoices/Constants/EInvoiceValidation.cs` (validation file outside
  `Validations/`); triple-negative `!= true` guard condition; dead ternary in
  the conflict predicate; response member existing only for sorting; Vietnamese
  XML summaries + TODO in doc comments.
