using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

#region Rules

/// <summary>
/// First rule targeting Name property - validates Name is not empty.
/// </summary>
public class NameNotEmptyRule : RuleBase<IStableRuleIdEntity>
{
    public NameNotEmptyRule()
    {
        TriggerProperties.Add(new TriggerProperty<IStableRuleIdEntity>(t => t.Name));
    }

    protected override IRuleMessages Execute(IStableRuleIdEntity target)
    {
        if (string.IsNullOrEmpty(target.Name))
        {
            return (nameof(IStableRuleIdEntity.Name), "Name cannot be empty (NameNotEmptyRule)").AsRuleMessages();
        }
        return IRuleMessages.None;
    }
}

/// <summary>
/// Second rule targeting Name property - validates Name length.
/// Different rule, same property - tests that RuleIds are distinct.
/// </summary>
public class NameLengthRule : RuleBase<IStableRuleIdEntity>
{
    public NameLengthRule()
    {
        TriggerProperties.Add(new TriggerProperty<IStableRuleIdEntity>(t => t.Name));
    }

    protected override IRuleMessages Execute(IStableRuleIdEntity target)
    {
        if (target.Name?.Length > 10)
        {
            return (nameof(IStableRuleIdEntity.Name), "Name too long (NameLengthRule)").AsRuleMessages();
        }
        return IRuleMessages.None;
    }
}

/// <summary>
/// Rule targeting Value property.
/// </summary>
public class ValuePositiveRule : RuleBase<IStableRuleIdEntity>
{
    public ValuePositiveRule()
    {
        TriggerProperties.Add(new TriggerProperty<IStableRuleIdEntity>(t => t.Value));
    }

    protected override IRuleMessages Execute(IStableRuleIdEntity target)
    {
        if (target.Value < 0)
        {
            return (nameof(IStableRuleIdEntity.Value), "Value must be positive (ValuePositiveRule)").AsRuleMessages();
        }
        return IRuleMessages.None;
    }
}

#endregion

#region Entity

public interface IStableRuleIdEntity : IEntityBase
{
    string? Name { get; set; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? FullName { get; set; }
    int Value { get; set; }
    string? Email { get; set; }
    int? RequiredField { get; set; }

    /// <summary>
    /// Test helper to access rule messages with their RuleId for serialization testing.
    /// </summary>
    IEnumerable<IRuleMessage> BrokenRuleMessages { get; }
}

/// <summary>
/// Entity designed to test stable rule identification across serialization.
/// Has multiple rules of different types targeting same and different properties.
/// </summary>
[Factory]
public partial class StableRuleIdEntity : EntityBase<StableRuleIdEntity>, IStableRuleIdEntity
{
    public StableRuleIdEntity(IEntityBaseServices<StableRuleIdEntity> services) : base(services)
    {
        // Injected rules (2 rules on Name, 1 on Value)
        RuleManager.AddRule(new NameNotEmptyRule());
        RuleManager.AddRule(new NameLengthRule());
        RuleManager.AddRule(new ValuePositiveRule());

        // Fluent validation on Name (third rule on same property)
        RuleManager.AddValidation(
            e => e.Name == "forbidden" ? "Name cannot be 'forbidden'" : "",
            e => e.Name);

        // Fluent validation on Value (second rule on Value)
        RuleManager.AddValidation(
            e => e.Value > 1000 ? "Value cannot exceed 1000" : "",
            e => e.Value);

        // Fluent validation on Email
        RuleManager.AddValidation(
            e => e.Email != null && !e.Email.Contains('@') ? "Email must contain @" : "",
            e => e.Email);

        // AddAction rule to compute FullName from FirstName and LastName
        // This tests that AddAction rules get stable IDs
        RuleManager.AddAction(
            e => e.FullName = $"{e.FirstName} {e.LastName}",
            e => e.FirstName,
            e => e.LastName);
    }

    public partial string? Name { get; set; }
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
    public partial string? FullName { get; set; }
    public partial int Value { get; set; }
    public partial string? Email { get; set; }

    [Required]
    public partial int? RequiredField { get; set; }

    /// <inheritdoc />
    public IEnumerable<IRuleMessage> BrokenRuleMessages
    {
        get
        {
            if (PropertyManager is IValidatePropertyManagerInternal<IValidateProperty> pmInternal)
            {
                foreach (var prop in pmInternal.GetProperties)
                {
                    // Access RuleMessages via reflection since it's not on the interface
                    var ruleMessagesProp = prop.GetType().GetProperty("RuleMessages");
                    if (ruleMessagesProp?.GetValue(prop) is IEnumerable<IRuleMessage> messages)
                    {
                        foreach (var msg in messages)
                            yield return msg;
                    }
                }
            }
        }
    }
}

#endregion
