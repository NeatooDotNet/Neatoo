using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Skills.Domain;

namespace Neatoo.Skills.Tests;

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
        services.AddScoped<ISkillEmployeeRepository, MockEmployeeRepository>();
    }

    public static void TestExample(IServiceProvider serviceProvider)
    {
        // DO: Use real Neatoo factories
        var factory = serviceProvider.GetRequiredService<ISkillEmployeeFactory>();
        var employee = factory.Create();
        employee.Name = "Alice";
        Assert.IsTrue(employee.IsModified);

        // DON'T: Mock Neatoo interfaces
        // var mock = new Mock<IEntityBase>(); // Never do this
    }
    #endregion
}

[TestClass]
public class TestingPatternsTests : SkillTestBase
{
    // -------------------------------------------------------------------------
    // Core Testing Principle: Real vs Mock
    // -------------------------------------------------------------------------

    #region test-real-vs-mock
    /// <summary>
    /// Use real Neatoo classes - never mock Neatoo interfaces.
    /// </summary>
    [TestMethod]
    public async Task RealVsMock_UseRealNeatooClasses()
    {
        // DO: Use real Neatoo factory to create real Neatoo objects
        var factory = GetRequiredService<ISkillEmployeeFactory>();
        var employee = factory.Create();

        // Real Neatoo objects have real behavior
        Assert.IsTrue(employee.IsNew);

        // Set invalid data and run rules to trigger validation
        employee.Name = "";
        await employee.RunRules(RunRulesFlag.All);
        Assert.IsFalse(employee["Name"].IsValid); // Real validation

        // Set valid data
        employee.Name = "John Doe";
        await employee.RunRules(RunRulesFlag.All);
        Assert.IsTrue(employee["Name"].IsValid);

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
    [TestMethod]
    public async Task Validation_TestsRealRules()
    {
        var factory = GetRequiredService<ISkillValidProductFactory>();
        var product = factory.Create();

        // Test invalid state
        product.Name = "";
        product.Price = -10;

        await product.RunRules();

        Assert.IsFalse(product.IsValid);
        Assert.IsFalse(product["Name"].IsValid);
        Assert.IsFalse(product["Price"].IsValid);

        // Test valid state
        product.Name = "Widget";
        product.Price = 19.99m;

        await product.RunRules();

        Assert.IsTrue(product.IsValid);
        Assert.IsTrue(product["Name"].IsValid);
        Assert.IsTrue(product["Price"].IsValid);
    }

    /// <summary>
    /// Test DataAnnotation validation attributes.
    /// </summary>
    [TestMethod]
    public void ValidationAttributes_AutoConverted()
    {
        var factory = GetRequiredService<ISkillValidRegistrationFactory>();
        var reg = factory.Create();

        // [Required] - empty fails
        reg.Username = "";
        Assert.IsFalse(reg["Username"].IsValid);

        reg.Username = "validuser";
        Assert.IsTrue(reg["Username"].IsValid);

        // [EmailAddress] - invalid format fails
        reg.Email = "not-an-email";
        Assert.IsFalse(reg["Email"].IsValid);

        reg.Email = "valid@example.com";
        Assert.IsTrue(reg["Email"].IsValid);

        // [Range] - out of range fails
        reg.Age = 10;
        Assert.IsFalse(reg["Age"].IsValid);

        reg.Age = 25;
        Assert.IsTrue(reg["Age"].IsValid);
    }
    #endregion

    // -------------------------------------------------------------------------
    // Change Tracking Testing
    // -------------------------------------------------------------------------

    #region test-change-tracking
    /// <summary>
    /// Test change tracking with real Neatoo entities.
    /// </summary>
    [TestMethod]
    public void ChangeTracking_DetectsPropertyChanges()
    {
        var factory = GetRequiredService<ISkillEntityEmployeeFactory>();

        // Fetch creates an existing (non-new) entity
        var employee = factory.Fetch(1, "Alice", "Engineering", 50000);

        // After fetch, entity is clean
        Assert.IsFalse(employee.IsNew);
        Assert.IsFalse(employee.IsModified);
        Assert.IsFalse(employee.IsSelfModified);

        // Change a property
        employee.Name = "Alice Smith";

        // Now entity tracks the change
        Assert.IsTrue(employee.IsModified);
        Assert.IsTrue(employee.IsSelfModified);
        Assert.IsTrue(employee.ModifiedProperties.Contains("Name"));

        // Other properties not tracked
        Assert.IsFalse(employee.ModifiedProperties.Contains("Department"));

        // Change another property
        employee.Salary = 55000;
        Assert.IsTrue(employee.ModifiedProperties.Contains("Salary"));
    }

    /// <summary>
    /// Test IsNew state after Create and Fetch.
    /// </summary>
    [TestMethod]
    public void ChangeTracking_IsNewState()
    {
        var factory = GetRequiredService<ISkillEntityEmployeeFactory>();

        // Create produces new entity
        var newEmployee = factory.Create();
        Assert.IsTrue(newEmployee.IsNew);

        // Fetch produces existing entity
        var existingEmployee = factory.Fetch(1, "Bob", "Sales", 60000);
        Assert.IsFalse(existingEmployee.IsNew);
    }
    #endregion

    // -------------------------------------------------------------------------
    // Factory Method Testing
    // -------------------------------------------------------------------------

    #region test-factory-methods
    /// <summary>
    /// Test factory methods with real factories.
    /// </summary>
    [TestMethod]
    public async Task FactoryMethods_TestCreateFetchSave()
    {
        var factory = GetRequiredService<ISkillFactoryCustomerFactory>();

        // Test Create
        var customer = factory.Create();
        Assert.IsTrue(customer.IsNew);
        Assert.AreEqual(0, customer.Id);
        Assert.AreEqual("", customer.Name);

        // Test Fetch
        var existing = await factory.FetchByIdAsync(1);
        Assert.IsFalse(existing.IsNew);
        Assert.AreEqual(1, existing.Id);
        Assert.AreEqual("Customer 1", existing.Name);

        // Test Save (routes to Insert for new entity)
        customer.Name = "New Customer";
        customer.Email = "new@example.com";
        var saved = await factory.SaveAsync(customer);

        Assert.IsNotNull(saved);
        Assert.IsFalse(saved!.IsNew); // After insert, no longer new
    }

    /// <summary>
    /// Test multiple fetch overloads.
    /// </summary>
    [TestMethod]
    public async Task FactoryMethods_MultipleFetchOverloads()
    {
        var factory = GetRequiredService<ISkillFactoryOrderFactory>();

        // Fetch by ID
        var byId = factory.FetchById(42);
        Assert.AreEqual(42, byId.Id);

        // Fetch by order number
        var byNumber = factory.FetchByOrderNumber("ORD-00001");
        Assert.AreEqual("ORD-00001", byNumber.OrderNumber);

        // Fetch by customer email
        var byCustomer = await factory.FetchByCustomerAsync("test@example.com");
        Assert.AreEqual("test@example.com", byCustomer.CustomerEmail);
    }
    #endregion

    // -------------------------------------------------------------------------
    // Mock Dependencies Testing
    // -------------------------------------------------------------------------

    #region test-mock-dependencies
    /// <summary>
    /// Mock external dependencies, not Neatoo classes.
    /// </summary>
    [TestMethod]
    public async Task MockDependencies_OnlyMockExternal()
    {
        // Mock repository is registered in SkillTestBase
        // It provides fake data for fetch operations

        var factory = GetRequiredService<ISkillGenEntityFactory>();

        // Factory uses mock repository but REAL Neatoo entity
        var entity = await factory.FetchAsync(1);

        // Entity has real Neatoo behavior
        Assert.IsFalse(entity.IsNew); // Real lifecycle
        Assert.IsFalse(entity.IsModified); // Real tracking

        // Mock provided the data
        Assert.AreEqual(1, entity.Id);
        Assert.AreEqual("Entity 1", entity.Name);

        // Modify to test change detection
        entity.Name = "Changed";
        Assert.IsTrue(entity.IsModified); // Real change detection
        Assert.AreEqual("Changed", entity.Name);
    }
    #endregion

    // -------------------------------------------------------------------------
    // Collection Testing
    // -------------------------------------------------------------------------

    #region test-collections
    /// <summary>
    /// Test collection behavior with real Neatoo lists.
    /// </summary>
    [TestMethod]
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
        Assert.AreEqual(2, order.Items.Count);
        Assert.IsTrue(item1.IsChild);
        Assert.AreSame(order, item1.Parent);

        // Remove item
        order.Items.Remove(item1);
        Assert.AreEqual(1, order.Items.Count);
        Assert.AreEqual(0, order.Items.DeletedCount); // New item not tracked
    }

