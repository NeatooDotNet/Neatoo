---
name: Neatoo
description: This skill should be used when working with Neatoo domain models, ValidateBase, EntityBase, ValidateListBase, EntityListBase, partial properties, property change tracking, validation rules, business rules, aggregate roots, entities, value objects, lazy loading, EntityLazyLoad, IEntityLazyLoadFactory, or any .NET DDD domain model framework work. Also triggers for IsValid, IsSelfValid, IsSavable, IsModified, IsNew, IsDeleted, RuleManager, AddActionAsync, AddValidationAsync, AddAction, AddValidation, IsBusy, WaitForTasks, IsLoaded, IsLoading, and base class behavior. This skill also decides where business logic belongs: the placement ladder (entity rule, entity verb, orchestration seam, read model / Info class) and the ViewModel boundary -- the gesture test, the mirror rule, and why a load-time policy is a seam, never a ViewModel. Consult it before writing any ViewModel member that writes to an entity, any [Execute] or [Fetch] orchestration body, any Info read model, or any .razor file that binds to Neatoo entities. Neatoo is the domain model framework -- it does NOT include factory generation. For factory attributes ([Factory], [Create], [Fetch], [Remote], [Service], [AuthorizeFactory]) see the RemoteFactory skill, which is independent and works with any .NET class.
version: 1.0.0
---

# Neatoo Domain Models

Neatoo is a .NET framework for building domain models with automatic change tracking, validation, and rules through Roslyn source generators. It provides base classes that map to DDD concepts.

Neatoo focuses on the domain model: properties, change tracking, validation, rules, and collections. RemoteFactory is a separate, independent tool that generates client-server factories for **any .NET class** — it works with Neatoo entities, plain ViewModels, or POCOs. For factory attributes, authorization, and client-server patterns, see the RemoteFactory skill.

Every code block in this skill is compiled and tested in the Neatoo repository's `Design.Domain` project.

## Quick Start

Every entity gets a public interface. A root's interface extends `IEntityRoot`, which exposes `IsSavable` and `Save()`:

<!-- snippet: skill-quick-start-interface -->
<a id='snippet-skill-quick-start-interface'></a>
```cs
/// <summary>
/// Aggregate root interface. Extends IEntityRoot: exposes IsSavable and Save().
/// </summary>
public interface IProduct : IEntityRoot
{
    Guid Id { get; }
    string? Name { get; set; }
    decimal Price { get; set; }
}
```
<sup><a href='/src/Design/Design.Domain/Entities/Product.cs#L16-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quick-start-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The concrete class is `internal`. `[Create]` runs locally; the persistence operations are `[Remote]` and `internal`, and take the repository as a `[Service]` parameter, so they run on the server:

<!-- snippet: skill-quick-start -->
<a id='snippet-skill-quick-start'></a>
```cs
/// <summary>
/// Demonstrates: the minimal aggregate root. Concrete is internal; consumers
/// hold IProduct.
/// </summary>
[Factory]
internal partial class Product : EntityBase<Product>, IProduct
{
    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public partial string? Name { get; set; }

    [Range(0, 1000000, ErrorMessage = "Price cannot be negative")]
    public partial decimal Price { get; set; }

    public Product(IEntityBaseServices<Product> services) : base(services) { }

    // Local: creating a product needs nothing from the server
    [Create]
    public void Create() { }

    // [Remote]: the client fetches this root, so the call crosses to the
    // server, where the repository resolves. Returning false makes the
    // generated factory return null: "no such product" is an answer.
    [Remote]
    [Fetch]
    internal bool Fetch(Guid id, [Service] IProductRepository repository)
    {
        var row = repository.Get(id);
        if (row == null)
        {
            return false;
        }

        // Paused for the length of the body: assignment is a clean baseline load
        Id = row.Id;
        Name = row.Name;
        Price = row.Price;
        return true;
    }

    [Remote]
    [Insert]
    internal async Task Insert([Service] IProductRepository repository)
    {
        // Re-run the rules on the server and refuse an invalid aggregate.
        // Throw, never return: after [Insert] returns, the framework marks
        // the entity saved whether or not anything was written.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();  // the entity sets its own key

        var row = new ProductRow();
        MapTo(row);
        repository.Add(row);
        repository.SaveChanges();
    }

    [Remote]
    [Update]
    internal async Task Update([Service] IProductRepository repository)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Product {Id} not found");

        MapTo(row);
        repository.SaveChanges();
    }

    [Remote]
    [Delete]
    internal void Delete([Service] IProductRepository repository)
    {
        var row = repository.Get(Id)
            ?? throw new KeyNotFoundException($"Product {Id} not found");

        repository.Remove(row);
        repository.SaveChanges();
    }

    private void MapTo(ProductRow row)
    {
        row.Id = Id;
        row.Name = Name!;
        row.Price = Price;
    }
}
```
<sup><a href='/src/Design/Design.Domain/Entities/Product.cs#L28-L126' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quick-start' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This generates `IProductFactory` with `Create()`, `Fetch(Guid)` and `Save(IProduct)`. Properties track changes, trigger validation, and fire `PropertyChanged`.

Using it: a created object is `IsNew` and not `IsModified` (it needs inserting but holds no user work); an edit makes it savable once its rules pass:

