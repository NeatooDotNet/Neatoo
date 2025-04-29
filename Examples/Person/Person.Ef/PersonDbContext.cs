
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
	Task<PersonEntity> FindPerson(int? id = null);
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
		 .UseLazyLoadingProxies();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Ef doesn't use property getter/setters by default
		// https://stackoverflow.com/questions/47382680/entity-framework-core-property-setter-is-never-called-violation-of-encapsulat
		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			if (entityType.ClrType.IsAssignableTo(typeof(IdPropertyChangedBase)))
			{
				var property = entityType.FindProperty(nameof(IdPropertyChangedBase.Id));

				property?.SetPropertyAccessMode(PropertyAccessMode.PreferFieldDuringConstruction);

			}
		}
	}

	public void AddPerson(PersonEntity personEntity)
	{
        Persons.Add(personEntity);
    }

	public Task<PersonEntity?> FindPerson(int? id = null)
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

public class PersonEntity : IdPropertyChangedBase
{
	[Required]
	public string FirstName { get; set; } = null!;
    [Required]
	public string LastName { get; set; } = null!;
    public string? Email { get; set; }
	public string? Phone { get; set; }
	public string? Notes { get; set; }
    public virtual IList<PersonPhoneEntity> Phones { get; } = [];
}

public class PersonPhoneEntity : IdPropertyChangedBase
{
    [Required]
    public string PhoneNumber { get; set; } = null!;

    public virtual int PersonId { get; set; }
	public virtual int PhoneType { get; set; }
}

public abstract class IdPropertyChangedBase : INotifyPropertyChanged
{
	private int? _id;

	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int? Id
	{
		get => this._id;
		set
		{
			this._id = value;
			this.OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}