using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.UnitTest.Performance
{

    [Factory]
    [AuthorizeFactory<NeatooEntityBaseAuth>]
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

        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        public bool CanCreate()
        {
            return principal.IsInRole("Admin");
        }
    }

    [TestClass]
    public class DeepTreeTests
    {
        private INeatooEntityBaseFactory factory;

        [TestInitialize]
        public void Initialize()
        {
            var serviceContainer = new ServiceCollection();
            serviceContainer.AddNeatooServices(NeatooFactory.Logical, typeof(DeepTreeTests).Assembly);
            serviceContainer.AddScoped<NeatooEntityBaseAuth>();
            serviceContainer.AddScoped<IPrincipal>(s => CreateDefaultClaimsPrincipal());
            var serviceProvider = serviceContainer.BuildServiceProvider();

            factory = serviceProvider.GetRequiredService<INeatooEntityBaseFactory>();
        }

        [TestMethod]
        public void DeepTreeTests_15()
        {
            var deepTree = factory.Create(1);
            Assert.AreEqual((uint) 32767, NeatooEntityBase.TotalCount);
        }

        static ClaimsPrincipal CreateDefaultClaimsPrincipal()
        {
            var identity = new ClaimsIdentity(new GenericIdentity("Admin"));

            identity.AddClaim(new Claim("Id", Guid.NewGuid().ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));

            return new ClaimsPrincipal(identity);
        }
    }



}
