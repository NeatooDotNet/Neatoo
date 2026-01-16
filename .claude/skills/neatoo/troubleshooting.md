# Neatoo Troubleshooting

Common issues and their solutions.

## Source Generator Issues

### Properties Don't Work / Entity Behaves Incorrectly

**Symptoms:**
- Properties return default values
- `IsModified` never becomes `true`
- Validation rules don't run
- No compile errors, but entity doesn't work

**Cause:** Missing `partial` keyword on class or properties.

**Solution:**

```csharp
// WRONG - compiles but doesn't work
[Factory]
internal class Person : EntityBase<Person>, IPerson
{
    public string? Name { get; set; }  // Not partial!
}

// CORRECT
[Factory]
internal partial class Person : EntityBase<Person>, IPerson  // partial class
{
    public partial string? Name { get; set; }  // partial property
}
```

### Factory Not Found at Runtime

**Symptoms:**
- `TypeNotRegisteredException` for factory
- DI fails to resolve `IPersonFactory`

**Solutions:**

1. Register assembly with `AddNeatooServices`:
```csharp
builder.Services.AddNeatooServices(
    NeatooFactory.Server,
    typeof(IPerson).Assembly);  // Include your domain assembly
```

2. Check interface is public:
```csharp
public partial interface IPerson : IEntityBase { }  // Must be public
```

3. Verify `[Factory]` attribute is present:
```csharp
[Factory]  // Required
internal partial class Person : EntityBase<Person>, IPerson { }
```

### Generated Code Not Appearing

**Solutions:**

1. Clean and rebuild:
```bash
dotnet clean && dotnet build
```

2. Restart IDE (analyzer host may need restart)

3. Check for generator errors in build output

4. Verify .NET 8+ target:
```xml
<TargetFramework>net8.0</TargetFramework>
```

### Hint Name Too Long Error

**Symptoms:**
- Build error mentioning hint name length
- Error for types with long namespace + class names (> 50 chars)

**Solution:**

Add assembly attribute to increase limit:

```csharp
// In any .cs file
[assembly: FactoryHintNameLength(100)]
```

### Analyzer Warning NEATOO010

**Symptoms:**
- Warning on property assignments in constructors

**Cause:** Direct assignment marks entity as modified during construction.

**Solution:**

```csharp
// WRONG - marks entity as modified
public Person(IEntityBaseServices<Person> services) : base(services)
{
    Status = "Active";  // Triggers NEATOO010
}

// CORRECT - use LoadValue
public Person(IEntityBaseServices<Person> services) : base(services)
{
    StatusProperty.LoadValue("Active");
}
```

## Validation Issues

### Rules Not Executing

**Check these in order:**

1. **Properties are partial:**
```csharp
public partial string? Email { get; set; }  // Correct
public string? Email { get; set; }          // Wrong - no tracking
```

2. **Rules are registered:**
```csharp
public Person(IEntityBaseServices<Person> services, IEmailRule emailRule)
    : base(services)
{
    RuleManager.AddRule(emailRule);  // Must add rule
}
```

3. **Trigger properties match:**
```csharp
public class EmailRule : RuleBase<IPerson>
{
    public EmailRule() : base(p => p.Email) { }  // Triggers on Email changes
}
```

4. **DI registration exists:**
```csharp
builder.Services.AddScoped<IEmailRule, EmailRule>();
```

### Validation Only Shows After Save

**Cause:** Validation logic is in factory methods instead of rules.

**Solution:** Move validation to rules:

```csharp
// WRONG - in [Insert] method
[Insert]
public async Task Insert([Service] IDbContext db)
{
    if (await db.EmailExistsAsync(Email))  // Only checked at save time!
        throw new InvalidOperationException("Email in use");
}

// CORRECT - in async rule (validates immediately when email changes)
public class UniqueEmailRule : AsyncRuleBase<IPerson>
{
    protected override async Task<IRuleMessages> Execute(IPerson target, ...)
    {
        if (await _checker.EmailExists(target.Email))
            return (nameof(target.Email), "Email in use").AsRuleMessages();
        return None;
    }
}
```

### Async Rules Not Completing

**Check these:**

1. **Using async/await properly:**
```csharp
// WRONG - can deadlock
person.WaitForTasks().Wait();

// CORRECT
await person.WaitForTasks();
```

2. **Handle cancellation:**
```csharp
protected override async Task<IRuleMessages> Execute(
    IPerson target, CancellationToken? token = null)
{
    token?.ThrowIfCancellationRequested();
    await service.ValidateAsync(target.Email, token ?? CancellationToken.None);
}
```

## Save Issues

### Save Not Persisting (Database Unchanged)

**Check these:**

1. **Check IsSavable:**
```csharp
Console.WriteLine($"IsModified: {person.IsModified}");
Console.WriteLine($"IsValid: {person.IsValid}");
Console.WriteLine($"IsBusy: {person.IsBusy}");
Console.WriteLine($"IsChild: {person.IsChild}");
Console.WriteLine($"IsSavable: {person.IsSavable}");
```

