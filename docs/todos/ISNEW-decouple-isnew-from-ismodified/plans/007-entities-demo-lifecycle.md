# Entities Demo Aggregate (Employee/Address) to Canonical Lifecycle

**Plan #:** 007
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-21
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** TBD at draft

---

## Scope

_Stub — Scope only; flesh out at Step 2. Queued from the ISNEW-001 code review
(`reviews/001-code-review.md`, veto #3), which found this third demo aggregate carrying the
identical broken lifecycle the ISNEW-001 sweep fixed elsewhere._

Rework `src/Design/Design.Domain/Entities/` (`Employee.cs`, `Address.cs`, `AddressList.cs`)
to the canonical aggregate lifecycle, mirroring ISNEW-001's OrderAggregate fix: `Employee.Fetch`
currently builds addresses via `addressFactory.Create()` + `LoadValue` (fetched children end
`IsNew=true`); `Employee.Update` writes child rows to the repository directly with no child
factory saves; a phantom `ProcessDeletedAddresses` stub and "FactoryComplete clears
DeletedList" claims describe machinery that doesn't exist. Note the extra content work:
`AddressList.cs` carries DID-NOT-DO / REJECTED-PATTERN blocks that reject list `[Fetch]` and
list persistence ops — the exact things now canonical — so those design-narrative blocks
need rewriting, not just mechanics. Add lifecycle tests in the `AggregateLifecycleTests`
style. Until this lands, Design.Domain contains two files teaching opposite rules about the
same lifecycle — this plan should run early.
