# Claude Code Design Guidance

This document provides specific guidance for Claude Code when working with Neatoo framework code. The `src/Design/` projects are your primary reference for understanding Neatoo's API design.

## How to Use Design Projects

### Reading Order

When trying to understand a Neatoo concept:

1. **Start with Design.Domain** - The heavily-commented source files explain design rationale
2. **Check Design.Tests** - Tests show expected behavior and usage patterns
3. **Reference main source** - For implementation details not in Design

### Key Files by Topic

| Topic | Primary File | Tests |
|-------|-------------|-------|
| Base class selection | `BaseClasses/AllBaseClasses.cs` | `BaseClassTests/*` |
| Factory operations | `FactoryOperations/*.cs` | `FactoryTests/*` |
| Aggregate patterns | `Aggregates/OrderAggregate/*` | `AggregateTests/*` |
| Property system | `PropertySystem/*.cs` | `PropertyTests/*` |
| Validation rules | `Rules/*.cs` | `RuleTests/*` |
| Generator behavior | `Generators/TwoGeneratorInteraction.cs` | N/A |
| DI setup | `DI/*.cs` | N/A |

## Critical Patterns to Understand

### 1. The Four Base Classes

**EntityBase<T>** - Use for persistent entities:
- Has IsNew, IsModified, IsDeleted, IsSavable
- Save() routes to Insert/Update/Delete based on state
- Child entities have IsChild=true and cannot save independently

**ValidateBase<T>** - Use for value objects and validation-only:
- Has IsValid, IsSelfValid, IsBusy
- No persistence state tracking
- RuleManager for validation

**EntityListBase<I>** - Use for child entity collections:
- DeletedList tracks removed items for persistence
- IsModified cascades from children
- Enforces aggregate boundaries

**ValidateListBase<I>** - Use for value object collections:
- IsValid aggregates from all children
- No DeletedList (no persistence tracking)

### 2. Constructor vs Method [Service] Injection

This distinction is critical for understanding client-server boundaries:

```csharp
// Constructor [Service] - Available on BOTH client AND server
public Employee([Service] IValidateBaseServices<Employee> services) : base(services)

// Method [Service] - Only available on SERVER
[Remote]
[Fetch]
public void Fetch(int id, [Service] IEmployeeRepository repository)
```

- Constructor services are in the DI container for both client and server
- Method services are only registered on the server
- If a non-[Remote] method has method-injected services and is called on client, you get a DI exception

### 3. DeletedList Lifecycle

When working with EntityListBase, understand this lifecycle:

```
Item removed from list:
  ├── If IsNew=true: Discarded (never persisted)
  └── If IsNew=false:
      ├── MarkDeleted() called
      ├── Added to DeletedList
      └── ContainingList reference preserved

During aggregate Save():
  ├── Each DeletedList item: [Delete] called
  └── FactoryComplete(Update):
      ├── DeletedList.Clear()
      └── ContainingList references cleared

Intra-aggregate move (item from ListA to ListB):
  ├── ListA.Remove(item) → goes to ListA.DeletedList
  ├── ListB.Add(item) → removed from DeletedList, UnDeleted
  └── Result: No persistence delete needed
```

### 4. Factory Operations and PauseAllActions

During factory operations, rules are paused:

```csharp
[Create]
public void Create()
{
    // Rules don't fire during Create - IsPaused=true
    Name = "Default";  // No rule triggered
}
// After Create: IsPaused=false, rules eligible

// To load data without triggering rules or modification:
[Fetch]
public void Fetch(int id, [Service] IRepository repo)
{
    using (PauseAllActions())  // For EntityBase - already paused during Fetch
    {
        this["Name"].LoadValue(repo.Get(id).Name);  // No IsModified=true
    }
}
```

### 5. Two-Generator Interaction

Neatoo uses two source generators:

**Neatoo.BaseGenerator:**
- Detects partial properties on EntityBase/ValidateBase
- Generates property backing fields (IEntityProperty<T>)
- Generates getter/setter implementations
- Generates InitializePropertyBackingFields() override

**RemoteFactory:**
- Detects [Factory] attribute on classes
- Detects [Create], [Fetch], etc. methods
- Generates factory interfaces (IEmployeeFactory)
- Generates factory implementations
- For [Remote] methods: generates HTTP client proxies

Both generators run independently during compilation.

## RemoteFactory Deep Dive

### How [Remote] Methods Work in Blazor WebAssembly

