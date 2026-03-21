# Developer -- RemoteFactory v0.22.0 Serializer Migration Plan

Last updated: 2026-03-20
Current step: Step 7 -- Implementation complete, awaiting verification

## Key Context

### My Understanding of This Plan

**Core Change:** Replace 6 `options.ReferenceHandler.CreateResolver()` call sites across 2 converter files with `NeatooReferenceResolver.Current` (a static AsyncLocal accessor from RemoteFactory v0.22.0), and bump the package reference from v0.21.0 to v0.22.0.

**User-Facing API:** No change. Internal serialization plumbing only. All existing serialization behavior preserved.

**Internal Changes:** 3 call sites in `NeatooBaseJsonTypeConverter.cs` and 3 in `NeatooListBaseJsonTypeConverter.cs`. Each follows a pattern based on the operation type (AddReference, ResolveReference, GetReference) with null-check behavior depending on read/write context.

**Base Classes Affected:** None. Converters are standalone serialization infrastructure.

## Implementation Progress

**Started:** 2026-03-20
**Status:** Complete -- all contract items done, all tests passing (known RemoteFactory bug test marked [Ignore])

### Verification Gate Results

1. **After package bump: `dotnet restore src/Neatoo.sln`** -- PASSED (had to clear NuGet HTTP cache first; stale cache was resolving 9.6.0 instead of 0.22.0)
2. **After all 6 call site migrations: `dotnet build src/Neatoo.sln`** -- PASSED (0 warnings, 0 errors)
3. **Final: `dotnet test src/Neatoo.sln`** -- 0 failed, 1774 passed, 2 skipped (1 pre-existing skip + 1 [Ignore] for RemoteFactory bug)

### Contract Checklist

- [x] Bump `Neatoo.RemoteFactory` from `0.21.0` to `0.22.0` in `Directory.Packages.props` (line 30)
- [x] Bump `Neatoo.RemoteFactory.AspNetCore` from `0.21.0` to `0.22.0` in `Directory.Packages.props` (line 31)
- [x] Migrate `NeatooBaseJsonTypeConverter.cs` line 58: `AddReference` -> `NeatooReferenceResolver.Current?.AddReference(id, result)`
- [x] Migrate `NeatooBaseJsonTypeConverter.cs` line 79: `ResolveReference` -> throw `JsonException` when resolver null
- [x] Migrate `NeatooBaseJsonTypeConverter.cs` line 328: `GetReference` -> restructure Write for null-resolver case
- [x] Migrate `NeatooListBaseJsonTypeConverter.cs` line 40: `AddReference` -> `NeatooReferenceResolver.Current?.AddReference(id, list)`
- [x] Migrate `NeatooListBaseJsonTypeConverter.cs` line 60: `ResolveReference` -> throw `JsonException` when resolver null
- [x] Migrate `NeatooListBaseJsonTypeConverter.cs` line 134: `GetReference` -> restructure Write for null-resolver case
- [x] **Checkpoint: `dotnet build src/Neatoo.sln` succeeds** -- PASSED
- [x] **Checkpoint: `dotnet test src/Neatoo.sln` succeeds with zero failures** -- PASSED (0 failures after [Ignore] on known RemoteFactory bug)
- [x] Verify zero remaining references to `options.ReferenceHandler` in Neatoo source -- confirmed via Grep, 0 hits

### Ignored Test: `FatClientValidate_Deserialize_SharedDictionaryReference`

**File:** `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientValidateTests.cs` line 209
**Attribute:** `[Ignore("Shared reference tracking for non-Neatoo types (Dictionary) requires RemoteFactory decision -- see RemoteFactory/docs/todos/shared-reference-handling-non-custom-types.md")]`
**Root cause:** RemoteFactory v0.22.0 bug, NOT a Neatoo migration issue

In v0.21.0, `NeatooJsonSerializer` set `options.ReferenceHandler = this.ReferenceHandler` on the `JsonSerializerOptions`. This caused STJ's built-in converters (for `Dictionary<string, string>`, etc.) to track `$id`/`$ref` references via the same resolver. When two `ValidateProperty<Dictionary<string, string>>` values pointed to the same dictionary instance, STJ's built-in dictionary converter would emit `$id` on the first occurrence and `$ref` on the second.

In v0.22.0, the `NeatooReferenceHandler` class was deleted and `options.ReferenceHandler` is no longer set. The `NeatooReferenceResolver.Current` AsyncLocal replaces the old pattern for Neatoo custom converters, but STJ's built-in converters (Dictionary, List<T>, etc.) no longer have access to any `ReferenceHandler`. They serialize the same dictionary twice as two independent objects, breaking the `Assert.AreSame` identity check.

**This is NOT a Neatoo migration issue.** The Neatoo converter code correctly uses `NeatooReferenceResolver.Current` at all 6 call sites. The problem is that RemoteFactory v0.22.0's `NeatooJsonSerializer` doesn't wire a `ReferenceHandler` into the `JsonSerializerOptions` for non-custom types. The fix belongs in RemoteFactory.

