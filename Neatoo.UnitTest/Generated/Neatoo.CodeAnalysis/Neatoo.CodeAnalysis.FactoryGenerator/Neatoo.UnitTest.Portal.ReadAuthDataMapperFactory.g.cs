using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using static Neatoo.UnitTest.RemoteFactory.ReadAuthTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.AuthorizationRules;
using Neatoo.UnitTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Neatoo.UnitTest.RemoteFactory.ReadRemoteAuthTests;

/*
                    Debugging Messages:
                    Parent class: ReadAuthTests
: ReadTests.ReadDataMapper

No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No MethodDeclarationSyntax for get_CanReadCalled
No MethodDeclarationSyntax for set_CanReadCalled
No MethodDeclarationSyntax for get_CanCreateCalled
No MethodDeclarationSyntax for set_CanCreateCalled
No MethodDeclarationSyntax for get_CanFetchCalled
No MethodDeclarationSyntax for set_CanFetchCalled
No MethodDeclarationSyntax for .ctor
No MethodDeclarationSyntax for .ctor
No MethodDeclarationSyntax for Equals
No MethodDeclarationSyntax for Equals
No MethodDeclarationSyntax for Finalize
No MethodDeclarationSyntax for GetHashCode
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No MethodDeclarationSyntax for ReferenceEquals
No MethodDeclarationSyntax for ToString
                    */
namespace Neatoo.UnitTest.RemoteFactory
{
    public interface IReadAuthDataMapperFactory
    {
        ReadAuthDataMapper CreateVoid();
        ReadAuthDataMapper? CreateBool();
        Task<ReadAuthDataMapper> CreateTask();
        Task<ReadAuthDataMapper?> CreateTaskBool();
        ReadAuthDataMapper CreateVoid(int? param);
        ReadAuthDataMapper? CreateBool(int? param);
        Task<ReadAuthDataMapper> CreateTask(int? param);
        Task<ReadAuthDataMapper?> CreateTaskBool(int? param);
        Task<ReadAuthDataMapper?> CreateTaskBoolFalse(int? param);
        ReadAuthDataMapper CreateVoidDep();
        ReadAuthDataMapper? CreateBoolTrueDep();
        ReadAuthDataMapper? CreateBoolFalseDep();
        Task<ReadAuthDataMapper> CreateTaskDep();
        Task<ReadAuthDataMapper?> CreateTaskBoolDep();
        Task<ReadAuthDataMapper?> CreateTaskBoolFalseDep();
        ReadAuthDataMapper CreateVoidDep(int? param);
        ReadAuthDataMapper? CreateBoolTrueDep(int? param);
        ReadAuthDataMapper? CreateBoolFalseDep(int? param);
        Task<ReadAuthDataMapper> CreateTaskDep(int? param);
        Task<ReadAuthDataMapper?> CreateTaskBoolDep(int? param);
        ReadAuthDataMapper FetchVoid();
        ReadAuthDataMapper? FetchBool();
        Task<ReadAuthDataMapper> FetchTask();
        Task<ReadAuthDataMapper?> FetchTaskBool();
        ReadAuthDataMapper FetchVoid(int? param);
        ReadAuthDataMapper? FetchBool(int? param);
        Task<ReadAuthDataMapper> FetchTask(int? param);
        Task<ReadAuthDataMapper?> FetchTaskBool(int? param);
        ReadAuthDataMapper FetchVoidDep();
        ReadAuthDataMapper? FetchBoolTrueDep();
        ReadAuthDataMapper? FetchBoolFalseDep();
        Task<ReadAuthDataMapper> FetchTaskDep();
        Task<ReadAuthDataMapper?> FetchTaskBoolDep();
        ReadAuthDataMapper FetchVoidDep(int? param);
        ReadAuthDataMapper? FetchBoolTrueDep(int? param);
        ReadAuthDataMapper? FetchBoolFalseDep(int? param);
        Task<ReadAuthDataMapper> FetchTaskDep(int? param);
        Task<ReadAuthDataMapper?> FetchTaskBoolDep(int? param);
        Task<ReadAuthDataMapper?> FetchTaskBoolFalseDep(int? param);
        Authorized CanCreateVoid();
        Authorized CanCreateBool();
        Authorized CanCreateTask();
        Authorized CanCreateTaskBool();
        Authorized CanCreateVoid(int? param);
        Authorized CanCreateBool(int? param);
        Authorized CanCreateTask(int? param);
        Authorized CanCreateTaskBool(int? param);
        Authorized CanCreateTaskBoolFalse(int? param);
        Authorized CanCreateVoidDep();
        Authorized CanCreateBoolTrueDep();
        Authorized CanCreateBoolFalseDep();
        Authorized CanCreateTaskDep();
        Authorized CanCreateTaskBoolDep();
        Authorized CanCreateTaskBoolFalseDep();
        Authorized CanCreateVoidDep(int? param);
        Authorized CanCreateBoolTrueDep(int? param);
        Authorized CanCreateBoolFalseDep(int? param);
        Authorized CanCreateTaskDep(int? param);
        Authorized CanCreateTaskBoolDep(int? param);
        Authorized CanFetchVoid();
        Authorized CanFetchBool();
        Authorized CanFetchTask();
        Authorized CanFetchTaskBool();
        Authorized CanFetchVoid(int? param);
        Authorized CanFetchBool(int? param);
        Authorized CanFetchTask(int? param);
        Authorized CanFetchTaskBool(int? param);
        Authorized CanFetchVoidDep();
        Authorized CanFetchBoolTrueDep();
        Authorized CanFetchBoolFalseDep();
        Authorized CanFetchTaskDep();
        Authorized CanFetchTaskBoolDep();
        Authorized CanFetchVoidDep(int? param);
        Authorized CanFetchBoolTrueDep(int? param);
        Authorized CanFetchBoolFalseDep(int? param);
        Authorized CanFetchTaskDep(int? param);
        Authorized CanFetchTaskBoolDep(int? param);
        Authorized CanFetchTaskBoolFalseDep(int? param);
    }

