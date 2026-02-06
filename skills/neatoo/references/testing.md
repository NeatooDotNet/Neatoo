# Testing

Testing Neatoo domain models requires a specific approach: **never mock Neatoo interfaces or classes**. Use real Neatoo objects to ensure your tests validate actual framework behavior.

## Core Testing Principle

**DO:** Use real Neatoo classes
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

## Why No Mocking?

1. **Cohesive behavior** - Neatoo classes work together; mocking breaks this
2. **Test real behavior** - Mocks test your mock setup, not Neatoo
3. **Catch integration issues** - Real objects reveal actual problems
4. **Framework guarantees** - Neatoo's behavior is the specification

## Test Project Setup

<!-- snippet: test-project-setup -->
<a id='snippet-test-project-setup'></a>
```cs
// Test project setup requirements:
//
// 1. Add project references:
//    - Reference to domain project (where Neatoo entities are)
//    - Reference to Neatoo NuGet package
//
// 2. Register services:
//    services.AddNeatooServices(NeatooFactory.Logical, typeof(MyEntity).Assembly);
//
// 3. Mock EXTERNAL dependencies, not Neatoo classes:
//    services.AddScoped<IMyRepository, MockMyRepository>();
//
// 4. Use real Neatoo factories:
//    var factory = GetRequiredService<IEmployeeFactory>();
//    var employee = factory.Create();
//
// NeatooFactory.Logical means all factory operations run locally
// (no HTTP calls to remote server)
```
<sup><a href='/src/samples/SkillTestBase.cs#L113-L132' title='Snippet source file'>snippet source</a> | <a href='#snippet-test-project-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Testing Validation

Test validation rules with real objects:

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

Verify change tracking behavior:

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

## Testing Factory Methods

Test factory operations:

<!-- snippet: test-factory-methods -->
<a id='snippet-test-factory-methods'></a>
```cs
/// <summary>
/// Test factory methods with real factories.
/// </summary>
[Fact]
public async Task FactoryMethods_TestCreateFetchSave()
{
    var factory = GetRequiredService<ISkillFactoryCustomerFactory>();

    // Test Create
    var customer = factory.Create();
    Assert.True(customer.IsNew);
    Assert.Equal(0, customer.Id);
    Assert.Equal("", customer.Name);

    // Test Fetch
    var existing = await factory.FetchByIdAsync(1);
    Assert.False(existing.IsNew);
    Assert.Equal(1, existing.Id);
    Assert.Equal("Customer 1", existing.Name);

    // Test Save (routes to Insert for new entity)
    customer.Name = "New Customer";
    customer.Email = "new@example.com";
    var saved = await factory.SaveAsync(customer);

    Assert.NotNull(saved);
    Assert.False(saved!.IsNew); // After insert, no longer new
}

/// <summary>
/// Test multiple fetch overloads.
/// </summary>
[Fact]
public async Task FactoryMethods_MultipleFetchOverloads()
{
    var factory = GetRequiredService<ISkillFactoryOrderFactory>();

    // Fetch by ID
    var byId = factory.FetchById(42);
    Assert.Equal(42, byId.Id);

    // Fetch by order number
    var byNumber = factory.FetchByOrderNumber("ORD-00001");
    Assert.Equal("ORD-00001", byNumber.OrderNumber);

    // Fetch by customer email
    var byCustomer = await factory.FetchByCustomerAsync("test@example.com");
    Assert.Equal("test@example.com", byCustomer.CustomerEmail);
}
```
<sup><a href='/src/samples/TestingPatternsTests.cs#L201-L251' title='Snippet source file'>snippet source</a> | <a href='#snippet-test-factory-methods' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Testing with Mocked Dependencies

Mock external dependencies, not Neatoo:

<!-- snippet: test-mock-dependencies -->
<a id='snippet-test-mock-dependencies'></a>
```cs
/// <summary>
/// Mock external dependencies, not Neatoo classes.
/// </summary>
[Fact]
public async Task MockDependencies_OnlyMockExternal()
{
    // Mock repository is registered in SkillTestBase
    // It provides fake data for fetch operations

    var factory = GetRequiredService<ISkillGenEntityFactory>();

    // Factory uses mock repository but REAL Neatoo entity
    var entity = await factory.FetchAsync(1);

    // Entity has real Neatoo behavior
    Assert.False(entity.IsNew); // Real lifecycle
    Assert.False(entity.IsModified); // Real tracking

    // Mock provided the data
    Assert.Equal(1, entity.Id);
    Assert.Equal("Entity 1", entity.Name);

    // Modify to test change detection
    entity.Name = "Changed";
    Assert.True(entity.IsModified); // Real change detection
    Assert.Equal("Changed", entity.Name);
}
```
<sup><a href='/src/samples/TestingPatternsTests.cs#L257-L285' title='Snippet source file'>snippet source</a> | <a href='#snippet-test-mock-dependencies' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Testing Collections

Test collection behavior:

<!-- snippet: test-collections -->
<a id='snippet-test-collections'></a>
```cs
/// <summary>
/// Test collection behavior with real Neatoo lists.
/// </summary>
[Fact]
public void Collections_TestRealBehavior()
{
    var orderFactory = GetRequiredService<ISkillCollOrderFactory>();
    var itemFactory = GetRequiredService<ISkillCollOrderItemFactory>();

    var order = orderFactory.Create();

    // Add items
    var item1 = itemFactory.Create();
    item1.ProductCode = "ITEM-001";
    item1.Price = 19.99m;
    item1.Quantity = 2;

    var item2 = itemFactory.Create();
    item2.ProductCode = "ITEM-002";
    item2.Price = 29.99m;
    item2.Quantity = 1;

    order.Items.Add(item1);
    order.Items.Add(item2);

    // Real collection behavior
    Assert.Equal(2, order.Items.Count);
    Assert.True(item1.IsChild);
    Assert.Same(order, item1.Parent);

    // Remove item
    order.Items.Remove(item1);
    Assert.Equal(1, order.Items.Count);
    Assert.Equal(0, order.Items.DeletedCount); // New item not tracked
}

/// <summary>
/// Test deletion tracking for existing items.
/// </summary>
[Fact]
public void Collections_DeletionTracking()
{
    var orderFactory = GetRequiredService<ISkillCollOrderFactory>();
    var itemFactory = GetRequiredService<ISkillCollOrderItemFactory>();

    var order = orderFactory.Create();

    // Add "existing" item (via Fetch)
    var item = itemFactory.Fetch(1, "WIDGET-001", 19.99m, 1);
    order.Items.Add(item);
    order.DoMarkUnmodified(); // Simulate loaded from DB

    Assert.False(item.IsNew);

    // Remove existing item
    order.Items.Remove(item);

    // Item tracked for deletion
    Assert.True(item.IsDeleted);
    Assert.Equal(1, order.Items.DeletedCount);
}
```
<sup><a href='/src/samples/TestingPatternsTests.cs#L291-L353' title='Snippet source file'>snippet source</a> | <a href='#snippet-test-collections' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Testing Parent-Child Relationships

Verify parent references:

<!-- snippet: test-parent-child -->
<a id='snippet-test-parent-child'></a>
```cs
/// <summary>
/// Test parent-child relationships.
/// </summary>
[Fact]
public void ParentChild_TracksRelationships()
{
    var deptFactory = GetRequiredService<ISkillEntityDepartmentFactory>();
    var memberFactory = GetRequiredService<ISkillEntityDepartmentMemberFactory>();

    var dept = deptFactory.Create();
    dept.Name = "Engineering";

    var member = memberFactory.Create();
    member.Name = "Alice";
    member.Role = "Developer";

    // Before adding - no parent
    Assert.Null(member.Parent);
    Assert.False(member.IsChild);

    // Add to collection
    dept.Members.Add(member);

    // After adding - parent established
    Assert.Same(dept, member.Parent);
    Assert.True(member.IsChild);

    // Root walks to aggregate root
    Assert.Same(dept, member.Root);
    Assert.Null(dept.Root); // Root has no parent
}
```
<sup><a href='/src/samples/TestingPatternsTests.cs#L359-L391' title='Snippet source file'>snippet source</a> | <a href='#snippet-test-parent-child' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Testing Async Rules

Test async validation:

<!-- snippet: test-async-rules -->
<a id='snippet-test-async-rules'></a>
```cs
/// <summary>
/// Test async validation rules.
/// </summary>
[Fact]
public async Task AsyncRules_WaitForCompletion()
{
    var factory = GetRequiredService<ISkillValidUserFactory>();
    var user = factory.Create();

    user.Username = "testuser";

    // Set email that will trigger async validation
    user.Email = "taken@example.com"; // Mock returns false for "taken"

    // Wait for async validation
    await user.WaitForTasks();

    // Async rule executed
    Assert.False(user["Email"].IsValid);

    // Change to valid email
    user.Email = "available@example.com";
    await user.WaitForTasks();

    Assert.True(user["Email"].IsValid);
}
```
<sup><a href='/src/samples/TestingPatternsTests.cs#L397-L424' title='Snippet source file'>snippet source</a> | <a href='#snippet-test-async-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Integration Test Base Class

Create a base class for common setup:

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

## Testing Authorization

Test authorization separately:

<!-- snippet: test-authorization -->
<a id='snippet-test-authorization'></a>
```cs
/// <summary>
/// Test authorization logic separately from entity behavior.
/// </summary>
[Fact]
public void Authorization_TestSeparately()
{
    // Test authorization implementation directly
    var principal = new System.Security.Principal.GenericPrincipal(
        new System.Security.Principal.GenericIdentity("testuser"),
        new[] { "Admin" });

    var auth = new SkillEmployeeAuthorization(principal);

    // Admin can create
    Assert.True(auth.CanCreate());
    Assert.True(auth.CanFetch());
    Assert.True(auth.CanSave());
    Assert.True(auth.CanDelete());

    // Non-admin limited
    var limitedPrincipal = new System.Security.Principal.GenericPrincipal(
        new System.Security.Principal.GenericIdentity("viewer"),
        new[] { "Viewer" });

    var limitedAuth = new SkillEmployeeAuthorization(limitedPrincipal);

    Assert.False(limitedAuth.CanCreate());
    Assert.True(limitedAuth.CanFetch()); // Authenticated
    Assert.False(limitedAuth.CanSave());
    Assert.False(limitedAuth.CanDelete());
}
```
<sup><a href='/src/samples/TestingPatternsTests.cs#L430-L462' title='Snippet source file'>snippet source</a> | <a href='#snippet-test-authorization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Test Organization

Organize tests by behavior:

```
Tests/
├── Validation/
│   ├── EmployeeValidationTests.cs
│   └── OrderValidationTests.cs
├── ChangeTracking/
│   ├── EmployeeChangeTrackingTests.cs
│   └── OrderChangeTrackingTests.cs
├── Factory/
│   ├── EmployeeFactoryTests.cs
│   └── OrderFactoryTests.cs
└── Integration/
    └── OrderWorkflowTests.cs
```

## Related

- [Validation](validation.md) - Validation rules
- [Entities](entities.md) - Entity lifecycle
- [Factory](factory.md) - Factory methods
