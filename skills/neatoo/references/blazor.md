# Blazor Integration

> **Required package:** Install `Neatoo.Blazor.MudNeatoo` to use the MudNeatoo components shown below.

Neatoo provides Blazor-specific components and patterns for building forms with validation display, change tracking, and two-way binding. Property binding basics are covered in [properties.md](properties.md) -- this page covers Blazor-specific patterns.

## The Thin Binding Layer Principle

The Blazor UI should be a **thin binding layer** over the domain model. It binds to domain properties, calls domain methods, and displays domain-computed state. It does not compute, decide, or validate.

**When implementing a feature: design domain properties and rules first. Write the UI as a binding layer over those properties. If you find yourself writing business logic in a `.razor` file, stop and move it to the domain model.**

**What belongs in .razor files:**
- Property binding (`EntityProperty="@entity[nameof(IOrder.Name)]"`, `@entity.Total`)
- Layout and styling (CSS classes, MudBlazor components, grid structure)
- Component selection (which MudBlazor component to use)
- Navigation and routing
- Calling domain methods on button click (`OnClick="@(() => entity.Approve())"`)

**What does NOT belong in .razor files:**
- Arithmetic (`@(a * b)`) -- use an `AddAction` computed property
- Conditional business logic (`@if (status == "X" && count > 0)`) -- use a domain `bool` property
- LINQ expressions (`@items.Where(...).Count()`) -- use a domain computed property
- Multi-property validation in event handlers -- use a rule
- Setting multiple properties in response to an event -- use a domain method or `AddAction`

### Anti-Pattern: Logic-Heavy Razor Files

A `.razor` file with 20+ conditional/LINQ expressions indicates business logic has leaked into the UI. This logic is untestable (requires E2E tests) and duplicates what the domain model should own.

```razor
<!-- WRONG: 21 conditional expressions in a panel -->
@if (visit.Status == "Active" && plan.IsApproved && !visit.IsComplete)
{ <TreatmentPanel /> }
@if (areas.Any(a => a.NeedsReview) && visit.Status != "Discharged")
{ <ReviewAlert /> }
@if (visit.CompletedVisits >= plan.MaxVisits && !plan.HasExtension)
{ <ExtensionWarning /> }
```

```razor
<!-- RIGHT: Domain exposes computed state, UI binds -->
@if (visit.ShowTreatmentPanel) { <TreatmentPanel /> }
@if (visit.ShowReviewAlert) { <ReviewAlert /> }
@if (visit.ShowExtensionWarning) { <ExtensionWarning /> }
```

The domain model computes `ShowTreatmentPanel`, `ShowReviewAlert`, `ShowExtensionWarning` via `AddAction` rules into `private set` partial properties. See `domain-logic-placement.md` for the full pattern.

## Two Binding Modes

Neatoo entities support two distinct binding modes in Blazor.

### Mode 1: Display Value Binding

Bind directly to entity properties to display values:

```razor
<MudText>@order.Total</MudText>
<MudText>@order.Status</MudText>
```

The entity implements `INotifyPropertyChanged`, but **Blazor does not subscribe on its own**. The page subscribes to the entity's `PropertyChanged` (the mudneatoo skill's Page Structure Pattern does this for `IsSavable`) and calls `StateHasChanged`; display bindings re-render on that cycle. When a rule updates `Total` in response to `Quantity`, the entity raises `PropertyChanged("Total")`, the page's handler re-renders, and the binding shows the new value. A view-only page with no form must subscribe itself.

### Mode 2: Property Metadata Binding

Each partial property is backed by its own property object (see [properties.md](properties.md) — Object-Per-Property Architecture), reached through the indexer `entity["PropertyName"]`. Each property object fires its own `PropertyChanged` for `IsValid`, `PropertyMessages`, `IsBusy`, and `IsReadOnly`:

```razor
@{ var emailProp = employee["Email"]; }
<MudTextField Value="@employee.Email" />
@if (!emailProp.IsValid)
{
    @foreach (var msg in emailProp.PropertyMessages)
    { <MudText Color="Color.Error">@msg.Message</MudText> }
}
@if (emailProp.IsBusy) { <MudProgressCircular Size="Size.Small" /> }
```

MudNeatoo components (`MudNeatooTextField`, etc.) handle Mode 2 internally — they bind to both the value and the property metadata.

**MudNeatoo components bind `EntityBase` properties only.** Every component's `EntityProperty` parameter is typed `IEntityProperty`. A `ValidateBase` object's properties are `IValidateProperty` and not `IEntityProperty`, so a `ValidateBase` (a value object, or form data without a persistence lifecycle) cannot be bound through MudNeatoo components. Bind it with the manual metadata pattern above (`Value`, `SetValue`, `IsValid`, `PropertyMessages`, `IsBusy`, `IsReadOnly` are all on `IValidateProperty`).

### When to Use Each Mode

