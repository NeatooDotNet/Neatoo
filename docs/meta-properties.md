# Meta-Properties Reference

Neatoo entities expose bindable meta-properties that track state. These properties integrate with UI frameworks for reactive updates.

## Property Hierarchy

```
IValidateMetaProperties
    - IsBusy
    - WaitForTasks()
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
    - Parent (IValidateBase?)
    - Root (IValidateBase?)
    - Save() (EntityBase only)
    - Save(CancellationToken) (EntityBase only)
```

## Validate Meta-Properties

### IsBusy

Indicates async operations are in progress.

<!-- pseudo:isbusy-signature -->
```csharp
bool IsBusy { get; }
```
<!-- /snippet -->

**True when:**
- Async validation rules are executing
- Any property is busy
- Any child object is busy

**Usage:**
<!-- pseudo:isbusy-razor-usage -->
```razor
<MudButton Disabled="@entity.IsBusy">Save</MudButton>

@if (entity.IsBusy)
{
    <MudProgressCircular Indeterminate="true" />
}
```
<!-- /snippet -->

### WaitForTasks()

Awaits all pending async operations.

<!-- pseudo:waitfortasks-signatures -->
```csharp
Task WaitForTasks();
Task WaitForTasks(CancellationToken token);
```
<!-- /snippet -->

**Usage:**
<!-- pseudo:waitfortasks-usage -->
```csharp
await entity.WaitForTasks();
// All async rules have completed
```
<!-- /snippet -->

**With cancellation:**
<!-- pseudo:waitfortasks-cancellation -->
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    await entity.WaitForTasks(cts.Token);
}
catch (OperationCanceledException)
{
    // Waiting was cancelled - entity marked invalid
    // entity.IsValid == false
}
```
<!-- /snippet -->

**Cancellation behavior:** When cancelled, the entity is marked invalid via `MarkInvalid()`. Recovery requires calling `RunRules(RunRulesFlag.All)` to re-validate.

## Validation Meta-Properties

### IsValid

All validation rules pass (including children).

<!-- pseudo:isvalid-signature -->
```csharp
bool IsValid { get; }
```
<!-- /snippet -->

**True when:**
- All properties pass validation
- All child objects are valid

**Usage:**
<!-- pseudo:isvalid-razor-usage -->
```razor
@if (!entity.IsValid)
{
    <MudAlert Severity="Severity.Error">Please correct errors</MudAlert>
}
```
<!-- /snippet -->

### IsSelfValid

This object's validation passes (excluding children).

<!-- pseudo:isselfvalid-signature -->
```csharp
bool IsSelfValid { get; }
```
<!-- /snippet -->

**True when:**
- All direct properties pass validation
- Does not consider child object validity

### IsValid vs IsSelfValid Example

<!-- pseudo:isvalid-vs-isselfvalid-example -->
```csharp
// Parent with valid properties but invalid child
var person = personFactory.Create();
person.FirstName = "John";        // Valid
person.LastName = "Doe";          // Valid

var phone = person.PersonPhoneList.AddPhoneNumber();
phone.PhoneNumber = "";           // Invalid - required

// Results:
person.IsSelfValid     // true  - person's own properties are valid
person.IsValid         // false - child phone is invalid

phone.IsSelfValid      // false - phone's properties are invalid
phone.IsValid          // false - same as IsSelfValid (no children)

// After fixing the child:
phone.PhoneNumber = "555-1234";

person.IsSelfValid     // true
person.IsValid         // true  - now all children are valid too
```
<!-- /snippet -->

### PropertyMessages

All validation messages for the object.

<!-- pseudo:propertymessages-signature -->
```csharp
IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }
```
<!-- /snippet -->

**Usage:**
<!-- pseudo:propertymessages-iteration -->
```csharp
foreach (var msg in entity.PropertyMessages)
{
    Console.WriteLine($"{msg.Property.Name}: {msg.Message}");
}
```
<!-- /snippet -->

### RunRules()

Execute validation rules.

<!-- pseudo:runrules-signatures -->
```csharp
Task RunRules(string propertyName, CancellationToken? token = null);
Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null);
```
<!-- /snippet -->

**Flags:**
| Flag | Value | Description |
|------|-------|-------------|
| `None` | 0 | No rules |
| `NoMessages` | 1 | Rules with no current messages |
| `Messages` | 2 | Rules with current messages |
| `NotExecuted` | 4 | Rules that haven't executed yet |
| `Executed` | 8 | Re-run previously executed rules |
| `Self` | 16 | Only this object's rules (exclude children) |
| `All` | 31 | All flags combined |

**Flags can be combined:**
<!-- pseudo:runrules-flag-combinations -->
```csharp
// Run only unexecuted rules on this object
await entity.RunRules(RunRulesFlag.NotExecuted | RunRulesFlag.Self);

