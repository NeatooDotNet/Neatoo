using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Samples;

// =============================================================================
// ENTITY SAMPLES - Demonstrates entity lifecycle and persistence state
// =============================================================================

// -----------------------------------------------------------------------------
// Entity Base Class
// -----------------------------------------------------------------------------

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

// -----------------------------------------------------------------------------
// Repository Interface
// -----------------------------------------------------------------------------

public interface ISkillEntityRepository
{
    Task InsertAsync(SkillEntityEmployee employee);
    Task UpdateAsync(SkillEntityEmployee employee);
    Task DeleteAsync(SkillEntityEmployee employee);
}
