# Move LazyLoad Registration to PropertyManager

**Status:** Complete
**Priority:** Medium
**Created:** 2026-03-14
**Last Updated:** 2026-03-14

---

## Problem

`ValidateBase.RegisterLazyLoadProperties()` contains ~100 lines of property management logic (reflection discovery, `MakeGenericType`, `Activator.CreateInstance`, PropertyBag lookups, reassignment detection) that belongs in `ValidatePropertyManager`/`EntityPropertyManager`. ValidateBase should just call `PropertyManager.RegisterLazyLoad(...)` and let the property manager handle creation and registration internally.

This was introduced in the LazyLoad PropertyManager unification (completed todo). The registration logic works correctly but lives in the wrong layer.

**Key files:**
- `src/Neatoo/ValidateBase.cs` — lines 305-450, the `#region LazyLoad PropertyManager Registration` block
- `src/Neatoo/Internal/ValidatePropertyManager.cs` — where the logic should move

## Solution

Move `RegisterLazyLoadProperties()` and `RegisterLazyLoadProperty<T>()` internals into ValidatePropertyManager (and EntityPropertyManager for the entity variant). ValidateBase keeps thin protected methods that delegate to PropertyManager.

---

## Clarifications

---

## Plans

- [Move LazyLoad Registration to PropertyManager](../plans/move-lazyload-registration.md)

---

## Tasks

- [x] Architect questions (Step 2) -- Ready, no questions
- [x] Architect plan (Step 3) -- Plan created
- [x] Developer review (Step 4) -- Approved
- [x] Implementation (Step 5) -- Complete, 2111 tests pass
- [x] Architect verification (Step 6) -- VERIFIED

---

## Progress Log

### 2026-03-14
- Created from observation that LazyLoad registration logic belongs in PropertyManager, not ValidateBase
- Architect comprehension check: Ready, no questions
- Architect plan created at `docs/plans/move-lazyload-registration.md`
- Developer review: initial concern raised (cast mechanism), architect resolved, plan approved. Implementation contract created.

---

## Results / Conclusions

Pure internal refactoring completed. ~130 lines of LazyLoad registration logic moved from ValidateBase into ValidatePropertyManager/EntityPropertyManager. ValidateBase methods are now 3-line delegations. Polymorphic `CreateLazyLoadProperty` virtual/override replaces the `is IEntityPropertyManager` type check. All 2111 tests pass unchanged.
