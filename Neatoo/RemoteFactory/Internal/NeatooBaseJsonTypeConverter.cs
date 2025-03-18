using System.Text.Json.Serialization;
using System.Text.Json;
using Neatoo.Core;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.Internal;

namespace Neatoo.RemoteFactory.Internal;

public class NeatooBaseJsonTypeConverter<T> : JsonConverter<T>
        where T : IBase
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



        T result = default;
        string id = string.Empty;

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

            string propertyName = reader.GetString();
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
                var type = localAssemblies.FindType(fullName);
                result = (T)scope.GetRequiredService(type);

                if (result is IJsonOnDeserializing jsonOnDeserializing)
                {
                    jsonOnDeserializing.OnDeserializing();
                }

                // TODO : Ugly code block
                editBaseType = result.GetType();

                do
                {
                    if (editBaseType.IsGenericType && editBaseType.GetGenericTypeDefinition() == typeof(EditBase<>))
                    {
                        editProperties = editBaseType.GetProperties().Where(p => p.SetMethod != null).ToList();
                        break;
                    }

                    editBaseType = editBaseType.BaseType;

                } while (editBaseType != null);

            }
            else if (propertyName == "PropertyManager")
            {

                List<IProperty> list = new List<IProperty>();

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

                            // Assume a Property<T> of some derivation
                            var pType = localAssemblies.FindType(typeFullName);
                            propertyType = pType.MakeGenericType(propertyType);

                        }
                        else if (propertyName == "$value")
                        {
                            var property = JsonSerializer.Deserialize(ref reader, propertyType, options);
                            list.Add((IProperty)property);
                        }
                    }
                }

                result.PropertyManager.SetProperties(list);

                if (result.PropertyManager is IJsonOnDeserialized jsonOnDeserialized)
                {
                    jsonOnDeserialized.OnDeserialized();
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
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var properties = value.PropertyManager.GetProperties.ToList();

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

            if (value is IEditMetaProperties editMetaProperties)
            {
                var editProperties = typeof(IEditMetaProperties).GetProperties().ToList();
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
