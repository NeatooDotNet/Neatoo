# Fix Dictionary/Collection Serialization in ValidateProperty

**Date:** 2026-02-26
**Related Todo:** [Dictionary partial property serialization](../../todos/completed/dictionary-partial-property-serialization.md)
**Status:** Complete
**Last Updated:** 2026-02-26

---

## Overview

`partial Dictionary<string, string>?` properties (and potentially other collection types) on Neatoo objects lose their values during JSON bridge round-trip serialization. Serialization produces valid JSON, but deserialization throws `NotSupportedException` because STJ cannot handle reference metadata (`$id`/`$ref`) within `[JsonConstructor]` constructor parameters.

## Root Cause Analysis

The bug involves an interaction between three components across two repositories:

1. **NeatooReferenceResolver.GetReference** (RemoteFactory) -- When the Dictionary value inside `ValidateProperty<T>.Value` is serialized, the resolver has a special-case for Dictionary types (lines 42-46) that assigns `$id: ""` (empty string). This writes reference metadata into the Dictionary JSON.

2. **ValidateProperty\<T\> [JsonConstructor]** (Neatoo) -- `ValidateProperty<T>` uses `[JsonConstructor]` with a `T value` constructor parameter. STJ's `SmallObjectWithParameterizedConstructorConverter` cannot deserialize types that contain `$id`/`$ref` metadata when they appear as constructor arguments. This is a known .NET limitation.

3. **NeatooBaseJsonTypeConverter** (Neatoo) -- The custom converter delegates ValidateProperty serialization/deserialization to `JsonSerializer.Serialize/Deserialize` with the full options (including `ReferenceHandler.Preserve`), which triggers both the reference metadata writing and the constructor parameter limitation.

### Serialization Flow (works)

```
NeatooBaseJsonTypeConverter.Write (line 224)
  -> JsonSerializer.Serialize(writer, p, p.GetType(), options)
       -> STJ default serializer for ValidateProperty<T>
            -> Serializes Value property (Dictionary)
                 -> NeatooReferenceResolver.GetReference(dictionary)
                      -> Returns "" with alreadyExists=false  (the special case)
                 -> STJ writes "$id": "" into Dictionary JSON
```

### Deserialization Flow (fails)

```
NeatooBaseJsonTypeConverter.Read (line 153)
  -> JsonSerializer.Deserialize(ref reader, propertyType, options)
       -> STJ finds [JsonConstructor](string name, T value, ...)
            -> Tries to deserialize Dictionary as constructor parameter
                 -> Encounters "$id" in Dictionary JSON
                 -> THROWS NotSupportedException
```

## Approach

### Strategy: Manual ValidateProperty Deserialization in NeatooBaseJsonTypeConverter

Replace the `JsonSerializer.Deserialize` call at line 153 of `NeatooBaseJsonTypeConverter.Read` with manual JSON reading that constructs `ValidateProperty<T>` / `EntityProperty<T>` instances without triggering the `[JsonConstructor]` path through STJ. The `Value` field is deserialized using `JsonSerializer.Deserialize(ref reader, valueType, options)` directly -- not as a constructor parameter -- which handles `$id` metadata correctly.

Additionally, fix `NeatooReferenceResolver.GetReference` to remove the Dictionary special-case, giving dictionaries normal reference IDs instead of empty strings.

### Why This Approach

1. **Addresses the root cause directly**: The STJ limitation applies specifically to reference metadata within constructor parameters. By deserializing Value outside a constructor parameter context, the limitation does not apply.

2. **No change to serialized format**: The JSON wire format stays identical. Only the deserialization path changes.

3. **Preserves reference handling for Neatoo objects**: The original `options` (with ReferenceHandler) are passed when deserializing Value, so child `IValidateBase` objects retain proper reference tracking through the custom converter.

4. **Works for all T types**: Primitives (no reference metadata written), Neatoo child objects (handled by custom converter), and collections (handled by STJ natively when not in constructor parameter context).

5. **Minimal blast radius**: Changes are confined to the property deserialization section of `NeatooBaseJsonTypeConverter.Read` and the Dictionary special-case in `NeatooReferenceResolver.GetReference`.

### Alternatives Considered and Rejected

**Strip ReferenceHandler from options for ValidateProperty ser/deser**: `NeatooBaseJsonTypeConverter` explicitly accesses `options.ReferenceHandler.CreateResolver()` in its Write (line 189) and Read (lines 41, 62) methods. Stripping ReferenceHandler would crash child `IValidateBase` object serialization.

