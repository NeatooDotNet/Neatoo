using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.BaseTests.Objects;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.UnitTest.BaseTests;


[TestClass]
public class FatClientBaseTests
{
    IServiceScope scope;
    IBaseObject target;
    Guid Id = Guid.NewGuid();
    string Name = Guid.NewGuid().ToString();
    NeatooJsonSerializer resolver;

    [TestInitialize]
    public void TestInitailize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        target = scope.GetRequiredService<IBaseObject>();
        target.Id = Id;
        target.StringProperty = Name;
        resolver = scope.GetRequiredService<NeatooJsonSerializer>();
    }

    private string Serialize(object target)
    {
        return resolver.Serialize(target);
    }

    private IBaseObject Deserialize(string json)
    {
        return resolver.Deserialize<IBaseObject>(json);
    }

    [TestMethod]
    public void FatClientBaseTests_Serialize()
    {
        var result = Serialize(target);

        Assert.IsTrue(result.Contains(Id.ToString()));
        Assert.IsTrue(result.Contains(Name));
    }

    [TestMethod]
    public void FatClientBaseTests_Deserialize()
    {
        var json = Serialize(target);

        var newTarget = Deserialize(json);

        Assert.AreEqual(target.Id, newTarget.Id);
        Assert.AreEqual(target.StringProperty, newTarget.StringProperty);
    }

    [TestMethod]
    public void FatClientBaseTests_Deserialize_Child()
    {
        var child = target.Child = scope.GetRequiredService<IBaseObject>();

        child.Id = Guid.NewGuid();
        child.StringProperty = Guid.NewGuid().ToString();

        var json = Serialize(target);

        // ITaskRespository and ILogger constructor parameters are injected by Autofac 
        var newTarget = Deserialize(json);

        Assert.IsNotNull(newTarget.Child);
        Assert.AreEqual(child.Id, newTarget.Child.Id);
        Assert.AreEqual(child.StringProperty, newTarget.Child.StringProperty);
    }

    [TestMethod]
    public void FatClientBaseTests_Deserialize_Child_ParentRef()
    {

        var child = target.Child = scope.GetRequiredService<IBaseObject>();

        child.Id = Guid.NewGuid();
        child.StringProperty = Guid.NewGuid().ToString();

        var json = Serialize(target);

        // ITaskRespository and ILogger constructor parameters are injected by Autofac 
        var newTarget = Deserialize(json);


        Assert.IsNotNull(newTarget.Child);
        Assert.AreEqual(child.Id, newTarget.Child.Id);
        Assert.AreEqual(child.StringProperty, newTarget.Child.StringProperty);
        Assert.AreSame(newTarget.Child.Parent, newTarget);
    }
}

