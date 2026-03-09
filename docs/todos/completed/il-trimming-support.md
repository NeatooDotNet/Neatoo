# IL Trimming Support

**Status:** Complete
**Priority:** High
**Created:** 2026-03-07
**Last Updated:** 2026-03-07

**Requirements Review Date:** 2026-03-07

---

## Problem

Neatoo does not currently declare itself as trimmable. Blazor WASM applications using Neatoo cannot benefit from IL trimming to reduce bundle size. Server-only code (internal factory methods, EF dependencies) remains in the client bundle.

## Solution

Add IL trimming support to Neatoo using RemoteFactory 0.20.0's trimming infrastructure:

- Add `<IsTrimmable>true</IsTrimmable>` to Neatoo's library projects (per v0.18.0 guidance)
- Ensure generated code uses trim-safe APIs (v0.19.0 fixes trim-safe ordinal serialization)
- Verify no IL2026 or other trimming warnings with `TreatWarningsAsErrors`
- Document how consuming Blazor WASM apps enable trimming via the `NeatooRuntime.IsServerRuntime` feature switch

Depends on the internal-factory-methods todo — `internal` methods are what the trimmer removes.

Reference: RemoteFactory v0.17.0 (feature switch), v0.18.0 (IsTrimmable), v0.19.0 (trim-safe serialization)

---

## Clarifications

**Q1: Scope of "library projects" — should Neatoo.csproj and/or MudNeatoo.csproj be marked trimmable?**
A: Neither. Just the consumer libraries like Person.DomainModel. For Neatoo, we just want to ensure the Neatoo types aren't the problem. If the consuming library has a problem they can use attributes to keep their types from being trimmed.

**Q2: JSON serializer reflection (NeatooBaseJsonTypeConverter) — how to handle trim warnings?**
A: Look at the RemoteFactory repository (`C:\src\neatoodotnet\RemoteFactory`), we dealt with some of these.

**Q3: PropertyInfoList reflection — should type parameter get [DynamicallyAccessedMembers]?**
A: I don't know.

**Q4: Verification scope — include actual `dotnet publish` with trimming?**
A: Yes, include publish.

**Q5: TFM targeting — both net9.0 and net10.0?**
A: Yes.

**Additional context:** RemoteFactory generated code registers types, which lets the trimmer know to keep them. The goal is to ensure Neatoo framework types aren't the problem — consuming libraries handle their own trimming issues with attributes.

---

## Requirements Review

**Reviewer:** neatoo-requirements-reviewer
**Reviewed:** 2026-03-07
**Verdict:** APPROVED (with constraints)

### Relevant Requirements Found

**Source 1: Design Projects (Design.Domain / Design.Tests / CLAUDE-DESIGN.md)**

1. **Serialization behavioral contracts are the highest-risk area.**
   `src/Design/CLAUDE-DESIGN.md` "Serialization Considerations" section (lines 401-528) defines what survives serialization round-trips. The JSON converters (`NeatooBaseJsonTypeConverter`, `NeatooListBaseJsonTypeConverter`) are the mechanism that implements these contracts. These converters are heavily reflection-based. Any trimming annotation changes to these converters must not alter serialization behavior.

2. **Constructor-injected services survive serialization; method-injected services do not.**
   `src/Design/CLAUDE-DESIGN.md` line 96: "Constructor-injected services survive serialization round-trips (available on client and server). Method-injected services are server-only and not serialized." Trimming annotations must not cause the trimmer to remove types needed for DI resolution of constructor-injected services.

3. **Internal child factory methods are already documented as "trimmable on client."**
   `src/Design/Design.Domain/FactoryOperations/FetchPatterns.cs` line 271: "Child persistence methods are internal: server-only, trimmable on client." `src/Design/Design.Domain/CommonGotchas.cs` lines 192, 461: Same comment on Gotcha2Item and Gotcha5Child. The internal-factory-methods todo (completed) formalized this. The IL trimming todo is the natural continuation -- enabling the trimmer to actually remove these methods.

4. **Property system uses reflection to discover properties at startup.**
   `src/Neatoo/Internal/PropertyInfoList.cs` uses `Type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)` to discover all properties on the concrete entity type and walk up the inheritance chain. This happens once per type (cached via static `isRegistered` flag). The trimmer could remove properties it considers unreferenced if `[DynamicallyAccessedMembers]` is not applied to the type parameter.

5. **LazyLoad property discovery uses reflection.**
   `src/Neatoo/ValidateBase.cs` lines 310-317: `GetLazyLoadProperties()` uses `Type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)` to find all `LazyLoad<>` properties. This is cached in a `ConcurrentDictionary` per type. The same reflection pattern is used in `NeatooBaseJsonTypeConverter.cs` for serialization (lines 116-121 for reading, lines 343-355 for writing).

