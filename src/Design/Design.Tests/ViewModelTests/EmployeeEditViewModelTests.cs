// -----------------------------------------------------------------------------
// Design.Tests - The Employee Page's ViewModel
// -----------------------------------------------------------------------------
// Pins the ViewModel behind EmployeeEdit.razor, the page the MudNeatoo skill
// renders its component bindings from. Run through the client/server pair:
// every field the page binds is set on the client, saved, and fetched back,
// so a property the page binds but the entity does not carry across the wire
// fails here. The page's markup is pinned by the build.
// -----------------------------------------------------------------------------

using Design.App.ViewModels;
using Design.Domain.Entities;
using Design.Tests.CompositionTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.ViewModelTests;

[TestClass]
public class EmployeeEditViewModelTests
{
    private IServiceScope _server = null!;
    private IServiceScope _client = null!;
    private ServerLink _link = null!;
    private EmployeeEditViewModel _vm = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        (_server, _client, _link) = DesignClientServer.Scopes();
        _vm = _client.ServiceProvider.GetRequiredService<EmployeeEditViewModel>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _client.Dispose();
        _server.Dispose();
    }

    private async Task<Guid> SaveNewEmployee()
    {
        _vm.New();
        var employee = _vm.Employee!;
        employee.FirstName = "Ada";
        employee.LastName = "Lovelace";
        employee.Email = "ada@example.com";
        employee.HireDate = DateTime.Today.AddYears(-1);
        employee.Salary = 70_000m;
        employee.Grade = new PayGrade("C");
        employee.Notes = "Prefers written specifications.";
        await employee.WaitForTasks();

        Assert.IsTrue(_vm.CanSave);
        await _vm.Save();
        return _vm.Employee!.Id;
    }

    [TestMethod]
    public async Task EditedThroughTheViewModel_SavesAndFetchesBackEveryBoundField()
    {
        CollectionAssert.AreEqual(
            new[] { "A", "B", "C", "D" },
            _vm.Grades.Select(g => g.Code).ToArray(),
            "the select offers every grade, in order");

        var id = await SaveNewEmployee();

        Assert.IsFalse(_vm.IsEditing, "a save returns the screen to view mode");
        Assert.AreEqual(1, _link.Calls);

        await _vm.Load(id);
        var fetched = _vm.Employee!;

        Assert.AreEqual(2, _link.Calls);
        Assert.AreEqual("Ada", fetched.FirstName);
        Assert.AreEqual("Lovelace", fetched.LastName);
        Assert.AreEqual("ada@example.com", fetched.Email);
        Assert.AreEqual(DateTime.Today.AddYears(-1), fetched.HireDate);
        Assert.AreEqual(70_000m, fetched.Salary);
        Assert.IsTrue(fetched.IsActive);
        Assert.AreEqual(new PayGrade("C"), fetched.Grade);
        Assert.AreEqual("Prefers written specifications.", fetched.Notes);
        Assert.IsFalse(fetched.IsModified);
    }

    [TestMethod]
    public async Task CancelEdit_RestoresTheSavedEmployee()
    {
        var id = await SaveNewEmployee();

        _vm.StartEdit();
        _vm.Employee!.FirstName = "Augusta";
        _vm.Employee.Notes = "Changed.";
        Assert.IsTrue(_vm.Employee.IsModified);

        await _vm.CancelEdit();

        Assert.IsFalse(_vm.IsEditing);
        Assert.AreEqual(id, _vm.Employee.Id);
        Assert.AreEqual("Ada", _vm.Employee.FirstName, "the edit is discarded");
        Assert.AreEqual("Prefers written specifications.", _vm.Employee.Notes);
        Assert.IsFalse(_vm.Employee.IsModified);
        Assert.AreEqual(2, _link.Calls, "save, then the fetch that cancel made");
    }

    [TestMethod]
    public async Task CancelEdit_NewEmployee_DiscardsTheEditForAFreshForm()
    {
        _vm.New();
        var discarded = _vm.Employee!;
        discarded.FirstName = "Ada";

        await _vm.CancelEdit();

        Assert.IsFalse(ReferenceEquals(discarded, _vm.Employee), "the edit is discarded");
        Assert.IsNull(_vm.Employee!.FirstName);
        Assert.IsTrue(_vm.Employee.IsNew);
        Assert.IsTrue(_vm.IsEditing, "still the new-employee form");
        Assert.AreEqual(0, _link.Calls, "nothing saved, nothing to fetch");
    }
}
