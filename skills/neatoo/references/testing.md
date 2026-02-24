# Testing

Testing Neatoo domain models requires a specific approach: **never mock Neatoo interfaces or classes**. Use real Neatoo objects to ensure your tests validate actual framework behavior.

## Core Principle

**DO:** Use real Neatoo classes and factories
**DON'T:** Mock Neatoo interfaces or implement stubs

<!-- snippet: test-real-vs-mock -->
<a id='snippet-test-real-vs-mock'></a>
```cs
/// <summary>
/// Use real Neatoo classes - never mock Neatoo interfaces.
/// </summary>
[Fact]
public async Task RealVsMock_UseRealNeatooClasses()
{
    // DO: Use real Neatoo factory to create real Neatoo objects
    var factory = GetRequiredService<ISkillEmployeeFactory>();
    var employee = factory.Create();

    // Real Neatoo objects have real behavior
    Assert.True(employee.IsNew);

    // Set invalid data and run rules to trigger validation
    employee.Name = "";
    await employee.RunRules(RunRulesFlag.All);
    Assert.False(employee["Name"].IsValid); // Real validation

    // Set valid data
    employee.Name = "John Doe";
    await employee.RunRules(RunRulesFlag.All);
    Assert.True(employee["Name"].IsValid);

    // For external dependencies, use mock implementations:
    // services.AddScoped<IMyRepository, MockMyRepository>();
}
```
<sup><a href='/src/samples/TestingPatternsTests.cs#L46-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-test-real-vs-mock' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Why no mocking:**
1. Neatoo classes work together as a cohesive unit — mocking breaks this
2. Mocks test your mock setup, not actual Neatoo behavior
3. Real objects reveal integration issues that mocks hide

## Test Base Class Setup

Register Neatoo services with `NeatooFactory.Logical` (all operations run locally, no HTTP calls) and mock only external dependencies:

<!-- snippet: test-base-class -->
<a id='snippet-test-base-class'></a>
```cs
/// <summary>
/// Base class for Neatoo skill tests.
/// Configures DI container with real Neatoo services and mock external dependencies.
/// </summary>
public abstract class SkillTestBase : IDisposable
{
    private static IServiceProvider? _container;
    private static readonly object _lock = new();
    private IServiceScope? _scope;

    /// <summary>
    /// Gets the current service scope.
    /// </summary>
    protected IServiceScope Scope
    {
        get
        {
            _scope ??= CreateScope();
            return _scope;
        }
    }

    /// <summary>
    /// Gets the service provider from the current scope.
    /// </summary>
    protected IServiceProvider ServiceProvider => Scope.ServiceProvider;

    private static IServiceScope CreateScope()
    {
        lock (_lock)
        {
            _container ??= CreateContainer();
            return _container.CreateScope();
        }
    }

    private static IServiceProvider CreateContainer()
    {
        var services = new ServiceCollection();

        // Register Neatoo services with NeatooFactory.Logical
        // (all operations run locally, no remote calls)
        services.AddNeatooServices(NeatooFactory.Logical, typeof(SkillTestBase).Assembly);

        // Register mock services for external dependencies
        RegisterMockServices(services);

        return services.BuildServiceProvider();
    }

    private static void RegisterMockServices(IServiceCollection services)
    {
        // Repository mocks
        services.AddScoped<ISkillEmployeeRepository, SkillMockEmployeeRepository>();
        services.AddScoped<ISkillCustomerRepository, MockCustomerRepository>();
        services.AddScoped<ISkillProductRepository, MockProductRepository>();
        services.AddScoped<ISkillOrderRepository, MockOrderRepository>();
        services.AddScoped<ISkillAccountRepository, MockAccountRepository>();
        services.AddScoped<ISkillProjectRepository, MockProjectRepository>();
        services.AddScoped<ISkillReportRepository, MockReportRepository>();
        services.AddScoped<ISkillReportGenerator, MockReportGenerator>();
        services.AddScoped<ISkillDataRepository, MockDataRepository>();
        services.AddScoped<ISkillOrderWithItemsRepository, MockOrderWithItemsRepository>();
        services.AddScoped<ISkillEntityRepository, MockEntityRepository>();
        services.AddScoped<ISkillGenRepository, MockGenRepository>();
        services.AddScoped<ISkillRemoteFactoryRepository, MockRemoteFactoryRepository>();

        // Service mocks
        services.AddScoped<ISkillEmailService, MockEmailService>();
        services.AddScoped<ISkillUserValidationService, MockUserValidationService>();
        services.AddScoped<ISkillAccountValidationService, MockAccountValidationService>();
        services.AddScoped<ISkillEmailValidationService, SkillMockEmailValidationService>();
        services.AddScoped<ISkillOrderAccessService, MockOrderAccessService>();
        services.AddScoped<ISkillProjectMembershipService, MockProjectMembershipService>();
        services.AddScoped<ISkillFeatureFlagService, MockFeatureFlagService>();
    }

    /// <summary>
    /// Gets a required service from the current scope.
    /// </summary>
    protected T GetRequiredService<T>() where T : notnull
    {
        return Scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets an optional service from the current scope.
    /// </summary>
    protected T? GetService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _scope = null;
        GC.SuppressFinalize(this);
    }
}
```
<sup><a href='/src/samples/SkillTestBase.cs#L10-L111' title='Snippet source file'>snippet source</a> | <a href='#snippet-test-base-class' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Testing Validation

