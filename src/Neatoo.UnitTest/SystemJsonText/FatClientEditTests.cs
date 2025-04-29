using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.UnitTest.SystemTextJson.EditTests;


[TestClass]
public class FatClientEditTests
{
    IServiceScope scope;
    IEditObject target;
    Guid Id = Guid.NewGuid();
    string Name = Guid.NewGuid().ToString();
    NeatooJsonSerializer resolver;

    [TestInitialize]
    public void TestInitailize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        target = scope.GetRequiredService<IEditObject>();
        target.ID = Id;
        target.Name = Name;
        resolver = scope.GetRequiredService<NeatooJsonSerializer>();
    }

    private string Serialize(object target)
    {
        return resolver.Serialize(target);
    }

    private IEditObject Deserialize(string json)
    {
        return resolver.Deserialize<IEditObject>(json);
    }

    [TestMethod]
    public void FatClientEdit_Serialize()
    {
        var result = Serialize(target);

        Assert.IsTrue(result.Contains(Id.ToString()));
        Assert.IsTrue(result.Contains(Name));
    }

    [TestMethod]
    public void FatClientEdit_Deserialize()
    {
        var json = Serialize(target);

        var newTarget = Deserialize(json);

        Assert.AreEqual(target.ID, newTarget.ID);
        Assert.AreEqual(target.Name, newTarget.Name);
    }


    [TestMethod]
    public void FatClientEdit_Deserialize_Modify()
    {
        var json = Serialize(target);

        var newTarget = Deserialize(json);

        var id = Guid.NewGuid();
        newTarget.ID = id;
        Assert.AreEqual(id, newTarget.ID);

    }

    [TestMethod]
    public void FatClientEdit_Deserialize_Child()
    {
        var child = target.Child = scope.GetRequiredService<IEditObject>();

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();

        var json = Serialize(target);

        var newTarget = Deserialize(json);

        Assert.IsNotNull(newTarget.Child);
        Assert.AreEqual(child.ID, newTarget.Child.ID);
        Assert.AreEqual(child.Name, newTarget.Child.Name);
    }

    [TestMethod]
    public void FatClientEdit_Deserialize_Child_ParentRef()
    {
        var child = target.Child = scope.GetRequiredService<IEditObject>();

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
    public void FatClientEdit_IsModified()
    {
        var json = Serialize(target);

        var newTarget = Deserialize(json);

        Assert.IsTrue(newTarget.IsModified);
        Assert.IsTrue(newTarget.IsSelfModified);
    }

    [TestMethod]
    public void FatClientEdit_IsModified_False()
    {

        target.MarkUnmodified();
        var json = Serialize(target);

        var newTarget = Deserialize(json);

        Assert.IsFalse(newTarget.IsModified);
        Assert.IsFalse(newTarget.IsSelfModified);

    }

    [TestMethod]
    public void FatClientEdit_IsNew()
    {

        target.MarkNew();
        var json = Serialize(target);

        var newTarget = Deserialize(json);

        Assert.IsTrue(newTarget.IsNew);

    }

    [TestMethod]
    public void FatClientEdit_IsNew_False()
    {

        target.MarkOld();

        var json = Serialize(target);

        var newTarget = Deserialize(json);

        Assert.IsFalse(newTarget.IsNew);

    }

    [TestMethod]
    public void FatClientEdit_IsChild()
    {

        target.MarkAsChild();

        var json = Serialize(target);

        var newTarget = Deserialize(json);

        Assert.IsTrue(newTarget.IsChild);

    }

    [TestMethod]
    public void FatClientEdit_IsChild_False()
    {

        var json = Serialize(target);

        var newTarget = Deserialize(json);

        Assert.IsFalse(newTarget.IsChild);

    }

    [TestMethod]
    public void FatClientEdit_ModifiedProperties()
    {

        var orig = target.ModifiedProperties.ToList();

        var json = Serialize(target);

        var newTarget = Deserialize(json);

        var result = newTarget.ModifiedProperties.ToList();

        CollectionAssert.AreEquivalent(orig, result);

    }

    [TestMethod]
    public void FatClientEdit_IsDeleted()
    {
        target.Delete();

        var json = Serialize(target);

        var newTarget = Deserialize(json);

        Assert.IsTrue(target.IsDeleted);
        Assert.IsTrue(target.IsModified);
        Assert.IsTrue(target.IsSelfModified);
        Assert.IsTrue(newTarget.IsDeleted);
        Assert.IsTrue(newTarget.IsModified);
        Assert.IsTrue(newTarget.IsSelfModified);
    }
}

