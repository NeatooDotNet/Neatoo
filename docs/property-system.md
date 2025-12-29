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
public interface IProperty : INotifyPropertyChanged, INotifyNeatooPropertyChanged
{
    string Name { get; }
    object? Value { get; set; }
    bool IsBusy { get; }
    bool IsReadOnly { get; }
    Type Type { get; }
    Task Task { get; }
    string? StringValue { get; }

    Task SetValue(object? newValue);
    void LoadValue(object? value);
    Task WaitForTasks();
    void AddMarkedBusy(long id);
    void RemoveMarkedBusy(long id);
}
```

> **Note:** `IProperty<T>` provides a strongly-typed `Value` property.

## IValidateProperty Interface

Extended for validated properties:

```csharp
public interface IValidateProperty : IProperty
{
    bool IsSelfValid { get; }  // This property only, excluding children
    bool IsValid { get; }      // This property and all children
    IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }

    Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null);
}
```

> **Note:** Methods like `ClearAllMessages()`, `SetMessagesForRule()` are internal and managed by the framework.

## IEntityProperty Interface

Full entity property with modification tracking:

```csharp
public interface IEntityProperty : IValidateProperty
{
    bool IsPaused { get; set; }      // Events and tracking suspended
    bool IsModified { get; }          // This property or children modified
    bool IsSelfModified { get; }      // This property value modified
    string DisplayName { get; }       // UI display name

    void MarkSelfUnmodified();        // Reset modification state
}
```

## Property Interface to Base Class Mapping

Each base class uses a specific property interface level:

| Base Class | Property Interface | Features |
|------------|-------------------|----------|
| `Base<T>` | `IProperty` | Value storage, busy tracking, notifications |
| `ValidateBase<T>` | `IValidateProperty` | + Validation, rule messages, IsValid |
| `EntityBase<T>` | `IEntityProperty` | + Modification tracking, pause/resume |

```csharp
// On ValidateBase - get validation property
IValidateProperty prop = validateObject[nameof(Name)];
bool isValid = prop.IsValid;

// On EntityBase - get full entity property
IEntityProperty entityProp = entity[nameof(Name)];
bool isModified = entityProp.IsModified;
string label = entityProp.DisplayName;
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
person[nameof(Name)].MarkSelfUnmodified();

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

During bulk operations, pause property tracking to improve performance and avoid intermediate validation states. The `PauseAllActions()` method returns an `IDisposable` that automatically resumes when disposed.

```csharp
using (person.PauseAllActions())
{
    person.FirstName = "John";
    person.LastName = "Doe";
    person.Email = "john@example.com";
}
// Meta-state recalculated when disposed
// You can then call RunRules() if validation is needed
```

### What Gets Paused

When paused via `PauseAllActions()`:

| Feature | Behavior When Paused |
|---------|---------------------|
| **PropertyChanged events** | Not raised - no UI updates |
| **NeatooPropertyChanged events** | Not raised - no async notifications |
| **Validation rules** | Not triggered by property changes |
| **Modification tracking** | Values tracked internally but events suppressed |
| **Meta-property updates** | Deferred until resume |

### What Does NOT Get Paused

- **Value storage** - Property values are still stored
- **Direct method calls** - Calling `RunRules()` directly still works
- **Child object pausing** - Child objects are NOT automatically paused (see below)

### When Pausing Happens Automatically

The framework automatically pauses actions during these operations:

| Operation | Method | Purpose |
|-----------|--------|---------|
| Factory Create | `FactoryStart()` | Prevent events during entity creation |
| Factory Fetch | `FactoryStart()` | Prevent rules during data loading |
| Factory Insert/Update/Delete | `FactoryStart()` | Prevent events during persistence |
| JSON Deserialization | `OnDeserializing()` | Prevent rules during state reconstruction |

```csharp
// Automatic pause flow in factory operations
public virtual void FactoryStart(FactoryOperation factoryOperation)
{
    PauseAllActions();  // Called by framework
}

public virtual void FactoryComplete(FactoryOperation factoryOperation)
{
    ResumeAllActions();  // Called by framework
}
```

### Manual Pause Use Cases

#### Bulk Property Updates

```csharp
// Without pause: 3 rule executions, 3 PropertyChanged events
person.FirstName = "John";
person.LastName = "Doe";
person.Age = 30;

// With pause: 0 rule executions during block, meta-state recalculated once
using (person.PauseAllActions())
{
    person.FirstName = "John";
    person.LastName = "Doe";
    person.Age = 30;
}
await person.RunRules();  // Run rules once after all changes
```

#### Loading External Data

```csharp
using (customer.PauseAllActions())
{
    // Load data from external source without triggering validation
    customer.Name = externalData.Name;
    customer.Email = externalData.Email;
    customer.Phone = externalData.Phone;
    customer.Address = externalData.Address;
}
// Validate everything once at the end
await customer.RunRules();
if (!customer.IsValid)
{
    // Handle validation errors
}
```

