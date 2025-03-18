using Microsoft.Extensions.DependencyInjection;
using Neatoo.Portal.Internal;
using Neatoo;
using Neatoo.Portal;
using HorseBarn.lib.Cart;
using HorseBarn.lib.Horse;
using Microsoft.EntityFrameworkCore;
using HorseBarn.Dal.Ef;
using System.ComponentModel;
using System.Diagnostics;

/*
                    Debugging Messages:
                    : CustomEditBase<HorseBarn>, IHorseBarn
: EditBase<T>
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
No MethodDeclarationSyntax for GetProperty
No MethodDeclarationSyntax for PauseAllActions
No MethodDeclarationSyntax for get_Item
No MethodDeclarationSyntax for get_RuleManager
No MethodDeclarationSyntax for get_MetaState
No MethodDeclarationSyntax for ChildNeatooPropertyChanged
No MethodDeclarationSyntax for get_ObjectInvalid
No MethodDeclarationSyntax for set_ObjectInvalid
No MethodDeclarationSyntax for get_IsPaused
No MethodDeclarationSyntax for set_IsPaused
No MethodDeclarationSyntax for RunSelfRules
No MethodDeclarationSyntax for RunAllRules
No MethodDeclarationSyntax for get_AsyncTaskSequencer
No MethodDeclarationSyntax for get_PropertyManager
No MethodDeclarationSyntax for set_PropertyManager
No MethodDeclarationSyntax for get_Parent
No MethodDeclarationSyntax for set_Parent
No MethodDeclarationSyntax for SetParent
No MethodDeclarationSyntax for Neatoo.Core.ISetParent.SetParent
No MethodDeclarationSyntax for Getter
No MethodDeclarationSyntax for Setter
No MethodDeclarationSyntax for WaitForTasks
No MethodDeclarationSyntax for add_PropertyChanged
No MethodDeclarationSyntax for remove_PropertyChanged
No MethodDeclarationSyntax for add_NeatooPropertyChanged
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
        Task<IHorseBarn?> Save(IHorseBarn target);
    }

    internal class HorseBarnFactory : FactoryEditBase<HorseBarn>, IFactoryEditBase<HorseBarn>, IHorseBarnFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        public delegate Task<IHorseBarn> FetchDelegate();
        public delegate Task<IHorseBarn?> SaveDelegate(IHorseBarn target);
        // Delegate Properties to provide Local or Remote fork in execution
        public FetchDelegate FetchProperty { get; }
        public SaveDelegate SaveProperty { get; }

        public HorseBarnFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            FetchProperty = LocalFetch;
            SaveProperty = LocalSave;
        }

        public HorseBarnFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
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
            var pasturePortal = ServiceProvider.GetService<IPastureFactory>();
            var cartListPortal = ServiceProvider.GetService<ICartListFactory>();
            return DoMapperMethodCall<IHorseBarn>(target, DataMapperMethod.Create, () => target.Create(pasturePortal, cartListPortal));
        }

        public virtual Task<IHorseBarn> Fetch()
        {
            return FetchProperty();
        }

        public virtual async Task<IHorseBarn> RemoteFetch()
        {
            return await DoRemoteRequest.ForDelegate<IHorseBarn>(typeof(FetchDelegate), []);
        }

        public Task<IHorseBarn> LocalFetch()
        {
            var target = ServiceProvider.GetRequiredService<HorseBarn>();
            var horseBarnContext = ServiceProvider.GetService<IHorseBarnContext>();
            var pasturePortal = ServiceProvider.GetService<IPastureFactory>();
            var cartPortal = ServiceProvider.GetService<ICartListFactory>();
            return DoMapperMethodCallAsync<IHorseBarn>(target, DataMapperMethod.Fetch, () => target.Fetch(horseBarnContext, pasturePortal, cartPortal));
        }

        public Task<IHorseBarn> LocalInsert(IHorseBarn target)
        {
            var cTarget = (HorseBarn)target ?? throw new Exception("IHorseBarn must implement HorseBarn");
            var horseBarnContext = ServiceProvider.GetService<IHorseBarnContext>();
            var pasturePortal = ServiceProvider.GetService<IPastureFactory>();
            var cartPortal = ServiceProvider.GetService<ICartListFactory>();
            return DoMapperMethodCallAsync<IHorseBarn>(cTarget, DataMapperMethod.Insert, () => cTarget.Insert(horseBarnContext, pasturePortal, cartPortal));
        }

        public Task<IHorseBarn> LocalUpdate(IHorseBarn target)
        {
            var cTarget = (HorseBarn)target ?? throw new Exception("IHorseBarn must implement HorseBarn");
            var horseBarnContext = ServiceProvider.GetService<IHorseBarnContext>();
            var pasturePortal = ServiceProvider.GetService<IPastureFactory>();
            var cartPortal = ServiceProvider.GetService<ICartListFactory>();
            return DoMapperMethodCallAsync<IHorseBarn>(cTarget, DataMapperMethod.Update, () => cTarget.Update(horseBarnContext, pasturePortal, cartPortal));
        }

        public virtual Task<IHorseBarn?> Save(IHorseBarn target)
        {
            return SaveProperty(target);
        }

        public virtual async Task<IHorseBarn?> RemoteSave(IHorseBarn target)
        {
            return await DoRemoteRequest.ForDelegate<IHorseBarn?>(typeof(SaveDelegate), [target]);
        }

        async Task<IEditBase?> IFactoryEditBase<HorseBarn>.Save(HorseBarn target)
        {
            return (IEditBase? )await Save(target);
        }

        public virtual Task<IHorseBarn?> LocalSave(IHorseBarn target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                throw new NotImplementedException();
            }
            else if (target.IsNew)
            {
                return LocalInsert(target);
            }
            else
            {
                return LocalUpdate(target);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<HorseBarn>();
            services.AddScoped<HorseBarnFactory>();
            services.AddScoped<IHorseBarnFactory, HorseBarnFactory>();
            services.AddTransient<IHorseBarn, HorseBarn>();
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
            services.AddScoped<IFactoryEditBase<HorseBarn>, HorseBarnFactory>();
        }
    }
}