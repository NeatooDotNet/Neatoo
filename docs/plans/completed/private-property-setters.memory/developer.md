# Developer -- Private Property Setters

Last updated: 2026-03-23
Current step: Developer Deliverable (MarkdownSnippets sample) complete - STOP

## Key Context

### Problem
Source generator ignores setter accessibility on partial properties. `public partial string Name { get; private set; }` generates a public setter and `get; set;` on the interface. The runtime already supports private setters end-to-end via `PropertyInfoWrapper.IsPrivateSetter` -> `ValidateProperty.IsReadOnly` -> MudNeatoo ReadOnly binding. The gap is purely in the generator.

### Critical Design Constraint
Generated setter for private-set properties MUST use `SetPrivateValue()` -- NOT `.Value = value` (which routes to `SetValue()` and throws `PropertyReadOnlyException` when `IsReadOnly=true`).

### Files Modified (Phase 1+2)
- `src/Neatoo/IValidateProperty.cs` -- Added `SetPrivateValue` to public interface
- `src/Neatoo.BaseGenerator/Models/PartialPropertyInfo.cs` -- Added `SetterAccessibility` field
- `src/Neatoo.BaseGenerator/Extractors/PropertyExtractor.cs` -- Extract setter accessibility from accessor modifiers
- `src/Neatoo.BaseGenerator/Generators/PropertyGenerator.cs` -- Emit correct setter pattern (private -> SetPrivateValue, protected/internal -> .Value, public -> unchanged) and interface declarations (get-only for non-public setters)
- `src/Neatoo.BaseGenerator.Tests/PartialPropertyGenerationTests.cs` -- 8 new generator tests covering scenarios 1-7, 12

### Files NOT Modified
- `src/Neatoo.BaseGenerator.Tests/GeneratorTestHelper.cs` -- NeatooStubs did NOT need updating. Generator tests verify generated text content, not compilation. `SetPrivateValue` does not need to be in stubs since the generator just emits the text.

### Phase 3 Verification (Design Project)
- `src/Design/Design.Domain/PropertySystem/PropertyBasics.cs` -- PrivateSetPropertyDemo verified present (architect pre-added), lines 168-192
- `src/Design/Design.Domain/PropertySystem/IPropertyInterfaces.cs` -- IPrivateSetPropertyDemo verified present (architect pre-added), lines 37-42
- `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs` -- PrivateSetPropertyTests verified present (architect pre-added), lines 94-262
- Generated code verified at `src/Design/Design.Domain/Generated/Neatoo.BaseGenerator/Neatoo.BaseGenerator.PartialBaseGenerator/Design.Domain.PropertySystem.PrivateSetPropertyDemo.g.cs`:
  - `ComputedTotal`: `private set { ComputedTotalProperty.SetPrivateValue(value); ... }` -- CORRECT
  - `Quantity`, `UnitPrice`: public `set { ...Property.Value = value; ... }` -- CORRECT
- Factory generated at `src/Design/Design.Domain/Generated/Neatoo.Generator/Neatoo.Factory/Design.Domain.PropertySystem.PrivateSetPropertyDemoFactory.g.cs`:
  - `IPrivateSetPropertyDemoFactory` with `Create` method returning `IPrivateSetPropertyDemo` -- CORRECT

### Design.Tests Pre-Existing Build Blockers
Design.Tests cannot compile due to pre-existing CS1061 errors in OTHER test files (not PrivateSetPropertyTests). These CS1061 errors exist because:
1. NF0105 errors (202 total, `public [Remote]` methods) prevent Design.Domain from building normally
2. When NF0105 is suppressed via `-p:NoWarn=NF0105`, Design.Domain compiles but the `[Remote]` methods are `public` instead of `internal`, so RemoteFactory doesn't generate `Fetch` on factory interfaces
3. Other test files (EntityBaseTests, FetchTests, SaveTests, StatePropertyTests, OrderAggregateTests, CommonGotchaTests) reference `.Fetch()` on factory interfaces that don't have it
4. Zero CS1061 errors exist in PropertyBasicsTests.cs -- PrivateSetPropertyTests compiles cleanly

## Mistakes to Avoid
- Do NOT use `IValidatePropertyInternal` in generated code -- it's `internal` to the Neatoo assembly.
- Do NOT use `.Value = value` for private-set properties -- routes to `SetValue()` which throws.
- `PropertyReadOnlyException` is `internal` -- tests must use `PropertyException`.
- NeatooStubs in `GeneratorTestHelper.cs` do NOT include `IValidateProperty`, `PropertyManager`, `RunningTasks`, or `Parent`. Generator tests verify generated text content, not compilation.