#### Bulk Collection Operations

```csharp
// Add multiple items efficiently
using (order.PauseAllActions())
{
    foreach (var item in bulkItems)
    {
        var lineItem = lineItemFactory.Create();
        lineItem.ProductId = item.ProductId;
        lineItem.Quantity = item.Quantity;
        order.LineItems.Add(lineItem);
    }
}
```

### Checking Pause State

```csharp
if (person.IsPaused)
{
    // Actions are currently suspended
    Console.WriteLine("Object is paused - changes won't trigger rules");
}
```

### Resume Behavior

When the `using` block completes (or `ResumeAllActions()` is called directly):

1. `IsPaused` is set to `false`
2. All property managers are un-paused
3. Meta-state is recalculated (`IsValid`, `IsModified`, `IsBusy`, `IsSavable`)
4. PropertyChanged events resume for future changes
5. **Note:** Rules do NOT automatically run - call `RunRules()` if needed

```csharp
using (person.PauseAllActions())
{
    person.Name = "Test";
}
// At this point:
// - IsPaused = false
// - IsModified = true (recalculated)
// - IsValid = previous state (rules haven't run yet)

await person.RunRules();  // NOW validation runs
// - IsValid = reflects current state
```

### Direct Resume (Without Using Block)

You can also call `ResumeAllActions()` directly:

```csharp
person.PauseAllActions();
try
{
    // Perform operations
    person.Name = "John";
    person.Email = "john@example.com";
}
finally
{
    person.ResumeAllActions();  // Always resume, even on exception
}
```

### Child Objects and Pausing

**Important:** `PauseAllActions()` on a parent does NOT automatically pause child objects. If you need to pause children, do so explicitly:

```csharp
using (order.PauseAllActions())
{
    // Order is paused, but LineItems are NOT
    foreach (var item in order.LineItems)
    {
        using (item.PauseAllActions())
        {
            item.Quantity = newQuantity;
        }
    }
}
```

### Nested Pause Calls

Multiple calls to `PauseAllActions()` are safe - each returns a disposable that calls `ResumeAllActions()`:

```csharp
using (person.PauseAllActions())  // Pauses
{
    person.Name = "John";

    using (person.PauseAllActions())  // Already paused, returns disposable
    {
        person.Email = "john@example.com";
    }  // Calls ResumeAllActions() - now unpaused!

    person.Age = 30;  // This WILL trigger rules (unpaused now)
}  // Calls ResumeAllActions() again (no-op, already unpaused)
```

**Best Practice:** Avoid nested pause blocks on the same object. Use a single pause block for all changes.

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

Neatoo provides two property change notification systems: the standard `PropertyChanged` event and the enhanced `NeatooPropertyChanged` event.

### Standard PropertyChanged

The familiar `INotifyPropertyChanged` event for UI binding:

```csharp
person.PropertyChanged += (sender, e) =>
{
    if (e.PropertyName == nameof(IPerson.Name))
    {
        Console.WriteLine("Name changed");
    }
};
```

**Characteristics:**
- Synchronous event
- Simple `PropertyName` string
- No source tracking for nested changes
- Sufficient for most UI binding scenarios

### NeatooPropertyChanged Event

The `NeatooPropertyChanged` event provides rich change tracking for complex object graphs:

```csharp
public delegate Task NeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs);

public interface INotifyNeatooPropertyChanged
{
    event NeatooPropertyChanged NeatooPropertyChanged;
}
```

**Key Differences from PropertyChanged:**

| Feature | PropertyChanged | NeatooPropertyChanged |
|---------|-----------------|----------------------|
| Return type | void | Task (async) |
| Property path | Single name | Full nested path |
| Source tracking | No | Yes - original change source |
| Event chain | No | Yes - InnerEventArgs |
| Async handlers | Not supported | Fully supported |

### NeatooPropertyChangedEventArgs

The event args provide detailed information about property changes:

```csharp
public record NeatooPropertyChangedEventArgs
{
    // The immediate property name that changed
    public string PropertyName { get; init; }

    // The property wrapper (if applicable)
    public IProperty? Property { get; init; }

    // The object where this event is being raised
    public object? Source { get; init; }

    // The original event args from the deepest nested change
    public NeatooPropertyChangedEventArgs OriginalEventArgs { get; init; }

    // The inner event args (from child object, if applicable)
    public NeatooPropertyChangedEventArgs? InnerEventArgs { get; init; }

    // Full property path from current object to original change
    // e.g., "Address.City" when City changes on nested Address
    public string FullPropertyName => ...;
}
```

### Understanding the Event Chain

When a property changes on a nested object, the event bubbles up through the object graph:

```
Order                      // Receives: PropertyName="LineItems", FullPropertyName="LineItems.Quantity"
  └── LineItems (List)     // Receives: PropertyName="Quantity", FullPropertyName="Quantity"
        └── LineItem       // Originates: PropertyName="Quantity", FullPropertyName="Quantity"
```

