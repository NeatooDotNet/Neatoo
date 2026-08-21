# Test-Infrastructure Debt Inherited From ISNEW

**Plan #:** 005
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md) (re-split out of the original LIST-001)
**Status:** Draft
**Last Updated:** 2026-08-21
**Plan-review opt-in:** No — test-only.
**Code-review opt-in:** No — test-only; the Step 5 test gate is the right reviewer.

---

## Scope

_Stub — Scope only; flesh out at Step 2._

Four test-infrastructure items the ISNEW gates recorded as queued. None changes library
behavior; each one is a place where the suite currently cannot distinguish a working framework
from a broken one.

1. **`MockOrderRepository.GetItems` / `MockEmployeeRepository.GetAddresses` ignore their
   parent-id argument**, so no per-parent child-loading test is expressible in Design.Tests —
   a fetch that loaded the wrong parent's children would pass. (ISNEW `plans/006` Outcome)
2. **`SaveFailureReason.NoFactoryMethod` has no assertion anywhere in the solution** — one
   save-guard reason entirely uncovered. (ISNEW `reviews/004-test-review.md`)
3. **`EntityParentChildFetchTests`' fixture cleans its own objects before asserting**, so it
   structurally cannot distinguish "the framework kept it clean" from "the test cleaned it" —
   the same vacuity shape the ISNEW-004 gate found in `ChildPropertyAttachTests`.
   (ISNEW `reviews/004-test-review.md`)
4. **A parent-side lazy-load assertion.** ISNEW-004's "lazy-loading a child does not dirty its
   parent" bullet is an accepted `MISSING`: the behavior is correct and doubly protected, but
   the only candidate test — `LazyLoadStatePropagationTests.LazyLoadChild_InitialState_ParentNotModified`
   — asserts `child.IsModified` and never touches the parent, despite its name.

Item 3 is the one with teeth: it is a live example of the vacuity shape, and fixing it is what
stops the shape from spreading.
