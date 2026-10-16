---
name: Neatoo
description: Use when building or reviewing a Neatoo domain model, Blazor page or ViewModel, or when asking the questions a mainstream .NET habit asks of it - where do I validate before Save, where do I throw on bad input, is this a value object so ValidateBase, nobody edits this so plain EF, where do the screen's derived values and visibility go, how do I get computed values after Fetch, can I call Save on a child, can a rule call the server, who saves the children. Also triggers on EntityBase, ValidateBase, EntityListBase, ValidateListBase, IEntityRoot, IEntityBase, partial properties, RuleManager, AddAction, AddValidation, IsValid, IsSavable, IsModified, IsNew, IsBusy, WaitForTasks, RunRules, PropertyMessages, MudNeatoo. Neatoo is the editing session over a domain graph; factory attributes ([Factory], [Create], [Fetch], [Remote], [Service], [Execute]) and tier composition are the RemoteFactory skill's, which is loaded beside this one.
version: 2.0.0
---

# Neatoo

Neatoo is the editing session. Between a load and a save a person - or a job - changes a graph of objects, and Neatoo makes the graph answer, continuously and per property: what changed, is it valid, is work in flight, can it be saved, and what else must change because of this edit. The answers aggregate to the root and bind to the UI. The same compiled classes run in the browser and on the server, so all of it runs where the object is, with no request.

Every C# block in this skill and its references is compiled and tested in the Neatoo repository's `src/Design` projects. Where a block is not shown inline, the row links to the reference that renders it.

## The model

1. **What an operation sets is baseline. What happens afterwards is the user's work.** Inside a factory operation (`[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`) the object is paused: an assignment marks nothing modified and runs no rule. Everything the object must hold when it comes back is written inside the operation that produces it, and each child hydrates inside its own operation. Persisted data is assumed valid.

2. **Neatoo is the editing session.** A type is a Neatoo entity when it carries rules or change tracking that something relies on to decide validity, modification or savability - a person over many edits, or a job over many rows. It is plain RemoteFactory when nothing is tracked or validated: a read model, a command, a context that holds an aggregate, a `record`.

3. **Logic is placed by who is driving it.** A property change drives a rule on the entity. A person's gesture on one aggregate drives a verb on that entity. A load, another aggregate, or anything that needs server services drives a seam - an `[Execute]` or a `[Fetch]`. A screen's need for server truth drives a read model.

4. **The UI binds and adapts gestures. It decides nothing.** A ViewModel turns a gesture into a call on the domain and reads the domain's answers. It never originates a mutation, re-derives a gate, chooses what to load, or holds a transaction.

The seam itself - one domain assembly on both tiers, the factory operation as the only way in or out, what `[Create]`/`[Fetch]`/`[Execute]` each claim, where `[Remote]` goes, every object writing its own row - is RemoteFactory's model. The RemoteFactory skill states it; this skill assumes it.

## The questions the habit asks

A mainstream .NET developer reaches for these under load. Each row is the question, Neatoo's answer, and the compiled example to copy instead.