<!-- snippet: skill-quick-start-create -->
<a id='snippet-skill-quick-start-create'></a>
```cs
[TestMethod]
public async Task Create_ThenEdit_IsSavable()
{
    var product = _factory.Create();
    Assert.IsTrue(product.IsNew);
    Assert.IsFalse(product.IsModified, "A created object holds no user work");

    product.Name = "Widget";
    product.Price = 9.99m;
    await product.WaitForTasks();

    Assert.IsTrue(product.IsValid);
    Assert.IsTrue(product.IsSavable);
}
```
<sup><a href='/src/Design/Design.Tests/EntityTests/ProductTests.cs#L35-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quick-start-create' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`Save()` routes to `Insert` or `Update` on `IsNew` and returns the saved instance — keep that one. A `Fetch` is a clean baseline:

<!-- snippet: skill-quick-start-save -->
<a id='snippet-skill-quick-start-save'></a>
```cs
[TestMethod]
public async Task Save_ThenFetch_RoundTrips()
{
    var product = _factory.Create();
    product.Name = "Widget";
    product.Price = 9.99m;
    await product.WaitForTasks();

    // Save returns the saved instance; keep that one
    product = (IProduct)await product.Save();
    Assert.IsFalse(product.IsNew);
    Assert.IsFalse(product.IsModified);

    var fetched = await _factory.Fetch(product.Id);
    Assert.IsNotNull(fetched);
    Assert.AreEqual("Widget", fetched.Name);
    Assert.IsFalse(fetched.IsModified, "A fetched object is a clean baseline");
}
```
<sup><a href='/src/Design/Design.Tests/EntityTests/ProductTests.cs#L52-L71' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-quick-start-save' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Interface-First Design

Every entity and list gets a matched public interface. Concretes are `internal`. All references — properties, parameters, list type arguments, factory method parameters — use the interface, never the concrete.

1. Root entity interfaces extend `IEntityRoot` (exposes `IsSavable` and `Save()`).
2. Child entity interfaces extend `IEntityBase` (no `IsSavable`, no `Save()`).
3. List interfaces extend `IEntityListBase<IChild>` — parameterized on the child **interface**.
4. `ValidateBase` objects follow the same pattern (`IValidateBase`, `IValidateListBase<I>`).

This is what makes the root/child split work. `EntityBase<T>` implements both `IEntityBase` and `IEntityRoot`, so `IsSavable` and `Save()` exist on every concrete — a modified child concrete reports `IsSavable == true`, because `IsSavable` knows nothing about aggregate position. Because concretes are `internal`, a consumer only ever holds the interface, and a child interface has no `Save()` to call. The mistake is a compile error, not a runtime check.

