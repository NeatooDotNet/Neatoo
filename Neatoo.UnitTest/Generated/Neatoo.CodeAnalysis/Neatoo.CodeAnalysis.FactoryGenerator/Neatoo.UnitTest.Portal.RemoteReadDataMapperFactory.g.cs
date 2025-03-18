using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using static Neatoo.UnitTest.RemoteFactory.RemoteReadTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
                    Debugging Messages:
                    Parent class: RemoteReadTests

No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute
                    */
namespace Neatoo.UnitTest.RemoteFactory
{
    public interface IRemoteReadDataMapperFactory
    {
        Task<RemoteReadDataMapper> CreateVoid();
        Task<RemoteReadDataMapper?> CreateBool();
        Task<RemoteReadDataMapper> CreateTask();
        Task<RemoteReadDataMapper?> CreateTaskBool();
        Task<RemoteReadDataMapper> CreateVoid(int? param);
        Task<RemoteReadDataMapper?> CreateBool(int? param);
        Task<RemoteReadDataMapper> CreateTask(int? param);
        Task<RemoteReadDataMapper?> CreateTaskBool(int? param);
        Task<RemoteReadDataMapper?> CreateTaskBoolFalse(int? param);
        Task<RemoteReadDataMapper> CreateVoidDep();
        Task<RemoteReadDataMapper?> CreateBoolTrueDep();
        Task<RemoteReadDataMapper?> CreateBoolFalseDep();
        Task<RemoteReadDataMapper> CreateTaskDep();
        Task<RemoteReadDataMapper?> CreateTaskBoolDep();
        Task<RemoteReadDataMapper?> CreateTaskBoolFalseDep();
        Task<RemoteReadDataMapper> CreateVoidDep(int? param);
        Task<RemoteReadDataMapper?> CreateBoolTrueDep(int? param);
        Task<RemoteReadDataMapper?> CreateBoolFalseDep(int? param);
        Task<RemoteReadDataMapper> CreateTaskDep(int? param);
        Task<RemoteReadDataMapper?> CreateTaskBoolDep(int? param);
        Task<RemoteReadDataMapper> FetchVoid();
        Task<RemoteReadDataMapper?> FetchBool();
        Task<RemoteReadDataMapper> FetchTask();
        Task<RemoteReadDataMapper?> FetchTaskBool();
        Task<RemoteReadDataMapper> FetchVoid(int? param);
        Task<RemoteReadDataMapper?> FetchBool(int? param);
        Task<RemoteReadDataMapper> FetchTask(int? param);
        Task<RemoteReadDataMapper?> FetchTaskBool(int? param);
        Task<RemoteReadDataMapper> FetchVoidDep();
        Task<RemoteReadDataMapper?> FetchBoolTrueDep();
        Task<RemoteReadDataMapper?> FetchBoolFalseDep();
        Task<RemoteReadDataMapper> FetchTaskDep();
        Task<RemoteReadDataMapper?> FetchTaskBoolDep();
        Task<RemoteReadDataMapper> FetchVoidDep(int? param);
        Task<RemoteReadDataMapper?> FetchBoolTrueDep(int? param);
        Task<RemoteReadDataMapper?> FetchBoolFalseDep(int? param);
        Task<RemoteReadDataMapper> FetchTaskDep(int? param);
        Task<RemoteReadDataMapper?> FetchTaskBoolDep(int? param);
        Task<RemoteReadDataMapper?> FetchTaskBoolFalseDep(int? param);
    }

