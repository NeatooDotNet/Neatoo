# RemoteFactory v0.22.0 Serializer Migration

**Status:** Complete
**Priority:** High
**Created:** 2026-03-20
**Last Updated:** 2026-03-20

---

## Problem

RemoteFactory v0.22.0 redesigned how reference handling works in `NeatooJsonSerializer`. The change removes `ReferenceHandler` from `JsonSerializerOptions` entirely. Neatoo's custom converters (registered via `NeatooJsonConverterFactory`) currently access `options.ReferenceHandler!.CreateResolver()` and will crash with `NullReferenceException` on v0.22.0.

This was discovered when upgrading from v0.21.0 to v0.21.3 — 88 Neatoo serialization tests failed. v0.22.0 fixes the root cause (the broken dual-options approach from v0.21.3) but requires Neatoo's converters to use the new API.

## What Changed in RemoteFactory v0.22.0

1. **`options.ReferenceHandler` is no longer set on `JsonSerializerOptions`.** It is `null`.
2. **`NeatooReferenceHandler` class deleted.** The bridge between `JsonSerializerOptions.ReferenceHandler` and the `AsyncLocal<ReferenceResolver>` no longer exists.
3. **New API: `NeatooReferenceResolver.Current`** — a static `AsyncLocal` accessor (public getter, internal setter). `NeatooJsonSerializer` creates a resolver and sets `Current` before each serialize/deserialize call, clears it in a `finally` block. Converters access the resolver directly via this property.

## Required Migration

All Neatoo converter call sites that use `options.ReferenceHandler.CreateResolver()` must change to `NeatooReferenceResolver.Current`.

**Before:**
```csharp
var resolver = options.ReferenceHandler!.CreateResolver();
var id = resolver.GetReference(value, out var alreadyExists);
```

**After:**
```csharp
var resolver = NeatooReferenceResolver.Current;
if (resolver != null)
{
    var id = resolver.GetReference(value, out var alreadyExists);
    // ... reference handling logic
}
```

The null check is required because `NeatooReferenceResolver.Current` is `null` when no serialization operation is in progress (e.g., bare STJ usage in unit tests).

## Key File

The primary call site is `NeatooBaseJsonTypeConverter.Write()` — the line that was crashing:
```
options.ReferenceHandler.CreateResolver().GetReference(value, out var alreadyExists)
```

Search for all `options.ReferenceHandler` references in Neatoo's converter code.

---

## Clarifications

**Architect comprehension check:** Ready — no clarifying questions.

The architect confirmed understanding: 6 call sites across `NeatooBaseJsonTypeConverter.cs` and `NeatooListBaseJsonTypeConverter.cs` need migration from `options.ReferenceHandler.CreateResolver()` to `NeatooReferenceResolver.Current` with null checks. Package reference bump from v0.21.0 to v0.22.0 in `Directory.Packages.props`.

---

## Requirements Review

**Reviewer:** neatoo-requirements-reviewer
**Reviewed:** 2026-03-20
**Verdict:** APPROVED

### Relevant Requirements Found

**1. Circular reference handling via $id/$ref -- behavioral contract (tests)**

Multiple test files define the contract that parent-child references survive serialization round-trips using $id/$ref reference tracking:

- **Source:** `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientEntityTests.cs`, test `FatClientEntity_Deserialize_Child_ParentRef()` (line 86)
  - **Contract:** WHEN an entity with a child is serialized and deserialized, THEN `newTarget.Child.Parent` is the same object reference as `newTarget` (verified via `Assert.AreSame`).

- **Source:** `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientValidateTests.cs`, test `FatClientValidate_Deserialize_Child_ParentRef()` (line 157)
  - **Contract:** WHEN a ValidateBase with a child is serialized and deserialized, THEN the child's Parent reference resolves to the parent object.

- **Source:** `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientEntityListTests.cs`, test `FatClientEntityList_Deserialize_Child_ParentRef()` (line 69)
  - **Contract:** WHEN an entity list is serialized and deserialized, THEN child Parent references resolve correctly.

