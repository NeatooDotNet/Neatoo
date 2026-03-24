# Private Property Setters in Source Generator

**Date:** 2026-03-23
**Related Todo:** [Private Property Setters](../todos/completed/private-property-setters.md)
**Status:** Complete
**Last Updated:** 2026-03-23

---

## Overview

The source generator ignores setter accessibility on partial properties. When a developer writes `public partial string Name { get; private set; }`, the generator produces a public setter and a `get; set;` interface declaration. This plan fixes the generator to respect `private set` (and other restricted setter accessors) by extracting setter accessibility, routing through `SetPrivateValue` for read-only properties, and emitting `get;` only on the interface.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/private-property-setters.md#requirements-review)

### Relevant Existing Requirements

#### Business Rules

1. **SetValue checks IsReadOnly, SetPrivateValue bypasses it.** `ValidateProperty.cs:124-131`: `SetValue()` throws `PropertyReadOnlyException` when `IsReadOnly` is true. `SetPrivateValue()` bypasses this check. The deprecated `Setter<P>()` in `ValidateBase.cs:446-450` and `ObjectInvalid` property in `ValidateBase.cs:649-651` both use `SetPrivateValue` as precedent.

2. **IsReadOnly is structural, set from IsPrivateSetter.** `ValidateProperty.cs:22`: `this.IsReadOnly = propertyInfo.IsPrivateSetter`. `PropertyInfoWrapper.cs:12`: `this.IsPrivateSetter = !propertyInfo.CanWrite || propertyInfo.SetMethod?.IsPrivate == true`. This is a compile-time structural decision.

3. **IsReadOnly is serialized.** `NeatooBaseJsonTypeConverter.cs:273-274`: The JSON converter reads `IsReadOnly` from the serialized property. Private-set state survives client-server round-trips.

4. **Partial properties are the ONLY supported pattern.** `Design.Domain/PropertySystem/PropertyBasics.cs`: Old Getter/Setter methods are deprecated. Private-set properties must work via the generated code path.

#### Existing Tests

- `Design.Tests/PropertyTests/PropertyBasicsTests.cs` — `Property_SetTriggersPropertyChanged()`: WHEN entity.Name is set, THEN PropertyChanged fires. Private-set properties set internally must preserve this.
- `Design.Tests/PropertyTests/PropertyBasicsTests.cs` — `Property_Indexer_CanLoadValue()`: WHEN `entity["Name"].LoadValue("Loaded")` is called, THEN value is set without modification tracking. Unaffected by this change.
- `Design.Tests/GotchaTests/CommonGotchaTests.cs` — `Gotcha1_*`: WHEN properties are set during [Create], THEN rules do not fire. After factory completes, property changes trigger rules normally.

#### Skill References

- `skills/neatoo/references/properties.md`: `IsReadOnly` is documented as "Whether this property is read-only."
- MudNeatoo components bind `ReadOnly="@EntityProperty.IsReadOnly"`. This is the primary consumer.

### Gaps

**GAP-1: No Design project examples of private-set partial properties.** Zero instances of `public partial string X { get; private set; }` in the codebase. This plan adds Design.Domain examples and Design.Tests behavioral contracts.

**GAP-2: No documented contract for how rules interact with private-set properties.** When `AddAction(t => t.Total = t.Quantity * t.Price, ...)` targets a private-set property, the rule's lambda calls the C# setter. If the generated setter uses `SetValue` (checks IsReadOnly), rules break. If it uses `SetPrivateValue` (bypasses IsReadOnly), it works. This plan establishes the contract: the generated setter for private-set properties uses `SetPrivateValue`.

**GAP-3: No contract for protected/internal setter accessibility.** The runtime's `PropertyInfoWrapper.IsPrivateSetter` only checks `propertyInfo.SetMethod?.IsPrivate`. `protected set` and `internal set` would NOT have `IsPrivate=true` at runtime. This plan scopes to `private set` only; `protected set` and `internal set` preserve their setter accessibility in generated code but do NOT set `IsReadOnly=true` (matching runtime behavior).

### Contradictions

None.

### Recommendations for Architect

Incorporated from the reviewer (see todo). The critical constraint is that the generated setter for private-set properties MUST use `SetPrivateValue`, not `SetValue`/`.Value =`.