## User Corrections
None so far.

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-23

### Assertion Trace Verification

| Rule # | Assertion | Implementation Path | Verified? |
|--------|-----------|-------------------|-----------|
| 1 | WHEN partial property has `private set`, THEN generated setter has `private set` accessor | `PropertyExtractor.ExtractProperties()`: extracts `setAccessor.Modifiers.FirstOrDefault()` -> `setterAccessibility = "private"`. `PropertyGenerator.GeneratePropertyImplementations()`: when `SetterAccessibility == "private"`, emits `private set { ... }` | Yes |
| 2 | WHEN partial property has `private set` and `NeedsInterfaceDeclaration=true`, THEN interface has `get;` only | `PropertyGenerator.GenerateInterfaceDeclaration()`: condition `property.HasSetter && property.SetterAccessibility == null` -> `"get; set;"` else `"get;"` | Yes |
| 3 | WHEN partial property has `private set`, THEN generated setter calls `SetPrivateValue(value)` | `PropertyGenerator.GeneratePropertyImplementations()`: when `SetterAccessibility == "private"`, emits `{name}Property.SetPrivateValue(value)` | Yes |
| 4 | WHEN partial property has `protected set`, THEN generated setter has `protected set` and uses `.Value = value` | `PropertyGenerator`: when `SetterAccessibility` is non-null but not `"private"`, emits `{setterAccessibility} set { {name}Property.Value = value; ... }` | Yes |
| 5 | WHEN partial property has `internal set`, THEN same as Rule 4 with `"internal"` | Same as Rule 4 | Yes |
| 6 | WHEN partial property has `private set` and is LazyLoad, THEN `private set` with `LoadValue(value)` | LazyLoad branch: prepends `setterAccessibility` to `set` keyword, keeps `LoadValue(value)` | Yes |
| 7 | WHEN partial property has no setter (get-only), THEN unchanged | `hasSetter = false` path unchanged | Yes |
| 13 | WHEN `SetPrivateValue(object?, bool)` added to `IValidateProperty`, THEN generated code can call it | Added to interface. `ValidateProperty<T>` already implements it as `public virtual`. | Yes |

## Implementation Contract

**Created:** 2026-03-23
**Approved by:** neatoo-developer

### Design Project Acceptance Criteria

- [x] `src/Design/Design.Domain/PropertySystem/PropertyBasics.cs:178` - `PrivateSetPropertyDemo.ComputedTotal { get; private set; }`: CS8799 -> Compiles after generator emits `private set`. Verified: `dotnet build src/Design/Design.Domain/Design.Domain.csproj -p:NoWarn=NF0105` succeeds with zero non-NF0105 errors.
- [x] `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs:257` - `totalProperty.SetPrivateValue(77.77m)`: Compiles after `SetPrivateValue` added to `IValidateProperty`. Verified: zero errors in PropertyBasicsTests.cs when Design.Tests is built (the only errors are pre-existing CS1061 in other files).

### In Scope

**Phase 1+2: API + Generator + Generator Tests**

- [x] Add `Task SetPrivateValue(object? newValue, bool quietly = false);` to `IValidateProperty` interface in `src/Neatoo/IValidateProperty.cs`
- [x] Add `string? SetterAccessibility` parameter to `PartialPropertyInfo` record struct in `src/Neatoo.BaseGenerator/Models/PartialPropertyInfo.cs`
- [x] Extract setter accessor modifier in `PropertyExtractor.ExtractProperties()` in `src/Neatoo.BaseGenerator/Extractors/PropertyExtractor.cs`
- [x] Pass `setterAccessibility` to `PartialPropertyInfo` constructor
- [x] Update `PropertyGenerator.GeneratePropertyImplementations()`:
  - Private setter: emit `private set { {name}Property.SetPrivateValue(value); if (!{name}Property.Task.IsCompleted) { ... } }`
  - Protected/internal setter: emit `{accessor} set { {name}Property.Value = value; if (!{name}Property.Task.IsCompleted) { ... } }`
  - LazyLoad with restricted setter: emit `{accessor} set { {name}Property.LoadValue(value); }`