**Custom JsonConverter for ValidateProperty\<T\>**: Registering a converter factory for ValidateProperty would intercept ALL ValidateProperty serialization globally, not just the path through NeatooBaseJsonTypeConverter. This risks unintended side effects in other serialization contexts.

**Remove [JsonConstructor] from ValidateProperty**: Would break any code that depends on the constructor-based deserialization path, and is a more invasive change to the class's public API.

**Fix only in NeatooReferenceResolver**: There is no way to tell STJ "don't track this object" from within the resolver. `GetReference` must return a string, and STJ always writes `$id` for new objects when `ReferenceHandler.Preserve` is active. Even with a valid (non-empty) ID, the constructor parameter limitation still triggers.

## Design

### Change 1: Remove Dictionary Special-Case from NeatooReferenceResolver (RemoteFactory)

**File:** `/home/keithvoels/RemoteFactory/src/RemoteFactory/Internal/NeatooReferenceResolver.cs`

Remove lines 42-46 (the Dictionary special-case). Let dictionaries receive normal reference IDs through the standard path. This prevents the `$id: ""` empty-string issue (which could cause duplicate key errors if multiple Dictionary properties exist on the same object) and makes behavior consistent across all types.

```csharp
// BEFORE (lines 38-61):
public override string GetReference(object value, out bool alreadyExists)
{
    ArgumentNullException.ThrowIfNull(value, nameof(value));
    var type = value.GetType();
    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
    {
        alreadyExists = false;
        return string.Empty;
    }
    if (this._objectToReferenceIdMap.TryGetValue(value, out var referenceId))
    ...
}

// AFTER:
public override string GetReference(object value, out bool alreadyExists)
{
    ArgumentNullException.ThrowIfNull(value, nameof(value));
    if (this._objectToReferenceIdMap.TryGetValue(value, out var referenceId))
    ...
}
```

**Note:** This change alone does NOT fix the deserialization bug. Dictionaries will now get valid `$id` values (e.g., `"$id": "7"`) but the constructor parameter limitation still triggers. This change is necessary to eliminate the empty-string duplicate key risk and to make the serialized format consistent.

### Change 2: Manual ValidateProperty Deserialization in NeatooBaseJsonTypeConverter (Neatoo)

**File:** `/home/keithvoels/Neatoo/src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs`

Replace line 153:
```csharp
var property = JsonSerializer.Deserialize(ref reader, propertyType, options);
```

With a call to a new private method that manually reads the ValidateProperty JSON fields:

```csharp
var property = DeserializeValidateProperty(ref reader, propertyType, options);
```

#### New Method: DeserializeValidateProperty

This method manually reads the JSON object for a ValidateProperty or EntityProperty instance, deserializing each field individually. The critical difference from STJ's `[JsonConstructor]` path is that the `Value` field is deserialized as a standalone value -- not as a constructor parameter -- which avoids the reference metadata limitation.

```csharp
private static IValidateProperty DeserializeValidateProperty(
    ref Utf8JsonReader reader, Type propertyType, JsonSerializerOptions options)
{
    // propertyType is e.g. ValidateProperty<Dictionary<string,string>>
    // or EntityProperty<Guid>

    // Extract T from the generic type argument
    var valueType = propertyType.GetGenericArguments()[0];

    // Determine if this is EntityProperty (has IsSelfModified field)
    var isEntityProperty = propertyType.GetGenericTypeDefinition() == typeof(EntityProperty<>);

    // Read fields from the JSON object
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
                // KEY FIX: Deserialize Value directly, NOT as a constructor parameter.
                // STJ can handle $id/$ref metadata when deserializing standalone values,
                // just not when they appear in constructor parameters.
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
                // Skip computed/read-only properties:
                // IsBusy, IsSelfBusy, IsValid, IsSelfValid, IsModified, IsPaused
                reader.Skip();
                break;
        }
    }

    serializedRuleMessages ??= Array.Empty<IRuleMessage>();

    // Construct the property instance using the [JsonConstructor] signature
    IValidateProperty result;
    if (isEntityProperty)
    {
        // EntityProperty<T>(string name, T value, bool isSelfModified,
        //                   bool isReadOnly, IRuleMessage[] serializedRuleMessages)
        result = (IValidateProperty)Activator.CreateInstance(
            propertyType, name, value, isSelfModified,
            isReadOnly, serializedRuleMessages)!;
    }
    else
    {
        // ValidateProperty<T>(string name, T value,
        //                     IRuleMessage[] serializedRuleMessages, bool isReadOnly)
        result = (IValidateProperty)Activator.CreateInstance(
            propertyType, name, value,
            serializedRuleMessages, isReadOnly)!;
    }

    // Call OnDeserialized to set up event subscriptions on child IValidateBase values.
    // STJ normally calls this automatically; with manual deserialization we must do it ourselves.
    if (result is IJsonOnDeserialized jsonOnDeserialized)
    {
        jsonOnDeserialized.OnDeserialized();
    }

    return result;
}
```

