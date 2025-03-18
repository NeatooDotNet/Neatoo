#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using HorseBarn.Dal.Ef;
using HorseBarn.lib.Cart;
using Neatoo;
using System.Collections.Specialized;

/*
Interface Found. TargetType: IHorseList ConcreteType: HorseList
Class: HorseBarn.lib.Horse.HorseList Name: HorseList
No MethodDeclarationSyntax for get_DeletedList
No MethodDeclarationSyntax for get_DeletedList
No MethodDeclarationSyntax for get_EditMetaState
No MethodDeclarationSyntax for InsertItem
No MethodDeclarationSyntax for get_IsPaused
No MethodDeclarationSyntax for set_IsPaused
No MethodDeclarationSyntax for get_MetaState
No MethodDeclarationSyntax for HandleNeatooPropertyChanged
No MethodDeclarationSyntax for RunAllRules
No MethodDeclarationSyntax for RunAllRules
No MethodDeclarationSyntax for RunAllRules
No MethodDeclarationSyntax for RunSelfRules
No MethodDeclarationSyntax for get_Parent
No MethodDeclarationSyntax for get_Parent
No MethodDeclarationSyntax for set_Parent
No MethodDeclarationSyntax for set_Parent
No MethodDeclarationSyntax for add_NeatooPropertyChanged
No MethodDeclarationSyntax for add_NeatooPropertyChanged
No MethodDeclarationSyntax for remove_NeatooPropertyChanged
No MethodDeclarationSyntax for remove_NeatooPropertyChanged
No MethodDeclarationSyntax for Neatoo.Core.ISetParent.SetParent
No MethodDeclarationSyntax for InsertItem
No MethodDeclarationSyntax for PostPortalConstruct
No MethodDeclarationSyntax for RaiseNeatooPropertyChanged
No MethodDeclarationSyntax for HandleNeatooPropertyChanged
No MethodDeclarationSyntax for HandlePropertyChanged
No MethodDeclarationSyntax for WaitForTasks
No MethodDeclarationSyntax for WaitForTasks
No MethodDeclarationSyntax for WaitForTasks
No MethodDeclarationSyntax for .ctor
No MethodDeclarationSyntax for .ctor
No MethodDeclarationSyntax for BlockReentrancy
No MethodDeclarationSyntax for InsertItem
No MethodDeclarationSyntax for OnCollectionChanged
No MethodDeclarationSyntax for OnPropertyChanged
No MethodDeclarationSyntax for SetItem
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute

*/
namespace HorseBarn.lib.Horse
{
    public interface IHorseListFactory
    {
        IHorseList Create();
        IHorseList Fetch(ICollection<Dal.Ef.Horse> horses);
        IHorseList Save(IHorseList target, Dal.Ef.Cart cart);
        IHorseList Save(IHorseList target, Dal.Ef.Pasture pasture);
    }

    internal class HorseListFactory : FactoryBase<IHorseList>, IHorseListFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public HorseListFactory(IServiceProvider serviceProvider, IFactoryCore<IHorseList> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public HorseListFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IHorseList> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public virtual IHorseList Create()
        {
            return LocalCreate();
        }

        public IHorseList LocalCreate()
        {
            var target = ServiceProvider.GetRequiredService<HorseList>();
            return DoFactoryMethodCall(target, FactoryOperation.Create, () => target.Create());
        }

        public virtual IHorseList Fetch(ICollection<Dal.Ef.Horse> horses)
        {
            return LocalFetch(horses);
        }

        public IHorseList LocalFetch(ICollection<Dal.Ef.Horse> horses)
        {
            var target = ServiceProvider.GetRequiredService<HorseList>();
            var lightHorsePortal = ServiceProvider.GetRequiredService<ILightHorseFactory>();
            var heavyHorsePortal = ServiceProvider.GetRequiredService<IHeavyHorseFactory>();
            return DoFactoryMethodCall(target, FactoryOperation.Fetch, () => target.Fetch(horses, lightHorsePortal, heavyHorsePortal));
        }

        public IHorseList LocalUpdate(IHorseList target, Dal.Ef.Cart cart)
        {
            var cTarget = (HorseList)target ?? throw new Exception("IHorseList must implement HorseList");
            var lightHorsePortal = ServiceProvider.GetRequiredService<ILightHorseFactory>();
            var heavyHorsePortal = ServiceProvider.GetRequiredService<IHeavyHorseFactory>();
            return DoFactoryMethodCall(cTarget, FactoryOperation.Update, () => cTarget.Update(cart, lightHorsePortal, heavyHorsePortal));
        }

        public IHorseList LocalUpdate1(IHorseList target, Dal.Ef.Pasture pasture)
        {
            var cTarget = (HorseList)target ?? throw new Exception("IHorseList must implement HorseList");
            var lightHorsePortal = ServiceProvider.GetRequiredService<ILightHorseFactory>();
            var heavyHorsePortal = ServiceProvider.GetRequiredService<IHeavyHorseFactory>();
            return DoFactoryMethodCall(cTarget, FactoryOperation.Update, () => cTarget.Update(pasture, lightHorsePortal, heavyHorsePortal));
        }

        public virtual IHorseList Save(IHorseList target, Dal.Ef.Cart cart)
        {
            return LocalSave(target, cart);
        }

        public virtual IHorseList LocalSave(IHorseList target, Dal.Ef.Cart cart)
        {
            if (target.IsDeleted)
            {
                throw new NotImplementedException();
            }
            else if (target.IsNew)
            {
                throw new NotImplementedException();
            }
            else
            {
                return LocalUpdate(target, cart);
            }
        }

        public virtual IHorseList Save(IHorseList target, Dal.Ef.Pasture pasture)
        {
            return LocalSave1(target, pasture);
        }

        public virtual IHorseList LocalSave1(IHorseList target, Dal.Ef.Pasture pasture)
        {
            if (target.IsDeleted)
            {
                throw new NotImplementedException();
            }
            else if (target.IsNew)
            {
                throw new NotImplementedException();
            }
            else
            {
                return LocalUpdate1(target, pasture);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddScoped<HorseListFactory>();
            services.AddScoped<IHorseListFactory, HorseListFactory>();
            services.AddTransient<HorseList>();
            services.AddTransient<IHorseList, HorseList>();
        }
    }
}