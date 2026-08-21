# Test-Infrastructure Debt Inherited From ISNEW

**Plan #:** 005
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md) (re-split out of the original LIST-001)
**Status:** Done
**Last Updated:** 2026-08-21
**Plan-review opt-in:** No — test-only.
**Code-review opt-in:** No — test-only; the Step 5 test gate is the right reviewer.

---

## Scope

Four test-infrastructure items the ISNEW gates recorded as queued. None changes library behavior;
each is a place where the suite could not distinguish a working framework from a broken one.

1. **`MockOrderRepository.GetItems` / `MockEmployeeRepository.GetAddresses` ignore their
   parent-id argument**, so no per-parent child-loading test is expressible in Design.Tests — a
   fetch that loaded the wrong parent's children would pass. (ISNEW `plans/006` Outcome)
2. **`SaveFailureReason.NoFactoryMethod` has no assertion anywhere in the solution.**
   (ISNEW `reviews/004-test-review.md`)
3. **`EntityParentChildFetchTests`' fixture cleans its own objects before asserting**, so it
   structurally cannot distinguish "the framework kept it clean" from "the test cleaned it".
   (ISNEW `reviews/004-test-review.md`)
4. **A parent-side lazy-load assertion.** `LazyLoadStatePropagationTests.LazyLoadChild_InitialState_ParentNotModified`
   asserts `child.IsModified` and never touches the parent, despite its name.

## Intent

Every one of these tests either means what its name says, or is honestly recorded as not covering
it. No test in this set can keep passing while the behavior it claims to check is broken.

## Constraints & Invariants

- Existing tests are sacred. Item 3 rewrites a fixture, which is only legitimate because the
  rewrite **preserves and strengthens** the original intent ("a fetched parent/child graph is
  clean") rather than weakening it. Nothing is deleted or relaxed.
- Test-only: no library file is touched by this plan.
- Mock changes must be additive — existing Design.Tests assertions about the default rows keep
  passing untouched.

## Current State

Walked 2026-08-21.

- **Item 1 confirmed.** `Design.Tests/TestInfrastructure.cs:110` — `GetItems(int orderId)` ignores
  `orderId` and returns two fixed rows; `:264` — `GetAddresses(int employeeId)` ignores
  `employeeId` and returns three.
- **Item 2 is ALREADY CLOSED — the finding is stale.**
  `EntityBaseStateTests.Save_WhenNoFactory_ThrowsSaveOperationException` (`:890-900`) asserts
  `Assert.AreEqual(SaveFailureReason.NoFactoryMethod, ex.Reason)`. Either it landed later in ISNEW
  than the review that flagged it, or the review missed it. Recorded rather than "fixed".
- **Item 3 confirmed, and fully vacuous.** `EntityParentChildFetchTests.TestInitialize` calls
  `MarkOld()` and `MarkUnmodified()` on both parent and child; `..._Fetch_InitialMeta` then
  asserts exactly `IsNew == false` and `IsModified == false` on both. **Every assertion in that
  test was guaranteed by its own setup.** The objects were also never fetched — they were newed
  up and filled via `FromDto`, despite the class being named `...FetchTests`.
- **Item 4 confirmed.** `LazyLoadStatePropagationTests.cs:34` asserts only the child.
- A real `[Fetch]` factory method exists — `PersonEntityBase.FillFromDto`, surfaced as
  `IEntityPersonFactory.FillFromDto` — so item 3 can use a genuine factory operation.
- `EntityBase.IsNew` (`:212`) has **no initializer**, so it defaults to `false`; and `FromDto`
  loads through `LoadValue` inside a pause. A real `[Fetch]` therefore already lands on
  old-and-clean, which is why item 3's manual marks were redundant as well as vacuity-inducing.

## Steps

1. Make the mock repositories parent-keyed, additively, and prove the new capability with a test
   that actually seeds two parents and asserts isolation.
2. Record item 2's stale-finding status rather than manufacturing a duplicate assertion.
3. Rebuild the `EntityParentChildFetchTests` fixture on the real `[Fetch]` factory and drop the
   self-cleaning marks, preserving every existing assertion.
4. Add the parent-side assertions item 4's test name promises.
5. Verify each change actually catches the failure it claims to; run both test projects.

## Acceptance

- A Design.Tests test loads two different parents and asserts each got its own children
  [integration]
- Reverting the mock to ignore its parent id fails that test [integration]
- `SaveFailureReason.NoFactoryMethod` is asserted somewhere in the solution [unit]
- `EntityParentChildFetchTests` derives its clean state from a real factory operation, not from
  setup marks, with every original assertion preserved [integration]
- `LazyLoadChild_InitialState_ParentNotModified` asserts the parent [integration]
- Both `src/Neatoo.sln` and `Design.Tests` green [unit]

---

## Test Evidence

| Acceptance bullet | Test | Tier | Status |
|---|---|---|---|
| Two parents load their own children | `AggregateLifecycleTests.Fetch_LoadsTheChildrenOfTheRequestedOrder_NotSomeOtherOrders` (new), backed by the seedable `MockOrderRepository.ItemsByOrderId` | integration | Pinned |
| Reverting the mock to ignore its parent id fails that test | Revert run 2026-08-21: `GetItems` restored to the id-ignoring form → **exactly 1 failure**, that test | integration | Pinned |
| `NoFactoryMethod` asserted somewhere | `EntityBaseStateTests.Save_WhenNoFactory_ThrowsSaveOperationException` — **pre-existing**; the ISNEW finding was stale | unit | Already covered; no change made |
| `EntityParentChildFetchTests` derives clean state from a real factory operation | Fixture rebuilt on `IEntityPersonFactory.FillFromDto`; `MarkOld`/`MarkUnmodified` removed; all 5 tests pass unmodified | integration | Pinned |
| `LazyLoadChild_InitialState_ParentNotModified` asserts the parent | Same test, now asserting `parent.IsModified` and `parent.IsSelfModified` | integration | Pinned |
| Both projects green | `src/Neatoo.sln`: 1845 / 0 / 2 skipped, plus Samples 254, BaseGenerator 42, Person.DomainModel 55. `Design.Tests`: 130 / 0 | unit | Pinned |

## Outcome

All four items closed, though one was closed by **finding it already done**: item 2's
`NoFactoryMethod` assertion exists and has for some time, so the ISNEW review's finding was stale.
Recording that is the honest close — manufacturing a second assertion to "complete" the item would
have added a duplicate test and a false sense of new coverage.

Item 3 was the one with teeth, and it was worse than the finding described. The fixture did not
merely clean its objects — it produced a state by hand (`MarkOld`, `MarkUnmodified`) that the test
then asserted verbatim, so **every assertion in `..._Fetch_InitialMeta` was guaranteed by its own
setup**, and the objects had never been through a fetch at all despite the class name. Rebuilding
it on the real `[Fetch]` factory means the same assertions now describe framework behavior. They
pass unchanged, which also confirms the manual marks were redundant: `IsNew` defaults to `false`
and `FromDto` loads through `LoadValue` under a pause.

One thing deliberately left alone and worth flagging: `child.MarkAsChild()` is still called by
hand in that fixture, because assigning a child entity to a parent's entity property does **not**
confer child identity. That is arguably its own gap — the attach channels ISNEW-003 unified do not
include this one — but it is entity-property behavior rather than list state machinery, so it is
recorded here rather than changed.

A process note: `Design.Tests` is **not** part of `src/Neatoo.sln`. Every "full suite" run earlier
in this todo covered only the solution. Both must be run to see the whole picture — this plan is
the first in the arc to touch Design.Tests, and it would have been easy to ship it unverified.
