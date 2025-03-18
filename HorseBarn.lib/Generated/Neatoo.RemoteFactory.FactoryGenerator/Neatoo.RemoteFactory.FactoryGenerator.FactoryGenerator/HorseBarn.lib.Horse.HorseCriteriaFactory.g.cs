#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.AuthorizationRules;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

/*
Interface Found. TargetType: IHorseCriteria ConcreteType: HorseCriteria
Class: HorseBarn.lib.Horse.HorseCriteria Name: HorseCriteria
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
    public interface IHorseCriteriaFactory
    {
        IHorseCriteria Fetch();
        IHorseCriteria Fetch(IEnumerable<string> horseNames);
    }

    internal class HorseCriteriaFactory : FactoryBase<IHorseCriteria>, IHorseCriteriaFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public HorseCriteriaFactory(IServiceProvider serviceProvider, IFactoryCore<IHorseCriteria> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public HorseCriteriaFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IHorseCriteria> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public virtual IHorseCriteria Fetch()
        {
            return LocalFetch();
        }

        public IHorseCriteria LocalFetch()
        {
            var target = ServiceProvider.GetRequiredService<HorseCriteria>();
            return DoFactoryMethodCall(target, FactoryOperation.Fetch, () => target.Fetch());
        }

        public virtual IHorseCriteria Fetch(IEnumerable<string> horseNames)
        {
            return LocalFetch1(horseNames);
        }

        public IHorseCriteria LocalFetch1(IEnumerable<string> horseNames)
        {
            var target = ServiceProvider.GetRequiredService<HorseCriteria>();
            return DoFactoryMethodCall(target, FactoryOperation.Fetch, () => target.Fetch(horseNames));
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddScoped<HorseCriteriaFactory>();
            services.AddScoped<IHorseCriteriaFactory, HorseCriteriaFactory>();
            services.AddTransient<HorseCriteria>();
            services.AddTransient<IHorseCriteria, HorseCriteria>();
        }
    }
}