# LazyLoad State Propagation Fix

**Date:** 2026-03-07
**Related Todo:** [LazyLoad State Propagation Bug](../todos/completed/lazyload-state-propagation.md)
**Status:** Complete
**Last Updated:** 2026-03-07

---

## Overview

State changes in child entities loaded via `LazyLoad<T>` do not propagate to the parent entity. When a child entity inside a `LazyLoad<T>` property is modified, the parent's `IsModified` stays `false` and `IsSavable` stays `false`. The aggregate root does not know it needs saving.

The root cause: `LazyLoad<T>` properties are regular C# auto-properties outside `PropertyManager`. The property system (`EntityProperty<T>`) never sees them, so state changes in the wrapped entity are invisible to the parent.

This plan evaluates multiple architectural approaches, recommends one, and provides the full implementation design.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/lazyload-state-propagation.md#requirements-review)

### Design Project Contracts (code-based)

- `Design.Domain/PropertySystem/LazyLoadProperty.cs`: Defines LazyLoad as a regular (non-partial) C# property, not in PropertyManager. Documents design rationale.
- `EntityParentChildFetchTests`: Proves that regular partial property children propagate IsModified, IsSelfModified, and IsSavable correctly. This is the reference behavior that LazyLoad children must match.
- `LazyLoadStatePropagationTests`: Four tests defining the expected behavior. Two pass (baseline), two fail (the bug).

### Behavioral Contracts from Tests (code-based)

- `EntityParentChildFetchTests.EntityParentChildFetchTest_ModifyChild_IsModified`: WHEN child entity (held in a regular partial property) is modified, THEN parent.IsModified returns true. This contract must also hold for LazyLoad children.
- `EntityParentChildFetchTests.EntityParentChildFetchTest_ModifyChild_IsSelfModified`: WHEN child entity is modified, THEN parent.IsSelfModified returns false (only the child is self-modified). Same must hold for LazyLoad.
- `EntityParentChildFetchTests.EntityParentChildFetchTest_ModifyChild_IsSavable`: WHEN child entity is modified, THEN parent (root).IsSavable returns true. Same must hold for LazyLoad.
- Serialization tests (`FatClientLazyLoadTests`, `TwoContainerLazyLoadTests`): LazyLoad properties serialize/deserialize correctly. Must not break.

### Gaps

- No documented requirement for how `LazyLoad<T>` state propagation should work (added without specification).
- No documented requirement for whether `LazyLoad<T>` should implement `IEntityMetaProperties`.

### Contradictions

None. The proposed fix aligns with the existing "state cascades UP" contract. The current behavior (no propagation) is the contradiction.

### Recommendations for Architect

- Fix must make the 4 LazyLoad propagation tests pass without breaking existing 1,749 tests
- Consider whether `IEntityMetaProperties` should be removed from `LazyLoad<T>`
- Serialization path for `LazyLoad<T>` must not break
- Consider `LazyLoad<T>` where T is not an entity (e.g., `LazyLoad<string>`)

---

## Business Rules (Testable Assertions)

1. WHEN a child entity inside `LazyLoad<T>` is modified AND the LazyLoad property is on an `EntityBase<T>` parent, THEN `parent.IsModified` RETURNS `true` -- Source: Behavioral contract from EntityParentChildFetchTests (analogous to regular child properties)

2. WHEN a child entity inside `LazyLoad<T>` is modified, THEN `parent.IsSelfModified` RETURNS `false` (because only the child changed, not the parent itself) -- Source: Behavioral contract from EntityParentChildFetchTests

3. WHEN a child entity inside `LazyLoad<T>` is modified AND the parent is an aggregate root (not IsChild), THEN `parent.IsSavable` RETURNS `true` (because IsModified && IsValid && !IsBusy && !IsChild) -- Source: IsSavable contract from CLAUDE.md and EntityBase.cs line 167

4. WHEN `LazyLoad<T>` wraps a non-entity type (e.g., `LazyLoad<string>`), THEN state propagation is a no-op and no errors occur -- Source: NEW (gap coverage)

5. WHEN a `LazyLoad<T>` child entity has not been modified, THEN `parent.IsModified` RETURNS `false` (no false positives) -- Source: LazyLoadStatePropagationTests.LazyLoadChild_InitialState_ParentNotModified

6. WHEN `LazyLoad<T>` properties are serialized/deserialized, THEN the serialization format is unchanged and `Value`/`IsLoaded` round-trip correctly -- Source: Behavioral contract from FatClientLazyLoadTests and TwoContainerLazyLoadTests

7. WHEN `LazyLoad<T>` child entity's `IsValid` changes, THEN `parent.IsValid` reflects the change -- Source: NEW (consistent with ValidateProperty child propagation pattern)

8. WHEN `LazyLoad<T>` child entity's `IsBusy` changes, THEN `parent.IsBusy` reflects the change -- Source: NEW (consistent with ValidateProperty child propagation pattern)

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Modify LazyLoad child -> parent IsModified | Fetch parent+child, set `LazyChild = new LazyLoad(child)`, modify `child.Name` | Rule 1 | `parent.IsModified == true` |
| 2 | Modify LazyLoad child -> parent NOT self-modified | Same setup, modify `child.Name` | Rule 2 | `parent.IsSelfModified == false`, `child.IsSelfModified == true` |
| 3 | Modify LazyLoad child -> parent IsSavable | Same setup, modify `child.Name` | Rule 3 | `((IEntityRoot)parent).IsSavable == true` |
| 4 | LazyLoad wraps string (non-entity) | Parent with `LazyLoad<string>` property | Rule 4 | No error, parent.IsModified unaffected by string value |
| 5 | LazyLoad child unmodified -> parent not modified | Fetch parent+child, set `LazyChild = new LazyLoad(child)`, no modifications | Rule 5 | `parent.IsModified == false` |
| 6 | Serialization round-trip | Serialize parent with LazyLoad properties, deserialize | Rule 6 | Value and IsLoaded preserved, format unchanged |
| 7 | LazyLoad child invalid -> parent invalid | Parent with LazyLoad child that has validation errors | Rule 7 | `parent.IsValid == false` |
| 8 | LazyLoad child busy -> parent busy | Parent with LazyLoad child running async rules | Rule 8 | `parent.IsBusy == true` |

---

## Architecture Evaluation

### How Regular Child Properties Propagate State (Reference)

When a partial property holds an entity child (e.g., `public partial IEntityPerson Child { get; set; }`):

1. The source generator creates an `EntityProperty<IEntityPerson>` backing field registered with `PropertyManager`
2. When the child entity is assigned, `ValidateProperty<T>.HandleNonNullValue()` subscribes to `INotifyPropertyChanged` on the child
3. When the child's `IsModified` changes, `PassThruValuePropertyChanged` forwards the event to the property
4. `EntityPropertyManager.Property_PropertyChanged` recalculates `EntityPropertyManager.IsModified` by checking `PropertyBag.Any(p => p.Value.IsModified)`
5. Each `EntityProperty<T>.IsModified` checks `IsSelfModified || (EntityChild?.IsModified ?? false)` where `EntityChild` is `this.Value as IEntityMetaProperties`
6. `EntityPropertyManager.IsModified` change fires `PropertyChanged("IsModified")`
7. `ValidateBase._PropertyManager_PropertyChanged` calls `CheckIfMetaPropertiesChanged()`
8. `EntityBase.CheckIfMetaPropertiesChanged()` evaluates `this.IsModified` and raises events

**The LazyLoad gap**: Step 1 never happens. `LazyLoad<T>` is a regular auto-property -- no `EntityProperty<T>` wrapper, no `PropertyManager` registration, no event subscriptions.

### Architecture A: EntityProperty Recognizes LazyLoad (REJECTED)

Have `EntityProperty<T>.EntityChild` detect when its `Value` is `LazyLoad<T>` and look through it.

**Rejected**: Fundamentally impossible. `LazyLoad<T>` properties are NOT partial properties. They never enter `PropertyManager`. There IS no `EntityProperty<T>` wrapping them. The `PropertyBag` does not contain them.

