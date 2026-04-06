# Architect -- Field-Level ReadOnly Authorization

Last updated: 2026-04-06
Current step: Post-implementation verification complete (Step 6 Part A)

## Key Context

### Scope: Minimal, Surgical Change
The plan proposes adding `void MarkReadOnly()` to `IValidateProperty` and implementing it in `ValidateProperty<T>` with a backing-field-based one-and-done `IsReadOnly` property. This is a 3-file change in the framework (IValidateProperty.cs, ValidateProperty.cs).

### Critical Findings

1. **EntityProperty<T> inherits cleanly.** Confirmed: `EntityProperty<T> : ValidateProperty<T>` does NOT override `IsReadOnly`. The base `ValidateProperty<T>.MarkReadOnly()` works for both entity and validate properties through inheritance. No additional work needed.

2. **LazyLoad properties are compatible.** Both `LazyLoadEntityProperty` and `LazyLoadValidateProperty` set `this.IsReadOnly = true` in their constructors. The one-and-done setter logic allows `false -> true` (the normal path) and `true -> true` (no-op, also fine). LazyLoad properties also override `SetValue()` to bypass `IsReadOnly`, so calling `MarkReadOnly()` on a LazyLoad property is a no-op (already true from constructor).

3. **JSON deserialization is compatible.** The `[JsonConstructor]` sets `this.IsReadOnly = isReadOnly` as the first write after field initialization. One-and-done only blocks `true -> false`, which the constructor never does (it sets the initial persisted state).

4. **Exactly 4 `IsReadOnly =` assignment sites exist** in the entire framework, all in constructors:
   - `ValidateProperty<T>(IPropertyInfo)` line 22
   - `ValidateProperty<T>(string, T, IRuleMessage[], bool)` line 30
   - `LazyLoadEntityProperty<T>(IPropertyInfo)` line 32
   - `LazyLoadValidateProperty<T>(IPropertyInfo)` line 114

5. **No code anywhere sets `IsReadOnly = false` after it was `true`.** One-and-done is safe.

6. **Indexer return types:** `ValidateBase<T>` indexer returns `IValidateProperty` (line 167); `EntityBase<T>` indexer returns `IEntityProperty` (line 513). Since `IEntityProperty : IValidateProperty`, `MarkReadOnly()` is accessible from both indexers.

7. **`PropertyChanged` pattern.** `ValidateProperty<T>.OnPropertyChanged` is a protected virtual method (line 282). The proposed `MarkReadOnly()` implementation calls `OnPropertyChanged(nameof(IsReadOnly))` which follows the same pattern used by `AddMarkedBusy()` and `RemoveMarkedBusy()`.

8. **All 10 MudNeatoo components** already bind `ReadOnly="@EntityProperty.IsReadOnly"` -- no UI changes needed.

### Design Project Verification

- **Design.Domain/PropertySystem/FieldLevelAuthorization.cs** -- New file exercising `MarkReadOnly()` in a `[Fetch]` method with authorization parameter
- **Design.Tests/PropertyTests/FieldLevelAuthorizationTests.cs** -- 5 tests covering the authorization pattern
- **Design.Tests/TestInfrastructure.cs** -- Added `MockFieldLevelAuthRepository` and DI registration

Build result: **Now compiles successfully** after framework implementation.

## Mistakes to Avoid

None identified in this pass.

## User Corrections

None -- first pass.

## Architectural Verification (Pre-Handoff)

### Scope Table