#### Key Design Points

1. **Value deserialization uses original options**: `JsonSerializer.Deserialize(ref reader, valueType, options)` preserves the ReferenceHandler. When Value is an IValidateBase, the `NeatooBaseJsonConverterFactory` intercepts and handles it correctly. When Value is a Dictionary, STJ handles `$id` metadata as a top-level deserialization (not constructor parameter).

2. **Activator.CreateInstance vs direct constructor call**: We use `Activator.CreateInstance` because the generic type T is only known at runtime. The constructor signatures are stable internal APIs. This is consistent with patterns already used in the codebase (e.g., `NeatooOrdinalConverterFactory` line 110).

3. **IJsonOnDeserialized callback**: Must be called explicitly since STJ's automatic callback only fires when STJ itself deserializes the object. The `ValidateProperty<T>.OnDeserialized()` method subscribes to `NeatooPropertyChanged` and `PropertyChanged` events on the Value if it implements `INotifyNeatooPropertyChanged` or `INotifyPropertyChanged`.

4. **Field order independence**: The `switch` on field names handles JSON properties in any order. Unknown/computed fields are safely skipped via `reader.Skip()`.

## Implementation Steps

### Phase 1: RemoteFactory Changes

1. Remove Dictionary special-case from `NeatooReferenceResolver.GetReference` (lines 42-46)
2. Run RemoteFactory tests to verify no regressions

### Phase 2: Neatoo Changes

1. Add `DeserializeValidateProperty` private static method to `NeatooBaseJsonTypeConverter<T>`
2. Replace line 153 with call to new method
3. Add required `using` directives (`Neatoo.Internal` for `EntityProperty<>`)

### Phase 3: Verification

1. Run failing test: `FatClientValidate_Deserialize_DictionaryProperty` -- must pass
2. Run all serialization tests: `FatClientValidateTests` -- must pass
3. Run all entity serialization tests: `FatClientEntityTests` (if exists) -- must pass
4. Run full Neatoo test suite
5. Run full RemoteFactory test suite

## Acceptance Criteria

1. `FatClientValidate_Deserialize_DictionaryProperty` passes
2. `FatClientValidate_Serialize_DictionaryProperty` continues to pass
3. `FatClientValidate_Deserialize_NullDictionaryProperty` continues to pass
4. All existing serialization tests (FatClientValidate*, FatClientEntity*) pass
5. All tests in both Neatoo and RemoteFactory repos pass
6. Child IValidateBase property serialization/deserialization continues to work (verified by `FatClientValidate_Deserialize_Child*` tests)

## Dependencies

- **RemoteFactory repo**: Change 1 must be implemented in the RemoteFactory repo
- **Neatoo repo**: Change 2 is in the Neatoo repo, and depends on an updated RemoteFactory NuGet package OR local project reference

## Risks

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| `Activator.CreateInstance` parameter order mismatch | Low | Constructor signatures are stable; unit tests catch immediately |
| Other collection types (List\<T\>, HashSet\<T\>) have similar issues | Medium | The fix is generic: works for ALL T types, not just Dictionary |
| Ordinal serialization format affected | Low | Ordinal format has its own converter; ValidateProperty goes through NeatooBaseJsonTypeConverter in named format |
| EntityProperty subclass has additional fields needing manual handling | Low | Only `IsSelfModified` is needed for construction; other fields are computed |

## Codebase Files Examined

