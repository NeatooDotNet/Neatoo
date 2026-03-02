# Advanced RemoteFactory Patterns

## Custom Authorization with [AuthorizeFactory\<T\>]

Define domain-specific authorization logic in a dedicated class:

<!-- snippet: authorization-interface -->
<a id='snippet-authorization-interface'></a>
```cs
// [AuthorizeFactory] declares which operations this method controls
[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
bool CanCreate();

[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
bool CanRead();

[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
bool CanWrite();
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L17-L27' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: authorization-implementation -->
<a id='snippet-authorization-implementation'></a>
```cs
// Inject services via constructor for authorization decisions
public bool CanCreate() => _userContext.IsAuthenticated;

public bool CanRead() => _userContext.IsAuthenticated;

public bool CanWrite() =>
    _userContext.IsInRole("HRManager") || _userContext.IsInRole("Admin");
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L42-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: authorization-apply -->
<a id='snippet-authorization-apply'></a>
```cs
// Apply authorization interface to entity - all operations check IEmployeeAuthorization
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class AuthorizedEmployeeEntity : IFactorySaveMeta
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L60-L65' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generated authorization methods:**
- `CanCreate()`, `CanFetch()`, `CanInsert()`, `CanUpdate()`, `CanDelete()` on factory interface
- Returns `Authorized<T>` result with `HasAccess` property

**Use `[AuthorizeFactory<T>]` when:**
- Authorization is domain-specific with complex rules
- You want testable auth without HTTP infrastructure
- Different operations have different authorization requirements

---

## Authorization with [AspAuthorize]

Apply ASP.NET Core policy-based authorization to factory operations:

<!-- snippet: authorization-policy-apply -->
<a id='snippet-authorization-policy-apply'></a>
```cs
// [AspAuthorize] applies ASP.NET Core policies to factory methods
[Remote, Fetch]
[AspAuthorize("RequireAuthenticated")]
public Task<bool> Fetch(Guid employeeId, CancellationToken ct = default)
{
    EmployeeId = employeeId;
    AnnualSalary = 75000m;
    return Task.FromResult(true);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L331-L341' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Server configuration:**

<!-- snippet: authorization-policy-config -->
<a id='snippet-authorization-policy-config'></a>
```cs
// Configure ASP.NET Core policies for [AspAuthorize] to reference
public static void ConfigureServices(IServiceCollection services) =>
    services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireHRManager", p => p.RequireRole("HRManager"));
        options.AddPolicy("RequireAuthenticated", p => p.RequireAuthenticatedUser());
    });
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Authorization/AuthorizationPolicyConfig.cs#L11-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Note:** Multiple [AspAuthorize] attributes use AND logic (all must pass).

---

## Correlation Context

Track operations across the client/server boundary for distributed tracing:

<!-- snippet: aspnetcore-correlation-id -->
<a id='snippet-aspnetcore-correlation-id'></a>
```cs
[Factory]
public partial class EmployeeWithCorrelation
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";

    [Create]
    public EmployeeWithCorrelation() => Id = Guid.NewGuid();

    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] ICorrelationContext correlationContext,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        // CorrelationId auto-populated from X-Correlation-Id header
        var correlationId = correlationContext.CorrelationId;

        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/AspNetCore/CorrelationIdSamples.cs#L7-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-correlation-id' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`ICorrelationContext` is automatically registered by `AddNeatooRemoteFactory` and `AddNeatooAspNetCore`. No manual DI registration is required.

---

## Entity Duality

An entity can be an aggregate root in one context and a child in another:

