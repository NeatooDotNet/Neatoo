using System.Text.Json.Serialization;
using System.Text.Json;
using System.Reflection;
using Neatoo.Internal;
using Neatoo.Rules;

namespace Neatoo.RemoteFactory.Internal;

public class NeatooBaseJsonTypeConverter<T> : JsonConverter<T>
        where T : IValidateBase
{
    private readonly IServiceProvider scope;
    private readonly IServiceAssemblies localAssemblies;

    public NeatooBaseJsonTypeConverter(IServiceProvider scope, IServiceAssemblies localAssemblies)
    {
        this.scope = scope;
        this.localAssemblies = localAssemblies;
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        List<PropertyInfo> editProperties = null;
        var editBaseType = typeToConvert;



        T? result = default;
        var id = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    options.ReferenceHandler.CreateResolver().AddReference(id, result);
                }

                if (result is IJsonOnDeserialized jsonOnDeserialized)
                {
                    jsonOnDeserialized.OnDeserialized();
                }

                return result;
            }

            // Get the key.
            if (reader.TokenType != JsonTokenType.PropertyName) { throw new JsonException(); }

            var propertyName = reader.GetString();
            reader.Read();

            if (propertyName == "$ref")
            {
                var refId = reader.GetString();

                result = (T)options.ReferenceHandler.CreateResolver().ResolveReference(refId);

                reader.Read();

                return result;
            }
            else if (propertyName == "$id")
            {
                id = reader.GetString();
            }
            else if (propertyName == "$type")
            {
                var fullName = reader.GetString();
                var type = this.localAssemblies.FindType(fullName);
                result = (T)this.scope.GetService(type);

                if(result == null)
                {
                    try
                    {
                        result = (T?)Activator.CreateInstance(type, []);
                    }
                    catch(Exception ex) {
                        throw new JsonException($"{type.FullName} must either be registered or have a parameterless constructor");
                    }
                }

                if(result == null)
                {
                    throw new JsonException($"{type.FullName} must either be registered or have a parameterless constructor");
                }

                if (result is IJsonOnDeserializing jsonOnDeserializing)
                {
                    jsonOnDeserializing.OnDeserializing();
                }

                editBaseType = result.GetType();

                do
                {
                    if (editBaseType.IsGenericType && editBaseType.GetGenericTypeDefinition() == typeof(EntityBase<>))
                    {
                        editProperties = editBaseType.GetProperties().Where(p => p.SetMethod != null).ToList();
                        break;
                    }

                    editBaseType = editBaseType.BaseType;

                } while (editBaseType != null);

            }
            else if (propertyName == "PropertyManager")
            {

                var list = new List<IValidateProperty>();

                if (reader.TokenType != JsonTokenType.StartArray) { throw new JsonException(); }

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray) { break; }
                    if (reader.TokenType != JsonTokenType.StartObject) { throw new JsonException(); }

                    Type propertyType = default;
                    string? pName = null;
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject) { break; }
                        if (reader.TokenType != JsonTokenType.PropertyName) { throw new JsonException(); }

                        propertyName = reader.GetString();

                        reader.Read();

                        if (propertyName == "$name")
                        {
                            pName = reader.GetString();
                            propertyType = result.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).First(p => p.Name == pName).PropertyType;
                        }
                        else if (propertyName == "$type")
                        {
                            var typeFullName = reader.GetString();

                            // Assume a ValidateProperty<T> of some derivation
                            var pType = this.localAssemblies.FindType(typeFullName);
                            propertyType = pType.MakeGenericType(propertyType);

                        }
                        else if (propertyName == "$value")
                        {
                            var property = DeserializeValidateProperty(ref reader, propertyType, options);
                            list.Add(property);
                        }
                    }
                }

                // Cast to internal interface to access PropertyManager
                if (result is IValidateBaseInternal resultInternal)
                {
                    resultInternal.PropertyManager.SetProperties(list);

                    if (resultInternal.PropertyManager is IJsonOnDeserialized jsonOnDeserialized)
                    {
                        jsonOnDeserialized.OnDeserialized();
                    }
                }

            }
            else if (editProperties != null && editProperties.Any(p => p.Name == propertyName))
            {
                var property = editProperties.First(p => p.Name == propertyName);
                var value = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                property.SetValue(result, value);
            }
        }

        throw new JsonException();
    }

    /// <summary>
    /// Manually deserializes a ValidateProperty or EntityProperty JSON object,
    /// reading each field individually. The Value field is deserialized as a standalone
    /// value (not as a constructor parameter), which avoids the STJ limitation where
    /// reference metadata ($id/$ref) cannot appear in constructor parameters.
    /// </summary>
    private static IValidateProperty DeserializeValidateProperty(
        ref Utf8JsonReader reader, Type propertyType, JsonSerializerOptions options)
    {
        var valueType = propertyType.GetGenericArguments()[0];
        var isEntityProperty = propertyType.GetGenericTypeDefinition() == typeof(EntityProperty<>);

        string? name = null;
        object? value = null;
        bool isReadOnly = false;
        IRuleMessage[]? serializedRuleMessages = null;
        bool isSelfModified = false;

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject for ValidateProperty");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            var fieldName = reader.GetString();
            reader.Read();

            switch (fieldName)
            {
                case "Name":
                    name = reader.GetString();
                    break;
                case "Value":
                    value = JsonSerializer.Deserialize(ref reader, valueType, options);
                    break;
                case "IsReadOnly":
                    isReadOnly = reader.GetBoolean();
                    break;
                case "SerializedRuleMessages":
                    serializedRuleMessages = JsonSerializer.Deserialize<IRuleMessage[]>(
                        ref reader, options);
                    break;
                case "IsSelfModified":
                    isSelfModified = reader.GetBoolean();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        serializedRuleMessages ??= Array.Empty<IRuleMessage>();

        IValidateProperty result;
        if (isEntityProperty)
        {
            result = (IValidateProperty)Activator.CreateInstance(
                propertyType, name, value, isSelfModified,
                isReadOnly, serializedRuleMessages)!;
        }
        else
        {
            result = (IValidateProperty)Activator.CreateInstance(
                propertyType, name, value,
                serializedRuleMessages, isReadOnly)!;
        }

        if (result is IJsonOnDeserialized jsonOnDeserialized)
        {
            jsonOnDeserialized.OnDeserialized();
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // Cast to internal interface to access GetProperties
        var properties = ((value is IValidateBaseInternal baseInternal)
            && baseInternal.PropertyManager is IValidatePropertyManagerInternal<IValidateProperty> pmInternal)
            ? pmInternal.GetProperties.ToList()
            : new List<IValidateProperty>();

        var reference = options.ReferenceHandler.CreateResolver().GetReference(value, out var alreadyExists);

        if (alreadyExists)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("$ref");
            writer.WriteStringValue(reference);
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteStartObject();

            writer.WritePropertyName("$id");
            writer.WriteStringValue(reference);

            writer.WritePropertyName("$type");
            writer.WriteStringValue(value.GetType().FullName);

            writer.WritePropertyName("PropertyManager");

            writer.WriteStartArray();

            foreach (var p in properties)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("$name");
                writer.WriteStringValue(p.Name);

                writer.WritePropertyName("$type");
                writer.WriteStringValue(p.GetType().GetGenericTypeDefinition().FullName);

                writer.WritePropertyName("$value");

                JsonSerializer.Serialize(writer, p, p.GetType(), options);

                writer.WriteEndObject();
            }


            writer.WriteEndArray();

            if (value is IEntityMetaProperties editMetaProperties)
            {
                var editProperties = typeof(IEntityMetaProperties).GetProperties().ToList();
                editProperties.AddRange(typeof(IFactorySaveMeta).GetProperties());

                foreach (var p in editProperties)
                {
                    writer.WritePropertyName(p.Name);
                    JsonSerializer.Serialize(writer, p.GetValue(editMetaProperties), p.PropertyType, options);
                }
            }

            writer.WriteEndObject();
        }
    }
}
