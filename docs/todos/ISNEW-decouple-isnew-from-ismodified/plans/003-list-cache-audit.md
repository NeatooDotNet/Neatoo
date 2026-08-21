# List Correctness Across Factory Ops (Caches + Child Marking)

**Plan #:** 003
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** In Progress
**Last Updated:** 2026-08-21
**Plan-review opt-in:** No (two contained defects in the list base classes, both diagnosed against source in the ISNEW-001 pre-flight and discovery log; the flip that depends on them is separately reviewed in ISNEW-004)
**Code-review opt-in:** Yes (first library change of the arc; touches state machinery ISNEW-004 then builds on)

---

## Scope

Fix two defects in the list base classes that make list state wrong after a factory
operation: `FactoryComplete` resumes without recalculating cached meta state, and items added
while the list is paused never get marked as children. Add regression coverage for both.
Does NOT change `IsModified`/`IsNew` semantics (ISNEW-004), and does not touch the
entity/property machinery outside what these two fixes require.

---

## Intent

Two observable wrongs today, both hit by the canonical fetch path this arc just made
standard:

- A list populated by its own `[Fetch]` can report stale validity/busy state, because
  factory completion resumes the list without the recalculation that `ResumeAllActions`
  performs. A fetched list containing an invalid child can therefore claim `IsValid = true`.
- Children loaded that way are not marked as children: `IsChild` stays false and
  `ContainingList` stays null, so `child.Delete()` silently bypasses list routing (no
  DeletedList entry, no deletion at save) and nothing stops a consumer treating a child as a
  root. ISNEW-001/007 had to document this defect rather than assert correct state.

After this plan, a list is in the same correct state after a factory operation as it is after
any other resume, and a child is a child however it got into the list. That also lets the
documentation caveats those plans added be removed.

---

## Framework & Architectural Alignment

- `PauseAllActions`/`ResumeAllActions` is the framework's single resume path; factory
  completion is a resume and should not be a second, weaker implementation of it.
- Child marking (`MarkAsChild`, `SetContainingList`) is **baseline-neutral** state — it says
  what an object *is*, not that anything changed — so it belongs on every add, paused or not.
  Dirt-producing steps (`MarkModified` on re-added existing items) stay on the live path only.
- Interface-first and the internal-interface pattern for the framework-only operations.

---

## Constraints & Invariants

- Adding items while paused must remain baseline-clean: no item and no list may come out of a
  factory `[Fetch]` reporting modified.
- Deserialization must keep working: it also adds while paused, and `IsChild` round-trips
  through serialization while `ContainingList` cannot.
- The DeletedList restore path on the paused branch keeps its behavior.
- Existing tests keep their intent; any that pin the *defective* behavior are a discovery,
  not a licence to edit.
- No change to `IsModified`, `IsNew`, `IsSavable`, or the save guard.

---

## Steps

1. Make factory completion on lists go through the same resume path as everything else, so
   cached meta state is recalculated rather than left stale.
2. Verify the entity-list layer's own factory-completion work (DeletedList cleanup, modified
   cache) still happens in the right order relative to that resume.
3. Mark items as children on the paused add path too, and give them their containing-list
   reference, keeping the dirt-producing steps on the live path.
4. Confirm the deserialization add path benefits from the same change without double-marking
   or resurrecting removed state.
5. Add regression coverage for both defects at the level that would have caught them: a
   fetched list with an invalid child, and a fetched child's identity and delete routing.
6. Remove the documentation caveats ISNEW-001/007 added that cite this plan.

---

## Acceptance

- [ ] A list populated through its own `[Fetch]` reports validity that reflects its children
      once the operation completes `[integration]`
- [ ] A child loaded through the canonical fetch path reports itself as a child and knows its
      containing list `[integration]`
- [ ] Calling `Delete()` on such a child routes through the list — it lands in the deleted
      set and is deleted at save, rather than silently doing nothing `[integration]`
- [ ] Fetching still produces a completely clean graph: no item and no list reports modified
      after the change `[integration]`
- [ ] Deserialization of a list with children preserves child identity and clean state
      `[integration]`
- [ ] Documentation caveats citing this plan are removed from Design.Domain
      `[explicit-skip: prose cleanup, verified by the opted-in code review]`
- [ ] Build and both suites green `[explicit-skip: meta-bullet, gate run]`

---

## Current State (Pre-Flight)

Walked 2026-08-21 before the first edit:

- **Defect 1 — resume without recalculation.** `ValidateListBase.FactoryComplete`
  (`ValidateListBase.cs:573-576`) sets `IsPaused = false` directly.
  `ValidateListBase.ResumeAllActions` (`:544-556`) is the real resume: it recalculates
  `_cachedIsValid` / `_cachedIsBusy` and calls `ResetMetaState()`.
  `EntityListBase.ResumeAllActions` (`EntityListBase.cs:404-413`) additionally recalculates
  `_cachedChildrenModified`. Because `InsertItem`'s cache updates are skipped while paused
  (`ValidateListBase.cs:146-156`), a list populated during its own `[Fetch]` keeps the
  initial `_cachedIsValid = true` regardless of its children.
  `EntityListBase.FactoryComplete` (`EntityListBase.cs:381-398`) recalculates
  `_cachedChildrenModified` **only** on `Update`, so Fetch/Create leave every cache stale.
  Note `OnDeserialized` (`ValidateListBase.cs:304-316`) already does this correctly — it
  calls `ResumeAllActions()` — which is the shape to copy.
