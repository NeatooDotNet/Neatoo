using System;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory.Internal;

public class NeatooListBaseJsonTypeConverter<T> : JsonConverter<T>
        where T : IListBase
{
    private readonly IServiceProvider scope;
    private readonly IServiceAssemblies localAssemblies;

    public NeatooListBaseJsonTypeConverter(IServiceProvider scope, IServiceAssemblies localAssemblies)
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

        T list = default;
        string id = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    options.ReferenceHandler.CreateResolver().AddReference(id, list);
                }

                if (list is IJsonOnDeserialized jsonOnDeserialized)
                {
                    jsonOnDeserialized.OnDeserialized();
                }

                return list;
            }

            // Get the key.
            if (reader.TokenType != JsonTokenType.PropertyName) { throw new JsonException(); }

            string propertyName = reader.GetString();
            reader.Read();

            if (propertyName == "$ref")
            {
                var refId = reader.GetString();
                list = (T)options.ReferenceHandler.CreateResolver().ResolveReference(refId);
                reader.Read();
                return list;
            }
            else if (propertyName == "$id")
            {
                id = reader.GetString();
            }
            else if (propertyName == "$type")
            {
                var typeString = reader.GetString();
                var type = localAssemblies.FindType(typeString);
                list = (T)scope.GetRequiredService(type);

                if (list is IJsonOnDeserializing jsonOnDeserializing)
                {
                    jsonOnDeserializing.OnDeserializing();
                }
            }
            else if (propertyName == "$items")
            {
                if (reader.TokenType != JsonTokenType.StartArray) { throw new JsonException(); }

                Type type = default;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray) { break; }
                    if (reader.TokenType != JsonTokenType.StartObject) { throw new JsonException(); }

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject) { break; }

                        propertyName = reader.GetString();
                        reader.Read();

                        if (propertyName == "$type")
                        {
                            var typeString = reader.GetString();
                            type = localAssemblies.FindType(typeString);
                        }
                        else if (propertyName == "$value")
                        {
                            var item = JsonSerializer.Deserialize(ref reader, type, options);

                            list.Add(item);
                        }
                    }
                }
            }

        }

        throw new JsonException();
    }
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var items = value.GetEnumerator();

        writer.WriteStartObject();

        var reference = options.ReferenceHandler.CreateResolver().GetReference(value, out var alreadyExists);

        writer.WritePropertyName("$id");
        writer.WriteStringValue(reference);

        writer.WritePropertyName("$type");
        writer.WriteStringValue(value.GetType().FullName);

        writer.WritePropertyName("$items");
        writer.WriteStartArray();

        while (items.MoveNext())
        {
            writer.WriteStartObject();

            writer.WritePropertyName("$type");
            writer.WriteStringValue(items.Current.GetType().FullName);

            writer.WritePropertyName("$value");

            JsonSerializer.Serialize(writer, items.Current, items.Current.GetType(), options);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
