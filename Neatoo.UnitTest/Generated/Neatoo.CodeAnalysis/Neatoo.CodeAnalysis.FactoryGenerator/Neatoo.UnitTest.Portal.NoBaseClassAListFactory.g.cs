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
                    : List<INoBaseClassA>, INoBaseClassAList
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute
                    */
namespace Neatoo.UnitTest.RemoteFactory
{
    public interface INoBaseClassAListFactory
    {
        INoBaseClassAList Create();
        Task<INoBaseClassAList> CreateRemote();
    }

    internal class NoBaseClassAListFactory : FactoryBase, INoBaseClassAListFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        public delegate Task<INoBaseClassAList> CreateRemoteDelegate();
        // Delegate Properties to provide Local or Remote fork in execution
        public CreateRemoteDelegate CreateRemoteProperty { get; }

        public NoBaseClassAListFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            CreateRemoteProperty = LocalCreateRemote;
        }

        public NoBaseClassAListFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
            CreateRemoteProperty = RemoteCreateRemote;
        }

        public virtual INoBaseClassAList Create()
        {
            return LocalCreate();
        }

        public INoBaseClassAList LocalCreate()
        {
            var target = ServiceProvider.GetRequiredService<NoBaseClassAList>();
            var factoryA = ServiceProvider.GetService<INoBaseClassAFactory>();
            return DoMapperMethodCall<INoBaseClassAList>(target, DataMapperMethod.Create, () => target.Create(factoryA));
        }

        public virtual Task<INoBaseClassAList> CreateRemote()
        {
            return CreateRemoteProperty();
        }

        public virtual async Task<INoBaseClassAList> RemoteCreateRemote()
        {
            return await DoRemoteRequest.ForDelegate<INoBaseClassAList>(typeof(CreateRemoteDelegate), []);
        }

        public Task<INoBaseClassAList> LocalCreateRemote()
        {
            var target = ServiceProvider.GetRequiredService<NoBaseClassAList>();
            var factoryA = ServiceProvider.GetService<INoBaseClassAFactory>();
            return Task.FromResult(DoMapperMethodCall<INoBaseClassAList>(target, DataMapperMethod.Create, () => target.CreateRemote(factoryA)));
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<NoBaseClassAList>();
            services.AddScoped<NoBaseClassAListFactory>();
            services.AddScoped<INoBaseClassAListFactory, NoBaseClassAListFactory>();
            services.AddTransient<INoBaseClassAList, NoBaseClassAList>();
            services.AddScoped<CreateRemoteDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<NoBaseClassAListFactory>();
                return () => factory.LocalCreateRemote();
            });
        }
    }
}