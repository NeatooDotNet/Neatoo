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
                    : INoBaseClassB
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute
                    */
namespace Neatoo.UnitTest.RemoteFactory
{
    public interface INoBaseClassBFactory
    {
        INoBaseClassB Create(string name);
        Task<INoBaseClassB> CreateRemote(string name);
    }

    internal class NoBaseClassBFactory : FactoryBase, INoBaseClassBFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        public delegate Task<INoBaseClassB> CreateRemoteDelegate(string name);
        // Delegate Properties to provide Local or Remote fork in execution
        public CreateRemoteDelegate CreateRemoteProperty { get; }

        public NoBaseClassBFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            CreateRemoteProperty = LocalCreateRemote;
        }

        public NoBaseClassBFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
            CreateRemoteProperty = RemoteCreateRemote;
        }

        public virtual INoBaseClassB Create(string name)
        {
            return LocalCreate(name);
        }

        public INoBaseClassB LocalCreate(string name)
        {
            var target = ServiceProvider.GetRequiredService<NoBaseClassB>();
            return DoMapperMethodCall<INoBaseClassB>(target, DataMapperMethod.Create, () => target.Create(name));
        }

        public virtual Task<INoBaseClassB> CreateRemote(string name)
        {
            return CreateRemoteProperty(name);
        }

        public virtual async Task<INoBaseClassB> RemoteCreateRemote(string name)
        {
            return await DoRemoteRequest.ForDelegate<INoBaseClassB>(typeof(CreateRemoteDelegate), [name]);
        }

        public Task<INoBaseClassB> LocalCreateRemote(string name)
        {
            var target = ServiceProvider.GetRequiredService<NoBaseClassB>();
            return Task.FromResult(DoMapperMethodCall<INoBaseClassB>(target, DataMapperMethod.Create, () => target.CreateRemote(name)));
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<NoBaseClassB>();
            services.AddScoped<NoBaseClassBFactory>();
            services.AddScoped<INoBaseClassBFactory, NoBaseClassBFactory>();
            services.AddTransient<INoBaseClassB, NoBaseClassB>();
            services.AddScoped<CreateRemoteDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<NoBaseClassBFactory>();
                return (string name) => factory.LocalCreateRemote(name);
            });
        }
    }
}