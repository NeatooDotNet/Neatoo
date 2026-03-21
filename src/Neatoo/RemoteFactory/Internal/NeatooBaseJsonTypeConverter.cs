using System.Diagnostics.CodeAnalysis;
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

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "JsonSerializer.Deserialize with runtime Type is required for polymorphic deserialization. " +
        "Types are preserved by RemoteFactory generated FactoryServiceRegistrar static references.")]
    [UnconditionalSuppressMessage("Trimming", "IL2055",
        Justification = "MakeGenericType creates ValidateProperty<T>/EntityProperty<T> with property types " +
        "from the serialized object. These property types are preserved by [DynamicallyAccessedMembers] " +
        "on the owning entity's type parameter.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "Activator.CreateInstance fallback for types not in DI. Types resolved via " +
        "IServiceAssemblies.FindType() from $type discriminator. RemoteFactory generated code " +
        "preserves all domain types via static references in FactoryServiceRegistrar.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "GetProperties() on runtime concrete types for property discovery during deserialization. " +
        "Properties are preserved by [DynamicallyAccessedMembers] on entity type parameters.")]
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        List<PropertyInfo> editProperties = null;
        List<PropertyInfo> lazyLoadProperties = null;
        var editBaseType = typeToConvert;



        T? result = default;
        var id = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    NeatooReferenceResolver.Current?.AddReference(id, result);
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

                var resolver = NeatooReferenceResolver.Current
                    ?? throw new JsonException("Cannot resolve $ref: no NeatooReferenceResolver is active. Use NeatooJsonSerializer for deserialization.");
                result = (T)resolver.ResolveReference(refId);

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

                // Detect LazyLoad<> properties on the concrete type (works for both EntityBase and ValidateBase)
                lazyLoadProperties = result.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition() == typeof(LazyLoad<>)
                        && p.SetMethod != null)
                    .ToList();

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
            else if (lazyLoadProperties != null && lazyLoadProperties.Any(p => p.Name == propertyName))
            {
                var property = lazyLoadProperties.First(p => p.Name == propertyName);
                var deserialized = JsonSerializer.Deserialize(ref reader, property.PropertyType, options);
                var existing = property.GetValue(result);

                if (existing is ILazyLoadDeserializable mergeable && deserialized is ILazyLoadDeserializable source)
                {
                    // Merge: apply serialized state into existing instance (preserves loader delegate)
                    mergeable.ApplyDeserializedState(source.BoxedValue, source.IsLoaded);
                }
                else if (existing == null && deserialized != null)
                {
                    // No constructor-created instance -- fall back to replacement
                    property.SetValue(result, deserialized);
                }
                // If existing != null && deserialized == null: keep existing (constructor's instance)
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
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "JsonSerializer.Deserialize with runtime types for property values and rule messages. " +
        "Value types are preserved by [DynamicallyAccessedMembers] on entity type parameters. " +
        "IRuleMessage[] is a framework type always preserved.")]
    [UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "Activator.CreateInstance for ValidateProperty<T> and EntityProperty<T>. " +
        "These are Neatoo framework types with public constructors that are always preserved " +
        "because they are directly referenced in Neatoo's own code.")]
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

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "JsonSerializer.Serialize with runtime types for property serialization. " +
        "Types are preserved by RemoteFactory generated FactoryServiceRegistrar static references " +
        "and [DynamicallyAccessedMembers] on entity type parameters.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "GetProperties() on runtime concrete types for LazyLoad property discovery and " +
        "IEntityMetaProperties/IFactorySaveMeta property iteration during serialization. " +
        "Entity properties are preserved by [DynamicallyAccessedMembers] on type parameters. " +
        "IEntityMetaProperties and IFactorySaveMeta are framework interfaces always preserved.")]
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // Cast to internal interface to access GetProperties
        var properties = ((value is IValidateBaseInternal baseInternal)
            && baseInternal.PropertyManager is IValidatePropertyManagerInternal<IValidateProperty> pmInternal)
            ? pmInternal.GetProperties.ToList()
            : new List<IValidateProperty>();

        var resolver = NeatooReferenceResolver.Current;

        if (resolver != null)
        {
            var reference = resolver.GetReference(value, out var alreadyExists);

            if (alreadyExists)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("$ref");
                writer.WriteStringValue(reference);
                writer.WriteEndObject();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName("$id");
            writer.WriteStringValue(reference);
        }
        else
        {
            // No resolver: write without $id/$ref reference tracking (degraded but functional)
            writer.WriteStartObject();
        }

        writer.WritePropertyName("$type");
        writer.WriteStringValue(value.GetType().FullName);

        writer.WritePropertyName("PropertyManager");

        writer.WriteStartArray();

        foreach (var p in properties)
        {
            // Skip LazyLoad property subclasses -- they are serialized as
            // top-level JSON properties alongside the existing LazyLoad<> path,
            // not as part of the PropertyManager array.
            if (p is ILazyLoadProperty)
                continue;

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
            // IEntityMetaProperties intentionally does not extend IValidateMetaProperties
            // so this reflection call only picks up persistence state (IsModified, IsChild, etc.)
            // and not validation state (IsValid, IsBusy, PropertyMessages, etc.)
            var editProperties = typeof(IEntityMetaProperties).GetProperties().ToList();
            editProperties.AddRange(typeof(IFactorySaveMeta).GetProperties());

            foreach (var p in editProperties)
            {
                writer.WritePropertyName(p.Name);
                JsonSerializer.Serialize(writer, p.GetValue(editMetaProperties), p.PropertyType, options);
            }
        }

        // Serialize LazyLoad<> properties on the concrete type
        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(LazyLoad<>))
            {
                var propValue = property.GetValue(value);
                if (propValue != null)
                {
                    writer.WritePropertyName(property.Name);
                    JsonSerializer.Serialize(writer, propValue, property.PropertyType, options);
                }
            }
        }

        writer.WriteEndObject();
    }
}
