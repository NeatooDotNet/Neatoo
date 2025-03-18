using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.AuthorizationRules;
using Neatoo.RemoteFactory.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

/*
                    Debugging Messages:
                    : List<INoBaseClass>, INoBaseClassList
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute
                    */
namespace Neatoo.UnitTest.RemoteFactory
{
    public interface INoBaseClassListFactory
    {
        INoBaseClassList Create();
        Task<INoBaseClassList> CreateRemote();
    }

    internal class NoBaseClassListFactory : FactoryBase, INoBaseClassListFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        public delegate Task<INoBaseClassList> CreateRemoteDelegate();
        // Delegate Properties to provide Local or Remote fork in execution
        public CreateRemoteDelegate CreateRemoteProperty { get; }

        public NoBaseClassListFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            CreateRemoteProperty = LocalCreateRemote;
        }

        public NoBaseClassListFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
            CreateRemoteProperty = RemoteCreateRemote;
        }

        public virtual INoBaseClassList Create()
        {
            return LocalCreate();
        }

        public INoBaseClassList LocalCreate()
        {
            var target = ServiceProvider.GetRequiredService<NoBaseClassList>();
            var factoryA = ServiceProvider.GetService<INoBaseClassAFactory>();
            var factoryB = ServiceProvider.GetService<INoBaseClassBFactory>();
            return DoMapperMethodCall<INoBaseClassList>(target, DataMapperMethod.Create, () => target.Create(factoryA, factoryB));
        }

        public virtual Task<INoBaseClassList> CreateRemote()
        {
            return CreateRemoteProperty();
        }

        public virtual async Task<INoBaseClassList> RemoteCreateRemote()
        {
            return await DoRemoteRequest.ForDelegate<INoBaseClassList>(typeof(CreateRemoteDelegate), []);
        }

        public Task<INoBaseClassList> LocalCreateRemote()
        {
            var target = ServiceProvider.GetRequiredService<NoBaseClassList>();
            var factoryA = ServiceProvider.GetService<INoBaseClassAFactory>();
            var factoryB = ServiceProvider.GetService<INoBaseClassBFactory>();
            return Task.FromResult(DoMapperMethodCall<INoBaseClassList>(target, DataMapperMethod.Create, () => target.CreateRemote(factoryA, factoryB)));
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<NoBaseClassList>();
            services.AddScoped<NoBaseClassListFactory>();
            services.AddScoped<INoBaseClassListFactory, NoBaseClassListFactory>();
            services.AddTransient<INoBaseClassList, NoBaseClassList>();
            services.AddScoped<CreateRemoteDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<NoBaseClassListFactory>();
                return () => factory.LocalCreateRemote();
            });
        }
    }
}