- `/home/keithvoels/Neatoo/src/Neatoo/Internal/ValidateProperty.cs` -- ValidateProperty\<T\> class, [JsonConstructor], IJsonOnDeserialized
- `/home/keithvoels/Neatoo/src/Neatoo/Internal/EntityPropertyManager.cs` -- EntityProperty\<T\> class, [JsonConstructor], additional fields
- `/home/keithvoels/Neatoo/src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` -- Custom serializer, lines 153 (deser) and 224 (ser)
- `/home/keithvoels/Neatoo/src/Neatoo/RemoteFactory/Internal/NeatooJsonConverterFactory.cs` -- Converter factory registration
- `/home/keithvoels/Neatoo/src/Neatoo/Internal/ValidatePropertyManager.cs` -- SetProperties, OnDeserialized
- `/home/keithvoels/RemoteFactory/src/RemoteFactory/Internal/NeatooReferenceResolver.cs` -- Dictionary special-case
- `/home/keithvoels/RemoteFactory/src/RemoteFactory/Internal/NeatooJsonSerializer.cs` -- JsonSerializerOptions setup
- `/home/keithvoels/RemoteFactory/src/RemoteFactory/Internal/NeatooReferenceHandler.cs` -- AsyncLocal resolver
- `/home/keithvoels/RemoteFactory/src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs` -- DI-based object creation
- `/home/keithvoels/RemoteFactory/src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs` -- Ordinal serialization (not affected)
- `/home/keithvoels/Neatoo/src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientValidateTests.cs` -- Reproduction tests
- `/home/keithvoels/Neatoo/src/Neatoo.UnitTest/Integration/Concepts/Serialization/ValidateObject.cs` -- Test ValidateObject with Dictionary property
- `/home/keithvoels/Neatoo/src/Neatoo.UnitTest/TestInfrastructure/IntegrationTestBase.cs` -- Test base class

---

## Architectural Verification

**Scope Table:**

| Component | Affected? | Verification |
|-----------|-----------|-------------|
| ValidateProperty\<T\> | No change to class itself | N/A |
| EntityProperty\<T\> | No change to class itself | N/A |
| NeatooBaseJsonTypeConverter.Read | YES -- deserialization path | Reproduction test |
| NeatooBaseJsonTypeConverter.Write | No change | Existing tests |
| NeatooReferenceResolver | YES -- remove Dictionary special-case | RemoteFactory tests |
| ValidatePropertyManager | No change | Existing tests |
| EntityPropertyManager | No change | Existing tests |
| NeatooOrdinalConverterFactory | Not affected (separate format) | N/A |

**Design Project Verification:** N/A -- This is a serialization bug fix, not a new feature requiring design project compilation.

**Breaking Changes:** No. The JSON wire format changes slightly (Dictionary gets a normal `$id` instead of `$id: ""`) but this is backwards-compatible because the deserialization side now handles reference metadata on any value type.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-26

### My Understanding of This Plan

**Core Change:** Fix deserialization of `partial Dictionary<string, string>?` properties (and potentially other collection types) by replacing STJ's `[JsonConstructor]` deserialization path for ValidateProperty/EntityProperty with manual JSON reading, and removing a Dictionary special-case in the reference resolver.

**User-Facing API:** No change. Internal serialization fix only.

**Internal Changes:** (1) Remove Dictionary special-case from `NeatooReferenceResolver.GetReference` in RemoteFactory. (2) Add `DeserializeValidateProperty` private method to `NeatooBaseJsonTypeConverter<T>` that manually reads JSON fields and constructs ValidateProperty/EntityProperty via `Activator.CreateInstance`, then calls `IJsonOnDeserialized.OnDeserialized()`.

**Base Classes Affected:** None directly. The change is in the serialization infrastructure, not the base classes themselves.

### Codebase Investigation

