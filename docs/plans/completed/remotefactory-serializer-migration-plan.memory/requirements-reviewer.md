# Requirements Reviewer -- RemoteFactory v0.22.0 Serializer Migration Plan

Last updated: 2026-03-20
Current step: Step 8 Part B -- Post-implementation requirements verification

## Verdict: REQUIREMENTS SATISFIED

The implementation respects all 8 requirements identified in the pre-design review. One behavioral contract (shared Dictionary reference preservation) is now broken due to a RemoteFactory v0.22.0 upstream change, but this is correctly attributed to RemoteFactory -- not to the Neatoo migration -- and the corresponding test is [Ignore]d with a tracking reference rather than deleted or gutted.

---

## Requirements Compliance

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| 1 | Circular reference handling via $id/$ref (parent-child identity) | Satisfied | `NeatooBaseJsonTypeConverter.Write()` lines 330-348: `NeatooReferenceResolver.Current` -> `GetReference()` -> emits `$id` on first encounter, `$ref` on subsequent. `Read()` line 58: `AddReference()` on EndObject. Line 79-81: `ResolveReference()` on `$ref`. Tests `FatClientEntity_Deserialize_Child_ParentRef` (line 86), `FatClientValidate_Deserialize_Child_ParentRef` (line 157), `FatClientEntityList_Deserialize_Child_ParentRef` (line 70) all PASS -- `Assert.AreSame` for parent identity confirmed. |
| 2 | Entity meta property serialization (IsNew, IsModified, IsDeleted, IsChild, ModifiedProperties) | Satisfied | Meta property serialization path in `NeatooBaseJsonTypeConverter.Write()` lines 389-401 is unchanged -- iterates `IEntityMetaProperties` and `IFactorySaveMeta` properties via reflection. Not touched by the migration. Tests `FatClientEntity_IsModified` (line 104), `FatClientEntity_IsNew` (line 129), `FatClientEntity_IsChild` (line 156), `FatClientEntity_IsDeleted` (line 198), `FatClientEntity_ModifiedProperties` (line 182) all PASS. TwoContainerMetaStateTests (12 tests, lines 31-283) all PASS. |
| 3 | LazyLoad property serialization (Value and IsLoaded survive round-trip) | Satisfied | LazyLoad serialization in `Write()` lines 404-417 and `Read()` lines 198-215 are unchanged by the migration. Nested entity serialization via `JsonSerializer.Serialize`/`Deserialize` with same options continues to work because `NeatooReferenceResolver.Current` (AsyncLocal) is visible on the same execution context. All 7 FatClientLazyLoad tests PASS. |
| 4 | EntityListBase DeletedList serialization | Satisfied | `NeatooListBaseJsonTypeConverter.Write()` lines 150-168: `addItems()` serializes both active items and DeletedList items. Reference tracking at line 136-142 uses `NeatooReferenceResolver.Current` correctly. `Read()` path delegates item deserialization to `JsonSerializer.Deserialize` at line 106, which invokes the base converter for each item. Test `FatClientEntityList_DeletedList` (line 158) PASSES -- deleted item appears in deserialized list's DeletedList. |
| 5 | Validation state serialization (IsValid, RuleRunCount, MarkInvalid) | Satisfied | Validation state is serialized through PropertyManager's `SerializedRuleMessages` field in the `DeserializeValidateProperty` method (lines 241-311). This code path is unchanged by the migration. Tests `FatClientValidate_Deserialize_RuleManager` (line 68), `FatClientValidate_Deserialize_Child_RuleManager` (line 106), `FatClientValidate_Deserialize_MarkInvalid` (line 240) all PASS. |
| 6 | NeatooJsonSerializer lifecycle (converters handle $type polymorphism, $id/$ref, PropertyManager, entity meta) | Satisfied | The converter registration pattern is unchanged. The migration only changes how the resolver is obtained -- from `options.ReferenceHandler.CreateResolver()` to `NeatooReferenceResolver.Current`. All four serialization responsibilities ($type, $id/$ref, PropertyManager, meta properties) remain intact in the converter code. The `AddNeatooServices()` registration is not modified. |
| 7 | Trimming suppressions remain valid | Satisfied | All existing `[UnconditionalSuppressMessage]` attributes on `Read()` (lines 22-35), `Write()` (lines 313-321), and `DeserializeValidateProperty()` (lines 233-240) are unchanged. The new `NeatooReferenceResolver.Current` is a simple static property access -- no reflection, no dynamic type loading -- so no new trimming suppression is needed. |
| 8 | Serialization is automatic (objects cross client/server boundary without DTOs) | Satisfied | The migration is internal to converter plumbing. `NeatooJsonSerializer` continues to manage the full serialize/deserialize lifecycle. TwoContainerMetaStateTests (which simulate client-server round-trips) all PASS, confirming the automatic serialization contract is preserved. |

