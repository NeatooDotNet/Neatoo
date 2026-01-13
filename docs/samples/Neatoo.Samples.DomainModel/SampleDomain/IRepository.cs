/// <summary>
/// Generic repository interface for samples.
/// Used to show real persistence patterns without commented-out code.
/// No implementation needed - just enables compilation.
/// </summary>

namespace Neatoo.Samples.DomainModel.SampleDomain;

/// <summary>
/// Generic repository interface for entity persistence.
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> FindAsync(object id);
    Task AddAsync(TEntity entity);
    Task RemoveAsync(TEntity entity);
    Task SaveChangesAsync();
}

/// <summary>
/// Repository with collection support for parent-child relationships.
/// </summary>
public interface IRepositoryWithChildren<TEntity, TChildEntity> : IRepository<TEntity>
    where TEntity : class
    where TChildEntity : class
{
    ICollection<TChildEntity> GetChildren(TEntity entity);
}

/// <summary>
/// Mock repository for testing - tracks added/removed entities.
/// </summary>
public class MockRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly List<TEntity> _entities = new();

    public Task<TEntity?> FindAsync(object id) => Task.FromResult<TEntity?>(null);
    public Task AddAsync(TEntity entity) { _entities.Add(entity); return Task.CompletedTask; }
    public Task RemoveAsync(TEntity entity) { _entities.Remove(entity); return Task.CompletedTask; }
    public Task SaveChangesAsync() => Task.CompletedTask;
}

/// <summary>
/// Mock repository with children support.
/// </summary>
public class MockRepositoryWithChildren<TEntity, TChildEntity> : MockRepository<TEntity>, IRepositoryWithChildren<TEntity, TChildEntity>
    where TEntity : class
    where TChildEntity : class
{
    public ICollection<TChildEntity> GetChildren(TEntity entity) => new List<TChildEntity>();
}
