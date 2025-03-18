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
    public interface IReadRemoteAuthDataMapperFactory
    {
        Task<ReadRemoteAuthDataMapper> CreateVoid();
        Task<ReadRemoteAuthDataMapper?> CreateBool();
        Task<ReadRemoteAuthDataMapper> CreateTask();
        Task<ReadRemoteAuthDataMapper?> CreateTaskBool();
        Task<ReadRemoteAuthDataMapper> CreateVoid(int? param);
        Task<ReadRemoteAuthDataMapper?> CreateBool(int? param);
        Task<ReadRemoteAuthDataMapper> CreateTask(int? param);
        Task<ReadRemoteAuthDataMapper?> CreateTaskBool(int? param);
        Task<ReadRemoteAuthDataMapper?> CreateTaskBoolFalse(int? param);
        Task<ReadRemoteAuthDataMapper> CreateVoidDep();
        Task<ReadRemoteAuthDataMapper?> CreateBoolTrueDep();
        Task<ReadRemoteAuthDataMapper?> CreateBoolFalseDep();
        Task<ReadRemoteAuthDataMapper> CreateTaskDep();
        Task<ReadRemoteAuthDataMapper?> CreateTaskBoolDep();
        Task<ReadRemoteAuthDataMapper?> CreateTaskBoolFalseDep();
        Task<ReadRemoteAuthDataMapper> CreateVoidDep(int? param);
        Task<ReadRemoteAuthDataMapper?> CreateBoolTrueDep(int? param);
        Task<ReadRemoteAuthDataMapper?> CreateBoolFalseDep(int? param);
        Task<ReadRemoteAuthDataMapper> CreateTaskDep(int? param);
        Task<ReadRemoteAuthDataMapper?> CreateTaskBoolDep(int? param);
        Task<ReadRemoteAuthDataMapper> FetchVoid();
        Task<ReadRemoteAuthDataMapper?> FetchBool();
        Task<ReadRemoteAuthDataMapper> FetchTask();
        Task<ReadRemoteAuthDataMapper?> FetchTaskBool();
        Task<ReadRemoteAuthDataMapper> FetchVoid(int? param);
        Task<ReadRemoteAuthDataMapper?> FetchBool(int? param);
        Task<ReadRemoteAuthDataMapper> FetchTask(int? param);
        Task<ReadRemoteAuthDataMapper?> FetchTaskBool(int? param);
        Task<ReadRemoteAuthDataMapper> FetchVoidDep();
        Task<ReadRemoteAuthDataMapper?> FetchBoolTrueDep();
        Task<ReadRemoteAuthDataMapper?> FetchBoolFalseDep();
        Task<ReadRemoteAuthDataMapper> FetchTaskDep();
        Task<ReadRemoteAuthDataMapper?> FetchTaskBoolDep();
        Task<ReadRemoteAuthDataMapper> FetchVoidDep(int? param);
        Task<ReadRemoteAuthDataMapper?> FetchBoolTrueDep(int? param);
        Task<ReadRemoteAuthDataMapper?> FetchBoolFalseDep(int? param);
        Task<ReadRemoteAuthDataMapper> FetchTaskDep(int? param);
        Task<ReadRemoteAuthDataMapper?> FetchTaskBoolDep(int? param);
        Task<ReadRemoteAuthDataMapper?> FetchTaskBoolFalseDep(int? param);
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

    internal class ReadRemoteAuthDataMapperFactory : FactoryBase, IReadRemoteAuthDataMapperFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateVoidDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateBoolDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateTaskDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateTaskBoolDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateVoid1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateBool1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateTask1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateTaskBool1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateTaskBoolFalseDelegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateVoidDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateBoolTrueDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateBoolFalseDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateTaskDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateTaskBoolDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateTaskBoolFalseDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateVoidDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateBoolTrueDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateBoolFalseDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateTaskDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> CreateTaskBoolDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchVoidDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchBoolDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchTaskDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchTaskBoolDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchVoid1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchBool1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchTask1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchTaskBool1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchVoidDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchBoolTrueDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchBoolFalseDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchTaskDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchTaskBoolDepDelegate();
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchVoidDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchBoolTrueDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchBoolFalseDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchTaskDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchTaskBoolDep1Delegate(int? param);
        public delegate Task<Authorized<ReadRemoteAuthDataMapper>> FetchTaskBoolFalseDepDelegate(int? param);
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

        public ReadRemoteAuthDataMapperFactory(IServiceProvider serviceProvider, ReadRemoteAuth readremoteauth)
        {
            this.ServiceProvider = serviceProvider;
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

        public ReadRemoteAuthDataMapperFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate, ReadRemoteAuth readremoteauth)
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

        public virtual async Task<ReadRemoteAuthDataMapper> CreateVoid()
        {
            return (await CreateVoidProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateVoid()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateVoidDelegate), []);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateVoid()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCall<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoid())));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> CreateBool()
        {
            return (await CreateBoolProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateBoolDelegate), []);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateBool()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateBool())));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> CreateTask()
        {
            return (await CreateTaskProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateTask()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateTaskDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateTask()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTask()));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> CreateTaskBool()
        {
            return (await CreateTaskBoolProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateTaskBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateTaskBoolDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateTaskBool()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBool()));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> CreateVoid(int? param)
        {
            return (await CreateVoid1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateVoid1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateVoid1Delegate), [param]);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateVoid1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCall<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoid(param))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> CreateBool(int? param)
        {
            return (await CreateBool1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateBool1Delegate), [param]);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateBool(param))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> CreateTask(int? param)
        {
            return (await CreateTask1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateTask1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateTask1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateTask1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTask(param)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> CreateTaskBool(int? param)
        {
            return (await CreateTaskBool1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateTaskBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateTaskBool1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBool(param)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> CreateTaskBoolFalse(int? param)
        {
            return (await CreateTaskBoolFalseProperty(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateTaskBoolFalse(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateTaskBoolFalseDelegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateTaskBoolFalse(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolFalse(param)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> CreateVoidDep()
        {
            return (await CreateVoidDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateVoidDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateVoidDepDelegate), []);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateVoidDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCall<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoidDep(disposableDependency))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> CreateBoolTrueDep()
        {
            return (await CreateBoolTrueDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateBoolTrueDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateBoolTrueDepDelegate), []);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateBoolTrueDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolTrueDep(disposableDependency))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> CreateBoolFalseDep()
        {
            return (await CreateBoolFalseDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateBoolFalseDepDelegate), []);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolFalseDep(disposableDependency))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> CreateTaskDep()
        {
            return (await CreateTaskDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateTaskDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateTaskDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateTaskDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> CreateTaskBoolDep()
        {
            return (await CreateTaskBoolDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateTaskBoolDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateTaskBoolDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateTaskBoolDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> CreateTaskBoolFalseDep()
        {
            return (await CreateTaskBoolFalseDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateTaskBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateTaskBoolFalseDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateTaskBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolFalseDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> CreateVoidDep(int? param)
        {
            return (await CreateVoidDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateVoidDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateVoidDep1Delegate), [param]);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCall<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateVoidDep(param, disposableDependency))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> CreateBoolTrueDep(int? param)
        {
            return (await CreateBoolTrueDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateBoolTrueDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateBoolTrueDep1Delegate), [param]);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolTrueDep(param, disposableDependency))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> CreateBoolFalseDep(int? param)
        {
            return (await CreateBoolFalseDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateBoolFalseDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateBoolFalseDep1Delegate), [param]);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateBoolFalseDep(param, disposableDependency))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> CreateTaskDep(int? param)
        {
            return (await CreateTaskDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateTaskDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateTaskDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> CreateTaskBoolDep(int? param)
        {
            return (await CreateTaskBoolDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteCreateTaskBoolDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(CreateTaskBoolDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalCreateTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Create, () => target.CreateTaskBoolDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> FetchVoid()
        {
            return (await FetchVoidProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchVoid()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchVoidDelegate), []);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchVoid()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCall<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoid())));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> FetchBool()
        {
            return (await FetchBoolProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchBoolDelegate), []);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchBool()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBool())));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> FetchTask()
        {
            return (await FetchTaskProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchTask()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchTaskDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchTask()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTask()));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> FetchTaskBool()
        {
            return (await FetchTaskBoolProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchTaskBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchTaskBoolDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchTaskBool()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBool()));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> FetchVoid(int? param)
        {
            return (await FetchVoid1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchVoid1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchVoid1Delegate), [param]);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchVoid1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCall<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoid(param))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> FetchBool(int? param)
        {
            return (await FetchBool1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchBool1Delegate), [param]);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBool(param))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> FetchTask(int? param)
        {
            return (await FetchTask1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchTask1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchTask1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchTask1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTask(param)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> FetchTaskBool(int? param)
        {
            return (await FetchTaskBool1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchTaskBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchTaskBool1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBool(param)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> FetchVoidDep()
        {
            return (await FetchVoidDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchVoidDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchVoidDepDelegate), []);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchVoidDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCall<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoidDep(disposableDependency))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> FetchBoolTrueDep()
        {
            return (await FetchBoolTrueDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchBoolTrueDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchBoolTrueDepDelegate), []);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchBoolTrueDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolTrueDep(disposableDependency))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> FetchBoolFalseDep()
        {
            return (await FetchBoolFalseDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchBoolFalseDepDelegate), []);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolFalseDep(disposableDependency))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> FetchTaskDep()
        {
            return (await FetchTaskDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchTaskDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchTaskDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchTaskDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> FetchTaskBoolDep()
        {
            return (await FetchTaskBoolDepProperty()).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchTaskBoolDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchTaskBoolDepDelegate), []);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchTaskBoolDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolDep(disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> FetchVoidDep(int? param)
        {
            return (await FetchVoidDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchVoidDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchVoidDep1Delegate), [param]);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCall<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchVoidDep(param, disposableDependency))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> FetchBoolTrueDep(int? param)
        {
            return (await FetchBoolTrueDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchBoolTrueDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchBoolTrueDep1Delegate), [param]);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolTrueDep(param, disposableDependency))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> FetchBoolFalseDep(int? param)
        {
            return (await FetchBoolFalseDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchBoolFalseDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchBoolFalseDep1Delegate), [param]);
        }

        public Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(new Authorized<ReadRemoteAuthDataMapper>(DoMapperMethodCallBool<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchBoolFalseDep(param, disposableDependency))));
        }

        public virtual async Task<ReadRemoteAuthDataMapper> FetchTaskDep(int? param)
        {
            return (await FetchTaskDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchTaskDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchTaskDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> FetchTaskBoolDep(int? param)
        {
            return (await FetchTaskBoolDep1Property(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchTaskBoolDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchTaskBoolDep1Delegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolDep(param, disposableDependency)));
        }

        public virtual async Task<ReadRemoteAuthDataMapper?> FetchTaskBoolFalseDep(int? param)
        {
            return (await FetchTaskBoolFalseDepProperty(param)).Result;
        }

        public virtual async Task<Authorized<ReadRemoteAuthDataMapper>> RemoteFetchTaskBoolFalseDep(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<ReadRemoteAuthDataMapper>>(typeof(FetchTaskBoolFalseDepDelegate), [param]);
        }

        public async Task<Authorized<ReadRemoteAuthDataMapper>> LocalFetchTaskBoolFalseDep(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadRemoteAuthDataMapper>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadRemoteAuthDataMapper>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<ReadRemoteAuthDataMapper>(await DoMapperMethodCallBoolAsync<ReadRemoteAuthDataMapper>(target, DataMapperMethod.Fetch, () => target.FetchTaskBoolFalseDep(param, disposableDependency)));
        }

        public virtual Task<Authorized> CanCreateVoid()
        {
            return CanCreateVoidProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateVoid()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateVoidDelegate), []);
        }

        public Task<Authorized> LocalCanCreateVoid()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateBool()
        {
            return CanCreateBoolProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateBoolDelegate), []);
        }

        public Task<Authorized> LocalCanCreateBool()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateTask()
        {
            return CanCreateTaskProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateTask()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskDelegate), []);
        }

        public Task<Authorized> LocalCanCreateTask()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateTaskBool()
        {
            return CanCreateTaskBoolProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskBoolDelegate), []);
        }

        public Task<Authorized> LocalCanCreateTaskBool()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateVoid(int? param)
        {
            return CanCreateVoid1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateVoid1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateVoid1Delegate), [param]);
        }

        public Task<Authorized> LocalCanCreateVoid1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateBool(int? param)
        {
            return CanCreateBool1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateBool1Delegate), [param]);
        }

        public Task<Authorized> LocalCanCreateBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateTask(int? param)
        {
            return CanCreateTask1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateTask1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTask1Delegate), [param]);
        }

        public Task<Authorized> LocalCanCreateTask1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateTaskBool(int? param)
        {
            return CanCreateTaskBool1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskBool1Delegate), [param]);
        }

        public Task<Authorized> LocalCanCreateTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateTaskBoolFalse(int? param)
        {
            return CanCreateTaskBoolFalseProperty(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskBoolFalse(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskBoolFalseDelegate), [param]);
        }

        public Task<Authorized> LocalCanCreateTaskBoolFalse(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateVoidDep()
        {
            return CanCreateVoidDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateVoidDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateVoidDepDelegate), []);
        }

        public Task<Authorized> LocalCanCreateVoidDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateBoolTrueDep()
        {
            return CanCreateBoolTrueDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateBoolTrueDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateBoolTrueDepDelegate), []);
        }

        public Task<Authorized> LocalCanCreateBoolTrueDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateBoolFalseDep()
        {
            return CanCreateBoolFalseDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateBoolFalseDepDelegate), []);
        }

        public Task<Authorized> LocalCanCreateBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateTaskDep()
        {
            return CanCreateTaskDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskDepDelegate), []);
        }

        public Task<Authorized> LocalCanCreateTaskDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateTaskBoolDep()
        {
            return CanCreateTaskBoolDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskBoolDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskBoolDepDelegate), []);
        }

        public Task<Authorized> LocalCanCreateTaskBoolDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateTaskBoolFalseDep()
        {
            return CanCreateTaskBoolFalseDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskBoolFalseDepDelegate), []);
        }

        public Task<Authorized> LocalCanCreateTaskBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateVoidDep(int? param)
        {
            return CanCreateVoidDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateVoidDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateVoidDep1Delegate), [param]);
        }

        public Task<Authorized> LocalCanCreateVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateBoolTrueDep(int? param)
        {
            return CanCreateBoolTrueDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateBoolTrueDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateBoolTrueDep1Delegate), [param]);
        }

        public Task<Authorized> LocalCanCreateBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateBoolFalseDep(int? param)
        {
            return CanCreateBoolFalseDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateBoolFalseDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateBoolFalseDep1Delegate), [param]);
        }

        public Task<Authorized> LocalCanCreateBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateTaskDep(int? param)
        {
            return CanCreateTaskDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskDep1Delegate), [param]);
        }

        public Task<Authorized> LocalCanCreateTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanCreateTaskBoolDep(int? param)
        {
            return CanCreateTaskBoolDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanCreateTaskBoolDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanCreateTaskBoolDep1Delegate), [param]);
        }

        public Task<Authorized> LocalCanCreateTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchVoid()
        {
            return CanFetchVoidProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchVoid()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchVoidDelegate), []);
        }

        public Task<Authorized> LocalCanFetchVoid()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchBool()
        {
            return CanFetchBoolProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchBoolDelegate), []);
        }

        public Task<Authorized> LocalCanFetchBool()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchTask()
        {
            return CanFetchTaskProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchTask()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskDelegate), []);
        }

        public Task<Authorized> LocalCanFetchTask()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchTaskBool()
        {
            return CanFetchTaskBoolProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskBool()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskBoolDelegate), []);
        }

        public Task<Authorized> LocalCanFetchTaskBool()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchVoid(int? param)
        {
            return CanFetchVoid1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchVoid1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchVoid1Delegate), [param]);
        }

        public Task<Authorized> LocalCanFetchVoid1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchBool(int? param)
        {
            return CanFetchBool1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchBool1Delegate), [param]);
        }

        public Task<Authorized> LocalCanFetchBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchTask(int? param)
        {
            return CanFetchTask1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchTask1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTask1Delegate), [param]);
        }

        public Task<Authorized> LocalCanFetchTask1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchTaskBool(int? param)
        {
            return CanFetchTaskBool1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskBool1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskBool1Delegate), [param]);
        }

        public Task<Authorized> LocalCanFetchTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchVoidDep()
        {
            return CanFetchVoidDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchVoidDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchVoidDepDelegate), []);
        }

        public Task<Authorized> LocalCanFetchVoidDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchBoolTrueDep()
        {
            return CanFetchBoolTrueDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchBoolTrueDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchBoolTrueDepDelegate), []);
        }

        public Task<Authorized> LocalCanFetchBoolTrueDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchBoolFalseDep()
        {
            return CanFetchBoolFalseDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchBoolFalseDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchBoolFalseDepDelegate), []);
        }

        public Task<Authorized> LocalCanFetchBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchTaskDep()
        {
            return CanFetchTaskDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskDepDelegate), []);
        }

        public Task<Authorized> LocalCanFetchTaskDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchTaskBoolDep()
        {
            return CanFetchTaskBoolDepProperty();
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskBoolDep()
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskBoolDepDelegate), []);
        }

        public Task<Authorized> LocalCanFetchTaskBoolDep()
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchVoidDep(int? param)
        {
            return CanFetchVoidDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchVoidDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchVoidDep1Delegate), [param]);
        }

        public Task<Authorized> LocalCanFetchVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchBoolTrueDep(int? param)
        {
            return CanFetchBoolTrueDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchBoolTrueDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchBoolTrueDep1Delegate), [param]);
        }

        public Task<Authorized> LocalCanFetchBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchBoolFalseDep(int? param)
        {
            return CanFetchBoolFalseDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchBoolFalseDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchBoolFalseDep1Delegate), [param]);
        }

        public Task<Authorized> LocalCanFetchBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchTaskDep(int? param)
        {
            return CanFetchTaskDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskDep1Delegate), [param]);
        }

        public Task<Authorized> LocalCanFetchTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchTaskBoolDep(int? param)
        {
            return CanFetchTaskBoolDep1Property(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskBoolDep1(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskBoolDep1Delegate), [param]);
        }

        public Task<Authorized> LocalCanFetchTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public virtual Task<Authorized> CanFetchTaskBoolFalseDep(int? param)
        {
            return CanFetchTaskBoolFalseDepProperty(param);
        }

        public virtual async Task<Authorized> RemoteCanFetchTaskBoolFalseDep(int? param)
        {
            return await DoRemoteRequest.ForDelegate<Authorized>(typeof(CanFetchTaskBoolFalseDepDelegate), [param]);
        }

        public Task<Authorized> LocalCanFetchTaskBoolFalseDep(int? param)
        {
            Authorized authorized;
            authorized = ReadRemoteAuth.CanReadRemoteBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanReadRemoteStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            authorized = ReadRemoteAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return Task.FromResult(authorized);
            }

            return Task.FromResult(new Authorized(true));
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<ReadRemoteAuthDataMapper>();
            services.AddScoped<ReadRemoteAuthDataMapperFactory>();
            services.AddScoped<IReadRemoteAuthDataMapperFactory, ReadRemoteAuthDataMapperFactory>();
            services.AddScoped<CreateVoidDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCreateVoid();
            });
            services.AddScoped<CreateBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCreateBool();
            });
            services.AddScoped<CreateTaskDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCreateTask();
            });
            services.AddScoped<CreateTaskBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCreateTaskBool();
            });
            services.AddScoped<CreateVoid1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCreateVoid1(param);
            });
            services.AddScoped<CreateBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCreateBool1(param);
            });
            services.AddScoped<CreateTask1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCreateTask1(param);
            });
            services.AddScoped<CreateTaskBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCreateTaskBool1(param);
            });
            services.AddScoped<CreateTaskBoolFalseDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCreateTaskBoolFalse(param);
            });
            services.AddScoped<CreateVoidDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCreateVoidDep();
            });
            services.AddScoped<CreateBoolTrueDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCreateBoolTrueDep();
            });
            services.AddScoped<CreateBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCreateBoolFalseDep();
            });
            services.AddScoped<CreateTaskDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCreateTaskDep();
            });
            services.AddScoped<CreateTaskBoolDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCreateTaskBoolDep();
            });
            services.AddScoped<CreateTaskBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCreateTaskBoolFalseDep();
            });
            services.AddScoped<CreateVoidDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCreateVoidDep1(param);
            });
            services.AddScoped<CreateBoolTrueDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCreateBoolTrueDep1(param);
            });
            services.AddScoped<CreateBoolFalseDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCreateBoolFalseDep1(param);
            });
            services.AddScoped<CreateTaskDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCreateTaskDep1(param);
            });
            services.AddScoped<CreateTaskBoolDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCreateTaskBoolDep1(param);
            });
            services.AddScoped<FetchVoidDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalFetchVoid();
            });
            services.AddScoped<FetchBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalFetchBool();
            });
            services.AddScoped<FetchTaskDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalFetchTask();
            });
            services.AddScoped<FetchTaskBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalFetchTaskBool();
            });
            services.AddScoped<FetchVoid1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalFetchVoid1(param);
            });
            services.AddScoped<FetchBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalFetchBool1(param);
            });
            services.AddScoped<FetchTask1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalFetchTask1(param);
            });
            services.AddScoped<FetchTaskBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalFetchTaskBool1(param);
            });
            services.AddScoped<FetchVoidDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalFetchVoidDep();
            });
            services.AddScoped<FetchBoolTrueDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalFetchBoolTrueDep();
            });
            services.AddScoped<FetchBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalFetchBoolFalseDep();
            });
            services.AddScoped<FetchTaskDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalFetchTaskDep();
            });
            services.AddScoped<FetchTaskBoolDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalFetchTaskBoolDep();
            });
            services.AddScoped<FetchVoidDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalFetchVoidDep1(param);
            });
            services.AddScoped<FetchBoolTrueDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalFetchBoolTrueDep1(param);
            });
            services.AddScoped<FetchBoolFalseDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalFetchBoolFalseDep1(param);
            });
            services.AddScoped<FetchTaskDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalFetchTaskDep1(param);
            });
            services.AddScoped<FetchTaskBoolDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalFetchTaskBoolDep1(param);
            });
            services.AddScoped<FetchTaskBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalFetchTaskBoolFalseDep(param);
            });
            services.AddScoped<CanCreateVoidDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanCreateVoid();
            });
            services.AddScoped<CanCreateBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanCreateBool();
            });
            services.AddScoped<CanCreateTaskDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanCreateTask();
            });
            services.AddScoped<CanCreateTaskBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanCreateTaskBool();
            });
            services.AddScoped<CanCreateVoid1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateVoid1(param);
            });
            services.AddScoped<CanCreateBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateBool1(param);
            });
            services.AddScoped<CanCreateTask1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateTask1(param);
            });
            services.AddScoped<CanCreateTaskBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateTaskBool1(param);
            });
            services.AddScoped<CanCreateTaskBoolFalseDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateTaskBoolFalse(param);
            });
            services.AddScoped<CanCreateVoidDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanCreateVoidDep();
            });
            services.AddScoped<CanCreateBoolTrueDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanCreateBoolTrueDep();
            });
            services.AddScoped<CanCreateBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanCreateBoolFalseDep();
            });
            services.AddScoped<CanCreateTaskDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanCreateTaskDep();
            });
            services.AddScoped<CanCreateTaskBoolDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanCreateTaskBoolDep();
            });
            services.AddScoped<CanCreateTaskBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanCreateTaskBoolFalseDep();
            });
            services.AddScoped<CanCreateVoidDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateVoidDep1(param);
            });
            services.AddScoped<CanCreateBoolTrueDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateBoolTrueDep1(param);
            });
            services.AddScoped<CanCreateBoolFalseDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateBoolFalseDep1(param);
            });
            services.AddScoped<CanCreateTaskDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateTaskDep1(param);
            });
            services.AddScoped<CanCreateTaskBoolDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanCreateTaskBoolDep1(param);
            });
            services.AddScoped<CanFetchVoidDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanFetchVoid();
            });
            services.AddScoped<CanFetchBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanFetchBool();
            });
            services.AddScoped<CanFetchTaskDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanFetchTask();
            });
            services.AddScoped<CanFetchTaskBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanFetchTaskBool();
            });
            services.AddScoped<CanFetchVoid1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchVoid1(param);
            });
            services.AddScoped<CanFetchBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchBool1(param);
            });
            services.AddScoped<CanFetchTask1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchTask1(param);
            });
            services.AddScoped<CanFetchTaskBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchTaskBool1(param);
            });
            services.AddScoped<CanFetchVoidDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanFetchVoidDep();
            });
            services.AddScoped<CanFetchBoolTrueDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanFetchBoolTrueDep();
            });
            services.AddScoped<CanFetchBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanFetchBoolFalseDep();
            });
            services.AddScoped<CanFetchTaskDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanFetchTaskDep();
            });
            services.AddScoped<CanFetchTaskBoolDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return () => factory.LocalCanFetchTaskBoolDep();
            });
            services.AddScoped<CanFetchVoidDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchVoidDep1(param);
            });
            services.AddScoped<CanFetchBoolTrueDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchBoolTrueDep1(param);
            });
            services.AddScoped<CanFetchBoolFalseDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchBoolFalseDep1(param);
            });
            services.AddScoped<CanFetchTaskDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchTaskDep1(param);
            });
            services.AddScoped<CanFetchTaskBoolDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchTaskBoolDep1(param);
            });
            services.AddScoped<CanFetchTaskBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<ReadRemoteAuthDataMapperFactory>();
                return (int? param) => factory.LocalCanFetchTaskBoolFalseDep(param);
            });
        }
    }
}