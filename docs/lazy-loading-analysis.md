# Lazy Loading Analysis for Neatoo

> **Document Type: Technical Analysis**
>
> This is an internal technical analysis document exploring the feasibility of adding lazy loading to Neatoo. Lazy loading is **not currently implemented**. See [lazy-loading-pattern.md](lazy-loading-pattern.md) for the proposed design.

## Current State: No Lazy Loading Exists

The codebase has **no built-in lazy loading** for child objects. The `Getter<P>()` in `Base.cs:242-245` is synchronous and returns values directly from `PropertyManager`:

```csharp
protected virtual P? Getter<P>([CallerMemberName] string propertyName = "")
{
    return (P?)this.PropertyManager[propertyName]?.Value;
}
```

All child objects and related data are loaded eagerly when the parent object is created or fetched.

---

## Existing Infrastructure That Supports Async/Deferred Operations

The framework already has excellent foundational infrastructure for lazy loading:

### A. Async Task Tracking System (AsyncTasks.cs, Base.cs:115-310)

- Built-in `AsyncTasks` class that tracks running operations
- `IsBusy` property that aggregates busy state across object graph
- `AddChildTask()` method propagates async operations up the parent hierarchy
- `WaitForTasks()` method allows awaiting all pending operations

```csharp
// From Base.cs
public bool IsBusy => this.RunningTasks.IsRunning || this.PropertyManager.IsBusy;
public virtual void AddChildTask(Task task) { ... }
public virtual async Task WaitForTasks() { ... }
```

### B. Property-Level Async Operations (Property.cs:85-120)

- `SetValue()` and `SetPrivateValue()` return `Task` allowing async operations
- Properties track `IsBusy` state for pending operations
- `LoadValue()` method exists for silent value loading without triggering rules
- Event propagation system (`PassThruValueNeatooPropertyChanged`) handles child property changes

```csharp
// From Property.cs
public virtual Task SetValue(object? newValue)
public virtual Task SetPrivateValue(object? newValue, bool quietly = false)
public virtual void LoadValue(object? value) // Silent loading
public Task WaitForTasks() // Async operation tracking
```

### C. Pause/Resume Pattern (ValidateBase.cs:328-378)

- Objects can be paused to suppress rule execution and events
- Useful during bulk loading or initialization
- Allows batch operations without intermediate validation states

```csharp
using (customer.PauseAllActions())
{
    customer.Name = "John";
    customer.Email = "john@example.com";
} // Rules run automatically when disposed
```

### D. Factory Pattern Integration (EntityBase.cs:108-120, RemoteFactory)

- Factory operations already support async methods ([Insert], [Fetch], [Update], [Delete])
- FactoryStart/FactoryComplete callbacks pause/resume actions automatically
- Decorated methods with `[Service]` parameter injection enable dependency passing

```csharp
[Fetch]
public async Task<bool> Fetch([Service] IPersonDbContext personContext,
                              [Service] IPersonPhoneListFactory personPhoneModelListFactory)
{
    var personEntity = await personContext.FindPerson();
    this.PersonPhoneList = personPhoneModelListFactory.Fetch(personEntity.Phones);
    return true;
}
```

### E. Property-Child Relationship Management (Base.cs:206-221, Property.cs:143-205)

- Parent-child relationships tracked through `ISetParent` interface
- Automatic parent assignment when child is set
- Property change propagation through object graph
- Event bubbling system supports nested change notifications

---

## Implementation Options

### Option A: Generic Lazy<T> Wrapper Approach (Simpler)

Create a wrapper type that defers materialization:

```csharp
// New type: Lazy child reference
public Lazy<ChildObject> Child { get; set; }

// Current: Eager loading (all child data loaded)
public ChildObject Child { get; set; }
```

**Pros:**
- Minimal framework changes
- Application-level implementation
- Leverages existing C# `Lazy<T>` pattern

**Cons:**
- Requires manual `Value` property access to trigger loading
- Doesn't integrate with Neatoo's busy state system
- Less discovery/IDE support

### Option B: Deferred Load Attribute/Pattern (Recommended)

Add framework support for deferred properties:

```csharp
[DeferredLoad]
public ChildObject Child { get; set; }

// Property getter would check if loaded, trigger factory method if not
```

**Required Changes:**

1. **Create IPropertyLoader Interface:**
   ```csharp
   public interface IPropertyLoader<T>
   {
       Task<T> LoadAsync();
       bool IsLoaded { get; }
   }
   ```

2. **Extend Property<T> Class:**
   - Add `IPropertyLoader<T>` support
   - Check `IsLoaded` on getter access
   - Trigger loading on first access
   - Mark property as `IsBusy` during load

