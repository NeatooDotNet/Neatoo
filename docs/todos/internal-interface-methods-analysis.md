# Internal Interface Methods Analysis

**Status**: Analysis Document
**Priority**: High
**Category**: API Design / Testability
**Created**: 2026-01-02

## Summary

This document provides a comprehensive analysis of all internal interface members in the Neatoo codebase. These internal members prevent external assemblies (including test projects like KnockOff) from creating stubs/mocks of Neatoo interfaces.

## Problem Statement

Users attempting to create stubs with KnockOff encounter errors because interfaces like `IEditBase` (and the base interfaces it inherits from) have internal methods that cannot be implemented outside the Neatoo assembly.

---

## Interfaces with Internal Members

### 1. IBase (Base.cs)

**File**: `src/Neatoo/Base.cs`

| Member | Signature | Purpose |
|--------|-----------|---------|
| `AddChildTask` | `internal void AddChildTask(Task task)` | Adds a child async task to be tracked for completion. Used to propagate async tasks up the object graph. |
| `GetProperty` | `internal IProperty GetProperty(string propertyName)` | Gets a property by name. Framework-internal access to the property bag. |
| `this[propertyName]` | `internal IProperty this[string propertyName]` | Indexer for property access. Convenience wrapper around `GetProperty`. |
| `PropertyManager` | `internal IPropertyManager<IProperty> PropertyManager { get; }` | Gets the property manager. Core infrastructure for property storage and events. |

**Why Internal**:
- `AddChildTask`: Framework infrastructure for async task bubbling. External callers should use `WaitForTasks()`.
- `GetProperty`/indexer: Framework-internal property access. Public API uses typed property accessors via `Getter<T>`/`Setter<T>`.
- `PropertyManager`: Internal implementation detail. Exposed only for serialization and framework coordination.

**Usage in Base{T}**:
```csharp
// Used in Setter<P> to set property values
var task = this.PropertyManager[propertyName].SetPrivateValue(value);

// Used in OnDeserialized to restore child parent references
foreach (var property in this.PropertyManager.GetProperties)
{
    if (property.Value is ISetParent setParent)
    {
        setParent.SetParent(this);
    }
}
```

---

### 2. IValidateBase (ValidateBase.cs)

**File**: `src/Neatoo/ValidateBase.cs`

| Member | Signature | Purpose |
|--------|-----------|---------|
| `ObjectInvalid` | `internal string? ObjectInvalid { get; }` | Gets an object-level validation error message set via `MarkInvalid()`. |

**Why Internal**:
- `ObjectInvalid`: The value is set through the protected `MarkInvalid()` method. The internal getter allows the framework to read this for validation rules without exposing the full property publicly.

**Usage**:
```csharp
// In RuleManager setup (ValidateBase constructor)
this.RuleManager.AddValidation(static (t) =>
{
    if (!string.IsNullOrEmpty(t.ObjectInvalid))
    {
        return t.ObjectInvalid;
    }
    return string.Empty;
}, (t) => t.ObjectInvalid);
```

---

### 3. IEntityBase (EntityBase.cs)

**File**: `src/Neatoo/EntityBase.cs`

| Member | Signature | Purpose |
|--------|-----------|---------|
| `MarkModified` | `internal void MarkModified()` | Explicitly marks the entity as modified, affecting `IsModified` and `IsSavable`. |
| `MarkAsChild` | `internal void MarkAsChild()` | Marks the entity as a child within an aggregate, preventing direct save. |

**Why Internal**:
- `MarkModified`: Called by `EntityListBase` when items are added to mark existing items as modified. Framework coordination for aggregate patterns.
- `MarkAsChild`: Called by `EntityListBase` when items are added. Prevents child entities from being saved independently.

**Usage in EntityListBase**:
```csharp
protected override void InsertItem(int index, I item)
{
    if (!this.IsPaused)
    {
        if (!item.IsNew)
        {
            item.MarkModified();
        }
        item.MarkAsChild();
    }
    // ...
}
```

---

### 4. IProperty (IProperty.cs)

**File**: `src/Neatoo/IProperty.cs`

| Member | Signature | Purpose |
|--------|-----------|---------|
| `SetPrivateValue` | `internal Task SetPrivateValue(object? newValue, bool quietly = false)` | Sets property value with optional suppression of change notifications. Bypasses read-only checks. |

**Why Internal**:
- `SetPrivateValue`: Called by `Base<T>.Setter<P>` to set values. The "private" in the name indicates it bypasses `IsReadOnly` checks. The `quietly` parameter suppresses events during initialization/deserialization.