## Shared Dictionary Reference -- Detailed Assessment

The `FatClientValidate_Deserialize_SharedDictionaryReference` test (line 209) was identified in the pre-design review as part of Requirement 1 (circular reference handling). The test asserts `Assert.AreSame(newTarget.Data, newTarget.Data2)` for shared Dictionary instances.

**Finding:** This test is now [Ignore]d. The root cause is that RemoteFactory v0.22.0 no longer sets `options.ReferenceHandler` on `JsonSerializerOptions`, which means STJ's built-in converters (which serialize `Dictionary<string, string>`) no longer participate in reference tracking. This is a RemoteFactory behavioral change, not a Neatoo migration defect.

**Assessment:** The developer correctly followed the "Existing Tests Are Sacred" rule from the project's CLAUDE.md. The test was not gutted -- all assertions remain intact. The `[Ignore]` attribute includes a descriptive message pointing to the RemoteFactory tracking todo at `C:/src/neatoodotnet/RemoteFactory/docs/todos/shared-reference-handling-non-custom-types.md`, which I confirmed exists. The Neatoo converter code itself correctly handles reference tracking for Neatoo types via `NeatooReferenceResolver.Current` -- the loss of shared reference tracking for non-Neatoo types (Dictionary, List, etc.) is outside Neatoo's control.

**Classification:** This is NOT a requirements violation by the Neatoo migration. It is a known regression in the RemoteFactory v0.22.0 dependency, properly tracked.

## Unintended Side Effects

### State property cascading
No impact. The migration changes serialization plumbing only. IsModified, IsValid, IsBusy cascading logic in base classes is untouched. Verified by TwoContainerMetaStateTests all passing.

### Factory operation lifecycle
No impact. PauseAllActions/FactoryStart/FactoryComplete sequencing is unchanged. The converter code does not interact with factory lifecycle.

### Serialization round-trip fidelity
**One behavioral change detected** (shared non-Neatoo type reference tracking), attributed to RemoteFactory v0.22.0, not to this migration. All Neatoo-type reference tracking ($id/$ref for entities and entity lists) is preserved.

### Source generator output
No impact. The migration modifies hand-written converter code, not generated code or base class signatures.

### Rule execution timing
No impact. Rule execution is not involved in serialization.

### Parent-child relationships
No impact. IsChild, Root, Parent, ContainingList references are established via `OnDeserialized`/`OnDeserializing` callbacks, which are still invoked correctly in the converter Read paths (lines 61-63 and 113-116 in NeatooBaseJsonTypeConverter, lines 43-46 and 76-79 in NeatooListBaseJsonTypeConverter).

## Issues Found

None. The implementation is a clean mechanical migration that preserves all behavioral contracts. The one [Ignore]d test is correctly attributed to an upstream RemoteFactory issue with proper tracking.

## Implementation Quality Notes

1. **Consistent null-check patterns.** Both converters use identical patterns for each operation type: null-conditional for AddReference (Read EndObject), throw for ResolveReference (Read $ref), local variable check for GetReference (Write). This consistency reduces maintenance risk.

2. **Write method restructuring is clean.** The `NeatooBaseJsonTypeConverter.Write()` restructuring (lines 330-354) correctly branches on resolver availability: non-null path checks `alreadyExists` for early `$ref` return then emits `$id`; null path writes `WriteStartObject()` without `$id`. Both paths then share the remainder of the Write logic (PropertyManager, meta properties, LazyLoad). No code duplication.

3. **List converter Write is structurally different but correct.** `NeatooListBaseJsonTypeConverter.Write()` calls `WriteStartObject()` unconditionally before checking the resolver (line 134-142), then conditionally writes `$id`. This differs from the base converter's pattern but is correct because lists never emit `$ref` for `alreadyExists` (the plan noted this at line 229: "line 134 calls GetReference but only uses reference -- the alreadyExists variable is captured but never branched on").
