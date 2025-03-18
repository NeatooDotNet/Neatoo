using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.PersonObjects;
using System.Collections.Generic;
using System;

/*
                    Debugging Messages:
                    : PersonEditBase<EditPerson>, IEditPerson
: EditBase<T>, IPersonBase
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
namespace Neatoo.UnitTest.EditBaseTests
{
    public interface IEditPersonFactory
    {
        IEditPerson FillFromDto(PersonDto dto);
        IEditPerson? Save(IEditPerson target);
    }

    internal class EditPersonFactory : FactoryEditBase<EditPerson>, IFactoryEditBase<EditPerson>, IEditPersonFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public EditPersonFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public EditPersonFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
        }

        public IEditPerson LocalInsert(IEditPerson target)
        {
            var cTarget = (EditPerson)target ?? throw new Exception("IEditPerson must implement EditPerson");
            return DoMapperMethodCall<IEditPerson>(cTarget, DataMapperMethod.Insert, () => cTarget.Insert());
        }

        public virtual IEditPerson FillFromDto(PersonDto dto)
        {
            return LocalFillFromDto(dto);
        }

        public IEditPerson LocalFillFromDto(PersonDto dto)
        {
            var target = ServiceProvider.GetRequiredService<EditPerson>();
            return DoMapperMethodCall<IEditPerson>(target, DataMapperMethod.Fetch, () => target.FillFromDto(dto));
        }

        public virtual IEditPerson? Save(IEditPerson target)
        {
            return LocalSave(target);
        }

        async Task<IEditBase?> IFactoryEditBase<EditPerson>.Save(EditPerson target)
        {
            return await Task.FromResult((IEditBase? )Save(target));
        }

        public virtual IEditPerson? LocalSave(IEditPerson target)
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
                throw new NotImplementedException();
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<EditPerson>();
            services.AddScoped<EditPersonFactory>();
            services.AddScoped<IEditPersonFactory, EditPersonFactory>();
            services.AddTransient<IEditPerson, EditPerson>();
            services.AddScoped<IFactoryEditBase<EditPerson>, EditPersonFactory>();
        }
    }
}