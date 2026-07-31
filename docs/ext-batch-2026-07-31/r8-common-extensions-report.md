# R8 label-implementation report — `common-extensions` (Group 6)

**Status: COMPLETE.** All nine approved labels verified against the corpus and
shipped. Nothing dropped. Full three-way loop run (both authors + arbiter, all
synchronous). Writes confined to `skills/common-extensions/`.

## 1. Per-label verdict — nine VERIFIED, zero DROPPED

Sites described by shape, not address, per the sanitization law.

| # | Verdict | The corpus site I confirmed myself |
|---|---|---|
| C1 | **VERIFIED** | The 169-line `ExpressionExtension`: five `using` directives importing entities and requests of four business modules, plus two ORM namespaces; generic core ~27 lines; five module-typed members, two of them eager-load chains, one an in-memory `foreach` scan typed `this IEnumerable<T>`. Clean variant of the same file elsewhere: 114 lines, one `using`. |
| C2 | **VERIFIED** | A 22-line `ActionContextExtension` occupying the canonical name and slot: three members, two different accessor abstractions in one static class, against a 114-line fullest corpus variant with nine members. |
| C5 | **VERIFIED, stronger than the decision note** | `Serialize<T>` configures inline, `Deserialize<T>` passes nothing — and in three of the four affected projects the read path has **no `configs` parameter on the interface**, so no call site can correct it. Census: **4 of 6**, not "one of them". |
| C6 | **VERIFIED** | `Synchronize<T>` calls `SemaphoreSlim.WaitAsync();` with no `await`, three lines below two async siblings that await the identical call against the same `new SemaphoreSlim(1, 1)` gate, then `Release()` in `finally`. Two of six projects carry the file; `diff` confirms both carry the defect unchanged. |
| C8 | **VERIFIED** | `A-z` at six members across five projects. The sixth project corrected it in place to `A-Za-z` with a source comment — a real before/after. |
| C9 | **VERIFIED, framing narrowed (see §4)** | `IsValidAllPhoneNumber` → `^[+]?[0-9]{5,15}$` as an inline literal, beside a strict rule reading a named regex field. Displacement verified: strict rule **0 callers**, lax twin **5**, including the shared `Common`-slot request interface that the other five projects wire to the strict rule. |
| C11 | **VERIFIED** | `Guid.Parse` on a route value, read from **twelve request-validator files** in one solution. Consequence corpus-verified from the house exception middleware: `case CustomException` → its own status, `default:` → `InternalServerError` + `Log.Error` + a support error id. Malformed URL segment → 500 where 400 is correct. |
| C13 | **VERIFIED, stronger than the decision note** | **Five implementations in four distinct behaviours across eleven call sites** in one solution. Plus a detail no author found: **one service file calls two of those answers** — the throwing base extension at two call sites, its own private null-returning method at two others. |
| C16 | **VERIFIED** | `public static readonly Random Random = new(Environment.TickCount);` at line 9 in **6 of 6** projects; `Generate` draws every character from that field; **nine call sites across three projects**. |

## 2. Route taken and final line counts

**Route: `references/anti-patterns.md`.** SKILL.md was 480 lines against a hard
<500 bar — 19 lines of headroom for nine entries, so the set could not go
inline. Entries are numbered **5–13**, continuing SKILL.md's 1–4 so
"anti-pattern 7" is globally unambiguous. Entries 1–4 untouched, not renumbered.

| File | Before | After |
|---|---|---|
| `skills/common-extensions/SKILL.md` | 480 | **488** (pointer block +6, census clause +2 — see §6) |
| `skills/common-extensions/references/anti-patterns.md` | — | **399** (new) |

The pointer block is 6 lines at the end of `## Anti-patterns`, immediately
before `## Decision Guide`.

**On the 399 lines.** The arbiter estimated 283; materialized with fences and
blank lines it runs 399 — longer than the largest existing sibling (239).
`references/` files are unconstrained by the SKILL.md budget, and the standing
don't-cut-content-to-chase-a-number ruling applies, so I shipped it. If the main
session wants it shorter, the cheapest cuts are entry 5's defect table (the
prose already carries it) and entry 13's routing table.

**H1 — a coordinator ruling against my own brief.** The house "no H1" law
governs skill *bodies*; all nine existing files in this `references/` directory
open with `# <Name>`. Both authors and the arbiter shipped `##` because my brief
said so. I promoted it to `# Anti-patterns 5–13` with entries at `##`, for
directory consistency. Revert is one character plus a level shift if the main
session disagrees.

## 3. Verdict log

**Loop:** `three-way-skill-loop` invoked; Author A = `skill-writer-a` (house
methodology), Author B = `skill-writer-sp` (Superpowers `writing-skills`),
arbiter = `skill-arbiter`. Independence held — A confirmed it loaded neither
`superpowers:writing-skills` nor `skill-creator`. Arbiter confirmed
`skill-creator:skill-creator` loaded live, no `Unknown skill`. All three
synchronous. Drafts went to the arbiter **verbatim**.

