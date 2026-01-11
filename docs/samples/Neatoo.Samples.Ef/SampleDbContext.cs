using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Samples.Ef;

#region dbcontext-interface
public interface ISampleDbContext
{
    DbSet<PersonEntity> Persons { get; }
    Task<PersonEntity?> FindPerson(Guid id);
    void AddPerson(PersonEntity person);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
#endregion

#region dbcontext-class
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
#endregion

#region entity-class
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
#endregion
