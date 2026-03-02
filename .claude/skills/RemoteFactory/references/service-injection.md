# Service Injection

RemoteFactory supports two injection patterns with different scopes.

## Constructor Injection (Client + Server)

Services injected via constructor are available on **both client and server**.

<!-- snippet: service-injection-constructor -->
<a id='snippet-service-injection-constructor'></a>
```cs
// Constructor injection - service available on BOTH client and server
[Create]
public EmployeeCompensation([Service] ISalaryCalculator calculator)
{
    _calculator = calculator;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L161-L168' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-constructor' title='Start of snippet'>anchor</a></sup>
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
// Server-only service - [Remote] ensures execution on server where IEmployeeDatabase exists
[Remote, Fetch]
public async Task Fetch(string query, [Service] IEmployeeDatabase database)
{
    QueryResult = await database.ExecuteQueryAsync(query);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L66-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-server-only' title='Start of snippet'>anchor</a></sup>
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