---

## Business Rules (Testable Assertions)

### Generator Behavior

1. WHEN a partial property has `private set`, THEN the generated setter has `private set` accessor. — Source: Todo Problem Statement
2. WHEN a partial property has `private set` and `NeedsInterfaceDeclaration=true`, THEN the generated interface declaration has `get;` only (no `set;`). — Source: Todo Solution point 4
3. WHEN a partial property has `private set`, THEN the generated setter body calls `SetPrivateValue(value)` instead of `.Value = value`. — Source: Requirements Review Recommendation 1, GAP-2
4. WHEN a partial property has `protected set`, THEN the generated setter has `protected set` accessor and the generated setter body uses `.Value = value` (same as public set). — Source: GAP-3, runtime `IsPrivateSetter` only checks `IsPrivate`
5. WHEN a partial property has `internal set`, THEN the generated setter has `internal set` accessor and the generated setter body uses `.Value = value` (same as public set). — Source: GAP-3
6. WHEN a partial property has `private set` and is a LazyLoad property, THEN the generated setter has `private set` and uses `LoadValue(value)` (same as public LazyLoad setter). — NEW: LazyLoad already bypasses IsReadOnly; private set only affects accessor visibility
7. WHEN a partial property has no setter (get-only), THEN generation is unchanged (no setter generated). — Source: Existing behavior, not affected

### Runtime Behavior (Existing, Verified by Design Project)

8. WHEN entity code sets a private-set property internally, THEN `SetPrivateValue` fires change tracking, PropertyChanged, and rules normally. — Source: Requirements Review Requirement 1, GAP-2
9. WHEN external code calls `entity["Name"].SetValue(x)` on a private-set property, THEN `PropertyReadOnlyException` is thrown. — Source: Requirements Review Requirement 4
10. WHEN external code calls `entity["Name"].LoadValue(x)` on a private-set property, THEN the value is set (Fetch escape hatch). — Source: Requirements Review Requirement 2
11. WHEN a private-set property has IsReadOnly=true, THEN MudNeatoo binds `ReadOnly="true"` automatically. — Source: Requirements Review Requirement 9. No code change needed.
12. WHEN a private-set property is serialized/deserialized, THEN IsReadOnly survives the round-trip. — Source: Requirements Review Requirement 6. No code change needed.

### API Change

13. WHEN `SetPrivateValue(object?, bool)` is added to `IValidateProperty`, THEN generated code can call it through the interface without casting to concrete types. — NEW: Required to enable Rule 3 cleanly

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Private setter generates private accessor | `public partial string Name { get; private set; }` | 1 | Generated code contains `private set` |
| 2 | Private setter interface is get-only | Private set property with NeedsInterfaceDeclaration=true | 2 | Interface declaration has `get;` only |
| 3 | Private setter uses SetPrivateValue | `public partial string Name { get; private set; }` | 3 | Generated setter body contains `SetPrivateValue(value)`, not `.Value = value` |
| 4 | Protected setter preserves accessor | `protected partial string Derived { get; protected set; }` | 4 | Generated code has `protected set` with `.Value = value` |
| 5 | Internal setter preserves accessor | `public partial string Data { get; internal set; }` | 5 | Generated code has `internal set` with `.Value = value` |
| 6 | LazyLoad with private setter | `public partial LazyLoad<IChild> Child { get; private set; }` | 6 | Generated code has `private set` with `LoadValue(value)` |
| 7 | Get-only property unchanged | `public partial string ReadOnly { get; }` | 7 | No setter generated (unchanged) |
| 8 | Private set property set internally triggers rules | Create entity, set private-set property from AddAction rule | 8 | Property value updates, PropertyChanged fires, rule chain works |
| 9 | Private set property rejects SetValue from indexer | Create entity, call `entity["Name"].SetValue("x")` | 9 | `PropertyReadOnlyException` thrown |
| 10 | Private set property accepts LoadValue | Create entity, call `entity["Name"].LoadValue("x")` | 10 | Value is set, no exception |
| 11 | SetPrivateValue on IValidateProperty interface | Cast IValidateProperty<T> to IValidateProperty, call SetPrivateValue | 13 | Method available, value set successfully |
| 12 | Mixed properties: public set + private set | Entity with both public and private set properties | 1, 3 | Public uses `.Value = value`, private uses `SetPrivateValue` |
| 13 | Private set with task tracking | Private-set property with async rule | 3, 8 | Task tracking (`RunningTasks.AddTask`) still works after SetPrivateValue |