// Run rules with messages (re-validate failed rules)
await entity.RunRules(RunRulesFlag.Messages | RunRulesFlag.Executed);

// Default - run all rules
await entity.RunRules();  // Same as RunRulesFlag.All
```
<!-- /snippet -->

**Usage:**
<!-- pseudo:runrules-before-save -->
```csharp
// Before save
await entity.RunRules();
if (entity.IsValid) { /* save */ }
```
<!-- /snippet -->

**Cancellation:** Pass a `CancellationToken` to cancel rule execution. See [Validation and Rules - Cancellation Support](validation-and-rules.md#cancellation-support) for details and recovery patterns.

### ClearAllMessages() / ClearSelfMessages()

Clear validation messages.

<!-- pseudo:clear-messages-signatures -->
```csharp
void ClearAllMessages();    // Clears all including children
void ClearSelfMessages();   // Clears only this object's messages
```
<!-- /snippet -->

## Entity Meta-Properties

### IsNew

Entity has not been persisted.

<!-- pseudo:isnew-signature -->
```csharp
bool IsNew { get; }
```
<!-- /snippet -->

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

<!-- pseudo:ismodified-signature -->
```csharp
bool IsModified { get; }
```
<!-- /snippet -->

**True when:**
- Any property value changed
- `IsNew = true`
- `IsDeleted = true`
- `IsSelfModified = true`
- Any child is modified

### IsSelfModified

This entity's properties have changed (excluding children).

<!-- pseudo:isselfmodified-signature -->
```csharp
bool IsSelfModified { get; }
```
<!-- /snippet -->

**True when:**
- Direct properties have changed
- `IsDeleted = true`
- `IsMarkedModified = true`

### IsMarkedModified

Explicitly marked as modified via `MarkModified()`.

<!-- pseudo:ismarkedmodified-signature -->
```csharp
bool IsMarkedModified { get; }
```
<!-- /snippet -->

### MarkModified()

Forces the entity to be considered modified even without property changes.

<!-- pseudo:markmodified-signature -->
```csharp
void MarkModified();
```
<!-- /snippet -->

**Use cases:**
- Force save when external state changed
- Re-save after related data updated
- Trigger update when calculated fields change

**Example:**
<!-- pseudo:markmodified-state-progression -->
```csharp
// Entity with no property changes
person.IsModified;        // false
person.IsSavable;         // false - nothing to save

// Force modification
person.MarkModified();

person.IsMarkedModified;  // true
person.IsSelfModified;    // true
person.IsModified;        // true
person.IsSavable;         // true - now savable

// After save, IsMarkedModified resets
person = await factory.Save(person);
person.IsMarkedModified;  // false
```
<!-- /snippet -->

> **Note:** `MarkModified()` is available via the `IEntityBase` interface. For child entities added to a collection, the framework automatically calls `MarkModified()` if the item is not new.

### IsDeleted

Marked for deletion.

<!-- pseudo:isdeleted-signature -->
```csharp
bool IsDeleted { get; }
```
<!-- /snippet -->

**Set via:**
<!-- pseudo:delete-undelete-calls -->
```csharp
entity.Delete();    // Marks IsDeleted = true
entity.UnDelete();  // Reverts to false
```
<!-- /snippet -->

**Effect on save:**
- `IsDeleted = true` triggers `Delete` operation

### IsChild

Entity is part of a parent aggregate.

<!-- pseudo:ischild-signature -->
```csharp
bool IsChild { get; }
```
<!-- /snippet -->

**Set true when:**
- Added to an `EntityListBase<I>` collection
- `MarkAsChild()` is called

**Effect:**
- `IsSavable` is always false for children
- Children are saved through parent's operations

### Parent

Immediate parent in the object graph.

<!-- pseudo:parent-signature -->
```csharp
IValidateBase? Parent { get; }
```
<!-- /snippet -->

**Value:**
- Set when entity is added to a collection
- Points to the collection's parent (not the collection itself)
- `null` for aggregate roots or standalone entities

**Usage:**
<!-- pseudo:parent-access-casting -->
```csharp
// Access typed parent
public IOrder? ParentOrder => Parent as IOrder;

// Check if entity is in an aggregate
if (entity.Parent != null)
{
    // Entity is part of a hierarchy
}
```
<!-- /snippet -->

### Root

Aggregate root of the object graph.

<!-- pseudo:root-signature -->
```csharp
IValidateBase? Root { get; }
```
<!-- /snippet -->

**Value:**
- `null` if this entity IS the aggregate root (or standalone)
- Returns the aggregate root for child entities at any depth

**Computation:**
- If `Parent` is null → `Root` is null (this is the root)
- If `Parent.Root` exists → return it
- If `Parent.Root` is null → Parent is the root → return `Parent`

**Usage:**
<!-- pseudo:root-usage -->
```csharp
var order = await orderFactory.Create();
var line = await order.Lines.AddLine();
var detail = await line.Details.AddDetail();

