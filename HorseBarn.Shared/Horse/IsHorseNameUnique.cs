using Neatoo.RemoteFactory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if !CLIENT
using HorseBarn.Dal.Ef;
#endif

namespace HorseBarn.lib.Horse
{
    public delegate Task<bool> IsHorseNameUnique(string name);

#if !CLIENT 

    
    public class IsHorseNameUniqueServer
    {
        public IsHorseNameUniqueServer(IHorseBarnContext horseBarnContext)
        {
            HorseBarnContext = horseBarnContext;
        }

        public IHorseBarnContext HorseBarnContext { get; }


        [Execute<IsHorseNameUnique>]
        public Task<bool> IsHorseNameUnique(string name)
        {
            return Task.FromResult(!HorseBarnContext.Horses.Any(h => h.Name == name));
        }
    }

#endif
}
