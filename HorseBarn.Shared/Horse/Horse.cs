using Neatoo;
using System.ComponentModel.DataAnnotations;
using Neatoo.RemoteFactory;


#if !CLIENT
using HorseBarn.Dal.Ef;
using Microsoft.EntityFrameworkCore;
#endif
namespace HorseBarn.lib.Horse;


public interface IHorse : IEditBase
{
    internal int? Id { get; }
    string Name { get; set; }
    DateOnly BirthDate { get; set; }
    Breed Breed { get; }

    private static IEnumerable<Breed> LightHorses = [Horse.Breed.QuarterHorse, Horse.Breed.Thoroughbred, Horse.Breed.Mustang];

    private static IEnumerable<Breed> HeavyHorses = [Horse.Breed.Clydesdale, Horse.Breed.Shire];

    internal static bool IsLightHorse(Breed breed)
    {
        return LightHorses.Contains(breed);
    }

    internal static bool IsHeavyHorse(Breed breed)
    {
        return HeavyHorses.Contains(breed);
    }
}

// TODO : Abstract causes portal methods (ex [Create]) to not be recognized by PortalOperationManager and I don't know why
internal class Horse<H> : CustomEditBase<H>, IHorse
    where H : Horse<H>
{
    public Horse(IEditBaseServices<H> services) : base(services)
    {
        RuleManager.AddValidation(static hc =>
        {
            if (hc.BirthDate > DateOnly.FromDateTime(DateTime.Now))
            {
                return "Birth date cannot be a future date.";
            }
            return string.Empty;
        }, _ => _.BirthDate);
    }

    [Required]
    public string Name { get => Getter<string>()!; set => Setter(value); }

    [Required]
    public DateOnly BirthDate { get => Getter<DateOnly>()!; set => Setter(value); }

    [Required]
    public Breed Breed { get => Getter<Breed>(); protected set => Setter(value); }

    public double? Weight { get => Getter<double>(); set => Setter(value); }

    internal void Create(IHorseCriteria horseCriteria)
    {
        this.Breed = horseCriteria.Breed;
        this.BirthDate = horseCriteria.BirthDay!.Value;
        this.Name = horseCriteria.Name!;
    }

#if !CLIENT

    [Fetch]
    internal void Fetch(Dal.Ef.Horse horse)
    {
        this.Id = horse.Id;
        this.BirthDate = horse.BirthDate;
        this.Breed = (Breed)horse.Breed;
        this.Name = horse.Name;
    }

    Dal.Ef.Horse? horse;

    [Insert]
    internal void Insert(Dal.Ef.Pasture pasture)
    {
        horse = new Dal.Ef.Horse();
        horse.BirthDate = this.BirthDate;
        horse.Breed = (int)this.Breed;
        horse.Name = this.Name;

        pasture.Horses.Add(horse);

        horse.PropertyChanged += HandleIdPropertyChanged;
    }

    [Insert]
    internal void Insert(Dal.Ef.Cart cart)
    {
        var horse = new Dal.Ef.Horse();
        horse.BirthDate = this.BirthDate;
        horse.Breed = (int)this.Breed;
        horse.Name = this.Name;

        cart.Horses.Add(horse);

        horse.PropertyChanged += HandleIdPropertyChanged;
    }

    [Update]
    internal async Task Update(Dal.Ef.Pasture pasture, [Service] IHorseBarnContext horseBarnContext)
    {
        var horse = await horseBarnContext.Horses.SingleAsync(h => h.Id == this.Id);

        if (horse.Cart != null)
        {
            horse.Cart.Horses.Remove(horse);
        }

        horse.BirthDate = this.BirthDate;
        horse.Breed = (int)this.Breed;
        horse.Name = this.Name;
        pasture.Horses.Add(horse);
    }

    [Update]
    internal async Task Update(Dal.Ef.Cart cart, [Service] IHorseBarnContext horseBarnContext)
    {
        var horse = await horseBarnContext.Horses.SingleAsync(h => h.Id == this.Id);

        if(horse.Pasture != null)
        {
            horse.Pasture.Horses.Remove(horse);
        }
        horse.BirthDate = this.BirthDate;
        horse.Breed = (int)this.Breed;
        horse.Name = this.Name;
        cart.Horses.Add(horse);
    }

    [Delete]
    internal void Delete(Dal.Ef.Cart cart)
    {
        var horse = cart.Horses.First(h => h.Id == this.Id);
        cart.Horses.Remove(horse);
    }

    [Delete]
    internal void Delete(Dal.Ef.Pasture pasture)
    {
        var horse = pasture.Horses.First(h => h.Id == this.Id);
        pasture.Horses.Remove(horse);
    }

#endif
}
