# E2E Aggregate Save Lifecycle Integration Tests

**Plan #:** 002
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-21
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** TBD at draft

---

## Scope

_Stub — Scope only; flesh out at Step 2._

Add integration tests to Neatoo.UnitTest that exercise the full aggregate save lifecycle —
the coverage gap found in the design session (exactly one file in the suite calls `.Save(`,
and list `FactoryComplete(Update)` behavior is only ever simulated manually). Cover
fetch→modify→save, create→save (insert path), child add/remove with DeletedList processing,
and post-save graph state, through the two-container client/server infrastructure where it
applies (`ClientServerTestBase`). Establishes the safety net ISNEW-004 changes semantics under;
assertions pin **current** (pre-flip) semantics and are updated by ISNEW-004. Does not change
library code.

Infrastructure note carried from the ISNEW-001 test review: `MockOrderRepository.GetItems`
ignores its `orderId` (fixed canned children) — key it off `orderId` before building
fetch-after-insert round trips on this infrastructure.