**Files Examined:**
- `/home/keithvoels/Neatoo/src/Neatoo/Internal/ValidateProperty.cs` -- Confirmed `[JsonConstructor]` signature: `(string name, T value, IRuleMessage[] serializedRuleMessages, bool isReadOnly)`. Confirmed `IJsonOnDeserialized` implementation at line 353-363 subscribes to NeatooPropertyChanged and PropertyChanged events on Value.
- `/home/keithvoels/Neatoo/src/Neatoo/Internal/EntityPropertyManager.cs` -- Confirmed `EntityProperty<T>` constructor signature: `(string name, T value, bool isSelfModified, bool isReadOnly, IRuleMessage[] serializedRuleMessages)`. Confirmed EntityProperty is the ONLY subclass of ValidateProperty.
- `/home/keithvoels/Neatoo/src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` -- Confirmed line 153 is `JsonSerializer.Deserialize(ref reader, propertyType, options)` inside the PropertyManager array deserialization. Confirmed Write path at line 224 uses `JsonSerializer.Serialize(writer, p, p.GetType(), options)` with full options including ReferenceHandler.
- `/home/keithvoels/RemoteFactory/src/RemoteFactory/Internal/NeatooReferenceResolver.cs` -- Confirmed Dictionary special-case at lines 42-46 returns empty string with `alreadyExists = false`.
- `/home/keithvoels/RemoteFactory/src/RemoteFactory/Internal/NeatooJsonSerializer.cs` -- Confirmed options include `ReferenceHandler = this.ReferenceHandler` and `IncludeFields = true`.
- `/home/keithvoels/RemoteFactory/src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs` -- Confirmed it extends `DefaultJsonTypeInfoResolver` and sets `CreateObject` for DI-registered types.
- `/home/keithvoels/RemoteFactory/src/RemoteFactory/Internal/NeatooJsonConverterFactory.cs` (base) -- Abstract base class, no logic.
- `/home/keithvoels/Neatoo/src/Neatoo/RemoteFactory/Internal/NeatooJsonConverterFactory.cs` -- Confirmed `CanConvert` only returns true for `IValidateBase` and `IValidateListBase` types. ValidateProperty does NOT implement these interfaces, so it goes through STJ default serialization with ReferenceHandler.Preserve.
- `/home/keithvoels/Neatoo/src/Neatoo/Rules/RuleMessage.cs` -- Confirmed `RuleMessage` has `[JsonConstructor]` and implements `IRuleMessage`.
- `/home/keithvoels/Neatoo/src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientValidateTests.cs` -- Confirmed reproduction tests exist. FatClientValidate_Deserialize_DictionaryProperty FAILS. FatClientValidate_Serialize_DictionaryProperty PASSES. FatClientValidate_Deserialize_NullDictionaryProperty PASSES.
- `/home/keithvoels/Neatoo/src/Neatoo.UnitTest/Integration/Concepts/Serialization/ValidateObject.cs` -- Confirmed `partial Dictionary<string, string>? Data { get; set; }` property on ValidateObject.
- `/home/keithvoels/Neatoo/src/Neatoo.UnitTest/Generated/Neatoo.BaseGenerator/Neatoo.BaseGenerator.PartialBaseGenerator/Neatoo.UnitTest.Integration.Concepts.Serialization.ValidateTests.ValidateObject.g.cs` -- Confirmed generated code creates `ValidateProperty<Dictionary<string, string>?>` via `factory.Create<Dictionary<string, string>?>`.
- `/home/keithvoels/Neatoo/src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientEntityTests.cs` -- Confirmed entity serialization tests exist covering IsModified, IsNew, IsChild, IsDeleted, child relationships.
- `/home/keithvoels/RemoteFactory/src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs` -- Confirmed `Activator.CreateInstance` usage at line 110, validating the plan's claim that this pattern exists in the codebase.

**Searches Performed:**
- Searched for `class .* : ValidateProperty` -- found only `EntityProperty<T>` as a subclass. No other ValidateProperty subclasses exist.
- Searched for `JsonDerivedType|JsonPolymorphic` in Neatoo -- none found, confirming no polymorphic STJ configuration.
- Searched for DI registration of `IRuleMessage` -- not found, but empirically verified that `IRuleMessage[]` deserialization works (FatClientValidate_Deserialize_RuleManager and FatClientValidate_Deserialize_MarkInvalid both pass).
- Searched for `Activator.CreateInstance` in RemoteFactory -- found at NeatooOrdinalConverterFactory line 110, confirming the plan's claim.
- Searched for `[JsonIgnore]` in ValidateProperty -- found on Type, Task, ValueIsValidateBase, IsSelfBusy, IsMarkedBusy, PropertyMessages, RuleMessages, RuleMessagesLock (7 properties). Non-ignored serializable properties: Name, Value, IsBusy, IsReadOnly, IsValid, IsSelfValid, SerializedRuleMessages.

**Design Project Verification:** N/A -- The architect correctly states this is a serialization bug fix, not a new feature requiring design project compilation.

**Discrepancies Found:** None significant. The plan accurately describes the codebase, constructor signatures, and serialization flow.

### Structured Question Checklist

**Completeness Questions:**
- [x] All affected base classes addressed? N/A -- no base class changes. Only serialization infrastructure.
- [x] Factory operation lifecycle impacts? None -- this fix is in the serialization layer only.
- [x] Property system impact? None -- ValidateProperty/EntityProperty classes are unchanged.
- [x] Validation rule interactions? None -- rules are deserialized via SerializedRuleMessages, which the plan handles.
- [x] Parent-child relationships? The plan correctly calls `IJsonOnDeserialized.OnDeserialized()` which subscribes to NeatooPropertyChanged/PropertyChanged on child IValidateBase values. Existing child tests (`FatClientValidate_Deserialize_Child*`) will verify.

