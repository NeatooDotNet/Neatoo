[↑ Up](index.md)

# API Reference

Reference for Neatoo's core classes, interfaces, attributes, and source-generated members. It targets .NET developers implementing DDD aggregates with validation and persistence. Member tables are taken from the framework source; every code example is a compiled example from the Design projects, so it is the shape the framework is designed around: a public interface per entity and list, an `internal` concrete, `[Remote] internal` root operations, `internal` child operations, and plain assignment inside factory operations.

## Contents

- [ValidateBase\<T\>](#validatebaset)
- [EntityBase\<T\>](#entitybaset)
- [ValidateListBase\<I\>](#validatelistbasei)
- [EntityListBase\<I\>](#entitylistbasei)
- [Key Interfaces](#key-interfaces) (IValidateBase, IEntityBase, IEntityRoot, IValidateListBase, IEntityListBase, IValidateProperty, IEntityProperty, IPropertyInfo, IValidateMetaProperties, IEntityMetaProperties)
- [Attributes](#attributes)
- [Source Generator Output](#source-generator-output)

---

## ValidateBase\<T\>

Abstract base class providing property management, validation, business rules, and parent-child relationships. Use it for a value object or any object that needs rules but has no persistence lifecycle. A read model needs no base class at all: it is a plain `[Factory]` class with `[Fetch]`.

### Constructor

| Member | Signature | Notes |
|---|---|---|
| Constructor | `public ValidateBase(IValidateBaseServices<T> services)` | `T` is the concrete class (CRTP). The services carry the property factory, rule manager factory and property info list. Objects are created through the generated factory, never with `new`. |

### Property System

Partial properties are the property system. Declare the signature; the generator supplies the backing property object, the accessors, and the registration. (`Getter<P>()`/`Setter<P>()` still exist on the class but are `[Obsolete]`; the generator does not call them.)

<!-- snippet: skill-value-object-interface -->
<a id='snippet-skill-value-object-interface'></a>
```cs
/// <summary>
/// Interface for ValidateBase demo — input models and validation-only scenarios.
/// </summary>
public interface IDemoInputModel : IValidateBase
{
    string? Name { get; set; }
    string? Description { get; set; }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/IBaseClassInterfaces.cs#L12-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-value-object-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-value-object -->
<a id='snippet-skill-value-object'></a>
```cs
/// <summary>
/// Demonstrates: ValidateBase&lt;T&gt; for input models and validation-only scenarios.
///
/// Key points:
/// - Provides validation infrastructure without persistence tracking
/// - IsValid/IsSelfValid track validation state
/// - IsBusy tracks async operations
/// - PauseAllActions()/ResumeAllActions() control event firing
/// - RuleManager provides fluent API for adding rules
/// </summary>
[Factory]
internal partial class DemoInputModel : ValidateBase<DemoInputModel>, IDemoInputModel
{
    public partial string? Name { get; set; }

    public partial string? Description { get; set; }

    public DemoInputModel(IValidateBaseServices<DemoInputModel> services) : base(services)
    {
        // Rules are added in the constructor; they run when a trigger property changes
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);
    }

    // Local: creating an object needs nothing from the server
    [Create]
    public void Create()
    {
    }

    [Create]
    public void Create(string name)
    {
        Name = name;
    }

    // Loaded by the list's [Fetch]: existing data comes through [Fetch],
    // never [Create]. Internal - only server-side code calls it.
    [Fetch]
    internal void Fetch(string name)
    {
        Name = name;
    }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs#L93-L139' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-value-object' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### Property Access

| Member | Signature | Notes |
|---|---|---|
| Indexer | `public IValidateProperty this[string propertyName] { get; }` | The property object behind a partial property: `Value`, `IsValid`, `PropertyMessages`, `IsBusy`, `IsReadOnly`. |
| GetProperty | `public IValidateProperty GetProperty(string propertyName)` | Same as the indexer; throws `PropertyNotFoundException` for an unknown name. |
| TryGetProperty | `public bool TryGetProperty(string propertyName, out IValidateProperty validateProperty)` | Non-throwing lookup. |
| PropertyManager | `protected IValidatePropertyManager<IValidateProperty> PropertyManager { get; set; }` | The registry the generated `InitializePropertyBackingFields` fills. Advanced use only. |

Each property object fires its own `PropertyChanged`, and the object's `PropertyMessages` aggregates every property's messages:

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

### Validation and Rules

| Member | Signature | Notes |
|---|---|---|
| RuleManager | `protected IRuleManager<T> RuleManager { get; }` | Register rules in the constructor: `AddValidation`, `AddValidationAsync`, `AddAction`, `AddActionAsync` (1–3 trigger expressions, or an `Expression<Func<T, object?>>[]`), `AddRule<T>(IRule<T>)` for class-based rules. |
| RunRules | `public virtual Task RunRules(string propertyName, CancellationToken? token = null)` | Re-runs the rules triggered by one property. |
| RunRules | `public virtual Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null)` | `All` clears messages and re-runs every rule. Works while paused (no `IsPaused` guard). A cancelled `RunRules` marks the object invalid with "Validation cancelled" until re-run. |
| ClearAllMessages | `public virtual void ClearAllMessages()` | Clears messages on this object and every descendant. |
| ClearSelfMessages | `public virtual void ClearSelfMessages()` | Clears this object's own property messages only. |
| MarkInvalid | `protected virtual void MarkInvalid(string message)` | Framework use: sets `ObjectInvalid` when a `RunRules` call is cancelled. Not an application validation channel; validation is a rule. |
| ObjectInvalid | `public string? ObjectInvalid { get; protected set; }` | The object-level message, or `null`. A built-in rule reports it as a property message so `IsValid` reflects it. `RunRules(RunRulesFlag.All)` does not clear it ([#96](https://github.com/NeatooDotNet/Neatoo/issues/96)). |

Rules run when a property is set outside a factory operation; there is nothing to call before reading `IsValid` except `WaitForTasks()` when async rules may be in flight. `RunRules` is for forcing a re-run — most often at the end of a `[Create]` or `[Fetch]` that set inputs while the object was paused, so that computed properties populate:

<!-- snippet: skill-run-rules-forces -->
<a id='snippet-skill-run-rules-forces'></a>
```cs
[TestMethod]
public async Task Gotcha1_RulesFireAfterCreate_WithExplicitRunRules()
{
    // Arrange
    var factory = _scope.GetRequiredService<IGotcha1DemoFactory>();

    // Act
    var entity = factory.Create();

    // RunRules works even after factory (IsPaused is now false)
    await entity.RunRules(RunRulesFlag.All);

    // Assert - Now the rule has run
    Assert.AreEqual(50.00m, entity.Total, "Total should be calculated after RunRules");
    Assert.IsTrue(entity.RuleHasRun, "Rule should have run after explicit RunRules call");
}
```
<sup><a href='/src/Design/Design.Tests/GotchaTests/CommonGotchaTests.cs#L52-L69' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-run-rules-forces' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Rules are declared in the constructor through `RuleManager`. Validation attributes become rules on construction; `AddValidation` attaches its message to the single trigger property:

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

### Meta Properties

| Member | Signature | Notes |
|---|---|---|
| IsValid | `public bool IsValid { get; }` | This object and every descendant pass their rules. |
| IsSelfValid | `public bool IsSelfValid { get; }` | This object's own properties pass; children excluded. |
| IsBusy | `public bool IsBusy { get; }` | An async rule or tracked task is running on this object or a descendant. |
| PropertyMessages | `public IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }` | Every message in the graph; each carries its `Property` and `Message`. |

Meta properties raise `PropertyChanged` when they flip, including flips caused by a descendant:

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

### Parent-Child Relationships

| Member | Signature | Notes |
|---|---|---|
| Parent | `public IValidateBase? Parent { get; protected set; }` | Set by the property system when the object is assigned to a parent's partial property or added to a child list. Cast to the parent's **interface** when reading ambient state: `((IOrder)Parent!).Status`. |
| SetParent | `protected virtual void SetParent(IValidateBase? parent)` | Framework hook; application code does not call it. |
| AddChildTask | `public virtual void AddChildTask(Task task)` | Propagates a task up the graph so the root's `WaitForTasks()` awaits it. |

A child's `Parent` is the entity it was added to; a root has no parent, and an entity's `Root` walks the chain:

<!-- snippet: skill-parent-and-root -->
<a id='snippet-skill-parent-and-root'></a>
```cs
[TestMethod]
public void AddItem_SetsParentAndRoot()
{
    var order = _orderFactory.Create();
    var item = _itemFactory.Create("Widget", 5, 10.00m);
    Assert.IsNull(item.Parent, "Not attached yet");

    order.Items!.Add(item);

    // Parent is the owning entity (the list is transparent); Root is the aggregate root
    Assert.AreSame<object>(order, item.Parent!);
    Assert.AreSame<object>(order, item.Root!);

    // The root itself has neither
    Assert.IsNull(order.Parent);
    Assert.IsNull(order.Root);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/OrderAggregateTests.cs#L75-L93' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-and-root' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Async Task Management

| Member | Signature | Notes |
|---|---|---|
| WaitForTasks | `public virtual Task WaitForTasks()` | Awaits every running rule and child task in the graph. Call it before reading `IsValid` or saving when async rules may be in flight. |
| WaitForTasks | `public virtual Task WaitForTasks(CancellationToken token)` | Cancelling the token stops the wait only; it does not cancel the rules. |

While an async rule runs, `IsBusy` is true and `IsSavable` is false:

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

### Pause/Resume

| Member | Signature | Notes |
|---|---|---|
| IsPaused | `public bool IsPaused { get; protected set; }` | True inside a factory operation and inside a `PauseAllActions()` block. |
| PauseAllActions | `public virtual IDisposable PauseAllActions()` | While paused, setters run no rules, raise no `PropertyChanged`, and (on an entity) mark nothing modified. Disposing resumes. |
| ResumeAllActions | `public virtual void ResumeAllActions()` | Clears `IsPaused` and recalculates cached validity. Runs no rules and replays no events. |

Every factory operation is already paused by the framework; never pause inside one. Whether application code should pause a live object at all is not settled. If it does: rules do not run on resume, and a paused assignment on an entity is a baseline load, not an edit, so it will not be saved.

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

### Events

| Member | Signature | Notes |
|---|---|---|
| PropertyChanged | `public event PropertyChangedEventHandler? PropertyChanged` | Standard `INotifyPropertyChanged`: own properties and meta flags. Blazor does not subscribe on its own; the page does. |
| NeatooPropertyChanged | `public event NeatooPropertyChanged? NeatooPropertyChanged` | Async (`Task NeatooPropertyChanged(NeatooPropertyChangedEventArgs)`); carries `PropertyName`, `Source`, a dotted `FullPropertyName` for descendant changes, and `Reason` (`UserEdit` or `Load`). |

### Factory Lifecycle Hooks

| Member | Signature | Notes |
|---|---|---|
| FactoryStart | `public virtual void FactoryStart(FactoryOperation factoryOperation)` | Called by the generated factory before your `[Create]`/`[Fetch]`/`[Insert]`/`[Update]`/`[Delete]` body; pauses the object. |
| FactoryComplete | `public virtual void FactoryComplete(FactoryOperation factoryOperation)` | Called after the body; resumes the object. `EntityBase` adds the state marking (below). Fires only on the factory target — never cascades through the graph. |
| OnDeserializing / OnDeserialized | `public void OnDeserializing()` / `public virtual void OnDeserialized()` | Pause and resume around JSON deserialization. |

Application code does not call these; the generated factory does.

### Services

| Member | Signature | Notes |
|---|---|---|
| Services | `protected IValidateBaseServices<T> Services { get; }` | The injected services object. |
| Logger | `protected ILogger<T> Logger { get; }` | From the services. |
| GetRuleId | `protected virtual uint GetRuleId(string sourceExpression)` | Overridden by the generator with compile-time hashes (see [Rule ID Generation](#rule-id-generation)). |

---

## EntityBase\<T\>

Extends `ValidateBase<T>` with persistence state, modification tracking, and the aggregate root's `Save()`.

### Constructor

| Member | Signature | Notes |
|---|---|---|
| Constructor | `public EntityBase(IEntityBaseServices<T> services)` | Entity services add the save factory (`IFactorySave<T>`) the generated factory supplies. |
| Factory | `public IFactorySave<T>? Factory { get; protected set; }` | The generated save factory `Save()` routes through; present only when the entity's `[Insert]`/`[Update]`/`[Delete]` take no non-service parameters. |

### Persistence State

| Member | Signature | Notes |
|---|---|---|
| IsNew | `public virtual bool IsNew { get; protected set; }` | Set by `FactoryComplete(Create)`; cleared by `FactoryComplete(Insert)`. Routing state for `Save()` (Insert vs Update). Never part of `IsModified`. |
| IsDeleted | `public virtual bool IsDeleted { get; protected set; }` | Set by `Delete()`; routes `Save()` to `[Delete]` unless the entity is also new. |

A created entity needs inserting but holds no user work:

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

### Modification Tracking

| Member | Signature | Notes |
|---|---|---|
| IsModified | `public virtual bool IsModified { get; }` | `PropertyManager.IsModified \|\| IsDeleted \|\| IsSelfModified` — this object, its children, or a deletion. `IsNew` is deliberately not a term ([why](../guides/change-tracking.md#why-isnew-is-not-part-of-ismodified)). |
| IsSelfModified | `public virtual bool IsSelfModified { get; protected set; }` | Own properties changed, or deleted, or `IsMarkedModified`. Children excluded. |
| IsMarkedModified | `public virtual bool IsMarkedModified { get; protected set; }` | Set by `MarkModified()`. |
| ModifiedProperties | `public virtual IEnumerable<string> ModifiedProperties { get; }` | Names of own properties whose values changed since the last factory operation. |

A fetched entity is a clean baseline; the first edit marks the property and the entity:

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

### Savability

| Member | Signature | Notes |
|---|---|---|
| IsSavable | `public virtual bool IsSavable { get; }` | `(IsModified \|\| IsNew) && IsValid && !IsBusy`. Exposed through `IEntityRoot` only. |

The formula says nothing about position in an aggregate — a modified child *concrete* reports `true`. That is why `IsSavable` and `Save()` live on `IEntityRoot` and not on `IEntityBase`: a child interface never shows them, so the mistake is a compile error.

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

### Aggregate Root

| Member | Signature | Notes |
|---|---|---|
| Root | `public IValidateBase? Root { get; }` | `Parent == null ? null : ((Parent as IEntityBase)?.Root ?? Parent)` — if the parent has a root, that; otherwise the parent is the root. `null` on the root itself and on a standalone object. |
| ContainingList | `protected IEntityListBase? ContainingList { get; set; }` | The list this entity was added to; `Delete()` routes through it. Stays set after removal until the list's `FactoryComplete(Update)`. |

### Save Operations

| Member | Signature | Notes |
|---|---|---|
| Save | `public virtual Task<IEntityBase> Save()` | Throws `SaveOperationException` when `!IsSavable` (reasons: `IsBusy`, `IsInvalid`, `NotModified`, no factory). Routes through the generated save factory: `IsDeleted` first (a deleted **new** entity is discarded without a call), then `IsNew` → `[Insert]`, else `[Update]`. Returns the saved instance — keep that one. |
| Save | `public virtual Task<IEntityBase> Save(CancellationToken token)` | Awaits `WaitForTasks(token)` first, then saves. |

The generated factory does not check `IsSavable` or wait for rules; the entity's `Save()` does. The UI binds the Save button to `IsSavable`, so reaching the exception is a programming error, not a validation channel. A root with `[Insert]`, `[Update]` and `[Delete]` that take no non-service parameters gets `IFactorySave<T>` and therefore `Save()`:

<!-- snippet: skill-entity-crud-interface -->
<a id='snippet-skill-entity-crud-interface'></a>
```cs
/// <summary>
/// Root interface for EntityBase demo — persistent domain entities.
/// </summary>
public interface IDemoEntity : IEntityRoot
{
    string? Name { get; set; }
    int Value { get; set; }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/IBaseClassInterfaces.cs#L23-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-crud-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-entity-crud -->
<a id='snippet-skill-entity-crud'></a>
```cs
/// <summary>
/// Demonstrates: EntityBase&lt;T&gt; for persistent domain entities.
///
/// Key points:
/// - Inherits all ValidateBase capabilities (validation, rules, busy tracking)
/// - Adds IsNew/IsModified/IsDeleted for persistence state
/// - IsSavable = (IsModified || IsNew) &amp;&amp; IsValid &amp;&amp; !IsBusy
/// - Save() routes to Insert/Update/Delete based on state
/// - Child entities cannot save independently: their interface has no Save()
/// </summary>
[Factory]
internal partial class DemoEntity : EntityBase<DemoEntity>, IDemoEntity
{
    public partial string? Name { get; set; }

    public partial int Value { get; set; }

    public DemoEntity(IEntityBaseServices<DemoEntity> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Name) ? "Name is required" : string.Empty,
            t => t.Name);

        RuleManager.AddValidation(
            t => t.Value < 0 ? "Value must be non-negative" : string.Empty,
            t => t.Value);
    }

    [Create]
    public void Create()
    {
        // FactoryComplete(Create) calls MarkNew(): IsNew=true, IsModified=false.
        // Nothing was set, so there is no user work - still savable, because
        // IsSavable admits IsNew.
    }

    // [Remote]: the client fetches this root, so the call crosses to the
    // server, where the repository resolves. The object is paused for the
    // body, so plain assignment is a clean baseline load.
    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IDemoRepository repository)
    {
        var data = repository.GetById(id);
        Name = data.Name;
        Value = data.Value;
        // After Fetch: IsNew=false, IsModified=false
    }

    // Save() routes here when IsNew. FactoryComplete(Insert) then calls
    // MarkUnmodified() and MarkOld().
    [Remote]
    [Insert]
    internal void Insert([Service] IDemoRepository repository)
    {
        repository.Insert(Name!, Value);
    }

    // Save() routes here when !IsDeleted && !IsNew. Routing never consults
    // IsModified; entity.Save()'s IsSavable gate stops unmodified saves.
    [Remote]
    [Update]
    internal void Update([Service] IDemoRepository repository)
    {
        repository.Update(Name!, Value);
    }

    // Save() routes here when IsDeleted (checked first - IsDeleted wins over IsNew)
    [Remote]
    [Delete]
    internal void Delete([Service] IDemoRepository repository)
    {
        repository.Delete(Name!);
    }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs#L226-L302' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-crud' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-save-routes-to-insert -->
<a id='snippet-skill-save-routes-to-insert'></a>
```cs
[TestMethod]
public async Task Save_WhenNew_RoutesToInsert_AndMarksOld()
{
    // Arrange
    var entity = _factory.Create();
    entity.Name = "Inserted";
    entity.Amount = 42m;

    // Act
    entity = (ISaveDemo)await entity.Save();

    // Assert - routed to Insert, and the generated Id landed on the entity
    Assert.AreEqual(1, _repository.InsertedIds.Count, "Should route to Insert");
    Assert.AreEqual(0, _repository.UpdatedIds.Count);
    Assert.AreEqual(_repository.InsertedIds[0], entity.Id,
        "The generated Id must land on the entity");

    // Assert - FactoryComplete(Insert) marked it unmodified and old
    Assert.IsFalse(entity.IsNew, "Inserted entity is no longer new");
    Assert.IsFalse(entity.IsModified);
    Assert.IsFalse(entity.IsSavable, "Nothing left to save");
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L105-L128' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-save-routes-to-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Delete Operations

| Member | Signature | Notes |
|---|---|---|
| Delete | `public void Delete()` | Marks for deletion. When the entity is in a list, it routes through the list's remove path so the collection and `DeletedList` stay consistent. Nothing reaches persistence until the root is saved. |
| UnDelete | `public void UnDelete()` | Reverses the mark. |

<!-- snippet: skill-undelete -->
<a id='snippet-skill-undelete'></a>
```cs
[TestMethod]
public async Task UnDelete_ReversesDeleteBeforeSave()
{
    var entity = await _factory.Fetch(1);

    entity.Delete();
    Assert.IsTrue(entity.IsDeleted);
    Assert.IsTrue(entity.IsModified, "Deleting is a modification");

    entity.UnDelete();

    Assert.IsFalse(entity.IsDeleted);
    Assert.IsFalse(entity.IsModified, "Back to the fetched baseline");
    Assert.IsFalse(entity.IsSavable, "Nothing left to save");
}
```
<sup><a href='/src/Design/Design.Tests/BaseClassTests/EntityBaseTests.cs#L143-L159' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-undelete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-delete-routes-to-delete -->
<a id='snippet-skill-delete-routes-to-delete'></a>
```cs
[TestMethod]
public async Task Save_WhenDeleted_RoutesToDelete()
{
    // Arrange - a persisted entity marked for deletion
    var entity = await _factory.Fetch(9);
    entity.Delete();

    Assert.IsTrue(entity.IsDeleted);
    Assert.IsTrue(entity.IsModified, "IsDeleted remains a term in IsModified");
    Assert.IsTrue(entity.IsSavable);

    // Act
    await entity.Save();

    // Assert - routed to Delete, not Update
    CollectionAssert.AreEqual(new[] { 9 }, _repository.DeletedIds, "Should route to Delete");
    Assert.AreEqual(0, _repository.UpdatedIds.Count);
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L146-L165' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-delete-routes-to-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### State Management Methods

| Member | Signature | Notes |
|---|---|---|
| MarkNew / MarkOld | `protected virtual void MarkNew()` / `protected virtual void MarkOld()` | Called by `FactoryComplete(Create)` and `FactoryComplete(Insert/Update)`. |
| MarkModified | `protected virtual void MarkModified()` | Sets `IsMarkedModified`. The one doctrinal use is a `[Create]` whose result *is* the user's work, so an unsaved-changes guard bound to `IsModified` speaks. Not needed to make a new object savable. |
| MarkUnmodified | `protected virtual void MarkUnmodified()` | Clears this object's own modification state. Called by `FactoryComplete(Insert/Update)`; children are cleared by their own factory completion, never by the parent's. |
| MarkDeleted | `protected virtual void MarkDeleted()` | Called by `Delete()`. |

These are protected for a reason: the generated factory drives entity state. Application and test code never calls `FactoryComplete` or exposes these through public wrappers; a baseline is manufactured by a real `[Fetch]` or `Save()` against a mock repository.

### Property Access

| Member | Signature | Notes |
|---|---|---|
| Indexer | `new public IEntityProperty this[string propertyName] { get; }` | Entity properties add `IsModified`/`IsSelfModified` and `MarkSelfUnmodified()` to the validate property. |
| PropertyManager | `protected new IEntityPropertyManager PropertyManager { get; }` | Entity property registry. |

---

## ValidateListBase\<I\>

Base class for collections of `ValidateBase` objects (value objects). Inherits `ObservableCollection<I>` where `I : IValidateBase`; aggregates validation state and coordinates tasks across items.

| Member | Signature | Notes |
|---|---|---|
| Constructor | `public ValidateListBase()` | Lists are created through their generated factory from the parent's `[Create]`/`[Fetch]`, not with `new`. |
| Parent | `public IValidateBase? Parent { get; protected set; }` | Set when the list is assigned to a parent's property. Items added to the list get the **list's parent** as their `Parent`, not the list. |
| IsValid / IsSelfValid / IsBusy | `public bool IsValid { get; }` / `public bool IsSelfValid { get; }` / `public bool IsBusy { get; }` | Aggregated from the items with incremental caching. `IsSelfValid` is always `true` (a list has no rules of its own). |
| PropertyMessages | `public IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }` | Every item's messages. |
| IsPaused | `public bool IsPaused { get; protected set; }` | Paused inside the list's own factory operation and during deserialization. |
| RunRules | `public Task RunRules(string propertyName, CancellationToken? token = default)` / `public Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = default)` | Runs the rules of every item. |
| ClearAllMessages / ClearSelfMessages | `public void ClearAllMessages()` / `public void ClearSelfMessages()` | On every item. |
| WaitForTasks | `public Task WaitForTasks()` / `public Task WaitForTasks(CancellationToken token)` | Awaits every item. |
| ResumeAllActions | `public virtual void ResumeAllActions()` | Recalculates cached meta state; runs no rules. |
| FactoryStart / FactoryComplete | `public virtual void FactoryStart(FactoryOperation)` / `public virtual void FactoryComplete(FactoryOperation)` | The list's own lifecycle hooks, fired by the list factory. |
| HandleNeatooPropertyChanged | `protected virtual Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)` | Override for cross-sibling consistency (re-run siblings' rules when one item changes). Only a list can override this. |
| Events | `NeatooPropertyChanged`, plus `PropertyChanged` and `CollectionChanged` from `ObservableCollection<I>` | |

A value-object list is interface-first like everything else:

<!-- snippet: skill-validate-list-interface -->
<a id='snippet-skill-validate-list-interface'></a>
```cs
/// <summary>
/// List interface for ValidateListBase demo — parameterized on child INTERFACE.
/// </summary>
public interface IDemoInputModelList : IValidateListBase<IDemoInputModel> { }
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/IBaseClassInterfaces.cs#L53-L58' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-validate-list-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-validate-list -->
<a id='snippet-skill-validate-list'></a>
```cs
/// <summary>
/// Demonstrates: ValidateListBase&lt;I&gt; for collections of ValidateBase items.
///
/// Key points:
/// - Extends ObservableCollection&lt;I&gt; with validation aggregation
/// - IsValid = all children are valid
/// - IsBusy = any child is busy
/// - Parent-child relationships managed automatically
/// </summary>
[Factory]
internal partial class DemoInputModelList : ValidateListBase<IDemoInputModel>, IDemoInputModelList
{
    // ValidateListBase has no required constructor - uses default.

    [Create]
    public void Create()
    {
        // Start with empty list
    }

    [Remote]
    [Fetch]
    internal void Fetch([Service] IDemoRepository repository, [Service] IDemoInputModelFactory inputModelFactory)
    {
        // The list is paused by its own factory operation (FactoryStart),
        // like any factory target. Each item is loaded by its own [Fetch].
        foreach (var name in repository.GetAllNames())
        {
            Add(inputModelFactory.Fetch(name));
        }
    }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs#L333-L366' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-validate-list' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Validity aggregates from the items:

<!-- snippet: skill-list-validity-aggregates -->
<a id='snippet-skill-list-validity-aggregates'></a>
```cs
[TestMethod]
public async Task Add_InvalidItem_ListBecomesInvalid()
{
    // Arrange
    var list = _listFactory.Create();
    var validItem = _itemFactory.Create("Valid");
    list.Add(validItem);
    Assert.IsTrue(list.IsValid);

    // Act - Add item then make it invalid
    var itemToInvalidate = _itemFactory.Create("Initially Valid");
    list.Add(itemToInvalidate);
    itemToInvalidate.Name = ""; // Make invalid
    await itemToInvalidate.WaitForTasks();

    // Assert
    Assert.IsFalse(list.IsValid, "List should be invalid if any child is invalid");
}
```
<sup><a href='/src/Design/Design.Tests/BaseClassTests/ValidateListBaseTests.cs#L60-L79' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-list-validity-aggregates' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Adding and removing are the standard `ObservableCollection<I>` operations (`Add`, `Remove`, `RemoveAt`, `Insert`, `Clear`, indexer). On add the list sets the item's `Parent` and subscribes to its events; on remove it unsubscribes and recalculates:

<!-- snippet: skill-validate-list-remove -->
<a id='snippet-skill-validate-list-remove'></a>
```cs
[TestMethod]
public void Remove_ItemLeavesImmediately()
{
    var list = _listFactory.Create();
    var item = _itemFactory.Create("Test Item");
    list.Add(item);

    // No persistence, so no DeletedList: the item is simply gone
    list.Remove(item);

    Assert.AreEqual(0, list.Count);
}
```
<sup><a href='/src/Design/Design.Tests/BaseClassTests/ValidateListBaseTests.cs#L81-L94' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-validate-list-remove' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

---

## EntityListBase\<I\>

Extends `ValidateListBase<I>` for entity children within an aggregate, `I : IEntityBase`: deleted-item tracking and modification aggregation. A list is never saved on its own; it has no `IsSavable`.

| Member | Signature | Notes |
|---|---|---|
| Constructor | `public EntityListBase()` | Created through the generated list factory. |
| IsModified | `public bool IsModified { get; }` | Any item modified, or `DeletedList` non-empty. |
| IsSelfModified / IsMarkedModified / IsNew / IsDeleted | `public bool ... { get; }` | Always `false`: a list has no properties and no persistence identity of its own. |
| Root | `public IValidateBase? Root { get; }` | `(Parent as IEntityBase)?.Root ?? Parent`. |
| DeletedList | `protected List<I> DeletedList { get; }` | Items removed after being persisted. Drained by the list's own `[Update]`; cleared by the list's `FactoryComplete(Update)`. |
| FactoryComplete | `public override void FactoryComplete(FactoryOperation factoryOperation)` | After the list's own `[Update]`: clears `DeletedList`, clears `ContainingList` on the deleted items, recalculates the modified cache. |

<!-- snippet: skill-entity-list-interface -->
<a id='snippet-skill-entity-list-interface'></a>
```cs
/// <summary>
/// List interface for EntityListBase demo — parameterized on child INTERFACE.
/// </summary>
public interface IDemoEntityList : IEntityListBase<IDemoChild>
{
    int DeletedCount { get; }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/IBaseClassInterfaces.cs#L60-L68' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-list-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-entity-list -->
<a id='snippet-skill-entity-list'></a>
```cs
/// <summary>
/// Demonstrates: EntityListBase&lt;I&gt; for collections of child entities.
///
/// Key points:
/// - Extends ValidateListBase with persistence tracking
/// - IsModified = any child modified OR DeletedList has items
/// - DeletedList tracks removed non-new items for persistence deletion
/// - Adding items: set ContainingList (routes the child's Delete through the list)
/// - Removing non-new items: MarkDeleted(), add to DeletedList
/// - Root property for aggregate boundary enforcement
/// - Parameterized on a CHILD interface (IDemoChild : IEntityBase), never on
///   a root interface
/// </summary>
[Factory]
internal partial class DemoEntityList : EntityListBase<IDemoChild>, IDemoEntityList
{
    // DESIGN DECISION: EntityListBase doesn't define IsSavable or Save().
    // Lists are ALWAYS saved through their parent aggregate root: the root's
    // [Insert]/[Update] hands its row's child collection to the list
    // factory's Save, and the list's own [Update] brings that collection in
    // line (see Aggregates/OrderAggregate/OrderItemList.cs).

    /// <summary>
    /// Test helper: Exposes the count of items in DeletedList.
    /// The DeletedList is protected, but tests need to verify deletion behavior.
    /// </summary>
    public int DeletedCount => DeletedList.Count;

    [Create]
    public void Create()
    {
        // Empty list
    }

    // Child list operations are internal and never [Remote]: the root's
    // [Fetch] calls this on the server.
    [Fetch]
    internal void Fetch(IEnumerable<string> names, [Service] IDemoChildFactory childFactory)
    {
        foreach (var name in names)
        {
            Add(childFactory.Fetch(name));
        }
    }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs#L443-L489' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-list' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Adding and Removing

On a live (unpaused) `Add`, the list:
- rejects an item that belongs to a **different aggregate** (`Root` mismatch); an item that sits in another list of the same aggregate is moved (removed from that list's `DeletedList`, un-deleted);
- rejects a busy item;
- sets `Parent` (to the list's parent) and `ContainingList`;
- **marks the item modified**, new or existing — attaching a child to a live parent is a change to the graph, and it is the only channel by which a new child's arrival reaches the parent (`IsNew` never aggregates upward).

On `Remove`:
- a new item (`IsNew == true`) is discarded — there is nothing to delete;
- a persisted item is marked deleted and goes to `DeletedList`; its `ContainingList` stays set until the list's `FactoryComplete(Update)`.

Adds inside the list's own `[Fetch]` are paused: they set identity (`Parent`, `ContainingList`) but mark nothing modified.

<!-- snippet: skill-add-marks-modified -->
<a id='snippet-skill-add-marks-modified'></a>
```cs
[TestMethod]
public void IsModified_TrueWhenNewItemAdded()
{
    // Arrange
    var order = _orderFactory.Create();

    // Act
    var item = _itemFactory.Create("Widget", 1, 10.00m);
    order.Items!.Add(item);

    // Assert - New order with new items is modified
    Assert.IsTrue(order.Items.IsModified);
    Assert.IsTrue(order.IsModified);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/DeletedListTests.cs#L111-L126' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-add-marks-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-remove-new-item-discarded -->
<a id='snippet-skill-remove-new-item-discarded'></a>
```cs
[TestMethod]
public void RemoveNewItem_NotAddedToDeletedList()
{
    // Arrange
    var order = _orderFactory.Create();
    var item = _itemFactory.Create("Widget", 1, 10.00m);
    order.Items!.Add(item);
    Assert.IsTrue(item.IsNew, "Created item should be new");

    // Act
    order.Items.Remove(item);

    // Assert
    Assert.AreEqual(0, order.Items.Count);
    Assert.AreEqual(0, order.Items.DeletedCount, "New items should not go to DeletedList");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/DeletedListTests.cs#L40-L57' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-remove-new-item-discarded' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-remove-fetched-item -->
<a id='snippet-skill-remove-fetched-item'></a>
```cs
[TestMethod]
public async Task Remove_FetchedItem_AddedToDeletedList()
{
    // Arrange - Fetch the root so its list holds existing (non-new) children
    var parent = await _parentFactory.Fetch();
    var list = parent.Children!;
    var item = list[0];
    Assert.IsFalse(item.IsNew, "A fetched child is not new");

    // Act
    list.Remove(item);

    // Assert
    Assert.AreEqual(2, list.Count);
    Assert.AreEqual(1, list.DeletedCount, "Removed fetched item should be in DeletedList");
}
```
<sup><a href='/src/Design/Design.Tests/BaseClassTests/EntityListBaseTests.cs#L108-L125' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-remove-fetched-item' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-deleted-list-marks-modified -->
<a id='snippet-skill-deleted-list-marks-modified'></a>
```cs
[TestMethod]
public async Task Remove_FetchedItem_MarksItemDeletedAndListModified()
{
    var parent = await _parentFactory.Fetch();
    var list = parent.Children!;
    var item = list[0];
    Assert.IsFalse(list.IsModified, "A fetched list is clean");

    list.Remove(item);

    Assert.IsTrue(item.IsDeleted, "The removed item is marked for deletion");
    Assert.IsTrue(list.IsModified, "A pending deletion makes the list modified");
    Assert.IsTrue(parent.IsModified, "...and the parent with it");
}
```
<sup><a href='/src/Design/Design.Tests/BaseClassTests/EntityListBaseTests.cs#L127-L142' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-deleted-list-marks-modified' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Persisting the List

The root's `[Insert]`/`[Update]` hands its row's child collection to the list factory's `Save`, which runs the list's own `[Update]` inside the list's factory operation: removed items have their rows removed, new and modified items go through per-item factory saves (each item maps itself into its row), and the list's `FactoryComplete(Update)` clears `DeletedList`. Nothing cascades automatically; the cleanup happens because the list is saved through its own factory operation.

<!-- snippet: skill-list-update -->
<a id='snippet-skill-list-update'></a>
```cs
[Update]
internal void Update(ICollection<OrderItemRow> rows,
                     [Service] IOrderItemFactory itemFactory)
{
    foreach (var item in this.Union(DeletedList))
    {
        if (item.IsDeleted)
        {
            // The !IsNew check is defensive: a new item removed from the
            // list is discarded (never enters DeletedList), so deleted
            // items reaching here are expected to be persisted ones.
            if (!item.IsNew)
            {
                rows.Remove(rows.Single(r => r.Id == item.Id));
            }
        }
        else if (item.IsNew)
        {
            var row = new OrderItemRow();
            rows.Add(row);
            itemFactory.Save(item, row);
        }
        else if (item.IsModified)
        {
            itemFactory.Save(item, rows.Single(r => r.Id == item.Id));
        }
    }
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItemList.cs#L102-L131' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-list-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

---

## Key Interfaces

### IValidateBase

`public interface IValidateBase : INeatooObject, INotifyPropertyChanged, INotifyNeatooPropertyChanged, IValidateMetaProperties`

| Member | Signature |
|---|---|
| Parent | `IValidateBase? Parent { get; }` |
| IsPaused | `bool IsPaused { get; }` |
| GetProperty | `IValidateProperty GetProperty(string propertyName)` |
| Indexer | `IValidateProperty this[string propertyName] { get; }` |
| TryGetProperty | `bool TryGetProperty(string propertyName, out IValidateProperty validateProperty)` |
| AddChildTask | `void AddChildTask(Task task)` |

Plus everything on [IValidateMetaProperties](#ivalidatemetaproperties). A user-defined value-object interface extends `IValidateBase`.

### IEntityBase

`public interface IEntityBase : IValidateBase, IEntityMetaProperties, IFactorySaveMeta`

The interface for **child** entities. It carries persistence state, modification tracking and deletion, and does **not** expose `IsSavable` or `Save()` — those belong to `IEntityRoot`.

| Member | Signature |
|---|---|
| Root | `IValidateBase? Root { get; }` |
| ModifiedProperties | `IEnumerable<string> ModifiedProperties { get; }` |
| Delete | `void Delete()` |
| UnDelete | `void UnDelete()` |
| Indexer | `new IEntityProperty this[string propertyName] { get; }` |

`IFactorySaveMeta` (RemoteFactory) contributes `IsNew` and `IsDeleted`, which the generated save factory routes on.

### IEntityRoot

`public interface IEntityRoot : IEntityBase`

| Member | Signature |
|---|---|
| IsSavable | `bool IsSavable { get; }` |
| Save | `Task<IEntityBase> Save()` |
| Save | `Task<IEntityBase> Save(CancellationToken token)` |

`IsSavable` on `EntityBase` is `(IsModified || IsNew) && IsValid && !IsBusy` — it knows nothing about aggregate position, so a modified child concrete reports `true` even though children are persisted by their root. Developers used it in save-cascade logic and reached for `Save()` on a child, which the framework does not support. The fix is to keep both off the child interface so the mistake is a compile error. `EntityBase<T>` implements both `IEntityBase` and `IEntityRoot`; this does not matter because the concrete is `internal` and consumers see only the interface. The user signals root vs child by choosing which framework interface to extend:

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

<!-- snippet: skill-child-interface-no-save -->
<a id='snippet-skill-child-interface-no-save'></a>
```cs
[TestMethod]
public void ChildInterface_DoesNotExposeIsSavable()
{
    // Arrange — IOrderItem extends IEntityBase, not IEntityRoot
    var order = _orderFactory.Create();
    var item = _itemFactory.Create("Widget", 5, 10.00m);
    order.Items!.Add(item);

    // Act — Cast to IEntityBase (which IOrderItem extends)
    // Intentionally using interface type to demonstrate the pattern
#pragma warning disable CA1859
    IEntityBase entityBase = item;
#pragma warning restore CA1859

    // Assert — IEntityBase does NOT have IsSavable
    // This is verified by the fact that the following would NOT compile:
    //   entityBase.IsSavable  // CS1061: IEntityBase does not contain IsSavable
    //   entityBase.Save()     // CS1061: IEntityBase does not contain Save
    Assert.AreSame<object>(order, entityBase.Root!, "Child entity belongs to the aggregate");
    Assert.IsTrue(entityBase.IsModified, "Child entity should be modified");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/EntityRootInterfaceTests.cs#L48-L70' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-interface-no-save' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IValidateListBase\<I\> and IEntityListBase\<I\>

| Interface | Declaration | Adds |
|---|---|---|
| IValidateListBase\<I\> | `public interface IValidateListBase<I> : IList<I>, INeatooObject, INotifyCollectionChanged, INotifyPropertyChanged, INotifyNeatooPropertyChanged, IValidateMetaProperties where I : IValidateBase` | `IValidateBase? Parent { get; }` |
| IEntityListBase\<I\> | `public interface IEntityListBase<I> : IValidateListBase<I>, IEntityMetaProperties where I : IEntityBase` | `new void RemoveAt(int index)` (marks a persisted item deleted) |

User-defined list interfaces are parameterized on the **child interface**: `public interface IOrderItemList : IEntityListBase<IOrderItem> { }`. Neither list interface has `IsSavable`.

### IValidateProperty

`public interface IValidateProperty : INotifyPropertyChanged, INotifyNeatooPropertyChanged` — the object behind every partial property. `IValidateProperty<T>` adds a typed `new T? Value { get; set; }`.

| Member | Signature | Notes |
|---|---|---|
| Name | `string Name { get; }` | |
| Type | `Type Type { get; }` | |
| Value | `object? Value { get; set; }` | The setter runs the rules (what the generated property setter calls) but cannot be awaited. |
| StringValue | `string? StringValue { get; }` | `Value?.ToString()`. |
| SetValue | `Task SetValue(object? newValue)` | The awaitable set; throws `PropertyException` (internal `PropertyReadOnlyException`) when `IsReadOnly`. |
| SetPrivateValue | `Task SetPrivateValue(object? newValue, bool quietly = false)` | What a `private set` partial property's setter calls; bypasses `IsReadOnly`; rules and `PropertyChanged` run normally. |
| LoadValue | `void LoadValue(object? value)` | Sets without rules or modification tracking regardless of pause state (`ChangeReason.Load`). Framework use (deserialization, `EntityLazyLoad`); not needed inside a factory operation, where plain assignment is already a baseline load. |
| IsReadOnly | `bool IsReadOnly { get; }` | `true` for a get-only or `private set` property, or after `MarkReadOnly()`. |
| MarkReadOnly | `void MarkReadOnly()` | Permanent, per instance. Called in `[Fetch]` from a server-side permission service for field-level authorization. |
| IsValid / IsSelfValid | `bool IsValid { get; }` / `bool IsSelfValid { get; }` | `IsValid` looks through to a child object held by the property. |
| PropertyMessages | `IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }` | |
| IsBusy | `bool IsBusy { get; }` | An async rule is running for this property. |
| Task | `Task Task { get; }` | The running rule task; `GetAwaiter()` makes the property awaitable. |
| WaitForTasks | `Task WaitForTasks()` | |
| RunRules | `Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null)` | Forwards to a child object held by the property; a no-op for a scalar. |
| AddMarkedBusy / RemoveMarkedBusy | `void AddMarkedBusy(long id)` / `void RemoveMarkedBusy(long id)` | Framework use by the rule manager. |

`SetValue` is the awaitable way to set a property; a component that must wait for the rules calls it:

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

`LoadValue` outside a factory operation sets without tracking:

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

`MarkReadOnly` decides per instance, during `[Fetch]`, from a server-side service — never from a parameter the client passes:

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

### IEntityProperty

`public interface IEntityProperty : IValidateProperty`; `IEntityProperty<T> : IEntityProperty, IValidateProperty<T>`. The property type of an `EntityBase` entity and the type MudNeatoo components bind.

| Member | Signature | Notes |
|---|---|---|
| IsModified | `bool IsModified { get; }` | This property changed, or the child object it holds is modified. |
| IsSelfModified | `bool IsSelfModified { get; }` | This property's own value changed. |
| MarkSelfUnmodified | `void MarkSelfUnmodified()` | Called by the entity's `MarkUnmodified()`. |
| IsPaused | `bool IsPaused { get; set; }` | Mirrors the owner's pause. |
| DisplayName | `string DisplayName { get; }` | From `[DisplayName]`, else the property name. |
| ApplyPropertyInfo | `void ApplyPropertyInfo(IPropertyInfo propertyInfo)` | Called after deserialization to re-read attributes. |

### IPropertyInfo

Metadata about a declared property, read once per type.

| Member | Signature |
|---|---|
| PropertyInfo | `PropertyInfo PropertyInfo { get; }` |
| Name | `string Name { get; }` |
| Type | `Type Type { get; }` |
| Key | `string Key { get; }` |
| IsPrivateSetter | `bool IsPrivateSetter { get; }` — `!CanWrite \|\| SetMethod?.IsPrivate == true`; the source of `IsReadOnly` |
| GetCustomAttribute | `T? GetCustomAttribute<T>() where T : Attribute` |
| GetCustomAttributes | `IEnumerable<Attribute> GetCustomAttributes()` |

### IValidateMetaProperties

| Member | Signature |
|---|---|
| IsValid / IsSelfValid / IsBusy | `bool IsValid { get; }` / `bool IsSelfValid { get; }` / `bool IsBusy { get; }` |
| PropertyMessages | `IReadOnlyCollection<IPropertyMessage> PropertyMessages { get; }` |
| WaitForTasks | `Task WaitForTasks()` / `Task WaitForTasks(CancellationToken token)` |
| RunRules | `Task RunRules(string propertyName, CancellationToken? token = null)` / `Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null)` |
| ClearAllMessages / ClearSelfMessages | `void ClearAllMessages()` / `void ClearSelfMessages()` |

### IEntityMetaProperties

`public interface IEntityMetaProperties : IFactorySaveMeta` — `IsNew` and `IsDeleted` come from `IFactorySaveMeta`.

| Member | Signature |
|---|---|
| IsModified / IsSelfModified / IsMarkedModified | `bool IsModified { get; }` / `bool IsSelfModified { get; }` / `bool IsMarkedModified { get; }` |

`IsSavable` is not here; it lives on `IEntityRoot` only, so child entities and entity lists never expose a property that invites a save they do not support. Code that casts a child to `IEntityRoot` to read it defeats the split — bind to the root's `IsSavable`.

### Supporting Types

| Type | Members |
|---|---|
| `IPropertyMessage` | `IValidateProperty Property { get; set; }`, `string Message { get; set; }` |
| `NeatooPropertyChangedEventArgs` | `PropertyName`, `Property`, `Source`, `OriginalEventArgs`, `InnerEventArgs`, `FullPropertyName` (dotted path), `Reason` |
| `ChangeReason` | `UserEdit`, `Load` |
| `RunRulesFlag` | `None`, `NoMessages`, `Messages`, `NotExecuted`, `Executed`, `Self`, `All` (see [rules-lifecycle](../../skills/neatoo/references/rules-lifecycle.md) for the semantics of each) |
| `FactoryOperation` (RemoteFactory) | `None`, `Execute`, `Create`, `Fetch`, `Insert`, `Update`, `Delete` |
| `SaveOperationException` | `Reason`: `SaveFailureReason.IsBusy`, `IsInvalid`, `NotModified`, … |

---

## Attributes

### RemoteFactory Attributes

Factory operations are declared with RemoteFactory attributes. Every one of them is a method the **generated factory** calls between `FactoryStart` and `FactoryComplete`, so the object is paused for the body: plain assignment is a clean baseline load.

| Attribute | Target | Meaning |
|---|---|---|
| `[Factory]` | class or interface | Generate `I{Name}Factory` for this type. Applies to any class: Neatoo entities and lists, plain read-model classes (`[Fetch]` only, no Neatoo base), and static command classes. |
| `[SuppressFactory]` | class or interface | No factory. For a test-only class derived from a Neatoo base and constructed directly with its services object. Neatoo.BaseGenerator keys on `[Factory]`, so a `[SuppressFactory]` class also gets no generated partial properties. |
| `[Create]` | method or constructor | `new`: produce an object that does not exist yet. Local; `[Service]` parameters resolve on the calling tier. |
| `[Fetch]` | method or constructor | Load an object that exists; its parameters identify it (a key). A `bool`/`Task<bool>` return of `false` makes the factory return `null`. |
| `[Insert]` / `[Update]` / `[Delete]` | method | Persistence operations reached through `Save`. With no non-service parameters they produce `IFactorySave<T>` and `entity.Save()`; with a row parameter (a child) they produce `Save(target, row)` for the list's `[Update]` to call. |
| `[Execute]` | static method | A command. `[Remote, Execute] private static Task<T> _Name(...)` when it needs the server; a bare `[Execute]` runs on the calling tier (RemoteFactory 1.9+). The generated delegate is what a rule injects to reach the server. |
| `[Remote]` | method | A **client entry point**: the client's call crosses to the server, where the `[Service]` dependencies live. Requires `internal` (NF0105 rejects it on a public method). Goes on the root's `Fetch`/`Insert`/`Update`/`Delete`, never on child operations, which are `internal` and reached only from the parent's or list's operation. |
| `[Service]` | parameter | Resolved from the DI container of the tier the operation runs on. Never passed on to another method as an ordinary argument. |
| `[AuthorizeFactory]` / `[AuthorizeFactory<T>]` | method / class | Factory-operation authorization; see the RemoteFactory documentation. |

#### [Factory]

A read model is the simplest `[Factory]` class: no Neatoo base, `[Fetch]` only.

<!-- snippet: skill-read-model -->
<a id='snippet-skill-read-model'></a>
```cs
/// <summary>
/// One row of the employee directory.
/// </summary>
public sealed record EmployeeSummary(int Id, string FullName, string Email, string Department, bool IsActive);

/// <summary>
/// Read model for the employee directory screen.
/// </summary>
public interface IEmployeeDirectory
{
    IReadOnlyList<EmployeeSummary> Employees { get; }

    /// <summary>
    /// Server-computed: how many of the returned employees are active.
    /// </summary>
    int ActiveCount { get; }
}

/// <summary>
/// Demonstrates: a read model as a plain [Factory] class with [Fetch] only.
/// </summary>
[Factory]
internal partial class EmployeeDirectory : IEmployeeDirectory
{
    public IReadOnlyList<EmployeeSummary> Employees { get; internal set; } = [];

    public int ActiveCount { get; internal set; }

    // =========================================================================
    // [Fetch] - All Employees
    // =========================================================================
    [Remote]
    [Fetch]
    internal void Fetch([Service] IEmployeeDirectoryRepository repository)
    {
        Load(repository.GetAll());
    }

    // =========================================================================
    // [Fetch] with Criteria - Filtered Results
    // =========================================================================
    // A criteria class keeps the signature short. It is a request parameter,
    // so on a trimmed client it needs a preserve entry (see the RemoteFactory
    // skill's trimming reference).
    // =========================================================================
    [Remote]
    [Fetch]
    internal void Fetch(EmployeeSearchCriteria criteria, [Service] IEmployeeDirectoryRepository repository)
    {
        Load(repository.Search(criteria.SearchTerm, criteria.Department, criteria.ActiveOnly));
    }

    private void Load(IEnumerable<EmployeeSummary> rows)
    {
        Employees = rows.ToList();
        ActiveCount = Employees.Count(e => e.IsActive);
    }

    // =========================================================================
    // No [Create], [Insert], [Update] or [Delete]
    // =========================================================================
    // A read model is not edited and not saved. To change an employee, fetch
    // the aggregate, change it, save it, then fetch the read model again:
    //
    //   var directory = await directoryFactory.Fetch(criteria);
    //   var employee = await employeeFactory.Fetch(selectedId);
    //   employee.Department = "Engineering";
    //   await employee.Save();
    //   directory = await directoryFactory.Fetch(criteria);
    // =========================================================================
}
```
<sup><a href='/src/Design/Design.Domain/ReadModels/EmployeeDirectory.cs#L41-L113' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-read-model' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### [Create]

A root's `[Create]` builds its child list through the injected list factory and sets defaults. It is local — no `[Remote]`.

<!-- snippet: skill-root-create -->
<a id='snippet-skill-root-create'></a>
```cs
[Create]
public void Create([Service] IOrderItemListFactory itemsFactory)
{
    Items = itemsFactory.Create();
    OrderDate = DateTime.Today;
    Status = "Draft";
    OrderNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmss}";
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L105-L114' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-root-create' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### [Fetch]

A root's `[Fetch]` is `[Remote] internal`, takes the key and a `[Service]` repository, and hands its row's child rows to the list factory's `Fetch`:

<!-- snippet: skill-root-fetch -->
<a id='snippet-skill-root-fetch'></a>
```cs
[Remote]
[Fetch]
internal bool Fetch(Guid id,
    [Service] IOrderRepository repository,
    [Service] IOrderItemListFactory itemsFactory)
{
    var row = repository.Get(id);
    if (row == null)
    {
        return false;
    }

    Id = row.Id;
    OrderNumber = row.OrderNumber;
    CustomerName = row.CustomerName;
    OrderDate = row.OrderDate;
    Status = row.Status;
    TotalAmount = row.TotalAmount;

    // Items and every item within: IsNew=false, IsModified=false
    Items = itemsFactory.Fetch(row.Items);

    // After Fetch completes: Order.IsNew=false, Order.IsModified=false
    return true;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L139-L165' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-root-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A child's `[Fetch]` is `internal`, never `[Remote]`, and takes its own row from the list's `[Fetch]`:

<!-- snippet: skill-child-fetch -->
<a id='snippet-skill-child-fetch'></a>
```cs
[Fetch]
internal void Fetch(OrderItemRow row)
{
    Id = row.Id;
    ProductName = row.ProductName;
    Quantity = row.Quantity;
    UnitPrice = row.UnitPrice;
    LineTotal = row.LineTotal;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs#L88-L98' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### [Insert]

Re-run the rules on the server and refuse an invalid aggregate (the framework does not do this for you); set the key; make the row; hand the row's child collection to the list factory; flush once:

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

#### [Update]

<!-- snippet: skill-root-update -->
<a id='snippet-skill-root-update'></a>
```cs
[Remote]
[Update]
internal async Task Update([Service] IOrderRepository repository,
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

    var row = repository.Get(Id)
        ?? throw new KeyNotFoundException($"Order {Id} not found");

    // Write the order's own columns only if they changed
    if (IsSelfModified)
    {
        MapTo(row);
    }

    itemsFactory.Save(Items!, row.Items);

    repository.SaveChanges();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L240-L269' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-root-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### [Delete]

The repository removes the root row with its child rows; the root does not loop its children:

<!-- snippet: skill-root-delete -->
<a id='snippet-skill-root-delete'></a>
```cs
[Remote]
[Delete]
internal void Delete([Service] IOrderRepository repository)
{
    var row = repository.Get(Id)
        ?? throw new KeyNotFoundException($"Order {Id} not found");

    repository.Remove(row);

    repository.SaveChanges();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L278-L290' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-root-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A child's persistence operations take its row and map it; the generated `Save(target, row)` is what the list's `[Update]` calls:

<!-- snippet: skill-child-insert-update -->
<a id='snippet-skill-child-insert-update'></a>
```cs
[Insert]
internal void Insert(OrderItemRow row)
{
    // The entity sets its own key. Paused - plain assignment stays clean.
    Id = Guid.NewGuid();
    MapTo(row);
}

[Update]
internal void Update(OrderItemRow row)
{
    MapTo(row);
}

private void MapTo(OrderItemRow row)
{
    row.Id = Id;
    row.ProductName = ProductName!;
    row.Quantity = Quantity;
    row.UnitPrice = UnitPrice;
    row.LineTotal = LineTotal;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs#L141-L164' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-insert-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### [Remote] and [Service]

`[Remote]` means "the client calls this", not "this runs on the server". A `[Create]` with no server dependency needs no `[Remote]`; the `[Fetch]` the client calls does, and its `[Service]` repository resolves on the server:

<!-- snippet: skill-remote-entry-point -->
<a id='snippet-skill-remote-entry-point'></a>
```cs
[Create]
public void Create()
{
    // No persistence, no server-only services needed
    // Can run on client or server
}

// The client fetches this root, so it is a client entry point: [Remote]
// makes the client call cross to the server, where the repository lives.
[Remote]
[Fetch]
internal void Fetch(int id, [Service] IRemoteDemoRepository repository)
{
    // This method body runs on SERVER only.
    // repository is resolved from server's DI container.
    var data = repository.GetById(id);
    Id = data.Id;
    Name = data.Name;
}
```
<sup><a href='/src/Design/Design.Domain/FactoryOperations/RemoteBoundary.cs#L82-L102' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-remote-entry-point' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

#### [Execute]

A command is a static `[Factory]` class with a `[Remote, Execute] private static` method. The generated delegate crosses to the server because of `[Remote]`:

<!-- snippet: skill-command -->
<a id='snippet-skill-command'></a>
```cs
[Factory]
public static partial class SendWelcomeEmail
{
    [Remote]
    [Execute]
    private static Task<bool> _Send(
        int employeeId,
        [Service] IEmailService emailService,
        [Service] IEmployeeQueryRepository repository)
    {
        var employee = repository.GetEmailInfo(employeeId)
            ?? throw new InvalidOperationException($"Employee {employeeId} not found");

        emailService.SendWelcome(employee.Email, employee.FullName);
        return Task.FromResult(true);
    }
}
```
<sup><a href='/src/Design/Design.Domain/Commands/ApproveEmployee.cs#L177-L195' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Validation Attributes

Neatoo converts these `System.ComponentModel.DataAnnotations` attributes to rules on construction: `[Required]`, `[StringLength]`, `[MinLength]`, `[MaxLength]`, `[RegularExpression]`, `[Range]`, `[EmailAddress]`. Other attributes (`[Phone]`, `[Url]`, …) are not mapped and are silently ignored.

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

---

## Source Generator Output

Neatoo has two source generators: Neatoo.BaseGenerator (partial properties, rule ids) and the RemoteFactory generator (factories).

### Partial Property Generation

For each partial property on a `[Factory]` class, the BaseGenerator emits a protected accessor over the property object, a getter and setter over its `Value`, and the registration in `InitializePropertyBackingFields`. The accessor is typed `IValidateProperty<T>` on both base classes; on an `EntityBase` the property factory creates an entity property, which adds per-property `IsModified`.

<!-- snippet: skill-partial-properties -->
<a id='snippet-skill-partial-properties'></a>
```cs
public partial string? Name { get; set; }

public partial int Count { get; set; }

public partial decimal Price { get; set; }
```
<sup><a href='/src/Design/Design.Domain/PropertySystem/PropertyBasics.cs#L75-L81' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-partial-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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

Setter accessibility is preserved: a `private set` setter calls `SetPrivateValue` and the property's `IsReadOnly` is `true`; the generated interface member for any non-public setter is get-only.

### Factory Generation

For each `[Factory]` class, RemoteFactory emits an `I{Name}Factory` interface and an `internal` implementation registered in DI by `AddNeatooServices`. Factory members are instance methods: one per `[Create]`/`[Fetch]` overload (service parameters removed; a `bool`-returning `Fetch` becomes `Task<T?>`), and a `Save(target, ...)` when the class has `[Insert]`/`[Update]`/`[Delete]`. A `[Remote]` operation also gets the client-side proxy that routes the call through the single RemoteFactory endpoint.

| Step (per call) | What the generated code does |
|---|---|
| 1 | Resolves the object from DI (its constructor runs, with its rules). |
| 2 | Calls `FactoryStart(operation)` — the object is paused. |
| 3 | Resolves each `[Service]` parameter from the current tier's container. |
| 4 | Invokes your method. |
| 5 | Calls `FactoryComplete(operation)` — resumes; `EntityBase` marks `IsNew` after `Create`, unmodified and old after `Insert`/`Update`. |
| 6 | Returns the object (or `null` for a `Fetch` that returned `false`). |

Consumers inject the factory interface and call it; the generated `Save` routes on `IsDeleted` and `IsNew`:

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

### Rule ID Generation

Rules are identified by a stable id so that messages survive serialization and the same rule is recognized on both tiers. The BaseGenerator emits a `GetRuleId` override mapping the **source text** of each `AddValidation`/`AddAction`/`AddRule` argument (captured by `CallerArgumentExpression`) to a compile-time FNV-1a hash. The hash matches `ValidateBase.ComputeRuleIdHash`, so a rule registered dynamically gets the same id at runtime. Changing a rule's expression changes its id.

---

**UPDATED:** 2026-10-06
