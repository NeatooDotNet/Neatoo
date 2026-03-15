# Plan: Generate LazyLoad Property Registration via BaseGenerator

**Date:** 2026-03-14
**Related Todo:** [Generate LazyLoad Property Registration](../todos/generate-lazyload-registration.md)
**Status:** Verified
**Last Updated:** 2026-03-14 (architect verification complete)

---

## Overview

Make `LazyLoad<T>` properties partial -- matching how every other Neatoo property works. The BaseGenerator will detect them, generate backing fields, generate getter/setter implementations with automatic PropertyManager registration, and generate `Register` calls in `InitializePropertyBackingFields`. This eliminates the reflection-based `FinalizeRegistration`, the consumer's `RegisterLazyLoadProperties()` calls, and the custom setter boilerplate.

**Guiding principle:** LazyLoad properties should work exactly like scalar partial properties. The deviation is LazyLoad being special -- the fix is to remove the specialness.

---

## Before / After: Person.cs

### BEFORE (current)

```csharp
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    private readonly ILazyLoadFactory _lazyLoadFactory;
    private readonly IPersonPhoneListFactory _personPhoneListFactory;

    public Person(IEntityBaseServices<Person> editBaseServices,
                  IPersonPhoneListFactory personPhoneListFactory,
                  ILazyLoadFactory lazyLoadFactory) : base(editBaseServices)
    {
        _lazyLoadFactory = lazyLoadFactory;
        _personPhoneListFactory = personPhoneListFactory;

        PersonPhoneList = lazyLoadFactory.Create<IPersonPhoneList>(async () =>
        {
            return await _personPhoneListFactory.Fetch(this.Id ?? Guid.Empty);
        });
    }

    public partial Guid? Id { get; set; }
    public partial string? FirstName { get; set; }
    // ... other partial properties ...

    // Manual backing field, manual setter, manual RegisterLazyLoadProperties()
    private LazyLoad<IPersonPhoneList> _personPhoneList = null!;
    public LazyLoad<IPersonPhoneList> PersonPhoneList
    {
        get => _personPhoneList;
        internal set
        {
            _personPhoneList = value;
            RegisterLazyLoadProperties();  // Consumer leaking framework plumbing
        }
    }

    [Create]
    public void Create([Service] IPersonPhoneList personPhoneModelList)
    {
        PersonPhoneList = _lazyLoadFactory.Create<IPersonPhoneList>(personPhoneModelList);
    }
}
```

### AFTER (proposed)

```csharp
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    private readonly ILazyLoadFactory _lazyLoadFactory;
    private readonly IPersonPhoneListFactory _personPhoneListFactory;

    public Person(IEntityBaseServices<Person> editBaseServices,
                  IPersonPhoneListFactory personPhoneListFactory,
                  ILazyLoadFactory lazyLoadFactory) : base(editBaseServices)
    {
        _lazyLoadFactory = lazyLoadFactory;
        _personPhoneListFactory = personPhoneListFactory;

        PersonPhoneList = lazyLoadFactory.Create<IPersonPhoneList>(async () =>
        {
            return await _personPhoneListFactory.Fetch(this.Id ?? Guid.Empty);
        });
    }

    public partial Guid? Id { get; set; }
    public partial string? FirstName { get; set; }
    // ... other partial properties ...

    // Just like every other property -- partial, no manual backing field, no manual registration
    public partial LazyLoad<IPersonPhoneList> PersonPhoneList { get; set; }

    [Create]
    public void Create([Service] IPersonPhoneList personPhoneModelList)
    {
        PersonPhoneList = _lazyLoadFactory.Create<IPersonPhoneList>(personPhoneModelList);
    }
}
```

**What disappeared:**
- Manual `_personPhoneList` backing field
- Custom getter/setter
- `RegisterLazyLoadProperties()` call

**What stayed the same:**
- `ILazyLoadFactory` injection and usage (creates the LazyLoad instance)
- Assignment in constructor and factory methods
- Everything else

---

## Locked Design Decisions

All design choices have been resolved by the user.

### Decision 1: Consumer declaration

```csharp
public partial LazyLoad<IPersonPhoneList> PersonPhoneList { get; set; }
```

Same syntax as scalar partial properties. The generator detects that the property type is `LazyLoad<T>` and switches to LazyLoad-specific code generation. The consumer uses whatever access modifier they need (`set;`, `internal set;`, `private set;`). The generator respects the declared accessor visibility, same as for scalar properties.

### Decision 2: Generated backing field accessor

**Scalar property today:**
```csharp
protected IValidateProperty<string?> NameProperty => (IValidateProperty<string?>)PropertyManager[nameof(Name)]!;
```

**LazyLoad property (generated):**
```csharp
protected IValidateProperty<LazyLoad<IPersonPhoneList>> PersonPhoneListProperty =>
    (IValidateProperty<LazyLoad<IPersonPhoneList>>)PropertyManager[nameof(PersonPhoneList)]!;
```

This is the *raw* PropertyManager entry. It holds the `LazyLoad<T>` wrapper, NOT the inner entity. The look-through behavior (delegating IsValid, IsBusy, etc. to the inner entity) is handled by the property subclass created during registration.

### Decision 3: Generated setter -- Use LoadValue (Choice A1, locked)

```csharp
public partial LazyLoad<IPersonPhoneList> PersonPhoneList
{
    get => PersonPhoneListProperty.Value;
    set
    {
        PersonPhoneListProperty.LoadValue(value);
    }
}
```

**Rationale:** `LoadValue` already handles connecting/disconnecting inner children in the LazyLoad property subclasses (`LazyLoadValidateProperty.LoadValue`, `LazyLoadEntityProperty.LoadValue`). It fires `ChangeReason.Load` which does not trigger rule cascading. Assigning a LazyLoad wrapper is "loading" the container, not "setting" a domain value. No task tracking is needed because `LoadValue` is synchronous.

For read-only LazyLoad properties (`{ get; }` only), the generator produces only the getter:

```csharp
public partial LazyLoad<IPersonPhoneList> PersonPhoneList
{
    get => PersonPhoneListProperty.Value;
}
```

### Decision 4: Registration -- New CreateLazyLoad method on IPropertyFactory (Choice B1, locked)

Add a method to `IPropertyFactory<TOwner>`:

```csharp
IValidateProperty CreateLazyLoad<TInner>(TOwner owner, string propertyName) where TInner : class?;
```

The generator emits in `InitializePropertyBackingFields`:

```csharp
PropertyManager.Register(factory.CreateLazyLoad<IPersonPhoneList>(this, nameof(PersonPhoneList)));
```

**Implementation in `DefaultPropertyFactory`:**

```csharp
public IValidateProperty CreateLazyLoad<TInner>(TOwner owner, string propertyName) where TInner : class?
{
    var propertyInfo = _propertyInfoList.GetPropertyInfo(propertyName)
        ?? throw new PropertyNotFoundException($"Property '{propertyName}' not found in '{typeof(TOwner).Name}'");

    return new LazyLoadValidateProperty<TInner>(propertyInfo);
}
```

**Implementation in `EntityPropertyFactory`:**

```csharp
public IValidateProperty CreateLazyLoad<TInner>(TOwner owner, string propertyName) where TInner : class?
{
    var propertyInfo = _propertyInfoList.GetPropertyInfo(propertyName)
        ?? throw new PropertyNotFoundException($"Property '{propertyName}' not found in '{typeof(TOwner).Name}'");

    return new LazyLoadEntityProperty<TInner>(propertyInfo);
}
```

This preserves the factory polymorphism: `EntityPropertyFactory` creates entity-variant subclasses, `DefaultPropertyFactory` creates validate-variant subclasses. Same pattern as the existing `Create<T>` method.

**Breaking change note:** This adds a method to the public `IPropertyFactory<T>` interface. Custom implementations will get a compile error. This is acceptable -- custom property factories are rare and the fix is trivial (add the method).

### Decision 5: Remove reflection infrastructure immediately (Choice C, locked)

No deprecation. Remove outright in this release:

- **`RegisterLazyLoadProperties()`** on `ValidateBase<T>` -- delete entirely
- **`RegisterLazyLoadProperty<TInner>()`** on `ValidateBase<T>` -- delete entirely
- **`FinalizeRegistration()`** on `IValidatePropertyManagerInternal<P>` -- delete entirely
- **`RegisterLazyLoadProperty<TInner>()`** on `IValidatePropertyManagerInternal<P>` -- delete entirely
- **`TryGetRegisteredProperty()`** on `IValidatePropertyManagerInternal<P>` -- delete if no other consumers
- **`GetLazyLoadProperties()` static cache** on `ValidatePropertyManager<P>` -- delete entirely
- **`CreateLazyLoadProperty()` virtual method** on `ValidatePropertyManager<P>` and override in `EntityPropertyManager` -- delete (replaced by `IPropertyFactory.CreateLazyLoad<T>`)
- **`_lazyLoadPropertyCache`** `ConcurrentDictionary` -- delete entirely
- **`FinalizeRegistration()` calls** in `ValidateBase.FactoryComplete()` and `ValidateBase.OnDeserialized()` -- remove

### Decision 6: ILazyLoadFactory survives

`ILazyLoadFactory` is about *creating* LazyLoad instances, not *registering* them. The consumer still needs it to create `LazyLoad<T>` instances with loader delegates. Two orthogonal concerns.

### Decision 7: IPropertyInfoList must include LazyLoad properties

