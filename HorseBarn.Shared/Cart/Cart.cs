using HorseBarn.lib.Horse;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace HorseBarn.lib.Cart;


internal class Cart<C, H> : CustomEditBase<C>, ICart
    where C : Cart<C, H>
    where H : IHorse
{
    public Cart(IEditBaseServices<C> services,
                ICartNumberOfHorsesRule cartNumberOfHorsesRule) : base(services)
    {
        RuleManager.AddRule(cartNumberOfHorsesRule);
    }

    event NotifyCollectionChangedEventHandler? INotifyCollectionChanged.CollectionChanged
    {
        add
        {
            HorseList.CollectionChanged += value;
        }

        remove
        {
            HorseList.CollectionChanged -= value;
        }
    }

    [Required]
    public string Name { get => Getter<string>(); set => Setter(value); }

    [Required]
    public int NumberOfHorses { get => Getter<int>(); set => Setter(value); }

    public IHorseList HorseList { get => Getter<IHorseList>(); private set => Setter(value); }

    internal IEnumerable<IHorse> Horses => HorseList.Cast<IHorse>();

    IEnumerable<IHorse> ICart.Horses => HorseList.Cast<IHorse>();


    public void RemoveHorse(IHorse horse)
    {
        HorseList.RemoveHorse(horse);
    }

    public void AddHorse(IHorse horse)
    {
        if (horse is H h)
        {
            HorseList.Add(h);
        }
        else
        {
            throw new ArgumentException($"Horse {horse.GetType().FullName} is not of type {typeof(H).FullName}");
        }
    }

    public bool CanAddHorse(IHorse horse)
    {
        if (horse is H && HorseList.Count < NumberOfHorses)
        {
            return true;
        }
        return false;
    }

    protected virtual CartType CartType => throw new NotImplementedException();

    [Create]
    public async Task Create([Service] IHorseListFactory horsePortal,
                [Service] IAllRequiredRulesExecuted.Factory allRequiredRulesExecutedFactory)
    {
        this.HorseList = horsePortal.Create();
        this.NumberOfHorses = 1;


        var allRequiredRulesExecuted = allRequiredRulesExecutedFactory(RuleManager.Rules.OfType<IRequiredRule>());
        RuleManager.AddRule(allRequiredRulesExecuted);

        // This is needed for IAllRequiredRulesExecuted rule to do it's thing
        // TODO : Should ValidateBase pause? What should "paused" be?
        // There's tradeoffse to everything!
        await CheckRules(nameof(NumberOfHorses));
        await allRequiredRulesExecuted.RunRule(this);
    }

#if !CLIENT

    [Fetch]
    public void Fetch(Dal.Ef.Cart cart, [Service] IHorseListFactory horsePortal)
    {
        this.Id = cart.Id;
        this.Name = cart.Name;
        this.NumberOfHorses = cart.NumberOfHorses;
        this.HorseList = horsePortal.Fetch(cart.Horses);
    }

    [Insert]
    internal void Insert(Dal.Ef.HorseBarn horseBarn, [Service] IHorseListFactory horsePortal)
    {
        Dal.Ef.Cart cart = new Dal.Ef.Cart();

        cart.Name = this.Name;
        cart.CartType = (int)this.CartType;
        cart.NumberOfHorses = this.NumberOfHorses;
        cart.HorseBarn = horseBarn;
        cart.PropertyChanged += HandleIdPropertyChanged;

        horseBarn.Carts.Add(cart);

        horsePortal.Save(this.HorseList, cart);
    }

    [Update]
    internal void Update(Dal.Ef.HorseBarn horseBarn, [Service] IHorseListFactory horsePortal)
    {
        var cart = horseBarn.Carts.First(c => c.Id == this.Id);

        Debug.Assert(cart.CartType == cart.CartType, "CartType mismatch");

        cart.Name = this.Name;
        cart.NumberOfHorses = this.NumberOfHorses;
        horsePortal.Save(this.HorseList, cart);
    }

#endif
}