| Feature | Design Project Evidence | Status |
|---------|------------------------|--------|
| `MarkReadOnly()` on `IValidateProperty` | `Design.Domain/PropertySystem/FieldLevelAuthorization.cs:56` calls `this["Salary"].MarkReadOnly()` | Verified |
| One-and-done `IsReadOnly` setter | `ValidateProperty.cs:122-132` -- backing field with `true->false` guard | Verified |
| `PropertyChanged("IsReadOnly")` fires | `ValidatePropertyTests.MarkReadOnly_FiresPropertyChanged()` passes | Verified |
| `SetValue` throws after `MarkReadOnly()` | `Design.Tests/PropertyTests/FieldLevelAuthorizationTests.cs:79` passes | Verified |
| `LoadValue` succeeds after `MarkReadOnly()` | `Design.Tests/PropertyTests/FieldLevelAuthorizationTests.cs:99` passes | Verified |
| Serialization round-trip | `ValidatePropertyTests.MarkReadOnly_SerializationRoundTrip()` passes | Verified |

### Affected Base Classes

- [x] `ValidateBase<T>` -- Indexer returns `IValidateProperty`. `MarkReadOnly()` accessible. No changes needed.
- [x] `EntityBase<T>` -- Indexer returns `IEntityProperty`. `IEntityProperty : IValidateProperty`, so `MarkReadOnly()` inherited. No changes needed.
- [x] `EntityListBase<I>` -- Not affected
- [x] `ValidateListBase<I>` -- Not affected

### Affected Factory Operations

- [x] `[Create]` -- Not affected
- [x] `[Fetch]` -- Primary use case. MarkReadOnly called during Fetch based on authorization. Design project demonstrates this.
- [x] `[Insert]` -- Not affected
- [x] `[Update]` -- Not affected
- [x] `[Delete]` -- Not affected
- [x] `[Execute]` -- Not affected

### Breaking Changes Assessment

**No breaking changes.** Adding a method to an interface IS a breaking change for external implementors, but `IValidateProperty` has no external implementors -- all implementations are internal to the Neatoo framework.

### Pattern Consistency

The `MarkReadOnly()` pattern follows existing conventions:
- `MarkSelfUnmodified()` on `IEntityProperty` is the exact same pattern
- `AddMarkedBusy()`/`RemoveMarkedBusy()` on `IValidateProperty` is a similar pattern
- The naming convention `Mark{State}()` is consistent

### Test Strategy

**Unit tests** (in `Neatoo.UnitTest/Unit/Core/`):
- `ValidatePropertyTests.cs` -- 9 new tests covering all scenarios

**Design.Tests** (in `Design.Tests/PropertyTests/`):
- `FieldLevelAuthorizationTests.cs` -- 5 integration tests

### Edge Cases

1. Calling MarkReadOnly() twice -- Idempotent. Only fires PropertyChanged on first call.
2. Calling MarkReadOnly() on LazyLoad property -- No-op (already IsReadOnly=true from constructor).
3. Calling MarkReadOnly() on private-set property -- No-op (already IsReadOnly=true from PropertyInfo).
4. Thread safety -- One-and-done semantic is inherently safe for the important direction.

### Files Examined

1. `src/Neatoo/IValidateProperty.cs` -- Interface with MarkReadOnly() added
2. `src/Neatoo/Internal/ValidateProperty.cs` -- Implementation with backing field and MarkReadOnly()
3. `src/Neatoo/Internal/EntityPropertyManager.cs` -- EntityProperty<T> inherits from ValidateProperty<T>
4. `src/Neatoo/IEntityProperty.cs` -- Confirmed extends IValidateProperty, no IsReadOnly override
5. `src/Neatoo/Internal/LazyLoadEntityProperty.cs` -- Sets IsReadOnly=true in constructor
6. `src/Neatoo/Internal/LazyLoadValidateProperty.cs` -- Sets IsReadOnly=true in constructor
7. `src/Neatoo/ValidateBase.cs` -- Indexer returns IValidateProperty
8. `src/Neatoo/EntityBase.cs` -- Indexer returns IEntityProperty
9. `src/Neatoo/InternalInterfaces.cs` -- IValidatePropertyInternal (internal, not affected)
10. `src/Design/Design.Domain/PropertySystem/FieldLevelAuthorization.cs` -- Authorization pattern demo
11. `src/Design/Design.Tests/PropertyTests/FieldLevelAuthorizationTests.cs` -- Integration tests
12. `src/Design/Design.Tests/TestInfrastructure.cs` -- DI setup with MockFieldLevelAuthRepository
13. `src/Neatoo.UnitTest/Unit/Core/ValidatePropertyTests.cs` -- 9 new unit tests

