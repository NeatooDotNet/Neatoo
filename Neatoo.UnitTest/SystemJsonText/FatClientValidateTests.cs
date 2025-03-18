using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.UnitTest.SystemTextJson;


[TestClass]
public class FatClientValidateTests
{
    IServiceScope scope;
    IValidateObject target;
    Guid Id = Guid.NewGuid();
    string Name = Guid.NewGuid().ToString();
    NeatooJsonSerializer resolver;

    [TestInitialize]
    public void TestInitailize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        target = scope.GetRequiredService<IValidateObject>();
        target.ID = Id;
        target.Name = Name;
        resolver = scope.GetRequiredService<NeatooJsonSerializer>();
    }

    [TestMethod]
    public void FatClientValidate_Serialize()
    {

        var result = Serialize(target);

        Assert.IsTrue(result.Contains(Id.ToString()));
        Assert.IsTrue(result.Contains(Name));
    }

    private string Serialize(object target)
    {
        return resolver.Serialize(target);
    }

    private IValidateObject Deserialize(string json)
    {
        return resolver.Deserialize<IValidateObject>(json);
    }

    [TestMethod]
    public void FatClientValidate_Deserialize()
    {

        var json = Serialize(target);

        // ITaskRespository and ILogger constructor parameters are injected by Autofac 
        var newTarget = Deserialize(json);

        Assert.AreEqual(target.ID, newTarget.ID);
        Assert.AreEqual(target.Name, newTarget.Name);
    }

    [TestMethod]
    public void FatClientValidate_Deserialize_Modify()
    {
        Assert.IsTrue(target.IsValid);

        var json = Serialize(target);

        // ITaskRespository and ILogger constructor parameters are injected by Autofac 
        var newTarget = Deserialize(json);

        newTarget.Name = "Error";
        Assert.IsFalse(newTarget.IsValid);
    }

    [TestMethod]
    public void FatClientValidate_Deserialize_RuleManager()
    {
        target.Name = "Error";
        Assert.IsFalse(target.IsValid);

        var json = Serialize(target);

        // ITaskRespository and ILogger constructor parameters are injected by Autofac 
        var newTarget = Deserialize(json);

        Assert.AreEqual(2, newTarget.RuleRunCount); // Ensure that RuleManager was deserialized, not run
        Assert.AreEqual(2, newTarget.Rules.Count());
        Assert.IsFalse(newTarget.IsValid);

        Assert.IsFalse(newTarget[nameof(IValidateObject.Name)].IsValid);

    }


    [TestMethod]
    public void FatClientValidate_Deserialize_Child()
    {

        var child = target.Child = scope.GetRequiredService<IValidateObject>();

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();

        var json = Serialize(target);

        var newTarget = Deserialize(json);

        Assert.IsNotNull(newTarget.Child);
        Assert.AreSame(newTarget.Child.Parent, newTarget);
        Assert.AreEqual(child.ID, newTarget.Child.ID);
        Assert.AreEqual(child.Name, newTarget.Child.Name);

    }

    [TestMethod]
    public void FatClientValidate_Deserialize_Child_RuleManager()
    {

        var child = target.Child = scope.GetRequiredService<IValidateObject>();

        child.ID = Guid.NewGuid();
        child.Name = "Error";
        Assert.IsFalse(child.IsValid);
        var json = Serialize(target);

        var newTarget = Deserialize(json);

        Assert.IsFalse(newTarget.IsValid);
        Assert.IsTrue(newTarget.IsSelfValid);
        Assert.AreEqual(1, newTarget.RuleRunCount);

        Assert.IsFalse(newTarget.Child.IsValid);
        Assert.IsFalse(newTarget.Child.IsSelfValid);
        Assert.AreEqual(1, newTarget.Child.RuleRunCount);

    }

    [TestMethod]
    public async Task FatClientValidate_Deserialize_ValidateProperty_Child()
    {
        // Ensure ValidateProperty.Child is a reference to the Child 

        var child = target.Child = scope.GetRequiredService<IValidateObject>();

        child.ID = Guid.NewGuid();
        child.Name = "Error";

        Assert.IsFalse(child.IsValid);

        var json = Serialize(target);
        var newTarget = Deserialize(json);

        Assert.IsFalse(newTarget.IsValid);

        child = newTarget.Child;

        await child.RunAllRules();

        Assert.IsFalse(newTarget.IsValid);

        newTarget.Child.Name = "Fine";

        Assert.IsTrue(newTarget.IsValid);

    }

    [TestMethod]
    public void FatClientValidate_Deserialize_Child_ParentRef()
    {

        var child = target.Child = scope.GetRequiredService<IValidateObject>();

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();

        var json = Serialize(target);

        // ITaskRespository and ILogger constructor parameters are injected by Autofac 
        var newTarget = Deserialize(json);

        Assert.IsNotNull(newTarget.Child);
        Assert.AreEqual(child.ID, newTarget.Child.ID);
        Assert.AreEqual(child.Name, newTarget.Child.Name);
        Assert.AreSame(newTarget.Child.Parent, newTarget);

    }

    [TestMethod]
    public void FatClientValidate_Deserialize_MarkInvalid()
    {
        // This caught a really critical issue that lead to the RuleManager.TransferredResults logic
        // After being transferred the RuleIndex values would not match up
        // So the object would be stuck in InValid

        target.MarkInvalid(Guid.NewGuid().ToString());

        var json = Serialize(target);
        var newTarget = Deserialize(json);

        Assert.IsFalse(target.IsValid);
        Assert.IsFalse(newTarget.IsValid);
        Assert.IsNotNull(newTarget.ObjectInvalid);
    }
}