| Goal | Mode | Example |
|------|------|---------|
| Show a property value | Mode 1 | `@order.Total` |
| Show validation errors | Mode 2 (automatic via MudNeatoo) | `<MudNeatooTextField T="string" EntityProperty="@employee[nameof(employee.Email)]" />` |
| Custom validation display | Mode 2 (manual) | `employee["Email"].IsValid`, `employee["Email"].PropertyMessages` |
| Show busy spinner per field | Mode 2 (manual) | `employee["Email"].IsBusy` |
| Conditional UI from domain state | Mode 1 | `@if (order.QualifiesForDiscount)` |

### Anti-Pattern: Manual PropertyChanged Handlers for Computed Values

Do NOT subscribe to `PropertyChanged` in Blazor code to recompute derived values (a handler that sets `totalPay = employee.Hours * employee.Rate` when `Hours` or `Rate` changes is domain logic in the UI). Use an `AddAction` rule in the domain model instead — it sets a `private set` partial property, which raises `PropertyChanged`, and the page re-renders. Blazor just binds `@task.Cost`:

<!-- snippet: skill-computed-properties -->
<a id='snippet-skill-computed-properties'></a>
```cs
// A derived value is a private-set partial property written by a rule.
// It notifies, it serializes, and the UI sees it read-only.
RuleManager.AddAction(
    t => t.Cost = t.Hours * t.Rate * (1 - t.Discount),
    t => t.Hours,
    t => t.Rate,
    t => t.Discount);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L37-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-computed-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Validation Display

A field's validation state is the property object's `IsValid` and `PropertyMessages`; the entity's `PropertyMessages` aggregates every property's messages for a summary. This is what MudNeatoo components and `NeatooValidationSummary` bind:

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

## Form Submission

A `[Create]` runs paused, so a freshly created entity reports `IsValid == true` until a rule runs — a Save button bound to `IsSavable` is enabled on an empty form. A `[Create]` that must not hand back an unvalidated object calls `RunRules()` at the end; otherwise the page calls it once after `Create`. After that, each committed field runs its own rules, and the Save handler calls `WaitForTasks()` and checks `IsSavable` before saving:

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

## Busy State

While an async rule runs, the property's and the entity's `IsBusy` are true and `IsSavable` is false. MudNeatoo components disable their input while `IsBusy`; a Save button bound to `IsSavable` disables itself:

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

## Manual Binding

For a control without a MudNeatoo wrapper, call `SetValue` from the control's `ValueChanged`. It runs the same rules as the property setter but returns a `Task` the component can await:

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

## Component Reference

Bind components to domain properties. Ensure the values you bind to are domain properties, not UI-computed expressions.

| Component | Purpose |
|-----------|---------|
| `MudNeatooTextField<T>` | Text input with validation |
| `MudNeatooNumericField<T>` | Numeric input |
| `MudNeatooDatePicker` | Date selection |
| `MudNeatooDateRangePicker` | Date range selection |
| `MudNeatooTimePicker` | Time selection |
| `MudNeatooCheckBox<T>` | Boolean toggle |
| `MudNeatooSwitch<T>` | Boolean toggle (switch style) |
| `MudNeatooSelect<T>` | Dropdown selection |
| `MudNeatooAutocomplete<T>` | Autocomplete text input |
| `MudNeatooRadioGroup<T>` | Radio button group |
| `MudNeatooSlider<T>` | Slider input |
| `NeatooValidationSummary` | All validation errors for an entity |

All components take an `EntityProperty` parameter: the `IEntityProperty` from an `EntityBase` entity's indexer. A curated set of the wrapped MudBlazor component's parameters (Variant, Margin, HelperText, Adornment, Class, and others) is forwarded; a parameter that is not forwarded means manual metadata binding for that field.

## Standard Practices

1. **Use Neatoo components** -- They handle validation display and change tracking
2. **Handle IsBusy** -- Disable buttons and show loading during async operations
3. **Show validation on commit** -- `MudNeatooTextField` and `MudNeatooNumericField` set the property when the field loses focus (`Immediate="false"`), so rules run and errors display once per committed value, not per keystroke. A control that sets the property continuously runs the rules on every change.

## Blazor WASM Project Structure

Keep EF Core out of the client by splitting data access into two projects: a **Dal** project with the repository interfaces and row types (no EF Core packages), and an **Ef** project that implements them with EF Core. The domain project references Dal only. The server references the domain, Ef, and the client; the client references the domain and MudNeatoo. Nothing in the client's reference graph reaches Ef.

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

The server registers the Ef implementations against the Dal interfaces; the client registers none, so a `[Service]` repository parameter resolves only on the server. The domain's `[Remote]` operations and `[Remote, Execute]` commands are the only paths from the client to that code.

## Related

- [Validation](validation.md) - Validation rules
- [Properties](properties.md) - Property change notifications
- [Entities](entities.md) - IsSavable for submit buttons
- [Domain Logic Placement](domain-logic-placement.md) - Where business logic belongs
