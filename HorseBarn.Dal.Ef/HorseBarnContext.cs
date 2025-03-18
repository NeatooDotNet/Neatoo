using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HorseBarn.Dal.Ef;

public class HorseBarnContext : DbContext, IHorseBarnContext
{
    public DbSet<HorseBarn> HorseBarns { get; set; }
    public DbSet<Horse> Horses { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<Pasture> Pastures { get; set; }

    public string DbPath { get; }

    public HorseBarnContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = System.IO.Path.Join(path, "blogging.db");
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}")
        .UseLazyLoadingProxies();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        var entries = this.ChangeTracker.Entries();

        return result;
    }

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
}

[Table("HorseBarn")]
public class HorseBarn : IdPropertyChangedBase
{
    public virtual Pasture Pasture { get; set; } = null!;
    public virtual List<Cart> Carts { get; set; } = new List<Cart>();
}

[Table("Horse")]
public class Horse : IdPropertyChangedBase
{

    public Horse()
    {

    }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    public DateOnly BirthDate { get; set; }

    [Required]
    public int Breed { get; set; }

    public int? CartId { get; set; }
    public virtual Cart? Cart { get; set; }
    public virtual Pasture? Pasture { get; set; }

}

[Table("Cart")]
public class Cart : IdPropertyChangedBase
{

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    public int CartType { get; set; }

    [Required]
    public int NumberOfHorses { get; set; }

    public virtual ICollection<Horse> Horses { get; set; } = new List<Horse>();

    [Required]
    public int HorseBarnId { get; set; }

    public virtual HorseBarn HorseBarn { get; set; } = null!;

}

[Table("Pasture")]
public class Pasture : IdPropertyChangedBase
{


    public int HorseBarnId { get; set; }
    public virtual HorseBarn HorseBarn { get; set; } = null!;
    public virtual ICollection<Horse> Horses { get; set; } = new List<Horse>();
}

public abstract class IdPropertyChangedBase : INotifyPropertyChanged
{
    private Guid uniqueid = Guid.NewGuid();

    private int _id;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id
    {
        get { return _id; }
        set
        {
            _id = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