RemoteFactory generates different code for server and client assemblies based on the `NeatooFactory` enum passed to `AddNeatooServices()`:

```csharp
// SERVER: NeatooFactory.Server
services.AddNeatooServices(NeatooFactory.Server, typeof(MyDomain).Assembly);

// CLIENT: NeatooFactory.Remote
services.AddNeatooServices(NeatooFactory.Remote, typeof(MyDomain).Assembly);
```

**Server-Side Factory (NeatooFactory.Server):**
```csharp
// Generated for: [Remote][Fetch] public void Fetch(int id, [Service] IRepo repo)
public Employee Fetch(int id)
{
    var obj = _serviceProvider.GetRequiredService<Employee>();
    var repo = _serviceProvider.GetRequiredService<IRepo>();  // [Service] resolved
    obj.FactoryStart(FactoryOperation.Fetch);
    obj.Fetch(id, repo);  // Actual method call
    obj.FactoryComplete(FactoryOperation.Fetch);
    return obj;
}
```

**Client-Side Factory (NeatooFactory.Remote):**
```csharp
// Generated for: [Remote][Fetch] public void Fetch(int id, [Service] IRepo repo)
public async Task<Employee> Fetch(int id)
{
    // User parameters serialized, [Service] parameters NOT serialized
    var request = new FetchRequest { id = id };
    var response = await _httpClient.PostAsJsonAsync("/api/Employee/Fetch", request);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<Employee>(_jsonOptions);
}
```

### Factory Method Invocation Flow

```
CLIENT (Blazor WASM)                    SERVER (ASP.NET Core)
     |                                        |
     | employeeFactory.Fetch(1)               |
     |     ↓                                  |
     | [Generated HTTP Proxy]                 |
     |     ↓                                  |
     | POST /api/Employee/Fetch               |
     | Body: { "id": 1 }  ------------------>  |
     |                                        | [API Controller receives]
     |                                        |     ↓
     |                                        | [Server Factory]
     |                                        |     ↓
     |                                        | employee = new Employee(services)
     |                                        | repo = DI.GetService<IRepo>()
     |                                        | employee.FactoryStart(Fetch)
     |                                        | employee.Fetch(1, repo)
     |                                        | employee.FactoryComplete(Fetch)
     |                                        |     ↓
     |                                        | Serialize employee
     | <------------------------------------ | JSON response
     |     ↓                                  |
     | Deserialize employee                   |
     | employee.OnDeserialized()              |
     |     ↓                                  |
     | return employee                        |
```

### NeatooFactory Enum Values

| Value | Description | Factory Behavior |
|-------|-------------|------------------|
| `Server` | Full server-side implementation | All methods execute locally, [Service] resolved from DI |
| `Remote` | Client-side HTTP proxy | [Remote] methods make HTTP calls, non-[Remote] execute locally |
| `Logical` | In-process (testing/monolith) | Everything local, no HTTP, [Remote] ignored |

### PrivateAssets="all" Pattern Explained

This pattern isolates server-only dependencies from client assemblies:

```xml
<!-- Infrastructure.csproj - Contains EF Core -->
<Project>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
  </ItemGroup>
</Project>

<!-- Domain.csproj - References Infrastructure privately -->
<Project>
  <ItemGroup>
    <!-- PrivateAssets="all" means: compile against this, but don't expose to consumers -->
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" PrivateAssets="all" />
  </ItemGroup>
</Project>

<!-- Server.csproj - Explicitly references both -->
<Project>
  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
  </ItemGroup>
</Project>

<!-- Client.csproj - Only sees Domain, never Infrastructure -->
<Project>
  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
    <!-- Infrastructure is NOT transitive due to PrivateAssets="all" -->
  </ItemGroup>
</Project>
```

**Why this works:**
1. Domain.csproj can reference `IDbContext` interface from Infrastructure
2. Domain methods can have `[Service] IDbContext` parameters
3. Client.csproj compiles (interfaces are satisfied)
4. Client.csproj has no EF Core DLLs (PrivateAssets prevents transitive dependency)
5. Server.csproj explicitly references Infrastructure, so EF Core is available
6. At runtime, client calls [Remote] methods which execute on server

### Non-[Remote] Methods with [Service] Parameters

Methods without [Remote] that have [Service] parameters:
- Are generated for BOTH client and server
- Work correctly on server (services resolved from DI)
- FAIL on client at runtime ("service not registered")

This is intentional - it catches incorrect usage:

```csharp
// WRONG: Missing [Remote] - will fail on client
[Fetch]
public void Fetch(int id, [Service] IDbContext db) { }
// Client call: factory.Fetch(1) -> DI error "IDbContext not registered"

// RIGHT: [Remote] triggers HTTP proxy generation
[Remote]
[Fetch]
public void Fetch(int id, [Service] IDbContext db) { }
// Client call: factory.Fetch(1) -> HTTP to server -> server resolves IDbContext
```

### Save() and [Remote] Routing

The `Save()` method on EntityBase intelligently routes to Insert/Update/Delete:

```csharp
public async Task Save()
{
    // Entity determines which operation based on state
    if (IsDeleted && !IsNew)
        await Factory.Delete(this);  // [Delete] method
    else if (IsNew)
        await Factory.Insert(this);  // [Insert] method
    else if (IsModified)
        await Factory.Update(this);  // [Update] method
}
```

For aggregate roots with [Remote] on Insert/Update/Delete:
- Client calls `entity.Save()`
- Save() calls `Factory.Insert/Update/Delete`
- Generated factory proxy makes HTTP call
- Server executes actual persistence

### HTTP Endpoint Conventions

RemoteFactory generates endpoints following this pattern:
- Base path: `/api/{TypeName}/`
- Method path: `/{OperationName}`
- HTTP method: POST (for all operations)
- Request body: JSON with user parameters (no [Service] parameters)
- Response body: JSON serialized entity

Example endpoints for `Employee`:
- `POST /api/Employee/Create` - Body: `{}`
- `POST /api/Employee/Fetch` - Body: `{ "id": 1 }`
- `POST /api/Employee/Insert` - Body: `{ employee: {...} }`
- `POST /api/Employee/Update` - Body: `{ employee: {...} }`
- `POST /api/Employee/Delete` - Body: `{ employee: {...} }`

## Common Implementation Tasks

### Adding a New Entity

1. Create class inheriting from `EntityBase<T>`
2. Add `[Factory]` attribute
3. Add partial properties for data
4. Add constructor with `IEntityBaseServices<T>` parameter
5. Add validation rules in constructor
6. Add factory methods: `[Create]`, `[Remote][Fetch]`, `[Remote][Insert]`, `[Remote][Update]`, `[Remote][Delete]`

### Adding a Child Entity

Same as above, but:
- The entity will have `IsChild=true` when added to an EntityListBase
- Do NOT add `[Remote]` to Insert/Update/Delete - parent handles persistence
- Child's Save() will throw because `IsSavable=false` when `IsChild=true`

### Adding Validation Rules

**Fluent API (simple cases):**
```csharp
RuleManager.AddValidation(
    t => string.IsNullOrEmpty(t.Name) ? "Name required" : "",
    t => t.Name);  // Trigger property

RuleManager.AddAction(
    t => t.Total = t.Quantity * t.Price,
    t => t.Quantity,
    t => t.Price);  // Multiple triggers
```

**Class-based rules (complex cases):**
```csharp
public class MyRule : AsyncRuleBase<MyEntity>
{
    public MyRule() : base(t => t.Name) { }  // Trigger in constructor

    protected override Task<IRuleMessages> Execute(MyEntity target, CancellationToken? token)
    {
        if (string.IsNullOrEmpty(target.Name))
            return Task.FromResult<IRuleMessages>(
                (nameof(MyEntity.Name), "Name required").AsRuleMessages());
        return Task.FromResult<IRuleMessages>(None);  // 'None' inherited from base
    }
}
```

### Testing Patterns

Design.Tests shows the testing approach:

```csharp
[TestClass]
public class MyTests
{
    private IServiceScope _scope = null!;
    private IMyEntityFactory _factory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IMyEntityFactory>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public async Task PropertyChange_TriggersRule()
    {
        var entity = _factory.Create();
        entity.Name = "Test";
        await entity.WaitForTasks();  // Wait for async rules
        Assert.IsTrue(entity.IsValid);
    }
}
```

## What NOT to Do

### Do NOT Mock Neatoo Classes

From main CLAUDE.md:
> When writing unit tests for Neatoo:
> 1. Only mock external dependencies
> 2. Use real Neatoo classes - "new up" Neatoo dependencies
> 3. Inherit from Neatoo base classes, don't implement interfaces manually

### Do NOT Call Protected Methods Directly

