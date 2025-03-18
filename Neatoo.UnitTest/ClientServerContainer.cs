using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Neatoo.UnitTest;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Neatoo.UnitTest
{
    public class ServerServiceProvider
    {
        public IServiceProvider serverProvider { get; set; } = null!;
    }

    internal sealed class MakeRemoteDelegateRequest : IMakeRemoteDelegateRequest
    {
        private readonly INeatooJsonSerializer NeatooJsonSerializer;
        private readonly IServiceProvider serviceProvider;

        public MakeRemoteDelegateRequest(INeatooJsonSerializer neatooJsonSerializer, IServiceProvider serviceProvider)
        {
            NeatooJsonSerializer = neatooJsonSerializer;
            this.serviceProvider = serviceProvider;
        }

        public async Task<T?> ForDelegate<T>(Type delegateType, object?[]? parameters)
        {
            // Mimic all the steps of a Remote call except the actual http call

            var remoteRequest = NeatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);

            // Mimic real life - use standard ASP.NET Core JSON serialization
            var json = JsonSerializer.Serialize(remoteRequest); //NeatooJsonSerializer.Serialize(remoteRequest);
            var remoteRequestOnServer = JsonSerializer.Deserialize<RemoteRequestDto>(json)!; // this.NeatooJsonSerializer.Deserialize<RemoteRequestDto>(json);

            // Use the Server's container
            var remoteResponseOnServer = await serviceProvider.GetRequiredService<ServerServiceProvider>()
                                                                                 .serverProvider
                                                                                 .GetRequiredService<HandleRemoteDelegateRequest>()(remoteRequestOnServer);

            json = JsonSerializer.Serialize(remoteResponseOnServer); // NeatooJsonSerializer.Serialize(remoteResponseOnServer);
            var result = JsonSerializer.Deserialize<RemoteResponseDto>(json); // NeatooJsonSerializer.Deserialize<RemoteResponseDto>(json);

            return NeatooJsonSerializer.DeserializeRemoteResponse<T>(result!);
        }
    }

    internal static class ClientServerContainers
    {
        private static object lockContainer = new object();
        static IServiceProvider serverContainer = null!;
        static IServiceProvider clientContainer = null!;

        public static (IServiceScope server, IServiceScope client) Scopes()
        {
            lock (lockContainer)
            {
                if (serverContainer == null)
                {
                    var serverCollection = new ServiceCollection();
                    var clientCollection = new ServiceCollection();

                    serverCollection.AddNeatooServices(NeatooFactory.Local, Assembly.GetExecutingAssembly());
                    clientCollection.AddNeatooServices(NeatooFactory.Remote, Assembly.GetExecutingAssembly());

                    serverCollection.AutoRegisterAssemblyTypes(Assembly.GetExecutingAssembly());
                    clientCollection.AutoRegisterAssemblyTypes(Assembly.GetExecutingAssembly());

                    serverCollection.AddTransient<Objects.IDisposableDependency, Objects.DisposableDependency>();
                    serverCollection.AddScoped<Objects.DisposableDependencyList>();

                    clientCollection.AddTransient<Objects.IDisposableDependency, Objects.DisposableDependency>();
                    clientCollection.AddScoped<Objects.DisposableDependencyList>();

                    clientCollection.AddScoped<ServerServiceProvider>();

                    clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeRemoteDelegateRequest>();
                    serverContainer = serverCollection.BuildServiceProvider();
                    clientContainer = clientCollection.BuildServiceProvider();
                }
            }

            var serverScope = serverContainer.CreateScope();
            var clientScope = clientContainer.CreateScope();

            clientScope.GetRequiredService<ServerServiceProvider>().serverProvider = serverScope.ServiceProvider;

            return (serverScope, clientScope);
        }


        public static void AutoRegisterAssemblyTypes(this IServiceCollection services, Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));
            ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));

            var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract).ToList();
            var interfaces = assembly.GetTypes().Where(t => t.IsInterface && t.Name.StartsWith("I")).ToDictionary(x => Regex.Replace(x.FullName, @"(.*)([\.|\+])\w+$", $"$1$2{x.Name.Substring(1)}"));

            foreach (var t in types)
            {
                if (interfaces.TryGetValue(t.FullName, out var i))
                {
                    //var singleConstructor = t.GetConstructors().SingleOrDefault();
                    //var zeroConstructorParams = singleConstructor != null && !singleConstructor.GetParameters().Any();

                    // AsSelf required for Deserialization
                    services.AddTransient(i, t);
                    services.AddTransient(t);
                }
            }


        }
    }
}