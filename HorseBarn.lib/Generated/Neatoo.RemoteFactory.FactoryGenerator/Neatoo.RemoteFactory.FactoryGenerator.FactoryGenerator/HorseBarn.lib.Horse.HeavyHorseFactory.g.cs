#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using System.ComponentModel.DataAnnotations;
using HorseBarn.Dal.Ef;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Diagnostics;

/*
Interface Found. TargetType: IHeavyHorse ConcreteType: HeavyHorse
Class: HorseBarn.lib.Horse.HeavyHorse Name: HeavyHorse
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
namespace HorseBarn.lib.Horse
{
    public interface IHeavyHorseFactory
    {
        IHeavyHorse Create(IHorseCriteria horseCriteria);
        IHeavyHorse Fetch(Dal.Ef.Horse horse);
        Task<IHeavyHorse?> Save(IHeavyHorse target, Dal.Ef.Pasture pasture);
        Task<IHeavyHorse?> Save(IHeavyHorse target, Dal.Ef.Cart cart);
    }

    internal class HeavyHorseFactory : FactoryBase<IHeavyHorse>, IHeavyHorseFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public HeavyHorseFactory(IServiceProvider serviceProvider, IFactoryCore<IHeavyHorse> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public HeavyHorseFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IHeavyHorse> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public virtual IHeavyHorse Create(IHorseCriteria horseCriteria)
        {
            return LocalCreate(horseCriteria);
        }

        public IHeavyHorse LocalCreate(IHorseCriteria horseCriteria)
        {
            var target = ServiceProvider.GetRequiredService<HeavyHorse>();
            return DoFactoryMethodCall(target, FactoryOperation.Create, () => target.Create(horseCriteria));
        }

        public virtual IHeavyHorse Fetch(Dal.Ef.Horse horse)
        {
            return LocalFetch(horse);
        }

        public IHeavyHorse LocalFetch(Dal.Ef.Horse horse)
        {
            var target = ServiceProvider.GetRequiredService<HeavyHorse>();
            return DoFactoryMethodCall(target, FactoryOperation.Fetch, () => target.Fetch(horse));
        }

        public IHeavyHorse LocalInsert(IHeavyHorse target, Dal.Ef.Pasture pasture)
        {
            var cTarget = (HeavyHorse)target ?? throw new Exception("IHeavyHorse must implement HeavyHorse");
            return DoFactoryMethodCall(cTarget, FactoryOperation.Insert, () => cTarget.Insert(pasture));
        }

        public IHeavyHorse LocalInsert1(IHeavyHorse target, Dal.Ef.Cart cart)
        {
            var cTarget = (HeavyHorse)target ?? throw new Exception("IHeavyHorse must implement HeavyHorse");
            return DoFactoryMethodCall(cTarget, FactoryOperation.Insert, () => cTarget.Insert(cart));
        }

        public Task<IHeavyHorse> LocalUpdate(IHeavyHorse target, Dal.Ef.Pasture pasture)
        {
            var cTarget = (HeavyHorse)target ?? throw new Exception("IHeavyHorse must implement HeavyHorse");
            var horseBarnContext = ServiceProvider.GetRequiredService<IHorseBarnContext>();
            return DoFactoryMethodCallAsync(cTarget, FactoryOperation.Update, () => cTarget.Update(pasture, horseBarnContext));
        }

        public Task<IHeavyHorse> LocalUpdate1(IHeavyHorse target, Dal.Ef.Cart cart)
        {
            var cTarget = (HeavyHorse)target ?? throw new Exception("IHeavyHorse must implement HeavyHorse");
            var horseBarnContext = ServiceProvider.GetRequiredService<IHorseBarnContext>();
            return DoFactoryMethodCallAsync(cTarget, FactoryOperation.Update, () => cTarget.Update(cart, horseBarnContext));
        }

        public IHeavyHorse LocalDelete(IHeavyHorse target, Dal.Ef.Cart cart)
        {
            var cTarget = (HeavyHorse)target ?? throw new Exception("IHeavyHorse must implement HeavyHorse");
            return DoFactoryMethodCall(cTarget, FactoryOperation.Delete, () => cTarget.Delete(cart));
        }

        public IHeavyHorse LocalDelete1(IHeavyHorse target, Dal.Ef.Pasture pasture)
        {
            var cTarget = (HeavyHorse)target ?? throw new Exception("IHeavyHorse must implement HeavyHorse");
            return DoFactoryMethodCall(cTarget, FactoryOperation.Delete, () => cTarget.Delete(pasture));
        }

        public virtual Task<IHeavyHorse?> Save(IHeavyHorse target, Dal.Ef.Pasture pasture)
        {
            return LocalSave(target, pasture);
        }

        public virtual async Task<IHeavyHorse?> LocalSave(IHeavyHorse target, Dal.Ef.Pasture pasture)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(IHeavyHorse);
                }

                return LocalDelete1(target, pasture);
            }
            else if (target.IsNew)
            {
                return LocalInsert(target, pasture);
            }
            else
            {
                return await LocalUpdate(target, pasture);
            }
        }

        public virtual Task<IHeavyHorse?> Save(IHeavyHorse target, Dal.Ef.Cart cart)
        {
            return LocalSave1(target, cart);
        }

        public virtual async Task<IHeavyHorse?> LocalSave1(IHeavyHorse target, Dal.Ef.Cart cart)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(IHeavyHorse);
                }

                return LocalDelete(target, cart);
            }
            else if (target.IsNew)
            {
                return LocalInsert1(target, cart);
            }
            else
            {
                return await LocalUpdate1(target, cart);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddScoped<HeavyHorseFactory>();
            services.AddScoped<IHeavyHorseFactory, HeavyHorseFactory>();
            services.AddTransient<HeavyHorse>();
            services.AddTransient<IHeavyHorse, HeavyHorse>();
        }
    }
}