using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using static Neatoo.UnitTest.RemoteFactory.RemoteWriteDataMapperTests;
using Microsoft.Extensions.DependencyInjection;
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
                    Parent class: RemoteWriteDataMapperTests
: IEditMetaSaveProperties
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute
                    */
namespace Neatoo.UnitTest.RemoteFactory
{
    public interface IRemoteWriteDataMapperFactory
    {
        Task<RemoteWriteDataMapper?> SaveVoid(RemoteWriteDataMapper target);
        Task<RemoteWriteDataMapper?> SaveBool(RemoteWriteDataMapper target);
        Task<RemoteWriteDataMapper?> SaveTask(RemoteWriteDataMapper target);
        Task<RemoteWriteDataMapper?> SaveTaskBool(RemoteWriteDataMapper target);
        Task<RemoteWriteDataMapper?> SaveVoidDep(RemoteWriteDataMapper target);
        Task<RemoteWriteDataMapper?> SaveBoolTrueDep(RemoteWriteDataMapper target);
        Task<RemoteWriteDataMapper?> SaveBoolFalseDep(RemoteWriteDataMapper target);
        Task<RemoteWriteDataMapper?> SaveTaskDep(RemoteWriteDataMapper target);
        Task<RemoteWriteDataMapper?> SaveTaskBoolDep(RemoteWriteDataMapper target);
        Task<RemoteWriteDataMapper?> SaveTaskBoolFalseDep(RemoteWriteDataMapper target);
        Task<RemoteWriteDataMapper?> SaveVoid(RemoteWriteDataMapper target, int? param);
        Task<RemoteWriteDataMapper?> SaveBool(RemoteWriteDataMapper target, int? param);
        Task<RemoteWriteDataMapper?> SaveTask(RemoteWriteDataMapper target, int? param);
        Task<RemoteWriteDataMapper?> SaveTaskBool(RemoteWriteDataMapper target, int? param);
        Task<RemoteWriteDataMapper?> SaveTaskBoolFalse(RemoteWriteDataMapper target, int? param);
        Task<RemoteWriteDataMapper?> SaveVoidDep(RemoteWriteDataMapper target, int? param);
        Task<RemoteWriteDataMapper?> SaveBoolTrueDep(RemoteWriteDataMapper target, int? param);
        Task<RemoteWriteDataMapper?> SaveBoolFalseDep(RemoteWriteDataMapper target, int? param);
        Task<RemoteWriteDataMapper?> SaveTaskDep(RemoteWriteDataMapper target, int? param);
        Task<RemoteWriteDataMapper?> SaveTaskBoolDep(RemoteWriteDataMapper target, int? param);
    }

