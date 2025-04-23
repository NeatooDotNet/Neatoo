using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.Console
{

    [Authorize<NeatooEditBaseAuth>]
    public partial class NeatooEditBase : ValidateBase<NeatooEditBase>
    {
        public static uint TotalCount = 0;

        public NeatooEditBase() : base(new ValidateBaseServices<NeatooEditBase>())
        {
            TotalCount++;
        }

        [Create]
        public void Create([Service] INeatooEditBaseFactory factory)
        {
            this.Id = 1;
            this.Description = Guid.NewGuid().ToString();
            this.ChildA = factory.Create(2);
            this.ChildB = factory.Create(2);
        }

        [Create]
        public void Create(int id, [Service] INeatooEditBaseFactory factory)
        {
            this.Id = id;
            this.Description = Guid.NewGuid().ToString();
            if (id < 15)
            {
                this.ChildA = factory.Create(id + 1);
                this.ChildB = factory.Create(id + 1);
            }
        }

        [Required]
        public partial int Id { get; set; }
        [Required]
        public partial string? Description { get; set; }
        public partial NeatooEditBase? ChildA { get; set; }
        public partial NeatooEditBase? ChildB { get; set; }
    }

    internal class NeatooEditBaseAuth
    {
        private readonly IPrincipal principal;

        public NeatooEditBaseAuth(IPrincipal principal)
        {
            this.principal = principal;
        }

        [Authorize(AuthorizeOperation.Create)]
        public bool CanCreate()
        {
            return principal.IsInRole("Admin");
        }
    }
}