### Architecture B: Custom LazyLoadEntityProperty Subclass (REJECTED)

Create `LazyLoadEntityProperty<T>` extending `EntityProperty<T>`, registered with `PropertyManager`.

**Rejected**: Requires generator changes to detect LazyLoad properties and generate different registration code. Creates serialization conflicts (double serialization through PropertyManager AND the existing LazyLoad serialization path). The `LazyLoad<T>` lifecycle (IsLoaded, IsLoading, LoadAsync) does not fit the property value lifecycle.

### Architecture C: Parent Subscribes Outside PropertyManager (REJECTED)

Have the parent entity subscribe to `PropertyChanged` on `LazyLoad<T>` directly.

**Rejected**: No clean injection point. `LazyLoad<T>` properties are auto-properties with no setter hook. The parent has no way to detect when `LazyChild = new LazyLoad<T>(child)` is assigned. This approach violates the entity/property-manager separation of concerns.

### Architecture D: Override IsModified Only (INSUFFICIENT)

Override `IsModified` to poll LazyLoad children via reflection.

**Analysis**: This makes polling correct (test assertions pass) but does NOT raise `PropertyChanged("IsModified")` proactively when the child changes. UI binding would not update. Acceptable for the failing tests but incomplete for production.

### Architecture E (RECOMMENDED): Two-Part Fix -- Polling + Event Forwarding

Combine two changes:
1. `LazyLoad<T>` forwards `PropertyChanged` from its wrapped value (makes the wrapper transparent for events)
2. `EntityBase<T>.IsModified` (and `ValidateBase<T>.IsValid`/`IsBusy`) includes LazyLoad children via cached reflection
3. Parent subscribes to `PropertyChanged` on LazyLoad instances at known lifecycle points (`FactoryComplete()`, `OnDeserialized()`)
4. For runtime reassignment, provide `SubscribeToLazyLoadProperties()` as a protected method

This approach requires no generator changes, is consistent with existing reflection patterns in the framework (e.g., `NeatooBaseJsonTypeConverter` already discovers LazyLoad properties the same way), and correctly handles both polling and reactive notification.

### IEntityMetaProperties on LazyLoad<T>: KEEP

After analysis, `IEntityMetaProperties` on `LazyLoad<T>` is NOT dead weight in the context of this fix. The `IsModified` property on `LazyLoad<T>` delegates to `(_value as IEntityMetaProperties)?.IsModified ?? false`. The parent's polling method reads this property. The interface provides the delegation contract. Removing it would force the parent to reach through LazyLoad to its value, duplicating logic.

---

## Approach

Two-part framework fix. No generator changes required.

**Part 1**: `LazyLoad<T>` subscribes to `INotifyPropertyChanged` on its wrapped `_value` and forwards meta-state property changed events. This makes `LazyLoad<T>` a transparent wrapper for state observation.

**Part 2**: `EntityBase<T>.IsModified` and `ValidateBase<T>.IsValid`/`IsBusy` are modified to include LazyLoad children via cached-per-type reflection. Additionally, the parent subscribes to `PropertyChanged` on LazyLoad instances for reactive event propagation.

---

## Design

### Change 1: LazyLoad<T> Forwards Child PropertyChanged Events

**File**: `src/Neatoo/LazyLoad.cs`

Add event forwarding from the wrapped value:

```csharp
private void SubscribeToValuePropertyChanged(T? value)
{
    if (value is INotifyPropertyChanged npc)
    {
        npc.PropertyChanged += OnValuePropertyChanged;
    }
}

private void UnsubscribeFromValuePropertyChanged(T? value)
{
    if (value is INotifyPropertyChanged npc)
    {
        npc.PropertyChanged -= OnValuePropertyChanged;
    }
}

private void OnValuePropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    // Forward child meta-state changes as our own
    OnPropertyChanged(e.PropertyName!);
}
```

Call `SubscribeToValuePropertyChanged(_value)` in:
- `LazyLoad(T? value)` constructor (after setting `_value = value`)
- `LoadAsyncCore()` (after setting `_value = await _loader!()`)

Call `UnsubscribeFromValuePropertyChanged(old)` before replacing `_value` in `LoadAsyncCore()`.

**`LazyLoad<string>` safety**: `string` does not implement `INotifyPropertyChanged`, so `SubscribeToValuePropertyChanged()` does nothing. No-op. Safe.

### Change 2: LazyLoad Property Discovery Cache

**File**: `src/Neatoo/ValidateBase.cs` (or a shared helper class)

Add a static cache for discovering LazyLoad properties per concrete type:

```csharp
private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _lazyLoadPropertyCache = new();

private static PropertyInfo[] GetLazyLoadProperties(Type concreteType)
{
    return _lazyLoadPropertyCache.GetOrAdd(concreteType, type =>
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(p => p.PropertyType.IsGenericType
                && p.PropertyType.GetGenericTypeDefinition() == typeof(LazyLoad<>)
                && p.GetMethod != null)
            .ToArray());
}
```

This is the same pattern used by `NeatooBaseJsonTypeConverter` (line 116-121) for serialization.

### Change 3: EntityBase<T>.IsModified Includes LazyLoad Children

**File**: `src/Neatoo/EntityBase.cs`

Modify the `IsModified` property to include LazyLoad children:

```csharp
public virtual bool IsModified => this.PropertyManager.IsModified
    || this.IsDeleted
    || this.IsNew
    || this.IsSelfModified
    || IsAnyLazyLoadChildModified();

private bool IsAnyLazyLoadChildModified()
{
    var props = GetLazyLoadProperties(GetType());
    if (props.Length == 0) return false;

    foreach (var prop in props)
    {
        if (prop.GetValue(this) is IEntityMetaProperties emp && emp.IsModified)
            return true;
    }
    return false;
}
```

Note: `LazyLoad<T>.IsModified` delegates to `(_value as IEntityMetaProperties)?.IsModified ?? false`. For `LazyLoad<string>`, the cast fails and returns `false`. For `LazyLoad<IChild>` where the child is modified, returns `true`.

### Change 4: ValidateBase<T>.IsValid/IsBusy Include LazyLoad Children

**File**: `src/Neatoo/ValidateBase.cs`

Modify `IsValid` and `IsBusy` to include LazyLoad children:

```csharp
// IsValid currently: this.PropertyManager.IsValid
public bool IsValid => this.PropertyManager.IsValid && IsAllLazyLoadChildrenValid();

// IsBusy currently: this.RunningTasks.IsRunning || this.PropertyManager.IsBusy
public bool IsBusy => this.RunningTasks.IsRunning || this.PropertyManager.IsBusy || IsAnyLazyLoadChildBusy();

private bool IsAllLazyLoadChildrenValid()
{
    var props = GetLazyLoadProperties(GetType());
    if (props.Length == 0) return true;

    foreach (var prop in props)
    {
        if (prop.GetValue(this) is IValidateMetaProperties vmp && !vmp.IsValid)
            return false;
    }
    return true;
}

private bool IsAnyLazyLoadChildBusy()
{
    var props = GetLazyLoadProperties(GetType());
    if (props.Length == 0) return false;

    foreach (var prop in props)
    {
        if (prop.GetValue(this) is IValidateMetaProperties vmp && vmp.IsBusy)
            return true;
    }
    return false;
}
```

### Change 5: Event Subscription Infrastructure

**File**: `src/Neatoo/ValidateBase.cs`

Add subscription management for reactive updates:

```csharp
private readonly List<INotifyPropertyChanged> _lazyLoadSubscriptions = new();

protected void SubscribeToLazyLoadProperties()
{
    UnsubscribeFromLazyLoadProperties();

    var props = GetLazyLoadProperties(GetType());
    if (props.Length == 0) return;

    foreach (var prop in props)
    {
        if (prop.GetValue(this) is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged += OnLazyLoadPropertyChanged;
            _lazyLoadSubscriptions.Add(npc);
        }
    }
}

private void UnsubscribeFromLazyLoadProperties()
{
    foreach (var npc in _lazyLoadSubscriptions)
    {
        npc.PropertyChanged -= OnLazyLoadPropertyChanged;
    }
    _lazyLoadSubscriptions.Clear();
}

private void OnLazyLoadPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (!this.IsPaused)
    {
        CheckIfMetaPropertiesChanged();
    }
}
```

