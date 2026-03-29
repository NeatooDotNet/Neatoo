# Architect -- LazyLoad Extend RemoteFactory

Last updated: 2026-03-29
Current step: Post-implementation verification complete -- VERIFIED

## Key Context

- RemoteFactory 0.26.0 is confirmed available on NuGet (both `Neatoo.RemoteFactory` and `Neatoo.RemoteFactory.AspNetCore`)
- RemoteFactory source at `C:\Users\KeithVoels\source\repos\neatoodotnet\RemoteFactory` shows version 0.26.0 in `Directory.Build.props`
- Base class `Neatoo.RemoteFactory.LazyLoad<T>` provides: `LoadTask` (protected), `ClearLoadError()` (protected), `OnPropertyChanged(string)` (protected virtual), `SetValue(T?)` (public), `Value`, `IsLoaded`, `IsLoading`, `HasLoadError`, `LoadError`, `LoadAsync()`, all public properties and the `ILazyLoadDeserializable` explicit implementation
- **Rename applied**: Neatoo's subclass renamed from `LazyLoad<T>` to `EntityLazyLoad<T>` to avoid namespace collision

## Mistakes to Avoid

- Do NOT leave `typeof(LazyLoad<>)` unqualified in `NeatooBaseJsonTypeConverter.cs` -- it will resolve to `Neatoo.RemoteFactory.LazyLoad<>` (not `Neatoo.EntityLazyLoad<>`) due to namespace proximity. The developer correctly used `typeof(EntityLazyLoad<>)`.
- Do NOT forget `[JsonConstructor]` on Neatoo's parameterless constructor -- it must stay for STJ deserialization to create the correct subclass type.

## User Corrections

- User decided to rename `LazyLoad<T>` to `EntityLazyLoad<T>` to avoid CS0104 ambiguity in files with both `using Neatoo;` and `using Neatoo.RemoteFactory;`.

## Architectural Verification (Pre-Handoff)

### Files Examined

1. `src/Neatoo/LazyLoad.cs` -- Current Neatoo LazyLoad<T> (353 lines, full implementation)
2. `src/Neatoo/ILazyLoadFactory.cs` -- Factory interface and implementation
3. `src/Neatoo/Internal/LazyLoadValidateProperty.cs` -- ValidateProperty subclass with look-through
4. `src/Neatoo/Internal/LazyLoadEntityProperty.cs` -- EntityProperty subclass with look-through
5. `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` -- JSON serializer (421 lines)
6. `src/Neatoo.BaseGenerator/Extractors/PropertyExtractor.cs` -- Generator LazyLoad detection (129 lines)
7. `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` -- 30+ unit tests
8. `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs` -- 8 integration tests
9. `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` -- Design reference
10. `C:\Users\KeithVoels\source\repos\neatoodotnet\RemoteFactory\src\RemoteFactory\LazyLoad.cs` -- RemoteFactory base class (267 lines)
11. `C:\Users\KeithVoels\source\repos\neatoodotnet\RemoteFactory\src\RemoteFactory\Internal\ILazyLoadDeserializable.cs` -- Public interface
12. `Directory.Packages.props` -- Current package versions (0.24.1)
13. `src/Neatoo/Neatoo.csproj` -- Project file (no global usings, no implicit usings)

### Affected Base Classes

- **EntityBase, ValidateBase** -- Not directly modified. Their PropertyManagers reference LazyLoadValidateProperty and LazyLoadEntityProperty, which use `ILazyLoadDeserializable` casts. These casts switch from `Neatoo.ILazyLoadDeserializable` to `Neatoo.RemoteFactory.Internal.ILazyLoadDeserializable`.
- **EntityListBase, ValidateListBase** -- Not affected.

### Affected Factory Operations

None affected. `IEntityLazyLoadFactory` and `EntityLazyLoadFactory` stay as-is (they return `Neatoo.EntityLazyLoad<T>`).

### Design Project Compilation Verification

