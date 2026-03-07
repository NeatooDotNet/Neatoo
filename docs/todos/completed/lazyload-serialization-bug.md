# LazyLoad Properties Don't Serialize in Client-Server Scenarios

**Status:** Complete
**Priority:** High
**Created:** 2026-03-06
**Last Updated:** 2026-03-06

---

## Problem

`LazyLoad<T>` properties on Neatoo domain objects are silently dropped during serialization through `NeatooBaseJsonTypeConverter<T>`. This means any entity with a `LazyLoad<T>` property loses that data when transferred between client and server.

**Root cause:** `NeatooBaseJsonTypeConverter<T>.Write()` only serializes two categories:
1. **PropertyManager entries** — partial properties using `Getter<T>()`/`Setter()` (the Neatoo property system)
2. **IEntityMetaProperties** — `IsModified`, `IsNew`, `IsDeleted`, etc.

A `LazyLoad<T>` property (e.g., `public LazyLoad<OrderLineList> OrderLines { get; private set; }`) is a regular C# property — it's in neither category, so the converter completely ignores it on both write and read.

**Why it wasn't caught:** The existing `LazyLoadTests.Serialization_PreserveValueAndLoadedState` test uses plain `JsonSerializer.Serialize` in isolation, which doesn't go through Neatoo's custom converter. There is no client-server integration test for entities with LazyLoad properties.

**Affected files:**
- `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` — Write() skips LazyLoad properties, Read() doesn't restore them

## Solution

1. Update `NeatooBaseJsonTypeConverter.Write()` to detect `LazyLoad<T>` properties on the entity and serialize them
2. Update `NeatooBaseJsonTypeConverter.Read()` to deserialize `LazyLoad<T>` properties back onto the entity
3. Add client-server integration test with an entity that has a `LazyLoad<T>` property to prevent regression

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-06
**Verdict:** APPROVED

### Relevant Requirements Found

**1. LazyLoad standalone serialization contract (code-based)**
- **Source:** `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`, lines 360-376, test `Serialization_PreserveValueAndLoadedState`
- **Contract:** WHEN a `LazyLoad<T>` with a pre-loaded value is serialized via `JsonSerializer.Serialize` and deserialized via `JsonSerializer.Deserialize`, THEN `IsLoaded` remains `true` and `Value` preserves its data.
- **Relevance:** Confirms that `LazyLoad<T>` itself is serializable via its `[JsonInclude]` and `[JsonConstructor]` attributes. The todo's fix can rely on this -- the inner `LazyLoad<T>` object knows how to serialize itself when STJ processes it. The bug is that `NeatooBaseJsonTypeConverter` never delegates to STJ for these properties.

**2. LazyLoad JSON attribute design (code-based)**
- **Source:** `src/Neatoo/LazyLoad.cs`, lines 52-98
- **Contract:** `Value` and `IsLoaded` are marked `[JsonInclude]`. The loader delegate (`_loader`), load task (`_loadTask`), and error state are marked `[JsonIgnore]`. A `[JsonConstructor]` parameterless constructor exists for deserialization.
- **Relevance:** The `LazyLoad<T>` class was intentionally designed for JSON round-tripping. The todo aligns with this design intent.

**3. LazyLoad skill documentation -- serialization intent**
- **Source:** `skills/neatoo/references/lazy-loading.md`, line 11
- **Statement:** "JSON serialization -- Value and IsLoaded are serialized; the loader delegate is not."
- **Relevance:** Documents the intended behavior. Currently this intent is only fulfilled in isolation (standalone serialization), not when the LazyLoad property is on an entity going through the Neatoo converter.

