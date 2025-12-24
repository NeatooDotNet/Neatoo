using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Aggregates.Person;
using System.ComponentModel;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;


[TestClass]
public class EntityBaseTests
{

    private IEntityPerson editPerson;
    private List<(bool isSavable, string propertyName)> neatooPropertyChanged = new List<(bool, string)>();
    private List<(bool isSavable, string propertyName)> propertyChanged = new List<(bool, string)>();

    [TestInitialize]
    public void TestInitialize()
    {
        var parentDto = PersonDto.Data().Where(p => !p.FatherId.HasValue && !p.MotherId.HasValue).First();

        editPerson = new EntityPerson();
        editPerson.FromDto(parentDto);

        editPerson.PropertyChanged += EntityPersonPropertyChanged;
        editPerson.NeatooPropertyChanged += NeatooPropertyChanged;
    }

    private Task NeatooPropertyChanged(NeatooPropertyChangedEventArgs propertyNameBreadCrumbs)
    {
        neatooPropertyChanged.Add((editPerson.IsSavable, propertyNameBreadCrumbs.FullPropertyName));
        return Task.CompletedTask;
    }

    private void EntityPersonPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        propertyChanged.Add((editPerson.IsSavable, e.PropertyName!));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Assert.IsFalse(editPerson.IsBusy);
        editPerson.PropertyChanged -= EntityPersonPropertyChanged;
        editPerson.NeatooPropertyChanged -= NeatooPropertyChanged;
    }

    [TestMethod]
    public void EntityBaseTest()
    {
        Assert.IsFalse(editPerson.IsModified);
        Assert.IsFalse(editPerson.IsSelfModified);
        Assert.IsFalse(editPerson.IsNew);
        Assert.IsFalse(editPerson.IsSavable);
        Assert.IsFalse(editPerson.IsBusy);
        Assert.IsFalse(editPerson.IsSavable);
    }

    [TestMethod]
    public void EntityBaseTest_SetString_IsModified()
    {
        Assert.IsFalse(editPerson.IsModified);
        Assert.IsFalse(editPerson.IsSelfModified);
        Assert.IsFalse(editPerson.IsBusy);

        editPerson.FullName = Guid.NewGuid().ToString();
        Assert.IsFalse(editPerson.IsBusy);
        Assert.IsTrue(editPerson.IsModified);
        Assert.IsTrue(editPerson.IsSelfModified);
        CollectionAssert.AreEquivalent(new List<string>() { nameof(IEntityPerson.FullName), }, editPerson.ModifiedProperties.ToList());

        var editPersonPropertyChanged = propertyChanged.Select(p => p.propertyName).ToList();
        CollectionAssert.Contains(editPersonPropertyChanged, nameof(IEntityPerson.FullName));
        CollectionAssert.Contains(editPersonPropertyChanged, nameof(IEntityPerson.IsModified));
        CollectionAssert.Contains(editPersonPropertyChanged, nameof(IEntityPerson.IsSelfModified));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEntityPerson.IsBusy));
        CollectionAssert.Contains(editPersonPropertyChanged, nameof(IEntityPerson.IsSavable));
    }

    [TestMethod]
    public void EntityBaseTest_SetSameString_IsModified_False()
    {
        var firstName = editPerson.FirstName;
        editPerson.FirstName = firstName;
        Assert.IsFalse(editPerson.IsModified);
        Assert.IsFalse(editPerson.IsSelfModified);


        var editPersonPropertyChanged = propertyChanged.Select(p => p.propertyName).ToList();
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEntityPerson.FullName));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEntityPerson.IsModified));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEntityPerson.IsSelfModified));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEntityPerson.IsBusy));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEntityPerson.IsSavable));
    }

    [TestMethod]
    public void EntityBaseTest_SetNonLoadedProperty_IsModified()
    {
        // Set a property that isn't loaded during the Fetch/Create
        editPerson.Age = 10;
        Assert.IsTrue(editPerson.IsModified);
        Assert.IsTrue(editPerson.IsSelfModified);
        CollectionAssert.AreEquivalent(new List<string>() { nameof(IEntityPerson.Age), }, editPerson.ModifiedProperties.ToList());
    }


    [TestMethod]
    public void EntityBaseTest_InitiallyDefined_SameInstance_IsModified_False()
    {
        var list = editPerson.InitiallyDefined;
        Assert.IsNotNull(list);
        editPerson.InitiallyDefined = list;
        Assert.IsFalse(editPerson.IsModified);
        Assert.IsFalse(editPerson.IsSelfModified);
        Assert.AreEqual(0, editPerson.ModifiedProperties.Count());

        var editPersonPropertyChanged = propertyChanged.Select(p => p.propertyName).ToList();
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEntityPerson.FullName));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEntityPerson.IsModified));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEntityPerson.IsSelfModified));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEntityPerson.IsBusy));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEntityPerson.IsSavable));
    }

    [TestMethod]
    public void EntityBaseTest_InitiallyDefined_NewInstance_IsModified_True()
    {
        editPerson.InitiallyDefined = editPerson.InitiallyDefined.ToList();
        Assert.IsTrue(editPerson.IsModified);
        Assert.IsTrue(editPerson.IsSelfModified);
        CollectionAssert.AreEquivalent(new List<string>() { nameof(IEntityPerson.InitiallyDefined), }, editPerson.ModifiedProperties.ToList());
    }

    [TestMethod]
    public void EntityBaseTest_InitiallyNull_IsModified()
    {
        editPerson.InitiallyNull = new List<int>() { 3, 4, 5 };
        Assert.IsTrue(editPerson.IsModified);
        Assert.IsTrue(editPerson.IsSelfModified);

    }

    [TestMethod]
    public void EntityBaseTest_IsDeleted()
    {
        editPerson.Delete();
        Assert.IsTrue(editPerson.IsDeleted);
        Assert.IsTrue(editPerson.IsModified);
        Assert.IsTrue(editPerson.IsSelfModified);
    }

    [TestMethod]
    public void EntityBaseTest_IsSavable()
    {
        editPerson.FirstName = Guid.NewGuid().ToString();
        Assert.IsTrue(editPerson.IsSavable);
    }

    [TestMethod]
    public void EntityBaseTest_AddInvalidChild_MakeValid_PropertyChanged()
    {
        editPerson.FirstName = Guid.NewGuid().ToString();
        Assert.IsTrue(editPerson.IsSavable);

        var child = new EntityPerson();
        child.FirstName = "Error";
        editPerson.Child = child;
        Assert.IsFalse(editPerson.IsSavable);

        propertyChanged.Clear();
        neatooPropertyChanged.Clear();

        child.FirstName = Guid.NewGuid().ToString();

        Assert.IsTrue(editPerson.IsSavable);
        var propertyChangedNames = propertyChanged.Select(p => p.propertyName).ToList();
        CollectionAssert.Contains(propertyChangedNames, nameof(IEntityBase.IsSavable));
    }
}