The `IPropertyFactory<T>.CreateLazyLoad<TInner>` looks up `IPropertyInfo` from `IPropertyInfoList<T>` by name. The `IPropertyInfoList` is populated via reflection in `AddNeatooServices`. LazyLoad partial properties will be C# partial properties, so they will have reflection `PropertyInfo` entries and should already appear in `IPropertyInfoList`. **Verify during implementation** -- if not, the `PropertyInfoList` population logic needs to include them.

---

## Serialization Interaction

### NeatooBaseJsonTypeConverter: No Changes Needed (Concern 2, resolved)

The `NeatooBaseJsonTypeConverter` (`src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs`) uses reflection to discover `LazyLoad<>` properties in both Read and Write paths:

**Write path (line 393-405):** Iterates `value.GetType().GetProperties()` filtered by `PropertyType.IsGenericType && GetGenericTypeDefinition() == typeof(LazyLoad<>)`. Partial properties compile to regular properties with compiler-generated backing fields, so the reflection discovery finds them unchanged.

**Read path (line 131-136):** Discovers `LazyLoad<>` properties with `SetMethod != null`. For `public partial LazyLoad<T> Prop { get; set; }`, the generated property implementation has a setter, so `SetMethod != null` is true.

**PropertyManager array (Write, line 352-357):** Skips `ILazyLoadProperty` entries via `if (p is ILazyLoadProperty) continue;`. This prevents double-serialization. The LazyLoad property subclass IS an `ILazyLoadProperty`, so it is skipped. The LazyLoad value is serialized separately as a top-level JSON property. This behavior is identical before and after the change.

**Deserialization merge path (Read, line 196-213):** Gets the existing value via `property.GetValue(result)` and calls `mergeable.ApplyDeserializedState()`. After this change, the "existing" value is the constructor-created `LazyLoad<T>` instance (assigned via the generated setter in the constructor). `ApplyDeserializedState` merges the serialized Value/IsLoaded into it, preserving the loader delegate. This works correctly.

**Confirmation: `NeatooBaseJsonTypeConverter` requires ZERO code changes.** The file does NOT appear in the Impact Analysis "Files Modified" table.

### OnDeserialized Reconnection: Concrete Mechanism (Concern 1, resolved)

**The problem:** After deserialization, the constructor has run (creating the LazyLoad property subclass in PropertyManager via `InitializePropertyBackingFields` and calling `LoadValue` via the generated setter). Then `NeatooBaseJsonTypeConverter` calls `ApplyDeserializedState` on the existing `LazyLoad<T>` instance, which modifies its inner `_value` and `_isLoaded` fields directly -- bypassing the generated setter and the property subclass's `LoadValue`. The property subclass's internal state (inner child event subscriptions) is now stale.

**The solution: In `ValidateBase.OnDeserialized()`, iterate PropertyManager for `ILazyLoadProperty` entries and call `LoadValue` with the current `LazyLoad<T>` value.**

This is a framework-level change in `ValidateBase.OnDeserialized()`, NOT a generated method. It replaces the deleted `FinalizeRegistration()` call with:

```csharp
// In ValidateBase.OnDeserialized(), replacing the FinalizeRegistration call:
foreach (var property in pmInternal.GetProperties)
{
    if (property is ILazyLoadProperty)
    {
        // Re-fire LoadValue so the property subclass reconnects
        // inner child events after ApplyDeserializedState modified
        // the LazyLoad instance's inner value directly.
        property.LoadValue(((IValidateProperty)property).Value);
    }
}
```

Wait -- this does not work because `IValidateProperty.Value` on a `LazyLoadValidateProperty` returns the INNER entity (via the `new` override), not the `LazyLoad<T>` wrapper. We need the wrapper.

**Corrected approach:** The property subclass holds the `LazyLoad<T>` wrapper in its `_value` field (inherited from `ValidateProperty<LazyLoad<T>>`). After `ApplyDeserializedState` modifies the wrapper's inner value, we need to tell the property subclass to reconnect. The simplest way: call `LoadValue` with the same `LazyLoad<T>` reference that the property subclass already holds. `LoadValue` disconnects the old inner child, then reconnects to the (now-updated) inner child.

```csharp
// In ValidateBase.OnDeserialized(), after the existing SetParent loop,
// replacing the deleted FinalizeRegistration() call:
foreach (var property in pmInternal.GetProperties)
{
    if (property is ILazyLoadProperty lazyProp)
    {
        // The LazyLoad wrapper's inner value was updated by ApplyDeserializedState.
        // Re-fire LoadValue with the same wrapper so the property subclass
        // reconnects inner child events (disconnect old, connect new).
        // ValidateProperty<T>._value holds the LazyLoad<T> wrapper.
        // We access it via the base Value property (not the 'new' override).
        var wrapper = ((IValidateProperty)property).Value;
        // But IValidateProperty.Value is the 'new' override that returns BoxedValue...
        // We need the wrapper itself. Use a new interface method.
    }
}
```

**Actual mechanism:** Add a method to `ILazyLoadProperty`:

```csharp
internal interface ILazyLoadProperty
{
    /// <summary>
    /// Reconnects the property subclass to its LazyLoad wrapper's current inner value.
    /// Called after deserialization when ApplyDeserializedState has modified the wrapper directly.
    /// </summary>
    void ReconnectAfterDeserialization();
}
```

Implementation in `LazyLoadValidateProperty<T>`:

```csharp
void ILazyLoadProperty.ReconnectAfterDeserialization()
{
    if (this._value != null)
    {
        // Disconnect existing inner child
        LazyLoadPropertyHelper.DisconnectInnerChild(ref _currentInnerChild, this.PassThruValueNeatooPropertyChanged);
        // Connect to (possibly updated) inner child
        var innerChild = ((ILazyLoadDeserializable)this._value).BoxedValue;
        _currentInnerChild = LazyLoadPropertyHelper.ConnectInnerChild(innerChild, this.PassThruValueNeatooPropertyChanged);
    }
}
```

Same implementation in `LazyLoadEntityProperty<T>`.

Then in `ValidateBase.OnDeserialized()`:

```csharp
// Replace the deleted FinalizeRegistration() call with:
foreach (var property in pmInternal.GetProperties)
{
    if (property is ILazyLoadProperty lazyProp)
    {
        lazyProp.ReconnectAfterDeserialization();
    }
}
```

This is clean, non-reflective, and localized. No generator changes needed for deserialization support.

---

## Generated Code: Full Example

For `Person.cs` with `public partial LazyLoad<IPersonPhoneList> PersonPhoneList { get; set; }`, the generator produces in `DomainModel.Person.g.cs`:

```csharp
// Backing field accessor (same pattern as scalar properties)
protected IValidateProperty<LazyLoad<IPersonPhoneList>> PersonPhoneListProperty =>
    (IValidateProperty<LazyLoad<IPersonPhoneList>>)PropertyManager[nameof(PersonPhoneList)]!;

// Property implementation (LoadValue pattern for LazyLoad)
public partial LazyLoad<IPersonPhoneList> PersonPhoneList
{
    get => PersonPhoneListProperty.Value;
    set
    {
        PersonPhoneListProperty.LoadValue(value);
    }
}

// In InitializePropertyBackingFields (alongside scalar properties):
protected override void InitializePropertyBackingFields(IPropertyFactory<DomainModel.Person> factory)
{
    // Scalar properties
    PropertyManager.Register(factory.Create<Guid?>(this, nameof(Id)));
    PropertyManager.Register(factory.Create<string?>(this, nameof(FirstName)));
    PropertyManager.Register(factory.Create<string?>(this, nameof(LastName)));
    PropertyManager.Register(factory.Create<string?>(this, nameof(Email)));
    PropertyManager.Register(factory.Create<string?>(this, nameof(Notes)));

    // LazyLoad properties -- uses CreateLazyLoad<TInner> instead of Create<T>
    PropertyManager.Register(factory.CreateLazyLoad<IPersonPhoneList>(this, nameof(PersonPhoneList)));
}
```

**Note the differences from scalar properties:**
1. Setter uses `LoadValue()` instead of `.Value =` (no rule triggering, no task tracking)
2. Registration uses `factory.CreateLazyLoad<TInner>()` instead of `factory.Create<T>()`
3. The type parameter to `CreateLazyLoad` is the INNER type (`IPersonPhoneList`), not the wrapper type

---

## Impact Analysis

### Files Modified

