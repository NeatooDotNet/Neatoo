using Microsoft.Extensions.DependencyInjection;
using Neatoo.Portal.Internal;
using Neatoo;
using Neatoo.Portal;
using HorseBarn.lib.Cart;
using System.Collections.Specialized;

/*
                    Debugging Messages:
                    : EditListBase<HorseList, IHorse>, IHorseList
No MethodDeclarationSyntax for get_DeletedList
No MethodDeclarationSyntax for get_EditMetaState
No MethodDeclarationSyntax for InsertItem
No MethodDeclarationSyntax for get_IsPaused
No MethodDeclarationSyntax for set_IsPaused
No MethodDeclarationSyntax for get_MetaState
No MethodDeclarationSyntax for HandleNeatooPropertyChanged
No MethodDeclarationSyntax for RunAllRules
No MethodDeclarationSyntax for RunSelfRules
No MethodDeclarationSyntax for PauseAllActions
No MethodDeclarationSyntax for Neatoo.Portal.Internal.IDataMapperTarget.PauseAllActions
No MethodDeclarationSyntax for get_Parent
No MethodDeclarationSyntax for set_Parent
No MethodDeclarationSyntax for add_NeatooPropertyChanged
No MethodDeclarationSyntax for remove_NeatooPropertyChanged
No MethodDeclarationSyntax for Neatoo.Core.ISetParent.SetParent
No MethodDeclarationSyntax for InsertItem
No MethodDeclarationSyntax for Neatoo.Portal.Internal.IDataMapperTarget.PauseAllActions
No MethodDeclarationSyntax for Neatoo.Portal.Internal.IDataMapperTarget.PostPortalConstruct
No MethodDeclarationSyntax for PostPortalConstruct
No MethodDeclarationSyntax for RaiseNeatooPropertyChanged
No MethodDeclarationSyntax for HandleNeatooPropertyChanged
No MethodDeclarationSyntax for HandlePropertyChanged
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
namespace HorseBarn.lib.Horse
{
    public interface IHorseListFactory
    {
        IHorseList Create();
    }

    internal class HorseListFactory : FactoryBase, IHorseListFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public HorseListFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public HorseListFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
        }

        public virtual IHorseList Create()
        {
            return LocalCreate();
        }

        public IHorseList LocalCreate()
        {
            var target = ServiceProvider.GetRequiredService<HorseList>();
            return DoMapperMethodCall<IHorseList>(target, DataMapperMethod.Create, () => target.Create());
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<HorseList>();
            services.AddScoped<HorseListFactory>();
            services.AddScoped<IHorseListFactory, HorseListFactory>();
            services.AddTransient<IHorseList, HorseList>();
        }
    }
}