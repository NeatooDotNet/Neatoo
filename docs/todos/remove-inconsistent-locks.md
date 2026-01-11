# Remove Inconsistent Threading Locks

Remove unnecessary/inconsistent locks based on Neatoo's single-async-flow threading model.

**Created:** 2026-01-10
**Status:** Not Started
**Origin:** code-smells-review.md

---

## Background

Neatoo assumes single async flow per entity instance. The current locks are inconsistent - some properties use locks while related properties don't.

**Files:**
- `src/Neatoo/Internal/ValidateProperty.cs:58-67` - `_isMarkedBusyLock`
- `src/Neatoo/Internal/ValidatePropertyManager.cs:43` - `_propertyBagLock`

**Issue:** `IsBusy` uses `_isMarkedBusyLock` but `IsSelfBusy` is read/written without synchronization. The lock is inconsistent and unnecessary given the threading model.

## Tasks

- [ ] Remove `_isMarkedBusyLock` from ValidateProperty
- [ ] Analyze if `_propertyBagLock` in ValidatePropertyManager is also unnecessary
- [ ] If unnecessary, remove it too
- [ ] Run all tests to verify no regressions

**Note:** Should be done alongside or after documenting the threading model (see advanced-documentation.md)