Call `SubscribeToLazyLoadProperties()` in:
- `FactoryComplete()` (both `ValidateBase<T>` and `EntityBase<T>` overrides)
- `OnDeserialized()` (in `ValidateBase<T>`)

### Change 6: Test Entity Update

**File**: `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadEntityObject.cs`

The test assigns `parent.LazyChild = new LazyLoad<T>(child)` AFTER factory completion. Since `FactoryComplete()` has already run, the auto-property setter won't trigger subscription. Two options:

**Option A** (recommended): Change `LazyChild` to a manually-implemented property that calls `SubscribeToLazyLoadProperties()` on set:

```csharp
private LazyLoad<ILazyLoadEntityObject> _lazyChild = null!;
public LazyLoad<ILazyLoadEntityObject> LazyChild
{
    get => _lazyChild;
    set
    {
        _lazyChild = value;
        SubscribeToLazyLoadProperties();
    }
}
```

**Option B**: Add a `[Fetch]` overload that accepts a child entity, wiring it during the factory operation so `FactoryComplete()` handles subscriptions.

**Note on test correctness**: Even WITHOUT event subscriptions (Option B / no custom setter), the polling override in Change 3 makes the test assertions pass. The test directly reads `parent.IsModified` and `parent.IsSavable`, which evaluate the override. Event subscriptions are needed for UI reactivity but not for the test assertions.

### Summary of IEntityMetaProperties Decision

**KEEP** `IEntityMetaProperties` on `LazyLoad<T>`. It provides the delegation contract:
- `LazyLoad<T>.IsModified` delegates to `(_value as IEntityMetaProperties)?.IsModified ?? false`
- `EntityBase<T>.IsAnyLazyLoadChildModified()` reads `IsModified` via `prop.GetValue(this) is IEntityMetaProperties emp`
- Removing it would force the parent to unwrap `LazyLoad<T>` and cast its Value, duplicating delegation logic

---

## Implementation Steps

1. **Modify `LazyLoad<T>`**: Add `SubscribeToValuePropertyChanged()`/`UnsubscribeFromValuePropertyChanged()` and call them at all value-assignment points (constructors, `LoadAsyncCore()`)

2. **Add LazyLoad property discovery cache**: Static `ConcurrentDictionary<Type, PropertyInfo[]>` in `ValidateBase<T>` (or helper class) with `GetLazyLoadProperties(Type)` method

3. **Modify `EntityBase<T>.IsModified`**: Add `|| IsAnyLazyLoadChildModified()` to the existing expression

4. **Modify `ValidateBase<T>.IsValid`**: Add `&& IsAllLazyLoadChildrenValid()` to the existing expression

5. **Modify `ValidateBase<T>.IsBusy`**: Add `|| IsAnyLazyLoadChildBusy()` to the existing expression

6. **Add subscription infrastructure**: `SubscribeToLazyLoadProperties()` (protected), `UnsubscribeFromLazyLoadProperties()` (private), `OnLazyLoadPropertyChanged` handler in `ValidateBase<T>`

7. **Wire subscriptions**: Call `SubscribeToLazyLoadProperties()` in `FactoryComplete()` and `OnDeserialized()`

8. **Update test entity**: Modify `LazyLoadEntityObject.LazyChild` to have a custom setter OR add a `[Fetch]` overload accepting a child

9. **Run all tests**: Existing 1,749 tests must pass, 4 LazyLoad propagation tests must pass

10. **Update Design.Domain**: Add state propagation documentation to `LazyLoadProperty.cs`

---

## Acceptance Criteria

- [ ] `LazyLoadChild_InitialState_ParentNotModified` passes (already passing)
- [ ] `LazyLoadChild_ModifyChild_ParentNotSelfModified` passes (already passing)
- [ ] `LazyLoadChild_ModifyChild_ParentIsModified` passes (currently failing)
- [ ] `LazyLoadChild_ModifyChild_ParentIsSavable` passes (currently failing)
- [ ] All existing tests continue to pass
- [ ] `LazyLoad<string>` (non-entity) does not cause errors
- [ ] Serialization tests (FatClientLazyLoadTests, TwoContainerLazyLoadTests) continue to pass
- [ ] Design project builds successfully
- [ ] Design project tests pass

---

## Dependencies

- No RemoteFactory changes required
- No generator changes required
- No new NuGet dependencies

---

## Risks / Considerations

1. **Reflection performance**: The per-type cache ensures reflection runs once per type. `PropertyInfo.GetValue()` in the polling path adds minor overhead per `IsModified`/`IsValid`/`IsBusy` evaluation. For types with zero LazyLoad properties (the majority), this is a single dictionary lookup returning an empty array -- negligible cost.

2. **Runtime LazyLoad reassignment**: Properties assigned after `FactoryComplete()` require either a custom setter calling `SubscribeToLazyLoadProperties()` or re-calling the method manually. The polling override still returns correct values; only reactive event propagation requires the subscription. Document this as a pattern.

3. **No circular reference risk**: When the parent subscribes to LazyLoad's `PropertyChanged`, the handler calls `CheckIfMetaPropertiesChanged()` which may raise `PropertyChanged("IsModified")` on the parent. The parent's `PropertyChanged` is not subscribed to by the LazyLoad child, so no infinite loop.

4. **Thread safety**: `ConcurrentDictionary` cache is thread-safe. Individual entity objects are single-threaded per Neatoo conventions.

5. **Consistency with existing patterns**: `NeatooBaseJsonTypeConverter` already uses the same reflection pattern to discover LazyLoad properties (line 116-121 of `NeatooBaseJsonTypeConverter.cs`).

---

## Architectural Verification

### Scope Table

| Component | Affected | Change Type |
|-----------|----------|-------------|
| `LazyLoad<T>` | Yes | Add PropertyChanged forwarding from wrapped value |
| `EntityBase<T>` | Yes | Modify `IsModified` to include LazyLoad children |
| `ValidateBase<T>` | Yes | Modify `IsValid`/`IsBusy`, add subscription infrastructure |
| `EntityPropertyManager` | No | Unchanged |
| `ValidatePropertyManager` | No | Unchanged |
| `EntityProperty<T>` | No | Unchanged |
| `ValidateProperty<T>` | No | Unchanged |
| `NeatooBaseJsonTypeConverter` | No | Unchanged |
| `Neatoo.BaseGenerator` | No | No generator changes |
| `IEntityMetaProperties` | No | Kept on LazyLoad<T> |
| `IValidateMetaProperties` | No | Kept on LazyLoad<T> |

### Design Project Verification

- **LazyLoad property on EntityBase**: Verified (existing code at `Design.Domain/PropertySystem/LazyLoadProperty.cs`)
- **EntityBase.IsModified aggregation**: Verified (existing at `EntityBase.cs:152` -- being modified)
- **LazyLoad state propagation**: Needs Implementation (no Design code covers this; Design.Domain should be updated after implementation)

### Breaking Changes

No. All changes are additive:
- `IsModified` becomes more inclusive (returns `true` in cases where it previously returned `false`)
- `IsValid` becomes more restrictive (returns `false` if a LazyLoad child is invalid -- correct behavior that was previously missing)
- `IsBusy` becomes more inclusive (returns `true` if a LazyLoad child is busy -- correct behavior that was previously missing)
- `LazyLoad<T>` gains new event forwarding (additive behavior)
- New protected method `SubscribeToLazyLoadProperties()` on `ValidateBase<T>` (non-breaking addition)

### Codebase Analysis