- [x] Update `PropertyGenerator.GenerateInterfaceDeclaration()`: emit `get;` only when `SetterAccessibility` is non-null
- [x] Checkpoint: `dotnet build src/Neatoo.sln` passes
- [x] Checkpoint: `dotnet test src/Neatoo.sln` passes (no regressions)
- [x] Add generator tests to `src/Neatoo.BaseGenerator.Tests/PartialPropertyGenerationTests.cs` for scenarios 1-7, 12
- [x] NeatooStubs in `GeneratorTestHelper.cs` did NOT need updating
- [x] Checkpoint: `dotnet test src/Neatoo.BaseGenerator.Tests/Neatoo.BaseGenerator.Tests.csproj` passes

**Phase 3: Design Project + Integration Tests (separate agent invocation)**

- [x] Verify `PrivateSetPropertyDemo` and `IPrivateSetPropertyDemo` already exist (architect pre-added)
- [x] Verify `PrivateSetPropertyTests` already exists (architect pre-added)
- [x] Build Design.Domain: `dotnet build src/Design/Design.Domain/Design.Domain.csproj -p:NoWarn=NF0105` -- Build succeeded, zero non-NF0105 errors. PrivateSetPropertyDemo compiles (CS8799 resolved).
- [BLOCKED] Run Design.Tests: `dotnet test src/Design/Design.Tests/Design.Tests.csproj` -- Cannot run. Pre-existing CS1061 errors in other test files prevent the entire test project from compiling. PrivateSetPropertyTests itself has zero compilation errors.
- [x] Checkpoint: Both Design project acceptance criteria compile (verified individually)

### Explicitly Out of Scope

- MudNeatoo component changes (binding already works via `IsReadOnly`)
- RemoteFactory changes (serialization uses `PropertyManager.SetProperties()`, not setters)
- Dynamic IsReadOnly (only structural from `PropertyInfoWrapper.IsPrivateSetter`)
- `private protected set` compound modifier support
- `private init` accessor handling (beyond what naturally falls out of the existing code path)
- Documentation updates (skill docs, release notes) -- Step 9

### Verification Gates

1. [x] After Phase 1 core changes: `dotnet build src/Neatoo.sln` passes with zero errors
2. [x] After Phase 1 complete: `dotnet test src/Neatoo.sln` passes with zero failures
3. [x] After Phase 2 generator tests: `dotnet test src/Neatoo.BaseGenerator.Tests/Neatoo.BaseGenerator.Tests.csproj` passes
4. [PARTIAL] After Phase 3: Design.Domain compiles (CS8799 resolved) -- YES. Design.Tests pass -- BLOCKED by pre-existing CS1061 errors in other files.
5. [PARTIAL] Final: All main test suites pass (2119 passed, 0 failures). Design project acceptance criteria compile. Design.Tests integration tests cannot be run (blocked by pre-existing issues).

### Stop Conditions

**Triggered:** Design.Tests cannot compile due to pre-existing CS1061 errors in other test files (not caused by this work). See "Design.Tests Pre-Existing Build Blockers" in Key Context above. This is the stop condition "Design.sln pre-existing NF0105 errors prevent verification" documented in the plan's Risks section.

**Severity:** The PrivateSetPropertyTests code compiles cleanly. The 8 test methods (scenarios 8, 9, 10, 11, 13) are syntactically correct and reference valid APIs. The pre-existing blockers are in EntityBaseTests, FetchTests, SaveTests, StatePropertyTests, OrderAggregateTests, and CommonGotchaTests -- all referencing `.Fetch()` methods that were never generated because those `[Remote]` methods are `public` instead of `internal`.

### Test Scenario Mapping

