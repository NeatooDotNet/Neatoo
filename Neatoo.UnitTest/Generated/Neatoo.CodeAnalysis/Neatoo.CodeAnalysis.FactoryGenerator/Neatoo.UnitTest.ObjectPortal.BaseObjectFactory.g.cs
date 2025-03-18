using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

/*
                    Debugging Messages:
                    : Base<BaseObject>, IBaseObject
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
namespace Neatoo.UnitTest.RemoteFactory
{
    public interface IBaseObjectFactory
    {
        Task<IBaseObject> CreateAsync();
        IBaseObject Create(int criteria);
        IBaseObject Create(int i, string s);
        IBaseObject Create(int i, double d);
        Task<IBaseObject> Create(Guid criteria);
        IBaseObject Fetch();
        IBaseObject Fetch(int criteria);
        Task<IBaseObject> Fetch(Guid criteria);
    }

    internal class BaseObjectFactory : FactoryBase, IBaseObjectFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        public delegate Task<IBaseObject> Create2Delegate(Guid criteria);
        public delegate Task<IBaseObject> Fetch2Delegate(Guid criteria);
        // Delegate Properties to provide Local or Remote fork in execution
        public Create2Delegate Create2Property { get; }
        public Fetch2Delegate Fetch2Property { get; }

        public BaseObjectFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            Create2Property = LocalCreate2;
            Fetch2Property = LocalFetch2;
        }

        public BaseObjectFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
            Create2Property = RemoteCreate2;
            Fetch2Property = RemoteFetch2;
        }

        public virtual Task<IBaseObject> CreateAsync()
        {
            return LocalCreateAsync();
        }

        public Task<IBaseObject> LocalCreateAsync()
        {
            var target = ServiceProvider.GetRequiredService<BaseObject>();
            return DoMapperMethodCallAsync<IBaseObject>(target, DataMapperMethod.Create, () => target.CreateAsync());
        }

        public virtual IBaseObject Create(int criteria)
        {
            return LocalCreate(criteria);
        }

        public IBaseObject LocalCreate(int criteria)
        {
            var target = ServiceProvider.GetRequiredService<BaseObject>();
            return DoMapperMethodCall<IBaseObject>(target, DataMapperMethod.Create, () => target.Create(criteria));
        }

        public virtual IBaseObject Create(int i, string s)
        {
            return LocalCreate1(i, s);
        }

        public IBaseObject LocalCreate1(int i, string s)
        {
            var target = ServiceProvider.GetRequiredService<BaseObject>();
            return DoMapperMethodCall<IBaseObject>(target, DataMapperMethod.Create, () => target.Create(i, s));
        }

        public virtual IBaseObject Create(int i, double d)
        {
            return LocalCreate3(i, d);
        }

        public IBaseObject LocalCreate3(int i, double d)
        {
            var target = ServiceProvider.GetRequiredService<BaseObject>();
            var dep = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<IBaseObject>(target, DataMapperMethod.Create, () => target.Create(i, d, dep));
        }

        public virtual Task<IBaseObject> Create(Guid criteria)
        {
            return Create2Property(criteria);
        }

        public virtual async Task<IBaseObject> RemoteCreate2(Guid criteria)
        {
            return await DoRemoteRequest.ForDelegate<IBaseObject>(typeof(Create2Delegate), [criteria]);
        }

        public Task<IBaseObject> LocalCreate2(Guid criteria)
        {
            var target = ServiceProvider.GetRequiredService<BaseObject>();
            var dependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<IBaseObject>(target, DataMapperMethod.Create, () => target.Create(criteria, dependency));
        }

        public virtual IBaseObject Fetch()
        {
            return LocalFetch();
        }

        public IBaseObject LocalFetch()
        {
            var target = ServiceProvider.GetRequiredService<BaseObject>();
            return DoMapperMethodCall<IBaseObject>(target, DataMapperMethod.Fetch, () => target.Fetch());
        }

        public virtual IBaseObject Fetch(int criteria)
        {
            return LocalFetch1(criteria);
        }

        public IBaseObject LocalFetch1(int criteria)
        {
            var target = ServiceProvider.GetRequiredService<BaseObject>();
            return DoMapperMethodCall<IBaseObject>(target, DataMapperMethod.Fetch, () => target.Fetch(criteria));
        }

        public virtual Task<IBaseObject> Fetch(Guid criteria)
        {
            return Fetch2Property(criteria);
        }

        public virtual async Task<IBaseObject> RemoteFetch2(Guid criteria)
        {
            return await DoRemoteRequest.ForDelegate<IBaseObject>(typeof(Fetch2Delegate), [criteria]);
        }

        public Task<IBaseObject> LocalFetch2(Guid criteria)
        {
            var target = ServiceProvider.GetRequiredService<BaseObject>();
            var dependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<IBaseObject>(target, DataMapperMethod.Fetch, () => target.Fetch(criteria, dependency));
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<BaseObject>();
            services.AddScoped<BaseObjectFactory>();
            services.AddScoped<IBaseObjectFactory, BaseObjectFactory>();
            services.AddTransient<IBaseObject, BaseObject>();
            services.AddScoped<Create2Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<BaseObjectFactory>();
                return (Guid criteria) => factory.LocalCreate2(criteria);
            });
            services.AddScoped<Fetch2Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<BaseObjectFactory>();
                return (Guid criteria) => factory.LocalFetch2(criteria);
            });
        }
    }
}