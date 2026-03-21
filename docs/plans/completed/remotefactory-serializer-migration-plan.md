# RemoteFactory v0.22.0 Serializer Migration Plan

**Date:** 2026-03-20
**Related Todo:** [RemoteFactory v0.22.0 Serializer Migration](../todos/remotefactory-serializer-migration.md)
**Status:** Complete
**Last Updated:** 2026-03-20

---

## Overview

Migrate Neatoo's custom JSON converters from the removed `options.ReferenceHandler.CreateResolver()` API to RemoteFactory v0.22.0's `NeatooReferenceResolver.Current` static AsyncLocal accessor. Six call sites across two converter files must change. The package reference must bump from v0.21.0 to v0.22.0.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/remotefactory-serializer-migration.md#requirements-review)

### Relevant Existing Requirements

#### Existing Tests (behavioral contracts)

- **Circular reference handling** (`FatClientEntityTests`, `FatClientValidateTests`, `FatClientEntityListTests`): WHEN entities with parent-child references are serialized and deserialized, THEN `$id`/`$ref` reference tracking preserves object identity (`Assert.AreSame`). Shared references (e.g., two properties pointing to the same Dictionary) are also preserved.

- **Entity meta property serialization** (`FatClientEntityTests` lines 103-212, `TwoContainerMetaStateTests` lines 31-283): WHEN entity meta properties (IsNew, IsModified, IsDeleted, IsChild, ModifiedProperties) are set and the entity is serialized/deserialized, THEN values are preserved.

- **LazyLoad property serialization** (`FatClientLazyLoadTests`, 7 tests): WHEN entities with LazyLoad properties are serialized and deserialized, THEN `LazyLoad.Value` and `LazyLoad.IsLoaded` survive the round-trip. Nested Neatoo entities inside LazyLoad go through the Neatoo converter for `$id`/`$ref`.

- **EntityListBase DeletedList serialization** (`FatClientEntityListTests` line 157): WHEN a non-new item is removed from an entity list and the list is serialized/deserialized, THEN the deserialized list's DeletedList contains the deleted item.

- **Validation state serialization** (`FatClientValidateTests` lines 67-253): WHEN a ValidateBase entity with rule violations is serialized/deserialized, THEN IsValid state, RuleRunCount, property-level validity, and MarkInvalid state are preserved.

#### Design Contract (CLAUDE-DESIGN.md)

- **NeatooJsonSerializer lifecycle**: Converters handle type polymorphism (`$type`), reference preservation (`$id`/`$ref`), property manager serialization, and entity meta properties. Registration via `AddNeatooServices()` is unchanged.

- **Trimming suppressions**: Existing `[UnconditionalSuppressMessage]` annotations on Read/Write methods remain valid. No new trimming suppression needed for accessing a static property.

- **Serialization is automatic**: Objects cross client/server boundary without DTOs. The migration preserves this contract.

### Gaps

**Gap 1: No contract for bare STJ usage.** No existing test exercises Neatoo converters via bare `JsonSerializer.Serialize`/`Deserialize` without `NeatooJsonSerializer`. The null-check behavior when `NeatooReferenceResolver.Current` is null needs a design decision.

**Gap 2: No Neatoo-side test for resolver lifecycle.** Tests validate round-trip behavior end-to-end via `NeatooJsonSerializer`. No test directly asserts the resolver is non-null during converter execution. If RemoteFactory's lifecycle management has a bug, converters would silently skip reference handling.

### Contradictions

None.

### Recommendations for Architect

1. **Differentiate null-check behavior between Read and Write.** On Write, skipping reference tracking degrades output (no `$id`/`$ref`). On Read, skipping means `$ref` tokens cannot be resolved -- circular references fail silently. Consider throwing on Read when resolver is null and a `$ref` token is encountered.

2. **Verify AsyncLocal visibility in nested serialization.** Converters call `JsonSerializer.Serialize`/`Deserialize` recursively for nested entities. Confirm `NeatooReferenceResolver.Current` remains visible. Since AsyncLocal flows with execution context and these are synchronous same-thread calls, this is expected to work.

3. **Run all 88 previously-failing tests as primary verification.** Test files in `src/Neatoo.UnitTest/Integration/Concepts/Serialization/`.

4. **No existing Neatoo references to `NeatooReferenceHandler` or `NeatooReferenceResolver`.** Confirmed via Grep.

5. **Package version bump scope.** Both `Neatoo.RemoteFactory` and `Neatoo.RemoteFactory.AspNetCore` in `Directory.Packages.props` need bumping from `0.21.0` to `0.22.0`.

---

## Business Rules (Testable Assertions)

