using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.PersonObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/*
                    Debugging Messages:
                    : EditListBase<EditPersonList, IEditPerson>, IEditPersonList
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
No MethodDeclarationSyntax for Neatoo.RemoteFactory.Internal.IDataMapperTarget.PauseAllActions
No MethodDeclarationSyntax for get_Parent
No MethodDeclarationSyntax for set_Parent
No MethodDeclarationSyntax for add_NeatooPropertyChanged
No MethodDeclarationSyntax for remove_NeatooPropertyChanged
No MethodDeclarationSyntax for Neatoo.Core.ISetParent.SetParent
No MethodDeclarationSyntax for InsertItem
No MethodDeclarationSyntax for Neatoo.RemoteFactory.Internal.IDataMapperTarget.PauseAllActions
No MethodDeclarationSyntax for Neatoo.RemoteFactory.Internal.IDataMapperTarget.PostPortalConstruct
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
namespace Neatoo.UnitTest.EditBaseTests
{
    public interface IEditPersonListFactory
    {
    }

    internal class EditPersonListFactory : FactoryBase, IEditPersonListFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public EditPersonListFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public EditPersonListFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<EditPersonList>();
            services.AddScoped<EditPersonListFactory>();
            services.AddScoped<IEditPersonListFactory, EditPersonListFactory>();
            services.AddTransient<IEditPersonList, EditPersonList>();
        }
    }
}