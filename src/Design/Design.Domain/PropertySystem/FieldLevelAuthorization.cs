// -----------------------------------------------------------------------------
// Design.Domain - Field-Level Authorization via MarkReadOnly
// -----------------------------------------------------------------------------
// Demonstrates runtime field-level authorization using MarkReadOnly() on
// properties during [Fetch]. When the current user lacks permission to edit
// a specific field, call this["FieldName"].MarkReadOnly() to lock it down.
//
// DESIGN DECISION: MarkReadOnly() is one-and-done — once a property is
// marked read-only, it cannot be reverted. This prevents accidental
// re-enabling and matches the authorization model: the server decides
// permissions during Fetch, and the client respects them.
//
// GENERATOR BEHAVIOR: No generator changes needed. MarkReadOnly() operates
// on the IValidateProperty returned by the indexer (this["PropertyName"]).
//
// Downstream behavior (all pre-existing):
// - SetValue() throws PropertyReadOnlyException when IsReadOnly is true
// - SetPrivateValue() bypasses IsReadOnly (rules still work)
// - LoadValue() ignores IsReadOnly (persistence loading still works)
// - IsReadOnly is serialized via JSON (client receives the flag)
// - MudNeatoo components bind ReadOnly="@EntityProperty.IsReadOnly"
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.PropertySystem;

/// <summary>
/// Demonstrates field-level authorization via MarkReadOnly() during Fetch.
/// </summary>
[Factory]
internal partial class FieldLevelAuthDemo : EntityBase<FieldLevelAuthDemo>, IFieldLevelAuthDemo
{
    public partial string? Name { get; set; }
    public partial decimal Salary { get; set; }
    public partial string? Department { get; set; }

    public FieldLevelAuthDemo(IEntityBaseServices<FieldLevelAuthDemo> services) : base(services) { }

    [Create]
    public void Create() { }

    [Remote]
    [Fetch]
    internal void Fetch(int id, bool canEditSalary, [Service] IFieldLevelAuthRepository repository)
    {
        var data = repository.GetById(id);
        this["Name"].LoadValue(data.Name);
        this["Salary"].LoadValue(data.Salary);
        this["Department"].LoadValue(data.Department);

        // Field-level authorization: lock down Salary if user lacks permission
        if (!canEditSalary)
        {
            this["Salary"].MarkReadOnly();
        }
    }

    [Remote]
    [Insert]
    internal void Insert([Service] IFieldLevelAuthRepository repository) { }

    [Remote]
    [Update]
    internal void Update([Service] IFieldLevelAuthRepository repository) { }

    [Remote]
    [Delete]
    internal void Delete([Service] IFieldLevelAuthRepository repository) { }
}

public interface IFieldLevelAuthDemo : IEntityRoot
{
    string? Name { get; set; }
    decimal Salary { get; set; }
    string? Department { get; set; }
}

public interface IFieldLevelAuthRepository
{
    (string Name, decimal Salary, string Department) GetById(int id);
}
