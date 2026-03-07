# Generated Property Backing Fields

**Status:** Complete
**Priority:** High
**Created:** 2026-01-15

---

## Problem

Current property access uses string-based dictionary lookups:

```csharp
public string Name
{
    get => Getter<string>();  // Dictionary lookup + cast every access
    set => Setter(value);     // Dictionary lookup + CallerMemberName magic
}
```

This has performance overhead, loses type safety, and makes advanced features like lazy loading awkward to configure (string-based access to `PropertyManager["Name"]`).

## Solution

Generate strongly-typed `Property<T>` backing fields for each Neatoo property, initialized via DI-friendly `IPropertyFactory<TOwner>`:

```csharp
// Generated partial
public partial class Person
{
    protected Property<string> NameProperty { get; private set; } = null!;
    protected Property<PersonPhoneList?> PhonesProperty { get; private set; } = null!;

    protected override void InitializePropertyBackingFields(IPropertyFactory<Person> factory)
    {
        NameProperty = factory.Create<string>(this, nameof(Name));
        PhonesProperty = factory.Create<PersonPhoneList?>(this, nameof(Phones));

        PropertyManager.Register(NameProperty);
        PropertyManager.Register(PhonesProperty);
    }
}

// User writes
public string Name
{
    get => NameProperty.Value;
    set => NameProperty.Value = value;
}

public PersonPhoneList? Phones
{
    get => PhonesProperty.Value;
    set => PhonesProperty.Value = value;
}
```

**Lazy loading configured in constructor:**
```csharp
public Person(
    IEntityBaseServices<Person> services,  // Bundles IPropertyFactory<Person>
    IPhoneDbContext context,
    IPersonPhoneListFactory factory)
    : base(services)
{
    PhonesProperty.OnLoad = async () =>
    {
        var phones = await context.LoadPhones(this.Id);
        return factory.Fetch(phones);
    };
}
```

---

## Design Decisions

### Property Access Pattern

**Getter returns value directly:**
```csharp
get => NameProperty.Value;
```

**Setter assigns value (triggers rules, events):**
```csharp
set => NameProperty.Value = value;
```

### Lazy Loading Behavior

1. `Property<T>` has `Func<Task<T>>? OnLoad` configured in constructor
2. `Task? _onLoadTask` tracks if load was attempted
3. On getter access: if `Value == null && OnLoad != null && _onLoadTask == null` → fire-and-forget load
4. Load completion sets value and fires `PropertyChanged`
5. Load failure creates broken rule on property
6. Deserialization doesn't prevent client-side lazy loading (intentional)

### IsBusy Integration

- Loading state tracked via existing `AsyncTasks` infrastructure
- `Property<T>.IsBusy` reflects pending load
- Parent `IsBusy` aggregates via `PropertyManager.IsBusy` (already exists)

### Error Handling

- Failed loads surface as broken rules on the property
- `IsValid` becomes `false`
- UI displays error via existing validation patterns
- Retry possible by clearing error and re-triggering load

### Task Tracking (Key Insight)

Current `Setter()` does explicit task tracking:
```csharp
task = property.SetPrivateValue(value);
if (!task.IsCompleted)
{
    this.RunningTasks.AddTask(task);
}
```

With the new pattern, task tracking moves to the event handler:
```csharp
private Task _PropertyManager_NeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    // eventArgs.Property already contains the property reference
    // IValidateProperty.Task exposes the async task
    if (eventArgs.Property?.Task is Task task && !task.IsCompleted)
    {
        if (this.Parent is IValidateBaseInternal parentInternal)
        {
            parentInternal.AddChildTask(task);
        }
        this.RunningTasks.AddTask(task);
    }

    return this.ChildNeatooPropertyChanged(eventArgs);
}
```

This is cleaner - task tracking happens in response to the event, not in the setter.

---

## DI Architecture

### IPropertyFactory<TOwner>

Generic factory interface allows per-type DI registration:

```csharp
public interface IPropertyFactory<TOwner> where TOwner : IBase
{
    Property<TProperty> Create<TProperty>(TOwner owner, string propertyName);
    IPropertyManager CreatePropertyManager(TOwner owner);
}
```

**DI Registration:**
```csharp
// Default for all types (open generic)
services.AddSingleton(typeof(IPropertyFactory<>), typeof(DefaultPropertyFactory<>));

// Custom for specific type (optional)
services.AddSingleton<IPropertyFactory<Person>, CustomPersonPropertyFactory>();
```

### Integration with Services Interfaces

`IPropertyFactory<T>` added to existing services interfaces:

```csharp
public interface IValidateBaseServices<T> where T : IValidateBase
{
    IPropertyFactory<T> PropertyFactory { get; }
    // ... existing services
}

public interface IEntityBaseServices<T> : IValidateBaseServices<T> where T : IEntityBase
{
    // ... existing entity services
}
```

### Base Class Implementation

