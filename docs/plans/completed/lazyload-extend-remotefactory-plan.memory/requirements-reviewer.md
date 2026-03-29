# Requirements Reviewer -- LazyLoad: Extend from RemoteFactory

Last updated: 2026-03-29
Current step: Post-implementation verification complete

## Key Context

- Implementation renamed `LazyLoad<T>` to `EntityLazyLoad<T>`, `ILazyLoadFactory` to `IEntityLazyLoadFactory`, `LazyLoadFactory` to `EntityLazyLoadFactory`, `CreateLazyLoad` to `CreateEntityLazyLoad`
- `EntityLazyLoad<T>` inherits from `Neatoo.RemoteFactory.LazyLoad<T>`, removing all duplicated core loading logic
- Old `ILazyLoadDeserializable` removed from Neatoo; all casts now use `Neatoo.RemoteFactory.Internal.ILazyLoadDeserializable`
- Generator detects `"EntityLazyLoad"` in `PropertyExtractor.cs` and emits `CreateEntityLazyLoad` in `InitializerGenerator.cs`
- Serializer uses `typeof(EntityLazyLoad<>)` which is unambiguous (no namespace collision risk)
- 30 files modified across core library, generator, analyzer, tests, design, samples, and examples
- All 2129 + 98 tests pass (0 failures)

## Mistakes to Avoid

None for this agent.

## User Corrections

None for this agent.

## Requirements Verification

**Verdict: REQUIREMENTS SATISFIED**

