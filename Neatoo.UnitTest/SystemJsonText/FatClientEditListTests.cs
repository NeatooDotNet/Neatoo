using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.UnitTest.SystemTextJson.EditTests;


[TestClass]
public class FatClientEditListTests
{
    IServiceScope scope;
    IEditObjectList target;
    NeatooJsonSerializer resolver;

    [TestInitialize]
    public void TestInitailize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        target = scope.GetRequiredService<IEditObjectList>();
        resolver = scope.GetRequiredService<NeatooJsonSerializer>();
    }

    private string Serialize(object target)
    {
        return resolver.Serialize(target);
    }

    private T Deserialize<T>(string json)
    {
        return resolver.Deserialize<T>(json);
    }

    [TestMethod]
    public void FatClientEditList_Serialize()
    {

        var result = Serialize(target);
    }

    [TestMethod]
    public void FatClientEditList_Deserialize()
    {

        var json = Serialize(target);

        var newTarget = Deserialize<IEditObjectList>(json);
    }

    [TestMethod]
    public void FatClientEditList_Deserialize_Child()
    {
        var child = scope.GetRequiredService<IEditObject>();
        target.Add(child);

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();

        var json = Serialize(target);

        var newTarget = Deserialize<IEditObjectList>(json);

        Assert.IsNotNull(newTarget.Single());
        Assert.AreEqual(child.ID, newTarget.Single().ID);
        Assert.AreEqual(child.Name, newTarget.Single().Name);

    }

    [TestMethod]
    public void FatClientEditList_Deserialize_Child_ParentRef()
    {
        var parent = scope.GetRequiredService<IEditObject>();
        parent.ChildList = target;

        var child = scope.GetRequiredService<IEditObject>();
        target.Add(child);

        Assert.AreSame(child.Parent, parent);

        var json = Serialize(parent);

        // ITaskRespository and ILogger constructor parameters are injected by Autofac 
        var newParent = Deserialize<IEditObject>(json);

        Assert.IsNotNull(newParent);
        var newChild = newParent.ChildList.Single();

        Assert.AreSame(child.Parent, parent);

        Assert.AreEqual(child.ID, newChild.ID);
        Assert.AreEqual(child.Name, newChild.Name);
    }

    [TestMethod]
    public void FatClientEditList_IsModified()
    {
        var child = scope.GetRequiredService<IEditObject>();
        target.Add(child);

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();

        Assert.IsTrue(target.IsModified);
        Assert.IsFalse(target.IsSelfModified);


        var json = Serialize(target);
        var newTarget = Deserialize<IEditObjectList>(json);

        Assert.IsTrue(newTarget.IsModified);
        Assert.IsFalse(newTarget.IsSelfModified);

    }

    [TestMethod]
    public void FatClientEditList_IsModified_False()
    {
        var child = scope.GetRequiredService<IEditObject>();
        target.Add(child);

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();

        child.MarkUnmodified();

        Assert.IsFalse(target.IsModified);
        Assert.IsFalse(target.IsSelfModified);

        var json = Serialize(target);

        var newTarget = Deserialize<IEditObjectList>(json);

        Assert.IsFalse(newTarget.IsModified);
        Assert.IsFalse(newTarget.IsSelfModified);
    }

    [TestMethod]
    public void FatClientEditList_IsNew_False()
    {
        var child = scope.GetRequiredService<IEditObject>();
        target.Add(child);

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();
        child.MarkOld();

        Assert.IsFalse(target.IsNew);

        var json = Serialize(target);

        var newTarget = Deserialize<IEditObjectList>(json);

        Assert.IsFalse(newTarget.IsNew);

    }
}

