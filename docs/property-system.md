# Property System

Neatoo's property system provides change tracking, validation state, and UI binding capabilities through a wrapper layer around property values.

## Overview

When you declare a partial property:

```csharp
public partial string? Name { get; set; }
```

Neatoo source-generates an implementation that:
- Stores the value in an `IProperty` wrapper
- Tracks modifications
- Triggers validation rules
- Raises property change notifications
- Integrates with async task tracking

## Getter and Setter

The generated implementation uses `Getter<T>` and `Setter<T>`:

```csharp
// Source-generated implementation
public string? Name
{
    get => Getter<string>();
    set => Setter(value);
}
```

### Getter Behavior

```csharp
protected virtual P? Getter<P>(string propertyName)
{
    return (P?)PropertyManager[propertyName]?.Value;
}
```

### Setter Behavior

The setter:
1. Updates the property value
2. Triggers validation rules for the property
3. Raises `PropertyChanged` and `NeatooPropertyChanged`
4. Updates `IsModified` state
5. Tracks async rule tasks

## Property Access

Access property wrappers through the indexer:

```csharp
// Get the Name property wrapper
IEntityProperty nameProperty = person[nameof(IPerson.Name)];

// Access property metadata
string value = (string)nameProperty.Value;
bool isModified = nameProperty.IsModified;
bool isBusy = nameProperty.IsBusy;
```

## IProperty Interface

Base property interface available on all Neatoo objects:

```csharp
public interface IProperty
{
    string Name { get; }
    object? Value { get; }
    bool IsBusy { get; }
    bool IsReadOnly { get; }

    Task SetValue(object? value);
    void LoadValue(object? value);
    Task WaitForTasks();
}
```

## IValidateProperty Interface

Extended for validated properties:

```csharp
public interface IValidateProperty : IProperty
{
    bool IsValid { get; }
    IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }

    void ClearAllMessages();
    void ClearMessagesForRule(uint ruleIndex);
    void SetMessagesForRule(IReadOnlyList<IRuleMessage> messages);

    void AddMarkedBusy(int id);
    void RemoveMarkedBusy(int id);
}
```

## IEntityProperty Interface

Full entity property with modification tracking:

```csharp
public interface IEntityProperty : IValidateProperty
{
    bool IsModified { get; }
    string? DisplayName { get; }

    void MarkUnmodified();
}
```

## Property Operations

### SetValue vs LoadValue

```csharp
// SetValue - triggers rules and marks modified
await person[nameof(Name)].SetValue("John");

// LoadValue - silent set, no rules or modification tracking
person[nameof(Name)].LoadValue("John");
```

Use `LoadValue` when:
- Loading data from database (in Fetch)
- Setting values in rules without triggering more rules
- Initializing default values

### Check Modification

```csharp
if (person[nameof(Name)].IsModified)
{
    // Property has changed since last save
}

// Get all modified property names
IEnumerable<string> modifiedProps = person.ModifiedProperties;
```

### Clear Modification

```csharp
// Mark single property as unmodified
person[nameof(Name)].MarkUnmodified();

// Called automatically after successful save
```

## Property Messages

Validation messages are attached to properties:

```csharp
var property = person[nameof(Email)];

// Check if property is valid
if (!property.IsValid)
{
    // Get all messages for this property
    foreach (var msg in property.PropertyMessages)
    {
        Console.WriteLine(msg.Message);
    }
}
```

### PropertyMessage Interface

```csharp
public interface IPropertyMessage
{
    IProperty Property { get; }
    string Message { get; }
}
```

## Busy State

Properties track async operations:

```csharp
var property = person[nameof(Email)];

// Check if async validation is running
if (property.IsBusy)
{
    // Show loading indicator
}

// Wait for all async operations
await property.WaitForTasks();
```

### Entity-Level Busy

```csharp
// True if any property is busy
if (person.IsBusy)
{
    // Disable save button
}

// Wait for all properties
await person.WaitForTasks();
```

## Display Name

Properties can have display names from `[DisplayName]`:

```csharp
[DisplayName("First Name*")]
public partial string? FirstName { get; set; }

// Access display name
string label = person[nameof(FirstName)].DisplayName;  // "First Name*"
```

## Read-Only State

Properties can be marked read-only:

```csharp
if (person[nameof(Name)].IsReadOnly)
{
    // Disable input
}
```

## Property Manager

The `PropertyManager` manages all properties on an entity:

```csharp
// Access via protected property
protected IPropertyManager<IProperty> PropertyManager { get; }

// Check if property exists
bool hasName = PropertyManager.HasProperty("Name");

// Get all properties
IEnumerable<IProperty> allProps = PropertyManager.GetProperties;

// Check modification state
bool isModified = PropertyManager.IsModified;
bool isSelfModified = PropertyManager.IsSelfModified;
```

## Pausing Property Actions

During bulk operations, pause property tracking:

```csharp
using (person.PauseAllActions())
{
    person.FirstName = "John";
    person.LastName = "Doe";
    person.Email = "john@example.com";
}
// Rules run after block completes
```

When paused:
- No `PropertyChanged` events
- No validation rules triggered
- No modification tracking updates

## UI Binding

Properties integrate with Blazor binding:

```razor
<!-- Bind to property value -->
<MudTextField Value="@((string)person[nameof(Name)].Value)"
              ValueChanged="@(async v => await person[nameof(Name)].SetValue(v))" />

<!-- Or use Neatoo components -->
<MudNeatooTextField T="string" EntityProperty="@person[nameof(Name)]" />
```

The Neatoo components automatically handle:
- Two-way binding
- Validation message display
- Busy state indication
- Read-only state

## Property Change Events

Properties raise change notifications:

```csharp
// Standard PropertyChanged
person.PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(IPerson.Name))
    {
        // Name changed
    }
};

// Extended NeatooPropertyChanged
person.NeatooPropertyChanged += async (args) =>
{
    // Rich event with source tracking
    string propertyPath = args.FullPropertyName;
    object source = args.Source;
};
```

## Complete Example

```csharp
// Access and manipulate properties
var person = personFactory.Create();

// Set values (triggers rules)
person.FirstName = "John";
person.LastName = "Doe";

// Wait for async validation
await person.WaitForTasks();

// Check property state
var emailProp = person[nameof(IPerson.Email)];
Console.WriteLine($"Email IsModified: {emailProp.IsModified}");
Console.WriteLine($"Email IsValid: {emailProp.IsValid}");
Console.WriteLine($"Email IsBusy: {emailProp.IsBusy}");
Console.WriteLine($"Email DisplayName: {emailProp.DisplayName}");

// Check messages
foreach (var msg in emailProp.PropertyMessages)
{
    Console.WriteLine($"Validation: {msg.Message}");
}

// Entity-level state
Console.WriteLine($"Person IsModified: {person.IsModified}");
Console.WriteLine($"Person IsValid: {person.IsValid}");
Console.WriteLine($"Person IsSavable: {person.IsSavable}");

// Modified properties
foreach (var prop in person.ModifiedProperties)
{
    Console.WriteLine($"Modified: {prop}");
}
```

## See Also

- [Meta-Properties Reference](meta-properties.md) - All meta-property details
- [Validation and Rules](validation-and-rules.md) - Property validation
- [Blazor Binding](blazor-binding.md) - UI property binding