All 23 behavioral contracts from the plan are satisfied by the implementation. The inheritance-based refactoring plus rename is a mechanical transformation that preserves all API behavior. The critical typeof namespace resolution issue identified in pre-design review was resolved by the rename itself (EntityLazyLoad is unique, no collision with RemoteFactory's LazyLoad).

### Requirements Compliance

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| 1 | Value passive read (no load triggered) | Satisfied | `EntityLazyLoad<T>` inherits `Value` from `Neatoo.RemoteFactory.LazyLoad<T>` which has identical passive-read behavior. Test `Value_BeforeLoad_ReturnsNullWithNoSideEffects` uses `new EntityLazyLoad<TestValue>(...)` and passes. |
| 2 | LoadAsync loads value | Satisfied | Inherited from base class. Test `LoadAsync_LoadsValue` passes. |
| 3 | Concurrent load safety | Satisfied | Inherited from base class. Test `LoadAsync_CalledConcurrently_OnlyLoadsOnce` passes. |
| 4 | Load error state | Satisfied | Inherited from base class. Tests `LoadAsync_OnFailure_SetsErrorState`, `LoadAsync_OnFailure_PropagatesException` pass. |
| 5 | PropertyChanged on load | Satisfied | Inherited from base class. Test `LoadAsync_RaisesPropertyChangedForAllStateProperties` passes. |
| 6 | Pre-loaded value | Satisfied | Constructor `EntityLazyLoad(T? value)` delegates to base. Test `Factory_Create_WithValue_CreatesPreLoadedLazyLoad` passes. |
| 7 | IsBusy during load | Satisfied | `EntityLazyLoad.IsBusy` at line 48: `IsLoading \|\| ((Value as IValidateMetaProperties)?.IsBusy ?? false)`. Test `IsBusy_WhenLoading_ReturnsTrue` passes. |
| 8 | IsBusy delegation to value | Satisfied | Same line 48 implementation. Test `IsBusy_DelegatesToValue_WhenLoaded` passes. |
| 9 | IsValid with load error | Satisfied | `EntityLazyLoad.IsValid` at line 51: `!HasLoadError && ((Value as IValidateMetaProperties)?.IsValid ?? true)`. Test `IsValid_WhenHasLoadError_ReturnsFalse` passes. |
| 10 | IsSelfValid = !HasLoadError | Satisfied | Line 54: `public bool IsSelfValid => !HasLoadError;`. Design Decision in LazyLoadProperty.cs lines 9-17 references EntityLazyLoad. |
| 11 | WaitForTasks awaits in-progress load | Satisfied | Lines 68-71: checks `LoadTask` (protected from base), delegates to value's WaitForTasks. Test `WaitForTasks_AfterExplicitLoad_AwaitsLoad` passes. |
| 12 | WaitForTasks no trigger on unaccessed | Satisfied | WaitForTasks only checks `LoadTask`, does not call `LoadAsync`. Test `ParentWaitForTasks_UnaccessedChild_CompletesWithoutTrigger` passes. |
| 13 | ClearAllMessages clears error + delegates | Satisfied | Lines 91-95: calls `ClearLoadError()` (protected from base) then delegates to value's `ClearAllMessages()`. |
| 14 | IsModified delegation | Satisfied | Line 111: `public bool IsModified => (Value as IEntityMetaProperties)?.IsModified ?? false;`. Test `IsModified_DelegatesToValue_WhenLoaded` passes. |
| 15 | IsSelfModified always false | Satisfied | Line 114: `public bool IsSelfModified => false;`. Test `IsSelfModified_AlwaysReturnsFalse` passes. |
| 16 | Unloaded defaults (false) | Satisfied | All delegation properties return false when Value is null. Tests `IsModified_BeforeLoad_ReturnsFalse`, `IsNew_BeforeLoad_ReturnsFalse`, `IsDeleted_BeforeLoad_ReturnsFalse` pass. |
| 17 | STJ serialization round-trip | Satisfied | `[JsonConstructor]` on parameterless constructor (line 30-31). Test `Serialization_PreserveValueAndLoadedState` passes. |
| 18 | Converter serialization | Satisfied | `NeatooBaseJsonTypeConverter.cs` line 136 and 408 use `typeof(EntityLazyLoad<>)`. FatClientLazyLoadTests (6 tests) pass. |
| 19 | Deserialization merge | Satisfied | `NeatooBaseJsonTypeConverter.cs` line 204 casts to `ILazyLoadDeserializable` (from `Neatoo.RemoteFactory.Internal`). `EntityLazyLoad<T>` inherits this interface from base class. FatClientLazyLoadTests pass. |
| 20 | State propagation: child modified -> parent IsModified | Satisfied | Test `LazyLoadChild_ModifyChild_ParentIsModified` passes. |
| 21 | State propagation: parent IsSelfModified stays false | Satisfied | Test `LazyLoadChild_ModifyChild_ParentNotSelfModified` passes. |
| 22 | State propagation: child load error -> parent IsValid false | Satisfied | Test `ParentIsValid_AfterExplicitChildLoadFailure` passes. |
| 23 | Generator detection | Satisfied | `PropertyExtractor.cs` line 65: `namedType.OriginalDefinition.Name == "EntityLazyLoad"` AND line 66: `ContainingNamespace?.ToString() == "Neatoo"`. Generator test `LazyLoadProperty_GeneratesCreateEntityLazyLoadRegistration` passes. |

### Design Project Verification

| File | Status | Notes |
|------|--------|-------|
| `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` | Updated | All references use `EntityLazyLoad<T>`, `IEntityLazyLoadFactory`, `CreateEntityLazyLoad`. Comments updated. |
| `src/Design/CLAUDE-DESIGN.md` | Updated | References `EntityLazyLoad<T>` correctly. |
| `src/Design/Design.Tests/` | N/A | No LazyLoad-specific tests exist (pre-existing gap, not introduced). |

### Skills Documentation Status

The skills documentation has NOT been updated. This is explicitly marked as out of scope by the developer (`"Skills documentation files -- out of scope for implementation, handled by documenter"`). However, these stale references will cause Claude to generate incorrect code when using the skill:

**Stale references found in skills:**

1. **`skills/neatoo/SKILL.md` line 3** (description) -- references `LazyLoad` and `ILazyLoadFactory` as trigger terms. Should include `EntityLazyLoad` and `IEntityLazyLoadFactory`.
2. **`skills/neatoo/SKILL.md` line 209** -- references `LazyLoad<T>` and `ILazyLoadFactory`. Should be `EntityLazyLoad<T>` and `IEntityLazyLoadFactory`.
3. **`skills/neatoo/references/lazy-loading.md`** -- 20+ stale references throughout:
   - Line 3, 8: `LazyLoad<T>` descriptions
   - Line 16: `ILazyLoadFactory`
   - Line 28: `LazyLoad<T>` pattern description
   - Line 41: `ILazyLoadFactory` in snippet code (stale -- sample source updated to `IEntityLazyLoadFactory`)
   - Line 71: `LazyLoad<ISkillLazyChild>` property declaration (stale -- sample source updated to `EntityLazyLoad<ISkillLazyChild>`)
   - Line 100: `factory.CreateLazyLoad<TInner>()` (stale -- now `CreateEntityLazyLoad`)
   - Line 104: `LazyLoad<IChild>` example
   - Lines 111, 126, 169, 183, 203, 243, 248: Various `LazyLoad<T>` references
   - **Note:** The embedded snippet code between `<!-- snippet: ... -->` and `<!-- endSnippet -->` tags will auto-update when `dotnet mdsnippets` runs (since the source samples are already updated). But the prose text around snippets and the non-snippet code examples remain stale.
4. **`skills/neatoo/references/pitfalls.md` line 18** -- references `LazyLoad<T>` in the anti-pattern description. Should be `EntityLazyLoad<T>`.
5. **`skills/neatoo/references/source-generation.md` line 79** -- references "LazyLoad properties" and "LazyLoad" as concept. Minor but should reference `EntityLazyLoad` for accuracy.

**Impact:** These stale references will cause Claude to generate code using the old class names (`LazyLoad<T>`, `ILazyLoadFactory`, `CreateLazyLoad`), which will produce compile errors. This is a documentation issue, not a requirements violation -- the implementation itself is correct.

### Unintended Side Effects

1. **State property cascading** -- No changes to cascading logic. `EntityLazyLoad<T>` implements `IValidateMetaProperties` and `IEntityMetaProperties` identically to the old `LazyLoad<T>`. Verified by examining `EntityLazyLoad.cs` lines 45-127.

2. **Factory operation lifecycle** -- No changes to factory lifecycle. `IEntityLazyLoadFactory` creates instances identically to old `ILazyLoadFactory`. Verified by examining `IEntityLazyLoadFactory.cs`.

3. **Serialization round-trip** -- The `typeof(EntityLazyLoad<>)` comparison in `NeatooBaseJsonTypeConverter.cs` (lines 136, 408) is correct and unambiguous. The rename actually RESOLVED the critical namespace ambiguity issue identified in the pre-design review (old `typeof(LazyLoad<>)` would have silently resolved to `Neatoo.RemoteFactory.LazyLoad<>` in the converter's namespace).

4. **Source generator output** -- Generator correctly detects `EntityLazyLoad` by name (PropertyExtractor.cs line 65) and emits `CreateEntityLazyLoad` (InitializerGenerator.cs line 45). Generator tests pass. No risk to other entities.

5. **Parent-child relationships** -- `ILazyLoadDeserializable` interface casts work correctly because `EntityLazyLoad<T>` inherits `ILazyLoadDeserializable` from `Neatoo.RemoteFactory.LazyLoad<T>`. Both `LazyLoadValidateProperty.cs` and `LazyLoadEntityProperty.cs` have `using Neatoo.RemoteFactory.Internal;` for the correct interface.

6. **DI registration** -- `AddNeatooServices.cs` line 112 registers `IEntityLazyLoadFactory` as `EntityLazyLoadFactory`. Verified.

### Issues Found

**No requirements violations found.** The implementation satisfies all 23 behavioral contracts.

**Documentation gap (not a violation):** Skills documentation (`skills/neatoo/`) contains stale references to old names (`LazyLoad<T>`, `ILazyLoadFactory`, `CreateLazyLoad`). This was explicitly scoped out of the implementation task. A follow-up task should update these skill files to prevent Claude from generating code with the old class names.
