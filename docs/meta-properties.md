# Meta-Properties Reference

Neatoo entities expose bindable meta-properties that track state. These properties integrate with UI frameworks for reactive updates.

## Property Hierarchy

```
IBaseMetaProperties
    - IsBusy
    - WaitForTasks()

IValidateMetaProperties : IBaseMetaProperties
    - IsValid
    - IsSelfValid
    - PropertyMessages
    - RunRules()
    - ClearAllMessages()
    - ClearSelfMessages()

IEntityMetaProperties
    - IsChild
    - IsModified
    - IsSelfModified
    - IsMarkedModified
    - IsSavable
    - IsNew (EntityBase only)
    - IsDeleted (EntityBase only)
```

## Base Meta-Properties

### IsBusy

Indicates async operations are in progress.

```csharp
bool IsBusy { get; }
```

**True when:**
- Async validation rules are executing
- Any property is busy
- Any child object is busy

**Usage:**
```razor
<MudButton Disabled="@entity.IsBusy">Save</MudButton>

@if (entity.IsBusy)
{
    <MudProgressCircular Indeterminate="true" />
}
```

### WaitForTasks()

Awaits all pending async operations.

```csharp
Task WaitForTasks();
```

**Usage:**
```csharp
await entity.WaitForTasks();
// All async rules have completed
```

## Validation Meta-Properties

### IsValid

All validation rules pass (including children).

```csharp
bool IsValid { get; }
```

**True when:**
- All properties pass validation
- All child objects are valid

**Usage:**
```razor
@if (!entity.IsValid)
{
    <MudAlert Severity="Severity.Error">Please correct errors</MudAlert>
}
```

### IsSelfValid

This object's validation passes (excluding children).

```csharp
bool IsSelfValid { get; }
```

**True when:**
- All direct properties pass validation
- Does not consider child object validity

### PropertyMessages

All validation messages for the object.

```csharp
IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }
```

**Usage:**
```csharp
foreach (var msg in entity.PropertyMessages)
{
    Console.WriteLine($"{msg.Property.Name}: {msg.Message}");
}
```

### RunRules()

Execute validation rules.

```csharp
Task RunRules(string propertyName, CancellationToken? token = null);
Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null);
```

**Flags:**
| Flag | Description |
|------|-------------|
| `All` | Run all rules, clear messages first |
| `Self` | Run only this object's rules |
| `NotExecuted` | Run rules that haven't executed |
| `Executed` | Re-run previously executed rules |
| `NoMessages` | Run rules with no current messages |
| `Messages` | Run rules with current messages |

**Usage:**
```csharp
// Before save
await entity.RunRules();
if (entity.IsValid) { /* save */ }
```

### ClearAllMessages() / ClearSelfMessages()

Clear validation messages.

```csharp
void ClearAllMessages();    // Clears all including children
void ClearSelfMessages();   // Clears only this object's messages
```

## Entity Meta-Properties

### IsNew

Entity has not been persisted.

```csharp
bool IsNew { get; }
```

**Set true:**
- After `Create` operation completes

**Set false:**
- After `Insert` operation completes
- After `Fetch` operation completes

**Effect on save:**
- `IsNew = true` triggers `Insert` operation
- `IsNew = false` triggers `Update` operation

### IsModified

Entity or any child has changes.

```csharp
bool IsModified { get; }
```

**True when:**
- Any property value changed
- `IsNew = true`
- `IsDeleted = true`
- `IsSelfModified = true`
- Any child is modified

### IsSelfModified

This entity's properties have changed (excluding children).

```csharp
bool IsSelfModified { get; }
```

**True when:**
- Direct properties have changed
- `IsDeleted = true`
- `IsMarkedModified = true`

### IsMarkedModified

Explicitly marked as modified.

```csharp
bool IsMarkedModified { get; }
```

Use `MarkModified()` to force save even without property changes.

### IsDeleted

