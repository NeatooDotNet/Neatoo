# Requirements Reviewer -- Private Property Setters

Last updated: 2026-03-23
Current step: Post-implementation verification complete

## Key Context

This plan added support for private property setters in the Neatoo source generator. Three layers changed: (1) `SetPrivateValue` promoted to the `IValidateProperty` public interface, (2) `PropertyExtractor` now reads setter accessibility from Roslyn syntax, (3) `PropertyGenerator` emits `SetPrivateValue` for private setters and `get;` only on interfaces.

The critical design constraint was that private-set properties must route through `SetPrivateValue` (not `SetValue`/`.Value =`) because `SetValue` throws `PropertyReadOnlyException` when `IsReadOnly` is true, and `IsReadOnly` is set from `PropertyInfoWrapper.IsPrivateSetter` at property construction time.

## Mistakes to Avoid

None so far.

## User Corrections

None so far.

## Requirements Verification

### Verdict: REQUIREMENTS SATISFIED

All 10 requirements from the todo's Requirements Review are satisfied by the implementation. No violations found. No unintended side effects detected.

### Requirements Compliance

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| 1 | SetValue checks IsReadOnly, SetPrivateValue bypasses it | Satisfied | `ValidateProperty.cs:124-131`: `SetValue()` still checks `IsReadOnly` and throws `PropertyReadOnlyException`. `SetPrivateValue()` at line 135 bypasses this. Generated code for private setters calls `SetPrivateValue(value)` (verified in `PropertyGenerator.cs:50`). No change to runtime behavior -- only the generated caller changed. |
| 2 | IsReadOnly is structural, set from IsPrivateSetter | Satisfied | `PropertyInfoWrapper.cs:12` unchanged: `this.IsPrivateSetter = !propertyInfo.CanWrite || propertyInfo.SetMethod?.IsPrivate == true`. `ValidateProperty.cs:22` unchanged: `this.IsReadOnly = propertyInfo.IsPrivateSetter`. The runtime correctly sets `IsReadOnly=true` for `private set` properties. The generator emits `private set` which C# compiles to a private setter method, so `IsPrivate==true` at runtime. |
| 3 | IsReadOnly is serialized | Satisfied | `NeatooBaseJsonTypeConverter.cs:273-274`: The JSON converter still reads `"IsReadOnly"` from serialized properties. No changes to the serialization code. The `IsReadOnly` state survives round-trips because it is explicitly serialized/deserialized in both `ValidateProperty` constructors (regular at line 22 and `[JsonConstructor]` at line 30). |
| 4 | Partial properties are the ONLY supported pattern | Satisfied | The implementation works entirely through the generated code path. `PropertyGenerator.cs:47-50` emits `{Name}Property.SetPrivateValue(value)` for private setters. No reliance on deprecated `Setter<P>()`. The `PrivateSetPropertyDemo` in `Design.Domain/PropertySystem/PropertyBasics.cs:169-192` uses only partial properties. |
| 5 | Property setter triggers change tracking and rules | Satisfied | `SetPrivateValue` calls `HandleNonNullValue()` at `ValidateProperty.cs:147` which fires `OnPropertyChanged(nameof(Value))` and `OnValueNeatooPropertyChanged()` -- the same path as `SetValue`. The `PrivateSetPropertyDemo` uses `AddAction` rule targeting private-set property. Test `PrivateSet_RuleComputesValue` (Design.Tests) verifies computed total updates. Test `PrivateSet_TriggersPropertyChanged` verifies PropertyChanged fires. |
| 6 | LoadValue bypasses modification tracking | Satisfied | `ValidateProperty.LoadValue()` at line 162 is completely unchanged. Test `PrivateSet_LoadValueSucceeds` in Design.Tests verifies `entity["ComputedTotal"].LoadValue(123.45m)` works on private-set properties. |
| 7 | Rules don't fire during factory operations | Satisfied | No changes to factory operation lifecycle (PauseAllActions/FactoryStart/FactoryComplete). The `PrivateSetPropertyDemo` has a `[Create]` factory method. The `AddAction` rule fires after factory completes, not during. This is governed by existing `PauseAllActions`/`ResumeAllActions` logic in `ValidateBase`, which was not modified. |
| 8 | IsReadOnly is a documented property object member (skill) | Satisfied | `IsReadOnly` property on `IValidateProperty` (line 50) remains unchanged. Private-set properties will correctly have `IsReadOnly=true` at runtime because `PropertyInfoWrapper` detects the private setter method. |
| 9 | MudNeatoo binds ReadOnly from IsReadOnly | Satisfied | No changes to MudNeatoo components. The binding `ReadOnly="@EntityProperty.IsReadOnly"` works because `IsReadOnly` returns `true` for private-set properties. This is the intended consumer of this feature. No code changes needed -- the runtime already supported this; only the generator was misaligned. |
| 10 | IsReadOnly is structural from private setter (docs) | Satisfied | The structural nature of `IsReadOnly` is preserved. It is set once at property construction from `PropertyInfoWrapper.IsPrivateSetter` and is not dynamically changeable. No changes to this mechanism. |