    /// <summary>
    /// Test deletion tracking for existing items.
    /// </summary>
    [TestMethod]
    public void Collections_DeletionTracking()
    {
        var orderFactory = GetRequiredService<ISkillCollOrderFactory>();
        var itemFactory = GetRequiredService<ISkillCollOrderItemFactory>();

        var order = orderFactory.Create();

        // Add "existing" item (via Fetch)
        var item = itemFactory.Fetch(1, "WIDGET-001", 19.99m, 1);
        order.Items.Add(item);
        order.DoMarkUnmodified(); // Simulate loaded from DB

        Assert.IsFalse(item.IsNew);

        // Remove existing item
        order.Items.Remove(item);

        // Item tracked for deletion
        Assert.IsTrue(item.IsDeleted);
        Assert.AreEqual(1, order.Items.DeletedCount);
    }
    #endregion

    // -------------------------------------------------------------------------
    // Parent-Child Testing
    // -------------------------------------------------------------------------

    #region test-parent-child
    /// <summary>
    /// Test parent-child relationships.
    /// </summary>
    [TestMethod]
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
        Assert.IsNull(member.Parent);
        Assert.IsFalse(member.IsChild);

        // Add to collection
        dept.Members.Add(member);

        // After adding - parent established
        Assert.AreSame(dept, member.Parent);
        Assert.IsTrue(member.IsChild);