Marked for deletion.

```csharp
bool IsDeleted { get; }
```

**Set via:**
```csharp
entity.Delete();    // Marks IsDeleted = true
entity.UnDelete();  // Reverts to false
```

**Effect on save:**
- `IsDeleted = true` triggers `Delete` operation

### IsChild

Entity is part of a parent aggregate.

```csharp
bool IsChild { get; }
```

**Set true when:**
- Added to an `EntityListBase<I>` collection
- `MarkAsChild()` is called

**Effect:**
- `IsSavable` is always false for children
- Children are saved through parent's operations

### IsSavable

Entity can be saved.

```csharp
bool IsSavable { get; }
```

**True when ALL conditions met:**
- `IsModified = true`
- `IsValid = true`
- `IsBusy = false`
- `IsChild = false`

**Usage:**
```razor
<MudButton Disabled="@(!entity.IsSavable)" OnClick="Save">Save</MudButton>
```

## Property-Level Meta-Properties

### IProperty

| Property | Type | Description |
|----------|------|-------------|
| `Name` | string | Property name |
| `Value` | object? | Current value |
| `IsBusy` | bool | Async operations running |
| `IsReadOnly` | bool | Cannot be edited |

### IValidateProperty

| Property | Type | Description |
|----------|------|-------------|
| `IsValid` | bool | Validation passes |
| `PropertyMessages` | collection | Validation messages |

### IEntityProperty

| Property | Type | Description |
|----------|------|-------------|
| `IsModified` | bool | Value changed since last save |
| `DisplayName` | string? | Display label |

## Change Notifications

All meta-properties raise `PropertyChanged` when values change:

```csharp
entity.PropertyChanged += (s, e) =>
{
    switch (e.PropertyName)
    {
        case nameof(IEntityBase.IsSavable):
            // Update save button state
            break;
        case nameof(IValidateBase.IsValid):
            // Update validation UI
            break;
        case nameof(IBase.IsBusy):
            // Show/hide loading indicator
            break;
    }
};
```

## Entity Lists

`EntityListBase<I>` provides aggregated meta-properties:

| Property | Description |
|----------|-------------|
| `IsModified` | Any item modified OR items pending deletion |
| `IsSelfModified` | Always false (lists have no own properties) |
| `IsValid` | All items are valid |
| `IsSelfValid` | Always true |
| `IsBusy` | Any item is busy |
| `PropertyMessages` | Aggregated from all items |

## Quick Reference

### Entity State Transitions

```
Create()
    IsNew = true
    IsModified = false

Fetch()
    IsNew = false
    IsModified = false

Property Changed:
    IsModified = true

Insert() Complete:
    IsNew = false
    IsModified = false

Update() Complete:
    IsModified = false

Delete():
    IsDeleted = true
    IsModified = true

UnDelete():
    IsDeleted = false
```

### IsSavable Decision Tree

```
IsSavable = false when:
    - IsChild = true           "Cannot save child entities directly"
    - IsModified = false       "No changes to save"
    - IsValid = false          "Fix validation errors first"
    - IsBusy = true            "Wait for operations to complete"

IsSavable = true when:
    - IsChild = false
    - IsModified = true
    - IsValid = true
    - IsBusy = false
```

### Common Patterns

```csharp
// Wait and check before save
await entity.WaitForTasks();
if (!entity.IsSavable)
{
    if (!entity.IsValid)
        ShowErrors(entity.PropertyMessages);
    return;
}
entity = await factory.Save(entity);

// Disable save button
Disabled="@(!entity.IsSavable)"

// Show loading state
@if (entity.IsBusy) { <Spinner /> }

// Show validation summary
@if (!entity.IsValid) { <ValidationSummary /> }
```

## See Also

- [Property System](property-system.md) - Property-level state
- [Validation and Rules](validation-and-rules.md) - Rule execution
- [Blazor Binding](blazor-binding.md) - UI patterns
