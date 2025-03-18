using Microsoft.Extensions.DependencyInjection;
using Neatoo.Portal.Internal;
using Neatoo;
using Neatoo.Portal;
using HorseBarn.lib.Horse;
using Neatoo.Rules.Rules;
using Neatoo.Rules;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.ComponentModel;
using HorseBarn.Dal.Ef;

/*
                    Debugging Messages:
                    : Cart<RacingChariot, ILightHorse>, IRacingChariot
: CustomEditBase<C>, ICart
: EditBase<T>
No MethodDeclarationSyntax for get_PropertyManager
No MethodDeclarationSyntax for .ctor
No MethodDeclarationSyntax for get_Factory
No MethodDeclarationSyntax for set_Factory
No MethodDeclarationSyntax for get_IsMarkedModified
No MethodDeclarationSyntax for set_IsMarkedModified
No MethodDeclarationSyntax for get_IsNew
No MethodDeclarationSyntax for set_IsNew
No MethodDeclarationSyntax for get_IsDeleted
No MethodDeclarationSyntax for set_IsDeleted
No MethodDeclarationSyntax for get_ModifiedProperties
No MethodDeclarationSyntax for get_IsChild
No MethodDeclarationSyntax for set_IsChild
No MethodDeclarationSyntax for get_EditMetaState
No MethodDeclarationSyntax for ChildNeatooPropertyChanged
No MethodDeclarationSyntax for Save
No MethodDeclarationSyntax for GetProperty
No MethodDeclarationSyntax for PauseAllActions
No MethodDeclarationSyntax for get_Item
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
No AuthorizeAttribute
                    */
namespace HorseBarn.lib.Cart
{
    public interface IRacingChariotFactory
    {
        Task<IRacingChariot> Create();
        IRacingChariot Fetch(Dal.Ef.Cart cart);
        IRacingChariot? Save(IRacingChariot target, Dal.Ef.HorseBarn horseBarn);
    }

    internal class RacingChariotFactory : FactoryEditBase<RacingChariot>, IFactoryEditBase<RacingChariot>, IRacingChariotFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public RacingChariotFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public RacingChariotFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
        }

        public virtual Task<IRacingChariot> Create()
        {
            return LocalCreate();
        }

        public Task<IRacingChariot> LocalCreate()
        {
            var target = ServiceProvider.GetRequiredService<RacingChariot>();
            var horsePortal = ServiceProvider.GetService<IHorseListFactory>();
            var allRequiredRulesExecutedFactory = ServiceProvider.GetService<IAllRequiredRulesExecuted.Factory>();
            return DoMapperMethodCallAsync<IRacingChariot>(target, DataMapperMethod.Create, () => target.Create(horsePortal, allRequiredRulesExecutedFactory));
        }

        public virtual IRacingChariot Fetch(Dal.Ef.Cart cart)
        {
            return LocalFetch(cart);
        }

        public IRacingChariot LocalFetch(Dal.Ef.Cart cart)
        {
            var target = ServiceProvider.GetRequiredService<RacingChariot>();
            var horsePortal = ServiceProvider.GetService<IHorseListFactory>();
            return DoMapperMethodCall<IRacingChariot>(target, DataMapperMethod.Fetch, () => target.Fetch(cart, horsePortal));
        }

        public IRacingChariot LocalInsert(IRacingChariot target, Dal.Ef.HorseBarn horseBarn)
        {
            var cTarget = (RacingChariot)target ?? throw new Exception("IRacingChariot must implement RacingChariot");
            var horsePortal = ServiceProvider.GetService<IHorseListFactory>();
            return DoMapperMethodCall<IRacingChariot>(cTarget, DataMapperMethod.Insert, () => cTarget.Insert(horseBarn, horsePortal));
        }

        public IRacingChariot LocalUpdate(IRacingChariot target, Dal.Ef.HorseBarn horseBarn)
        {
            var cTarget = (RacingChariot)target ?? throw new Exception("IRacingChariot must implement RacingChariot");
            var horsePortal = ServiceProvider.GetService<IHorseListFactory>();
            return DoMapperMethodCall<IRacingChariot>(cTarget, DataMapperMethod.Update, () => cTarget.Update(horseBarn, horsePortal));
        }

        public virtual IRacingChariot? Save(IRacingChariot target, Dal.Ef.HorseBarn horseBarn)
        {
            return LocalSave(target, horseBarn);
        }

        public virtual IRacingChariot? LocalSave(IRacingChariot target, Dal.Ef.HorseBarn horseBarn)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return null;
                }

                throw new NotImplementedException();
            }
            else if (target.IsNew)
            {
                return LocalInsert(target, horseBarn);
            }
            else
            {
                return LocalUpdate(target, horseBarn);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<RacingChariot>();
            services.AddScoped<RacingChariotFactory>();
            services.AddScoped<IRacingChariotFactory, RacingChariotFactory>();
            services.AddTransient<IRacingChariot, RacingChariot>();
            services.AddScoped<IFactoryEditBase<RacingChariot>, RacingChariotFactory>();
        }
    }
}