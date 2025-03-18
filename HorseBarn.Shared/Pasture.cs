using HorseBarn.lib.Horse;
using Neatoo;
using System.Diagnostics;
using Neatoo.RemoteFactory;


#if !CLIENT
using HorseBarn.Dal.Ef;
using Microsoft.EntityFrameworkCore;
#endif

namespace HorseBarn.lib;

public interface IPasture : IEditBase
{
    internal int? Id { get; }
    internal IHorseList HorseList { get; }
    public IReadOnlyListBase<IHorse> Horses { get; }

    internal void RemoveHorse(IHorse horse);
}

[Factory]
internal class Pasture : CustomEditBase<Pasture>, IPasture
{
    public Pasture(IEditBaseServices<Pasture> services) : base(services)
    {

    }

    public IHorseList HorseList {  get => Getter<IHorseList>(); private set => Setter(value); }

    public IReadOnlyListBase<IHorse> Horses => HorseList;

    public void RemoveHorse(IHorse horse)
    {
        HorseList.RemoveHorse(horse);
    }

    [Create]
    public void Create([Service] IHorseListFactory horseListPortal)
    {
        HorseList = horseListPortal.Create(); 
    }

#if !CLIENT



    [Fetch]
    public void Fetch(Dal.Ef.Pasture pasture, [Service] IHorseListFactory horseListPortal)
    {
        this.Id = pasture.Id;
        this.HorseList = horseListPortal.Fetch(pasture.Horses);
    }

    [Insert]
    public void Insert(Dal.Ef.HorseBarn horseBarn, [Service] IHorseListFactory horseListPortal)
    {
        var pasture = new Dal.Ef.Pasture();
        pasture.PropertyChanged += HandleIdPropertyChanged;
        horseBarn.Pasture = pasture;
        horseListPortal.Save(HorseList, pasture);
    }

    [Update]
    public void Update(Dal.Ef.HorseBarn horseBarn, [Service] IHorseListFactory horseListPortal)
    {
        var pasture = horseBarn.Pasture;
        Debug.Assert(pasture.Id == this.Id, "Unexpected Id");
        horseListPortal.Save(HorseList, pasture);
    }

#endif

}
