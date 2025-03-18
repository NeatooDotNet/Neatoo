#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using HorseBarn.Dal.Ef;
using HorseBarn.lib.Horse;
using Neatoo;

/*
Interface Found. TargetType: ICartList ConcreteType: CartList
Class: HorseBarn.lib.Cart.CartList Name: CartList
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
namespace HorseBarn.lib.Cart
{
    public interface ICartListFactory
    {
        ICartList Create();
        ICartList Fetch(ICollection<Dal.Ef.Cart> carts);
        ICartList Save(ICartList target, Dal.Ef.HorseBarn horseBarn);
    }

    internal class CartListFactory : FactoryBase<ICartList>, ICartListFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public CartListFactory(IServiceProvider serviceProvider, IFactoryCore<ICartList> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public CartListFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<ICartList> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public virtual ICartList Create()
        {
            return LocalCreate();
        }

        public ICartList LocalCreate()
        {
            var target = ServiceProvider.GetRequiredService<CartList>();
            return DoFactoryMethodCall(target, FactoryOperation.Create, () => target.Create());
        }

        public virtual ICartList Fetch(ICollection<Dal.Ef.Cart> carts)
        {
            return LocalFetch(carts);
        }

        public ICartList LocalFetch(ICollection<Dal.Ef.Cart> carts)
        {
            var target = ServiceProvider.GetRequiredService<CartList>();
            var racingChariotPortal = ServiceProvider.GetRequiredService<RacingChariotFactory>();
            var wagonPortal = ServiceProvider.GetRequiredService<WagonFactory>();
            return DoFactoryMethodCall(target, FactoryOperation.Fetch, () => target.Fetch(carts, racingChariotPortal, wagonPortal));
        }

        public ICartList LocalUpdate(ICartList target, Dal.Ef.HorseBarn horseBarn)
        {
            var cTarget = (CartList)target ?? throw new Exception("ICartList must implement CartList");
            var racingChariotPortal = ServiceProvider.GetRequiredService<RacingChariotFactory>();
            var wagonPortal = ServiceProvider.GetRequiredService<WagonFactory>();
            return DoFactoryMethodCall(cTarget, FactoryOperation.Update, () => cTarget.Update(horseBarn, racingChariotPortal, wagonPortal));
        }

        public virtual ICartList Save(ICartList target, Dal.Ef.HorseBarn horseBarn)
        {
            return LocalSave(target, horseBarn);
        }

        public virtual ICartList LocalSave(ICartList target, Dal.Ef.HorseBarn horseBarn)
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
                return LocalUpdate(target, horseBarn);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddScoped<CartListFactory>();
            services.AddScoped<ICartListFactory, CartListFactory>();
            services.AddTransient<CartList>();
            services.AddTransient<ICartList, CartList>();
        }
    }
}