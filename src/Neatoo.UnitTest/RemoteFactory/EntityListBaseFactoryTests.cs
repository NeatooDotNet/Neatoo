using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.UnitTest.RemoteFactory;

[TestClass]
public class EntityListBaseFactoryTests
{
    private IServiceScope serverScope;
    private IServiceScope clientScope;

    [TestInitialize]
    public void TestIntialize()
    {
        var scopes = ClientServerContainers.Scopes();
        serverScope = scopes.server;
        clientScope = scopes.client;
    }

    [TestMethod]
    public void EntityListBaseFactoryTests_Fetch_IsModified_False()
    {
        var factory = clientScope.GetRequiredService<IEntityObjectListFactory>();
        var result = factory.Fetch();

        Assert.IsFalse(result.IsModified);
        Assert.IsFalse(result.IsNew);
    }

    [TestMethod]
    public async Task EntityListBaseFactoryTests_FetchDeleteSave_IsModified_False()
    {
        var factory = clientScope.GetRequiredService<IEntityObjectListFactory>();
        var result = factory.Fetch();

        result.Remove(result.First());

        result = factory.Save(result);

        Assert.IsFalse(result.IsModified);
        Assert.IsFalse(result.IsNew);
        Assert.AreEqual(2, result.Count);
    }
}