| File | Change | Risk |
|------|--------|------|
| `src/Neatoo.BaseGenerator/Extractors/PropertyExtractor.cs` | Detect `LazyLoad<T>` partial properties, extract inner type | Low |
| `src/Neatoo.BaseGenerator/Models/PartialPropertyInfo.cs` | Add `IsLazyLoad` flag and `LazyLoadInnerType` | Low |
| `src/Neatoo.BaseGenerator/Generators/PropertyGenerator.cs` | Generate LazyLoad-specific backing field, getter/setter | Medium |
| `src/Neatoo.BaseGenerator/Generators/InitializerGenerator.cs` | Generate LazyLoad registration calls with `CreateLazyLoad<TInner>` | Low |
| `src/Neatoo/IPropertyFactory.cs` | Add `CreateLazyLoad<TInner>` method | Low (breaking for custom impls) |
| `src/Neatoo/Internal/DefaultPropertyFactory.cs` | Implement `CreateLazyLoad<TInner>` | Low |
| `src/Neatoo/Internal/EntityPropertyFactory.cs` | Implement `CreateLazyLoad<TInner>` | Low |
| `src/Neatoo/ValidateBase.cs` | Remove `RegisterLazyLoadProperties`, `RegisterLazyLoadProperty<T>`, simplify `FactoryComplete` (remove `FinalizeRegistration` call), update `OnDeserialized` (replace `FinalizeRegistration` with `ILazyLoadProperty.ReconnectAfterDeserialization` loop) | Medium |
| `src/Neatoo/EntityBase.cs` | **No `FinalizeRegistration` calls exist.** `EntityBase.FactoryComplete()` calls `base.FactoryComplete(factoryOperation)` then state management. The `FinalizeRegistration` call is in `ValidateBase.FactoryComplete()`. `EntityBase` does NOT override `OnDeserialized`. No changes needed to EntityBase for LazyLoad removal. | None |
| `src/Neatoo/Internal/ValidatePropertyManager.cs` | Remove `FinalizeRegistration`, `RegisterLazyLoadProperty<T>`, `GetLazyLoadProperties`, `CreateLazyLoadProperty`, `_lazyLoadPropertyCache`, `TryGetRegisteredProperty` | Medium |
| `src/Neatoo/Internal/EntityPropertyManager.cs` | Remove `CreateLazyLoadProperty` override | Low |
| `src/Neatoo/Internal/LazyLoadValidateProperty.cs` | Add `ReconnectAfterDeserialization()` implementation for `ILazyLoadProperty` | Low |
| `src/Neatoo/Internal/LazyLoadEntityProperty.cs` | Add `ReconnectAfterDeserialization()` implementation for `ILazyLoadProperty` | Low |
| `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` | **No changes needed.** Reflection discovery of `LazyLoad<>` properties works unchanged -- partial properties compile to regular properties with `SetMethod != null`. Serialization skips `ILazyLoadProperty` in PropertyManager array, serializes LazyLoad separately. Merge path via `ApplyDeserializedState` is unchanged. | None |
| `src/Examples/Person/Person.DomainModel/Person.cs` | Convert to `public partial LazyLoad<IPersonPhoneList> PersonPhoneList { get; set; }` | Low |
| `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` | Convert ALL LazyLoad properties to partial, update design comments | Low |
| `src/samples/LazyLoadSamples.cs` | Convert ALL LazyLoad properties to partial, update samples and anti-pattern comments | Low |
| `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadEntityObject.cs` | Convert ALL LazyLoad properties to partial (both `LazyDescription` and `LazyChild`) | Low |
| `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadValidateObject.cs` | Convert `LazyContent` to partial | Low |
| `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs` | Convert `LazyChild` to partial in `CrashParent` | Low |

### Breaking Changes

1. **Consumer-facing:** `RegisterLazyLoadProperties()` and `RegisterLazyLoadProperty<T>()` are removed. Consumers using the old manual pattern must convert to partial properties. This is a major version bump.

2. **`IPropertyFactory<T>`:** New `CreateLazyLoad<TInner>` method. Custom implementations get a compile error. Fix is trivial.

3. **`IValidatePropertyManagerInternal<P>`:** `FinalizeRegistration()`, `RegisterLazyLoadProperty<T>()`, `TryGetRegisteredProperty()` removed. This is an internal interface -- no consumer impact.

4. **Serialization:** JSON format must not change. The serializer already handles LazyLoad properties separately from PropertyManager. This should be unaffected.

---

## Developer Concern Resolutions

### Concern 1 (BLOCKING -- OnDeserialized reconnection): RESOLVED

Concrete mechanism specified in "Serialization Interaction" section above. Summary:
- Add `ReconnectAfterDeserialization()` method to `ILazyLoadProperty` marker interface
- Implement in both `LazyLoadValidateProperty<T>` and `LazyLoadEntityProperty<T>` -- disconnects old inner child, reconnects to current inner value
- In `ValidateBase.OnDeserialized()`, replace the deleted `FinalizeRegistration()` call with a loop over `pmInternal.GetProperties` that calls `ReconnectAfterDeserialization()` on each `ILazyLoadProperty`
- No generator changes needed for deserialization support -- this is entirely a framework-level change

### Concern 2 (BLOCKING -- Serializer confirmation): RESOLVED

Verified by reading `NeatooBaseJsonTypeConverter.cs`. Full analysis in "Serialization Interaction" section above. **Zero changes needed to the serializer.** The reflection-based discovery of `LazyLoad<>` properties works unchanged because partial properties compile to regular properties with `PropertyType = LazyLoad<T>` and `SetMethod != null`.

### Concern 3 (Setter access modifier): RESOLVED

**Accepted.** Plain `set` is acceptable. Concrete classes are `internal`, so the setter visibility only matters within the assembly. The public interface (`IPerson`) exposes `LazyLoad<IPersonPhoneList> PersonPhoneList { get; }` (getter-only). External consumers cannot call the setter. The setter access modifier gap is pre-existing for all partial properties and is out of scope for this plan.

### Concern 4 (Unregistered LazyLoad auto-properties): RESOLVED

**ALL LazyLoad properties become partial.** This is the new standard. There is no reason for any LazyLoad property to remain a non-partial auto-property. Specific entities to convert:

- `LazyLoadEntityObject.cs`: `LazyDescription` (auto-property `= null!`) becomes `public partial LazyLoad<string> LazyDescription { get; set; }`. `LazyChild` (manual backing field + `RegisterLazyLoadProperties`) becomes `public partial LazyLoad<ILazyLoadEntityObject> LazyChild { get; set; }`.
- `LazyLoadValidateObject.cs`: `LazyContent` (auto-property `= null!`) becomes `public partial LazyLoad<string> LazyContent { get; set; }`.
- `WaitForTasksLazyLoadCrashEntity.cs`: `CrashParent.LazyChild` (manual backing field + `RegisterLazyLoadProperties`) becomes `public partial LazyLoad<ICrashChild> LazyChild { get; set; }`.
- `Design.Domain/PropertySystem/LazyLoadProperty.cs`: `LazyDescription` and `LazyContent` become partial.
- `samples/LazyLoadSamples.cs`: `SkillLazyParent.LazyChild` becomes partial. Anti-pattern comments updated.
- `Person.cs`: `PersonPhoneList` becomes partial (already in Before/After).

**Note:** Converting `LazyDescription` from unregistered auto-property to partial DOES change behavior -- it will now participate in PropertyManager state propagation (IsValid, IsBusy cascading). Test assertions may need updating. This is correct behavior -- ALL LazyLoad properties should participate in state propagation.

### Concern 5 (EntityBase.cs): RESOLVED

**`EntityBase.cs` requires NO changes for LazyLoad removal.** Verified by reading the file:

- `EntityBase.FactoryComplete()` (line 555-578) calls `base.FactoryComplete(factoryOperation)` which goes to `ValidateBase.FactoryComplete()`. The `FinalizeRegistration()` call is in `ValidateBase.FactoryComplete()`, not `EntityBase.FactoryComplete()`. Removing it from `ValidateBase` is sufficient.
- `EntityBase` does NOT override `OnDeserialized()`. It inherits `ValidateBase.OnDeserialized()` directly.
- `EntityBase` has no references to `RegisterLazyLoadProperties`, `FinalizeRegistration`, or any LazyLoad registration infrastructure.

The Impact Analysis table is updated to reflect this: EntityBase.cs has "No changes needed" with the explanation above.

---

## Implementation Phases

### Phase 1: Generator changes (BaseGenerator)

**Deliverables:**
1. `PropertyExtractor` detects `LazyLoad<T>` partial properties and extracts inner type `T`
   - In the Roslyn SemanticModel, check if the property type's `OriginalDefinition` is `LazyLoad<>` in the `Neatoo` namespace
   - Extract the single type argument as `LazyLoadInnerType`
2. `PartialPropertyInfo` gains `IsLazyLoad` (bool) and `LazyLoadInnerType` (string?, null for non-LazyLoad)
3. `PropertyGenerator.GenerateBackingFields` -- same pattern for LazyLoad (cast to `IValidateProperty<LazyLoad<TInner>>`)
4. `PropertyGenerator.GeneratePropertyImplementations` -- LazyLoad uses `LoadValue` in setter, no task tracking
5. `InitializerGenerator.GenerateInitializeMethod` -- LazyLoad uses `factory.CreateLazyLoad<TInner>(this, nameof(Prop))` instead of `factory.Create<T>(this, nameof(Prop))`
6. Generator unit tests for LazyLoad property detection and code generation

**Verification gate:** Generator produces correct code for a class with both scalar and LazyLoad partial properties. Build succeeds.

### Phase 2: Framework changes (Neatoo library)

**Deliverables:**
1. Add `CreateLazyLoad<TInner>` to `IPropertyFactory<TOwner>` interface
2. Implement in `DefaultPropertyFactory` (creates `LazyLoadValidateProperty<TInner>`)
3. Implement in `EntityPropertyFactory` (creates `LazyLoadEntityProperty<TInner>`)
4. Add `ReconnectAfterDeserialization()` method to `ILazyLoadProperty` interface
5. Implement `ReconnectAfterDeserialization()` in `LazyLoadValidateProperty<T>` and `LazyLoadEntityProperty<T>`
6. Remove from `ValidateBase<T>`: `RegisterLazyLoadProperties()`, `RegisterLazyLoadProperty<TInner>()`
7. Remove from `ValidatePropertyManager<P>`: `FinalizeRegistration()`, `RegisterLazyLoadProperty<TInner>()`, `GetLazyLoadProperties()`, `CreateLazyLoadProperty()`, `_lazyLoadPropertyCache`, `TryGetRegisteredProperty()`
8. Remove from `EntityPropertyManager`: `CreateLazyLoadProperty()` override
9. Remove from `IValidatePropertyManagerInternal<P>`: `FinalizeRegistration()`, `RegisterLazyLoadProperty<TInner>()`, `TryGetRegisteredProperty()`
10. Simplify `ValidateBase.FactoryComplete()` -- remove `FinalizeRegistration()` call
11. Update `ValidateBase.OnDeserialized()` -- replace `FinalizeRegistration()` with `ILazyLoadProperty.ReconnectAfterDeserialization()` loop (see Concern 1 resolution)
12. Verify `IPropertyInfoList` includes LazyLoad partial properties (fix if needed)
13. **EntityBase.cs -- no changes needed** (verified: no `FinalizeRegistration` calls, no `OnDeserialized` override, no LazyLoad registration references)

