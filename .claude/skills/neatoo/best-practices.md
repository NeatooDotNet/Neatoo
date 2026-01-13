# Neatoo Best Practices

Recommended patterns for building applications with Neatoo.

## 1. Interface-First Design

**Define a public interface for every Neatoo entity.** The interface is your API contract; all code outside the entity class works with interfaces.

### The Pattern

<!-- pseudo:skill-interface-first-pattern -->
```csharp
// Public interface - defines the API contract
public partial interface ICustomer : IEntityBase
{
    string? Name { get; set; }
    string? Email { get; set; }
    IPhoneList Phones { get; }

    // Business operations belong on the interface
    Task<ICustomer> Archive();
}

// Internal class - implementation hidden
[Factory]
internal partial class Customer : EntityBase<Customer>, ICustomer
{
    public Customer(IEntityBaseServices<Customer> services) : base(services) { }

    public partial string? Name { get; set; }
    public partial string? Email { get; set; }
    public partial IPhoneList Phones { get; set; }

    public async Task<ICustomer> Archive()
    {
        Status = CustomerStatus.Archived;
        return (ICustomer)await this.Save();
    }
}
```
<!-- /snippet -->

### Benefits

| Benefit | Description |
|---------|-------------|
| **Testability** | Mock dependencies and stub entities in unit tests |
| **Clean API** | Interface defines what consumers can do; implementation hidden |
| **Client-server** | RemoteFactory generates transfer code from interfaces |
| **Encapsulation** | `internal` class prevents direct instantiation |

### Applying the Pattern

**Always use interface types** in consuming code:

<!-- pseudo:skill-interface-usage -->
```csharp
// Fields and properties - use interfaces
private IOrder _order;
public ICustomer SelectedCustomer { get; set; }

// Method parameters and returns - use interfaces
public void ProcessOrder(IOrder order) { ... }
public async Task<ICustomer> LoadCustomer(Guid id) { ... }

// Factory calls return interfaces
var customer = await customerFactory.Create();
customer = await customerFactory.Save(customer);
```
<!-- /snippet -->

### Anti-Patterns

**WRONG - Casting to concrete type:**
<!-- invalid:skill-casting-to-concrete -->
```csharp
var person = personFactory.Create();
var concrete = (Person)person;  // Don't do this
```
<!-- /snippet -->

**WRONG - Storing concrete types:**
<!-- invalid:skill-storing-concrete-types -->
```csharp
private Person _person;  // Should be IPerson
```
<!-- /snippet -->

**WRONG - Calling child factory directly:**
<!-- invalid:skill-calling-child-factory -->
```csharp
var phone = phoneFactory.Create();  // Bypass aggregate's add method
contact.PhoneNumbers.Add(phone);
```
<!-- /snippet -->

**If you need to cast, the interface is incomplete.** Add the needed method or property:

| Symptom | Solution |
|---------|----------|
| Need a method not on interface | Add the method to the interface |
| Need to access internal state | Expose via interface property |
| Child factory called directly | Use `parent.Children.AddItem()` pattern |

See pitfalls.md Section 14 for detailed anti-pattern examples.

## 2. Factory Pattern for Entity Creation

**Never instantiate entities directly.** Always use the generated factory.

<!-- pseudo:skill-factory-creation -->
```csharp
// WRONG - Direct instantiation
var order = new Order();  // Won't compile (internal class)

// CORRECT - Factory creation
var order = await orderFactory.Create();
```
<!-- /snippet -->

The factory provides:
- Parent-child relationship setup
- State tracking initialization
- Dependency injection
- Client-server transfer capability

## 3. Aggregate Root Saves Children

**Save through the aggregate root, not individual children.**

<!-- pseudo:skill-aggregate-save -->
```csharp
// WRONG - Saving child directly
await orderLineFactory.Save(lineItem);  // Child factories don't have Save

// CORRECT - Save through aggregate root
order = await orderFactory.Save(order);  // Saves order AND all line items
```
<!-- /snippet -->

## 4. Reassign After Save

**Always capture the return value from Save().** The server deserializes a new instance.

<!-- pseudo:skill-save-reassign -->
```csharp
// WRONG - Changes lost
await personFactory.Save(person);

// CORRECT - Reassign
person = await personFactory.Save(person);
```
<!-- /snippet -->

## 5. Check IsSavable, Not Just IsValid

**Use `IsSavable` for complete save-readiness check.**

<!-- pseudo:skill-issavable-check -->
```csharp
// WRONG - Incomplete check
if (person.IsValid) { await Save(); }

// CORRECT - Complete check
if (person.IsSavable) { await Save(); }
```
<!-- /snippet -->

`IsSavable` is `true` when: `IsModified && IsValid && !IsBusy && !IsChild`

## 6. Wait for Async Rules

