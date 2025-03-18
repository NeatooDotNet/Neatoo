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
        if (typeToConvert.IsAssignableTo(typeof(IBase))
                || typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Base<>))
        {
            return true;
        }
        else if (typeToConvert.IsAssignableTo(typeof(IListBase)))
        {
            return true;
        }
        else if ((typeToConvert.IsInterface || typeToConvert.IsAbstract) && !typeToConvert.IsGenericType && serviceAssemblies.HasType(typeToConvert))
        {
            return true;
        }

        return false;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert.IsAssignableTo(typeof(IBase)))
        {
            return (JsonConverter)scope.GetRequiredService(typeof(NeatooBaseJsonTypeConverter<>).MakeGenericType(typeToConvert));
        }
        else if (typeToConvert.IsAssignableTo(typeof(IListBase)))
        {
            return (JsonConverter)scope.GetRequiredService(typeof(NeatooListBaseJsonTypeConverter<>).MakeGenericType(typeToConvert));
        }
        else if (typeToConvert.IsInterface || typeToConvert.IsAbstract)
        {
            return (JsonConverter)scope.GetRequiredService(typeof(NeatooInterfaceJsonTypeConverter<>).MakeGenericType(typeToConvert));
        }

        return null;
    }
}