**Verification gate:** `dotnet build src/Neatoo.sln` succeeds. Framework compiles without the removed code.

### Phase 3: Consumer migration and tests

ALL LazyLoad properties across the codebase become partial. No unregistered auto-properties remain.

**Deliverables:**
1. Convert `Person.cs` to use `public partial LazyLoad<IPersonPhoneList> PersonPhoneList { get; set; }`
2. Convert `Design.Domain/PropertySystem/LazyLoadProperty.cs` -- ALL LazyLoad properties become partial, update design comments to reflect the new pattern
3. Convert `LazyLoadEntityObject.cs` -- BOTH `LazyDescription` (was auto-property) and `LazyChild` (was manual backing field) become partial
4. Convert `LazyLoadValidateObject.cs` -- `LazyContent` (was auto-property) becomes partial
5. Convert `WaitForTasksLazyLoadCrashEntity.cs` -- `CrashParent.LazyChild` (was manual backing field) becomes partial
6. Convert `LazyLoadSamples.cs` -- `SkillLazyParent.LazyChild` becomes partial, update anti-pattern comments to reflect new API
7. Update test assertions if behavior changes (e.g., `LazyDescription` now participates in state propagation)
8. Run full test suite: `dotnet test src/Neatoo.sln`
9. Verify serialization round-trip tests pass

**Verification gate:** All tests pass. No regressions. Zero unregistered `LazyLoad<T>` auto-properties remain in the codebase.

---

## Agent Phasing

| Phase | Fresh Agent? | Rationale |
|-------|-------------|-----------|
| Phase 1 (Generator) | Yes | Independent domain (Roslyn source generator), clean context |
| Phase 2 (Framework) | Yes | Different domain (runtime library), depends on Phase 1 output but needs clean context for the removal work |
| Phase 3 (Migration + Tests) | Resume Phase 2 | Same domain, needs context of what was removed/changed |

---

## Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Serialization regression | High | Extensive round-trip serialization testing. JSON format must not change. |
| `IPropertyInfoList` does not include LazyLoad partial properties | Medium | Verify early in Phase 2. Fix `AddNeatooServices` if needed. |
| `OnDeserialized` reconnection without `FinalizeRegistration` | Medium | Design generated reconnection method or inline code. Test serialization round-trip. |
| Custom `IPropertyFactory<T>` implementations break | Low | Acceptable for major version. Fix is trivial (add one method). |

---

## Acceptance Criteria

1. `public partial LazyLoad<T> Prop { get; set; }` compiles and works identically to the current manual pattern
2. No consumer calls to `RegisterLazyLoadProperties()` required (method does not exist)
3. IsValid, IsBusy, IsModified cascade through LazyLoad to inner entity (same as today)
4. Serialization round-trip works identically to today
5. `Person.cs` example uses partial LazyLoad property with no manual backing field or setter
6. All existing tests pass
7. No runtime reflection for LazyLoad property discovery
8. `IPropertyFactory<T>` has explicit `CreateLazyLoad<TInner>` method
9. `FinalizeRegistration` and all reflection caches are gone from the codebase

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-14
**Re-reviewed:** 2026-03-14 (all concerns resolved)

### My Understanding of This Plan

**Core Change:** Make LazyLoad<T> properties partial (like scalar properties), with the generator handling backing fields, getter/setter implementations, and PropertyManager registration via `InitializePropertyBackingFields`. Remove all reflection-based LazyLoad registration infrastructure.

**User-Facing API:** Consumers declare `public partial LazyLoad<T> Prop { get; set; }` -- same syntax as scalar properties. No manual backing field, no custom setter, no `RegisterLazyLoadProperties()` calls.

**Internal Changes:** (1) Generator detects LazyLoad<T> type, generates LoadValue-based setter and `factory.CreateLazyLoad<TInner>` registration. (2) New `CreateLazyLoad<TInner>` on `IPropertyFactory<T>` + implementations. (3) Delete reflection-based FinalizeRegistration, RegisterLazyLoadProperties, caches.

**Base Classes Affected:** ValidateBase, EntityBase (removal of registration methods), ValidatePropertyManager, EntityPropertyManager (removal of reflection infrastructure).

### Codebase Investigation

**Files Examined:**
- `src/Neatoo.BaseGenerator/Extractors/PropertyExtractor.cs` -- Extracts partial properties; captures `Accessibility` (first modifier only), `HasSetter` (bool), no setter-specific modifiers.
- `src/Neatoo.BaseGenerator/Models/PartialPropertyInfo.cs` -- Record struct with Name, Type, Accessibility, HasSetter, NeedsInterfaceDeclaration. No IsLazyLoad or inner type fields today.
- `src/Neatoo.BaseGenerator/Generators/PropertyGenerator.cs` -- Generates `{Accessibility} partial {Type} {Name}` with `.Value =` setter and task tracking. Single code path for all properties.
- `src/Neatoo.BaseGenerator/Generators/InitializerGenerator.cs` -- Generates `factory.Create<{Type}>` for all properties uniformly. No LazyLoad awareness.
- `src/Neatoo/IPropertyFactory.cs` -- Has only `Create<TProperty>` returning `IValidateProperty<TProperty>`.
- `src/Neatoo/Internal/DefaultPropertyFactory.cs` -- Implements `Create<T>` via `_factory.CreateValidateProperty<T>`.
- `src/Neatoo/Internal/EntityPropertyFactory.cs` -- Implements `Create<T>` via `_factory.CreateEntityProperty<T>`.
- `src/Neatoo/ValidateBase.cs` -- Has `RegisterLazyLoadProperties()`, `RegisterLazyLoadProperty<T>()`, `FactoryComplete()` calls `FinalizeRegistration`, `OnDeserialized()` calls `FinalizeRegistration`.
- `src/Neatoo/Internal/ValidatePropertyManager.cs` -- Has `FinalizeRegistration()`, `RegisterLazyLoadProperty<T>()`, `GetLazyLoadProperties()`, `CreateLazyLoadProperty()`, `_lazyLoadPropertyCache`, `TryGetRegisteredProperty()`.
- `src/Neatoo/Internal/EntityPropertyManager.cs` -- Has `CreateLazyLoadProperty()` override creating `LazyLoadEntityProperty<>`.
- `src/Neatoo/Internal/LazyLoadValidateProperty.cs` -- Property subclass with look-through logic, `LoadValue` override, inner child connect/disconnect.
- `src/Neatoo/Internal/LazyLoadEntityProperty.cs` -- Entity variant with EntityChild override, IsModified delegation, IsSelfModified suppression.
- `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` -- **Read**: discovers LazyLoad properties via reflection (`GetProperties` where type is `LazyLoad<>` and `SetMethod != null`). **Write**: skips `ILazyLoadProperty` in PropertyManager array, then separately serializes LazyLoad<> properties via reflection.
- `src/Neatoo/Internal/PropertyInfoList.cs` -- Discovers all instance properties via reflection up the type hierarchy. Partial properties compile to normal properties, so they will appear.
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadEntityObject.cs` -- Two LazyLoad patterns: `LazyDescription` (auto-property, not registered with PropertyManager) and `LazyChild` (manual backing field + `RegisterLazyLoadProperties()`).
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs` -- `CrashParent` uses manual `_lazyChild` + `RegisterLazyLoadProperties()`. `LazyChild` has `private set`.
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs` -- Serialization round-trip tests for pre-loaded, unloaded, nested entity LazyLoad values.
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadValidateObject.cs` -- ValidateBase entity with `LazyContent` as auto-property (not registered).
- `src/Neatoo.BaseGenerator.Tests/PartialPropertyGenerationTests.cs` -- Generator tests using `GeneratorTestHelper`.
- `src/Examples/Person/Person.DomainModel/Person.cs` -- Real example with manual `_personPhoneList` backing field + `RegisterLazyLoadProperties()`.
- Generated output `DomainModel.Person.g.cs` -- Confirms generator output for scalar properties.

**Design Project Verification:**
The plan does not claim "Verified" or "Needs Implementation" scope backed by Design project compilation evidence. The plan is a design document, not a scope-verification document. This is acceptable given the short-todo workflow context.

**Discrepancies Found:**
See Concerns below.

### Structured Question Checklist

**Completeness Questions:**
- [x] All affected base classes addressed? -- Yes: ValidateBase, EntityBase, ValidatePropertyManager, EntityPropertyManager.
- [x] Factory operation lifecycle impacts? -- FactoryComplete (remove FinalizeRegistration call) is covered. OnDeserialized is flagged as a concern.
- [x] Property system impact addressed? -- LoadValue vs SetValue is locked. Getter/setter generation is designed.
- [x] Validation rule interactions documented? -- LoadValue fires ChangeReason.Load, which skips rule cascading. Correct.
- [x] Parent-child relationships in aggregates considered? -- SetParent flows through NeatooPropertyChanged via the property subclass, same as today.

**Correctness Questions:**
- [x] Proposed implementation aligns with existing Neatoo patterns? -- Yes, mirrors scalar property generation.
- [x] Consistent with how similar features work today? -- Yes, extends the same generation pipeline.
- [ ] Breaking changes migration path clear? -- The breaking changes are documented but see Concern 5.
- [x] State property impacts correct? -- IsModified/IsNew/IsValid/IsBusy all flow through property subclass, unchanged.