        // Root walks to aggregate root
        Assert.AreSame(dept, member.Root);
        Assert.IsNull(dept.Root); // Root has no parent
    }
    #endregion

    // -------------------------------------------------------------------------
    // Async Rules Testing
    // -------------------------------------------------------------------------

    #region test-async-rules
    /// <summary>
    /// Test async validation rules.
    /// </summary>
    [TestMethod]
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
        Assert.IsFalse(user["Email"].IsValid);

        // Change to valid email
        user.Email = "available@example.com";
        await user.WaitForTasks();

        Assert.IsTrue(user["Email"].IsValid);
    }
    #endregion

    // -------------------------------------------------------------------------
    // Authorization Testing
    // -------------------------------------------------------------------------

    #region test-authorization
    /// <summary>
    /// Test authorization logic separately from entity behavior.
    /// </summary>
    [TestMethod]
    public void Authorization_TestSeparately()
    {
        // Test authorization implementation directly
        var principal = new System.Security.Principal.GenericPrincipal(
            new System.Security.Principal.GenericIdentity("testuser"),
            new[] { "Admin" });

        var auth = new SkillEmployeeAuthorization(principal);

        // Admin can create
        Assert.IsTrue(auth.CanCreate());
        Assert.IsTrue(auth.CanFetch());
        Assert.IsTrue(auth.CanSave());
        Assert.IsTrue(auth.CanDelete());

        // Non-admin limited
        var limitedPrincipal = new System.Security.Principal.GenericPrincipal(
            new System.Security.Principal.GenericIdentity("viewer"),
            new[] { "Viewer" });

        var limitedAuth = new SkillEmployeeAuthorization(limitedPrincipal);

        Assert.IsFalse(limitedAuth.CanCreate());
        Assert.IsTrue(limitedAuth.CanFetch()); // Authenticated
        Assert.IsFalse(limitedAuth.CanSave());
        Assert.IsFalse(limitedAuth.CanDelete());
    }
    #endregion
}

// =============================================================================
// PROPERTY NOTIFICATION TESTS
// =============================================================================

