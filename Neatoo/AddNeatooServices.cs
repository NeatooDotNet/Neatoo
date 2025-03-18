using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Neatoo.Core;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.Reflection;

namespace Neatoo;

public delegate Type GetServiceImplementationType(Type type);
public delegate IEnumerable<Type> GetServiceImplementationTypes(Type type);

public static class AddNeatooServicesExtension
{

    public static void AddNeatooServices(this IServiceCollection services, NeatooFactory portalServer, params Assembly[] assemblies)
    {

        services.AddSingleton<GetServiceImplementationTypes>(s =>
        {
            return (Type type) =>
            {
                // This is why these are delegates
                // Need access to the DI container
                return services
                        .Where(d => d.ServiceType == type && d.ImplementationType != null)
                        .Select(d => d.ImplementationType);
            };
        });

        services.AddSingleton<GetServiceImplementationType>(s =>
        {
            var getServiceImplementationTypes = s.GetRequiredService<GetServiceImplementationTypes>();
            return (Type type) =>
            {
                if (type.IsInterface)
                {
                    // This is why these are delegates
                    // Need access to the DI container
                    var implementationType = getServiceImplementationTypes(type).SingleOrDefault();

                    if (implementationType == null)
                    {
                        throw new Exception($"Type {type.FullName} not registered");
                    };

                    type = implementationType;
                }

                return type;
            };
        });


        // SingleInstance as long it is isn't modified to accept dependencies
        services.AddSingleton<IFactory, DefaultFactory>();

        // Meta Data about the properties and methods of Classes
        // This will not change during runtime
        // So SingleInstance
        services.AddSingleton(typeof(IPropertyInfoList<>), typeof(PropertyInfoList<>));

        services.AddSingleton<IAttributeToRule, AttributeToRule>();

        services.AddTransient(typeof(IRuleManager<>), typeof(RuleManager<>));

        services.AddTransient(typeof(RuleManagerFactory<>));

        services.AddTransient<CreatePropertyInfoWrapper>(factory =>
        {
            return (propertyInfo) =>
            {
                return new PropertyInfoWrapper(propertyInfo);
            };
        });

        // Stored values for each Domain Object instance
        // MUST BE per instance

        // Use delegates to construct - including this confuses ASP.NET Core DI
        //services.AddTransient<IPropertyManager<IProperty>, PropertyManager<IProperty>>();
        //services.AddTransient<IValidatePropertyManager<IValidateProperty>, ValidatePropertyManager<IValidateProperty>>();
        //services.AddTransient<IEditPropertyManager, EditPropertyManager>();

        services.AddTransient<CreatePropertyManager>(services =>
        {
            return (IPropertyInfoList propertyInfoList) =>
            {
                return new PropertyManager<IProperty>(propertyInfoList, services.GetRequiredService<IFactory>());
            };
        });

        services.AddTransient<CreateValidatePropertyManager>(services =>
        {
            return (IPropertyInfoList propertyInfoList) =>
            {
                return new ValidatePropertyManager<IValidateProperty>(propertyInfoList, services.GetRequiredService<IFactory>());
            };
        });

        services.AddTransient<CreateEditPropertyManager>(services =>
        {
            return (IPropertyInfoList propertyInfoList) =>
            {
                return new EditPropertyManager(propertyInfoList, services.GetRequiredService<IFactory>());
            };
        });

        // Simple wrapper - Always InstancePerDependency
        services.AddTransient(typeof(IBaseServices<>), typeof(BaseServices<>));
        services.AddTransient(typeof(IValidateBaseServices<>), typeof(ValidateBaseServices<>));
        services.AddTransient(typeof(IEditBaseServices<>), typeof(EditBaseServices<>));

        services.AddTransient<IAllRequiredRulesExecuted.Factory>(cc =>
        {
            return (IEnumerable<IRequiredRule> rules) =>
            {
                return new AllRequiredRulesExecuted(rules);
            };
        });

        if (!assemblies.Contains(typeof(AddNeatooServicesExtension).Assembly))
        {
            assemblies = assemblies.Append(typeof(AddNeatooServicesExtension).Assembly).ToArray();
        }

        services.AddNeatooRemoteFactory(portalServer, assemblies);

        services.RemoveAll<NeatooJsonConverterFactory>();
        services.AddTransient<NeatooJsonConverterFactory, NeatooBaseJsonConverterFactory>();
        services.AddTransient(typeof(NeatooBaseJsonTypeConverter<>));
        services.AddTransient(typeof(NeatooListBaseJsonTypeConverter<>));


    }

    public static IServiceCollection AddTransientSelf<I, T>(this IServiceCollection services) where T : class, I where I : class
    {
        services.AddTransient<I, T>();
        services.AddTransient(
            provider => (T)provider.GetService<I>());
        return services;
    }

    public static IServiceCollection AddScopedSelf<I, T>(this IServiceCollection services) where T : class, I where I : class
    {
        services.AddScoped<I, T>();
        services.AddScoped(
            provider => (T)provider.GetService<I>());
        return services;
    }

}
