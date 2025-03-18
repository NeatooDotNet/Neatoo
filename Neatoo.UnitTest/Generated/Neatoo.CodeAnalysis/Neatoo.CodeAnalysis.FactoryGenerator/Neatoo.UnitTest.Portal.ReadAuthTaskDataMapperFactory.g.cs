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
No MethodDeclarationSyntax for .ctor
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
    public interface IReadAuthTaskDataMapperFactory
    {
        Task<ReadAuthTaskDataMapper> CreateVoid();
        Task<ReadAuthTaskDataMapper?> CreateBool();
        Task<ReadAuthTaskDataMapper> CreateTask();
        Task<ReadAuthTaskDataMapper?> CreateTaskBool();
        Task<ReadAuthTaskDataMapper> CreateVoid(int? param);
        Task<ReadAuthTaskDataMapper?> CreateBool(int? param);
        Task<ReadAuthTaskDataMapper> CreateTask(int? param);
        Task<ReadAuthTaskDataMapper?> CreateTaskBool(int? param);
        Task<ReadAuthTaskDataMapper?> CreateTaskBoolFalse(int? param);
        Task<ReadAuthTaskDataMapper> CreateVoidDep();
        Task<ReadAuthTaskDataMapper?> CreateBoolTrueDep();
        Task<ReadAuthTaskDataMapper?> CreateBoolFalseDep();
        Task<ReadAuthTaskDataMapper> CreateTaskDep();
        Task<ReadAuthTaskDataMapper?> CreateTaskBoolDep();
        Task<ReadAuthTaskDataMapper?> CreateTaskBoolFalseDep();
        Task<ReadAuthTaskDataMapper> CreateVoidDep(int? param);
        Task<ReadAuthTaskDataMapper?> CreateBoolTrueDep(int? param);
        Task<ReadAuthTaskDataMapper?> CreateBoolFalseDep(int? param);
        Task<ReadAuthTaskDataMapper> CreateTaskDep(int? param);
        Task<ReadAuthTaskDataMapper?> CreateTaskBoolDep(int? param);
        Task<ReadAuthTaskDataMapper> FetchVoid();
        Task<ReadAuthTaskDataMapper?> FetchBool();
        Task<ReadAuthTaskDataMapper> FetchTask();
        Task<ReadAuthTaskDataMapper?> FetchTaskBool();
        Task<ReadAuthTaskDataMapper> FetchVoid(int? param);
        Task<ReadAuthTaskDataMapper?> FetchBool(int? param);
        Task<ReadAuthTaskDataMapper> FetchTask(int? param);
        Task<ReadAuthTaskDataMapper?> FetchTaskBool(int? param);
        Task<ReadAuthTaskDataMapper> FetchVoidDep();
        Task<ReadAuthTaskDataMapper?> FetchBoolTrueDep();
        Task<ReadAuthTaskDataMapper?> FetchBoolFalseDep();
        Task<ReadAuthTaskDataMapper> FetchTaskDep();
        Task<ReadAuthTaskDataMapper?> FetchTaskBoolDep();
        Task<ReadAuthTaskDataMapper> FetchVoidDep(int? param);
        Task<ReadAuthTaskDataMapper?> FetchBoolTrueDep(int? param);
        Task<ReadAuthTaskDataMapper?> FetchBoolFalseDep(int? param);
        Task<ReadAuthTaskDataMapper> FetchTaskDep(int? param);
        Task<ReadAuthTaskDataMapper?> FetchTaskBoolDep(int? param);
        Task<ReadAuthTaskDataMapper?> FetchTaskBoolFalseDep(int? param);
        Task<Authorized> CanCreateVoid();
        Task<Authorized> CanCreateBool();
        Task<Authorized> CanCreateTask();
        Task<Authorized> CanCreateTaskBool();
        Task<Authorized> CanCreateVoid(int? param);
        Task<Authorized> CanCreateBool(int? param);
        Task<Authorized> CanCreateTask(int? param);
        Task<Authorized> CanCreateTaskBool(int? param);
        Task<Authorized> CanCreateTaskBoolFalse(int? param);
        Task<Authorized> CanCreateVoidDep();
        Task<Authorized> CanCreateBoolTrueDep();
        Task<Authorized> CanCreateBoolFalseDep();
        Task<Authorized> CanCreateTaskDep();
        Task<Authorized> CanCreateTaskBoolDep();
        Task<Authorized> CanCreateTaskBoolFalseDep();
        Task<Authorized> CanCreateVoidDep(int? param);
        Task<Authorized> CanCreateBoolTrueDep(int? param);
        Task<Authorized> CanCreateBoolFalseDep(int? param);
        Task<Authorized> CanCreateTaskDep(int? param);
        Task<Authorized> CanCreateTaskBoolDep(int? param);
        Task<Authorized> CanFetchVoid();
        Task<Authorized> CanFetchBool();
        Task<Authorized> CanFetchTask();
        Task<Authorized> CanFetchTaskBool();
        Task<Authorized> CanFetchVoid(int? param);
        Task<Authorized> CanFetchBool(int? param);
        Task<Authorized> CanFetchTask(int? param);
        Task<Authorized> CanFetchTaskBool(int? param);
        Task<Authorized> CanFetchVoidDep();
        Task<Authorized> CanFetchBoolTrueDep();
        Task<Authorized> CanFetchBoolFalseDep();
        Task<Authorized> CanFetchTaskDep();
        Task<Authorized> CanFetchTaskBoolDep();
        Task<Authorized> CanFetchVoidDep(int? param);
        Task<Authorized> CanFetchBoolTrueDep(int? param);
        Task<Authorized> CanFetchBoolFalseDep(int? param);
        Task<Authorized> CanFetchTaskDep(int? param);
        Task<Authorized> CanFetchTaskBoolDep(int? param);
        Task<Authorized> CanFetchTaskBoolFalseDep(int? param);
    }

    internal class ReadAuthTaskDataMapperFactory : FactoryBase, IReadAuthTaskDataMapperFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public ReadAuthTask ReadAuthTask { get; }
        public ReadAuth ReadAuth { get; }

        public ReadAuthTaskDataMapperFactory(IServiceProvider serviceProvider, ReadAuthTask readauthtask, ReadAuth readauth)
        {
            this.ServiceProvider = serviceProvider;
            this.ReadAuthTask = readauthtask;
            this.ReadAuth = readauth;
        }

        public ReadAuthTaskDataMapperFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate, ReadAuthTask readauthtask, ReadAuth readauth)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
            this.ReadAuthTask = readauthtask;
            this.ReadAuth = readauth;
        }

        public virtual async Task<ReadAuthTaskDataMapper> CreateVoid()
        {
            return (await LocalCreateVoid()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateVoid()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCall<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoid()));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> CreateBool()
        {
            return (await LocalCreateBool()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCallBool<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateBool()));
        }

        public virtual async Task<ReadAuthTaskDataMapper> CreateTask()
        {
            return (await LocalCreateTask()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateTask()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTask()));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> CreateTaskBool()
        {
            return (await LocalCreateTaskBool()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateTaskBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBool()));
        }

        public virtual async Task<ReadAuthTaskDataMapper> CreateVoid(int? param)
        {
            return (await LocalCreateVoid1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateVoid1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCall<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoid(param)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> CreateBool(int? param)
        {
            return (await LocalCreateBool1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCallBool<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateBool(param)));
        }

        public virtual async Task<ReadAuthTaskDataMapper> CreateTask(int? param)
        {
            return (await LocalCreateTask1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateTask1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTask(param)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> CreateTaskBool(int? param)
        {
            return (await LocalCreateTaskBool1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBool(param)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> CreateTaskBoolFalse(int? param)
        {
            return (await LocalCreateTaskBoolFalse(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateTaskBoolFalse(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolFalse(param)));
        }

        public virtual async Task<ReadAuthTaskDataMapper> CreateVoidDep()
        {
            return (await LocalCreateVoidDep()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateVoidDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCall<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoidDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> CreateBoolTrueDep()
        {
            return (await LocalCreateBoolTrueDep()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateBoolTrueDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCallBool<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolTrueDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> CreateBoolFalseDep()
        {
            return (await LocalCreateBoolFalseDep()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCallBool<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolFalseDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper> CreateTaskDep()
        {
            return (await LocalCreateTaskDep()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateTaskDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> CreateTaskBoolDep()
        {
            return (await LocalCreateTaskBoolDep()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateTaskBoolDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> CreateTaskBoolFalseDep()
        {
            return (await LocalCreateTaskBoolFalseDep()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateTaskBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolFalseDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper> CreateVoidDep(int? param)
        {
            return (await LocalCreateVoidDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCall<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoidDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> CreateBoolTrueDep(int? param)
        {
            return (await LocalCreateBoolTrueDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCallBool<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolTrueDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> CreateBoolFalseDep(int? param)
        {
            return (await LocalCreateBoolFalseDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCallBool<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolFalseDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper> CreateTaskDep(int? param)
        {
            return (await LocalCreateTaskDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> CreateTaskBoolDep(int? param)
        {
            return (await LocalCreateTaskBoolDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalCreateTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper> FetchVoid()
        {
            return (await LocalFetchVoid()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchVoid()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCall<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoid()));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> FetchBool()
        {
            return (await LocalFetchBool()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCallBool<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBool()));
        }

        public virtual async Task<ReadAuthTaskDataMapper> FetchTask()
        {
            return (await LocalFetchTask()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchTask()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTask()));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> FetchTaskBool()
        {
            return (await LocalFetchTaskBool()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchTaskBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBool()));
        }

        public virtual async Task<ReadAuthTaskDataMapper> FetchVoid(int? param)
        {
            return (await LocalFetchVoid1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchVoid1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCall<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoid(param)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> FetchBool(int? param)
        {
            return (await LocalFetchBool1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCallBool<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBool(param)));
        }

        public virtual async Task<ReadAuthTaskDataMapper> FetchTask(int? param)
        {
            return (await LocalFetchTask1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchTask1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTask(param)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> FetchTaskBool(int? param)
        {
            return (await LocalFetchTaskBool1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBool(param)));
        }

        public virtual async Task<ReadAuthTaskDataMapper> FetchVoidDep()
        {
            return (await LocalFetchVoidDep()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchVoidDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCall<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoidDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> FetchBoolTrueDep()
        {
            return (await LocalFetchBoolTrueDep()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchBoolTrueDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCallBool<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolTrueDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> FetchBoolFalseDep()
        {
            return (await LocalFetchBoolFalseDep()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCallBool<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolFalseDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper> FetchTaskDep()
        {
            return (await LocalFetchTaskDep()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchTaskDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> FetchTaskBoolDep()
        {
            return (await LocalFetchTaskBoolDep()).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchTaskBoolDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolDep(disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper> FetchVoidDep(int? param)
        {
            return (await LocalFetchVoidDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCall<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoidDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> FetchBoolTrueDep(int? param)
        {
            return (await LocalFetchBoolTrueDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCallBool<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolTrueDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> FetchBoolFalseDep(int? param)
        {
            return (await LocalFetchBoolFalseDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(DoMapperMethodCallBool<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolFalseDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper> FetchTaskDep(int? param)
        {
            return (await LocalFetchTaskDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> FetchTaskBoolDep(int? param)
        {
            return (await LocalFetchTaskBoolDep1(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolDep(param, disposableDependency)));
        }

        public virtual async Task<ReadAuthTaskDataMapper?> FetchTaskBoolFalseDep(int? param)
        {
            return (await LocalFetchTaskBoolFalseDep(param)).Result;
        }

        public async Task<Authorized<ReadAuthTaskDataMapper>> LocalFetchTaskBoolFalseDep(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolFalseDep(param, disposableDependency)));
        }

        public virtual Task<Authorized> CanCreateVoid()
        {
            return LocalCanCreateVoid();
        }

        public async Task<Authorized> LocalCanCreateVoid()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateBool()
        {
            return LocalCanCreateBool();
        }

        public async Task<Authorized> LocalCanCreateBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTask()
        {
            return LocalCanCreateTask();
        }

        public async Task<Authorized> LocalCanCreateTask()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskBool()
        {
            return LocalCanCreateTaskBool();
        }

        public async Task<Authorized> LocalCanCreateTaskBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateVoid(int? param)
        {
            return LocalCanCreateVoid1(param);
        }

        public async Task<Authorized> LocalCanCreateVoid1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateBool(int? param)
        {
            return LocalCanCreateBool1(param);
        }

        public async Task<Authorized> LocalCanCreateBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTask(int? param)
        {
            return LocalCanCreateTask1(param);
        }

        public async Task<Authorized> LocalCanCreateTask1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskBool(int? param)
        {
            return LocalCanCreateTaskBool1(param);
        }

        public async Task<Authorized> LocalCanCreateTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskBoolFalse(int? param)
        {
            return LocalCanCreateTaskBoolFalse(param);
        }

        public async Task<Authorized> LocalCanCreateTaskBoolFalse(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateVoidDep()
        {
            return LocalCanCreateVoidDep();
        }

        public async Task<Authorized> LocalCanCreateVoidDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateBoolTrueDep()
        {
            return LocalCanCreateBoolTrueDep();
        }

        public async Task<Authorized> LocalCanCreateBoolTrueDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateBoolFalseDep()
        {
            return LocalCanCreateBoolFalseDep();
        }

        public async Task<Authorized> LocalCanCreateBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskDep()
        {
            return LocalCanCreateTaskDep();
        }

        public async Task<Authorized> LocalCanCreateTaskDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskBoolDep()
        {
            return LocalCanCreateTaskBoolDep();
        }

        public async Task<Authorized> LocalCanCreateTaskBoolDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskBoolFalseDep()
        {
            return LocalCanCreateTaskBoolFalseDep();
        }

        public async Task<Authorized> LocalCanCreateTaskBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateVoidDep(int? param)
        {
            return LocalCanCreateVoidDep1(param);
        }

        public async Task<Authorized> LocalCanCreateVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateBoolTrueDep(int? param)
        {
            return LocalCanCreateBoolTrueDep1(param);
        }

        public async Task<Authorized> LocalCanCreateBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateBoolFalseDep(int? param)
        {
            return LocalCanCreateBoolFalseDep1(param);
        }

        public async Task<Authorized> LocalCanCreateBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskDep(int? param)
        {
            return LocalCanCreateTaskDep1(param);
        }

        public async Task<Authorized> LocalCanCreateTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskBoolDep(int? param)
        {
            return LocalCanCreateTaskBoolDep1(param);
        }

        public async Task<Authorized> LocalCanCreateTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchVoid()
        {
            return LocalCanFetchVoid();
        }

        public async Task<Authorized> LocalCanFetchVoid()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchBool()
        {
            return LocalCanFetchBool();
        }

        public async Task<Authorized> LocalCanFetchBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTask()
        {
            return LocalCanFetchTask();
        }

        public async Task<Authorized> LocalCanFetchTask()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskBool()
        {
            return LocalCanFetchTaskBool();
        }

        public async Task<Authorized> LocalCanFetchTaskBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchVoid(int? param)
        {
            return LocalCanFetchVoid1(param);
        }

        public async Task<Authorized> LocalCanFetchVoid1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchBool(int? param)
        {
            return LocalCanFetchBool1(param);
        }

        public async Task<Authorized> LocalCanFetchBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTask(int? param)
        {
            return LocalCanFetchTask1(param);
        }

        public async Task<Authorized> LocalCanFetchTask1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskBool(int? param)
        {
            return LocalCanFetchTaskBool1(param);
        }

        public async Task<Authorized> LocalCanFetchTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchVoidDep()
        {
            return LocalCanFetchVoidDep();
        }

        public async Task<Authorized> LocalCanFetchVoidDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchBoolTrueDep()
        {
            return LocalCanFetchBoolTrueDep();
        }

        public async Task<Authorized> LocalCanFetchBoolTrueDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchBoolFalseDep()
        {
            return LocalCanFetchBoolFalseDep();
        }

        public async Task<Authorized> LocalCanFetchBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskDep()
        {
            return LocalCanFetchTaskDep();
        }

        public async Task<Authorized> LocalCanFetchTaskDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskBoolDep()
        {
            return LocalCanFetchTaskBoolDep();
        }

        public async Task<Authorized> LocalCanFetchTaskBoolDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchVoidDep(int? param)
        {
            return LocalCanFetchVoidDep1(param);
        }

        public async Task<Authorized> LocalCanFetchVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchBoolTrueDep(int? param)
        {
            return LocalCanFetchBoolTrueDep1(param);
        }

        public async Task<Authorized> LocalCanFetchBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchBoolFalseDep(int? param)
        {
            return LocalCanFetchBoolFalseDep1(param);
        }

        public async Task<Authorized> LocalCanFetchBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskDep(int? param)
        {
            return LocalCanFetchTaskDep1(param);
        }

        public async Task<Authorized> LocalCanFetchTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskBoolDep(int? param)
        {
            return LocalCanFetchTaskBoolDep1(param);
        }

        public async Task<Authorized> LocalCanFetchTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskBoolFalseDep(int? param)
        {
            return LocalCanFetchTaskBoolFalseDep(param);
        }

        public async Task<Authorized> LocalCanFetchTaskBoolFalseDep(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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
            services.AddTransient<ReadAuthTaskDataMapper>();
            services.AddScoped<ReadAuthTaskDataMapperFactory>();
            services.AddScoped<IReadAuthTaskDataMapperFactory, ReadAuthTaskDataMapperFactory>();
        }
    }
}