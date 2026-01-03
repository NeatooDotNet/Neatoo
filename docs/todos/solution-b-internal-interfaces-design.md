# Solution B: Internal Interfaces for Framework Coordination

**Status**: Design Document
**Priority**: High
**Category**: API Design / Testability
**Created**: 2026-01-02

## Overview

This design separates public interfaces (stubbable by KnockOff) from internal interfaces (framework coordination). External consumers implement/stub only the public interfaces; framework code casts to internal interfaces when needed.

---

## Design Pattern

```
┌─────────────────────────────────────────────────────────────┐
│                     External Consumer                        │
│  (KnockOff stubs, test doubles, external implementations)   │
│                                                              │
│  Implements: IBase, IValidateBase, IEntityBase              │
│  Cannot see: IBaseInternal, IValidateBaseInternal, etc.     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Public Interfaces                         │
│                                                              │
│  IBase, IValidateBase, IEntityBase, IProperty, etc.         │
│  - Clean, minimal API                                        │
│  - No internal members                                       │
│  - Fully stubbable                                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                  Neatoo Base Classes                         │
│                                                              │
│  Base<T>, ValidateBase<T>, EntityBase<T>, etc.              │
│  - Implements BOTH public and internal interfaces           │
│  - class Base<T> : IBase, IBaseInternal                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   Internal Interfaces                        │
│                                                              │
│  IBaseInternal, IValidateBaseInternal, etc.                 │
│  - Framework coordination only                              │
│  - Invisible to external consumers                          │
│  - Cast via: if (target is IBaseInternal bi) { ... }        │
└─────────────────────────────────────────────────────────────┘
```

---

## Interface Definitions

### 1. IBase / IBaseInternal

```csharp
// ═══════════════════════════════════════════════════════════
// PUBLIC INTERFACE - Stubbable
// ═══════════════════════════════════════════════════════════
public interface IBase : INeatooObject, INotifyPropertyChanged,
    INotifyNeatooPropertyChanged, IBaseMetaProperties
{
    /// <summary>
    /// Parent object in the object graph hierarchy.
    /// </summary>
    IBase? Parent { get; }

    /// <summary>
    /// Whether async operations are in progress.
    /// </summary>
    bool IsBusy { get; }

    /// <summary>
    /// Waits for all async tasks to complete.
    /// </summary>
    Task WaitForTasks();
}

// ═══════════════════════════════════════════════════════════
// INTERNAL INTERFACE - Framework Coordination
// ═══════════════════════════════════════════════════════════
internal interface IBaseInternal
{
    /// <summary>
    /// Adds a child task to be tracked for completion.
    /// Called by child objects to bubble tasks up the graph.
    /// </summary>
    void AddChildTask(Task task);

    /// <summary>
    /// Gets a property by name. Framework-internal access.
    /// </summary>
    IProperty GetProperty(string propertyName);

    /// <summary>
    /// Indexer for property access.
    /// </summary>
    IProperty this[string propertyName] { get; }

    /// <summary>
    /// Gets the property manager. Used by serialization and rules.
    /// </summary>
    IPropertyManager<IProperty> PropertyManager { get; }
}

// ═══════════════════════════════════════════════════════════
// BASE CLASS - Implements Both
// ═══════════════════════════════════════════════════════════
public abstract class Base<T> : IBase, IBaseInternal, ISetParent, IJsonOnDeserialized
    where T : Base<T>
{
    // Public interface implementation
    public IBase? Parent { get; protected set; }
    public bool IsBusy => RunningTasks.IsRunning || PropertyManager.IsBusy;
    public virtual Task WaitForTasks() => RunningTasks.AllDone;

    // Internal interface implementation (explicit)
    void IBaseInternal.AddChildTask(Task task) => AddChildTask(task);
    IProperty IBaseInternal.GetProperty(string propertyName) => GetProperty(propertyName);
    IProperty IBaseInternal.this[string propertyName] => GetProperty(propertyName);
    IPropertyManager<IProperty> IBaseInternal.PropertyManager => PropertyManager;

    // Protected implementation for derived classes
    protected IPropertyManager<IProperty> PropertyManager { get; set; }
    protected IProperty this[string propertyName] => GetProperty(propertyName);

    public virtual void AddChildTask(Task task)
    {
        if (Parent is IBaseInternal parentInternal)
        {
            parentInternal.AddChildTask(task);
        }
        RunningTasks.AddTask(task);
    }

    public IProperty GetProperty(string propertyName)
    {
        return PropertyManager[propertyName];
    }
}
```

