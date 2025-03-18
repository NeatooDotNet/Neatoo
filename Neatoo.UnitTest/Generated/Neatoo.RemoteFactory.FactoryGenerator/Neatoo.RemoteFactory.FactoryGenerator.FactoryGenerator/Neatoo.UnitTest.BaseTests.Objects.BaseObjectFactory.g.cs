#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;

/*
Interface Found. TargetType: IBaseObject ConcreteType: BaseObject
Class: Neatoo.UnitTest.BaseTests.Objects.BaseObject Name: BaseObject
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
namespace Neatoo.UnitTest.BaseTests.Objects
{
    public interface IBaseObjectFactory
    {
    }

    internal class BaseObjectFactory : FactoryBase<IBaseObject>, IBaseObjectFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public BaseObjectFactory(IServiceProvider serviceProvider, IFactoryCore<IBaseObject> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public BaseObjectFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IBaseObject> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddScoped<BaseObjectFactory>();
            services.AddScoped<IBaseObjectFactory, BaseObjectFactory>();
        }
    }
}