```csharp
public abstract class ValidateBase<T> where T : ValidateBase<T>
{
    protected IValidateBaseServices<T> Services { get; }
    public IPropertyManager PropertyManager { get; }

    public ValidateBase(IValidateBaseServices<T> services)
    {
        Services = services;
        PropertyManager = services.PropertyFactory.CreatePropertyManager((T)this);
        InitializePropertyBackingFields(services.PropertyFactory);
    }

    protected abstract void InitializePropertyBackingFields(IPropertyFactory<T> factory);
}
```

---

## Generated Code Specification

### For Each Property

Given user-defined property:
```csharp
public string Name
{
    get => NameProperty.Value;
    set => NameProperty.Value = value;
}
```

Generator creates:
```csharp
// In partial class
public partial class Person
{
    protected Property<string> NameProperty { get; private set; } = null!;

    protected override void InitializePropertyBackingFields(IPropertyFactory<Person> factory)
    {
        NameProperty = factory.Create<string>(this, nameof(Name));
        PropertyManager.Register(NameProperty);
    }
}
```

**No reflection** - Generator provides property name as string literal. Factory implementation can use that to build metadata without runtime reflection.

### PropertyManager Role

`PropertyManager` becomes:
- **Registry** for enumeration (`foreach (var prop in PropertyManager)`)
- **Name-based access** for serialization, debugging
- **No longer primary access path** for user code

---

## API Changes

### Property<T> New Members

```csharp
public class Property<T> : IProperty
{
    // Existing
    public T? Value { get; set; }
    public bool IsBusy { get; }
    public IEnumerable<BrokenRule> BrokenRules { get; }

    // New for lazy loading
    public Func<Task<T>>? OnLoad { get; set; }
    public bool IsLoaded { get; }
    public Task? LoadTask { get; }  // Expose for explicit await
    public Task<T?> LoadAsync();    // Explicit load trigger
}
```

### IProperty Interface Updates

```csharp
public interface IProperty
{
    // Existing
    object? Value { get; }
    bool IsBusy { get; }

    // New
    bool IsLoaded { get; }
    Task LoadAsync();
}
```

---

## Impact Analysis

### Files to Modify

**New Interfaces:**
- `src/Neatoo/IPropertyFactory.cs` - New `IPropertyFactory<TOwner>` interface

**Services Interfaces:**
- `src/Neatoo/IValidateBaseServices.cs` - Add `IPropertyFactory<T>` property
- `src/Neatoo/IEntityBaseServices.cs` - Inherits from above

**Core Property System:**
- `src/Neatoo/IProperty.cs` - Add lazy loading members
- `src/Neatoo/Core/Property.cs` - Implement lazy loading, DI-created
- `src/Neatoo/Core/PropertyManager.cs` - Simplify to registry/enumeration
- `src/Neatoo/Core/DefaultPropertyFactory.cs` - New default implementation

**Base Classes:**
- `src/Neatoo/Base.cs` - Remove `Getter<T>()`/`Setter()`
- `src/Neatoo/ValidateBase.cs` - Add `InitializePropertyBackingFields` abstract method, call in constructor
- `src/Neatoo/EditBase.cs` - Property tracking via typed properties
- `src/Neatoo/EntityBase.cs` - Same

**Generator:**
- `src/Neatoo/Generator/BaseGenerator.cs` - Generate property backing fields and `InitializePropertyBackingFields` override

**Tests:**
- All property-related tests need updating
- Add lazy loading tests

### What Stays the Same

- Rule system (may get typed property references as enhancement)
- Parent-child relationships
- Serialization (PropertyManager still exists for enumeration)
- IsBusy/IsValid/IsModified aggregation
- Event propagation

---

## Implementation Phases

### Phase 1: DI Infrastructure ✅

- [x] Create `IPropertyFactory<TOwner>` interface
- [x] Create `DefaultPropertyFactory<TOwner>` implementation
- [x] Add `IPropertyFactory<T>` to `IValidateBaseServices<T>`
- [x] Update `ValidateBaseServices<T>` implementation
- [x] Add abstract `InitializePropertyBackingFields` to `ValidateBase<T>`
- [x] Update `ValidateBase<T>` constructor to call initialization

### Phase 2: Property<T> Updates ✅

- [x] Update `Property<T>` to be DI-created (no internal `new()`)
- [x] Add `OnLoad`, `IsLoaded`, `LoadTask`, `LoadAsync()` to `Property<T>`
- [x] Implement fire-and-forget loading in value getter
- [x] Add broken rule on load failure
- [x] Wire into `IsBusy` tracking
- [x] Update `PropertyManager` to registry pattern (Register method)

### Phase 3: Generator - Property Backing Fields ✅

- [x] Analyze existing `BaseGenerator.cs` capabilities
- [x] Generate `protected IValidateProperty<T> {Name}Property` for each property
- [x] Generate `InitializePropertyBackingFields` override
- [x] Handle inheritance chain (call base, then initialize own properties)
- [x] Handle different property types (value types, reference types, collections)

### Phase 4: Update Existing Code ✅

