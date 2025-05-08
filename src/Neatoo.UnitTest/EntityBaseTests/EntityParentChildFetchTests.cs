using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.PersonObjects;

namespace Neatoo.UnitTest.EntityBaseTests;


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
        


        parent = scope.GetRequiredService<IEntityPerson>();
        parent.FromDto(persons.Where(p => !p.FatherId.HasValue && !p.MotherId.HasValue).First());

        child = scope.GetRequiredService<IEntityPerson>();
        child.FromDto(persons.Where(p => p.FatherId == parent.Id).First());
        parent.Child = child;

        child.MarkOld();
        child.MarkUnmodified();
        child.MarkAsChild();
        parent.MarkOld();
        parent.MarkUnmodified();


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

        Assert.IsFalse(parent.IsChild);
        Assert.IsTrue(child.IsChild);

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
        Assert.IsFalse(child.IsSavable);

    }


    [TestMethod]
    public void EntityParentChildFetchTest_Parent()
    {
        Assert.AreSame(parent, child.Parent);
    }
}

