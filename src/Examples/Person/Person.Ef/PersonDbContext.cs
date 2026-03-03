
using Microsoft.EntityFrameworkCore;
using Person.Dal;

namespace Person.Ef;

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
                            .UseLazyLoadingProxies();

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

    public Task<PersonEntity?> FindPerson(CancellationToken cancellationToken = default)
	{
        return Persons.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PersonEntity?> FindPerson(Guid? id, CancellationToken cancellationToken = default)
	{
        return Persons.FirstOrDefaultAsync(_ => _.Id == id, cancellationToken);
    }

    public async Task DeleteAllPersons(CancellationToken cancellationToken = default)
    {
		await PersonPhones.ExecuteDeleteAsync(cancellationToken);
        await Persons.ExecuteDeleteAsync(cancellationToken);
    }

    public void DeletePerson(PersonEntity person)
    {
		PersonPhones.RemoveRange(person.Phones);
        Persons.Remove(person);
    }

    public Task<bool> PersonNameExists(Guid? excludeId, string firstName, string lastName)
    {
        return Persons.AnyAsync(x => (excludeId == null || x.Id != excludeId) && x.FirstName == firstName && x.LastName == lastName);
    }
}
