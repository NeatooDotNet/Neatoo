# Blazor Integration

[Previous: Async Operations](async.md) | [Up](index.md) | [Next: Business Rules](business-rules.md)

Neatoo provides MudBlazor integration through the `Neatoo.Blazor.MudNeatoo` package. A MudNeatoo component binds to one `IEntityProperty` — the property object behind a partial property — and from that single binding point gets the value, the label, the validation messages, the busy state and the read-only state. The UI is a thin binding layer over the domain model: no POCOs, no manual handlers, no duplicate validation.

## Installation

Install the MudNeatoo package alongside MudBlazor:

```bash
dotnet add package Neatoo.Blazor.MudNeatoo
dotnet add package MudBlazor
```

In `Program.cs`, register MudBlazor with `builder.Services.AddMudServices();` next to `AddNeatooServices(...)`. MudNeatoo requires MudBlazor 9.0 or later and targets .NET 9.0 and 10.0.

Add the namespaces to `_Imports.razor`:

```razor
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using Neatoo.Blazor.MudNeatoo.Extensions
```

## Component Overview

MudNeatoo provides typed wrappers for the common MudBlazor input components:

| Component | Wraps | Type parameter |
|-----------|-------|----------------|
| `MudNeatooTextField<T>` | `MudTextField<T>` | `string`, `int`, ... |
| `MudNeatooNumericField<T>` | `MudNumericField<T>` | `int`, `decimal`, `double` |
| `MudNeatooSelect<T>` | `MudSelect<T>` | enum or value type |
| `MudNeatooCheckBox<T>` | `MudCheckBox<T>` | `bool`, `bool?` |
| `MudNeatooSwitch<T>` | `MudSwitch<T>` | `bool` |
| `MudNeatooDatePicker` | `MudDatePicker` | (none — `DateTime?`) |
| `MudNeatooTimePicker` | `MudTimePicker` | (none — `TimeSpan?`) |
| `MudNeatooDateRangePicker` | `MudDateRangePicker` | (none — `DateRange`) |
| `MudNeatooAutocomplete<T>` | `MudAutocomplete<T>` | any |
| `MudNeatooSlider<T>` | `MudSlider<T>` | numeric |
| `MudNeatooRadioGroup<T>` | `MudRadioGroup<T>` | enum or value type |
| `NeatooValidationSummary` | `MudAlert` | (entity-level messages) |

Every input component takes an `EntityProperty` parameter typed `IEntityProperty`, which is the property type of an `EntityBase` entity. A `ValidateBase` object's properties are `IValidateProperty` only, so a value object or form model that derives from `ValidateBase` cannot be bound through these components; use the manual binding shown at the end of this page, which needs nothing beyond `IValidateProperty`.

## Basic Property Binding

Set `EntityProperty` to the property object from the entity's indexer. The component reads `DisplayName` for its label (from `[DisplayName]`, or the property name), and on change calls `IEntityProperty.SetValue(value)` — the same path the partial property setter takes, so every rule registered on the property runs with `ChangeReason.UserEdit`.

```razor
<MudNeatooTextField T="string"
                    EntityProperty="@employee[nameof(IEmployee.FirstName)]"
                    Variant="Variant.Outlined" />
```

Text and numeric fields are `Immediate="false"`: they commit when the field loses focus, so a rule behind them — including an async rule that calls the server through a `[Remote, Execute]` command — runs once per committed value, not once per keystroke.

What the component binds to is the property object's metadata. The same members are what you read when you bind by hand:

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

## Validation Display

A MudNeatoo component shows the property's `PropertyMessages` under the input. Each rule stores its messages on the trigger property under the rule's stable id, so a message disappears when the rule that produced it passes. Nothing is wired in the page; the component subscribes to the property object's `PropertyChanged` and re-renders when `PropertyMessages` or `IsValid` change.

```razor
<MudNeatooTextField T="string"
                    EntityProperty="@employee[nameof(IEmployee.Email)]" />
```

When a property has async rules, `IsValid` is final only after they finish. Before reading validity in code — a test, a handler — `await entity.WaitForTasks()`; the component does not need to, because it re-renders when the rule completes.

