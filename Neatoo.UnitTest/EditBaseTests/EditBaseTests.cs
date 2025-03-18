using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.PersonObjects;
using System.ComponentModel;

namespace Neatoo.UnitTest.EditBaseTests;


[TestClass]
public class EditBaseTests
{

    private IServiceScope scope;
    private IEditPerson editPerson;

    [TestInitialize]
    public void TestInitialize()
    {
        scope = UnitTestServices.GetLifetimeScope();

        editPerson = scope.GetRequiredService<IEditPerson>();
        var parentDto = scope.GetRequiredService<IReadOnlyList<PersonDto>>().Where(p => !p.FatherId.HasValue && !p.MotherId.HasValue).First();

        editPerson.FillFromDto(parentDto);
        editPerson.MarkOld();
        editPerson.MarkUnmodified();
        Assert.IsFalse(editPerson.IsModified);
        Assert.IsFalse(editPerson.IsNew);
        Assert.IsFalse(editPerson.IsSavable);
        Assert.IsFalse(editPerson.IsBusy);

        editPerson.PropertyChanged += EditPersonPropertyChanged;
    }

    private List<string> editPersonPropertyChanged = new List<string>();
    private void EditPersonPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        editPersonPropertyChanged.Add(e.PropertyName);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Assert.IsFalse(editPerson.IsBusy);
    }

    [TestMethod]
    public void EditBaseTest()
    {
        Assert.IsFalse(editPerson.IsModified);
        Assert.IsFalse(editPerson.IsSelfModified);
        Assert.IsFalse(editPerson.IsSavable);
    }

    [TestMethod]
    public void EditBaseTest_SetString_IsModified()
    {
        Assert.IsFalse(editPerson.IsModified);
        Assert.IsFalse(editPerson.IsSelfModified);
        Assert.IsFalse(editPerson.IsBusy);

        editPerson.FullName = Guid.NewGuid().ToString();
        Assert.IsFalse(editPerson.IsBusy);
        Assert.IsTrue(editPerson.IsModified);
        Assert.IsTrue(editPerson.IsSelfModified);
        CollectionAssert.AreEquivalent(new List<string>() { nameof(IEditPerson.FullName), }, editPerson.ModifiedProperties.ToList());
        CollectionAssert.Contains(editPersonPropertyChanged, nameof(IEditPerson.FullName));
        CollectionAssert.Contains(editPersonPropertyChanged, nameof(IEditPerson.IsModified));
        CollectionAssert.Contains(editPersonPropertyChanged, nameof(IEditPerson.IsSelfModified));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEditPerson.IsBusy));
        CollectionAssert.Contains(editPersonPropertyChanged, nameof(IEditPerson.IsSavable));
    }

    [TestMethod]
    public void EditBaseTest_SetSameString_IsModified_False()
    {
        var firstName = editPerson.FirstName;
        editPerson.FirstName = firstName;
        Assert.IsFalse(editPerson.IsModified);
        Assert.IsFalse(editPerson.IsSelfModified);

        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEditPerson.FullName));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEditPerson.IsModified));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEditPerson.IsSelfModified));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEditPerson.IsBusy));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEditPerson.IsSavable));
    }

    [TestMethod]
    public void EditBaseTest_SetNonLoadedProperty_IsModified()
    {
        // Set a property that isn't loaded during the Fetch/Create
        editPerson.Age = 10;
        Assert.IsTrue(editPerson.IsModified);
        Assert.IsTrue(editPerson.IsSelfModified);
        CollectionAssert.AreEquivalent(new List<string>() { nameof(IEditPerson.Age), }, editPerson.ModifiedProperties.ToList());
    }


    [TestMethod]
    public void EditBaseTest_InitiallyDefined_SameInstance_IsModified_False()
    {
        var list = editPerson.InitiallyDefined;
        Assert.IsNotNull(list);
        editPerson.InitiallyDefined = list;
        Assert.IsFalse(editPerson.IsModified);
        Assert.IsFalse(editPerson.IsSelfModified);
        Assert.AreEqual(0, editPerson.ModifiedProperties.Count());

        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEditPerson.FullName));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEditPerson.IsModified));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEditPerson.IsSelfModified));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEditPerson.IsBusy));
        CollectionAssert.DoesNotContain(editPersonPropertyChanged, nameof(IEditPerson.IsSavable));
    }

    [TestMethod]
    public void EditBaseTest_InitiallyDefined_NewInstance_IsModified_True()
    {
        editPerson.InitiallyDefined = editPerson.InitiallyDefined.ToList();
        Assert.IsTrue(editPerson.IsModified);
        Assert.IsTrue(editPerson.IsSelfModified);
        CollectionAssert.AreEquivalent(new List<string>() { nameof(IEditPerson.InitiallyDefined), }, editPerson.ModifiedProperties.ToList());
    }

    [TestMethod]
    public void EditBaseTest_InitiallyNull_IsModified()
    {
        editPerson.InitiallyNull = new List<int>() { 3, 4, 5 };
        Assert.IsTrue(editPerson.IsModified);
        Assert.IsTrue(editPerson.IsSelfModified);

    }

    [TestMethod]
    public void EditBaseTest_IsDeleted()
    {
        editPerson.Delete();
        Assert.IsTrue(editPerson.IsDeleted);
        Assert.IsTrue(editPerson.IsModified);
        Assert.IsTrue(editPerson.IsSelfModified);
    }

    [TestMethod]
    public void EditBaseTest_IsSavable()
    {
        editPerson.FirstName = Guid.NewGuid().ToString();
        Assert.IsTrue(editPerson.IsSavable);
    }

    //[TestMethod]
    //public async Task EditBaseTest_Save()
    //{
    //    var mock = (MockDataMapper<EditPerson>)  scope.GetRequiredService<INeatooPortal<EditPerson>>();

    //    mock.MockPortal.Setup(x => x.Update((EditPerson) editPerson)).Returns(Task.FromResult((EditPerson) editPerson));

    //    editPerson.FirstName = Guid.NewGuid().ToString();
    //    await editPerson.Save();

    //    mock.MockPortal.Verify(x => x.Update((EditPerson)editPerson), Times.Once);
    //}
}