**Verdict: MERGE**, with per-entry splits — 5 MERGE · 6 NEITHER (rewritten on a
new verified fact) · 7 MERGE · 8 B · 9 MERGE · 10 MERGE · 11 MERGE · 12 MERGE ·
13 A.

**Process incident:** the first arbiter spawn was killed mid-run by the monthly
spend limit. No work lost — both drafts were held verbatim in the coordinator
transcript and the arbiter was re-spawned with an identical prompt.

## 4. Coordinator catches

**Before the arbiter** (my greps, supplied to it as a fact sheet it re-verified):

1. **C13 census** — A wrote "three implementations across five call sites"; B
   wrote five implementations in four behaviours. **B was right**; A's
   breakdown was wrong. Arbiter confirmed and shipped B's.
2. **C11 count** — A said 13, B said 14. Both are grep artifacts (13 lines, 14
   invocations, one line calling twice). Shipped as the unambiguous **twelve
   request files**.
3. **B's "most call sites are written `RouteValue("id") ?? fallback`" — FALSE**
   (3 of 13). Cut. The arbiter caught that **A's GOOD-block comment**
   generalized the same error and cut that too.
4. **SHARED claim tested and held**: the phone rule's 0-callers / 5-callers
   displacement. Both drafts asserted it; it is true.
5. **C16 severity** — the account-password path hashes before storage *and*
   emails the plaintext, i.e. exactly the "temporary password delivered out of
   band" case the shipped Notes already sanction. **B's framing presented that
   compliant path as the defect**; it did not survive. A's move of the headline
   to the long-lived signing secret (no hashing fallback, 33 characters drawn
   from a 26-letter alphabet) shipped instead.

**The arbiter's own catches**, which I verified:

6. **A SHARED false number at the centre of the headline entry** — both drafts
   wrote the eager-load chains as "10 and 11 rungs". Actual: **9 and 10
   `Include` roots with 12 `ThenInclude` each**. Confirmed.
7. **A's sharpest sentence was false** — "the two chains differ by three rungs
   and every caller pays for whichever one it picked". The chains are typed to
   **different root entities**; no caller can pick between them. Cut.
8. **B's "the two symmetric projects route both directions through a single
   options object" — FALSE**; one of them declares the same literal twice,
   inline, once per direction. Cut.

**My checks on the arbiter's eight self-declared additions** — six confirmed as
written, **two corrected before shipping**:

| # | Arbiter addition | My verification |
|---|---|---|
| 1 | Include roots 9 and 10, 12 `ThenInclude` each | ✅ exact |
| 2 | The contamination was a *trade*: the 169-line file lost `Combine`, `Operation`, `ReplaceParameter`, `ApplyOperation` and `ParameterReplacer` | ✅ confirmed — the clean variant has all five |
| 3 | The same solution holds a **"seven-member"** 75-line proxy-aware variant of the same class name in a subsystem folder | ⚠️ **CORRECTED to five-member.** The file is 75 lines with **five** public static members (the arbiter's grep counted the class declaration line). The load-bearing point — same solution, same class name, fuller answer sitting outside the canonical slot — is verified and stands. |
| 4 | One service file calls two different IP answers | ✅ confirmed at four call sites in one file |
| 5 | "Twelve request files" for C11 | ✅ confirmed across 12 files |
| 6 | 4 of 6 asymmetric; in 3 of those 4 the asymmetry is in the interface | ✅ confirmed (the fourth omits the parameter on both members) |
| 7 | Entry 13's closing rule (a formulation, no factual claim) | ✅ no claim to check |
| 8 | `A-z` at six members across five projects, and **"four of the six"** skip the escaping | ⚠️ **CORRECTED to five of the six.** `Regex.Escape` appears exactly once in the six projects' `ValidatorExtension` — in the corrected one. Five `A-z` members interpolate a caller-supplied set raw; the sixth takes no set and hard-codes its own. |

**Settled structural questions:** C16 ships **last** (entry 13) — it is a
routing ruling rather than a shape you write, and it reads as a closer; A's
mitigations kept (preamble callout + "**the security one**" in the pointer).
Entry 5's BAD block ships as **elided C#**, not a comment manifest (B's own
self-flag was right). Line width ~100, matching the `references/` siblings.
**Two** documentation-derived blocks, not three — the arbiter cut the `Random`
one because both shipped reference files already carry that text verbatim, and
repeating it would violate the "must not merely repeat" rule.

## 5. Provenance handling

- **C5** — refused the System.Text.Json default-casing behavioural claim
  outright. The entry teaches the *contract* asymmetry, corpus-grounded, and
  delegates the round-trip consequence to the shipped
  `references/serializer-extension.md`.