## Validation Summary

`NeatooValidationSummary` renders every message in the aggregate in one `MudAlert`. It subscribes to the root's `NeatooPropertyChanged`, which carries changes from every descendant, so a child's broken rule reaches the summary too.

```razor
<NeatooValidationSummary Entity="@order"
                         ShowHeader="false"
                         Dense="true"
                         IncludePropertyNames="false" />
```

Parameters: `Entity` (required, `IValidateMetaProperties`), `ShowHeader`, `HeaderText`, `Dense`, `IncludePropertyNames`, `Variant`, `Elevation`, `Class`. The entity's `PropertyMessages` is the collection it displays: `IsValid` aggregates the object and every descendant, `IsSelfValid` is the object alone, and a child's messages are visible on the parent:

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

## Form Integration

Use `MudForm` with MudNeatoo components, not `EditForm` with `DataAnnotationsValidator`: the annotations validator knows nothing about `AddValidation`, class-based rules or `PropertyMessages`. Bind the Save button to `IsSavable` — `(IsModified || IsNew) && IsValid && !IsBusy` — so it is enabled for a new entity and disabled while async rules run or while the entity is invalid.

```razor
<MudForm>
    <NeatooValidationSummary Entity="@employee" ShowHeader="false" Dense="true" />

    <MudNeatooTextField T="string" EntityProperty="@employee[nameof(IEmployee.FirstName)]" />
    <MudNeatooTextField T="string" EntityProperty="@employee[nameof(IEmployee.LastName)]" />
    <MudNeatooTextField T="string" EntityProperty="@employee[nameof(IEmployee.Email)]" />

    <MudButton OnClick="Save" Disabled="@(!employee.IsSavable)" Variant="Variant.Filled" Color="Color.Primary">
        Save
    </MudButton>
</MudForm>
```

The Save handler awaits `WaitForTasks()`, re-checks `IsSavable` (a guard only — the button is already disabled), and keeps the instance `Save()` returns:

```razor
@code {
    private async Task Save()
    {
        await employee!.WaitForTasks();
        if (!employee.IsSavable) return;
        employee = (IEmployee)await employee.Save();
    }
}
```

A `[Create]` runs paused, so a freshly created entity reports `IsValid == true` until a rule runs: an empty required field has not been validated yet. A `[Create]` that must not hand back an unvalidated object calls `RunRules()` at its end; otherwise the page may call it once after `Create`. After that, every committed field runs its own rules and `RunRules` is only for forcing a re-run.

<!-- snippet: skill-run-rules-after-create -->
<a id='snippet-skill-run-rules-after-create'></a>
```cs
[TestMethod]
public async Task Address_InvalidAddressType_IsInvalid()
{
    // Address.Create(street, city, state, zip, type) - the overload
    // AddressList documents as the RIGHT way to copy across aggregates,
    // which nothing called.
    var address = _addressFactory.Create("1 Main St", "Springfield", "IL", "62701", "Vacation");
    await address.WaitForTasks();

    // Factory operations run paused, so no rule has evaluated this data yet -
    // the object reports valid until something asks. This is why a factory
    // method that must not produce invalid objects calls RunRules() itself.
    Assert.IsTrue(address.IsValid, "Rules have not run yet - the factory op was paused");

    await address.RunRules();
    Assert.IsFalse(address.IsValid, "Address type must be Home, Work, or Other");

    // A live edit runs rules automatically
    address.AddressType = "Work";
    await address.WaitForTasks();
    Assert.IsTrue(address.IsValid);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/AggregateCoverageGapTests.cs#L206-L229' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-run-rules-after-create' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Busy State Handling

While an async rule runs, the property's `IsBusy` is true, the entity's `IsBusy` is true, and `IsSavable` is false. The component disables its input while `IsBusy`; it also accepts a `Disabled` parameter, OR'd with `IsBusy`, for a UI-driven condition. You cannot force a field enabled while a rule is running.

```razor
<MudNeatooTextField T="string"
                    EntityProperty="@order[nameof(IOrder.CustomerName)]"
                    Disabled="@(order.Status == "Shipped")" />
