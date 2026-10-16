# Property Change Events: PropertyChanged vs NeatooPropertyChanged

Neatoo raises two distinct property-change events. Choosing the right one depends on **what you're subscribing to**, not on what you want to observe.

## The Two Events

| Event | Interface | Shape | Bubbles child names? |
|-------|-----------|-------|----------------------|
| `PropertyChanged` | `INotifyPropertyChanged` | Synchronous, `PropertyChangedEventArgs` | No — only meta flags bubble (see below) |
| `NeatooPropertyChanged` | `INotifyNeatooPropertyChanged` | Async `Task`, `NeatooPropertyChangedEventArgs` with dotted `FullPropertyName` | Yes — full path like `"Items.Total"` |

### What `PropertyChanged` raises on an entity

- The entity's own directly-set properties (e.g. `FirstName`)
- The entity's own meta flags when they flip — including flips caused by descendant changes:
  - `IsValid`, `IsSelfValid`, `IsBusy` (from `ValidateBase`)
  - `IsModified`, `IsSelfModified`, `IsSavable`, `IsDeleted` (from `EntityBase`; `EntityListBase` raises `IsModified`/`IsSelfModified` the same way)

It does **not** raise `PropertyChanged("Total")` when `Child.Total` changes. The child's property name never appears on the parent's `PropertyChanged`.

### What `NeatooPropertyChanged` raises on an entity

Every change anywhere in the graph, with a dotted `FullPropertyName` like `"Items.Total"` or `"Address.City"`. Each level wraps the child event rather than flattening, so you can walk `InnerEventArgs` if you need the exact source.

## The Rule

Pick the event by looking at **what object you subscribe to**:

| Subscribing to... | Use | Reason |
|---|---|---|
| A leaf `IEntityProperty` (from `entity["Name"]`) | `PropertyChanged` | Property wrapper raises its own meta events (`PropertyMessages`, `IsValid`, `IsBusy`, `IsReadOnly`). Nothing below it to bubble. |
| An entity, caring only about its own fields + meta flags | `PropertyChanged` | Meta flags already bubble through this event. |
| An entity, caring about anything in the graph | `NeatooPropertyChanged` | The only event that carries property names from descendants. |

## How MudNeatoo Applies the Rule

**Input components** (`MudNeatooTextField`, `MudNeatooSelect`, etc.) subscribe to `this.EntityProperty.PropertyChanged`. The subscription target is the **property wrapper**, not the entity. Each component is scoped to a single leaf, so `PropertyChanged` is sufficient — there is nothing below the property to bubble from.

**`NeatooValidationSummary`** subscribes to **both** on the root entity: `PropertyChanged` when the entity implements `INotifyPropertyChanged`, and `NeatooPropertyChanged` when it implements `INotifyNeatooPropertyChanged`.

Because the summary displays `entity.PropertyMessages` across the whole aggregate, it needs notification whenever any descendant becomes invalid. That's what `NeatooPropertyChanged` provides.

## Guidance for Custom Container Components

If you're building a Blazor component that takes a root entity as a parameter and renders state that depends on descendants:

1. **Subscribe when the entity arrives, unsubscribe in `Dispose`.** Neither event is auto-managed by Blazor. A component whose entity is a parameter subscribes in `OnParametersSet` and moves the subscription when the parameter changes (the TrainingPlan row in SKILL.md); one that loads its own entity can subscribe in `OnInitialized`.
2. **Use `NeatooPropertyChanged` for graph-wide awareness.** Marshal `StateHasChanged` via `InvokeAsync` — the delegate is async and may run off the UI thread.
3. **Use `PropertyChanged` alone if you only care about root-level flags** like `IsSavable`, `IsValid`, `IsBusy`.
4. **Move the subscription when the instance changes.** `Save()` returns a new instance; the old one raises nothing afterwards.

The Employee page does all four (it subscribes to `NeatooPropertyChanged` because its view branch reads entity properties as well as flags):

<!-- snippet: skill-mud-page-code -->
<a id='snippet-skill-mud-page-code'></a>
```razor
@code {
    [Parameter] public Guid? Id { get; set; }

    private IEmployee? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        if (Id is Guid id)
        {
            await Vm.Load(id);
        }
        else
        {
            Vm.New();
        }

        Resubscribe();
    }

    // The page owns the subscription: the input components subscribe to their
    // own property wrappers, but the Save button and the view-mode text read
    // the entity, so the page re-renders on the graph-wide event.
    private void Resubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnEntityChanged;
        }

        _subscribed = Vm.Employee;

        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged += OnEntityChanged;
        }
    }

    // Save and Cancel each leave the ViewModel holding a new instance.
    private async Task SaveAsync()
    {
        await Vm.Save();
        Resubscribe();
        StateHasChanged();
    }

    private async Task CancelAsync()
    {
        await Vm.CancelEdit();
        Resubscribe();
        StateHasChanged();
    }

    private Task OnEntityChanged(NeatooPropertyChangedEventArgs e)
    {
        return InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnEntityChanged;
        }
    }
}
```
<sup><a href='/src/Design/Design.App/Pages/EmployeeEdit.razor#L109-L175' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-mud-page-code' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Common Misconception: Blazor Does Not Auto-Subscribe

Blazor does **not** observe `INotifyPropertyChanged` automatically. A binding like `<MudText>@entity.Total</MudText>` only re-renders when the containing component calls `StateHasChanged`. In practice that call happens because the component displaying it is subscribed to the entity: a row component to its child, a summary to the root, or a small page to its entity (SKILL.md → The Component Is the Unit of Refresh). It is not automatic.

If you render a view-only page with no form subscription, you must subscribe to `PropertyChanged` (for root-level state) or `NeatooPropertyChanged` (for any descendant state) yourself.
