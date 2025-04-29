using Microsoft.Extensions.DependencyInjection;
using Neatoo.UnitTest.Objects;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.ValidateBaseTests;
using Neatoo.UnitTest.EditBaseTests;

namespace Neatoo.UnitTest;


public static class UnitTestServices
{
    private static IServiceProvider Container;
    private static IServiceProvider LocalPortalContainer;
    private static object lockContainer = new object();

    public static IServiceScope GetLifetimeScope(bool localPortal = false)
    {

        lock (lockContainer)
        {
            if (Container == null)
            {

                IServiceProvider CreateContainer(NeatooFactory? portal)
                {
                    var services = new ServiceCollection();

                    services.AddNeatooServices(NeatooFactory.Server, typeof(IEditPerson).Assembly);
                    services.RegisterMatchingName(typeof(IEditPerson).Assembly);

                    // Unit Test Library
                    //services.AddScoped<BaseTests.Authorization.IAuthorizationGrantedRule, BaseTests.Authorization.AuthorizationGrantedRule>();
                    //services.AddScoped<BaseTests.Authorization.IAuthorizationGrantedAsyncRule, BaseTests.Authorization.AuthorizationGrantedAsyncRule>();
                    //services.AddScoped<BaseTests.Authorization.IAuthorizationGrantedDependencyRule, BaseTests.Authorization.AuthorizationGrantedDependencyRule>();

                    services.AddTransient(typeof(ISharedShortNameRule<>), typeof(SharedShortNameRule<>));
                    services.AddTransient<Func<IDisposableDependency>>(cc => () => cc.GetRequiredService<IDisposableDependency>());

                    services.AddTransient<Objects.IDisposableDependency, Objects.DisposableDependency>();
                    services.AddScoped<Objects.DisposableDependencyList>();

                    services.AddSingleton<IReadOnlyList<PersonObjects.PersonDto>>(cc => PersonObjects.PersonDto.Data());

                    //services.AutoRegisterAssemblyTypes(typeof(IEditPerson).Assembly);

                    return services.BuildServiceProvider();
                }

                Container = CreateContainer(null);
                LocalPortalContainer = CreateContainer(NeatooFactory.Local);

            }

            if (!localPortal)
            {
                return Container.CreateScope();
            }
            else
            {
                return LocalPortalContainer.CreateScope();
            }
        }
    }
}

public static class  ServiceScopeProviderExtension
{
    public static T GetRequiredService<T>(this IServiceScope service)
    {
        return service.ServiceProvider.GetRequiredService<T>();
    }
}
