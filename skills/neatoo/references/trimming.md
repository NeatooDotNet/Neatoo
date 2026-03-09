# IL Trimming Support

Neatoo supports IL trimming for Blazor WASM applications. Consumer domain model projects can enable `<IsTrimmable>true</IsTrimmable>` and Blazor WASM apps can publish with `<PublishTrimmed>true</PublishTrimmed>` without Neatoo's APIs causing trimming warnings or failures.

## Trimming Strategy

Neatoo itself is NOT marked `<IsTrimmable>true</IsTrimmable>`. Instead, Neatoo's public API type parameters are annotated with `[DynamicallyAccessedMembers]` so the trimmer preserves the necessary members when consumer assemblies enable trimming. Reflection-heavy internal methods that cannot be made fully trim-safe use `[UnconditionalSuppressMessage]` with documented justifications.

### Why Neatoo Is Not Marked Trimmable

Neatoo's JSON converters, property system, and rule system use extensive reflection internally. Marking Neatoo itself as trimmable would surface these as errors in Neatoo's own build. Instead, the approach is:
- Annotate public API surfaces so consumers get clean builds
- Suppress internal reflection warnings with documented justifications
- Enable `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>` on Neatoo.csproj for ongoing detection of new trim-unsafe patterns

## Annotated Type Parameters

The following type parameters have `[DynamicallyAccessedMembers(PublicProperties | NonPublicProperties)]`:

| Type | Parameter | Purpose |
|------|-----------|---------|
| `ValidateBase<T>` | `T` | Property discovery via `PropertyInfoList<T>` and LazyLoad reflection |
| `EntityBase<T>` | `T` | Inherits from ValidateBase; same property preservation |
| `IPropertyInfoList<T>` | `T` | Interface for property registry |
| `PropertyInfoList<T>` | `T` | Discovers properties via `Type.GetProperties()` at startup |
| `IValidateBaseServices<T>` | `T` | Service cascade for ValidateBase |
| `ValidateBaseServices<T>` | `T` | Implementation of service cascade |
| `IEntityBaseServices<T>` | `T` | Service cascade for EntityBase |
| `EntityBaseServices<T>` | `T` | Implementation of service cascade |
| `IPropertyFactory<TOwner>` | `TOwner` | Creates property wrappers |
| `DefaultPropertyFactory<TOwner>` | `TOwner` | ValidateBase property factory |
| `EntityPropertyFactory<TOwner>` | `TOwner` | EntityBase property factory |
| `IRuleManager<T>` | `T` | Rule management interface |
| `RuleManagerFactory<T>` | `T` | Creates rule managers |
| `RuleManager<T>` | `T` | Rule management implementation |

`AddTransientSelf<I,T>` and `AddScopedSelf<I,T>` in `AddNeatooServices.cs` have `[DynamicallyAccessedMembers(PublicConstructors)]` on `T` to preserve constructors for DI resolution.

List base classes (`ValidateListBase<I>`, `EntityListBase<I>`) do NOT have annotations on their type parameter `I` -- they do not use reflection on the child type. Child entities handle their own property discovery.

## Suppressed Reflection Sites

These methods have `[UnconditionalSuppressMessage]` because they use reflection patterns that cannot be made fully trim-safe:

| Method | Warning(s) | Justification |
|--------|-----------|---------------|
| `PropertyInfoList.RegisterProperties()` | IL2075 | BaseType walk; T annotation preserves entire hierarchy |
| `ValidateBase.GetLazyLoadProperties()` | IL2070 | Callers use `GetType()` which returns concrete type preserved by T annotation |
| `ValidatePropertyManager.GetProperty()` | IL2075, IL2060 | `GetType().GetMethod` + `MakeGenericMethod` on framework types always preserved |
| `NeatooBaseJsonConverterFactory.CanConvert()` | IL2070 | `GetInterfaces` on types from serialization graph |
| `NeatooBaseJsonConverterFactory.CreateConverter()` | IL2070, IL2055 | `GetInterfaces` + `MakeGenericType`; types preserved by RemoteFactory generated `FactoryServiceRegistrar` |
| `NeatooBaseJsonTypeConverter<T>.Read()` | IL2026, IL2055, IL2072, IL2075 | `JsonSerializer.Deserialize`, `MakeGenericType`, `Activator.CreateInstance`, `GetProperties`; types preserved by generated code |
| `NeatooBaseJsonTypeConverter<T>.DeserializeValidateProperty()` | IL2026, IL2067 | `JsonSerializer.Deserialize`, `Activator.CreateInstance` for framework property types |
| `NeatooBaseJsonTypeConverter<T>.Write()` | IL2026, IL2075 | `JsonSerializer.Serialize`, `GetProperties` on runtime types |
| `NeatooListBaseJsonTypeConverter<T>.Read()` | IL2026 | `JsonSerializer.Deserialize` with runtime types |
| `NeatooListBaseJsonTypeConverter<T>.Write()` | IL2026 | `JsonSerializer.Serialize` with runtime types |
| `RequiredRule<T>.Execute()` | IL2072 | `Activator.CreateInstance` for value type defaults; value type constructors are CLR intrinsics, never trimmed |
| `AttributeToRule.CreateTriggerProperty<T>()` | IL2026 | `Expression.Property`; properties preserved by `DynamicallyAccessedMembers` chain |

### Why Suppressions Are Safe

All suppressions rely on RemoteFactory's generated `FactoryServiceRegistrar` creating static type references that root all consumer domain types. The generated code calls `services.AddTransient<IMyEntity, MyEntity>()` etc., which creates references the trimmer preserves. Neatoo's JSON converters find these types at runtime via `IServiceAssemblies.FindType()`. Without the generated registrations, trimming would break -- but that is the consumer's responsibility (and why consumer projects must use generated service registration).

## Consumer Project Configuration

### Domain Model Project

Add `<IsTrimmable>true</IsTrimmable>` to mark the domain model assembly as trim-compatible:

```xml
<PropertyGroup>
    <IsTrimmable>true</IsTrimmable>
</PropertyGroup>
```

This enables the trim analyzer on the domain model project. Neatoo's annotated type parameters prevent trimming warnings from Neatoo API usage.

### Blazor WASM App Project

Add trimming configuration to the Blazor WASM app:

```xml
<PropertyGroup>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>full</TrimMode>
</PropertyGroup>

<ItemGroup>
    <RuntimeHostConfigurationOption Include="NeatooRuntime.IsServerRuntime"
                                    Value="false"
                                    Trim="true" />
</ItemGroup>
```

The `RuntimeHostConfigurationOption` sets `NeatooRuntime.IsServerRuntime` to `false` at trim time, enabling dead code elimination of server-only factory method bodies (internal methods with `IsServerRuntime` guards).

## Trimming Results (Person Example)

With the Person example app configured as above:

| Assembly | Untrimmed | Trimmed | Reduction |
|----------|-----------|---------|-----------|
| Person.DomainModel | 59,157 bytes | 4,373 bytes | 93% |
| Neatoo | 143,637 bytes | 60,693 bytes | 58% |
| Person.Dal | 6,421 bytes | Removed entirely | 100% |
| Neatoo.Blazor.MudNeatoo | 63,765 bytes | Removed entirely | 100% |

The 93% reduction in Person.DomainModel demonstrates that internal factory method bodies (server-only code) and their dependencies are effectively removed by the trimmer.

## Interaction with Internal Factory Methods

IL trimming works in concert with the internal factory methods pattern. Child entity persistence methods (`[Insert]`, `[Update]`, `[Delete]`, `[Fetch]`) that are `internal` get `IsServerRuntime` guards in generated code. When published with `IsServerRuntime=false` and `Trim="true"`, the trimmer eliminates these guarded code paths as dead code, along with their server-only dependencies (EF Core, repositories, etc.).

## Related

- [Entities](entities.md) - Child Entity Factory Method Visibility section
- [Source Generation](source-generation.md) - What gets generated
- RemoteFactory skill - `NeatooRuntime.IsServerRuntime` feature switch
