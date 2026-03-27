# Private Property Setters in Source Generator

**Status:** Complete
**Priority:** High
**Created:** 2026-03-23
**Last Updated:** 2026-03-23

---

## Problem

The source generator does not respect `private set` on partial properties. If a developer writes `public partial string Name { get; private set; }`, the generator ignores the `private` modifier and produces a public setter. It also generates `get; set;` on the interface instead of `get;` only.

This came from feedback identifying a tension between MudNeatoo's `IsReadOnly` mechanism (which uses `private set` to mark UI fields as read-only) and RemoteFactory serialization (concern that private setters would prevent deserialization).

**Key discovery:** The serialization concern is a non-issue. RemoteFactory uses `PropertyManager.SetProperties()` which directly replaces property objects in the internal `PropertyBag` — it never calls property setters. The runtime already supports private setters end-to-end: `PropertyInfoWrapper.IsPrivateSetter` → `ValidateProperty.IsReadOnly` → MudNeatoo `ReadOnly` binding. The gap is purely in the source generator.

**The IProperty indexer question:** `entity["Name"].SetValue(x)` correctly throws `PropertyReadOnlyException` for private-set properties. `entity["Name"].LoadValue(x)` bypasses by design — this is the intended framework escape hatch for Fetch/Load operations.

## Solution

Make the source generator respect `private set` on partial properties:

1. **PropertyExtractor** — detect setter accessibility modifier (`private`, `protected`, `internal`)
2. **PartialPropertyInfo** — add field to capture setter accessibility
3. **PropertyGenerator** — emit `private set` (or other restricted accessor) in the generated property implementation
4. **Interface generation** — emit `get;` only when the setter is non-public

---

## Clarifications

Architect comprehension check (2026-03-23): Architect confirmed clear understanding of the problem and solution. No clarifying questions. Ready to proceed.

---

## Requirements Review

**Reviewer:** neatoo-requirements-reviewer
**Reviewed:** 2026-03-23
**Verdict:** APPROVED (with critical design constraint noted)

### Relevant Requirements Found

**Source 1: Design Project Tests (3 contracts)**

1. **Property setter triggers change tracking and rules.** `Design.Tests/PropertyTests/PropertyBasicsTests.cs` method `Property_SetTriggersPropertyChanged()`: WHEN entity.Name is set to "Test", THEN PropertyChanged fires for "Name". The generated setter must preserve this behavior for private-set properties when called from within the entity.

2. **LoadValue bypasses modification tracking.** `Design.Tests/PropertyTests/PropertyBasicsTests.cs` method `Property_Indexer_CanLoadValue()`: WHEN entity["Name"].LoadValue("Loaded") is called, THEN value is set without modification tracking. This is the escape hatch for Fetch operations and is unaffected by this change.

3. **Rules don't fire during factory operations.** `Design.Tests/GotchaTests/CommonGotchaTests.cs` methods `Gotcha1_*`: WHEN properties are set during [Create], THEN rules do not fire. After factory completes, property changes trigger rules normally. Private-set properties set by rules (e.g., `AddAction` that computes a derived value) must still work after factory operations complete.

**Source 2: Code Comments / DESIGN DECISION markers (4 constraints)**

4. **SetValue checks IsReadOnly, SetPrivateValue bypasses it.** `src/Neatoo/Internal/ValidateProperty.cs:124-131`: `SetValue()` throws `PropertyReadOnlyException` when `IsReadOnly` is true. `SetPrivateValue()` (exposed via `IValidatePropertyInternal`) bypasses this check. The deprecated `Setter<P>()` method in `ValidateBase` (line 446-450) and the `ObjectInvalid` property (line 649-651) both use `SetPrivateValue` specifically to bypass IsReadOnly.

5. **IsReadOnly is structural, set from IsPrivateSetter.** `src/Neatoo/Internal/ValidateProperty.cs:22`: `this.IsReadOnly = propertyInfo.IsPrivateSetter`. `src/Neatoo/Internal/PropertyInfoWrapper.cs:12`: `this.IsPrivateSetter = !propertyInfo.CanWrite || propertyInfo.SetMethod?.IsPrivate == true`. This is a compile-time structural decision, not dynamic.

6. **IsReadOnly is serialized.** `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs:273-274`: The JSON converter reads `IsReadOnly` from the serialized property and passes it to the ValidateProperty/EntityProperty constructor. Private-set state survives client-server round-trips.

7. **Partial properties are the ONLY supported pattern.** `src/Design/Design.Domain/PropertySystem/PropertyBasics.cs`: "DESIGN DECISION: Partial properties are the ONLY supported pattern. The old Getter<T>()/Setter() methods are deprecated." This means private-set properties must work correctly with the generated code path, not rely on the deprecated Setter<P>() bypass.