    internal class RemoteReadDataMapperFactory : FactoryBase, IRemoteReadDataMapperFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        public delegate Task<RemoteReadDataMapper> CreateVoidDelegate();
        public delegate Task<RemoteReadDataMapper?> CreateBoolDelegate();
        public delegate Task<RemoteReadDataMapper> CreateTaskDelegate();
        public delegate Task<RemoteReadDataMapper?> CreateTaskBoolDelegate();
        public delegate Task<RemoteReadDataMapper> CreateVoid1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper?> CreateBool1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper> CreateTask1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper?> CreateTaskBool1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper?> CreateTaskBoolFalseDelegate(int? param);
        public delegate Task<RemoteReadDataMapper> CreateVoidDepDelegate();
        public delegate Task<RemoteReadDataMapper?> CreateBoolTrueDepDelegate();
        public delegate Task<RemoteReadDataMapper?> CreateBoolFalseDepDelegate();
        public delegate Task<RemoteReadDataMapper> CreateTaskDepDelegate();
        public delegate Task<RemoteReadDataMapper?> CreateTaskBoolDepDelegate();
        public delegate Task<RemoteReadDataMapper?> CreateTaskBoolFalseDepDelegate();
        public delegate Task<RemoteReadDataMapper> CreateVoidDep1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper?> CreateBoolTrueDep1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper?> CreateBoolFalseDep1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper> CreateTaskDep1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper?> CreateTaskBoolDep1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper> FetchVoidDelegate();
        public delegate Task<RemoteReadDataMapper?> FetchBoolDelegate();
        public delegate Task<RemoteReadDataMapper> FetchTaskDelegate();
        public delegate Task<RemoteReadDataMapper?> FetchTaskBoolDelegate();
        public delegate Task<RemoteReadDataMapper> FetchVoid1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper?> FetchBool1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper> FetchTask1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper?> FetchTaskBool1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper> FetchVoidDepDelegate();
        public delegate Task<RemoteReadDataMapper?> FetchBoolTrueDepDelegate();
        public delegate Task<RemoteReadDataMapper?> FetchBoolFalseDepDelegate();
        public delegate Task<RemoteReadDataMapper> FetchTaskDepDelegate();
        public delegate Task<RemoteReadDataMapper?> FetchTaskBoolDepDelegate();
        public delegate Task<RemoteReadDataMapper> FetchVoidDep1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper?> FetchBoolTrueDep1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper?> FetchBoolFalseDep1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper> FetchTaskDep1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper?> FetchTaskBoolDep1Delegate(int? param);
        public delegate Task<RemoteReadDataMapper?> FetchTaskBoolFalseDepDelegate(int? param);
        // Delegate Properties to provide Local or Remote fork in execution
        public CreateVoidDelegate CreateVoidProperty { get; }
        public CreateBoolDelegate CreateBoolProperty { get; }
        public CreateTaskDelegate CreateTaskProperty { get; }
        public CreateTaskBoolDelegate CreateTaskBoolProperty { get; }
        public CreateVoid1Delegate CreateVoid1Property { get; }
        public CreateBool1Delegate CreateBool1Property { get; }
        public CreateTask1Delegate CreateTask1Property { get; }
        public CreateTaskBool1Delegate CreateTaskBool1Property { get; }
        public CreateTaskBoolFalseDelegate CreateTaskBoolFalseProperty { get; }
        public CreateVoidDepDelegate CreateVoidDepProperty { get; }
        public CreateBoolTrueDepDelegate CreateBoolTrueDepProperty { get; }
        public CreateBoolFalseDepDelegate CreateBoolFalseDepProperty { get; }
        public CreateTaskDepDelegate CreateTaskDepProperty { get; }
        public CreateTaskBoolDepDelegate CreateTaskBoolDepProperty { get; }
        public CreateTaskBoolFalseDepDelegate CreateTaskBoolFalseDepProperty { get; }
        public CreateVoidDep1Delegate CreateVoidDep1Property { get; }
        public CreateBoolTrueDep1Delegate CreateBoolTrueDep1Property { get; }
        public CreateBoolFalseDep1Delegate CreateBoolFalseDep1Property { get; }
        public CreateTaskDep1Delegate CreateTaskDep1Property { get; }
        public CreateTaskBoolDep1Delegate CreateTaskBoolDep1Property { get; }
        public FetchVoidDelegate FetchVoidProperty { get; }
        public FetchBoolDelegate FetchBoolProperty { get; }
        public FetchTaskDelegate FetchTaskProperty { get; }
        public FetchTaskBoolDelegate FetchTaskBoolProperty { get; }
        public FetchVoid1Delegate FetchVoid1Property { get; }
        public FetchBool1Delegate FetchBool1Property { get; }
        public FetchTask1Delegate FetchTask1Property { get; }
        public FetchTaskBool1Delegate FetchTaskBool1Property { get; }
        public FetchVoidDepDelegate FetchVoidDepProperty { get; }
        public FetchBoolTrueDepDelegate FetchBoolTrueDepProperty { get; }
        public FetchBoolFalseDepDelegate FetchBoolFalseDepProperty { get; }
        public FetchTaskDepDelegate FetchTaskDepProperty { get; }
        public FetchTaskBoolDepDelegate FetchTaskBoolDepProperty { get; }
        public FetchVoidDep1Delegate FetchVoidDep1Property { get; }
        public FetchBoolTrueDep1Delegate FetchBoolTrueDep1Property { get; }
        public FetchBoolFalseDep1Delegate FetchBoolFalseDep1Property { get; }
        public FetchTaskDep1Delegate FetchTaskDep1Property { get; }
        public FetchTaskBoolDep1Delegate FetchTaskBoolDep1Property { get; }
        public FetchTaskBoolFalseDepDelegate FetchTaskBoolFalseDepProperty { get; }