<!-- snippet: skill-aggregate-interfaces -->
<a id='snippet-skill-aggregate-interfaces'></a>
```cs
/// <summary>
/// Aggregate root interface — extends IEntityRoot.
/// Exposes IsSavable and Save() for the root entity.
/// All property types use interfaces, never concretes.
/// </summary>
public interface IOrder : IEntityRoot
{
    Guid Id { get; }
    string? OrderNumber { get; set; }
    string? CustomerName { get; set; }
    DateTime OrderDate { get; set; }
    string? Status { get; set; }
    decimal TotalAmount { get; }
    IOrderItemList? Items { get; }

    /// <summary>
    /// Verb: moves the order to Submitted. Sets state; never persists.
    /// </summary>
    void Submit();
}

/// <summary>
/// Child entity interface — extends IEntityBase only.
/// No IsSavable, no Save(). Child entities are saved through the aggregate root.
/// </summary>
public interface IOrderItem : IEntityBase
{
    Guid Id { get; }
    string? ProductName { get; set; }
    int Quantity { get; set; }
    decimal UnitPrice { get; set; }
    decimal LineTotal { get; }
}

/// <summary>
/// List interface — extends IEntityListBase parameterized on child INTERFACE.
/// Lists never expose IsSavable — they are always saved through the aggregate root.
/// </summary>
public interface IOrderItemList : IEntityListBase<IOrderItem>
{
    /// <summary>
    /// Test helper: Exposes the count of items in DeletedList.
    /// </summary>
    int DeletedCount { get; }
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/IOrderInterfaces.cs#L68-L114' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-aggregate-interfaces' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Domain Logic First — The Core Principle

**Business logic belongs in the domain layer.** In a Neatoo application that layer is more than the entities. It is four things: the entities and their rules; the verbs on those entities; the orchestration seams — `[Remote, Execute]` operations on static commands and plain `[Factory]` classes — that coordinate across aggregates and run at load time; and the read models (`Info` classes) that compute server truth for display and gating. All four are domain. The UI — ViewModel and Razor together — is a binding and gesture-adapting layer over them. It holds view state. It holds no policy.

**Decide ownership before mechanism.** The most common placement failure is finding a mechanism that works and letting it pick the layer. Before writing any behavior, answer three questions about it:

1. **Who initiates it?** A user gesture · a property change · a load or fetch · another aggregate or a command.
2. **What does it need?** Nothing beyond its own entity · injected services · other aggregates · server-side truth.
3. **Is it applied or staged?** Takes effect immediately · sits in memory for the user to confirm.

The answers select a rung on the ladder. The rung selects the mechanism. Never the reverse.

### The Placement Ladder

Work down from the top and stop at the first rung that fits. The two rungs below the line are not homes: they bind to, invoke, or mirror the rung above them.

| Rung | Home | Choose it when | Mechanism |
|---|---|---|---|
| 1 | **Entity rule** | The behavior reacts to a property change and runs wherever the entity is — the browser included. What it needs from the server it reaches through an injected `[Remote, Execute]` command delegate, never a server-only service. | `AddAction` · `AddValidation` · `RuleBase<T>` · `AsyncRuleBase<T>` |
| 2 | **Entity verb** | A user invokes an operation on one aggregate. The verb sets state, rules validate, the caller saves. | Public method on the entity; `CanX` exposed by a rung-1 rule |
| 3 | **Orchestration seam** | The behavior crosses aggregates, needs server `[Service]`s, or runs at load — where rules are paused. Load-time policy lives here. A seam that may create or load returns from an `[Execute]`; `[Fetch]` is only for data that exists. | `[Remote, Execute]` on a static command or a plain `[Factory]` class |
| 4 | **Read model** | The screen needs server truth for display or gating: flags, counts, cadence, "is X due." | Plain `[Factory]` `Info` class, `[Remote, Fetch]` only, no Neatoo base |
| — | | | |
| 5 | ViewModel | Adapts a gesture into a call on rung 1–3 · binds · **mirrors a gate by reading it** · coordinates save and navigation. | `ObservableObject`; factories and commands by DI |
| 6 | Razor | Binds. | MudNeatoo components |

**Never fall from "not a rule" to "so, the ViewModel."** When rung 1 doesn't fit — the work is something the user should start on purpose, or must run during `[Fetch]` where rules are paused, or must be staged rather than applied — the next rung is 3, not 5. The ViewModel is not exempt from this ladder because it is C#, testable, and has DI.

### Where Logic Goes

Indexed by the behavior you are placing, not by the trigger you would wire.

| The behavior | Home | Not in |
|---|---|---|
| A value derived from the entity's own properties | Entity rule (1) | ViewModel or Razor arithmetic |
| A value that needs server truth — a count, a cadence, "is X due" | Read model (4) | ViewModel composing it from ids and flags |
| Whether a control is enabled | Entity `CanX` by rule (1) or a read-model flag (4); the ViewModel reads it | ViewModel `&&`-ing flags together |
| A mutation caused by a user gesture | ViewModel calls an entity setter or verb (2) | The ViewModel holding the logic the verb should own |
| A mutation caused by a property change | Entity rule (1) | A ViewModel `PropertyChanged` handler that writes back |
| A mutation caused by a load — a policy applied on the user's behalf | Orchestration seam (3): applied to the returned in-memory graph, staged, with an explanation the UI can show | ViewModel `InitializeAsync` |
| An operation across two aggregates | Orchestration seam (3) | ViewModel bridging two entities |
| Whether a verb may run | Entity `CanX` rule or validation rule (1), or a read-model flag (4); the ViewModel reads it | A seam that checks data and drops out; a ViewModel re-deriving the check from atoms |
| Parent reacts to a child's change | Entity rule with child trigger `t => t.Items![0].Prop` (1) | UI event handler |
| Cross-property validation | `AddValidation` on a computed property, or `RuleBase<T>` with several triggers (1) | UI validation |
| Cross-sibling consistency in a list | Override `HandleNeatooPropertyChanged` on the list class (1) | UI bridging |
| A check or lookup that reacts to a property change and needs server truth — uniqueness, overlap, a duplicate | Async rule (1) with an injected `[Remote, Execute]` command | A guard in `[Insert]`/`[Update]`; a rule or entity constructor that takes a repository or other server-only service |
| A recalculation the user should start on purpose, or one that takes several server calls | Entity verb (2) or seam (3), invoked by the ViewModel on a gesture | A rule that fires it every time a field changes |

### The Mirror Rule

A ViewModel may pre-disable a control so the user isn't sent on a round-trip the domain would refuse. That mirror **reads** the domain's answer. It never **re-derives** it.

- Reads — cannot drift from the domain: `public bool CanEndEarly => _visit?.Plan.CanEndEarly ?? false;`
- Re-derives — the rule now has two owners, and this one silently decides what the UI shows: `public bool CanStartTherapy => IsApproved && IsSymptomsComplete && (IsSignsComplete || !IsSignsDue);`

If the domain doesn't expose the answer, that is the missing domain member — a `CanX` rule on the entity or a flag on the read model — not a ViewModel computation.

### The Gesture Test

Before writing any ViewModel member that writes to an entity or calls a mutating verb: **which user gesture is this handling?** A dropdown pick, a button, a keystroke — name it. If the honest answer is "none, it's just what should happen," stop. It is a rule or a seam, and it is in the wrong layer.

### The Smell Test

**Razor:** more than three conditional or computed expressions, and logic has leaked. Move it down.

**ViewModel** — the leak the Razor count cannot see, because the ViewModel absorbed the logic before it reached the markup: any member that reads two or more entity or read-model values and yields a bool or a derived value is re-deriving domain logic. Any method that writes to an entity and is not the handler for a named gesture is a rule or a seam in the wrong layer.

- WRONG — the ViewModel composes a gate from server atoms: `public bool CanStartTherapy => IsApprovedOrMaintenance && IsSymptomsComplete && IsSignsReady;`
- RIGHT — the read model computes it server-side and the ViewModel reads it: `public bool CanStartTherapy => Info.CanStartTherapy;`

See `references/domain-logic-placement.md` for the decision tree that walks the ladder, and for the rung-1 wiring patterns: computed properties, cascading state, child property triggers, class-based rules with an injected command.

## The Three-Phase Pattern

Every user interaction in a Neatoo app follows three sequential, non-overlapping phases:

**1. Set state.** Business methods — on the root, on children, called by any consumer — mutate properties. `IsModified` becomes true. `PropertyChanged` fires. Adding a child to a collection (`itemFactory.Create(...)` then `order.Items.Add(item)`, or a list method such as `AddPhoneNumber()` when the child's `[Create]` takes no parameters) is also phase 1; these are state mutations, not persistence.

**2. Validate.** Rules re-run on the affected properties — sync (`AddAction`, `AddValidation`, `RuleBase<T>`) and async (`AddActionAsync`, `AddValidationAsync`, `AsyncRuleBase<T>`). `IsSelfValid` is the entity alone; `IsValid` aggregates the entity plus every descendant. Consumers call `await entity.WaitForTasks()` if async rules may be in flight before reading `IsValid` / calling `Save()`.

**3. Save.** The caller invokes `Save()` on the root. Factory methods (`[Insert]` / `[Update]` / `[Delete]`, routed by `IsNew` / `IsDeleted`) execute the persistence cascade: re-run the rules, map the root into its row, hand the row's child collection to the list factory's `Save`, flush once, raise factory events.

**The phases don't cross.** The line is *ownership*, not what happens downstream of a call.

- A business method never owns persistence — no repositories, no transactions, no factory events, no `[Service]` parameters. It may pass *through* a factory-generated seam (`Save()`, an `[Execute]`, a command delegate) exactly as a ViewModel does: the seam carries the injection, authorization, transaction, and save cascade, and the caller owns none of it. A method that saves and returns its successor, or hands a validated value to a command, is still inside the three phases.
- A persisting factory method — `[Insert]`, `[Update]`, `[Delete]` — never calls a business method or reaches back into phase 1. What it needs to read, it reads from state that phase 1 set.
- An orchestration seam — a `[Remote, Execute]` — is not a persisting factory method, and the line above does not bind it. It *is* phases 1 and 2 performed on the caller's behalf, for the mutations no user gesture initiates (ladder rung 3): it may set state and call verbs on the graph it hands back, staged in memory, and it persists nothing except through `Save`.

### Persisting Factory Method vs. Business Method Boundary

| Persisting factory methods own | Business methods own |
|---|---|
| `MapTo` the row (EF entity) | Property setters |
| Repository calls | Call other business methods on `this` |
| Transaction begin / commit (one flush per root save) | Call business methods on children (`this.Child.Method()`) |
| Raise factory events (via `[Service] IFactoryEvents`) | Add items to child collections |
| Hand the row's child collection to the list factory's `Save` | Queue records onto state-collection properties |
| `[Service]` parameter injection | Read `Parent` reference for ambient root state |
| DB-snapshot-vs-in-memory diffing | Call `Save()`, an `[Execute]`, or a command delegate — pass-through, not ownership |

A `[Service]` parameter is resolved by the factory for that operation. It is never passed on to another method as an ordinary argument; the body that needs it is the operation itself.

### What the Save Needs Must Be State

Factory methods can only read what's on the entity graph. They cannot read call-context, local variables from the business method that triggered the save, or "intentions" the caller held in their head.

If a save decision depends on something, put it on an entity:

- "Emit the deferred APPROVED audit at archive time" → `[Update]` reads `IsApproved && !PreHasApprovedAudit && ((IVisit)Parent!).Archived`
- "Force a side-effect on this save" → a flag the business method sets and the `[Update]` reads (e.g., `ForceEndReassess`)
- "This save is an extension, not a modification" → loaded state from Fetch (e.g., `PreApprovedTreatments`) compared against current in-memory value
- "Idempotency — don't emit this audit twice" → a `PreHasX` flag loaded during Fetch

Don't:

- Pass flags as parameters to the root's `Save()` — its signature is fixed
- Stash post-business state in a service instance
- Reach from `[Update]` back into a business method to "ask" about something
- Infer intent from `IsModified` alone when the save decision depends on a combination of in-memory values and ambient state

## The Aggregate Is a Graph, Not a Façade

Strict DDD treats the aggregate root as the single entry point: a `Visit` class would expose `EndPlanEarly(reason)` that internally calls `Plan.EndEarly(reason)`; consumers only ever touch the root; children are hidden implementation details.

Neatoo rejects that encapsulation boundary. The aggregate is a graph whose nodes are all directly addressable by any consumer:

- A ViewModel calls a child business method directly: `visit.Plan.EndEarly(reason);`
- A Razor component binds to a deep property: `<MudNeatooTextField EntityProperty="@visit.Plan[nameof(IPlan.EndedEarlyReason)]" />`
- A service reads any depth: `order.LineItems[0].Product.Sku`

**The root is a coordinator, not a gate.** It owns `Save()`, raises factory events at save time, and its `IsValid` / `IsModified` / `IsSavable` aggregate every descendant. But it does not mediate access to children.

**Encapsulation lives at the property level.** Private setters, business methods, and validation rules guard state wherever it lives. You don't need the root to mediate; each entity's own surface does.

**Consequence:** design every entity as though any consumer can call its methods and bind to its properties. Don't try to rebuild strict DDD gatekeeping by routing every child mutation through root wrappers — you'll fight the framework and end up duplicating logic between the root and child.

## Designing Rules for Open Mutation

Because any consumer can call `visit.Plan.EndEarly(reason)` as a first-class operation, your rule graph must converge correctly after that call:

1. `Plan.EndEarly` sets `Plan.EndedEarly = true` and `Plan.EndedEarlyReason = reason`
2. `Plan`'s own rules run — child-level validation
3. `Plan.IsValid` / `Plan.IsSelfValid` update; `PropertyChanged` / `NeatooPropertyChanged` fires
4. `Visit`'s rules that trigger on `Plan` properties re-run (via `AddAction` with a child-property trigger)
5. `Visit.IsValid` aggregates — root valid only if self plus every descendant is valid
6. `PropertyChanged` fires on the root for `IsValid` / `IsSavable`; bindings re-render

**Design rule:** every mutation a consumer can make must leave the root in a correct state after all rules have run. If external mutation of a child can put the root into an invalid-but-unreported state, the rule graph is incomplete. This is the rule author's responsibility, not a framework guarantee.

### Rule placement by scope

| Invariant scope | Where the rule lives | Trigger |
|---|---|---|
| Child's own state | Child class | Own property |
| Root state that depends on child state | Root class | Child-property trigger on root rule |
| Summary of child state exposed on root | Root class (`AddAction`) | Child-property trigger |
| Sibling consistency in a list | List class | Override `HandleNeatooPropertyChanged` on the list |

**Convergence check.** After any business method that mutates a descendant, confirm `root.IsValid` reflects the full graph. If not, a rule is missing — typically on the root or an ancestor, triggered by the child property that was mutated. See `references/domain-logic-placement.md` → Pattern 6 (Child Property Triggers) and `references/rules-lifecycle.md` for trigger semantics.

## Parent — The Child's Window to Ambient State

Every child entity has a `Parent` reference, set by the property system at runtime when the child is assigned to a parent's partial property or added to a child collection. `Parent` is a first-class API, not an implementation detail.

### Inside a child rule

A rule can run before the child is attached, so pattern-match rather than cast:

<!-- snippet: skill-parent-in-child-rule -->
<a id='snippet-skill-parent-in-child-rule'></a>
```cs
// A child rule reads ambient root state through Parent. Parent is null
// until the task is attached, so pattern-match instead of casting.
RuleManager.AddAction(
    t => t.IsSchedulable = t.Hours > 0 && t.Parent is IWorkOrder root && !root.IsOnHold,
    t => t.Hours);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L47-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-in-child-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Inside a child factory method

