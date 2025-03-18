using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.ObjectPortal;

namespace Neatoo.UnitTest.RemoteFactory
{

[TestClass]
    public class EditBaseFactoryTests
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
        public void EditBaseFactoryTests_IEditObjectCreateEditBaseObject()
        {
            var factory = clientScope.GetRequiredService<EditObjectFactory>();

            var result = factory.Create();

            Assert.IsTrue(result.CreateCalled);
            Assert.IsTrue(result.IsNew);
            Assert.IsTrue(result.IsModified);
        }

        [TestMethod]
        public async Task EditBaseFactoryTests_IEditObjectCreateEditBaseObjectInt()
        {
            var factory = clientScope.GetRequiredService<EditObjectFactory>();

            var criteria = 10;

            var result = await factory.CreateAsync(criteria);

            Assert.AreEqual(criteria, result.IntCriteria);
            Assert.IsTrue(result.IsNew);
            Assert.IsTrue(result.IsModified);
        }

        [TestMethod]
        public void EditBaseFactoryTests_EditBaseObjectCreateDependency()
        {
            var factory = clientScope.GetRequiredService<EditObjectFactory>();
            var guidCriteria = Guid.NewGuid();
            var result = factory.Create(guidCriteria);

            Assert.AreEqual(guidCriteria, result.GuidCriteria);
            Assert.IsTrue(result.IsNew);
            Assert.IsTrue(result.IsModified);
        }

        [TestMethod]
        public async Task EditBaseFactoryTests_EditBaseObjectCreateRemote()
        {
            var factory = clientScope.GetRequiredService<EditObjectFactory>();
            var guidCriteria = Guid.NewGuid();
            var result = await factory.CreateRemote(guidCriteria);

            Assert.AreEqual(guidCriteria, result.GuidCriteria);
            Assert.IsTrue(result.IsNew);
            Assert.IsTrue(result.IsModified);
        }

        [TestMethod]
        public void EditBaseFactoryTests_IEditObjectFetchEditBaseObject()
        {
            var factory = clientScope.GetRequiredService<EditObjectFactory>();

            var result = factory.Fetch();

            Assert.IsNotNull(result.FetchCalled);
            Assert.IsFalse(result.IsNew);
            Assert.IsFalse(result.IsModified);
        }

        [TestMethod]
        public void EditBaseFactoryTests_IEditObjectFetchEditBaseObjectGuid()
        {
            var factory = clientScope.GetRequiredService<EditObjectFactory>();

            var guidCriteria = Guid.NewGuid();

            var result = factory.Fetch(guidCriteria);

            Assert.AreEqual(guidCriteria, result.GuidCriteria);
            Assert.IsFalse(result.IsNew);
            Assert.IsFalse(result.IsModified);
        }

        [TestMethod]
        public async Task EditBaseFactoryTests_FetchRemote()
        {
            var factory = clientScope.GetRequiredService<EditObjectFactory>();

            var guidCriteria = Guid.NewGuid();

            var result = await factory.FetchRemote(guidCriteria);

            Assert.AreEqual(guidCriteria, result.GuidCriteria);
            Assert.IsFalse(result.IsNew);
            Assert.IsFalse(result.IsModified);
        }

        [TestMethod]
        public async Task EditBaseFactoryTests_Save()
        {
            var factory = clientScope.GetRequiredService<EditObjectFactory>();

            var result = factory.Create();

            result = await factory.Save(result);

            Assert.IsTrue(result.InsertCalled);
            Assert.IsFalse(result.IsNew);
            Assert.IsFalse(result.IsModified);
        }

        [TestMethod]
        public void EditBaseFactoryTests_FetchFail()
        {
            var factory = clientScope.GetRequiredService<EditObjectFactory>();

            var result = factory.FetchFail();

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task EditBaseFactoryTests_FetchFailAsync()
        {
            var factory = clientScope.GetRequiredService<EditObjectFactory>();

            var result = await factory.FetchFailAsync();

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task EditBaseFactoryTests_FetchFailDependency()
        {
            var factory = clientScope.GetRequiredService<EditObjectFactory>();

            var result = await factory.FetchFailDependency();

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task EditBaseFactoryTests_FetchFailAsyncDependency()
        {
            var factory = clientScope.GetRequiredService<EditObjectFactory>();

            var result = await factory.FetchFailAsyncDependency();

            Assert.IsNull(result);
        }
    }
}
