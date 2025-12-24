using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.Portal;

/// <summary>
/// Integration tests for BaseObject factory operations.
/// Tests Create and Fetch operations through the client-server portal.
/// </summary>
[TestClass]
public class BaseFactoryTests : ClientServerTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScopes();
    }

    [TestMethod]
    public async Task BaseFactoryTests_IBaseObjectCreateBaseObject()
    {
        var factory = GetClientService<IBaseObjectFactory>();

        var result = await factory.CreateAsync();

        Assert.IsTrue(result.CreateCalled);
    }

    [TestMethod]
    public void BaseFactoryTests_IBaseObjectCreateBaseObjectInt()
    {
        var factory = GetClientService<IBaseObjectFactory>();

        var criteria = 10;

        var result = factory.Create(criteria);

        Assert.AreEqual(criteria, result.IntCriteria);
    }

    [TestMethod]
    public void BaseFactoryTests_BaseObjectCreateDependency()
    {
        var factory = GetClientService<IBaseObjectFactory>();

        var result = factory.Create(2, 10d);

        Assert.IsNotNull(result.MultipleCriteria);
    }

    [TestMethod]
    public void BaseFactoryTests_IBaseObjectFetchBaseObject()
    {
        var factory = GetClientService<IBaseObjectFactory>();

        var result = factory.Fetch();

        Assert.IsNotNull(result.FetchCalled);
    }

    [TestMethod]
    public async Task BaseFactoryTests_IBaseObjectFetchBaseObjectGuid()
    {
        var factory = GetClientService<IBaseObjectFactory>();

        var guidCriteria = Guid.NewGuid();

        var result = await factory.Fetch(guidCriteria);

        Assert.AreEqual(guidCriteria, result.GuidCriteria);
    }
}