---

## Approach

**Three-layer change: API surface, generator extraction, generator emission.**

**Layer 1: Public API (Neatoo library)**
Add `SetPrivateValue(object? newValue, bool quietly = false)` to the `IValidateProperty` public interface. The method already exists as `public virtual` on `ValidateProperty<T>` -- promoting it to the interface formalizes existing behavior. This lets generated code call `{Name}Property.SetPrivateValue(value)` through the typed `IValidateProperty<T>` backing field without casting to a concrete type.

**Layer 2: Generator extraction (PropertyExtractor + PartialPropertyInfo)**
Extract setter accessibility from the Roslyn syntax tree. The accessor declaration's `Modifiers` collection contains any accessibility keyword (`private`, `protected`, `internal`). Store this as a nullable string field `SetterAccessibility` on `PartialPropertyInfo`.

**Layer 3: Generator emission (PropertyGenerator)**
When emitting property implementations, check `SetterAccessibility`:
- If `"private"`: emit `private set { ... SetPrivateValue(value) ... }` and task tracking
- If `"protected"` or `"internal"`: emit `{accessor} set { ... .Value = value ... }` and task tracking (same as current public set pattern)
- If null (public): emit current pattern unchanged

For interface declarations, if setter accessibility is non-null (non-public), emit `get;` only.

---

## Design

### File Changes

#### 1. `IValidateProperty.cs` — Add `SetPrivateValue` to public interface

Add `SetPrivateValue` method to `IValidateProperty`:

```csharp
/// <summary>
/// Sets the value bypassing IsReadOnly checks.
/// Used by generated setters for private-set properties and by framework internals.
/// </summary>
Task SetPrivateValue(object? newValue, bool quietly = false);
```

This matches the existing signature on `ValidateProperty<T>:135` and `IValidatePropertyInternal:115`. Since `ValidateProperty<T>` already implements this as `public virtual`, no additional implementation is needed -- the existing method satisfies the interface.

#### 2. `PartialPropertyInfo.cs` — Add setter accessibility field

Add `string? SetterAccessibility` to the record struct:

```csharp
internal readonly record struct PartialPropertyInfo(
    string Name,
    string Type,
    string Accessibility,
    bool HasSetter,
    string? SetterAccessibility,  // NEW: "private", "protected", "internal", or null
    bool NeedsInterfaceDeclaration,
    bool IsLazyLoad,
    string? LazyLoadInnerType
) : IEquatable<PartialPropertyInfo>;
```

#### 3. `PropertyExtractor.cs` — Extract setter accessibility

After the `hasSetter` check, extract the setter's accessibility modifier:

```csharp
string? setterAccessibility = null;
if (hasSetter)
{
    var setAccessor = property.AccessorList?.Accessors
        .FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                             a.IsKind(SyntaxKind.InitAccessorDeclaration));
    if (setAccessor != null)
    {
        var accessModifier = setAccessor.Modifiers.FirstOrDefault();
        if (accessModifier != default)
        {
            setterAccessibility = accessModifier.ToString();
        }
    }
}
```

Pass `setterAccessibility` to the `PartialPropertyInfo` constructor.

#### 4. `PropertyGenerator.cs` — Emit correct setter pattern

**Property implementations (`GeneratePropertyImplementations`):**

For scalar (non-LazyLoad) properties with a setter:

- **Private setter** (`SetterAccessibility == "private"`): Use `SetPrivateValue` path:
  ```
  {accessibility} partial {type} {name} { get => {name}Property.Value; private set { {name}Property.SetPrivateValue(value); if (!{name}Property.Task.IsCompleted) { Parent?.AddChildTask({name}Property.Task); RunningTasks.AddTask({name}Property.Task); } } }
  ```

- **Protected/internal setter**: Use `.Value = value` path (same as public, with accessor):
  ```
  {accessibility} partial {type} {name} { get => {name}Property.Value; {setterAccessibility} set { {name}Property.Value = value; if (!{name}Property.Task.IsCompleted) { Parent?.AddChildTask({name}Property.Task); RunningTasks.AddTask({name}Property.Task); } } }
  ```

