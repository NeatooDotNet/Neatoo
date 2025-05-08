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

    [Authorize<NeatooEntityBaseAuth>]
    public partial class NeatooEntityBase : ValidateBase<NeatooEntityBase>
    {
        public static uint TotalCount = 0;

        public NeatooEntityBase() : base(new ValidateBaseServices<NeatooEntityBase>())
        {
            TotalCount++;
        }

        [Create]
        public void Create([Service] INeatooEntityBaseFactory factory)
        {
            this.Id = 1;
            this.Description = Guid.NewGuid().ToString();
            this.ChildA = factory.Create(2);
            this.ChildB = factory.Create(2);
        }

        [Create]
        public void Create(int id, [Service] INeatooEntityBaseFactory factory)
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
        public partial NeatooEntityBase? ChildA { get; set; }
        public partial NeatooEntityBase? ChildB { get; set; }
    }

    internal class NeatooEntityBaseAuth
    {
        private readonly IPrincipal principal;

        public NeatooEntityBaseAuth(IPrincipal principal)
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