### 2. IValidateBase / IValidateBaseInternal

```csharp
// ═══════════════════════════════════════════════════════════
// PUBLIC INTERFACE - Stubbable
// ═══════════════════════════════════════════════════════════
public interface IValidateBase : IBase, IValidateMetaProperties
{
    /// <summary>
    /// Whether events, rules, and modification tracking are suspended.
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Gets a validation property by name.
    /// </summary>
    new IValidateProperty GetProperty(string propertyName);

    /// <summary>
    /// Indexer for validation property access.
    /// </summary>
    new IValidateProperty this[string propertyName] { get; }

    /// <summary>
    /// Tries to get a validation property by name.
    /// </summary>
    bool TryGetProperty(string propertyName, out IValidateProperty validateProperty);
}

// ═══════════════════════════════════════════════════════════
// INTERNAL INTERFACE - Framework Coordination
// ═══════════════════════════════════════════════════════════
internal interface IValidateBaseInternal : IBaseInternal
{
    /// <summary>
    /// Object-level validation error message set via MarkInvalid().
    /// Read by RuleManager for object-level validation.
    /// </summary>
    string? ObjectInvalid { get; }
}

// ═══════════════════════════════════════════════════════════
// BASE CLASS - Implements Both
// ═══════════════════════════════════════════════════════════
public abstract class ValidateBase<T> : Base<T>, IValidateBase, IValidateBaseInternal
    where T : ValidateBase<T>
{
    // Public interface
    public bool IsPaused { get; protected set; }
    public new IValidateProperty GetProperty(string propertyName) => PropertyManager[propertyName];
    public new IValidateProperty this[string propertyName] => GetProperty(propertyName);
    public bool TryGetProperty(string propertyName, out IValidateProperty validateProperty) { ... }

    // Internal interface (explicit)
    string? IValidateBaseInternal.ObjectInvalid => ObjectInvalid;

    // Protected implementation
    public string? ObjectInvalid { get => Getter<string>(); protected set => Setter(value); }
}
```

### 3. IEntityBase / IEntityBaseInternal

```csharp
// ═══════════════════════════════════════════════════════════
// PUBLIC INTERFACE - Stubbable
// ═══════════════════════════════════════════════════════════
public interface IEntityBase : IValidateBase, IEntityMetaProperties, IFactorySaveMeta
{
    /// <summary>
    /// Property names that have been modified since the last save.
    /// </summary>
    IEnumerable<string> ModifiedProperties { get; }

    /// <summary>
    /// Marks the entity for deletion.
    /// </summary>
    void Delete();

    /// <summary>
    /// Reverses a previous Delete() call.
    /// </summary>
    void UnDelete();

    /// <summary>
    /// Persists the entity.
    /// </summary>
    Task<IEntityBase> Save();

    /// <summary>
    /// Gets an entity property by name.
    /// </summary>
    new IEntityProperty this[string propertyName] { get; }
}

// ═══════════════════════════════════════════════════════════
// INTERNAL INTERFACE - Framework Coordination
// ═══════════════════════════════════════════════════════════
internal interface IEntityBaseInternal : IValidateBaseInternal
{
    /// <summary>
    /// Explicitly marks the entity as modified.
    /// Called by EntityListBase when items are added.
    /// </summary>
    void MarkModified();

    /// <summary>
    /// Marks the entity as a child within an aggregate.
    /// Called by EntityListBase when items are added.
    /// </summary>
    void MarkAsChild();
}

// ═══════════════════════════════════════════════════════════
// BASE CLASS - Implements Both
// ═══════════════════════════════════════════════════════════
public abstract class EntityBase<T> : ValidateBase<T>, IEntityBase, IEntityBaseInternal
    where T : EntityBase<T>
{
    // Public interface
    public virtual IEnumerable<string> ModifiedProperties => PropertyManager.ModifiedProperties;
    public void Delete() => MarkDeleted();
    public void UnDelete() { ... }
    public virtual async Task<IEntityBase> Save() { ... }
    public new IEntityProperty this[string propertyName] => GetProperty(propertyName);

    // Internal interface (explicit)
    void IEntityBaseInternal.MarkModified() => MarkModified();
    void IEntityBaseInternal.MarkAsChild() => MarkAsChild();

    // Protected implementation
    protected virtual void MarkModified() { IsMarkedModified = true; ... }
    protected virtual void MarkAsChild() { IsChild = true; }
}
```

### 4. IProperty / IPropertyInternal

