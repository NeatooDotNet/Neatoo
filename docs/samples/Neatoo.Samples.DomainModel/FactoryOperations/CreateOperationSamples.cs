/// <summary>
/// Code samples for docs/factory-operations.md - Create operation section
///
/// Snippets injected into docs:
/// - docs:factory-operations:create-with-service
///
/// Compile-time validation only (simpler create pattern):
/// - docs:factory-operations:create-basic
///
/// Corresponding tests: CreateOperationSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;

namespace Neatoo.Samples.DomainModel.FactoryOperations;

#region create-basic
/// <summary>
/// Basic entity with simple Create operation.
/// </summary>
public partial interface ISimpleProduct : IEntityBase
{
    Guid Id { get; }
    string? Name { get; set; }
    DateTime CreatedDate { get; }
}

[Factory]
internal partial class SimpleProduct : EntityBase<SimpleProduct>, ISimpleProduct
{
    public SimpleProduct(IEntityBaseServices<SimpleProduct> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }
    public partial DateTime CreatedDate { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
        CreatedDate = DateTime.UtcNow;
    }
}
#endregion

#region create-with-service
/// <summary>
/// Entity with Create operation using service injection.
/// </summary>
public partial interface IProjectWithTasks : IEntityBase
{
    Guid Id { get; }
    string? Name { get; set; }
    IProjectTaskList Tasks { get; }
}

public partial interface IProjectTask : IEntityBase
{
    Guid Id { get; }
    string? Title { get; set; }
}

public interface IProjectTaskList : IEntityListBase<IProjectTask> { }

[Factory]
internal partial class ProjectTask : EntityBase<ProjectTask>, IProjectTask
{
    public ProjectTask(IEntityBaseServices<ProjectTask> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Title { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}

[Factory]
internal class ProjectTaskList : EntityListBase<IProjectTask>, IProjectTaskList
{
    [Create]
    public void Create() { }
}

[Factory]
internal partial class ProjectWithTasks : EntityBase<ProjectWithTasks>, IProjectWithTasks
{
    public ProjectWithTasks(IEntityBaseServices<ProjectWithTasks> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? Name { get; set; }
    public partial IProjectTaskList Tasks { get; set; }

    [Create]
    public void Create([Service] IProjectTaskListFactory taskListFactory)
    {
        Id = Guid.NewGuid();
        Tasks = taskListFactory.Create();
    }
}
#endregion
