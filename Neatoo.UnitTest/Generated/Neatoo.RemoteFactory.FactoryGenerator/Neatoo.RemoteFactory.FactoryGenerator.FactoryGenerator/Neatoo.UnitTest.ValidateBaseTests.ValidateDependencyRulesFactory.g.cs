#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Objects;
using Neatoo.UnitTest.PersonObjects;

/*
Interface Found. TargetType: IValidateDependencyRules ConcreteType: ValidateDependencyRules
Class: Neatoo.UnitTest.ValidateBaseTests.ValidateDependencyRules Name: ValidateDependencyRules
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
namespace Neatoo.UnitTest.ValidateBaseTests
{
    public interface IValidateDependencyRulesFactory
    {
    }

    internal class ValidateDependencyRulesFactory : FactoryBase<IValidateDependencyRules>, IValidateDependencyRulesFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public ValidateDependencyRulesFactory(IServiceProvider serviceProvider, IFactoryCore<IValidateDependencyRules> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public ValidateDependencyRulesFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IValidateDependencyRules> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddScoped<ValidateDependencyRulesFactory>();
            services.AddScoped<IValidateDependencyRulesFactory, ValidateDependencyRulesFactory>();
        }
    }
}