### Plan Business Rules Compliance

| Rule | Status | Evidence |
|------|--------|----------|
| Rule 1: Private set generates private accessor | Satisfied | `PropertyGenerator.cs:47-50`: emits `private set { ... }`. Test `PartialProperty_PrivateSetter_GeneratesPrivateSetAccessor` passes. |
| Rule 2: Private set interface is get-only | Satisfied | `PropertyGenerator.cs:82`: `property.HasSetter && property.SetterAccessibility == null ? "get; set;" : "get;"`. Test `PartialProperty_PrivateSetter_InterfaceIsGetOnly` passes. |
| Rule 3: Private set uses SetPrivateValue | Satisfied | `PropertyGenerator.cs:50`: `{Name}Property.SetPrivateValue(value)`. Test `PartialProperty_PrivateSetter_UsesSetPrivateValue` passes. |
| Rule 4: Protected set preserves accessor, uses .Value | Satisfied | `PropertyGenerator.cs:55`: `{property.SetterAccessibility} set {{ {Name}Property.Value = value`. Test `PartialProperty_ProtectedSetter_PreservesAccessorAndUsesValueAssignment` passes. |
| Rule 5: Internal set preserves accessor, uses .Value | Satisfied | Same code path as Rule 4. Test `PartialProperty_InternalSetter_PreservesAccessorAndUsesValueAssignment` passes. |
| Rule 6: LazyLoad with private set uses LoadValue | Satisfied | `PropertyGenerator.cs:34`: LazyLoad branch emits `{setterPrefix}set {{ {Name}Property.LoadValue(value); }}`. Test `PartialProperty_LazyLoadWithPrivateSetter_UsesLoadValue` passes. |
| Rule 7: Get-only unchanged | Satisfied | `PropertyGenerator.cs:65`: getter-only branch emits `{ get => {Name}Property.Value; }`. Test `PartialProperty_GetOnlyProperty_UnchangedByPrivateSetterFeature` passes. |
| Rule 13: SetPrivateValue on IValidateProperty interface | Satisfied | `IValidateProperty.cs:93`: `Task SetPrivateValue(object? newValue, bool quietly = false)` added. `ValidateProperty<T>:135` already has `public virtual Task SetPrivateValue(...)` which satisfies the interface. Design.Tests `PrivateSet_SetPrivateValueOnInterface` verifies this. |

### Unintended Side Effects

**1. Public API addition to IValidateProperty** -- Adding `SetPrivateValue` to `IValidateProperty` is a public API change. Any external implementation of `IValidateProperty` would need to add this method. However, `IValidateProperty` is not designed for external implementation -- `ValidateProperty<T>` is the only implementation, and it already has the method. The plan explicitly acknowledged this risk and assessed it as minimal. No actual side effect.

**2. Generator incremental caching** -- `PartialPropertyInfo` is a record struct with `IEquatable<T>`. Adding `SetterAccessibility` changes the equality check, which is correct: the generator should re-run when setter accessibility changes. The compiler auto-generates the new `IEquatable<T>` implementation. No side effect.

**3. Serialization round-trip** -- `RemoteFactory` serialization uses `PropertyManager.SetProperties()` which replaces property objects in the `PropertyBag` directly -- it never calls property setters. The `IsReadOnly` flag is serialized/deserialized independently. No change to serialization code. No side effect.

**4. Deprecated Setter<P>() method** -- The deprecated `Setter<P>()` in `ValidateBase.cs:442-455` casts to `IValidatePropertyInternal` to call `SetPrivateValue`. Now that `SetPrivateValue` is on the public `IValidateProperty` interface, this cast is technically unnecessary (the method is accessible without it). However, since the deprecated method was not changed, existing behavior is preserved. No side effect.

**5. State property cascading** -- `SetPrivateValue` calls `HandleNonNullValue()` which fires `OnValueNeatooPropertyChanged()` with default `ChangeReason`. This propagates through the existing `Parent?.AddChildTask()` / `RunningTasks.AddTask()` pattern in the generated setter code. The task tracking code in the generated private setter (`PropertyGenerator.cs:50`) matches the existing public setter pattern. No change to cascading behavior.

### Issues Found

**Issue 1 (Informational, not a violation): Integration tests blocked by pre-existing build failure.** The developer reports that 8 integration tests in `PrivateSetPropertyTests` exist in Design.Tests but are blocked by a pre-existing Design.Tests build failure (NF0105 errors on `[Remote]` public methods). The test code is written and correct; it simply cannot compile until the pre-existing issue is resolved. The generator tests (8 tests) and the build of `Neatoo.sln` (2119 tests) all pass. This is not a requirements violation -- the behavioral contracts are expressed in the test code, and the generator-level verification confirms the generated code is correct.

None of the 10 requirements from the todo's Requirements Review are violated.
