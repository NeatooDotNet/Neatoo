using Neatoo.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HorseBarn.lib.Horse
{
    public interface IHorseNameUniqueRule : IRule<IHorseCriteria>
    {

    }

    internal class HorseNameUniqueRule : AsyncRuleBase<IHorseCriteria>, IHorseNameUniqueRule
    {

        public HorseNameUniqueRule(IsHorseNameUnique isHorseNameUnique)
        {
            IsHorseNameUnique = isHorseNameUnique;
            AddTriggerProperties(t => t.Name);
        }

        public IsHorseNameUnique IsHorseNameUnique { get; }

        public override async Task<PropertyErrors> Execute(IHorseCriteria t, CancellationToken? token = null)
        {
            if (!string.IsNullOrWhiteSpace(t.Name))
            {
                var isUnique = await IsHorseNameUnique(t.Name);

                if (!isUnique)
                {
                    return new PropertyError(nameof(t.Name), "Name must be unique");
                }
            }

            return None;
        }
    }
}
