using HorseBarn.lib.Cart;
using HorseBarn.lib.Horse;
using Neatoo;
using Neatoo.RemoteFactory;

#if !CLIENT
using Microsoft.EntityFrameworkCore;
using HorseBarn.Dal.Ef;
#endif

namespace HorseBarn.lib;

public interface IHorseBarn : IEditBase
{
    internal int? Id { get; }
    IPasture Pasture { get; }
    IReadOnlyListBase<ICart> Carts { get; }
    IEnumerable<IHorse> Horses { get; }
    IHorse AddNewHorse(IHorseCriteria horseCriteria);
    Task<IRacingChariot> AddRacingChariot();
    Task<IWagon> AddWagon();
    void MoveHorseToCart(IHorse horse, ICart cart);
    void MoveHorseToPasture(IHorse horse);
}

[Factory]
internal class HorseBarn : CustomEditBase<HorseBarn>, IHorseBarn
{
    private readonly ILightHorseFactory lightHorsePortal;
    private readonly IHeavyHorseFactory heavyHorsePortal;
    private readonly IRacingChariotFactory racingChariotPortal;
    private readonly IWagonFactory wagonPortal;

    public HorseBarn(IEditBaseServices<HorseBarn> services,
                        ILightHorseFactory lightHorsePortal,
                        IHeavyHorseFactory heavyHorsePortal,
                        IRacingChariotFactory racingChariotPortal,
                        IWagonFactory wagonPortal) : base(services)
    {
        this.lightHorsePortal = lightHorsePortal;
        this.heavyHorsePortal = heavyHorsePortal;
        this.racingChariotPortal = racingChariotPortal;
        this.wagonPortal = wagonPortal;
    }

    public IPasture Pasture { get => Getter<IPasture>()!; private set => Setter(value); }
    public ICartList Carts { get => Getter<ICartList>()!; private set => Setter(value); }
    public IEnumerable<IHorse> Horses => Carts.SelectMany(c => c.Horses).Union(Pasture.HorseList);

    IReadOnlyListBase<ICart> IHorseBarn.Carts => this.Carts;

    public async Task<IRacingChariot> AddRacingChariot()
    {
        var newCart = await racingChariotPortal.Create();
        this.Carts.Add(newCart);
        return newCart;
    }

    public async Task<IWagon> AddWagon()
    {
        var newCart = await wagonPortal.Create();
        this.Carts.Add(newCart);
        return newCart;
    }

    public IHorse AddNewHorse(IHorseCriteria horseCriteria)
    {
        IHorse horse;

        if (IHorse.IsLightHorse(horseCriteria.Breed))
        {
            horse = lightHorsePortal.Create(horseCriteria);
        }
        else if (IHorse.IsHeavyHorse(horseCriteria.Breed))
        {
            horse = heavyHorsePortal.Create(horseCriteria);
        }
        else
        {
            throw new Exception($"Cannot create child horse for breed {horseCriteria.Breed}");
        }

        this.Pasture.HorseList.Add(horse);
        return horse;
    }

    public void MoveHorseToCart(IHorse horse, ICart cart)
    {
        Pasture.RemoveHorse(horse);
        Carts.RemoveHorse(horse);
        cart.AddHorse(horse);
    }

    public void MoveHorseToPasture(IHorse horse)
    {
        Carts.RemoveHorse(horse);

        if (!Pasture.HorseList.Contains(horse))
        {
            Pasture.HorseList.Add(horse);
        }
    }

    [Create]
    public void Create([Service] IPastureFactory pasturePortal, [Service] ICartListFactory cartListPortal)
    {
        this.Pasture = pasturePortal.Create();
        this.Carts = cartListPortal.Create();
    }

#if !CLIENT



    [Remote]
    [Fetch]
    public async Task Fetch([Service] IHorseBarnContext horseBarnContext,
                            [Service] IPastureFactory pasturePortal,
                            [Service] ICartListFactory cartPortal)
    {

        var horseBarn = await horseBarnContext.HorseBarns.FirstOrDefaultAsync();
        if (horseBarn != null)
        {
            this.Id = horseBarn.Id;
            this.Pasture = pasturePortal.Fetch(horseBarn.Pasture);
            this.Carts = cartPortal.Fetch(horseBarn.Carts);
        }
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IHorseBarnContext horseBarnContext,
                            [Service] IPastureFactory pasturePortal,
                            [Service] ICartListFactory cartPortal)
    {
        var horseBarn = new Dal.Ef.HorseBarn();

        horseBarn.PropertyChanged += HandleIdPropertyChanged;

        pasturePortal.Save(this.Pasture, horseBarn);
        cartPortal.Save(this.Carts, horseBarn);

        horseBarnContext.HorseBarns.Add(horseBarn);

        await horseBarnContext.SaveChangesAsync();
    }

    [Remote]
    [Update]
    public async Task Update([Service] IHorseBarnContext horseBarnContext,
                            [Service] IPastureFactory pasturePortal,
                            [Service] ICartListFactory cartPortal)
    {
        var horseBarn = await horseBarnContext.HorseBarns.FirstAsync(hb => hb.Id == this.Id);
        if (this.Pasture.IsModified)
        {
            pasturePortal.Save(this.Pasture, horseBarn);
        }

        if (this.Carts.IsModified)
        {
            cartPortal.Save(this.Carts, horseBarn);
        }

        await horseBarnContext.SaveChangesAsync();
    }

#endif

#if CLIENT

    [Fetch]
    [Remote]
    public void Fetch()
    {
    }

    [Update]
    [Remote]
    public void Update()
    {
    }
#endif

}
