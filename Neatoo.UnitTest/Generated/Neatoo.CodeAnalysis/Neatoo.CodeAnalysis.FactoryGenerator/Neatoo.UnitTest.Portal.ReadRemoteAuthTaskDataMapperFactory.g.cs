using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using static Neatoo.UnitTest.RemoteFactory.ReadRemoteAuthTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.AuthorizationRules;
using Neatoo.UnitTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
                    Debugging Messages:
                    Parent class: ReadRemoteAuthTests
: ReadTests.ReadDataMapper

No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No MethodDeclarationSyntax for .ctor
No MethodDeclarationSyntax for get_CanReadRemoteCalled
No MethodDeclarationSyntax for set_CanReadRemoteCalled
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
    public interface IReadRemoteAuthTaskDataMapperFactory
    {
        Task<ReadRemoteAuthTaskDataMapper> CreateVoid();
        Task<ReadRemoteAuthTaskDataMapper?> CreateBool();
        Task<ReadRemoteAuthTaskDataMapper> CreateTask();
        Task<ReadRemoteAuthTaskDataMapper?> CreateTaskBool();
        Task<ReadRemoteAuthTaskDataMapper> CreateVoid(int? param);
        Task<ReadRemoteAuthTaskDataMapper?> CreateBool(int? param);
        Task<ReadRemoteAuthTaskDataMapper> CreateTask(int? param);
        Task<ReadRemoteAuthTaskDataMapper?> CreateTaskBool(int? param);
        Task<ReadRemoteAuthTaskDataMapper?> CreateTaskBoolFalse(int? param);
        Task<ReadRemoteAuthTaskDataMapper> CreateVoidDep();
        Task<ReadRemoteAuthTaskDataMapper?> CreateBoolTrueDep();
        Task<ReadRemoteAuthTaskDataMapper?> CreateBoolFalseDep();
        Task<ReadRemoteAuthTaskDataMapper> CreateTaskDep();
        Task<ReadRemoteAuthTaskDataMapper?> CreateTaskBoolDep();
        Task<ReadRemoteAuthTaskDataMapper?> CreateTaskBoolFalseDep();
        Task<ReadRemoteAuthTaskDataMapper> CreateVoidDep(int? param);
        Task<ReadRemoteAuthTaskDataMapper?> CreateBoolTrueDep(int? param);
        Task<ReadRemoteAuthTaskDataMapper?> CreateBoolFalseDep(int? param);
        Task<ReadRemoteAuthTaskDataMapper> CreateTaskDep(int? param);
        Task<ReadRemoteAuthTaskDataMapper?> CreateTaskBoolDep(int? param);
        Task<ReadRemoteAuthTaskDataMapper> FetchVoid();
        Task<ReadRemoteAuthTaskDataMapper?> FetchBool();
        Task<ReadRemoteAuthTaskDataMapper> FetchTask();
        Task<ReadRemoteAuthTaskDataMapper?> FetchTaskBool();
        Task<ReadRemoteAuthTaskDataMapper> FetchVoid(int? param);
        Task<ReadRemoteAuthTaskDataMapper?> FetchBool(int? param);
        Task<ReadRemoteAuthTaskDataMapper> FetchTask(int? param);
        Task<ReadRemoteAuthTaskDataMapper?> FetchTaskBool(int? param);
        Task<ReadRemoteAuthTaskDataMapper> FetchVoidDep();
        Task<ReadRemoteAuthTaskDataMapper?> FetchBoolTrueDep();
        Task<ReadRemoteAuthTaskDataMapper?> FetchBoolFalseDep();
        Task<ReadRemoteAuthTaskDataMapper> FetchTaskDep();
        Task<ReadRemoteAuthTaskDataMapper?> FetchTaskBoolDep();
        Task<ReadRemoteAuthTaskDataMapper> FetchVoidDep(int? param);
        Task<ReadRemoteAuthTaskDataMapper?> FetchBoolTrueDep(int? param);
        Task<ReadRemoteAuthTaskDataMapper?> FetchBoolFalseDep(int? param);
        Task<ReadRemoteAuthTaskDataMapper> FetchTaskDep(int? param);
        Task<ReadRemoteAuthTaskDataMapper?> FetchTaskBoolDep(int? param);
        Task<ReadRemoteAuthTaskDataMapper?> FetchTaskBoolFalseDep(int? param);
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

    internal class ReadRemoteAuthTaskDataMapperFactory : FactoryBase, IReadRemoteAuthTaskDataMapperFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateVoidDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateBoolDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateTaskDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateTaskBoolDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateVoid1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateBool1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateTask1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateTaskBool1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateTaskBoolFalseDelegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateVoidDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateBoolTrueDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateBoolFalseDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateTaskDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateTaskBoolDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateTaskBoolFalseDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateVoidDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateBoolTrueDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateBoolFalseDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateTaskDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> CreateTaskBoolDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchVoidDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchBoolDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchTaskDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchTaskBoolDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchVoid1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchBool1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchTask1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchTaskBool1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchVoidDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchBoolTrueDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchBoolFalseDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchTaskDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchTaskBoolDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchVoidDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchBoolTrueDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchBoolFalseDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchTaskDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchTaskBoolDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthTaskDataMapper>> FetchTaskBoolFalseDepDelegate(int? param);
        public delegate Task<Authorized> CanCreateVoidDelegate();
        public delegate Task<Authorized> CanCreateBoolDelegate();
        public delegate Task<Authorized> CanCreateTaskDelegate();
        public delegate Task<Authorized> CanCreateTaskBoolDelegate();
        public delegate Task<Authorized> CanCreateVoid1Delegate(int? param);
        public delegate Task<Authorized> CanCreateBool1Delegate(int? param);
        public delegate Task<Authorized> CanCreateTask1Delegate(int? param);
        public delegate Task<Authorized> CanCreateTaskBool1Delegate(int? param);
        public delegate Task<Authorized> CanCreateTaskBoolFalseDelegate(int? param);
        public delegate Task<Authorized> CanCreateVoidDepDelegate();
        public delegate Task<Authorized> CanCreateBoolTrueDepDelegate();
        public delegate Task<Authorized> CanCreateBoolFalseDepDelegate();
        public delegate Task<Authorized> CanCreateTaskDepDelegate();
        public delegate Task<Authorized> CanCreateTaskBoolDepDelegate();
        public delegate Task<Authorized> CanCreateTaskBoolFalseDepDelegate();
        public delegate Task<Authorized> CanCreateVoidDep1Delegate(int? param);
        public delegate Task<Authorized> CanCreateBoolTrueDep1Delegate(int? param);
        public delegate Task<Authorized> CanCreateBoolFalseDep1Delegate(int? param);
        public delegate Task<Authorized> CanCreateTaskDep1Delegate(int? param);
        public delegate Task<Authorized> CanCreateTaskBoolDep1Delegate(int? param);
        public delegate Task<Authorized> CanFetchVoidDelegate();
        public delegate Task<Authorized> CanFetchBoolDelegate();
        public delegate Task<Authorized> CanFetchTaskDelegate();
        public delegate Task<Authorized> CanFetchTaskBoolDelegate();
        public delegate Task<Authorized> CanFetchVoid1Delegate(int? param);
        public delegate Task<Authorized> CanFetchBool1Delegate(int? param);
        public delegate Task<Authorized> CanFetchTask1Delegate(int? param);
        public delegate Task<Authorized> CanFetchTaskBool1Delegate(int? param);
        public delegate Task<Authorized> CanFetchVoidDepDelegate();
        public delegate Task<Authorized> CanFetchBoolTrueDepDelegate();
        public delegate Task<Authorized> CanFetchBoolFalseDepDelegate();
        public delegate Task<Authorized> CanFetchTaskDepDelegate();
        public delegate Task<Authorized> CanFetchTaskBoolDepDelegate();
        public delegate Task<Authorized> CanFetchVoidDep1Delegate(int? param);
        public delegate Task<Authorized> CanFetchBoolTrueDep1Delegate(int? param);
        public delegate Task<Authorized> CanFetchBoolFalseDep1Delegate(int? param);
        public delegate Task<Authorized> CanFetchTaskDep1Delegate(int? param);
        public delegate Task<Authorized> CanFetchTaskBoolDep1Delegate(int? param);
        public delegate Task<Authorized> CanFetchTaskBoolFalseDepDelegate(int? param);
        // Delegate Properties to provide Local or Remote fork in execution
        public ReadRemoteAuthTask ReadRemoteAuthTask { get; }
        public ReadRemoteAuth ReadRemoteAuth { get; }
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
        public CanCreateVoidDelegate CanCreateVoidProperty { get; }
        public CanCreateBoolDelegate CanCreateBoolProperty { get; }
        public CanCreateTaskDelegate CanCreateTaskProperty { get; }
        public CanCreateTaskBoolDelegate CanCreateTaskBoolProperty { get; }
        public CanCreateVoid1Delegate CanCreateVoid1Property { get; }
        public CanCreateBool1Delegate CanCreateBool1Property { get; }
        public CanCreateTask1Delegate CanCreateTask1Property { get; }
        public CanCreateTaskBool1Delegate CanCreateTaskBool1Property { get; }
        public CanCreateTaskBoolFalseDelegate CanCreateTaskBoolFalseProperty { get; }
        public CanCreateVoidDepDelegate CanCreateVoidDepProperty { get; }
        public CanCreateBoolTrueDepDelegate CanCreateBoolTrueDepProperty { get; }
        public CanCreateBoolFalseDepDelegate CanCreateBoolFalseDepProperty { get; }
        public CanCreateTaskDepDelegate CanCreateTaskDepProperty { get; }
        public CanCreateTaskBoolDepDelegate CanCreateTaskBoolDepProperty { get; }
        public CanCreateTaskBoolFalseDepDelegate CanCreateTaskBoolFalseDepProperty { get; }
        public CanCreateVoidDep1Delegate CanCreateVoidDep1Property { get; }
        public CanCreateBoolTrueDep1Delegate CanCreateBoolTrueDep1Property { get; }
        public CanCreateBoolFalseDep1Delegate CanCreateBoolFalseDep1Property { get; }
        public CanCreateTaskDep1Delegate CanCreateTaskDep1Property { get; }
        public CanCreateTaskBoolDep1Delegate CanCreateTaskBoolDep1Property { get; }
        public CanFetchVoidDelegate CanFetchVoidProperty { get; }
        public CanFetchBoolDelegate CanFetchBoolProperty { get; }
        public CanFetchTaskDelegate CanFetchTaskProperty { get; }
        public CanFetchTaskBoolDelegate CanFetchTaskBoolProperty { get; }
        public CanFetchVoid1Delegate CanFetchVoid1Property { get; }
        public CanFetchBool1Delegate CanFetchBool1Property { get; }
        public CanFetchTask1Delegate CanFetchTask1Property { get; }
        public CanFetchTaskBool1Delegate CanFetchTaskBool1Property { get; }
        public CanFetchVoidDepDelegate CanFetchVoidDepProperty { get; }
        public CanFetchBoolTrueDepDelegate CanFetchBoolTrueDepProperty { get; }
        public CanFetchBoolFalseDepDelegate CanFetchBoolFalseDepProperty { get; }
        public CanFetchTaskDepDelegate CanFetchTaskDepProperty { get; }
        public CanFetchTaskBoolDepDelegate CanFetchTaskBoolDepProperty { get; }
        public CanFetchVoidDep1Delegate CanFetchVoidDep1Property { get; }
        public CanFetchBoolTrueDep1Delegate CanFetchBoolTrueDep1Property { get; }
        public CanFetchBoolFalseDep1Delegate CanFetchBoolFalseDep1Property { get; }
        public CanFetchTaskDep1Delegate CanFetchTaskDep1Property { get; }
        public CanFetchTaskBoolDep1Delegate CanFetchTaskBoolDep1Property { get; }
        public CanFetchTaskBoolFalseDepDelegate CanFetchTaskBoolFalseDepProperty { get; }

        public ReadRemoteAuthTaskDataMapperFactory(IServiceProvider serviceProvider, ReadRemoteAuthTask readremoteauthtask, ReadRemoteAuth readremoteauth)
        {
            this.ServiceProvider = serviceProvider;
            this.ReadRemoteAuthTask = readremoteauthtask;
            this.ReadRemoteAuth = readremoteauth;
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
            CanCreateVoidProperty = LocalCanCreateVoid;
            CanCreateBoolProperty = LocalCanCreateBool;
            CanCreateTaskProperty = LocalCanCreateTask;
            CanCreateTaskBoolProperty = LocalCanCreateTaskBool;
            CanCreateVoid1Property = LocalCanCreateVoid1;
            CanCreateBool1Property = LocalCanCreateBool1;
            CanCreateTask1Property = LocalCanCreateTask1;
            CanCreateTaskBool1Property = LocalCanCreateTaskBool1;
            CanCreateTaskBoolFalseProperty = LocalCanCreateTaskBoolFalse;
            CanCreateVoidDepProperty = LocalCanCreateVoidDep;
            CanCreateBoolTrueDepProperty = LocalCanCreateBoolTrueDep;
            CanCreateBoolFalseDepProperty = LocalCanCreateBoolFalseDep;
            CanCreateTaskDepProperty = LocalCanCreateTaskDep;
            CanCreateTaskBoolDepProperty = LocalCanCreateTaskBoolDep;
            CanCreateTaskBoolFalseDepProperty = LocalCanCreateTaskBoolFalseDep;
            CanCreateVoidDep1Property = LocalCanCreateVoidDep1;
            CanCreateBoolTrueDep1Property = LocalCanCreateBoolTrueDep1;
            CanCreateBoolFalseDep1Property = LocalCanCreateBoolFalseDep1;
            CanCreateTaskDep1Property = LocalCanCreateTaskDep1;
            CanCreateTaskBoolDep1Property = LocalCanCreateTaskBoolDep1;
            CanFetchVoidProperty = LocalCanFetchVoid;
            CanFetchBoolProperty = LocalCanFetchBool;
            CanFetchTaskProperty = LocalCanFetchTask;
            CanFetchTaskBoolProperty = LocalCanFetchTaskBool;
            CanFetchVoid1Property = LocalCanFetchVoid1;
            CanFetchBool1Property = LocalCanFetchBool1;
            CanFetchTask1Property = LocalCanFetchTask1;
            CanFetchTaskBool1Property = LocalCanFetchTaskBool1;
            CanFetchVoidDepProperty = LocalCanFetchVoidDep;
            CanFetchBoolTrueDepProperty = LocalCanFetchBoolTrueDep;
            CanFetchBoolFalseDepProperty = LocalCanFetchBoolFalseDep;
            CanFetchTaskDepProperty = LocalCanFetchTaskDep;
            CanFetchTaskBoolDepProperty = LocalCanFetchTaskBoolDep;
            CanFetchVoidDep1Property = LocalCanFetchVoidDep1;
            CanFetchBoolTrueDep1Property = LocalCanFetchBoolTrueDep1;
            CanFetchBoolFalseDep1Property = LocalCanFetchBoolFalseDep1;
            CanFetchTaskDep1Property = LocalCanFetchTaskDep1;
            CanFetchTaskBoolDep1Property = LocalCanFetchTaskBoolDep1;
            CanFetchTaskBoolFalseDepProperty = LocalCanFetchTaskBoolFalseDep;
        }

        public ReadRemoteAuthTaskDataMapperFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate, ReadRemoteAuthTask readremoteauthtask, ReadRemoteAuth readremoteauth)
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
            CanCreateVoidProperty = RemoteCanCreateVoid;
            CanCreateBoolProperty = RemoteCanCreateBool;
            CanCreateTaskProperty = RemoteCanCreateTask;
            CanCreateTaskBoolProperty = RemoteCanCreateTaskBool;
            CanCreateVoid1Property = RemoteCanCreateVoid1;
            CanCreateBool1Property = RemoteCanCreateBool1;
            CanCreateTask1Property = RemoteCanCreateTask1;
            CanCreateTaskBool1Property = RemoteCanCreateTaskBool1;
            CanCreateTaskBoolFalseProperty = RemoteCanCreateTaskBoolFalse;
            CanCreateVoidDepProperty = RemoteCanCreateVoidDep;
            CanCreateBoolTrueDepProperty = RemoteCanCreateBoolTrueDep;
            CanCreateBoolFalseDepProperty = RemoteCanCreateBoolFalseDep;
            CanCreateTaskDepProperty = RemoteCanCreateTaskDep;
            CanCreateTaskBoolDepProperty = RemoteCanCreateTaskBoolDep;
            CanCreateTaskBoolFalseDepProperty = RemoteCanCreateTaskBoolFalseDep;
            CanCreateVoidDep1Property = RemoteCanCreateVoidDep1;
            CanCreateBoolTrueDep1Property = RemoteCanCreateBoolTrueDep1;
            CanCreateBoolFalseDep1Property = RemoteCanCreateBoolFalseDep1;
            CanCreateTaskDep1Property = RemoteCanCreateTaskDep1;
            CanCreateTaskBoolDep1Property = RemoteCanCreateTaskBoolDep1;
            CanFetchVoidProperty = RemoteCanFetchVoid;
            CanFetchBoolProperty = RemoteCanFetchBool;
            CanFetchTaskProperty = RemoteCanFetchTask;
            CanFetchTaskBoolProperty = RemoteCanFetchTaskBool;
            CanFetchVoid1Property = RemoteCanFetchVoid1;
            CanFetchBool1Property = RemoteCanFetchBool1;
            CanFetchTask1Property = RemoteCanFetchTask1;
            CanFetchTaskBool1Property = RemoteCanFetchTaskBool1;
            CanFetchVoidDepProperty = RemoteCanFetchVoidDep;
            CanFetchBoolTrueDepProperty = RemoteCanFetchBoolTrueDep;
            CanFetchBoolFalseDepProperty = RemoteCanFetchBoolFalseDep;
            CanFetchTaskDepProperty = RemoteCanFetchTaskDep;
            CanFetchTaskBoolDepProperty = RemoteCanFetchTaskBoolDep;
            CanFetchVoidDep1Property = RemoteCanFetchVoidDep1;
            CanFetchBoolTrueDep1Property = RemoteCanFetchBoolTrueDep1;
            CanFetchBoolFalseDep1Property = RemoteCanFetchBoolFalseDep1;
            CanFetchTaskDep1Property = RemoteCanFetchTaskDep1;
            CanFetchTaskBoolDep1Property = RemoteCanFetchTaskBoolDep1;
            CanFetchTaskBoolFalseDepProperty = RemoteCanFetchTaskBoolFalseDep;
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> CreateVoid()
        {
            return (await CreateVoidProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateVoid()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateVoidDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateVoid()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCall<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoid()));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> CreateBool()
        {
            return (await CreateBoolProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateBoolDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateBool()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateBool()));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> CreateTask()
        {
            return (await CreateTaskProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateTask()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateTaskDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateTask()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTask()));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> CreateTaskBool()
        {
            return (await CreateTaskBoolProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateTaskBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateTaskBoolDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateTaskBool()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBool()));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> CreateVoid(int? param)
        {
            return (await CreateVoid1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateVoid1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateVoid1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateVoid1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCall<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoid(param)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> CreateBool(int? param)
        {
            return (await CreateBool1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateBool1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateBool(param)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> CreateTask(int? param)
        {
            return (await CreateTask1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateTask1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateTask1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateTask1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTask(param)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> CreateTaskBool(int? param)
        {
            return (await CreateTaskBool1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateTaskBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateTaskBool1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBool(param)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> CreateTaskBoolFalse(int? param)
        {
            return (await CreateTaskBoolFalseProperty(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateTaskBoolFalse(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateTaskBoolFalseDelegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateTaskBoolFalse(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolFalse(param)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> CreateVoidDep()
        {
            return (await CreateVoidDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateVoidDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateVoidDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateVoidDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCall<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoidDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> CreateBoolTrueDep()
        {
            return (await CreateBoolTrueDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateBoolTrueDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateBoolTrueDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateBoolTrueDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolTrueDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> CreateBoolFalseDep()
        {
            return (await CreateBoolFalseDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateBoolFalseDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolFalseDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> CreateTaskDep()
        {
            return (await CreateTaskDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateTaskDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateTaskDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateTaskDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> CreateTaskBoolDep()
        {
            return (await CreateTaskBoolDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateTaskBoolDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateTaskBoolDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateTaskBoolDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> CreateTaskBoolFalseDep()
        {
            return (await CreateTaskBoolFalseDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateTaskBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateTaskBoolFalseDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateTaskBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolFalseDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> CreateVoidDep(int? param)
        {
            return (await CreateVoidDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateVoidDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateVoidDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCall<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoidDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> CreateBoolTrueDep(int? param)
        {
            return (await CreateBoolTrueDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateBoolTrueDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateBoolTrueDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolTrueDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> CreateBoolFalseDep(int? param)
        {
            return (await CreateBoolFalseDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateBoolFalseDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateBoolFalseDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolFalseDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> CreateTaskDep(int? param)
        {
            return (await CreateTaskDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateTaskDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateTaskDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> CreateTaskBoolDep(int? param)
        {
            return (await CreateTaskBoolDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteCreateTaskBoolDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(CreateTaskBoolDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalCreateTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> FetchVoid()
        {
            return (await FetchVoidProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchVoid()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchVoidDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchVoid()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCall<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoid()));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> FetchBool()
        {
            return (await FetchBoolProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchBoolDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchBool()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBool()));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> FetchTask()
        {
            return (await FetchTaskProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchTask()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchTaskDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchTask()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTask()));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> FetchTaskBool()
        {
            return (await FetchTaskBoolProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchTaskBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchTaskBoolDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchTaskBool()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBool()));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> FetchVoid(int? param)
        {
            return (await FetchVoid1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchVoid1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchVoid1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchVoid1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCall<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoid(param)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> FetchBool(int? param)
        {
            return (await FetchBool1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchBool1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBool(param)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> FetchTask(int? param)
        {
            return (await FetchTask1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchTask1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchTask1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchTask1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTask(param)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> FetchTaskBool(int? param)
        {
            return (await FetchTaskBool1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchTaskBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchTaskBool1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBool(param)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> FetchVoidDep()
        {
            return (await FetchVoidDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchVoidDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchVoidDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchVoidDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCall<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoidDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> FetchBoolTrueDep()
        {
            return (await FetchBoolTrueDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchBoolTrueDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchBoolTrueDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchBoolTrueDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolTrueDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> FetchBoolFalseDep()
        {
            return (await FetchBoolFalseDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchBoolFalseDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolFalseDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> FetchTaskDep()
        {
            return (await FetchTaskDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchTaskDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchTaskDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchTaskDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> FetchTaskBoolDep()
        {
            return (await FetchTaskBoolDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchTaskBoolDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchTaskBoolDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchTaskBoolDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> FetchVoidDep(int? param)
        {
            return (await FetchVoidDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchVoidDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchVoidDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCall<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoidDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> FetchBoolTrueDep(int? param)
        {
            return (await FetchBoolTrueDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchBoolTrueDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchBoolTrueDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolTrueDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> FetchBoolFalseDep(int? param)
        {
            return (await FetchBoolFalseDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchBoolFalseDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchBoolFalseDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolFalseDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper> FetchTaskDep(int? param)
        {
            return (await FetchTaskDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchTaskDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchTaskDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> FetchTaskBoolDep(int? param)
        {
            return (await FetchTaskBoolDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchTaskBoolDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchTaskBoolDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthTaskDataMapper?> FetchTaskBoolFalseDep(int? param)
        {
            return (await FetchTaskBoolFalseDepProperty(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthTaskDataMapper>> RemoteFetchTaskBoolFalseDep(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthTaskDataMapper>>(typeof(FetchTaskBoolFalseDepDelegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthTaskDataMapper>> LocalFetchTaskBoolFalseDep(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthTaskDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthTaskDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthTaskDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthTaskDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolFalseDep(param, disposableDependency)));
        }

        public virtual Task<Authorized> CanCreateVoid()
        {
            return CanCreateVoidProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateVoid()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateVoidDelegate), []);
        }

        public async Task<Authorized> LocalCanCreateVoid()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateBool()
        {
            return CanCreateBoolProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateBoolDelegate), []);
        }

        public async Task<Authorized> LocalCanCreateBool()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateTask()
        {
            return CanCreateTaskProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateTask()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskDelegate), []);
        }

        public async Task<Authorized> LocalCanCreateTask()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateTaskBool()
        {
            return CanCreateTaskBoolProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskBoolDelegate), []);
        }

        public async Task<Authorized> LocalCanCreateTaskBool()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateVoid(int? param)
        {
            return CanCreateVoid1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateVoid1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateVoid1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanCreateVoid1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateBool(int? param)
        {
            return CanCreateBool1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateBool1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanCreateBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateTask(int? param)
        {
            return CanCreateTask1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateTask1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTask1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanCreateTask1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateTaskBool(int? param)
        {
            return CanCreateTaskBool1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskBool1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanCreateTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateTaskBoolFalse(int? param)
        {
            return CanCreateTaskBoolFalseProperty(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskBoolFalse(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskBoolFalseDelegate), [param]);
        }

        public async Task<Authorized> LocalCanCreateTaskBoolFalse(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateVoidDep()
        {
            return CanCreateVoidDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateVoidDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateVoidDepDelegate), []);
        }

        public async Task<Authorized> LocalCanCreateVoidDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateBoolTrueDep()
        {
            return CanCreateBoolTrueDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateBoolTrueDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateBoolTrueDepDelegate), []);
        }

        public async Task<Authorized> LocalCanCreateBoolTrueDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateBoolFalseDep()
        {
            return CanCreateBoolFalseDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateBoolFalseDepDelegate), []);
        }

        public async Task<Authorized> LocalCanCreateBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateTaskDep()
        {
            return CanCreateTaskDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskDepDelegate), []);
        }

        public async Task<Authorized> LocalCanCreateTaskDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateTaskBoolDep()
        {
            return CanCreateTaskBoolDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskBoolDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskBoolDepDelegate), []);
        }

        public async Task<Authorized> LocalCanCreateTaskBoolDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateTaskBoolFalseDep()
        {
            return CanCreateTaskBoolFalseDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskBoolFalseDepDelegate), []);
        }

        public async Task<Authorized> LocalCanCreateTaskBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateVoidDep(int? param)
        {
            return CanCreateVoidDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateVoidDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateVoidDep1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanCreateVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateBoolTrueDep(int? param)
        {
            return CanCreateBoolTrueDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateBoolTrueDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateBoolTrueDep1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanCreateBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateBoolFalseDep(int? param)
        {
            return CanCreateBoolFalseDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateBoolFalseDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateBoolFalseDep1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanCreateBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateTaskDep(int? param)
        {
            return CanCreateTaskDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskDep1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanCreateTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanCreateTaskBoolDep(int? param)
        {
            return CanCreateTaskBoolDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskBoolDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskBoolDep1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanCreateTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchVoid()
        {
            return CanFetchVoidProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchVoid()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchVoidDelegate), []);
        }

        public async Task<Authorized> LocalCanFetchVoid()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchBool()
        {
            return CanFetchBoolProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchBoolDelegate), []);
        }

        public async Task<Authorized> LocalCanFetchBool()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchTask()
        {
            return CanFetchTaskProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchTask()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskDelegate), []);
        }

        public async Task<Authorized> LocalCanFetchTask()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchTaskBool()
        {
            return CanFetchTaskBoolProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskBoolDelegate), []);
        }

        public async Task<Authorized> LocalCanFetchTaskBool()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchVoid(int? param)
        {
            return CanFetchVoid1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchVoid1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchVoid1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanFetchVoid1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchBool(int? param)
        {
            return CanFetchBool1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchBool1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanFetchBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchTask(int? param)
        {
            return CanFetchTask1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchTask1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTask1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanFetchTask1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchTaskBool(int? param)
        {
            return CanFetchTaskBool1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskBool1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanFetchTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchVoidDep()
        {
            return CanFetchVoidDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchVoidDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchVoidDepDelegate), []);
        }

        public async Task<Authorized> LocalCanFetchVoidDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchBoolTrueDep()
        {
            return CanFetchBoolTrueDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchBoolTrueDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchBoolTrueDepDelegate), []);
        }

        public async Task<Authorized> LocalCanFetchBoolTrueDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchBoolFalseDep()
        {
            return CanFetchBoolFalseDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchBoolFalseDepDelegate), []);
        }

        public async Task<Authorized> LocalCanFetchBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchTaskDep()
        {
            return CanFetchTaskDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskDepDelegate), []);
        }

        public async Task<Authorized> LocalCanFetchTaskDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchTaskBoolDep()
        {
            return CanFetchTaskBoolDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskBoolDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskBoolDepDelegate), []);
        }

        public async Task<Authorized> LocalCanFetchTaskBoolDep()
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchVoidDep(int? param)
        {
            return CanFetchVoidDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchVoidDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchVoidDep1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanFetchVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchBoolTrueDep(int? param)
        {
            return CanFetchBoolTrueDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchBoolTrueDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchBoolTrueDep1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanFetchBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchBoolFalseDep(int? param)
        {
            return CanFetchBoolFalseDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchBoolFalseDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchBoolFalseDep1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanFetchBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchTaskDep(int? param)
        {
            return CanFetchTaskDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskDep1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanFetchTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchTaskBoolDep(int? param)
        {
            return CanFetchTaskBoolDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskBoolDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskBoolDep1Delegate), [param]);
        }

        public async Task<Authorized> LocalCanFetchTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Task<Authorized> CanFetchTaskBoolFalseDep(int? param)
        {
            return CanFetchTaskBoolFalseDepProperty(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskBoolFalseDep(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskBoolFalseDepDelegate), [param]);
        }

        public async Task<Authorized> LocalCanFetchTaskBoolFalseDep(int? param)
        {
            Authorized authorized;
            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanReadRemoteStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadRemoteAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<ReadRemoteAuthTaskDataMapper>();
            services.AddScoped<ReadRemoteAuthTaskDataMapperFactory>();
            services.AddScoped<IReadRemoteAuthTaskDataMapperFactory, ReadRemoteAuthTaskDataMapperFactory>();
            services.AddScoped<CreateVoidDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCreateVoid();
            });
            services.AddScoped<CreateBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCreateBool();
            });
            services.AddScoped<CreateTaskDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCreateTask();
            });
            services.AddScoped<CreateTaskBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCreateTaskBool();
            });
            services.AddScoped<CreateVoid1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCreateVoid1(param);
            });
            services.AddScoped<CreateBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCreateBool1(param);
            });
            services.AddScoped<CreateTask1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCreateTask1(param);
            });
            services.AddScoped<CreateTaskBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCreateTaskBool1(param);
            });
            services.AddScoped<CreateTaskBoolFalseDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCreateTaskBoolFalse(param);
            });
            services.AddScoped<CreateVoidDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCreateVoidDep();
            });
            services.AddScoped<CreateBoolTrueDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCreateBoolTrueDep();
            });
            services.AddScoped<CreateBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCreateBoolFalseDep();
            });
            services.AddScoped<CreateTaskDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCreateTaskDep();
            });
            services.AddScoped<CreateTaskBoolDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCreateTaskBoolDep();
            });
            services.AddScoped<CreateTaskBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCreateTaskBoolFalseDep();
            });
            services.AddScoped<CreateVoidDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCreateVoidDep1(param);
            });
            services.AddScoped<CreateBoolTrueDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCreateBoolTrueDep1(param);
            });
            services.AddScoped<CreateBoolFalseDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCreateBoolFalseDep1(param);
            });
            services.AddScoped<CreateTaskDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCreateTaskDep1(param);
            });
            services.AddScoped<CreateTaskBoolDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCreateTaskBoolDep1(param);
            });
            services.AddScoped<FetchVoidDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalFetchVoid();
            });
            services.AddScoped<FetchBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalFetchBool();
            });
            services.AddScoped<FetchTaskDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalFetchTask();
            });
            services.AddScoped<FetchTaskBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalFetchTaskBool();
            });
            services.AddScoped<FetchVoid1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalFetchVoid1(param);
            });
            services.AddScoped<FetchBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalFetchBool1(param);
            });
            services.AddScoped<FetchTask1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalFetchTask1(param);
            });
            services.AddScoped<FetchTaskBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalFetchTaskBool1(param);
            });
            services.AddScoped<FetchVoidDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalFetchVoidDep();
            });
            services.AddScoped<FetchBoolTrueDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalFetchBoolTrueDep();
            });
            services.AddScoped<FetchBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalFetchBoolFalseDep();
            });
            services.AddScoped<FetchTaskDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalFetchTaskDep();
            });
            services.AddScoped<FetchTaskBoolDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalFetchTaskBoolDep();
            });
            services.AddScoped<FetchVoidDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalFetchVoidDep1(param);
            });
            services.AddScoped<FetchBoolTrueDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalFetchBoolTrueDep1(param);
            });
            services.AddScoped<FetchBoolFalseDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalFetchBoolFalseDep1(param);
            });
            services.AddScoped<FetchTaskDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalFetchTaskDep1(param);
            });
            services.AddScoped<FetchTaskBoolDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalFetchTaskBoolDep1(param);
            });
            services.AddScoped<FetchTaskBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalFetchTaskBoolFalseDep(param);
            });
            services.AddScoped<CanCreateVoidDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanCreateVoid();
            });
            services.AddScoped<CanCreateBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanCreateBool();
            });
            services.AddScoped<CanCreateTaskDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanCreateTask();
            });
            services.AddScoped<CanCreateTaskBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanCreateTaskBool();
            });
            services.AddScoped<CanCreateVoid1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateVoid1(param);
            });
            services.AddScoped<CanCreateBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateBool1(param);
            });
            services.AddScoped<CanCreateTask1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateTask1(param);
            });
            services.AddScoped<CanCreateTaskBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateTaskBool1(param);
            });
            services.AddScoped<CanCreateTaskBoolFalseDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateTaskBoolFalse(param);
            });
            services.AddScoped<CanCreateVoidDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanCreateVoidDep();
            });
            services.AddScoped<CanCreateBoolTrueDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanCreateBoolTrueDep();
            });
            services.AddScoped<CanCreateBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanCreateBoolFalseDep();
            });
            services.AddScoped<CanCreateTaskDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanCreateTaskDep();
            });
            services.AddScoped<CanCreateTaskBoolDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanCreateTaskBoolDep();
            });
            services.AddScoped<CanCreateTaskBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanCreateTaskBoolFalseDep();
            });
            services.AddScoped<CanCreateVoidDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateVoidDep1(param);
            });
            services.AddScoped<CanCreateBoolTrueDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateBoolTrueDep1(param);
            });
            services.AddScoped<CanCreateBoolFalseDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateBoolFalseDep1(param);
            });
            services.AddScoped<CanCreateTaskDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateTaskDep1(param);
            });
            services.AddScoped<CanCreateTaskBoolDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateTaskBoolDep1(param);
            });
            services.AddScoped<CanFetchVoidDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanFetchVoid();
            });
            services.AddScoped<CanFetchBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanFetchBool();
            });
            services.AddScoped<CanFetchTaskDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanFetchTask();
            });
            services.AddScoped<CanFetchTaskBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanFetchTaskBool();
            });
            services.AddScoped<CanFetchVoid1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchVoid1(param);
            });
            services.AddScoped<CanFetchBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchBool1(param);
            });
            services.AddScoped<CanFetchTask1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchTask1(param);
            });
            services.AddScoped<CanFetchTaskBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchTaskBool1(param);
            });
            services.AddScoped<CanFetchVoidDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanFetchVoidDep();
            });
            services.AddScoped<CanFetchBoolTrueDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanFetchBoolTrueDep();
            });
            services.AddScoped<CanFetchBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanFetchBoolFalseDep();
            });
            services.AddScoped<CanFetchTaskDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanFetchTaskDep();
            });
            services.AddScoped<CanFetchTaskBoolDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return () => factory.LocalCanFetchTaskBoolDep();
            });
            services.AddScoped<CanFetchVoidDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchVoidDep1(param);
            });
            services.AddScoped<CanFetchBoolTrueDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchBoolTrueDep1(param);
            });
            services.AddScoped<CanFetchBoolFalseDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchBoolFalseDep1(param);
            });
            services.AddScoped<CanFetchTaskDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchTaskDep1(param);
            });
            services.AddScoped<CanFetchTaskBoolDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchTaskBoolDep1(param);
            });
            services.AddScoped<CanFetchTaskBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthTaskDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchTaskBoolFalseDep(param);
            });
        }
    }
}