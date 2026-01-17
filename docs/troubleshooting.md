# Troubleshooting Guide

This guide covers common issues and solutions when working with Neatoo.

## Source Generator Issues

### Generated Code Not Appearing

**Symptoms:**
- Factory not found
- Interface members not generated
- Mapper methods show as partial without implementation

**Solutions:**

1. **Check .NET version**
   ```xml
   <TargetFramework>net8.0</TargetFramework>
   ```
   Neatoo requires .NET 8.0 or later.

2. **Clean and rebuild**
   ```bash
   dotnet clean
   dotnet build
   ```

3. **Check for generator errors**
   - Visual Studio: View > Output > Build
   - Look for analyzer/generator warnings

4. **Verify class structure**
   ```csharp
   // All required elements:
   [Factory]                                    // Required attribute
   internal partial class Person                 // Must be partial
       : EntityBase<Person>, IPerson            // Inherit base, implement interface
   {
       public partial string? Name { get; set; } // Properties must be partial
   }
   ```

5. **Restart IDE**
   Sometimes the analyzer host needs a restart.

### Properties Not Working (Class Not Partial)

**Symptoms:**
- Properties return default values
- `IsModified` never becomes `true`
- Validation rules don't run
- No compile errors, but entity doesn't behave correctly

**Cause:** The entity class is missing the `partial` keyword. The source generator cannot extend non-partial classes.

**Solution:**

Add `partial` to the class declaration:

<!-- pseudo:partial-class-fix -->
```csharp
// Wrong - will compile but properties won't work
[Factory]
internal class Person : EntityBase<Person>, IPerson { }

// Correct
[Factory]
internal partial class Person : EntityBase<Person>, IPerson { }
```
<!-- /snippet -->

### Factory Hint Name Too Long

**Symptoms:**
- Build error mentioning hint name length or character limit
- Error occurs for types with long fully qualified names (namespace + class name > 50 characters)

**Cause:** RemoteFactory (v9.20.1+) enforces a 50-character default limit on fully qualified type names for generated source file naming.

**Solution:**

Add an assembly attribute to increase the limit:

<!-- pseudo:factory-hint-name-length -->
```csharp
// In AssemblyAttributes.cs or any .cs file in your project
[assembly: FactoryHintNameLength(100)]
```
<!-- /snippet -->

Typical values:
- `100` - suitable for most projects
- `150` - for deeply nested namespaces

**Note:** This is a RemoteFactory feature, not Neatoo itself. The limit ensures generated file names work across all operating systems.

### Factory Not Found at Runtime

**Symptoms:**
- `TypeNotRegisteredException` for factory
- DI fails to resolve `IPersonFactory`

**Solutions:**

1. **Register assembly with AddNeatooServices**
   ```csharp
   builder.Services.AddNeatooServices(
       NeatooFactory.Server,
       typeof(IPerson).Assembly);  // Include your domain model assembly
   ```

2. **Check interface is public**
   ```csharp
   public partial interface IPerson : IEntityBase { }  // Must be public
   ```

3. **Verify [Factory] attribute**
   ```csharp
   [Factory]  // Required for factory generation
   internal partial class Person : EntityBase<Person>, IPerson { }
   ```

### Analyzer Warnings

#### NEATOO010: Constructor Property Assignment

**Symptoms:**
- Warning NEATOO010 on property assignments in constructors
- Message: "Property 'X' should be assigned using LoadValue() or XProperty.LoadValue() instead of direct assignment in constructor"

**Why It Matters:**

Direct property assignment in constructors marks the entity as modified (`IsModified = true`), which causes incorrect behavior:
- Entity appears "dirty" immediately after creation
- May trigger unnecessary saves
- Rules execute during construction (before entity is fully initialized)

**Solution:**

Use `LoadValue()` instead of direct assignment:

<!-- pseudo:loadvalue-constructor-fix -->
```csharp
// Warning: Direct assignment marks entity as modified
public Person(IEntityBaseServices<Person> services) : base(services)
{
    Status = "Active";  // NEATOO010 warning
}

// Correct: LoadValue sets the value without triggering modification tracking
public Person(IEntityBaseServices<Person> services) : base(services)
{
    StatusProperty.LoadValue("Active");  // No warning
}
```
<!-- /snippet -->

## Validation Issues

### Validation Errors Only Appear After Clicking Save

**Symptoms:**
- Users don't see validation errors while editing
- Errors only show after attempting to save
- Exception thrown instead of inline validation message

**Cause:** Business validation logic is in factory methods (`[Insert]`/`[Update]`) instead of the rule system.

**Solution:** Move validation to `AsyncRuleBase<T>` with a Command:

<!-- invalid:validation-in-factory -->
```csharp
// WRONG - in factory method
[Insert]
public async Task Insert([Service] IUserRepository repo)
{
    await RunRules();
    if (!IsSavable) return;

    // DON'T DO THIS!
    if (await repo.EmailExistsAsync(Email))
        throw new InvalidOperationException("Email in use");
}

// RIGHT - in async rule
public class UniqueEmailRule : AsyncRuleBase<IUser>, IUniqueEmailRule
{
    protected override async Task<IRuleMessages> Execute(IUser target, ...)
    {
        if (await _checkUnique.EmailExists(target.Email, target.Id))
            return (nameof(target.Email), "Email in use").AsRuleMessages();
        return None;
    }
}
```
<!-- /snippet -->

See [Database-Dependent Validation](database-dependent-validation.md) for complete guidance.

### Rules Not Executing

**Symptoms:**
- Properties change but validation doesn't run
- `IsValid` stays true despite invalid data

**Solutions:**

1. **Check properties are partial**
   ```csharp
   public partial string? Email { get; set; }  // Correct
   public string? Email { get; set; }          // Wrong - no tracking
   ```

2. **Verify rules are registered**
   ```csharp
   public Person(IEntityBaseServices<Person> services,
                 IEmailValidationRule emailRule) : base(services)
   {
       RuleManager.AddRule(emailRule);  // Must add rule
   }
   ```

3. **Check DI registration**
   ```csharp
   builder.Services.AddScoped<IEmailValidationRule, EmailValidationRule>();
   ```

4. **Verify trigger properties match**
   ```csharp
   public class EmailRule : RuleBase<IPerson>
   {
       public EmailRule() : base(p => p.Email) { }  // Triggers on Email changes
   }
   ```

5. **Not paused**
   ```csharp
   // Rules don't run during pause
   if (person.IsPaused)
   {
       // Actions are paused
   }
   ```

### Async Rules Not Completing

**Symptoms:**
- `IsBusy` stays true
- Validation seems stuck
- Rule never returns result

**Solutions:**

1. **Check for deadlocks**
   ```csharp
   // Wrong - can deadlock in UI
   person.WaitForTasks().Wait();

   // Correct - use async/await
   await person.WaitForTasks();
   ```

2. **Verify cancellation token handling**
   ```csharp
   protected override async Task<IRuleMessages> Execute(
       IPerson target, CancellationToken? token = null)
   {
       // Check cancellation
       token?.ThrowIfCancellationRequested();

       // Use token in async calls
       await service.ValidateAsync(target.Email, token ?? CancellationToken.None);
   }
   ```

3. **Check service exceptions**
   ```csharp
   catch (Exception ex)
   {
       logger.LogError(ex, "Rule execution failed");
       // Return error message or rethrow
   }
   ```

### Validation Messages Not Showing

**Symptoms:**
- `IsValid` is false but no messages display
- UI doesn't show errors

**Solutions:**

1. **Check property messages**
   ```csharp
   foreach (var msg in person.PropertyMessages)
   {
       Console.WriteLine($"{msg.Property.Name}: {msg.Message}");
   }
   ```

