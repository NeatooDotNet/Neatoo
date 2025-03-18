using Microsoft.Extensions.DependencyInjection;
using Neatoo.Portal.Internal;
using Neatoo;
using Neatoo.Portal;
using Neatoo.AuthorizationRules;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

/*
                    Debugging Messages:
                    : ValidateBase<HorseCriteria>, IHorseCriteria
No MethodDeclarationSyntax for get_RuleManager
No MethodDeclarationSyntax for get_MetaState
No MethodDeclarationSyntax for ChildNeatooPropertyChanged
No MethodDeclarationSyntax for get_ObjectInvalid
No MethodDeclarationSyntax for set_ObjectInvalid
No MethodDeclarationSyntax for get_IsPaused
No MethodDeclarationSyntax for set_IsPaused
No MethodDeclarationSyntax for RunSelfRules
No MethodDeclarationSyntax for RunAllRules
No MethodDeclarationSyntax for get_AsyncTaskSequencer
No MethodDeclarationSyntax for get_PropertyManager
No MethodDeclarationSyntax for set_PropertyManager
No MethodDeclarationSyntax for get_Parent
No MethodDeclarationSyntax for set_Parent
No MethodDeclarationSyntax for SetParent
No MethodDeclarationSyntax for Neatoo.Core.ISetParent.SetParent
No MethodDeclarationSyntax for Getter
No MethodDeclarationSyntax for Setter
No MethodDeclarationSyntax for WaitForTasks
No MethodDeclarationSyntax for add_PropertyChanged
No MethodDeclarationSyntax for remove_PropertyChanged
No MethodDeclarationSyntax for add_NeatooPropertyChanged
No MethodDeclarationSyntax for remove_NeatooPropertyChanged
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
                    */
namespace HorseBarn.lib.Horse
{
    public interface IHorseCriteriaFactory
    {
        IHorseCriteria Fetch();
        IHorseCriteria Fetch(IEnumerable<string> horseNames);
        Authorized CanFetch();
        Authorized CanFetch(IEnumerable<string> horseNames);
    }

    internal class HorseCriteriaFactory : FactoryBase, IHorseCriteriaFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public IHorseCriteriaAuthorization IHorseCriteriaAuthorization { get; }

        public HorseCriteriaFactory(IServiceProvider serviceProvider, IHorseCriteriaAuthorization ihorsecriteriaauthorization)
        {
            this.ServiceProvider = serviceProvider;
            this.IHorseCriteriaAuthorization = ihorsecriteriaauthorization;
        }

        public HorseCriteriaFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate, IHorseCriteriaAuthorization ihorsecriteriaauthorization)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
            this.IHorseCriteriaAuthorization = ihorsecriteriaauthorization;
        }

        public virtual IHorseCriteria Fetch()
        {
            return (LocalFetch()).Result;
        }

        public Authorized<IHorseCriteria> LocalFetch()
        {
            Authorized authorized;
            authorized = IHorseCriteriaAuthorization.CanFetch();
            if (!authorized.HasAccess)
            {
                return new Authorized<IHorseCriteria>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<HorseCriteria>();
            return new Authorized<IHorseCriteria>(DoMapperMethodCall<IHorseCriteria>(target, DataMapperMethod.Fetch, () => target.Fetch()));
        }

        public virtual IHorseCriteria Fetch(IEnumerable<string> horseNames)
        {
            return (LocalFetch1(horseNames)).Result;
        }

        public Authorized<IHorseCriteria> LocalFetch1(IEnumerable<string> horseNames)
        {
            Authorized authorized;
            authorized = IHorseCriteriaAuthorization.CanFetch();
            if (!authorized.HasAccess)
            {
                return new Authorized<IHorseCriteria>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<HorseCriteria>();
            return new Authorized<IHorseCriteria>(DoMapperMethodCall<IHorseCriteria>(target, DataMapperMethod.Fetch, () => target.Fetch(horseNames)));
        }

        public virtual Authorized CanFetch()
        {
            return LocalCanFetch();
        }

        public Authorized LocalCanFetch()
        {
            Authorized authorized;
            authorized = IHorseCriteriaAuthorization.CanFetch();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetch(IEnumerable<string> horseNames)
        {
            return LocalCanFetch1(horseNames);
        }

        public Authorized LocalCanFetch1(IEnumerable<string> horseNames)
        {
            Authorized authorized;
            authorized = IHorseCriteriaAuthorization.CanFetch();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<HorseCriteria>();
            services.AddScoped<HorseCriteriaFactory>();
            services.AddScoped<IHorseCriteriaFactory, HorseCriteriaFactory>();
            services.AddTransient<IHorseCriteria, HorseCriteria>();
        }
    }
}