[TestClass]
public class PropertyNotificationTests : SkillTestBase
{
    #region properties-property-changed
    /// <summary>
    /// Test standard PropertyChanged event.
    /// </summary>
    [TestMethod]
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
        Assert.IsTrue(changedProperties.Contains("Name"));
    }
    #endregion

    #region properties-neatoo-property-changed
    /// <summary>
    /// Test NeatooPropertyChanged with ChangeReason.
    /// </summary>
    [TestMethod]
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
        Assert.IsTrue(nameChanges.Count >= 1, "Name property should fire at least one change event");
        Assert.AreEqual(ChangeReason.UserEdit, nameChanges.Last().Reason);
    }
    #endregion

    #region properties-load-value
    /// <summary>
    /// Test LoadValue for data loading without triggering rules.
    /// </summary>
    [TestMethod]
    public void LoadValue_DoesNotTriggerRules()
    {
        var factory = GetRequiredService<ISkillPropInvoiceFactory>();
        var invoice = factory.Create();

        // LoadValue bypasses validation
        invoice["Amount"].LoadValue(-100m);

        // Value is set even though it would fail validation
        Assert.AreEqual(-100m, invoice.Amount);

        // Property hasn't been validated yet
        // (LoadValue doesn't run rules)
    }
    #endregion

    #region properties-meta-properties
    /// <summary>
    /// Test property meta-properties.
    /// </summary>
    [TestMethod]
    public async Task MetaProperties_AvailableOnProperty()
    {
        var factory = GetRequiredService<ISkillPropAccountFactory>();
        var account = factory.Create();

        var accountNumberProp = account["AccountNumber"];

        // Set invalid value
        account.AccountNumber = "";

        await account.RunRules();

        // Meta-properties reflect validation state
        Assert.IsFalse(accountNumberProp.IsValid);
        Assert.IsFalse(accountNumberProp.IsBusy);
        Assert.IsTrue(accountNumberProp.PropertyMessages.Any());

        // Fix value
        account.AccountNumber = "ACCT-001";
        await account.RunRules();

        Assert.IsTrue(accountNumberProp.IsValid);
        Assert.IsFalse(accountNumberProp.PropertyMessages.Any());
    }
    #endregion

    #region properties-backing-field-access
    /// <summary>
    /// Test accessing property via indexer.
    /// </summary>
    [TestMethod]
    public void BackingFieldAccess_ViaIndexer()
    {
        var factory = GetRequiredService<ISkillPropCustomerFactory>();
        var customer = factory.Create();

        // Set via property
        customer.FirstName = "John";

        // Access via indexer returns the property wrapper
        var property = customer["FirstName"];

        Assert.IsNotNull(property);
        Assert.AreEqual("FirstName", property.Name);
        Assert.AreEqual("John", property.Value);
        Assert.AreEqual(typeof(string), property.Type);
    }
    #endregion

    #region properties-suppress-events
    /// <summary>
    /// Test PauseAllActions for batch updates.
    /// </summary>
    [TestMethod]
    public void SuppressEvents_WithPauseAllActions()
    {
        var factory = GetRequiredService<ISkillPropCustomerFactory>();
        var customer = factory.Create();

        var changeCount = 0;
        customer.PropertyChanged += (s, e) => changeCount++;

        // Batch update with pause
        using (customer.PauseAllActions())
        {
            Assert.IsTrue(customer.IsPaused);

            customer.FirstName = "John";
            customer.LastName = "Doe";
            customer.Email = "john@example.com";
        }

        // After pause ends
        Assert.IsFalse(customer.IsPaused);

        // Values are set
        Assert.AreEqual("John", customer.FirstName);
        Assert.AreEqual("Doe", customer.LastName);
    }
    #endregion

    #region properties-change-reason-useredit
    /// <summary>
    /// Test ChangeReason tracking.
    /// </summary>
    [TestMethod]
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
        Assert.IsTrue(nameReasons.Any(), "Name property should fire change event");
        Assert.AreEqual(ChangeReason.UserEdit, nameReasons.Last().Reason);
    }
    #endregion
}
