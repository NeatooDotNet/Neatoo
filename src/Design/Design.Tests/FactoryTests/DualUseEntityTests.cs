// -----------------------------------------------------------------------------
// Design.Tests - One Entity Class in Both Roles
// -----------------------------------------------------------------------------
// Pins entity duality: the same class is fetched and saved as a root through its
// [Remote] service-taking operations, and fetched and saved as a child through
// its internal row-taking operations. The factory method signature selects the
// role; neither path marks the other's state wrong.
// -----------------------------------------------------------------------------

using Design.Domain.FactoryOperations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.FactoryTests;

[TestClass]
public class DualUseEntityTests
{
    private IServiceScope _scope = null!;
    private IDualUseEntityFactory _factory = null!;
    private MockDualUseRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _factory = _scope.GetRequiredService<IDualUseEntityFactory>();
        _repository = (MockDualUseRepository)_scope.GetRequiredService<IDualUseRepository>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    #region skill-entity-both-roles-test
    [TestMethod]
    public async Task RootRole_FetchByIdAndSave_WritesThroughTheRepository()
    {
        var id = _repository.Seed("1 Main St", "Springfield");

        var entity = await _factory.Fetch(id);

        Assert.IsNotNull(entity);
        Assert.IsFalse(entity.IsNew);
        entity.City = "Shelbyville";

        entity = (IDualUseEntity)await entity.Save();

        Assert.AreEqual("Shelbyville", _repository.Store[id].City);
        Assert.IsFalse(entity.IsModified);
    }

    [TestMethod]
    public async Task ChildRole_FetchFromRowAndSave_WritesIntoTheRow()
    {
        // The parent's row holds this row; the list's [Update] would hand it over.
        var row = new DualUseRow { Id = 7, Street = "2 Elm St", City = "Springfield" };

        var entity = _factory.Fetch(row);

        Assert.IsFalse(entity.IsNew);
        Assert.AreEqual("2 Elm St", entity.Street);
        entity.City = "Shelbyville";

        // Local operation: the child role runs on the server beside its list, so it is synchronous.
        entity = _factory.Save(entity, row);

        Assert.AreEqual("Shelbyville", row.City);
        Assert.IsFalse(entity.IsModified);
        Assert.AreEqual(0, _repository.Store.Count, "the child role never touches the repository");
    }
    #endregion
}