```csharp
// ═══════════════════════════════════════════════════════════
// PUBLIC INTERFACE - Stubbable
// ═══════════════════════════════════════════════════════════
public interface IProperty : INotifyPropertyChanged, INotifyNeatooPropertyChanged
{
    string Name { get; }
    object? Value { get; set; }
    Task SetValue(object? newValue);
    Task Task { get; }
    bool IsBusy { get; }
    bool IsReadOnly { get; }
    void AddMarkedBusy(long id);
    void RemoveMarkedBusy(long id);
    void LoadValue(object? value);
    Task WaitForTasks();
    Type Type { get; }
    string? StringValue => Value?.ToString();
}

// ═══════════════════════════════════════════════════════════
// INTERNAL INTERFACE - Framework Coordination
// ═══════════════════════════════════════════════════════════
internal interface IPropertyInternal
{
    /// <summary>
    /// Sets value bypassing IsReadOnly checks.
    /// Called by Base{T}.Setter{P}. The "quietly" param suppresses events during init/deserialization.
    /// </summary>
    Task SetPrivateValue(object? newValue, bool quietly = false);
}
```

### 5. IValidateProperty / IValidatePropertyInternal

```csharp
// ═══════════════════════════════════════════════════════════
// PUBLIC INTERFACE - Stubbable
// ═══════════════════════════════════════════════════════════
public interface IValidateProperty : IProperty
{
    bool IsSelfValid { get; }
    bool IsValid { get; }
    Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null);
    IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }
}

// ═══════════════════════════════════════════════════════════
// INTERNAL INTERFACE - Framework Coordination
// ═══════════════════════════════════════════════════════════
internal interface IValidatePropertyInternal : IPropertyInternal
{
    /// <summary>
    /// Sets validation messages produced by a specific rule.
    /// Called exclusively by RuleManager during rule execution.
    /// </summary>
    void SetMessagesForRule(IReadOnlyList<IRuleMessage> ruleMessages);

    /// <summary>
    /// Clears messages from a specific rule by index.
    /// Called by RuleManager when a rule clears its previous messages.
    /// </summary>
    void ClearMessagesForRule(uint ruleIndex);

    /// <summary>
    /// Clears all validation messages including child messages.
    /// Called during RunRules(RunRulesFlag.All) to reset validation state.
    /// </summary>
    void ClearAllMessages();

    /// <summary>
    /// Clears only this property's messages, not child messages.
    /// Called by ValidateBase.ClearSelfMessages().
    /// </summary>
    void ClearSelfMessages();
}
```

### 6. IPropertyManager / IPropertyManagerInternal

```csharp
// ═══════════════════════════════════════════════════════════
// PUBLIC INTERFACE - Stubbable
// ═══════════════════════════════════════════════════════════
public interface IPropertyManager<out P> : INotifyNeatooPropertyChanged, INotifyPropertyChanged
    where P : IProperty
{
    bool IsBusy { get; }
    Task WaitForTasks();
    bool HasProperty(string propertyName);
    P GetProperty(string propertyName);
    P this[string propertyName] { get; }
    void SetProperties(IEnumerable<IProperty> properties);
}

// ═══════════════════════════════════════════════════════════
// INTERNAL INTERFACE - Framework Coordination
// ═══════════════════════════════════════════════════════════
internal interface IPropertyManagerInternal<out P> where P : IProperty
{
    /// <summary>
    /// Gets metadata about all registered properties.
    /// Used during deserialization and rule setup.
    /// </summary>
    IPropertyInfoList PropertyInfoList { get; }

    /// <summary>
    /// Gets all instantiated properties.
    /// Used during serialization and deserialization.
    /// </summary>
    IEnumerable<P> GetProperties { get; }
}
```

### 7. IEntityListBase / IEntityListBaseInternal

```csharp
// ═══════════════════════════════════════════════════════════
// PUBLIC INTERFACE - Stubbable
// ═══════════════════════════════════════════════════════════
public interface IEntityListBase : IValidateListBase, IEntityMetaProperties
{
    // No additional public members beyond inherited
}

// ═══════════════════════════════════════════════════════════
// INTERNAL INTERFACE - Framework Coordination
// ═══════════════════════════════════════════════════════════
internal interface IEntityListBaseInternal
{
    /// <summary>
    /// Items removed but needing persistence deletion.
    /// Used during save operations.
    /// </summary>
    IEnumerable DeletedList { get; }
}
```

### 8. IRuleMessage (Setter Only)

