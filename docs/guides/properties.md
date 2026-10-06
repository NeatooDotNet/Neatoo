# Properties

[← Parent-Child](parent-child.md) | [↑ Guides](index.md) | [Remote Factory →](remote-factory.md)

Plain C# auto-properties don't fire change notifications, track validation state, execute business rules, or cascade dirty state to a parent aggregate. A data-binding UI needs all of these. Neatoo's property system wraps each property with a managed layer that intercepts gets and sets — giving the framework a single point to handle change tracking, rule execution, async task management, and parent notification. You declare `partial` properties; the source generator fills in the implementation.

> The code samples are MSTest tests and domain classes from the Neatoo Design projects. Tests resolve factories from a DI scope (`DesignTestServices.GetScope()`); in an application you inject the factory interface into the component that needs it.

## Partial Property Declaration

Properties in ValidateBase and EntityBase are declared as partial properties. The source generator completes the implementation by creating backing property accessors that retrieve strongly-typed property objects from PropertyManager. Every class gets a matched public interface; the concrete is `internal`:

<!-- snippet: skill-partial-property-class -->
<a id='snippet-skill-partial-property-class'></a>
```cs
[Factory]
internal partial class ValidationChildDemo : ValidateBase<ValidationChildDemo>, IValidationChildDemo
{
    public partial string? RequiredField { get; set; }

    public ValidationChildDemo(IValidateBaseServices<ValidationChildDemo> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.RequiredField) ? "Child field is required" : string.Empty,
            t => t.RequiredField);
    }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/StateProperties.cs#L96-L112' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-partial-property-class' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The source generator creates:
- A protected `NameProperty` accessor that retrieves `IValidateProperty<string>` from PropertyManager
- Full getter/setter implementation for the partial property
- Property change notifications
- Task tracking for async rules, propagated to the parent

The generator implements every `partial` property on a `[Factory]` class and preserves the accessibility you declare. It also creates an override of `InitializePropertyBackingFields` that registers each property with the PropertyManager during construction.

## Source-Generated Implementation

For each partial property the generator emits a protected accessor over the property object, a getter and setter over its `Value`, and the registration in `InitializePropertyBackingFields`. The shape is the same for ValidateBase and EntityBase; on an EntityBase the property factory creates an entity property, which adds modification tracking:

<!-- snippet: skill-generated-property-shape -->
<a id='snippet-skill-generated-property-shape'></a>
```cs
// For this declaration:
//   public partial string? Name { get; set; }
//
// GENERATOR BEHAVIOR: Neatoo.BaseGenerator produces the same shape for
// ValidateBase and EntityBase (from DemoEntity.g.cs):
//
//   protected IValidateProperty<string?> NameProperty
//       => (IValidateProperty<string?>)PropertyManager[nameof(Name)]!;
//
//   public partial string? Name
//   {
//       get => NameProperty.Value;
//       set
//       {
//           NameProperty.Value = value;
//           if (!NameProperty.Task.IsCompleted)
//           {
//               Parent?.AddChildTask(NameProperty.Task);
//               RunningTasks.AddTask(NameProperty.Task);
//           }
//       }
//   }
//
//   protected override void InitializePropertyBackingFields(IPropertyFactory<T> factory)
//   {
//       PropertyManager.Register(factory.Create<string?>(this, nameof(Name)));
//   }
//
// On an EntityBase the factory creates an entity property (modification
// tracking); the accessor is still typed IValidateProperty<T>. The real output
// is on disk under Generated/Neatoo.BaseGenerator/.
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/PropertyBasics.cs#L24-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-generated-property-shape' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The PropertyManager stores the actual property instances and handles validation, events, and state management behind the scenes.

## Property Objects and Meta-Properties

Each partial property is backed by its own property object, reached through the indexer `entity["Name"]`. It is not just a backing field: it owns the value, `IsValid`, `IsSelfValid`, `PropertyMessages`, `IsBusy`, `IsReadOnly`, and (on an entity) `IsModified`. This is what makes per-field UI feedback possible — an error icon next to the field that is broken, a spinner on just the field running an async lookup. The entity's `IsValid` and `IsBusy` are aggregations of its properties and children, so per-property tracking is the source of truth.

