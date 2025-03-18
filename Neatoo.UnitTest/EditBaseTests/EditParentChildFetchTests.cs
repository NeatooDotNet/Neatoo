using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.PersonObjects;

namespace Neatoo.UnitTest.EditBaseTests;


[TestClass]
public class EditParentChildFetchTests
{

    private IServiceScope scope;
    private IEditPerson parent;
    private IEditPerson child;

    [TestInitialize]
    public void TestInitialize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        var persons = scope.GetRequiredService<IReadOnlyList<PersonDto>>();
        


        parent = scope.GetRequiredService<IEditPerson>();
        parent.FillFromDto(persons.Where(p => !p.FatherId.HasValue && !p.MotherId.HasValue).First());

        child = scope.GetRequiredService<IEditPerson>();
        child.FillFromDto(persons.Where(p => p.FatherId == parent.Id).First());
        parent.Child = child;

        child.MarkOld();
        child.MarkUnmodified();
        child.MarkAsChild();
        parent.MarkOld();
        parent.MarkUnmodified();


    }

    [TestMethod]
    public void EditParentChildFetchTest_Fetch_InitialMeta()
    {
        void AssertMeta(IEditPerson t)
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
    public async Task EditParentChildFetchTest_ModifyChild_IsModified()
    {

        child.FirstName = Guid.NewGuid().ToString();
        await parent.WaitForTasks();
        Assert.IsTrue(parent.IsModified);
        Assert.IsTrue(child.IsModified);

    }

    [TestMethod]
    public async Task EditParentChildFetchTest_ModifyChild_IsSelfModified()
    {

        child.FirstName = Guid.NewGuid().ToString();
        await parent.WaitForTasks();

        Assert.IsFalse(parent.IsSelfModified);
        Assert.IsTrue(child.IsSelfModified);

    }

    [TestMethod]
    public async Task EditParentChildFetchTest_ModifyChild_IsSavable()
    {

        child.FirstName = Guid.NewGuid().ToString();
        await parent.WaitForTasks();

        Assert.IsTrue(parent.IsSavable);
        Assert.IsFalse(child.IsSavable);

    }


    [TestMethod]
    public void EditParentChildFetchTest_Parent()
    {
        Assert.AreSame(parent, child.Parent);
    }
}

