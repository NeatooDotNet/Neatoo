# LazyLoad Serialization Fix

**Date:** 2026-03-06
**Related Todo:** [LazyLoad Properties Don't Serialize in Client-Server Scenarios](../todos/lazyload-serialization-bug.md)
**Status:** Complete
**Last Updated:** 2026-03-06

---

## Overview

Fix `NeatooBaseJsonTypeConverter<T>` to serialize and deserialize `LazyLoad<T>` properties on Neatoo domain objects. Currently, any entity with a `LazyLoad<T>` property silently loses that data when transferred between client and server because the converter only handles PropertyManager entries and IEntityMetaProperties.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/lazyload-serialization-bug.md#requirements-review)

### Design Project Contracts (code-based)

- `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs:361` (Serialization_PreserveValueAndLoadedState): WHEN a `LazyLoad<T>` with a pre-loaded value is serialized via plain `JsonSerializer.Serialize` and deserialized via `JsonSerializer.Deserialize`, THEN `IsLoaded` remains `true` and `Value` preserves its data. -- Relevance: Proves `LazyLoad<T>` itself round-trips via STJ default serialization. The bug is that `NeatooBaseJsonTypeConverter` never delegates to STJ for these properties.

- `src/Neatoo/LazyLoad.cs:52-98`: `Value` and `IsLoaded` are `[JsonInclude]`. The loader delegate, load task, and error state are `[JsonIgnore]`. A `[JsonConstructor]` parameterless constructor exists. -- Relevance: `LazyLoad<T>` was intentionally designed for JSON round-tripping.

- `skills/neatoo/references/lazy-loading.md:11`: "JSON serialization -- Value and IsLoaded are serialized; the loader delegate is not." -- Relevance: Documents the design intent that is currently only fulfilled in isolation.

### Behavioral Contracts from Tests (code-based)

- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientEntityTests.cs`: Multiple tests verify that property values, meta state (IsNew, IsModified, IsDeleted, IsChild), child entity references, and parent references survive serialization round-trips through the Neatoo converter. -- Relevance: The fix must not break any existing serialization contract.

- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/TwoContainerMetaStateTests.cs`: Tests verify meta property state after factory operations crossing the client-server serialization boundary. -- Relevance: Pattern to follow for new two-container tests with LazyLoad.

### Gaps

- **No Design project representation for LazyLoad serialization.** No `src/Design/` entity uses `LazyLoad<T>`.
- **No integration test for LazyLoad through the Neatoo converter.** Only standalone STJ test exists.
- **ValidateBase entities with LazyLoad properties.** The `editProperties` fallback in Read only applies to `EntityBase<>`. If a `ValidateBase<>` entity has a LazyLoad property, the Read side has no mechanism to restore it.
- **Circular reference handling for LazyLoad.Value.** When `LazyLoad<T>.Value` contains a Neatoo entity that has back-references, the `$id`/`$ref` reference tracking must remain consistent.
- **Post-deserialization lifecycle.** After deserialization, `LazyLoad<T>` has no loader delegate. `LoadAsync()` will throw `InvalidOperationException`. This is already documented and handled by `LazyLoad.cs` line 133.

### Contradictions

None found. The proposed fix aligns with the documented design intent.

### Recommendations for Architect

1. Leverage `LazyLoad<T>`'s built-in JSON support -- delegate to `JsonSerializer` for the wrapper
2. Handle nested Neatoo entities inside `LazyLoad.Value` (must go through Neatoo converter for `$id`/`$ref`)
3. Cover both `EntityBase` and `ValidateBase` paths
4. Add a two-container integration test (ClientServerTestBase style)
5. Watch for `$id`/`$ref` interaction with shared entity references
6. Update CLAUDE-DESIGN.md serialization table

---

## Business Rules (Testable Assertions)

1. WHEN an `EntityBase<T>` entity with a `LazyLoad<TChild>` property (pre-loaded, `IsLoaded=true`) is serialized through `NeatooBaseJsonTypeConverter.Write()`, THEN the JSON output contains a property with the LazyLoad property name, and that property's value includes `Value` and `IsLoaded`. -- Source: Skill documentation `lazy-loading.md:11`, LazyLoad `[JsonInclude]` attributes

2. WHEN the JSON from Rule 1 is deserialized through `NeatooBaseJsonTypeConverter.Read()`, THEN the entity's LazyLoad property is non-null, `IsLoaded` is `true`, and `Value` preserves its data. -- Source: NEW (filling Gap 2)

3. WHEN a `ValidateBase<T>` entity with a `LazyLoad<TChild>` property (pre-loaded) is serialized and deserialized through `NeatooBaseJsonTypeConverter`, THEN the LazyLoad property round-trips correctly (same as Rules 1 and 2). -- Source: NEW (filling Gap 3)

4. WHEN a `LazyLoad<TChild>` property is not loaded (`IsLoaded=false`, `Value=null`) and the parent entity is serialized and deserialized, THEN the deserialized LazyLoad property has `IsLoaded=false` and `Value=null`. -- Source: LazyLoad `[JsonConstructor]` default state

5. WHEN `LazyLoad<T>.Value` contains a Neatoo entity (e.g., `EntityBase` or `ValidateBase`), THEN the inner entity is serialized through the Neatoo converter (with `$id`/`$ref`, PropertyManager, meta properties), not through STJ default serialization. -- Source: NeatooBaseJsonConverterFactory.CanConvert claims IValidateBase types

6. WHEN an entity with a LazyLoad property is serialized and deserialized through a two-container remote call (client->server->client), THEN the LazyLoad property and its inner value survive the round-trip. -- Source: NEW (filling Gap 2, pattern from TwoContainerMetaStateTests)

7. WHEN an entity is serialized by `NeatooBaseJsonTypeConverter.Write()` and does NOT have any `LazyLoad<T>` properties, THEN the serialization output is identical to the current behavior (no regression). -- Source: FatClientEntityTests existing contract

8. WHEN `LazyLoad<T>` is deserialized without a loader delegate, calling `LoadAsync()` THEN throws `InvalidOperationException`. -- Source: `LazyLoad.cs:132-135`, existing behavior preserved

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | EntityBase with pre-loaded LazyLoad serialization | `EntityObject` with `LazyLoad<string>("hello")`, IsLoaded=true | Rule 1 | JSON contains `"LazyLoadProp":{"Value":"hello","IsLoaded":true}` |
| 2 | EntityBase with pre-loaded LazyLoad deserialization | JSON from Scenario 1 | Rule 2 | Deserialized entity has `LazyLoadProp.IsLoaded=true`, `LazyLoadProp.Value="hello"` |
| 3 | ValidateBase with pre-loaded LazyLoad round-trip | `ValidateObject` with `LazyLoad<string>("test")` | Rule 3 | Round-trip preserves IsLoaded and Value |
| 4 | Unloaded LazyLoad round-trip | `EntityObject` with `LazyLoad<string>()` (not loaded) | Rule 4 | Deserialized has `IsLoaded=false`, `Value=null` |
| 5 | LazyLoad with nested Neatoo entity | `EntityObject` with `LazyLoad<IEntityObject>(childEntity)` where child has PropertyManager properties | Rule 5 | Child entity serialized through Neatoo converter with $id, PropertyManager; deserialized child has correct property values |
| 6 | Two-container remote round-trip | Client calls remote [Fetch] that returns entity with pre-loaded LazyLoad | Rule 6 | Client receives entity with LazyLoad.IsLoaded=true and Value intact |
| 7 | No-LazyLoad entity serialization regression | Standard `EntityObject` without LazyLoad properties | Rule 7 | All existing FatClientEntityTests continue to pass |
| 8 | Post-deserialization LoadAsync throws | Deserialized LazyLoad (no loader), call LoadAsync() | Rule 8 | InvalidOperationException thrown |

---

## Approach

The fix targets `NeatooBaseJsonTypeConverter<T>` in both the Write and Read paths. The core strategy is:

1. **Write path**: After serializing PropertyManager and IEntityMetaProperties, detect `LazyLoad<>` properties on the concrete type via reflection and serialize each using `JsonSerializer.Serialize(writer, lazyLoadValue, lazyLoadType, options)`. Because the same `JsonSerializerOptions` is passed through, the `NeatooBaseJsonConverterFactory` will claim any `IValidateBase` types inside `LazyLoad.Value`, ensuring proper `$id`/`$ref` handling.

2. **Read path**: Detect `LazyLoad<>` properties on the concrete type (same reflection as Write) and store them in a property lookup. When a JSON property name matches a known LazyLoad property, deserialize the value using `JsonSerializer.Deserialize(ref reader, propertyType, options)` and set it on the entity via the PropertyInfo setter. This works for both `EntityBase<>` and `ValidateBase<>` entities because it uses the concrete type's properties, not the `editProperties` mechanism that is limited to `EntityBase<>`.

3. **No changes to `LazyLoad<T>` itself**. The class already has proper `[JsonInclude]`, `[JsonIgnore]`, and `[JsonConstructor]` attributes.

4. **No changes to `NeatooBaseJsonConverterFactory`**. `LazyLoad<T>` is correctly NOT claimed by the factory (it's not an `IValidateBase`). The factory will claim inner `IValidateBase` types when STJ processes `LazyLoad<T>.Value`.

---

## Design

### Write Path Changes (`NeatooBaseJsonTypeConverter<T>.Write()`)

After the IEntityMetaProperties block (after line 322, before the final `writer.WriteEndObject()`):

```
Detect LazyLoad<> properties on value.GetType():
  - Scan concrete type for properties where PropertyType.IsGenericType
    && PropertyType.GetGenericTypeDefinition() == typeof(LazyLoad<>)
  - For each matched property:
    1. Get the property value via PropertyInfo.GetValue(value)
    2. If the value is not null:
       writer.WritePropertyName(property.Name)
       JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options)
```

The key insight: by passing the same `options` (which contains the `NeatooBaseJsonConverterFactory`), any `IValidateBase` entity inside `LazyLoad<T>.Value` will be claimed by the Neatoo converter factory and serialized with proper `$id`/`$ref` handling.

### Read Path Changes (`NeatooBaseJsonTypeConverter<T>.Read()`)

After the `$type` processing block creates the instance (after line 112):

```
In addition to detecting EntityBase<> editProperties, also detect LazyLoad<> properties:
  - Scan result.GetType() for properties where PropertyType.IsGenericType
    && PropertyType.GetGenericTypeDefinition() == typeof(LazyLoad<>)
    && PropertyInfo.SetMethod != null (settable)
  - Store these in a separate list (e.g., lazyLoadProperties)
```

In the property dispatch (around line 172):

```
Add a new else-if branch BEFORE the editProperties branch:
  else if (lazyLoadProperties != null && lazyLoadProperties has matching property name)
    var property = match from lazyLoadProperties
    var value = JsonSerializer.Deserialize(ref reader, property.PropertyType, options)
    property.SetValue(result, value)
```

### Why This Works for Both EntityBase and ValidateBase

The current `editProperties` mechanism only works for `EntityBase<>` types (line 104 checks for `EntityBase<>` specifically). The LazyLoad property detection is separate -- it scans the concrete type's properties regardless of base class. This means `ValidateBase<>` entities with LazyLoad properties will also work.

### $id/$ref Consistency

When `LazyLoad<T>.Value` contains a Neatoo entity:
- During Write: `JsonSerializer.Serialize(writer, lazyLoadValue, ...)` with the same options means the inner entity goes through `NeatooBaseJsonTypeConverter.Write()`, which calls `options.ReferenceHandler.CreateResolver().GetReference()`. If the entity was already serialized (e.g., as a PropertyManager value), it will output a `$ref`. If not, it gets a `$id`.
- During Read: `JsonSerializer.Deserialize(ref reader, ...)` with the same options means the inner entity goes through `NeatooBaseJsonTypeConverter.Read()`, which handles `$ref` resolution.

This is exactly the same mechanism used for child entities in PropertyManager properties -- no special handling needed.

### Performance Consideration

The reflection scan for `LazyLoad<>` properties happens once per entity during serialization/deserialization. This is consistent with the existing approach (the converter already uses reflection to access PropertyManager, IEntityMetaProperties, and editProperties). For production use, caching could be added later, but correctness first.

---

## Implementation Steps

1. **Modify `NeatooBaseJsonTypeConverter<T>.Write()`** -- Add LazyLoad property detection and serialization after the IEntityMetaProperties block
2. **Modify `NeatooBaseJsonTypeConverter<T>.Read()`** -- Add LazyLoad property detection in the `$type` block and a new dispatch branch for LazyLoad properties
3. **Add integration test entities** -- Create test entity/validate classes with LazyLoad properties in `src/Neatoo.UnitTest/Integration/Concepts/Serialization/`
4. **Add FatClient serialization tests** -- Test LazyLoad round-trip through the Neatoo converter for both EntityBase and ValidateBase
5. **Add two-container test** -- Test LazyLoad survives full client-server remote call pipeline
6. **Update CLAUDE-DESIGN.md** -- Add LazyLoad properties to the serialization table
7. **Update lazy-loading.md skill** -- Add note about Neatoo converter support
8. **Run all existing tests** -- Verify no regression in existing serialization tests

---

## Acceptance Criteria

- [ ] `NeatooBaseJsonTypeConverter.Write()` serializes `LazyLoad<T>` properties
- [ ] `NeatooBaseJsonTypeConverter.Read()` deserializes `LazyLoad<T>` properties for both EntityBase and ValidateBase
- [ ] Pre-loaded LazyLoad (IsLoaded=true, Value set) survives round-trip
- [ ] Unloaded LazyLoad (IsLoaded=false, Value=null) survives round-trip
- [ ] LazyLoad with nested Neatoo entity Value survives round-trip with proper $id/$ref
- [ ] Two-container remote call with LazyLoad property works
- [ ] All existing serialization tests pass (no regression)
- [ ] Design project builds successfully

---

## Dependencies

- No changes to `LazyLoad<T>` class
- No changes to `NeatooBaseJsonConverterFactory`
- No changes to RemoteFactory
- Depends on existing `[JsonInclude]`/`[JsonConstructor]` attributes on `LazyLoad<T>`

---

## Risks / Considerations

1. **Reflection performance**: The property scan adds a small amount of reflection overhead per entity serialization. This is consistent with existing patterns in the converter and acceptable for correctness. Caching can be added as a future optimization if profiling reveals it as a bottleneck.

2. **Null LazyLoad properties**: If a `LazyLoad<T>` property is `null` (not just unloaded, but the property itself is null -- meaning the factory method hasn't set it yet), the Write path should skip it (don't write null). The Read path will leave it as whatever the entity's default is.

3. **Private setter access**: The skill documentation shows `LazyLoad<T>` properties declared as `public LazyLoad<OrderLineList> OrderLines { get; private set; }`. The `PropertyInfo.SetMethod` will return the private setter, and `PropertyInfo.SetValue()` will work because we're using reflection. This is the same pattern the `editProperties` mechanism uses.

4. **Post-deserialization loader absence**: After deserialization, `LazyLoad<T>._loader` is null because it's `[JsonIgnore]`. Calling `LoadAsync()` throws `InvalidOperationException`. This is existing, documented behavior. If the LazyLoad was pre-loaded before serialization, the Value is available without needing to load. If it was not loaded, `Value` is null and `IsLoaded` is false. The consumer must handle this.

5. **Multiple LazyLoad properties**: An entity could have multiple LazyLoad properties. The implementation must handle all of them, not just the first one found.

---

## Architectural Verification

### Scope Table

| Pattern/Feature | Affected? | Current Support | After Fix |
|----------------|-----------|-----------------|-----------|
| EntityBase + LazyLoad Write | Yes | Not serialized | Serialized |
| EntityBase + LazyLoad Read | Yes | Not deserialized | Deserialized |
| ValidateBase + LazyLoad Write | Yes | Not serialized | Serialized |
| ValidateBase + LazyLoad Read | Yes | Not deserialized (editProperties only for EntityBase) | Deserialized |
| LazyLoad with nested IValidateBase Value | Yes | Not serialized | Serialized through Neatoo converter |
| LazyLoad with simple Value (string, int) | Yes | Not serialized | Serialized through STJ default |
| LazyLoad standalone (no Neatoo converter) | No | Works (existing test) | Unchanged |
| PropertyManager properties | No | Works | Unchanged |
| IEntityMetaProperties | No | Works | Unchanged |
| EntityListBase serialization | No | Works | Unchanged |
| $id/$ref reference handling | No | Works | Unchanged (reused for inner entities) |

### Design Project Verification

**LazyLoad serialization in Design project:** Not found -- no existing `src/Design/` entity used `LazyLoad<T>` before this plan.

Verification code was added to prove that `LazyLoad<T>` properties can be declared on both `EntityBase` and `ValidateBase` entities and the Design project compiles. Serialization round-trip testing belongs in the UnitTest project (which has `NeatooJsonSerializer` access and the `IntegrationTestBase`/`ClientServerTestBase` infrastructure).

- LazyLoad on EntityBase: **Verified (new code)** at `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs:39` -- `LazyLoadEntityDemo : EntityBase<LazyLoadEntityDemo>` with `LazyLoad<string> LazyDescription` property
- LazyLoad on ValidateBase: **Verified (new code)** at `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs:69` -- `LazyLoadValidateDemo : ValidateBase<LazyLoadValidateDemo>` with `LazyLoad<string> LazyContent` property
- Design.sln build: **Passed** -- 0 warnings, 0 errors

### Breaking Changes

**No** -- This change only adds new serialization behavior for a previously-ignored property type. Existing entities without LazyLoad properties are completely unaffected. The Write path adds code AFTER the existing serialization blocks. The Read path adds a new dispatch branch that only matches LazyLoad property names.

### Codebase Analysis

**Files examined:**

- `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` -- The converter to fix
- `src/Neatoo/RemoteFactory/Internal/NeatooJsonConverterFactory.cs` -- Factory that claims types (confirms LazyLoad<T> is NOT claimed)
- `src/Neatoo/LazyLoad.cs` -- LazyLoad class with JSON attributes
- `src/Neatoo/ILazyLoadFactory.cs` -- Factory interface for creating LazyLoad instances
- `src/Neatoo/IMetaProperties.cs` -- IEntityMetaProperties and IValidateMetaProperties interfaces
- `src/Neatoo/EntityBase.cs` -- EntityBase interface definition
- `src/Neatoo/ValidateBase.cs` -- ValidateBase class definition
- `src/Neatoo/AddNeatooServices.cs` -- LazyLoadFactory DI registration
- `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` -- Existing standalone serialization test
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientEntityTests.cs` -- Existing entity serialization tests
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/EntityObject.cs` -- Test entity used in serialization tests
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/TwoContainerMetaStateTests.cs` -- Two-container test pattern
- `src/Neatoo.UnitTest/TestInfrastructure/IntegrationTestBase.cs` -- Test base classes
- `src/Neatoo.UnitTest/ClientServerContainer.cs` -- Client-server container setup
- `src/Design/Design.Domain/Entities/Employee.cs` -- Design entity pattern reference
- `src/Design/Design.Tests/TestInfrastructure.cs` -- Design test infrastructure
- `src/Design/CLAUDE-DESIGN.md` -- Serialization documentation table
- `skills/neatoo/references/lazy-loading.md` -- LazyLoad skill documentation

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Converter fix | developer | Yes | Core bug fix in NeatooBaseJsonTypeConverter.cs, focused scope | None |
| Phase 2: Tests | developer | No | Tests validate Phase 1 changes, needs implementation context | Phase 1 |
| Phase 3: Documentation | developer | Yes | Isolated doc updates to CLAUDE-DESIGN.md and skill file | Phase 1 |

**Parallelizable phases:** Phase 3 can run in parallel with Phase 2 if desired.

**Notes:** Phases 1 and 2 should be in the same agent since the test entities and tests are tightly coupled to understanding the converter changes.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-06

### My Understanding of This Plan

**Core Change:** Fix `NeatooBaseJsonTypeConverter<T>` to serialize and deserialize `LazyLoad<T>` properties on Neatoo domain objects, which are currently silently dropped during client-server transfer.

**User-Facing API:** No API changes. Existing `LazyLoad<T>` properties on entities will now automatically survive serialization round-trips.

**Internal Changes:** Add LazyLoad property detection (via reflection on the concrete type) and serialization/deserialization to both the Write and Read paths of `NeatooBaseJsonTypeConverter<T>`.

**Base Classes Affected:** EntityBase and ValidateBase (both gain LazyLoad serialization support via concrete-type scanning that is independent of the EntityBase-only `editProperties` mechanism).

### Codebase Investigation

**Files Examined:**
- `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` -- Confirmed Write path (lines 261-326) only handles PropertyManager and IEntityMetaProperties. Read path (lines 21-181) handles `$ref`, `$id`, `$type`, `PropertyManager`, and `editProperties` (EntityBase-only, lines 100-112). No LazyLoad handling exists.
- `src/Neatoo/RemoteFactory/Internal/NeatooJsonConverterFactory.cs` -- Confirmed `CanConvert` (lines 18-35) returns true for `IValidateBase`, `ValidateBase<>`, `IValidateListBase`, and abstract/interface types. `LazyLoad<T>` does NOT implement `IValidateBase` (confirmed: it implements `INotifyPropertyChanged`, `IValidateMetaProperties`, `IEntityMetaProperties` only). So the factory correctly will NOT claim `LazyLoad<T>`.
- `src/Neatoo/LazyLoad.cs` -- Confirmed `[JsonInclude]` on `Value` (line 83) and `IsLoaded` (line 93). `[JsonIgnore]` on `_loader` (line 24), `_loadLock` (line 27), `_loadTask` (line 34), `_loadError` (line 37-38 implicit via JsonIgnore on HasLoadError/LoadError). `[JsonConstructor]` on parameterless constructor (line 52). `LoadAsync()` (line 127-144) throws `InvalidOperationException` when `_loader == null` (line 132-135).
- `src/Neatoo/ValidateBase.cs` -- Confirmed `IValidateBase` interface requires `INeatooObject`, `INotifyPropertyChanged`, `INotifyNeatooPropertyChanged`, `IValidateMetaProperties` (line 18).
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientEntityTests.cs` -- 13 existing serialization tests covering property values, meta state, child relationships, parent references, modified properties. All use `IntegrationTestBase.Serialize()`/`Deserialize()` which go through `NeatooJsonSerializer` and thus `NeatooBaseJsonTypeConverter`.
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/EntityObject.cs` -- Test entity with partial properties (ID, Name, Child, ChildList, Required). Uses PropertyManager. No LazyLoad properties.
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/TwoContainerMetaStateTests.cs` -- Two-container test pattern using `ClientServerTestBase`, `GetClientService<IEntityObjectFactory>()`, and `GetServerService<>()`.
- `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` -- Lines 360-375: `Serialization_PreserveValueAndLoadedState` test uses plain `JsonSerializer.Serialize`/`Deserialize` (not Neatoo converter). Confirms standalone LazyLoad serialization works.
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- New file from architect with design comments. `LazyLoadEntityDemo : EntityBase<LazyLoadEntityDemo>` with `LazyLoad<string> LazyDescription` (line 45) and `LazyLoadValidateDemo : ValidateBase<LazyLoadValidateDemo>` with `LazyLoad<string> LazyContent` (line 82).

**Searches Performed:**
- Searched for `IValidateBase` in `LazyLoad.cs` -- 0 matches. Confirmed `LazyLoad<T>` does not implement `IValidateBase`.
- Searched for `interface IValidateBase` -- Found at `ValidateBase.cs:18`, extends `INeatooObject, INotifyPropertyChanged, INotifyNeatooPropertyChanged, IValidateMetaProperties`.
- Searched for `IFactorySaveMeta` -- Defined in RemoteFactory project (`RemoteFactory/src/RemoteFactory/IFactorySaveMeta.cs`). Inherited by `IEntityMetaProperties`. Contains `IsNew` and `IsDeleted`.
- Searched for `editProperties` usage in converter -- Only in Read path (line 28, 106, 172, 174). In Write path, `editProperties` local variable at line 314 is a DIFFERENT variable (confusing naming but separate scope).

**Design Project Verification:**
- LazyLoad on EntityBase: Architect provided verified code at `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs:40` -- CONFIRMED. `LazyLoadEntityDemo : EntityBase<LazyLoadEntityDemo>` with `LazyLoad<string> LazyDescription` property at line 45.
- LazyLoad on ValidateBase: Architect provided verified code at `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs:77` -- CONFIRMED. `LazyLoadValidateDemo : ValidateBase<LazyLoadValidateDemo>` with `LazyLoad<string> LazyContent` property at line 82.
- Design.sln build: Architect reports 0 warnings, 0 errors -- accepted (will verify during implementation).
- Note: Design project only verifies that LazyLoad properties CAN be declared. Serialization round-trip testing is correctly deferred to the UnitTest project since it requires `NeatooJsonSerializer` infrastructure.

**Discrepancies Found:**
- Plan references line numbers that are off by 1-2 (e.g., says line 39, actual is line 40). Minor -- does not affect correctness.
- No other discrepancies found between the plan and the actual codebase.

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `Write()` method, lines ~322-324 area: After `IEntityMetaProperties` block, new code scans `value.GetType().GetProperties()` for properties where `PropertyType.IsGenericType && PropertyType.GetGenericTypeDefinition() == typeof(LazyLoad<>)`. For each match, calls `PropertyInfo.GetValue(value)` then `JsonSerializer.Serialize(writer, propValue, property.PropertyType, options)`. The `[JsonInclude]` attributes on `LazyLoad<T>.Value` and `LazyLoad<T>.IsLoaded` cause STJ to emit both fields. | JSON contains `"LazyDescription":{"Value":"hello","IsLoaded":true}` | Yes | Relies on STJ default serialization for `LazyLoad<T>` wrapper, which is proven by existing `Serialization_PreserveValueAndLoadedState` test. The `writer.WritePropertyName(property.Name)` provides the property name key. |
| 2 | `Read()` method: In `$type` block (lines ~100-112 area), new code scans `result.GetType().GetProperties()` for `LazyLoad<>` properties with `SetMethod != null`, stores in `lazyLoadProperties` list. New dispatch branch at lines ~172 area: `else if (lazyLoadProperties != null && lazyLoadProperties.Any(p => p.Name == propertyName))` -> `JsonSerializer.Deserialize(ref reader, property.PropertyType, options)` -> `property.SetValue(result, deserializedValue)`. `LazyLoad<T>`'s `[JsonConstructor]` (parameterless) creates instance, `[JsonInclude]` on `Value`/`IsLoaded` with private setters restores state. | Entity's LazyLoad property is non-null, `IsLoaded=true`, `Value` preserved | Yes | `PropertyInfo.SetValue` works with private setters via reflection, same pattern as `editProperties` at line 176. |
| 3 | Same `Write()`/`Read()` paths as Rules 1/2. LazyLoad detection scans `value.GetType()` (Write) and `result.GetType()` (Read) -- these are the CONCRETE type, not the base class. The `editProperties` mechanism at lines 100-112 only activates for `EntityBase<>` types, but LazyLoad detection is INDEPENDENT of that check -- it runs for any type. | Works for `ValidateBase` entities identically | Yes | Verified: `editProperties` is null for ValidateBase types (the `do/while` loop at lines 102-112 only breaks on `EntityBase<>`). LazyLoad scan uses separate list/detection. |
| 4 | Write: `LazyLoad<T>` with `IsLoaded=false` has `Value=null`. The `[JsonInclude]` on both properties means STJ emits `{"Value":null,"IsLoaded":false}`. Plan says skip null LazyLoad property values (property itself is null), but an unloaded LazyLoad instance is non-null. So it IS serialized. Read: `JsonSerializer.Deserialize<LazyLoad<T>>()` uses `[JsonConstructor]` parameterless constructor which sets `_isLoaded=false` (line 56), then `[JsonInclude]` private setters set `IsLoaded=false` and `Value=null`. | Unloaded state `IsLoaded=false`, `Value=null` preserved | Yes | Important distinction: null property (skip) vs. unloaded instance (serialize). Plan handles this correctly -- only skips when `PropertyInfo.GetValue` returns null. |
| 5 | Write: `JsonSerializer.Serialize(writer, lazyLoadValue, property.PropertyType, options)` -- the `options` parameter carries the `NeatooBaseJsonConverterFactory` registered in `JsonSerializerOptions.Converters`. When STJ processes `LazyLoad<T>.Value` and `T` is an `IValidateBase` type, `NeatooBaseJsonConverterFactory.CanConvert(typeof(T))` returns true (line 20: `typeToConvert.IsAssignableTo(typeof(IValidateBase))`). STJ delegates to `NeatooBaseJsonTypeConverter<T>.Write()` for the inner entity, which outputs `$id`, `$type`, `PropertyManager`, and meta properties. Read: same mechanism in reverse -- `JsonSerializer.Deserialize` with same options claims the inner `IValidateBase` type. | Inner Neatoo entity serialized with `$id`/`$ref`, PropertyManager, meta properties | Yes | This is the same mechanism used for child entities in PropertyManager properties (line 219: `JsonSerializer.Deserialize(ref reader, valueType, options)` and line 304: `JsonSerializer.Serialize(writer, p, p.GetType(), options)`). |
| 6 | Full pipeline: Server factory creates entity with LazyLoad -> server's `NeatooBaseJsonTypeConverter.Write()` includes LazyLoad property -> JSON transferred to client -> client's `NeatooBaseJsonTypeConverter.Read()` restores LazyLoad property. The `options` containing `NeatooBaseJsonConverterFactory` are created fresh on each side by `NeatooJsonSerializer`. Same converter code runs on both sides. | LazyLoad survives remote call | Yes | Two-container test will validate this end-to-end. The converter is registered identically in both containers via `AddNeatooServices`. |
| 7 | Write: The new LazyLoad scan iterates `value.GetType().GetProperties()` and filters for `PropertyType.GetGenericTypeDefinition() == typeof(LazyLoad<>)`. For entities without any `LazyLoad<>` properties, the iteration produces zero matches and no additional JSON is written. No existing serialization output is affected. | No regression for entities without LazyLoad | Yes | The scan itself has minimal overhead (property reflection, already used elsewhere in the converter). Zero matches = zero additional writes. |
| 8 | No changes to `LazyLoad.cs`. After deserialization via `[JsonConstructor]`, `_loader` is null (line 55: `_loader = null`). Calling `LoadAsync()` hits line 132: `if (_loader == null) throw new InvalidOperationException(...)`. | `InvalidOperationException` thrown | Yes | This is existing, documented behavior. The plan explicitly does not change `LazyLoad.cs`. |

### Structured Question Checklist

**Completeness Questions:**
- [x] All affected base classes addressed? YES -- EntityBase and ValidateBase both covered. EntityListBase and ValidateListBase are not affected (they contain child items, not LazyLoad properties).
- [x] Factory operation lifecycle impacts? NO impact -- the change only affects serialization/deserialization, not factory operations. LazyLoad properties are set in [Create]/[Fetch] methods, which are unaffected.
- [x] Property system impact? NO impact -- LazyLoad properties are regular C# properties, NOT partial properties. They don't use Getter/Setter, LoadValue/SetValue, or PropertyManager.
- [x] Validation rule interactions? NO impact -- validation rules operate on PropertyManager properties. LazyLoad is not a PropertyManager property.
- [x] Parent-child relationships in aggregates? ADDRESSED -- the plan discusses $id/$ref handling for nested Neatoo entities inside LazyLoad.Value (Design section: "$id/$ref Consistency").

**Correctness Questions:**
- [x] Alignment with existing patterns? YES -- the approach (reflection scan + JsonSerializer delegation) is consistent with the existing `editProperties` mechanism in Read and the `IEntityMetaProperties` property reflection in Write.
- [x] Consistency with similar features? YES -- similar to how `editProperties` handles non-PropertyManager properties on EntityBase.
- [x] Breaking changes migration? N/A -- no breaking changes. New behavior is additive.
- [x] State property impacts? NO impact on IsModified, IsNew, IsValid, IsBusy, IsPaused. LazyLoad serialization doesn't change any state property behavior.

**Clarity Questions:**
- [x] Implementable without clarifying questions? YES -- the plan specifies exact insertion points (after line 322 for Write, after line 112 for Read), exact conditions for detection, and exact serialization calls.
- [x] Ambiguous requirements? NO -- all 8 business rules are crisp and unambiguous.
- [x] Edge cases explicitly handled? YES -- null LazyLoad property (skip), unloaded LazyLoad (serialize with IsLoaded=false), multiple LazyLoad properties, nested Neatoo entities in Value, post-deserialization LoadAsync behavior.
- [x] Test strategy specific enough? YES -- 8 test scenarios with specific inputs, expected results, and rule mappings.

**Risk Questions:**
- [x] What could go wrong? (1) Reflection performance -- acknowledged and accepted as consistent with existing patterns. (2) Private setter access -- confirmed to work via reflection, same as editProperties. (3) JSON property ordering -- LazyLoad properties appear after IEntityMetaProperties in the JSON, which is fine since the Read path dispatches by property name.
- [x] Existing tests that might fail? NONE expected -- the change is purely additive. Entities without LazyLoad properties produce identical JSON. All existing FatClientEntityTests and TwoContainerMetaStateTests should pass unchanged.
- [x] Serialization/state transfer implications? This IS a serialization fix. Fully addressed.
- [x] RemoteFactory source generation impacts? NONE -- LazyLoad properties are regular properties, not partial properties. The generators don't process them.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. What if a `LazyLoad<T>` property has NO setter at all (read-only property, e.g., `public LazyLoad<string> X { get; }`)? The Read detection filters for `SetMethod != null`, so this would be skipped on Read. But the Write path would still serialize it. This creates an asymmetry: Write emits the property, Read silently ignores it. This is probably fine in practice since LazyLoad properties with no setter at all would be unusual, but the plan should acknowledge it.
2. What if an entity has a `LazyLoad<T>` property where `T` is itself a `LazyLoad<U>` (nested lazy loads)? This is an extreme edge case. The inner LazyLoad would be serialized through STJ default since LazyLoad doesn't implement IValidateBase. Should work, but is untested.
3. What about LazyLoad properties declared on intermediate base classes (e.g., a `MiddleBase : EntityBase<ConcreteEntity>` with a LazyLoad property, then `ConcreteEntity : MiddleBase`)? `GetType().GetProperties()` returns all public properties including inherited ones, so this should work. Not explicitly tested but logically sound.

**Ways this could break existing functionality:**
1. If any existing entity type happens to have a property of type `LazyLoad<T>` that is currently being handled by `editProperties` (for EntityBase types), the new LazyLoad dispatch branch (which runs BEFORE editProperties) would claim it first. However, I verified that `editProperties` scans from the `EntityBase<>` level, which does NOT include properties declared on concrete subclasses. So there is no overlap. No breakage expected.

**Ways users could misunderstand the API:**
1. After deserialization, users might expect `LazyLoad.LoadAsync()` to work. The plan documents that it throws `InvalidOperationException` because the loader delegate is not serialized. This is existing behavior, but worth highlighting in the skill documentation update.

### Concerns

No blocking concerns found.

**Non-blocking observations (for implementation awareness):**

1. **Missing `reader.Skip()` for unmatched properties (pre-existing):** The Read path has no default `else { reader.Skip(); }` branch. If any future change introduces an unmatched JSON property name for a complex value, parsing will corrupt. This is a pre-existing issue, not introduced by this plan, and fixing it is out of scope. However, the developer should be aware of it.

2. **Reflection usage is consistent with existing patterns:** The plan uses `PropertyInfo.GetValue()`, `PropertyInfo.SetValue()`, and `GetProperties()` -- all of which are already used in the converter. Per CLAUDE.md rules, reflection should be avoided where possible, but in this case the existing converter is entirely reflection-based for property access, so this is consistent.

3. **Write-path property naming for `editProperties` local variable:** In the Write path (line 314), there is a local variable also named `editProperties` that shadows the Read path's `editProperties` variable. These are in completely separate scopes (Write vs Read method), so no confusion, but the developer should be aware of the naming reuse.

### Why This Plan Is Exceptionally Clear

This plan is unusually well-constructed for the following reasons:

1. **The bug is precisely diagnosed.** The root cause (two serialization categories in Write, neither covers LazyLoad) is verified against the actual code at specific line numbers.
2. **The fix is surgically scoped.** Only one file changes (`NeatooBaseJsonTypeConverter.cs`). No changes to `LazyLoad<T>`, `NeatooBaseJsonConverterFactory`, or any other framework class.
3. **The design leverages existing infrastructure.** By passing the same `JsonSerializerOptions` through `JsonSerializer.Serialize`/`Deserialize`, nested Neatoo entities inside LazyLoad.Value automatically get correct `$id`/`$ref` and PropertyManager handling without any special code.
4. **All 8 business rules trace cleanly through the implementation.** Every assertion maps to a specific code path with clear conditions.
5. **The architect provided compilable Design project evidence** for scope claims (EntityBase and ValidateBase with LazyLoad properties).
6. **Edge cases are explicitly addressed** (null property, unloaded state, nested entities, private setters, multiple LazyLoad properties, post-deserialization lifecycle).

### Review Summary

- Files examined: 10 source files, 2 test files, 1 Design project file, 2 documentation files
- Questions checked: 16 of 16
- Devil's advocate items: 7 generated (3 edge cases, 1 breakage scenario, 1 misunderstanding scenario, 2 pre-existing observations), 0 blocking

---

## Implementation Contract

**Created:** 2026-03-06
**Approved by:** neatoo-developer

### Design Project Acceptance Criteria

- [x] `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- LazyLoadEntityDemo and LazyLoadValidateDemo compile with LazyLoad properties (already verified by architect)
- [x] `dotnet build src/Design/Design.sln` -- Must pass after implementation (re-verified: 0 warnings, 0 errors)

### In Scope

**Phase 1: Converter Fix**
- [x] Modify `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` Write() -- Add LazyLoad property detection and serialization after the IEntityMetaProperties block (after line 322, before `writer.WriteEndObject()`)
- [x] Modify `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` Read() -- Add LazyLoad property detection in the `$type` block (alongside editProperties detection, after line 112) and add new dispatch branch before the editProperties branch (before line 172)
- [x] Checkpoint: `dotnet build src/Neatoo.sln` compiles

**Phase 2: Tests**
- [x] Create test entity class with LazyLoad property (EntityBase-based) in `src/Neatoo.UnitTest/Integration/Concepts/Serialization/`
- [x] Create test validate class with LazyLoad property (ValidateBase-based) in `src/Neatoo.UnitTest/Integration/Concepts/Serialization/`
- [x] Add FatClient serialization test: EntityBase with pre-loaded LazyLoad round-trip (Scenario 1+2)
- [x] Add FatClient serialization test: ValidateBase with pre-loaded LazyLoad round-trip (Scenario 3)
- [x] Add FatClient serialization test: Unloaded LazyLoad round-trip (Scenario 4)
- [x] Add FatClient serialization test: LazyLoad with nested Neatoo entity Value (Scenario 5)
- [x] Add two-container test: Remote call with LazyLoad property (Scenario 6)
- [x] Add test: Post-deserialization LoadAsync throws InvalidOperationException (Scenario 8)
- [x] Checkpoint: `dotnet test src/Neatoo.sln` -- all tests pass (including all existing serialization tests for Scenario 7 regression)

**Phase 3: Documentation**
- [x] Update `src/Design/CLAUDE-DESIGN.md` -- Add `LazyLoad<T> properties` row to serialization table
- [x] Update `skills/neatoo/references/lazy-loading.md` -- Add note about Neatoo converter support

### Explicitly Out of Scope

- Performance optimization (reflection caching) -- future work if profiling shows need
- Adding `reader.Skip()` for unmatched properties -- pre-existing issue, separate todo
- LazyLoad properties on EntityListBase/ValidateListBase -- not a supported pattern
- Re-attaching loader delegate after deserialization -- existing documented limitation
- Changes to `LazyLoad<T>` class itself
- Changes to `NeatooBaseJsonConverterFactory`
- Changes to RemoteFactory source generators

### Verification Gates

1. After Phase 1: `dotnet build src/Neatoo.sln` succeeds. Manual inspection confirms Write path emits LazyLoad properties and Read path restores them.
2. After Phase 2: `dotnet test src/Neatoo.sln` succeeds with ALL tests passing (new + existing). No existing test failures.
3. After Phase 3: Documentation updated. `dotnet build src/Design/Design.sln` succeeds.
4. Final: All tests pass, Design.sln builds, all contract items checked.

### Stop Conditions

If any of these occur, STOP and report:
- Any existing serialization test (FatClientEntityTests, TwoContainerMetaStateTests) starts failing
- Any out-of-scope test fails
- The `editProperties` mechanism unexpectedly captures LazyLoad properties (overlap between branches)
- `$id`/`$ref` reference tracking produces incorrect references with nested Neatoo entities in LazyLoad.Value
- Architectural contradiction discovered (e.g., LazyLoad<T> type is claimed by NeatooBaseJsonConverterFactory)

---

## Implementation Progress

**Started:** 2026-03-06
**Developer:** neatoo-developer
**Current Status:** Complete

### Phase 1: Converter Fix (Complete)

Modified `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs`:

**Write path (lines 339-352):** After the IEntityMetaProperties block, added a `foreach` loop that scans `value.GetType().GetProperties()` for properties where `PropertyType.IsGenericType && PropertyType.GetGenericTypeDefinition() == typeof(LazyLoad<>)`. For each match, gets the value via `PropertyInfo.GetValue(value)`, skips null, writes property name, and calls `JsonSerializer.Serialize(writer, propValue, property.PropertyType, options)`.

**Read path (lines 29, 115-121, 181-186):**
- Added `List<PropertyInfo> lazyLoadProperties = null` declaration alongside `editProperties`
- After the `editProperties` detection (do/while loop for EntityBase<>), added LazyLoad property detection on `result.GetType()` filtering for `LazyLoad<>` with `SetMethod != null`
- Added new `else if` dispatch branch BEFORE the `editProperties` branch that matches LazyLoad property names, deserializes via `JsonSerializer.Deserialize(ref reader, property.PropertyType, options)`, and sets via `property.SetValue(result, value)`

Checkpoint: `dotnet build src/Neatoo.sln` -- 0 errors.

### Phase 2: Tests (Complete)

Created 4 new files:
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadEntityObject.cs` -- EntityBase entity with `LazyLoad<string>` and `LazyLoad<ILazyLoadEntityObject>` properties, two [Fetch] methods
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadValidateObject.cs` -- ValidateBase entity with `LazyLoad<string>` property, one [Fetch] method
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs` -- 7 FatClient serialization tests
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/TwoContainerLazyLoadTests.cs` -- 2 two-container tests

9 new tests total, all passing:
1. `FatClientLazyLoad_EntityBase_PreLoaded_RoundTrip` (Rules 1+2)
2. `FatClientLazyLoad_EntityBase_Serialize_ContainsLazyLoadProperty` (Rule 1)
3. `FatClientLazyLoad_ValidateBase_PreLoaded_RoundTrip` (Rule 3)
4. `FatClientLazyLoad_Unloaded_RoundTrip` (Rule 4)
5. `FatClientLazyLoad_NestedNeatooEntity_RoundTrip` (Rule 5)
6. `FatClientLazyLoad_PostDeserialization_LoadAsync_Throws` (Rule 8)
7. `FatClientLazyLoad_PropertyManagerProperties_StillWork` (Rule 7 supplement)
8. `Fetch_TwoContainer_LazyLoad_PreservesValue` (Rule 6)
9. `Fetch_TwoContainer_LazyLoad_LoadAsync_Throws` (Rule 8)

Checkpoint: `dotnet test src/Neatoo.sln` -- 1753 total tests, 1752 passed, 1 skipped (pre-existing), 0 failed.

### Phase 3: Documentation (Complete)

- Updated `src/Design/CLAUDE-DESIGN.md` -- Added `LazyLoad<T> properties` row to serialization table
- Updated `skills/neatoo/references/lazy-loading.md` -- Expanded JSON serialization bullet with Neatoo converter details
- `dotnet build src/Design/Design.sln` -- 0 warnings, 0 errors

---

## Completion Evidence

**Completed:** 2026-03-06

### Test Results

```
Test Run Successful.
Total tests: 1753
     Passed: 1752
    Skipped: 1
 Total time: 6.0089 Seconds
```

All 9 new LazyLoad serialization tests pass. All 1744 existing tests continue to pass (no regressions). The 1 skipped test (`AsyncFlowTests_CheckAllRules`) is pre-existing and unrelated.

### Design Project Compilation

`dotnet build src/Design/Design.sln` -- PASS (0 warnings, 0 errors)

### All Contract Items Verified

All 15 contract items checked. All 4 verification gates passed:
1. Phase 1: `dotnet build src/Neatoo.sln` succeeded
2. Phase 2: `dotnet test src/Neatoo.sln` succeeded with ALL tests passing
3. Phase 3: Documentation updated, `dotnet build src/Design/Design.sln` succeeded
4. Final: All tests pass, Design.sln builds, all contract items checked

### No Stop Conditions Triggered

- No existing serialization tests failed
- No out-of-scope tests failed
- No overlap between LazyLoad and editProperties dispatch branches
- $id/$ref reference tracking works correctly with nested Neatoo entities in LazyLoad.Value (verified by NestedNeatooEntity test)
- No architectural contradictions discovered

---

## Documentation

### Expected Deliverables

- [x] `src/Design/CLAUDE-DESIGN.md` -- Add `LazyLoad<T> properties` row to serialization table
- [x] `skills/neatoo/references/lazy-loading.md` -- Add note about Neatoo converter support (currently only documents standalone STJ)
- [x] Skill updates: Yes
- [x] Sample updates: No

### Files Updated

- `src/Design/CLAUDE-DESIGN.md` -- Added LazyLoad row to serialization table at line 378
- `skills/neatoo/references/lazy-loading.md` -- Expanded JSON serialization bullet with Neatoo converter details
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- New Design project example demonstrating LazyLoad on both EntityBase and ValidateBase (fills Gap 1 from requirements review)

### Requirements Documentation Verification (Step 8 Part A)

**Verified by:** business-requirements-documenter
**Date:** 2026-03-06

**New rules added to requirements docs: 3** (Rules 2, 3, 6 -- all code-based, established through new tests and Design project examples during implementation)

- Rule 2 (EntityBase LazyLoad deserialization): Code-based requirement at `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs` -- fills Gap 2
- Rule 3 (ValidateBase LazyLoad round-trip): Code-based requirement at `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs` -- fills Gap 3
- Rule 6 (Two-container remote round-trip): Code-based requirement at `src/Neatoo.UnitTest/Integration/Concepts/Serialization/TwoContainerLazyLoadTests.cs` -- fills Gap 2

**Existing rules updated: 2**

- `skills/neatoo/references/lazy-loading.md` line 11: JSON serialization bullet expanded from "Value and IsLoaded are serialized; the loader delegate is not" to include Neatoo converter behavior, inner entity $id/$ref handling, and post-deserialization LoadAsync behavior. Verified accurate against implementation at `NeatooBaseJsonTypeConverter.cs` lines 339-352 (Write) and 115-186 (Read).
- `src/Design/CLAUDE-DESIGN.md` line 378: New row added to serialization table for `LazyLoad<T> properties`. Verified accurate: "Value and IsLoaded serialized; loader delegate is not. Inner Neatoo entities go through Neatoo converter for $id/$ref. Post-deserialization LoadAsync() throws if value was not pre-loaded."

**Outdated rules reconciled: 0**

No existing requirements were flagged as outdated. The pre-existing documentation accurately described the behavior at the time (LazyLoad properties were simply not mentioned in the converter documentation because the converter did not handle them).

**All documentation verified accurate against implementation:**

1. `skills/neatoo/references/lazy-loading.md` -- Serialization bullet matches converter Write path (line 349: `JsonSerializer.Serialize` with same `options`) and Read path (line 184: `JsonSerializer.Deserialize` with same `options`). Inner entity routing confirmed via `NeatooBaseJsonConverterFactory.CanConvert` claiming `IValidateBase` types.
2. `src/Design/CLAUDE-DESIGN.md` -- Serialization table row matches implementation behavior for all three serialized categories (PropertyManager, IEntityMetaProperties, and now LazyLoad).
3. `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- Design comments accurately describe: LazyLoad as regular property (not partial), generator non-processing, reflection-based detection, and inner entity routing. Both EntityBase and ValidateBase examples compile (verified by architect: Design.sln 0 warnings, 0 errors).

---

## Architect Verification

**Verified by:** neatoo-architect
**Date:** 2026-03-06
**Verdict:** VERIFIED

### Independent Build and Test Results

**Neatoo.sln build:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**Neatoo.sln tests:**
```
Passed!  - Failed:     0, Passed:    26, Skipped:     0, Total:    26 - Neatoo.BaseGenerator.Tests.dll (net9.0)
Passed!  - Failed:     0, Passed:   245, Skipped:     0, Total:   245 - Samples.dll (net9.0)
Passed!  - Failed:     0, Passed:    55, Skipped:     0, Total:    55 - Person.DomainModel.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:  1752, Skipped:     1, Total:  1753 - Neatoo.UnitTest.dll (net9.0)
```

Zero failures. The 1 skipped test (`AsyncFlowTests_CheckAllRules`) is pre-existing and unrelated to LazyLoad serialization.

**Design.sln build:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Implementation Design Match

Verified the implementation in `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` matches the plan's design:

**Write path (lines 339-352):** Correctly positioned after the IEntityMetaProperties block (lines 327-337) and before `writer.WriteEndObject()` (line 354). Scans `value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)` for `LazyLoad<>` generic type properties. Skips null values. Serializes non-null values with `JsonSerializer.Serialize(writer, propValue, property.PropertyType, options)`, passing the same `options` so nested `IValidateBase` entities route through the Neatoo converter. Matches plan exactly.

**Read path (lines 29, 115-121, 181-186):**
- Line 29: `lazyLoadProperties` declaration alongside `editProperties` -- matches plan.
- Lines 115-121: After the EntityBase-specific `editProperties` do/while loop, scans `result.GetType()` with `BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic` for `LazyLoad<>` properties with `SetMethod != null` -- matches plan. Uses concrete type, independent of base class hierarchy, so works for both EntityBase and ValidateBase.
- Lines 181-186: New `else if` dispatch branch positioned BEFORE the `editProperties` branch (lines 187-192). Matches LazyLoad property names via `lazyLoadProperties.Any(p => p.Name == propertyName)`, deserializes with `JsonSerializer.Deserialize(ref reader, property.PropertyType, options)`, and sets via `property.SetValue(result, value)` -- matches plan exactly.

**Both EntityBase and ValidateBase support:** Confirmed. LazyLoad detection at line 116 uses `result.GetType()` (concrete type), independent of the EntityBase-only `editProperties` mechanism at lines 103-113.

**Null LazyLoad handling:** Line 346 checks `if (propValue != null)` before writing -- correctly skips null LazyLoad properties.

**Nested Neatoo entities:** Same `options` parameter passed through to `JsonSerializer.Serialize`/`Deserialize`, so `NeatooBaseJsonConverterFactory` claims inner `IValidateBase` types for proper `$id`/`$ref` handling.

### Test Coverage Verification

All 8 business rules have test coverage across 9 new tests:

| Rule | Description | Test(s) | Verified |
|------|-------------|---------|----------|
| 1 | EntityBase LazyLoad Write | `FatClientLazyLoad_EntityBase_PreLoaded_RoundTrip`, `FatClientLazyLoad_EntityBase_Serialize_ContainsLazyLoadProperty` | Yes |
| 2 | EntityBase LazyLoad Read | `FatClientLazyLoad_EntityBase_PreLoaded_RoundTrip` | Yes |
| 3 | ValidateBase LazyLoad round-trip | `FatClientLazyLoad_ValidateBase_PreLoaded_RoundTrip` | Yes |
| 4 | Unloaded LazyLoad round-trip | `FatClientLazyLoad_Unloaded_RoundTrip` | Yes |
| 5 | Nested Neatoo entity in LazyLoad | `FatClientLazyLoad_NestedNeatooEntity_RoundTrip` | Yes |
| 6 | Two-container remote round-trip | `Fetch_TwoContainer_LazyLoad_PreservesValue` | Yes |
| 7 | No regression (no LazyLoad entities) | `FatClientLazyLoad_PropertyManagerProperties_StillWork` + all 1744 existing tests pass | Yes |
| 8 | Post-deserialization LoadAsync throws | `FatClientLazyLoad_PostDeserialization_LoadAsync_Throws`, `Fetch_TwoContainer_LazyLoad_LoadAsync_Throws` | Yes |

**Test entity quality:** `LazyLoadEntityObject` (EntityBase) includes both `LazyLoad<string>` and `LazyLoad<ILazyLoadEntityObject>` properties, two `[Fetch]` overloads, and uses proper `PauseAllActions()` and `LoadValue()` patterns. `LazyLoadValidateObject` (ValidateBase) includes a `LazyLoad<string>` property with a `[Fetch]` method. Both follow established Neatoo test entity conventions.

**Test infrastructure:** FatClient tests correctly extend `IntegrationTestBase` and use `Serialize()`/`Deserialize()` which go through `NeatooJsonSerializer` and `NeatooBaseJsonTypeConverter`. Two-container tests correctly extend `ClientServerTestBase` and use `GetClientService<ILazyLoadEntityObjectFactory>()` for the full client-server pipeline.

### Documentation Verification

- `src/Design/CLAUDE-DESIGN.md` line 378: LazyLoad row added to serialization table with accurate description.
- `skills/neatoo/references/lazy-loading.md` line 11: JSON serialization bullet expanded with Neatoo converter details, `$id`/`$ref` handling, and post-deserialization behavior.
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs`: Design project examples for both EntityBase and ValidateBase with comprehensive design comments.

### Design Project Verification

`dotnet build src/Design/Design.sln` passes with 0 warnings, 0 errors. `LazyLoadProperty.cs` contains properly structured demonstrations of LazyLoad on both EntityBase (`LazyLoadEntityDemo`) and ValidateBase (`LazyLoadValidateDemo`) with appropriate design decision comments.

---

## Requirements Verification

**Verified by:** business-requirements-reviewer
**Date:** 2026-03-06
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|
| LazyLoad standalone serialization contract (Value and IsLoaded survive STJ round-trip) | Satisfied | `LazyLoad.cs` lines 83-98: `[JsonInclude]` on `Value` and `IsLoaded` unchanged. Existing test `LazyLoadTests.Serialization_PreserveValueAndLoadedState` at `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs:361` still exists unmodified. `LazyLoad.cs` was not changed by this implementation -- all JSON attributes preserved. |
| LazyLoad JSON attribute design ([JsonInclude], [JsonConstructor] respected) | Satisfied | `LazyLoad.cs` line 52: `[JsonConstructor]` on parameterless constructor. Lines 83, 93: `[JsonInclude]` on `Value` and `IsLoaded`. Lines 24, 27, 34, 103, 109, 115: `[JsonIgnore]` on loader, lock, task, IsLoading, HasLoadError, LoadError. No changes to `LazyLoad.cs` -- all attributes intact. The converter at `NeatooBaseJsonTypeConverter.cs:349` delegates to `JsonSerializer.Serialize(writer, propValue, property.PropertyType, options)` which respects these attributes. |
| LazyLoad skill documentation intent (Value and IsLoaded serialized, loader is not) | Satisfied | `skills/neatoo/references/lazy-loading.md` line 11 updated to document both standalone STJ and Neatoo converter support. States: "Value and IsLoaded are serialized; the loader delegate is not." Implementation at `NeatooBaseJsonTypeConverter.cs:349` serializes via STJ default which respects `[JsonInclude]`/`[JsonIgnore]`, so Value and IsLoaded are included and the loader is excluded. |
| NeatooBaseJsonTypeConverter Write behavior (PropertyManager + IEntityMetaProperties + now LazyLoad) | Satisfied | `NeatooBaseJsonTypeConverter.cs` Write path: lines 303-325 (PropertyManager), lines 327-337 (IEntityMetaProperties), lines 339-352 (new LazyLoad block). LazyLoad block scans `value.GetType().GetProperties(BindingFlags.Instance \| BindingFlags.Public \| BindingFlags.NonPublic)` for `LazyLoad<>` types, skips null at line 346, serializes non-null via `JsonSerializer.Serialize` at line 349. Existing PropertyManager and meta property serialization code is untouched. |
| NeatooBaseJsonTypeConverter Read behavior (editProperties + now LazyLoad, for both EntityBase and ValidateBase) | Satisfied | `NeatooBaseJsonTypeConverter.cs` Read path: lines 115-121 detect `LazyLoad<>` properties on `result.GetType()` (concrete type, independent of base class). Lines 181-186 dispatch LazyLoad properties via `JsonSerializer.Deserialize(ref reader, property.PropertyType, options)` and `property.SetValue(result, value)`. This block runs BEFORE `editProperties` (lines 187-192). `editProperties` only contains `EntityBase<>` level properties (line 107), so no overlap. LazyLoad detection works for ValidateBase because it scans the concrete type directly. Test `FatClientLazyLoad_ValidateBase_PreLoaded_RoundTrip` at `FatClientLazyLoadTests.cs:73` confirms. |
| NeatooBaseJsonConverterFactory routing (LazyLoad NOT claimed by factory, inner IValidateBase IS claimed) | Satisfied | `NeatooJsonConverterFactory.cs` lines 18-36: `CanConvert` checks for `IValidateBase`, `ValidateBase<>`, `IValidateListBase`. `LazyLoad<T>` implements `INotifyPropertyChanged`, `IValidateMetaProperties`, `IEntityMetaProperties` only (confirmed at `LazyLoad.cs:22`), NOT `IValidateBase`. So the factory does not claim `LazyLoad<T>`. The converter passes the same `options` at line 349 (Write) and line 184 (Read), so when `LazyLoad<T>.Value` contains an `IValidateBase` entity, `NeatooBaseJsonConverterFactory.CanConvert` returns true for the inner type. Test `FatClientLazyLoad_NestedNeatooEntity_RoundTrip` at `FatClientLazyLoadTests.cs:126` exercises this path with `LazyLoad<ILazyLoadEntityObject>`. |
| Existing entity serialization tests (all still passing -- no regression) | Satisfied | Architect verification reports 1752 passed, 1 skipped (pre-existing), 0 failed across all test assemblies. `FatClientEntityTests.cs` and `EntityObject.cs` contain zero references to `LazyLoad` (confirmed via Grep). Write path: entities without LazyLoad properties produce zero matches in the `foreach` at line 340, so zero additional JSON is written. Read path: `lazyLoadProperties` list is empty for non-LazyLoad entities, so the dispatch branch at line 181 never matches. |

### Unintended Side Effects

None found. The implementation is strictly additive:

1. **Loading strategy**: No change. LazyLoad properties are detected at serialization time only. The scan adds a reflection iteration over the entity's properties on each Write and Read call, but this is consistent with existing reflection patterns in the converter (e.g., `editProperties` scan at line 107, `IEntityMetaProperties.GetProperties()` at line 329).

2. **Validation timing**: No change. LazyLoad properties are not PropertyManager properties and do not participate in the validation rule system. The serialization fix does not alter when validation runs.

3. **Default values**: No change. LazyLoad properties initialized to `null!` before factory methods run are skipped by the Write path's null check at line 346.

4. **Ownership and lifecycle**: No change. The `$id`/`$ref` reference tracking for nested Neatoo entities inside `LazyLoad.Value` uses the same `JsonSerializerOptions` and `ReferenceHandler` as all other entity serialization, so no duplicate IDs or broken references are introduced.

5. **editProperties overlap**: No overlap. The `editProperties` list (line 107) contains properties from the `EntityBase<>` type level only. LazyLoad properties are declared on concrete subclasses, not on `EntityBase<>`. The LazyLoad dispatch branch (line 181) runs before editProperties (line 187), but in practice no property name appears in both lists.

### Issues Found

None.
