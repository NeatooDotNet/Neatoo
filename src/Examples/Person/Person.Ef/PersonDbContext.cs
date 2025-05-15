
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;

namespace Person.Ef;

public interface IPersonDbContext
{
	DbSet<PersonEntity> Persons { get; }
    DbSet<PersonPhoneEntity> PersonPhones { get; }
	Task<PersonEntity> FindPerson(Guid? id = null);
	void AddPerson(PersonEntity personEntity);
    Task DeleteAllPersons();
	void DeletePerson(PersonEntity person);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class PersonDbContext : DbContext, IPersonDbContext
{
	public virtual DbSet<PersonEntity> Persons { get; set; } = null!;
	public virtual DbSet<PersonPhoneEntity> PersonPhones { get; set; } = null!;

    public string DbPath { get; }

	public PersonDbContext()
	{
		var folder = Environment.SpecialFolder.LocalApplicationData;
		var path = Environment.GetFolderPath(folder);
		this.DbPath = System.IO.Path.Join(path, "NeatooPerson.db");
    }

	public PersonDbContext(DbContextOptions<PersonDbContext> options)
        : base(options)
    {
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
         => optionsBuilder.UseSqlite($"Data Source={this.DbPath}")
                            .UseLazyLoadingProxies()
                            .LogTo(Console.WriteLine);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PersonEntity>().Property(e => e.Id).ValueGeneratedNever();
        modelBuilder.Entity<PersonPhoneEntity>().Property(e => e.Id).ValueGeneratedNever();
    }
    public void AddPerson(PersonEntity personEntity)
	{
        Persons.Add(personEntity);
    }

    public Task<PersonEntity?> FindPerson(Guid? id = null)
	{
        return Persons.FirstOrDefaultAsync(_ => id == null || _.Id == id);
    }

    public async Task DeleteAllPersons()
    {
		await PersonPhones.ExecuteDeleteAsync();
        await Persons.ExecuteDeleteAsync();
    }

    public void DeletePerson(PersonEntity person)
    {
		PersonPhones.RemoveRange(person.Phones);
        Persons.Remove(person);
    }
}

public class PersonEntity
{
    [Key]
    public Guid? Id { get; set; }

    [Required]
	public string FirstName { get; set; } = null!;
    [Required]
	public string LastName { get; set; } = null!;
    public string? Email { get; set; }
	public string? Phone { get; set; }
	public string? Notes { get; set; }
    public virtual ICollection<PersonPhoneEntity> Phones { get; } = [];
}

public class PersonPhoneEntity
{
	[Key]
	public Guid? Id { get; set; }

    [Required]
    public string PhoneNumber { get; set; } = null!;

    [Required]
    public int PhoneType { get; set; }

    public virtual PersonEntity PersonEntity { get; set; }

}