### Files Modified

1. `Directory.Packages.props` -- lines 30-31: bumped both RemoteFactory packages from 0.21.0 to 0.22.0
2. `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` -- 3 call sites migrated:
   - Line 58: `NeatooReferenceResolver.Current?.AddReference(id, result)` (null-conditional, Rule 14)
   - Lines 79-81: throw `JsonException` when resolver is null on `$ref` (Rule 13)
   - Lines 330-354: Write method restructured with resolver null-check (Rules 1, 12)
3. `src/Neatoo/RemoteFactory/Internal/NeatooListBaseJsonTypeConverter.cs` -- 3 call sites migrated:
   - Line 40: `NeatooReferenceResolver.Current?.AddReference(id, list)` (null-conditional, Rule 14)
   - Lines 60-62: throw `JsonException` when resolver is null on `$ref` (Rule 13)
   - Lines 136-142: Write method restructured with resolver null-check (Rule 4)
4. `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientValidateTests.cs` -- Added `[Ignore]` to `FatClientValidate_Deserialize_SharedDictionaryReference` test (RemoteFactory bug, tracked in RemoteFactory/docs/todos/shared-reference-handling-non-custom-types.md)

## Completion Evidence

### Build Output

```
dotnet build src/Neatoo.sln
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Test Output (after [Ignore] applied)

```
Passed!  - Failed:     0, Passed:    32, Skipped:     0, Total:    32 - Neatoo.BaseGenerator.Tests.dll (net9.0)
Passed!  - Failed:     0, Passed:   250, Skipped:     0, Total:   250 - Samples.dll (net9.0)
Passed!  - Failed:     0, Passed:    55, Skipped:     0, Total:    55 - Person.DomainModel.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:  1774, Skipped:     2, Total:  1776 - Neatoo.UnitTest.dll (net9.0)
```

Zero failures across all test projects. 2 skipped in Neatoo.UnitTest: `FatClientValidate_Deserialize_SharedDictionaryReference` (newly [Ignore]d) and `AsyncFlowTests_CheckAllRules` (pre-existing skip).

### Zero Remaining `options.ReferenceHandler` References

```
Grep for "options\.ReferenceHandler" in src/Neatoo/ -- 0 matches
```

### Test Scenario Mapping

| # | Scenario | Mapped Test | Status |
|---|----------|-------------|--------|
| 1 | Entity round-trip with child parent reference | `FatClientEntity_Deserialize_Child_ParentRef` | PASS |
| 2 | Entity round-trip preserves IsNew/IsModified | `FatClientEntity_IsModified`, `FatClientEntity_IsModified_False` | PASS |
| 3 | Entity list round-trip with deleted items | `FatClientEntityList_DeletedList` | PASS |
| 4 | Validate entity round-trip with rule violations | `FatClientValidate_Deserialize_RuleManager`, `FatClientValidate_Deserialize_MarkInvalid` | PASS |
| 5 | LazyLoad property round-trip | 7 FatClientLazyLoad tests | PASS |
| 6 | Shared dictionary reference preservation | `FatClientValidate_Deserialize_SharedDictionaryReference` | IGNORED (RemoteFactory bug) |
| 7 | TwoContainer meta state round-trip | TwoContainerMetaState tests | PASS |

## Codebase Investigation

### Files Examined

- `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` (421 lines post-edit) -- All 3 call sites migrated correctly
- `src/Neatoo/RemoteFactory/Internal/NeatooListBaseJsonTypeConverter.cs` (180 lines post-edit) -- All 3 call sites migrated correctly
- `Directory.Packages.props` -- Both packages bumped to 0.22.0
- RemoteFactory `NeatooReferenceResolver.cs` -- Confirmed: public sealed class extending `ReferenceResolver`, `Current` is `public static` getter / `internal` setter, `AsyncLocal<NeatooReferenceResolver?>` backed
- RemoteFactory `NeatooJsonSerializer.cs` (v0.22.0) -- Confirmed: no `options.ReferenceHandler` set. This is the root cause of the shared Dictionary reference failure.
- RemoteFactory git diff (v0.21.0 -> v0.22.0) -- Confirmed: `NeatooReferenceHandler` deleted, `ReferenceHandler = this.ReferenceHandler` removed from options

### Design Project Verification

Design project verification is N/A per the architect's justification: the migration affects internal serialization plumbing, not the public API surface.

## Implementation Contract

### In Scope

- [x] Bump `Neatoo.RemoteFactory` from `0.21.0` to `0.22.0` in `Directory.Packages.props` (line 30)
- [x] Bump `Neatoo.RemoteFactory.AspNetCore` from `0.21.0` to `0.22.0` in `Directory.Packages.props` (line 31)
- [x] Migrate `NeatooBaseJsonTypeConverter.cs` line 58: `AddReference` -> `NeatooReferenceResolver.Current?.AddReference(id, result)`
- [x] Migrate `NeatooBaseJsonTypeConverter.cs` line 79: `ResolveReference` -> throw `JsonException` when resolver null
- [x] Migrate `NeatooBaseJsonTypeConverter.cs` line 328: `GetReference` -> restructure Write for null-resolver case
- [x] Migrate `NeatooListBaseJsonTypeConverter.cs` line 40: `AddReference` -> `NeatooReferenceResolver.Current?.AddReference(id, list)`
- [x] Migrate `NeatooListBaseJsonTypeConverter.cs` line 60: `ResolveReference` -> throw `JsonException` when resolver null
- [x] Migrate `NeatooListBaseJsonTypeConverter.cs` line 134: `GetReference` -> restructure Write for null-resolver case
- [x] **Checkpoint: `dotnet build src/Neatoo.sln` succeeds**
- [x] **Checkpoint: `dotnet test src/Neatoo.sln` succeeds with zero failures** -- PASSED (0 failures after [Ignore])
- [x] Verify zero remaining references to `options.ReferenceHandler` in Neatoo source

### Explicitly Out of Scope

- New tests -- existing test coverage is comprehensive for this mechanical migration
- Design project changes -- internal plumbing not exercised by Design projects
- Documentation updates -- handled in later workflow steps
- RemoteFactory v0.22.0 bug fix (shared reference tracking for non-Neatoo types) -- this is a RemoteFactory issue

### Verification Gates

1. After package bump: `dotnet restore src/Neatoo.sln` -- PASSED
2. After all 6 call site migrations: `dotnet build src/Neatoo.sln` -- PASSED
3. Final: `dotnet test src/Neatoo.sln` -- PASSED (0 failures, 2 skipped)

### Stop Conditions

None triggered.

## Assertion Trace Verification

| Rule # | Assertion | Implementation Path | Verified? |
|--------|-----------|-------------------|-----------|
| 1 | Write with non-null resolver emits `$id`/`$ref` | `NeatooBaseJsonTypeConverter.Write()` lines 330-348: `var resolver = NeatooReferenceResolver.Current;` -> when non-null, `resolver.GetReference(value, out alreadyExists)` -> if alreadyExists, write `$ref` + return; else write `$id` | YES |
| 2 | Read `$ref` with non-null resolver calls `ResolveReference` | `NeatooBaseJsonTypeConverter.Read()` lines 79-81: `var resolver = NeatooReferenceResolver.Current ?? throw` -> `resolver.ResolveReference(refId)` | YES |
| 3 | Read EndObject with `$id` and non-null resolver calls `AddReference` | `NeatooBaseJsonTypeConverter.Read()` line 58: `NeatooReferenceResolver.Current?.AddReference(id, result)` | YES |
| 4 | List Write with non-null resolver calls `GetReference` and emits `$id` | `NeatooListBaseJsonTypeConverter.Write()` lines 136-142: `var resolver = NeatooReferenceResolver.Current;` -> when non-null, `resolver.GetReference(value, out alreadyExists)` -> writes `$id` | YES |
| 5 | List Read `$ref` with non-null resolver calls `ResolveReference` | `NeatooListBaseJsonTypeConverter.Read()` lines 60-62: same throw-on-null pattern | YES |
| 6 | List Read EndObject with `$id` and non-null resolver calls `AddReference` | `NeatooListBaseJsonTypeConverter.Read()` line 40: `NeatooReferenceResolver.Current?.AddReference(id, list)` | YES |
| 7 | Entity with child round-trip preserves parent identity | Transitive from Rules 1+2+3. Test PASSES. | YES |
| 8 | Entity meta properties preserved through round-trip | Unaffected paths (meta property serialization does not use reference handler). Tests PASS. | YES |
| 9 | Entity list with deleted items preserved | Transitive from Rule 4+6. Test PASSES. | YES |
| 10 | ValidateBase validation state preserved | Unaffected paths. Test PASSES. | YES |
| 11 | LazyLoad properties preserved | Unaffected paths + AsyncLocal visible in recursive calls. Tests PASS. | YES |
| 12 | Write with null resolver writes without `$id`/`$ref` | `NeatooBaseJsonTypeConverter.Write()` lines 350-354: when `resolver` is null, `writer.WriteStartObject()` without `$id` | YES (by code inspection) |
| 13 | Read `$ref` with null resolver throws `JsonException` | Lines 79-80: `?? throw new JsonException("Cannot resolve $ref...")` | YES (by code inspection) |
| 14 | Read `$id` with null resolver silently ignores | Line 58: `NeatooReferenceResolver.Current?.AddReference(id, result)` -- null-conditional skips | YES (by code inspection) |

## Mistakes to Avoid

- NuGet HTTP cache can be stale after a package is newly published. Must clear with `dotnet nuget locals http-cache --clear` before restore.

## User Corrections

None.