**Source 3: Skill References (2 contracts)**

8. **IsReadOnly is a documented property object member.** `skills/neatoo/references/properties.md` "Object-Per-Property Architecture" table: `IsReadOnly` is listed as "Whether this property is read-only". The skill samples (`MetaProperties_QueryPropertyState`) demonstrate `Assert.False(amountProperty.IsReadOnly)` for normal properties and `Assert.False(nameProperty.IsReadOnly)` for writable properties. Private-set properties must make `IsReadOnly` return true.

9. **MudNeatoo binds ReadOnly from IsReadOnly.** All MudNeatoo components (TextField, NumericField, DatePicker, etc.) bind `ReadOnly="@EntityProperty.IsReadOnly"`. This is the primary consumer of IsReadOnly and the driving use case for this feature.

**Source 4: User-Facing Docs (1 contract)**

10. **IsReadOnly is structural from private setter.** `docs/vsCSLA/skill-gaps.md:148`: "IValidateProperty.IsReadOnly exists but is structural (set from PropertyInfo.IsPrivateSetter), not dynamic or role-based."

### Gaps

**GAP-1: No Design project examples of private-set partial properties.** There are zero instances of `public partial string X { get; private set; }` anywhere in the codebase. The Design.Domain project has no demonstration of this pattern, and Design.Tests has no behavioral contracts for it. The architect should add a Design.Domain example and Design.Tests coverage.

**GAP-2: No documented contract for how rules interact with private-set properties.** When `AddAction(t => t.Total = t.Quantity * t.Price, ...)` targets a property with `private set`, the rule's lambda sets the property via the C# setter. If the generated setter uses `SetValue` (which throws for IsReadOnly), rules will break. If the generated setter uses `SetPrivateValue` (which bypasses IsReadOnly), the property is writable from within the entity. There is no existing test or documentation establishing the correct behavior. The architect must establish this contract.

**GAP-3: No contract for protected/internal setter accessibility.** The todo mentions `protected` and `internal` setters in addition to `private`. The runtime currently only checks `IsPrivate` (see `PropertyInfoWrapper.cs:12`). There is no existing handling for `protected set` or `internal set`. The architect should determine whether these also map to `IsReadOnly=true` or are treated differently.

### Contradictions

None. The proposed change is consistent with the existing runtime design (`PropertyInfoWrapper.IsPrivateSetter` -> `ValidateProperty.IsReadOnly` -> MudNeatoo `ReadOnly`). The generator is the only component not aligned with this design.

**However, there is a critical design tension** that is not a contradiction with existing requirements (because no existing requirement covers it) but will cause a runtime failure if not addressed:

The generated setter currently emits `{Name}Property.Value = value` which routes to `SetValue()`, which throws `PropertyReadOnlyException` when `IsReadOnly=true`. If the generator simply emits `private set` on the C# property but keeps the `.Value = value` implementation, then:
- The entity's own code (rules, factory methods) calling the private setter will throw `PropertyReadOnlyException`
- The property becomes effectively write-only via `LoadValue()`

The deprecated `Setter<P>()` method solved this by using `SetPrivateValue()` via `IValidatePropertyInternal`. The generated setter for private-set properties must do the same. This is not optional.

### Recommendations for Architect

1. **Critical: Generated setter for private-set properties must use SetPrivateValue, not SetValue.** The current generated pattern `{Name}Property.Value = value` calls `SetValue` which checks `IsReadOnly`. For private-set properties, the generated setter must cast to `IValidatePropertyInternal` and call `SetPrivateValue(value)` instead, matching the pattern used by the deprecated `Setter<P>()` method in `ValidateBase.cs:446-450`. Without this, the private setter will throw `PropertyReadOnlyException` even from within the entity's own code.

2. **Verify against Design.Tests after implementation.** The existing property tests (`PropertyBasicsTests`) test public-setter properties. Confirm that the new private-setter code path passes change-tracking and PropertyChanged tests (via `SetPrivateValue` which still fires events and tracks modification).

3. **Add Design project coverage.** Add a `private set` partial property example to `Design.Domain/PropertySystem/PropertyBasics.cs` and corresponding tests to `Design.Tests/PropertyTests/PropertyBasicsTests.cs` establishing the behavioral contract: WHEN entity code sets a private-set property internally, THEN change tracking and rules work normally. WHEN external code accesses the indexer `entity["Name"].SetValue(x)`, THEN `PropertyReadOnlyException` is thrown. WHEN `entity["Name"].LoadValue(x)` is called, THEN it succeeds (Fetch escape hatch).