        public RemoteReadDataMapperFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            CreateVoidProperty = LocalCreateVoid;
            CreateBoolProperty = LocalCreateBool;
            CreateTaskProperty = LocalCreateTask;
            CreateTaskBoolProperty = LocalCreateTaskBool;
            CreateVoid1Property = LocalCreateVoid1;
            CreateBool1Property = LocalCreateBool1;
            CreateTask1Property = LocalCreateTask1;
            CreateTaskBool1Property = LocalCreateTaskBool1;
            CreateTaskBoolFalseProperty = LocalCreateTaskBoolFalse;
            CreateVoidDepProperty = LocalCreateVoidDep;
            CreateBoolTrueDepProperty = LocalCreateBoolTrueDep;
            CreateBoolFalseDepProperty = LocalCreateBoolFalseDep;
            CreateTaskDepProperty = LocalCreateTaskDep;
            CreateTaskBoolDepProperty = LocalCreateTaskBoolDep;
            CreateTaskBoolFalseDepProperty = LocalCreateTaskBoolFalseDep;
            CreateVoidDep1Property = LocalCreateVoidDep1;
            CreateBoolTrueDep1Property = LocalCreateBoolTrueDep1;
            CreateBoolFalseDep1Property = LocalCreateBoolFalseDep1;
            CreateTaskDep1Property = LocalCreateTaskDep1;
            CreateTaskBoolDep1Property = LocalCreateTaskBoolDep1;
            FetchVoidProperty = LocalFetchVoid;
            FetchBoolProperty = LocalFetchBool;
            FetchTaskProperty = LocalFetchTask;
            FetchTaskBoolProperty = LocalFetchTaskBool;
            FetchVoid1Property = LocalFetchVoid1;
            FetchBool1Property = LocalFetchBool1;
            FetchTask1Property = LocalFetchTask1;
            FetchTaskBool1Property = LocalFetchTaskBool1;
            FetchVoidDepProperty = LocalFetchVoidDep;
            FetchBoolTrueDepProperty = LocalFetchBoolTrueDep;
            FetchBoolFalseDepProperty = LocalFetchBoolFalseDep;
            FetchTaskDepProperty = LocalFetchTaskDep;
            FetchTaskBoolDepProperty = LocalFetchTaskBoolDep;
            FetchVoidDep1Property = LocalFetchVoidDep1;
            FetchBoolTrueDep1Property = LocalFetchBoolTrueDep1;
            FetchBoolFalseDep1Property = LocalFetchBoolFalseDep1;
            FetchTaskDep1Property = LocalFetchTaskDep1;
            FetchTaskBoolDep1Property = LocalFetchTaskBoolDep1;
            FetchTaskBoolFalseDepProperty = LocalFetchTaskBoolFalseDep;
        }

        public RemoteReadDataMapperFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
            CreateVoidProperty = RemoteCreateVoid;
            CreateBoolProperty = RemoteCreateBool;
            CreateTaskProperty = RemoteCreateTask;
            CreateTaskBoolProperty = RemoteCreateTaskBool;
            CreateVoid1Property = RemoteCreateVoid1;
            CreateBool1Property = RemoteCreateBool1;
            CreateTask1Property = RemoteCreateTask1;
            CreateTaskBool1Property = RemoteCreateTaskBool1;
            CreateTaskBoolFalseProperty = RemoteCreateTaskBoolFalse;
            CreateVoidDepProperty = RemoteCreateVoidDep;
            CreateBoolTrueDepProperty = RemoteCreateBoolTrueDep;
            CreateBoolFalseDepProperty = RemoteCreateBoolFalseDep;
            CreateTaskDepProperty = RemoteCreateTaskDep;
            CreateTaskBoolDepProperty = RemoteCreateTaskBoolDep;
            CreateTaskBoolFalseDepProperty = RemoteCreateTaskBoolFalseDep;
            CreateVoidDep1Property = RemoteCreateVoidDep1;
            CreateBoolTrueDep1Property = RemoteCreateBoolTrueDep1;
            CreateBoolFalseDep1Property = RemoteCreateBoolFalseDep1;
            CreateTaskDep1Property = RemoteCreateTaskDep1;
            CreateTaskBoolDep1Property = RemoteCreateTaskBoolDep1;
            FetchVoidProperty = RemoteFetchVoid;
            FetchBoolProperty = RemoteFetchBool;
            FetchTaskProperty = RemoteFetchTask;
            FetchTaskBoolProperty = RemoteFetchTaskBool;
            FetchVoid1Property = RemoteFetchVoid1;
            FetchBool1Property = RemoteFetchBool1;
            FetchTask1Property = RemoteFetchTask1;
            FetchTaskBool1Property = RemoteFetchTaskBool1;
            FetchVoidDepProperty = RemoteFetchVoidDep;
            FetchBoolTrueDepProperty = RemoteFetchBoolTrueDep;
            FetchBoolFalseDepProperty = RemoteFetchBoolFalseDep;
            FetchTaskDepProperty = RemoteFetchTaskDep;
            FetchTaskBoolDepProperty = RemoteFetchTaskBoolDep;
            FetchVoidDep1Property = RemoteFetchVoidDep1;
            FetchBoolTrueDep1Property = RemoteFetchBoolTrueDep1;
            FetchBoolFalseDep1Property = RemoteFetchBoolFalseDep1;
            FetchTaskDep1Property = RemoteFetchTaskDep1;
            FetchTaskBoolDep1Property = RemoteFetchTaskBoolDep1;
            FetchTaskBoolFalseDepProperty = RemoteFetchTaskBoolFalseDep;
        }

        public virtual Task<RemoteReadDataMapper> CreateVoid()
        {
            return CreateVoidProperty();
        }

        public virtual async Task<RemoteReadDataMapper> RemoteCreateVoid()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(CreateVoidDelegate), []);
        }

        public Task<RemoteReadDataMapper> LocalCreateVoid()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return Task.FromResult(DoMapperMethodCall<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoid()));
        }

        public virtual Task<RemoteReadDataMapper?> CreateBool()
        {
            return CreateBoolProperty();
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteCreateBool()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(CreateBoolDelegate), []);
        }

        public Task<RemoteReadDataMapper?> LocalCreateBool()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateBool()));
        }

        public virtual Task<RemoteReadDataMapper> CreateTask()
        {
            return CreateTaskProperty();
        }

        public virtual async Task<RemoteReadDataMapper> RemoteCreateTask()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(CreateTaskDelegate), []);
        }

        public Task<RemoteReadDataMapper> LocalCreateTask()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return DoMapperMethodCallAsync<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTask());
        }

        public virtual Task<RemoteReadDataMapper?> CreateTaskBool()
        {
            return CreateTaskBoolProperty();
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteCreateTaskBool()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(CreateTaskBoolDelegate), []);
        }

        public Task<RemoteReadDataMapper?> LocalCreateTaskBool()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return DoMapperMethodCallBoolAsync<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBool());
        }

        public virtual Task<RemoteReadDataMapper> CreateVoid(int? param)
        {
            return CreateVoid1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper> RemoteCreateVoid1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(CreateVoid1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper> LocalCreateVoid1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return Task.FromResult(DoMapperMethodCall<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoid(param)));
        }

        public virtual Task<RemoteReadDataMapper?> CreateBool(int? param)
        {
            return CreateBool1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteCreateBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(CreateBool1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper?> LocalCreateBool1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateBool(param)));
        }

        public virtual Task<RemoteReadDataMapper> CreateTask(int? param)
        {
            return CreateTask1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper> RemoteCreateTask1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(CreateTask1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper> LocalCreateTask1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return DoMapperMethodCallAsync<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTask(param));
        }

        public virtual Task<RemoteReadDataMapper?> CreateTaskBool(int? param)
        {
            return CreateTaskBool1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteCreateTaskBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(CreateTaskBool1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper?> LocalCreateTaskBool1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return DoMapperMethodCallBoolAsync<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBool(param));
        }

        public virtual Task<RemoteReadDataMapper?> CreateTaskBoolFalse(int? param)
        {
            return CreateTaskBoolFalseProperty(param);
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteCreateTaskBoolFalse(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(CreateTaskBoolFalseDelegate), [param]);
        }

        public Task<RemoteReadDataMapper?> LocalCreateTaskBoolFalse(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return DoMapperMethodCallBoolAsync<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolFalse(param));
        }

        public virtual Task<RemoteReadDataMapper> CreateVoidDep()
        {
            return CreateVoidDepProperty();
        }

        public virtual async Task<RemoteReadDataMapper> RemoteCreateVoidDep()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(CreateVoidDepDelegate), []);
        }

        public Task<RemoteReadDataMapper> LocalCreateVoidDep()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCall<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoidDep(disposableDependency)));
        }

        public virtual Task<RemoteReadDataMapper?> CreateBoolTrueDep()
        {
            return CreateBoolTrueDepProperty();
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteCreateBoolTrueDep()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(CreateBoolTrueDepDelegate), []);
        }

        public Task<RemoteReadDataMapper?> LocalCreateBoolTrueDep()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolTrueDep(disposableDependency)));
        }

        public virtual Task<RemoteReadDataMapper?> CreateBoolFalseDep()
        {
            return CreateBoolFalseDepProperty();
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteCreateBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(CreateBoolFalseDepDelegate), []);
        }

        public Task<RemoteReadDataMapper?> LocalCreateBoolFalseDep()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolFalseDep(disposableDependency)));
        }

        public virtual Task<RemoteReadDataMapper> CreateTaskDep()
        {
            return CreateTaskDepProperty();
        }

        public virtual async Task<RemoteReadDataMapper> RemoteCreateTaskDep()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(CreateTaskDepDelegate), []);
        }

        public Task<RemoteReadDataMapper> LocalCreateTaskDep()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskDep(disposableDependency));
        }

        public virtual Task<RemoteReadDataMapper?> CreateTaskBoolDep()
        {
            return CreateTaskBoolDepProperty();
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteCreateTaskBoolDep()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(CreateTaskBoolDepDelegate), []);
        }

        public Task<RemoteReadDataMapper?> LocalCreateTaskBoolDep()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolDep(disposableDependency));
        }

        public virtual Task<RemoteReadDataMapper?> CreateTaskBoolFalseDep()
        {
            return CreateTaskBoolFalseDepProperty();
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteCreateTaskBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(CreateTaskBoolFalseDepDelegate), []);
        }

        public Task<RemoteReadDataMapper?> LocalCreateTaskBoolFalseDep()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolFalseDep(disposableDependency));
        }

        public virtual Task<RemoteReadDataMapper> CreateVoidDep(int? param)
        {
            return CreateVoidDep1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper> RemoteCreateVoidDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(CreateVoidDep1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper> LocalCreateVoidDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCall<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoidDep(param, disposableDependency)));
        }

        public virtual Task<RemoteReadDataMapper?> CreateBoolTrueDep(int? param)
        {
            return CreateBoolTrueDep1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteCreateBoolTrueDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(CreateBoolTrueDep1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper?> LocalCreateBoolTrueDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolTrueDep(param, disposableDependency)));
        }

        public virtual Task<RemoteReadDataMapper?> CreateBoolFalseDep(int? param)
        {
            return CreateBoolFalseDep1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteCreateBoolFalseDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(CreateBoolFalseDep1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper?> LocalCreateBoolFalseDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolFalseDep(param, disposableDependency)));
        }

        public virtual Task<RemoteReadDataMapper> CreateTaskDep(int? param)
        {
            return CreateTaskDep1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper> RemoteCreateTaskDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(CreateTaskDep1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper> LocalCreateTaskDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskDep(param, disposableDependency));
        }

        public virtual Task<RemoteReadDataMapper?> CreateTaskBoolDep(int? param)
        {
            return CreateTaskBoolDep1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteCreateTaskBoolDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(CreateTaskBoolDep1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper?> LocalCreateTaskBoolDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolDep(param, disposableDependency));
        }

        public virtual Task<RemoteReadDataMapper> FetchVoid()
        {
            return FetchVoidProperty();
        }

        public virtual async Task<RemoteReadDataMapper> RemoteFetchVoid()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(FetchVoidDelegate), []);
        }

        public Task<RemoteReadDataMapper> LocalFetchVoid()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return Task.FromResult(DoMapperMethodCall<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoid()));
        }

        public virtual Task<RemoteReadDataMapper?> FetchBool()
        {
            return FetchBoolProperty();
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteFetchBool()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(FetchBoolDelegate), []);
        }

        public Task<RemoteReadDataMapper?> LocalFetchBool()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBool()));
        }

        public virtual Task<RemoteReadDataMapper> FetchTask()
        {
            return FetchTaskProperty();
        }

        public virtual async Task<RemoteReadDataMapper> RemoteFetchTask()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(FetchTaskDelegate), []);
        }

        public Task<RemoteReadDataMapper> LocalFetchTask()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return DoMapperMethodCallAsync<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTask());
        }

        public virtual Task<RemoteReadDataMapper?> FetchTaskBool()
        {
            return FetchTaskBoolProperty();
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteFetchTaskBool()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(FetchTaskBoolDelegate), []);
        }

        public Task<RemoteReadDataMapper?> LocalFetchTaskBool()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return DoMapperMethodCallBoolAsync<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBool());
        }

        public virtual Task<RemoteReadDataMapper> FetchVoid(int? param)
        {
            return FetchVoid1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper> RemoteFetchVoid1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(FetchVoid1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper> LocalFetchVoid1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return Task.FromResult(DoMapperMethodCall<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoid(param)));
        }

        public virtual Task<RemoteReadDataMapper?> FetchBool(int? param)
        {
            return FetchBool1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteFetchBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(FetchBool1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper?> LocalFetchBool1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBool(param)));
        }

        public virtual Task<RemoteReadDataMapper> FetchTask(int? param)
        {
            return FetchTask1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper> RemoteFetchTask1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(FetchTask1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper> LocalFetchTask1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return DoMapperMethodCallAsync<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTask(param));
        }

        public virtual Task<RemoteReadDataMapper?> FetchTaskBool(int? param)
        {
            return FetchTaskBool1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteFetchTaskBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(FetchTaskBool1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper?> LocalFetchTaskBool1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            return DoMapperMethodCallBoolAsync<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBool(param));
        }

        public virtual Task<RemoteReadDataMapper> FetchVoidDep()
        {
            return FetchVoidDepProperty();
        }

        public virtual async Task<RemoteReadDataMapper> RemoteFetchVoidDep()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(FetchVoidDepDelegate), []);
        }

        public Task<RemoteReadDataMapper> LocalFetchVoidDep()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCall<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoidDep(disposableDependency)));
        }

        public virtual Task<RemoteReadDataMapper?> FetchBoolTrueDep()
        {
            return FetchBoolTrueDepProperty();
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteFetchBoolTrueDep()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(FetchBoolTrueDepDelegate), []);
        }

        public Task<RemoteReadDataMapper?> LocalFetchBoolTrueDep()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolTrueDep(disposableDependency)));
        }

        public virtual Task<RemoteReadDataMapper?> FetchBoolFalseDep()
        {
            return FetchBoolFalseDepProperty();
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteFetchBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(FetchBoolFalseDepDelegate), []);
        }

        public Task<RemoteReadDataMapper?> LocalFetchBoolFalseDep()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolFalseDep(disposableDependency)));
        }

        public virtual Task<RemoteReadDataMapper> FetchTaskDep()
        {
            return FetchTaskDepProperty();
        }

        public virtual async Task<RemoteReadDataMapper> RemoteFetchTaskDep()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(FetchTaskDepDelegate), []);
        }

        public Task<RemoteReadDataMapper> LocalFetchTaskDep()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskDep(disposableDependency));
        }

        public virtual Task<RemoteReadDataMapper?> FetchTaskBoolDep()
        {
            return FetchTaskBoolDepProperty();
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteFetchTaskBoolDep()
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(FetchTaskBoolDepDelegate), []);
        }

        public Task<RemoteReadDataMapper?> LocalFetchTaskBoolDep()
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolDep(disposableDependency));
        }

        public virtual Task<RemoteReadDataMapper> FetchVoidDep(int? param)
        {
            return FetchVoidDep1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper> RemoteFetchVoidDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(FetchVoidDep1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper> LocalFetchVoidDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCall<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoidDep(param, disposableDependency)));
        }

        public virtual Task<RemoteReadDataMapper?> FetchBoolTrueDep(int? param)
        {
            return FetchBoolTrueDep1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteFetchBoolTrueDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(FetchBoolTrueDep1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper?> LocalFetchBoolTrueDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolTrueDep(param, disposableDependency)));
        }

        public virtual Task<RemoteReadDataMapper?> FetchBoolFalseDep(int? param)
        {
            return FetchBoolFalseDep1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteFetchBoolFalseDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(FetchBoolFalseDep1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper?> LocalFetchBoolFalseDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolFalseDep(param, disposableDependency)));
        }

        public virtual Task<RemoteReadDataMapper> FetchTaskDep(int? param)
        {
            return FetchTaskDep1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper> RemoteFetchTaskDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper>(typeof(FetchTaskDep1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper> LocalFetchTaskDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskDep(param, disposableDependency));
        }

        public virtual Task<RemoteReadDataMapper?> FetchTaskBoolDep(int? param)
        {
            return FetchTaskBoolDep1Property(param);
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteFetchTaskBoolDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(FetchTaskBoolDep1Delegate), [param]);
        }

        public Task<RemoteReadDataMapper?> LocalFetchTaskBoolDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolDep(param, disposableDependency));
        }

        public virtual Task<RemoteReadDataMapper?> FetchTaskBoolFalseDep(int? param)
        {
            return FetchTaskBoolFalseDepProperty(param);
        }

        public virtual async Task<RemoteReadDataMapper?> RemoteFetchTaskBoolFalseDep(int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteReadDataMapper?>(typeof(FetchTaskBoolFalseDepDelegate), [param]);
        }

        public Task<RemoteReadDataMapper?> LocalFetchTaskBoolFalseDep(int? param)
        {
            var target = ServiceProvider.GetRequiredService<RemoteReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolFalseDep(param, disposableDependency));
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<RemoteReadDataMapper>();
            services.AddScoped<RemoteReadDataMapperFactory>();
            services.AddScoped<IRemoteReadDataMapperFactory, RemoteReadDataMapperFactory>();
            services.AddScoped<CreateVoidDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalCreateVoid();
            });
            services.AddScoped<CreateBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalCreateBool();
            });
            services.AddScoped<CreateTaskDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalCreateTask();
            });
            services.AddScoped<CreateTaskBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalCreateTaskBool();
            });
            services.AddScoped<CreateVoid1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalCreateVoid1(param);
            });
            services.AddScoped<CreateBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalCreateBool1(param);
            });
            services.AddScoped<CreateTask1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalCreateTask1(param);
            });
            services.AddScoped<CreateTaskBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalCreateTaskBool1(param);
            });
            services.AddScoped<CreateTaskBoolFalseDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalCreateTaskBoolFalse(param);
            });
            services.AddScoped<CreateVoidDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalCreateVoidDep();
            });
            services.AddScoped<CreateBoolTrueDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalCreateBoolTrueDep();
            });
            services.AddScoped<CreateBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalCreateBoolFalseDep();
            });
            services.AddScoped<CreateTaskDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalCreateTaskDep();
            });
            services.AddScoped<CreateTaskBoolDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalCreateTaskBoolDep();
            });
            services.AddScoped<CreateTaskBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalCreateTaskBoolFalseDep();
            });
            services.AddScoped<CreateVoidDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalCreateVoidDep1(param);
            });
            services.AddScoped<CreateBoolTrueDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalCreateBoolTrueDep1(param);
            });
            services.AddScoped<CreateBoolFalseDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalCreateBoolFalseDep1(param);
            });
            services.AddScoped<CreateTaskDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalCreateTaskDep1(param);
            });
            services.AddScoped<CreateTaskBoolDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalCreateTaskBoolDep1(param);
            });
            services.AddScoped<FetchVoidDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalFetchVoid();
            });
            services.AddScoped<FetchBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalFetchBool();
            });
            services.AddScoped<FetchTaskDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalFetchTask();
            });
            services.AddScoped<FetchTaskBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalFetchTaskBool();
            });
            services.AddScoped<FetchVoid1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalFetchVoid1(param);
            });
            services.AddScoped<FetchBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalFetchBool1(param);
            });
            services.AddScoped<FetchTask1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalFetchTask1(param);
            });
            services.AddScoped<FetchTaskBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalFetchTaskBool1(param);
            });
            services.AddScoped<FetchVoidDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalFetchVoidDep();
            });
            services.AddScoped<FetchBoolTrueDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalFetchBoolTrueDep();
            });
            services.AddScoped<FetchBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalFetchBoolFalseDep();
            });
            services.AddScoped<FetchTaskDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalFetchTaskDep();
            });
            services.AddScoped<FetchTaskBoolDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return () => factory.LocalFetchTaskBoolDep();
            });
            services.AddScoped<FetchVoidDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalFetchVoidDep1(param);
            });
            services.AddScoped<FetchBoolTrueDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalFetchBoolTrueDep1(param);
            });
            services.AddScoped<FetchBoolFalseDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalFetchBoolFalseDep1(param);
            });
            services.AddScoped<FetchTaskDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalFetchTaskDep1(param);
            });
            services.AddScoped<FetchTaskBoolDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalFetchTaskBoolDep1(param);
            });
            services.AddScoped<FetchTaskBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteReadDataMapperFactory>();
                return (int? param) => factory.LocalFetchTaskBoolFalseDep(param);
            });
        }
    }
}