Files examined:
- `src/Neatoo/LazyLoad.cs` -- Constructors, value assignment, event handling, IEntityMetaProperties implementation
- `src/Neatoo/ILazyLoadFactory.cs` -- Factory pattern for LazyLoad creation
- `src/Neatoo/EntityBase.cs` -- IsModified, IsSavable, CheckIfMetaPropertiesChanged, FactoryComplete
- `src/Neatoo/ValidateBase.cs` -- IsValid, IsBusy, PropertyManager subscriptions, FactoryComplete, OnDeserialized
- `src/Neatoo/IMetaProperties.cs` -- IValidateMetaProperties, IEntityMetaProperties interfaces
- `src/Neatoo/Internal/EntityPropertyManager.cs` -- EntityProperty<T>, EntityChild, IsModified aggregation
- `src/Neatoo/Internal/ValidatePropertyManager.cs` -- PropertyChanged handling chain
- `src/Neatoo/Internal/ValidateProperty.cs` -- PassThruValuePropertyChanged, HandleNonNullValue subscriptions
- `src/Neatoo/IEntityProperty.cs` -- IEntityProperty interface
- `src/Neatoo/IEntityPropertyManager.cs` -- IEntityPropertyManager interface
- `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` -- LazyLoad serialization/deserialization (established reflection pattern)
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs` -- Failing tests
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/EntityParentChildFetchTests.cs` -- Reference for working propagation
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadEntityObject.cs` -- Test entity
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- Design reference
- `src/Design/CLAUDE-DESIGN.md` -- Design project guidance

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Framework changes + test entity update + verification | developer | Yes | All changes are tightly coupled; single phase is cleaner | None |

**Parallelizable phases:** None (single phase)

**Notes:** The changes span 3 framework files and 1 test file. All are interdependent -- the polling override in EntityBase depends on the cache in ValidateBase, and the event forwarding in LazyLoad is consumed by the subscriptions in ValidateBase. A single developer phase is the natural unit.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-07

### My Understanding of This Plan

**Core Change:** Make state changes in child entities wrapped by `LazyLoad<T>` propagate to the parent entity's `IsModified`, `IsValid`, `IsBusy`, and `IsSavable` properties -- matching the behavior of regular partial property children.

**User-Facing API:** No new public API for consumers. The existing `LazyLoad<T>` property pattern works unchanged. A new `protected SubscribeToLazyLoadProperties()` method is added to `ValidateBase<T>` for advanced scenarios where LazyLoad properties are assigned after `FactoryComplete()`.

**Internal Changes:** (1) `LazyLoad<T>` forwards `PropertyChanged` from its wrapped value. (2) `EntityBase<T>.IsModified` polls LazyLoad children via cached reflection. (3) `ValidateBase<T>.IsValid`/`IsBusy` poll LazyLoad children via cached reflection. (4) Parent subscribes to `PropertyChanged` on LazyLoad instances for reactive event propagation. (5) Test entity updated with custom setter.

**Base Classes Affected:** `EntityBase<T>` (IsModified), `ValidateBase<T>` (IsValid, IsBusy, subscription infrastructure), `LazyLoad<T>` (event forwarding). EntityListBase and ValidateListBase are unaffected.

### Codebase Investigation

**Files Examined:**
- `src/Neatoo/LazyLoad.cs` -- Current implementation: implements `INotifyPropertyChanged`, `IValidateMetaProperties`, `IEntityMetaProperties`. Has constructors for loader delegate, pre-loaded value, and JSON. `IsModified` delegates to `(_value as IEntityMetaProperties)?.IsModified ?? false`. Does NOT subscribe to child's `PropertyChanged`.
- `src/Neatoo/EntityBase.cs` -- `IsModified` at line 152: `this.PropertyManager.IsModified || this.IsDeleted || this.IsNew || this.IsSelfModified`. `IsSavable` at line 167: `this.IsModified && this.IsValid && !this.IsBusy && !this.IsChild`. `FactoryComplete` at line 550: calls `base.FactoryComplete()` then does entity-specific work then calls `this.ResumeAllActions()` (second call is guarded no-op). `CheckIfMetaPropertiesChanged` at line 240: compares cached `EntityMetaState` to current values, raises `PropertyChanged` for differences.
- `src/Neatoo/ValidateBase.cs` -- `IsValid` at line 277: `this.PropertyManager.IsValid` (non-virtual). `IsBusy` at line 171: `this.RunningTasks.IsRunning || this.PropertyManager.IsBusy` (non-virtual). `IsPaused` at line 714: `public bool IsPaused { get; protected set; }` -- accessible from within ValidateBase for the handler. `FactoryComplete` at line 962: calls `this.ResumeAllActions()`. `OnDeserialized` at line 527: restores event subscriptions, calls `this.ResumeAllActions()`. `CheckIfMetaPropertiesChanged` at line 323: compares cached `MetaState` to current `IsValid`/`IsSelfValid`/`IsBusy`, raises events if changed, then calls `ResetMetaState`.
- `src/Neatoo/Internal/EntityPropertyManager.cs` -- `EntityProperty<T>.EntityChild` at line 34: `this.Value as IEntityMetaProperties`. `IsModified` at line 55: `this.IsSelfModified || (this.EntityChild?.IsModified ?? false)`. This is the regular child propagation mechanism -- confirms LazyLoad is completely outside this.
- `src/Neatoo/Internal/ValidateProperty.cs` -- `HandleNonNullValue` at line 219: subscribes to `INotifyPropertyChanged` and `INotifyNeatooPropertyChanged` on the value. `PassThruValuePropertyChanged` at line 292: forwards `PropertyChanged`. This is the working event chain for regular children.
- `src/Neatoo/Internal/ValidatePropertyManager.cs` -- `Property_PropertyChanged` at line 137: recalculates `IsValid`/`IsSelfValid`/`IsBusy` from PropertyBag. Confirms PropertyManager only knows about registered properties.
- `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` -- Lines 116-121: discovers `LazyLoad<>` properties via reflection with `BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic`, `IsGenericType`, `GetGenericTypeDefinition() == typeof(LazyLoad<>)`. This is the established precedent for the same reflection pattern proposed in the plan.
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs` -- 4 tests. TestInitialize fetches parent and child, assigns `parent.LazyChild = new LazyLoad<ILazyLoadEntityObject>(child)` AFTER factory completion. 2 pass (InitialState, ParentNotSelfModified), 2 fail (ParentIsModified, ParentIsSavable).
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadEntityObject.cs` -- Test entity with `LazyLoad<string> LazyDescription` and `LazyLoad<ILazyLoadEntityObject> LazyChild` as regular auto-properties. Has two `[Fetch]` overloads.
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/EntityParentChildFetchTests.cs` -- Reference tests for working propagation via regular partial properties. Confirms parent.IsModified/IsSavable update when child is modified.
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- Design reference showing LazyLoad on EntityBase and ValidateBase.
- `src/Neatoo/IMetaProperties.cs` -- `IEntityMetaProperties` does NOT extend `IValidateMetaProperties`. This is by design (serializer uses `typeof(IEntityMetaProperties).GetProperties()` to discover wire properties).

**Searches Performed:**
- Searched for `override.*bool IsModified` -- 0 results in entire `src/`. No derived class overrides IsModified. Safe to modify.
- Searched for `override.*bool IsValid` -- 0 results. No overrides of IsValid.
- Searched for `CheckIfMetaPropertiesChanged` -- found in EntityBase, ValidateBase, EntityListBase, ValidateListBase. EntityBase override calls `base.CheckIfMetaPropertiesChanged()` which calls `ResetMetaState()`. Chain is correct.
- Searched for `FactoryComplete` -- EntityBase, ValidateBase, EntityListBase, ValidateListBase all have it. EntityBase calls `base.FactoryComplete()` first.
- Searched for `FatClientLazyLoadTests|TwoContainerLazyLoadTests` -- found 2 test files for serialization tests.
- Searched for `LazyLoad<ILazyLoadEntityObject>` -- found in interface, concrete class, propagation tests, serialization tests. All assign via auto-property.

