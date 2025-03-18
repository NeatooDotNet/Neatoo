using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using static Neatoo.UnitTest.RemoteFactory.PersonObjectTests;
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
                    Parent class: PersonObjectTests
: IPersonObject
No DataMapperMethod or Authorized attribute for get_IsDeleted
No DataMapperMethod or Authorized attribute for set_IsDeleted
No DataMapperMethod or Authorized attribute for get_IsNew
No DataMapperMethod or Authorized attribute for set_IsNew
No DataMapperMethod or Authorized attribute for get_FirstName
No DataMapperMethod or Authorized attribute for set_FirstName
No DataMapperMethod or Authorized attribute for get_LastName
No DataMapperMethod or Authorized attribute for set_LastName
No DataMapperMethod or Authorized attribute for .ctor
No DataMapperMethod or Authorized attribute for .ctor
No DataMapperMethod or Authorized attribute for Equals
No DataMapperMethod or Authorized attribute for Equals
No DataMapperMethod or Authorized attribute for Finalize
No DataMapperMethod or Authorized attribute for GetHashCode
No DataMapperMethod or Authorized attribute for GetType
No DataMapperMethod or Authorized attribute for MemberwiseClone
No DataMapperMethod or Authorized attribute for ReferenceEquals
No DataMapperMethod or Authorized attribute for ToString
Factory CanInsert not created because it matches to an auth method with a IPersonObject parameter
Factory CanUpdate not created because it matches to an auth method with a IPersonObject parameter
Factory CanDelete not created because it matches to an auth method with a IPersonObject parameter
                    */
namespace Neatoo.UnitTest.RemoteFactory
{
    public interface IPersonObjectFactory
    {
        IPersonObject Create(string firstName, string lastName);
        Authorized CanCreate(string firstName, string lastName);
        IPersonObject? Save(IPersonObject target);
        Authorized<IPersonObject> TrySave(IPersonObject target);
    }

    internal class PersonObjectFactory : FactoryBase, IPersonObjectFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public IAuthorizePersonObject IAuthorizePersonObject { get; }

        public PersonObjectFactory(IServiceProvider serviceProvider, IAuthorizePersonObject iauthorizepersonobject)
        {
            this.ServiceProvider = serviceProvider;
            this.IAuthorizePersonObject = iauthorizepersonobject;
        }

        public PersonObjectFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate, IAuthorizePersonObject iauthorizepersonobject)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
            this.IAuthorizePersonObject = iauthorizepersonobject;
        }

        public virtual IPersonObject Create(string firstName, string lastName)
        {
            return (LocalCreate(firstName, lastName)).Result;
        }

        public Authorized<IPersonObject> LocalCreate(string firstName, string lastName)
        {
            Authorized authorized;
            authorized = IAuthorizePersonObject.CanRead();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonObject>(authorized);
            }

            authorized = IAuthorizePersonObject.CanCreate();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<PersonObject>();
            return new Authorized<IPersonObject>(DoMapperMethodCall<IPersonObject>(target, DataMapperMethod.Create, () => target.Create(firstName, lastName)));
        }

        public Authorized<IPersonObject> LocalInsert(IPersonObject target)
        {
            Authorized authorized;
            authorized = IAuthorizePersonObject.CanWrite();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonObject>(authorized);
            }

            authorized = IAuthorizePersonObject.CanInsert(target);
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonObject>(authorized);
            }

            var cTarget = (PersonObject)target ?? throw new Exception("IPersonObject must implement PersonObject");
            return new Authorized<IPersonObject>(DoMapperMethodCall<IPersonObject>(cTarget, DataMapperMethod.Insert, () => cTarget.Insert()));
        }

        public Authorized<IPersonObject> LocalUpdate(IPersonObject target)
        {
            Authorized authorized;
            authorized = IAuthorizePersonObject.CanWrite();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonObject>(authorized);
            }

            authorized = IAuthorizePersonObject.CanUpdate(target);
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonObject>(authorized);
            }

            var cTarget = (PersonObject)target ?? throw new Exception("IPersonObject must implement PersonObject");
            return new Authorized<IPersonObject>(DoMapperMethodCall<IPersonObject>(cTarget, DataMapperMethod.Update, () => cTarget.Update()));
        }

        public Authorized<IPersonObject> LocalDelete(IPersonObject target)
        {
            Authorized authorized;
            authorized = IAuthorizePersonObject.CanWrite();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonObject>(authorized);
            }

            authorized = IAuthorizePersonObject.CanDelete(target);
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonObject>(authorized);
            }

            var cTarget = (PersonObject)target ?? throw new Exception("IPersonObject must implement PersonObject");
            return new Authorized<IPersonObject>(DoMapperMethodCall<IPersonObject>(cTarget, DataMapperMethod.Delete, () => cTarget.Delete()));
        }

        public virtual Authorized CanCreate(string firstName, string lastName)
        {
            return LocalCanCreate(firstName, lastName);
        }

        public Authorized LocalCanCreate(string firstName, string lastName)
        {
            Authorized authorized;
            authorized = IAuthorizePersonObject.CanRead();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = IAuthorizePersonObject.CanCreate();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual IPersonObject? Save(IPersonObject target)
        {
            var authorized = (LocalSave(target));
            if (!authorized.HasAccess)
            {
                throw new NotAuthorizedException(authorized.Message);
            }

            return authorized.Result;
        }

        public virtual Authorized<IPersonObject> TrySave(IPersonObject target)
        {
            return LocalSave(target);
        }

        public virtual Authorized<IPersonObject> LocalSave(IPersonObject target)
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
                return LocalInsert(target);
            }
            else
            {
                return LocalUpdate(target);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<PersonObject>();
            services.AddScoped<PersonObjectFactory>();
            services.AddScoped<IPersonObjectFactory, PersonObjectFactory>();
            services.AddTransient<IPersonObject, PersonObject>();
        }
    }
}