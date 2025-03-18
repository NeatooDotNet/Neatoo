using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using static Neatoo.UnitTest.RemoteFactory.MixedReturnTypeWriteTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.UnitTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
                    Debugging Messages:
                    Parent class: MixedReturnTypeWriteTests
: IEditMetaSaveProperties
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute
                    */
namespace Neatoo.UnitTest.RemoteFactory
{
    public interface IMixedReturnTypeWriteDataMapperFactory
    {
        MixedReturnTypeWriteDataMapper? SaveVoid(MixedReturnTypeWriteDataMapper target);
        Task<MixedReturnTypeWriteDataMapper?> SaveBool(MixedReturnTypeWriteDataMapper target);
        Task<MixedReturnTypeWriteDataMapper?> SaveTask(MixedReturnTypeWriteDataMapper target);
        Task<MixedReturnTypeWriteDataMapper?> SaveTaskBool(MixedReturnTypeWriteDataMapper target);
        Task<MixedReturnTypeWriteDataMapper?> SaveVoidDep(MixedReturnTypeWriteDataMapper target);
        Task<MixedReturnTypeWriteDataMapper?> SaveBoolTrueDep(MixedReturnTypeWriteDataMapper target);
        MixedReturnTypeWriteDataMapper? SaveBoolFalseDep(MixedReturnTypeWriteDataMapper target);
        Task<MixedReturnTypeWriteDataMapper?> SaveTaskDep(MixedReturnTypeWriteDataMapper target);
        Task<MixedReturnTypeWriteDataMapper?> SaveTaskBoolDep(MixedReturnTypeWriteDataMapper target);
        Task<MixedReturnTypeWriteDataMapper?> SaveTaskBoolFalseDep(MixedReturnTypeWriteDataMapper target);
        MixedReturnTypeWriteDataMapper? SaveVoid(MixedReturnTypeWriteDataMapper target, int? param);
        Task<MixedReturnTypeWriteDataMapper?> SaveBool(MixedReturnTypeWriteDataMapper target, int? param);
        Task<MixedReturnTypeWriteDataMapper?> SaveTask(MixedReturnTypeWriteDataMapper target, int? param);
        Task<MixedReturnTypeWriteDataMapper?> SaveTaskBool(MixedReturnTypeWriteDataMapper target, int? param);
        Task<MixedReturnTypeWriteDataMapper?> SaveTaskBoolFalse(MixedReturnTypeWriteDataMapper target, int? param);
        Task<MixedReturnTypeWriteDataMapper?> SaveVoidDep(MixedReturnTypeWriteDataMapper target, int? param);
        Task<MixedReturnTypeWriteDataMapper?> SaveBoolTrueDep(MixedReturnTypeWriteDataMapper target, int? param);
        Task<MixedReturnTypeWriteDataMapper?> SaveBoolFalseDep(MixedReturnTypeWriteDataMapper target, int? param);
        Task<MixedReturnTypeWriteDataMapper?> SaveTaskDep(MixedReturnTypeWriteDataMapper target, int? param);
        Task<MixedReturnTypeWriteDataMapper?> SaveTaskBoolDep(MixedReturnTypeWriteDataMapper target, int? param);
    }

    internal class MixedReturnTypeWriteDataMapperFactory : FactoryEditBase<MixedReturnTypeWriteDataMapper>, IFactoryEditBase<MixedReturnTypeWriteDataMapper>, IMixedReturnTypeWriteDataMapperFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        public delegate Task<MixedReturnTypeWriteDataMapper?> SaveVoidDepDelegate(MixedReturnTypeWriteDataMapper target);
        public delegate Task<MixedReturnTypeWriteDataMapper?> SaveBoolTrueDepDelegate(MixedReturnTypeWriteDataMapper target);
        public delegate Task<MixedReturnTypeWriteDataMapper?> SaveBool1Delegate(MixedReturnTypeWriteDataMapper target, int? param);
        public delegate Task<MixedReturnTypeWriteDataMapper?> SaveBoolTrueDep1Delegate(MixedReturnTypeWriteDataMapper target, int? param);
        public delegate Task<MixedReturnTypeWriteDataMapper?> SaveBoolFalseDep1Delegate(MixedReturnTypeWriteDataMapper target, int? param);
        public delegate Task<MixedReturnTypeWriteDataMapper?> SaveTaskDep1Delegate(MixedReturnTypeWriteDataMapper target, int? param);
        public delegate Task<MixedReturnTypeWriteDataMapper?> SaveTaskBoolDep1Delegate(MixedReturnTypeWriteDataMapper target, int? param);
        // Delegate Properties to provide Local or Remote fork in execution
        public SaveVoidDepDelegate SaveVoidDepProperty { get; }
        public SaveBoolTrueDepDelegate SaveBoolTrueDepProperty { get; }
        public SaveBool1Delegate SaveBool1Property { get; }
        public SaveBoolTrueDep1Delegate SaveBoolTrueDep1Property { get; }
        public SaveBoolFalseDep1Delegate SaveBoolFalseDep1Property { get; }
        public SaveTaskDep1Delegate SaveTaskDep1Property { get; }
        public SaveTaskBoolDep1Delegate SaveTaskBoolDep1Property { get; }

        public MixedReturnTypeWriteDataMapperFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            SaveVoidDepProperty = LocalSaveVoidDep;
            SaveBoolTrueDepProperty = LocalSaveBoolTrueDep;
            SaveBool1Property = LocalSaveBool1;
            SaveBoolTrueDep1Property = LocalSaveBoolTrueDep1;
            SaveBoolFalseDep1Property = LocalSaveBoolFalseDep1;
            SaveTaskDep1Property = LocalSaveTaskDep1;
            SaveTaskBoolDep1Property = LocalSaveTaskBoolDep1;
        }

        public MixedReturnTypeWriteDataMapperFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
            SaveVoidDepProperty = RemoteSaveVoidDep;
            SaveBoolTrueDepProperty = RemoteSaveBoolTrueDep;
            SaveBool1Property = RemoteSaveBool1;
            SaveBoolTrueDep1Property = RemoteSaveBoolTrueDep1;
            SaveBoolFalseDep1Property = RemoteSaveBoolFalseDep1;
            SaveTaskDep1Property = RemoteSaveTaskDep1;
            SaveTaskBoolDep1Property = RemoteSaveTaskBoolDep1;
        }

        public MixedReturnTypeWriteDataMapper LocalInsertVoid(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertVoid());
        }

        public MixedReturnTypeWriteDataMapper? LocalInsertBool(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBool());
        }

        public Task<MixedReturnTypeWriteDataMapper> LocalInsertTask(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTask());
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalInsertTaskBool(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBool());
        }

        public MixedReturnTypeWriteDataMapper LocalInsertVoid1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertVoid(param));
        }

        public MixedReturnTypeWriteDataMapper? LocalInsertBool1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBool(param));
        }

        public Task<MixedReturnTypeWriteDataMapper> LocalInsertTask1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTask(param));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalInsertTaskBool1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBool(param));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalInsertTaskBoolFalse(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBoolFalse(param));
        }

        public MixedReturnTypeWriteDataMapper LocalInsertVoidDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertVoidDep(disposableDependency));
        }

        public MixedReturnTypeWriteDataMapper? LocalInsertBoolTrueDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBoolTrueDep(disposableDependency));
        }

        public MixedReturnTypeWriteDataMapper? LocalInsertBoolFalseDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBoolFalseDep(disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper> LocalInsertTaskDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskDep(disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalInsertTaskBoolDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBoolDep(disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalInsertTaskBoolFalseDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBoolFalseDep(disposableDependency));
        }

        public MixedReturnTypeWriteDataMapper LocalInsertVoidDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertVoidDep(param, disposableDependency));
        }

        public MixedReturnTypeWriteDataMapper? LocalInsertBoolTrueDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBoolTrueDep(param, disposableDependency));
        }

        public MixedReturnTypeWriteDataMapper? LocalInsertBoolFalseDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBoolFalseDep(param, disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper> LocalInsertTaskDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskDep(param, disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalInsertTaskBoolDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBoolDep(param, disposableDependency));
        }

        public MixedReturnTypeWriteDataMapper LocalUpdateVoid(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateVoid());
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalUpdateBool(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBool());
        }

        public Task<MixedReturnTypeWriteDataMapper> LocalUpdateTask(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTask());
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalUpdateTaskBool(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBool());
        }

        public MixedReturnTypeWriteDataMapper LocalUpdateVoid1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateVoid(param));
        }

        public MixedReturnTypeWriteDataMapper LocalUpdateBool1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBool(param));
        }

        public MixedReturnTypeWriteDataMapper LocalUpdateTask1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTask(param));
        }

        public MixedReturnTypeWriteDataMapper LocalUpdateTaskBool1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBool(param));
        }

        public MixedReturnTypeWriteDataMapper LocalUpdateTaskBoolFalse(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBoolFalse(param));
        }

        public MixedReturnTypeWriteDataMapper LocalUpdateVoidDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateVoidDep(disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalUpdateBoolTrueDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBoolTrueDep(disposableDependency)));
        }

        public MixedReturnTypeWriteDataMapper? LocalUpdateBoolFalseDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBoolFalseDep(disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper> LocalUpdateTaskDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskDep(disposableDependency));
        }

        public MixedReturnTypeWriteDataMapper LocalUpdateTaskBoolDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBoolDep(disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalUpdateTaskBoolFalseDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBoolFalseDep(disposableDependency));
        }

        public MixedReturnTypeWriteDataMapper LocalUpdateVoidDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateVoidDep(param, disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalUpdateBoolTrueDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBoolTrueDep(param, disposableDependency)));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalUpdateBoolFalseDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBoolFalseDep(param, disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper> LocalUpdateTaskDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskDep(param, disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalUpdateTaskBoolDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBoolDep(param, disposableDependency));
        }

        public MixedReturnTypeWriteDataMapper LocalDeleteVoid(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteVoid());
        }

        public MixedReturnTypeWriteDataMapper? LocalDeleteBool(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBool());
        }

        public Task<MixedReturnTypeWriteDataMapper> LocalDeleteTask(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTask());
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalDeleteTaskBool(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBool());
        }

        public MixedReturnTypeWriteDataMapper LocalDeleteVoid1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteVoid(param));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalDeleteBool1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return Task.FromResult(DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBool(param)));
        }

        public MixedReturnTypeWriteDataMapper LocalDeleteTask1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTask(param));
        }

        public Task<MixedReturnTypeWriteDataMapper> LocalDeleteTaskBool1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBool(param));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalDeleteTaskBoolFalse(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBoolFalse(param));
        }

        public Task<MixedReturnTypeWriteDataMapper> LocalDeleteVoidDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteVoidDep(disposableDependency)));
        }

        public MixedReturnTypeWriteDataMapper? LocalDeleteBoolTrueDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBoolTrueDep(disposableDependency));
        }

        public MixedReturnTypeWriteDataMapper? LocalDeleteBoolFalseDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBoolFalseDep(disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper> LocalDeleteTaskDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskDep(disposableDependency));
        }

        public MixedReturnTypeWriteDataMapper? LocalDeleteTaskBoolDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBoolDep(disposableDependency));
        }

        public MixedReturnTypeWriteDataMapper LocalDeleteTaskBoolFalseDep(MixedReturnTypeWriteDataMapper target)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBoolFalseDep(disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalDeleteVoidDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteVoidDep(param, disposableDependency));
        }

        public MixedReturnTypeWriteDataMapper LocalDeleteBoolTrueDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBoolTrueDep(param, disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper?> LocalDeleteBoolFalseDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBoolFalseDep(param, disposableDependency)));
        }

        public Task<MixedReturnTypeWriteDataMapper> LocalDeleteTaskDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskDep(param, disposableDependency));
        }

        public Task<MixedReturnTypeWriteDataMapper> LocalDeleteTaskBoolDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            var cTarget = (MixedReturnTypeWriteDataMapper)target ?? throw new Exception("MixedReturnTypeWriteDataMapper must implement MixedReturnTypeWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCall<MixedReturnTypeWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBoolDep(param, disposableDependency)));
        }

        public virtual MixedReturnTypeWriteDataMapper? SaveVoid(MixedReturnTypeWriteDataMapper target)
        {
            return LocalSaveVoid(target);
        }

        async Task<IEditBase?> IFactoryEditBase<MixedReturnTypeWriteDataMapper>.Save(MixedReturnTypeWriteDataMapper target)
        {
            return await Task.FromResult((IEditBase? )SaveVoid(target));
        }

        public virtual MixedReturnTypeWriteDataMapper? LocalSaveVoid(MixedReturnTypeWriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteVoid(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertVoid(target);
            }
            else
            {
                return LocalUpdateVoid(target);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveBool(MixedReturnTypeWriteDataMapper target)
        {
            return LocalSaveBool(target);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> LocalSaveBool(MixedReturnTypeWriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteBool(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertBool(target);
            }
            else
            {
                return await LocalUpdateBool(target);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveTask(MixedReturnTypeWriteDataMapper target)
        {
            return LocalSaveTask(target);
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> LocalSaveTask(MixedReturnTypeWriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteTask(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertTask(target);
            }
            else
            {
                return LocalUpdateTask(target);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveTaskBool(MixedReturnTypeWriteDataMapper target)
        {
            return LocalSaveTaskBool(target);
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> LocalSaveTaskBool(MixedReturnTypeWriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteTaskBool(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskBool(target);
            }
            else
            {
                return LocalUpdateTaskBool(target);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveVoidDep(MixedReturnTypeWriteDataMapper target)
        {
            return SaveVoidDepProperty(target);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> RemoteSaveVoidDep(MixedReturnTypeWriteDataMapper target)
        {
            return await DoRemoteRequest.ForDelegate<MixedReturnTypeWriteDataMapper?>(typeof(SaveVoidDepDelegate), [target]);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> LocalSaveVoidDep(MixedReturnTypeWriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return await LocalDeleteVoidDep(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertVoidDep(target);
            }
            else
            {
                return LocalUpdateVoidDep(target);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveBoolTrueDep(MixedReturnTypeWriteDataMapper target)
        {
            return SaveBoolTrueDepProperty(target);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> RemoteSaveBoolTrueDep(MixedReturnTypeWriteDataMapper target)
        {
            return await DoRemoteRequest.ForDelegate<MixedReturnTypeWriteDataMapper?>(typeof(SaveBoolTrueDepDelegate), [target]);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> LocalSaveBoolTrueDep(MixedReturnTypeWriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteBoolTrueDep(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertBoolTrueDep(target);
            }
            else
            {
                return await LocalUpdateBoolTrueDep(target);
            }
        }

        public virtual MixedReturnTypeWriteDataMapper? SaveBoolFalseDep(MixedReturnTypeWriteDataMapper target)
        {
            return LocalSaveBoolFalseDep(target);
        }

        public virtual MixedReturnTypeWriteDataMapper? LocalSaveBoolFalseDep(MixedReturnTypeWriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteBoolFalseDep(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertBoolFalseDep(target);
            }
            else
            {
                return LocalUpdateBoolFalseDep(target);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveTaskDep(MixedReturnTypeWriteDataMapper target)
        {
            return LocalSaveTaskDep(target);
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> LocalSaveTaskDep(MixedReturnTypeWriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteTaskDep(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskDep(target);
            }
            else
            {
                return LocalUpdateTaskDep(target);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveTaskBoolDep(MixedReturnTypeWriteDataMapper target)
        {
            return LocalSaveTaskBoolDep(target);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> LocalSaveTaskBoolDep(MixedReturnTypeWriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteTaskBoolDep(target);
            }
            else if (target.IsNew)
            {
                return await LocalInsertTaskBoolDep(target);
            }
            else
            {
                return LocalUpdateTaskBoolDep(target);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveTaskBoolFalseDep(MixedReturnTypeWriteDataMapper target)
        {
            return LocalSaveTaskBoolFalseDep(target);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> LocalSaveTaskBoolFalseDep(MixedReturnTypeWriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteTaskBoolFalseDep(target);
            }
            else if (target.IsNew)
            {
                return await LocalInsertTaskBoolFalseDep(target);
            }
            else
            {
                return await LocalUpdateTaskBoolFalseDep(target);
            }
        }

        public virtual MixedReturnTypeWriteDataMapper? SaveVoid(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return LocalSaveVoid1(target, param);
        }

        public virtual MixedReturnTypeWriteDataMapper? LocalSaveVoid1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteVoid1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertVoid1(target, param);
            }
            else
            {
                return LocalUpdateVoid1(target, param);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveBool(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return SaveBool1Property(target, param);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> RemoteSaveBool1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<MixedReturnTypeWriteDataMapper?>(typeof(SaveBool1Delegate), [target, param]);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> LocalSaveBool1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return await LocalDeleteBool1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertBool1(target, param);
            }
            else
            {
                return LocalUpdateBool1(target, param);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveTask(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return LocalSaveTask1(target, param);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> LocalSaveTask1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteTask1(target, param);
            }
            else if (target.IsNew)
            {
                return await LocalInsertTask1(target, param);
            }
            else
            {
                return LocalUpdateTask1(target, param);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveTaskBool(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return LocalSaveTaskBool1(target, param);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> LocalSaveTaskBool1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return await LocalDeleteTaskBool1(target, param);
            }
            else if (target.IsNew)
            {
                return await LocalInsertTaskBool1(target, param);
            }
            else
            {
                return LocalUpdateTaskBool1(target, param);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveTaskBoolFalse(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return LocalSaveTaskBoolFalse(target, param);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> LocalSaveTaskBoolFalse(MixedReturnTypeWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return await LocalDeleteTaskBoolFalse(target, param);
            }
            else if (target.IsNew)
            {
                return await LocalInsertTaskBoolFalse(target, param);
            }
            else
            {
                return LocalUpdateTaskBoolFalse(target, param);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveVoidDep(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return LocalSaveVoidDep1(target, param);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> LocalSaveVoidDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return await LocalDeleteVoidDep1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertVoidDep1(target, param);
            }
            else
            {
                return LocalUpdateVoidDep1(target, param);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveBoolTrueDep(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return SaveBoolTrueDep1Property(target, param);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> RemoteSaveBoolTrueDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<MixedReturnTypeWriteDataMapper?>(typeof(SaveBoolTrueDep1Delegate), [target, param]);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> LocalSaveBoolTrueDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteBoolTrueDep1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertBoolTrueDep1(target, param);
            }
            else
            {
                return await LocalUpdateBoolTrueDep1(target, param);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveBoolFalseDep(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return SaveBoolFalseDep1Property(target, param);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> RemoteSaveBoolFalseDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<MixedReturnTypeWriteDataMapper?>(typeof(SaveBoolFalseDep1Delegate), [target, param]);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> LocalSaveBoolFalseDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return await LocalDeleteBoolFalseDep1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertBoolFalseDep1(target, param);
            }
            else
            {
                return await LocalUpdateBoolFalseDep1(target, param);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveTaskDep(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return SaveTaskDep1Property(target, param);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> RemoteSaveTaskDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<MixedReturnTypeWriteDataMapper?>(typeof(SaveTaskDep1Delegate), [target, param]);
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> LocalSaveTaskDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteTaskDep1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskDep1(target, param);
            }
            else
            {
                return LocalUpdateTaskDep1(target, param);
            }
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> SaveTaskBoolDep(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return SaveTaskBoolDep1Property(target, param);
        }

        public virtual async Task<MixedReturnTypeWriteDataMapper?> RemoteSaveTaskBoolDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<MixedReturnTypeWriteDataMapper?>(typeof(SaveTaskBoolDep1Delegate), [target, param]);
        }

        public virtual Task<MixedReturnTypeWriteDataMapper?> LocalSaveTaskBoolDep1(MixedReturnTypeWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteTaskBoolDep1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskBoolDep1(target, param);
            }
            else
            {
                return LocalUpdateTaskBoolDep1(target, param);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<MixedReturnTypeWriteDataMapper>();
            services.AddScoped<MixedReturnTypeWriteDataMapperFactory>();
            services.AddScoped<IMixedReturnTypeWriteDataMapperFactory, MixedReturnTypeWriteDataMapperFactory>();
            services.AddScoped<SaveVoidDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<MixedReturnTypeWriteDataMapperFactory>();
                return (MixedReturnTypeWriteDataMapper target) => factory.LocalSaveVoidDep(target);
            });
            services.AddScoped<SaveBoolTrueDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<MixedReturnTypeWriteDataMapperFactory>();
                return (MixedReturnTypeWriteDataMapper target) => factory.LocalSaveBoolTrueDep(target);
            });
            services.AddScoped<SaveBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<MixedReturnTypeWriteDataMapperFactory>();
                return (MixedReturnTypeWriteDataMapper target, int? param) => factory.LocalSaveBool1(target, param);
            });
            services.AddScoped<SaveBoolTrueDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<MixedReturnTypeWriteDataMapperFactory>();
                return (MixedReturnTypeWriteDataMapper target, int? param) => factory.LocalSaveBoolTrueDep1(target, param);
            });
            services.AddScoped<SaveBoolFalseDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<MixedReturnTypeWriteDataMapperFactory>();
                return (MixedReturnTypeWriteDataMapper target, int? param) => factory.LocalSaveBoolFalseDep1(target, param);
            });
            services.AddScoped<SaveTaskDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<MixedReturnTypeWriteDataMapperFactory>();
                return (MixedReturnTypeWriteDataMapper target, int? param) => factory.LocalSaveTaskDep1(target, param);
            });
            services.AddScoped<SaveTaskBoolDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<MixedReturnTypeWriteDataMapperFactory>();
                return (MixedReturnTypeWriteDataMapper target, int? param) => factory.LocalSaveTaskBoolDep1(target, param);
            });
            services.AddScoped<IFactoryEditBase<MixedReturnTypeWriteDataMapper>, MixedReturnTypeWriteDataMapperFactory>();
        }
    }
}