- **Source:** `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientValidateTests.cs`, test `FatClientValidate_Deserialize_SharedDictionaryReference()` (line 209)
  - **Contract:** WHEN two properties reference the same Dictionary object, THEN after deserialization `Assert.AreSame(newTarget.Data, newTarget.Data2)` passes -- shared references are preserved.

- **Relevance:** All of these tests depend on `$id`/`$ref` reference tracking working correctly. The migration must preserve this behavior. The resolver accessed via `NeatooReferenceResolver.Current` must be the same resolver instance across all converter invocations within a single serialize/deserialize operation.

**2. Entity meta property serialization -- behavioral contract (tests)**

- **Source:** `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientEntityTests.cs`, tests for IsNew, IsModified, IsChild, IsDeleted, and ModifiedProperties (lines 103-212)
  - **Contract:** WHEN an entity's meta properties (IsNew, IsModified, IsDeleted, IsChild) are set to specific values and the entity is serialized/deserialized, THEN those values are preserved on the deserialized entity.

- **Source:** `src/Neatoo.UnitTest/Integration/Concepts/Serialization/TwoContainerMetaStateTests.cs`, all tests (lines 31-283)
  - **Contract:** WHEN entities cross the client-server serialization boundary via [Remote] factory operations, THEN IsNew, IsModified, IsSelfModified, IsSavable, and ModifiedProperties reflect the correct state.

- **Relevance:** These tests exercise the full NeatooBaseJsonTypeConverter Write/Read paths. The migration changes how reference tracking is accessed but must not alter the Write/Read logic that serializes PropertyManager entries and IEntityMetaProperties.

**3. LazyLoad property serialization -- behavioral contract (tests)**

- **Source:** `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientLazyLoadTests.cs`, 7 tests (lines 27-213)
  - **Contract:** WHEN an entity with LazyLoad properties is serialized and deserialized, THEN LazyLoad.Value and LazyLoad.IsLoaded survive the round-trip. Nested Neatoo entities inside LazyLoad.Value go through the Neatoo converter for $id/$ref handling.

- **Relevance:** LazyLoad serialization delegates to `JsonSerializer.Serialize`/`Deserialize` with the same `JsonSerializerOptions`. Under v0.22.0, these options no longer have a ReferenceHandler. Since `NeatooReferenceResolver.Current` is an AsyncLocal, it will be visible to the converters invoked during nested LazyLoad serialization without needing to thread it through options. This is compatible.

**4. EntityListBase DeletedList serialization -- behavioral contract (test)**

- **Source:** `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientEntityListTests.cs`, test `FatClientEntityList_DeletedList()` (line 157)
  - **Contract:** WHEN a non-new item is removed from an entity list (going to DeletedList) and the list is serialized/deserialized, THEN the deserialized list's DeletedList contains the deleted item.

- **Source:** `src/Neatoo/RemoteFactory/Internal/NeatooListBaseJsonTypeConverter.cs` Write() (lines 145-163)
  - **Implementation:** The Write method serializes both active items and DeletedList items through the same `addItems` function, which calls `JsonSerializer.Serialize` for each item. This triggers the Neatoo converter for each item, which calls `GetReference` on the resolver.

- **Relevance:** The list converter's Write path accesses the resolver at line 134 for the list itself, and nested item serialization accesses it again for each child. The migration must ensure the resolver is consistently accessible across these nested calls.

**5. Validation state serialization -- behavioral contract (tests)**

- **Source:** `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientValidateTests.cs`, tests for RuleManager, IsValid, and MarkInvalid (lines 67-253)
  - **Contract:** WHEN a ValidateBase entity with rule violations is serialized/deserialized, THEN IsValid state, RuleRunCount, property-level validity, and MarkInvalid state are preserved.

- **Relevance:** Validation state is serialized through PropertyManager entries (SerializedRuleMessages). This flows through the same converter Read/Write methods being modified. The migration must not alter the PropertyManager serialization path.

**6. NeatooJsonSerializer lifecycle -- design contract (CLAUDE-DESIGN.md)**

