using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using static Neatoo.UnitTest.RemoteFactory.ReadTests;
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
                    Parent class: ReadTests

No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute
                    */
namespace Neatoo.UnitTest.RemoteFactory
{
    public interface IReadDataMapperFactory
    {
        ReadDataMapper CreateVoid();
        ReadDataMapper? CreateBool();
        Task<ReadDataMapper> CreateTask();
        Task<ReadDataMapper?> CreateTaskBool();
        ReadDataMapper CreateVoid(int? param);
        ReadDataMapper? CreateBool(int? param);
        Task<ReadDataMapper> CreateTask(int? param);
        Task<ReadDataMapper?> CreateTaskBool(int? param);
        Task<ReadDataMapper?> CreateTaskBoolFalse(int? param);
        ReadDataMapper CreateVoidDep();
        ReadDataMapper? CreateBoolTrueDep();
        ReadDataMapper? CreateBoolFalseDep();
        Task<ReadDataMapper> CreateTaskDep();
        Task<ReadDataMapper?> CreateTaskBoolDep();
        Task<ReadDataMapper?> CreateTaskBoolFalseDep();
        ReadDataMapper CreateVoidDep(int? param);
        ReadDataMapper? CreateBoolTrueDep(int? param);
        ReadDataMapper? CreateBoolFalseDep(int? param);
        Task<ReadDataMapper> CreateTaskDep(int? param);
        Task<ReadDataMapper?> CreateTaskBoolDep(int? param);
        ReadDataMapper FetchVoid();
        ReadDataMapper? FetchBool();
        Task<ReadDataMapper> FetchTask();
        Task<ReadDataMapper?> FetchTaskBool();
        ReadDataMapper FetchVoid(int? param);
        ReadDataMapper? FetchBool(int? param);
        Task<ReadDataMapper> FetchTask(int? param);
        Task<ReadDataMapper?> FetchTaskBool(int? param);
        ReadDataMapper FetchVoidDep();
        ReadDataMapper? FetchBoolTrueDep();
        ReadDataMapper? FetchBoolFalseDep();
        Task<ReadDataMapper> FetchTaskDep();
        Task<ReadDataMapper?> FetchTaskBoolDep();
        ReadDataMapper FetchVoidDep(int? param);
        ReadDataMapper? FetchBoolTrueDep(int? param);
        ReadDataMapper? FetchBoolFalseDep(int? param);
        Task<ReadDataMapper> FetchTaskDep(int? param);
        Task<ReadDataMapper?> FetchTaskBoolDep(int? param);
        Task<ReadDataMapper?> FetchTaskBoolFalseDep(int? param);
    }

