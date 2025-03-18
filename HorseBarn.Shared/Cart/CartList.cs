#if !CLIENT
using HorseBarn.Dal.Ef;
#endif
using HorseBarn.lib.Horse;
using Neatoo;
using Neatoo.RemoteFactory;

namespace HorseBarn.lib.Cart;



public interface ICartList : IEditListBase<ICart>

{

    internal void RemoveHorse(IHorse horse);
}

[Factory]
internal class CartList : EditListBase<ICart>, ICartList
{
    public CartList() : base()
    {

    }

    public void RemoveHorse(IHorse horse)
    {
        foreach(var c in this)
        {
            c.RemoveHorse(horse);
        }
    }

    [Create]
    public void Create()
    {
    }

#if !CLIENT

    [Fetch]
    public void Fetch(ICollection<Dal.Ef.Cart> carts, [Service] RacingChariotFactory racingChariotPortal,[Service] WagonFactory wagonPortal)
    {
        foreach (var cart in carts)
        {
            if (cart.CartType == (int)CartType.RacingChariot)
            {
                Add(racingChariotPortal.Fetch(cart));
            }
            else if(cart.CartType == (int)CartType.Wagon)
            {
                Add(wagonPortal.Fetch(cart));
            }
        }
    }

    [Update]
    public void Update(Dal.Ef.HorseBarn horseBarn, [Service] RacingChariotFactory racingChariotPortal, [Service] WagonFactory wagonPortal)
    {
        void SaveCart(ICart cart)
        {
            if (cart is RacingChariot racingChariot)
            {
                racingChariotPortal.Save(racingChariot, horseBarn);
            }
            else if (cart is Wagon wagon)
            {
                wagonPortal.Save(wagon, horseBarn);
            }
        }

        foreach (var cart in this.DeletedList)
        {
            if (cart.IsDeleted)
            {
                SaveCart(cart);
            }
        }

        DeletedList.Clear();

        foreach (var cart in this)
        {
            SaveCart(cart);
        }
    }
#endif
}