| Scenario # | Test Type | Test Method | File Path | Status |
|------------|-----------|-------------|-----------|--------|
| 1 | Generator | `PartialProperty_PrivateSetter_GeneratesPrivateSetAccessor` | `src/Neatoo.BaseGenerator.Tests/PartialPropertyGenerationTests.cs` | PASS |
| 2 | Generator | `PartialProperty_PrivateSetter_InterfaceIsGetOnly` | `src/Neatoo.BaseGenerator.Tests/PartialPropertyGenerationTests.cs` | PASS |
| 3 | Generator | `PartialProperty_PrivateSetter_UsesSetPrivateValue` | `src/Neatoo.BaseGenerator.Tests/PartialPropertyGenerationTests.cs` | PASS |
| 4 | Generator | `PartialProperty_ProtectedSetter_PreservesAccessorAndUsesValueAssignment` | `src/Neatoo.BaseGenerator.Tests/PartialPropertyGenerationTests.cs` | PASS |
| 5 | Generator | `PartialProperty_InternalSetter_PreservesAccessorAndUsesValueAssignment` | `src/Neatoo.BaseGenerator.Tests/PartialPropertyGenerationTests.cs` | PASS |
| 6 | Generator | `PartialProperty_LazyLoadWithPrivateSetter_UsesLoadValue` | `src/Neatoo.BaseGenerator.Tests/PartialPropertyGenerationTests.cs` | PASS |
| 7 | Generator | `PartialProperty_GetOnlyProperty_UnchangedByPrivateSetterFeature` | `src/Neatoo.BaseGenerator.Tests/PartialPropertyGenerationTests.cs` | PASS |
| 8 | Integration | `PrivateSet_RuleComputesValue`, `PrivateSet_TriggersPropertyChanged`, `PrivateSet_IsReadOnlyTrue`, `PrivateSet_PublicPropertyIsReadOnlyFalse` | `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs` | COMPILES (cannot run -- blocked by pre-existing CS1061 in other files) |
| 9 | Integration | `PrivateSet_SetValueThrows` | `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs` | COMPILES (cannot run -- blocked) |
| 10 | Integration | `PrivateSet_LoadValueSucceeds` | `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs` | COMPILES (cannot run -- blocked) |
| 11 | Integration | `PrivateSet_InterfaceExposesGetOnly`, `PrivateSet_SetPrivateValueOnInterface` | `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs` | COMPILES (cannot run -- blocked) |
| 12 | Generator | `PartialProperty_MixedPublicAndPrivateSetters_GeneratesCorrectPatterns` | `src/Neatoo.BaseGenerator.Tests/PartialPropertyGenerationTests.cs` | PASS |
| 13 | Integration | `PrivateSet_SetPrivateValueOnInterface` | `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs` | COMPILES (cannot run -- blocked) |

## Implementation Progress

**Started:** 2026-03-23
**Developer:** neatoo-developer

### Current Status: All Phases Complete

**Phase 1 (API + Generator Changes):**
- All 4 source files modified successfully
- `dotnet build src/Neatoo.sln` -- 0 warnings, 0 errors
- `dotnet test src/Neatoo.sln` -- 2111 passed, 2 skipped, 0 failures

**Phase 2 (Generator Tests):**
- 8 new test methods added to `PartialPropertyGenerationTests.cs`
- `dotnet test src/Neatoo.BaseGenerator.Tests/Neatoo.BaseGenerator.Tests.csproj` -- 40 passed (32 existing + 8 new)
- No changes needed to `GeneratorTestHelper.cs` (NeatooStubs)

**Phase 3 (Design Project + Integration Tests):**
- Verified all 3 pre-added Design project files exist with correct content
- `dotnet build src/Design/Design.Domain/Design.Domain.csproj -p:NoWarn=NF0105` -- Build succeeded. Zero non-NF0105 errors. CS8799 is resolved.
- Generated code verified: `ComputedTotal` has `private set` with `SetPrivateValue(value)`, `Quantity`/`UnitPrice` have public setters with `.Value = value`
- Factory generated correctly: `IPrivateSetPropertyDemoFactory` returns `IPrivateSetPropertyDemo` via `Create`
- Design.Tests cannot run: pre-existing CS1061 errors in other test files block compilation of the entire test project. PrivateSetPropertyTests itself has zero compilation errors.
- `dotnet test src/Neatoo.sln` -- 2119 passed, 2 skipped, 0 failures (final sanity check)

## Completion Evidence

**Completed:** 2026-03-23 (All phases)

### Build Results

**Main solution:**
`dotnet build src/Neatoo.sln` -- Build succeeded. 0 Warning(s). 0 Error(s).

**Design.Domain (with NF0105 suppressed):**
`dotnet build src/Design/Design.Domain/Design.Domain.csproj -p:NoWarn=NF0105` -- Build succeeded. Zero non-NF0105 errors.

**Design.Tests:**
Cannot compile due to pre-existing CS1061 errors in 6 unrelated test files. Zero errors in PrivateSetPropertyTests.

### Test Results

```
Passed!  - Failed:     0, Passed:    40, Skipped:     0, Total:    40 - Neatoo.BaseGenerator.Tests.dll (net9.0)
Passed!  - Failed:     0, Passed:   250, Skipped:     0, Total:   250 - Samples.dll (net9.0)
Passed!  - Failed:     0, Passed:    55, Skipped:     0, Total:    55 - Person.DomainModel.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:  1774, Skipped:     2, Total:  1776 - Neatoo.UnitTest.dll (net9.0)
```

Total: 2119 passed, 0 failed, 2 skipped.

### Design Project Compilation