```csharp
// ═══════════════════════════════════════════════════════════
// PUBLIC INTERFACE - Stubbable (getter only)
// ═══════════════════════════════════════════════════════════
public interface IRuleMessage
{
    string PropertyName { get; }
    string Message { get; }
    RuleSeverity Severity { get; }
    uint RuleIndex { get; }  // Getter is public
}

// ═══════════════════════════════════════════════════════════
// INTERNAL INTERFACE - Framework Coordination
// ═══════════════════════════════════════════════════════════
internal interface IRuleMessageInternal
{
    /// <summary>
    /// Sets the rule index. Called by RuleManager when processing rule results.
    /// </summary>
    uint RuleIndex { set; }
}
```

---

## Framework Code Changes

### Before (using internal interface members)

```csharp
// In EntityListBase.InsertItem
protected override void InsertItem(int index, I item)
{
    if (!IsPaused)
    {
        if (!item.IsNew)
        {
            item.MarkModified();    // ❌ Internal member on IEntityBase
        }
        item.MarkAsChild();         // ❌ Internal member on IEntityBase
    }
    base.InsertItem(index, item);
}
```

### After (casting to internal interface)

```csharp
// In EntityListBase.InsertItem
protected override void InsertItem(int index, I item)
{
    if (!IsPaused)
    {
        if (item is IEntityBaseInternal entityInternal)
        {
            if (!item.IsNew)
            {
                entityInternal.MarkModified();    // ✅ Via internal interface
            }
            entityInternal.MarkAsChild();         // ✅ Via internal interface
        }
    }
    base.InsertItem(index, item);
}
```

### Before (Base{T}.Setter)

```csharp
protected virtual void Setter<P>(P? value, string propertyName = "")
{
    var task = PropertyManager[propertyName].SetPrivateValue(value);  // ❌ Internal

    if (!task.IsCompleted && Parent != null)
    {
        Parent.AddChildTask(task);  // ❌ Internal
    }
    // ...
}
```

### After (casting to internal interface)

```csharp
protected virtual void Setter<P>(P? value, string propertyName = "")
{
    var property = PropertyManager[propertyName];
    Task task;

    if (property is IPropertyInternal propertyInternal)
    {
        task = propertyInternal.SetPrivateValue(value);  // ✅ Via internal interface
    }
    else
    {
        task = property.SetValue(value);  // Fallback for stubs
    }

    if (!task.IsCompleted && Parent is IBaseInternal parentInternal)
    {
        parentInternal.AddChildTask(task);  // ✅ Via internal interface
    }
    // ...
}
```

---

## Summary of Changes

| Public Interface | Internal Interface | Members Moved |
|-----------------|-------------------|---------------|
| `IBase` | `IBaseInternal` | `AddChildTask`, `GetProperty`, indexer, `PropertyManager` |
| `IValidateBase` | `IValidateBaseInternal` | `ObjectInvalid` |
| `IEntityBase` | `IEntityBaseInternal` | `MarkModified`, `MarkAsChild` |
| `IProperty` | `IPropertyInternal` | `SetPrivateValue` |
| `IValidateProperty` | `IValidatePropertyInternal` | `SetMessagesForRule`, `ClearMessagesForRule`, `ClearAllMessages`, `ClearSelfMessages` |
| `IPropertyManager<P>` | `IPropertyManagerInternal<P>` | `PropertyInfoList`, `GetProperties` |
| `IEntityListBase` | `IEntityListBaseInternal` | `DeletedList` |
| `IRuleMessage` | `IRuleMessageInternal` | `RuleIndex` setter |

---

## Benefits

1. **Fully Stubbable**: KnockOff can generate stubs for all public interfaces
2. **Clean API**: Public interfaces show only what consumers need
3. **No Breaking Changes**: Existing derived classes continue to work
4. **Clear Separation**: Framework coordination is hidden from consumers
5. **Type Safety**: Casts are explicit and checked at runtime

## Risks

1. **Runtime Failures**: If a stub is accidentally used in production code that expects internal interface, cast fails
2. **Performance**: Additional type checks (minimal impact)
3. **Debugging**: Stack traces show internal interface calls

## Mitigation

```csharp
// Helper extension method for safe casting with clear error
internal static class InternalInterfaceExtensions
{
    public static IBaseInternal AsInternal(this IBase target)
    {
        return target as IBaseInternal
            ?? throw new InvalidOperationException(
                $"Expected {target.GetType().Name} to implement IBaseInternal. " +
                "Stubs cannot be used in framework operations.");
    }
}

// Usage
Parent.AsInternal().AddChildTask(task);
```