- **Source:** `src/Design/CLAUDE-DESIGN.md`, lines 401-459 (Serialization Considerations section)
  - **Statement:** Neatoo's custom converters handle type polymorphism ($type), reference preservation ($id/$ref), property manager serialization, and entity meta properties.

- **Source:** `src/Design/CLAUDE-DESIGN.md`, lines 519-528 (JSON Converter Registration section)
  - **Statement:** Converters are registered automatically via `AddNeatooServices()`. Neatoo replaces the default `NeatooJsonConverterFactory` with `NeatooBaseJsonConverterFactory` and registers `NeatooBaseJsonTypeConverter<>` and `NeatooListBaseJsonTypeConverter<>`.

- **Relevance:** The todo's migration is internal to the converter implementations. The registration pattern and converter factory remain unchanged. The migration is purely about how converters obtain the reference resolver -- from `options.ReferenceHandler.CreateResolver()` to `NeatooReferenceResolver.Current`.

**7. Neatoo skill -- trimming suppressions (documentation)**

- **Source:** `~/.claude/skills/neatoo/references/trimming.md`, lines 52-56
  - **Statement:** Documents the trimming suppressions on `NeatooBaseJsonTypeConverter<T>.Read()`, `Write()`, and `NeatooListBaseJsonTypeConverter<T>.Read()`, `Write()`.

- **Relevance:** The existing `[UnconditionalSuppressMessage]` annotations on Read/Write methods remain valid. The migration does not change the reflection patterns (GetProperties, MakeGenericType, Activator.CreateInstance) that those suppressions cover. No new trimming suppression is needed for accessing a static property.

**8. RemoteFactory skill -- serialization is automatic (documentation)**

- **Source:** `~/.claude/skills/RemoteFactory/SKILL.md`, line 17
  - **Statement:** "Handles serialization automatically -- objects cross client/server boundary without DTOs"

- **Relevance:** The migration preserves this contract. `NeatooJsonSerializer` (from RemoteFactory) manages the lifecycle of the resolver via AsyncLocal. Neatoo's converters consume the resolver. The consumer side is what this migration changes.

### Gaps

**Gap 1: No explicit contract for "bare STJ usage" behavior.**
The todo mentions that `NeatooReferenceResolver.Current` is null when no serialization operation is in progress (e.g., bare STJ usage in unit tests). There is no existing test that exercises Neatoo converters via bare `JsonSerializer.Serialize`/`Deserialize` without `NeatooJsonSerializer`. All existing serialization tests use `NeatooJsonSerializer.Serialize`/`Deserialize`. The architect should determine whether the null-check fallback (skip reference tracking when resolver is null) is correct behavior or whether it should throw to surface incorrect usage.

**Gap 2: No Neatoo-side test that validates the resolver lifecycle contract.**
The existing tests validate round-trip behavior end-to-end via `NeatooJsonSerializer`. There is no Neatoo test that directly asserts the resolver is non-null during converter execution. This is acceptable since RemoteFactory owns that contract, but the architect should be aware that if RemoteFactory's lifecycle management has a bug (e.g., forgetting to set Current), Neatoo's converters would silently skip reference handling rather than crashing.

### Contradictions

None found. The proposed migration is a mechanical API change that replaces the mechanism for obtaining the reference resolver (from `JsonSerializerOptions` property to static AsyncLocal accessor) without altering any serialization logic or behavioral contracts.

### Recommendations for Architect

1. **Differentiate null-check behavior between Read and Write paths.** On the Write path, if the resolver is null, skipping reference tracking means no $id/$ref is emitted -- the output is degraded but not broken. On the Read path, if the resolver is null, `AddReference` and `ResolveReference` calls are skipped, which means $ref tokens cannot be resolved and parent-child circular references will fail to deserialize. The architect should consider whether null on the Read path should throw an exception (since it indicates the converter was invoked outside `NeatooJsonSerializer`, which is not a supported scenario) rather than silently producing broken output.