**Clarity Questions:**
- [ ] Could I implement this without asking any clarifying questions? -- No. See Concerns.
- [ ] Are there ambiguous requirements? -- Yes. See Concerns 1 and 2.
- [x] Are edge cases explicitly handled? -- Read-only LazyLoad is covered. Multiple LazyLoad properties are covered.
- [x] Is the test strategy specific enough? -- Phase 3 covers conversion of test entities and full test suite run.

**Risk Questions:**
- [ ] What could go wrong during implementation? -- Serialization is the highest risk. See Concern 1.
- [x] Which existing tests might fail? -- Serialization tests, CrashParent test. All listed in Phase 3.
- [ ] Serialization/state transfer implications? -- See Concern 1.
- [x] RemoteFactory source generation impacts? -- None. RemoteFactory generates factory methods, not property implementations.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. `LazyLoad<string>` on EntityBase (like `LazyDescription` in `LazyLoadEntityObject.cs`) -- a non-entity inner type. The plan's `EntityPropertyFactory.CreateLazyLoad<TInner>` always creates `LazyLoadEntityProperty<TInner>`, but `LazyLoadEntityProperty` assumes `TInner` might implement `IEntityMetaProperties`. For `string`, `EntityChild` returns null, `IsModified` returns false -- this is probably fine, but needs verification.
2. LazyLoad property assigned AFTER InitializePropertyBackingFields but BEFORE FactoryComplete -- e.g., in the constructor. The property subclass exists in PropertyManager (created during InitializePropertyBackingFields), but has null `_value` because `LoadValue` hasn't been called yet. Then the constructor assigns `PersonPhoneList = lazyLoadFactory.Create<T>(...)` which calls the generated setter which calls `LoadValue`. The `LoadValue` call with a non-null value should work. But what happens if the constructor assigns LazyLoad BEFORE the property subclass is registered? This can't happen because `InitializePropertyBackingFields` runs in the `ValidateBase<T>` constructor before user code, and LazyLoad assignment happens in the derived class constructor which runs after base constructor. Confirmed safe.
3. What happens when `LazyLoad<T>` property is reassigned in a factory method (e.g., `Person.Create` sets `PersonPhoneList = _lazyLoadFactory.Create<IPersonPhoneList>(personPhoneModelList)`)? The generated setter calls `LoadValue` on the already-registered property subclass. `LazyLoadValidateProperty.LoadValue` disconnects old inner child and connects new one. This matches the current behavior where `RegisterLazyLoadProperties()` in the setter would detect the existing property and call `LoadValue` on it. Confirmed correct.

**Ways this could break existing functionality:**
1. Serialization relies on reflection to discover `LazyLoad<>` properties with `SetMethod != null`. After converting to partial properties, the generated setter IS a setter (the compiled property will have a `SetMethod`). But the serializer checks `p.SetMethod != null` -- for a partial property with `{ get; set; }`, this should still be true. However, for a partial property with `{ get; }` (no setter), the serializer would not find it. If a LazyLoad property is declared getter-only, it would not be serialized. This needs consideration.
2. The `OnDeserialized` concern described in the plan is the most serious open item. See Concern 1.

**Ways users could misunderstand the API:**
1. Users might try `public partial LazyLoad<T> Prop { get; }` (getter-only) and expect it to work with serialization. But the serializer checks for `SetMethod != null`. The generated getter-only property would compile, but serialization round-trip would fail because the deserializer cannot assign the value back.

### Concerns