**Await `WaitForTasks()` before checking validity** when using async validation rules.

<!-- pseudo:skill-waitfortasks -->
```csharp
// WRONG - May check before async rules complete
if (person.IsValid) { ... }

// CORRECT - Wait for async rules
await person.WaitForTasks();
if (person.IsValid) { ... }
```
<!-- /snippet -->

## 7. Nullable IDs for Database-Generated Keys

**Use nullable types for ID properties when the database generates the value.** A `null` ID means "not yet persisted."

### The Pattern

<!-- pseudo:skill-nullable-id-pattern -->
```csharp
public partial interface IOrder : IEntityBase
{
    Guid? Id { get; }  // null = not persisted, Guid = persisted
}

[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public partial Guid? Id { get; set; }

    [Create]
    public void Create()
    {
        // Id stays null - assigned during Insert
    }

    [Insert]
    public async Task Insert([Service] IDbContext db)
    {
        var entity = new OrderEntity();
        MapTo(entity);
        db.Orders.Add(entity);
        await db.SaveChangesAsync();
        Id = entity.Id;  // Database-generated ID assigned here
    }
}
```
<!-- /snippet -->

### Why This Matters

| Approach | Problem |
|----------|---------|
| `Guid Id` with `Guid.Empty` | Ambiguous - "not set" or actual empty GUID? |
| `long Id` with `0` | Ambiguous - "not set" or ID zero? |
| `Guid? Id` with `null` | Clear - `null` = "doesn't exist in database yet" |

### Key Points

1. **ID is null until Insert** - Don't assign IDs in Create methods
2. **Database generates the ID** - Assigned during Insert after `SaveChangesAsync()`
3. **After Save(), capture the return** - Returned instance has database-assigned ID

### Interfaces Are Persistence-Agnostic

**Foreign keys are persistence implementation details—never expose them on interfaces.**

<!-- invalid:fk-on-interface-skill -->
```csharp
// WRONG - FK on interface
public interface IOrderLine : IEntityBase
{
    long? OrderId { get; }  // Leaks persistence detail
}
```
<!-- /snippet -->

<!-- pseudo:parent-navigation-skill -->
```csharp
// CORRECT - Use parent navigation property (object reference)
public interface IOrderLine : IEntityBase
{
    IOrder? ParentOrder { get; }  // Returns object, not FK
}

// Implementation uses Neatoo's built-in Parent property
// NO FK property on domain object
internal partial class OrderLine : EntityBase<OrderLine>, IOrderLine
{
    public IOrder? ParentOrder => this.Parent as IOrder;
}
```
<!-- /snippet -->

#### Best Practice: Don't Store FKs on Domain Objects

**Pass parent IDs through factory method parameters, not as stored properties.**

- Child's `[Insert]` receives parent ID as parameter
- Pass ID directly to EF entity during persistence
- Domain object never stores the FK
- Factory's `Save(child, parentId)` passes IDs through

### Child Entity Pattern

Child entities receive the parent's ID as a parameter to their Insert method. The FK goes directly to the EF entity—not stored on the domain object:

<!-- pseudo:child-insert-pattern-skill -->
```csharp
// Child Insert accepts parent ID - passes directly to EF entity
[Insert]
public async Task Insert(long orderId, [Service] IDbContext db)
{
    var entity = new OrderLineEntity
    {
        OrderId = orderId,  // FK goes directly to EF entity
        ProductName = ProductName,
        Quantity = Quantity
    };
    db.OrderLines.Add(entity);
    await db.SaveChangesAsync();
    Id = entity.Id;
}
```
<!-- /snippet -->

Parent saves itself first, then passes its ID to children:

<!-- pseudo:parent-saves-children-skill -->
```csharp
// Parent passes its ID when saving children
[Insert]
public async Task Insert([Service] IDbContext db, [Service] IOrderLineFactory lineFactory)
{
    // Save parent first
    db.Orders.Add(entity);
    await db.SaveChangesAsync();
    Id = entity.Id;

    // Pass parent ID to each child's Insert
    foreach (var line in Lines)
    {
        await lineFactory.Save(line, Id.Value);
    }
}
```
<!-- /snippet -->

**Key insight:** The factory's `Save(child, parentId)` passes the parent ID through to the child's Insert method.

## Quick Checklist

Before implementing an entity:
- [ ] Define public interface (IEntityBase or IValidateBase)
- [ ] Make concrete class `internal partial`
- [ ] Use interface types in all consuming code
- [ ] Expose business operations on interface
- [ ] Use nullable ID (`Guid?`, `long?`) for database-generated keys

Before saving:
- [ ] Check `IsSavable` (not just `IsValid`)
- [ ] Await `WaitForTasks()` if async rules exist
- [ ] Save through aggregate root
- [ ] Reassign return value: `entity = await factory.Save(entity)`