**Usage in Base{T}**:
```csharp
protected virtual void Setter<P>(P? value, string propertyName = "")
{
    var task = this.PropertyManager[propertyName].SetPrivateValue(value);
    // ... async task tracking
}
```

---

### 5. IValidateProperty (IValidateProperty.cs)

**File**: `src/Neatoo/IValidateProperty.cs`

| Member | Signature | Purpose |
|--------|-----------|---------|
| `SetMessagesForRule` | `internal void SetMessagesForRule(IReadOnlyList<IRuleMessage> ruleMessages)` | Sets validation messages produced by a specific rule. |
| `ClearMessagesForRule` | `internal void ClearMessagesForRule(uint ruleIndex)` | Clears messages from a specific rule by index. |
| `ClearAllMessages` | `internal void ClearAllMessages()` | Clears all validation messages including child messages. |
| `ClearSelfMessages` | `internal void ClearSelfMessages()` | Clears only this property's messages, not child messages. |

**Why Internal**:
- These methods are called exclusively by `RuleManager` during rule execution. External code should not manipulate validation messages directly - rules produce messages through their return values.

**Usage in RuleManager**:
```csharp
public async Task RunRule(IRule r, CancellationToken? token = null)
{
    // ...
    foreach (var propertyName in triggerProperties.Select(t => t.PropertyName).Except(ruleMessages.Select(p => p.PropertyName)))
    {
        if(this.Target.TryGetProperty(propertyName, out var targetProperty))
        {
            targetProperty.ClearMessagesForRule(rule.UniqueIndex);
        }
    }

    foreach (var ruleMessage in ruleMessages.GroupBy(rm => rm.PropertyName).ToDictionary(g => g.Key, g => g.ToList()))
    {
        if(this.Target.TryGetProperty(ruleMessage.Key, out var targetProperty))
        {
            ruleMessage.Value.ForEach(rm => rm.RuleIndex = rule.UniqueIndex);
            targetProperty.SetMessagesForRule(ruleMessage.Value);
        }
    }
}
```

---

### 6. IPropertyManager{P} (IPropertyManager.cs)

**File**: `src/Neatoo/IPropertyManager.cs`

| Member | Signature | Purpose |
|--------|-----------|---------|
| `PropertyInfoList` | `internal IPropertyInfoList PropertyInfoList { get; }` | Gets metadata about all registered properties. |
| `GetProperties` | `internal IEnumerable<P> GetProperties { get; }` | Gets all instantiated properties. |

**Why Internal**:
- `PropertyInfoList`: Used during deserialization and rule setup to access property metadata.
- `GetProperties`: Used during serialization and deserialization to iterate all properties.

**Usage in Base{T}**:
```csharp
public virtual void OnDeserialized()
{
    // ...
    foreach (var property in this.PropertyManager.GetProperties)
    {
        if (property.Value is ISetParent setParent)
        {
            setParent.SetParent(this);
        }
    }
}
```

---

### 7. IEntityListBase (EntityListBase.cs)

**File**: `src/Neatoo/EntityListBase.cs`

| Member | Signature | Purpose |
|--------|-----------|---------|
| `DeletedList` | `internal IEnumerable DeletedList { get; }` | Gets items that have been removed but need persistence deletion. |

**Why Internal**:
- `DeletedList`: Used during save operations to persist deletions. External code should not manipulate this list directly - use `RemoveAt()` which manages the deleted list automatically.

---

### 8. IRuleMessage (Rules/RuleMessage.cs)

**File**: `src/Neatoo/Rules/RuleMessage.cs`

| Member | Signature | Purpose |
|--------|-----------|---------|
| `RuleIndex` | `uint RuleIndex { get; internal set; }` | The unique index of the rule that produced this message. Setter is internal. |

**Why Internal**:
- `RuleIndex` setter: Set by `RuleManager` when processing rule results. Rules don't set their own index - the manager assigns it based on registration order.

**Usage in RuleManager**:
```csharp
ruleMessage.Value.ForEach(rm => rm.RuleIndex = rule.UniqueIndex);
targetProperty.SetMessagesForRule(ruleMessage.Value);
```

---

## Impact on Stubbing/Mocking

### Affected Interfaces (Cannot Be Stubbed)

The following interfaces cannot be fully stubbed outside the Neatoo assembly due to internal members:

