# Design.Tests Pre-Existing Coverage Debt

**Plan #:** 006
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done (see Outcome; no separate gate — test-only plan whose additions were verified by the full-suite runs in reviews/006-*.log)
**Last Updated:** 2026-08-21
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** TBD at draft

---

## Scope

_Stub — Scope only; flesh out at Step 2. Queued from the ISNEW-001 test review
(`reviews/001-test-review.md`), which split these out as pre-existing tech debt._

Close the pre-existing Design.Tests coverage gaps surfaced by the ISNEW-001 gate:
`Order.Update`'s `IsSelfModified` header-write guard asserted in the positive direction
(root-only change → UpdateOrder called; the skip direction is now pinned in
`SaveAggregateLifecycleTests`); `SaveTests` actually calling `Save()` so
`SaveDemo.Insert/Update/Delete` execute; the cross-aggregate boundary exception
(`EntityListBase.InsertItem` Root check) exercised with its real message — the documented
message renders both aggregates as the same type name ("belongs to aggregate 'Order' …
belongs to aggregate 'Order'"), which is unhelpful and may warrant a library message fix
(instance identity, not just type name; coordinate with ISNEW-003/ISNEW-004 if a library edit is
preferred); root delete paths (`Order.Delete`, `SaveAggregateDemo.Delete`); and the
"must have at least one item" validation rule on non-Draft orders.

Added from the ISNEW-007 gate (`reviews/007-*.md`): `Employee.Update` header-guard positive
direction; `Employee.Delete` coverage (it also ignores `DeletedList`, same shape as
`Order.Delete`); Employee/Address validation-rule coverage; `Address.Create(street, …)`
overload never exercised; `MockEmployeeRepository.GetAddresses` ignoring its `employeeId`.

Added from the ISNEW-002 gate (`reviews/002-test-review.md`) — these live in
Neatoo.UnitTest rather than Design.Tests, so this plan's scope widens to "test-infrastructure
debt across both suites": a remote-call counter on `MakeRemoteDelegateRequest` exposed through
`ClientServerTestBase` (would prove the wire was used for every two-container test at once,
replacing the per-test instance-identity proxy; `TwoContainerMetaStateTests` has the same
blind spot); an explicit parallelization policy for Neatoo.UnitTest (several fixtures depend
on implicit sequential execution — static containers, `SaveLifecycleStore`); and `[Delete]`
paths on the SaveLifecycle aggregate.

---

## Outcome (2026-08-21)

Executed in two passes, as recorded in the Discovery Log.

**Infrastructure pass (before ISNEW-004, so the flip was verified against it):**
`RemoteCallRecorder` + `ClientServerTestBase.RemoteCallCount` proves a call genuinely went
remote — the two-container tests previously could not tell in-process execution from a real
round trip. `ClientServerTestBase` marked `[DoNotParallelize]` with its static-state
dependency documented. Cross-aggregate boundary message fixed (it rendered both aggregates
by type name, so the common case read "belongs to aggregate 'Order' … but this list belongs
to aggregate 'Order'"); `AggregateBoundaryTests` now covers the boundary, the no-aggregate
case, and the documented copy-and-remove alternative.

**Coverage pass (after ISNEW-004, so assertions describe final semantics):**
- `SaveTests` now actually calls `Save()` — four routing tests covering insert / update /
  delete / new-and-untouched, so `SaveDemo`'s persistence bodies execute for the first time.
  `MockSaveDemoRepository` made scoped + recording, id-seeded clear of fetched ids.
- `AggregateCoverageGapTests` (new): the `IsSelfModified` header guard in the **positive**
  direction for both Order and Employee; `Order.Delete` and `Employee.Delete` root delete
  paths (including that a never-persisted child produces no delete); the "non-Draft order
  needs items" rule; Employee negative-salary and future-hire-date rules; the
  `Address.Create(street, …)` overload with its address-type rule; and add-child-then-save-
  twice proving no duplicate insert.
- `ListFactoryStateTests.FetchedChild_RemovedThenReAdded_LeavesDeletedList` pins the
  remove-then-re-add bug ISNEW-003 fixed incidentally (the ISNEW-003 code review's callout 4).

One teaching point surfaced while writing the Address rule test and is preserved in it:
factory operations run paused, so a `[Create]` that populates invalid data reports
`IsValid = true` until something runs rules. The test asserts that first, then calls
`RunRules()` — rather than hiding it behind a `WaitForTasks`.

**Still open, deliberately deferred:** `MockOrderRepository.GetItems` /
`MockEmployeeRepository.GetAddresses` still ignore their parent-id argument (blocks
per-parent child-loading tests); the `HandlePropertyChanged` pause-guard asymmetry and the
broader `EntityListBase.IsModified` notification hole are ISNEW-008's, not this plan's.
