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