6. **Validation attribute rules use reflection to read custom attributes.**
   `src/Neatoo/Internal/PropertyInfoWrapper.cs` line 27: `PropertyInfo.GetCustomAttribute<T>()` reads validation attributes. `src/Neatoo/Rules/RuleManager.cs` lines 396-413: `AddAttributeRules()` iterates property attributes and converts them to rules. If the trimmer removes attribute metadata, validation rules from `[Required]`, `[StringLength]`, etc. would silently stop working.

7. **RequiredRule uses Activator.CreateInstance for default value comparison.**
   `src/Neatoo/Rules/Rules/RequiredRule.cs` line 46: `value.Equals(Activator.CreateInstance(value.GetType()))` creates a default instance of the value type to compare against. This is trim-unsafe for value types whose constructors might be trimmed.

8. **JSON converter factory uses MakeGenericType.**
   `src/Neatoo/RemoteFactory/Internal/NeatooJsonConverterFactory.cs` lines 42, 47, 51: `typeof(NeatooBaseJsonTypeConverter<>).MakeGenericType(typeToConvert)` and similar for list and interface converters. `MakeGenericType` is inherently trim-unsafe because the trimmer cannot predict which type arguments will be used at runtime.

9. **JSON deserialization uses Activator.CreateInstance.**
   `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` line 84: `Activator.CreateInstance(type, [])` as fallback when DI resolution fails. Lines 257-264: `Activator.CreateInstance(propertyType, ...)` for `ValidateProperty<T>` and `EntityProperty<T>` deserialization. The trimmer may remove constructors it considers unused.

10. **ServiceAssemblies.FindType uses Assembly.GetTypes() at startup.**
    `C:\src\neatoodotnet\RemoteFactory\src\RemoteFactory\Internal\ServiceAssemblies.cs` line 29: `assemblies.SelectMany(a => a.GetTypes())` scans all types in registered assemblies. This is the type resolution mechanism used by JSON converters to find types by `$type` full name during deserialization. This is in RemoteFactory, not Neatoo, but is consumed by Neatoo's converters. The trimmer could remove types from the consumer assembly that are only referenced via this runtime scan.

**Source 2: Framework Source Comments (src/Neatoo/)**

11. **No DESIGN DECISION markers in Neatoo source code relate to trimming or reflection strategy.** The framework source does not contain explicit design decisions constraining or guiding trimming annotations. The reflection usage is "incidental" -- it exists because it was the natural approach, not because a deliberate decision was made to use reflection over alternatives.

12. **The "no reflection" aspiration in CLAUDE.md.**
    `CLAUDE.md` global instructions: "The goal is to have no reflection, even in tests." This aspiration conflicts with the current Neatoo codebase reality, which has extensive reflection in the serializer, property system, rule system, and LazyLoad discovery. The todo does not propose removing reflection -- it proposes annotating existing reflection to be trim-safe. This is consistent with the user's clarification: "we just want to ensure the Neatoo types aren't the problem."

**Source 3: User-Facing Documentation (docs/)**

13. **The completed internal-factory-methods todo documents the trimming enablement.**
    `docs/todos/completed/internal-factory-methods.md` line 177: "Generated code shows IsServerRuntime guards on all internal method implementations, enabling IL trimming on the client." This confirms the prerequisite is met.

14. **No existing Neatoo release notes mention IL trimming.** The feature is new to Neatoo (though RemoteFactory has had trimming support since v0.17.0).

**Source 4: Skills (skills/neatoo/, RemoteFactory trimming skill)**

15. **RemoteFactory trimming skill defines the setup pattern.**
    `C:\src\neatoodotnet\RemoteFactory\skills\RemoteFactory\references\trimming.md` lines 22-28: Domain model projects should add `<IsTrimmable>true</IsTrimmable>`. However, the user's Clarification A1 says Neatoo.csproj itself should NOT be marked trimmable -- only consumer libraries like Person.DomainModel.

16. **RemoteFactory trimming skill documents RegisterMatchingName as trim-unsafe.**
    `C:\src\neatoodotnet\RemoteFactory\skills\RemoteFactory\references\trimming.md` line 83: "RegisterMatchingName uses reflection (assembly.GetTypes()) at runtime. The trimmer cannot see these references and may trim types only registered through convention."

17. **Neatoo skill entities reference documents internal method trimming.**
    `skills/neatoo/references/entities.md` lines 682-745: "Child Entity Factory Method Visibility" section documents that internal methods get `IsServerRuntime` guards and are "trimmable on the client."

18. **Neatoo skill pitfalls reference has no trimming-related entries.** This is a gap that should be addressed after implementation.

