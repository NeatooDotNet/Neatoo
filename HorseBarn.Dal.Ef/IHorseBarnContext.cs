using Microsoft.EntityFrameworkCore;

namespace HorseBarn.Dal.Ef;

public interface IHorseBarnContext
{
    DbSet<Cart> Carts { get; set; }
    DbSet<HorseBarn> HorseBarns { get; set; }
    DbSet<Horse> Horses { get; set; }
    DbSet<Pasture> Pastures { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}