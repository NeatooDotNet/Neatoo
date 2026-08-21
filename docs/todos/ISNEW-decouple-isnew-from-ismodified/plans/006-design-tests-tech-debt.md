# Design.Tests Pre-Existing Coverage Debt

**Plan #:** 006
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
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
