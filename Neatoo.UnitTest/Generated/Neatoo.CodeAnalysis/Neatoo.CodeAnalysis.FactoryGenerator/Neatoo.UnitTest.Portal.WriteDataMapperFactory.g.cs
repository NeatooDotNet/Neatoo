using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using static Neatoo.UnitTest.RemoteFactory.WriteTests;
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
                    Parent class: WriteTests
: IEditMetaSaveProperties
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute
                    */
namespace Neatoo.UnitTest.RemoteFactory
{
    public interface IWriteDataMapperFactory
    {
        WriteDataMapper? SaveVoid(WriteDataMapper target);
        WriteDataMapper? SaveBool(WriteDataMapper target);
        Task<WriteDataMapper?> SaveTask(WriteDataMapper target);
        Task<WriteDataMapper?> SaveTaskBool(WriteDataMapper target);
        WriteDataMapper? SaveVoidDep(WriteDataMapper target);
        WriteDataMapper? SaveBoolTrueDep(WriteDataMapper target);
        WriteDataMapper? SaveBoolFalseDep(WriteDataMapper target);
        Task<WriteDataMapper?> SaveTaskDep(WriteDataMapper target);
        Task<WriteDataMapper?> SaveTaskBoolDep(WriteDataMapper target);
        Task<WriteDataMapper?> SaveTaskBoolFalseDep(WriteDataMapper target);
        WriteDataMapper? SaveVoid(WriteDataMapper target, int? param);
        WriteDataMapper? SaveBool(WriteDataMapper target, int? param);
        Task<WriteDataMapper?> SaveTask(WriteDataMapper target, int? param);
        Task<WriteDataMapper?> SaveTaskBool(WriteDataMapper target, int? param);
        Task<WriteDataMapper?> SaveTaskBoolFalse(WriteDataMapper target, int? param);
        WriteDataMapper? SaveVoidDep(WriteDataMapper target, int? param);
        WriteDataMapper? SaveBoolTrueDep(WriteDataMapper target, int? param);
        WriteDataMapper? SaveBoolFalseDep(WriteDataMapper target, int? param);
        Task<WriteDataMapper?> SaveTaskDep(WriteDataMapper target, int? param);
        Task<WriteDataMapper?> SaveTaskBoolDep(WriteDataMapper target, int? param);
    }