- [x] Mark `Getter<T>()` as `[Obsolete]` (kept for nested private test classes)
- [x] Mark `Setter()` as `[Obsolete]` (kept for nested private test classes)
- [x] Task tracking in generated property setters
- [x] Update Person example to use new pattern
- [x] Update all integration tests
- [x] Verify serialization still works
- [x] Verify rules still trigger correctly

### Phase 5: Lazy Loading Integration Tests ✅

- [x] Test lazy load triggers on property access
- [x] Test load completion fires PropertyChanged
- [x] Test load failure creates broken rule
- [x] Test serialization with lazy properties (OnLoad not serialized by design)
- [x] Test concurrent access thread-safety
- [x] 14 integration tests added

### Phase 6: Documentation ✅

- [x] Update property-system.md with backing fields pattern
- [x] Add lazy loading documentation to property-system.md
- [x] Update IValidateProperty interface documentation

---

## Open Questions

1. **Keep Getter<T>/Setter as fallback?** - Remove entirely (no public release yet)
2. **Rule triggers** - Update to use typed `XProperty` references, or keep string-based?
3. **Collection properties** - Special handling for `EntityListBase<T>` properties?
4. **Inheritance** - How does `InitializePropertyBackingFields` work with inheritance chain?

---

## Progress Log

### 2026-01-15
- Brainstorming session identified opportunity to expand lazy loading into full property backing field redesign
- Core decisions made:
  - Generated `Property<T>` backing fields for all properties
  - Lazy loading via `OnLoad` func configured in constructor
  - Fire-and-forget pattern with PropertyChanged notification
  - Failed loads become broken rules
  - No special deserialization handling (client can retry)
- Created this plan document
- Refined DI architecture:
  - `IPropertyFactory<TOwner>` generic interface for per-type DI registration
  - Integrated into existing `IValidateBaseServices<T>` / `IEntityBaseServices<T>`
  - No runtime reflection - generator provides all metadata as literals
  - `InitializePropertyBackingFields(IPropertyFactory<T>)` called by base constructor
  - Consumers can implement custom `IPropertyFactory<T>` for full control
- Confirmed `Getter<T>()` and `Setter()` can both be removed:
  - New pattern: `get => NameProperty.Value;` and `set => NameProperty.Value = value;`
  - Task tracking moves from `Setter()` to `_PropertyManager_NeatooPropertyChanged`
  - `eventArgs.Property.Task` already available via `IValidateProperty.Task`
  - Rules still triggered via existing `NeatooPropertyChanged` event chain
- Properties now created upfront (not lazily) - this is a good change for predictability

### 2026-01-15 (continued)
- **Phase 1-3 Complete**: Property backing fields infrastructure implemented
- **Phase 4 Complete**:
  - Getter/Setter marked `[Obsolete]` instead of removed (nested private test classes need generator support)
  - All top-level classes converted to partial property pattern
  - 1698 unit tests pass
- **Phase 5 Complete**:
  - 14 lazy loading integration tests added
  - Fixed thread-safety issue in lazy load trigger (double-check locking pattern)
  - Updated `ValidateBase.WaitForTasks()` to also wait for `PropertyManager.WaitForTasks()` (lazy loads)
  - Tests cover: trigger on access, PropertyChanged events, error handling, concurrent access

---

## Results / Conclusions

### Implementation Summary

**Property Backing Fields Pattern:**
```csharp
// User writes (partial property):
public partial string Name { get; set; }

// Generator creates:
protected IValidateProperty<string> NameProperty =>
    (IValidateProperty<string>)PropertyManager[nameof(Name)]!;

public partial string Name
{
    get => NameProperty.Value;
    set
    {
        NameProperty.Value = value;
        if (!NameProperty.Task.IsCompleted)
        {
            Parent?.AddChildTask(NameProperty.Task);
            RunningTasks.AddTask(NameProperty.Task);
        }
    }
}
```

**Lazy Loading Pattern:**
```csharp
// In constructor, after base():
PhonesProperty.OnLoad = async () =>
{
    var phones = await context.LoadPhones(this.Id);
    return factory.Fetch(phones);
};
```

### Key Decisions

1. **Property backing fields as computed properties**: Instead of stored fields, backing fields are computed from PropertyManager. This ensures PropertyManager remains the source of truth.

2. **Getter/Setter kept as `[Obsolete]`**: Nested private test classes cannot use generated code (generator outputs at namespace level). Future work could add nested class support.

3. **Thread-safe lazy loading**: Double-check locking pattern prevents multiple concurrent load triggers.

4. **WaitForTasks includes lazy loads**: `ValidateBase.WaitForTasks()` now also awaits `PropertyManager.WaitForTasks()` to ensure lazy loads complete.

### All Phases Complete

All implementation phases have been completed:
- Infrastructure (IPropertyFactory, DefaultPropertyFactory)
- Lazy loading (OnLoad, IsLoaded, LoadAsync)
- Source generator updates (backing fields, InitializePropertyBackingFields)
- Code migration (partial properties, Getter/Setter obsolete)
- Integration tests (14 lazy loading tests)
- Documentation (property-system.md updated)