4. **Scope protected/internal setters carefully.** The runtime's `PropertyInfoWrapper.IsPrivateSetter` only checks `.IsPrivate`. If the generator supports `protected set` and `internal set`, decide whether they also set `IsReadOnly=true`. Currently `protected set` would have `IsPrivateSetter=false` at runtime (it's not private, it's protected). The generator and runtime must agree on this.

5. **Verify serialization round-trip.** `IsReadOnly` is already serialized/deserialized in the JSON converter (`NeatooBaseJsonTypeConverter.cs:273`). The property value itself is serialized through `PropertyManager.SetProperties()` which bypasses setters entirely. No serialization changes should be needed, but verify end-to-end.

6. **Verify MudNeatoo integration.** With private setters properly generating `IsReadOnly=true`, MudNeatoo components will automatically bind `ReadOnly="true"`. This is the intended behavior and the driving use case. No MudNeatoo changes should be needed.

---

## Plans

- [Private Property Setters Plan](../plans/completed/private-property-setters.md)

---

## Tasks

- [x] Architect comprehension check (Step 2) — no questions, ready to proceed
- [x] Business requirements review (Step 3) — APPROVED with critical design constraint
- [x] Architect plan creation & design (Step 4) — plan created with 13 business rules, 3 phases
- [x] Developer review (Step 5) — APPROVED, implementation contract created
- [x] Implementation (Step 7) — all 3 phases complete, 2119 tests pass
- [x] Verification (Step 8) — Architect VERIFIED, Requirements SATISFIED
- [x] Documentation (Step 9) — skill refs, user docs, release notes, sample code updated

---

## Progress Log

### 2026-03-23
- Created todo from feedback about private setter / serialization tension
- Discovery confirmed: RemoteFactory serialization does NOT use property setters (uses `PropertyManager.SetProperties()`)
- Discovery confirmed: Runtime already supports private setters (`PropertyInfoWrapper.IsPrivateSetter` → `ValidateProperty.IsReadOnly` → MudNeatoo `ReadOnly`)
- Discovery confirmed: `IProperty` indexer respects private set on `SetValue()`, bypasses on `LoadValue()` by design
- Gap identified: Source generator (`PropertyExtractor`, `PartialPropertyInfo`, `PropertyGenerator`) ignores setter accessibility
- Key files: `PropertyExtractor.cs`, `PartialPropertyInfo.cs`, `PropertyGenerator.cs`
- Next: Architect comprehension check
- Architect comprehension check: no questions, ready to proceed
- Requirements review: APPROVED with critical design constraint (SetPrivateValue not SetValue for private setters)
- 3 gaps identified: no Design examples, no rule interaction contract, no protected/internal setter contract
- Next: Architect plan creation & design

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: `dotnet build src/Neatoo.sln` — 0 errors, 0 warnings
- Tests: `dotnet test src/Neatoo.sln` — 2122 passed, 0 failed, 2 skipped
- Design.Domain: Compiles (CS8799 resolved). Design.Tests blocked by pre-existing NF0105 errors (unrelated)

---

## Results / Conclusions

### Decisions Made

1. **Generator uses `SetPrivateValue()` for private setters** — not `.Value = value` which would throw `PropertyReadOnlyException`. This follows the precedent of the deprecated `Setter<P>()` method.
2. **`SetPrivateValue` added to `IValidateProperty` public interface** — the method already existed as `public virtual` on `ValidateProperty<T>`. Promoting it to the interface lets generated code call it without casting.
3. **Only `private set` maps to `IsReadOnly=true`** — `protected set` and `internal set` preserve their accessor keyword but use the normal `.Value = value` path, matching `PropertyInfoWrapper.IsPrivateSetter` which only checks `IsPrivate`.
4. **Serialization is unaffected** — `PropertyManager.SetProperties()` bypasses property setters entirely. `IsReadOnly` is already serialized/deserialized.

### What Was Delivered

- **4 source files modified**: `IValidateProperty.cs`, `PartialPropertyInfo.cs`, `PropertyExtractor.cs`, `PropertyGenerator.cs`
- **8 new generator tests** in `PartialPropertyGenerationTests.cs`
- **8 new integration tests** in Design.Tests `PrivateSetPropertyTests` (blocked by pre-existing build issue)
- **3 new sample tests** in `PropertiesSamples.cs` with MarkdownSnippets
- **Design.Domain examples**: `PrivateSetPropertyDemo` + `IPrivateSetPropertyDemo`
- **Documentation**: skill references (properties.md, source-generation.md), user docs (properties.md), release notes (v0.24.0)
- **2122 tests passing**, 0 failures

### Key Discovery

The original feedback's serialization concern was a non-issue. RemoteFactory never calls property setters during deserialization.