### Gaps

1. **No existing requirement defines what "trim-safe" means for Neatoo.** The todo proposes ensuring "no IL2026 or other trimming warnings," but the scope of what constitutes "trim-safe" for Neatoo is undefined. Key question: should Neatoo guarantee that its serialization works correctly after consumer assembly trimming, or just that Neatoo itself compiles without trim warnings? The user's clarification suggests the latter: "we just want to ensure the Neatoo types aren't the problem." The architect must make this scope explicit.

2. **No existing test verifies behavior after trimming.** Design.Tests tests serialization round-trips indirectly (via factory Fetch/Save operations), but no test verifies that these operations work after IL trimming has run. The user requested `dotnet publish` verification (Q4), but there is no framework for automated trim-and-verify testing in Neatoo.

3. **No documented strategy for suppressing vs. fixing trim warnings.** When a reflection call cannot be made trim-safe (e.g., `Activator.CreateInstance` with runtime-determined types), the architect must decide: suppress with `[UnconditionalSuppressMessage]` and document the risk, or refactor to avoid the reflection. There is no existing policy.

4. **The todo's Solution section contradicts the Clarifications.** The Solution says "Add `<IsTrimmable>true</IsTrimmable>` to Neatoo's library projects (per v0.18.0 guidance)" but Clarification A1 says "Neither" when asked if Neatoo.csproj should be marked trimmable. The architect must reconcile: is the goal to (a) mark consumer domain model projects as trimmable and ensure Neatoo doesn't block them, or (b) mark Neatoo itself as trimmable? These have very different scopes.

5. **No documentation exists for how Neatoo's reflection-heavy JSON converters interact with consumer assembly trimming.** When Person.DomainModel marks itself `<IsTrimmable>true</IsTrimmable>`, the trimmer may remove types from Person.DomainModel that are only referenced at runtime via `IServiceAssemblies.FindType()`. Neatoo's converters resolve types by `$type` full name string. The RemoteFactory generated code creates static references that preserve types, but Neatoo's own converter registration (in `AddNeatooServices`) uses open generic types (`typeof(NeatooBaseJsonTypeConverter<>)`) which the trimmer handles differently.

### Contradictions

None found that would warrant a VETO. The internal contradiction between the Solution section and Clarification A1 is an ambiguity to resolve, not a conflict with existing requirements.

### Recommendations for Architect

1. **Resolve the scope ambiguity first.** The Solution section says "mark Neatoo's library projects" but the clarification says "neither Neatoo.csproj nor MudNeatoo.csproj." Recommend interpreting the scope as: ensure Neatoo does not produce IL2026/IL2104 warnings when _consumed by_ a trimmable assembly (Person.DomainModel with `<IsTrimmable>true</IsTrimmable>`). This means adding trimming annotations to Neatoo's public APIs and internal reflection sites, but NOT adding `<IsTrimmable>true</IsTrimmable>` to Neatoo.csproj.

2. **Catalog every reflection site in Neatoo before planning.** Based on this review, there are at least 7 distinct reflection patterns to address:
   - `PropertyInfoList<T>.RegisterProperties()` -- `Type.GetProperties()` walk
   - `ValidateBase<T>.GetLazyLoadProperties()` -- `Type.GetProperties()` for LazyLoad
   - `NeatooBaseJsonTypeConverter<T>.Read()` -- `GetProperties()`, `Activator.CreateInstance`, `MakeGenericType`
   - `NeatooBaseJsonTypeConverter<T>.Write()` -- `typeof(IEntityMetaProperties).GetProperties()`, `property.GetValue()`
   - `NeatooJsonConverterFactory.CreateConverter()` -- `MakeGenericType`
   - `PropertyInfoWrapper.GetCustomAttribute<T>()` -- `PropertyInfo.GetCustomAttribute`
   - `RequiredRule<T>.Execute()` -- `Activator.CreateInstance(value.GetType())`

3. **Look at RemoteFactory for patterns.** The user's Clarification A2 points to RemoteFactory for how they handled similar issues. Key references:
   - `C:\src\neatoodotnet\RemoteFactory\docs\todos\completed\fix-ordinal-trimming-errors.md` -- how they fixed IL2026 in generated code
   - RemoteFactory v0.19.0 release notes -- switched from `JsonSerializer.Serialize<T>()` to `GetTypeInfo`-based overloads
   - `C:\src\neatoodotnet\RemoteFactory\src\Tests\RemoteFactory.TrimmingTests/` -- an existing test project that verifies trimming behavior (could serve as a template for Neatoo)

