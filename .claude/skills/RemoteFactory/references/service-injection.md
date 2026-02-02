# Service Injection

RemoteFactory supports two injection patterns with different scopes.

## Constructor Injection (Client + Server)

Services injected via constructor are available on **both client and server**.

<!-- snippet: service-injection-constructor -->
<a id='snippet-service-injection-constructor'></a>
```cs
/// <summary>
/// Service for calculating employee salary.
/// </summary>
public interface ISalaryCalculator
{
    decimal Calculate(decimal baseSalary, decimal bonus);
}

/// <summary>
/// Simple salary calculator implementation.
/// </summary>
public class SalaryCalculator : ISalaryCalculator
{
    public decimal Calculate(decimal baseSalary, decimal bonus)
    {
        return baseSalary + bonus;
    }
}

/// <summary>
/// Employee compensation demonstrating constructor service injection.
/// </summary>
[Factory]
public partial class EmployeeCompensation
{
    private readonly ISalaryCalculator _calculator;

    public decimal TotalCompensation { get; private set; }

    /// <summary>
    /// Constructor with service injection.
    /// ISalaryCalculator is resolved from DI when the factory creates the instance.
    /// </summary>
    [Create]
    public EmployeeCompensation([Service] ISalaryCalculator calculator)
    {
        _calculator = calculator;
    }

    public void CalculateTotal(decimal baseSalary, decimal bonus)
    {
        TotalCompensation = _calculator.Calculate(baseSalary, bonus);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L166-L211' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-constructor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Use constructor injection when:**
- Service is needed on both client and server
- Service must survive serialization round-trip
- Examples: ILogger, IValidator (client-side validation)

---

## Method Injection (Server Only)

Services injected via method parameters are **server-only**.

<!-- snippet: service-injection-server-only -->
<a id='snippet-service-injection-server-only'></a>
```cs
/// <summary>
/// Interface for database access (server-only service).
/// </summary>
public interface IEmployeeDatabase
{
    Task<string> ExecuteQueryAsync(string query);
}

/// <summary>
/// Simple implementation for demonstration.
/// </summary>
public class EmployeeDatabase : IEmployeeDatabase
{
    public Task<string> ExecuteQueryAsync(string query)
    {
        // Simulated query execution
        return Task.FromResult($"Query result for: {query}");
    }
}

/// <summary>
/// Employee report demonstrating server-only service injection.
/// </summary>
[Factory]
public partial class ServiceEmployeeReport
{
    public string QueryResult { get; private set; } = "";

    [Create]
    public ServiceEmployeeReport()
    {
    }

    /// <summary>
    /// Fetches report data from the database.
    /// </summary>
    /// <remarks>
    /// This service only exists on the server - [Remote] ensures the method runs there.
    /// </remarks>
    [Remote, Fetch]
    public async Task Fetch(string query, [Service] IEmployeeDatabase database)
    {
        QueryResult = await database.ExecuteQueryAsync(query);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L41-L87' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-server-only' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Use method injection when:**
- Service runs only on server (most common case)
- Service handles data access, external APIs, etc.
- Examples: IRepository, IEmailService, IPaymentGateway

### Critical Rule: Don't Store Method-Injected Services

```csharp
// WRONG - field will be null after round-trip
private IMyService _service;

[Remote, Create]
public void Create([Service] IMyService service)
{
    _service = service;  // Lost after serialization!
}

public void DoSomething()
{
    _service.Execute();  // NullReferenceException on client!
}
```

```csharp
// RIGHT - use immediately, don't store
[Remote, Create]
public void Create([Service] IMyService service)
{
    service.DoSomething();  // Use it here
}
```

---

## Child Entities (No [Remote])

Child entities within an aggregate do NOT need `[Remote]`:

<!-- snippet: skill-child-entity-no-remote -->
<a id='snippet-skill-child-entity-no-remote'></a>
```cs
[Factory]
public partial class Assignment
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal HoursAllocated { get; set; }
    public DateTime StartDate { get; set; }

    [Create]  // No [Remote] - called from server-side Employee operations
    public void Create(string projectName, decimal hoursAllocated, DateTime startDate)
    {
        ProjectName = projectName;
        HoursAllocated = hoursAllocated;
        StartDate = startDate;
    }

    [Fetch]  // No [Remote]
    public void Fetch(int id, string projectName, decimal hoursAllocated, DateTime startDate)
    {
        Id = id;
        ProjectName = projectName;
        HoursAllocated = hoursAllocated;
        StartDate = startDate;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/ChildEntitySamples.cs#L5-L31' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-entity-no-remote' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Why No [Remote] on Children?

`[Remote]` marks entry points from client to server. Once execution is on the server, subsequent calls stay there.

```csharp
// In Employee.Create (already on server):
[Remote, Create]
public void Create(string firstName, [Service] IAssignmentFactory assignmentFactory)
{
    FirstName = firstName;

    // assignmentFactory.Create() is server-side - no network call
    Assignments.Add(assignmentFactory.Create("Project Alpha", 40, DateTime.Today));
    Assignments.Add(assignmentFactory.Create("Project Beta", 20, DateTime.Today.AddDays(7)));
}
```

### The N+1 Problem

Adding `[Remote]` to child entities causes N+1 remote calls:

```csharp
// WRONG - 10 assignments = 10 HTTP calls!
[Factory]
public partial class Assignment
{
    [Remote, Create]  // DON'T DO THIS
    public void Create(string name) { }
}
```

```csharp
// RIGHT - all children created in single server call
[Factory]
public partial class Assignment
{
    [Create]  // No [Remote]
    public void Create(string name) { }
}
```

---

## Service Registration

### Server (ASP.NET Core)

```csharp
// Program.cs
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
```

### Client (Blazor WASM)

Only register services needed on client:

```csharp
// Program.cs
builder.Services.AddSingleton<ILogger, ConsoleLogger>();
// Don't register server-only services
```