2. **Verify rule returns messages**
   ```csharp
   protected override IRuleMessages Execute(IPerson target)
   {
       if (string.IsNullOrEmpty(target.Email))
       {
           // Must return messages, not just modify state
           return (nameof(target.Email), "Email is required").AsRuleMessages();
       }
       return None;  // Explicit return for valid
   }
   ```

3. **Use correct UI binding**
   ```razor
   <MudNeatooTextField T="string"
       EntityProperty="@person[nameof(IPerson.Email)]" />
   <!-- Shows validation automatically -->
   ```

## Serialization Issues

### Properties Not Transferring to Server

**Symptoms:**
- Server receives null values
- State lost during remote call

**Solutions:**

1. **Properties must be partial**
   ```csharp
   public partial string? Name { get; set; }  // Serialized
   public string? Calculated => ...;          // Not serialized
   ```

2. **Check [Remote] attribute**
   ```csharp
   [Remote]  // Required for client-callable operations
   [Fetch]
   public async Task Fetch(int id, [Service] IDbContext db) { }
   ```

3. **Verify interface includes properties**
   ```csharp
   public partial interface IPerson : IEntityBase
   {
       // Properties auto-generated from partial class
   }
   ```

### Circular Reference Errors

**Symptoms:**
- JSON serialization exception
- Stack overflow during serialize

**Solutions:**

1. **Check parent-child relationships**
   - Parent references child via property
   - Child accesses parent via `Parent` property (not a stored property)

2. **Use interface references**
   ```csharp
   // Child references parent via cast, not stored property
   public IPerson? ParentPerson => Parent as IPerson;
   ```

### Type Mismatch on Deserialization

**Symptoms:**
- `JsonException` during remote call
- Type not found errors

**Solutions:**

1. **Register all assemblies**
   ```csharp
   builder.Services.AddNeatooServices(
       NeatooFactory.Remote,
       typeof(IPerson).Assembly,
       typeof(IOrder).Assembly);  // All domain model assemblies
   ```

2. **Check type names match**
   - Server and client must share same type definitions
   - Use shared project for domain models

## Factory Operation Issues

### Save Not Persisting

**Symptoms:**
- `Save()` completes but database unchanged
- No errors but data not saved

**Solutions:**

1. **Check IsSavable before save**
   ```csharp
   if (!person.IsSavable)
   {
       // Won't save - check why
       Console.WriteLine($"IsModified: {person.IsModified}");
       Console.WriteLine($"IsValid: {person.IsValid}");
       Console.WriteLine($"IsBusy: {person.IsBusy}");
       Console.WriteLine($"IsChild: {person.IsChild}");
   }
   ```

2. **Verify factory methods exist**
   ```csharp
   [Remote]
   [Insert]
   public async Task Insert([Service] IDbContext db) { }

   [Remote]
   [Update]
   public async Task Update([Service] IDbContext db) { }
   ```

3. **Call SaveChangesAsync**
   ```csharp
   [Insert]
   public async Task Insert([Service] IDbContext db)
   {
       // ...
       await db.SaveChangesAsync();  // Don't forget this!
   }
   ```

4. **Capture return value**
   ```csharp
   // Correct - captures updated entity
   person = await personFactory.Save(person);

   // Wrong - loses updated state
   await personFactory.Save(person);
   ```

### Stale Data After Save / UI Not Updating

**Symptoms:**
- Database-generated ID is still empty/zero after save
- UI shows old values after save completes
- `IsModified` is still `true` when it should be `false`
- Navigation to `/{id}` routes fails with empty ID
- Subsequent saves fail with concurrency errors

**Cause:** You forgot to reassign the return value from `Save()`:

<!-- invalid:save-without-reassign -->
```csharp
// WRONG - person is now stale
await person.Save();
// person still has old state - it's the PRE-save instance

// CORRECT - person has new state
person = await person.Save();
// person is now the POST-save instance with updated values
```
<!-- /snippet -->

**Why This Happens:**