order.Root     // null (it IS the root)
line.Root      // order
detail.Root    // order (not line)

// Access typed root
public IOrder? OrderRoot => Root as IOrder;
```
<!-- /snippet -->

**Cross-Aggregate Enforcement:**

Adding an entity to a different aggregate throws `InvalidOperationException`:

<!-- invalid:cross-aggregate-add -->
```csharp
var order1 = await orderFactory.Create();
var order2 = await orderFactory.Create();

var line = order1.Lines.AddLine();
order2.Lines.Add(line);  // THROWS - line belongs to order1
```
<!-- /snippet -->

### IsSavable

Entity can be saved.

<!-- pseudo:issavable-signature -->
```csharp
bool IsSavable { get; }
```
<!-- /snippet -->

**True when ALL conditions met:**
- `IsModified = true`
- `IsValid = true`
- `IsBusy = false`
- `IsChild = false`

**Usage:**
```razor
<MudButton Disabled="@(!entity.IsSavable)" OnClick="Save">Save</MudButton>
```

### Save()

Persists the entity to the database. Available only on `EntityBase<T>`.

<!-- pseudo:save-signatures -->
```csharp
Task<IEntityBase> Save();
Task<IEntityBase> Save(CancellationToken token);
```
<!-- /snippet -->

**Behavior:**
- Routes to `[Insert]`, `[Update]`, or `[Delete]` based on entity state
- Internally calls `factory.Save(this)`
- Returns a **new instance** after client-server roundtrip

**Usage:**
<!-- pseudo:entity-based-save -->
```csharp
// Entity-based save
person = (IPerson)await person.Save();

// With cancellation
person = (IPerson)await person.Save(cts.Token);
```
<!-- /snippet -->

**Throws `SaveOperationException` when:**
| Reason | Condition |
|--------|-----------|
| `IsChildObject` | `IsChild = true` |
| `IsInvalid` | `IsValid = false` |
| `NotModified` | `IsModified = false` |
| `IsBusy` | `IsBusy = true` |
| `NoFactoryMethod` | No factory reference |

**Business Operation Pattern:**

The `Save()` method enables domain operations that modify state and persist atomically:

<!-- snippet: business-operation-example -->
```cs
/// <summary>
/// Minimal example showing the business operation pattern.
/// Used in meta-properties.md to demonstrate Save() usage.
/// </summary>
public partial interface IOrder : IEntityBase
{
    OrderStatus Status { get; set; }
    DateTime? CompletedDate { get; }

    Task<IOrder> Complete();
}

public enum OrderStatus { Pending, Completed }

[Factory]
[SuppressFactory]  // Suppress factory generation - example only
internal partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    public partial OrderStatus Status { get; set; }
    public partial DateTime? CompletedDate { get; set; }

    public async Task<IOrder> Complete()
    {
        if (Status == OrderStatus.Completed)
            throw new InvalidOperationException("Already completed");

        Status = OrderStatus.Completed;
        CompletedDate = DateTime.UtcNow;

        return (IOrder)await this.Save();
    }

    [Create]
    public void Create() => Status = OrderStatus.Pending;
}
```
<!-- endSnippet -->

See [Factory Operations - Business Operations](factory-operations.md#business-operations) for the full pattern.

## Property-Level Meta-Properties

### IValidateProperty (base)

| Property | Type | Description |
|----------|------|-------------|
| `Name` | string | Property name |
| `Value` | object? | Current value |
| `IsBusy` | bool | Async operations running |
| `IsReadOnly` | bool | Cannot be edited |
| `IsValid` | bool | Validation passes |
| `PropertyMessages` | collection | Validation messages |

### IEntityProperty

| Property | Type | Description |
|----------|------|-------------|
| `IsModified` | bool | Value changed since last save |
| `DisplayName` | string? | Display label |

## Change Notifications

All meta-properties raise `PropertyChanged` when values change:

<!-- pseudo:propertychanged-handler -->
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
        case nameof(IValidateBase.IsBusy):
            // Show/hide loading indicator
            break;
    }
};
```
<!-- /snippet -->

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

<!-- pseudo:common-ui-patterns -->
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
<!-- /snippet -->

## See Also

- [Property System](property-system.md) - Property-level state
- [Validation and Rules](validation-and-rules.md) - Rule execution
- [Blazor Binding](blazor-binding.md) - UI patterns
