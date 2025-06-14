﻿#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Objects;
using Neatoo.UnitTest.PersonObjects;

/*
							READONLY - DO NOT EDIT!!!!
							Generated by Neatoo.RemoteFactory
*/
namespace Neatoo.UnitTest.ValidateBaseTests
{
    public interface IValidateDependencyRulesFactory
    {
    }

    internal class ValidateDependencyRulesFactory : FactoryBase<IValidateDependencyRules>, IValidateDependencyRulesFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public ValidateDependencyRulesFactory(IServiceProvider serviceProvider, IFactoryCore<IValidateDependencyRules> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public ValidateDependencyRulesFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IValidateDependencyRules> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
        {
            services.AddScoped<ValidateDependencyRulesFactory>();
            services.AddScoped<IValidateDependencyRulesFactory, ValidateDependencyRulesFactory>();
        }
    }
}