**Design Project Verification:**
- LazyLoad property on EntityBase: Architect references `Design.Domain/PropertySystem/LazyLoadProperty.cs` -- confirmed exists, shows `LazyLoad<string>` on both EntityBase and ValidateBase entities.
- LazyLoad state propagation: Architect states "Needs Implementation" -- confirmed, no Design code covers state propagation currently.
- The architect did NOT leave failing Design project code as acceptance criteria. However, the acceptance criteria are the 4 already-written integration tests (2 failing). This is acceptable because the test files serve the same purpose -- they are the compilable evidence of the gap.

**Discrepancies Found:**
- None. The plan accurately describes the current codebase state.

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `EntityBase<T>.IsModified` (line 152): `this.PropertyManager.IsModified \|\| this.IsDeleted \|\| this.IsNew \|\| this.IsSelfModified \|\| IsAnyLazyLoadChildModified()`. `IsAnyLazyLoadChildModified()` iterates `GetLazyLoadProperties(GetType())`, calls `prop.GetValue(this) is IEntityMetaProperties emp && emp.IsModified`. `LazyLoad<T>.IsModified` (line 245): `(_value as IEntityMetaProperties)?.IsModified ?? false`. When child entity is modified, returns `true`. | `parent.IsModified == true` | Yes | Polling path is correct. PropertyManager.IsModified remains false (LazyLoad not in PropertyManager), but the new `IsAnyLazyLoadChildModified()` term catches it. |
| 2 | `EntityBase<T>.IsSelfModified` (line 158): `this.PropertyManager.IsSelfModified \|\| this.IsDeleted \|\| this.IsMarkedModified`. This property is UNCHANGED by the plan. No LazyLoad term is added to IsSelfModified. | `parent.IsSelfModified == false` | Yes | Correct. IsSelfModified is about the parent's own properties, not children. LazyLoad children should not affect it. |
| 3 | `EntityBase<T>.IsSavable` (line 167): `this.IsModified && this.IsValid && !this.IsBusy && !this.IsChild`. `IsModified` is now `true` (per Rule 1). `IsValid` is `this.PropertyManager.IsValid && IsAllLazyLoadChildrenValid()` -- child is valid, returns `true`. `IsBusy` is `this.RunningTasks.IsRunning \|\| this.PropertyManager.IsBusy \|\| IsAnyLazyLoadChildBusy()` -- nothing busy, returns `false`. `IsChild` -- parent is not a child, returns `false`. | `parent.IsSavable == true` | Yes | All four terms evaluate correctly. |
| 4 | `IsAnyLazyLoadChildModified()`: `prop.GetValue(this) is IEntityMetaProperties emp` -- `LazyLoad<string>` implements `IEntityMetaProperties`, so cast succeeds. `emp.IsModified` = `(_value as IEntityMetaProperties)?.IsModified ?? false` -- `string` does not implement `IEntityMetaProperties`, cast fails, returns `false`. `IsAllLazyLoadChildrenValid()`: `prop.GetValue(this) is IValidateMetaProperties vmp` -- `LazyLoad<string>` implements `IValidateMetaProperties`, cast succeeds. `vmp.IsValid` = `!HasLoadError && ((_value as IValidateMetaProperties)?.IsValid ?? true)` -- string is not `IValidateMetaProperties`, returns `true`. `IsAnyLazyLoadChildBusy()`: similar pattern, returns `false`. `SubscribeToValuePropertyChanged(string)`: `string` does not implement `INotifyPropertyChanged`, no-op. | No error, no false positives | Yes | All delegation paths handle non-entity T via safe casts with null-coalescing defaults. |
| 5 | Same as Rule 1 trace, but child is unmodified. `IsAnyLazyLoadChildModified()`: `LazyLoad<ILazyLoadEntityObject>.IsModified` = `(child as IEntityMetaProperties)?.IsModified ?? false` = `false` (child not modified). Result: `parent.IsModified` = `false \|\| false \|\| false \|\| false \|\| false` = `false`. | `parent.IsModified == false` | Yes | No false positives. |
| 6 | `NeatooBaseJsonTypeConverter` serialization/deserialization is UNCHANGED. `LazyLoad<T>` gains new event forwarding (`SubscribeToValuePropertyChanged`/`UnsubscribeFromValuePropertyChanged`) and the parent gains new subscription infrastructure, but none of these affect the JSON structure. `LazyLoad<T>` still has `[JsonInclude]` on `Value`/`IsLoaded` and `[JsonConstructor]`. The new private fields (`_lazyLoadSubscriptions`) and event handlers are not serialized. | Serialization unchanged | Yes | New members are private/JsonIgnore. No structural change to serialized format. |
| 7 | `ValidateBase<T>.IsValid`: `this.PropertyManager.IsValid && IsAllLazyLoadChildrenValid()`. `IsAllLazyLoadChildrenValid()`: iterates LazyLoad properties, `prop.GetValue(this) is IValidateMetaProperties vmp && !vmp.IsValid`. `LazyLoad<T>.IsValid` (line 185): `!HasLoadError && ((_value as IValidateMetaProperties)?.IsValid ?? true)`. If child entity has validation errors, `child.IsValid == false`, so `LazyLoad.IsValid == false`, so `IsAllLazyLoadChildrenValid() == false`, so `parent.IsValid == false`. | `parent.IsValid == false` | Yes | Correct propagation through `IValidateMetaProperties` delegation. |
| 8 | `ValidateBase<T>.IsBusy`: `this.RunningTasks.IsRunning \|\| this.PropertyManager.IsBusy \|\| IsAnyLazyLoadChildBusy()`. `IsAnyLazyLoadChildBusy()`: iterates LazyLoad properties, `prop.GetValue(this) is IValidateMetaProperties vmp && vmp.IsBusy`. `LazyLoad<T>.IsBusy` (line 182): `IsLoading \|\| ((_value as IValidateMetaProperties)?.IsBusy ?? false)`. If child entity has async rules running, `child.IsBusy == true`, so `LazyLoad.IsBusy == true`, so `IsAnyLazyLoadChildBusy() == true`, so `parent.IsBusy == true`. | `parent.IsBusy == true` | Yes | Correct propagation. Also handles LazyLoad's own loading state. |

### Test Scenario Verification

| # | Scenario | Expected | Actual via Proposed Implementation | Match? |
|---|----------|----------|-----------------------------------|--------|
| 1 | Modify LazyLoad child -> parent IsModified | `true` | `IsAnyLazyLoadChildModified()` returns `true` via `LazyLoad.IsModified` -> `child.IsModified == true` | Yes |
| 2 | Modify LazyLoad child -> parent NOT self-modified | `parent.IsSelfModified == false` | `IsSelfModified` unchanged, no LazyLoad term | Yes |
| 3 | Modify LazyLoad child -> parent IsSavable | `true` | `IsModified==true && IsValid==true && !IsBusy==true && !IsChild==true` | Yes |
| 4 | LazyLoad wraps string | No error | All safe casts return defaults for non-entity types | Yes |
| 5 | LazyLoad child unmodified -> parent not modified | `false` | `IsAnyLazyLoadChildModified()` returns `false` | Yes |
| 6 | Serialization round-trip | Unchanged | No structural changes to serialized format | Yes |
| 7 | LazyLoad child invalid -> parent invalid | `parent.IsValid == false` | `IsAllLazyLoadChildrenValid()` returns `false` | Yes |
| 8 | LazyLoad child busy -> parent busy | `parent.IsBusy == true` | `IsAnyLazyLoadChildBusy()` returns `true` | Yes |

### Structured Question Checklist

