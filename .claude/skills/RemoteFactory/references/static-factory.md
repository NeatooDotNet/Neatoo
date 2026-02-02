# Static Factory Pattern

Use for stateless operations like commands and events. No instance state - pure functions with side effects via services.

## Execute Commands (Request-Response)

Use `[Execute]` for operations that return a result. The client awaits the response.

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
/// <summary>
/// Demonstrates IEventTracker singleton access and basic usage.
/// </summary>
public class EventTrackerAccessDemo
{
    private readonly IEventTracker _eventTracker;
    private readonly EmployeeBasicEvent.SendWelcomeEmailEvent _sendWelcomeEmail;

    public EventTrackerAccessDemo(
        IEventTracker eventTracker,
        EmployeeBasicEvent.SendWelcomeEmailEvent sendWelcomeEmail)
    {
        // IEventTracker is a singleton registered by AddNeatooAspNetCore
        _eventTracker = eventTracker;
        _sendWelcomeEmail = sendWelcomeEmail;
    }

    public async Task DemonstrateEventTrackerAsync()
    {
        // Fire multiple events
        _ = _sendWelcomeEmail(Guid.NewGuid(), "employee1@company.com");
        _ = _sendWelcomeEmail(Guid.NewGuid(), "employee2@company.com");
        _ = _sendWelcomeEmail(Guid.NewGuid(), "employee3@company.com");

        // Check pending count (may be 0 if events complete quickly)
        var pendingCount = _eventTracker.PendingCount;
        Console.WriteLine($"Pending events: {pendingCount}");

        // Wait for all events to complete
        await _eventTracker.WaitAllAsync();

        // Verify all events completed
        if (_eventTracker.PendingCount != 0)
        {
            throw new InvalidOperationException("Expected no pending events");
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Events/EventTrackerSamples.cs#L6-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-access' title='Start of snippet'>anchor</a></sup>
<a id='snippet-events-eventtracker-access-1'></a>
```cs
/// <summary>
/// Demonstrates injecting and using IEventTracker.
/// </summary>
public static class EventTrackerAccessSample
{
    /// <summary>
    /// IEventTracker is registered automatically by AddNeatooRemoteFactory/AddNeatooAspNetCore.
    /// </summary>
    public static void AccessEventTracker(IServiceProvider serviceProvider)
    {
        // Resolve IEventTracker from DI
        var eventTracker = serviceProvider.GetRequiredService<IEventTracker>();

        // Check pending event count
        var pendingCount = eventTracker.PendingCount;
        Console.WriteLine($"Pending events: {pendingCount}");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L343-L362' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-access-1' title='Start of snippet'>anchor</a></sup>
<a id='snippet-events-eventtracker-access-2'></a>
```cs
/// <summary>
/// Accessing IEventTracker for pending event monitoring.
/// </summary>
public class EventTrackerAccessTests
{
    [Fact]
    public void EventTracker_ResolvedFromDI()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();

        // Act - IEventTracker is registered by AddNeatooRemoteFactory
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();

        // Assert
        Assert.NotNull(eventTracker);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L560-L579' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-access-2' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**ASP.NET Core Integration:**

RemoteFactory registers `EventTrackerHostedService` which:
- Waits for all pending events during application shutdown
- Uses `IHostApplicationLifetime.ApplicationStopping` as CancellationToken source

**Testing events:**

<!-- snippet: events-testing -->
<a id='snippet-events-testing'></a>
```cs
/// <summary>
/// Standard pattern for testing event side effects.
/// </summary>
public static class EventTestingPatternSample
{
    public static async Task TestWelcomeEmailEvent(
        IServiceProvider serviceProvider,
        IEventTracker eventTracker)
    {
        // Arrange - get event delegate from DI
        var sendWelcomeEmail = serviceProvider
            .GetRequiredService<EmployeeBasicEvent.SendWelcomeEmailEvent>();

        // Clear test data
        InMemoryEmailService.Clear();

        // Act - fire the event
        var employeeId = Guid.NewGuid();
        var testEmail = "newemployee@company.com";
        _ = sendWelcomeEmail(employeeId, testEmail);

        // Wait for event completion
        await eventTracker.WaitAllAsync();

        // Assert - verify email was sent
        var sentEmails = InMemoryEmailService.GetSentEmails();
        Assert.Single(sentEmails);
        Assert.Equal(testEmail, sentEmails[0].Recipient);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/Events/EventTestingSamples.cs#L69-L100' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-testing' title='Start of snippet'>anchor</a></sup>
<a id='snippet-events-testing-1'></a>
```cs
/// <summary>
/// Testing event handlers via delegate injection.
/// Events are fired via generated delegates, not factory methods.
/// </summary>
public class EventsTests
{
    [Fact]
    public async Task EventDelegate_FiresAsynchronously()
    {
        // Arrange - Clear any previous data
        InMemoryEmailService.Clear();

        var scopes = TestClientServerContainers.CreateScopes();

        // Events are invoked via delegates resolved from DI
        // The delegate is: EmployeeEventHandlers.NotifyHROfNewEmployeeEvent
        var notifyDelegate = scopes.local.ServiceProvider
            .GetRequiredService<EmployeeManagement.Domain.Events.EmployeeEventHandlers.NotifyHROfNewEmployeeEvent>();

        // Act - Invoke event delegate (fire-and-forget)
        await notifyDelegate(
            Guid.NewGuid(),
            "John Doe");

        // Assert - Wait for event to complete
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();
        await eventTracker.WaitAllAsync();

        // Verify email was sent
        var emails = InMemoryEmailService.GetSentEmails();
        Assert.Contains(emails, e =>
            e.Recipient == "hr@company.com" &&
            e.Subject.Contains("John Doe", StringComparison.Ordinal));
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L371-L407' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-testing-1' title='Start of snippet'>anchor</a></sup>
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