<!-- snippet: test-validation -->
<a id='snippet-test-validation'></a>
```cs
/// <summary>
/// Test validation rules with real Neatoo validation.
/// </summary>
[Fact]
public async Task Validation_TestsRealRules()
{
    var factory = GetRequiredService<ISkillValidProductFactory>();
    var product = factory.Create();

    // Test invalid state
    product.Name = "";
    product.Price = -10;

    await product.RunRules();

    Assert.False(product.IsValid);
    Assert.False(product["Name"].IsValid);
    Assert.False(product["Price"].IsValid);

    // Test valid state
    product.Name = "Widget";
    product.Price = 19.99m;

    await product.RunRules();

    Assert.True(product.IsValid);
    Assert.True(product["Name"].IsValid);
    Assert.True(product["Price"].IsValid);
}

/// <summary>
/// Test DataAnnotation validation attributes.
/// </summary>
[Fact]
public void ValidationAttributes_AutoConverted()
{
    var factory = GetRequiredService<ISkillValidRegistrationFactory>();
    var reg = factory.Create();

    // [Required] - empty fails
    reg.Username = "";
    Assert.False(reg["Username"].IsValid);

    reg.Username = "validuser";
    Assert.True(reg["Username"].IsValid);

    // [EmailAddress] - invalid format fails
    reg.Email = "not-an-email";
    Assert.False(reg["Email"].IsValid);

    reg.Email = "valid@example.com";
    Assert.True(reg["Email"].IsValid);

    // [Range] - out of range fails
    reg.Age = 10;
    Assert.False(reg["Age"].IsValid);

    reg.Age = 25;
    Assert.True(reg["Age"].IsValid);
}
```
<sup><a href='/src/samples/TestingPatternsTests.cs#L79-L140' title='Snippet source file'>snippet source</a> | <a href='#snippet-test-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Testing Change Tracking

<!-- snippet: test-change-tracking -->
<a id='snippet-test-change-tracking'></a>
```cs
/// <summary>
/// Test change tracking with real Neatoo entities.
/// </summary>
[Fact]
public void ChangeTracking_DetectsPropertyChanges()
{
    var factory = GetRequiredService<ISkillEntityEmployeeFactory>();

    // Fetch creates an existing (non-new) entity
    var employee = factory.Fetch(1, "Alice", "Engineering", 50000);

    // After fetch, entity is clean
    Assert.False(employee.IsNew);
    Assert.False(employee.IsModified);
    Assert.False(employee.IsSelfModified);

    // Change a property
    employee.Name = "Alice Smith";

    // Now entity tracks the change
    Assert.True(employee.IsModified);
    Assert.True(employee.IsSelfModified);
    Assert.True(employee.ModifiedProperties.Contains("Name"));

    // Other properties not tracked
    Assert.False(employee.ModifiedProperties.Contains("Department"));

    // Change another property
    employee.Salary = 55000;
    Assert.True(employee.ModifiedProperties.Contains("Salary"));
}

/// <summary>
/// Test IsNew state after Create and Fetch.
/// </summary>
[Fact]
public void ChangeTracking_IsNewState()
{
    var factory = GetRequiredService<ISkillEntityEmployeeFactory>();

    // Create produces new entity
    var newEmployee = factory.Create();
    Assert.True(newEmployee.IsNew);

    // Fetch produces existing entity
    var existingEmployee = factory.Fetch(1, "Bob", "Sales", 60000);
    Assert.False(existingEmployee.IsNew);
}
```
<sup><a href='/src/samples/TestingPatternsTests.cs#L146-L195' title='Snippet source file'>snippet source</a> | <a href='#snippet-test-change-tracking' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Related

- [Validation](validation.md) - Validation rules
- [Entities](entities.md) - Entity lifecycle
- [Pitfalls](pitfalls.md) - Common mistakes