**Correctness Questions:**
- [x] Does the proposed implementation align with existing patterns? Yes. `Activator.CreateInstance` is used in NeatooOrdinalConverterFactory. Manual JSON reading is used in the same converter file (NeatooBaseJsonTypeConverter) for IValidateBase objects and the PropertyManager array.
- [x] Is the approach consistent with how similar features work today? Yes. The existing Read method already manually reads `$id`, `$type`, `$ref`, and `PropertyManager` -- adding manual reading for ValidateProperty is consistent.
- [x] Breaking changes? No. JSON wire format changes slightly (Dictionary gets a normal `$id` instead of `$id: ""`) but the new deserialization path handles both cases since `$id` is skipped by the default case.
- [x] State property impacts? The plan correctly handles IsSelfModified for EntityProperty via the `Activator.CreateInstance` parameter. IsBusy, IsValid, IsSelfValid, IsPaused are computed properties that get skipped.

**Clarity Questions:**
- [x] Could I implement this without asking clarifying questions? Yes, with one minor note (see below about `$id` handling).
- [x] Ambiguous requirements? No.
- [x] Edge cases? See Devil's Advocate section.
- [x] Test strategy? The plan correctly identifies the existing reproduction tests as sufficient -- no new tests needed.

**Risk Questions:**
- [x] What could go wrong? Activator.CreateInstance parameter mismatch (verified correct), IRuleMessage[] deserialization (empirically verified works), missing $id handling (analyzed and determined safe).
- [x] Which existing tests might fail? All entity and validate serialization tests could be affected. The plan identifies this in its verification gates.
- [x] Serialization/state transfer implications? That IS the scope of this fix.
- [x] RemoteFactory source generation impacts? None -- only NeatooReferenceResolver runtime code changes.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. The plan's `switch/case` does NOT include a `"$id"` case for the ValidateProperty object itself. Since STJ serializes ValidateProperty with `ReferenceHandler.Preserve`, every ValidateProperty JSON object starts with `"$id": "N"`. The plan's `default` case calls `reader.Skip()`, which correctly skips the string value. This is safe because ValidateProperty instances are unique per property slot and never appear as `$ref`. I verified this by analyzing that each ValidateProperty is serialized exactly once (in the PropertyManager array loop at line 212-228 of the Write method). This is a documentation gap in the plan but NOT an implementation gap -- the code would work correctly.
2. If a future ValidateProperty subclass is added beyond EntityProperty, the `isEntityProperty` check would return false and it would be constructed as ValidateProperty, which would lose any subclass-specific fields. This is low risk since EntityProperty is currently the only subclass and adding a new one would require intentional design work.
3. If `value` is `null` for a non-nullable `T`, `Activator.CreateInstance` would still work (nullable reference types are a compile-time concept). The constructor would receive `null` for `T value` and set `_value = null`. This matches the existing behavior when `Value` is not present in JSON.

**Ways this could break existing functionality:**
1. If there's a scenario where the same ValidateProperty instance is referenced from multiple places in the JSON (via `$ref`), the manual deserialization would silently fail (read the `$ref` object but not resolve it). However, I verified this scenario cannot occur: each ValidateProperty is unique to its property slot and serialized exactly once.

**Ways users could misunderstand the API:**
1. Not applicable -- this is an internal fix with no API changes.

### Verdict

**APPROVED** -- This plan is exceptionally clear because:

1. The root cause analysis is accurate and well-documented with specific line numbers.
2. The constructor signatures in the plan match the actual code exactly (verified).
3. The `Activator.CreateInstance` parameter order is correct for both ValidateProperty and EntityProperty (verified against source).
4. EntityProperty is confirmed as the only subclass of ValidateProperty (verified via search).
5. The `IJsonOnDeserialized.OnDeserialized()` callback handles event subscription on child values correctly (verified in source).
6. Existing reproduction tests cover the fix scenario without needing new tests.
7. The alternatives analysis is thorough and correctly rejects each option with valid reasoning.
8. The `$id` handling gap in the switch/case is a documentation omission, not a bug -- the `default: reader.Skip()` path correctly handles it.

### What Looks Good