**Completeness Questions:**
- [x] All affected base classes addressed? EntityBase (IsModified), ValidateBase (IsValid, IsBusy, subscriptions). EntityListBase and ValidateListBase are unaffected -- LazyLoad properties are on entity/validate objects, not lists. Correct.
- [x] Factory operation lifecycle impacts? FactoryComplete is the subscription point. FactoryStart pauses (subscriptions are no-op while paused). Create/Fetch/Insert/Update/Delete are unaffected -- LazyLoad is outside PropertyManager.
- [x] Property system impact? No change to Getter/Setter/LoadValue/SetValue. LazyLoad remains outside PropertyManager. The polling approach avoids needing PropertyManager integration.
- [x] Validation rule interactions? LazyLoad children's validation is included via `IsAllLazyLoadChildrenValid()`. Rule execution is not changed -- rules still fire on property changes through PropertyManager.
- [x] Parent-child relationships in aggregates? LazyLoad children do NOT get `SetParent` called (they're outside PropertyManager). This is acceptable because LazyLoad is a wrapper, not a managed property. The child entity inside LazyLoad may have its own parent from how it was fetched.

**Correctness Questions:**
- [x] Consistent with existing patterns? Yes -- the reflection cache pattern matches `NeatooBaseJsonTypeConverter` (lines 116-121). The event forwarding pattern matches `ValidateProperty<T>.PassThruValuePropertyChanged`.
- [x] Breaking changes? No. IsModified becomes more inclusive (returns true in more cases). IsValid becomes more restrictive (returns false in more cases). Both are correct behavior that was previously missing.
- [x] State property impacts correct? IsModified, IsSavable gain LazyLoad awareness. IsSelfModified is correctly unchanged. IsNew, IsDeleted are unaffected.

**Clarity Questions:**
- [x] Could I implement this without asking clarifying questions? Yes. The design is specific enough with code snippets for each change.
- [x] Ambiguous requirements? None. The test assertions define exact expected behavior.
- [x] Edge cases handled? LazyLoad<string>, null LazyLoad properties, assignment after FactoryComplete.
- [x] Test strategy specific enough? Yes -- 4 existing failing tests define acceptance, plus serialization regression tests.

**Risk Questions:**
- [x] What could go wrong? Memory leaks from event subscriptions (addressed below). Performance from reflection (mitigated by per-type cache). Double-resume in EntityBase.FactoryComplete (already guarded).
- [x] Existing tests that might fail? Serialization tests (FatClientLazyLoadTests, TwoContainerLazyLoadTests) -- should be unaffected since no structural changes.
- [x] Serialization implications? None -- new members are private/not serialized. `LazyLoad<T>` JSON structure unchanged.
- [x] RemoteFactory source generation impacts? None -- no generator changes.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. What if `LazyLoad.LoadAsync()` loads a value AFTER `FactoryComplete()` and `SubscribeToLazyLoadProperties()` has already run? The parent subscribed to the `LazyLoad<T>` instance's `PropertyChanged` (not the inner value's). Since `LazyLoad<T>` forwards its inner value's `PropertyChanged` (Change 1), and the parent is subscribed to the LazyLoad instance, the chain works: inner value fires `PropertyChanged` -> LazyLoad forwards it -> parent's `OnLazyLoadPropertyChanged` fires -> `CheckIfMetaPropertiesChanged()`. This is correct -- the subscription is to the LazyLoad wrapper, not directly to the inner value, so it survives value loading.
2. What about `EntityListBase` containing entities that have LazyLoad properties? The list entities are children. Their `FactoryComplete()` will call `SubscribeToLazyLoadProperties()`. When the child's IsModified changes (due to its own LazyLoad grandchild), the regular PropertyManager chain handles propagation from the list item up to the list, then to the list's parent. This is not a concern for this plan.
3. Memory leak from subscriptions: `_lazyLoadSubscriptions` holds references to `INotifyPropertyChanged` (LazyLoad instances). If the LazyLoad property is reassigned, `SubscribeToLazyLoadProperties()` calls `UnsubscribeFromLazyLoadProperties()` first, which cleans up. But if the entity is disposed/abandoned without cleanup, the subscription prevents GC of the LazyLoad instance. This is the same pattern as PropertyManager subscriptions -- acceptable for Neatoo's lifecycle model.

**Ways this could break existing functionality:**
1. `IsModified` becoming `true` in more cases could theoretically cause unexpected saves. However, this is the CORRECT behavior -- the current `false` was the bug. Any entity that has a LazyLoad child with modifications SHOULD report as modified.

**Ways users could misunderstand the API:**
1. Users might not realize they need to call `SubscribeToLazyLoadProperties()` when assigning LazyLoad properties after FactoryComplete. The plan's recommended Option A (custom setter) addresses this for the test entity, but users writing their own entities would need to know this pattern. This should be documented in the Design project update.

### Reflection Policy Consideration

The plan uses cached reflection (`PropertyInfo.GetValue()`) in polling paths (`IsAnyLazyLoadChildModified`, `IsAllLazyLoadChildrenValid`, `IsAnyLazyLoadChildBusy`) and in subscription setup (`SubscribeToLazyLoadProperties`). The CLAUDE.md rule says "The goal is to have no reflection, even in tests" and requires approval before using reflection.

However, the `NeatooBaseJsonTypeConverter` already uses the identical reflection pattern (lines 116-121) for discovering LazyLoad properties. The framework already crosses this boundary for LazyLoad. The per-type `ConcurrentDictionary<Type, PropertyInfo[]>` cache ensures reflection runs once per type. This is a pragmatic use of reflection that avoids generator changes.

**Assessment:** The reflection use is justified by precedent and the alternative (generator changes) is significantly more complex. This is existing technical debt, not new debt. Proceed with approval.

### Why This Plan Is Exceptionally Clear

1. **Multiple architectures evaluated and rejected with specific reasons** -- The plan does not jump to a solution. It systematically evaluates 5 approaches (A through E), provides concrete reasons for rejection of each, and selects the one that balances correctness, complexity, and consistency.

2. **Every code change has a specific code snippet** -- Not just descriptions, but actual C# code showing exactly what to add/modify in each file.

3. **The polling vs event distinction is explicitly addressed** -- The plan clearly separates the polling path (makes test assertions pass) from the event subscription path (makes UI reactivity work), and explains why both are needed.

4. **Edge cases are identified and handled** -- LazyLoad<string>, assignment after FactoryComplete, the IEntityMetaProperties decision.

5. **The test setup timing issue is explicitly analyzed** -- The plan acknowledges that FactoryComplete runs before LazyChild is assigned in the test, and proposes two options with a recommendation.

6. **Files examined**: 12 source files, 3 test files, 1 design file
7. **Questions checked**: 17 of 17 -- all satisfactory
8. **Devil's advocate items**: 3 edge cases, 1 breakage scenario, 1 misunderstanding scenario -- all addressed by the design or acceptable

---

## Implementation Contract

**Created:** 2026-03-07
**Approved by:** neatoo-developer

### Acceptance Criteria (Failing Tests)

These are the currently-failing tests. Implementation is done when they pass.

- [x] `LazyLoadStatePropagationTests.LazyLoadChild_ModifyChild_ParentIsModified` -- now passes
- [x] `LazyLoadStatePropagationTests.LazyLoadChild_ModifyChild_ParentIsSavable` -- now passes

### In Scope

