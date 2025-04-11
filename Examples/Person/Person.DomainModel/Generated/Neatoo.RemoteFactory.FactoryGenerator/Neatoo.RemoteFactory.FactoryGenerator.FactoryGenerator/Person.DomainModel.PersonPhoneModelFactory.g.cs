#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using Person.Ef;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

/*
Interface Found. TargetType: IPersonPhoneModel ConcreteType: PersonPhoneModel
Class: Person.DomainModel.PersonPhoneModel Name: PersonPhoneModel
No MethodDeclarationSyntax for get_PropertyManager
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
No MethodDeclarationSyntax for PauseAllActions
No MethodDeclarationSyntax for get_RuleManager
No MethodDeclarationSyntax for get_MetaState
No MethodDeclarationSyntax for get_MetaState
No MethodDeclarationSyntax for ChildNeatooPropertyChanged
No MethodDeclarationSyntax for ChildNeatooPropertyChanged
No MethodDeclarationSyntax for get_ObjectInvalid
No MethodDeclarationSyntax for set_ObjectInvalid
No MethodDeclarationSyntax for get_IsPaused
No MethodDeclarationSyntax for set_IsPaused
No MethodDeclarationSyntax for RunRules
No MethodDeclarationSyntax for RunRules
No MethodDeclarationSyntax for get_AsyncTaskSequencer
No MethodDeclarationSyntax for get_PropertyManager
No MethodDeclarationSyntax for set_PropertyManager
No MethodDeclarationSyntax for get_Parent
No MethodDeclarationSyntax for get_Parent
No MethodDeclarationSyntax for set_Parent
No MethodDeclarationSyntax for set_Parent
No MethodDeclarationSyntax for SetParent
No MethodDeclarationSyntax for Neatoo.Internal.ISetParent.SetParent
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
namespace Person.DomainModel
{
    public interface IPersonPhoneModelFactory
    {
        IPersonPhoneModel Create();
        IPersonPhoneModel Fetch(PersonPhoneEntity personPhoneEntity);
        IPersonPhoneModel Save(IPersonPhoneModel target, PersonPhoneEntity personPhoneEntity);
    }

    internal class PersonPhoneModelFactory : FactoryBase<IPersonPhoneModel>, IPersonPhoneModelFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public PersonPhoneModelFactory(IServiceProvider serviceProvider, IFactoryCore<IPersonPhoneModel> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public PersonPhoneModelFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IPersonPhoneModel> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public virtual IPersonPhoneModel Create()
        {
            return LocalCreate();
        }

        public IPersonPhoneModel LocalCreate()
        {
            var uniquePhoneNumberRule = ServiceProvider.GetRequiredService<IUniquePhoneNumberRule>();
            var uniquePhoneTypeRule = ServiceProvider.GetRequiredService<IUniquePhoneTypeRule>();
            var services = ServiceProvider.GetRequiredService<IEditBaseServices<PersonPhoneModel>>();
            return DoFactoryMethodCall(FactoryOperation.Create, () => new PersonPhoneModel(uniquePhoneNumberRule, uniquePhoneTypeRule, services));
        }

        public virtual IPersonPhoneModel Fetch(PersonPhoneEntity personPhoneEntity)
        {
            return LocalFetch(personPhoneEntity);
        }

        public IPersonPhoneModel LocalFetch(PersonPhoneEntity personPhoneEntity)
        {
            var target = ServiceProvider.GetRequiredService<PersonPhoneModel>();
            return DoFactoryMethodCall(target, FactoryOperation.Fetch, () => target.Fetch(personPhoneEntity));
        }

        public IPersonPhoneModel LocalUpdate(IPersonPhoneModel target, PersonPhoneEntity personPhoneEntity)
        {
            var cTarget = (PersonPhoneModel)target ?? throw new Exception("IPersonPhoneModel must implement PersonPhoneModel");
            return DoFactoryMethodCall(cTarget, FactoryOperation.Insert, () => cTarget.Update(personPhoneEntity));
        }

        public IPersonPhoneModel LocalUpdate1(IPersonPhoneModel target, PersonPhoneEntity personPhoneEntity)
        {
            var cTarget = (PersonPhoneModel)target ?? throw new Exception("IPersonPhoneModel must implement PersonPhoneModel");
            return DoFactoryMethodCall(cTarget, FactoryOperation.Update, () => cTarget.Update(personPhoneEntity));
        }

        public virtual IPersonPhoneModel Save(IPersonPhoneModel target, PersonPhoneEntity personPhoneEntity)
        {
            return LocalSave(target, personPhoneEntity);
        }

        public virtual IPersonPhoneModel LocalSave(IPersonPhoneModel target, PersonPhoneEntity personPhoneEntity)
        {
            if (target.IsDeleted)
            {
                throw new NotImplementedException();
            }
            else if (target.IsNew)
            {
                return LocalUpdate(target, personPhoneEntity);
            }
            else
            {
                return LocalUpdate1(target, personPhoneEntity);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
        {
            services.AddScoped<PersonPhoneModelFactory>();
            services.AddScoped<IPersonPhoneModelFactory, PersonPhoneModelFactory>();
            services.AddTransient<PersonPhoneModel>();
            services.AddTransient<IPersonPhoneModel, PersonPhoneModel>();
        }
    }
}