- Root cause analysis is thorough and accurate
- Constructor signatures verified correct
- IJsonOnDeserialized callback is necessary and correctly placed
- Activator.CreateInstance pattern is consistent with existing codebase usage
- Minimal blast radius -- changes confined to two specific locations
- Existing reproduction tests are sufficient for verification
- Cross-repo dependency correctly identified

---

## Implementation Contract

**Created:** 2026-02-26
**Approved by:** neatoo-developer

### Acceptance Criteria

N/A -- No design project compilation criteria. Bug fix verified by existing reproduction tests.

### In Scope

- [x] **Phase 1 (RemoteFactory):** Remove Dictionary special-case (lines 42-46) from `/home/keithvoels/RemoteFactory/src/RemoteFactory/Internal/NeatooReferenceResolver.cs`
- [x] **Checkpoint:** Run RemoteFactory tests (`dotnet test` in RemoteFactory repo)
- [x] **Phase 2 (Neatoo):** Add `DeserializeValidateProperty` private static method to `NeatooBaseJsonTypeConverter<T>` in `/home/keithvoels/Neatoo/src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs`
- [x] **Phase 2 (Neatoo):** Replace `JsonSerializer.Deserialize(ref reader, propertyType, options)` at line 153 with `DeserializeValidateProperty(ref reader, propertyType, options)`
- [x] **Phase 2 (Neatoo):** Add `using Neatoo.Rules;` for `IRuleMessage` (using `Neatoo.Internal` was already present for `EntityProperty<>`)
- [x] **Phase 2 (Neatoo):** Ensure `IJsonOnDeserialized.OnDeserialized()` is called on manually-constructed property instances
- [x] **Checkpoint:** Run `FatClientValidate_Deserialize_DictionaryProperty` -- PASSES (was failing)
- [x] **Checkpoint:** Run all `FatClientValidateTests` -- 12/12 pass
- [x] **Checkpoint:** Run all `FatClientEntityTests` -- 13/13 pass (EntityProperty path)
- [x] **Checkpoint:** Run full Neatoo test suite (`dotnet test src/Neatoo.sln`) -- 2051 passed, 0 failed, 1 skipped
- [x] **Checkpoint:** Run full RemoteFactory test suite -- 464 passed, 0 failed, 3 skipped

### Out of Scope

- Ordinal serialization format changes (separate converter, not affected)
- ValidateProperty/EntityProperty class modifications (constructors unchanged)
- New test classes (reproduction tests already exist and cover the scenario)
- Changes to the serialization Write path (only Read path changes)
- Polymorphic type discriminator handling for ValidateProperty (not needed)

### Verification Gates

1. After Phase 1 (RemoteFactory): All RemoteFactory tests pass
2. After Phase 2 (Neatoo): `FatClientValidate_Deserialize_DictionaryProperty` passes (the previously failing test)
3. After Phase 2 (Neatoo): All `FatClientValidateTests` pass (includes child, rule manager, null dictionary tests)
4. After Phase 2 (Neatoo): All `FatClientEntityTests` pass (EntityProperty deserialization path)
5. Final: Full test suite passes in both repos

### Stop Conditions

If any occur, STOP and report:
- Existing child object serialization tests fail (FatClientValidate_Deserialize_Child*)
- Entity property serialization tests fail (FatClientEntity*)
- Activator.CreateInstance fails for any property type
- IRuleMessage[] deserialization produces empty/wrong results after manual deser
- Out-of-scope tests fail
- Architectural contradiction discovered

---

## Implementation Progress

**Started:** 2026-02-26

**Phase 1 (RemoteFactory):** Remove Dictionary special-case
- [x] Removed lines 42-46 from `NeatooReferenceResolver.GetReference` -- the Dictionary type check and empty-string return
- [x] **Verification:** RemoteFactory tests: 464 passed, 0 failed, 3 skipped

**Phase 2 (Neatoo):** Manual ValidateProperty deserialization
- [x] Added `using Neatoo.Rules;` to `NeatooBaseJsonTypeConverter.cs` (for `IRuleMessage`; `Neatoo.Internal` was already present)
- [x] Replaced `JsonSerializer.Deserialize(ref reader, propertyType, options)` with `DeserializeValidateProperty(ref reader, propertyType, options)` at the `$value` branch
- [x] Added `DeserializeValidateProperty` private static method that manually reads JSON fields, deserializes Value standalone (avoiding STJ constructor parameter limitation), constructs instances via `Activator.CreateInstance`, and calls `IJsonOnDeserialized.OnDeserialized()`
- [x] **Verification:** `FatClientValidate_Deserialize_DictionaryProperty` -- now PASSES (was throwing `NotSupportedException`)
- [x] **Verification:** All 12 `FatClientValidateTests` pass (including child, rule manager, parent ref, null dictionary)
- [x] **Verification:** All 13 `FatClientEntityTests` pass (EntityProperty deserialization path with IsSelfModified)