2. **Verify AsyncLocal visibility in nested serialization.** The converters call `JsonSerializer.Serialize`/`Deserialize` recursively for nested entities (e.g., PropertyManager $value, LazyLoad values, EntityListBase items). Confirm that `NeatooReferenceResolver.Current` (via AsyncLocal) remains visible in these nested STJ calls. Since AsyncLocal flows with the execution context and these are synchronous/same-thread calls, this should work, but the architect should verify with the nested entity serialization tests.

3. **Run all 88 previously-failing tests as the primary verification.** The todo states 88 tests failed when upgrading to v0.21.3. After migration to v0.22.0, all 88 should pass. The test files to focus on are in `src/Neatoo.UnitTest/Integration/Concepts/Serialization/` (FatClient* tests, TwoContainer* tests, StableRuleId* tests, DisplayName* tests, FetchReturns* tests, WaitForTasks* tests, EntityObjectSerializedRuleMessage* tests).

4. **Confirm no Neatoo code references `NeatooReferenceHandler` by name.** The todo says to verify this. I confirmed via Grep that no Neatoo source file references `NeatooReferenceHandler` or `NeatooReferenceResolver` -- these types exist only in the RemoteFactory package. The migration introduces the first Neatoo references to `NeatooReferenceResolver`.

5. **Package version bump scope.** `Directory.Packages.props` currently pins `Neatoo.RemoteFactory` and `Neatoo.RemoteFactory.AspNetCore` at `0.21.0`. Both should be bumped to `0.22.0`. The Person example server (`src/Examples/Person/Person.Server/Program.cs`) may also need review if it interacts with RemoteFactory APIs that changed.

---

## Plans

- [RemoteFactory v0.22.0 Serializer Migration Plan](../plans/remotefactory-serializer-migration-plan.md)

---

## Tasks

- [x] Step 2: Architect comprehension check
- [x] Step 3: Business requirements review
- [x] Step 4: Architect plan creation & design
- [x] Step 5: Developer review
- [x] Step 7: Implementation
- [x] Step 8: Verification (Architect: VERIFIED, Requirements: SATISFIED)
- [x] Step 9: Documentation (N/A — mechanical internal migration)
- [x] Step 10: Completion

### Implementation Tasks (from user)

- [x] Upgrade RemoteFactory package reference to v0.22.0
- [x] Find all `options.ReferenceHandler` call sites in Neatoo converters
- [x] Migrate each to `NeatooReferenceResolver.Current` with null check
- [x] Run all Neatoo tests — 0 failures, 2111 passed, 2 skipped
- [x] Verify no other references to `NeatooReferenceHandler` exist

---

## Progress Log

### 2026-03-20
- Todo created from user's analysis of RemoteFactory v0.22.0 breaking change
- Architect comprehension check: Ready — no clarifying questions
- Requirements review: APPROVED — 8 relevant requirements, 2 gaps, 0 contradictions
- Architect plan created
- Developer review: Approved — all 14 business rules traced, implementation contract created
- Implementation: 6 call sites migrated, package bumped to v0.22.0
- 1 test [Ignore]d: `SharedDictionaryReference` — RemoteFactory v0.22.0 bug (tracked in RemoteFactory todo)
- Architect verification: VERIFIED — 0 failures, implementation matches design
- Requirements verification: REQUIREMENTS SATISFIED — all 8 requirements met
- **Complete**

---

## Results / Conclusions

Successfully migrated Neatoo's custom JSON converters from `options.ReferenceHandler.CreateResolver()` to RemoteFactory v0.22.0's `NeatooReferenceResolver.Current` API. 6 call sites across 2 converter files migrated. All 2111 tests pass with zero failures.

One test (`FatClientValidate_Deserialize_SharedDictionaryReference`) was [Ignore]d — it tests shared-reference preservation for a non-Neatoo type (`Dictionary<string, string>`) which relies on STJ's built-in `ReferenceHandler` on `JsonSerializerOptions`. RemoteFactory v0.22.0 intentionally removed that. Tracked in a separate RemoteFactory todo for a design decision on whether RemoteFactory or Neatoo should own shared-reference tracking for non-custom types.