```

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

## Read-Only Properties

`ReadOnly` is not a parameter you pass: every MudNeatoo input hard-binds it to `EntityProperty.IsReadOnly`, so read-only state is owned by the domain model. There are two sources. A `private set` partial property is read-only on every instance — the recipe for a derived value written by an `AddAction` rule:

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

`IValidateProperty.MarkReadOnly()` locks one property on one instance, permanently. The entity calls it during `[Fetch]` from a server-side permission service, so the same field is editable for one user and read-only for another, and the component renders accordingly without configuration:

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

Read-only components stay visually enabled but reject edits. View/edit mode switching is therefore a Razor conditional between MudNeatoo inputs and plain `MudText`, not a `ReadOnly` toggle.

## Select, Checkbox, Date, Numeric and Autocomplete

Every input binds the same way; only the wrapped component changes. Each one commits through `SetValue`, so rules run and `PropertyMessages` display without any handler code.

```razor
@* Select: options are MudSelectItem children *@
<MudNeatooSelect T="string" EntityProperty="@order[nameof(IOrder.Status)]">
    <MudSelectItem Value="@("Draft")">Draft</MudSelectItem>
    <MudSelectItem Value="@("Submitted")">Submitted</MudSelectItem>
</MudNeatooSelect>

@* Boolean *@
<MudNeatooCheckBox T="bool" EntityProperty="@employee[nameof(IEmployee.IsActive)]" />

@* Date: DateTime? properties. TimePicker binds TimeSpan?, DateRangePicker binds DateRange *@
<MudNeatooDatePicker EntityProperty="@employee[nameof(IEmployee.HireDate)]"
                     MaxDate="@DateTime.Today" />

@* Numeric *@
<MudNeatooNumericField T="int" EntityProperty="@item[nameof(IOrderItem.Quantity)]" />
<MudNeatooNumericField T="decimal" EntityProperty="@item[nameof(IOrderItem.UnitPrice)]"
                       Adornment="Adornment.Start" AdornmentText="$" />

@* Autocomplete: SearchFunc supplies the candidates *@
<MudNeatooAutocomplete T="string" EntityProperty="@address[nameof(IAddress.State)]"
                       SearchFunc="@SearchStates" />
```

Range and format constraints are validation attributes on the property, not component parameters: the component displays what the rule reports.

<!-- snippet: skill-validation-attributes -->
<a id='snippet-skill-validation-attributes'></a>
```cs
[Required(ErrorMessage = "Product name is required")]
[StringLength(100)]
public partial string? ProductName { get; set; }

[Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10000")]
public partial int Quantity { get; set; }

[Range(0.01, 1000000, ErrorMessage = "Unit price must be positive")]
public partial decimal UnitPrice { get; set; }
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs#L32-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-validation-attributes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Change Tracking in Forms

The components do not bind `IsModified`; the page does, for two different purposes. The Save button binds `IsSavable`. An unsaved-changes prompt binds `IsModified`, which answers "would discarding this lose work?" — and is deliberately false for a freshly created entity, so the prompt stays quiet until the user edits something while the Save button is already enabled through `IsNew`:

<!-- snippet: skill-create-is-new-not-modified -->
<a id='snippet-skill-create-is-new-not-modified'></a>
```cs
[TestMethod]
public void Create_SetsIsModifiedFalse_ButStillSavable()
{
    // Arrange & Act
    var entity = _factory.Create();

    // Assert - IsNew and IsModified answer different questions. A created
    // entity needs inserting (IsNew), but holds no user work (not modified),
    // so unsaved-changes guards stay quiet on it. Savability comes from the
    // IsNew term. A [Create] that IS the user's work opts in with
    // MarkModified() in its body.
    Assert.IsTrue(entity.IsNew, "New entity should have IsNew=true");
    Assert.IsFalse(entity.IsModified, "New entity holds no user work");
    Assert.IsTrue(entity.IsSavable, "...but it is savable, so the Insert can happen");
}
```
<sup><a href='/src/Design/Design.Tests/BaseClassTests/EntityBaseTests.cs#L43-L59' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-create-is-new-not-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A fetched entity is a clean baseline; the first edit marks the property and the entity modified, and the state cascades from children to the root:

<!-- snippet: skill-fetch-then-modify -->
<a id='snippet-skill-fetch-then-modify'></a>
```cs
[TestMethod]
public async Task Fetch_ThenModify_IsModified()
{
    // Arrange
    var entity = await _factory.Fetch(1);

    // Act
    entity.Name = "Changed";

    // Assert
    Assert.IsTrue(entity.IsModified, "Entity should be modified after change");
    Assert.IsTrue(entity["Name"].IsModified, "Name property should be modified");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/StatePropertyTests.cs#L74-L88' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-fetch-then-modify' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-is-savable -->
<a id='snippet-skill-is-savable'></a>
```cs
[TestMethod]
public async Task FetchedEntity_NotSavableWhenUnmodified()
{
    // Arrange
    var entity = await _factory.Fetch(1);

    // Assert
    Assert.IsFalse(entity.IsNew);
    Assert.IsFalse(entity.IsModified);
    Assert.IsFalse(entity.IsSavable, "Unmodified fetched entity should not be savable");
}

[TestMethod]
public async Task FetchedEntity_IsSavableWhenModified()
{
    // Arrange
    var entity = await _factory.Fetch(1);

    // Act
    entity.Name = "Updated Name";

    // Assert
    Assert.IsFalse(entity.IsNew);
    Assert.IsTrue(entity.IsModified);
    Assert.IsTrue(entity.IsValid);
    Assert.IsTrue(entity.IsSavable, "Modified valid entity should be savable");
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L69-L97' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-is-savable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Customizing Component Appearance

Each component forwards a curated set of its wrapped MudBlazor component's parameters — `Variant`, `Margin`, `HelperText`, `Adornment`, `AdornmentText`, `Class`, `Lines`, and others — not every parameter (`MudNeatooTextField` has no `Mask` or `Label`, for example: the label is `DisplayName`). `ReadOnly` and `Disabled` are never forwarded as-is (see above). A parameter that is not forwarded means manual binding for that field.

```razor
<MudNeatooTextField T="string"
                    EntityProperty="@employee[nameof(IEmployee.LastName)]"
                    Variant="Variant.Outlined"
                    Margin="Margin.Dense"
                    HelperText="As it appears on the payroll record" />
```

`MudNeatooTextField` also forwards `UserAttributes` (`Dictionary<string, object>`), which MudBlazor spreads onto the native `<input>`/`<textarea>` — the escape hatch for `spellcheck`, inline `style` and other attributes with no typed parameter.

## Property Extensions

`Neatoo.Blazor.MudNeatoo.Extensions.EntityPropertyExtensions` adapts an `IEntityProperty` to a standard MudBlazor input's validation surface for the manual-binding case: `GetErrorText()` joins the property's messages into one string, `HasErrors()` is true when any message exists, and `GetValidationFunc<T>()` returns a `Func<T, IEnumerable<string>>` suitable for a `MudTextField.Validation` parameter. They read `PropertyMessages`; they do not run rules.

## Two-Way Binding

MudNeatoo components use Blazor's two-way binding, but against the property object, so a change flows through the whole Neatoo pipeline:

1. User commits a value (blur for text and numeric fields; change for the others)
2. Component calls `IEntityProperty.SetValue(value)` with `ChangeReason.UserEdit`
3. The property and the entity are marked modified; `PropertyChanged` fires on both
4. RuleManager runs the rules whose trigger is this property (sync first, then async; the property is `IsBusy` meanwhile)
5. Each rule stores its messages on the trigger property under its id
6. `IsValid`, `IsModified`, `IsBusy`, `IsSavable` recalculate and cascade to the aggregate root
7. The property object raises `PropertyChanged` for its metadata; the component re-renders

## StateHasChanged Integration

Blazor does not observe `INotifyPropertyChanged` on its own. A MudNeatoo component subscribes to its property object's `PropertyChanged` in `OnInitialized` and calls `InvokeAsync(StateHasChanged)` when `PropertyMessages`, `IsValid`, `IsBusy` or `IsReadOnly` change (most also on `Value`; `MudNeatooTextField` leaves the value display to MudBlazor's own binding). Components unsubscribe in `Dispose`.

Anything else on the page that shows entity state — a `MudText` bound to `@order.TotalAmount`, the Save button's `Disabled` — re-renders only because the page subscribes to the entity's `PropertyChanged` (for `IsSavable`, `IsValid`, `IsBusy`, `IsModified` and its own properties) and calls `StateHasChanged`; a view-only page with no form has to subscribe itself. A derived value therefore lives in the domain as a rule-written `private set` property, which raises the event the page needs; computing it in a `PropertyChanged` handler in the page is business logic in the UI.

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

For state that depends on descendants by name, subscribe to `NeatooPropertyChanged`, whose `FullPropertyName` is the dotted path from the root (`"Items.LineTotal"`); this is what `NeatooValidationSummary` uses.

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

## Using Standard MudBlazor Components

For a control without a MudNeatoo wrapper, or a parameter that is not forwarded, bind the MudBlazor component to the property object by hand. Read `Value`, call `SetValue` from `ValueChanged`, and bind `DisplayName`, `IsBusy`, `IsReadOnly` and the messages yourself — this is what the components do internally. This pattern needs only `IValidateProperty`, so it is also how a `ValidateBase` object is bound.

```razor
@{ var phoneProp = entity[nameof(IPatient.PrimaryPhone)]; }
<MudTextField T="string"
              Value="@((string?)phoneProp.Value)"
              ValueChanged="@(async (string v) => await phoneProp.SetValue(v))"
              Label="@phoneProp.DisplayName"
              Disabled="@phoneProp.IsBusy"
              ReadOnly="@phoneProp.IsReadOnly"
              Immediate="false"
              Mask="@(new PatternMask("(000) 000-0000"))" />
@if (!phoneProp.IsValid)
{
    @foreach (var msg in phoneProp.PropertyMessages)
    { <MudText Color="Color.Error">@msg.Message</MudText> }
}
```

`SetValue` runs the same rules as the property setter but returns a `Task`, so the handler can await them. Set `Immediate="false"` on a manually bound text field: a field that sets the property on every keystroke runs the rules on every keystroke.

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

## Performance Considerations

- Rules run when a value commits. The MudNeatoo text and numeric fields already commit on blur; for a manually bound `MudTextField`, use `Immediate="false"` rather than debouncing.
- Before reading `IsValid` or saving in code, `await entity.WaitForTasks()`. `RunRules()` is a forced re-run, not the step that makes validity current.
- `PauseAllActions()` on a live entity stops rules and `PropertyChanged` for the duration of the `using` block. Nothing is queued: disposing it does not replay events or run the skipped rules, and on an entity a property set while paused is **not marked modified**, so a paused user edit on a fetched entity is silently not saved. Never use it inside a factory operation (already paused; disposing resumes early).

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

- Consider virtualization (`MudVirtualize`) for long lists of input components.

## Blazor WASM Project Structure

Keep EF Core out of the client by splitting data access into two projects: a **Dal** project with the repository interfaces and row types (no EF Core packages), and an **Ef** project that implements them with EF Core. The domain project references Dal only; the server references the domain, Ef and the client; the client references the domain and MudNeatoo. Nothing in the client's reference graph reaches Ef, so a `[Service]` repository parameter resolves only on the server and the domain's `[Remote]` operations and `[Remote, Execute]` commands are the only paths from the client to that code. This is the layout the Person example in the Neatoo repository uses; it is a description of one working arrangement, not a framework requirement.

```xml
<!-- Domain.csproj — repository interfaces only -->
<ItemGroup>
  <ProjectReference Include="..\Dal\Dal.csproj" />
</ItemGroup>

<!-- Ef.csproj — EF Core and the implementations -->
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
  <ProjectReference Include="..\Dal\Dal.csproj" />
</ItemGroup>

<!-- Server.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Client\Client.csproj" />
  <ProjectReference Include="..\Domain\Domain.csproj" />
  <ProjectReference Include="..\Ef\Ef.csproj" />
</ItemGroup>

<!-- Client.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Domain\Domain.csproj" />
</ItemGroup>
```

---

**UPDATED:** 2026-10-06