    internal class RemoteWriteDataMapperFactory : FactoryEditBase<RemoteWriteDataMapper>, IFactoryEditBase<RemoteWriteDataMapper>, IRemoteWriteDataMapperFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        public delegate Task<RemoteWriteDataMapper?> SaveVoidDelegate(RemoteWriteDataMapper target);
        public delegate Task<RemoteWriteDataMapper?> SaveBoolDelegate(RemoteWriteDataMapper target);
        public delegate Task<RemoteWriteDataMapper?> SaveTaskDelegate(RemoteWriteDataMapper target);
        public delegate Task<RemoteWriteDataMapper?> SaveTaskBoolDelegate(RemoteWriteDataMapper target);
        public delegate Task<RemoteWriteDataMapper?> SaveVoidDepDelegate(RemoteWriteDataMapper target);
        public delegate Task<RemoteWriteDataMapper?> SaveBoolTrueDepDelegate(RemoteWriteDataMapper target);
        public delegate Task<RemoteWriteDataMapper?> SaveBoolFalseDepDelegate(RemoteWriteDataMapper target);
        public delegate Task<RemoteWriteDataMapper?> SaveTaskDepDelegate(RemoteWriteDataMapper target);
        public delegate Task<RemoteWriteDataMapper?> SaveTaskBoolDepDelegate(RemoteWriteDataMapper target);
        public delegate Task<RemoteWriteDataMapper?> SaveTaskBoolFalseDepDelegate(RemoteWriteDataMapper target);
        public delegate Task<RemoteWriteDataMapper?> SaveVoid1Delegate(RemoteWriteDataMapper target, int? param);
        public delegate Task<RemoteWriteDataMapper?> SaveBool1Delegate(RemoteWriteDataMapper target, int? param);
        public delegate Task<RemoteWriteDataMapper?> SaveTask1Delegate(RemoteWriteDataMapper target, int? param);
        public delegate Task<RemoteWriteDataMapper?> SaveTaskBool1Delegate(RemoteWriteDataMapper target, int? param);
        public delegate Task<RemoteWriteDataMapper?> SaveTaskBoolFalseDelegate(RemoteWriteDataMapper target, int? param);
        public delegate Task<RemoteWriteDataMapper?> SaveVoidDep1Delegate(RemoteWriteDataMapper target, int? param);
        public delegate Task<RemoteWriteDataMapper?> SaveBoolTrueDep1Delegate(RemoteWriteDataMapper target, int? param);
        public delegate Task<RemoteWriteDataMapper?> SaveBoolFalseDep1Delegate(RemoteWriteDataMapper target, int? param);
        public delegate Task<RemoteWriteDataMapper?> SaveTaskDep1Delegate(RemoteWriteDataMapper target, int? param);
        public delegate Task<RemoteWriteDataMapper?> SaveTaskBoolDep1Delegate(RemoteWriteDataMapper target, int? param);
        // Delegate Properties to provide Local or Remote fork in execution
        public SaveVoidDelegate SaveVoidProperty { get; }
        public SaveBoolDelegate SaveBoolProperty { get; }
        public SaveTaskDelegate SaveTaskProperty { get; }
        public SaveTaskBoolDelegate SaveTaskBoolProperty { get; }
        public SaveVoidDepDelegate SaveVoidDepProperty { get; }
        public SaveBoolTrueDepDelegate SaveBoolTrueDepProperty { get; }
        public SaveBoolFalseDepDelegate SaveBoolFalseDepProperty { get; }
        public SaveTaskDepDelegate SaveTaskDepProperty { get; }
        public SaveTaskBoolDepDelegate SaveTaskBoolDepProperty { get; }
        public SaveTaskBoolFalseDepDelegate SaveTaskBoolFalseDepProperty { get; }
        public SaveVoid1Delegate SaveVoid1Property { get; }
        public SaveBool1Delegate SaveBool1Property { get; }
        public SaveTask1Delegate SaveTask1Property { get; }
        public SaveTaskBool1Delegate SaveTaskBool1Property { get; }
        public SaveTaskBoolFalseDelegate SaveTaskBoolFalseProperty { get; }
        public SaveVoidDep1Delegate SaveVoidDep1Property { get; }
        public SaveBoolTrueDep1Delegate SaveBoolTrueDep1Property { get; }
        public SaveBoolFalseDep1Delegate SaveBoolFalseDep1Property { get; }
        public SaveTaskDep1Delegate SaveTaskDep1Property { get; }
        public SaveTaskBoolDep1Delegate SaveTaskBoolDep1Property { get; }

        public RemoteWriteDataMapperFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            SaveVoidProperty = LocalSaveVoid;
            SaveBoolProperty = LocalSaveBool;
            SaveTaskProperty = LocalSaveTask;
            SaveTaskBoolProperty = LocalSaveTaskBool;
            SaveVoidDepProperty = LocalSaveVoidDep;
            SaveBoolTrueDepProperty = LocalSaveBoolTrueDep;
            SaveBoolFalseDepProperty = LocalSaveBoolFalseDep;
            SaveTaskDepProperty = LocalSaveTaskDep;
            SaveTaskBoolDepProperty = LocalSaveTaskBoolDep;
            SaveTaskBoolFalseDepProperty = LocalSaveTaskBoolFalseDep;
            SaveVoid1Property = LocalSaveVoid1;
            SaveBool1Property = LocalSaveBool1;
            SaveTask1Property = LocalSaveTask1;
            SaveTaskBool1Property = LocalSaveTaskBool1;
            SaveTaskBoolFalseProperty = LocalSaveTaskBoolFalse;
            SaveVoidDep1Property = LocalSaveVoidDep1;
            SaveBoolTrueDep1Property = LocalSaveBoolTrueDep1;
            SaveBoolFalseDep1Property = LocalSaveBoolFalseDep1;
            SaveTaskDep1Property = LocalSaveTaskDep1;
            SaveTaskBoolDep1Property = LocalSaveTaskBoolDep1;
        }

        public RemoteWriteDataMapperFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
            SaveVoidProperty = RemoteSaveVoid;
            SaveBoolProperty = RemoteSaveBool;
            SaveTaskProperty = RemoteSaveTask;
            SaveTaskBoolProperty = RemoteSaveTaskBool;
            SaveVoidDepProperty = RemoteSaveVoidDep;
            SaveBoolTrueDepProperty = RemoteSaveBoolTrueDep;
            SaveBoolFalseDepProperty = RemoteSaveBoolFalseDep;
            SaveTaskDepProperty = RemoteSaveTaskDep;
            SaveTaskBoolDepProperty = RemoteSaveTaskBoolDep;
            SaveTaskBoolFalseDepProperty = RemoteSaveTaskBoolFalseDep;
            SaveVoid1Property = RemoteSaveVoid1;
            SaveBool1Property = RemoteSaveBool1;
            SaveTask1Property = RemoteSaveTask1;
            SaveTaskBool1Property = RemoteSaveTaskBool1;
            SaveTaskBoolFalseProperty = RemoteSaveTaskBoolFalse;
            SaveVoidDep1Property = RemoteSaveVoidDep1;
            SaveBoolTrueDep1Property = RemoteSaveBoolTrueDep1;
            SaveBoolFalseDep1Property = RemoteSaveBoolFalseDep1;
            SaveTaskDep1Property = RemoteSaveTaskDep1;
            SaveTaskBoolDep1Property = RemoteSaveTaskBoolDep1;
        }

        public Task<RemoteWriteDataMapper> LocalInsertVoid(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return Task.FromResult(DoMapperMethodCall<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertVoid()));
        }

        public Task<RemoteWriteDataMapper?> LocalInsertBool(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBool()));
        }

        public Task<RemoteWriteDataMapper> LocalInsertTask(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTask());
        }

        public Task<RemoteWriteDataMapper?> LocalInsertTaskBool(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBool());
        }

        public Task<RemoteWriteDataMapper> LocalInsertVoid1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return Task.FromResult(DoMapperMethodCall<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertVoid(param)));
        }

        public Task<RemoteWriteDataMapper?> LocalInsertBool1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBool(param)));
        }

        public Task<RemoteWriteDataMapper> LocalInsertTask1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTask(param));
        }

        public Task<RemoteWriteDataMapper?> LocalInsertTaskBool1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBool(param));
        }

        public Task<RemoteWriteDataMapper?> LocalInsertTaskBoolFalse(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBoolFalse(param));
        }

        public Task<RemoteWriteDataMapper> LocalInsertVoidDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCall<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertVoidDep(disposableDependency)));
        }

        public Task<RemoteWriteDataMapper?> LocalInsertBoolTrueDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBoolTrueDep(disposableDependency)));
        }

        public Task<RemoteWriteDataMapper?> LocalInsertBoolFalseDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBoolFalseDep(disposableDependency)));
        }

        public Task<RemoteWriteDataMapper> LocalInsertTaskDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskDep(disposableDependency));
        }

        public Task<RemoteWriteDataMapper?> LocalInsertTaskBoolDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBoolDep(disposableDependency));
        }

        public Task<RemoteWriteDataMapper?> LocalInsertTaskBoolFalseDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBoolFalseDep(disposableDependency));
        }

        public Task<RemoteWriteDataMapper> LocalInsertVoidDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCall<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertVoidDep(param, disposableDependency)));
        }

        public Task<RemoteWriteDataMapper?> LocalInsertBoolTrueDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBoolTrueDep(param, disposableDependency)));
        }

        public Task<RemoteWriteDataMapper?> LocalInsertBoolFalseDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBoolFalseDep(param, disposableDependency)));
        }

        public Task<RemoteWriteDataMapper> LocalInsertTaskDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskDep(param, disposableDependency));
        }

        public Task<RemoteWriteDataMapper?> LocalInsertTaskBoolDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBoolDep(param, disposableDependency));
        }

        public Task<RemoteWriteDataMapper> LocalUpdateVoid(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return Task.FromResult(DoMapperMethodCall<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateVoid()));
        }

        public Task<RemoteWriteDataMapper?> LocalUpdateBool(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBool()));
        }

        public Task<RemoteWriteDataMapper> LocalUpdateTask(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTask());
        }

        public Task<RemoteWriteDataMapper?> LocalUpdateTaskBool(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBool());
        }

        public Task<RemoteWriteDataMapper> LocalUpdateVoid1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return Task.FromResult(DoMapperMethodCall<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateVoid(param)));
        }

        public Task<RemoteWriteDataMapper?> LocalUpdateBool1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBool(param)));
        }

        public Task<RemoteWriteDataMapper> LocalUpdateTask1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTask(param));
        }

        public Task<RemoteWriteDataMapper?> LocalUpdateTaskBool1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBool(param));
        }

        public Task<RemoteWriteDataMapper?> LocalUpdateTaskBoolFalse(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBoolFalse(param));
        }

        public Task<RemoteWriteDataMapper> LocalUpdateVoidDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCall<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateVoidDep(disposableDependency)));
        }

        public Task<RemoteWriteDataMapper?> LocalUpdateBoolTrueDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBoolTrueDep(disposableDependency)));
        }

        public Task<RemoteWriteDataMapper?> LocalUpdateBoolFalseDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBoolFalseDep(disposableDependency)));
        }

        public Task<RemoteWriteDataMapper> LocalUpdateTaskDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskDep(disposableDependency));
        }

        public Task<RemoteWriteDataMapper?> LocalUpdateTaskBoolDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBoolDep(disposableDependency));
        }

        public Task<RemoteWriteDataMapper?> LocalUpdateTaskBoolFalseDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBoolFalseDep(disposableDependency));
        }

        public Task<RemoteWriteDataMapper> LocalUpdateVoidDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCall<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateVoidDep(param, disposableDependency)));
        }

        public Task<RemoteWriteDataMapper?> LocalUpdateBoolTrueDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBoolTrueDep(param, disposableDependency)));
        }

        public Task<RemoteWriteDataMapper?> LocalUpdateBoolFalseDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBoolFalseDep(param, disposableDependency)));
        }

        public Task<RemoteWriteDataMapper> LocalUpdateTaskDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskDep(param, disposableDependency));
        }

        public Task<RemoteWriteDataMapper?> LocalUpdateTaskBoolDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBoolDep(param, disposableDependency));
        }

        public Task<RemoteWriteDataMapper> LocalDeleteVoid(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return Task.FromResult(DoMapperMethodCall<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteVoid()));
        }

        public Task<RemoteWriteDataMapper?> LocalDeleteBool(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBool()));
        }

        public Task<RemoteWriteDataMapper> LocalDeleteTask(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTask());
        }

        public Task<RemoteWriteDataMapper?> LocalDeleteTaskBool(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBool());
        }

        public Task<RemoteWriteDataMapper> LocalDeleteVoid1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return Task.FromResult(DoMapperMethodCall<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteVoid(param)));
        }

        public Task<RemoteWriteDataMapper?> LocalDeleteBool1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBool(param)));
        }

        public Task<RemoteWriteDataMapper> LocalDeleteTask1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTask(param));
        }

        public Task<RemoteWriteDataMapper?> LocalDeleteTaskBool1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBool(param));
        }

        public Task<RemoteWriteDataMapper?> LocalDeleteTaskBoolFalse(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBoolFalse(param));
        }

        public Task<RemoteWriteDataMapper> LocalDeleteVoidDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCall<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteVoidDep(disposableDependency)));
        }

        public Task<RemoteWriteDataMapper?> LocalDeleteBoolTrueDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBoolTrueDep(disposableDependency)));
        }

        public Task<RemoteWriteDataMapper?> LocalDeleteBoolFalseDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBoolFalseDep(disposableDependency)));
        }

        public Task<RemoteWriteDataMapper> LocalDeleteTaskDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskDep(disposableDependency));
        }

        public Task<RemoteWriteDataMapper?> LocalDeleteTaskBoolDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBoolDep(disposableDependency));
        }

        public Task<RemoteWriteDataMapper?> LocalDeleteTaskBoolFalseDep(RemoteWriteDataMapper target)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBoolFalseDep(disposableDependency));
        }

        public Task<RemoteWriteDataMapper> LocalDeleteVoidDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCall<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteVoidDep(param, disposableDependency)));
        }

        public Task<RemoteWriteDataMapper?> LocalDeleteBoolTrueDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBoolTrueDep(param, disposableDependency)));
        }

        public Task<RemoteWriteDataMapper?> LocalDeleteBoolFalseDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(DoMapperMethodCallBool<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBoolFalseDep(param, disposableDependency)));
        }

        public Task<RemoteWriteDataMapper> LocalDeleteTaskDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskDep(param, disposableDependency));
        }

        public Task<RemoteWriteDataMapper?> LocalDeleteTaskBoolDep1(RemoteWriteDataMapper target, int? param)
        {
            var cTarget = (RemoteWriteDataMapper)target ?? throw new Exception("RemoteWriteDataMapper must implement RemoteWriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<RemoteWriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBoolDep(param, disposableDependency));
        }

        public virtual Task<RemoteWriteDataMapper?> SaveVoid(RemoteWriteDataMapper target)
        {
            return SaveVoidProperty(target);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveVoid(RemoteWriteDataMapper target)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveVoidDelegate), [target]);
        }

        async Task<IEditBase?> IFactoryEditBase<RemoteWriteDataMapper>.Save(RemoteWriteDataMapper target)
        {
            return (IEditBase? )await SaveVoid(target);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveVoid(RemoteWriteDataMapper target)
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

        public virtual Task<RemoteWriteDataMapper?> SaveBool(RemoteWriteDataMapper target)
        {
            return SaveBoolProperty(target);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveBool(RemoteWriteDataMapper target)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveBoolDelegate), [target]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveBool(RemoteWriteDataMapper target)
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
                return LocalUpdateBool(target);
            }
        }

        public virtual Task<RemoteWriteDataMapper?> SaveTask(RemoteWriteDataMapper target)
        {
            return SaveTaskProperty(target);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveTask(RemoteWriteDataMapper target)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveTaskDelegate), [target]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveTask(RemoteWriteDataMapper target)
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

        public virtual Task<RemoteWriteDataMapper?> SaveTaskBool(RemoteWriteDataMapper target)
        {
            return SaveTaskBoolProperty(target);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveTaskBool(RemoteWriteDataMapper target)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveTaskBoolDelegate), [target]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveTaskBool(RemoteWriteDataMapper target)
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

        public virtual Task<RemoteWriteDataMapper?> SaveVoidDep(RemoteWriteDataMapper target)
        {
            return SaveVoidDepProperty(target);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveVoidDep(RemoteWriteDataMapper target)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveVoidDepDelegate), [target]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveVoidDep(RemoteWriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteVoidDep(target);
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

        public virtual Task<RemoteWriteDataMapper?> SaveBoolTrueDep(RemoteWriteDataMapper target)
        {
            return SaveBoolTrueDepProperty(target);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveBoolTrueDep(RemoteWriteDataMapper target)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveBoolTrueDepDelegate), [target]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveBoolTrueDep(RemoteWriteDataMapper target)
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
                return LocalUpdateBoolTrueDep(target);
            }
        }

        public virtual Task<RemoteWriteDataMapper?> SaveBoolFalseDep(RemoteWriteDataMapper target)
        {
            return SaveBoolFalseDepProperty(target);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveBoolFalseDep(RemoteWriteDataMapper target)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveBoolFalseDepDelegate), [target]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveBoolFalseDep(RemoteWriteDataMapper target)
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

        public virtual Task<RemoteWriteDataMapper?> SaveTaskDep(RemoteWriteDataMapper target)
        {
            return SaveTaskDepProperty(target);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveTaskDep(RemoteWriteDataMapper target)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveTaskDepDelegate), [target]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveTaskDep(RemoteWriteDataMapper target)
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

        public virtual Task<RemoteWriteDataMapper?> SaveTaskBoolDep(RemoteWriteDataMapper target)
        {
            return SaveTaskBoolDepProperty(target);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveTaskBoolDep(RemoteWriteDataMapper target)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveTaskBoolDepDelegate), [target]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveTaskBoolDep(RemoteWriteDataMapper target)
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
                return LocalInsertTaskBoolDep(target);
            }
            else
            {
                return LocalUpdateTaskBoolDep(target);
            }
        }

        public virtual Task<RemoteWriteDataMapper?> SaveTaskBoolFalseDep(RemoteWriteDataMapper target)
        {
            return SaveTaskBoolFalseDepProperty(target);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveTaskBoolFalseDep(RemoteWriteDataMapper target)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveTaskBoolFalseDepDelegate), [target]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveTaskBoolFalseDep(RemoteWriteDataMapper target)
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
                return LocalInsertTaskBoolFalseDep(target);
            }
            else
            {
                return LocalUpdateTaskBoolFalseDep(target);
            }
        }

        public virtual Task<RemoteWriteDataMapper?> SaveVoid(RemoteWriteDataMapper target, int? param)
        {
            return SaveVoid1Property(target, param);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveVoid1(RemoteWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveVoid1Delegate), [target, param]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveVoid1(RemoteWriteDataMapper target, int? param)
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

        public virtual Task<RemoteWriteDataMapper?> SaveBool(RemoteWriteDataMapper target, int? param)
        {
            return SaveBool1Property(target, param);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveBool1(RemoteWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveBool1Delegate), [target, param]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveBool1(RemoteWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteBool1(target, param);
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

        public virtual Task<RemoteWriteDataMapper?> SaveTask(RemoteWriteDataMapper target, int? param)
        {
            return SaveTask1Property(target, param);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveTask1(RemoteWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveTask1Delegate), [target, param]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveTask1(RemoteWriteDataMapper target, int? param)
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
                return LocalInsertTask1(target, param);
            }
            else
            {
                return LocalUpdateTask1(target, param);
            }
        }

        public virtual Task<RemoteWriteDataMapper?> SaveTaskBool(RemoteWriteDataMapper target, int? param)
        {
            return SaveTaskBool1Property(target, param);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveTaskBool1(RemoteWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveTaskBool1Delegate), [target, param]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveTaskBool1(RemoteWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteTaskBool1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskBool1(target, param);
            }
            else
            {
                return LocalUpdateTaskBool1(target, param);
            }
        }

        public virtual Task<RemoteWriteDataMapper?> SaveTaskBoolFalse(RemoteWriteDataMapper target, int? param)
        {
            return SaveTaskBoolFalseProperty(target, param);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveTaskBoolFalse(RemoteWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveTaskBoolFalseDelegate), [target, param]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveTaskBoolFalse(RemoteWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteTaskBoolFalse(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskBoolFalse(target, param);
            }
            else
            {
                return LocalUpdateTaskBoolFalse(target, param);
            }
        }

        public virtual Task<RemoteWriteDataMapper?> SaveVoidDep(RemoteWriteDataMapper target, int? param)
        {
            return SaveVoidDep1Property(target, param);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveVoidDep1(RemoteWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveVoidDep1Delegate), [target, param]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveVoidDep1(RemoteWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteVoidDep1(target, param);
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

        public virtual Task<RemoteWriteDataMapper?> SaveBoolTrueDep(RemoteWriteDataMapper target, int? param)
        {
            return SaveBoolTrueDep1Property(target, param);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveBoolTrueDep1(RemoteWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveBoolTrueDep1Delegate), [target, param]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveBoolTrueDep1(RemoteWriteDataMapper target, int? param)
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
                return LocalUpdateBoolTrueDep1(target, param);
            }
        }

        public virtual Task<RemoteWriteDataMapper?> SaveBoolFalseDep(RemoteWriteDataMapper target, int? param)
        {
            return SaveBoolFalseDep1Property(target, param);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveBoolFalseDep1(RemoteWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveBoolFalseDep1Delegate), [target, param]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveBoolFalseDep1(RemoteWriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDeleteBoolFalseDep1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertBoolFalseDep1(target, param);
            }
            else
            {
                return LocalUpdateBoolFalseDep1(target, param);
            }
        }

        public virtual Task<RemoteWriteDataMapper?> SaveTaskDep(RemoteWriteDataMapper target, int? param)
        {
            return SaveTaskDep1Property(target, param);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveTaskDep1(RemoteWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveTaskDep1Delegate), [target, param]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveTaskDep1(RemoteWriteDataMapper target, int? param)
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

        public virtual Task<RemoteWriteDataMapper?> SaveTaskBoolDep(RemoteWriteDataMapper target, int? param)
        {
            return SaveTaskBoolDep1Property(target, param);
        }

        public virtual async Task<RemoteWriteDataMapper?> RemoteSaveTaskBoolDep1(RemoteWriteDataMapper target, int? param)
        {
            return await DoRemoteRequest.ForDelegate<RemoteWriteDataMapper?>(typeof(SaveTaskBoolDep1Delegate), [target, param]);
        }

        public virtual Task<RemoteWriteDataMapper?> LocalSaveTaskBoolDep1(RemoteWriteDataMapper target, int? param)
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
            services.AddTransient<RemoteWriteDataMapper>();
            services.AddScoped<RemoteWriteDataMapperFactory>();
            services.AddScoped<IRemoteWriteDataMapperFactory, RemoteWriteDataMapperFactory>();
            services.AddScoped<SaveVoidDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target) => factory.LocalSaveVoid(target);
            });
            services.AddScoped<SaveBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target) => factory.LocalSaveBool(target);
            });
            services.AddScoped<SaveTaskDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target) => factory.LocalSaveTask(target);
            });
            services.AddScoped<SaveTaskBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target) => factory.LocalSaveTaskBool(target);
            });
            services.AddScoped<SaveVoidDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target) => factory.LocalSaveVoidDep(target);
            });
            services.AddScoped<SaveBoolTrueDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target) => factory.LocalSaveBoolTrueDep(target);
            });
            services.AddScoped<SaveBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target) => factory.LocalSaveBoolFalseDep(target);
            });
            services.AddScoped<SaveTaskDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target) => factory.LocalSaveTaskDep(target);
            });
            services.AddScoped<SaveTaskBoolDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target) => factory.LocalSaveTaskBoolDep(target);
            });
            services.AddScoped<SaveTaskBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target) => factory.LocalSaveTaskBoolFalseDep(target);
            });
            services.AddScoped<SaveVoid1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target, int? param) => factory.LocalSaveVoid1(target, param);
            });
            services.AddScoped<SaveBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target, int? param) => factory.LocalSaveBool1(target, param);
            });
            services.AddScoped<SaveTask1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target, int? param) => factory.LocalSaveTask1(target, param);
            });
            services.AddScoped<SaveTaskBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target, int? param) => factory.LocalSaveTaskBool1(target, param);
            });
            services.AddScoped<SaveTaskBoolFalseDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target, int? param) => factory.LocalSaveTaskBoolFalse(target, param);
            });
            services.AddScoped<SaveVoidDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target, int? param) => factory.LocalSaveVoidDep1(target, param);
            });
            services.AddScoped<SaveBoolTrueDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target, int? param) => factory.LocalSaveBoolTrueDep1(target, param);
            });
            services.AddScoped<SaveBoolFalseDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target, int? param) => factory.LocalSaveBoolFalseDep1(target, param);
            });
            services.AddScoped<SaveTaskDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target, int? param) => factory.LocalSaveTaskDep1(target, param);
            });
            services.AddScoped<SaveTaskBoolDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteDataMapperFactory>();
                return (RemoteWriteDataMapper target, int? param) => factory.LocalSaveTaskBoolDep1(target, param);
            });
            services.AddScoped<IFactoryEditBase<RemoteWriteDataMapper>, RemoteWriteDataMapperFactory>();
        }
    }
}