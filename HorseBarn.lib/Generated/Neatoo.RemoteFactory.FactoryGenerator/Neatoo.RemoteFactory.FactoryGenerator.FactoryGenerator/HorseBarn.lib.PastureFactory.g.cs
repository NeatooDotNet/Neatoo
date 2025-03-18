#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using HorseBarn.lib.Horse;
using Neatoo;
using System.Diagnostics;
using HorseBarn.Dal.Ef;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

/*
Interface Found. TargetType: IPasture ConcreteType: Pasture
Class: HorseBarn.lib.Pasture Name: Pasture
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
    public interface IPastureFactory
    {
        IPasture Create();
        IPasture Fetch(Dal.Ef.Pasture pasture);
        IPasture Save(IPasture target, Dal.Ef.HorseBarn horseBarn);
    }

    internal class PastureFactory : FactoryBase<IPasture>, IPastureFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public PastureFactory(IServiceProvider serviceProvider, IFactoryCore<IPasture> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public PastureFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IPasture> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public virtual IPasture Create()
        {
            return LocalCreate();
        }

        public IPasture LocalCreate()
        {
            var target = ServiceProvider.GetRequiredService<Pasture>();
            var horseListPortal = ServiceProvider.GetRequiredService<IHorseListFactory>();
            return DoFactoryMethodCall(target, FactoryOperation.Create, () => target.Create(horseListPortal));
        }

        public virtual IPasture Fetch(Dal.Ef.Pasture pasture)
        {
            return LocalFetch(pasture);
        }

        public IPasture LocalFetch(Dal.Ef.Pasture pasture)
        {
            var target = ServiceProvider.GetRequiredService<Pasture>();
            var horseListPortal = ServiceProvider.GetRequiredService<IHorseListFactory>();
            return DoFactoryMethodCall(target, FactoryOperation.Fetch, () => target.Fetch(pasture, horseListPortal));
        }

        public IPasture LocalInsert(IPasture target, Dal.Ef.HorseBarn horseBarn)
        {
            var cTarget = (Pasture)target ?? throw new Exception("IPasture must implement Pasture");
            var horseListPortal = ServiceProvider.GetRequiredService<IHorseListFactory>();
            return DoFactoryMethodCall(cTarget, FactoryOperation.Insert, () => cTarget.Insert(horseBarn, horseListPortal));
        }

        public IPasture LocalUpdate(IPasture target, Dal.Ef.HorseBarn horseBarn)
        {
            var cTarget = (Pasture)target ?? throw new Exception("IPasture must implement Pasture");
            var horseListPortal = ServiceProvider.GetRequiredService<IHorseListFactory>();
            return DoFactoryMethodCall(cTarget, FactoryOperation.Update, () => cTarget.Update(horseBarn, horseListPortal));
        }

        public virtual IPasture Save(IPasture target, Dal.Ef.HorseBarn horseBarn)
        {
            return LocalSave(target, horseBarn);
        }

        public virtual IPasture LocalSave(IPasture target, Dal.Ef.HorseBarn horseBarn)
        {
            if (target.IsDeleted)
            {
                throw new NotImplementedException();
            }
            else if (target.IsNew)
            {
                return LocalInsert(target, horseBarn);
            }
            else
            {
                return LocalUpdate(target, horseBarn);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddScoped<PastureFactory>();
            services.AddScoped<IPastureFactory, PastureFactory>();
            services.AddTransient<Pasture>();
            services.AddTransient<IPasture, Pasture>();
        }
    }
}