3. **Add Factory Methods:**
   - [Load] attribute for lazy load methods
   - Receives parent object + other dependencies
   - Signature: `public async Task<T> LoadChild([Service] IContext context)`

4. **Integrate with Property Access:**
   ```csharp
   // From Setter/Getter
   public virtual P? Getter<P>()
   {
       var property = this.PropertyManager[propertyName];
       if (property is IPropertyWithDeferredLoad deferred && !deferred.IsLoaded)
       {
           return (P?)deferred.EnsureLoaded().Result; // or await
       }
       return (P?)property?.Value;
   }
   ```

5. **Update PropertyManager:**
   - PropertyManager creates properties with loader metadata
   - Passes loader factory to properties
   - Coordinates busy state tracking

6. **Entity/List Support:**
   - Extend to EntityListBase for lazy collection loading
   - Support for pagination patterns
   - Deferred child collection materialization

---

## Current Patterns That Block Lazy Loading

### Issue 1: Property Getter Synchronicity

```csharp
// Current pattern from Base.cs:242-245
protected virtual P? Getter<P>([CallerMemberName] string propertyName = "")
{
    return (P?)this.PropertyManager[propertyName]?.Value;
}
```

**Problem:** Synchronous getter cannot await async load operations. Would need redesign or Task<T> return type.

### Issue 2: Direct Value Assumption

Property classes throughout assume values are already materialized. No interception layer for deferred access.

### Issue 3: Serialization Model

JSON serialization/deserialization (OnDeserializing/OnDeserialized) assumes values are present. Lazy properties would need special handling to avoid materializing during deserialization.

---

## Recommended Implementation Path

### Phase 1: Low-Risk Infrastructure
1. Add `IPropertyLoader<T>` interface
2. Extend Property<T> with optional loader support
3. Add `[Load]` attribute for factory methods
4. Property automatically triggers load on first Value access
5. Integrates with existing `IsBusy` tracking

### Phase 2: PropertyManager Integration
1. Update PropertyManager to create lazy-aware properties
2. Coordinate with factory system for loader injection
3. Handle pause/resume for batch loading scenarios

### Phase 3: Validation & Serialization
1. Ensure validation rules work with lazy properties
2. Handle JSON serialization of lazy properties
3. Provide override points for eager loading when needed

### Phase 4: Collection Support
1. Extend ValidateListBase/EntityListBase for lazy children
2. Pagination patterns for large collections
3. Batch loading optimization

---

## Key Advantages of Lazy Loading in Neatoo

1. **Already has async infrastructure:** Busy state tracking, task aggregation, WaitForTasks()
2. **Parent-child relationships established:** SetParent mechanism works
3. **Validation ready:** Rules can execute on lazy properties when loaded
4. **Factory pattern ready:** [Load] methods fit existing factory architecture
5. **Serialization hooks exist:** OnDeserializing/OnDeserialized can manage lazy state
6. **Pause/resume ready:** Can suppress events during lazy batch loading

---

## Affected Files That Would Need Changes

### Core Framework Changes:
- `src/Neatoo/Internal/Property.cs` - Add lazy loader support
- `src/Neatoo/Internal/PropertyManager.cs` - Create lazy properties
- `src/Neatoo/Base.cs` - Getter logic for deferred loads
- `src/Neatoo/IProperty.cs` - New lazy property interface
- `src/Neatoo/RemoteFactory/*` - Add [Load] attribute support

### Optional Extensions:
- `src/Neatoo/ValidateListBase.cs` - Lazy collection support
- `src/Neatoo/EntityListBase.cs` - Lazy entity collection support
- JSON converter factory - Handle lazy serialization

---

## Critical Design Considerations

1. **Thread Safety:** AsyncTasks uses locks; lazy loading must be thread-safe
2. **Circular References:** Parent-child circular loading must be prevented
3. **Exception Handling:** Load failures should propagate through WaitForTasks()
4. **Memory:** Lazy properties add minimal overhead until accessed
5. **Testing:** Without mocking per CLAUDE.md guidelines, need real factory integration
6. **Backward Compatibility:** Must not break existing property access patterns

---

## Summary

**Current State:** NO lazy loading capability exists in Neatoo.

**Good News:** The framework has comprehensive async infrastructure (AsyncTasks, IsBusy tracking, WaitForTasks, Factory pattern) that provides an excellent foundation for lazy loading implementation.

**Implementation Complexity:** Medium - requires extending Property<T>, PropertyManager, and adding loader factory integration, but the async infrastructure is already mature.

**Best Approach:** Deferred Load pattern via [Load] attribute, leveraging existing factory and async task tracking systems.