1. WHEN `NeatooBaseJsonTypeConverter.Write()` serializes an entity and `NeatooReferenceResolver.Current` is non-null, THEN it calls `GetReference()` on the resolver and emits `$id`/`$ref` tokens identically to the pre-migration behavior. -- Source: Circular reference handling (Req 1)

2. WHEN `NeatooBaseJsonTypeConverter.Read()` encounters a `$ref` token and `NeatooReferenceResolver.Current` is non-null, THEN it calls `ResolveReference()` on the resolver and returns the previously-deserialized object. -- Source: Circular reference handling (Req 1)

3. WHEN `NeatooBaseJsonTypeConverter.Read()` reaches EndObject with a non-empty `$id` and `NeatooReferenceResolver.Current` is non-null, THEN it calls `AddReference()` on the resolver. -- Source: Circular reference handling (Req 1)

4. WHEN `NeatooListBaseJsonTypeConverter.Write()` serializes a list and `NeatooReferenceResolver.Current` is non-null, THEN it calls `GetReference()` on the resolver and emits `$id`. -- Source: DeletedList serialization (Req 4)

5. WHEN `NeatooListBaseJsonTypeConverter.Read()` encounters a `$ref` token and `NeatooReferenceResolver.Current` is non-null, THEN it calls `ResolveReference()` on the resolver. -- Source: Circular reference handling (Req 1)

6. WHEN `NeatooListBaseJsonTypeConverter.Read()` reaches EndObject with a non-empty `$id` and `NeatooReferenceResolver.Current` is non-null, THEN it calls `AddReference()` on the resolver. -- Source: Circular reference handling (Req 1)

7. WHEN an entity with a child is serialized via `NeatooJsonSerializer` and deserialized, THEN `child.Parent` is the same object reference as the parent (`Assert.AreSame`). -- Source: Circular reference handling (Req 1)

8. WHEN entity meta properties (IsNew, IsModified, IsDeleted, IsChild) are set and the entity round-trips through serialization, THEN those values are preserved. -- Source: Entity meta property serialization (Req 2)

9. WHEN an entity list with deleted items is serialized and deserialized, THEN the DeletedList contains the deleted items. -- Source: DeletedList serialization (Req 4)

10. WHEN a ValidateBase entity with rule violations round-trips through serialization, THEN IsValid state and rule messages are preserved. -- Source: Validation state serialization (Req 5)

11. WHEN an entity with LazyLoad properties round-trips through serialization, THEN `LazyLoad.Value` and `LazyLoad.IsLoaded` are preserved. -- Source: LazyLoad serialization (Req 3)

12. WHEN `NeatooReferenceResolver.Current` is null (bare STJ usage) and `NeatooBaseJsonTypeConverter.Write()` is invoked, THEN it writes the entity without `$id`/`$ref` reference tracking (degraded but functional). -- Source: NEW (Gap 1)

13. WHEN `NeatooReferenceResolver.Current` is null (bare STJ usage) and `NeatooBaseJsonTypeConverter.Read()` encounters a `$ref` token, THEN it throws `JsonException` because the reference cannot be resolved. -- Source: NEW (Gap 1, Recommendation 1)

14. WHEN `NeatooReferenceResolver.Current` is null and `NeatooBaseJsonTypeConverter.Read()` reaches EndObject with a `$id`, THEN the `$id` is silently ignored (no resolver to register with). -- Source: NEW (Gap 1)

### Design Decisions on Gaps

**Gap 1 Resolution -- Null resolver behavior:**

- **Write path**: When resolver is null, skip `$id`/`$ref` entirely. Write the object inline without reference tracking. This is degraded but produces valid JSON. Rationale: converters may be invoked outside `NeatooJsonSerializer` (e.g., in unit tests using bare STJ), and crashing is worse than degraded output.

- **Read path, `$ref` token**: When resolver is null and a `$ref` token is encountered, throw `JsonException`. Rationale: a `$ref` token means the JSON was produced WITH reference tracking, so it cannot be correctly deserialized without a resolver. Silently returning null or default would produce corrupt object graphs. The throw surfaces the misconfiguration.

