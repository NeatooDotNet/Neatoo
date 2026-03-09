using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Neatoo.RemoteFactory.Internal;

public class NeatooBaseJsonConverterFactory : NeatooJsonConverterFactory
{
    private IServiceProvider scope;
    private readonly IServiceAssemblies serviceAssemblies;

    public NeatooBaseJsonConverterFactory(IServiceProvider scope, IServiceAssemblies serviceAssemblies)
    {
        this.scope = scope;
        this.serviceAssemblies = serviceAssemblies;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "GetInterfaces() is called on types passed to JsonConverter.CanConvert(). " +
        "These types are resolved by System.Text.Json from the serialization graph and their interfaces " +
        "are preserved because RemoteFactory generated FactoryServiceRegistrar creates static references " +
        "that root all domain types and their interfaces.")]
    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert.IsAssignableTo(typeof(IValidateBase))
                || typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ValidateBase<>))
        {
            return true;
        }
        else if (typeToConvert.IsAssignableTo(typeof(IValidateListBase)) ||
            typeToConvert.GetInterfaces().Where(x => x.IsGenericType).Any(x => x.GetGenericTypeDefinition() == typeof(IValidateListBase<>)))
        {
            return true;
        }
        else if ((typeToConvert.IsInterface || typeToConvert.IsAbstract) && !typeToConvert.IsGenericType && this.serviceAssemblies.HasType(typeToConvert))
        {
            return true;
        }

        return false;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "GetInterfaces() on types from the serialization graph. Types are preserved " +
        "by RemoteFactory generated FactoryServiceRegistrar static references.")]
    [UnconditionalSuppressMessage("Trimming", "IL2055",
        Justification = "MakeGenericType creates NeatooBaseJsonTypeConverter<T>/NeatooListBaseJsonTypeConverter<T>/" +
        "NeatooInterfaceJsonTypeConverter<T> with types from the serialization graph. These converter types " +
        "are registered as open generics in AddNeatooServices, and the type arguments are preserved " +
        "by RemoteFactory generated FactoryServiceRegistrar.")]
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert.IsAssignableTo(typeof(IValidateBase)))
        {
            return (JsonConverter)this.scope.GetRequiredService(typeof(NeatooBaseJsonTypeConverter<>).MakeGenericType(typeToConvert));
        }
        else if (typeToConvert.IsAssignableTo(typeof(IValidateListBase)) ||
            typeToConvert.GetInterfaces().Where(x => x.IsGenericType).Any(x => x.GetGenericTypeDefinition() == typeof(IValidateListBase<>)))
        {
            return (JsonConverter)this.scope.GetRequiredService(typeof(NeatooListBaseJsonTypeConverter<>).MakeGenericType(typeToConvert));
        }
        else if (typeToConvert.IsInterface || typeToConvert.IsAbstract)
        {
            return (JsonConverter)this.scope.GetRequiredService(typeof(NeatooInterfaceJsonTypeConverter<>).MakeGenericType(typeToConvert));
        }

        return null;
    }
}