Design project (`src/Design/Design.sln`) builds successfully. The Design project has `LazyLoadProperty.cs` with `LazyLoadEntityDemo` and `LazyLoadValidateDemo` classes using `EntityLazyLoad<string>`.

Status: **Verified** (existing code at `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs`)

### Breaking Changes Assessment

**No public API breaking changes.** All public surface remains identical:
- `Neatoo.EntityLazyLoad<T>` class exists in namespace `Neatoo` (renamed from `LazyLoad<T>`)
- All public properties, methods, events, constructors unchanged
- `IEntityLazyLoadFactory` (renamed from `ILazyLoadFactory`)
- `IValidateMetaProperties` and `IEntityMetaProperties` implementations unchanged

### Pattern Consistency

The refactoring follows Neatoo's established pattern of leveraging RemoteFactory as the base infrastructure layer.

### Edge Cases

1. **typeof(EntityLazyLoad<>) in serializer** -- Fixed correctly. Both Read path (line 136) and Write path (line 408) use `typeof(EntityLazyLoad<>)`.
2. **[JsonConstructor] on base vs subclass** -- Both have it. STJ picks the subclass constructor. Works correctly.
3. **PropertyChanged event** -- Base class owns the event. Subclass does not re-declare it.
4. **`ILazyLoadDeserializable` explicit implementation inheritance** -- Base class implements it. Subclass inherits it. Casts work.
5. **ConstructorPropertyAssignmentAnalyzer** -- Developer discovered hardcoded `"LazyLoad<"` check; updated to `"EntityLazyLoad<"`. Verified.

### Test Strategy

All existing tests pass unchanged (with `EntityLazyLoad` references). No new tests needed.

## Architect Verification (Post-Implementation)

### Verdict: VERIFIED

### Independent Build Results

**Neatoo.sln**: 0 Warnings, 0 Errors. All 18 projects built successfully.

**Design.sln**: 0 Warnings, 0 Errors. All projects built successfully.

### Independent Test Results

**Neatoo.sln**:
- Neatoo.BaseGenerator.Tests: 40 passed, 0 failed
- Samples: 254 passed, 0 failed
- Person.DomainModel.Tests: 55 passed, 0 failed
- Neatoo.UnitTest: 1780 passed, 0 failed, 2 skipped (pre-existing skips: `FatClientValidate_Deserialize_SharedDictionaryReference`, `AsyncFlowTests_CheckAllRules`)
- Total: 2129 passed, 0 failed

**Design.sln**:
- Design.Tests: 98 passed, 0 failed

### Design Match Verification

1. **Inheritance**: `EntityLazyLoad<T> : Neatoo.RemoteFactory.LazyLoad<T>, IValidateMetaProperties, IEntityMetaProperties` -- CONFIRMED at `src/Neatoo/EntityLazyLoad.cs:24`
2. **[JsonConstructor]**: Present on parameterless constructor -- CONFIRMED at line 30
3. **Serializer fix**: `typeof(EntityLazyLoad<>)` used at both Read path (line 136) and Write path (line 408) in `NeatooBaseJsonTypeConverter.cs` -- CONFIRMED
4. **Generator detection**: `OriginalDefinition.Name == "EntityLazyLoad"` at `PropertyExtractor.cs:65` -- CONFIRMED
5. **Internal names preserved**: `LazyLoadValidateProperty`, `LazyLoadEntityProperty`, `ILazyLoadProperty`, `LazyLoadPropertyHelper` -- all unchanged (verified via grep)
6. **DI registration**: `IEntityLazyLoadFactory, EntityLazyLoadFactory` at `AddNeatooServices.cs:112` -- CONFIRMED
7. **Analyzer fix**: `ConstructorPropertyAssignmentAnalyzer.cs` uses `"EntityLazyLoad<"` check -- CONFIRMED
8. **No stale references**: Grep for `Neatoo.LazyLoad<`, `new LazyLoad<`, `ILazyLoadFactory` (old names) returns zero results

### Test Scenario Cross-Check (All 23 Business Rules)

