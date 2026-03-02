# Static Factory Pattern

Use for stateless operations like commands and events. No instance state - pure functions with side effects via services.

## Execute Commands (Request-Response)

Use `[Execute]` for operations that return a result. The client awaits the response.

> **Note:** `[Execute]` also works on non-static `[Factory]` classes when the operation returns the containing type. See `references/class-factory.md` for that pattern.

<!-- snippet: skill-static-execute-commands -->
<a id='snippet-skill-static-execute-commands'></a>
```cs
[Factory]
public static partial class SkillEmployeeCommands
{
    [Remote, Execute]
    private static async Task<bool> _SendNotification(
        string recipient,
        string message,
        [Service] IEmailService service)
    {
        await service.SendAsync(recipient, "Notification", message);
        return true;
    }

    [Remote, Execute]
    private static async Task<SkillEmployeeSummary> _GetEmployeeSummary(
        Guid employeeId,
        [Service] IEmployeeRepository repo)
    {
        var employee = await repo.GetByIdAsync(employeeId);
        if (employee == null)
            return new SkillEmployeeSummary { Id = employeeId, Found = false };

        return new SkillEmployeeSummary
        {
            Id = employeeId,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Position = employee.Position,
            Found = true
        };
    }
}

public class SkillEmployeeSummary
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool Found { get; set; }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/StaticFactorySamples.cs#L7-L47' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-static-execute-commands' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generates**:
- `SkillEmployeeCommands.SendNotification(recipient, message)` delegate
- `SkillEmployeeCommands.GetEmployeeSummary(employeeId)` delegate

**Usage**:
```csharp
var success = await SkillEmployeeCommands.SendNotification("admin@example.com", "Hello!");
var summary = await SkillEmployeeCommands.GetEmployeeSummary(employeeId);
```

---

## Event Handlers (Fire-and-Forget)

Use `[Event]` for operations that run asynchronously. The caller doesn't wait for completion.

<!-- snippet: skill-static-event-handlers -->
<a id='snippet-skill-static-event-handlers'></a>
```cs
[Factory]
public static partial class SkillEmployeeEvents
{
    [Remote, Event]
    private static async Task _OnEmployeeCreated(
        Guid employeeId,
        string employeeName,
        [Service] IEmailService emailService,
        CancellationToken cancellationToken)
    {
        await emailService.SendAsync(
            "hr@company.com",
            "New Employee",
            $"Welcome {employeeName}!",
            cancellationToken);
    }

    [Remote, Event]
    private static async Task _OnPaymentReceived(
        Guid employeeId,
        decimal amount,
        [Service] IEmailService email,
        [Service] IAuditLogService audit,
        CancellationToken cancellationToken)
    {
        var message = string.Format(
            CultureInfo.InvariantCulture,
            "Payment of {0:C} received for employee {1}",
            amount,
            employeeId);
        await email.SendAsync(
            "payroll@company.com",
            "Payment Received",
            message,
            cancellationToken);
        await audit.LogAsync(
            "PaymentReceived",
            employeeId,
            "Employee",
            string.Format(CultureInfo.InvariantCulture, "Payment received: {0:C}", amount),
            cancellationToken);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/StaticFactorySamples.cs#L49-L93' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-static-event-handlers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generates**:
- `SkillEmployeeEvents.OnEmployeeCreatedEvent` delegate (note `Event` suffix)
- `SkillEmployeeEvents.OnPaymentReceivedEvent` delegate

**Usage**:
```csharp
// Fire-and-forget - don't await
_ = SkillEmployeeEvents.OnEmployeeCreatedEvent(employeeId, "John Doe");
_ = SkillEmployeeEvents.OnPaymentReceivedEvent(employeeId, 99.95m);
```

---

## Critical Rules

### Methods must be `private static` with underscore prefix

```csharp
// WRONG - conflicts with generated code
[Remote, Execute]
public static Task<bool> SendNotification(...) { }

// RIGHT - private with underscore
[Remote, Execute]
private static Task<bool> _SendNotification(...) { }
```

The generator creates the public method. Your code provides the private implementation.

### [Execute] must return `Task<T>`, not `Task`

```csharp
// WRONG - no return value
[Remote, Execute]
private static Task _DoSomething(...) { }

// RIGHT - returns a value
[Remote, Execute]
private static Task<bool> _DoSomething(...) { return Task.FromResult(true); }
```

The client needs something to await and confirm the operation completed.

### [Event] must have CancellationToken as final parameter

```csharp
// WRONG - missing CancellationToken
[Remote, Event]
private static Task _OnSomething(int id, [Service] IService svc) { }

// RIGHT - CancellationToken as final parameter
[Remote, Event]
private static Task _OnSomething(int id, [Service] IService svc, CancellationToken ct) { }
```

Events use `IHostApplicationLifetime.ApplicationStopping` for graceful shutdown.

---

## Graceful Shutdown with IEventTracker

Use `IEventTracker` to wait for pending fire-and-forget events to complete:

<!-- snippet: events-eventtracker-access -->
<a id='snippet-events-eventtracker-access'></a>
```cs
// IEventTracker registered by AddNeatooRemoteFactory/AddNeatooAspNetCore
public static class EventTrackerAccessSample
{
    public static void AccessEventTracker(IServiceProvider sp)
    {
        var tracker = sp.GetRequiredService<IEventTracker>();
        Console.WriteLine($"Pending: {tracker.PendingCount}");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L155-L165' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-access' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**ASP.NET Core Integration:**

RemoteFactory registers `EventTrackerHostedService` which:
- Waits for all pending events during application shutdown
- Uses `IHostApplicationLifetime.ApplicationStopping` as CancellationToken source

**Testing events:**

<!-- snippet: events-testing -->
<a id='snippet-events-testing'></a>
```cs
// Fire event delegate, wait via IEventTracker, assert side effects
public static class EventTestingPatternSample
{
    public static async Task TestWelcomeEmailEvent(IServiceProvider sp, IEventTracker tracker)
    {
        var sendEmail = sp.GetRequiredService<EmployeeBasicEvent.SendWelcomeEmailEvent>();
        InMemoryEmailService.Clear();
        _ = sendEmail(Guid.NewGuid(), "test@example.com");
        await tracker.WaitAllAsync();
        Assert.Single(InMemoryEmailService.GetSentEmails());
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/Events/EventTestingSamples.cs#L8-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

---

### Static classes must be `partial`

```csharp
// WRONG - missing partial
[Factory]
public static class Commands { }

// RIGHT
[Factory]
public static partial class Commands { }
```

---

## When to Use Static Factory

- **Stateless commands** - No instance state needed
- **Fire-and-forget events** - Side effects that don't block the caller
- **Request-response operations** - Clean function-style API
- **Cross-cutting operations** - Notifications, auditing, logging
