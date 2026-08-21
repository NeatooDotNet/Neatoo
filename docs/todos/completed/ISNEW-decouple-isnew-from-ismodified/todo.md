# Decouple IsNew from IsModified — Create Means Savable, Not Modified

**ID:** ISNEW (assigned 2026-08-21, replacing the legacy series ID `H5`; unique across active and completed todos; registered in `docs/todos/_ids.md`. Plans referenced as `ISNEW-NNN`.)
**Type:** Enhancement
**Status:** Complete
**Priority:** High
**Created:** 2026-08-19
**Last Updated:** 2026-08-21

**Design document:** [design.md](./design.md) — the full decided design, rationale, CSLA
findings, and The Why (canonical explanation). Plans inherit it; they do not restate it.

**Execution mode (Keith, 2026-08-21):** autonomous run through ALL remaining plans on a
single branch; single PR review at the end. Gates (test-reviewer, code-reviewer,
plan-reviewer where warranted) still run. Discoveries outside the active plan are logged
here rather than pausing; stop only for findings that contradict the decided design scope.
Plan order (as executed): ISNEW-001 → 007 → 002 → 003 → 006-infra → 004 → 006-coverage → 005,
with ISNEW-008 and ISNEW-009 carved out along the way.

---

## Goal

Make `IsModified` answer only "does this object graph differ from its baseline — the state
the factory operation left it in?" and `IsNew` answer only "does persistence not know this
object yet?", with savability admitting either (`IsSavable = (IsModified || IsNew) && …`).
After Create an untouched object is savable but not modified; a `[Create]` that itself
represents user work opts into dirty with `MarkModified()`; attaching a child to a live graph
marks the child modified (dirt, never `IsNew`, is what aggregates upward). Land this on a
verified baseline: the Design.Domain reference aggregate made lifecycle-faithful, real E2E
aggregate save coverage, and list-cache correctness across factory ops. Ships as **0.29.0
(breaking)** with the why documented in code, skill, docs, and briefly in the README.

## Acceptance Criteria

