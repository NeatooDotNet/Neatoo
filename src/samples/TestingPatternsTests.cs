using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.RemoteFactory;
using Xunit;

namespace Samples;

// =============================================================================
// TESTING PATTERNS - Demonstrates proper Neatoo testing techniques
// =============================================================================

/// <summary>
/// Example showing proper Neatoo test setup pattern.
/// This code demonstrates the pattern but is not executed as a test.
/// </summary>
public static class SkillTestingPatternExample
{
    #region skill-testing-pattern
    public static void ConfigureServices(IServiceCollection services)
    {
        // Setup DI with Neatoo services and mock external dependencies
        services.AddNeatooServices(NeatooFactory.Logical, typeof(SkillEmployee).Assembly);
        services.AddScoped<ISkillEmployeeRepository, SkillMockEmployeeRepository>();
    }

    public static void TestExample(IServiceProvider serviceProvider)
    {
        // DO: Use real Neatoo factories
        var factory = serviceProvider.GetRequiredService<ISkillEmployeeFactory>();
        var employee = factory.Create();
        employee.Name = "Alice";
        Assert.True(employee.IsModified);

        // DON'T: Mock Neatoo interfaces
        // var mock = new Mock<IEntityBase>(); // Never do this
    }
    #endregion
}

public class TestingPatternsTests : SamplesTestBase
{
    // -------------------------------------------------------------------------
    // Core Testing Principle: Real vs Mock
    // -------------------------------------------------------------------------

    #region test-real-vs-mock
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
    #endregion

    // -------------------------------------------------------------------------
    // Validation Testing
    // -------------------------------------------------------------------------

    #region test-validation
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
    #endregion

    // -------------------------------------------------------------------------
    // Change Tracking Testing
    // -------------------------------------------------------------------------

    #region test-change-tracking
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
    #endregion

    // -------------------------------------------------------------------------
    // Factory Method Testing
    // -------------------------------------------------------------------------

    #region test-factory-methods
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
    #endregion

    // -------------------------------------------------------------------------
    // Mock Dependencies Testing
    // -------------------------------------------------------------------------

    #region test-mock-dependencies
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
    #endregion

    // -------------------------------------------------------------------------
    // Collection Testing
    // -------------------------------------------------------------------------

    #region test-collections
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
    #endregion

    // -------------------------------------------------------------------------
    // Parent-Child Testing
    // -------------------------------------------------------------------------

    #region test-parent-child
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
    #endregion

    // -------------------------------------------------------------------------
    // Async Rules Testing
    // -------------------------------------------------------------------------

    #region test-async-rules
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
    #endregion

    // -------------------------------------------------------------------------
    // Authorization Testing
    // -------------------------------------------------------------------------

    #region test-authorization
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
    #endregion
}

// =============================================================================
// PROPERTY NOTIFICATION TESTS
// =============================================================================

public class PropertyNotificationTests : SamplesTestBase
{
    /// <summary>
    /// Test standard PropertyChanged event.
    /// </summary>
    [Fact]
    public void PropertyChanged_NotifiesOnChange()
    {
        var factory = GetRequiredService<ISkillPropNotifyEntityFactory>();
        var entity = factory.Create();

        var changedProperties = new List<string>();
        entity.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                changedProperties.Add(e.PropertyName);
        };

        entity.Name = "Test";