`Save()` uses the Remote Factory pattern which serializes your object to the server and deserializes a **new instance** back. The original object in memory is unchanged - it's a completely different object from what the server returns.

Think of it like mailing a document:
1. You write a document (your aggregate)
2. You mail it (serialize to server)
3. Someone adds information and mails it back (server persistence + serialize back)
4. You receive a **new document** (deserialized instance)
5. Your original draft is still on your desk unchanged (original object)

**Solution:**

Always capture the return value:

<!-- pseudo:save-with-reassign -->
```csharp
// In a Blazor component
this.Person = await this.Person.Save();

// In a service/handler
var savedPerson = await person.Save();
return savedPerson;

// Chained operations
person = await person.Save();
var id = person.Id;  // Now has the database-generated ID
```
<!-- /snippet -->

See [Factory Operations](factory-operations.md#critical-always-reassign-after-save) and [Blazor Binding](blazor-binding.md#critical-reassign-after-save-in-blazor-components) for more details.

### Child Entities Not Saving

**Symptoms:**
- Parent saves but children don't
- New child items missing from database

**Solutions:**

1. **Call child factory Save**
   ```csharp
   [Update]
   public async Task Update([Service] IDbContext db,
                            [Service] IPersonPhoneListFactory phoneFactory)
   {
       var entity = await db.Persons.FindAsync(Id);
       MapModifiedTo(entity);

       // Must explicitly save children
       phoneFactory.Save(PersonPhoneList, entity.Phones);

       await db.SaveChangesAsync();
   }
   ```

2. **Include DeletedList**
   ```csharp
   foreach (var phone in PersonPhoneList.Union(PersonPhoneList.DeletedList))
   {
       // Process all items including deleted
   }
   ```

### Fetch Returns Null

**Symptoms:**
- `Fetch()` returns null or empty entity
- Data exists in database but not loading

**Solutions:**

1. **Check entity exists**
   ```csharp
   [Fetch]
   public async Task<bool> Fetch(int id, [Service] IDbContext db)
   {
       var entity = await db.Persons.FindAsync(id);
       if (entity == null)
           return false;  // Indicate not found

       MapFrom(entity);
       return true;
   }
   ```

2. **Include related data**
   ```csharp
   var entity = await db.Persons
       .Include(p => p.Phones)  // Include children
       .FirstOrDefaultAsync(p => p.Id == id);
   ```

## UI Binding Issues

### Changes Not Reflecting in UI

**Symptoms:**
- Property changes but UI doesn't update
- Bound controls show stale data

**Solutions:**

1. **Subscribe to PropertyChanged**
   ```razor
   @implements IDisposable

   @code {
       protected override void OnInitialized()
       {
           person.PropertyChanged += OnPropertyChanged;
       }

       private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
       {
           InvokeAsync(StateHasChanged);
       }

       public void Dispose()
       {
           person.PropertyChanged -= OnPropertyChanged;
       }
   }
   ```

2. **Use MudNeatoo components**
   ```razor
   <!-- Auto-handles change notifications -->
   <MudNeatooTextField T="string"
       EntityProperty="@person[nameof(IPerson.Name)]" />
   ```

### Save Button Always Disabled

**Symptoms:**
- `IsSavable` is false
- Can't save even with valid changes

**Solutions:**

1. **Wait for async operations**
   ```csharp
   await person.WaitForTasks();
   // Now check IsSavable
   ```

2. **Check all conditions**
   ```csharp
   // IsSavable requires ALL:
   // - IsModified = true
   // - IsValid = true
   // - IsBusy = false
   // - IsChild = false
   ```

3. **Debug state**
   ```razor
   <MudText>Modified: @person.IsModified</MudText>
   <MudText>Valid: @person.IsValid</MudText>
   <MudText>Busy: @person.IsBusy</MudText>
   <MudText>Child: @person.IsChild</MudText>
   <MudText>Savable: @person.IsSavable</MudText>
   ```

## Remote Factory Issues

### Connection Refused

**Symptoms:**
- HTTP connection errors
- Can't reach server endpoint

**Solutions:**

1. **Check endpoint configuration**
   ```csharp
   // Server
   app.MapPost("/api/neatoo", async (HttpContext ctx, RemoteRequestDto request) =>
       await NeatooEndpoint.HandleRequest(ctx, request));

   // Client
   builder.Services.AddScoped(sp => new HttpClient
   {
       BaseAddress = new Uri("https://localhost:5001")
   });
   ```

2. **Verify CORS if needed**
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowBlazor", policy =>
       {
           policy.WithOrigins("https://localhost:5002")
                 .AllowAnyMethod()
                 .AllowAnyHeader();
       });
   });
   ```

### Authorization Denied

**Symptoms:**
- Operations fail with authorization error
- `CanCreate()` returns false

**Solutions:**

1. **Check authorization implementation**
   ```csharp
   public class PersonAuth : IPersonAuth
   {
       private readonly ICurrentUser _user;

       public bool CanCreate() => _user.HasPermission("Person.Create");
   }
   ```

2. **Register authorization service**
   ```csharp
   builder.Services.AddScoped<IPersonAuth, PersonAuth>();
   ```

3. **Verify user context on server**
   ```csharp
   // Ensure authentication middleware runs before Neatoo endpoint
   app.UseAuthentication();
   app.UseAuthorization();
   app.MapPost("/api/neatoo", ...);
   ```

## Design Issues

### Feeling the Need to Cast to Concrete Types

**Symptoms:**
- You're casting `IPerson` to `Person` to access methods
- You're injecting child factories outside the aggregate
- You're bypassing `factory.Save()` with internal methods

**Cause:** This indicates a design issue, not a Neatoo limitation.

**Solutions:**

| What You're Doing | Why It's Wrong | Correct Approach |
|-------------------|----------------|------------------|
| Casting to call a method | Interface is incomplete | Add method to interface |
| Injecting child factories | Bypassing aggregate | Use parent's `AddItem()` method |
| Calling internal save methods | Avoiding factory pattern | Use `factory.Save()` or `entity.Save()` |
| Storing concrete types | Breaks serialization | Always use interface types |

**The interface is your public API.** If consuming code needs a method, add it to the interface:

<!-- pseudo:interface-method-addition -->
```csharp
// Before - casting to access method
var concrete = (Visit)visit;
await concrete.Archive();