- **Defect 2 — paused adds skip child marking.** `EntityListBase.InsertItem`
  (`EntityListBase.cs:198-267`): the un-paused branch runs the aggregate checks, `UnDelete`,
  `MarkModified` (non-new only), `MarkAsChild`, `SetContainingList` and the dirt-cache
  update; the paused branch only diverts deleted items into `DeletedList` and returns. So
  canonical fetched children get neither `IsChild` nor `ContainingList`. `MarkAsChild`
  (`EntityBase.cs:284-287`) sets a bool; `SetContainingList` (`:624-627`) sets a field —
  both baseline-neutral. `MarkModified` is the only dirt-producing step and must stay on the
  live path.
- **Serialization interaction.** `IsChild` is on `IEntityMetaProperties`, is written by the
  converter (`NeatooBaseJsonTypeConverter.cs:414-427`), and is restored because
  `EntityBase.IsChild` has a `protected set` (the read side collects `EntityBase<>`
  properties with a setter, `:131-141`). `ContainingList` is neither public nor on the
  interface, so it does **not** round-trip — fixing the paused add path is what restores it
  after deserialization too.
- **Blast radius.** `MarkAsChild` is called from exactly one place (`EntityListBase.cs:247`);
  `SetContainingList` from two (`:250`, and cleared at `:389`). No analyzer or generator
  depends on either.
- **Existing coverage.** `Unit/Core/EntityListBaseTests.cs` drives `FactoryComplete` directly
  to simulate saves and asserts DeletedList/modified behavior — those assertions are about
  `Update` and should survive. No existing test asserts `IsChild` for a fetched child (the
  ISNEW-001 discovery), and none asserts list validity after a factory fetch.

---

## Test Evidence

Both regression tests were **verified by reverting the fix and confirming they fail** — see
Plan Amendments for what that exercise changed about the defect-1 test.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| Fetched list reports validity reflecting its children | `[integration]` | `EntityListBaseStateTransitionTests.FactoryComplete_AfterPausedAddOfInvalidItem_ListReportsInvalid` (defect-1 regression; verified failing on revert) + `ListFactoryStateTests.FetchedInvalidData_OnceRulesRun_AggregateReportsInvalid` (aggregate-scope propagation) | ✓ |
| Fetched child reports itself a child and knows its list | `[integration]` | `ListFactoryStateTests.FetchedChild_IsMarkedAsChild`; `ContainingListTests.ContainingList_WhenPaused_IsSet`; `EntityListBaseStateTransitionTests.Add_Item_WhenPaused_IsChildIsSet` | ✓ |
| `Delete()` on such a child routes through the list and deletes at save | `[integration]` | `ListFactoryStateTests.FetchedChild_Delete_RoutesThroughList_AndDeletesOnSave` (verified failing on revert) | ✓ |
| Fetching still produces a completely clean graph | `[integration]` | `ListFactoryStateTests.FetchedGraph_IsCompletelyClean`; `EntityListBaseTests.Add_WhenPaused_DoesNotMarkModified` | ✓ |
| Deserialization preserves child identity and clean state | `[integration]` | `ListFactoryStateTests.DeserializedChildren_KeepIdentityAndCleanState` (verified failing on revert) | ✓ |
| ISNEW-003 caveats removed from Design.Domain | `[explicit-skip]` | Covered by opted-in code review; `grep ISNEW-003 src/Design` returns nothing | — |
| Build + both suites green | `[explicit-skip]` | `reviews/003-*.log` — sln 2160 passed / 2 pre-existing skips; Design.Tests 113/113 | — |

---

## Plan Amendments

### 2026-08-21 — Defect 1's stale window is narrower than the pre-flight assumed

- **Section affected:** Current State (defect 1), Acceptance bullet 1
- **Original said:** a list populated by its own `[Fetch]` "can report stale validity/busy
  state… a fetched list containing an invalid child can therefore claim `IsValid = true`."
- **What changed:** The first regression test asserted exactly that through the canonical
  fetch path — and it **passed with the fix reverted**, so it was not pinning the defect.
  Cause: `HandlePropertyChanged` has no pause guard, so any child that becomes invalid
  *after* being added updates the cache normally; and fetched children start valid because
  rules do not run during a factory operation. The stale window is therefore only reachable
  when an item is **already invalid at the moment of a paused add**. The test was rewritten
  at that level (`FactoryComplete_AfterPausedAddOfInvalidItem_ListReportsInvalid`) and
  verified to fail on revert; the aggregate-scope test was kept but relabelled to say
  plainly that it exercises propagation, not the cache fix.
- **Why:** A regression test that passes without the fix is false coverage — the exact
  failure mode the Test Evidence map exists to catch.
- **Discovery Log link:** 2026-08-21 — ISNEW-003 (defect 1 reachability)

### 2026-08-21 — Three existing tests pinned the defective behavior

- **Section affected:** Constraints (existing tests keep their intent), Step 3
- **Original said:** tests pinning the defect are "a discovery, not a licence to edit."
- **What changed:** `EntityListBaseTests.Add_WhenPaused_DoesNotMarkAsChild`,
  `EntityListBaseStateTransitionTests.Add_Item_WhenPaused_IsChildNotSet`, and
  `ContainingListTests.ContainingList_WhenPaused_NotSet` failed. All three are
  characterization tests whose comments describe the mechanism ("MarkAsChild is in
  EntityListBase, skipped when paused") rather than asserting a requirement. They were
  updated to characterize the corrected behavior, keeping their subject (what a paused add
  does) and citing ISNEW-003; a new `Add_WhenPaused_DoesNotMarkModified` was added so the
  other half of the paused-add contract — identity yes, dirt no — stays pinned.
- **Why:** The defect they described is the one this plan exists to fix, recorded in the
  ISNEW-001 discovery and in design.md before implementation began.
- **Discovery Log link:** 2026-08-21 — ISNEW-003 (defect 1 reachability)
