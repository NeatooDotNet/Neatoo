using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.UnitTest.SystemTextJson.ValidateTests;


[TestClass]
public class FatClientValidateListTests
{
    IServiceScope scope;
    IValidateObjectList target;
    IValidateObject child;
    NeatooJsonSerializer resolver;

    [TestInitialize]
    public void TestInitailize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        target = scope.GetRequiredService<IValidateObjectList>();
        resolver = scope.GetRequiredService<NeatooJsonSerializer>();
        child = scope.GetRequiredService<IValidateObject>();
        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();
        target.Add(child);
    }

    [TestMethod]
    public void FatClientListValidate_Serialize()
    {

        var result = Serialize(target);

        Assert.IsTrue(result.Contains(child.ID.ToString()));
        Assert.IsTrue(result.Contains(child.Name));
    }

    [TestMethod]
    public void FatClientListValidate_Serialize_Invalid()
    {

        child.Name = "Error";

        var result = Serialize(target);

        Assert.IsFalse(target.IsValid);
        Assert.IsTrue(result.Contains("Error")); // Weak check
    }

    private string Serialize(object target)
    {
        return resolver.Serialize(target);
    }

    private IValidateObjectList Deserialize(string json)
    {
        return resolver.Deserialize<IValidateObjectList>(json);
    }

    [TestMethod]
    public void FatClientListValidate_Deserialize()
    {

        var json = Serialize(target);

        var newTarget = Deserialize(json);
    }


    [TestMethod]
    public void FatClientListValidate_Deserialize_Child()
    {

        var json = Serialize(target);

        var newTarget = Deserialize(json);

        Assert.AreEqual(child.ID, newTarget.Single().ID);
        Assert.AreEqual(child.Name, newTarget.Single().Name);

    }

    [TestMethod]
    public void FatClientListValidate_Deserialize_Child_RuleManager()
    {

        child.Name = "Error";
        Assert.IsFalse(child.IsValid);

        var json = Serialize(target);
        var newTarget = Deserialize(json);


        Assert.IsFalse(newTarget.IsValid);
        Assert.IsTrue(newTarget.IsSelfValid);

        Assert.IsFalse(newTarget.Single().IsValid);
        Assert.IsFalse(newTarget.Single().IsSelfValid);
        Assert.AreEqual(child.RuleRunCount, newTarget.Single().RuleRunCount);

    }


}

