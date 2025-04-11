#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Person.Ef;

/*
Interface Found. TargetType: IPersonPhoneModelList ConcreteType: PersonPhoneModelList
Class: Person.DomainModel.PersonPhoneModelList Name: PersonPhoneModelList
No MethodDeclarationSyntax for get_DeletedList
No MethodDeclarationSyntax for get_DeletedList
No MethodDeclarationSyntax for get_EditMetaState
No MethodDeclarationSyntax for Neatoo.IEditListBase.get_DeletedList
No MethodDeclarationSyntax for InsertItem
No MethodDeclarationSyntax for get_IsPaused
No MethodDeclarationSyntax for set_IsPaused
No MethodDeclarationSyntax for get_MetaState
No MethodDeclarationSyntax for get_PropertyMessages
No MethodDeclarationSyntax for RunRules
No MethodDeclarationSyntax for RunRules
No MethodDeclarationSyntax for RunRules
No MethodDeclarationSyntax for RunRules
No MethodDeclarationSyntax for RunRules
No MethodDeclarationSyntax for RunRules
No MethodDeclarationSyntax for get_Parent
No MethodDeclarationSyntax for get_Parent
No MethodDeclarationSyntax for set_Parent
No MethodDeclarationSyntax for set_Parent
No MethodDeclarationSyntax for add_NeatooPropertyChanged
No MethodDeclarationSyntax for add_NeatooPropertyChanged
No MethodDeclarationSyntax for remove_NeatooPropertyChanged
No MethodDeclarationSyntax for remove_NeatooPropertyChanged
No MethodDeclarationSyntax for Neatoo.Internal.ISetParent.SetParent
No MethodDeclarationSyntax for InsertItem
No MethodDeclarationSyntax for PostPortalConstruct
No MethodDeclarationSyntax for RaiseNeatooPropertyChanged
No MethodDeclarationSyntax for HandleNeatooPropertyChanged
No MethodDeclarationSyntax for HandlePropertyChanged
No MethodDeclarationSyntax for WaitForTasks
No MethodDeclarationSyntax for WaitForTasks
No MethodDeclarationSyntax for WaitForTasks
No MethodDeclarationSyntax for BlockReentrancy
No MethodDeclarationSyntax for InsertItem
No MethodDeclarationSyntax for OnCollectionChanged
No MethodDeclarationSyntax for OnPropertyChanged
No MethodDeclarationSyntax for SetItem
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute

*/
namespace Person.DomainModel
{
    public interface IPersonPhoneModelListFactory
    {
        IPersonPhoneModelList Fetch(IEnumerable<PersonPhoneEntity> personPhoneEntities);
        IPersonPhoneModelList Save(IPersonPhoneModelList target, ICollection<PersonPhoneEntity> personPhoneEntities);
    }

    internal class PersonPhoneModelListFactory : FactoryBase<IPersonPhoneModelList>, IPersonPhoneModelListFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public PersonPhoneModelListFactory(IServiceProvider serviceProvider, IFactoryCore<IPersonPhoneModelList> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public PersonPhoneModelListFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IPersonPhoneModelList> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public virtual IPersonPhoneModelList Fetch(IEnumerable<PersonPhoneEntity> personPhoneEntities)
        {
            return LocalFetch(personPhoneEntities);
        }

        public IPersonPhoneModelList LocalFetch(IEnumerable<PersonPhoneEntity> personPhoneEntities)
        {
            var target = ServiceProvider.GetRequiredService<PersonPhoneModelList>();
            var personPhoneModelFactory = ServiceProvider.GetRequiredService<IPersonPhoneModelFactory>();
            return DoFactoryMethodCall(target, FactoryOperation.Fetch, () => target.Fetch(personPhoneEntities, personPhoneModelFactory));
        }

        public IPersonPhoneModelList LocalUpdate(IPersonPhoneModelList target, ICollection<PersonPhoneEntity> personPhoneEntities)
        {
            var cTarget = (PersonPhoneModelList)target ?? throw new Exception("IPersonPhoneModelList must implement PersonPhoneModelList");
            var personPhoneModelFactory = ServiceProvider.GetRequiredService<IPersonPhoneModelFactory>();
            return DoFactoryMethodCall(cTarget, FactoryOperation.Update, () => cTarget.Update(personPhoneEntities, personPhoneModelFactory));
        }

        public virtual IPersonPhoneModelList Save(IPersonPhoneModelList target, ICollection<PersonPhoneEntity> personPhoneEntities)
        {
            return LocalSave(target, personPhoneEntities);
        }

        public virtual IPersonPhoneModelList LocalSave(IPersonPhoneModelList target, ICollection<PersonPhoneEntity> personPhoneEntities)
        {
            if (target.IsDeleted)
            {
                throw new NotImplementedException();
            }
            else if (target.IsNew)
            {
                throw new NotImplementedException();
            }
            else
            {
                return LocalUpdate(target, personPhoneEntities);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
        {
            services.AddScoped<PersonPhoneModelListFactory>();
            services.AddScoped<IPersonPhoneModelListFactory, PersonPhoneModelListFactory>();
            services.AddTransient<PersonPhoneModelList>();
            services.AddTransient<IPersonPhoneModelList, PersonPhoneModelList>();
        }
    }
}