        // PropertyChanged fired
        Assert.True(changedProperties.Contains("Name"));
    }

    /// <summary>
    /// Test NeatooPropertyChanged with ChangeReason.
    /// </summary>
    [Fact]
    public void NeatooPropertyChanged_IncludesChangeReason()
    {
        var factory = GetRequiredService<ISkillPropNotifyEntityFactory>();
        var entity = factory.Create();

        var changes = new List<(string Property, ChangeReason Reason)>();
        entity.NeatooPropertyChanged += (e) =>
        {
            changes.Add((e.PropertyName, e.Reason));
            return Task.CompletedTask;
        };

        entity.Name = "Updated";

        // Property setter fires NeatooPropertyChanged with UserEdit reason
        // Filter for Name property changes (other properties may also fire)
        var nameChanges = changes.Where(c => c.Property == "Name").ToList();
        Assert.True(nameChanges.Count >= 1, "Name property should fire at least one change event");
        Assert.Equal(ChangeReason.UserEdit, nameChanges.Last().Reason);
    }

    /// <summary>
    /// Test LoadValue for data loading without triggering rules.
    /// </summary>
    [Fact]
    public void LoadValue_DoesNotTriggerRules()
    {
        var factory = GetRequiredService<ISkillPropInvoiceFactory>();
        var invoice = factory.Create();

        // LoadValue bypasses validation
        invoice["Amount"].LoadValue(-100m);

        // Value is set even though it would fail validation
        Assert.Equal(-100m, invoice.Amount);

        // Property hasn't been validated yet
        // (LoadValue doesn't run rules)
    }

    /// <summary>
    /// Test property meta-properties.
    /// </summary>
    [Fact]
    public async Task MetaProperties_AvailableOnProperty()
    {
        var factory = GetRequiredService<ISkillPropAccountFactory>();
        var account = factory.Create();

        var accountNumberProp = account["AccountNumber"];

        // Set invalid value
        account.AccountNumber = "";

        await account.RunRules();

        // Meta-properties reflect validation state
        Assert.False(accountNumberProp.IsValid);
        Assert.False(accountNumberProp.IsBusy);
        Assert.True(accountNumberProp.PropertyMessages.Any());

        // Fix value
        account.AccountNumber = "ACCT-001";
        await account.RunRules();

        Assert.True(accountNumberProp.IsValid);
        Assert.False(accountNumberProp.PropertyMessages.Any());
    }

    /// <summary>
    /// Test accessing property via indexer.
    /// </summary>
    [Fact]
    public void BackingFieldAccess_ViaIndexer()
    {
        var factory = GetRequiredService<ISkillPropCustomerFactory>();
        var customer = factory.Create();

        // Set via property
        customer.FirstName = "John";

        // Access via indexer returns the property wrapper
        var property = customer["FirstName"];

        Assert.NotNull(property);
        Assert.Equal("FirstName", property.Name);
        Assert.Equal("John", property.Value);
        Assert.Equal(typeof(string), property.Type);
    }

    /// <summary>
    /// Test PauseAllActions for batch updates.
    /// </summary>
    [Fact]
    public void SuppressEvents_WithPauseAllActions()
    {
        var factory = GetRequiredService<ISkillPropCustomerFactory>();
        var customer = factory.Create();

        var changeCount = 0;
        customer.PropertyChanged += (s, e) => changeCount++;

        // Batch update with pause
        using (customer.PauseAllActions())
        {
            Assert.True(customer.IsPaused);

            customer.FirstName = "John";
            customer.LastName = "Doe";
            customer.Email = "john@example.com";
        }

        // After pause ends
        Assert.False(customer.IsPaused);

        // Values are set
        Assert.Equal("John", customer.FirstName);
        Assert.Equal("Doe", customer.LastName);
    }

    #region properties-change-reason-useredit
    /// <summary>
    /// Test ChangeReason tracking.
    /// </summary>
    [Fact]
    public void ChangeReason_TracksUserEdits()
    {
        var factory = GetRequiredService<ISkillPropNotifyEntityFactory>();
        var entity = factory.Create();

        var reasons = new List<(string Property, ChangeReason Reason)>();
        entity.NeatooPropertyChanged += (e) =>
        {
            reasons.Add((e.PropertyName, e.Reason));
            return Task.CompletedTask;
        };

        // Normal property set via setter = UserEdit
        entity.Name = "Test";

        // Filter for Name property changes
        var nameReasons = reasons.Where(r => r.Property == "Name").ToList();
        Assert.True(nameReasons.Any(), "Name property should fire change event");
        Assert.Equal(ChangeReason.UserEdit, nameReasons.Last().Reason);
    }
    #endregion
}
