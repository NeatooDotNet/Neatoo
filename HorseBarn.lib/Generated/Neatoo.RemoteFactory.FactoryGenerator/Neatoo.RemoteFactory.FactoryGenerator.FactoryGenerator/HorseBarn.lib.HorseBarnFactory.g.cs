#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using HorseBarn.lib.Cart;
using HorseBarn.lib.Horse;
using Neatoo;
using Microsoft.EntityFrameworkCore;
using HorseBarn.Dal.Ef;
using System.ComponentModel;
using System.Diagnostics;

/*
Interface Found. TargetType: IHorseBarn ConcreteType: HorseBarn
Class: HorseBarn.lib.HorseBarn Name: HorseBarn
Ignoring [Fetch] method with attribute [Remote]. Not a Factory or Authorize attribute.
No Factory or Authorize attribute for Fetch attribute RemoteAttribute
Ignoring [Insert] method with attribute [Remote]. Not a Factory or Authorize attribute.
No Factory or Authorize attribute for Insert attribute RemoteAttribute
Ignoring [Update] method with attribute [Remote]. Not a Factory or Authorize attribute.
No Factory or Authorize attribute for Update attribute RemoteAttribute
No MethodDeclarationSyntax for get_PropertyManager
No MethodDeclarationSyntax for .ctor
No MethodDeclarationSyntax for get_Factory
No MethodDeclarationSyntax for set_Factory
No MethodDeclarationSyntax for get_IsMarkedModified
No MethodDeclarationSyntax for set_IsMarkedModified
No MethodDeclarationSyntax for get_IsNew
No MethodDeclarationSyntax for set_IsNew
No MethodDeclarationSyntax for get_IsDeleted
No MethodDeclarationSyntax for set_IsDeleted
No MethodDeclarationSyntax for get_ModifiedProperties
No MethodDeclarationSyntax for get_IsChild
No MethodDeclarationSyntax for set_IsChild
No MethodDeclarationSyntax for get_EditMetaState
No MethodDeclarationSyntax for ChildNeatooPropertyChanged
No MethodDeclarationSyntax for Save
No MethodDeclarationSyntax for Save
No MethodDeclarationSyntax for Save
No MethodDeclarationSyntax for GetProperty
No MethodDeclarationSyntax for get_Item
No MethodDeclarationSyntax for get_RuleManager
No MethodDeclarationSyntax for get_MetaState
No MethodDeclarationSyntax for get_MetaState
No MethodDeclarationSyntax for ChildNeatooPropertyChanged
No MethodDeclarationSyntax for ChildNeatooPropertyChanged
No MethodDeclarationSyntax for get_ObjectInvalid
No MethodDeclarationSyntax for set_ObjectInvalid
No MethodDeclarationSyntax for get_IsPaused
No MethodDeclarationSyntax for set_IsPaused
No MethodDeclarationSyntax for RunSelfRules
No MethodDeclarationSyntax for RunSelfRules
No MethodDeclarationSyntax for RunAllRules
No MethodDeclarationSyntax for RunAllRules
No MethodDeclarationSyntax for get_AsyncTaskSequencer
No MethodDeclarationSyntax for get_PropertyManager
No MethodDeclarationSyntax for set_PropertyManager
No MethodDeclarationSyntax for get_Parent
No MethodDeclarationSyntax for get_Parent
No MethodDeclarationSyntax for set_Parent
No MethodDeclarationSyntax for set_Parent
No MethodDeclarationSyntax for SetParent
No MethodDeclarationSyntax for Neatoo.Core.ISetParent.SetParent
No MethodDeclarationSyntax for Getter
No MethodDeclarationSyntax for Setter
No MethodDeclarationSyntax for WaitForTasks
No MethodDeclarationSyntax for WaitForTasks
No MethodDeclarationSyntax for add_PropertyChanged
No MethodDeclarationSyntax for add_PropertyChanged
No MethodDeclarationSyntax for remove_PropertyChanged
No MethodDeclarationSyntax for remove_PropertyChanged
No MethodDeclarationSyntax for add_NeatooPropertyChanged
No MethodDeclarationSyntax for add_NeatooPropertyChanged
No MethodDeclarationSyntax for remove_NeatooPropertyChanged
No MethodDeclarationSyntax for remove_NeatooPropertyChanged
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute

*/
namespace HorseBarn.lib
{
    public interface IHorseBarnFactory
    {
        IHorseBarn Create();
        Task<IHorseBarn> Fetch();
        Task<IHorseBarn> Save(IHorseBarn target);
    }

