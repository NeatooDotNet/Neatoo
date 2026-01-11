# Entity Collections

Neatoo provides list base classes for managing child entity collections within aggregates.

## Class Hierarchy

```
ValidateListBase<I>          - Observable collection, parent-child, aggregated validation
    |
EntityListBase<I>            - Deleted item tracking, modification state
```

Choose based on requirements:

| Base Class | Use Case |
|------------|----------|
| `ValidateListBase<I>` | Collections with validation |
| `EntityListBase<I>` | Full entity collections with persistence |

## Defining a Collection

### Interface

<!-- snippet: interface-definition -->
```cs
/// <summary>
/// Collection interface with domain-specific methods.
/// </summary>
public interface IPhoneList : IEntityListBase<IPhone>
{
    IPhone AddPhoneNumber();
    void RemovePhoneNumber(IPhone phone);
}
```
<!-- endSnippet -->

### Implementation

<!-- snippet: list-implementation -->
```cs
/// <summary>
/// EntityListBase implementation with factory injection.
/// </summary>
[Factory]
internal class PhoneList : EntityListBase<IPhone>, IPhoneList
{
    private readonly IPhoneFactory _phoneFactory;

    public PhoneList([Service] IPhoneFactory phoneFactory)
    {
        _phoneFactory = phoneFactory;
    }

    public IPhone AddPhoneNumber()
    {
        var phone = _phoneFactory.Create();
        Add(phone);  // Marks as child, sets parent
        return phone;
    }

    public void RemovePhoneNumber(IPhone phone)
    {
        Remove(phone);  // Marks for deletion if not new
    }

    [Fetch]
    public void Fetch(IEnumerable<PhoneEntity> entities,
                      [Service] IPhoneFactory phoneFactory)
    {
        foreach (var entity in entities)
        {
            var phone = phoneFactory.Fetch(entity);
            Add(phone);
        }
    }

    [Update]
    public void Update(ICollection<PhoneEntity> entities,
                       [Service] IPhoneFactory phoneFactory)
    {
        // Process all items including deleted ones
        foreach (var phone in this.Union(DeletedList))
        {
            PhoneEntity entity;

            if (phone.IsNew)
            {
                // Create new EF entity
                entity = new PhoneEntity();
                entities.Add(entity);
            }
            else
            {
                // Find existing EF entity
                entity = entities.Single(e => e.Id == phone.Id);
            }

            if (phone.IsDeleted)
            {
                // Remove from EF collection
                entities.Remove(entity);
            }
            else
            {
                // Save the item (insert or update)
                phoneFactory.Save(phone, entity);
            }
        }
    }

    [Create]
    public void Create() { }
}
```
<!-- endSnippet -->

## Adding Items

### Always Use Parent's Add Methods

Child entities should be created through the parent collection's add methods, not by calling child factories directly:

<!-- invalid:add-methods-antipattern -->
```csharp
// CORRECT - Use parent's domain method
var phone = contact.PhoneNumbers.AddPhoneNumber();
phone.Number = "555-1234";

// WRONG - Calling child factory directly
var phone = phoneFactory.Create();  // Don't do this
phone.Number = "555-1234";
contact.PhoneNumbers.Add(phone);    // Missing parent relationship setup
```
<!-- /snippet -->

**Why this matters:**
- The parent's add method ensures proper parent-child relationships
- Factory injection is handled by the list, not the caller
- Domain operations belong on the aggregate, not scattered in consuming code

If you find yourself injecting child factories outside the aggregate, refactor to expose an add method on the collection interface.

### What Happens When Items Are Added

When items are added to an EntityListBase:

1. **Child marking** - `item.MarkAsChild()` is called
2. **Parent assignment** - `item.SetParent(parentEntity)` is called
3. **Undelete** - If item was deleted, it's undeleted
4. **Modification** - If item is not new, it's marked modified

<!-- pseudo:add-item-behavior -->
```csharp
var item = lineItemFactory.Create();
lineItems.Add(item);
// item.IsChild = true
// item.Parent = parent entity (not the list)
```
<!-- /snippet -->

## Removing Items

When items are removed:

1. **New items** - Simply removed from list
2. **Existing items** - Marked as deleted and moved to `DeletedList`

<!-- pseudo:remove-item-behavior -->
```csharp
lineItems.Remove(item);

if (item.IsNew)
{
    // Just removed, no deletion needed
}
else
{
    // item.IsDeleted = true
    // item is in lineItems.DeletedList
}
```
<!-- /snippet -->

### Delete/Remove Consistency

Calling `entity.Delete()` on an entity in a list is equivalent to calling `list.Remove(entity)`. Both operations:

1. Remove the entity from the list
2. Mark it as deleted (`IsDeleted = true`)
3. Add it to the `DeletedList` (if not new)

<!-- pseudo:delete-remove-equivalence -->
```csharp
// These are equivalent:
order.LineItems.Remove(item);
item.Delete();
```
<!-- /snippet -->

For standalone entities (not in a list), `Delete()` simply sets `IsDeleted = true`.

### DeletedList

When existing items are removed from a list, they are moved to `DeletedList` rather than discarded. This allows your `[Update]` factory method to delete them from the database.

