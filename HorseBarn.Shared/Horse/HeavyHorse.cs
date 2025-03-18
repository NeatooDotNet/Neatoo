using Neatoo;
using Neatoo.RemoteFactory;

namespace HorseBarn.lib.Horse;

public interface IHeavyHorse : IHorse
{
}

[Factory]
internal class HeavyHorse : Horse<HeavyHorse>, IHeavyHorse
{
    public HeavyHorse(IEditBaseServices<HeavyHorse> services) : base(services)
    {
    }

    [Create]
    public void Create(IHorseCriteria horseCriteria)
    {

        if (!IHorse.IsHeavyHorse(horseCriteria.Breed))
        {
            throw new Exception($"Incorrect Breed: {horseCriteria.Breed.ToString()}");
        }

        base.Create(horseCriteria);
    }
}