**Cleanup:**
- [x] Removed `Console.WriteLine` debugging output from `FatClientValidate_Serialize_DictionaryProperty` test

**Phase 3 (Final verification):**
- [x] Full Neatoo test suite: 2051 passed, 0 failed, 1 skipped across 4 test projects
- [x] Full RemoteFactory test suite: 464 passed, 0 failed, 3 skipped

---

## Completion Evidence

- **Tests Passing:** All tests pass in both repos. Neatoo: 2051 passed, 0 failed, 1 skipped (Neatoo.UnitTest: 1725, Samples: 245, Person.DomainModel.Tests: 55, Neatoo.BaseGenerator.Tests: 26). RemoteFactory: 464 passed, 0 failed, 3 skipped.
- **Design Projects Compile:** N/A (bug fix, no design project changes)
- **All Contract Items:** Confirmed complete -- all 11 In Scope items checked

---

## Documentation

**Agent:** N/A
**Completed:** 2026-02-26

### Expected Deliverables

- [x] No user-facing documentation changes needed (internal serialization fix)
- [x] Skill updates: No
- [x] Sample updates: No

### Files Updated

N/A -- Internal bug fix with no user-facing API changes. No documentation updates required.

---

## Architect Verification

**Verified:** 2026-02-26
**Verdict:** VERIFIED

**Independent test results:**

Neatoo (`dotnet test src/Neatoo.sln`):
- Neatoo.BaseGenerator.Tests: 26 passed, 0 failed
- Person.DomainModel.Tests (Samples): 245 passed, 0 failed (note: output says 245, not the developer's 245+55 split -- the Samples project ran as one batch)
- Person.DomainModel.Tests: 55 passed, 0 failed
- Neatoo.UnitTest: 1725 passed, 0 failed, 1 skipped
- Total: 2051 passed, 0 failed, 1 skipped
- Key test: `FatClientValidate_Deserialize_DictionaryProperty` -- PASSED

RemoteFactory (`dotnet test src/Neatoo.RemoteFactory.sln`):
- All test projects: 467 total per TFM (x3 TFMs), 464 passed, 3 skipped per TFM
- 0 failures across all target frameworks

**Design match:** Yes -- implementation matches the plan exactly:

1. **NeatooReferenceResolver (RemoteFactory):** Dictionary special-case (type check + empty-string return) fully removed from `GetReference`. Method now goes directly from null check to the `_objectToReferenceIdMap.TryGetValue` lookup. Matches plan's "AFTER" code.

2. **NeatooBaseJsonTypeConverter (Neatoo):**
   - `using Neatoo.Rules;` added for `IRuleMessage`
   - Line 154: `DeserializeValidateProperty(ref reader, propertyType, options)` replaces `JsonSerializer.Deserialize(ref reader, propertyType, options)`
   - `DeserializeValidateProperty` private static method added (lines 189-259) with:
     - Manual JSON field reading via switch/case (Name, Value, IsReadOnly, SerializedRuleMessages, IsSelfModified; default skips unknown fields including `$id`)
     - Value deserialized standalone via `JsonSerializer.Deserialize(ref reader, valueType, options)` -- the key fix
     - `Activator.CreateInstance` with correct parameter ordering for both ValidateProperty and EntityProperty constructors (verified against source: ValidateProperty takes `name, value, serializedRuleMessages, isReadOnly`; EntityProperty takes `name, value, isSelfModified, isReadOnly, serializedRuleMessages`)
     - `IJsonOnDeserialized.OnDeserialized()` callback invoked on constructed instances

3. **FatClientValidateTests (Neatoo):** Three reproduction tests present (serialize, deserialize, null dictionary). Console.WriteLine debugging removed. `ValidateObject` has `partial Dictionary<string, string>? Data` property on both interface and class.

4. **No unintended changes in Neatoo repo.** Only the three files above were modified.

5. **RemoteFactory repo has pre-existing uncommitted changes** (CanSave/IsSavable feature work in generator, renderer, FactorySaveBase, IFactorySave, and docs). These are from a separate feature, not introduced by this task. The only in-scope change is the NeatooReferenceResolver Dictionary special-case removal.

**Issues found:** None