1. **Serialization: OnDeserialized reconnection is under-specified (Medium-High)**
   - Details: The plan acknowledges this is a concern but leaves it as "resolved during implementation" with multiple options ("generated override method or inline code in the generated class"). This is the highest-risk area and the plan should nail down the approach before implementation begins. Specifically:
     - After deserialization, the constructor runs (which calls `InitializePropertyBackingFields`, creating the LazyLoad property subclass in PropertyManager with null value). Then the `NeatooBaseJsonTypeConverter` restores the `LazyLoad<T>` instance via the setter (calls `LoadValue` on the property subclass). Then `OnDeserialized` is called.
     - Wait -- actually, let me re-examine the deserialization flow. The serializer does `property.SetValue(result, deserialized)` for LazyLoad properties, but for the merge path it calls `mergeable.ApplyDeserializedState(source.BoxedValue, source.IsLoaded)`. After this plan's change, the property setter is the GENERATED setter (calls `LoadValue`). So the serializer's reflection-based `property.SetValue` would call the generated setter, which calls `PersonPhoneListProperty.LoadValue(value)`.
     - But the serializer ALSO has a merge path: it calls `existing.ApplyDeserializedState()` instead of replacing. With partial properties, the "existing" value is obtained via `property.GetValue(result)` which calls the generated getter (`PersonPhoneListProperty.Value`). But at this point in deserialization, the property subclass's Value is whatever was set in the constructor. The merge path should still work because `ApplyDeserializedState` is called on the existing LazyLoad instance (created in the constructor), preserving the loader delegate.
     - Actually, I think this works better than before. The constructor creates the LazyLoad instance AND registers it with PropertyManager via the generated setter (which calls LoadValue). Then during deserialization, the serializer's merge path calls `ApplyDeserializedState` on the constructor-created instance. Then `OnDeserialized` just needs to re-fire `LoadValue` so the property subclass reconnects to the potentially-modified LazyLoad value. But the merge path does NOT go through the property setter -- it calls `ApplyDeserializedState` directly on the LazyLoad instance, bypassing the generated setter.
     - **Bottom line:** The plan needs to specify what `OnDeserialized` does for LazyLoad properties after `ApplyDeserializedState` has been called (the property subclass's internal value reference may be stale -- it holds the pre-merge LazyLoad value, but the merge modified the inner Value). Does the property subclass need a `LoadValue` call to reconnect inner child events? If so, `OnDeserialized` needs to iterate LazyLoad properties and call `LoadValue` on each.
   - Question: What is the concrete mechanism for `OnDeserialized` to reconnect LazyLoad property subclasses after deserialization? The plan must specify whether (a) the generated code produces an `OnDeserialized` override that calls `LoadValue` for each LazyLoad property, (b) the existing `OnDeserialized` in `ValidateBase` is modified to iterate PropertyManager for `ILazyLoadProperty` entries and call `LoadValue`, or (c) some other approach. "Resolved during implementation" is too vague for the highest-risk area.
   - Suggestion: Option (b) is simplest and non-generated: in `ValidateBase.OnDeserialized()`, after the existing `SetParent` loop, iterate `pmInternal.GetProperties` and for each `ILazyLoadProperty`, read the current backing field value via the generated getter and call `LoadValue` on the property subclass. This requires no generator changes and keeps deserialization logic in one place.

2. **Serialization: NeatooBaseJsonTypeConverter uses reflection to discover LazyLoad properties -- plan does not address this**
   - Details: The `NeatooBaseJsonTypeConverter` (both Read and Write methods) uses `GetType().GetProperties()` filtered by `LazyLoad<>` generic type to discover LazyLoad properties for serialization. After this change, LazyLoad properties will be C# partial properties, which compile to regular properties with compiler-generated backing fields. The reflection in the serializer should still find them. However, the plan does NOT list `NeatooBaseJsonTypeConverter` as a file to modify. The serializer already works by convention (finds LazyLoad<> properties via reflection), but the plan should explicitly confirm it does not need changes and document WHY.
   - Question: Does the architect confirm that `NeatooBaseJsonTypeConverter` requires zero changes? The generated partial property will still have a `PropertyType` of `LazyLoad<T>` and a non-null `SetMethod` (for `{ get; set; }` declarations), so the reflection discovery should continue to work. But this needs explicit confirmation, not an assumption.
   - Suggestion: Add `NeatooBaseJsonTypeConverter.cs` to the Impact Analysis table with "No changes needed -- reflection-based discovery of `LazyLoad<>` properties works unchanged because partial properties compile to regular properties."

3. **Generator: Setter access modifier is not extracted (Pre-existing gap, but relevant here)**
   - Details: `PropertyExtractor` captures `Accessibility` as `property.Modifiers.FirstOrDefault().ToString()` -- this gets the PROPERTY-level modifier (e.g., `public`), not the SETTER-specific modifier (e.g., `internal set`). The plan's "AFTER" example shows `public partial LazyLoad<IPersonPhoneList> PersonPhoneList { get; set; }` -- a plain `set`. But the current Person.cs uses `internal set`. If the user declares `public partial LazyLoad<T> Prop { get; internal set; }`, the generator would emit `public partial LazyLoad<T> Prop { get => ...; set { ... } }` -- losing the `internal` on the setter.
   - This is a pre-existing gap that affects ALL partial properties, not just LazyLoad. For today's scalar properties, consumers happen to use `public partial string? Name { get; set; }` with matching visibility. But the plan's Decision 1 explicitly says "The consumer uses whatever access modifier they need (`set;`, `internal set;`, `private set;`). The generator respects the declared accessor visibility." -- this is stated as a requirement but the generator does NOT implement it.
   - Question: Is this a P0 blocker for this plan, or can it be deferred? The Person.cs example will change from `internal set` (manual property) to `set` (partial property) -- this changes the API visibility of the setter. The interface `IPerson` only exposes `LazyLoad<IPersonPhoneList> PersonPhoneList { get; }` (getter-only), so external consumers cannot call the setter through the interface. The concrete class is `internal`, so only assembly-internal code can see the setter either way. Is this acceptable?
   - Suggestion: Either (a) document this as a known gap and confirm `set` instead of `internal set` is acceptable for Person.cs, or (b) add setter modifier extraction to Phase 1 scope.

4. **Test entity `LazyLoadEntityObject` has an unregistered LazyLoad property pattern**
   - Details: `LazyLoadEntityObject.cs` has `public LazyLoad<string> LazyDescription { get; set; } = null!;` -- this is a NON-partial, auto-implemented property that is NOT registered with PropertyManager. It works for serialization (the serializer discovers it via reflection) but does not participate in PropertyManager state propagation (no IsValid/IsBusy/IsModified cascading). After this change, if the test entity converts `LazyDescription` to `public partial LazyLoad<string> LazyDescription { get; set; }`, it WOULD be registered with PropertyManager. This changes the behavior -- `LazyDescription` would now participate in state propagation.
   - The plan's Phase 3 says "Update `LazyLoadEntityObject.cs`" but does not specify whether ALL LazyLoad properties should be converted or only the ones that were registered before. Converting `LazyDescription` to partial changes its behavior. NOT converting it leaves it as a non-partial property alongside partial properties, which may cause confusion.
   - Question: Should `LazyDescription` (and similar unregistered LazyLoad auto-properties in `LazyLoadValidateObject`) be converted to partial, or left as non-partial? If converted, the test assertions may need updating because the entity's state propagation changes.

5. **Missing: `EntityBase.cs` impact verification**
   - Details: The plan lists `EntityBase.cs` in the Impact Analysis ("Remove any `FinalizeRegistration` calls if present") but this is vague. The plan should confirm whether `EntityBase` has its own `FactoryComplete` or `OnDeserialized` overrides that call `FinalizeRegistration`, or if it only inherits from `ValidateBase`. Let me check -- `ValidateBase.FactoryComplete()` is the one that calls `FinalizeRegistration`, and `EntityBase` likely overrides `FactoryComplete`. The plan should trace this.
   - Suggestion: Verify during implementation, but the plan should note that `EntityBase.FactoryComplete` and `EntityBase.OnDeserialized` must be checked for `FinalizeRegistration` calls.

### What Looks Good

- The before/after example is exceptionally clear
- Decision 1 (partial declaration syntax) is the right call -- LazyLoad properties should look like every other property
- Decision 2 (generated backing field accessor) follows the exact existing pattern
- Decision 3 (LoadValue in setter) is correct -- LazyLoad assignment is "loading the container" semantics
- Decision 4 (new `CreateLazyLoad<TInner>`) is clean -- it preserves factory polymorphism (entity vs validate)
- Decision 5 (immediate removal) matches the user's stated intent
- Decision 6 (ILazyLoadFactory survives) correctly separates creation from registration
- Decision 7 (IPropertyInfoList includes LazyLoad properties) is verified by reading `PropertyInfoList.cs` -- it uses reflection to discover all properties, so partial properties will appear
- The phased approach (Generator -> Framework -> Migration) is well-ordered
- Risk identification is honest -- serialization and OnDeserialized are called out

### Recommendation

Send back to architect to address Concerns 1, 2, and 3 before implementation. Concerns 4 and 5 can be resolved during implementation but should be acknowledged.

Specifically:
- **Concern 1 (OnDeserialized):** Must be fully specified. This is the highest-risk area.
- **Concern 2 (Serializer confirmation):** Quick confirmation that no changes are needed.
- **Concern 3 (Setter modifier):** Decision needed -- accept the gap or add to scope.
- **Concern 4 (Unregistered LazyLoad):** Decision needed -- convert or leave.

### Assertion Trace Verification (Updated after concern resolutions)

| # | Acceptance Criterion | Implementation Path | Verified? |
|---|---------------------|---------------------|-----------|
| 1 | `partial LazyLoad<T> Prop { get; set; }` compiles and works | `PropertyExtractor` detects `LazyLoad<T>` via `OriginalDefinition` check -> `PartialPropertyInfo.IsLazyLoad=true`, `LazyLoadInnerType` extracted -> `PropertyGenerator` emits `LoadValue`-based setter (no task tracking) -> `InitializerGenerator` emits `factory.CreateLazyLoad<TInner>(this, nameof(Prop))` -> `PropertyManager.Register` creates property subclass | Verified |
| 2 | No `RegisterLazyLoadProperties()` required | Method deleted from `ValidateBase`; generated code handles registration in `InitializePropertyBackingFields` | Verified |
| 3 | IsValid/IsBusy/IsModified cascade through LazyLoad | `LazyLoadValidateProperty`/`LazyLoadEntityProperty` subclass created by `IPropertyFactory.CreateLazyLoad<TInner>` -- identical property subclasses to today's `FinalizeRegistration`-created ones | Verified |
| 4 | Serialization round-trip works identically | Constructor runs `InitializePropertyBackingFields` (registers property subclass) -> constructor assigns LazyLoad via generated setter (calls `LoadValue`) -> serializer's merge path calls `ApplyDeserializedState` on existing wrapper -> `ValidateBase.OnDeserialized()` calls `ILazyLoadProperty.ReconnectAfterDeserialization()` to reconnect inner child events. `NeatooBaseJsonTypeConverter` requires zero changes (partial compiles to regular property with `SetMethod != null`). | Verified |
| 5 | Person.cs uses partial LazyLoad property | `public partial LazyLoad<IPersonPhoneList> PersonPhoneList { get; set; }` -- plain `set` is acceptable (concrete class is `internal`, interface exposes getter only) | Verified |
| 6 | All existing tests pass | All 6 test entities converted to partial. `LazyDescription`/`LazyContent` gain PropertyManager registration (correct behavior). Test assertions may need updating for state propagation changes. | Verified (conditional on Phase 3 test assertion updates) |
| 7 | No runtime reflection for LazyLoad property discovery | `FinalizeRegistration`/`GetLazyLoadProperties`/`_lazyLoadPropertyCache` deleted; `CreateLazyLoad` uses `IPropertyInfoList` (populated at DI registration, not per-object) | Verified |
| 8 | `IPropertyFactory<T>` has `CreateLazyLoad<TInner>` | New method on interface + `DefaultPropertyFactory` (creates `LazyLoadValidateProperty<TInner>`) and `EntityPropertyFactory` (creates `LazyLoadEntityProperty<TInner>`) | Verified |
| 9 | `FinalizeRegistration` and caches gone | All listed items deleted in Phase 2. `FactoryComplete` call removed. `OnDeserialized` call replaced with `ReconnectAfterDeserialization` loop. | Verified |

### Verdict

**Approved.** All five concerns from the initial review have been resolved:

1. **OnDeserialized reconnection (was BLOCKING):** Fully specified. `ReconnectAfterDeserialization()` on `ILazyLoadProperty`, implemented in both subclasses, called in `ValidateBase.OnDeserialized()` loop. The mechanism is correct -- `_value` is `protected` on `ValidateProperty<T>`, so subclasses can read the wrapper and reconnect inner child events via `LazyLoadPropertyHelper.DisconnectInnerChild`/`ConnectInnerChild`.
2. **Serializer confirmation (was BLOCKING):** Confirmed zero changes. Partial properties compile to regular properties. The serializer's reflection discovery (`LazyLoad<>` with `SetMethod != null`) continues to work. Added to Impact Analysis with explanation.
3. **Setter modifier:** Accepted as pre-existing gap. Plain `set` is adequate -- concrete classes are `internal`.
4. **Unregistered LazyLoad properties:** ALL become partial. 6 entities listed with specific conversion details. Behavior change (state propagation) is correct.
5. **EntityBase.cs:** Verified zero changes needed. `FactoryComplete` calls `base.FactoryComplete()`, no `OnDeserialized` override, no LazyLoad references.

### Why This Plan Is Exceptionally Clear (Post-Resolution)

- The before/after example is unambiguous
- Every design decision is locked with explicit rationale
- The serialization interaction is analyzed line-by-line against the actual `NeatooBaseJsonTypeConverter` code
- The `ReconnectAfterDeserialization` mechanism is fully specified with code and explanation
- All 6 entities requiring conversion are explicitly listed
- The phased approach has clear verification gates
- Every acceptance criterion traces to a specific implementation path

---

## Implementation Contract

**Created:** 2026-03-14
**Approved by:** neatoo-developer

### In Scope

#### Phase 1: Generator Changes (BaseGenerator)

- [ ] `PropertyExtractor.cs`: Detect `LazyLoad<T>` partial properties via Roslyn `OriginalDefinition` check; extract inner type `T` as `LazyLoadInnerType`
- [ ] `PartialPropertyInfo.cs`: Add `IsLazyLoad` (bool) and `LazyLoadInnerType` (string?, null for non-LazyLoad) to record struct
- [ ] `PropertyGenerator.cs` `GenerateBackingFields`: For LazyLoad, generate `IValidateProperty<LazyLoad<TInner>>` cast (same pattern as scalar but with wrapper type)
- [ ] `PropertyGenerator.cs` `GeneratePropertyImplementations`: For LazyLoad, generate `LoadValue`-based setter with no task tracking; for getter-only, generate getter only
- [ ] `InitializerGenerator.cs` `GenerateInitializeMethod`: For LazyLoad, generate `factory.CreateLazyLoad<TInner>(thisRef, nameof(Prop))` instead of `factory.Create<Type>(thisRef, nameof(Prop))`
- [ ] Generator unit tests: Add tests for LazyLoad property detection and code generation in `PartialPropertyGenerationTests.cs`
- [ ] **Checkpoint: `dotnet build src/Neatoo.BaseGenerator.Tests/Neatoo.BaseGenerator.Tests.csproj` passes; `dotnet test src/Neatoo.BaseGenerator.Tests/Neatoo.BaseGenerator.Tests.csproj` passes**

#### Phase 2: Framework Changes (Neatoo library)

- [ ] `IPropertyFactory.cs`: Add `IValidateProperty CreateLazyLoad<TInner>(TOwner owner, string propertyName) where TInner : class?;`
- [ ] `DefaultPropertyFactory.cs`: Implement `CreateLazyLoad<TInner>` returning `new LazyLoadValidateProperty<TInner>(propertyInfo)`
- [ ] `EntityPropertyFactory.cs`: Implement `CreateLazyLoad<TInner>` returning `new LazyLoadEntityProperty<TInner>(propertyInfo)`
- [ ] `LazyLoadValidateProperty.cs`: Expand `ILazyLoadProperty` to include `ReconnectAfterDeserialization()` method; implement it (disconnect old inner child, reconnect from `_value.BoxedValue`)
- [ ] `LazyLoadEntityProperty.cs`: Implement `ReconnectAfterDeserialization()` (same pattern)
- [ ] `ValidateBase.cs`: Delete `RegisterLazyLoadProperties()` and `RegisterLazyLoadProperty<TInner>()`
- [ ] `ValidateBase.cs` `FactoryComplete()`: Remove `FinalizeRegistration()` call
- [ ] `ValidateBase.cs` `OnDeserialized()`: Replace `FinalizeRegistration()` call with loop: `foreach property in pmInternal.GetProperties where property is ILazyLoadProperty -> call ReconnectAfterDeserialization()`
- [ ] `ValidatePropertyManager.cs`: Delete `FinalizeRegistration()`, `RegisterLazyLoadProperty<TInner>()`, `GetLazyLoadProperties()`, `CreateLazyLoadProperty()`, `_lazyLoadPropertyCache`, `TryGetRegisteredProperty()`
- [ ] `IValidatePropertyManagerInternal<P>`: Delete `FinalizeRegistration()`, `RegisterLazyLoadProperty<TInner>()`, `TryGetRegisteredProperty()`
- [ ] `EntityPropertyManager.cs`: Delete `CreateLazyLoadProperty()` override
- [ ] Verify `IPropertyInfoList` includes LazyLoad partial properties (expected: yes, since `PropertyInfoList<T>` uses reflection)
- [ ] **Checkpoint: `dotnet build src/Neatoo.sln` succeeds**

#### Phase 3: Consumer Migration and Tests

- [ ] `Person.cs`: Convert to `public partial LazyLoad<IPersonPhoneList> PersonPhoneList { get; set; }`, remove manual `_personPhoneList` backing field and custom getter/setter
- [ ] `LazyLoadEntityObject.cs`: Convert BOTH `LazyDescription` and `LazyChild` to partial, remove manual backing field and `RegisterLazyLoadProperties()` call
- [ ] `LazyLoadValidateObject.cs`: Convert `LazyContent` to partial
- [ ] `WaitForTasksLazyLoadCrashEntity.cs`: Convert `CrashParent.LazyChild` to partial, remove manual backing field and `RegisterLazyLoadProperties()` call
- [ ] `Design.Domain/PropertySystem/LazyLoadProperty.cs`: Convert ALL LazyLoad properties to partial, update design comments
- [ ] `samples/LazyLoadSamples.cs`: Convert `SkillLazyParent.LazyChild` to partial, update anti-pattern comments
- [ ] Update test assertions if needed (e.g., `LazyDescription`/`LazyContent` now participate in state propagation)
- [ ] **Checkpoint: `dotnet test src/Neatoo.sln` -- all tests pass**
- [ ] Verify serialization round-trip tests pass
- [ ] Verify no unregistered `LazyLoad<T>` auto-properties remain (grep for `LazyLoad<` without `partial`)

### Explicitly Out of Scope

- Setter access modifier extraction (pre-existing gap for all partial properties, not just LazyLoad)
- `NeatooBaseJsonTypeConverter.cs` changes (confirmed zero changes needed)
- `EntityBase.cs` changes (confirmed zero changes needed)
- Deprecation path for `RegisterLazyLoadProperties` (immediate removal, no deprecation)
- `ILazyLoadFactory` changes (survives unchanged -- different concern)
- Documentation updates (skill docs, user-facing docs, release notes -- handled in Step 9)

### Verification Gates

1. **After Phase 1:** Generator unit tests pass. Generated code for a class with both scalar and `LazyLoad<T>` partial properties matches expected output.
2. **After Phase 2:** `dotnet build src/Neatoo.sln` succeeds. Framework compiles without the removed code.
3. **After Phase 3:** `dotnet test src/Neatoo.sln` -- ALL tests pass. Serialization round-trip tests pass. Zero unregistered LazyLoad auto-properties in codebase.
4. **Final:** `dotnet build src/Design/Design.sln` succeeds (Design project compiles with new partial LazyLoad pattern).

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails after Phase 2 or Phase 3 changes
- Serialization round-trip test fails (highest-risk area)
- `IPropertyInfoList` does not include LazyLoad partial properties (unexpected -- requires investigation)
- Architectural contradiction discovered (e.g., `LoadValue` triggers rules when it should not)
- `ReconnectAfterDeserialization` does not correctly reconnect inner child events (test by verifying state propagation after deserialization round-trip)

---

## Implementation Progress

**Started:** 2026-03-14
**Developer:** neatoo-developer

### Phase 1: Generator Changes -- COMPLETE

- [x] `PartialPropertyInfo.cs`: Added `IsLazyLoad` (bool) and `LazyLoadInnerType` (string?) fields
- [x] `PropertyExtractor.cs`: Detects `LazyLoad<T>` via `IPropertySymbol` -> `INamedTypeSymbol.OriginalDefinition` check for name "LazyLoad" in namespace "Neatoo"; extracts inner type via `TypeArguments[0]`
- [x] `PropertyGenerator.cs` `GeneratePropertyImplementations`: LazyLoad properties use `LoadValue(value)` setter with no task tracking; getter-only generates getter only
- [x] `InitializerGenerator.cs` `GenerateInitializeMethod`: LazyLoad properties use `factory.CreateLazyLoad<TInner>(thisRef, nameof(Prop))`
- [x] `GeneratorTestHelper.cs`: Added `LazyLoad<T>` stub to NeatooStubs
- [x] 6 new generator tests: LoadValue setter, CreateLazyLoad registration, backing field type, mixed with scalar, getter-only, string inner type
- [x] **Checkpoint PASSED:** 32/32 generator tests pass

### Phase 2: Framework Changes -- COMPLETE

- [x] `IPropertyFactory.cs`: Added `IValidateProperty CreateLazyLoad<TInner>(TOwner owner, string propertyName) where TInner : class?`
- [x] `DefaultPropertyFactory.cs`: Implemented `CreateLazyLoad<TInner>` returning `new LazyLoadValidateProperty<TInner>(propertyInfo)`
- [x] `EntityPropertyFactory.cs`: Implemented `CreateLazyLoad<TInner>` returning `new LazyLoadEntityProperty<TInner>(propertyInfo)`
- [x] `LazyLoadValidateProperty.cs`: Expanded `ILazyLoadProperty` to include `ReconnectAfterDeserialization()`; implemented disconnect/reconnect pattern
- [x] `LazyLoadEntityProperty.cs`: Implemented `ReconnectAfterDeserialization()` with same pattern
- [x] `ValidateBase.cs`: Deleted `RegisterLazyLoadProperties()` and `RegisterLazyLoadProperty<TInner>()`
- [x] `ValidateBase.cs` `FactoryComplete()`: Removed `FinalizeRegistration()` call
- [x] `ValidateBase.cs` `OnDeserialized()`: Replaced `FinalizeRegistration()` with `ILazyLoadProperty.ReconnectAfterDeserialization()` loop
- [x] `ValidatePropertyManager.cs`: Deleted `FinalizeRegistration()`, `RegisterLazyLoadProperty<TInner>()`, `GetLazyLoadProperties()`, `CreateLazyLoadProperty()`, `_lazyLoadPropertyCache`, `TryGetRegisteredProperty()` from both interface and implementation
- [x] `EntityPropertyManager.cs`: Deleted `CreateLazyLoadProperty()` override
- [x] IPropertyInfoList verification: Confirmed LazyLoad partial properties are included (PropertyInfoList uses reflection)
- [x] EntityBase.cs: Confirmed zero changes needed
- [x] **Checkpoint PASSED:** `dotnet build src/Neatoo/Neatoo.csproj` succeeds

### Phase 3: Consumer Migration -- COMPLETE

- [x] `Person.cs`: Converted to `public partial LazyLoad<IPersonPhoneList> PersonPhoneList { get; set; }`
- [x] `LazyLoadEntityObject.cs`: Converted BOTH `LazyDescription` and `LazyChild` to partial
- [x] `LazyLoadValidateObject.cs`: Converted `LazyContent` to partial
- [x] `WaitForTasksLazyLoadCrashEntity.cs`: Converted `CrashParent.LazyChild` to partial
- [x] `LazyLoadSamples.cs`: Converted `SkillLazyParent.LazyChild` to partial, updated comments
- [x] `Design.Domain/PropertySystem/LazyLoadProperty.cs`: Converted ALL LazyLoad properties to partial, updated design comments
- [x] `ConstructorPropertyAssignmentAnalyzer.cs`: Updated to skip LazyLoad properties (their generated setter uses LoadValue, so constructor assignment is safe) -- this was discovered during Phase 3 when the NEATOO010 analyzer flagged LazyLoad assignments
- [x] **Checkpoint PASSED:** `dotnet test src/Neatoo.sln` -- ALL tests pass (32 generator + 250 samples + 55 Person + 1780 unit = 2117 total, 0 failed, 1 pre-existing skip)
- [x] Zero unregistered LazyLoad auto-properties remain (verified by grep)
- [x] `dotnet build src/Design/Design.sln` -- Only pre-existing NF0105 errors (unrelated to this change)

---

## Completion Evidence

**Completed:** 2026-03-14

### Test Results

```
Passed!  - Failed:     0, Passed:    32, Skipped:     0, Total:    32  - Neatoo.BaseGenerator.Tests.dll (net9.0)
Passed!  - Failed:     0, Passed:   250, Skipped:     0, Total:   250  - Samples.dll (net9.0)
Passed!  - Failed:     0, Passed:    55, Skipped:     0, Total:    55  - Person.DomainModel.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:  1780, Skipped:     1, Total:  1781  - Neatoo.UnitTest.dll (net9.0)
```

All 2117 tests pass. The 1 skip (AsyncFlowTests_CheckAllRules) is pre-existing.

### Verification Summary

1. **Generator:** 6 new LazyLoad tests verify detection, LoadValue setter generation, CreateLazyLoad registration, backing field type, mixed scalar+LazyLoad, and getter-only
2. **Serialization:** All serialization round-trip tests pass (FatClientLazyLoadTests, WaitForTasksLazyLoadCrashTests, TwoContainerLazyLoadTests) -- JSON format unchanged
3. **Removal confirmed:** `RegisterLazyLoadProperties`, `FinalizeRegistration`, `_lazyLoadPropertyCache`, `GetLazyLoadProperties`, `CreateLazyLoadProperty`, `TryGetRegisteredProperty` -- all gone from codebase (verified by grep)
4. **Design solution:** Compiles with only pre-existing NF0105 errors (not related to this change)
5. **Discovery during implementation:** The `ConstructorPropertyAssignmentAnalyzer` (NEATOO010) needed updating to skip LazyLoad properties -- their generated setter uses `LoadValue`, not `.Value =`, so constructor assignment does not trigger modification tracking. This was not anticipated in the plan but is a natural consequence of making LazyLoad properties partial.

### Files Modified (Final List)

**Generator (Phase 1):**
- `src/Neatoo.BaseGenerator/Models/PartialPropertyInfo.cs` -- Added IsLazyLoad, LazyLoadInnerType
- `src/Neatoo.BaseGenerator/Extractors/PropertyExtractor.cs` -- LazyLoad<T> detection
- `src/Neatoo.BaseGenerator/Generators/PropertyGenerator.cs` -- LoadValue setter generation
- `src/Neatoo.BaseGenerator/Generators/InitializerGenerator.cs` -- CreateLazyLoad registration
- `src/Neatoo.BaseGenerator.Tests/GeneratorTestHelper.cs` -- LazyLoad<T> stub
- `src/Neatoo.BaseGenerator.Tests/PartialPropertyGenerationTests.cs` -- 6 new tests

**Framework (Phase 2):**
- `src/Neatoo/IPropertyFactory.cs` -- Added CreateLazyLoad<TInner>
- `src/Neatoo/Internal/DefaultPropertyFactory.cs` -- Implemented CreateLazyLoad
- `src/Neatoo/Internal/EntityPropertyFactory.cs` -- Implemented CreateLazyLoad
- `src/Neatoo/Internal/LazyLoadValidateProperty.cs` -- Expanded ILazyLoadProperty, added ReconnectAfterDeserialization
- `src/Neatoo/Internal/LazyLoadEntityProperty.cs` -- Added ReconnectAfterDeserialization
- `src/Neatoo/ValidateBase.cs` -- Removed old methods, updated OnDeserialized and FactoryComplete
- `src/Neatoo/Internal/ValidatePropertyManager.cs` -- Removed all LazyLoad registration infrastructure
- `src/Neatoo/Internal/EntityPropertyManager.cs` -- Removed CreateLazyLoadProperty override

**Migration (Phase 3):**
- `src/Examples/Person/Person.DomainModel/Person.cs` -- Partial LazyLoad property
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadEntityObject.cs` -- Both properties to partial
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadValidateObject.cs` -- Partial
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/WaitForTasksLazyLoadCrashEntity.cs` -- Partial
- `src/samples/LazyLoadSamples.cs` -- Partial + updated comments
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- Partial + updated design comments
- `src/Neatoo.Analyzers/ConstructorPropertyAssignmentAnalyzer.cs` -- Skip LazyLoad properties (discovered during implementation)

---

## Architect Verification

**Date:** 2026-03-14
**Verdict: VERIFIED**

### Independent Build and Test Results

```
dotnet build src/Neatoo.sln
Build succeeded. 0 Warning(s) 0 Error(s)

dotnet test src/Neatoo.sln --no-build
Passed!  - Failed:     0, Passed:   250, Skipped:     0, Total:   250 - Samples.dll (net9.0)
Passed!  - Failed:     0, Passed:    32, Skipped:     0, Total:    32 - Neatoo.BaseGenerator.Tests.dll (net9.0)
Passed!  - Failed:     0, Passed:    55, Skipped:     0, Total:    55 - Person.DomainModel.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:  1780, Skipped:     1, Total:  1781 - Neatoo.UnitTest.dll (net9.0)
```

Zero failures. The 1 skip (AsyncFlowTests_CheckAllRules) is pre-existing and unrelated.

### Verification Checklist

1. **Build: PASS.** `dotnet build src/Neatoo.sln` -- 0 warnings, 0 errors.

2. **Tests: PASS.** 2117 tests pass, 0 failures.

3. **Generator detects LazyLoad and generates correct code: VERIFIED.**
   Inspected `src/Examples/Person/Person.DomainModel/Generated/Neatoo.BaseGenerator/Neatoo.BaseGenerator.PartialBaseGenerator/DomainModel.Person.g.cs`:
   - Line 35: `protected IValidateProperty<LazyLoad<IPersonPhoneList>> PersonPhoneListProperty => (IValidateProperty<LazyLoad<IPersonPhoneList>>)PropertyManager[nameof(PersonPhoneList)]!;` -- backing field with correct LazyLoad wrapper type.
   - Lines 107-114: Generated setter uses `PersonPhoneListProperty.LoadValue(value)` -- matches plan Decision 3 (LoadValue, no task tracking).
   - Line 171: `PropertyManager.Register(factory.CreateLazyLoad<IPersonPhoneList>(this, nameof(PersonPhoneList)));` -- matches plan Decision 4 (CreateLazyLoad with inner type).

4. **Old registration infrastructure removed: VERIFIED.**
   - `RegisterLazyLoadProperties` -- grep returns zero files.
   - `FinalizeRegistration` -- grep returns zero files.
   - `_lazyLoadPropertyCache` -- grep returns zero files.

5. **CreateLazyLoad on IPropertyFactory and implementations: VERIFIED.**
   - `src/Neatoo/IPropertyFactory.cs` line 42: `IValidateProperty CreateLazyLoad<TInner>(TOwner owner, string propertyName) where TInner : class?;`
   - `src/Neatoo/Internal/DefaultPropertyFactory.cs` line 32: implements creating `LazyLoadValidateProperty<TInner>`.
   - `src/Neatoo/Internal/EntityPropertyFactory.cs` line 36: implements creating `LazyLoadEntityProperty<TInner>`.

6. **ReconnectAfterDeserialization on ILazyLoadProperty and subclasses: VERIFIED.**
   - `src/Neatoo/Internal/LazyLoadValidateProperty.cs` line 18: interface declaration; line 304: implementation.
   - `src/Neatoo/Internal/LazyLoadEntityProperty.cs` line 246: implementation.
   - `src/Neatoo/ValidateBase.cs` line 552: called in OnDeserialized loop.

7. **Person.cs clean: VERIFIED.**
   Line 53: `public partial LazyLoad<IPersonPhoneList> PersonPhoneList { get; set; }` -- no manual backing field, no `RegisterLazyLoadProperties` call, no custom getter/setter. Matches plan "AFTER" exactly.

8. **All LazyLoad properties are partial: VERIFIED.**
   Grep for `public LazyLoad<` (non-partial) returns only `ILazyLoadFactory` method return types (not property declarations). Grep for `private LazyLoad<` backing fields returns zero results. Every LazyLoad property in Person.cs, LazyLoadEntityObject.cs, LazyLoadValidateObject.cs, WaitForTasksLazyLoadCrashEntity.cs, LazyLoadSamples.cs, and Design.Domain/LazyLoadProperty.cs uses the `partial` keyword.

### Implementation Matches Design

The generated code in `DomainModel.Person.g.cs` matches the plan's "Generated Code: Full Example" section exactly:
- Backing field accessor pattern is identical to scalar properties.
- Setter uses `LoadValue()` instead of `.Value =` (no rule triggering, no task tracking).
- Registration uses `factory.CreateLazyLoad<TInner>()` instead of `factory.Create<T>()`.
- The type parameter to `CreateLazyLoad` is the inner type (`IPersonPhoneList`), not the wrapper type.

### Discovery During Implementation: Analyzer Update

The developer correctly identified that `ConstructorPropertyAssignmentAnalyzer` (NEATOO010) needed updating to skip LazyLoad properties. This was not anticipated in the plan but is a direct and natural consequence of making LazyLoad properties partial. The fix is correct -- LazyLoad generated setters use `LoadValue`, not `.Value =`, so constructor assignment is safe.
