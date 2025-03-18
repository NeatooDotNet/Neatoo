using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.PersonObjects;

namespace Neatoo.UnitTest.EditBaseTests;


[TestClass]
public class EditListBaseTests
{

    private IServiceScope scope;
    private IEditPersonList list;
    private IEditPerson child;

    [TestInitialize]
    public void TestInitialize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        var parentDto = scope.GetRequiredService<IReadOnlyList<PersonDto>>().Where(p => !p.FatherId.HasValue && !p.MotherId.HasValue).First();

        list = scope.GetRequiredService<IEditPersonList>();

        child = scope.GetRequiredService<IEditPerson>();

        child.MarkUnmodified();
        child.MarkOld();
        child.MarkAsChild();

        //using (((IDataMapperTarget)list).PauseAllActions())
        {
            list.Add(child);
        }

        Assert.IsFalse(list.IsBusy);

    }

    [TestCleanup]
    public void TestCleanup()
    {
        Assert.IsFalse(list.IsBusy);
    }

    [TestMethod]
    public void EditListBaseTest()
    {
        Assert.IsFalse(list.IsModified);
        Assert.IsFalse(list.IsSelfModified);
    }

    [TestMethod]
    public void EditListBaseTest_ModifyChild_IsModified()
    {

        child.FirstName = Guid.NewGuid().ToString();
        Assert.IsTrue(list.IsModified);
        Assert.IsTrue(child.IsModified);

    }

    [TestMethod]
    public void EditListBaseTest_ModifyChild_IsSelfModified()
    {

        child.FirstName = Guid.NewGuid().ToString();

        Assert.IsFalse(list.IsSelfModified);
        Assert.IsTrue(child.IsSelfModified);

    }

    [TestMethod]
    public void EditListBaseTest_ModifyChild_IsSavable()
    {

        child.FirstName = Guid.NewGuid().ToString();

        Assert.IsFalse(list.IsSavable);
        Assert.IsFalse(child.IsSavable);
    }

    [TestMethod]
    public void EditListBaseTest_Remove()
    {
        list.Remove(list.First());
        Assert.AreEqual(0, list.Count);
        Assert.AreEqual(1, list.DeletedCount);
    }
    [TestMethod]
    public void EditListBaseTest_Remove_IsModified()
    {
        Assert.IsTrue(list.Remove(list.First()));
        Assert.IsTrue(list.IsModified);
        // Self modified means it's own properties
        // List items are considered children
        Assert.IsFalse(list.IsSelfModified);
    }

    [TestMethod]
    public void EditListBaseTest_RemoveAt()
    {
        list.RemoveAt(0);
        Assert.AreEqual(0, list.Count);
        Assert.AreEqual(1, list.DeletedCount);
    }

    [TestMethod]
    public void EditListBaseTest_RemoveAt_IsModified()
    {
        list.Remove(list.First());
        Assert.IsTrue(list.IsModified);
        // Self modified means it's own properties
        // List items are considered children
        Assert.IsFalse(list.IsSelfModified);
    }


}

