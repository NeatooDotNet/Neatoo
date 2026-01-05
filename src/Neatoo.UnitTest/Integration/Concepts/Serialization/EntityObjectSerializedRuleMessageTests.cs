using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Neatoo.Rules;
using Neatoo.UnitTest.TestInfrastructure;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// Rule that adds a validation error when Id is 2.
/// </summary>
public class EntityRule : RuleBase<IEntityRuleMessages>
{
    public EntityRule()
    {
        TriggerProperties.Add(new TriggerProperty<IEntityRuleMessages>(t => t.Id));
    }
    protected override IRuleMessages Execute(IEntityRuleMessages target)
    {
        if (target.Id == 2)
        {
            return (nameof(IEntityRuleMessages.Id), "Id is 2").AsRuleMessages();
        }
        return IRuleMessages.None;
    }
}

/// <summary>
/// Interface for EntityRuleMessages to support proper serialization/deserialization.
/// </summary>
public interface IEntityRuleMessages : IEntityBase
{
    int Id { get; set; }
    int? Required { get; set; }
}

/// <summary>
/// Entity for testing rule message serialization behavior.
/// </summary>
[Factory]
public partial class EntityRuleMessages : EntityBase<EntityRuleMessages>, IEntityRuleMessages
{
    public EntityRuleMessages(IEntityBaseServices<EntityRuleMessages> services) : base(services)
    {
        RuleManager.AddValidation((e) =>
        {
            if (e.Id == 0)
            {
                return "Id is 0";
            }
            return string.Empty;
        }, (t) => t.Id);

        RuleManager.AddRule(new EntityRule());

        RuleManager.AddValidation((e) =>
        {
            if (e.Id == 1)
            {
                return "Id is 1";
            }
            return string.Empty;
        }, (t) => t.Id);
    }

    public partial int Id { get; set; }

    [Required]
    public partial int? Required { get; set; }
}

/// <summary>
/// Tests for validating that rule messages survive serialization/deserialization.
/// </summary>
[TestClass]
public class EntityObjectSerializedRuleMessageTests : IntegrationTestBase
{
    private IEntityRuleMessages _target = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        InitializeScope();
        _target = GetRequiredService<IEntityRuleMessages>();
        await _target.RunRules();
    }

    private IEntityRuleMessages DeserializeEntity(string json)
    {
        return Deserialize<IEntityRuleMessages>(json);
    }

    [TestMethod]
    public void EntityObjectSerializedRuleMessageTests_IsValid_False()
    {
        Assert.IsFalse(_target.IsValid);
    }

    [TestMethod]
    public void EntityObjectSerializedRuleMessageTests_Deserialize_IsValid_False()
    {
        var json = Serialize(_target);
        var deserialized = DeserializeEntity(json);
        Assert.IsFalse(deserialized.IsValid);
    }

    [TestMethod]
    public void EntityObjectSerializedRuleMessageTests_Deserialize_IsValid_Fixed()
    {
        var json = Serialize(_target);
        var deserialized = DeserializeEntity(json);
        _target.Id = 5;
        _target.Required = 5;
        Assert.IsTrue(_target.IsValid);
    }

    [TestMethod]
    public void EntityObjectSerializedRuleMessageTests_Deserialize_IsValid_FluentFixed()
    {
        _target.Id = 1;
        var json = Serialize(_target);
        var deserialized = DeserializeEntity(json);
        _target.Id = 5;
        _target.Required = 5;
        Assert.IsTrue(_target.IsValid);
    }
}