<!-- snippet: skill-property-metadata -->
<a id='snippet-skill-property-metadata'></a>
```cs
[TestMethod]
public void Indexer_ExposesPropertyMetadata()
{
    var entity = _factory.Create();

    // Each partial property is backed by its own property object
    var nameProperty = entity["Name"];

    entity.Name = "";  // Name is required
    Assert.IsFalse(nameProperty.IsValid);
    Assert.IsTrue(nameProperty.PropertyMessages.Count > 0);
    Assert.IsFalse(nameProperty.IsBusy);
    Assert.IsFalse(nameProperty.IsReadOnly);

    // The object aggregates every property's messages
    Assert.IsTrue(entity.PropertyMessages.Any(m => m.Property.Name == "Name"));

    entity.Name = "Set";
    Assert.IsTrue(nameProperty.IsValid);
    Assert.AreEqual(0, nameProperty.PropertyMessages.Count);

    // Strongly typed access by casting
    var typed = (Neatoo.IValidateProperty<string?>)nameProperty;
    Assert.AreEqual("Set", typed.Value);
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs#L83-L109' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-property-metadata' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Property objects provide:
- **Value**: Get or set the property value (cast to `IValidateProperty<T>` for the typed value)
- **IsValid / IsSelfValid**: Validation state of the property (and of a child object it holds)
- **PropertyMessages**: Validation messages for this property
- **IsBusy**: True while an async rule is running for this property
- **Task**: The pending rule task (`Task.CompletedTask` when not busy)
- **IsReadOnly**: True for a `private set` property or after `MarkReadOnly()`
- **RunRules**: Runs the rules of a child object held by the property (a no-op for a scalar)

`SetValue` is the awaitable way to set a property. The generated setter runs the same rules but returns no `Task`; a component that must wait for the rules calls `SetValue`:

<!-- snippet: skill-set-value -->
<a id='snippet-skill-set-value'></a>
```cs
[TestMethod]
public async Task SetValue_IsTheAwaitablePath()
{
    var entity = _factory.Create();

    // The property setter runs the same rules but returns no Task.
    // A component that needs to await the rules calls SetValue.
    await entity["Name"].SetValue("Manual Value");

    Assert.AreEqual("Manual Value", entity.Name);
    Assert.IsTrue(entity["Name"].IsValid);
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs#L111-L124' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-set-value' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Indexer patterns:
- Returns `IValidateProperty` (non-generic); cast to `IValidateProperty<T>` for strongly-typed access
- Throws `PropertyNotFoundException` if the property does not exist; use `TryGetProperty` for safe access
- Enables generic validation display and rule engines without reflection

## PropertyChanged Events

Properties raise two change events: standard `INotifyPropertyChanged` for UI binding and `NeatooPropertyChanged` for framework coordination.

### INotifyPropertyChanged

The standard `PropertyChanged` event fires when property values change:

<!-- snippet: skill-property-changed -->
<a id='snippet-skill-property-changed'></a>
```cs
[TestMethod]
public void Property_SetTriggersPropertyChanged()
{
    // Arrange
    var entity = _factory.Create();
    var changedProperties = new List<string>();
    entity.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

    // Act
    entity.Name = "Test";

    // Assert
    Assert.IsTrue(changedProperties.Contains("Name"));
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs#L45-L60' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-property-changed' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

PropertyChanged behavior:
- Fires after value changes via normal property setters
- Includes the property name in the event args
- Is not raised by the property object on `LoadValue`; the owning object still raises its own `PropertyChanged(name)` when it is not paused
- Does not include old/new value comparison
- Fires for meta-properties (`IsValid`, `IsSelfValid`, `IsBusy`, and on EntityBase: `IsModified`, `IsSelfModified`, `IsSavable`, `IsDeleted`)

Blazor does not subscribe to `INotifyPropertyChanged` on its own: the page subscribes and calls `StateHasChanged`, and display bindings re-render on that cycle.

### NeatooPropertyChanged

`INotifyPropertyChanged` is synchronous and carries only a property name — fine for UI binding but not enough for Neatoo's internals. `NeatooPropertyChanged` is async (needed for async rule execution and cascading) and carries richer metadata: the `ChangeReason`, a dotted `FullPropertyName` for changes that bubble up from descendants, and the `Source` object that originated the change:

<!-- snippet: skill-neatoo-property-changed -->
<a id='snippet-skill-neatoo-property-changed'></a>
```cs
[TestMethod]
public async Task NeatooPropertyChanged_CarriesFullNameAndReason()
{
    var entity = _factory.Create();
    var received = new List<Neatoo.NeatooPropertyChangedEventArgs>();
    entity.NeatooPropertyChanged += args =>
    {
        received.Add(args);
        return Task.CompletedTask;
    };

    entity.Name = "Test";
    await entity.WaitForTasks();

    var nameEvent = received.Single(e => e.PropertyName == "Name");
    Assert.AreEqual("Name", nameEvent.FullPropertyName, "A dotted path for descendants; the bare name here");
    Assert.AreEqual(Neatoo.ChangeReason.UserEdit, nameEvent.Reason, "A setter outside a factory operation is a user edit");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs#L62-L81' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-neatoo-property-changed' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

NeatooPropertyChanged provides:
- **PropertyName**: Name of the changed property
- **FullPropertyName**: Dotted path through nested objects (e.g., `"Items.LineTotal"`)
- **Source**: The object that raised the event
- **Reason**: `ChangeReason` (`UserEdit` or `Load`)
- **Property**: The `IValidateProperty` instance
- **OriginalEventArgs**: The root event that started the cascade; `InnerEventArgs` is the wrapped child event

The Reason distinguishes user edits (which trigger rules) from loads (which only establish structure).

## ChangeReason: UserEdit vs Load

Application code sets a value one way: the property setter. What it does depends on whether the object is paused.

- Inside a factory operation (`[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`) the object is paused, so plain assignment is a clean baseline load: nothing is marked modified, no rules run, no `PropertyChanged`. Do not use `LoadValue`, `PauseAllActions` or `MarkUnmodified` there.
- After the operation returns, a setter is a user edit (`ChangeReason.UserEdit`): the property and the entity are marked modified, rules run, `PropertyChanged` fires. The `NeatooPropertyChanged` test above shows the reason on a setter.

`LoadValue()` on the property object is a load (`ChangeReason.Load`) regardless of pause state. The framework uses it — deserialization, the generated `EntityLazyLoad` setter. It is not needed inside a factory operation:

<!-- snippet: skill-load-value-outside-operation -->
<a id='snippet-skill-load-value-outside-operation'></a>
```cs
[TestMethod]
public void LoadValue_DoesNotMarkPropertyModified()
{
    // Arrange
    var entity = _factory.Create();

    // Act
    entity["Name"].LoadValue("Loaded");

    // Assert
    Assert.IsFalse(entity["Name"].IsModified, "Property should not be marked modified via LoadValue");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/StatePropertyTests.cs#L46-L59' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-load-value-outside-operation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

LoadValue behavior:
- Validation rules do NOT execute
- The property is not marked modified
- The property object raises no `PropertyChanged`; the owning object raises `PropertyChanged(name)` when it is not paused
- `NeatooPropertyChanged` fires with `Reason = Load`
- Parent-child relationships ARE established (SetParent is called on a child object)

## Computed Properties: `private set` Plus a Rule

A derived value is a partial property with a `private set`, computed by an `AddAction` rule. The generator emits `private set` on the implementation and `get;` only on the interface, so consumers read it and cannot write it; the rule sets it through the private setter, which routes through `SetPrivateValue()`. `IsReadOnly` is `true`, so MudNeatoo renders it read-only without configuration. (A rule may also write a value with `LoadProperty`, which sets it without running the rules registered on that property.)

<!-- snippet: skill-private-set-property -->
<a id='snippet-skill-private-set-property'></a>
```cs
/// <summary>
/// Demonstrates: Private setter properties with computed values via rules.
/// </summary>
[Factory]
internal partial class PrivateSetPropertyDemo : EntityBase<PrivateSetPropertyDemo>, IPrivateSetPropertyDemo
{
    // Writable properties - external consumers can set these
    public partial int Quantity { get; set; }
    public partial decimal UnitPrice { get; set; }

    // Private-set property - only settable from within the entity
    // The interface exposes only `get;` - consumers see this as read-only
    // MudNeatoo components automatically bind ReadOnly="true"
    public partial decimal ComputedTotal { get; private set; }

    public PrivateSetPropertyDemo(IEntityBaseServices<PrivateSetPropertyDemo> services) : base(services)
    {
        // Rule: when Quantity or UnitPrice changes, recompute Total
        // The lambda sets the private setter, which calls SetPrivateValue internally
        RuleManager.AddAction(
            t => t.ComputedTotal = t.Quantity * t.UnitPrice,
            t => t.Quantity,
            t => t.UnitPrice);
    }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/PropertyBasics.cs#L173-L202' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-private-set-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The rule recomputes the value whenever an input changes, and `PropertyChanged` fires for the computed property, so a bound UI refreshes:

<!-- snippet: docs-private-set-rule-computes -->
<a id='snippet-docs-private-set-rule-computes'></a>
```cs
[TestMethod]
public void PrivateSet_RuleComputesValue()
{
    // Scenario 8: Private-set property set internally via rule
    // WHEN Quantity and UnitPrice are set, THEN ComputedTotal is updated by AddAction rule

    // Arrange
    var entity = _factory.Create();

    // Act
    entity.Quantity = 5;
    entity.UnitPrice = 10.00m;

    // Assert
    Assert.AreEqual(50.00m, entity.ComputedTotal);
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs#L179-L196' title='Snippet source file'>snippet source</a> | <a href='#snippet-docs-private-set-rule-computes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: docs-private-set-read-only -->
<a id='snippet-docs-private-set-read-only'></a>
```cs
[TestMethod]
public void PrivateSet_IsReadOnlyTrue()
{
    // Scenario 8/11: Private-set property has IsReadOnly=true
    // WHEN a property has private set, THEN its IsReadOnly is true

    // Arrange
    var entity = _factory.Create();

    // Act
    var totalProperty = entity["ComputedTotal"];

    // Assert
    Assert.IsTrue(totalProperty.IsReadOnly,
        "Private-set property should have IsReadOnly=true");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs#L218-L235' title='Snippet source file'>snippet source</a> | <a href='#snippet-docs-private-set-read-only' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: docs-private-set-set-value-throws -->
<a id='snippet-docs-private-set-set-value-throws'></a>
```cs
[TestMethod]
public void PrivateSet_SetValueThrows()
{
    // Scenario 9: SetValue on private-set property throws
    // WHEN entity["ComputedTotal"].SetValue(x) is called, THEN a PropertyException is thrown
    // (PropertyReadOnlyException is internal; verify via the public base class)

    // Arrange
    var entity = _factory.Create();

    // Act & Assert
    try
    {
        entity["ComputedTotal"].SetValue(99.99m);
        Assert.Fail("Expected PropertyException to be thrown for read-only property");
    }
    catch (Exception ex) when (ex is Neatoo.PropertyException)
    {
        // Expected: PropertyReadOnlyException (derives from PropertyException)
        StringAssert.Contains(ex.Message, "read-only");
    }
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs#L253-L276' title='Snippet source file'>snippet source</a> | <a href='#snippet-docs-private-set-set-value-throws' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Generated Behavior

For `public partial decimal ComputedTotal { get; private set; }`, the generator emits:
- **Property implementation:** `private set` accessor calling `SetPrivateValue(value)` (bypasses the `IsReadOnly` check)
- **Interface declaration:** `decimal ComputedTotal { get; }` (no setter exposed)
- **Backing field:** `IsReadOnly = true`

### Indexer Behavior

Accessing a private-set property through the indexer:
- `entity["Total"].SetValue(x)` throws `PropertyException` (`PropertyReadOnlyException` is internal; catch the public base class)
- `entity["Total"].LoadValue(x)` sets the value (framework and deserialization use)
- `entity["Total"].SetPrivateValue(x)` sets the value, bypassing `IsReadOnly`; rules and `PropertyChanged` run normally

### Protected and Internal Setters

`protected set` and `internal set` preserve their accessor visibility but do NOT set `IsReadOnly = true`. The runtime reports read-only for a get-only property or a private setter. Protected and internal setters use the standard `.Value = value` path.

### A Plain Getter Is Not a Neatoo Property

A regular (non-partial) computed getter is allowed but is not tracked: it raises no `PropertyChanged`, so a bound UI does not refresh it when its inputs change, and it is not serialized. Use it only for a value nothing binds to:

<!-- snippet: skill-plain-computed-getter -->
<a id='snippet-skill-plain-computed-getter'></a>
```cs
// =========================================================================
// Computed Property (not persisted)
// =========================================================================
// This is a regular property, not partial - not tracked by Neatoo and it
// raises no PropertyChanged. A bound UI does not refresh it when
// FirstName or LastName changes; for that, use a partial property set by
// an AddAction rule triggered on both.
// =========================================================================
public string FullName => $"{FirstName} {LastName}";
```
<sup><a href='/src/Design/Design.Domain/Entities/Employee.cs#L70-L80' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-plain-computed-getter' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Marking a Property Read-Only at Runtime

`private set` makes a property read-only on every instance. `IValidateProperty.MarkReadOnly()` makes it read-only on one instance, permanently — decided during `[Fetch]` from a server-side permission service, so a field is editable for one user and locked for another. The permission is never a parameter the client passes:

<!-- snippet: skill-mark-read-only -->
<a id='snippet-skill-mark-read-only'></a>
```cs
[Remote]
[Fetch]
internal void Fetch(int id, [Service] IFieldLevelAuthRepository repository, [Service] ISalaryPermission permission)
{
    var data = repository.GetById(id);
    Name = data.Name;
    Salary = data.Salary;
    Department = data.Department;

    // Field-level authorization: lock down Salary if user lacks permission
    if (!permission.CanEditSalary)
    {
        this["Salary"].MarkReadOnly();
    }
}
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/FieldLevelAuthorization.cs#L58-L74' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mark-read-only' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

After `MarkReadOnly()`, `SetValue` and the property setter throw `PropertyException`; `SetPrivateValue` and `LoadValue` still succeed, so rules and deserialization are unaffected. The flag travels to the client with the entity; the server's `[Update]` enforces the permission again before writing the field.

## Suppressing Property Events

`PauseAllActions()` pauses an object outside a factory operation. While paused, setters run no rules and raise no `PropertyChanged`; `ResumeAllActions` recalculates cached validity but does not run the skipped rules:

<!-- snippet: skill-pause-all-actions -->
<a id='snippet-skill-pause-all-actions'></a>
```cs
[TestMethod]
public void Gotcha4_PausedPropertyChanges_DoNotTriggerRules()
{
    // Arrange
    var factory = _scope.GetRequiredService<IGotcha4DemoFactory>();
    var entity = factory.Create();

    // Act - Modify properties while paused
    using (entity.PauseAllActions())
    {
        entity.Quantity = 10;
        entity.Price = 5.00m;
    }
    // ResumeAllActions() is called, but rules don't automatically run

    // Assert - Total is NOT calculated
    Assert.AreEqual(0m, entity.Total, "Total should be 0 - rules did not run while paused");
}
```
<sup><a href='/src/Design/Design.Tests/GotchaTests/CommonGotchaTests.cs#L177-L196' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-pause-all-actions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

PauseAllActions behavior:
- `PropertyChanged` and `NeatooPropertyChanged` are not raised while paused
- Validation rules do NOT execute
- On an entity, a property set while paused is not marked modified
- Parent cascade is suppressed

After Resume:
- Events and rules resume for new property changes
- No catch-up events fire and no rules run for changes made during the pause
- Edits made under `PauseAllActions()` on a fetched entity leave `IsModified` false and are not saved

Never use it inside a factory operation: the operation is already paused, and disposing the `using` resumes the object early. Deserialization pauses the object on its own.

## Task Tracking and IsBusy

Async rule tasks are tracked per property, not per entity. When one field triggers an async lookup, only that field is busy — the rest of the form remains editable. The entity's `IsBusy` aggregates its properties, and `WaitForTasks()` awaits them:

<!-- snippet: skill-is-busy -->
<a id='snippet-skill-is-busy'></a>
```cs
[TestMethod]
public async Task AsyncRule_SetsIsBusyUntilItCompletes()
{
    var entity = _scope.GetRequiredService<IBusyStateDemoFactory>().Create();

    entity.Name = "Test";  // triggers the async action rule

    Assert.IsTrue(entity.IsBusy, "The async rule is still running");

    await entity.WaitForTasks();

    Assert.IsFalse(entity.IsBusy);
    Assert.AreEqual("Processed: Test", entity.ComputedValue);
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/ValidationStateTests.cs#L52-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-is-busy' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Task tracking behavior:
- Property setters that trigger async rules return immediately but track the task
- `IsBusy` is true while tasks are pending
- `Task` on the property object holds the pending task (or `Task.CompletedTask` when not busy)
- Tasks propagate to the parent for aggregate-level coordination
- `WaitForTasks()` awaits all property tasks; await it before reading `IsValid` or saving

## Property Validation Integration

Rules run when a property is set, messages land on the property, the property's `IsValid` follows its messages, and the object's `IsValid` aggregates its properties and children — while `IsSelfValid` ignores children:

<!-- snippet: skill-is-valid-vs-self-valid -->
<a id='snippet-skill-is-valid-vs-self-valid'></a>
```cs
[TestMethod]
public async Task InvalidChild_MakesParentInvalid_ButNotSelfInvalid()
{
    var parent = _scope.GetRequiredService<IValidationStateDemoFactory>().Create();
    parent.RequiredField = "set";
    parent.Child!.RequiredField = "set";
    await parent.WaitForTasks();
    Assert.IsTrue(parent.IsValid);

    // Break the child only
    parent.Child.RequiredField = "";
    await parent.WaitForTasks();

    Assert.IsTrue(parent.IsSelfValid, "The parent's own rules pass");
    Assert.IsFalse(parent.IsValid, "IsValid aggregates the child");
    Assert.IsFalse(parent.Child.IsValid);
    Assert.IsTrue(parent.PropertyMessages.Count > 0, "The child's message reaches the parent");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/ValidationStateTests.cs#L31-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-is-valid-vs-self-valid' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Validation flow:
1. Property value changes
2. Validation rules execute (if not paused and `Reason == UserEdit`)
3. `PropertyMessages` updated with any errors
4. `IsValid` recalculates based on `PropertyMessages`
5. Parent's `IsValid` recalculates (cascade)

See [Validation](validation.md) for details on rule execution and [Business Rules](business-rules.md) for custom validation logic.

## Property Change Propagation

UI data-binding works at the property level — the UI binds to each property via the entity's indexer. But the *aggregate root* needs to know when children change so it can run aggregate-level rules — "all line item percentages must sum to 100%" — or notify a sibling. `NeatooPropertyChanged` bubbles child changes up to the root with a dotted path:

<!-- snippet: docs-change-propagation -->
<a id='snippet-docs-change-propagation'></a>
```cs
[TestMethod]
public async Task ChildPropertyChange_BubblesToTheRoot_WithADottedPath()
{
    var order = _orderFactory.Create();
    var item = _itemFactory.Create("Widget", 1, 5.00m);
    order.Items!.Add(item);

    var paths = new List<string>();
    order.NeatooPropertyChanged += args =>
    {
        paths.Add(args.FullPropertyName);
        return Task.CompletedTask;
    };

    item.UnitPrice = 7.00m;
    await order.WaitForTasks();

    // The root sees the child's change under the child collection's path
    Assert.IsTrue(paths.Contains("Items.UnitPrice"), string.Join(", ", paths));
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/OrderAggregateTests.cs#L194-L215' title='Snippet source file'>snippet source</a> | <a href='#snippet-docs-change-propagation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Cascade behavior:
- Child property changes fire `NeatooPropertyChanged`
- The parent re-raises the event wrapped with its own property name, building `FullPropertyName`
- The event bubbles to the aggregate root
- A root rule with a child property trigger (`t => t.Items![0].LineTotal`) matches the same dotted path — see [Business Rules](business-rules.md)

`FullPropertyName` concatenates property names with dots (e.g., `"Items.UnitPrice"`). Collection indexes are not included.

## Constructor Property Assignment

A constructor runs before any factory operation pauses the object, so a property set in a constructor is tracked as a modification. Default values belong in `[Create]`, where the object is paused and the assignment is a clean baseline. The analyzer (NEATOO010) warns about constructor assignments and offers a code fix to convert to `LoadValue`.

Use LoadValue in constructors when initial values must be set outside of factory Create methods.

---

**UPDATED:** 2026-10-06
