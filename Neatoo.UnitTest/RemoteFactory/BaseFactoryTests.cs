using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.ObjectPortal;

namespace Neatoo.UnitTest.RemoteFactory
{
    [TestClass]
    public class BaseFactoryTests
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
        public async Task BaseFactoryTests_IBaseObjectCreateBaseObject()
        {
            var factory = clientScope.GetRequiredService<IBaseObjectFactory>();

            var result = await factory.CreateAsync();

            Assert.IsTrue(result.CreateCalled);
        }

        [TestMethod]
        public void BaseFactoryTests_IBaseObjectCreateBaseObjectInt()
        {
            var factory = clientScope.GetRequiredService<IBaseObjectFactory>();

            var criteria = 10;

            var result = factory.Create(criteria);

            Assert.AreEqual(criteria, result.IntCriteria);
        }

        [TestMethod]
        public void BaseFactoryTests_BaseObjectCreateDependency()
        {
            var factory = clientScope.GetRequiredService<IBaseObjectFactory>();

            var result = factory.Create(2, 10d);

            Assert.IsNotNull(result.MultipleCriteria);
        }

        [TestMethod]
        public void BaseFactoryTests_IBaseObjectFetchBaseObject()
        {
            var factory = clientScope.GetRequiredService<IBaseObjectFactory>();

            var result = factory.Fetch();

            Assert.IsNotNull(result.FetchCalled);
        }

        [TestMethod]
        public async Task BaseFactoryTests_IBaseObjectFetchBaseObjectGuid()
        {
            var factory = clientScope.GetRequiredService<IBaseObjectFactory>();

            var guidCriteria = Guid.NewGuid();

            var result = await factory.Fetch(guidCriteria);

            Assert.AreEqual(guidCriteria, result.GuidCriteria);
        }
    }
}
