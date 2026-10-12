# Base Classes

Neatoo provides base classes that map to Domain-Driven Design concepts. Choose the base class by what the object does: an object a person edits that needs rules is a Neatoo object; anything else is a plain `[Factory]` class.

## Base Class to DDD Mapping

| Neatoo Base Class | DDD Concept | Persistence | Validation | Change Tracking |
|-------------------|-------------|-------------|------------|-----------------|
| `ValidateBase<T>` | Value Object, validated input | No | Yes | Yes |
| `EntityBase<T>` | Entity / Aggregate Root | Yes | Yes | Yes |
| `EntityListBase<I>` | Collection of child entities | Yes (via root) | Yes | Yes |
| `ValidateListBase<I>` | Collection of `ValidateBase` objects | No | Yes | Yes |
| Static partial class with `[Remote, Execute]` | Command | Execute only | No | No |
| Plain `[Factory]` class, `[Remote, Fetch]` only | Read Model | No | No | No |

For validated criteria objects (search forms with rules and `IsValid`), use `ValidateBase<T>`. For simple criteria, use method parameters or POCOs.

## Interface-First

Every entity and list gets a matched public interface; the concrete is `internal`. Root interfaces extend `IEntityRoot`, child interfaces extend `IEntityBase`, list interfaces extend `IEntityListBase<IChild>`, `ValidateBase` objects extend `IValidateBase`. Every reference — property type, parameter, list type argument — is the interface. The examples below all follow this; the interfaces are shown first.

## ValidateBase<T>

Use for objects that need validation and change tracking but no persistence lifecycle of their own.

**DDD Concept:** Value Object or validated input. Despite the DDD "immutable" ideal, Neatoo value objects have setters for form-binding and validation-before-submit workflows.

**When to use:**
- Composite values a person edits (Address, Money, DateRange)
- Form data that needs validation before submission
- Search/criteria objects with validation rules

Use `ValidateBase<T>` only when the object needs rules. A read model is not a `ValidateBase`.

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

## EntityBase<T>

Use for persistent entities with full CRUD lifecycle.

**DDD Concept:** Entity or Aggregate Root - Objects with identity that persist across time.

**When to use:**
- Domain entities that are saved to a database
- Aggregate roots that coordinate child entities
- Any object that needs Create/Fetch/Update/Delete operations

A root's interface extends `IEntityRoot`:

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

`[Create]` is local. The persistence operations are `[Remote]` (the client calls them, so the call crosses to the server) and `internal`, and take the repository as a method `[Service]` so it resolves on the server. Inside each operation the object is paused: assignment is a clean baseline load.

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

The root's `[Insert]` and `[Update]` should also re-run the rules and refuse an invalid aggregate before writing; see `entities.md` → "Re-run the Rules on the Server" and the `Order` aggregate there.

## EntityListBase<I>

Use for collections of child entities within an aggregate.

**DDD Concept:** Collection of Entities - Managed collection within an aggregate boundary.

**When to use:**
- Order lines in an Order aggregate
- Addresses on an Employee aggregate
- Any collection of child entities

The child interface extends `IEntityBase` (no `IsSavable`, no `Save()`); the root that owns the list extends `IEntityRoot`; the list interface is parameterized on the child interface:

<!-- snippet: skill-entity-list-interfaces -->
<a id='snippet-skill-entity-list-interfaces'></a>
```cs
/// <summary>
/// Child interface for the EntityListBase demo. Extends IEntityBase: a child
/// has no IsSavable and no Save().
/// </summary>
public interface IDemoChild : IEntityBase
{
    string? Name { get; set; }
}

/// <summary>
/// Root interface that owns the EntityListBase demo list.
/// </summary>
public interface IDemoParent : IEntityRoot
{
    IDemoEntityList? Children { get; }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/IBaseClassInterfaces.cs#L34-L51' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-list-interfaces' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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

The list's own `[Fetch]` loads its children through the child factory's `[Fetch]`, so every child lands `IsNew=false`. List operations are `internal` and never `[Remote]`: the root's `[Fetch]` calls them on the server.

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

The root creates the list through the list factory inside its own `[Create]`, and loads it through the list factory inside its `[Fetch]`:

<!-- snippet: skill-parent-creates-list -->
<a id='snippet-skill-parent-creates-list'></a>
```cs
/// <summary>
/// Root that owns DemoEntityList, so the list demo has fetched (not new)
/// children. Persistence is shown in Aggregates/OrderAggregate.
/// </summary>
[Factory]
internal partial class DemoParent : EntityBase<DemoParent>, IDemoParent
{
    public partial IDemoEntityList? Children { get; set; }

