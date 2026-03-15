# LazyLoad Value Propagation

**Status:** Complete
**Priority:** Medium
**Created:** 2026-03-14
**Last Updated:** 2026-03-14

---

## Problem

LazyLoad property subclasses (LazyLoadValidateProperty, LazyLoadEntityProperty) expose the `LazyLoad<T>` wrapper as their `Value`, forcing ValidateBase to have LazyLoad-aware look-through code in `_PropertyManager_NeatooPropertyChanged`. Framework code outside the LazyLoad subclasses should be unaware of the LazyLoad nature of the property — when not loaded it's null, when loaded it's the same as a property value being set.

Additionally, ValidateBase calls `RegisterLazyLoadProperties()` explicitly in FactoryComplete and OnDeserialized — these could be generic lifecycle hooks that PropertyManager handles without ValidateBase knowing about LazyLoad.

## Solution

1. Override `Value` getter on LazyLoad subclasses to return the inner entity (via BoxedValue), not the LazyLoad wrapper
2. Remove the SetParent look-through in `_PropertyManager_NeatooPropertyChanged`
3. Replace `RegisterLazyLoadProperties()` calls in FactoryComplete/OnDeserialized with a generic `PropertyManager.FinalizeRegistration()` lifecycle hook

Goal: ValidateBase reaches zero LazyLoad _logic_ — only thin pass-through registration methods remain (which reference the `LazyLoad<T>` type but contain no logic).

---

## Clarifications

---

## Plans

- [LazyLoad Value Propagation](../plans/lazyload-value-propagation.md)

---

## Tasks

- [x] Architect questions (Step 2) -- Ready (analysis done in prior conversation)
- [x] Architect plan (Step 3) -- Plan created
- [x] Developer review (Step 4) -- Approved with implementation contract
- [x] Implementation (Step 5) -- Complete, 2111 tests pass
- [x] Architect verification (Step 6) -- VERIFIED

---

## Progress Log

### 2026-03-14
- Created from discussion about eliminating LazyLoad awareness from ValidateBase
- Architect already analyzed: Value change works, serialization not affected, all consumers traced
- Plan created at `docs/plans/lazyload-value-propagation.md`
- Developer review: first pass raised 4 concerns (Value getter-only, BR13 accuracy, OnDeserialized undocumented, import cleanup step)
- Architect updated plan to address all concerns (Value now has getter+setter via new LazyLoad.SetValue, BR13 corrected, OnDeserialized documented, step removed)
- Developer re-review: all concerns resolved, plan approved, implementation contract created

---

## Results / Conclusions

LazyLoad property subclasses now propagate the inner entity as their Value — framework code outside the subclasses is unaware of the LazyLoad nature. Added `LazyLoad<T>.SetValue(T?)` enabling direct value assignment that bypasses the loader. ValidateBase has zero LazyLoad-specific logic: SetParent look-through eliminated, FactoryComplete/OnDeserialized use generic FinalizeRegistration hook. Only thin pass-through registration methods remain. All 2111 tests pass.