    internal class ReadAuthDataMapperFactory : FactoryBase, IReadAuthDataMapperFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public ReadAuth ReadAuth { get; }

        public ReadAuthDataMapperFactory(IServiceProvider serviceProvider, ReadAuth readauth)
        {
            this.ServiceProvider = serviceProvider;
            this.ReadAuth = readauth;
        }

        public ReadAuthDataMapperFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate, ReadAuth readauth)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
            this.ReadAuth = readauth;
        }

        public virtual ReadAuthDataMapper CreateVoid()
        {
            return (LocalCreateVoid()).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalCreateVoid()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCall<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoid()));
        }

        public virtual ReadAuthDataMapper? CreateBool()
        {
            return (LocalCreateBool()).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalCreateBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCallBool<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateBool()));
        }

        public virtual async Task<ReadAuthDataMapper> CreateTask()
        {
            return (await LocalCreateTask()).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalCreateTask()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallAsync<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTask()));
        }

        public virtual async Task<ReadAuthDataMapper?> CreateTaskBool()
        {
            return (await LocalCreateTaskBool()).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalCreateTaskBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBool()));
        }

        public virtual ReadAuthDataMapper CreateVoid(int? param)
        {
            return (LocalCreateVoid1(param)).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalCreateVoid1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCall<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoid(param)));
        }

        public virtual ReadAuthDataMapper? CreateBool(int? param)
        {
            return (LocalCreateBool1(param)).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalCreateBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCallBool<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateBool(param)));
        }

        public virtual async Task<ReadAuthDataMapper> CreateTask(int? param)
        {
            return (await LocalCreateTask1(param)).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalCreateTask1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallAsync<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTask(param)));
        }

        public virtual async Task<ReadAuthDataMapper?> CreateTaskBool(int? param)
        {
            return (await LocalCreateTaskBool1(param)).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalCreateTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBool(param)));
        }

        public virtual async Task<ReadAuthDataMapper?> CreateTaskBoolFalse(int? param)
        {
            return (await LocalCreateTaskBoolFalse(param)).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalCreateTaskBoolFalse(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolFalse(param)));
        }

        public virtual ReadAuthDataMapper CreateVoidDep()
        {
            return (LocalCreateVoidDep()).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalCreateVoidDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCall<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoidDep(disposableDependency)));
        }

        public virtual ReadAuthDataMapper? CreateBoolTrueDep()
        {
            return (LocalCreateBoolTrueDep()).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalCreateBoolTrueDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCallBool<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolTrueDep(disposableDependency)));
        }

        public virtual ReadAuthDataMapper? CreateBoolFalseDep()
        {
            return (LocalCreateBoolFalseDep()).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalCreateBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCallBool<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolFalseDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthDataMapper> CreateTaskDep()
        {
            return (await LocalCreateTaskDep()).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalCreateTaskDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallAsync<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthDataMapper?> CreateTaskBoolDep()
        {
            return (await LocalCreateTaskBoolDep()).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalCreateTaskBoolDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthDataMapper?> CreateTaskBoolFalseDep()
        {
            return (await LocalCreateTaskBoolFalseDep()).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalCreateTaskBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolFalseDep(disposableDependency)));
        }

        public virtual ReadAuthDataMapper CreateVoidDep(int? param)
        {
            return (LocalCreateVoidDep1(param)).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalCreateVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCall<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoidDep(param, disposableDependency)));
        }

        public virtual ReadAuthDataMapper? CreateBoolTrueDep(int? param)
        {
            return (LocalCreateBoolTrueDep1(param)).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalCreateBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCallBool<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolTrueDep(param, disposableDependency)));
        }

        public virtual ReadAuthDataMapper? CreateBoolFalseDep(int? param)
        {
            return (LocalCreateBoolFalseDep1(param)).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalCreateBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCallBool<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolFalseDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthDataMapper> CreateTaskDep(int? param)
        {
            return (await LocalCreateTaskDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalCreateTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallAsync<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthDataMapper?> CreateTaskBoolDep(int? param)
        {
            return (await LocalCreateTaskBoolDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalCreateTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolDep(param, disposableDependency)));
        }

        public virtual ReadAuthDataMapper FetchVoid()
        {
            return (LocalFetchVoid()).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalFetchVoid()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCall<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoid()));
        }

        public virtual ReadAuthDataMapper? FetchBool()
        {
            return (LocalFetchBool()).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalFetchBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCallBool<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBool()));
        }

        public virtual async Task<ReadAuthDataMapper> FetchTask()
        {
            return (await LocalFetchTask()).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalFetchTask()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallAsync<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTask()));
        }

        public virtual async Task<ReadAuthDataMapper?> FetchTaskBool()
        {
            return (await LocalFetchTaskBool()).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalFetchTaskBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBool()));
        }

        public virtual ReadAuthDataMapper FetchVoid(int? param)
        {
            return (LocalFetchVoid1(param)).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalFetchVoid1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCall<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoid(param)));
        }

        public virtual ReadAuthDataMapper? FetchBool(int? param)
        {
            return (LocalFetchBool1(param)).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalFetchBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCallBool<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBool(param)));
        }

        public virtual async Task<ReadAuthDataMapper> FetchTask(int? param)
        {
            return (await LocalFetchTask1(param)).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalFetchTask1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallAsync<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTask(param)));
        }

        public virtual async Task<ReadAuthDataMapper?> FetchTaskBool(int? param)
        {
            return (await LocalFetchTaskBool1(param)).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalFetchTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBool(param)));
        }

        public virtual ReadAuthDataMapper FetchVoidDep()
        {
            return (LocalFetchVoidDep()).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalFetchVoidDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCall<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoidDep(disposableDependency)));
        }

        public virtual ReadAuthDataMapper? FetchBoolTrueDep()
        {
            return (LocalFetchBoolTrueDep()).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalFetchBoolTrueDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCallBool<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolTrueDep(disposableDependency)));
        }

        public virtual ReadAuthDataMapper? FetchBoolFalseDep()
        {
            return (LocalFetchBoolFalseDep()).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalFetchBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCallBool<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolFalseDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthDataMapper> FetchTaskDep()
        {
            return (await LocalFetchTaskDep()).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalFetchTaskDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallAsync<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthDataMapper?> FetchTaskBoolDep()
        {
            return (await LocalFetchTaskBoolDep()).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalFetchTaskBoolDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolDep(disposableDependency)));
        }

        public virtual ReadAuthDataMapper FetchVoidDep(int? param)
        {
            return (LocalFetchVoidDep1(param)).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalFetchVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCall<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoidDep(param, disposableDependency)));
        }

        public virtual ReadAuthDataMapper? FetchBoolTrueDep(int? param)
        {
            return (LocalFetchBoolTrueDep1(param)).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalFetchBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCallBool<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolTrueDep(param, disposableDependency)));
        }

        public virtual ReadAuthDataMapper? FetchBoolFalseDep(int? param)
        {
            return (LocalFetchBoolFalseDep1(param)).Result;
        }

        public Authorized<ReadAuthDataMapper> LocalFetchBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(DoMapperMethodCallBool<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolFalseDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthDataMapper> FetchTaskDep(int? param)
        {
            return (await LocalFetchTaskDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalFetchTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallAsync<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthDataMapper?> FetchTaskBoolDep(int? param)
        {
            return (await LocalFetchTaskBoolDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalFetchTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthDataMapper?> FetchTaskBoolFalseDep(int? param)
        {
            return (await LocalFetchTaskBoolFalseDep(param)).Result;
        }

        public async Task<Authorized<ReadAuthDataMapper>> LocalFetchTaskBoolFalseDep(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolFalseDep(param, disposableDependency)));
        }

        public virtual Authorized CanCreateVoid()
        {
            return LocalCanCreateVoid();
        }

        public Authorized LocalCanCreateVoid()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateBool()
        {
            return LocalCanCreateBool();
        }

        public Authorized LocalCanCreateBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTask()
        {
            return LocalCanCreateTask();
        }

        public Authorized LocalCanCreateTask()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskBool()
        {
            return LocalCanCreateTaskBool();
        }

        public Authorized LocalCanCreateTaskBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateVoid(int? param)
        {
            return LocalCanCreateVoid1(param);
        }

        public Authorized LocalCanCreateVoid1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateBool(int? param)
        {
            return LocalCanCreateBool1(param);
        }

        public Authorized LocalCanCreateBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTask(int? param)
        {
            return LocalCanCreateTask1(param);
        }

        public Authorized LocalCanCreateTask1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskBool(int? param)
        {
            return LocalCanCreateTaskBool1(param);
        }

        public Authorized LocalCanCreateTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskBoolFalse(int? param)
        {
            return LocalCanCreateTaskBoolFalse(param);
        }

        public Authorized LocalCanCreateTaskBoolFalse(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateVoidDep()
        {
            return LocalCanCreateVoidDep();
        }

        public Authorized LocalCanCreateVoidDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateBoolTrueDep()
        {
            return LocalCanCreateBoolTrueDep();
        }

        public Authorized LocalCanCreateBoolTrueDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateBoolFalseDep()
        {
            return LocalCanCreateBoolFalseDep();
        }

        public Authorized LocalCanCreateBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskDep()
        {
            return LocalCanCreateTaskDep();
        }

        public Authorized LocalCanCreateTaskDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskBoolDep()
        {
            return LocalCanCreateTaskBoolDep();
        }

        public Authorized LocalCanCreateTaskBoolDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskBoolFalseDep()
        {
            return LocalCanCreateTaskBoolFalseDep();
        }

        public Authorized LocalCanCreateTaskBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateVoidDep(int? param)
        {
            return LocalCanCreateVoidDep1(param);
        }

        public Authorized LocalCanCreateVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateBoolTrueDep(int? param)
        {
            return LocalCanCreateBoolTrueDep1(param);
        }

        public Authorized LocalCanCreateBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateBoolFalseDep(int? param)
        {
            return LocalCanCreateBoolFalseDep1(param);
        }

        public Authorized LocalCanCreateBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskDep(int? param)
        {
            return LocalCanCreateTaskDep1(param);
        }

        public Authorized LocalCanCreateTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskBoolDep(int? param)
        {
            return LocalCanCreateTaskBoolDep1(param);
        }

        public Authorized LocalCanCreateTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchVoid()
        {
            return LocalCanFetchVoid();
        }

        public Authorized LocalCanFetchVoid()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchBool()
        {
            return LocalCanFetchBool();
        }

        public Authorized LocalCanFetchBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTask()
        {
            return LocalCanFetchTask();
        }

        public Authorized LocalCanFetchTask()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskBool()
        {
            return LocalCanFetchTaskBool();
        }

        public Authorized LocalCanFetchTaskBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchVoid(int? param)
        {
            return LocalCanFetchVoid1(param);
        }

        public Authorized LocalCanFetchVoid1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchBool(int? param)
        {
            return LocalCanFetchBool1(param);
        }

        public Authorized LocalCanFetchBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTask(int? param)
        {
            return LocalCanFetchTask1(param);
        }

        public Authorized LocalCanFetchTask1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskBool(int? param)
        {
            return LocalCanFetchTaskBool1(param);
        }

        public Authorized LocalCanFetchTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchVoidDep()
        {
            return LocalCanFetchVoidDep();
        }

        public Authorized LocalCanFetchVoidDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchBoolTrueDep()
        {
            return LocalCanFetchBoolTrueDep();
        }

        public Authorized LocalCanFetchBoolTrueDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchBoolFalseDep()
        {
            return LocalCanFetchBoolFalseDep();
        }

        public Authorized LocalCanFetchBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskDep()
        {
            return LocalCanFetchTaskDep();
        }

        public Authorized LocalCanFetchTaskDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskBoolDep()
        {
            return LocalCanFetchTaskBoolDep();
        }

        public Authorized LocalCanFetchTaskBoolDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchVoidDep(int? param)
        {
            return LocalCanFetchVoidDep1(param);
        }

        public Authorized LocalCanFetchVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchBoolTrueDep(int? param)
        {
            return LocalCanFetchBoolTrueDep1(param);
        }

        public Authorized LocalCanFetchBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchBoolFalseDep(int? param)
        {
            return LocalCanFetchBoolFalseDep1(param);
        }

        public Authorized LocalCanFetchBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskDep(int? param)
        {
            return LocalCanFetchTaskDep1(param);
        }

        public Authorized LocalCanFetchTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskBoolDep(int? param)
        {
            return LocalCanFetchTaskBoolDep1(param);
        }

        public Authorized LocalCanFetchTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskBoolFalseDep(int? param)
        {
            return LocalCanFetchTaskBoolFalseDep(param);
        }

        public Authorized LocalCanFetchTaskBoolFalseDep(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<ReadAuthDataMapper>();
            services.AddScoped<ReadAuthDataMapperFactory>();
            services.AddScoped<IReadAuthDataMapperFactory, ReadAuthDataMapperFactory>();
        }
    }
}