- **Public setter** (null `SetterAccessibility`): Unchanged.

For LazyLoad properties with a setter:

- **Any restricted setter**: Add the accessor keyword, keep `LoadValue(value)`:
  ```
  {accessibility} partial {type} {name} { get => {name}Property.Value; {setterAccessibility} set { {name}Property.LoadValue(value); } }
  ```

- **Public setter**: Unchanged.

**Interface declarations (`GenerateInterfaceDeclaration`):**

If `SetterAccessibility` is non-null (non-public setter), emit `get;` only:

```csharp
var accessors = property.HasSetter && property.SetterAccessibility == null
    ? "get; set;"
    : "get;";
```

### Why Not Cast to IValidatePropertyInternal

`IValidatePropertyInternal` is `internal` to the Neatoo assembly. Generated code lives in the consuming assembly and cannot access internal types. The alternative of casting to `ValidateProperty<T>` (which is public) would work but couples generated code to a concrete implementation type. Adding `SetPrivateValue` to the `IValidateProperty` interface is the cleanest design.

### Why Protected/Internal Setters Use SetValue (Not SetPrivateValue)

`PropertyInfoWrapper.IsPrivateSetter` checks `propertyInfo.SetMethod?.IsPrivate == true`. For `protected set` and `internal set`, `IsPrivate` is `false`, so `IsReadOnly` is `false`, and `SetValue` does not throw. The generator and runtime agree: only `private set` makes a property read-only. This is consistent.

---

## Domain Model Behavioral Design

Not applicable. This plan modifies the source generator and framework API, not domain model behavior. No new computed properties, visibility flags, reactive rules, or validation rules are needed.

---

## Implementation Steps

### Phase 1: API and Generator Changes

1. **Add `SetPrivateValue` to `IValidateProperty` interface** in `src/Neatoo/IValidateProperty.cs`. Add the method signature matching `ValidateProperty<T>.SetPrivateValue(object?, bool)`.

2. **Add `SetterAccessibility` field to `PartialPropertyInfo`** in `src/Neatoo.BaseGenerator/Models/PartialPropertyInfo.cs`. Add `string? SetterAccessibility` parameter to the record constructor.

3. **Extract setter accessibility in `PropertyExtractor`** in `src/Neatoo.BaseGenerator/Extractors/PropertyExtractor.cs`. After the `hasSetter` detection, extract the setter accessor's first modifier keyword.

4. **Update `PropertyGenerator` for property implementations** in `src/Neatoo.BaseGenerator/Generators/PropertyGenerator.cs`. In `GeneratePropertyImplementations`, add branching for private set (SetPrivateValue) vs other restricted setters vs public setter. Handle both scalar and LazyLoad cases.

5. **Update `PropertyGenerator` for interface declarations** in `src/Neatoo.BaseGenerator/Generators/PropertyGenerator.cs`. In `GenerateInterfaceDeclaration`, emit `get;` only when the setter is non-public.

6. **Build and verify**: `dotnet build src/Neatoo.sln` must pass. Run `dotnet test src/Neatoo.sln` to confirm no regressions.

### Phase 2: Generator Tests

7. **Add private setter tests to `PartialPropertyGenerationTests`** in `src/Neatoo.BaseGenerator.Tests/PartialPropertyGenerationTests.cs`. Tests for scenarios 1-7, 12: private setter accessor, interface get-only, SetPrivateValue call, protected/internal setter pass-through, LazyLoad with private set, mixed properties. Update the NeatooStubs in `GeneratorTestHelper.cs` if needed (add `SetPrivateValue` to the `IValidateProperty` stub).

8. **Build and test**: `dotnet test src/Neatoo.BaseGenerator.Tests/Neatoo.BaseGenerator.Tests.csproj`

### Phase 3: Design Project and Integration Tests

9. **Add private-set property examples to Design.Domain** in `src/Design/Design.Domain/PropertySystem/PropertyBasics.cs`. Add a `PrivateSetPropertyDemo` entity with:
   - A public property with `private set` (e.g., `ComputedTotal`)
   - A public property with normal `set` (e.g., `Quantity`, `Price`)
   - An `AddAction` rule that computes `ComputedTotal = Quantity * Price`
   - A corresponding interface in `IPropertyInterfaces.cs` with `get;` only for the private-set property