> **Critical**: Your list's `[Update]` method must iterate `this.Union(DeletedList)` to process
> both active and deleted items. If you only iterate `this`, removed items will silently
> remain in the database.

<!-- pseudo:update-with-deletedlist -->
```csharp
// In Update operation - MUST include DeletedList
foreach (var item in lineItems.Union(lineItems.DeletedList))
{
    if (item.IsDeleted)
    {
        // Delete from database
    }
    // ...
}
```
<!-- /snippet -->

See [List Factory Operations](factory-operations.md#list-factory-operations) for complete examples.

### Intra-Aggregate Moves

Entities can be moved between lists within the same aggregate (same `Root`). When an entity is added to a new list after being removed from another list in the same aggregate:

1. Entity is removed from the old list's `DeletedList`
2. Entity is undeleted (`IsDeleted = false`)
3. Entity is added to the new list

<!-- pseudo:intra-aggregate-moves -->
```csharp
// Company aggregate with two departments
var project = dept1.Projects[0];

// Remove from Dept1's projects
dept1.Projects.Remove(project);
// project.IsDeleted = true
// project in dept1.Projects.DeletedList

// Add to Dept2's projects (same aggregate)
dept2.Projects.Add(project);
// project removed from dept1.Projects.DeletedList
// project.IsDeleted = false
// project now in dept2.Projects
```
<!-- /snippet -->

**Cross-aggregate moves are not allowed:**

<!-- invalid:cross-aggregate-move-error -->
```csharp
var project = company1.Dept.Projects[0];

// Attempt to move to different aggregate
company2.Dept.Projects.Add(project);  // THROWS InvalidOperationException
```
<!-- /snippet -->

## Collection Properties

### From EntityListBase

| Property | Description |
|----------|-------------|
| `IsModified` | Any item modified or items pending deletion |
| `IsSelfModified` | Always false (lists don't have own properties) |
| `DeletedList` | Items removed but needing deletion |

### From ValidateListBase

| Property | Description |
|----------|-------------|
| `IsValid` | All items are valid |
| `IsSelfValid` | Always true (lists have no validation) |
| `PropertyMessages` | Aggregated messages from all items |
| `IsPaused` | Rule execution is paused |

### From ValidateListBase (base properties)

| Property | Description |
|----------|-------------|
| `IsBusy` | Any item is busy |
| `Parent` | Parent entity (items get this as their parent) |
| `Count` | Number of items |

## Fetch Operation

<!-- snippet: fetch-operation -->
```cs
[Fetch]
public void Fetch(IEnumerable<PhoneEntity> entities,
                  [Service] IPhoneFactory phoneFactory)
{
    foreach (var entity in entities)
    {
        var phone = phoneFactory.Fetch(entity);
        Add(phone);
    }
}
```
<!-- endSnippet -->

## Save Operation

Handle inserts, updates, and deletes:

<!-- snippet: collections-update-operation -->
```cs
[Update]
public void Update(ICollection<PhoneEntity> entities,
                   [Service] IPhoneFactory phoneFactory)
{
    // Process all items including deleted ones
    foreach (var phone in this.Union(DeletedList))
    {
        PhoneEntity entity;

        if (phone.IsNew)
        {
            // Create new EF entity
            entity = new PhoneEntity();
            entities.Add(entity);
        }
        else
        {
            // Find existing EF entity
            entity = entities.Single(e => e.Id == phone.Id);
        }

        if (phone.IsDeleted)
        {
            // Remove from EF collection
            entities.Remove(entity);
        }
        else
        {
            // Save the item (insert or update)
            phoneFactory.Save(phone, entity);
        }
    }
}
```
<!-- endSnippet -->

## Validation Across Items

Run rules on all items:

<!-- pseudo:running-rules-on-items -->
```csharp
// Run rules for a specific property on all items
await lineItems.RunRules(nameof(IOrderLineItem.Quantity));

// Run all rules on all items
await lineItems.RunRules();
```
<!-- /snippet -->

## Cross-Item Validation

Handle property changes that affect sibling validation:

<!-- snippet: cross-item-validation -->
```cs
/// <summary>
/// List that re-validates siblings when properties change.
/// </summary>
public interface IContactPhoneList : IEntityListBase<IContactPhone>
{
    IContactPhone AddPhone();
}

[Factory]
internal class ContactPhoneList : EntityListBase<IContactPhone>, IContactPhoneList
{
    private readonly IContactPhoneFactory _phoneFactory;

    public ContactPhoneList([Service] IContactPhoneFactory phoneFactory)
    {
        _phoneFactory = phoneFactory;
    }

    public IContactPhone AddPhone()
    {
        var phone = _phoneFactory.Create();
        Add(phone);
        return phone;
    }

    /// <summary>
    /// Re-validate siblings when PhoneType changes to enforce uniqueness.
    /// </summary>
    protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        await base.HandleNeatooPropertyChanged(eventArgs);

        // When PhoneType changes, re-validate all other items for uniqueness
        if (eventArgs.PropertyName == nameof(IContactPhone.PhoneType))
        {
            if (eventArgs.Source is IContactPhone changedPhone)
            {
                // Re-run rules on all OTHER items
                await Task.WhenAll(
                    this.Except([changedPhone])
                        .Select(phone => phone.RunRules()));
            }
        }
    }

    [Create]
    public void Create() { }
}
```
<!-- endSnippet -->

## Custom Add Methods

Provide domain-specific add methods:

<!-- pseudo:custom-add-methods -->
```csharp
public interface IPersonPhoneList : IEntityListBase<IPersonPhone>
{
    IPersonPhone AddPhoneNumber();
    Task RemovePhoneNumber(IPersonPhone phone);
}

[Factory]
internal class PersonPhoneList : EntityListBase<IPersonPhone>, IPersonPhoneList
{
    private readonly IPersonPhoneFactory _phoneFactory;

    public PersonPhoneList([Service] IPersonPhoneFactory phoneFactory)
    {
        _phoneFactory = phoneFactory;
    }

    public IPersonPhone AddPhoneNumber()
    {
        var phone = _phoneFactory.Create();
        Add(phone);
        return phone;
    }

    public async Task RemovePhoneNumber(IPersonPhone phone)
    {
        Remove(phone);
        await RunRules();  // Re-validate after removal
    }
}
```
<!-- /snippet -->

## UI Binding

Bind collections in Blazor:

<!-- pseudo:blazor-ui-binding -->
```razor
@foreach (var item in order.LineItems)
{
    <MudNeatooTextField T="string" EntityProperty="@item[nameof(IOrderLineItem.Description)]" />
    <MudNeatooNumericField T="decimal" EntityProperty="@item[nameof(IOrderLineItem.Amount)]" />
    <MudIconButton Icon="@Icons.Material.Filled.Delete" OnClick="@(() => RemoveItem(item))" />
}

<MudButton OnClick="AddItem">Add Line Item</MudButton>

@if (!order.LineItems.IsValid)
{
    <MudAlert Severity="Severity.Error">Please correct line item errors</MudAlert>
}

@code {
    private void AddItem()
    {
        order.LineItems.AddItem();
    }

    private void RemoveItem(IOrderLineItem item)
    {
        order.LineItems.RemoveItem(item);
    }
}
```
<!-- /snippet -->

## Pausing Actions

During bulk operations:

<!-- pseudo:pausing-during-bulk-ops -->
```csharp
// Pause is set during:
// - Factory operations (FactoryStart/FactoryComplete)
// - Deserialization (OnDeserializing/OnDeserialized)

// When paused:
// - Items added go to DeletedList if deleted
// - No modification tracking
// - No rule execution
```
<!-- /snippet -->

## Complete Example

<!-- pseudo:complete-personphonelist -->
```csharp
public interface IPersonPhoneList : IEntityListBase<IPersonPhone>
{
    IPersonPhone AddPhoneNumber();
    Task RemovePhoneNumber(IPersonPhone phone);
}

[Factory]
internal class PersonPhoneList : EntityListBase<IPersonPhone>, IPersonPhoneList
{
    private readonly IPersonPhoneFactory _phoneFactory;

    public PersonPhoneList([Service] IPersonPhoneFactory phoneFactory)
    {
        _phoneFactory = phoneFactory;
    }

    public IPersonPhone AddPhoneNumber()
    {
        var phone = _phoneFactory.Create();
        Add(phone);
        return phone;
    }

    public async Task RemovePhoneNumber(IPersonPhone phone)
    {
        Remove(phone);
        await RunRules();
    }

    // Re-validate siblings when properties change
    protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        await base.HandleNeatooPropertyChanged(eventArgs);

        if (eventArgs.PropertyName == nameof(IPersonPhone.PhoneType) ||
            eventArgs.PropertyName == nameof(IPersonPhone.PhoneNumber))
        {
            if (eventArgs.Source is IPersonPhone changedPhone)
            {
                await Task.WhenAll(
                    this.Except([changedPhone])
                        .Select(p => p.RunRules()));
            }
        }
    }

    [Fetch]
    public void Fetch(IEnumerable<PersonPhoneEntity> entities,
                      [Service] IPersonPhoneFactory phoneFactory)
    {
        foreach (var entity in entities)
        {
            var phone = phoneFactory.Fetch(entity);
            Add(phone);
        }
    }

    [Update]
    public void Update(ICollection<PersonPhoneEntity> entities,
                       [Service] IPersonPhoneFactory phoneFactory)
    {
        foreach (var phone in this.Union(DeletedList))
        {
            PersonPhoneEntity entity;

            if (phone.IsNew)
            {
                entity = new PersonPhoneEntity();
                entities.Add(entity);
            }
            else
            {
                entity = entities.Single(e => e.Id == phone.Id);
            }

            if (phone.IsDeleted)
            {
                entities.Remove(entity);
            }
            else
            {
                phoneFactory.Save(phone, entity);
            }
        }
    }
}
```
<!-- /snippet -->

## See Also

- [Aggregates and Entities](aggregates-and-entities.md) - Parent entity patterns
- [Factory Operations](factory-operations.md) - Collection factory methods
- [Validation and Rules](validation-and-rules.md) - Cross-item validation
