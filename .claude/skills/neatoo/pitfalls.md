# Neatoo Common Pitfalls

Consolidated list of common mistakes when working with Neatoo.

## 1. Forgetting to Reassign After Save()

The server deserializes and returns a **new instance**. Always capture the return value:

<!-- pseudo:pitfall-reassign-save -->
```csharp
// WRONG - changes lost
await personFactory.Save(person);

// CORRECT
person = await personFactory.Save(person);
```
<!-- /snippet -->

See: factories.md, blazor-integration.md

## 2. Not Waiting for Async Rules

Async validation rules run in the background. Check `IsBusy` or call `WaitForTasks()` before checking validity:

<!-- pseudo:pitfall-async-rules -->
```csharp
// WRONG - may check validity before async rules complete
if (person.IsValid) { ... }

// CORRECT
await person.WaitForTasks();
if (person.IsValid) { ... }
```
<!-- /snippet -->

See: rules.md, blazor-integration.md

## 3. Saving Child Entities Directly

Children save through their parent aggregate root. Don't call Save on child factories:

<!-- pseudo:pitfall-child-save -->
```csharp
// WRONG - children don't have their own Save
await orderLineFactory.Save(lineItem);

// CORRECT - save through aggregate root
order = await orderFactory.Save(order);  // Saves order AND all line items
```
<!-- /snippet -->

See: aggregates.md, factories.md

## 4. Missing [Remote] Attribute

Server-side operations need `[Remote]` to execute on the server in client-server scenarios:

<!-- pseudo:pitfall-missing-remote -->
```csharp
// WRONG - runs on client, can't access database
[Fetch]
public void Fetch(Guid id) { ... }

// CORRECT - runs on server
[Remote]
[Fetch]
public void Fetch(Guid id) { ... }
```
<!-- /snippet -->

See: client-server.md, factories.md

## 5. Not Handling Null from Fetch

Fetch returns `null` when the entity isn't found. Always check:

<!-- pseudo:pitfall-null-fetch -->
```csharp
// WRONG - NullReferenceException if not found
var person = personFactory.Fetch(id);
Console.WriteLine(person.Name);

// CORRECT
var person = personFactory.Fetch(id);
if (person is null)
{
    // Handle not found
    return;
}
```
<!-- /snippet -->

See: factories.md

## 6. Checking IsValid Instead of IsSavable

`IsSavable` is the comprehensive check for save readiness:

<!-- pseudo:pitfall-issavable -->
```csharp
// WRONG - doesn't check IsBusy, IsModified, or IsChild
if (person.IsValid) { await Save(); }

// CORRECT - checks IsModified && IsValid && !IsBusy && !IsChild
if (person.IsSavable) { await Save(); }
```
<!-- /snippet -->

See: entities.md, properties.md, blazor-integration.md

## 7. Modifying Properties During Fetch

Use `LoadProperty` instead of property setters during Fetch to avoid triggering rules:

<!-- pseudo:pitfall-loadproperty -->
```csharp
// WRONG - triggers rules during load
[Fetch]
public void Fetch(PersonEntity entity)
{
    Name = entity.Name;  // Triggers validation rules
}

// CORRECT - bypasses rules
[Fetch]
public void Fetch(PersonEntity entity)
{
    LoadProperty(nameof(Name), entity.Name);
}
```
<!-- /snippet -->

Note: `MapFrom` methods typically use direct assignment since rules are paused during factory operations.

See: data-mapping.md, properties.md

## 8. Circular Reference in Aggregates

Don't create circular parent-child references. Children reference parents, not vice versa in terms of ownership:

<!-- pseudo:pitfall-circular-reference -->
```csharp
// WRONG - Order owns LineItems, LineItem owns Order?
public partial IOrder Order { get; set; }  // In LineItem - creates cycle

// CORRECT - Use Parent property (read-only, set by framework)
public IOrder? Parent => GetParent<IOrder>();  // Framework manages this
```
<!-- /snippet -->

See: aggregates.md

## 9. PauseAllActions() on Interface

`PauseAllActions()` is defined on the concrete class, not the interface. Use it inside entity methods:

<!-- snippet: pause-all-actions-interface -->
```csharp
/// <summary>
/// PauseAllActions() is on the concrete class, not the interface.
/// </summary>
public partial interface IPauseActionsPitfall : IValidateBase
{
    string? FirstName { get; set; }
    string? LastName { get; set; }
}

[Factory]
internal partial class PauseActionsPitfall : ValidateBase<PauseActionsPitfall>, IPauseActionsPitfall
{
    public PauseActionsPitfall(IValidateBaseServices<PauseActionsPitfall> services) : base(services) { }

    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }

    [Create]
    public void Create() { }

    /// <summary>
    /// CORRECT: Use PauseAllActions inside entity methods where you have access to the concrete type.
    /// </summary>
    public void BulkUpdate(string first, string last)
    {
        // CORRECT - inside concrete class, PauseAllActions is accessible
        using (PauseAllActions())
        {
            FirstName = first;
            LastName = last;
        }
        // Rules run when disposed
    }
}

/// <summary>
/// Shows the WRONG way - trying to call PauseAllActions on interface.
/// </summary>
public static class PauseActionsPitfallExample
{
    public static void WrongWay(IPauseActionsPitfall person)
    {
        // WRONG - interfaces don't expose PauseAllActions
        // using (person.PauseAllActions()) { }  // Won't compile!

        // Must use methods on the entity that internally use PauseAllActions
        // or work with the concrete type
    }
}
```
<!-- /snippet -->

See: rules.md, properties.md

## 10. Parent Property Points to Aggregate Root

The `Parent` property returns the **aggregate root**, not the immediate containing list:

<!-- snippet: parent-aggregate-root -->
```csharp
/// <summary>
/// Parent property returns the aggregate root, NOT the containing list.
/// </summary>
public partial interface IParentPitfallOrder : IEntityBase
{
    IParentPitfallLineList Lines { get; }
}

public partial interface IParentPitfallLine : IEntityBase
{
    string? Description { get; set; }

    /// <summary>
    /// Parent returns the Order (aggregate root), NOT the LineList.
    /// </summary>
    IParentPitfallOrder? ParentOrder { get; }
}

public interface IParentPitfallLineList : IEntityListBase<IParentPitfallLine> { }

[Factory]
internal partial class ParentPitfallOrder : EntityBase<ParentPitfallOrder>, IParentPitfallOrder
{
    public ParentPitfallOrder(IEntityBaseServices<ParentPitfallOrder> services) : base(services) { }

    public partial IParentPitfallLineList Lines { get; set; }

    [Create]
    public void Create([Service] IParentPitfallLineListFactory listFactory)
    {
        Lines = listFactory.Create();
    }
}

[Factory]
internal partial class ParentPitfallLine : EntityBase<ParentPitfallLine>, IParentPitfallLine
{
    public ParentPitfallLine(IEntityBaseServices<ParentPitfallLine> services) : base(services) { }

    public partial string? Description { get; set; }

    /// <summary>
    /// Parent returns the aggregate root (Order), not the containing list.
    /// Cast to the expected parent type.
    /// </summary>
    public IParentPitfallOrder? ParentOrder => Parent as IParentPitfallOrder;

    /// <summary>
    /// To count siblings, go through the parent aggregate root.
    /// </summary>
    public int SiblingCount => ParentOrder?.Lines.Count ?? 0;

    [Create]
    public void Create() { }
}

[Factory]
internal class ParentPitfallLineList : EntityListBase<IParentPitfallLine>, IParentPitfallLineList
{
    [Create]
    public void Create() { }
}
```
<!-- /snippet -->

See: aggregates.md

## 11. [Required] Is Stricter on Strings

Neatoo's `[Required]` validation uses `IsNullOrWhiteSpace()`, not just `IsNullOrEmpty()`. This catches null, empty, AND whitespace-only strings - stricter than standard .NET:

<!-- snippet: required-whitespace -->
```csharp
/// <summary>
/// [Required] on strings uses IsNullOrWhiteSpace, catching whitespace-only values.
/// This is STRICTER than standard .NET [Required] behavior.
/// </summary>
public partial interface IRequiredWhitespacePitfall : IValidateBase
{
    /// <summary>
    /// All of these fail validation:
    /// - null
    /// - "" (empty string)
    /// - "   " (whitespace only) - STRICTER than standard .NET!
    /// </summary>
    [Required]
    string? Name { get; set; }
}

[Factory]
internal partial class RequiredWhitespacePitfall : ValidateBase<RequiredWhitespacePitfall>, IRequiredWhitespacePitfall
{
    public RequiredWhitespacePitfall(IValidateBaseServices<RequiredWhitespacePitfall> services) : base(services) { }

    [Required]
    public partial string? Name { get; set; }

    [Create]
    public void Create() { }
}

/// <summary>
/// Demonstrates the stricter behavior.
/// </summary>
public static class RequiredWhitespacePitfallExample
{
    public static void ShowBehavior(IRequiredWhitespacePitfall entity)
    {
        entity.Name = null;      // Invalid - as expected
        entity.Name = "";        // Invalid - as expected
        entity.Name = "   ";     // Invalid - STRICTER than standard .NET!
        entity.Name = "John";    // Valid
    }
}
```
<!-- /snippet -->

See: rules.md

## 12. MapModifiedTo Declaration Pattern

`MapModifiedTo` is generated by Neatoo.BaseGenerator. Declare it as a partial method:

<!-- snippet: map-modified-to-declaration -->
```csharp
/// <summary>
/// MapModifiedTo is generated - declare as partial method.
/// </summary>
public partial interface IMapModifiedToPitfall : IEntityBase
{
    Guid Id { get; }
    string? Name { get; set; }
    string? Description { get; set; }
}

/// <summary>
/// EF Core entity for persistence.
/// </summary>
public class MapModifiedToPitfallEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

[Factory]
internal partial class MapModifiedToPitfall : EntityBase<MapModifiedToPitfall>, IMapModifiedToPitfall
{
    public MapModifiedToPitfall(IEntityBaseServices<MapModifiedToPitfall> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }
    public partial string? Description { get; set; }

    /// <summary>
    /// Declare as partial - Neatoo.BaseGenerator provides the implementation.
    /// The generated code only copies properties where IsModified == true.
    /// </summary>
    public partial void MapModifiedTo(MapModifiedToPitfallEntity entity);

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Update]
    public async Task Update()
    {
        await RunRules();
        if (!IsSavable) return;

        // In real code: var entity = await db.FindAsync(Id);
        var entity = new MapModifiedToPitfallEntity { Id = Id };

        // Only modified properties are copied
        MapModifiedTo(entity);

        // In real code: await db.SaveChangesAsync();
    }
}
```
<!-- /snippet -->

See: data-mapping.md

## 13. Using EntityBaseServices<T>() in Production

The parameterless `EntityBaseServices<T>()` constructor is for **unit testing only**. Using it in production code will break your application:

<!-- invalid:pitfall-production-services -->
```csharp
// WRONG - Production code should NEVER do this
public class MyEntity : EntityBase<MyEntity>
{
    public MyEntity() : base(new EntityBaseServices<MyEntity>())  // No factory!
    {
    }
}

// CORRECT - Production entities use DI constructor
[Factory]
internal partial class MyEntity : EntityBase<MyEntity>
{
    public MyEntity(IEntityBaseServices<MyEntity> services) : base(services) { }
}
```
<!-- /snippet -->

**What breaks in production:**
- **Save() fails** - Factory is `null`, no persistence possible
- **[Service] attributes ignored** - No DI container, dependencies won't inject
- **Parent-child relationships broken** - No factory to manage aggregates

**Acceptable use cases (unit testing only):**
- Testing entity state tracking (IsModified, IsNew, etc.)
- Testing calculated properties and business logic methods
- Testing property change behavior