`dotnet build src/Design/Design.Domain/Design.Domain.csproj -p:NoWarn=NF0105` -- PASS (Build succeeded)
`dotnet build src/Design/Design.Tests/Design.Tests.csproj -p:NoWarn=NF0105` -- FAIL (pre-existing CS1061 errors in other test files, not in PrivateSetPropertyTests)

### Generated Code Verification

Confirmed generated output at `src/Design/Design.Domain/Generated/Neatoo.BaseGenerator/Neatoo.BaseGenerator.PartialBaseGenerator/Design.Domain.PropertySystem.PrivateSetPropertyDemo.g.cs`:

- Line 47-58: `ComputedTotal` has `private set { ComputedTotalProperty.SetPrivateValue(value); ... }` -- CORRECT per Rule 3
- Lines 19-31: `Quantity` has public `set { QuantityProperty.Value = value; ... }` -- CORRECT (public setter unchanged)
- Lines 33-45: `UnitPrice` has public `set { UnitPriceProperty.Value = value; ... }` -- CORRECT (public setter unchanged)

### All Contract Items Verified

All Phase 1+2 items checked. Phase 3 items checked where possible:
- Design project files exist: YES
- Design.Domain compiles: YES
- Generated code correct: YES
- Design.Tests run: BLOCKED by pre-existing issues (not introduced by this work)

### Known Blocker: Design.Tests Integration Tests

The 8 integration tests in `PrivateSetPropertyTests` (scenarios 8-11, 13) cannot be executed because the Design.Tests project has pre-existing compilation errors:
- 202 NF0105 errors in Design.Domain (public `[Remote]` methods) -- when suppressed, leads to:
- 17 CS1061 errors in 6 other Design.Tests files referencing `.Fetch()` on factory interfaces that were never generated

These are entirely pre-existing and unrelated to the private-setter implementation. The PrivateSetPropertyTests code itself compiles cleanly (verified: zero errors in PropertyBasicsTests.cs). The integration tests are ready to run once the pre-existing Design project issues are fixed.

## Developer Deliverable: MarkdownSnippets Sample for Private Setters

**Completed:** 2026-03-23

### What Was Done

1. **Added sample entity** `PropPrivateSetterDemo` to `src/samples/PropertiesSamples.cs`:
   - ValidateBase entity with `Quantity` (int), `UnitPrice` (decimal), and `ComputedTotal` (decimal, private set)
   - `AddAction` rule computing `ComputedTotal = Quantity * UnitPrice`
   - Snippet marker: `// begin-snippet: properties-private-setter-declaration` / `// end-snippet`

2. **Added 3 test methods** in `PropertiesSamplesTests` class:
   - `PrivateSetter_RuleRecomputesValue` -- sets Quantity/UnitPrice, asserts ComputedTotal recomputes
   - `PrivateSetter_IsReadOnly` -- asserts `entity["ComputedTotal"].IsReadOnly == true` and writable properties are false
   - `PrivateSetter_SetValueThrows` -- asserts `entity["ComputedTotal"].SetValue(x)` throws `PropertyException` (using try/catch since `PropertyReadOnlyException` is internal and `Assert.ThrowsAsync` requires exact type match)
   - Snippet marker: `// begin-snippet: properties-private-setter-usage` / `// end-snippet`

3. **Updated `docs/guides/properties.md`** -- Replaced inline code blocks with `<!-- snippet: properties-private-setter-declaration -->` and `<!-- snippet: properties-private-setter-usage -->` references. Also updated "PropertyReadOnlyException" mention to "PropertyException" in the Indexer Behavior section to match the public API.

4. **Ran `dotnet mdsnippets`** -- Both snippets filled correctly in the markdown.

5. **Build and test results:**
   - `dotnet build src/samples/Samples.csproj` -- 0 errors
   - `dotnet test src/samples/Samples.csproj` -- 253 passed, 0 failed (3 new tests)
   - `dotnet test src/Neatoo.sln` -- 2122 passed, 0 failed, 2 skipped (no regressions)

### Files Modified

- `src/samples/PropertiesSamples.cs` -- Added `PropPrivateSetterDemo` entity class and 3 test methods with snippet markers
- `docs/guides/properties.md` -- Replaced inline code blocks with snippet references, updated exception name in Indexer Behavior section

### Note on SetValue Exception Assertion

`SetValue` returns `Task`, so xUnit v3 requires `Assert.ThrowsAsync` (not `Assert.Throws`). However, `Assert.ThrowsAsync<PropertyException>` fails because the actual exception is `PropertyReadOnlyException` (a subclass), and xUnit checks for exact type match. Used try/catch pattern (same as Design.Tests) to catch the public `PropertyException` base class.
