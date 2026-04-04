using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections;

namespace Neatoo.RemoteFactory.Internal;

public class NeatooListBaseJsonTypeConverter<T> : JsonConverter<T>
{
    private readonly IServiceProvider scope;
    private readonly IServiceAssemblies localAssemblies;
    private readonly ILogger trace;

    public NeatooListBaseJsonTypeConverter(IServiceProvider scope, IServiceAssemblies localAssemblies)
    {
        this.scope = scope;
        this.localAssemblies = localAssemblies;
        this.trace = scope.GetService<ILoggerFactory>()?.CreateLogger("Neatoo.Trace")
            ?? NullLoggerFactory.Instance.CreateLogger("Neatoo.Trace");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "JsonSerializer.Deserialize with runtime Type for polymorphic list item deserialization. " +
        "Item types are resolved via IServiceAssemblies.FindType() from $type discriminator and are " +
        "preserved by RemoteFactory generated FactoryServiceRegistrar static references.")]
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var listSw = Stopwatch.StartNew();
        IList? list = default;
        var id = string.Empty;
        string? listTypeName = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    NeatooReferenceResolver.Current?.AddReference(id, list);
                }

                if (list is IJsonOnDeserialized jsonOnDeserialized)
                {
                    jsonOnDeserialized.OnDeserialized();
                }

                listSw.Stop();
                trace.ListReadComplete(listTypeName ?? "?", list?.Count ?? 0, listSw.ElapsedMilliseconds);
                return (T?) list;
            }

            // Get the key.
            if (reader.TokenType != JsonTokenType.PropertyName) { throw new JsonException(); }

            var propertyName = reader.GetString();
            reader.Read();

            if (propertyName == "$ref")
            {
                var refId = reader.GetString();
                var resolver = NeatooReferenceResolver.Current
                    ?? throw new JsonException("Cannot resolve $ref: no NeatooReferenceResolver is active. Use NeatooJsonSerializer for deserialization.");
                list = (IList)resolver.ResolveReference(refId);
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
                listTypeName = typeString;
                trace.ListReadResolvingType(typeString ?? "null");
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
                var itemIndex = 0;

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
                            var itemSw = Stopwatch.StartNew();
                            var item = JsonSerializer.Deserialize(ref reader, type, options);
                            itemSw.Stop();
                            trace.ListReadItemDeserialized(itemIndex, type?.Name ?? "?", itemSw.ElapsedMilliseconds);

                            list.Add(item);
                            itemIndex++;
                        }
                    }
                }
            }

        }

        throw new JsonException();
    }
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "JsonSerializer.Serialize with runtime types for polymorphic list item serialization. " +
        "Item types are preserved by RemoteFactory generated FactoryServiceRegistrar static references.")]
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if(!(value is IList list))
        {
            throw new JsonException($"{value.GetType()} is not an IList");
        }

        var writeSw = Stopwatch.StartNew();
        var listWriteTypeName = value.GetType().Name;
        trace.ListWriteStarted(listWriteTypeName, list.Count);

        if (value is IJsonOnSerializing jsonOnSerializing)
        {
            jsonOnSerializing.OnSerializing();
        }


        writer.WriteStartObject();

        var resolver = NeatooReferenceResolver.Current;
        if (resolver != null)
        {
            var reference = resolver.GetReference(value, out var alreadyExists);
            writer.WritePropertyName("$id");
            writer.WriteStringValue(reference);
        }

        writer.WritePropertyName("$type");
        writer.WriteStringValue(value.GetType().FullName);

        writer.WritePropertyName("$items");
        writer.WriteStartArray();

        var itemCount = 0;
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
                itemCount++;
            }
        }
        addItems(list.GetEnumerator());
        var activeCount = itemCount;
        // Cast to internal interface to access DeletedList
        if (value is IEntityListBaseInternal editListInternal)
        {
            addItems(editListInternal.DeletedList.GetEnumerator());
            var deletedCount = itemCount - activeCount;
            if (deletedCount > 0)
            {
                trace.ListWriteDeletedItems(listWriteTypeName, deletedCount);
            }
        }

        writer.WriteEndArray();

        writer.WriteEndObject();

        if (value is IJsonOnSerialized jsonOnSerialized)
        {
            jsonOnSerialized.OnSerialized();
        }

        writeSw.Stop();
        trace.ListWriteCompleted(listWriteTypeName, writeSw.ElapsedMilliseconds, itemCount);
    }
}