<!-- pseudo:pitfall-test-entity -->
```csharp
// Unit test entity - use [SuppressFactory] to make intent clear
[SuppressFactory]
public class TestableProduct : EntityBase<TestableProduct>
{
    public TestableProduct() : base(new EntityBaseServices<TestableProduct>()) { }

    public string? Name { get => Getter<string>(); set => Setter(value); }
    public decimal TotalValue => Price * Quantity;  // Testable without factory
}
```
<!-- /snippet -->

See: testing.md

## 14. Not Using Interface-First Design

**See best-practices.md for the recommended interface-first pattern.** This section covers violations of that best practice.

Interface-first design is a core Neatoo best practice: define a public interface for every entity and use interface types everywhere in consuming code. The concrete class should be `internal`.

### Anti-Patterns

<!-- invalid:pitfall-interface-antipatterns -->
```csharp
// WRONG - Casting to concrete type
var person = personFactory.Create();
var concrete = (Person)person;  // Don't do this
concrete.InternalMethod();       // If you need this, add it to IPerson

// WRONG - Storing concrete types
private Person _person;  // Should be IPerson

// WRONG - Bypassing factory.Save()
var concrete = (Person)person;
await concrete.SomeInternalSaveMethod();  // Use factory.Save() or person.Save()

// WRONG - Calling child factory directly
var phone = phoneFactory.Create();  // Don't inject child factories
contact.PhoneNumbers.Add(phone);

// CORRECT - Use parent's domain method
var phone = contact.PhoneNumbers.AddPhoneNumber();
```
<!-- /snippet -->

### Diagnosing the Problem

**If you "need" to cast to concrete, the interface is incomplete:**

| Symptom | Solution |
|---------|----------|
| Need a method not on interface | Add the method to the interface |
| Need to access internal state | Expose via interface property/method |
| Child factory called directly | Use `parent.Children.AddItem()` |
| Calling concrete's Save() | Use `factory.Save(entity)` or `entity.Save()` |

### Correct Pattern

<!-- pseudo:pitfall-interface-correct -->
```csharp
// Method needed by consumers? Add to interface
public partial interface IVisit : IEntityBase
{
    Task<IVisit> Archive();  // Business operation on interface
}

// Implementation in concrete class
public async Task<IVisit> Archive()
{
    Status = VisitStatus.Archived;
    return (IVisit)await this.Save();
}
```
<!-- /snippet -->

See: best-practices.md, entities.md, factories.md, aggregates.md

## 15. Injecting Child Factories Outside Aggregates

Child entities should be created through the parent collection's add methods, not by injecting child factories into consuming code:

<!-- pseudo:pitfall-child-factory-injection -->
```csharp
// WRONG - Injecting child factory into Blazor component
@inject IPhoneFactory PhoneFactory

void AddPhone()
{
    var phone = PhoneFactory.Create();  // Bypasses aggregate
    contact.PhoneNumbers.Add(phone);
}

// CORRECT - Use aggregate's domain method
void AddPhone()
{
    var phone = contact.PhoneNumbers.AddPhoneNumber();  // Aggregate manages creation
}
```
<!-- /snippet -->

**Why this matters:**
- The parent's add method ensures proper parent-child relationships
- Factory injection is handled by the list, not the caller
- Domain operations belong on the aggregate, not scattered in consuming code

If you find yourself injecting child factories outside the aggregate, refactor to expose an add method on the collection interface:

<!-- pseudo:pitfall-list-add-method -->
```csharp
public interface IPhoneList : IEntityListBase<IPhone>
{
    IPhone AddPhoneNumber();  // Expose domain operation
}
```
<!-- /snippet -->

See: aggregates.md, entities.md

## Quick Checklist

See best-practices.md for recommended patterns. This checklist helps catch common mistakes.

Before saving:
- [ ] Did I reassign after Save()?
- [ ] Did I await WaitForTasks()?
- [ ] Am I saving the aggregate root (not a child)?
- [ ] Did I check IsSavable (not just IsValid)?
- [ ] Do server methods have [Remote]?
- [ ] Did I handle null from Fetch?

Interface-first design (see best-practices.md):
- [ ] Am I using interface types (not concrete)?
- [ ] Are methods needed by consumers on the interface?
- [ ] Am I creating children through parent's add methods (not child factories)?