| Interface | Internal Members |
|-----------|------------------|
| `IBase` | 4 members |
| `IValidateBase` | 1 member (inherits 4 from IBase) |
| `IEntityBase` | 2 members (inherits 5 from IValidateBase) |
| `IProperty` | 1 member |
| `IValidateProperty` | 4 members (inherits 1 from IProperty) |
| `IEntityProperty` | 0 new (inherits 5 from IValidateProperty) |
| `IPropertyManager{P}` | 2 members |
| `IEntityListBase` | 1 member |
| `IRuleMessage` | 1 member (setter only) |

### Interfaces That CAN Be Stubbed

The following interfaces have no internal members:

- `IPropertyInfo`
- `IPropertyInfoList` / `IPropertyInfoList{T}`
- `IBaseMetaProperties`
- `IValidateMetaProperties`
- `IEntityMetaProperties`
- `IRuleManager` / `IRuleManager{T}`
- `IRule` / `IRule{T}`
- `IRuleMessages`
- `IListBase` / `IListBase{I}`
- `IValidateListBase` / `IValidateListBase{I}`
- `IEntityListBase{I}` (generic version has no internal members)

---

## Recommendations

### Option 1: InternalsVisibleTo for Test Assemblies

Add `InternalsVisibleTo` to allow KnockOff-generated stubs:

```csharp
// In Neatoo assembly
[assembly: InternalsVisibleTo("Neatoo.UnitTest")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // For Moq/NSubstitute
```

**Pros**: Quick fix, no API changes.
**Cons**: Doesn't help external consumers. Requires coordination with dynamic proxy assemblies.

### Option 2: Extract Public Subset Interfaces

Create public-only interfaces for testing scenarios:

```csharp
// Stubbable subset of IBase
public interface IBaseReadOnly
{
    IBase? Parent { get; }
    bool IsBusy { get; }
    Task WaitForTasks();
}

// IBase inherits from IBaseReadOnly
public interface IBase : IBaseReadOnly
{
    internal void AddChildTask(Task task);
    // ... other internal members
}
```

**Pros**: Clean separation. External stubs implement `IBaseReadOnly`.
**Cons**: Additional interfaces to maintain. May require refactoring consuming code.

### Option 3: Move Internal Members to Explicit Interface Implementation

Keep internal methods but provide default implementations in base classes:

```csharp
public interface IBase
{
    // Make all members public but provide reasonable defaults
    void AddChildTask(Task task) => Task.CompletedTask; // Default implementation (C# 8+)
    // ...
}
```

**Pros**: Interfaces become stubbable.
**Cons**: Default interface implementations may have unexpected behavior. Requires C# 8+.

### Option 4: Use Protected Internal in Base Classes Only

Remove internal members from interfaces entirely. Keep the functionality in base classes:

```csharp
public interface IBase
{
    IBase? Parent { get; }
    bool IsBusy { get; }
    Task WaitForTasks();
    // No internal members
}

public abstract class Base<T> : IBase
{
    // Protected internal for framework use
    protected internal void AddChildTask(Task task) { ... }
    protected internal IProperty GetProperty(string propertyName) { ... }
}
```

**Pros**: Clean interface. Base classes handle framework coordination.
**Cons**: Requires casting to base class type in some framework code.

---

## Testing Guidelines (Current State)

Until internal members are addressed, follow these guidelines:

1. **Prefer Real Neatoo Classes**: As documented in CLAUDE.md, inherit from `ValidateBase<T>`, `EntityBase<T>`, etc. rather than stubbing interfaces.

2. **Stub External Dependencies Only**: Stub repositories, services, and other non-Neatoo interfaces.

3. **Use Factory-Created Objects**: Let the Neatoo factory system create objects with all internal wiring intact.

4. **For Property-Level Testing**: Create test classes that inherit from the appropriate base class:

```csharp
[SuppressFactory]
public class TestValidateObject : ValidateBase<TestValidateObject>
{
    public string Name { get => Getter<string>(); set => Setter(value); }

    public TestValidateObject(IValidateBaseServices<TestValidateObject> services)
        : base(services) { }
}
```

---

## Related Files

- `src/Neatoo/Base.cs` - IBase interface and Base{T} implementation
- `src/Neatoo/ValidateBase.cs` - IValidateBase interface
- `src/Neatoo/EntityBase.cs` - IEntityBase interface
- `src/Neatoo/IProperty.cs` - IProperty interface
- `src/Neatoo/IValidateProperty.cs` - IValidateProperty interface
- `src/Neatoo/IPropertyManager.cs` - IPropertyManager{P} interface
- `src/Neatoo/EntityListBase.cs` - IEntityListBase interface
- `src/Neatoo/Rules/RuleMessage.cs` - IRuleMessage interface
