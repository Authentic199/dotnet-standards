# Lane B — API & Security Surface · CLOSED at S15 (dotnet-testing v0.3.11 shipped)

> **STATUS — 2026-07-27, S15 close.** Lane B's queue is COMPLETE. Shipped:
> `api-surface` v0.3.2 (S12), `error-handling` v0.3.4 (S13), `message-keys`
> v0.3.7 (S13b), `dotnet-testing` v0.3.11 (S15, the promoted B4).
>
> **`auth-and-security` is NOT Lane B's anymore** — reassigned to Lane A by
> explicit user direction (commit `f2d60c0`, Lane A S9 close). Any session
> opening from an older Lane B file that names auth-and-security as B4 is
> reading a STALE opener: stop and surface the collision. `observability`
> remains PENDING per the S14 reprioritization.
>
> **What runs next is not a Lane B session:** the four review rubrics
> (`dotnet-code-review`, `dotnet-architecture-review`, `dotnet-security-review`,
> `dotnet-performance-review`) run as SOLO sequential sessions from the
> rubric-phase prompt (commit `268aeec`, one file, harvest-before-mine). Their
> input is banked in the lane logs — harvest `CLAUDE.md` `## Lane log` and the
> CHANGELOG before re-mining source.
>
> If the user reopens Lane B for a new deliverable, copy the process/discipline
> sections of `CLAUDE.md` (S15 close version, commit history) as the opener:
> the three-way loop is codified in `.claude/skills/three-way-skill-loop/` with
> agents `skill-writer-a` / `skill-writer-sp` / `skill-arbiter`, the main
> session coordinates only, and the standing delegation + R7/R8 + sanitization
> rules bind unchanged.

## Rubric harvesting index (Lane B's contributions)

- **S15 (dotnet-testing v0.3.11):** message-keys "Which form where" table gap
  (selector-bearing entity-typed service throw — corpus shape, no table row);
  `module-feature/references/validation-rules.md:322` stale entity-typed rule
  (led both S15 authors astray — Lane A's file to fix); kit anti-example bank
  (fixture `RemoveAll`+`AddDbContext`; kit's own `CreateInMemoryDb()`;
  Moq-syntax illustration; Verify section; WireMock row). Verified mechanism
  facts usable by the security rubric: `VerifyJwtUserMiddleware` reads the
  established principal then re-checks the user row (NotFound/Blocked/
  ApplicationId).
- **S13b (message-keys v0.3.7):** hardcoded const key
  (`Messages.Middleware.IPAddressForbidden`) — a third key mechanism no ruling
  covers; `Action(MessagesType.X)` bypass (compiles, zero call sites);
  validator dual-form census (apsp 194 constants vs 135 lambda, digitalcity
  142 vs 245). Anti-example (user-labelled, real path for reviewer use):
  `apsp .../Modules/Customers/Request/*.cs` — four request classes lacking
  `[MessageDisplay]`. NEVER to appear in artifacts (user ruling): the wrong-`T`
  copy-paste (`AnalyzeRetrievalImageRequest.cs:79`) and the pseudo-segment
  string keys.
- **S13 (error-handling v0.3.4):** four unruled candidates in CHANGELOG 0.3.4;
  R7 split precedent (ops-service SHAPE / apsp THROW PATTERNS); UnAuthorized
  census (3 middleware + 2 current-principal).
- **S12 (api-surface v0.3.2):** anti-example list @ superseded lane file
  `6848e17` + CHANGELOG 0.3.2; single `[HasPermission]` ctor + positional trap.
- **Roadmap row added at S15 close (user direction):** `dotnet-test-report`
  hook — Group B, post-rubrics; PostToolUse on `dotnet test`, TRX/console
  parse, auto-report of cases run/passed; kit precedent
  `hooks/post-test-analyze.sh`; needs the Windows polyglot wrapper (§6) and the
  Group B conflict check.

## Install state at S15 close

USER-scope `dotnet-standards 0.3.11` from marketplace `dotnet-standards-dev`
(directory source = this checkout). `claude plugin details` reports
**Skills (11)**. Registry healthy this session — no S13b-style vanishing. The
S13b recovery recipe stays valid if it recurs: `claude plugin marketplace add
./` (bare `.` rejected), then install; check `installed_plugins.json` before
deleting any cached version dir.
