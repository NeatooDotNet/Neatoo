# Testing

Testing Neatoo domain models requires a specific approach: **never mock Neatoo interfaces or classes**. Use real Neatoo objects to ensure tests validate actual framework behavior.

## Core Principle

**DO:** Use real Neatoo classes and factories
**DON'T:** Mock Neatoo interfaces or implement stubs

A test creates the entity through its real factory, sets properties as a user would, awaits `WaitForTasks()` so async rules finish, and reads the real `IsValid`/`IsSavable`:

<!-- snippet: skill-test-real-objects -->
<a id='snippet-skill-test-real-objects'></a>
```cs
[TestMethod]
public async Task Employee_NegativeSalary_IsInvalid()
{
    var employee = _employeeFactory.Create();
    employee.FirstName = "Ada";
    employee.LastName = "Lovelace";
    employee.Email = "ada@example.com";
    employee.Salary = -1m;
    await employee.WaitForTasks();

    Assert.IsFalse(employee.IsValid, "Salary cannot be negative");
    Assert.IsFalse(employee.IsSavable);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/AggregateCoverageGapTests.cs#L177-L191' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-test-real-objects' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Why no mocking:**
1. Neatoo classes work together as a cohesive unit — mocking breaks this
2. Mocks test the mock setup, not actual Neatoo behavior
3. Real objects reveal integration issues that mocks hide

## Test Services Setup

Register Neatoo services for the domain assembly, register the domain's DI-provided rules the way both tiers would, and mock only the external dependencies (repositories, permission services, the repositories behind `[Remote, Execute]` commands). `NeatooFactory.Server` runs every operation in-process; `NeatooFactory.Logical` does the same without a tier:

<!-- snippet: skill-test-services -->
<a id='snippet-skill-test-services'></a>
```cs
public static IServiceScope GetScope()
{
    lock (_lock)
    {
        if (_serviceProvider == null)
        {
            var services = new ServiceCollection();

            // Real Neatoo services and the generated factories for the
            // domain assembly. Server mode: every operation runs in-process.
            services.AddNeatooServices(
                NeatooFactory.Server,
                typeof(Design.Domain.BaseClasses.IDemoInputModel).Assembly);

            // The domain's DI-provided rules, as both tiers would register them
            services.AddDesignDomainRules();

            // Mocks for the external dependencies only - never for Neatoo types
            RegisterMockRepositories(services);

            _serviceProvider = services.BuildServiceProvider();
        }
        return _serviceProvider.CreateScope();
    }
}
```
<sup><a href='/src/Design/Design.Tests/TestInfrastructure.cs#L35-L61' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-test-services' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Mock repositories that a save writes to are registered **scoped**, so the test scope and the factory operations observe the same instance and the test can assert on the rows the aggregate wrote.

## Testing Validation

Set the property, await `WaitForTasks()`, read `IsValid`. `RunRules()` is for forcing a re-run (for example after a `[Create]` that set values while paused), not the routine step before reading validity:

<!-- snippet: skill-test-validation -->
<a id='snippet-skill-test-validation'></a>
```cs
[TestMethod]
public async Task ValidationRule_MakesInvalidOnFailure()
{
    // Arrange
    var entity = _factory.Create();
    entity.Name = "Valid"; // Start with valid name
    await entity.WaitForTasks();
    Assert.IsTrue(entity.IsValid);

    // Act
    entity.Name = null; // Triggers NameRequiredRule
    await entity.WaitForTasks();

    // Assert
    Assert.IsFalse(entity.IsValid, "Entity should be invalid when name is empty");
}
```
<sup><a href='/src/Design/Design.Tests/RuleTests/SyncRuleTests.cs#L46-L63' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-test-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Testing Change Tracking

A fetched entity is a clean baseline:

<!-- snippet: skill-fetch-clean-baseline -->
<a id='snippet-skill-fetch-clean-baseline'></a>
```cs
[TestMethod]
public async Task Fetch_LoadsCleanBaseline_NotModified()
{
    // Arrange & Act
    var entity = await _factory.Fetch(1);

    // Assert
    Assert.IsFalse(entity.IsModified, "Fetched entity should not be modified");
    Assert.IsFalse(entity.IsSelfModified, "Fetched entity should not be self-modified");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/StatePropertyTests.cs#L61-L72' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-fetch-clean-baseline' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

An edit after the fetch marks the property and the entity modified:

<!-- snippet: skill-fetch-then-modify -->
<a id='snippet-skill-fetch-then-modify'></a>
```cs
[TestMethod]
public async Task Fetch_ThenModify_IsModified()
{
    // Arrange
    var entity = await _factory.Fetch(1);

    // Act
    entity.Name = "Changed";

    // Assert
    Assert.IsTrue(entity.IsModified, "Entity should be modified after change");
    Assert.IsTrue(entity["Name"].IsModified, "Name property should be modified");
}
```
<sup><a href='/src/Design/Design.Tests/PropertyTests/StatePropertyTests.cs#L74-L88' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-fetch-then-modify' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Testing a Save

Seed the scoped mock store, fetch through the real factory, edit, save, and assert on the rows and on the graph's state afterward. [entities.md](entities.md) → "Aggregate Save Cascading" has a full example: a fetched order, a child added, `Save()` through the root, the row store asserted.

## Related

- [Validation](validation.md) - Validation rules
- [Entities](entities.md) - Entity lifecycle
- [Pitfalls](pitfalls.md) - Common mistakes
