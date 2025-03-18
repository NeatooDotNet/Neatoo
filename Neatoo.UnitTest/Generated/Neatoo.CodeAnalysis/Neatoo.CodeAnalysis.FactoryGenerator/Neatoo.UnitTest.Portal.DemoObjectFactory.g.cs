using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using static Neatoo.UnitTest.RemoteFactory.AuthorizationClassTests;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neatoo.AuthorizationRules;
using Neatoo.Internal;
using Neatoo.RemoteFactory.Internal;
using Neatoo.UnitTest.RemoteFactory;
using Neatoo.UnitTest.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

/*
                    Debugging Messages:
                    Parent class: AuthorizationClassTests
: IDemoObject
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
                    */
namespace Neatoo.UnitTest.RemoteFactory
{
    public interface IDemoObjectFactory
    {
        IDemoObject Create();
        IDemoObject? CreateCanReturnNull();
        Task<IDemoObject> CreateAsync();
        Task<IDemoObject?> CreateRemote();
        Authorized CanCreate();
        Authorized CanCreateCanReturnNull();
        Authorized CanCreateAsync();
        Authorized CanCreateRemote();
        Authorized CanInsert();
        Authorized CanUpdate();
        Authorized CanDelete();
        Task<IDemoObject?> Save(IDemoObject target);
        Task<Authorized<IDemoObject>> TrySave(IDemoObject target);
    }

    internal class DemoObjectFactory : FactoryEditBase<DemoObject>, IFactoryEditBase<DemoObject>, IDemoObjectFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        public delegate Task<Authorized<IDemoObject>> CreateRemoteDelegate();
        public delegate Task<Authorized<IDemoObject>> SaveDelegate(IDemoObject target);
        // Delegate Properties to provide Local or Remote fork in execution
        public IAuthorization IAuthorization { get; }
        public CreateRemoteDelegate CreateRemoteProperty { get; }
        public SaveDelegate SaveProperty { get; }

        public DemoObjectFactory(IServiceProvider serviceProvider, IAuthorization iauthorization)
        {
            this.ServiceProvider = serviceProvider;
            this.IAuthorization = iauthorization;
            CreateRemoteProperty = LocalCreateRemote;
            SaveProperty = LocalSave;
        }