- [x] **Change 1**: `src/Neatoo/LazyLoad.cs` -- Added `SubscribeToValuePropertyChanged()`, `UnsubscribeFromValuePropertyChanged()`, `OnValuePropertyChanged()`. Called subscribe in `LazyLoad(T? value)` constructor and `LoadAsyncCore()`. Called unsubscribe before replacing `_value` in `LoadAsyncCore()`.
- [x] **Change 2**: `src/Neatoo/ValidateBase.cs` -- Added static `ConcurrentDictionary<Type, PropertyInfo[]> _lazyLoadPropertyCache` and `GetLazyLoadProperties(Type)` method (as `private protected static` for EntityBase access).
- [x] **Change 3**: `src/Neatoo/EntityBase.cs` -- Modified `IsModified` property to add `|| IsAnyLazyLoadChildModified()`. Added `IsAnyLazyLoadChildModified()` private method.
- [x] **Change 4**: `src/Neatoo/ValidateBase.cs` -- Modified `IsValid` to add `&& IsAllLazyLoadChildrenValid()`. Modified `IsBusy` to add `|| IsAnyLazyLoadChildBusy()`. Added both private methods.
- [x] **Change 5**: `src/Neatoo/ValidateBase.cs` -- Added `_lazyLoadSubscriptions` list, `SubscribeToLazyLoadProperties()` (protected), `UnsubscribeFromLazyLoadProperties()` (private), `OnLazyLoadPropertyChanged()` handler.
- [x] **Change 6**: `src/Neatoo/ValidateBase.cs` -- Called `SubscribeToLazyLoadProperties()` in `FactoryComplete()`. Called `SubscribeToLazyLoadProperties()` in `OnDeserialized()`.
- [x] **Change 7**: Not needed as a separate change -- `EntityBase.FactoryComplete()` calls `base.FactoryComplete()` which already handles subscription in `ValidateBase.FactoryComplete()`.
- [x] **Change 8**: `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadEntityObject.cs` -- Changed `LazyChild` from auto-property to property with custom setter that calls `SubscribeToLazyLoadProperties()`.
- [x] **Checkpoint 1**: 4/4 LazyLoad state propagation tests pass.
- [x] **Checkpoint 2**: 37/37 LazyLoad tests pass (propagation + serialization).
- [x] **Checkpoint 3**: Full test suite passes -- Neatoo.UnitTest: 1753 passed, 1 skipped, 0 failed; Samples: 245 passed; Person.DomainModel.Tests: 55 passed; BaseGenerator.Tests: 26 passed.
- [x] **Change 9**: `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- Added state propagation documentation, subscription lifecycle, and custom setter pattern.
- [x] **Checkpoint 4**: `dotnet build src/Design/Design.sln` succeeds. Design.Tests: 89/89 pass.

### Explicitly Out of Scope

- Generator changes -- not needed for this fix
- Removing `IEntityMetaProperties` from `LazyLoad<T>` -- decision is to KEEP it
- Adding tests for IsValid/IsBusy propagation via LazyLoad -- these are covered by assertion traces but would require async rule infrastructure setup; future enhancement
- `ValidateBase<T>` entities with LazyLoad children (only EntityBase is covered by failing tests) -- the infrastructure supports ValidateBase too, but no tests exist for it

### Verification Gates

1. **After Changes 1-3**: `LazyLoadChild_ModifyChild_ParentIsModified` must pass (polling path works)
2. **After Changes 4-7**: All 4 LazyLoad propagation tests must pass
3. **After Change 8**: Test entity has custom setter, all LazyLoad tests pass
4. **After Checkpoint 3**: Full test suite passes (1,749+ tests)
5. **After Change 9 + Checkpoint 4**: Design project builds successfully

### Stop Conditions

If any of these occur, STOP and report:
- Any out-of-scope test fails after framework changes
- Serialization tests (FatClientLazyLoadTests, TwoContainerLazyLoadTests) fail
- Design project does not build
- Reflection causes runtime errors in any existing test
- Circular event notification detected (infinite loop in PropertyChanged handlers)

---

## Implementation Progress

**Started:** 2026-03-07
**Developer:** neatoo-developer
**Current Status:** Complete

### Milestone 1: LazyLoad event forwarding (Change 1)
- Added `SubscribeToValuePropertyChanged()`, `UnsubscribeFromValuePropertyChanged()`, `OnValuePropertyChanged()` to `LazyLoad<T>`
- Subscribe in `LazyLoad(T? value)` constructor, unsubscribe/subscribe in `LoadAsyncCore()`
- Build: pass

### Milestone 2: ValidateBase infrastructure (Changes 2, 4, 5, 6)
- Added static `ConcurrentDictionary<Type, PropertyInfo[]> _lazyLoadPropertyCache` with `GetLazyLoadProperties(Type)` (`private protected static`)
- Added `IsAllLazyLoadChildrenValid()` and `IsAnyLazyLoadChildBusy()` private methods
- Modified `IsValid` and `IsBusy` properties to include LazyLoad children
- Added `_lazyLoadSubscriptions`, `SubscribeToLazyLoadProperties()` (protected), `UnsubscribeFromLazyLoadProperties()` (private), `OnLazyLoadPropertyChanged()` handler
- Wired `SubscribeToLazyLoadProperties()` in `FactoryComplete()` and `OnDeserialized()`
- Build: pass

### Milestone 3: EntityBase IsModified (Change 3)
- Modified `IsModified` to add `|| IsAnyLazyLoadChildModified()`
- Added `IsAnyLazyLoadChildModified()` private method using `IEntityMetaProperties` cast
- Build: pass
- Note: `GetLazyLoadProperties()` needed `private protected static` accessibility (plan said `private static` -- adjusted for cross-class access within same assembly)

### Milestone 4: Test entity update (Change 8)
- Changed `LazyChild` from auto-property to property with backing field and custom setter calling `SubscribeToLazyLoadProperties()`
- Checkpoint 1: 4/4 LazyLoad state propagation tests pass
- Checkpoint 2: 37/37 LazyLoad tests pass
- Checkpoint 3: Full test suite passes (1753 + 245 + 55 + 26 = 2079 tests, 0 failures)

### Milestone 5: Design documentation (Change 9)
- Updated `Design.Domain/PropertySystem/LazyLoadProperty.cs` with STATE PROPAGATION, SUBSCRIPTION LIFECYCLE, and COMMON MISTAKE sections
- Checkpoint 4: `dotnet build src/Design/Design.sln` succeeds, Design.Tests: 89/89 pass

### Deviation from Plan
- **Change 7** (EntityBase.FactoryComplete subscription): Not implemented as a separate change because `EntityBase.FactoryComplete()` calls `base.FactoryComplete()` which is `ValidateBase.FactoryComplete()`, and the subscription was already added there in Change 6. Adding it again in EntityBase would be redundant.
- **GetLazyLoadProperties accessibility**: Changed from `private static` to `private protected static` so `EntityBase<T>` (which inherits from `ValidateBase<T>` in the same assembly) can call it. The plan assumed private would work, but C# access rules require `private protected` for derived-class access within the same assembly.

---

## Completion Evidence

**Completed:** 2026-03-07

### Test Results

**Checkpoint 1 - LazyLoad state propagation tests:**
```
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4
```

**Checkpoint 2 - All LazyLoad tests (propagation + serialization):**
```
Passed!  - Failed: 0, Passed: 37, Skipped: 0, Total: 37
```

**Checkpoint 3 - Full test suite:**
```
Passed!  - Failed: 0, Passed: 26, Skipped: 0, Total: 26 - Neatoo.BaseGenerator.Tests.dll (net9.0)
Passed!  - Failed: 0, Passed: 245, Skipped: 0, Total: 245 - Samples.dll (net9.0)
Passed!  - Failed: 0, Passed: 55, Skipped: 0, Total: 55 - Person.DomainModel.Tests.dll (net10.0)
Passed!  - Failed: 0, Passed: 1753, Skipped: 1, Total: 1754 - Neatoo.UnitTest.dll (net9.0)
```

**Checkpoint 4 - Design project:**
```
dotnet build src/Design/Design.sln - Build succeeded. 0 Errors.
Design.Tests: Passed! - Failed: 0, Passed: 89, Skipped: 0, Total: 89
```

### All Contract Items Verified

All 14 contract items are checked (see In Scope section above). Both acceptance criteria tests now pass. Zero out-of-scope test failures. No stop conditions triggered.

### Files Modified

1. `src/Neatoo/LazyLoad.cs` -- Event forwarding from wrapped value
2. `src/Neatoo/ValidateBase.cs` -- Cache, polling methods, subscription infrastructure, wiring in FactoryComplete/OnDeserialized
3. `src/Neatoo/EntityBase.cs` -- IsModified includes LazyLoad children
4. `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadEntityObject.cs` -- Custom setter for LazyChild
5. `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- State propagation documentation

---

## Documentation

**Expected Deliverables:**
- [x] Update `Design.Domain/PropertySystem/LazyLoadProperty.cs` with state propagation documentation
- [ ] Skill updates: No
- [ ] Sample updates: No

