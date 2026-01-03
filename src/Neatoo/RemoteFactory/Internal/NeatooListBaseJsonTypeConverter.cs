using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;

namespace Neatoo.RemoteFactory.Internal;

public class NeatooListBaseJsonTypeConverter<T> : JsonConverter<T>
{
    private readonly IServiceProvider scope;
    private readonly IServiceAssemblies localAssemblies;

    public NeatooListBaseJsonTypeConverter(IServiceProvider scope, IServiceAssemblies localAssemblies)
    {
        this.scope = scope;
        this.localAssemblies = localAssemblies;
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        IList? list = default;
        var id = string.Empty;

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

                return (T?) list;
            }

            // Get the key.
            if (reader.TokenType != JsonTokenType.PropertyName) { throw new JsonException(); }

            var propertyName = reader.GetString();
            reader.Read();

            if (propertyName == "$ref")
            {
                var refId = reader.GetString();
                list = (IList)options.ReferenceHandler.CreateResolver().ResolveReference(refId);
                reader.Read();
                return (T) list;
            }
            else if (propertyName == "$id")
            {
                id = reader.GetString();
            }
            else if (propertyName == "$type")
            {
                var typeString = reader.GetString();
                var type = this.localAssemblies.FindType(typeString);
                list = (IList)this.scope.GetRequiredService(type);

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
                            type = this.localAssemblies.FindType(typeString);
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
        if(!(value is IList list))
        {
            throw new JsonException($"{value.GetType()} is not an IList");
        }

        if (value is IJsonOnSerializing jsonOnSerializing)
        {
            jsonOnSerializing.OnSerializing();
        }


        writer.WriteStartObject();

        var reference = options.ReferenceHandler.CreateResolver().GetReference(value, out var alreadyExists);

        writer.WritePropertyName("$id");
        writer.WriteStringValue(reference);

        writer.WritePropertyName("$type");
        writer.WriteStringValue(value.GetType().FullName);

        writer.WritePropertyName("$items");
        writer.WriteStartArray();

        void addItems(IEnumerator items)
        {
            while(items.MoveNext())
            {
                var item = items.Current;
                writer.WriteStartObject();
                writer.WritePropertyName("$type");
                writer.WriteStringValue(item.GetType().FullName);
                writer.WritePropertyName("$value");
                JsonSerializer.Serialize(writer, item, item.GetType(), options);
                writer.WriteEndObject();
            }
        }
        addItems(list.GetEnumerator());
        // Cast to internal interface to access DeletedList
        if (value is IEntityListBaseInternal editListInternal)
        {
            addItems(editListInternal.DeletedList.GetEnumerator());
        }

        writer.WriteEndArray();

        writer.WriteEndObject();

        if (value is IJsonOnSerialized jsonOnSerialized)
        {
            jsonOnSerialized.OnSerialized();
        }
    }
}
