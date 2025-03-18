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
                    : INoBaseClassA
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute
                    */
namespace Neatoo.UnitTest.RemoteFactory
{
    public interface INoBaseClassAFactory
    {
        INoBaseClassA Create(string name);
        Task<INoBaseClassA> CreateRemote(string name);
    }

    internal class NoBaseClassAFactory : FactoryBase, INoBaseClassAFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IDoRemoteRequest DoRemoteRequest;
        // Delegates
        public delegate Task<INoBaseClassA> CreateRemoteDelegate(string name);
        // Delegate Properties to provide Local or Remote fork in execution
        public CreateRemoteDelegate CreateRemoteProperty { get; }

        public NoBaseClassAFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            CreateRemoteProperty = LocalCreateRemote;
        }

        public NoBaseClassAFactory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.DoRemoteRequest = remoteMethodDelegate;
            CreateRemoteProperty = RemoteCreateRemote;
        }

        public virtual INoBaseClassA Create(string name)
        {
            return LocalCreate(name);
        }

        public INoBaseClassA LocalCreate(string name)
        {
            var target = ServiceProvider.GetRequiredService<NoBaseClassA>();
            return DoMapperMethodCall<INoBaseClassA>(target, DataMapperMethod.Create, () => target.Create(name));
        }

        public virtual Task<INoBaseClassA> CreateRemote(string name)
        {
            return CreateRemoteProperty(name);
        }

        public virtual async Task<INoBaseClassA> RemoteCreateRemote(string name)
        {
            return await DoRemoteRequest.ForDelegate<INoBaseClassA>(typeof(CreateRemoteDelegate), [name]);
        }

        public Task<INoBaseClassA> LocalCreateRemote(string name)
        {
            var target = ServiceProvider.GetRequiredService<NoBaseClassA>();
            return Task.FromResult(DoMapperMethodCall<INoBaseClassA>(target, DataMapperMethod.Create, () => target.CreateRemote(name)));
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<NoBaseClassA>();
            services.AddScoped<NoBaseClassAFactory>();
            services.AddScoped<INoBaseClassAFactory, NoBaseClassAFactory>();
            services.AddTransient<INoBaseClassA, NoBaseClassA>();
            services.AddScoped<CreateRemoteDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<NoBaseClassAFactory>();
                return (string name) => factory.LocalCreateRemote(name);
            });
        }
    }
}