```csharp
// At the Order level
order.NeatooPropertyChanged += async (args) =>
{
    Console.WriteLine($"PropertyName: {args.PropertyName}");         // "LineItems"
    Console.WriteLine($"FullPropertyName: {args.FullPropertyName}"); // "LineItems.Quantity"
    Console.WriteLine($"Source: {args.Source?.GetType().Name}");     // "LineItem"

    // Navigate the event chain
    if (args.InnerEventArgs != null)
    {
        Console.WriteLine($"Inner property: {args.InnerEventArgs.PropertyName}"); // "Quantity"
    }

    // Access the original event
    Console.WriteLine($"Original: {args.OriginalEventArgs.PropertyName}"); // "Quantity"
};
```

### Subscribing to NeatooPropertyChanged

```csharp
// Subscribe
person.NeatooPropertyChanged += OnNeatooPropertyChanged;

// Handler must return Task
private Task OnNeatooPropertyChanged(NeatooPropertyChangedEventArgs args)
{
    Console.WriteLine($"Changed: {args.FullPropertyName}");
    return Task.CompletedTask;
}

// Unsubscribe
person.NeatooPropertyChanged -= OnNeatooPropertyChanged;
```

### Use Cases for NeatooPropertyChanged

#### 1. Tracking Nested Property Changes

```csharp
order.NeatooPropertyChanged += async (args) =>
{
    // React to any change anywhere in the order graph
    if (args.FullPropertyName.StartsWith("LineItems."))
    {
        // A line item property changed
        await RecalculateTotals();
    }
};
```

#### 2. Cross-Item Validation in Collections

Override `HandleNeatooPropertyChanged` in list classes to re-validate siblings:

```csharp
protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    await base.HandleNeatooPropertyChanged(eventArgs);

    // When PhoneType changes, re-validate all OTHER items for uniqueness
    if (eventArgs.PropertyName == nameof(IPersonPhone.PhoneType))
    {
        if (eventArgs.Source is IPersonPhone changedPhone)
        {
            // Re-run validation on all siblings (except the changed item)
            await Task.WhenAll(
                this.Except([changedPhone])
                    .Select(phone => phone.RunRules())
            );
        }
    }
}
```

#### 3. Parent Reacting to Child Changes

```csharp
// In ValidateBase, ChildNeatooPropertyChanged runs rules for the changed child property
protected override async Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    if (!this.IsPaused)
    {
        // Run rules that depend on the changed child property
        await this.RunRules(eventArgs.FullPropertyName);

        // Propagate to parent
        await base.ChildNeatooPropertyChanged(eventArgs);

        // Update meta-properties
        this.CheckIfMetaPropertiesChanged();
    }
}
```

#### 4. UI State Management in Blazor

```razor
@code {
    private IPerson person = default!;

    protected override void OnInitialized()
    {
        person = PersonFactory.Create();
        person.NeatooPropertyChanged += OnPropertyChanged;
    }

    private Task OnPropertyChanged(NeatooPropertyChangedEventArgs e)
    {
        // Log all changes for debugging
        Console.WriteLine($"Property changed: {e.FullPropertyName} on {e.Source}");

        // Trigger UI update
        return InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        person.NeatooPropertyChanged -= OnPropertyChanged;
    }
}
```

### When Events Are Raised

| Scenario | PropertyChanged | NeatooPropertyChanged |
|----------|-----------------|----------------------|
| Direct property set | Yes | Yes |
| `SetValue()` call | Yes | Yes |
| `LoadValue()` call | No | No |
| During `PauseAllActions()` | No | No |
| Child property changes | Yes (if configured) | Yes (bubbles up) |
| Meta-property changes (IsValid, etc.) | Yes | Yes |

### Event Flow Example

```csharp
// Given: Order -> LineItems -> LineItem.Quantity
lineItem.Quantity = 5;

// Event sequence:
// 1. LineItem raises NeatooPropertyChanged(PropertyName="Quantity")
// 2. LineItems.HandleNeatooPropertyChanged receives it
// 3. LineItems raises NeatooPropertyChanged(PropertyName="Quantity") - passes through
// 4. Order.ChildNeatooPropertyChanged receives it, wraps with PropertyName="LineItems"
// 5. Order runs rules triggered by "LineItems.Quantity"
// 6. Order raises NeatooPropertyChanged(FullPropertyName="LineItems.Quantity")
```

### Comparison: When to Use Which Event

| Use Case | Recommended Event |
|----------|-------------------|
| Simple UI binding | PropertyChanged |
| Nested change tracking | NeatooPropertyChanged |
| Cross-item validation | NeatooPropertyChanged |
| Blazor component updates | Either (PropertyChanged simpler) |
| Complex parent-child logic | NeatooPropertyChanged |
| Third-party library compatibility | PropertyChanged |

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