    internal class HorseBarnFactory : FactorySaveBase<IHorseBarn>, IFactorySave<HorseBarn>, IHorseBarnFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        public delegate Task<IHorseBarn> FetchDelegate();
        public delegate Task<IHorseBarn> SaveDelegate(IHorseBarn target);
        // Delegate Properties to provide Local or Remote fork in execution
        public FetchDelegate FetchProperty { get; }
        public SaveDelegate SaveProperty { get; }

        public HorseBarnFactory(IServiceProvider serviceProvider, IFactoryCore<IHorseBarn> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            FetchProperty = LocalFetch;
            SaveProperty = LocalSave;
        }

        public HorseBarnFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IHorseBarn> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
            FetchProperty = RemoteFetch;
            SaveProperty = RemoteSave;
        }

        public virtual IHorseBarn Create()
        {
            return LocalCreate();
        }

        public IHorseBarn LocalCreate()
        {
            var target = ServiceProvider.GetRequiredService<HorseBarn>();
            var pasturePortal = ServiceProvider.GetRequiredService<IPastureFactory>();
            var cartListPortal = ServiceProvider.GetRequiredService<ICartListFactory>();
            return DoFactoryMethodCall(target, FactoryOperation.Create, () => target.Create(pasturePortal, cartListPortal));
        }

        public virtual Task<IHorseBarn> Fetch()
        {
            return FetchProperty();
        }

        public virtual async Task<IHorseBarn> RemoteFetch()
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<IHorseBarn>(typeof(FetchDelegate), []))!;
        }

        public Task<IHorseBarn> LocalFetch()
        {
            var target = ServiceProvider.GetRequiredService<HorseBarn>();
            var horseBarnContext = ServiceProvider.GetRequiredService<IHorseBarnContext>();
            var pasturePortal = ServiceProvider.GetRequiredService<IPastureFactory>();
            var cartPortal = ServiceProvider.GetRequiredService<ICartListFactory>();
            return DoFactoryMethodCallAsync(target, FactoryOperation.Fetch, () => target.Fetch(horseBarnContext, pasturePortal, cartPortal));
        }

        public Task<IHorseBarn> LocalInsert(IHorseBarn target)
        {
            var cTarget = (HorseBarn)target ?? throw new Exception("IHorseBarn must implement HorseBarn");
            var horseBarnContext = ServiceProvider.GetRequiredService<IHorseBarnContext>();
            var pasturePortal = ServiceProvider.GetRequiredService<IPastureFactory>();
            var cartPortal = ServiceProvider.GetRequiredService<ICartListFactory>();
            return DoFactoryMethodCallAsync(cTarget, FactoryOperation.Insert, () => cTarget.Insert(horseBarnContext, pasturePortal, cartPortal));
        }

        public Task<IHorseBarn> LocalUpdate(IHorseBarn target)
        {
            var cTarget = (HorseBarn)target ?? throw new Exception("IHorseBarn must implement HorseBarn");
            var horseBarnContext = ServiceProvider.GetRequiredService<IHorseBarnContext>();
            var pasturePortal = ServiceProvider.GetRequiredService<IPastureFactory>();
            var cartPortal = ServiceProvider.GetRequiredService<ICartListFactory>();
            return DoFactoryMethodCallAsync(cTarget, FactoryOperation.Update, () => cTarget.Update(horseBarnContext, pasturePortal, cartPortal));
        }

        public virtual Task<IHorseBarn> Save(IHorseBarn target)
        {
            return SaveProperty(target);
        }

        public virtual async Task<IHorseBarn> RemoteSave(IHorseBarn target)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<IHorseBarn>(typeof(SaveDelegate), [target]))!;
        }

        async Task<IFactorySaveMeta?> IFactorySave<HorseBarn>.Save(HorseBarn target)
        {
            return (IFactorySaveMeta? )await Save(target);
        }

        public virtual async Task<IHorseBarn> LocalSave(IHorseBarn target)
        {
            if (target.IsDeleted)
            {
                throw new NotImplementedException();
            }
            else if (target.IsNew)
            {
                return await LocalInsert(target);
            }
            else
            {
                return await LocalUpdate(target);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddScoped<HorseBarnFactory>();
            services.AddScoped<IHorseBarnFactory, HorseBarnFactory>();
            services.AddScoped<FetchDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<HorseBarnFactory>();
                return () => factory.LocalFetch();
            });
            services.AddScoped<SaveDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<HorseBarnFactory>();
                return (IHorseBarn target) => factory.LocalSave(target);
            });
            services.AddTransient<HorseBarn>();
            services.AddTransient<IHorseBarn, HorseBarn>();
            services.AddScoped<IFactorySave<HorseBarn>, HorseBarnFactory>();
        }
    }
}