        public DemoObjectFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate, IAuthorization iauthorization)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
            this.IAuthorization = iauthorization;
            CreateRemoteProperty = RemoteCreateRemote;
            SaveProperty = RemoteSave;
        }

        public virtual IDemoObject Create()
        {
            return (LocalCreate()).Result;
        }

        public Authorized<IDemoObject> LocalCreate()
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IDemoObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<DemoObject>();
            return new Authorized<IDemoObject>(DoMapperMethodCall<IDemoObject>(target, DataMapperMethod.Create, () => target.Create()));
        }

        public virtual IDemoObject? CreateCanReturnNull()
        {
            return (LocalCreateCanReturnNull()).Result;
        }

        public Authorized<IDemoObject> LocalCreateCanReturnNull()
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IDemoObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<DemoObject>();
            return new Authorized<IDemoObject>(DoMapperMethodCallBool<IDemoObject>(target, DataMapperMethod.Create, () => target.CreateCanReturnNull()));
        }

        public virtual async Task<IDemoObject> CreateAsync()
        {
            return (await LocalCreateAsync()).Result;
        }

        public async Task<Authorized<IDemoObject>> LocalCreateAsync()
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IDemoObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<DemoObject>();
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<IDemoObject>(await DoMapperMethodCallAsync<IDemoObject>(target, DataMapperMethod.Create, () => target.CreateAsync(disposableDependency)));
        }

        public virtual async Task<IDemoObject?> CreateRemote()
        {
            return (await CreateRemoteProperty()).Result;
        }

        public virtual async Task<Authorized<IDemoObject>> RemoteCreateRemote()
        {
            return await DoRemoteRequest.ForDelegate<Authorized<IDemoObject>>(typeof(CreateRemoteDelegate), []);
        }

        public Task<Authorized<IDemoObject>> LocalCreateRemote()
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<IDemoObject>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<DemoObject>();
            return Task.FromResult(new Authorized<IDemoObject>(DoMapperMethodCallBool<IDemoObject>(target, DataMapperMethod.Create, () => target.CreateRemote())));
        }

        public Task<Authorized<IDemoObject>> LocalInsert(IDemoObject target)
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<IDemoObject>(authorized));
            }

            var cTarget = (DemoObject)target ?? throw new Exception("IDemoObject must implement DemoObject");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return Task.FromResult(new Authorized<IDemoObject>(DoMapperMethodCall<IDemoObject>(cTarget, DataMapperMethod.Insert, () => cTarget.Insert(disposableDependency))));
        }

        public Authorized<IDemoObject> LocalUpdate(IDemoObject target)
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IDemoObject>(authorized);
            }

            var cTarget = (DemoObject)target ?? throw new Exception("IDemoObject must implement DemoObject");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<IDemoObject>(DoMapperMethodCall<IDemoObject>(cTarget, DataMapperMethod.Update, () => cTarget.Update(disposableDependency)));
        }

        public Authorized<IDemoObject> LocalDelete(IDemoObject target)
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IDemoObject>(authorized);
            }

            var cTarget = (DemoObject)target ?? throw new Exception("IDemoObject must implement DemoObject");
            var disposableDependency = ServiceProvider.GetService<IDisposableDependency>();
            return new Authorized<IDemoObject>(DoMapperMethodCall<IDemoObject>(cTarget, DataMapperMethod.Delete, () => cTarget.Delete(disposableDependency)));
        }

        public virtual Authorized CanCreate()
        {
            return LocalCanCreate();
        }

        public Authorized LocalCanCreate()
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateCanReturnNull()
        {
            return LocalCanCreateCanReturnNull();
        }

        public Authorized LocalCanCreateCanReturnNull()
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateAsync()
        {
            return LocalCanCreateAsync();
        }

        public Authorized LocalCanCreateAsync()
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateRemote()
        {
            return LocalCanCreateRemote();
        }

        public Authorized LocalCanCreateRemote()
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanInsert()
        {
            return LocalCanInsert();
        }

        public Authorized LocalCanInsert()
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanUpdate()
        {
            return LocalCanUpdate();
        }

        public Authorized LocalCanUpdate()
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanDelete()
        {
            return LocalCanDelete();
        }

        public Authorized LocalCanDelete()
        {
            Authorized authorized;
            authorized = IAuthorization.AnyAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual async Task<IDemoObject?> Save(IDemoObject target)
        {
            var authorized = (await SaveProperty(target));
            if (!authorized.HasAccess)
            {
                throw new NotAuthorizedException(authorized.Message);
            }

            return authorized.Result;
        }

        public virtual async Task<Authorized<IDemoObject>> TrySave(IDemoObject target)
        {
            return await SaveProperty(target);
        }

        public virtual async Task<Authorized<IDemoObject>> RemoteSave(IDemoObject target)
        {
            return await DoRemoteRequest.ForDelegate<Authorized<IDemoObject>>(typeof(SaveDelegate), [target]);
        }

        async Task<IEditBase?> IFactoryEditBase<DemoObject>.Save(DemoObject target)
        {
            return (IEditBase? )await Save(target);
        }

        public virtual async Task<Authorized<IDemoObject>> LocalSave(IDemoObject target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                return LocalDelete(target);
            }
            else if (target.IsNew)
            {
                return await LocalInsert(target);
            }
            else
            {
                return LocalUpdate(target);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<DemoObject>();
            services.AddScoped<DemoObjectFactory>();
            services.AddScoped<IDemoObjectFactory, DemoObjectFactory>();
            services.AddTransient<IDemoObject, DemoObject>();
            services.AddScoped<CreateRemoteDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<DemoObjectFactory>();
                return () => factory.LocalCreateRemote();
            });
            services.AddScoped<SaveDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<DemoObjectFactory>();
                return (IDemoObject target) => factory.LocalSave(target);
            });
            services.AddScoped<IFactoryEditBase<DemoObject>, DemoObjectFactory>();
        }
    }
}