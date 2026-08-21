# Decouple IsNew from IsModified — Create Means Savable, Not Modified

**ID:** ISNEW (assigned 2026-08-21, replacing the legacy series ID `H5`; unique across active and completed todos; registered in `docs/todos/_ids.md`. Plans referenced as `ISNEW-NNN`.)
**Type:** Enhancement
**Status:** In Progress
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
| 008 | [008-list-metastate-baseline](./plans/008-list-metastate-baseline.md) | List meta-state baseline + paused delete | Draft (stub) |
| 009 | [009-setitem-replaced-item](./plans/009-setitem-replaced-item.md) | SetItem: identity + replaced-item persistence | Draft (stub) |

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

- None yet.

---

## Close-Out Audit

_Not yet run._

---

## Docs & Retro

_Filled at Step 8._

---

## Results / Conclusions

_Filled at Step 8._