## Architect Verification (Post-Implementation)

### Build Results (independently verified 2026-04-06)

| Solution | Result |
|----------|--------|
| `src/Neatoo.sln` | Build succeeded. 0 Warnings, 0 Errors |
| `src/Design/Design.sln` | Build succeeded. 0 Warnings, 0 Errors |

### Test Results (independently verified 2026-04-06)

| Solution | Passed | Failed | Skipped |
|----------|--------|--------|---------|
| `Neatoo.sln` (all projects) | 2140 | 0 | 2 |
| `Design.sln` | 104 | 0 | 0 |

The 2 skipped tests in Neatoo.sln are pre-existing (`FatClientValidate_Deserialize_SharedDictionaryReference` and `AsyncFlowTests_CheckAllRules`) and not related to this change.

### Test Scenario Cross-Check

| Plan Scenario # | Description | Corresponding Test(s) | Status |
|---|---|---|---|
| 1 | MarkReadOnly sets flag | `ValidatePropertyTests.MarkReadOnly_SetsIsReadOnlyTrue()` | PASS |
| 2 | MarkReadOnly prevents SetValue | `ValidatePropertyTests.MarkReadOnly_SetValueThrows()` + `FieldLevelAuthorizationTests.MarkReadOnly_SetValueThrowsOnReadOnlyField()` | PASS |
| 3 | MarkReadOnly allows SetPrivateValue | `ValidatePropertyTests.MarkReadOnly_SetPrivateValueSucceeds()` | PASS |
| 4 | MarkReadOnly allows LoadValue | `ValidatePropertyTests.MarkReadOnly_LoadValueSucceeds()` + `FieldLevelAuthorizationTests.MarkReadOnly_LoadValueSucceedsOnReadOnlyField()` | PASS |
| 5 | MarkReadOnly is permanent | `ValidatePropertyTests.MarkReadOnly_IsPermanent()` | PASS |
| 6 | MarkReadOnly on already-readonly | `ValidatePropertyTests.MarkReadOnly_OnAlreadyReadOnly_IsNoOp()` | PASS |
| 7 | MarkReadOnly via entity indexer | `FieldLevelAuthorizationTests.MarkReadOnly_DuringFetch_SetsSalaryReadOnly()` + `FieldLevelAuthorizationTests.MarkReadOnly_DuringFetch_NameRemainsWritable()` + `FieldLevelAuthorizationTests.MarkReadOnly_WhenCanEdit_SalaryRemainsWritable()` | PASS |
| 8 | MarkReadOnly fires PropertyChanged | `ValidatePropertyTests.MarkReadOnly_FiresPropertyChanged()` + `ValidatePropertyTests.MarkReadOnly_OnAlreadyReadOnly_DoesNotFirePropertyChanged()` | PASS |
| 9 | MarkReadOnly in Fetch serializes | `ValidatePropertyTests.MarkReadOnly_SerializationRoundTrip()` | PASS |

### Design Match

The implementation matches the plan's design exactly:
- `IValidateProperty` has `void MarkReadOnly()` with correct XML documentation
- `ValidateProperty<T>.IsReadOnly` uses a backing field with one-and-done protection
- `MarkReadOnly()` checks `!IsReadOnly` before setting and fires `PropertyChanged`
- Design.Domain demonstrates the authorization pattern in a `[Fetch]` method
- Design.Tests verifies the pattern through the factory with DI

### Verdict: VERIFIED

All builds pass with zero errors. All tests pass with zero failures. All 9 test scenarios from the plan have corresponding test methods that pass. The implementation matches the plan's design exactly.
