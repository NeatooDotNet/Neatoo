using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Aggregates.Person;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;


[TestClass]
public class EntityParentChildFetchTests
{

    private IServiceScope scope;
    private IEntityPerson parent;
    private IEntityPerson child;

    [TestInitialize]
    public void TestInitialize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        var persons = scope.GetRequiredService<IReadOnlyList<PersonDto>>();
        var factory = scope.GetRequiredService<IEntityPersonFactory>();

        // Built through the real [Fetch] factory rather than by newing up objects and
        // hand-marking them clean.
        //
        // This fixture used to call MarkOld() and MarkUnmodified() on both objects
        // during setup - and then EntityParentChildFetchTest_Fetch_InitialMeta asserted
        // exactly IsNew == false and IsModified == false. Every one of those assertions
        // was guaranteed by the setup, so the test could not distinguish "the framework
        // left a fetched graph clean" from "the test cleaned it". A fetch that dirtied
        // everything it touched would have passed.
        //
        // The marks turn out to have been redundant anyway: IsNew has no initializer and
        // so defaults to false, and FromDto loads through LoadValue inside a pause, so a
        // genuine [Fetch] already lands on this state. Now the assertions are the
        // framework's behavior. (LIST-005)
        parent = factory.FillFromDto(persons.Where(p => !p.FatherId.HasValue && !p.MotherId.HasValue).First());

        child = factory.FillFromDto(persons.Where(p => p.FatherId == parent.Id).First());
        parent.Child = child;

    }

    [TestMethod]
    public void EntityParentChildFetchTest_Fetch_InitialMeta()
    {
        void AssertMeta(IEntityPerson t)
        {
            Assert.IsNotNull(t);
            Assert.IsFalse(t.IsModified);
            Assert.IsFalse(t.IsSelfModified);
            Assert.IsFalse(t.IsNew);
            Assert.IsFalse(t.IsSavable);
        }

        AssertMeta(parent);
        AssertMeta(child);

    }

    [TestMethod]
    public async Task EntityParentChildFetchTest_ModifyChild_IsModified()
    {

        child.FirstName = Guid.NewGuid().ToString();
        await parent.WaitForTasks();
        Assert.IsTrue(parent.IsModified);
        Assert.IsTrue(child.IsModified);

    }

    [TestMethod]
    public async Task EntityParentChildFetchTest_ModifyChild_IsSelfModified()
    {

        child.FirstName = Guid.NewGuid().ToString();
        await parent.WaitForTasks();

        Assert.IsFalse(parent.IsSelfModified);
        Assert.IsTrue(child.IsSelfModified);

    }

    [TestMethod]
    public async Task EntityParentChildFetchTest_ModifyChild_IsSavable()
    {

        child.FirstName = Guid.NewGuid().ToString();
        await parent.WaitForTasks();

        Assert.IsTrue(parent.IsSavable);

        // The child CONCRETE is savable by the formula now that the IsChild flag
        // is gone. Note this child is held as a direct property, not in a list -
        // the framework never marked it as a child even before the removal; the
        // setup had to call MarkAsChild() by hand. What stops a consumer saving
        // it is the interface split, not a runtime flag.
        Assert.IsTrue(child.IsSavable);

    }


    [TestMethod]
    public void EntityParentChildFetchTest_Parent()
    {
        Assert.AreSame(parent, child.Parent);
    }
}