    internal class ReadDataMapperFactory : FactoryBase, IReadDataMapperFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public ReadDataMapperFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public ReadDataMapperFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
        }

        public virtual ReadDataMapper CreateVoid()
        {
            return LocalCreateVoid();
        }

        public ReadDataMapper LocalCreateVoid()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCall<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoid());
        }

        public virtual ReadDataMapper? CreateBool()
        {
            return LocalCreateBool();
        }

        public ReadDataMapper? LocalCreateBool()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCallBool<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateBool());
        }

        public virtual Task<ReadDataMapper> CreateTask()
        {
            return LocalCreateTask();
        }

        public Task<ReadDataMapper> LocalCreateTask()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCallAsync<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTask());
        }

        public virtual Task<ReadDataMapper?> CreateTaskBool()
        {
            return LocalCreateTaskBool();
        }

        public Task<ReadDataMapper?> LocalCreateTaskBool()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCallBoolAsync<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBool());
        }

        public virtual ReadDataMapper CreateVoid(int? param)
        {
            return LocalCreateVoid1(param);
        }

        public ReadDataMapper LocalCreateVoid1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCall<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoid(param));
        }

        public virtual ReadDataMapper? CreateBool(int? param)
        {
            return LocalCreateBool1(param);
        }

        public ReadDataMapper? LocalCreateBool1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCallBool<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateBool(param));
        }

        public virtual Task<ReadDataMapper> CreateTask(int? param)
        {
            return LocalCreateTask1(param);
        }

        public Task<ReadDataMapper> LocalCreateTask1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCallAsync<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTask(param));
        }

        public virtual Task<ReadDataMapper?> CreateTaskBool(int? param)
        {
            return LocalCreateTaskBool1(param);
        }

        public Task<ReadDataMapper?> LocalCreateTaskBool1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCallBoolAsync<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBool(param));
        }

        public virtual Task<ReadDataMapper?> CreateTaskBoolFalse(int? param)
        {
            return LocalCreateTaskBoolFalse(param);
        }

        public Task<ReadDataMapper?> LocalCreateTaskBoolFalse(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCallBoolAsync<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolFalse(param));
        }

        public virtual ReadDataMapper CreateVoidDep()
        {
            return LocalCreateVoidDep();
        }

        public ReadDataMapper LocalCreateVoidDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoidDep(disposableDependency));
        }

        public virtual ReadDataMapper? CreateBoolTrueDep()
        {
            return LocalCreateBoolTrueDep();
        }

        public ReadDataMapper? LocalCreateBoolTrueDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolTrueDep(disposableDependency));
        }

        public virtual ReadDataMapper? CreateBoolFalseDep()
        {
            return LocalCreateBoolFalseDep();
        }

        public ReadDataMapper? LocalCreateBoolFalseDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolFalseDep(disposableDependency));
        }

        public virtual Task<ReadDataMapper> CreateTaskDep()
        {
            return LocalCreateTaskDep();
        }

        public Task<ReadDataMapper> LocalCreateTaskDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskDep(disposableDependency));
        }

        public virtual Task<ReadDataMapper?> CreateTaskBoolDep()
        {
            return LocalCreateTaskBoolDep();
        }

        public Task<ReadDataMapper?> LocalCreateTaskBoolDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolDep(disposableDependency));
        }

        public virtual Task<ReadDataMapper?> CreateTaskBoolFalseDep()
        {
            return LocalCreateTaskBoolFalseDep();
        }

        public Task<ReadDataMapper?> LocalCreateTaskBoolFalseDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolFalseDep(disposableDependency));
        }

        public virtual ReadDataMapper CreateVoidDep(int? param)
        {
            return LocalCreateVoidDep1(param);
        }

        public ReadDataMapper LocalCreateVoidDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoidDep(param, disposableDependency));
        }

        public virtual ReadDataMapper? CreateBoolTrueDep(int? param)
        {
            return LocalCreateBoolTrueDep1(param);
        }

        public ReadDataMapper? LocalCreateBoolTrueDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolTrueDep(param, disposableDependency));
        }

        public virtual ReadDataMapper? CreateBoolFalseDep(int? param)
        {
            return LocalCreateBoolFalseDep1(param);
        }

        public ReadDataMapper? LocalCreateBoolFalseDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolFalseDep(param, disposableDependency));
        }

        public virtual Task<ReadDataMapper> CreateTaskDep(int? param)
        {
            return LocalCreateTaskDep1(param);
        }

        public Task<ReadDataMapper> LocalCreateTaskDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskDep(param, disposableDependency));
        }

        public virtual Task<ReadDataMapper?> CreateTaskBoolDep(int? param)
        {
            return LocalCreateTaskBoolDep1(param);
        }

        public Task<ReadDataMapper?> LocalCreateTaskBoolDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<ReadDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolDep(param, disposableDependency));
        }

        public virtual ReadDataMapper FetchVoid()
        {
            return LocalFetchVoid();
        }

        public ReadDataMapper LocalFetchVoid()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCall<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoid());
        }

        public virtual ReadDataMapper? FetchBool()
        {
            return LocalFetchBool();
        }

        public ReadDataMapper? LocalFetchBool()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCallBool<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBool());
        }

        public virtual Task<ReadDataMapper> FetchTask()
        {
            return LocalFetchTask();
        }

        public Task<ReadDataMapper> LocalFetchTask()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCallAsync<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTask());
        }

        public virtual Task<ReadDataMapper?> FetchTaskBool()
        {
            return LocalFetchTaskBool();
        }

        public Task<ReadDataMapper?> LocalFetchTaskBool()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCallBoolAsync<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBool());
        }

        public virtual ReadDataMapper FetchVoid(int? param)
        {
            return LocalFetchVoid1(param);
        }

        public ReadDataMapper LocalFetchVoid1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCall<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoid(param));
        }

        public virtual ReadDataMapper? FetchBool(int? param)
        {
            return LocalFetchBool1(param);
        }

        public ReadDataMapper? LocalFetchBool1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCallBool<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBool(param));
        }

        public virtual Task<ReadDataMapper> FetchTask(int? param)
        {
            return LocalFetchTask1(param);
        }

        public Task<ReadDataMapper> LocalFetchTask1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCallAsync<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTask(param));
        }

        public virtual Task<ReadDataMapper?> FetchTaskBool(int? param)
        {
            return LocalFetchTaskBool1(param);
        }

        public Task<ReadDataMapper?> LocalFetchTaskBool1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            return DoMapperMethodCallBoolAsync<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBool(param));
        }

        public virtual ReadDataMapper FetchVoidDep()
        {
            return LocalFetchVoidDep();
        }

        public ReadDataMapper LocalFetchVoidDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoidDep(disposableDependency));
        }

        public virtual ReadDataMapper? FetchBoolTrueDep()
        {
            return LocalFetchBoolTrueDep();
        }

        public ReadDataMapper? LocalFetchBoolTrueDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolTrueDep(disposableDependency));
        }

        public virtual ReadDataMapper? FetchBoolFalseDep()
        {
            return LocalFetchBoolFalseDep();
        }

        public ReadDataMapper? LocalFetchBoolFalseDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolFalseDep(disposableDependency));
        }

        public virtual Task<ReadDataMapper> FetchTaskDep()
        {
            return LocalFetchTaskDep();
        }

        public Task<ReadDataMapper> LocalFetchTaskDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskDep(disposableDependency));
        }

        public virtual Task<ReadDataMapper?> FetchTaskBoolDep()
        {
            return LocalFetchTaskBoolDep();
        }

        public Task<ReadDataMapper?> LocalFetchTaskBoolDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolDep(disposableDependency));
        }

        public virtual ReadDataMapper FetchVoidDep(int? param)
        {
            return LocalFetchVoidDep1(param);
        }

        public ReadDataMapper LocalFetchVoidDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoidDep(param, disposableDependency));
        }

        public virtual ReadDataMapper? FetchBoolTrueDep(int? param)
        {
            return LocalFetchBoolTrueDep1(param);
        }

        public ReadDataMapper? LocalFetchBoolTrueDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolTrueDep(param, disposableDependency));
        }

        public virtual ReadDataMapper? FetchBoolFalseDep(int? param)
        {
            return LocalFetchBoolFalseDep1(param);
        }

        public ReadDataMapper? LocalFetchBoolFalseDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolFalseDep(param, disposableDependency));
        }

        public virtual Task<ReadDataMapper> FetchTaskDep(int? param)
        {
            return LocalFetchTaskDep1(param);
        }

        public Task<ReadDataMapper> LocalFetchTaskDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskDep(param, disposableDependency));
        }

        public virtual Task<ReadDataMapper?> FetchTaskBoolDep(int? param)
        {
            return LocalFetchTaskBoolDep1(param);
        }

        public Task<ReadDataMapper?> LocalFetchTaskBoolDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolDep(param, disposableDependency));
        }

        public virtual Task<ReadDataMapper?> FetchTaskBoolFalseDep(int? param)
        {
            return LocalFetchTaskBoolFalseDep(param);
        }

        public Task<ReadDataMapper?> LocalFetchTaskBoolFalseDep(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<ReadDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolFalseDep(param, disposableDependency));
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<ReadDataMapper>();
            services.AddScoped<ReadDataMapperFactory>();
            services.AddScoped<IReadDataMapperFactory, ReadDataMapperFactory>();
        }
    }
}