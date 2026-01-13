/// <summary>
/// Code samples for docs/testing.md
///
/// Snippets in this file:
/// - docs:testing:sync-rule-definition
/// - docs:testing:async-rule-definition
/// - docs:testing:rule-with-parent-definition
///
/// Corresponding tests: TestingRuleSamplesTests.cs
/// </summary>

using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Neatoo.Samples.DomainModel.Testing;

#region sync-rule-definition
/// <summary>
/// Interface for entities validated by NameValidationRule.
/// </summary>
public interface INamedEntity : IValidateBase
{
    string? Name { get; set; }
    int? Id { get; set; }
}

/// <summary>
/// Simple sync rule that validates Name and Id fields.
/// Demonstrates basic RuleBase pattern for testing documentation.
/// </summary>
public class NameValidationRule : RuleBase<INamedEntity>
{
    public NameValidationRule() : base(e => e.Name, e => e.Id) { }

    protected override IRuleMessages Execute(INamedEntity target)
    {
        // Use RuleMessages.If for simple conditional checks
        // For multiple potential errors, collect and return
        return RuleMessages
            .If(string.IsNullOrWhiteSpace(target.Name), nameof(target.Name), "Name is required")
            .ElseIf(() => target.Id is null or <= 0, nameof(target.Id), "Id must be a positive number");
    }
}
#endregion

#region async-rule-definition
/// <summary>
/// Interface for uniqueness checking - typically generated from a [Factory] Command.
/// </summary>
public interface ICheckNameUnique
{
    Task<bool> IsUnique(string name, int? excludeId);
}

/// <summary>
/// Interface for entities with IsModified tracking (extends IEntityBase).
/// </summary>
public interface INamedEntityWithTracking : IEntityBase
{
    string? Name { get; set; }
    int? Id { get; set; }
}

/// <summary>
/// Async rule with injected dependency for database validation.
/// Demonstrates AsyncRuleBase pattern for testing documentation.
/// </summary>
public class UniqueNameAsyncRule : AsyncRuleBase<INamedEntityWithTracking>
{
    private readonly ICheckNameUnique _checkUnique;

    public UniqueNameAsyncRule(ICheckNameUnique checkUnique) : base(e => e.Name)
    {
        _checkUnique = checkUnique;
    }

    protected override async Task<IRuleMessages> Execute(INamedEntityWithTracking target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Name))
            return None;

        // Skip check if Name hasn't been modified (optimization for existing entities)
        if (!target[nameof(target.Name)].IsModified)
            return None;

        var isUnique = await _checkUnique.IsUnique(target.Name, target.Id);

        return isUnique
            ? None
            : (nameof(target.Name), "Name already exists").AsRuleMessages();
    }
}
#endregion

#region rule-with-parent-definition
/// <summary>
/// Interface for a line item that has a parent order.
/// </summary>
public interface ILineItem : IValidateBase
{
    int Quantity { get; set; }
    IOrderHeader? Parent { get; }
}

/// <summary>
/// Interface for the parent order with a quantity limit.
/// </summary>
public interface IOrderHeader
{
    int MaxQuantityPerLine { get; }
}

/// <summary>
/// Rule that validates against parent entity properties.
/// Demonstrates cross-entity validation for testing documentation.
/// </summary>
public class QuantityLimitRule : RuleBase<ILineItem>
{
    public QuantityLimitRule() : base(l => l.Quantity) { }

    protected override IRuleMessages Execute(ILineItem target)
    {
        if (target.Parent is null)
            return None;

        if (target.Quantity > target.Parent.MaxQuantityPerLine)
        {
            return (nameof(target.Quantity),
                $"Quantity cannot exceed {target.Parent.MaxQuantityPerLine}").AsRuleMessages();
        }

        return None;
    }
}
#endregion

#region correct-real-neatoo-class
/// <summary>
/// CORRECT - Use real Neatoo classes for integration tests.
/// Don't mock Neatoo interfaces like IValidateBase.
/// </summary>
[SuppressFactory]
public class TestPerson : ValidateBase<TestPerson>
{
    public TestPerson() : base(new ValidateBaseServices<TestPerson>()) { }

    public string? Name { get => Getter<string>(); set => Setter(value); }
}
#endregion
