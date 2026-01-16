# Neatoo Quick Reference

One-page cheatsheet for Neatoo patterns.

## Entity Structure

```csharp
public partial interface IOrder : IEntityBase
{
    Guid? Id { get; }
    string? CustomerName { get; set; }
}

[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    public partial Guid? Id { get; set; }
    public partial string? CustomerName { get; set; }

    [Create]
    public void Create() { Id = Guid.NewGuid(); }

    [Remote][Fetch]
    public void Fetch(Guid id, [Service] IDbContext db) { /* load from db */ }

    [Remote][Insert]
    public async Task Insert([Service] IDbContext db) { /* persist new */ }

    [Remote][Update]
    public async Task Update([Service] IDbContext db) { /* persist changes */ }

    [Remote][Delete]
    public async Task Delete([Service] IDbContext db) { /* remove */ }
}
```

## Base Class Selection

| Base Class | Use For | Key Features |
|------------|---------|--------------|
| `EntityBase<T>` | Persisted entities | IsNew, IsModified, IsDeleted, Save() |
| `ValidateBase<T>` | Non-persisted (criteria, DTOs) | IsValid, rules, no persistence tracking |
| `EntityListBase<I>` | Child collections | DeletedList, parent-child relationships |

## Factory Attributes

| Attribute | Where | Purpose |
|-----------|-------|---------|
| `[Factory]` | Class | Generate factory interface and implementation |
| `[Create]` | Method | Initialize new entity (runs on client) |
| `[Fetch]` | Method | Load from database |
| `[Insert]` | Method | Persist new entity |
| `[Update]` | Method | Persist changes |
| `[Delete]` | Method | Remove entity |
| `[Remote]` | Method | Execute on server (required for Fetch/Insert/Update/Delete in Blazor) |
| `[Service]` | Parameter | Inject dependency |

## Meta-Properties

| Property | Type | Meaning |
|----------|------|---------|
| `IsValid` | bool | All validation passes (self + children) |
| `IsSelfValid` | bool | This entity's validation passes |
| `IsModified` | bool | Has changes (self or children) |
| `IsSelfModified` | bool | This entity has changes |
| `IsBusy` | bool | Async operations running |
| `IsNew` | bool | Not yet persisted |
| `IsDeleted` | bool | Marked for deletion |
| `IsSavable` | bool | `IsModified && IsValid && !IsBusy && !IsChild` |
| `IsChild` | bool | Part of parent aggregate |

## Property-Level Access

```csharp
var prop = entity[nameof(entity.Name)];
prop.IsModified     // Was this property changed?
prop.IsBusy         // Async rule running on this property?
prop.DisplayName    // UI-friendly label from [DisplayName]
prop.PropertyMessages  // Validation messages for this property
prop.LoadValue(x)   // Set value without triggering rules
```

## Validation Attributes

```csharp
[Required]
[Required(ErrorMessage = "Name is required")]

[StringLength(100)]
[StringLength(100, MinimumLength = 2)]

[Range(0, 100)]
[Range(typeof(decimal), "0.01", "999.99")]

[EmailAddress]
[RegularExpression(@"^\d{3}-\d{4}$")]
```

## Custom Rules

### Sync Rule
```csharp
public class AgeRule : RuleBase<IPerson>
{
    public AgeRule() : base(p => p.Age) { }  // Trigger property

    protected override IRuleMessages Execute(IPerson target)
    {
        if (target.Age < 0)
            return (nameof(target.Age), "Age cannot be negative").AsRuleMessages();
        return None;
    }
}
```

### Async Rule
```csharp
public class UniqueEmailRule : AsyncRuleBase<IPerson>
{
    private readonly IEmailChecker _checker;

    public UniqueEmailRule(IEmailChecker checker) : base(p => p.Email)
    {
        _checker = checker;
    }

    protected override async Task<IRuleMessages> Execute(IPerson target, CancellationToken? token = null)
    {
        if (!target[nameof(target.Email)].IsModified) return None;  // Skip if unchanged
        if (await _checker.EmailExists(target.Email))
            return (nameof(target.Email), "Email in use").AsRuleMessages();
        return None;
    }
}
```

### Register Rules
```csharp
public Person(IEntityBaseServices<Person> services, IAgeRule ageRule) : base(services)
{
    RuleManager.AddRule(ageRule);

    // Or inline:
    RuleManager.AddValidation(
        t => string.IsNullOrEmpty(t.Name) ? "Name required" : "",
        t => t.Name);
}
```

## Save Pattern

```csharp
// Always reassign!
person = await personFactory.Save(person);

// Or via entity:
person = (IPerson)await person.Save();
```

## Child Collections

```csharp
// List interface
public interface IPhoneList : IEntityListBase<IPhone>
{
    IPhone AddPhone();
}

// List implementation
[Factory]
internal class PhoneList : EntityListBase<IPhone>, IPhoneList
{
    private readonly IPhoneFactory _phoneFactory;

    public PhoneList([Service] IPhoneFactory phoneFactory)
    {
        _phoneFactory = phoneFactory;
    }

    public IPhone AddPhone()
    {
        var phone = _phoneFactory.Create();
        Add(phone);  // Sets parent, marks as child
        return phone;
    }

    [Update]
    public void Update(ICollection<PhoneEntity> entities, [Service] IPhoneFactory phoneFactory)
    {
        // CRITICAL: Include DeletedList
        foreach (var phone in this.Union(DeletedList))
        {
            // Handle insert/update/delete
        }
    }
}
```

## DI Registration

```csharp
// Server
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IPerson).Assembly);

// Client (Blazor WASM)
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IPerson).Assembly);
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
    new HttpClient { BaseAddress = new Uri("https://localhost:5001") });

// Endpoint (Server)
app.MapPost("/api/neatoo", (HttpContext ctx, RemoteRequestDto request, CancellationToken token) =>
{
    var handler = ctx.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
    return handler(request, token);
});
```

## Blazor Binding

```razor
@inject IOrderFactory OrderFactory

<MudNeatooTextField T="string" EntityProperty="@order[nameof(IOrder.CustomerName)]" />
<MudButton Disabled="@(!order.IsSavable)" OnClick="@Save">Save</MudButton>

@if (order.IsBusy) { <MudProgressCircular Indeterminate="true" /> }

@code {
    private IOrder order = default!;

    protected override void OnInitialized() => order = OrderFactory.Create();

    private async Task Save() => order = await OrderFactory.Save(order);
}
```
