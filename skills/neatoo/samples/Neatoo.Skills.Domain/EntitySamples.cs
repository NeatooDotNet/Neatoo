using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Skills.Domain;

// =============================================================================
// ENTITY SAMPLES - Demonstrates entity lifecycle and persistence state
// =============================================================================

// -----------------------------------------------------------------------------
// Entity Base Class
// -----------------------------------------------------------------------------

#region entities-base-class
/// <summary>
/// Employee entity demonstrating EntityBase lifecycle.
/// </summary>
[Factory]
public partial class SkillEntityEmployee : EntityBase<SkillEntityEmployee>
{
    public SkillEntityEmployee(IEntityBaseServices<SkillEntityEmployee> services) : base(services)
    {
        RuleManager.AddValidation(
            emp => !string.IsNullOrEmpty(emp.Name) ? "" : "Name is required",
            e => e.Name);
    }

    public partial int Id { get; set; }

    [Required]
    public partial string Name { get; set; }

    public partial string Department { get; set; }

    public partial decimal Salary { get; set; }

    // Expose protected methods for demonstration
    public void DoMarkNew() => MarkNew();
    public void DoMarkOld() => MarkOld();
    public void DoMarkUnmodified() => MarkUnmodified();

    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
        Department = "";
        Salary = 0;
    }

    [Fetch]
    public void Fetch(int id, string name, string department, decimal salary)
    {
        Id = id;
        Name = name;
        Department = department;
        Salary = salary;
    }

    [Insert]
    public Task InsertAsync([Service] ISkillEntityRepository repo) => repo.InsertAsync(this);

    [Update]
    public Task UpdateAsync([Service] ISkillEntityRepository repo) => repo.UpdateAsync(this);

    [Delete]
    public Task DeleteAsync([Service] ISkillEntityRepository repo) => repo.DeleteAsync(this);
}
#endregion

// -----------------------------------------------------------------------------
// Aggregate Root
// -----------------------------------------------------------------------------

/// <summary>
/// Department item for aggregate samples.
/// </summary>
public interface ISkillEntityDepartmentMember : IEntityBase
{
    int Id { get; set; }
    string Name { get; set; }
    string Role { get; set; }
}

[Factory]
public partial class SkillEntityDepartmentMember : EntityBase<SkillEntityDepartmentMember>, ISkillEntityDepartmentMember
{
    public SkillEntityDepartmentMember(IEntityBaseServices<SkillEntityDepartmentMember> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial string Role { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id, string name, string role)
    {
        Id = id;
        Name = name;
        Role = role;
    }
}

public interface ISkillEntityDepartmentMemberList : IEntityListBase<ISkillEntityDepartmentMember>
{
    int DeletedCount { get; }
}

public class SkillEntityDepartmentMemberList : EntityListBase<ISkillEntityDepartmentMember>, ISkillEntityDepartmentMemberList
{
    public int DeletedCount => DeletedList.Count;
}

#region entities-aggregate-root
/// <summary>
/// Department aggregate root containing member entities.
/// </summary>
[Factory]
public partial class SkillEntityDepartment : EntityBase<SkillEntityDepartment>
{
    public SkillEntityDepartment(IEntityBaseServices<SkillEntityDepartment> services) : base(services)
    {
        MembersProperty.LoadValue(new SkillEntityDepartmentMemberList());
    }

    public partial int Id { get; set; }

    public partial string Name { get; set; }

    public partial string Location { get; set; }

    // Child collection - part of the aggregate
    public partial ISkillEntityDepartmentMemberList Members { get; set; }

    [Create]
    public void Create()
    {
        Id = 0;
        Name = "";
        Location = "";
    }