- [x] Design.Domain OrderAggregate demonstrates the lifecycle-correct canonical pattern
      (fetched children `IsNew=false`; post-save graph fully clean; no comments claiming
      framework behavior that doesn't exist)
- [x] E2E aggregate save integration tests cover fetch→modify→save, create→save, and child
      add/remove/delete flows, asserting graph state at each stage
- [x] `ValidateListBase.FactoryComplete` cache staleness is fixed or explicitly dispositioned
- [x] After Create: `IsNew=true, IsModified=false, IsSavable=true` — including aggregates
      with factory-populated children; `MarkModified()` in a `[Create]` body yields
      `IsModified=true`
- [x] A new child attached by user code to a fetched aggregate makes the parent modified and
      savable, and its insert is not skipped by `IsModified`-guarded cascades
- [x] The pinned tests named in design.md are updated to the new semantics; full suite green
- [x] The Why is documented per design.md: code comments, neatoo skill (incl. the COMMON
      MISTAKE), change-tracking guide, api reference + samples, README brief mention with
      link, release notes with migration guide
- [x] Version is 0.29.0

## Out of Scope

- `IsDeleted` semantics — the `IsDeleted` terms in `IsModified`/`IsSelfModified` stay
- RemoteFactory generator changes — save routing (`IsDeleted`/`IsNew` dispatch) is untouched
- MudNeatoo — consults none of these flags (verified)
- `EntityLazyLoad` behavior changes beyond verifying lazy loads stay baseline-clean
- Publishing/releasing 0.29.0 (tag, push, NuGet) — user-initiated per CI standards

---

## Plan Index

Keith's three-phase framing in design.md maps as: "Plan 1 — verified baseline" = ISNEW-001…003,
"Plan 2 — the flip" = ISNEW-004, "Plan 3 — docs & release" = ISNEW-005.

| # | File | Title | Status |
|---|------|-------|--------|
| 001 | [001-design-domain-lifecycle](./plans/001-design-domain-lifecycle.md) | Design.Domain OrderAggregate to Person-canonical lifecycle | Done |
| 002 | [002-e2e-save-coverage](./plans/002-e2e-save-coverage.md) | E2E aggregate save lifecycle integration tests | Done |
| 003 | [003-list-cache-audit](./plans/003-list-cache-audit.md) | List correctness across factory ops (caches + child marking) | Done |
| 004 | [004-decouple-flip](./plans/004-decouple-flip.md) | The flip: IsModified/IsSavable/attach-marking | Done |
| 005 | [005-docs-and-release](./plans/005-docs-and-release.md) | Docs, skill, README, release notes, 0.29.0 | Done |
| 006 | [006-design-tests-tech-debt](./plans/006-design-tests-tech-debt.md) | Design.Tests + harness coverage debt | Done |
| 007 | [007-entities-demo-lifecycle](./plans/007-entities-demo-lifecycle.md) | Entities demo aggregate (Employee/Address) to canonical | Done |
| 008 | _(carved out)_ | List meta-state baseline + paused delete | Retired — became [LIST-001](../LIST-entitylist-state-machinery/plans/001-metastate-baseline-and-paused-delete.md) |
| 009 | _(carved out)_ | SetItem: identity + replaced-item persistence | Retired — became [LIST-002](../LIST-entitylist-state-machinery/plans/002-setitem-replaced-item.md) |

---

## Discovery Log

### 2026-08-21 — ISNEW-004 (gate: two more weld channels)
- **Finding:** The code-review gate found attach-marking had replaced two of **four** channels
  the removed `IsNew` term carried. A factory-populated list assigned to a live parent stopped
  dirtying it (its children were paused-added, so unmarked), and `list[i] = newItem` stopped
  dirtying the list (the cache reads `item.IsModified`, true via the weld for a fresh item).
  Both were regressions this plan introduced; the justifications for excluding them were
  factually wrong. The test gate separately found the guard's `|| IsNew` term unreachable by
  any test, and two vacuous/mis-scoped assertions in tests I had written.
- **Decision:** Amend — both channels fixed within this plan and pinned by tests verified
  failing on revert; `SetItem` partially reversed back in (save-side stays ISNEW-009); the
  new-but-busy guard test added; vacuous assertions corrected. Records in
  `reviews/004-code-review.md` and `reviews/004-test-review.md`.
- **Follow-up:** ISNEW-009

### 2026-08-21 — ISNEW-004 (RemoveItem notification)
- **Finding:** The reversibility acceptance bullet exposed a pre-existing bug —
  `EntityListBase.RemoveItem` updates its modified cache *after* `base.RemoveItem` has
  already run the meta-property check, so the list's IsModified true→false was never
  announced and the parent's cached IsModified stayed true. Add a child, remove it, and the
  aggregate still claimed unsaved changes with nothing to save.
- **Decision:** Amend — `RemoveItem` now announces the change (required by this plan's own
  acceptance); the broader list-notification hole stays queued to ISNEW-008.
- **Follow-up:** ISNEW-008

### 2026-08-21 — ISNEW-004 (property-channel scope)
- **Finding:** Marking *every* assigned child broke six existing tests whose intent is the
  derivation invariant "a property holding an unmodified child is not modified". Parity with
  the weld means marking **new** children only. Separately, marking on `SetItem` broke a
  deliberate cache invariant, and design.md never decided replacement semantics.
- **Decision:** Amend — property marking scoped to new children; `SetItem` dropped from this
  plan entirely and routed to ISNEW-009 (plan Amendments; design.md migration bullet
  corrected a second time).
- **Follow-up:** ISNEW-009

### 2026-08-21 — ISNEW-004 (plan review vetoes)
- **Finding:** Plan review returned CONCERNS with two vetoes. B1: design.md itself was
  factually wrong — assigning a child entity to a parent property was described as never
  dirtying the parent ("a quirk this fixes") when it dirties it today for **new** children,
  through the weld; the channel is mandatory, not a bonus. B2: the original Step 4 silently
  decided a persistence question design.md left open. Also: repo-root `CLAUDE.md` documents
  the pre-flip `IsSavable` and was on no touchpoint list, and design.md cited a SKILL.md
  string that does not exist.
- **Decision:** Amend — design.md corrected at both places, `CLAUDE.md` added to ISNEW-005
  touchpoints with the real SKILL.md line numbers, ISNEW-009 created for `SetItem`, and a
  characterization test for the child-property channel written before the library edit.
- **Follow-up:** ISNEW-009, ISNEW-005

### 2026-08-21 — ISNEW-006 (infrastructure subset pulled forward)
- **Finding:** Two ISNEW-006 items are verification infrastructure rather than coverage debt —
  a remote-call counter on the two-container harness (proves a call actually went remote,
  replacing ISNEW-002's per-test instance-identity proxy) and an explicit parallelization
  policy for the fixtures that depend on sequential execution. Both strengthen the net
  ISNEW-004 is verified against, so running them after the flip wastes their value.
- **Decision:** Re-split — ISNEW-006's infrastructure subset executes before ISNEW-004; its
  coverage subset stays after (those tests want final semantics).
- **Index changes:** No plans added or dropped; ISNEW-006 executes in two passes.
- **Follow-up:** ISNEW-004

### 2026-08-21 — ISNEW (plan order)
- **Finding:** ISNEW-006 (test-debt cleanup) queued ahead of ISNEW-004 would produce tests
  written against pre-flip semantics that the flip then invalidates, and several of its items
  (delete paths, savability assertions) only settle after the flip.
- **Decision:** Re-split — run ISNEW-004 before ISNEW-006 while the ISNEW-002/003 safety net
  is fresh.
- **Index changes:** Order changed to 001, 007, 002, 003, **004, 006, 005**; no plans added
  or dropped.
- **Follow-up:** ISNEW-004

### 2026-08-21 — ISNEW-003 (defect 1 reachability)
- **Finding:** The first cache-staleness regression test passed with the fix reverted —
  `HandlePropertyChanged` has no pause guard, so caches self-heal on any later event, and
  fetched children start valid (rules don't run during factory ops). The stale window only
  exists for items already invalid at the moment of a paused add. Separately, three existing
  characterization tests pinned the child-identity defect.
- **Decision:** Amend — regression test rewritten at the reachable level and verified failing
  on revert; the three characterization tests updated to the corrected behavior with a new
  test pinning the other half of the paused-add contract (plan Amendments).
- **Follow-up:** n/a

### 2026-08-21 — ISNEW-002 (test review)
- **Finding:** The static shared store made the client/server round trip unprovable (tests
  passed identically in-process), and the created-untouched savability test was pinned to
  property dirt rather than the `IsNew` weld — both would have hidden ISNEW-004 regressions.
  Also: a user-attached new child was never isolated from other dirt.
- **Decision:** Amend — boundary proof, rich `[Create]` overload, and isolated attach/remove
  tests added (plan Amendments; `reviews/002-test-review.md`).
- **Follow-up:** ISNEW-006 (harness remote-call counter; parallelization policy)

### 2026-08-21 — ISNEW-007 (code review: Address standalone role)
- **Finding:** Address's standalone-root role was both unreachable (`IAddress : IEntityBase`
  declares child) and harmful: any parent-less `[Remote]` op makes the generator emit a
  **public** `Save(IAddress)`, letting consumers persist a child outside its aggregate — a
  hole the canonical `IOrderItemFactory` does not have. `RemoteBoundary.cs` also still taught
  the pre-ISNEW-001 rule by name.
- **Decision:** Amend — role removed (with `IAddressOnlyRepository`), duality teaching moved
  and corrected in `RemoteBoundary.cs`; the plan's "preserve dual-role commentary" constraint
  was overridden (plan Amendments; `reviews/007-code-review.md`).
- **Follow-up:** ISNEW-006 (Employee.Delete + DeletedList, rule coverage, header guard)

### 2026-08-21 — ISNEW-007 (test review: FK propagation)
- **Finding:** Child inserts discarded the parent id in the mock, so reordering
  `Employee.Insert` (delegate before writing own Id) would orphan every child at
  `employeeId = 0` with all tests still green.
- **Decision:** Amend — mock records parent ids; both save tests assert FK correctness.
- **Follow-up:** n/a

### 2026-08-21 — ISNEW-001 (code review: Entities aggregate + routing docs)
- **Finding:** Code review (`reviews/001-code-review.md`) found (a)
  `Design.Domain/Entities/` (Employee/Address/AddressList) is a third demo aggregate with
  the identical broken lifecycle, including DID-NOT-DO blocks rejecting what is now
  canonical; (b) Save-routing pseudocode in SavePatterns.cs contradicted the generated code
  — actual routing is `IsDeleted` first, then `IsNew`, else Update, and **IsModified is
  never consulted**; a created-then-deleted root routes to `[Delete]`, not a no-op.
- **Decision:** Re-split — ISNEW-007 added (Entities rework, run early); routing docs fixed
  in-plan. ISNEW-004 must account for the IsDeleted-wins-over-IsNew routing when Save() admits
  IsNew.
- **Index changes:** Plan 007 added.
- **Follow-up:** ISNEW-007; note for ISNEW-004 draft.

### 2026-08-21 — ISNEW-001 (IsChild gap)
- **Finding:** No fetch shape today yields fully correct child state — paused adds (canonical
  list `[Fetch]`) skip `MarkAsChild`/`SetContainingList`; the un-paused path is unusable for
  fetch because it `MarkModified()`s non-new items. Fetched children have `IsChild=false`,
  `ContainingList=null`; `item.Delete()` bypasses list routing for them.
- **Decision:** Amend (ISNEW-001 documents the gap, steers removal through `list.Remove`;
  details in the plan's Amendments). ISNEW-003 upgraded from "decide whether" to **required**.
- **Follow-up:** ISNEW-003

### 2026-08-21 — ISNEW-001 (sweep)
- **Finding:** `FetchPatterns.cs` and `SavePatterns.cs` demonstrated the broken lifecycle as
  positive patterns (Create+LoadValue child fetch, direct repo child writes), plus wrong
  `PauseAllActions` guidance (explicit `using` inside a factory op resumes early on dispose).
- **Decision:** Amend — both demo aggregates reworked to canonical within ISNEW-001 (see the
  plan's Amendments).
- **Follow-up:** n/a

### 2026-08-21 — ISNEW (design session, pre-implementation)
- **Finding:** Design analysis surfaced three baseline defects that ISNEW's semantics would sit
  on: Design.Domain OrderAggregate's Fetch leaves children `IsNew=true` and its Update never
  cleans child state (no FactoryComplete cascade exists in the framework — verified); the
  suite has essentially no E2E aggregate `Save()` coverage; `ValidateListBase.FactoryComplete`
  skips the cache recalculation `ResumeAllActions` performs. Full record in
  [design.md](./design.md).
- **Decision:** Re-split
- **Index changes:** Initial split seeded with baseline plans ISNEW-001…003 ordered ahead of the
  flip (ISNEW-004) and docs/release (ISNEW-005).
- **Follow-up:** ISNEW-001

---

## Skipped Steps

- Step 1 ID proposal — initially kept the legacy series ID `H5`; renamed to `ISNEW`
  2026-08-21 at Keith's request (full sweep: folder, todo/plans/reviews, and the four source
  files citing plan IDs in comments). `_ids.md` bootstrapped with the same change.
- Step 1 reconnaissance agents — recon performed directly in the 2026-08-20/21 design
  session (full code walk recorded in design.md and plan Current State sections).
- ISNEW-005 — Step 5 gate skipped (doc-only plan; no behavior to review). Verified instead by
  `dotnet mdsnippets` regeneration plus full-suite runs.
- ISNEW-006 — Step 5 gate skipped (test-only, additive; the coverage it adds was itemized by
  the ISNEW-001/003/007 gates rather than designed fresh). Flagged by the close-out audit,
  which spot-checked the ungated `AggregateCoverageGapTests.cs` and found its assertions
  substantive — exact-match routing counts, a rules-run-ordering teaching point, and a
  save-twice idempotency check. Recorded as process hygiene, not a coverage gap.

---

## Sibling Todos

- [LIST — EntityListBase State Machinery](../LIST-entitylist-state-machinery/todo.md) — the
  two plans carved out of this arc at close-out (ISNEW-008/009 → LIST-001/002). Every item
  was found by an ISNEW gate but does not advance this Goal.

---

## Close-Out Audit

### 2026-08-21 — Verdict: CONCERNS → resolved

**Veto-tier findings:** 7, all resolved. No runtime defects — every one was documentation
still teaching the removed semantics, or container integrity.

- V1 `docs/reference/api.md` stated the pre-flip `IsModified` derivation verbatim — fixed.
- V2 `docs/release-notes/v0.29.0.md` shipped the justification the ISNEW-004 gate disproved,
  and listed two of four attach-marking channels — fixed.
- V3 `docs/guides/entities.md` (+ its sample) taught the removed rule in three places — fixed.
- V4 Design.Domain contradicted itself: `AllBaseClasses.cs` said "New Entity: IsModified=true"
  sixteen lines above the opposite, and `OrderItem.cs` still documented the deleted
  `!item.IsNew` exemption — fixed.
- V5 `reviews/003-code-review.md` never existed while four documents cited it, and the plan
  header called it "clean" despite five callouts (two spawning ISNEW-008) — written from the
  findings preserved downstream, marked as reconstructed.
- V6 `plans/004` cited a test its own gate had renamed, and the fallback citation asserts only
  the child — the lazy-load bullet is now an accepted `MISSING`, behavior traced and correct.
- V7 five gate-recorded deferrals traced to no Plan Index entry — folded into ISNEW-008.

**Callouts:** all seven swept — MudNeatoo skill formula; README's "IsNew cascades" claim;
unverifiable Test Evidence numbers (ISNEW-003's archived logs predate its fix loop);
`TBD at draft` gate headers on plans 005/006 plus Skipped Steps entries; `SetItem` added to
migration point 5; `docs/todos/index.md` now lists this todo and explains the two-registry
split with `_ids.md`.

**Verified clean by the audit:** Plan Index ↔ `plans/` reconciles (nine files, nine rows,
monotonic, no Abandoned/Retired); all five Out of Scope items hold; build warnings are
byte-identical to the pre-arc baseline (the arc introduced none); no sacred test was gutted
across ~15 changed pre-existing assertions; and both core acceptance tests are strong and
non-vacuous.

**Full audit:** delivered by the close-out agent; findings and dispositions recorded above and
in the commit `docs: close all seven close-out audit vetoes (ISNEW)`.

**Process gap noted for next time:** this repo has no `docs/code-review-calibration.md`, so
each reviewer reasoned from first principles about what "clean" means here.

---

## Deferred Work Carrying Forward

| # | Description | Queued | Cost |
|---|---|---|---|
| 1 | Stale meta-state baseline after `FactoryComplete(Update)` swallows the next child-edit notification | ISNEW-008 | Fat-client saves only; aggregate reports not-savable after a save that carried deletions |
| 2 | `Delete()` inside a paused window silently drops a child | ISNEW-008 | No canonical flow reaches it today |
| 3 | `HandlePropertyChanged` has no pause guard while the three mutators do | ISNEW-008 | Adding one "for symmetry" would reopen the ISNEW-003 defect with a green suite |
| 4 | `EntityListBase.IsModified` raises no `PropertyChanged` | ISNEW-008 | Real hole for Blazor bindings |
| 5 | `ResumeAllActions`' `if (IsPaused)` guard makes `FactoryComplete` a no-op on a never-paused list | ISNEW-008 | Live hole in the ISNEW-003 fix |
| 6 | Paused `InsertItem` skips duplicate/busy/aggregate-boundary guards | ISNEW-008 | Defensible for trusted input, unasserted |
| 7 | Repository mocks ignore their parent-id argument | ISNEW-008 | Blocks per-parent child-loading tests |
| 8 | `SaveFailureReason.NoFactoryMethod` has no assertion anywhere | ISNEW-008 | One save-guard reason uncovered |
| 9 | `EntityParentChildFetchTests` fixture cleans its own objects before asserting | ISNEW-008 | Cannot distinguish framework-clean from test-cleaned |
| 10 | Parent-side lazy-load assertion (ISNEW-004's accepted `MISSING`) | ISNEW-008 | Behavior correct and doubly protected, but unasserted |
| 11 | `SetItem`: displaced item orphaned, no child identity, no guards | ISNEW-009 | Replacing a persisted child silently orphans its row — needs its own release note |
| 12 | `MapModifiedTo` over an entity-child property | Accepted (`reviews/004-test-review.md`) | Unchanged for scalars, which is every current usage |
| 13 | Publishing 0.29.0 (tag, push, NuGet) | Out of Scope, per CI standards | User-initiated |

---

## Docs & Retro

**Documentation:** doc deltas shipped in the same branch as the behavior change (ISNEW-005),
then twice more after review: a self-directed repo-wide sweep found eleven stale `IsSavable`
formulas the per-plan gates could not see, and the close-out audit found four more surfaces.
No documentation debt remains open; `skills/neatoo/` and `skills/mudneatoo/` are copied to
`~/.claude/skills/`.

**Retro.** Three things this arc taught about the workflow itself.

*Per-plan gates are scoped to a plan's diff, so a semantics change that ripples across a repo
needs a deliberate repo-wide sweep on top.* Every stale-doc finding — mine and the audit's —
sat in a file no individual plan touched. The gates were not failing; they were correctly
scoped and structurally blind to it. Worth making an explicit step for cross-cutting changes.

*"Verify by reverting the fix" caught false coverage three separate times*, including a case
where my own regression test passed with the fix removed, and a case where a revert run
silently used a stale binary because the revert did not compile. It should be the default for
any test claiming to pin a bug fix, not a spot check.

*The most valuable findings were all "your justification is factually wrong," not "your code
is wrong."* The plan review found design.md itself mis-describing current behavior; the code
review found two channels I had excluded with confident, incorrect reasoning. Both times the
code compiled and every test passed. That is the failure mode reviews exist for, and it argues
for briefs that ask reviewers to verify *claims* against source rather than review diffs.
## Results / Conclusions

`IsModified` and `IsNew` now answer their own questions. After `[Create]` an object is
`IsNew=true, IsModified=false, IsSavable=true` — it needs inserting, but holds no user work,
so unsaved-changes guards stay quiet on it, including on a freshly re-derived object after a
save. That was the zTreatment symptom that started this. A `[Create]` whose result *is* the
user's work opts in with the existing `MarkModified()`; no new API, no configuration knob.

The load-bearing insight, which took three attempts to get right: the removed `IsNew` term was
doing a second, hidden job — carrying a new child's arrival up the object graph, because
`IsNew` itself never aggregates. Replacing that job took **four** attach-marking channels, and
the first implementation shipped two of them. The gates found the other two.

Five defects were fixed along the way, none of them on the original list: fetched children
loaded with `Create()+LoadValue` (so the next save re-inserted them), a list reporting the
state of an empty list after its own factory op, fetched children with no `IsChild` so
`Delete()` silently did nothing, a list going clean without announcing it (so the parent
stayed permanently dirty), and a cross-aggregate error message that named both aggregates
identically. Design.Domain — the documented source of truth — was teaching a child-persistence
cascade the framework does not implement.

### Plan Sequence

```
Plans for this todo (`docs/todos/completed/ISNEW-decouple-isnew-from-ismodified/todo.md`):
- [x] 001-design-domain-lifecycle    — Done
- [x] 002-e2e-save-coverage          — Done
- [x] 003-list-cache-audit           — Done
- [x] 004-decouple-flip              — Done
- [x] 005-docs-and-release           — Done
- [x] 006-design-tests-tech-debt     — Done (two passes, straddling 004)
- [x] 007-entities-demo-lifecycle    — Done
- [ ] 008-list-metastate-baseline    — Draft (carved out; carries 10 deferrals)
- [ ] 009-setitem-replaced-item      — Draft (carved out)

Discovery Log: 11 entries.
Gates: 4 test reviews, 4 code reviews, 1 plan review, 1 close-out audit.
Close-Out Audit: CONCERNS (7 veto) → all resolved.
Tests: 2144 → 2178 (solution), 110 → 129 (Design.Tests). Zero failures, 2 pre-existing skips.
Version: 0.28.1 → 0.29.0 (breaking).
```

ISNEW-008 and ISNEW-009 remain open as Draft stubs and do not block this todo's Goal. They
carry thirteen deferred items between them (see the table above), each traceable to a gate
finding rather than to prose.

---

## ID Redirect Note

`ISNEW-008` and `ISNEW-009` were carved out to the sibling todo **LIST** at close-out and
renumbered. References to them anywhere in this container — plans, reviews, Discovery Log —
resolve as:

- **ISNEW-008** → [LIST-001](../../LIST-entitylist-state-machinery/plans/001-metastate-baseline-and-paused-delete.md)
  (meta-state baseline, paused delete, notification holes, inherited test debt)
- **ISNEW-009** → [LIST-002](../../LIST-entitylist-state-machinery/plans/002-setitem-replaced-item.md)
  (SetItem identity + replaced-item persistence)

The in-place citations were left as written rather than rewritten, so the records read as they
did when each decision was made; this note is the mapping.
