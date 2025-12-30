# ListBase Parent-Setting Behavior

## Overview

In Neatoo, when items are added to a `ListBase<T>` collection, their `Parent` property is set to the **list's parent** (grandparent), not to the list itself. This is a subtle but important behavior that affects how you access parent relationships in child objects.

## How It Works

### Parent Chain

Consider this object hierarchy:
```
Person (aggregate root)
└── PersonPhoneList (child list)
    └── PersonPhone (list item)
```

When `PersonPhoneList` is assigned to `Person.PersonPhoneList`:
1. The list's `Parent` is set to `Person`

When `PersonPhone` is added to `PersonPhoneList`:
1. The phone's `Parent` is set to `PersonPhoneList.Parent` (which is `Person`)
2. **NOT** to `PersonPhoneList` itself

### Code Reference

In `ListBase.cs`:

```csharp
// When the list's parent is set, it cascades to all items
void ISetParent.SetParent(IBase parent)
{
    // The list is not the Parent
    this.Parent = parent;

    foreach (var item in this)
    {
        if (item is ISetParent setParent)
        {
            setParent.SetParent(parent);  // Items get the list's parent
        }
    }
}

// When an item is added, it gets the list's parent
protected override void InsertItem(int index, I item)
{
    ((ISetParent)item).SetParent(this.Parent);  // Not 'this', but 'this.Parent'
    base.InsertItem(index, item);
    // ...
}
```

## Accessing the Parent

### Correct Pattern

In list item classes (like `PersonPhone`), access the aggregate root directly:

```csharp
public class PersonPhone : EntityBase<PersonPhone>, IPersonPhone
{
    // Correct - Parent IS the Person (not the list)
    public IPerson? ParentPerson => this.Parent as IPerson;
}
```

### Common Mistake

Do NOT traverse an extra level thinking the list is in between:

```csharp
// WRONG - This would return null!
public IPerson? ParentPerson => this.Parent?.Parent as IPerson;
```

## Rationale

This design decision has several benefits:

1. **Simpler navigation**: List items can directly access their aggregate root without traversing through the list
2. **Cleaner rules**: Business rules in list items can easily reference the parent aggregate
3. **Consistent semantics**: From a domain perspective, `PersonPhone` belongs to `Person`, not to an intermediate list

## Timing Considerations

When adding items to a list:

1. If the list is already attached to a parent, new items immediately get that parent
2. If the list is NOT yet attached (e.g., created but not assigned), items will have `Parent = null`
3. When the list is later attached, all existing items get their parent updated

### Example Timeline

```csharp
var phoneList = factory.CreatePhoneList();  // phoneList.Parent = null
var phone = factory.CreatePhone();
phoneList.Add(phone);                       // phone.Parent = null (list has no parent yet)

person.PhoneList = phoneList;               // phoneList.Parent = person
                                            // AND phone.Parent = person (cascaded)
```

## Testing Implications

When writing tests, ensure the list is attached to its parent BEFORE adding items if you need the parent relationship to be set immediately:

```csharp
// Good - parent is set when items are added
var person = factory.Create();              // PhoneList already attached
var phone = person.PhoneList.AddPhone();    // phone.Parent = person immediately
Assert.NotNull(phone.ParentPerson);

// Problematic - parent not set yet
var phoneList = factory.CreatePhoneList();
var phone = factory.CreatePhone();
phoneList.Add(phone);
Assert.Null(phone.Parent);                  // Parent is null until list is attached!
```