    [Fetch]
    public void Fetch(
        int id, string name, string location,
        [Service] ISkillEntityDepartmentMemberFactory memberFactory)
    {
        Id = id;
        Name = name;
        Location = location;

        // Load child collection
        // Factory.Fetch creates existing (non-new) items
    }
}
// Aggregate root coordinates persistence of all contained entities.
// Save on the root saves/deletes all children as a unit.
#endregion

// -----------------------------------------------------------------------------
// Entity Lifecycle States
// -----------------------------------------------------------------------------

#region entities-is-new
// IsNew indicates whether the entity has been persisted:
//
// After Create():  entity.IsNew = true   → Will call Insert on Save
// After Fetch():   entity.IsNew = false  → Will call Update on Save
// After Insert():  entity.IsNew = false  → Now an existing entity
#endregion

#region entities-lifecycle-new
// New entity lifecycle:
//
// var employee = factory.Create();
// Assert.True(employee.IsNew);      // New entity
// Assert.True(employee.IsModified); // New is considered modified
//
// employee.Name = "Alice";
// employee.Salary = 50000;
//
// await factory.SaveAsync(employee); // Routes to Insert
// Assert.False(employee.IsNew);      // Now existing
#endregion

#region entities-fetch
// Fetched entity lifecycle:
//
// var employee = await factory.Fetch(1);
// Assert.False(employee.IsNew);      // Existing entity
// Assert.False(employee.IsModified); // No changes yet
//
// employee.Salary = 55000;           // Make a change
// Assert.True(employee.IsModified);  // Now modified
//
// await factory.SaveAsync(employee); // Routes to Update
// Assert.False(employee.IsModified); // Clean again
#endregion

#region entities-save
// Save routing based on state:
//
// entity.IsNew && !entity.IsDeleted    → Insert
// !entity.IsNew && !entity.IsDeleted   → Update
// entity.IsDeleted                     → Delete
//
// // New entity → Insert
// var newEmp = factory.Create();
// await factory.SaveAsync(newEmp); // Calls InsertAsync
//
// // Existing entity → Update
// var existing = await factory.Fetch(1);
// existing.Name = "Updated";
// await factory.SaveAsync(existing); // Calls UpdateAsync
//
// // Deleted entity → Delete
// existing.Delete();
// await factory.SaveAsync(existing); // Calls DeleteAsync
#endregion

#region entities-delete
// Delete marks entity for removal:
//
// var employee = await factory.Fetch(1);
// Assert.False(employee.IsDeleted);
//
// employee.Delete();
// Assert.True(employee.IsDeleted);
// Assert.True(employee.IsModified);  // Deletion is a modification
//
// await factory.SaveAsync(employee); // Routes to DeleteAsync
#endregion

#region entities-undelete
// UnDelete reverses the deletion mark:
//
// employee.Delete();
// Assert.True(employee.IsDeleted);
//
// employee.UnDelete();
// Assert.False(employee.IsDeleted);
//
// // Now Save will route to Update instead of Delete
// await factory.SaveAsync(employee);
#endregion

#region entities-modification-state
// Modification tracking properties:
//
// entity.IsModified       - True if entity or children have changes
// entity.IsSelfModified   - True if this entity's properties changed
// entity.IsMarkedModified - True if explicitly marked modified
// entity.ModifiedProperties - List of changed property names
//
// var emp = await factory.Fetch(1);
// Assert.False(emp.IsModified);
//
// emp.Name = "Changed";
// Assert.True(emp.IsModified);
// Assert.Contains("Name", emp.ModifiedProperties);
#endregion

#region entities-mark-modified
// MarkModified forces entity to appear modified:
//
// var emp = await factory.Fetch(1);
// Assert.False(emp.IsModified);
//
// emp.MarkModified();  // Force modified state
// Assert.True(emp.IsMarkedModified);
// Assert.True(emp.IsModified);
//
// // Useful when external changes require a save
#endregion

#region entities-mark-unmodified
// MarkUnmodified resets modification tracking:
//
// var emp = factory.Create();
// emp.Name = "Test";
// Assert.True(emp.IsModified);
//
// emp.MarkUnmodified();  // Reset modification state
// Assert.False(emp.IsModified);
//
// // Typically called after custom persistence logic
#endregion

#region entities-persistence-state
// Full persistence state properties:
//
// IsNew        - Not yet persisted
// IsDeleted    - Marked for deletion
// IsChild      - Part of another aggregate
// IsModified   - Has changes (self or children)
// IsSavable    - Can be saved (valid, modified, not busy, not child)
#endregion

#region entities-savable
// IsSavable determines if Save will succeed:
//
// entity.IsSavable = entity.IsValid
//                 && entity.IsModified
//                 && !entity.IsBusy
//                 && !entity.IsChild;
//
// // Check before save:
// if (employee.IsSavable)
// {
//     await factory.SaveAsync(employee);
// }
// else if (!employee.IsValid)
// {
//     ShowValidationErrors(employee.PropertyMessages);
// }
// else if (employee.IsBusy)
// {
//     ShowBusyIndicator();
// }
#endregion

#region entities-child-state
// Child entities are part of their parent aggregate:
//
// var dept = factory.Create<Department>();
// var member = memberFactory.Create();
// dept.Members.Add(member);
//
// Assert.True(member.IsChild);   // Part of aggregate
// Assert.Same(dept, member.Parent);
// Assert.Same(dept, member.Root);  // Aggregate root
//
// // Children are saved when their root is saved
// await factory.SaveAsync(dept); // Saves dept AND all members
#endregion

#region entities-factory-services
// Inject services into factory methods:
//
// [Fetch]
// public async Task FetchAsync(
//     int id,
//     [Service] IEmployeeRepository repository,
//     [Service] ILogger<Employee> logger)
// {
//     logger.LogInformation("Fetching employee {Id}", id);
//     var data = await repository.FetchAsync(id);
//     // ...
// }
#endregion

#region entities-save-cancellation
// Save supports cancellation:
//
// using var cts = new CancellationTokenSource();
//
// try
// {
//     await factory.SaveAsync(employee, cts.Token);
// }
// catch (OperationCanceledException)
// {
//     // Save was cancelled
// }
#endregion

#region entities-parent-property
// Parent property tracks aggregate relationships:
//
// var dept = deptFactory.Create();
// var member = memberFactory.Create();
//
// dept.Members.Add(member);
//
// Assert.Same(dept, member.Parent);
// Assert.True(member.IsChild);
//
// // Root finds the aggregate root
// Assert.Same(dept, member.Root);
// Assert.Null(dept.Root); // Root has no parent
#endregion

// -----------------------------------------------------------------------------
// Repository Interface
// -----------------------------------------------------------------------------

public interface ISkillEntityRepository
{
    Task InsertAsync(SkillEntityEmployee employee);
    Task UpdateAsync(SkillEntityEmployee employee);
    Task DeleteAsync(SkillEntityEmployee employee);
}
