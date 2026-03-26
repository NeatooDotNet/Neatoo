using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Xunit;

namespace Samples;

// =============================================================================
// SHARED RULES PATTERN: Interface-typed AsyncRuleBase for cross-entity rules
// =============================================================================

// Step 1: Define a shared interface extending IValidateBase
#region shared-rule-interface
public interface IHasUniqueId : IValidateBase
{
    int? ID { get; }
    Guid ExcludeId { get; }
}
#endregion

// Step 2: Write a non-generic rule on the shared interface
#region shared-rule-class
public class IdUniquenessRule : AsyncRuleBase<IHasUniqueId>, IIdUniquenessRule
{
    private readonly IIdUniquenessService _service;

    public IdUniquenessRule(IIdUniquenessService service)
        : base(e => e.ID)
    {
        _service = service;
    }

    protected override async Task<IRuleMessages> Execute(
        IHasUniqueId target, CancellationToken? token = null)
    {
        if (target.ID == null) return None;

        var isUnique = await _service.IsUniqueAsync(target.ID.Value, target.ExcludeId);
        return isUnique
            ? None
            : (nameof(target.ID), $"ID {target.ID} is already assigned.").AsRuleMessages();
    }
}
#endregion

// Step 3: Create a DI interface for the rule
#region shared-rule-di-interface
public interface IIdUniquenessRule : IRule<IHasUniqueId> { }
#endregion

// Step 4: Entities implement the shared interface and inject the rule
#region shared-rule-entity-usage
[Factory]
internal partial class SharedRuleEmployee : EntityBase<SharedRuleEmployee>, ISharedRuleEmployee
{
    public SharedRuleEmployee(
        IEntityBaseServices<SharedRuleEmployee> services,
        IIdUniquenessRule idUniquenessRule) : base(services)
    {
        RuleManager.AddRule(idUniquenessRule);
    }

    [Create]
    public void Create() { }

    public Guid ExcludeId => EmployeeID;
    public partial Guid EmployeeID { get; set; }
    public partial int? ID { get; set; }
    public partial string Name { get; set; }
}
#endregion

public interface ISharedRuleEmployee : IEntityBase, IHasUniqueId
{
    new int? ID { get; set; }
    string Name { get; set; }
}

// A second entity reusing the same rule
[Factory]
internal partial class SharedRuleProduct : EntityBase<SharedRuleProduct>, ISharedRuleProduct
{
    public SharedRuleProduct(
        IEntityBaseServices<SharedRuleProduct> services,
        IIdUniquenessRule idUniquenessRule) : base(services)
    {
        RuleManager.AddRule(idUniquenessRule);
    }

    [Create]
    public void Create() { }

    public Guid ExcludeId => ProductID;
    public partial Guid ProductID { get; set; }
    public partial int? ID { get; set; }
    public partial string ProductName { get; set; }
}

public interface ISharedRuleProduct : IEntityBase, IHasUniqueId
{
    new int? ID { get; set; }
    string ProductName { get; set; }
}

// Step 5: Register in DI
#region shared-rule-di-registration
// Use transient lifetime -- each entity instance gets its own rule instance
// because rules track execution state.
// services.AddTransient<IIdUniquenessRule, IdUniquenessRule>();
#endregion

// Contrast: entity-specific rules don't need the shared pattern
#region shared-rule-entity-specific
// For rules specific to one entity type, inject the service and new the rule:
// RuleManager.AddRule(new HireDateRule()); // no DI needed, entity-specific
#endregion

// =============================================================================
// Mock service and DI interface
// =============================================================================

public interface IIdUniquenessService
{
    Task<bool> IsUniqueAsync(int id, Guid excludeId);
}

public class MockIdUniquenessService : IIdUniquenessService
{
    private readonly HashSet<int> _takenIds = [42];

    public Task<bool> IsUniqueAsync(int id, Guid excludeId)
    {
        return Task.FromResult(!_takenIds.Contains(id));
    }
}

// =============================================================================
// Tests
// =============================================================================

public class SharedRulesSamples : SamplesTestBase
{
    [Fact]
    public async Task SharedRule_ValidatesAcrossEntityTypes()
    {
        var employeeFactory = GetRequiredService<ISharedRuleEmployeeFactory>();
        var employee = employeeFactory.Create();
        employee.Name = "Alice";

        // Unique ID passes validation
        employee.ID = 1;
        await employee.WaitForTasks();
        Assert.True(employee.IsValid);

        // Taken ID fails validation
        employee.ID = 42;
        await employee.WaitForTasks();
        Assert.False(employee.IsValid);

        // Same rule works on a different entity type
        var productFactory = GetRequiredService<ISharedRuleProductFactory>();
        var product = productFactory.Create();
        product.ProductName = "Widget";

        product.ID = 1;
        await product.WaitForTasks();
        Assert.True(product.IsValid);

        product.ID = 42;
        await product.WaitForTasks();
        Assert.False(product.IsValid);
    }
}