Methods like `MarkOld()`, `MarkUnmodified()` are protected for a reason. Use factory operations that call them appropriately:
- `Create()` → `MarkNew()`
- `Fetch()` → `MarkOld()`, `MarkUnmodified()`
- `Save()` after Insert → `MarkOld()`, `MarkUnmodified()`
- `Save()` after Update → `MarkUnmodified()`

### Do NOT Use Reflection

From main CLAUDE.md:
> Do NOT use reflection in code without reviewing and getting approval first.
> The goal is to have no reflection, even in tests.

### Do NOT Skip WaitForTasks()

After property changes that trigger async rules:
```csharp
entity.Name = "Test";
await entity.WaitForTasks();  // REQUIRED before checking IsValid
Assert.IsTrue(entity.IsValid);
```

## Design Project Maintenance

When updating Design projects:

1. **Update source files first** - Design.Domain is the source of truth
2. **Update tests** - Design.Tests must pass
3. **Update comments** - Keep DESIGN DECISION, GENERATOR BEHAVIOR, etc. accurate
4. **Update this document** if guidance changes

## Quick Reference: State Properties

| Property | Base Class | Meaning |
|----------|-----------|---------|
| `IsNew` | EntityBase | Not yet persisted |
| `IsModified` | EntityBase | Has unsaved changes (includes children) |
| `IsSelfModified` | EntityBase | This object has changes (excludes children) |
| `IsDeleted` | EntityBase | Marked for deletion |
| `IsSavable` | EntityBase | IsModified && IsValid && !IsBusy && !IsChild |
| `IsChild` | EntityBase | Part of parent aggregate |
| `IsValid` | ValidateBase | All rules pass (includes children) |
| `IsSelfValid` | ValidateBase | This object's rules pass (excludes children) |
| `IsBusy` | ValidateBase | Async operations pending |
| `IsPaused` | ValidateBase | Events/rules suppressed |
| `Root` | EntityBase/List | Aggregate root reference |
| `Parent` | All bases | Parent in object graph |

## Threading Guarantees

### Single-Threaded Object Model

Neatoo objects are designed for **single-threaded access per object graph**. The framework does not use locks or thread-safe collections internally.

**Core Assumptions:**
- One thread owns an object graph at a time
- UI thread binding in WPF/Blazor (automatic with data binding)
- Request thread in ASP.NET Core (one request = one thread context)

**What IS Thread-Safe:**
- Rule ID generation uses `Interlocked.Increment` for unique execution IDs
- Static factory registrations (happen once at startup)
- Reading properties is generally safe if no writes are occurring

**What IS NOT Thread-Safe:**
- Concurrent property modifications on the same object
- Adding/removing items from EntityListBase concurrently
- Running rules while properties are being modified
- Pausing/resuming while rules are executing

### Async Rules and the Synchronization Context

Rules run asynchronously but Neatoo relies on the synchronization context to ensure proper sequencing.

**Rule Execution Flow:**
```
Property set → RuleManager.RunRules → Async rule starts → IsBusy=true
                                    → Property.AddMarkedBusy(execId)
                                    → Rule completes
                                    → Property.RemoveMarkedBusy(execId)
                                    → IsBusy recalculated
```

**Best Practices:**
```csharp
// CORRECT: Set property and await completion
entity.Name = "Test";
await entity.WaitForTasks();  // Blocks until all async rules complete
Assert.IsTrue(entity.IsValid);

// INCORRECT: Check validity before rules complete
entity.Name = "Test";
Assert.IsTrue(entity.IsValid);  // May be stale - rule still running!
```

### LoadValue vs SetValue Thread Safety

Both `LoadValue()` and `SetValue()` should be called from the owning thread:

| Method | Triggers Rules | Sets IsModified | Safe During Pause |
|--------|---------------|-----------------|-------------------|
| `SetValue()` | Yes (if not paused) | Yes | Only if paused |
| `LoadValue()` | No | No | Yes |

**Key Point:** During `[Fetch]` operations, the factory pauses the object, so `LoadValue()` calls are safe and don't trigger rule cascades.

### Factory Operation Threading

Factory operations are designed for single-threaded execution:

```
FactoryStart(operation)  → IsPaused = true
  ↓
User's [Create]/[Fetch]/[Insert]/[Update]/[Delete] method runs
  ↓
FactoryComplete(operation) → IsPaused = false, state updates
```