    public DemoParent(IEntityBaseServices<DemoParent> services) : base(services) { }

    // The list is created through its own factory inside the parent's [Create]
    [Create]
    public void Create([Service] IDemoEntityListFactory listFactory)
    {
        Children = listFactory.Create();
    }

    [Remote]
    [Fetch]
    internal void Fetch([Service] IDemoRepository repository, [Service] IDemoEntityListFactory listFactory)
    {
        Children = listFactory.Fetch(repository.GetAllNames());
    }
}
```
<sup><a href='/src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs#L520-L546' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-creates-list' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

For the list's `[Update]` — the save cascade — see `entities.md` → "Aggregate Save Cascading".

## ValidateListBase<I>

Use for collections of `ValidateBase` objects: items that need rules but no persistence tracking. There is no `DeletedList`; a removed item is simply gone.

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

## Commands (Static Classes with [Remote, Execute])

Use for operations that need the server and are not an edit of one aggregate: a batch, a side effect, a check a rule asks for.

**DDD Concept:** Command - Request to perform an action.

**When to use:**
- Operations that don't return entity state
- Batch operations
- Side-effect operations (send email, generate report)
- The server call behind an async rule (see `validation.md`)

A command is a static partial `[Factory]` class. Each operation is `[Remote, Execute] private static Task<T> _Name(...)`. `[Remote]` makes a client call cross to the server (RemoteFactory 1.9 and later: without it, an `[Execute]` runs on the calling tier). The generated delegate — `SendWelcomeEmail.Send` here — is the public API; the method itself is never called directly. The method must return `Task<T>`: a plain `Task` generates a delegate returning `Task<Task>` that does not compile.

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

A command does not check data and return a failure result. Whether the action is allowed is decided before the command is called — by a rule on the entity or a flag on a read model that the screen reads. If the command is reached with data it cannot act on, the client and server disagree: that is an application failure, so it throws. An exception is never caught to produce a validation message.

## Read Models (Plain [Factory] Class, [Fetch] Only)

Use for server truth shaped for one screen: a list, a dashboard tile, the flags a page gates on.

**DDD Concept:** Read Model - Optimized view of data for queries.

**When to use:**
- Dashboard data
- Dropdown lists
- Search results
- Flags and counts a screen gates on (`CanStartTherapy`, `ActiveCount`)

A read model has no Neatoo base class. It is never edited, so change tracking, rules and `IsSavable` have nothing to do. RemoteFactory works with any class: `[Factory]` on a plain class gives it a factory and carries it across the wire. Properties have `internal` setters (only `[Fetch]` writes them), the rows are immutable records, and the read model computes what the screen shows or gates on so the UI reads an answer instead of re-deriving it.

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

## Inheritance Guidelines

1. **Always inherit from the appropriate base class** - Don't implement `IValidateBase`/`IEntityBase` directly
2. **Use `partial` keyword** - Source generators extend the class
3. **Interface-first** - public interface, `internal` concrete, every reference typed on the interface
4. **Follow DDD aggregate boundaries** - `IEntityRoot` for roots, `IEntityBase` for children within the aggregate

## Related

- [Properties](properties.md) - Partial properties and the property object
- [Entities](entities.md) - EntityBase lifecycle, persistence and the save cascade
- [Collections](collections.md) - EntityListBase patterns
