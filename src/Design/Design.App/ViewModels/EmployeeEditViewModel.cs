// -----------------------------------------------------------------------------
// Design.App - Employee Edit ViewModel
// -----------------------------------------------------------------------------
// The ViewModel behind the Employee page, the page that shows each MudNeatoo
// component binding an entity property. Same shape as OrderEditViewModel: it
// holds the live IEmployee, the page binds the entity's property wrappers,
// and the save handler waits, asks IsSavable, saves and reassigns.
//
// View/edit mode is the screen's own state: which components the page shows.
// It is not read-only state - that belongs to the entity (a private setter,
// or MarkReadOnly in [Fetch]) and the components bind it themselves.
//
// DESIGN DECISION: CancelEdit fetches the saved employee again. The entity's
// tracked state is the edit; discarding the edit means going back to the
// saved one, which only the server has. A new employee has none, so
// discarding its edit is a fresh form.
// -----------------------------------------------------------------------------

using Design.Domain.Entities;

namespace Design.App.ViewModels;

#region skill-mud-viewmodel
/// <summary>
/// ViewModel for the Employee screen: holds the aggregate, toggles the screen
/// between viewing and editing, saves.
/// </summary>
internal sealed class EmployeeEditViewModel
{
    private readonly IEmployeeFactory _employeeFactory;

    public EmployeeEditViewModel(IEmployeeFactory employeeFactory)
    {
        _employeeFactory = employeeFactory;
    }

    /// <summary>The aggregate the page binds to. Null until loaded or created.</summary>
    public IEmployee? Employee { get; private set; }

    /// <summary>What the grade select offers.</summary>
    public IReadOnlyList<PayGrade> Grades => PayGrade.All;

    /// <summary>The screen shows the edit components; a new employee starts there.</summary>
    public bool IsEditing { get; private set; }

    public void New()
    {
        Employee = _employeeFactory.Create();
        IsEditing = true;
    }

    public async Task Load(Guid id)
    {
        Employee = await _employeeFactory.Fetch(id);
        IsEditing = false;
    }

    public void StartEdit()
    {
        IsEditing = true;
    }

    public async Task CancelEdit()
    {
        if (Employee is null)
        {
            return;
        }

        if (Employee.IsNew)
        {
            New();  // nothing saved to go back to: discarding the edit is a fresh form
            return;
        }

        await Load(Employee.Id);
    }

    public bool CanSave => Employee?.IsSavable ?? false;

    public async Task Save()
    {
        if (Employee is null)
        {
            return;
        }

        await Employee.WaitForTasks();

        if (!Employee.IsSavable)
        {
            return;
        }

        Employee = (IEmployee)await Employee.Save();
        IsEditing = false;
    }
}
#endregion