    internal class WriteDataMapperFactory : FactoryEditBase<WriteDataMapper>, IFactoryEditBase<WriteDataMapper>, IWriteDataMapperFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public WriteDataMapperFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public WriteDataMapperFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
        }

        public WriteDataMapper LocalInsertVoid(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCall<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertVoid());
        }

        public WriteDataMapper? LocalInsertBool(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBool());
        }

        public Task<WriteDataMapper> LocalInsertTask(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallAsync<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTask());
        }

        public Task<WriteDataMapper?> LocalInsertTaskBool(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBool());
        }

        public WriteDataMapper LocalInsertVoid1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCall<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertVoid(param));
        }

        public WriteDataMapper? LocalInsertBool1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBool(param));
        }

        public Task<WriteDataMapper> LocalInsertTask1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallAsync<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTask(param));
        }

        public Task<WriteDataMapper?> LocalInsertTaskBool1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBool(param));
        }

        public Task<WriteDataMapper?> LocalInsertTaskBoolFalse(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBoolFalse(param));
        }

        public WriteDataMapper LocalInsertVoidDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertVoidDep(disposableDependency));
        }

        public WriteDataMapper? LocalInsertBoolTrueDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBoolTrueDep(disposableDependency));
        }

        public WriteDataMapper? LocalInsertBoolFalseDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBoolFalseDep(disposableDependency));
        }

        public Task<WriteDataMapper> LocalInsertTaskDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskDep(disposableDependency));
        }

        public Task<WriteDataMapper?> LocalInsertTaskBoolDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBoolDep(disposableDependency));
        }

        public Task<WriteDataMapper?> LocalInsertTaskBoolFalseDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBoolFalseDep(disposableDependency));
        }

        public WriteDataMapper LocalInsertVoidDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertVoidDep(param, disposableDependency));
        }

        public WriteDataMapper? LocalInsertBoolTrueDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBoolTrueDep(param, disposableDependency));
        }

        public WriteDataMapper? LocalInsertBoolFalseDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertBoolFalseDep(param, disposableDependency));
        }

        public Task<WriteDataMapper> LocalInsertTaskDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskDep(param, disposableDependency));
        }

        public Task<WriteDataMapper?> LocalInsertTaskBoolDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Insert, () => cTarget.InsertTaskBoolDep(param, disposableDependency));
        }

        public WriteDataMapper LocalUpdateVoid(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCall<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateVoid());
        }

        public WriteDataMapper? LocalUpdateBool(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBool());
        }

        public Task<WriteDataMapper> LocalUpdateTask(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallAsync<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTask());
        }

        public Task<WriteDataMapper?> LocalUpdateTaskBool(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBool());
        }

        public WriteDataMapper LocalUpdateVoid1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCall<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateVoid(param));
        }

        public WriteDataMapper? LocalUpdateBool1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBool(param));
        }

        public Task<WriteDataMapper> LocalUpdateTask1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallAsync<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTask(param));
        }

        public Task<WriteDataMapper?> LocalUpdateTaskBool1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBool(param));
        }

        public Task<WriteDataMapper?> LocalUpdateTaskBoolFalse(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBoolFalse(param));
        }

        public WriteDataMapper LocalUpdateVoidDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateVoidDep(disposableDependency));
        }

        public WriteDataMapper? LocalUpdateBoolTrueDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBoolTrueDep(disposableDependency));
        }

        public WriteDataMapper? LocalUpdateBoolFalseDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBoolFalseDep(disposableDependency));
        }

        public Task<WriteDataMapper> LocalUpdateTaskDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskDep(disposableDependency));
        }

        public Task<WriteDataMapper?> LocalUpdateTaskBoolDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBoolDep(disposableDependency));
        }

        public Task<WriteDataMapper?> LocalUpdateTaskBoolFalseDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBoolFalseDep(disposableDependency));
        }

        public WriteDataMapper LocalUpdateVoidDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateVoidDep(param, disposableDependency));
        }

        public WriteDataMapper? LocalUpdateBoolTrueDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBoolTrueDep(param, disposableDependency));
        }

        public WriteDataMapper? LocalUpdateBoolFalseDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateBoolFalseDep(param, disposableDependency));
        }

        public Task<WriteDataMapper> LocalUpdateTaskDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskDep(param, disposableDependency));
        }

        public Task<WriteDataMapper?> LocalUpdateTaskBoolDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Update, () => cTarget.UpdateTaskBoolDep(param, disposableDependency));
        }

        public WriteDataMapper LocalDeleteVoid(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCall<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteVoid());
        }

        public WriteDataMapper? LocalDeleteBool(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBool());
        }

        public Task<WriteDataMapper> LocalDeleteTask(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallAsync<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTask());
        }

        public Task<WriteDataMapper?> LocalDeleteTaskBool(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBool());
        }

        public WriteDataMapper LocalDeleteVoid1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCall<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteVoid(param));
        }

        public WriteDataMapper? LocalDeleteBool1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBool(param));
        }

        public Task<WriteDataMapper> LocalDeleteTask1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallAsync<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTask(param));
        }

        public Task<WriteDataMapper?> LocalDeleteTaskBool1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBool(param));
        }

        public Task<WriteDataMapper?> LocalDeleteTaskBoolFalse(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBoolFalse(param));
        }

        public WriteDataMapper LocalDeleteVoidDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteVoidDep(disposableDependency));
        }

        public WriteDataMapper? LocalDeleteBoolTrueDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBoolTrueDep(disposableDependency));
        }

        public WriteDataMapper? LocalDeleteBoolFalseDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBoolFalseDep(disposableDependency));
        }

        public Task<WriteDataMapper> LocalDeleteTaskDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskDep(disposableDependency));
        }

        public Task<WriteDataMapper?> LocalDeleteTaskBoolDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBoolDep(disposableDependency));
        }

        public Task<WriteDataMapper?> LocalDeleteTaskBoolFalseDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBoolFalseDep(disposableDependency));
        }

        public WriteDataMapper LocalDeleteVoidDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCall<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteVoidDep(param, disposableDependency));
        }

        public WriteDataMapper? LocalDeleteBoolTrueDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBoolTrueDep(param, disposableDependency));
        }

        public WriteDataMapper? LocalDeleteBoolFalseDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBool<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteBoolFalseDep(param, disposableDependency));
        }

        public Task<WriteDataMapper> LocalDeleteTaskDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallAsync<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskDep(param, disposableDependency));
        }

        public Task<WriteDataMapper?> LocalDeleteTaskBoolDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return DoMapperMethodCallBoolAsync<WriteDataMapper>(cTarget, DataMapperMethod.Delete, () => cTarget.DeleteTaskBoolDep(param, disposableDependency));
        }

        public virtual WriteDataMapper? SaveVoid(WriteDataMapper target)
        {
            return LocalSaveVoid(target);
        }

        async Task<IEditBase?> IFactoryEditBase<WriteDataMapper>.Save(WriteDataMapper target)
        {
            return await Task.FromResult((IEditBase? )SaveVoid(target));
        }

        public virtual WriteDataMapper? LocalSaveVoid(WriteDataMapper target)
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

        public virtual WriteDataMapper? SaveBool(WriteDataMapper target)
        {
            return LocalSaveBool(target);
        }

        public virtual WriteDataMapper? LocalSaveBool(WriteDataMapper target)
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

        public virtual Task<WriteDataMapper?> SaveTask(WriteDataMapper target)
        {
            return LocalSaveTask(target);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTask(WriteDataMapper target)
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

        public virtual Task<WriteDataMapper?> SaveTaskBool(WriteDataMapper target)
        {
            return LocalSaveTaskBool(target);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskBool(WriteDataMapper target)
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

        public virtual WriteDataMapper? SaveVoidDep(WriteDataMapper target)
        {
            return LocalSaveVoidDep(target);
        }

        public virtual WriteDataMapper? LocalSaveVoidDep(WriteDataMapper target)
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

        public virtual WriteDataMapper? SaveBoolTrueDep(WriteDataMapper target)
        {
            return LocalSaveBoolTrueDep(target);
        }

        public virtual WriteDataMapper? LocalSaveBoolTrueDep(WriteDataMapper target)
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

        public virtual WriteDataMapper? SaveBoolFalseDep(WriteDataMapper target)
        {
            return LocalSaveBoolFalseDep(target);
        }

        public virtual WriteDataMapper? LocalSaveBoolFalseDep(WriteDataMapper target)
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

        public virtual Task<WriteDataMapper?> SaveTaskDep(WriteDataMapper target)
        {
            return LocalSaveTaskDep(target);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskDep(WriteDataMapper target)
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

        public virtual Task<WriteDataMapper?> SaveTaskBoolDep(WriteDataMapper target)
        {
            return LocalSaveTaskBoolDep(target);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskBoolDep(WriteDataMapper target)
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

        public virtual Task<WriteDataMapper?> SaveTaskBoolFalseDep(WriteDataMapper target)
        {
            return LocalSaveTaskBoolFalseDep(target);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskBoolFalseDep(WriteDataMapper target)
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

        public virtual WriteDataMapper? SaveVoid(WriteDataMapper target, int? param)
        {
            return LocalSaveVoid1(target, param);
        }

        public virtual WriteDataMapper? LocalSaveVoid1(WriteDataMapper target, int? param)
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

        public virtual WriteDataMapper? SaveBool(WriteDataMapper target, int? param)
        {
            return LocalSaveBool1(target, param);
        }

        public virtual WriteDataMapper? LocalSaveBool1(WriteDataMapper target, int? param)
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

        public virtual Task<WriteDataMapper?> SaveTask(WriteDataMapper target, int? param)
        {
            return LocalSaveTask1(target, param);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTask1(WriteDataMapper target, int? param)
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

        public virtual Task<WriteDataMapper?> SaveTaskBool(WriteDataMapper target, int? param)
        {
            return LocalSaveTaskBool1(target, param);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskBool1(WriteDataMapper target, int? param)
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

        public virtual Task<WriteDataMapper?> SaveTaskBoolFalse(WriteDataMapper target, int? param)
        {
            return LocalSaveTaskBoolFalse(target, param);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskBoolFalse(WriteDataMapper target, int? param)
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

        public virtual WriteDataMapper? SaveVoidDep(WriteDataMapper target, int? param)
        {
            return LocalSaveVoidDep1(target, param);
        }

        public virtual WriteDataMapper? LocalSaveVoidDep1(WriteDataMapper target, int? param)
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

        public virtual WriteDataMapper? SaveBoolTrueDep(WriteDataMapper target, int? param)
        {
            return LocalSaveBoolTrueDep1(target, param);
        }

        public virtual WriteDataMapper? LocalSaveBoolTrueDep1(WriteDataMapper target, int? param)
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

        public virtual WriteDataMapper? SaveBoolFalseDep(WriteDataMapper target, int? param)
        {
            return LocalSaveBoolFalseDep1(target, param);
        }

        public virtual WriteDataMapper? LocalSaveBoolFalseDep1(WriteDataMapper target, int? param)
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

        public virtual Task<WriteDataMapper?> SaveTaskDep(WriteDataMapper target, int? param)
        {
            return LocalSaveTaskDep1(target, param);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskDep1(WriteDataMapper target, int? param)
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

        public virtual Task<WriteDataMapper?> SaveTaskBoolDep(WriteDataMapper target, int? param)
        {
            return LocalSaveTaskBoolDep1(target, param);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskBoolDep1(WriteDataMapper target, int? param)
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
            services.AddTransient<WriteDataMapper>();
            services.AddScoped<WriteDataMapperFactory>();
            services.AddScoped<IWriteDataMapperFactory, WriteDataMapperFactory>();
            services.AddScoped<IFactoryEditBase<WriteDataMapper>, WriteDataMapperFactory>();
        }
    }
}