// After - method on interface
public partial interface IVisit : IEntityBase
{
    Task<IVisit> Archive();
}

// Usage
visit = await visit.Archive();
```
<!-- /snippet -->

See [Aggregates and Entities](aggregates-and-entities.md#why-interfaces-are-required-not-optional) for complete guidance.

## Quick Reference

### Common Error Messages

| Error | Likely Cause | Solution |
|-------|--------------|----------|
| "Type not registered" | Missing `AddNeatooServices()` | Register assembly |
| "Rule must be added" | Rule not in RuleManager | Add in constructor |
| "Cannot save child" | IsChild = true | Save parent instead |
| "Object is not valid" | Validation errors | Check PropertyMessages |
| "Factory method not found" | Missing [Insert]/[Update] | Add factory methods |
| "Property not found" | Non-partial property | Make property partial |
| Properties don't work | Non-partial class | Add `partial` to class declaration |
| "Hint name too long" | Type name > 50 chars | Add `[assembly: FactoryHintNameLength(100)]` |
| NEATOO010 | Property assigned in constructor | Use `XProperty.LoadValue()` instead |

### Diagnostic Code

<!-- pseudo:diagnose-entity -->
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
<!-- /snippet -->

## See Also

- [Database-Dependent Validation](database-dependent-validation.md) - Async validation pattern
- [Exceptions](exceptions.md) - Exception handling guide
- [Installation](installation.md) - Setup and configuration
- [Factory Operations](factory-operations.md) - Factory lifecycle
- [Validation and Rules](validation-and-rules.md) - Rule implementation
