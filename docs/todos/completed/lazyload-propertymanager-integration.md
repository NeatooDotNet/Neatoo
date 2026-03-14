# LazyLoad PropertyManager Integration

**Status:** Complete
**Priority:** High
**Created:** 2026-03-13
**Last Updated:** 2026-03-14

---

## Problem

LazyLoad<T> properties exist outside the PropertyManager system. This creates two parallel property systems with duplicated patterns:

1. **PropertyManager** — manages `partial` properties via `Property<T>` backing fields. Has `WaitForTasks()`, `IsBusy`, `IsValid`, subscriptions.
2. **LazyLoad<T>** — standalone wrapper, discovered via reflection (`GetLazyLoadProperties()`). Has its own `WaitForTasks()`, `IsBusy`, `IsValid`, and parallel helper methods in ValidateBase: `IsAnyLazyLoadChildBusy()`, `IsAllLazyLoadChildrenValid()`, `WaitForLazyLoadChildren()`, `SubscribeToLazyLoadProperties()`.

Every meta property check in ValidateBase has to call both systems. For example, `IsBusy` calls `PropertyManager.IsBusy || IsAnyLazyLoadChildBusy()`. This is fragile — adding a new meta property means remembering to add the LazyLoad parallel check.

The split dates from when LazyLoad<T> replaced the old `Property<T>.OnLoad` system. LazyLoad was designed as a regular property (not a partial property) because it wraps a child entity, not a scalar value. But the consequence is it's invisible to PropertyManager.

## Solution

Register LazyLoad<T> instances with PropertyManager so there's one unified system for property-level state aggregation and task tracking. This eliminates the reflection-based discovery and the parallel helper methods in ValidateBase.

### Evidence from Person Example LazyLoad Conversion (2026-03-14)

Converting PersonPhoneList to LazyLoad exposed three concrete gaps:

1. **SetParent gap** (fixed in ValidateBase.cs, uncommitted) — LazyLoad values didn't get `SetParent()` called, breaking parent-child navigation. Fix: added SetParent calls in `SubscribeToLazyLoadProperties()` and `OnLazyLoadPropertyChanged()`.
2. **RunRules doesn't cascade** — `person.RunRules()` doesn't cascade to LazyLoad children. PropertyManager-managed properties cascade; LazyLoad properties don't.
3. **PropertyMessages doesn't aggregate** — `person.PropertyMessages` doesn't include messages from LazyLoad children.

Gaps 2 and 3 cause 3 Person integration test failures that are **intentionally left in place** to demonstrate the problem. The fix is this todo — unify the systems.

---

## Clarifications

---

## Requirements Review

**Reviewer:** neatoo-requirements-reviewer
**Reviewed:** 2026-03-14
**Verdict:** APPROVED (with constraints)

### Relevant Requirements Found

**From Design.Domain (authoritative reference):**