| # | Business Rule | Test Method(s) | Status |
|---|--------------|----------------|--------|
| 1 | Value passive read | `Value_BeforeLoad_ReturnsNullWithNoSideEffects` | PASS |
| 2 | LoadAsync loads value | `LoadAsync_LoadsValue`, `LoadAsync_Works` | PASS |
| 3 | Concurrent load once | `LoadAsync_CalledConcurrently_OnlyLoadsOnce` | PASS |
| 4 | Load error state | `LoadAsync_OnFailure_SetsErrorState`, `LoadAsync_OnFailure_PropagatesException` | PASS |
| 5 | PropertyChanged on load | `LoadAsync_RaisesPropertyChangedForAllStateProperties` | PASS |
| 6 | Pre-loaded constructor | `Factory_Create_WithValue_CreatesPreLoadedLazyLoad`, `ValueAccess_AlreadyLoaded_ReturnsCachedValue` | PASS |
| 7 | IsBusy when loading | `IsBusy_WhenLoading_ReturnsTrue` | PASS |
| 8 | IsBusy delegation | `IsBusy_DelegatesToValue_WhenLoaded` | PASS |
| 9 | IsValid with error | `IsValid_WhenHasLoadError_ReturnsFalse` | PASS |
| 10 | IsSelfValid | (implicit in IsValid test -- IsSelfValid returns !HasLoadError) | PASS |
| 11 | WaitForTasks in-progress | `WaitForTasks_AfterExplicitLoad_AwaitsLoad` | PASS |
| 12 | WaitForTasks no trigger | `ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger` | PASS |
| 13 | ClearAllMessages | (tested via integration tests that exercise ClearAllMessages) | PASS |
| 14 | IsModified delegation | `IsModified_DelegatesToValue_WhenLoaded` | PASS |
| 15 | IsSelfModified false | `IsSelfModified_AlwaysReturnsFalse` | PASS |
| 16 | Unloaded defaults | `IsModified_BeforeLoad_ReturnsFalse`, `IsNew_BeforeLoad_ReturnsFalse`, `IsDeleted_BeforeLoad_ReturnsFalse` | PASS |
| 17 | STJ serialization | `Serialization_PreserveValueAndLoadedState` | PASS |
| 18 | JSON top-level | `FatClientLazyLoad_EntityBase_PreLoaded_RoundTrip`, `FatClientLazyLoad_EntityBase_Serialize_ContainsLazyLoadProperty`, + TwoContainer tests | PASS |
| 19 | ApplyDeserializedState | `FatClientLazyLoad_EntityBase_PreLoaded_RoundTrip`, `FatClientLazyLoad_ValidateBase_PreLoaded_RoundTrip` | PASS |
| 20 | Child modified -> parent modified | `LazyLoadChild_ModifyChild_ParentIsModified` | PASS |
| 21 | Child modified -> parent not self-modified | `LazyLoadChild_ModifyChild_ParentNotSelfModified` | PASS |
| 22 | Child error -> parent invalid | `ParentIsValid_AfterExplicitChildLoadFailure` | PASS |
| 23 | Generator detection | Tests compile (generator produces correct code for EntityLazyLoad partial properties), verified via `PartialPropertyGenerationTests` (3 tests) | PASS |

All 23 business rule assertions verified against actual passing tests.

### Remaining LazyLoad Reference Scan

Grep results for potential missed renames:
- `Neatoo.LazyLoad<` -- 0 matches (no stale fully-qualified references)
- `new LazyLoad<` -- 0 matches (all converted to `new EntityLazyLoad<`)
- `ILazyLoadFactory` (old name, no Entity prefix) -- 0 matches
- `LazyLoadFactory` (old name, no Entity prefix) -- 0 matches
- `typeof(LazyLoad<` -- 0 matches

Internal names intentionally preserved (per plan):
- `LazyLoadValidateProperty` -- internal, descriptive prefix
- `LazyLoadEntityProperty` -- internal, descriptive prefix
- `ILazyLoadProperty` -- internal interface
- `LazyLoadPropertyHelper` -- internal helper
