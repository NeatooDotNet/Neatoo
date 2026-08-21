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
Plan order: ISNEW-007 → 002 → 003 → 006 → 004 → 005.

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

- [ ] Design.Domain OrderAggregate demonstrates the lifecycle-correct canonical pattern
      (fetched children `IsNew=false`; post-save graph fully clean; no comments claiming
      framework behavior that doesn't exist)
- [ ] E2E aggregate save integration tests cover fetch→modify→save, create→save, and child
      add/remove/delete flows, asserting graph state at each stage
- [ ] `ValidateListBase.FactoryComplete` cache staleness is fixed or explicitly dispositioned
- [ ] After Create: `IsNew=true, IsModified=false, IsSavable=true` — including aggregates
      with factory-populated children; `MarkModified()` in a `[Create]` body yields
      `IsModified=true`
- [ ] A new child attached by user code to a fetched aggregate makes the parent modified and
      savable, and its insert is not skipped by `IsModified`-guarded cascades
- [ ] The pinned tests named in design.md are updated to the new semantics; full suite green
- [ ] The Why is documented per design.md: code comments, neatoo skill (incl. the COMMON
      MISTAKE), change-tracking guide, api reference + samples, README brief mention with
      link, release notes with migration guide
- [ ] Version is 0.29.0

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
| 003 | [003-list-cache-audit](./plans/003-list-cache-audit.md) | List correctness across factory ops (caches + child marking) | Draft (stub) |
| 004 | [004-decouple-flip](./plans/004-decouple-flip.md) | The flip: IsModified/IsSavable/attach-marking | Draft (stub) |
| 005 | [005-docs-and-release](./plans/005-docs-and-release.md) | Docs, skill, README, release notes, 0.29.0 | Draft (stub) |
| 006 | [006-design-tests-tech-debt](./plans/006-design-tests-tech-debt.md) | Design.Tests pre-existing coverage debt | Draft (stub) |
| 007 | [007-entities-demo-lifecycle](./plans/007-entities-demo-lifecycle.md) | Entities demo aggregate (Employee/Address) to canonical | Done |

---

## Discovery Log

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
