#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;

/*
Interface Found. TargetType: IEditObject ConcreteType: EditObject
Class: Neatoo.UnitTest.SystemTextJson.EditTests.EditObject Name: EditObject
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
No MethodDeclarationSyntax for Save
No MethodDeclarationSyntax for Save
No MethodDeclarationSyntax for GetProperty
No MethodDeclarationSyntax for get_Item
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
namespace Neatoo.UnitTest.SystemTextJson.EditTests
{
    public interface IEditObjectFactory
    {
        Task<IEditObject> Create(Guid ID, string Name);
        Task<IEditObject> Save(IEditObject target);
    }

    internal class EditObjectFactory : FactorySaveBase<IEditObject>, IFactorySave<EditObject>, IEditObjectFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public EditObjectFactory(IServiceProvider serviceProvider, IFactoryCore<IEditObject> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public EditObjectFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IEditObject> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public virtual Task<IEditObject> Create(Guid ID, string Name)
        {
            return LocalCreate(ID, Name);
        }

        public Task<IEditObject> LocalCreate(Guid ID, string Name)
        {
            var target = ServiceProvider.GetRequiredService<EditObject>();
            return DoFactoryMethodCallAsync(target, FactoryOperation.Create, () => target.Create(ID, Name));
        }

        public Task<IEditObject> LocalUpdate(IEditObject target)
        {
            var cTarget = (EditObject)target ?? throw new Exception("IEditObject must implement EditObject");
            return DoFactoryMethodCallAsync(cTarget, FactoryOperation.Update, () => cTarget.Update());
        }

        public Task<IEditObject> LocalUpdate1(IEditObject target)
        {
            var cTarget = (EditObject)target ?? throw new Exception("IEditObject must implement EditObject");
            return DoFactoryMethodCallAsync(cTarget, FactoryOperation.Insert, () => cTarget.Update());
        }

        public virtual Task<IEditObject> Save(IEditObject target)
        {
            return LocalSave(target);
        }

        async Task<IFactorySaveMeta?> IFactorySave<EditObject>.Save(EditObject target)
        {
            return (IFactorySaveMeta? )await Save(target);
        }

        public virtual async Task<IEditObject> LocalSave(IEditObject target)
        {
            if (target.IsDeleted)
            {
                throw new NotImplementedException();
            }
            else if (target.IsNew)
            {
                return await LocalUpdate1(target);
            }
            else
            {
                return await LocalUpdate(target);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddScoped<EditObjectFactory>();
            services.AddScoped<IEditObjectFactory, EditObjectFactory>();
            services.AddTransient<EditObject>();
            services.AddTransient<IEditObject, EditObject>();
            services.AddScoped<IFactorySave<EditObject>, EditObjectFactory>();
        }
    }
}