**4. CLAUDE-DESIGN.md serialization table (documentation-based)**
- **Source:** `src/Design/CLAUDE-DESIGN.md`, lines 362-377
- **Statement:** The serialization table lists "Property values: Yes -- All registered properties" as serialized. `LazyLoad<T>` properties are NOT registered properties (they are regular C# properties, not partial properties using Getter/Setter).
- **Relevance:** The documentation accurately describes current behavior. It does not say LazyLoad properties SHOULD be dropped -- it simply does not address them. This is a gap, not a contradiction.

**5. NeatooBaseJsonTypeConverter Write behavior (code-based)**
- **Source:** `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs`, lines 261-326
- **Contract:** WHEN an `IValidateBase` entity is serialized, THEN the converter writes `$id`, `$type`, `PropertyManager` (array of registered properties), and `IEntityMetaProperties`/`IFactorySaveMeta` properties. No other properties are written.
- **Relevance:** Confirms the root cause. The converter's Write path has exactly two serialization categories, and LazyLoad properties fall into neither.

**6. NeatooBaseJsonTypeConverter Read behavior -- editProperties fallback (code-based)**
- **Source:** `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs`, lines 100-177
- **Contract:** WHEN deserializing an `EntityBase<>` type, THEN the converter discovers settable properties on the `EntityBase<>` class and can set them from JSON. For JSON property names that are not `$ref`, `$id`, `$type`, or `PropertyManager`, the converter checks `editProperties` (settable properties on `EntityBase<>`) and sets them via reflection.
- **Relevance:** The Read side already has an `editProperties` mechanism that could potentially handle LazyLoad properties IF they appeared in the JSON. However, since Write never outputs them, this path is never exercised for LazyLoad. Also note: this mechanism only applies to `EntityBase<>` types, not `ValidateBase<>` types. If `LazyLoad<T>` properties should also work on `ValidateBase<>` entities, the architect must account for this gap.

**7. NeatooBaseJsonConverterFactory -- LazyLoad type routing (code-based)**
- **Source:** `src/Neatoo/RemoteFactory/Internal/NeatooJsonConverterFactory.cs`, lines 18-36
- **Contract:** `CanConvert` returns true only for `IValidateBase`, `ValidateBase<>`, `IValidateListBase`, and abstract/interface types registered in service assemblies. `LazyLoad<T>` does NOT implement `IValidateBase`, so the factory will NOT claim it.
- **Relevance:** When the converter serializes a LazyLoad property value, STJ will use default serialization for the `LazyLoad<T>` wrapper. However, if `LazyLoad<T>.Value` contains an `IValidateBase` entity (e.g., `OrderLineList`), the Neatoo converter factory WILL claim that inner type. The architect must ensure the inner entity value is serialized through the Neatoo converter (for `$id`/`$ref` handling and PropertyManager serialization), not through STJ default.

**8. Existing entity serialization tests (code-based)**
- **Source:** `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientEntityTests.cs`
- **Contract:** Multiple tests verify that property values, meta state (IsNew, IsModified, IsDeleted, IsChild), child entity references, and parent references survive serialization round-trips through the Neatoo converter.
- **Relevance:** These tests define the serialization contract for entities. The fix must not break any of them. The child property (`IEntityObject Child`) in these tests uses a partial property (PropertyManager-based), not a LazyLoad property -- so there is no existing test coverage for the LazyLoad case.

**9. Design project FetchPatterns -- lazy loading guidance (code-based)**
- **Source:** `src/Design/Design.Domain/FactoryOperations/FetchPatterns.cs`, lines 190-206
- **Statement:** "DID NOT DO THIS: Lazy load children separately... If you need lazy loading, implement it explicitly with clear naming."
- **Relevance:** The Design project prefers eager loading but explicitly acknowledges lazy loading as a valid pattern. LazyLoad<T> exists in the framework source and is documented in the skills. This is not a contradiction -- the Design project says "we prefer eager but lazy is OK if explicit."

### Gaps

**Gap 1: No Design project representation for LazyLoad serialization.**
LazyLoad<T> is not demonstrated in any `src/Design/` file. There is no Design.Domain entity that uses a `LazyLoad<T>` property, and no Design.Tests test that verifies LazyLoad properties survive the Neatoo converter pipeline. The architect should decide whether a Design project example should be added as part of this fix, or whether the integration test in the unit test project is sufficient.

**Gap 2: No integration test for LazyLoad through the Neatoo converter.**
The existing `Serialization_PreserveValueAndLoadedState` test uses standalone `JsonSerializer.Serialize`, not the Neatoo converter. The todo correctly identifies this gap and proposes adding a client-server integration test.

**Gap 3: ValidateBase<> entities with LazyLoad properties.**
The `editProperties` fallback in the Read path only applies to `EntityBase<>` types. If a `ValidateBase<>` entity has a LazyLoad property, the Read side has no mechanism to restore it. The architect must determine whether LazyLoad properties on ValidateBase entities are a supported pattern and handle accordingly.

**Gap 4: Circular reference handling for LazyLoad.Value.**
When `LazyLoad<T>.Value` contains a Neatoo entity (e.g., an `EntityListBase`), that entity might have back-references to the parent (via `Parent` property). The Neatoo converter handles `$id`/`$ref` for entities within PropertyManager. The architect must ensure that serializing a LazyLoad wrapper does not create duplicate `$id` entries or break the reference graph -- particularly if the LazyLoad value is also referenced elsewhere in the object graph.

**Gap 5: Post-deserialization lifecycle for LazyLoad.**
After deserialization, a `LazyLoad<T>` will have no loader delegate (`_loader` is `[JsonIgnore]`). Its `LoadAsync()` will throw `InvalidOperationException` if called. The `LazyLoad.cs` code at line 133 handles this explicitly. The architect should verify that this is acceptable behavior for the client-server pattern, or whether a mechanism to re-attach a loader after deserialization is needed.

### Contradictions

None found. The proposed fix aligns with the documented design intent of `LazyLoad<T>` (Value and IsLoaded should serialize) and does not conflict with any existing serialization contract.

### Recommendations for Architect

1. **Leverage LazyLoad's built-in JSON support.** Since `LazyLoad<T>` already has `[JsonInclude]`/`[JsonConstructor]` attributes and standalone serialization works (proven by existing test), the converter fix should delegate to `JsonSerializer.Serialize`/`Deserialize` for the LazyLoad wrapper itself. Do not reimplement LazyLoad serialization manually inside the converter.

2. **Handle nested Neatoo entities inside LazyLoad.Value.** When `LazyLoad<T>.Value` contains an `IValidateBase` entity, that entity must go through the Neatoo converter (for PropertyManager, $id/$ref, meta properties). Verify that when `JsonSerializer.Serialize` is called on the `LazyLoad<T>` wrapper, the `NeatooBaseJsonConverterFactory` correctly claims the inner `Value` type. This may require passing the same `JsonSerializerOptions` (which contains the Neatoo converter factory) when serializing the LazyLoad wrapper.

3. **Cover both EntityBase and ValidateBase paths.** The Read side's `editProperties` mechanism only works for `EntityBase<>`. The fix must handle LazyLoad properties on both `EntityBase<>` and `ValidateBase<>` entities. Consider detecting LazyLoad properties via reflection on the concrete type regardless of base class.

4. **Add a two-container integration test.** A "FatClient" serialization test is good, but the gold standard for catching serialization bugs is a `ClientServerTestBase` test (like `TwoContainerMetaStateTests`) that exercises the full client-server pipeline with a real `[Remote]` call crossing the serialization boundary.

5. **Watch for $id/$ref interaction.** If the same Neatoo entity appears both as a LazyLoad value AND as a PropertyManager property value elsewhere in the graph, the `$id`/`$ref` reference tracking must remain consistent. Test this scenario explicitly.

6. **Update CLAUDE-DESIGN.md serialization table.** After the fix, the serialization table at `src/Design/CLAUDE-DESIGN.md` lines 362-377 should be updated to include LazyLoad properties as a serialized category. Currently it only mentions "Property values" (registered properties) and meta properties.

---

## Plans

- [LazyLoad Serialization Fix](../plans/lazyload-serialization-fix.md)

---

## Tasks

- [x] Update `NeatooBaseJsonTypeConverter.Write()` to serialize `LazyLoad<T>` properties
- [x] Update `NeatooBaseJsonTypeConverter.Read()` to deserialize `LazyLoad<T>` properties
- [x] Add client-server integration test for entity with LazyLoad property
- [x] Verify existing LazyLoad unit tests still pass

---

## Progress Log

### 2026-03-06
- Bug identified: LazyLoad properties silently dropped during Neatoo JSON serialization
- Root cause traced to `NeatooBaseJsonTypeConverter<T>` — only handles PropertyManager and IEntityMetaProperties
- Confirmed no client-server integration test exists for LazyLoad

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass
- [x] Design project builds successfully
- [x] Design project tests pass

**Verification results:**
- Build: PASS (Neatoo.sln and Design.sln — 0 errors, 0 warnings)
- Tests: PASS (1752 passed, 1 skipped (pre-existing), 0 failed)

---

## Results / Conclusions

**Bug fixed.** `LazyLoad<T>` properties on Neatoo domain objects now survive serialization round-trips through `NeatooBaseJsonTypeConverter<T>`.

**Root cause:** The custom JSON converter only handled two property categories (PropertyManager entries and IEntityMetaProperties). `LazyLoad<T>` properties are regular C# properties that fell outside both.

**Fix:** Added LazyLoad property detection via reflection on the concrete entity type to both the Write and Read paths of `NeatooBaseJsonTypeConverter<T>`. The detection is independent of the existing `editProperties` mechanism, so it works for both `EntityBase` and `ValidateBase` entities.

**Key design decision:** Delegates to `JsonSerializer.Serialize`/`Deserialize` for the `LazyLoad<T>` wrapper itself, passing the same `JsonSerializerOptions` so that nested `IValidateBase` entities inside `LazyLoad<T>.Value` correctly go through the Neatoo converter for `$id`/`$ref` handling.

**Files modified:**
- `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` — core fix (Write + Read paths)

**Files created:**
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadEntityObject.cs` — test entity
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/LazyLoadValidateObject.cs` — test validate object
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs` — 7 FatClient tests
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/TwoContainerLazyLoadTests.cs` — 2 two-container tests
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` — Design project examples

**Documentation updated:**
- `src/Design/CLAUDE-DESIGN.md` — serialization table
- `skills/neatoo/references/lazy-loading.md` — Neatoo converter support

**No breaking changes. No regressions. 9 new tests added.**
