# Entity Framework Integration

Neatoo integrates with Entity Framework Core for persistence. The domain model remains decoupled from EF entities through mapper methods.

## DbContext Setup

Define an interface for your DbContext to enable testing and dependency injection:

<!-- snippet: dbcontext-interface -->
```cs
public interface ISampleDbContext
{
    DbSet<PersonEntity> Persons { get; }
    Task<PersonEntity?> FindPerson(Guid id);
    void AddPerson(PersonEntity person);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```
<!-- endSnippet -->

Implement the DbContext:

<!-- snippet: dbcontext-class -->
```cs
public class SampleDbContext : DbContext, ISampleDbContext
{
    public virtual DbSet<PersonEntity> Persons { get; set; } = null!;

    public string DbPath { get; }

    public SampleDbContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "NeatooSamples.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={DbPath}")
                         .UseLazyLoadingProxies();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<PersonEntity>().Property(e => e.Id).ValueGeneratedNever();
    }

    public void AddPerson(PersonEntity person) => Persons.Add(person);

    public Task<PersonEntity?> FindPerson(Guid id)
        => Persons.FirstOrDefaultAsync(p => p.Id == id);
}
```
<!-- endSnippet -->

## EF Entity Classes

EF entities are separate from Neatoo domain entities:

<!-- snippet: entity-class -->
```cs
public class PersonEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string FirstName { get; set; } = null!;

    [Required]
    public string LastName { get; set; } = null!;

    public string? Email { get; set; }
}
```
<!-- endSnippet -->

## Mapping Between Domain and EF Entities

Use mapper methods to transfer data between domain entities and EF entities:

<!-- pseudo:ef-mapper-usage -->
```csharp
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    public partial Guid Id { get; set; }
    public partial string FirstName { get; set; }
    public partial string LastName { get; set; }
    public partial string? Email { get; set; }

    [Fetch]
    public async Task Fetch(Guid id, [Service] ISampleDbContext db)
    {
        var entity = await db.FindPerson(id);
        if (entity == null) throw new KeyNotFoundException();
        MapFrom(entity);  // Copy from EF entity to domain entity
    }

    [Insert]
    public async Task Insert([Service] ISampleDbContext db)
    {
        await RunRules();
        if (!IsSavable) return;

        var entity = new PersonEntity();
        MapTo(entity);  // Copy all properties
        db.AddPerson(entity);
        await db.SaveChangesAsync();
    }

    [Update]
    public async Task Update([Service] ISampleDbContext db)
    {
        await RunRules();
        if (!IsSavable) return;

        var entity = await db.FindPerson(Id);
        if (entity == null) throw new KeyNotFoundException();
        MapModifiedTo(entity);  // Copy only modified properties
        await db.SaveChangesAsync();
    }
}
```
<!-- /snippet -->

## Best Practices

### 1. Keep EF Entities Simple

EF entities should be simple POCOs without business logic:

<!-- pseudo:ef-entity-simple -->
```csharp
// Good: Simple data class
public class PersonEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

// Avoid: Business logic in EF entity
public class PersonEntity
{
    public string FullName => $"{FirstName} {LastName}";  // Domain logic
}
```
<!-- /snippet -->

### 2. Use Interface for DbContext

Defining an interface enables:
- Unit testing with mock contexts
- Dependency injection flexibility
- Clear contract for data access

### 3. MapModifiedTo for Updates

Use `MapModifiedTo` instead of `MapTo` when updating:

<!-- pseudo:mapmodifiedto-usage -->
```csharp
// Good: Only updates changed properties
MapModifiedTo(entity);

// Less efficient: Updates all properties
MapTo(entity);
```
<!-- /snippet -->

## See Also

- [Mapper Methods](mapper-methods.md) - MapFrom, MapTo, MapModifiedTo
- [Factory Operations](factory-operations.md) - Fetch, Insert, Update, Delete
- [Remote Factory](remote-factory.md) - Client-server persistence