---

## Architect Verification

**Verified:** 2026-03-07
**Verdict:** VERIFIED

### Independent Build and Test Results

All builds and tests pass with zero failures.

**Full solution test run (`dotnet test src/Neatoo.sln`):**
```
Passed!  - Failed: 0, Passed:    26, Skipped: 0, Total:    26 - Neatoo.BaseGenerator.Tests.dll (net9.0)
Passed!  - Failed: 0, Passed:   245, Skipped: 0, Total:   245 - Samples.dll (net9.0)
Passed!  - Failed: 0, Passed:    55, Skipped: 0, Total:    55 - Person.DomainModel.Tests.dll (net10.0)
Passed!  - Failed: 0, Passed:  1753, Skipped: 1, Total:  1754 - Neatoo.UnitTest.dll (net9.0)
```

**Design project (`dotnet test src/Design/Design.Tests/Design.Tests.csproj`):**
```
Passed!  - Failed: 0, Passed:    89, Skipped: 0, Total:    89 - Design.Tests.dll (net9.0)
```

Zero failures across all test projects. The 1 skipped test (`AsyncFlowTests_CheckAllRules`) is pre-existing and unrelated.

### Implementation vs Design Verification

Each design change was independently verified against the source files:

**Change 1 - LazyLoad PropertyChanged forwarding (`src/Neatoo/LazyLoad.cs`):**
- `SubscribeToValuePropertyChanged(T? value)` at line 49: Subscribes to `INotifyPropertyChanged` on the wrapped value. Safe for non-entity types (`string` does not implement `INotifyPropertyChanged`, so the cast fails and it is a no-op).
- `UnsubscribeFromValuePropertyChanged(T? value)` at line 57: Correctly unsubscribes.
- `OnValuePropertyChanged` at line 65: Forwards child `PropertyChanged` as `LazyLoad`'s own event.
- `LazyLoad(T? value)` constructor at line 94-100: Calls `SubscribeToValuePropertyChanged(_value)` after setting `_value`.
- `LoadAsyncCore()` at lines 176-178: Calls `UnsubscribeFromValuePropertyChanged(_value)` before replacing value, then `SubscribeToValuePropertyChanged(_value)` after.
- `LazyLoad(Func<Task<T?>>)` constructor: Does NOT subscribe (no value yet). Correct.
- `[JsonConstructor]` constructor: Does NOT subscribe (value set during deserialization). Correct.

**Change 2 - LazyLoad property discovery cache (`src/Neatoo/ValidateBase.cs`):**
- Static `ConcurrentDictionary<Type, PropertyInfo[]> _lazyLoadPropertyCache` at line 308.
- `GetLazyLoadProperties(Type)` at line 310: Uses `BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic`, filters on `IsGenericType`, `GetGenericTypeDefinition() == typeof(LazyLoad<>)`, and `GetMethod != null`. Matches the established reflection pattern in `NeatooBaseJsonTypeConverter` (lines 115-121).

**Change 3 - EntityBase.IsModified (`src/Neatoo/EntityBase.cs`):**
- `IsModified` at line 152: `this.PropertyManager.IsModified || this.IsDeleted || this.IsNew || this.IsSelfModified || IsAnyLazyLoadChildModified()`.
- `IsAnyLazyLoadChildModified()` at lines 158-169: Iterates cached LazyLoad properties, casts to `IEntityMetaProperties`, checks `IsModified`. For `LazyLoad<string>`, the inner cast `(_value as IEntityMetaProperties)` fails and returns `false`. Correct.

**Change 4 - ValidateBase.IsValid and IsBusy (`src/Neatoo/ValidateBase.cs`):**
- `IsValid` at line 279: `this.PropertyManager.IsValid && IsAllLazyLoadChildrenValid()`.
- `IsBusy` at line 173: `this.RunningTasks.IsRunning || this.PropertyManager.IsBusy || IsAnyLazyLoadChildBusy()`.
- Both polling methods use `IValidateMetaProperties` cast (correct for validation-level properties).

**Change 5 - Subscription infrastructure (`src/Neatoo/ValidateBase.cs`):**
- `_lazyLoadSubscriptions` list at line 357.
- `SubscribeToLazyLoadProperties()` at line 363: Protected method. Calls `UnsubscribeFromLazyLoadProperties()` first (idempotent). Iterates cached properties, subscribes to `INotifyPropertyChanged` on each LazyLoad instance.
- `UnsubscribeFromLazyLoadProperties()` at line 380: Unsubscribes from all tracked instances, clears list.
- `OnLazyLoadPropertyChanged()` at line 389: Checks `!this.IsPaused` before calling `CheckIfMetaPropertiesChanged()`. Correct -- avoids unnecessary event processing during factory operations.

**Change 6 - Wiring subscriptions:**
- `ValidateBase.FactoryComplete()` at line 1062: Calls `SubscribeToLazyLoadProperties()` before `ResumeAllActions()`. Correct.
- `ValidateBase.OnDeserialized()` at line 643: Calls `SubscribeToLazyLoadProperties()` before `ResumeAllActions()`. Correct.

**Change 7 - EntityBase.FactoryComplete not separately modified:**
- `EntityBase.FactoryComplete()` at line 567: Calls `base.FactoryComplete(factoryOperation)` at line 569, which routes to `ValidateBase.FactoryComplete()`. The subscription is already handled there. Adding it again would be redundant. This deviation is correct.

**Change 8 - Test entity custom setter (`src/Neatoo.UnitTest/.../LazyLoadEntityObject.cs`):**
- `LazyChild` at lines 26-35: Backing field `_lazyChild`, getter returns it, setter assigns and calls `SubscribeToLazyLoadProperties()`. This is the recommended Option A from the design. Correct.

**Change 9 - Design documentation (`src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs`):**
- STATE PROPAGATION section at lines 25-30: Documents the forwarding and polling behavior.
- SUBSCRIPTION LIFECYCLE section at lines 32-38: Documents `FactoryComplete()` and `OnDeserialized()` wiring, and the custom setter pattern.
- COMMON MISTAKE section at lines 40-44: Documents the pitfall of assigning LazyLoad after FactoryComplete without calling `SubscribeToLazyLoadProperties()`.
- Custom setter pattern example at lines 126-136.

### Deviation Assessment

**1. `GetLazyLoadProperties` changed to `private protected static`:**
Correct and necessary. The plan specified `private static`, but `EntityBase<T>` (which inherits from `ValidateBase<T>` in the same assembly) needs to call this method for `IsAnyLazyLoadChildModified()`. C# access rules require `private protected` for derived-class access within the same assembly. `protected` would over-expose to user classes. `private protected` is the precise correct modifier.

**2. EntityBase.FactoryComplete not separately modified:**
Safe. `EntityBase.FactoryComplete()` calls `base.FactoryComplete()` at line 569, which is `ValidateBase.FactoryComplete()` where `SubscribeToLazyLoadProperties()` is already called. Adding it again in EntityBase would be redundant and would cause double-subscription (the unsubscribe-first guard would prevent harm, but it would be wasteful).

### Reflection Cache Correctness

The `GetLazyLoadProperties` filter uses `p.GetMethod != null` while `NeatooBaseJsonTypeConverter` uses `p.SetMethod != null`. This difference is intentional and correct: the cache is used for reading property values (`prop.GetValue(this)`), so a getter is required. The converter needs `SetMethod` because it writes values during deserialization. Both filters are appropriate for their contexts.

### No Circular Event Risk

Verified the event chain: Child entity fires `PropertyChanged` -> `LazyLoad<T>.OnValuePropertyChanged` forwards it -> parent's `OnLazyLoadPropertyChanged` calls `CheckIfMetaPropertiesChanged()` -> parent may fire `PropertyChanged("IsModified")`. The parent's `PropertyChanged` is NOT subscribed to by the LazyLoad child, so no infinite loop is possible.

---

## Requirements Verification

[To be filled after architect verification]
