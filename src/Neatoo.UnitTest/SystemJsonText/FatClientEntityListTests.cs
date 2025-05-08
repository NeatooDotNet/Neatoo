using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.UnitTest.SystemTextJson.EntityTests;


[TestClass]
public class FatClientEntityListTests
{
    IServiceScope scope;
    IEntityObjectList target;
    NeatooJsonSerializer resolver;

    [TestInitialize]
    public void TestInitailize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        target = scope.GetRequiredService<IEntityObjectList>();
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
    public void FatClientEntityList_Serialize()
    {

        var result = Serialize(target);
    }

    [TestMethod]
    public void FatClientEntityList_Deserialize()
    {

        var json = Serialize(target);

        var newTarget = Deserialize<IEntityObjectList>(json);
    }

    [TestMethod]
    public void FatClientEntityList_Deserialize_Child()
    {
        var child = scope.GetRequiredService<IEntityObject>();
        target.Add(child);

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();

        var json = Serialize(target);

        var newTarget = Deserialize<IEntityObjectList>(json);

        Assert.IsNotNull(newTarget.Single());
        Assert.AreEqual(child.ID, newTarget.Single().ID);
        Assert.AreEqual(child.Name, newTarget.Single().Name);

    }

    [TestMethod]
    public void FatClientEntityList_Deserialize_Child_ParentRef()
    {
        var parent = scope.GetRequiredService<IEntityObject>();
        parent.ChildList = target;

        var child = scope.GetRequiredService<IEntityObject>();
        target.Add(child);

        Assert.AreSame(child.Parent, parent);

        var json = Serialize(parent);

        // ITaskRespository and ILogger constructor parameters are injected by Autofac 
        var newParent = Deserialize<IEntityObject>(json);

        Assert.IsNotNull(newParent);
        var newChild = newParent.ChildList.Single();

        Assert.AreSame(child.Parent, parent);

        Assert.AreEqual(child.ID, newChild.ID);
        Assert.AreEqual(child.Name, newChild.Name);
    }

    [TestMethod]
    public void FatClientEntityList_IsModified()
    {
        var child = scope.GetRequiredService<IEntityObject>();
        target.Add(child);

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();

        Assert.IsTrue(target.IsModified);
        Assert.IsFalse(target.IsSelfModified);


        var json = Serialize(target);
        var newTarget = Deserialize<IEntityObjectList>(json);

        Assert.IsTrue(newTarget.IsModified);
        Assert.IsFalse(newTarget.IsSelfModified);

    }

    [TestMethod]
    public void FatClientEntityList_IsModified_False()
    {
        var child = scope.GetRequiredService<IEntityObject>();
        target.Add(child);

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();

        child.MarkUnmodified();

        Assert.IsFalse(target.IsModified);
        Assert.IsFalse(target.IsSelfModified);

        var json = Serialize(target);

        var newTarget = Deserialize<IEntityObjectList>(json);

        Assert.IsFalse(newTarget.IsModified);
        Assert.IsFalse(newTarget.IsSelfModified);
    }

    [TestMethod]
    public void FatClientEntityList_IsNew_False()
    {
        var child = scope.GetRequiredService<IEntityObject>();
        target.Add(child);

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();
        child.MarkOld();

        Assert.IsFalse(target.IsNew);

        var json = Serialize(target);

        var newTarget = Deserialize<IEntityObjectList>(json);

        Assert.IsFalse(newTarget.IsNew);

    }

    [TestMethod]
    public void FatClientEntityList_DeletedList()
    {
        var child = scope.GetRequiredService<IEntityObject>();
        target.Add(child);

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();
        child.MarkOld();

        target.Remove(child);

        Assert.IsTrue(child.IsDeleted);
        Assert.IsTrue(target.DeletedList.Contains(child));

        var json = Serialize(target);

        var newTarget = Deserialize<IEntityObjectList>(json);

        Assert.IsTrue(newTarget.DeletedList.Any(d => d.ID == child.ID));
    }
}

