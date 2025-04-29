using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.SystemJsonText;

public class EditRule : RuleBase<EditRuleMessages>
{
    public EditRule()
    {
        TriggerProperties.Add(new TriggerProperty<EditRuleMessages>(t => t.Id));
    }
    protected override IRuleMessages Execute(EditRuleMessages target)
    {
        if (target.Id == 2)
        {
            return (nameof(EditRuleMessages.Id), "Id is 2").AsRuleMessages();
        }
        return IRuleMessages.None;
    }
}

[Factory]
public partial class EditRuleMessages : EditBase<EditRuleMessages>
{
    public EditRuleMessages() : base(new EditBaseServices<EditRuleMessages>(null))
    {
        RuleManager.AddValidation((e) =>
        {
            if (e.Id == 0)
            {
                return "Id is 0";
            }
            return string.Empty;
        }, (t) => t.Id);

        RuleManager.AddRule(new EditRule());

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
public class EditObjectSerializedRuleMessageTests
{
    IServiceScope scope;
    NeatooJsonSerializer resolver;
    EditRuleMessages target = new EditRuleMessages();

    [TestInitialize]
    public async Task TestInitailize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        resolver = scope.GetRequiredService<NeatooJsonSerializer>();
        await target.RunRules();
    }

    private string Serialize(object target)
    {
        return resolver.Serialize(target);
    }

    private EditRuleMessages? Deserialize(string json)
    {
        return resolver.Deserialize<EditRuleMessages>(json);
    }

    [TestMethod]
    public void EditObjectSerializedRuleMessageTests_IsValid_False()
    {
        Assert.IsFalse(target.IsValid);
    }

    [TestMethod]
    public void EditObjectSerializedRuleMessageTests_Deserialize_IsValid_False()
    {
        var json = Serialize(target);
        var deserialized = Deserialize(json);
        Assert.IsFalse(deserialized.IsValid);
    }

    [TestMethod]
    public void EditObjectSerializedRuleMessageTests_Deserialize_IsValid_Fixed()
    {
        var json = Serialize(target);
        var deserialized = Deserialize(json);
        target.Id = 5;
        target.Required = 5;
        Assert.IsTrue(target.IsValid);
    }

    [TestMethod]
    public void EditObjectSerializedRuleMessageTests_Deserialize_IsValid_FluentFixed()
    {
        target.Id = 1;
        var json = Serialize(target);
        var deserialized = Deserialize(json);
        target.Id = 5;
        target.Required = 5;
        Assert.IsTrue(target.IsValid);
    }
}