- **Read path, `$id` token**: When resolver is null and a `$id` is found, silently ignore it (just don't call `AddReference`). The object is still constructed correctly; the `$id` is simply unused metadata.

**Gap 2**: No action needed in this migration. RemoteFactory owns the resolver lifecycle contract. Neatoo's converters defensively null-check and degrade/throw as appropriate.

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Entity round-trip with child parent reference | Entity with Child property, Child.Parent = Entity | 1, 2, 3, 7 | After deserialization, `newEntity.Child.Parent` is same object as `newEntity` |
| 2 | Entity round-trip preserves IsNew/IsModified | Entity with IsNew=false, IsModified=true | 8 | After deserialization, IsNew=false, IsModified=true |
| 3 | Entity list round-trip with deleted items | EntityList with one active item and one deleted item | 4, 6, 9 | After deserialization, list has 1 active item, DeletedList has 1 item |
| 4 | Validate entity round-trip with rule violations | ValidateBase with Name="" triggering "Name required" rule | 10 | After deserialization, IsValid=false, rule message preserved |
| 5 | LazyLoad property round-trip | Entity with LazyLoad<string> Value="loaded", IsLoaded=true | 11 | After deserialization, LazyLoad.Value="loaded", IsLoaded=true |
| 6 | Shared dictionary reference preservation | ValidateBase with Data and Data2 pointing to same Dictionary | 1, 2, 3 | After deserialization, `Assert.AreSame(newTarget.Data, newTarget.Data2)` |
| 7 | TwoContainer meta state round-trip | Entity created, modified, serialized to "server", back to "client" | 8 | Meta state (IsNew, IsModified, IsSavable) correct after each hop |

All 7 scenarios are already covered by existing tests in `src/Neatoo.UnitTest/Integration/Concepts/Serialization/`. The migration is a mechanical API change that must not alter any test outcomes.

---

## Approach

This is a mechanical migration with zero behavioral change. The strategy:

1. **Bump package references** from `0.21.0` to `0.22.0` in `Directory.Packages.props`.
2. **Replace all 6 call sites** in the two converter files, using `NeatooReferenceResolver.Current` with appropriate null-check behavior per the gap resolution above.
3. **Run all tests** to verify zero regressions.

No new files, no new classes, no architectural changes. The `NeatooReferenceResolver` class is already in the `Neatoo.RemoteFactory.Internal` namespace (same namespace as the converters), so no `using` statements need to change.

---

## Domain Model Behavioral Design

Not applicable. This migration changes internal serialization plumbing only. No domain model properties, computed values, reactive rules, or validation rules are affected.

---

## Design

### Call Site Transformations

**File 1: `NeatooBaseJsonTypeConverter.cs`**

**Line 58 (Read, AddReference at EndObject):**
```csharp
// Before:
options.ReferenceHandler!.CreateResolver().AddReference(id, result);

// After:
NeatooReferenceResolver.Current?.AddReference(id, result);
```
When resolver is null, silently skip (Rule 14).

**Line 79 (Read, ResolveReference for $ref):**
```csharp
// Before:
result = (T)options.ReferenceHandler!.CreateResolver().ResolveReference(refId);

// After:
var resolver = NeatooReferenceResolver.Current
    ?? throw new JsonException("Cannot resolve $ref: no NeatooReferenceResolver is active. Use NeatooJsonSerializer for deserialization.");
result = (T)resolver.ResolveReference(refId);
```
When resolver is null, throw (Rule 13). A `$ref` token requires a resolver.

**Line 328 (Write, GetReference):**
```csharp
// Before:
var reference = options.ReferenceHandler.CreateResolver().GetReference(value, out var alreadyExists);

// After:
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
        return; // Early return for $ref
    }
    // ... rest of Write with $id
}
else
{
    // No resolver: write without $id/$ref reference tracking (Rule 12)
}
```
The Write method's control flow needs restructuring. When resolver is non-null, behavior is identical to today. When null, the entity is written inline without `$id`/`$ref`.

**File 2: `NeatooListBaseJsonTypeConverter.cs`**

**Line 40 (Read, AddReference at EndObject):**
```csharp
// Before:
options.ReferenceHandler.CreateResolver().AddReference(id, list);

// After:
NeatooReferenceResolver.Current?.AddReference(id, list);
```
Same pattern as base converter -- silently skip when null.

**Line 60 (Read, ResolveReference for $ref):**
```csharp
// Before:
list = (IList)options.ReferenceHandler.CreateResolver().ResolveReference(refId);

// After:
var resolver = NeatooReferenceResolver.Current
    ?? throw new JsonException("Cannot resolve $ref: no NeatooReferenceResolver is active. Use NeatooJsonSerializer for deserialization.");
list = (IList)resolver.ResolveReference(refId);
```
Same pattern -- throw on `$ref` when null.

**Line 134 (Write, GetReference):**
```csharp
// Before:
var reference = options.ReferenceHandler.CreateResolver().GetReference(value, out var alreadyExists);

// After:
var resolver = NeatooReferenceResolver.Current;
```
The list converter's Write method needs similar restructuring to the base converter for the null case. However, the list converter currently does NOT check `alreadyExists` -- it always writes the full list. Looking at the code, line 134 calls `GetReference` but only uses `reference` (the `$id` string). The `alreadyExists` variable is captured but never branched on. So for the null case, the list can be written without `$id`.

### AsyncLocal Visibility Verification

`NeatooReferenceResolver.Current` uses `AsyncLocal<T>`, which flows with the execution context. The converters invoke `JsonSerializer.Serialize`/`Deserialize` recursively for:
- PropertyManager `$value` entries (line 369 in base converter Write)
- LazyLoad values (line 199 in base converter Read, line 402 in Write)
- EntityListBase items (line 154 in list converter Write)

These are all synchronous calls on the same thread. AsyncLocal values are inherited by child execution contexts and visible on the same thread. No issues expected.

### Package Version Bump

In `Directory.Packages.props`:
- `Neatoo.RemoteFactory`: `0.21.0` -> `0.22.0`
- `Neatoo.RemoteFactory.AspNetCore`: `0.21.0` -> `0.22.0`

---

## Implementation Steps

1. **Bump package references.** Change both `Neatoo.RemoteFactory` and `Neatoo.RemoteFactory.AspNetCore` from `0.21.0` to `0.22.0` in `Directory.Packages.props`.

2. **Migrate `NeatooBaseJsonTypeConverter.cs`.** Replace all 3 call sites per the design above:
   - Line 58: `AddReference` -> null-conditional call on `NeatooReferenceResolver.Current`
   - Line 79: `ResolveReference` -> throw `JsonException` when resolver is null
   - Line 328: `GetReference` -> restructure Write method control flow for null resolver case

3. **Migrate `NeatooListBaseJsonTypeConverter.cs`.** Replace all 3 call sites per the design above:
   - Line 40: `AddReference` -> null-conditional call on `NeatooReferenceResolver.Current`
   - Line 60: `ResolveReference` -> throw `JsonException` when resolver is null
   - Line 134: `GetReference` -> restructure Write method for null resolver case

4. **Build and run all tests.** Verify zero regressions across all serialization tests.

---

## Acceptance Criteria

- [ ] `Directory.Packages.props` references RemoteFactory v0.22.0
- [ ] Zero references to `options.ReferenceHandler` remain in Neatoo source
- [ ] `NeatooBaseJsonTypeConverter.cs` uses `NeatooReferenceResolver.Current` at all 3 call sites
- [ ] `NeatooListBaseJsonTypeConverter.cs` uses `NeatooReferenceResolver.Current` at all 3 call sites
- [ ] Write path gracefully degrades when resolver is null (no crash, no `$id`/`$ref`)
- [ ] Read path throws `JsonException` when resolver is null and `$ref` token is encountered
- [ ] All existing serialization tests pass (including the 88 previously-failing tests)
- [ ] `dotnet build src/Neatoo.sln` succeeds
- [ ] `dotnet test src/Neatoo.sln` succeeds with zero failures

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Package bump + converter migration | developer | Yes | Single coherent unit -- 2 files + 1 props file. Small scope fits one context window. | None |
| Phase 2: Build and test verification | developer | No (same agent) | Must verify immediately after changes. Same context has the change details. | Phase 1 |

**Parallelizable phases:** None -- Phase 2 depends on Phase 1.

**Notes:** This is a single-phase task. The developer implements the changes and runs verification in one session. No fresh agent boundary needed between phases -- the total scope is 3 files with mechanical changes.

---

## Dependencies

- RemoteFactory v0.22.0 NuGet package must be published to NuGet.org (or available via local feed). Currently the code is committed but no v0.22.0 tag exists in the RemoteFactory repo. **The developer must verify the package is available before starting.**
- If v0.22.0 is not published yet, the RemoteFactory repo needs `git tag v0.22.0 && git push origin v0.22.0` to trigger the publish workflow.

---

## Risks / Considerations

1. **RemoteFactory v0.22.0 not yet published.** The code is on main but the tag doesn't exist. If the package isn't on NuGet, `dotnet restore` will fail. Mitigation: check NuGet availability first; if missing, tag and publish.

2. **Write method restructuring complexity.** The null-resolver path in `NeatooBaseJsonTypeConverter.Write()` requires duplicating the "write object body" logic or extracting it. The developer should minimize duplication while keeping the code readable. The cleanest approach is: get the resolver, if non-null call GetReference and check alreadyExists for early `$ref` return, then proceed with the main write body using the resolver for `$id` only at the top; if resolver is null, skip the `$id` write.

3. **Design project pre-existing build failures.** The `src/Design/Design.sln` has 101 pre-existing NF0105 errors unrelated to this migration. Design project compilation verification is not applicable to this task because the migration affects internal serialization plumbing, not the public API surface that Design projects exercise.

4. **Expected documentation deliverables.** After implementation, the following may need updating:
   - `src/Design/CLAUDE-DESIGN.md` Serialization Considerations section (lines 401-459) -- should note that reference handling now uses `NeatooReferenceResolver.Current` rather than `options.ReferenceHandler`
   - Neatoo skill serialization documentation if it references the old API
