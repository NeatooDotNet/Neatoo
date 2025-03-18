#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.Core;
using Neatoo.UnitTest.PersonObjects;

/*
Interface Found. TargetType: IValidateAsyncObject ConcreteType: ValidateAsyncObject
Class: Neatoo.UnitTest.ValidateBaseTests.ValidateAsyncObject Name: ValidateAsyncObject
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
    public interface IValidateAsyncObjectFactory
    {
        Task<IValidateAsyncObject> Fetch(PersonDto person);
    }

    internal class ValidateAsyncObjectFactory : FactoryBase<IValidateAsyncObject>, IValidateAsyncObjectFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public ValidateAsyncObjectFactory(IServiceProvider serviceProvider, IFactoryCore<IValidateAsyncObject> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public ValidateAsyncObjectFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IValidateAsyncObject> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public virtual Task<IValidateAsyncObject> Fetch(PersonDto person)
        {
            return LocalFetch(person);
        }

        public Task<IValidateAsyncObject> LocalFetch(PersonDto person)
        {
            var target = ServiceProvider.GetRequiredService<ValidateAsyncObject>();
            var portal = ServiceProvider.GetRequiredService<ValidateAsyncObjectFactory>();
            var personTable = ServiceProvider.GetRequiredService<IReadOnlyList<PersonDto>>();
            return DoFactoryMethodCallAsync(target, FactoryOperation.Fetch, () => target.Fetch(person, portal, personTable));
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddScoped<ValidateAsyncObjectFactory>();
            services.AddScoped<IValidateAsyncObjectFactory, ValidateAsyncObjectFactory>();
            services.AddTransient<ValidateAsyncObject>();
            services.AddTransient<IValidateAsyncObject, ValidateAsyncObject>();
        }
    }
}