<!-- snippet: skill-entity-duality -->
<a id='snippet-skill-entity-duality'></a>
```cs
[Factory]
public partial class SkillDepartment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    // Aggregate root context - client entry point
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IDepartmentRepository repo, CancellationToken ct)
    {
        var data = await repo.GetByIdAsync(id, ct);
        if (data == null) return false;

        Id = data.Id;
        Name = data.Name;
        Code = data.Code;
        return true;
    }

    // Child context - called from Employee.Fetch on server
    [Fetch]  // No [Remote] - server-side only
    public void FetchAsChild(Guid id, string name, string code)
    {
        Id = id;
        Name = name;
        Code = code;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/EntityDualitySamples.cs#L6-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-duality' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Key insight:** [Remote] is about *how the method is called*, not *what the type is*.

---

## Value Objects

Value objects serialize correctly when they have public setters:

<!-- snippet: skill-value-object-factory -->
<a id='snippet-skill-value-object-factory'></a>
```cs
[Factory]
public partial record SkillMoney
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    [Create]
    public void Create(decimal amount, string currency = "USD")
    {
        Amount = amount;
        Currency = currency;
    }

    public static SkillMoney Zero => new() { Amount = 0, Currency = "USD" };

    public SkillMoney Add(SkillMoney other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return new SkillMoney { Amount = Amount + other.Amount, Currency = Currency };
    }

    public static SkillMoney operator +(SkillMoney a, SkillMoney b)
    {
        return a.Add(b);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/ValueObjectSamples.cs#L5-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-value-object-factory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Note:** Even record types need public setters for serialization.

---

## Complex Aggregate with Child Collections

<!-- snippet: skill-complex-aggregate -->
<a id='snippet-skill-complex-aggregate'></a>
```cs
[Factory]
public partial class SkillEmployeeWithAssignments : IFactorySaveMeta
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public SkillAssignmentList Assignments { get; set; } = null!;
    public decimal TotalHours => Assignments?.CalculateTotalHours() ?? 0;

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Remote, Create]
    public void Create(
        string firstName,
        string lastName,
        [Service] ISkillAssignmentListFactory assignmentListFactory)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Assignments = assignmentListFactory.Create();
        IsNew = true;
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] ISkillAssignmentListFactory assignmentListFactory,
        [Service] IEmployeeRepository repo,
        CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;

        // Fetch child collection with data
        var assignmentData = new List<(int, string, decimal, DateTime)>
        {
            (1, "Project Alpha", 40, DateTime.Today),
            (2, "Project Beta", 20, DateTime.Today.AddDays(7))
        };
#pragma warning disable CA2016 // Factory method does not accept CancellationToken
        Assignments = assignmentListFactory.Fetch(assignmentData);
#pragma warning restore CA2016
        IsNew = false;
        return true;
    }

    // Domain method - business logic in entity
    public void AddAssignment(string projectName, decimal hoursAllocated, DateTime startDate)
    {
        Assignments.AddAssignment(projectName, hoursAllocated, startDate);
    }
}

[Factory]
public partial class SkillAssignmentList : List<Assignment>
{
    private readonly IAssignmentFactory _assignmentFactory;

    [Create]
    public SkillAssignmentList([Service] IAssignmentFactory assignmentFactory)
    {
        _assignmentFactory = assignmentFactory;
    }

    [Fetch]
    public void Fetch(
        List<(int Id, string ProjectName, decimal Hours, DateTime StartDate)> data,
        [Service] IAssignmentFactory assignmentFactory)
    {
        foreach (var item in data)
        {
            var assignment = assignmentFactory.Fetch(
                item.Id, item.ProjectName, item.Hours, item.StartDate);
            Add(assignment);
        }
    }

    public void AddAssignment(string projectName, decimal hoursAllocated, DateTime startDate)
    {
        var assignment = _assignmentFactory.Create(projectName, hoursAllocated, startDate);
        Add(assignment);
    }

    public decimal CalculateTotalHours()
    {
        return this.Sum(a => a.HoursAllocated);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/ComplexAggregateSamples.cs#L6-L101' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-complex-aggregate' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Note:** Collection uses constructor injection (`[Service]` on constructor) so factory is available after round-trip.

---

## Child Collection Factory

<!-- snippet: collection-factory-basic -->
<a id='snippet-collection-factory-basic'></a>
```cs
// Constructor-injected child factory (survives serialization)
[Create]
public OrderLineList([Service] IOrderLineFactory lineFactory) => _lineFactory = lineFactory;

// Fetch populates collection from data
[Fetch]
public void Fetch(
    IEnumerable<(int id, string name, decimal price, int qty)> items,
    [Service] IOrderLineFactory lineFactory)
{
    foreach (var item in items)
        Add(lineFactory.Fetch(item.id, item.name, item.price, item.qty));
}

// Domain method uses stored factory to add children
public void AddLine(string name, decimal price, int qty) => Add(_lineFactory.Create(name, price, qty));
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Collections/OrderLineCollectionSamples.cs#L45-L62' title='Snippet source file'>snippet source</a> | <a href='#snippet-collection-factory-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

---

## Testing with ClientServerContainers

Validate serialization round-trips using the two DI container pattern:

<!-- snippet: clientserver-container-usage -->
<a id='snippet-clientserver-container-usage'></a>
```cs
[Fact]
public void Local_Create_WorksWithoutSerialization()
{
    var (server, client, local) = ClientServerContainers.Scopes();
    var factory = local.ServiceProvider.GetRequiredService<IEmployeeFactory>();
    var employee = factory.Create();  // Runs locally (Logical mode)
    Assert.NotNull(employee);
    server.Dispose(); client.Dispose(); local.Dispose();
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/ClientServerContainerSamples.cs#L63-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-clientserver-container-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Container types:**
- `client` - Simulates remote client (serializes through HTTP simulation)
- `server` - Direct server execution (no serialization)
- `local` - Single-tier mode (factory runs locally)

---

## NuGet Package Structure

When consuming RemoteFactory via NuGet:

```xml
<PackageReference Include="Neatoo.RemoteFactory" Version="x.y.z" />
<PackageReference Include="Neatoo.RemoteFactory.AspNetCore" Version="x.y.z" />
```

**Neatoo.RemoteFactory**: Core library + embedded source generator
**Neatoo.RemoteFactory.AspNetCore**: Server-side ASP.NET Core integration

---

## Framework Support

RemoteFactory supports:
- .NET 8.0 (LTS)
- .NET 9.0 (STS)
- .NET 10.0 (LTS)

All three frameworks are included in the NuGet packages.

---

## Generated Code Location

Generated code appears in:
```
obj/Debug/{tfm}/generated/Neatoo.Generator/Neatoo.Factory/
```

Files generated:
- `{Namespace}.{TypeName}Factory.g.cs` - Factory interface and implementation
- `{Namespace}.{TypeName}.Ordinal.g.cs` - Serialization implementation

---

## Assembly-Level Attributes

### [assembly: FactoryHintNameLength]

Limits generated file hint name length for Windows path limits:

<!-- snippet: attributes-factoryhintnamelength -->
<a id='snippet-attributes-factoryhintnamelength'></a>
```cs
// Increase hint name length to accommodate long namespace/type names
// Use when hitting Windows path length limits (260 characters)
[assembly: FactoryHintNameLength(100)]
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/AssemblyAttributes.cs#L3-L7' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factoryhintnamelength' title='Start of snippet'>anchor</a></sup>
<a id='snippet-attributes-factoryhintnamelength-1'></a>
```cs
// Limits generated file name length for Windows path limits
// [assembly: FactoryHintNameLength(100)]
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AssemblyAttributeSamples.cs#L13-L16' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factoryhintnamelength-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use when hitting Windows 260-character path limits with deeply nested namespaces or generic types.

---

## Attribute Inheritance Rules

| Attribute | Inherited | Notes |
|-----------|-----------|-------|
| `[Factory]` | Yes | Derived classes get factories |
| `[SuppressFactory]` | Yes | Stops inheritance chain |
| `[Remote]` | Yes | Derived methods are also remote |
| `[Create]`, `[Fetch]`, etc. | No | Must apply to each method |
| `[Service]` | No | Must apply to each parameter |
| `[AuthorizeFactory<T>]` | No | Must apply per class |
| `[AspAuthorize]` | No | Must apply per method |

**Example:**

<!-- snippet: attributes-inheritance -->
<a id='snippet-attributes-inheritance'></a>
```cs
[Factory]   // Inherited: Yes
public partial class BaseWithFactory
{
    [Create]    // Inherited: No
    public BaseWithFactory() { }

    [Remote, Fetch]  // [Remote] Inherited: Yes
    public Task<bool> Fetch(Guid id, [Service] IEmployeeRepository r, CancellationToken ct) => Task.FromResult(true);
}

public partial class DerivedEntity : BaseWithFactory
{
    // Inherits [Factory] and [Remote] from base
    // Does NOT inherit [Create] - must redeclare
    [Create]
    public DerivedEntity() : base() { }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs#L220-L238' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-inheritance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

---

## Troubleshooting

### "Type not registered" at runtime

The factory method wasn't registered. Check:
1. Is the assembly passed to `AddNeatooRemoteFactory()`?
2. Does the class have `[Factory]` attribute?
3. Does the method have appropriate operation attribute (`[Create]`, `[Fetch]`, etc.)?

### Serialization returns null properties

Properties need public setters. Change:
```csharp
public int Id { get; private set; }  // Won't serialize
```
To:
```csharp
public int Id { get; set; }  // Will serialize
```

### CS0260 compilation error

Missing `partial` keyword:
```csharp
[Factory]
public partial class MyEntity { }  // Add 'partial'
```

### Method-injected service is null

Services injected via method parameters are NOT serialized:
```csharp
// This field will be null after round-trip
private IService _service;

[Remote, Create]
public void Create([Service] IService service)
{
    _service = service;  // WRONG - will be null on client
}
```

Use constructor injection if you need the service after serialization:
```csharp
public MyEntity([Service] IService service)
{
    _service = service;  // Available on both sides
}
```

### N+1 remote calls performance issue

Child entities should NOT have `[Remote]`:
```csharp
// WRONG - each line causes a remote call
[Factory]
public partial class Assignment
{
    [Remote, Create]  // Remove [Remote]
    public void Create() { }
}
```