1. **DESIGN DECISION: LazyLoad is a regular property, not in PropertyManager** -- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` lines 8-12: "LazyLoad<T> is declared as a regular property because: It wraps a child entity/value, not a scalar property value; It has its own lifecycle (IsLoaded, IsLoading, LoadAsync); It implements IValidateMetaProperties and IEntityMetaProperties for delegation to the loaded value." This is the design decision the todo proposes to change.

2. **DESIGN DECISION: Generators do not process LazyLoad properties** -- Same file, lines 31-32: "The generators do NOT process LazyLoad<T> properties because they are not partial properties. No backing field is generated." Any PropertyManager registration approach must not require generator changes for LazyLoad.

3. **SERIALIZATION: LazyLoad has separate JSON path** -- Same file, lines 34-40: "NeatooBaseJsonTypeConverter detects LazyLoad<> properties via reflection and serializes them separately from PropertyManager entries." The serializer at `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` writes LazyLoad properties outside the PropertyManager array (line 386-399 in Write, line 196-213 in Read). Changing where LazyLoad lives will require matching serialization changes.

4. **STATE PROPAGATION: Parent includes LazyLoad in meta calculations** -- Same file, lines 43-47: "The parent entity includes LazyLoad children in its IsModified, IsValid, and IsBusy calculations via cached reflection." The current architecture achieves correct polling results but misses RunRules cascading and PropertyMessages aggregation.

5. **SUBSCRIPTION LIFECYCLE: SubscribeToLazyLoadProperties at FactoryComplete and OnDeserialized** -- Same file, lines 49-51. ValidateBase.cs confirms this at line 702 (OnDeserialized) and line 1127 (FactoryComplete).

**From completed todo (past decision):**

6. **Explicit decision to keep LazyLoad outside PropertyManager** -- `docs/todos/completed/lazyload-state-propagation.md` Results section: "LazyLoad<T> stays outside PropertyManager -- the documented design decision is respected; propagation works alongside PropertyManager, not through it." This was a deliberate architectural decision made 2026-03-07 after evaluating 7 approaches. The current todo proposes reversing this decision.

**From framework source (behavioral contracts):**

7. **ValidateProperty.RunRules cascades to child IValidateBase** -- `src/Neatoo/Internal/ValidateProperty.cs` line 370: `RunRules` delegates to `ValueIsValidateBase?.RunRules()`. When a partial property holds a child Neatoo object, calling `PropertyManager.RunRules()` cascades to that child. LazyLoad children miss this because they are not in PropertyManager. This is the root cause of the RunRules gap described in the todo.

8. **ValidateProperty.PropertyMessages delegates to child IValidateBase** -- Same file, lines 373-375: PropertyMessages returns `ValueIsValidateBase.PropertyMessages` when the value is an IValidateBase. LazyLoad children miss this aggregation. This is the root cause of the PropertyMessages gap.

9. **ValidateProperty.IsValid delegates to child IValidateBase** -- Same file, line 368: `IsValid => ValueIsValidateBase != null ? ValueIsValidateBase.IsValid : RuleMessages.Count == 0`. If LazyLoad were registered, its IsValid would cascade automatically instead of requiring the separate `IsAllLazyLoadChildrenValid()` polling method.

10. **PropertyManager.WaitForTasks iterates PropertyBag** -- `src/Neatoo/Internal/ValidatePropertyManager.cs` lines 62-72. If LazyLoad were in PropertyBag, its WaitForTasks would be called automatically, eliminating the `WaitForLazyLoadChildren()` parallel method.

11. **IValidateProperty interface contract** -- `src/Neatoo/IValidateProperty.cs`: Properties in PropertyManager must implement IValidateProperty, which requires Name, Value, SetValue, Task, IsBusy, IsReadOnly, AddMarkedBusy, RemoveMarkedBusy, LoadValue, WaitForTasks, Type, IsSelfValid, IsValid, RunRules, and PropertyMessages. LazyLoad<T> does NOT implement this interface. Registration would require either (a) LazyLoad implementing IValidateProperty, (b) an adapter/wrapper that bridges LazyLoad to IValidateProperty, or (c) changing PropertyManager to accept a different contract.

12. **EntityProperty requires IEntityProperty** -- `src/Neatoo/Internal/EntityPropertyManager.cs` line 88: EntityPropertyManager manages `IEntityProperty` instances, not IValidateProperty. For EntityBase entities with LazyLoad, the property manager expects IEntityProperty (which adds IsModified, IsSelfModified, IsPaused, DisplayName). This is an additional interface gap.

**From Design.Tests (behavioral contracts expressed as tests):**

13. **WHEN entity.WaitForTasks() completes, THEN IsBusy is false** -- `src/Design/Design.Tests/BaseClassTests/ValidateBaseTests.cs` method `WaitForTasks_CompletesWhenNotBusy`. Any change must preserve this contract.

14. **WHEN property changes on child, THEN parent rule cascading fires** -- `src/Design/Design.Tests/AggregateTests/OrderAggregateTests.cs` method `ChildItemLineTotalChange_RecalculatesOrderTotalAmount`. This demonstrates that child NeatooPropertyChanged events trigger parent rules via `ChildNeatooPropertyChanged`. LazyLoad children currently bypass this path because they are not in PropertyManager and do not fire NeatooPropertyChanged.

15. **WHEN RunRules(All) is called, THEN messages cascade to children** -- Implicit in the PropertyManager.RunRules flow (line 265-271 of ValidatePropertyManager.cs) which iterates PropertyBag and calls RunRules on each property, which delegates to ValueIsValidateBase.RunRules. No Design.Test covers this for LazyLoad specifically.

**From skill files:**

16. **LazyLoad property declaration pattern with SubscribeToLazyLoadProperties** -- `skills/neatoo/references/lazy-loading.md`: Documents the private setter + SubscribeToLazyLoadProperties() pattern. Any unification must preserve or simplify this pattern for consumers.

17. **Common mistake: assigning LazyLoad after FactoryComplete without SubscribeToLazyLoadProperties** -- `skills/neatoo/references/pitfalls.md` and Design.Domain LazyLoadProperty.cs lines 57-61. This gotcha exists because of the parallel system; if LazyLoad were in PropertyManager, the subscription might be handled automatically.

**From Person example (evidence of the problem):**

18. **PersonIntegrationTests demonstrate the gaps** -- `src/Examples/Person/Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs`: Tests like `PersonTests_End_To_End` (line 83) call `await person.RunRules()` and then check `person.IsValid` -- these rely on RunRules cascading to the phone list inside LazyLoad. `UniquePhoneTypeRule_ShouldReturnError_WhenPhoneTypeIsNotUnique` (line 270) checks `person.PropertyMessages` which must aggregate messages from the LazyLoad phone list.

### Gaps

1. **No Design.Tests for LazyLoad behavior** -- Design.Tests has zero tests for LazyLoad<T>. No test covers RunRules cascading, PropertyMessages aggregation, IsBusy propagation, WaitForTasks, or serialization round-trip for LazyLoad properties. The architect should add LazyLoad tests to Design.Tests as part of this work.

2. **No documented requirement for how PropertyManager.Register should handle non-partial-property registrations** -- Register() is currently called only by generated InitializePropertyBackingFields code. Registering LazyLoad instances outside generated code is a new usage pattern with no existing specification.

3. **No requirement for how NeatooPropertyChanged should propagate from LazyLoad children** -- ValidateBase's `_PropertyManager_NeatooPropertyChanged` handler triggers rule cascading and SetParent. LazyLoad children currently bypass this entirely. The unification must define how LazyLoad child events enter the NeatooPropertyChanged pathway (or provide an equivalent).

4. **No requirement for ClearAllMessages/ClearSelfMessages on LazyLoad children** -- ValidateBase.ClearAllMessages() calls `PropertyManager.ClearAllMessages()`, which iterates PropertyBag. LazyLoad children are not cleared. If the unification adds them to PropertyBag, this would be fixed automatically. If not, it remains a gap.

### Contradictions

**No blocking contradictions found.** The todo proposes reversing a prior design decision (requirement 1 and requirement 6 above), but that decision was made when the scope of the problem was smaller (only IsModified/IsBusy/IsValid polling). The Person example has since exposed that the parallel system also fails for RunRules cascading, PropertyMessages aggregation, ClearAllMessages, and NeatooPropertyChanged event propagation. The user's explicit directive ("We can't have two property approaches") overrides the prior design decision.

The prior design decision was sound given the information at the time. It is being revisited because accumulating patches to the parallel system have reached a tipping point where unification is less complex than continued patching.

### Recommendations for Architect

1. **IValidateProperty interface gap is the core design challenge.** LazyLoad<T> does not implement IValidateProperty. The PropertyManager's PropertyBag is `IDictionary<string, P> where P : IValidateProperty`. The architect must decide between: (a) making LazyLoad implement IValidateProperty (requires stub implementations for many members like AddMarkedBusy, SetValue, LoadValue, IsReadOnly, Type, NeatooPropertyChanged), (b) creating an adapter/wrapper class that bridges LazyLoad<T> to IValidateProperty, or (c) changing PropertyManager to accept a broader interface. Option (b) is likely cleanest -- an adapter can delegate RunRules, PropertyMessages, IsValid, IsBusy, WaitForTasks to the LazyLoad instance while stubbing inapplicable members.

2. **EntityProperty needs IEntityProperty for EntityBase.** For EntityBase entities, the PropertyManager is an EntityPropertyManager that manages IEntityProperty instances. LazyLoad already implements IEntityMetaProperties, so an adapter would need to also implement IsModified, IsSelfModified, IsPaused, etc. This is a separate interface from IValidateProperty.

3. **Serialization must change in sync.** The NeatooBaseJsonTypeConverter reads and writes LazyLoad properties separately from the PropertyManager array. If LazyLoad is registered with PropertyManager, the serializer must either: (a) continue the separate LazyLoad path (detecting LazyLoad-backed properties in the PropertyManager array and serializing them differently), or (b) serialize them as part of the PropertyManager array with a special marker. The adapter approach (recommendation 1b) makes this easier since the adapter can be serialized like any other property.

4. **Source generator must NOT need changes.** The generators detect partial properties. LazyLoad properties are regular properties and must remain so. Registration should happen at runtime (in FactoryComplete/OnDeserialized or constructor), not via generated code.

5. **Verify against Design.Domain/PropertySystem/LazyLoadProperty.cs after implementation.** The DESIGN DECISION comments in that file must be updated to reflect the new architecture. The current comments explicitly say "LazyLoad is NOT managed by PropertyManager."

6. **Verify against the NeatooPropertyChanged propagation pathway.** Currently ValidateBase._PropertyManager_NeatooPropertyChanged handles SetParent and rule cascading for PropertyManager properties. LazyLoad children trigger OnLazyLoadPropertyChanged which only does SetParent and CheckIfMetaPropertiesChanged. Unification should route LazyLoad child events through the same NeatooPropertyChanged pathway for consistent rule cascading.

7. **Add Design.Tests for LazyLoad.** The lack of Design.Tests for LazyLoad is a gap that predates this todo. The architect should add tests covering: RunRules cascading, PropertyMessages aggregation, IsBusy propagation, WaitForTasks, and IsValid cascading for LazyLoad children.

8. **Preserve the "no reflection" goal.** The current parallel system uses cached-per-type reflection (GetLazyLoadProperties). The unification should ideally eliminate this reflection, which would align with the CLAUDE.md directive to minimize reflection. If registration is explicit (e.g., in the constructor or a protected method), reflection discovery can be removed.

9. **The 3 intentionally failing Person tests are the acceptance criteria.** `PersonTests_End_To_End`, `UniquePhoneTypeRule_ShouldReturnError_WhenPhoneTypeIsNotUnique`, and `UniquePhoneNumberRule_ShouldReturnError_WhenPhoneNumberIsNotUnique` must pass after the unification without modification to those test files.

---

## Plans

- [LazyLoad PropertyManager Unification](../plans/lazyload-propertymanager-unification.md)

---

## Tasks

- [x] Architect comprehension check
- [x] Architect explores whether PropertyManager can host LazyLoad instances
- [x] Evaluate impact on source generator (partial property detection)
- [x] Evaluate impact on serialization (NeatooBaseJsonTypeConverter LazyLoad handling)
- [x] Business requirements review (APPROVED with constraints)
- [x] Architect plan creation and design
- [x] Developer review (Approved)
- [x] Implementation (Phases 1-4 complete)
- [x] Architect verification (VERIFIED)
- [x] Requirements verification (REQUIREMENTS SATISFIED)

---

## Progress Log

### 2026-03-14
- Plan updated: Replaced adapter approach with look-through subclass approach (user approved)
- New design: LazyLoadValidateProperty<T> extends ValidateProperty<LazyLoad<T>>, LazyLoadEntityProperty<T> extends EntityProperty<LazyLoad<T>>
- Subclasses override 5-6 virtual methods to look through LazyLoad wrapper to inner entity, reusing existing child-linking infrastructure
- Rationale: 5-6 method overrides via inheritance vs 20+ stub interface implementations in adapter; maintenance inherits for free
- Architect plan created: [LazyLoad PropertyManager Unification](../plans/lazyload-propertymanager-unification.md)
- Design: LazyLoadPropertyAdapter<T> bridges LazyLoad into PropertyManager via IEntityProperty/IValidateProperty (superseded)
- 18 business rules extracted as testable assertions, 14 test scenarios defined
- Source generator confirmed unchanged, serialization wire format preserved
- Priority raised to High after Person example LazyLoad conversion exposed 3 concrete gaps
- SetParent gap fixed in ValidateBase.cs (uncommitted) — LazyLoad values now get SetParent called
- RunRules cascading and PropertyMessages aggregation gaps left as failing tests (3 Person integration tests)
- User decision: "We can't have two property approaches" — unification required, not more patches

### 2026-03-13
- Created from observation that ValidateBase has parallel property systems
- Stale comment on `PropertyManager.WaitForTasks()` line exposed the architectural split

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors, 0 warnings
- Tests: 2111 passed, 0 failed, 1 skipped (pre-existing)

---

## Results / Conclusions

LazyLoad<T> instances now participate in PropertyManager via look-through property subclasses (LazyLoadValidateProperty<T>, LazyLoadEntityProperty<T>). The parallel helper methods in ValidateBase and EntityBase have been eliminated. All 3 previously-failing Person integration tests pass.

**Approach:** Specialized property subclasses that override virtual methods (ValueIsValidateBase, HandleNonNullValue, PassThruValuePropertyChanged, etc.) to look through the LazyLoad wrapper to the inner entity's .Value. LazyLoad itself is invisible outside PropertyManager.

**Breaking change:** Protected method `SubscribeToLazyLoadProperties()` renamed to `RegisterLazyLoadProperties()`.

**Deferred:** Phase 5 (Design.Tests for LazyLoad) — blocked by pre-existing NF0105 errors in Design.sln. Should be a separate follow-up.

**Pre-existing gap noted:** `ValidateBase.WaitForTasks(CancellationToken)` does not call `PropertyManager.WaitForTasks()`. This predates this work and is documented for future attention.
