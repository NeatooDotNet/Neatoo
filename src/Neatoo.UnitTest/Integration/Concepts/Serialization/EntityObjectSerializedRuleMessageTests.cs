using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

public class EntityRule : RuleBase<EntityRuleMessages>
{
    public EntityRule()
    {
        TriggerProperties.Add(new TriggerProperty<EntityRuleMessages>(t => t.Id));
    }
    protected override IRuleMessages Execute(EntityRuleMessages target)
    {
        if (target.Id == 2)
        {
            return (nameof(EntityRuleMessages.Id), "Id is 2").AsRuleMessages();
        }
        return IRuleMessages.None;
    }
}

[Factory]
public partial class EntityRuleMessages : EntityBase<EntityRuleMessages>
{
    public EntityRuleMessages() : base(new EntityBaseServices<EntityRuleMessages>(null))
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

[TestClass]
public class EntityObjectSerializedRuleMessageTests
{
    IServiceScope scope;
    NeatooJsonSerializer resolver;
    EntityRuleMessages target = new EntityRuleMessages();

    [TestInitialize]
    public async Task TestInitialize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        resolver = scope.GetRequiredService<NeatooJsonSerializer>();
        await target.RunRules();
    }

    private string Serialize(object target)
    {
        return resolver.Serialize(target);
    }

    private EntityRuleMessages? Deserialize(string json)
    {
        return resolver.Deserialize<EntityRuleMessages>(json);
    }

    [TestMethod]
    public void EntityObjectSerializedRuleMessageTests_IsValid_False()
    {
        Assert.IsFalse(target.IsValid);
    }

    [TestMethod]
    public void EntityObjectSerializedRuleMessageTests_Deserialize_IsValid_False()
    {
        var json = Serialize(target);
        var deserialized = Deserialize(json);
        Assert.IsFalse(deserialized.IsValid);
    }

    [TestMethod]
    public void EntityObjectSerializedRuleMessageTests_Deserialize_IsValid_Fixed()
    {
        var json = Serialize(target);
        var deserialized = Deserialize(json);
        target.Id = 5;
        target.Required = 5;
        Assert.IsTrue(target.IsValid);
    }

    [TestMethod]
    public void EntityObjectSerializedRuleMessageTests_Deserialize_IsValid_FluentFixed()
    {
        target.Id = 1;
        var json = Serialize(target);
        var deserialized = Deserialize(json);
        target.Id = 5;
        target.Required = 5;
        Assert.IsTrue(target.IsValid);
    }
}