| The habit asks | Neatoo's answer | Copy this |
|---|---|---|
| Where do I validate before Save? | Nowhere. The rules ran when the property was set. `await WaitForTasks()` for async rules, read `IsSavable`, save, hold the returned instance. There is no validate step - and `RunRules(All)` before Save is not one. | The save handler below; [blazor.md](references/blazor.md#the-save-handler) |
| Where do I throw when input is bad? | In a rule, as a message. An exception is an application failure. A verb sets state and never checks-and-throws; the admission rule (`CanApprove`) says whether the verb applies and the UI binds the button to it. | [domain-logic-placement.md](references/domain-logic-placement.md) - `Approve` and `CanApprove`; [validation.md](references/validation.md) - attributes and rules |
| Nobody edits this, so plain EF? | An entity is right with no person involved. A job reuses the aggregate, its verb and its rules: verb, `WaitForTasks()`, then `IsSavable` decides - saved, reported with its messages, or skipped. | [base-classes.md](references/base-classes.md#a-job-over-entities) |
| Where do the screen's derived values and visibility go? | On the entity, as business facts (`IsHeld`, `CanApprove`, `IsOverBudget`), computed by rules. The page binds; the ViewModel may give a fact a screen name, one-to-one, deciding nothing. Never a property named for a screen element. | The page below; [domain-logic-placement.md](references/domain-logic-placement.md) - derived facts |
| How do I set a value in Fetch without marking it modified? | Assign it. Inside an operation the object is paused; plain assignment is the baseline. `LoadValue` is not for operations. | [entities.md](references/entities.md) - Fetch starts clean |
| How do I get computed values after Fetch - `RunRules(All)` at the end? | Rules do not fire during an operation, so the operation computes what it needs: load the value from the row, or run the rules that compute it - `RunRules(nameof(Status))`, named by trigger. Not `All` from a root whose rules write children (they finished their own Fetch and would be marked modified). A per-operation choice, never a routine step, and persisted data is assumed valid. Running rules to *validate* what an operation hands back is the developer's choice, made in the operation (`Order.Create` does, so an empty form shows its required fields) or by the caller; the framework never makes it. | The Fetch below; [rules-lifecycle.md](references/rules-lifecycle.md) |
| The parent saves its children's rows? | The list coordinates: it hands each child its row and the child's own operation writes it; removed children sit in `DeletedList` until the save. The RemoteFactory skill owns "every object writes itself". | [entities.md](references/entities.md) - list update; [collections.md](references/collections.md) |
| A rule can't call the server? | It can. Inject the delegate of a `[Remote, Execute]` command, or an interface-factory domain service; on the client both are a wrapped server call. Never a server-only service. No preference between the two. | [domain-logic-placement.md](references/domain-logic-placement.md) - rule with a command; [validation.md](references/validation.md#a-rule-that-takes-a-domain-service) |
| Value object, so `ValidateBase`? | A `record`: immutable, by-value, no rules; its constructor may throw because code assigns it. `ValidateBase` is an input model - something a person edits. If a value needs rules it is not a value object. | [base-classes.md](references/base-classes.md#a-value-object-is-a-record) |
| Can I call `Save()` on a child? | Not through a child interface: `IEntityBase` has no `Save()` or `IsSavable`; only `IEntityRoot` does. A type that plays both roles is saved as a root only when it was fetched as one. | [collections.md](references/collections.md) - the aggregate interfaces; [entities.md](references/entities.md#one-class-in-both-roles) |

**RemoteFactory's questions.** The RemoteFactory skill, loaded beside this one, answers these; they are listed here so the two skills read as one list. How do I load-or-create? Can I `[Create]` from loaded data? How does the client call the repository? Where does my application service go? Who writes a child's row? `Save()` updates my object? Which services can a constructor take? Should the child's `[Insert]` be `[Remote]`? Does `[Remote]` mean "runs on the server"? My command needs the server, so plain `[Execute]`? I'll return a DTO from a controller for this screen?

## The decisions

### Neatoo, or plain RemoteFactory

Apply the model's second statement. An `Order` a person edits, a `WorkOrder` a job approves in bulk: Neatoo entities, and so are their children. An `EmployeeDirectory` a screen reads, an `ApproveEmployee` command, a `PayGrade` record: plain RemoteFactory. The test is what the object must answer, not who is at the keyboard. The job over entities in [base-classes.md](references/base-classes.md#a-job-over-entities) is the case the habit gets wrong.

### What an operation sets is baseline

After `[Create]`: `IsNew = true`, `IsModified = false`, savable once valid. After `[Fetch]`: nothing modified, every derived value present. The operation produces that state; nothing is run afterwards to finish it. The fetch below shows both ways to produce a derived value - load it, or run the rules that write the object:

<!-- snippet: skill-fetch-computes-derived -->
<a id='snippet-skill-fetch-computes-derived'></a>
```cs
[Remote]
[Fetch]
internal async Task<bool> Fetch(Guid id,
    [Service] IWorkOrderRepository repository,
    [Service] IWorkOrderTaskListFactory tasksFactory)
{
    var row = repository.Get(id);
    if (row == null)
    {
        return false;
    }

    Id = row.Id;
    Status = row.Status;
    ApprovedBy = row.ApprovedBy;
    IsOnHold = row.IsOnHold;
    Budget = row.Budget;
    Discount = row.Discount;
    TotalCost = row.TotalCost;
    HasUnschedulableTasks = row.HasUnschedulableTasks;

    Tasks = tasksFactory.Fetch(row.Tasks);

    // Rules do not fire during an operation, so this one computes what it
    // needs. Two shapes, both used here:
    //  - Aggregates over the children (TotalCost, HasUnschedulableTasks) are
    //    persisted and loaded, as the tasks load Cost and IsSchedulable and
    //    OrderItem loads LineTotal.
    //  - The root's own derived values (CanApprove, IsHeld, IsOverBudget,
    //    Summary) are recomputed by running the rules that write THIS
    //    object, named by their trigger. Not RunRulesFlag.All: the tasks
    //    finished their own [Fetch] and are no longer paused, so a root rule
    //    that changed one of their values would mark it modified and the graph
    //    would report IsModified == true.
    // Neither is a validate step; persisted data is assumed valid.
    await RunRules(nameof(Status));
    await RunRules(nameof(Budget));
    return true;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L153-L193' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-fetch-computes-derived' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`LoadValue` is for the rare write that must be baseline after the operation has ended; `MarkModified()` in a `[Create]` is for the rare created object that *is* the user's work. Both in [entities.md](references/entities.md).

### Logic is placed by who drives it

| Who drives | Where it goes | Example |
|---|---|---|
| A property change | A rule on the entity (`AddAction` for a derived value, `AddValidation` for a message) | the admission rule below |
| A person's gesture on one aggregate | A verb on the entity: sets state, never persists, never checks-and-throws | `Approve`, `Submit` in [domain-logic-placement.md](references/domain-logic-placement.md) |
| A load, another aggregate, server services | A seam: an `[Execute]` or `[Fetch]` operation | the job in [base-classes.md](references/base-classes.md#a-job-over-entities) |
| A screen's need for server truth | A read model: `[Factory]` with `[Fetch]` only, no base class | `EmployeeDirectory` in [base-classes.md](references/base-classes.md) |

The admission rule is the shape most often put in the wrong place. It is domain state, bound by the button, not re-derived by the page:

<!-- snippet: skill-can-x-rule -->
<a id='snippet-skill-can-x-rule'></a>
```cs
// Admission for the Approve verb. The UI binds the button to it; the
// verb itself does not check and throw.
RuleManager.AddActionAsync(
    async t => await t.SetCanApprove(t.Status == "Pending"),
    t => t.Status);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L48-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-can-x-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A root reads its children and writes derived facts; a child reads the root through `Parent`. The chained rules, child-property triggers and cross-sibling validation that make an aggregate behave as one are in [domain-logic-placement.md](references/domain-logic-placement.md).

### The UI binds and adapts gestures

The ViewModel holds the live aggregate, exposes its verbs, and saves. The Save button binds to `IsSavable`. The handler:

<!-- snippet: skill-save-handler -->
<a id='snippet-skill-save-handler'></a>
```cs
/// <summary>
/// ViewModel for the Order edit screen: holds the aggregate, saves it.
/// </summary>
internal sealed class OrderEditViewModel
{
    private readonly IOrderFactory _orderFactory;
    private readonly IOrderItemFactory _itemFactory;

    public OrderEditViewModel(IOrderFactory orderFactory, IOrderItemFactory itemFactory)
    {
        _orderFactory = orderFactory;
        _itemFactory = itemFactory;
    }

    /// <summary>The aggregate the page binds to. Null until loaded or created.</summary>
    public IOrder? Order { get; private set; }

    public async Task New()
    {
        Order = await _orderFactory.Create();
    }

    public async Task Load(Guid id)
    {
        Order = await _orderFactory.Fetch(id);
    }

    public void AddItem()
    {
        Order?.Items?.Add(_itemFactory.Create());
    }

    public void RemoveItem(IOrderItem item)
    {
        Order?.Items?.Remove(item);
    }

    /// <summary>
    /// The Save button binds to this: enabled only when the entity says it
    /// can be saved. No separate validity check lives in the UI.
    /// </summary>
    public bool CanSave => Order?.IsSavable ?? false;

    /// <summary>
    /// The save handler. Waits for any async rule still running, then lets the
    /// entity decide; the messages for a refused save are already on the entity.
    /// </summary>
    public async Task Save()
    {
        if (Order is null)
        {
            return;
        }

        await Order.WaitForTasks();

        if (!Order.IsSavable)
        {
            return;  // invalid, or nothing changed; the summary shows why
        }

        Order = (IOrder)await Order.Save();  // reassign: Save returns a new instance
    }
}
```
<sup><a href='/src/Design/Design.App/ViewModels/OrderEditViewModel.cs#L31-L96' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-save-handler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Pain:** `Save()` does not mark the entity busy during the remote call, so a second click sends a second Insert until the handler has reassigned the result ([Neatoo#103](https://github.com/NeatooDotNet/Neatoo/issues/103)). Disable the button while a save is in flight.

The page binds to facts; every show/hide and enable/disable is a value the aggregate computed:

<!-- snippet: skill-page-binds-facts -->
<a id='snippet-skill-page-binds-facts'></a>
```razor
@* @key: Save returns a new instance; keying on it rebuilds the bound
   components so each field subscribes to the new instance's wrappers. *@
<div @key="Vm.WorkOrder">
@* Every show/hide and enable/disable here is a business fact the
   aggregate computed. The page has no condition of its own. *@
@if (Vm.ShowHoldBanner)
{
    <MudAlert Severity="Severity.Warning" Class="mb-3">This work order is on hold.</MudAlert>
}

@if (Vm.WorkOrder.IsOverBudget)
{
    <MudAlert Severity="Severity.Error" Class="mb-3">Over budget: @Vm.WorkOrder.Summary</MudAlert>
}

@if (Vm.WorkOrder.HasUnschedulableTasks)
{
    <MudAlert Severity="Severity.Info" Class="mb-3">Some tasks cannot be scheduled.</MudAlert>
}

<NeatooValidationSummary Entity="@Vm.WorkOrder" Class="mb-4" />

<MudText Typo="Typo.subtitle1">Status: @Vm.WorkOrder.Status</MudText>

<MudNeatooNumericField T="decimal" EntityProperty="@Vm.WorkOrder[nameof(IWorkOrder.Budget)]" Class="mb-3" />
<MudNeatooNumericField T="decimal" EntityProperty="@Vm.WorkOrder[nameof(IWorkOrder.Discount)]" Class="mb-3" />
<MudNeatooSwitch T="bool" EntityProperty="@Vm.WorkOrder[nameof(IWorkOrder.IsOnHold)]" Class="mb-3" />

<MudText Typo="Typo.h6" Class="my-3">Tasks</MudText>

@if (Vm.WorkOrder.Tasks is not null)
{
    @foreach (var task in Vm.WorkOrder.Tasks)
    {
        <MudGrid Class="mb-2" Spacing="2">
            <MudItem xs="12" sm="5">
                <MudNeatooTextField T="string" EntityProperty="@task[nameof(IWorkOrderTask.Name)]" />
            </MudItem>
            <MudItem xs="6" sm="2">
                <MudNeatooNumericField T="decimal" EntityProperty="@task[nameof(IWorkOrderTask.Hours)]" />
            </MudItem>
            <MudItem xs="6" sm="2">
                <MudNeatooNumericField T="decimal" EntityProperty="@task[nameof(IWorkOrderTask.Rate)]" />
            </MudItem>
            <MudItem xs="12" sm="3" Class="d-flex align-center">
                <MudText>@task.Cost.ToString("C")</MudText>
            </MudItem>
        </MudGrid>
    }
}

<MudButton Variant="Variant.Outlined" OnClick="@(() => Vm.AddTask("New task"))" Class="mb-3">Add Task</MudButton>

<MudText Typo="Typo.subtitle1" Class="mb-3">Total: @Vm.WorkOrder.TotalCost.ToString("C")</MudText>

@* The verb's button is enabled by the entity's admission fact. *@
<MudButton Variant="Variant.Outlined"
           Color="Color.Secondary"
           Disabled="@(!Vm.CanApprove)"
           OnClick="@(() => Vm.Approve("design.app"))"
           Class="mr-2">
    Approve
</MudButton>

<MudButton Variant="Variant.Filled"
           Color="Color.Primary"
           Disabled="@(!Vm.CanSave)"
           OnClick="SaveAsync">
    Save
</MudButton>
</div>
```
<sup><a href='/src/Design/Design.App/Pages/WorkOrderPage.razor#L20-L92' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-page-binds-facts' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The ViewModel that page binds to, the field components, the validation summary and the property-change events are in [blazor.md](references/blazor.md). Business logic in a `.razor` file or a ViewModel is the largest incident cluster in the record; the test is whether the logic could be unit-tested without a browser.

### Interface-first, and why

Every entity and list gets a matched public interface; concretes are `internal`; every reference uses the interface. Root interfaces extend `IEntityRoot` (adds `IsSavable` and `Save()`); child interfaces extend `IEntityBase`. Strongly recommended, not enforced - and the reason is a real bug: `EntityBase` gives every concrete `IsSavable`, and a modified child concrete reports `true` although children are saved by their root. Through `IEntityBase` the mistake is a compile error. The interfaces: [collections.md](references/collections.md); the child that cannot be saved: [entities.md](references/entities.md).

### A rule that needs the server

Two shapes, no preference: the rule takes the delegate of a `[Remote, Execute]` command, or an interface-factory domain service. On the client either one is a wrapped server call; a server-only service in a rule's constructor makes the entity impossible to build on the client. Both in [validation.md](references/validation.md) and [domain-logic-placement.md](references/domain-logic-placement.md). Bind the property on field commit, not per keystroke.

### One class, two roles

An entity type is a root, child-only, or both. Both: root operations take services and are `[Remote]`; child operations take the child's row from its parent and are `internal`. The factory method signature is the whole distinction, and an instance fetched as a child is saved by its list - calling `Save()` on it would route to the root `[Update]` and bypass the parent. [entities.md](references/entities.md#one-class-in-both-roles).

### Value object, input model, read model

A value object is a `record` ([base-classes.md](references/base-classes.md#a-value-object-is-a-record)). An input model is `ValidateBase` - rules, no persistence lifecycle of its own ([base-classes.md](references/base-classes.md)). A read model is a plain `[Factory]` class with `[Fetch]`, no base class. None of the three is an entity.

### The server gate

The root's `[Insert]` and `[Update]` run `RunRules(All)` and throw on an invalid aggregate before writing. That is the server's safety net against a caller that skipped `IsSavable` - a direct `factory.Save` - and callers never mirror it. The framework does not run it for you. [validation.md](references/validation.md) - the gate refuses.

## Known pains

The ones a new aggregate meets first; [pains.md](references/pains.md) has all of them with today's workaround.

- `Save()` does not mark the entity busy; double-click sends two Inserts - [Neatoo#103](https://github.com/NeatooDotNet/Neatoo/issues/103).
- `Save()` returns a new instance; a discarded result leaves a stale object - an analyzer is pending; until then, reassign.
- A rule trigger on a collection *reference* (`t => t.Items`) fires only when the list is reassigned; trigger on the child property path (`t => t.Items![0].LineTotal`) - an analyzer is pending.
- `LoadValue` inside a factory operation is wrong (the object is already paused) - an analyzer is pending.
- Registering `AddNeatooAspNetCore` after `AddNeatooServices(Server)` breaks every remote Save - [RemoteFactory#117](https://github.com/NeatooDotNet/RemoteFactory/issues/117); the RemoteFactory skill has the safe shape.
- `LazyLoad<T>` / `EntityLazyLoad` are being removed from RemoteFactory and Neatoo; build nothing new on them. The replacement is a `[Remote] [Fetch]` on the child list's factory - RemoteFactory skill.

## References

Each file is a mechanism reference under the decisions above. Every block in them is compiled.

- [base-classes.md](references/base-classes.md) - the four base classes, the input model, the read model, the command, the job over entities, the value object record
- [entities.md](references/entities.md) - the entity lifecycle: Create/Fetch/Insert/Update/Delete, IsNew vs IsModified, the child that cannot be saved, the list update, one class in both roles
- [collections.md](references/collections.md) - lists, the aggregate interfaces, DeletedList, validity aggregation
- [properties.md](references/properties.md) - partial properties, the property object, LoadValue, MarkReadOnly, pause
- [validation.md](references/validation.md) - rule types, attributes and rules, the server gate, rules that reach the server
- [rules-lifecycle.md](references/rules-lifecycle.md) - when rules fire and when they don't, the Fetch that computes what it needs, RunRulesFlag
- [domain-logic-placement.md](references/domain-logic-placement.md) - rule, verb, seam, read model with the WorkOrder aggregate: admission, derived facts, chained rules, child triggers, Parent
- [shared-rules.md](references/shared-rules.md) - one rule across several entity types
- [blazor.md](references/blazor.md) - the save handler, the ViewModel over an aggregate, the page that binds facts, Neatoo on each tier, MudNeatoo
- [testing.md](references/testing.md) - real Neatoo objects, the test host, the client/server pair
- [source-generation.md](references/source-generation.md) - what the two generators produce
- [trimming.md](references/trimming.md) - IL trimming for the Blazor client
- [pitfalls.md](references/pitfalls.md) - the mistakes, each with its fix
- [pains.md](references/pains.md) - open framework defects and missing analyzers, each with its issue and workaround
- [lazy-loading.md](references/lazy-loading.md) - pending removal; do not copy

Mechanism that used to live in this file lives in these references: the base-class quick reference and a complete entity → [base-classes.md](references/base-classes.md); the state properties, save routing, edit-then-save and the aggregate save cascade → [entities.md](references/entities.md); partial properties → [properties.md](references/properties.md); `Parent` (in a rule, a factory method, a verb), the aggregate as a graph and rules for open mutation, child-property triggers and the parent orchestrator → [domain-logic-placement.md](references/domain-logic-placement.md); the server gate and validation → [validation.md](references/validation.md); troubleshooting → [pitfalls.md](references/pitfalls.md).
