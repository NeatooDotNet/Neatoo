using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.UnitTest.RemoteFactory
{

[TestClass]
    public class EntityBaseFactoryTests
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
        public void EntityBaseFactoryTests_IEntityObjectCreateEntityBaseObject()
        {
            var factory = clientScope.GetRequiredService<IEntityObjectFactory>();

            var result = factory.Create();

            Assert.IsTrue(result.CreateCalled);
            Assert.IsTrue(result.IsNew);
            Assert.IsTrue(result.IsModified);
        }

        [TestMethod]
        public async Task EntityBaseFactoryTests_IEntityObjectCreateEntityBaseObjectInt()
        {
            var factory = clientScope.GetRequiredService<IEntityObjectFactory>();

            var criteria = 10;

            var result = await factory.CreateAsync(criteria);

            Assert.AreEqual(criteria, result.IntCriteria);
            Assert.IsTrue(result.IsNew);
            Assert.IsTrue(result.IsModified);
        }

        [TestMethod]
        public void EntityBaseFactoryTests_EntityBaseObjectCreateDependency()
        {
            var factory = clientScope.GetRequiredService<IEntityObjectFactory>();
            var guidCriteria = Guid.NewGuid();
            var result = factory.Create(guidCriteria);

            Assert.AreEqual(guidCriteria, result.GuidCriteria);
            Assert.IsTrue(result.IsNew);
            Assert.IsTrue(result.IsModified);
        }

        [TestMethod]
        public async Task EntityBaseFactoryTests_EntityBaseObjectCreateRemote()
        {
            var factory = clientScope.GetRequiredService<IEntityObjectFactory>();
            var guidCriteria = Guid.NewGuid();
            var result = await factory.CreateRemote(guidCriteria);

            Assert.AreEqual(guidCriteria, result.GuidCriteria);
            Assert.IsTrue(result.IsNew);
            Assert.IsTrue(result.IsModified);
        }

        [TestMethod]
        public void EntityBaseFactoryTests_IEntityObjectFetchEntityBaseObject()
        {
            var factory = clientScope.GetRequiredService<IEntityObjectFactory>();

            var result = factory.Fetch();

            Assert.IsNotNull(result.FetchCalled);
            Assert.IsFalse(result.IsNew);
            Assert.IsFalse(result.IsModified);
            Assert.IsFalse(result.IsSavable);
        }

        [TestMethod]
        public void EntityBaseFactoryTests_IEntityObjectFetchEntityBaseObjectGuid()
        {
            var factory = clientScope.GetRequiredService<IEntityObjectFactory>();

            var guidCriteria = Guid.NewGuid();

            var result = factory.Fetch(guidCriteria);

            Assert.AreEqual(guidCriteria, result.GuidCriteria);
            Assert.IsFalse(result.IsNew);
            Assert.IsFalse(result.IsModified);
            Assert.IsFalse(result.IsSavable);
        }

        [TestMethod]
        public async Task EntityBaseFactoryTests_FetchRemote()
        {
            var factory = clientScope.GetRequiredService<IEntityObjectFactory>();

            var guidCriteria = Guid.NewGuid();

            var result = await factory.FetchRemote(guidCriteria);

            Assert.AreEqual(guidCriteria, result.GuidCriteria);
            Assert.IsFalse(result.IsNew);
            Assert.IsFalse(result.IsModified);
            Assert.IsFalse(result.IsSavable);
        }

        [TestMethod]
        public async Task EntityBaseFactoryTests_Save()
        {
            var factory = clientScope.GetRequiredService<IEntityObjectFactory>();

            var result = factory.Create();

            result = await factory.Save(result);

            Assert.IsTrue(result.InsertCalled);
            Assert.IsFalse(result.IsNew);
            Assert.IsFalse(result.IsModified);
            Assert.IsFalse(result.IsSavable);
        }

        [TestMethod]
        public void EntityBaseFactoryTests_FetchFail()
        {
            var factory = clientScope.GetRequiredService<IEntityObjectFactory>();

            var result = factory.FetchFail();

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task EntityBaseFactoryTests_FetchFailAsync()
        {
            var factory = clientScope.GetRequiredService<IEntityObjectFactory>();

            var result = await factory.FetchFailAsync();

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task EntityBaseFactoryTests_FetchFailDependency()
        {
            var factory = clientScope.GetRequiredService<IEntityObjectFactory>();

            var result = await factory.FetchFailDependency();

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task EntityBaseFactoryTests_FetchFailAsyncDependency()
        {
            var factory = clientScope.GetRequiredService<IEntityObjectFactory>();

            var result = await factory.FetchFailAsyncDependency();

            Assert.IsNull(result);
        }
    }
}
