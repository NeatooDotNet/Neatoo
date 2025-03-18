#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Objects;

/*
Interface Found. TargetType: IEditObject ConcreteType: EditObject
Class: Neatoo.UnitTest.ObjectPortal.EditObject Name: EditObject
Ignoring [CreateRemote] method with attribute [Remote]. Not a Factory or Authorize attribute.
No Factory or Authorize attribute for CreateRemote attribute RemoteAttribute
Ignoring [FetchRemote] method with attribute [Remote]. Not a Factory or Authorize attribute.
No Factory or Authorize attribute for FetchRemote attribute RemoteAttribute
Ignoring [FetchFailDependency] method with attribute [Remote]. Not a Factory or Authorize attribute.
No Factory or Authorize attribute for FetchFailDependency attribute RemoteAttribute
Ignoring [FetchFailAsyncDependency] method with attribute [Remote]. Not a Factory or Authorize attribute.
No Factory or Authorize attribute for FetchFailAsyncDependency attribute RemoteAttribute
Ignoring [Update] method with attribute [Remote]. Not a Factory or Authorize attribute.
No Factory or Authorize attribute for Update attribute RemoteAttribute
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
namespace Neatoo.UnitTest.ObjectPortal
{
    public interface IEditObjectFactory
    {
        IEditObject Create();
        Task<IEditObject> CreateAsync(int criteria);
        IEditObject Create(Guid criteria);
        Task<IEditObject> CreateRemote(Guid criteria);
        IEditObject Fetch();
        Task<IEditObject> Fetch(int criteria);
        IEditObject Fetch(Guid criteria);
        Task<IEditObject> FetchRemote(Guid criteria);
        IEditObject? FetchFail();
        Task<IEditObject?> FetchFailAsync();
        Task<IEditObject?> FetchFailDependency();
        Task<IEditObject?> FetchFailAsyncDependency();
        Task<IEditObject?> Save(IEditObject target);
        Task<IEditObject?> Save(IEditObject target, int criteriaA);
        Task<IEditObject?> Save(IEditObject target, Guid criteria);
    }

    internal class EditObjectFactory : FactorySaveBase<IEditObject>, IFactorySave<EditObject>, IEditObjectFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        public delegate Task<IEditObject> CreateRemoteDelegate(Guid criteria);
        public delegate Task<IEditObject> FetchRemoteDelegate(Guid criteria);
        public delegate Task<IEditObject?> FetchFailDependencyDelegate();
        public delegate Task<IEditObject?> FetchFailAsyncDependencyDelegate();
        public delegate Task<IEditObject?> Save2Delegate(IEditObject target, Guid criteria);
        // Delegate Properties to provide Local or Remote fork in execution
        public CreateRemoteDelegate CreateRemoteProperty { get; }
        public FetchRemoteDelegate FetchRemoteProperty { get; }
        public FetchFailDependencyDelegate FetchFailDependencyProperty { get; }
        public FetchFailAsyncDependencyDelegate FetchFailAsyncDependencyProperty { get; }
        public Save2Delegate Save2Property { get; }

        public EditObjectFactory(IServiceProvider serviceProvider, IFactoryCore<IEditObject> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            CreateRemoteProperty = LocalCreateRemote;
            FetchRemoteProperty = LocalFetchRemote;
            FetchFailDependencyProperty = LocalFetchFailDependency;
            FetchFailAsyncDependencyProperty = LocalFetchFailAsyncDependency;
            Save2Property = LocalSave2;
        }

        public EditObjectFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IEditObject> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
            CreateRemoteProperty = RemoteCreateRemote;
            FetchRemoteProperty = RemoteFetchRemote;
            FetchFailDependencyProperty = RemoteFetchFailDependency;
            FetchFailAsyncDependencyProperty = RemoteFetchFailAsyncDependency;
            Save2Property = RemoteSave2;
        }

        public virtual IEditObject Create()
        {
            return LocalCreate();
        }

        public IEditObject LocalCreate()
        {
            var target = ServiceProvider.GetRequiredService<EditObject>();
            return DoFactoryMethodCall(target, FactoryOperation.Create, () => target.Create());
        }

        public virtual Task<IEditObject> CreateAsync(int criteria)
        {
            return LocalCreateAsync(criteria);
        }

        public Task<IEditObject> LocalCreateAsync(int criteria)
        {
            var target = ServiceProvider.GetRequiredService<EditObject>();
            return DoFactoryMethodCallAsync(target, FactoryOperation.Create, () => target.CreateAsync(criteria));
        }

        public virtual IEditObject Create(Guid criteria)
        {
            return LocalCreate1(criteria);
        }

        public IEditObject LocalCreate1(Guid criteria)
        {
            var target = ServiceProvider.GetRequiredService<EditObject>();
            var dependency = ServiceProvider.GetRequiredService<IDisposableDependency>();
            return DoFactoryMethodCall(target, FactoryOperation.Create, () => target.Create(criteria, dependency));
        }

        public virtual Task<IEditObject> CreateRemote(Guid criteria)
        {
            return CreateRemoteProperty(criteria);
        }

        public virtual async Task<IEditObject> RemoteCreateRemote(Guid criteria)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<IEditObject>(typeof(CreateRemoteDelegate), [criteria]))!;
        }

        public Task<IEditObject> LocalCreateRemote(Guid criteria)
        {
            var target = ServiceProvider.GetRequiredService<EditObject>();
            var dependency = ServiceProvider.GetRequiredService<IDisposableDependency>();
            return Task.FromResult(DoFactoryMethodCall(target, FactoryOperation.Create, () => target.CreateRemote(criteria, dependency)));
        }

        public virtual IEditObject Fetch()
        {
            return LocalFetch();
        }

        public IEditObject LocalFetch()
        {
            var target = ServiceProvider.GetRequiredService<EditObject>();
            return DoFactoryMethodCall(target, FactoryOperation.Fetch, () => target.Fetch());
        }

        public virtual Task<IEditObject> Fetch(int criteria)
        {
            return LocalFetch1(criteria);
        }

        public Task<IEditObject> LocalFetch1(int criteria)
        {
            var target = ServiceProvider.GetRequiredService<EditObject>();
            return DoFactoryMethodCallAsync(target, FactoryOperation.Fetch, () => target.Fetch(criteria));
        }

        public virtual IEditObject Fetch(Guid criteria)
        {
            return LocalFetch2(criteria);
        }

        public IEditObject LocalFetch2(Guid criteria)
        {
            var target = ServiceProvider.GetRequiredService<EditObject>();
            var dependency = ServiceProvider.GetRequiredService<IDisposableDependency>();
            return DoFactoryMethodCall(target, FactoryOperation.Fetch, () => target.Fetch(criteria, dependency));
        }

        public virtual Task<IEditObject> FetchRemote(Guid criteria)
        {
            return FetchRemoteProperty(criteria);
        }

        public virtual async Task<IEditObject> RemoteFetchRemote(Guid criteria)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<IEditObject>(typeof(FetchRemoteDelegate), [criteria]))!;
        }

        public Task<IEditObject> LocalFetchRemote(Guid criteria)
        {
            var target = ServiceProvider.GetRequiredService<EditObject>();
            var dependency = ServiceProvider.GetRequiredService<IDisposableDependency>();
            return Task.FromResult(DoFactoryMethodCall(target, FactoryOperation.Fetch, () => target.FetchRemote(criteria, dependency)));
        }

        public virtual IEditObject? FetchFail()
        {
            return LocalFetchFail();
        }

        public IEditObject? LocalFetchFail()
        {
            var target = ServiceProvider.GetRequiredService<EditObject>();
            return DoFactoryMethodCallBool(target, FactoryOperation.Fetch, () => target.FetchFail());
        }

        public virtual Task<IEditObject?> FetchFailAsync()
        {
            return LocalFetchFailAsync();
        }

        public Task<IEditObject?> LocalFetchFailAsync()
        {
            var target = ServiceProvider.GetRequiredService<EditObject>();
            return DoFactoryMethodCallBoolAsync(target, FactoryOperation.Fetch, () => target.FetchFailAsync());
        }

        public virtual Task<IEditObject?> FetchFailDependency()
        {
            return FetchFailDependencyProperty();
        }

        public virtual async Task<IEditObject?> RemoteFetchFailDependency()
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<IEditObject?>(typeof(FetchFailDependencyDelegate), []))!;
        }

        public Task<IEditObject?> LocalFetchFailDependency()
        {
            var target = ServiceProvider.GetRequiredService<EditObject>();
            var dependency = ServiceProvider.GetRequiredService<IDisposableDependency>();
            return Task.FromResult(DoFactoryMethodCallBool(target, FactoryOperation.Fetch, () => target.FetchFailDependency(dependency)));
        }

        public virtual Task<IEditObject?> FetchFailAsyncDependency()
        {
            return FetchFailAsyncDependencyProperty();
        }

        public virtual async Task<IEditObject?> RemoteFetchFailAsyncDependency()
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<IEditObject?>(typeof(FetchFailAsyncDependencyDelegate), []))!;
        }

        public Task<IEditObject?> LocalFetchFailAsyncDependency()
        {
            var target = ServiceProvider.GetRequiredService<EditObject>();
            var dependency = ServiceProvider.GetRequiredService<IDisposableDependency>();
            return DoFactoryMethodCallBoolAsync(target, FactoryOperation.Fetch, () => target.FetchFailAsyncDependency(dependency));
        }

        public IEditObject LocalInsert(IEditObject target)
        {
            var cTarget = (EditObject)target ?? throw new Exception("IEditObject must implement EditObject");
            return DoFactoryMethodCall(cTarget, FactoryOperation.Insert, () => cTarget.Insert());
        }

        public Task<IEditObject> LocalInsert1(IEditObject target, int criteriaA)
        {
            var cTarget = (EditObject)target ?? throw new Exception("IEditObject must implement EditObject");
            return DoFactoryMethodCallAsync(cTarget, FactoryOperation.Insert, () => cTarget.Insert(criteriaA));
        }

        public IEditObject LocalInsert2(IEditObject target, Guid criteria)
        {
            var cTarget = (EditObject)target ?? throw new Exception("IEditObject must implement EditObject");
            var dependency = ServiceProvider.GetRequiredService<IDisposableDependency>();
            return DoFactoryMethodCall(cTarget, FactoryOperation.Insert, () => cTarget.Insert(criteria, dependency));
        }

        public Task<IEditObject> LocalUpdate(IEditObject target)
        {
            var cTarget = (EditObject)target ?? throw new Exception("IEditObject must implement EditObject");
            return DoFactoryMethodCallAsync(cTarget, FactoryOperation.Update, () => cTarget.Update());
        }

        public Task<IEditObject> LocalUpdate1(IEditObject target, int criteriaB)
        {
            var cTarget = (EditObject)target ?? throw new Exception("IEditObject must implement EditObject");
            return DoFactoryMethodCallAsync(cTarget, FactoryOperation.Update, () => cTarget.Update(criteriaB));
        }

        public Task<IEditObject> LocalUpdate2(IEditObject target, Guid criteria)
        {
            var cTarget = (EditObject)target ?? throw new Exception("IEditObject must implement EditObject");
            var dependency = ServiceProvider.GetRequiredService<IDisposableDependency>();
            return DoFactoryMethodCallAsync(cTarget, FactoryOperation.Update, () => cTarget.Update(criteria, dependency));
        }

        public IEditObject LocalDelete(IEditObject target)
        {
            var cTarget = (EditObject)target ?? throw new Exception("IEditObject must implement EditObject");
            return DoFactoryMethodCall(cTarget, FactoryOperation.Delete, () => cTarget.Delete());
        }

        public Task<IEditObject> LocalDelete1(IEditObject target, int criteriaC)
        {
            var cTarget = (EditObject)target ?? throw new Exception("IEditObject must implement EditObject");
            return DoFactoryMethodCallAsync(cTarget, FactoryOperation.Delete, () => cTarget.Delete(criteriaC));
        }

        public IEditObject LocalDelete2(IEditObject target, Guid criteria)
        {
            var cTarget = (EditObject)target ?? throw new Exception("IEditObject must implement EditObject");
            var dependency = ServiceProvider.GetRequiredService<IDisposableDependency>();
            return DoFactoryMethodCall(cTarget, FactoryOperation.Delete, () => cTarget.Delete(criteria, dependency));
        }

        public virtual Task<IEditObject?> Save(IEditObject target)
        {
            return LocalSave(target);
        }

        async Task<IFactorySaveMeta?> IFactorySave<EditObject>.Save(EditObject target)
        {
            return (IFactorySaveMeta? )await Save(target);
        }

        public virtual async Task<IEditObject?> LocalSave(IEditObject target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(IEditObject);
                }

                return LocalDelete(target);
            }
            else if (target.IsNew)
            {
                return LocalInsert(target);
            }
            else
            {
                return await LocalUpdate(target);
            }
        }

        public virtual Task<IEditObject?> Save(IEditObject target, int criteriaA)
        {
            return LocalSave1(target, criteriaA);
        }

        public virtual async Task<IEditObject?> LocalSave1(IEditObject target, int criteriaA)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(IEditObject);
                }

                return await LocalDelete1(target, criteriaA);
            }
            else if (target.IsNew)
            {
                return await LocalInsert1(target, criteriaA);
            }
            else
            {
                return await LocalUpdate1(target, criteriaA);
            }
        }

        public virtual Task<IEditObject?> Save(IEditObject target, Guid criteria)
        {
            return Save2Property(target, criteria);
        }

        public virtual async Task<IEditObject?> RemoteSave2(IEditObject target, Guid criteria)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<IEditObject?>(typeof(Save2Delegate), [target, criteria]))!;
        }

        public virtual async Task<IEditObject?> LocalSave2(IEditObject target, Guid criteria)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(IEditObject);
                }

                return LocalDelete2(target, criteria);
            }
            else if (target.IsNew)
            {
                return LocalInsert2(target, criteria);
            }
            else
            {
                return await LocalUpdate2(target, criteria);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddScoped<EditObjectFactory>();
            services.AddScoped<IEditObjectFactory, EditObjectFactory>();
            services.AddScoped<CreateRemoteDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<EditObjectFactory>();
                return (Guid criteria) => factory.LocalCreateRemote(criteria);
            });
            services.AddScoped<FetchRemoteDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<EditObjectFactory>();
                return (Guid criteria) => factory.LocalFetchRemote(criteria);
            });
            services.AddScoped<FetchFailDependencyDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<EditObjectFactory>();
                return () => factory.LocalFetchFailDependency();
            });
            services.AddScoped<FetchFailAsyncDependencyDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<EditObjectFactory>();
                return () => factory.LocalFetchFailAsyncDependency();
            });
            services.AddScoped<Save2Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<EditObjectFactory>();
                return (IEditObject target, Guid criteria) => factory.LocalSave2(target, criteria);
            });
            services.AddTransient<EditObject>();
            services.AddTransient<IEditObject, EditObject>();
            services.AddScoped<IFactorySave<EditObject>, EditObjectFactory>();
        }
    }
}