**Never do this:**
```csharp
// WRONG: Concurrent factory operations
var t1 = Task.Run(() => factory.Fetch(1));
var t2 = Task.Run(() => factory.Fetch(2));  // Different objects - OK
var t3 = Task.Run(() => entity.Save());     // Same object as t1 - DANGER
await Task.WhenAll(t1, t2, t3);
```

## Serialization Considerations

### What Survives Serialization

Neatoo uses custom JSON converters for client-server state transfer:

| State | Serialized | Notes |
|-------|-----------|-------|
| Property values | Yes | All registered properties |
| `IsNew` | Yes | Via `IEntityMetaProperties` |
| `IsDeleted` | Yes | Via `IEntityMetaProperties` |
| `IsModified` | Yes | Via `IEntityMetaProperties` |
| `IsChild` | Yes | Via `IEntityMetaProperties` |
| `DeletedList` items | Yes | Serialized as part of list |
| Validation messages | No | Rules re-run on deserialization |
| `IsBusy` | No | Reset on deserialization |
| `IsPaused` | No | Paused during deserialization, resumed after |
| Rule state (executed flags) | No | Rules start fresh |
| Parent reference | Yes | Via `$ref`/`$id` reference handling |

### Circular Reference Handling

Neatoo's JSON converters handle circular references using `$id`/`$ref` patterns:

```json
{
  "$id": "1",
  "$type": "Domain.Employee",
  "PropertyManager": [...],
  "Addresses": {
    "$id": "2",
    "$type": "Domain.AddressList",
    "$items": [
      { "$type": "Domain.Address", "$value": { "$id": "3", ... } }
    ]
  }
}
```

**Key Behaviors:**
- First occurrence of an object uses `$id`
- Subsequent references use `$ref` pointing to the `$id`
- Parent-child relationships are preserved through references

### DeletedList Serialization

When an `EntityListBase` is serialized, both active items AND deleted items are included:

```csharp
// On server after removal:
order.Items.Remove(item);  // item goes to DeletedList
// Serialize to client: both Items and DeletedList travel

// On client after modification:
order.Items.Remove(anotherItem);  // another item to DeletedList
// Serialize back to server: all deletions preserved
```

**Important:** Items with `IsNew=true` that are removed are NOT added to DeletedList (they were never persisted, so no delete needed).

### Client-Server State Transfer Pattern

```
CLIENT                                SERVER
  |                                      |
  |--[Remote] Fetch(id)----------------->|
  |                                      | Creates entity
  |                                      | Loads from DB
  |<----- JSON (entity state) -----------|
  | Deserializes                         |
  | Rules run fresh                      |
  | User modifies                        |
  |                                      |
  |--[Remote] Save() (entity state)----->|
  |                                      | Deserializes
  |                                      | IsModified preserved
  |                                      | DeletedList preserved
  |                                      | Calls Insert/Update/Delete
  |<----- JSON (updated state) ----------|
  | IsNew=false now                      |
  | IsModified=false now                 |
```

### Serialization Pitfalls

**Pitfall 1: Rules not re-running after deserialization**
```csharp
// After deserialization, rules have not run yet
// IsValid reflects the serialized state, not validated state
var employee = await factory.Fetch(1);  // Deserialized on client
await employee.RunRules(RunRulesFlag.All);  // Explicitly run all rules
// Now IsValid is accurate
```

**Pitfall 2: Transient services in rules**
```csharp
// WRONG: Rule captures transient service during construction
public MyRule(ITransientService svc) : base(t => t.Name)
{
    _svc = svc;  // Captured on server, not available after deserialize!
}

// RIGHT: Inject service per execution (not currently supported)
// Or: Use method-injected services in factory methods, not rules
```

**Pitfall 3: Non-serializable property types**
```csharp
// WRONG: Property holds non-serializable type
public partial Stream DataStream { get; set; }  // Will fail to serialize

// RIGHT: Use serializable types or exclude from serialization
public partial byte[] DataBytes { get; set; }  // Serializable
```

### JSON Converter Registration

Neatoo's custom converters are registered automatically via `AddNeatooServices()`:

```csharp
services.AddNeatooServices(NeatooFactory.Server, typeof(MyDomain).Assembly);
// Registers:
// - NeatooJsonConverterFactory
// - NeatooBaseJsonTypeConverter<T> for IValidateBase types
// - NeatooListBaseJsonTypeConverter<T> for list types
```

The converters handle:
- Type polymorphism (`$type` property)
- Reference preservation (`$id`/`$ref`)
- Property manager serialization
- Entity meta properties (IsNew, IsModified, etc.)
