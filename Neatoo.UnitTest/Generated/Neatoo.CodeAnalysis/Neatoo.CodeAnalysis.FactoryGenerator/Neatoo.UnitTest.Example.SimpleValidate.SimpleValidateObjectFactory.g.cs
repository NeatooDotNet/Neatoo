using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Core;
using System;

/*
                    Debugging Messages:
                    : ValidateBase<SimpleValidateObject>, ISimpleValidateObject
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
namespace Neatoo.UnitTest.Example.SimpleValidate
{
    public interface ISimpleValidateObjectFactory
    {
    }

    internal class SimpleValidateObjectFactory : FactoryBase, ISimpleValidateObjectFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public SimpleValidateObjectFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public SimpleValidateObjectFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<SimpleValidateObject>();
            services.AddScoped<SimpleValidateObjectFactory>();
            services.AddScoped<ISimpleValidateObjectFactory, SimpleValidateObjectFactory>();
            services.AddTransient<ISimpleValidateObject, SimpleValidateObject>();
        }
    }
}