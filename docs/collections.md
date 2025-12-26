# Entity Collections

Neatoo provides list base classes for managing child entity collections within aggregates.

## Class Hierarchy

```
ListBase<I>                  - Observable collection, parent-child relationships
    |
ValidateListBase<I>          - Aggregated validation across items
    |
EntityListBase<I>            - Deleted item tracking, modification state
```

Choose based on requirements:

| Base Class | Use Case |
|------------|----------|
| `ListBase<I>` | Read-only collections |
| `ValidateListBase<I>` | Collections with validation |
| `EntityListBase<I>` | Full entity collections with persistence |

## Defining a Collection

### Interface

```csharp
public interface IOrderLineItemList : IEntityListBase<IOrderLineItem>
{
    IOrderLineItem AddItem();
    void RemoveItem(IOrderLineItem item);
}
```

### Implementation

```csharp
[Factory]
internal class OrderLineItemList : EntityListBase<IOrderLineItem>, IOrderLineItemList
{
    private readonly IOrderLineItemFactory _itemFactory;

    public OrderLineItemList([Service] IOrderLineItemFactory itemFactory)
    {
        _itemFactory = itemFactory;
    }

    public IOrderLineItem AddItem()
    {
        var item = _itemFactory.Create();
        Add(item);  // Marks as child, sets parent
        return item;
    }

    public void RemoveItem(IOrderLineItem item)
    {
        Remove(item);  // Marks for deletion if not new
    }
}
```

## Adding Items

When items are added to an EntityListBase:

1. **Child marking** - `item.MarkAsChild()` is called
2. **Parent assignment** - `item.SetParent(parentEntity)` is called
3. **Undelete** - If item was deleted, it's undeleted
4. **Modification** - If item is not new, it's marked modified

```csharp
var item = lineItemFactory.Create();
lineItems.Add(item);
// item.IsChild = true
// item.Parent = parent entity (not the list)
```

## Removing Items

When items are removed:

1. **New items** - Simply removed from list
2. **Existing items** - Marked as deleted and moved to `DeletedList`

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

### DeletedList

The `DeletedList` contains items marked for deletion during save:

```csharp
// In Update operation
foreach (var item in lineItems.Union(lineItems.DeletedList))
{
    if (item.IsDeleted)
    {
        // Delete from database
    }
    // ...
}
```

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

### From ListBase

| Property | Description |
|----------|-------------|
| `IsBusy` | Any item is busy |
| `Parent` | Parent entity (items get this as their parent) |
| `Count` | Number of items |

## Fetch Operation

```csharp
[Fetch]
public void Fetch(IEnumerable<LineItemEntity> entities,
                  [Service] IOrderLineItemFactory itemFactory)
{
    foreach (var entity in entities)
    {
        var item = itemFactory.Fetch(entity);
        Add(item);
    }
}
```

## Save Operation

Handle inserts, updates, and deletes:

```csharp
[Update]
public void Update(ICollection<LineItemEntity> entities,
                   [Service] IOrderLineItemFactory itemFactory)
{
    // Process all items including deleted ones
    foreach (var item in this.Union(DeletedList))
    {
        LineItemEntity entity;

        if (item.IsNew)
        {
            // Create new EF entity
            entity = new LineItemEntity();
            entities.Add(entity);
        }
        else
        {
            // Find existing EF entity
            entity = entities.Single(e => e.Id == item.Id);
        }

        if (item.IsDeleted)
        {
            // Remove from EF collection
            entities.Remove(entity);
        }
        else
        {
            // Save the item (insert or update)
            itemFactory.Save(item, entity);
        }
    }
}
```

## Validation Across Items

Run rules on all items:

```csharp
// Run rules for a specific property on all items
await lineItems.RunRules(nameof(IOrderLineItem.Quantity));

// Run all rules on all items
await lineItems.RunRules();
```

## Cross-Item Validation

Handle property changes that affect sibling validation:

```csharp
protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    await base.HandleNeatooPropertyChanged(eventArgs);

    // When PhoneType changes, re-validate all other items for uniqueness
    if (eventArgs.PropertyName == nameof(IPersonPhone.PhoneType))
    {
        if (eventArgs.Source is IPersonPhone changedPhone)
        {
            // Re-run rules on all OTHER items
            await Task.WhenAll(
                this.Except([changedPhone])
                    .Select(phone => phone.RunRules()));
        }
    }
}
```

## Custom Add Methods

Provide domain-specific add methods:

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

## UI Binding

Bind collections in Blazor:

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

## Pausing Actions

During bulk operations:

```csharp
// Pause is set during:
// - Factory operations (FactoryStart/FactoryComplete)
// - Deserialization (OnDeserializing/OnDeserialized)

// When paused:
// - Items added go to DeletedList if deleted
// - No modification tracking
// - No rule execution
```

## Complete Example

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

## See Also

- [Aggregates and Entities](aggregates-and-entities.md) - Parent entity patterns
- [Factory Operations](factory-operations.md) - Collection factory methods
- [Validation and Rules](validation-and-rules.md) - Cross-item validation