On a persistence path the child is always attached. Here the task decides a denormalized column from the root's state:

<!-- snippet: skill-parent-in-child-update -->
<a id='snippet-skill-parent-in-child-update'></a>
```cs
[Update]
internal void Update(WorkOrderTaskRow row)
{
    MapTo(row);
}

private void MapTo(WorkOrderTaskRow row)
{
    row.Id = Id;
    row.Name = Name!;
    row.Sequence = Sequence;
    row.Hours = Hours;
    row.Rate = Rate;
    row.Discount = Discount;
    row.Cost = Cost;
    row.IsSchedulable = IsSchedulable;

    // A denormalized column decided from root state, read through Parent.
    // On a persistence path the task is always attached.
    row.WorkOrderStatus = ((IWorkOrder)this.Parent!).Status!;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L107-L129' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-in-child-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The saved row carries the root's status, read through `Parent` at save time:

<!-- snippet: skill-parent-in-update-test -->
<a id='snippet-skill-parent-in-update-test'></a>
```cs
[TestMethod]
public async Task Save_TaskMapsRootStateIntoItsRow_ThroughParent()
{
    var row = _repository.SeedWorkOrder(MockWorkOrderRepository.Task("Design", 1, 2m, 30m));

    var order = (await _factory.Fetch(row.Id))!;
    Assert.IsFalse(order.IsModified, "A fetched aggregate is a clean baseline");
    Assert.IsTrue(order.CanApprove, "Fetch re-ran the root's own rules for its derived values");

    order.Tasks![0].Hours = 3m;   // a modified child: the list hands it its row
    order.Approve("ada");
    await order.WaitForTasks();
    Assert.IsTrue(order.IsSavable);

    order = (IWorkOrder)await order.Save();

    var saved = _repository.Store[row.Id];
    Assert.AreEqual("Approved", saved.Status);
    Assert.AreEqual("ada", saved.ApprovedBy);
    Assert.AreEqual(3m, saved.Tasks.Single().Hours);
    Assert.AreEqual("Approved", saved.Tasks.Single().WorkOrderStatus,
        "The task read the root's status through Parent when it mapped itself");
    Assert.IsFalse(order.IsModified);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/WorkOrderAggregateTests.cs#L154-L179' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-in-update-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Inside a child business method

<!-- snippet: skill-parent-in-child-method -->
<a id='snippet-skill-parent-in-child-method'></a>
```cs
/// <summary>
/// Child business method reading root state through Parent. Called by the
/// root's orchestrator rule whenever the discount changes.
/// </summary>
public void ApplyParentDiscount()
{
    Discount = ((IWorkOrder)this.Parent!).Discount;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L74-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-in-child-method' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Cast pattern

`Parent` is typed `IValidateBase?`. Cast to the root's interface when accessed: `((IRoot)this.Parent!).X`. If the same child type can be attached to different roots in different contexts, guard with `is IRoot root` pattern matching.

### Rules of use

- **Children read Parent; parents write children.** Don't mutate through `Parent` from child code — it inverts the graph.
- **Parent access is aggregate-scoped.** A child shouldn't use `Parent` to reach an entity belonging to a *different* aggregate. If you need that, the aggregate boundary is drawn wrong.
- **`Parent` is nullable at the type level, non-null at runtime once attached.** Before attachment (just-constructed, not yet assigned to a parent), `Parent` is null. In business methods and `[Update]` paths the entity is always attached.
- **A child rule that reads `Parent` has no trigger for the parent's property.** When that state changes, the parent re-runs the children's rules (an `AddActionAsync` on the parent property that calls `task.RunRules()` on each child — see the orchestrator rule under "Child Property Triggers").

### Why Parent is under-taught in DDD literature

DDD orthodoxy flags child→parent references as smelly: "children shouldn't know about parents; if they need parent state, the parent should call a child method passing the data." Neatoo's position: **domain aggregates are inherently coupled graphs.** Coupling inside the aggregate boundary is expected and wanted. `Parent` is the natural API for a child to read ambient aggregate context, and building workarounds to avoid it produces duplicate state flow and harder-to-follow code.

The coupling concerns DDD raises are real — they apply to coupling *across* aggregate boundaries, not within. Neatoo's `Parent` stays within the aggregate by design.

## Base Class Quick Reference

| DDD Concept | Neatoo Base Class | Use When |
|-------------|-------------------|----------|
| Aggregate Root | `EntityBase<T>`, interface extends `IEntityRoot` | Root entity with full CRUD lifecycle |
| Entity | `EntityBase<T>`, interface extends `IEntityBase` | Child entity within an aggregate |
| Value Object | `ValidateBase<T>` | Editable data that needs rules, no persistence lifecycle of its own |
| Entity Collection | `EntityListBase<I>` | List of child entities (tracks deletions) |
| Validate Collection | `ValidateListBase<I>` | List of `ValidateBase` objects (no deletion tracking) |
| Command | Static partial class with `[Remote, Execute]` | Operation that needs the server; a bare `[Execute]` runs on the calling tier |
| Orchestration context | Plain `[Factory]` class, no Neatoo base, `[Remote, Execute]` verbs | Bundles entities and derived state for one screen or flow; runs cross-aggregate and load-time logic (ladder rung 3) |
| Read Model | Plain `[Factory]` class, `[Remote, Fetch]` only, `internal set` properties | Server-computed truth for display and gating — `XxxInfo` (ladder rung 4). No rules, no validation. |

## Key Properties

**There is no `IsDirty` in Neatoo.** Use `IsModified` / `IsSelfModified`.

| Property | Type | Meaning |
|----------|------|---------|
| `IsModified` | bool | Differs from the baseline the factory op left — would discarding it lose work? `PropertyManager.IsModified \|\| IsDeleted \|\| IsSelfModified`. **Does NOT include `IsNew`**: false after Create, false after Fetch. |
| `IsSelfModified` | bool | This object's own properties changed (excludes children) |
| `IsValid` | bool | This object and all children pass validation |
| `IsSelfValid` | bool | This object (only) passes validation |
| `IsSavable` | bool | `(IsModified \|\| IsNew) && IsValid && !IsBusy`. Knows nothing about aggregate position — a modified child concrete reports `true`. Only `IEntityRoot` exposes it. |
| `IsNew` | bool | Not yet persisted. Set true by Create, false by Fetch/Insert. Routing state only — it does **not** imply `IsModified`; a created object is savable but not modified. |
| `IsDeleted` | bool | Marked for deletion |
| `RuleManager` | IRuleManager | Access to validation rules |

## Core Patterns

### Properties with Change Tracking

All Neatoo properties use `partial` properties. The source generator implements backing fields with automatic change tracking and validation triggering:

<!-- snippet: skill-partial-properties -->
<a id='snippet-skill-partial-properties'></a>
```cs
public partial string? Name { get; set; }

public partial int Count { get; set; }

public partial decimal Price { get; set; }
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/PropertyBasics.cs#L75-L81' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-partial-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Each property is backed by its own `IValidateProperty<T>` object; the generated setter writes `NameProperty.Value`. See `references/properties.md`.

### Factory Methods

Neatoo entities use RemoteFactory for factory generation. See the `/RemoteFactory` skill for factory attributes (`[Factory]`, `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`), service injection (`[Service]`), remote execution (`[Remote]`), and authorization (`[AuthorizeFactory]`).

Two facts the Neatoo examples depend on:

- `[Remote]` marks a client entry point: when the client calls the operation, the call crosses to the server. It does not mean "this code runs on the server" — `internal` child and list operations run on the server too, because only server code calls them. Root operations the client calls are `[Remote] internal`; child and list operations are `internal`, never `[Remote]`.
- `[Create]` is `new`: it takes what a constructor would take. `[Fetch]` loads existing data by key. A loaded child is built with its own `[Fetch]`, never `[Create]`.

### Save Routing (Neatoo State-Based)

When `Save()` is called, the factory routes based on Neatoo entity state:
- `IsDeleted == true` → `[Delete]` (checked first; a created-then-deleted object is a no-op)
- `IsNew == true` → `[Insert]`
- otherwise → `[Update]`

`IsModified` is not consulted by routing. `entity.Save()` refuses to call the factory unless `IsSavable` is true; a direct `factory.Save(target)` does not check.

### Re-run the Rules on the Server Before the Write

Rules run in the browser as the user edits, so the client sees every broken rule before it saves. The server should not trust that. The root's `[Insert]` and `[Update]` re-run every rule and refuse an invalid aggregate before writing anything. The framework does not do this for you; it is the strongly recommended form:

<!-- snippet: skill-root-insert -->
<a id='snippet-skill-root-insert'></a>
```cs
[Remote]
[Insert]
internal async Task Insert([Service] IOrderRepository repository,
    [Service] IOrderItemListFactory itemsFactory)
{
    // Re-run every rule on the server and refuse an invalid aggregate.
    // Recommended - the framework does not do this for you. Throw, never
    // return: after [Insert]/[Update] returns, the framework marks the
    // entity saved whether or not anything was written.
    await RunRules(RunRulesFlag.All);
    if (!IsValid)
    {
        throw new SaveOperationException(SaveFailureReason.IsInvalid);
    }

    // Object is paused — assignment is clean
    Id = Guid.NewGuid();

    var row = new OrderRow();
    MapTo(row);
    repository.Add(row);

    itemsFactory.Save(Items!, row.Items);

    repository.SaveChanges();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L186-L213' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-root-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- `RunRulesFlag.All` checks the root and every child.
- `SaveOperationException(SaveFailureReason.IsInvalid)` is what `entity.Save()` already throws on the client for the same condition.
- **Throw, never `return`.** After `[Insert]` or `[Update]` returns, the framework marks the entity saved (not new, not modified) whether or not anything was written. An early `return` reports a save that did not happen.
- Reaching the throw means the client let an invalid aggregate through. It is an application failure, not validation feedback; the user's feedback is the rule message they saw while editing.

A test pins it: a direct `factory.Save` of an invalid order is refused and nothing is written.

<!-- snippet: skill-server-gate-refuses -->
<a id='snippet-skill-server-gate-refuses'></a>
```cs
[TestMethod]
public async Task InvalidOrder_DirectFactorySave_ServerRulesRefuseBeforeWriting()
{
    // Arrange: CustomerName is [Required], but rules do not run during
    // [Create], so the new order still reports valid on this side.
    var order = _orderFactory.Create();
    Assert.IsTrue(order.IsValid, "No rule has run yet, so nothing is broken");

    // Act: a direct factory.Save does not check IsSavable, so only the
    // re-run of the rules inside [Insert] stands between this order and
    // the write.
    var ex = await Assert.ThrowsExactlyAsync<Neatoo.SaveOperationException>(
        () => _orderFactory.Save(order));

    // Assert: refused, and nothing was written
    Assert.AreEqual(Neatoo.SaveFailureReason.IsInvalid, ex.Reason);
    Assert.AreEqual(0, _repository.AddedRows.Count, "No row may be added");
    Assert.AreEqual(0, _repository.SaveChangesCount, "No flush may happen");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/AggregateLifecycleTests.cs#L188-L208' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-server-gate-refuses' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Aggregate Save Cascading

State cascades UP automatically; saves cascade DOWN through the factories. The root makes or gets its row, hands the row's child collection to the list factory's `Save`, and flushes once. The list's `[Update]` makes, finds and removes child rows and hands each live child its row through the child factory's `Save`. The child maps itself into the row it is handed. See `references/entities.md` → "Aggregate Save Cascading" for the full pattern.

### Validation

Validation attributes on properties and `AddValidation` rules in the constructor:

<!-- snippet: skill-validation-attributes-and-rules -->
<a id='snippet-skill-validation-attributes-and-rules'></a>
```cs
[Required(ErrorMessage = "Street is required")]
[StringLength(100)]
public partial string? Street { get; set; }

[Required(ErrorMessage = "City is required")]
[StringLength(50)]
public partial string? City { get; set; }

[Required(ErrorMessage = "State is required")]
[StringLength(2, MinimumLength = 2, ErrorMessage = "State must be 2 characters")]
public partial string? State { get; set; }

[Required(ErrorMessage = "Zip code is required")]
[RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid zip code format")]
public partial string? ZipCode { get; set; }

[Required(ErrorMessage = "Address type is required")]
public partial string? AddressType { get; set; } // "Home", "Work", "Other"

public Address(IEntityBaseServices<Address> services) : base(services)
{
    // Validation rules
    RuleManager.AddValidation(
        t => !new[] { "Home", "Work", "Other" }.Contains(t.AddressType)
            ? "Address type must be Home, Work, or Other"
            : string.Empty,
        t => t.AddressType);
}
```
<sup><a href='/src/Design/Design.Domain/Entities/Address.cs#L30-L59' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-validation-attributes-and-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

RuleManager also provides `AddAction`, `AddActionAsync`, `AddValidationAsync`, and class-based rules. **`AddValidation`/`AddValidationAsync` accept exactly one trigger property** — for multiple triggers, validate a computed property or use a class-based rule. See `references/validation.md` for details.

Check validation state with `IsValid`, `IsSelfValid`, and `PropertyMessages`.

### Rules Do NOT Fire During Factory Methods

**Rules (including AddAction computed properties) do NOT fire during `[Create]`, `[Fetch]`, `[Insert]`, `[Update]` or `[Delete]`.** Every factory operation runs paused: `FactoryStart` pauses the object and `FactoryComplete` resumes it. `ResumeAllActions()` does NOT run rules — it only recalculates cached validity. `PropertyChanged` does NOT fire for changes made while paused.

Inside the operation, assign properties directly. The assignment is a clean baseline load: nothing is marked modified. Do not use `LoadValue`, `PauseAllActions` or `MarkUnmodified` there.

**`RunRules` works while paused** — it has no `IsPaused` guard. Call `await RunRules(RunRulesFlag.All)` at the end of any factory method that sets properties with dependent `AddAction` rules:

<!-- snippet: skill-create-run-rules -->
<a id='snippet-skill-create-run-rules'></a>
```cs
/// <summary>
/// RIGHT WAY: Call RunRules at end of factory method.
/// RunRules works even while paused — no IsPaused guard.
/// </summary>
[Create]
public async Task CreateWithRunRules()
{
    Quantity = 10;
    Price = 5.00m;
    await RunRules(RunRulesFlag.All);  // Forces all rules to execute
    // Total is now 50.00
}
```
<sup><a href='/src/Design/Design.Domain/CommonGotchas.cs#L97-L110' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-create-run-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Without this call, computed properties remain at their default values when the entity reaches the client. See `references/rules-lifecycle.md` for the complete execution lifecycle, `RunRulesFlag` enum reference, and the factory method timeline.

### Child Property Triggers — Parent Reacts to Child Changes

To react to child property changes in an aggregate, use a child property trigger expression with `AddAction`. The `[0]` indexer is a syntactic placeholder — any child whose named property changes triggers the rule:

<!-- snippet: skill-child-property-trigger -->
<a id='snippet-skill-child-property-trigger'></a>
```cs
RuleManager.AddAction(
    t => t.TotalAmount = t.Items?.Sum(i => i.LineTotal) ?? 0,
    t => t.Items![0].LineTotal);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L98-L102' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-property-trigger' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The action body can also push changes to other children — the parent acts as orchestrator, writing children through their business methods, and re-running their rules when a root property they read has changed:

<!-- snippet: skill-parent-orchestrator -->
<a id='snippet-skill-parent-orchestrator'></a>
```cs
// Parent writes children: when the discount changes, every task
// re-applies it through its own business method.
RuleManager.AddAction(
    t =>
    {
        foreach (var task in t.Tasks ?? Enumerable.Empty<IWorkOrderTask>())
        {
            task.ApplyParentDiscount();
        }
    },
    t => t.Discount);

// A child rule that READS root state (IsSchedulable reads IsOnHold) has
// no trigger for it. The root re-runs the children's rules when it changes.
RuleManager.AddActionAsync(
    async t =>
    {
        foreach (var task in t.Tasks ?? Enumerable.Empty<IWorkOrderTask>())
        {
            await task.RunRules();
        }
    },
    t => t.IsOnHold);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L86-L110' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-orchestrator' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Do NOT use `t => t.Items` as the trigger** — that only fires when the `Items` property reference itself is reassigned, not when child items change. `TriggerProperty.IsMatch` uses exact string equality: `"Items" != "Items.LineTotal"`.

See `references/domain-logic-placement.md` → "Pattern 6: Child Property Triggers" for child triggers, orchestrator patterns, `NeatooPropertyChanged`, and `HandleNeatooPropertyChanged` overrides.

## Testing

**Critical:** Never mock Neatoo interfaces or classes. Use real factories and mock only external dependencies. See `references/testing.md` for patterns and `references/pitfalls.md` for common mistakes.

## Reference Documentation

Detailed documentation for each topic area:

- **`references/domain-logic-placement.md`** - Where business logic belongs: computed properties, conditional visibility, cascading state, async rules that call the server, child property triggers, workflow state machines, refactoring smell test
- **`references/base-classes.md`** - Neatoo-to-DDD mapping, when to use each base
- **`references/properties.md`** - Partial properties, change tracking, calculated properties
- **`references/validation.md`** - RuleManager, attributes, async validation
- **`references/rules-lifecycle.md`** - When rules fire and when they don't, RunRulesFlag enum, factory method gap, RunRules works while paused
- **`references/shared-rules.md`** - Shared rules across entities via interface-typed AsyncRuleBase and DI injection
- **`references/entities.md`** - EntityBase lifecycle, persistence, Save routing, the save cascade
- **`references/collections.md`** - EntityListBase, parent-child relationships, deletion tracking
- **`references/lazy-loading.md`** - EntityLazyLoad&lt;T&gt;, IEntityLazyLoadFactory, explicit LoadAsync(), passive Value read, WaitForTasks integration
- **`references/source-generation.md`** - What gets generated, Generated/ folder, [SuppressFactory]
- **`references/trimming.md`** - IL trimming annotations, suppression strategy, consumer project setup
- **`references/blazor.md`** - Blazor-specific binding and component patterns (see also the **MudNeatoo skill** for component binding and anti-patterns)
- **`references/testing.md`** - No mocking Neatoo, integration test patterns
- **`references/pitfalls.md`** - Common mistakes and gotchas

**RemoteFactory topics** (see `/RemoteFactory` skill):
- Factory attributes, service injection, remote execution, authorization

## Troubleshooting

See `references/pitfalls.md` for common issues. Key quick checks: class and properties must be `partial`, class needs `[Factory]` attribute, and `IsSavable` requires `IsValid` plus a reason to persist (`IsModified` **or** `IsNew`).

## IsNew vs IsModified

They answer different questions, and Neatoo keeps them separate:

- **`IsModified`** — "would discarding this lose work?" Drives unsaved-changes guards.
- **`IsNew`** — "does persistence not know this yet?" Drives Insert-vs-Update routing.

A created entity is `IsNew=true, IsModified=false, IsSavable=true`: it needs inserting, but holds no user work, so guards bound to `IsModified` stay quiet on it — including on a freshly re-derived object after a save.

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

A `[Create]` that IS the user's work says so with `MarkModified()` in its body — not to make it savable (it already is), but to say a guard should speak.

**COMMON MISTAKE:** calling `MarkModified()` in a `[Create]` so the entity can be saved. New entities are already savable — `IsSavable` admits `IsNew`. Using it that way re-welds the two meanings and makes guards cry wolf on every new object.

**`IsNew` never aggregates.** It is per-object routing state; lists report `IsNew => false` and a parent's `IsNew` ignores its children. What flows up a graph is modification state — which is why attaching a child to a live parent (list add, or assigning a new child to a property) marks the child modified, so the parent becomes modified and savable.
