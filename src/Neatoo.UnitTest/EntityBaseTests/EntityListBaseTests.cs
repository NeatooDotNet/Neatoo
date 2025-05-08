using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.PersonObjects;
using System.ComponentModel;

namespace Neatoo.UnitTest.EntityBaseTests;


[TestClass]
public class EntityListBaseTests
{
    private IEntityPerson parent;
    private IEntityPersonList list;
    private IEntityPerson child;
    private List<(bool isSavable, string propertyName)> propertyChangedBreadCrumbs = new List<(bool, string)>();
    private List<(bool isSavable, string propertyName)> propertyChanged = new List<(bool, string)>();

    [TestInitialize]
    public void TestInitialize()
    {
        var parentDto = PersonDto.Data().Where(p => !p.FatherId.HasValue && !p.MotherId.HasValue).First();

        list = new EntityPersonList();
        child = new EntityPerson();
        child.FromDto(parentDto);
        //using (((IDataMapperTarget)list).PauseAllActions())
        list.Add(child);
        child.MarkUnmodified();

        parent = new EntityPerson();
        parent.ChildList = list;
        parent.MarkUnmodified();

        Assert.IsFalse(list.IsBusy);

        parent.PropertyChanged += Parent_PropertyChanged;
        parent.NeatooPropertyChanged += Parent_NeatooPropertyChanged;
    }

    private Task Parent_NeatooPropertyChanged(NeatooPropertyChangedEventArgs propertyNameBreadCrumbs)
    {
        propertyChangedBreadCrumbs.Add((parent.IsSavable, propertyNameBreadCrumbs.FullPropertyName));
        return Task.CompletedTask;
    }

    private void Parent_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        propertyChanged.Add((parent.IsSavable, e.PropertyName!));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Assert.IsFalse(list.IsBusy);
        parent.PropertyChanged -= Parent_PropertyChanged;
        parent.NeatooPropertyChanged -= Parent_NeatooPropertyChanged;
    }

    [TestMethod]
    public void EntityListBaseTest()
    {
        Assert.IsFalse(list.IsModified);
        Assert.IsFalse(list.IsSelfModified);
        Assert.IsFalse(parent.IsModified);
        Assert.IsFalse(parent.IsSavable);
    }

    [TestMethod]
    public void EntityListBaseTest_ModifyChild_IsModified()
    {

        child.FirstName = Guid.NewGuid().ToString();
        Assert.IsTrue(list.IsModified);
        Assert.IsTrue(child.IsModified);

    }

    [TestMethod]
    public void EntityListBaseTest_ModifyChild_IsSelfModified()
    {

        child.FirstName = Guid.NewGuid().ToString();

        Assert.IsFalse(list.IsSelfModified);
        Assert.IsTrue(child.IsSelfModified);

    }

    [TestMethod]
    public void EntityListBaseTest_ModifyChild_IsSavable()
    {

        child.FirstName = Guid.NewGuid().ToString();

        Assert.IsFalse(list.IsSavable);
        Assert.IsFalse(child.IsSavable);
    }

    [TestMethod]
    public void EntityListBaseTest_Remove()
    {
        list.Remove(list.First());
        Assert.AreEqual(0, list.Count);
        Assert.AreEqual(1, list.DeletedCount);
    }
    [TestMethod]
    public void EntityListBaseTest_Remove_IsModified()
    {
        Assert.IsTrue(list.Remove(list.First()));
        Assert.IsTrue(list.IsModified);
        // Self modified means it's own properties
        // List items are considered children
        Assert.IsFalse(list.IsSelfModified);
    }

    [TestMethod]
    public void EntityListBaseTest_RemoveAt()
    {
        list.RemoveAt(0);
        Assert.AreEqual(0, list.Count);
        Assert.AreEqual(1, list.DeletedCount);
    }

    [TestMethod]
    public void EntityListBaseTest_RemoveAt_IsModified()
    {
        list.Remove(list.First());
        Assert.IsTrue(list.IsModified);
        // Self modified means it's own properties
        // List items are considered children
        Assert.IsFalse(list.IsSelfModified);
    }

    [TestMethod]
    public void EntityListBaseTest_AddInvalidChild_MakeValid_PropertyChanged()
    {
        // Adding an invalid child, then fixing the child, not getting the property changed event

        var newChild = new EntityPerson();
        newChild.FirstName = "Error";
        list.Add(newChild);

        Assert.IsFalse(parent.IsValid);
        propertyChangedBreadCrumbs.Clear();
        propertyChanged.Clear();

        newChild.FirstName = Guid.NewGuid().ToString();
        Assert.IsTrue(parent.IsValid);
        Assert.IsTrue(parent.IsSavable);
    }
}