4. **Consider the `[UnconditionalSuppressMessage]` approach for inherently trim-unsafe patterns.** Some of Neatoo's reflection (e.g., `Activator.CreateInstance` with runtime-determined types in the JSON converter) cannot be made fully trim-safe without a fundamental redesign. The pragmatic approach is to suppress the warnings with `[UnconditionalSuppressMessage("Trimming", "IL2026", ...)]` and document that Neatoo's serializer requires types to be preserved by the consumer (which RemoteFactory's generated code already does via static references in `FactoryServiceRegistrar`).

5. **Verify against Design project serialization behavior after implementation.** `src/Design/CLAUDE-DESIGN.md` lines 401-460 define what survives serialization. After adding trimming annotations, verify that the serialization contracts are still met by running Design.Tests and the Person example tests.

6. **The `dotnet publish` verification (Clarification A4) should use the Person example.** `src/Examples/Person/` is a full application with client/server architecture. Publishing the client project with trimming enabled and verifying the output is the most realistic verification approach. RemoteFactory has an existing trimming test project (`RemoteFactory.TrimmingTests`) that could serve as a template.

7. **`TreatWarningsAsErrors` is already `True` in `Directory.Build.props`.** This means any IL2026 warnings introduced by adding trimming annotations would immediately become build errors. The architect should be aware that the NoWarn list in `Directory.Build.props` does NOT currently suppress any IL/SYSLIB trimming warnings (only CA, CS, IDE, and SYSLIB0051 are suppressed). Adding `<IsTrimmable>true</IsTrimmable>` to Neatoo.csproj would immediately surface all existing trim warnings as errors.

8. **Update the Neatoo skill after implementation.** Add trimming guidance to `skills/neatoo/references/pitfalls.md` and potentially create a `skills/neatoo/references/trimming.md` reference file.

---

## Plans

- [IL Trimming Support Design](../plans/il-trimming-support-design.md)

---

## Tasks

- [x] Architect comprehension check
- [x] Business requirements review
- [x] Architect plan creation & design
- [x] Developer review
- [x] Implementation
- [x] Verification
- [x] Documentation
- [x] Completion

---

## Progress Log

### 2026-03-07
- Created todo after upgrading RemoteFactory from 0.16.1 to 0.20.0
- Build and all tests pass with the upgrade
- Depends on internal-factory-methods todo (internal methods are what gets trimmed)
- Architect plan created at `docs/plans/il-trimming-support-design.md`. Three-tier approach: (1) annotate public APIs with `[DynamicallyAccessedMembers]` to preserve property discovery, (2) suppress inherently trim-unsafe patterns in JSON converters with `[UnconditionalSuppressMessage]` and documented rationale, (3) verify with Person example app `dotnet publish` with trimming. 12 business rules, 6 test scenarios. Ready for developer review.
- Developer review: Approved with 6 minor concerns (1 substantive about GetType() not propagating DynamicallyAccessedMembers — resolved with method-level suppression). Implementation contract created.
- Implementation: All 3 phases complete. 14 type parameters annotated with [DynamicallyAccessedMembers], 19 [UnconditionalSuppressMessage] attributes on 12 methods, EnableTrimAnalyzer added to Neatoo.csproj, Person.DomainModel configured as trimmable, Person.App configured for trimmed publish.
- Build: 0 errors, 0 warnings. Tests: 2,083 passed (Neatoo.sln) + 90 passed (Design.Tests), 0 failed.
- Publish: Person.App with trimming succeeds. Size reductions: Person.DomainModel 93%, Neatoo 58%, Person.App 66%.
- Architect verification: VERIFIED.
- Requirements verification: REQUIREMENTS SATISFIED.
- Requirements documentation: New skills/neatoo/references/trimming.md created, entities.md and SKILL.md updated, release notes v0.18.0 created, docs/index.md updated.
- Todo completed.

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors, 0 warnings (with EnableTrimAnalyzer=true)
- Tests: 2,083 passed (Neatoo.sln), 90 passed (Design.Tests), 0 failed

---

## Results / Conclusions

Added IL trimming support to Neatoo framework. Consumer domain model projects can now set `<IsTrimmable>true</IsTrimmable>` and Blazor WASM apps can publish with `<PublishTrimmed>true</PublishTrimmed>` without Neatoo causing trim warnings. The approach uses `[DynamicallyAccessedMembers]` annotations on 14 type parameters to preserve property discovery, and `[UnconditionalSuppressMessage]` on 12 inherently trim-unsafe methods (JSON converters, RequiredRule) with documented justification that RemoteFactory's generated FactoryServiceRegistrar roots all consumer types. Neatoo.csproj itself is not marked trimmable — only consumer libraries. Verified with Person example: Person.DomainModel reduced 93%, Neatoo 58%, Person.App 66%.