2. **Factory methods exist:**
```csharp
[Remote][Insert]
public async Task Insert([Service] IDbContext db) { }

[Remote][Update]
public async Task Update([Service] IDbContext db) { }
```

3. **SaveChangesAsync called:**
```csharp
[Insert]
public async Task Insert([Service] IDbContext db)
{
    // ...
    await db.SaveChangesAsync();  // Don't forget!
}
```

### Stale Data After Save / UI Not Updating

**Cause:** Not reassigning the return value from `Save()`.

**Why:** `Save()` serializes to server and returns a NEW instance. Original object is stale.

**Solution:**

```csharp
// WRONG
await personFactory.Save(person);
// person is now stale!

// CORRECT
person = await personFactory.Save(person);
// person is now the updated instance
```

### Child Entities Not Saving

**Check these:**

1. **Include DeletedList in Update:**
```csharp
[Update]
public void Update(ICollection<PhoneEntity> entities, [Service] IPhoneFactory factory)
{
    // CRITICAL: Union with DeletedList
    foreach (var phone in this.Union(DeletedList))
    {
        // process all items including deleted
    }
}
```

2. **Call child factory Save:**
```csharp
[Update]
public async Task Update([Service] IDbContext db, [Service] IPhoneListFactory phoneFactory)
{
    // ...
    phoneFactory.Save(PhoneList, entity.Phones);  // Explicitly save children
    await db.SaveChangesAsync();
}
```

## Remote/Serialization Issues

### Properties Not Transferring to Server

**Check these:**

1. **Properties are partial** (only partial properties serialize)

2. **[Remote] attribute present:**
```csharp
[Remote]  // Required for client-callable operations
[Fetch]
public async Task Fetch(int id, [Service] IDbContext db) { }
```

3. **Interface includes properties** (auto-generated from partial class)

### Connection Errors

**Check these:**

1. **Endpoint configured:**
```csharp
// Server
app.MapPost("/api/neatoo", (HttpContext ctx, RemoteRequestDto request, CancellationToken token) =>
{
    var handler = ctx.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
    return handler(request, token);
});

// Client
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
    new HttpClient { BaseAddress = new Uri("https://localhost:5001") });
```

2. **CORS configured (if needed):**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
        policy.WithOrigins("https://localhost:5002")
              .AllowAnyMethod()
              .AllowAnyHeader());
});
```

## Design Issues

### Feeling the Need to Cast to Concrete Types

**Symptoms:**
- Casting `IPerson` to `Person`
- Injecting child factories in Blazor components
- Bypassing `factory.Save()`

**Cause:** Interface is incomplete - not a Neatoo limitation.

**Solution:** Add needed methods to the interface:

```csharp
// WRONG - casting to access method
var concrete = (Visit)visit;
await concrete.Archive();

// CORRECT - add method to interface
public partial interface IVisit : IEntityBase
{
    Task<IVisit> Archive();  // Now available via interface
}

// Usage
visit = await visit.Archive();
```

## Diagnostic Code

Use this to debug entity state:

```csharp
void DiagnoseEntity(IEntityBase entity)
{
    Console.WriteLine($"Type: {entity.GetType().Name}");
    Console.WriteLine($"IsNew: {entity.IsNew}");
    Console.WriteLine($"IsModified: {entity.IsModified}");
    Console.WriteLine($"IsValid: {entity.IsValid}");
    Console.WriteLine($"IsBusy: {entity.IsBusy}");
    Console.WriteLine($"IsChild: {entity.IsChild}");
    Console.WriteLine($"IsDeleted: {entity.IsDeleted}");
    Console.WriteLine($"IsSavable: {entity.IsSavable}");

    if (!entity.IsValid)
    {
        Console.WriteLine("Validation Errors:");
        foreach (var msg in entity.PropertyMessages)
        {
            Console.WriteLine($"  {msg.Property.Name}: {msg.Message}");
        }
    }
}
```

## Quick Error Reference

| Error/Symptom | Likely Cause | Solution |
|---------------|--------------|----------|
| "Type not registered" | Missing `AddNeatooServices()` | Register assembly |
| Properties don't work | Non-partial class | Add `partial` keyword |
| `IsModified` always false | Non-partial property | Add `partial` keyword |
| Factory not found | Missing `[Factory]` | Add attribute to class |
| "Cannot save child" | `IsChild = true` | Save parent instead |
| Stale data after save | Not reassigning | `x = await Save(x)` |
| Validation only at save | Logic in factory method | Move to rule class |
| "Hint name too long" | Long namespace | Add `[assembly: FactoryHintNameLength(100)]` |
| NEATOO010 warning | Constructor assignment | Use `XProperty.LoadValue()` |
