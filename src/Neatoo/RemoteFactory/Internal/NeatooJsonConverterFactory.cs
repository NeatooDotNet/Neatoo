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