- **C6** — corpus-observable facts (the missing keyword, the awaiting siblings,
  the one-permit gate) sit outside the marked block; `SemaphoreSlim` semantics
  sit inside it. `Wait()` was demoted out of the GOOD code fence into that
  block: **zero corpus call sites**, so it may not ship as prescribed code.
- **C8** — ASCII code points are arithmetic, derived in the text, not API
  recall. No claim made about how an engine treats `\` or `^` positionally.
- **C16** — no entropy, seed-space, attack-cost or timing claim. No
  cryptographic API named. The entry contributes the corpus-verified
  reachability census and the routing rule; the security property itself is
  cited to the two shipped reference files that already state it.

## 6. Two edits beyond the pure-addition mandate — flag for review

Both are inside my write scope (my own skill's directory) and both are cheap to
revert; I am naming them rather than burying them.

1. **A one-clause factual correction to shipped SKILL.md text.** The JSON
   pattern said *"in one of them the deserialize path passes no options at
   all"*. Verified false — **four of six**. Shipping entry 11 with the correct
   census while SKILL.md carried the wrong one would put a visible
   self-contradiction inside one skill about one file. Applied (arbiter
   concurred, called it warranted and in scope), +2 lines:

   > `…and in four of the six the deserialize path passes no options at all —
   > three of those omit the parameter from the interface, so no call site can
   > supply them. Those services write camelCase and read with the defaults.`

2. **The H1 promotion** described in §2.

**Not done, deliberately:** the canonical
`references/action-context-extension.md` still transcribes the `Guid.Parse`
member. Scope constraint (a) forbade rewriting it here, and both authors plus
the arbiter independently agreed to bank it. Entry 7's closing blockquote does
the work in the meantime: the reference file is the transcription, the entry is
the ruling.

## 7. Proposed CHANGELOG fragment (main session renumbers)

> feat(common-extensions): nine R8-approved anti-examples as
> `references/anti-patterns.md`, numbered 5–13 continuing SKILL.md's four, with
> a 6-line pointer block in SKILL.md (480 → 488 lines; the references file is
> 399). Labelled: a base `Common` file grown a feature department (the user's
> most-confused boundary — the measured inventory behind principle 4, four
> defects with four different owners, and the contamination shown as a trade
> that cost the `ParameterReplacer` rebinding machinery); the canonical name
> and slot holding a three-member stub while the same solution keeps a fuller
> variant elsewhere; `Guid.Parse` on a route value (twelve request-validator
> files → 500 instead of 400, traced through the house exception middleware);
> one more IP answer inlined in middleware (five implementations, four
> behaviours, eleven call sites — one file calling two of them); the `A-z`
> character class (six members, five projects, corrected in the sixth);
> a laxer twin rule that displaced its strict sibling to zero callers;
> a serializer whose asymmetry is in the interface (four of six); `WaitAsync()`
> unawaited in a synchronous overload; and a credential drawn from the shared
> `TickCount`-seeded `Random`. Also corrects a verified-false census clause in
> the shipped JSON pattern (one of six → four of six). No canon rewritten: the
> `Random`/`Guid.Parse` corrections ship as rulings and call-site rules, not as
> edits to the canonical reference files.

## 8. Banked, refused, and open

**Banked R8 candidates surfaced during verification — none labelled** (R8 is
the user's alone; my delegation covers only the nine approved rows):

1. The `do…while (existing.Contains(x))` dedupe loop around the secret
   generator — collision-checking a value that is supposed to be unguessable,
   with an O(n) scan per attempt. Adjacent to C16, a distinct shape.
2. A symmetric serializer variant that clones its options via an AutoMapper
   `CreateMap<JsonSerializerOptions, JsonSerializerOptions>()` profile, where a
   copy constructor exists in-box.
3. `static readonly DefaultOptions` declared **on the interface** — state in a
   contract.
4. The other symmetric serializer variant declaring the same options literal
   twice, inline, once per direction — symmetric today, two declaration sites
   tomorrow.
5. The byte-for-byte-duplicated private IP getter in two service files. Ruled
   **census evidence for C13**, not a tenth label.

**Refused:** nothing was dropped for lack of a site. Two claims were refused on
provenance (System.Text.Json default casing; any quantified statement about the
`Random` seed space), and one candidate framing was refused as inflated — C9's
"validates almost nothing". The entry states what the rule *does* accept and
puts the weight on the verified displacement instead.

**Open item for the user, noticed while verifying:**
`references/validator-extension.md` says the lax phone rule "accepts almost
anything". Entry 10 deliberately states the narrower, accurate version. They do
not contradict outright, but that shipped phrase is the one place the canon
overstates this defect — a candidate for a one-clause narrowing in a later pass.
I did not touch it.