10. **Add behavioral contract tests to Design.Tests** in `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs`. Tests for scenarios 8-10, 13:
    - Private-set property set internally via rule (triggers PropertyChanged, change tracking)
    - Private-set property rejects SetValue from indexer (throws PropertyReadOnlyException)
    - Private-set property accepts LoadValue from indexer
    - SetPrivateValue callable on IValidateProperty interface

11. **Build and test**: `dotnet build src/Design/Design.sln && dotnet test src/Design/Design.Tests/Design.Tests.csproj`

---

## Acceptance Criteria

- [ ] `dotnet build src/Neatoo.sln` passes (zero errors)
- [ ] `dotnet test src/Neatoo.sln` passes (zero failures)
- [ ] Design.Domain has a private-set property example that compiles
- [ ] Design.Tests has behavioral tests for private-set properties that pass
- [ ] Generator tests cover scenarios 1-7, 12 from the test scenario table
- [ ] Integration tests cover scenarios 8-10, 13 from the test scenario table
- [ ] `IValidateProperty` public interface includes `SetPrivateValue` method
- [ ] Generated setter for `private set` uses `SetPrivateValue` (not `.Value =`)
- [ ] Generated interface declaration for `private set` uses `get;` only
- [ ] No existing tests are broken or modified in incompatible ways

**Note:** Design.sln has pre-existing NF0105 build errors (public `[Remote]` methods). These are unrelated to this work. The developer should ensure the private-set additions to Design.Domain do not introduce NEW errors beyond these pre-existing ones.

**Documentation deliverables (Step 9):** Neatoo skill reference for properties, Design.Domain code comments, release notes for the `IValidateProperty` public API addition.

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: API + Generator | developer | Yes | Core implementation: 5 files, ~40 lines changed | None |
| Phase 2: Generator Tests | developer | No (continue) | Tests verify Phase 1 output, same context needed | Phase 1 |
| Phase 3: Design Project + Integration Tests | developer | Yes | Different solution (Design.sln), clean context beneficial | Phase 1 |

**Parallelizable phases:** Phases 2 and 3 can run in parallel after Phase 1 completes (they test different aspects independently). However, since Phase 2 is small, running it as a continuation of Phase 1 is more practical.

**Notes:** Phase 1 and 2 should be a single developer invocation. Phase 3 can be separate since Design.sln has pre-existing build issues that need careful handling.

---

## Dependencies

- **Neatoo.BaseGenerator**: The Roslyn source generator project
- **Neatoo**: Core library where `IValidateProperty` lives
- **Design.Domain / Design.Tests**: For verification examples (pre-existing NF0105 errors may affect build)
- No RemoteFactory changes needed (serialization uses `PropertyManager.SetProperties()` which bypasses setters)

---

## Risks / Considerations

1. **Public API addition**: Adding `SetPrivateValue` to `IValidateProperty` is a public API change. Any external implementations of `IValidateProperty` would need to implement this method. In practice, only `ValidateProperty<T>` (and `EntityProperty<T>` by inheritance) implement this interface -- both already have the method. Risk: minimal.

2. **Design.sln pre-existing errors**: Design.sln has 101 NF0105 errors (public `[Remote]` methods). Phase 3 may not be able to build the full solution. The developer should try building just the new files or suppress these errors for verification. If the Design project cannot compile the new additions, leave the failing code in place as acceptance criteria per architect protocol.

3. **Generator incremental caching**: Adding `SetterAccessibility` to `PartialPropertyInfo` changes the record struct's `IEquatable<T>` implementation. This is correct -- the generator should re-run when setter accessibility changes. No additional cache invalidation work needed.

4. **Task tracking with SetPrivateValue**: `SetPrivateValue` returns a `Task` just like `SetValue` does (it's the same underlying path minus the IsReadOnly check). The task tracking pattern (`RunningTasks.AddTask`) works identically. Verified by reading `ValidateProperty<T>.SetPrivateValue()` -> `HandleNonNullValue()` -> sets `this.Task`.
