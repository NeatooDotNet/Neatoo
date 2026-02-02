# Advanced RemoteFactory Patterns

## Custom Authorization with [AuthorizeFactory\<T\>]

Define domain-specific authorization logic in a dedicated class:

<!-- snippet: authorization-interface -->
<a id='snippet-authorization-interface'></a>
```cs
/// <summary>
/// Authorization interface for Employee operations.
/// Methods with [AuthorizeFactory] control access to specific operations.
/// </summary>
public interface IEmployeeAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L11-L27' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: authorization-implementation -->
<a id='snippet-authorization-implementation'></a>
```cs
/// <summary>
/// Authorization rules for Employee operations with realistic HR domain logic.
/// </summary>
public partial class EmployeeAuthorizationImpl : IEmployeeAuthorization
{
    private readonly IUserContext _userContext;

    public EmployeeAuthorizationImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanCreate()
    {
        // Only authenticated users can create employees
        return _userContext.IsAuthenticated;
    }

    public bool CanRead()
    {
        // Only authenticated users can view employee data
        return _userContext.IsAuthenticated;
    }

    public bool CanWrite()
    {
        // Only HRManager or Admin can modify employee records
        return _userContext.IsInRole("HRManager") || _userContext.IsInRole("Admin");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L29-L60' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: authorization-apply -->
<a id='snippet-authorization-apply'></a>
```cs
/// <summary>
/// Employee aggregate with authorization applied via AuthorizeFactory attribute.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class AuthorizedEmployeeEntity : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public AuthorizedEmployeeEntity()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,
        [Service] IEmployeeRepository repository,
        CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        DepartmentId = entity.DepartmentId;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            DepartmentId = DepartmentId,
            HireDate = DateTime.UtcNow
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            DepartmentId = DepartmentId
        };
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L66-L145' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-apply' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Salary information with policy-based authorization for sensitive data.
/// </summary>
[Factory]
public partial class SalaryInfo
{
    public Guid EmployeeId { get; private set; }
    public decimal AnnualSalary { get; set; }
    public DateTime EffectiveDate { get; set; }

    [Create]
    public SalaryInfo()
    {
        EmployeeId = Guid.NewGuid();
        EffectiveDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Basic salary fetch - requires authenticated user.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]
    public Task<bool> Fetch(Guid employeeId, CancellationToken ct = default)
    {
        EmployeeId = employeeId;
        AnnualSalary = 75000m;
        EffectiveDate = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    /// <summary>
    /// Fetch with full compensation details - requires Payroll access.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize("RequirePayroll")]
    public Task<bool> FetchWithCompensation(Guid employeeId, decimal bonusAmount, CancellationToken ct = default)
    {
        EmployeeId = employeeId;
        AnnualSalary = 75000m + bonusAmount;
        EffectiveDate = DateTime.UtcNow;
        return Task.FromResult(true);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L331-L375' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-apply' title='Start of snippet'>anchor</a></sup>
<a id='snippet-authorization-policy-apply-1'></a>
```cs
/// <summary>
/// Applying ASP.NET Core policies with [AspAuthorize].
/// </summary>
[Factory]
public partial class PolicyProtectedEmployee2 : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public PolicyProtectedEmployee2() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [AspAuthorize] with named policy via constructor.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repo,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    [AspAuthorize("RequireManagerOrHR")]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToUpperInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L42-L93' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-apply-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Server configuration:**

<!-- snippet: authorization-policy-config -->
<a id='snippet-authorization-policy-config'></a>
```cs
/// <summary>
/// ASP.NET Core authorization policy configuration for HR domain.
/// </summary>
public static class AuthorizationPolicyConfig
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // HR Manager policy - only HR managers can access
            options.AddPolicy("RequireHRManager", policy =>
                policy.RequireRole("HRManager"));

            // Payroll policy - payroll staff or HR managers can access
            options.AddPolicy("RequirePayroll", policy =>
                policy.RequireRole("Payroll", "HRManager"));

            // Authenticated policy - any authenticated user can access
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());
        });
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Authorization/AuthorizationPolicyConfig.cs#L6-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-config' title='Start of snippet'>anchor</a></sup>
<a id='snippet-authorization-policy-config-1'></a>
```cs
/// <summary>
/// ASP.NET Core authorization policy configuration.
/// </summary>
public static class AuthorizationPolicyConfigSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Policy requiring authentication
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());

            // Policy requiring HR role
            options.AddPolicy("RequireHR", policy =>
                policy.RequireRole("HR"));

            // Policy requiring Manager or HR role
            options.AddPolicy("RequireManagerOrHR", policy =>
                policy.RequireRole("Manager", "HR"));

            // Custom policy with claim requirements
            options.AddPolicy("RequireEmployeeAccess", policy =>
                policy.RequireClaim("department")
                      .RequireAuthenticatedUser());
        });
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L11-L40' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-config-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Note:** Multiple [AspAuthorize] attributes use AND logic (all must pass).

---

## Correlation Context

Track operations across the client/server boundary for distributed tracing:

<!-- snippet: aspnetcore-correlation-id -->
<a id='snippet-aspnetcore-correlation-id'></a>
```cs
/// <summary>
/// Employee with correlation ID support for distributed tracing.
/// </summary>
[Factory]
public partial class EmployeeWithCorrelation
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public EmailAddress Email { get; set; } = null!;

    [Create]
    public EmployeeWithCorrelation()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an Employee and logs the access with correlation ID for tracing.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // CorrelationId is auto-populated from X-Correlation-Id header
        var correlationId = CorrelationContext.CorrelationId;

        var entity = await repository.GetByIdAsync(id, ct);

        if (entity == null)
            return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = new EmailAddress(entity.Email);

        // Log with correlation ID for distributed tracing
        await auditLog.LogAsync(
            action: "Fetch",
            entityId: Id,
            entityType: nameof(EmployeeWithCorrelation),
            details: $"Fetched by correlation: {correlationId}",
            ct);

        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/AspNetCore/CorrelationIdSamples.cs#L8-L60' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-correlation-id' title='Start of snippet'>anchor</a></sup>
<a id='snippet-aspnetcore-correlation-id-1'></a>
```cs
/// <summary>
/// Factory method accessing correlation ID for distributed tracing.
/// </summary>
[Factory]
public partial class EmployeeCorrelationDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeCorrelationDemo() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Access correlation ID via static CorrelationContext.
    /// Note: This API may be redesigned to support DI in a future version.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        [Service] ILogger<EmployeeCorrelationDemo> logger,
        CancellationToken ct)
    {
        // Access correlation ID from static context
        var correlationId = CorrelationContext.CorrelationId;

        // Include in structured logs
        logger.LogInformation(
            "Fetching employee {EmployeeId} with correlation {CorrelationId}",
            id, correlationId);

        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L159-L202' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-correlation-id-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Register in DI:**
```csharp
builder.Services.AddScoped<ICorrelationContext, HttpCorrelationContext>();
```

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
/// <summary>
/// Collection of OrderLines within an Order aggregate.
/// </summary>
[Factory]
public partial class OrderLineList : List<OrderLine>
{
    private readonly IOrderLineFactory _lineFactory;

    /// <summary>
    /// Creates an empty collection with injected child factory.
    /// </summary>
    [Create]
    public OrderLineList([Service] IOrderLineFactory lineFactory)
    {
        _lineFactory = lineFactory;
    }

    /// <summary>
    /// Fetches a collection from data.
    /// </summary>
    [Fetch]
    public void Fetch(
        IEnumerable<(int id, string name, decimal price, int qty)> items,
        [Service] IOrderLineFactory lineFactory)
    {
        foreach (var item in items)
        {
            Add(lineFactory.Fetch(item.id, item.name, item.price, item.qty));
        }
    }

    /// <summary>
    /// Domain method using stored factory to add children.
    /// </summary>
    public void AddLine(string name, decimal price, int qty)
    {
        var line = _lineFactory.Create(name, price, qty);
        Add(line);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Collections/OrderLineCollectionSamples.cs#L43-L84' title='Snippet source file'>snippet source</a> | <a href='#snippet-collection-factory-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

---

## Testing with ClientServerContainers

Validate serialization round-trips using the two DI container pattern:

<!-- snippet: clientserver-container-usage -->
<a id='snippet-clientserver-container-usage'></a>
```cs
/// <summary>
/// Example tests using the ClientServerContainers pattern.
/// </summary>
public class ClientServerContainerTests
{
    [Fact]
    public void Local_Create_WorksWithoutSerialization()
    {
        var (server, client, local) = ClientServerContainers.Scopes();

        // Get factory from local container - no serialization
        var factory = local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create runs entirely locally (Logical mode)
        var employee = factory.Create();

        Assert.NotNull(employee);
        Assert.NotEqual(Guid.Empty, employee.Id);
        Assert.True(employee.IsNew);

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/ClientServerContainerSamples.cs#L68-L94' title='Snippet source file'>snippet source</a> | <a href='#snippet-clientserver-container-usage' title='Start of snippet'>anchor</a></sup>
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
// Assembly-level hint name length configuration:
//
// [assembly: FactoryHintNameLength(100)]
// - Limits generated file hint name length to 100 characters
// - Use when hitting Windows 260-character path limits
// - Value is maximum characters for the generated file hint name
//
// Default behavior uses full type names which can be long
// for deeply nested namespaces or generic types.
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AssemblyAttributeSamples.cs#L20-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factoryhintnamelength-1' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates attribute inheritance behavior.
/// </summary>
[Factory]   // Inherited: Yes
public partial class BaseEntityWithFactory
{
    public Guid Id { get; protected set; }
    public string Name { get; set; } = "";

    [Create]    // Inherited: No - must be redeclared
    public BaseEntityWithFactory()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]  // [Remote] Inherited: Yes
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        Name = entity.FirstName;
        return true;
    }
}

/// <summary>
/// Derived class inherits [Factory] and [Remote] but not [Create].
/// </summary>
public partial class DerivedWithInheritedFactory : BaseEntityWithFactory
{
    public string DerivedProperty { get; set; } = "";

    // Inherits: [Factory] from base
    // Inherits: [Remote] from base.Fetch()
    // Does NOT inherit: [Create] - must redeclare if needed

    [Create]  // Must redeclare for this class to have a Create
    public DerivedWithInheritedFactory() : base()
    {
        DerivedProperty = "Default";
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L693-L737' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-inheritance' title='Start of snippet'>anchor</a></sup>
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
