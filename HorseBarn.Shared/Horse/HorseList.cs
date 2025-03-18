#if !CLIENT
using HorseBarn.Dal.Ef;
#endif

using HorseBarn.lib.Cart;
using Neatoo;
using Neatoo.RemoteFactory;
using System.Collections.Specialized;

namespace HorseBarn.lib.Horse;

public interface IHorseList : IEditListBase<IHorse>, INotifyCollectionChanged
{

    internal void RemoveHorse(IHorse horse);
}

[Factory]
internal class HorseList : EditListBase<IHorse>, IHorseList
{
    public HorseList() : base()
    {
    }

    public void RemoveHorse(IHorse horse)
    {
        if (Contains(horse))
        {
            Remove(horse);
        }
    }

    [Create]
    public void Create()
    {

    }

#if !CLIENT

    [Fetch]
    public void Fetch(ICollection<Dal.Ef.Horse> horses,
                                    [Service] ILightHorseFactory lightHorsePortal,
                                    [Service] IHeavyHorseFactory heavyHorsePortal)
    {
        foreach (var horse in horses)
        {
            if (IHorse.IsLightHorse((Breed)horse.Breed))
            {
                var h = lightHorsePortal.Fetch(horse);
                Add(h);
            }
            else
            {
                var h = heavyHorsePortal.Fetch(horse);
                Add(h);
            }
        }
    }

    [Update]
    public void Update(Dal.Ef.Cart cart,
                                    [Service] ILightHorseFactory lightHorsePortal,
                                    [Service] IHeavyHorseFactory heavyHorsePortal)
    {
        foreach (var horse in this.Union(DeletedList))
        {
            if (horse is ILightHorse h)
            {
                lightHorsePortal.Save(h, cart);
            }
            else if (horse is IHeavyHorse hh)
            {
                heavyHorsePortal.Save(hh, cart);
            }
        }

        DeletedList.Clear();
    }

    [Update]
    public void Update(Dal.Ef.Pasture pasture,
                                    [Service] ILightHorseFactory lightHorsePortal,
                                    [Service] IHeavyHorseFactory heavyHorsePortal)
    {
        void SaveHorse(IHorse horse)
        {
            if (horse is ILightHorse h)
            {
                lightHorsePortal.Save(h, pasture);
            }
            else if (horse is IHeavyHorse hh)
            {
                heavyHorsePortal.Save(hh, pasture);
            }
        }
        foreach (var horse in DeletedList) // TODO: Room for improvement here
        {
            if (horse.IsDeleted)
            {
                SaveHorse(horse);
            }
        }

        DeletedList.Clear();

        foreach (var horse in this)
        {
            SaveHorse(horse);
        }
    }

#endif
}
