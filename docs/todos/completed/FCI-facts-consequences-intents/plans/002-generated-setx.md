# Generated awaitable SetX on class and interface

**Plan #:** 002
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-2
**Status:** Done
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (the shape was decided by Keith in the design session; public API change already ruled, D28)
**Code-review opt-in:** Yes (generator change that every consumer compiles)
**Branch:** fci-002-generated-setx
**PR:** #116

---

## Scope

The base generator emits `Task SetX(T value)` for every settable partial property: on the class with the setter's semantics, including the private-set bypass of `IsReadOnly`, returning the whole cascade's Task; and on the generated partial `I{ClassName}` interface for public-set properties only. Generator tests pin the shape; integration tests pin that awaiting it waits for the nested cascade, async rules included, and that an awaited write re-entering the rule manager from inside a rule completes in order. Does not add the analyzer, change any existing rule, or make any Design interface partial.

## Intent

- Domain code gets a typed, awaitable write with no `nameof` and no boxing, reachable through the interface every other type holds.
- The awaited write covers the consequences of the write, which the forking setter cannot.

## Framework & Architectural Alignment

- VisionAlignment D28; interface-first (D19): a generated member reaches other types only through the interface.
- The generator's existing partial-interface path (`I{ClassName}`, declared `partial`) is the one mechanism; no new opt-in.

## Constraints & Invariants

- The generated setter is unchanged; every existing test stays green.
- No generated member collides with a hand-written one: a class or base that declares any member named `Set{Name}` gets no generated `SetX` for that property.
- `init`, get-only and lazy-load properties get no `SetX`.
- `SetX` tracks its task exactly as the setter does, so `WaitForTasks` and `IsBusy` are unchanged for a caller that does not await.

## Steps

1. Extend the property model with what the generator needs to decide: init accessor, an existing `Set{Name}` member on the class or a base, and whether the partial interface already declares one.
2. Emit `SetX` on the class at the setter's accessibility, with the setter's write path and task tracking, returning the cascade's Task.
3. Emit `SetX` on the partial interface for public-set properties.
4. Generator tests for the shapes and the skips; integration tests for the awaited cascade, the private-set bypass and the interface.

## Acceptance

- [x] A public-set partial property gets a public `SetX` on the class, and on the partial interface when there is one `[unit]`
- [x] A private-set property gets a private `SetX` that writes past `IsReadOnly`, and none on the interface `[integration]`
- [x] Get-only, init, lazy-load and colliding properties get no `SetX` `[unit]`
- [x] Awaiting `SetX` returns only after the property's async rules and its parent's async rules have finished `[integration]`
- [x] Inside an async rule, an awaited `SetX` whose own cascade runs an async rule completes before the next statement `[integration]`
- [x] A rule class typed on the interface writes through the interface's `SetX` `[integration]`
- [x] Build and all tests green `[explicit-skip: meta-bullet]`

## Current State (Pre-Flight)

- `PropertyGenerator.cs:43-63` emits the setter: `XProperty.Value = value` (public, protected, internal) or `XProperty.SetPrivateValue(value)` (private), then `Parent?.AddChildTask` and `RunningTasks.AddTask` when the task is not complete. `:74-86` emits the partial interface's property declarations when `HasPartialInterface`.
- `PropertyExtractor.cs:22-84` builds `PartialPropertyInfo`; `HasSetter` counts `init` as a setter; `GetExistingInterfaceProperties` reads only property members.
- `ValidateProperty.SetValue` throws on `IsReadOnly` and returns `SetPrivateValue`'s Task, which is the awaited nested cascade (`ValidateProperty.cs:144-181,317-330`; `ValidateBase.cs:381-389`).
- No hand-written `Set{Name}` collides in Neatoo or Design; `DefaultFactoryTests.SetReadOnlyProperty` is a test helper and collides with no property of that name. zTreatment has 375 `Set*` methods; the collision skip covers any that match a property name.
- Generator tests assert generated text over stubs; the stubs do not compile generated code.

## Test Evidence

Flip check: with the generated SetX returning a completed Task instead of the cascade, the three cascade tests failed (SetX_Awaited_ReturnsAfterChildAndParentAsyncRules, SetX_InsideAsyncRule_CompletesNestedCascadeBeforeNextStatement, SetX_PrivateSet_WritesPastIsReadOnly); restored, all pass.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Public set: SetX on class and partial interface | Required | `[unit]` | `Neatoo.BaseGenerator.Tests.AwaitableSetterGenerationTests.PublicSet_GeneratesPublicSetterOnClassAndInterface`, `.ProtectedAndInternalSet_KeepTheirAccessibility_NotOnInterface`, `.InterfaceAlreadyDeclaresSetter_NotRedeclared`, `.NonPartialInterface_ClassSetterOnly` | ✓ |
| Private set: private SetX past IsReadOnly, not on interface | Required | `[integration]` | `Neatoo.UnitTest.Integration.Concepts.AwaitableSetter.AwaitableSetterTests.SetX_PrivateSet_WritesPastIsReadOnly`; shape in `AwaitableSetterGenerationTests.PrivateSet_GeneratesPrivateSetterOnClassOnly` | ✓ |
| Get-only, init, lazy-load, colliding: no SetX | Required | `[unit]` | `AwaitableSetterGenerationTests.GetOnlyInitAndLazyLoad_GenerateNoSetter`, `.HandWrittenSetMember_GeneratesNoSetter`, `.InterfaceDeclaresPropertyGetOnly_NoSetterOnInterface` | ✓ |
| Awaited SetX returns after child and parent async rules | Required | `[integration]` | `AwaitableSetterTests.SetX_Awaited_ReturnsAfterChildAndParentAsyncRules`; contrast `AwaitableSetterTests.Setter_Assigned_ReturnsBeforeAsyncConsequencesLand` | ✓ |
| Awaited SetX inside an async rule completes before the next statement | Required | `[integration]` | `AwaitableSetterTests.SetX_InsideAsyncRule_CompletesNestedCascadeBeforeNextStatement` | ✓ |
| Rule class typed on the interface writes through it | Required | `[integration]` | `NoteFollowsTotalRule` asserted in `SetX_Awaited_ReturnsAfterChildAndParentAsyncRules`; `AwaitableSetterTests.SetX_ThroughInterface_IsTheTypedWrite` | ✓ |
| Build and all tests green | Required | `[explicit-skip]` | `reviews/002-build.log`, `002-design-build.log`, `002-test.log` | ✓ |

## Gate Record

- Round 1 (2026-10-16): APPROVED, no findings — `reviews/002-test-review.md`. Code review APPROVED, one Medium callout fixed (get-only interface property gets no SetX) — `reviews/002-code-review.md`.
- Round 2: not needed; full suite re-run green after the fix — Done

## Plan Amendments

### 2026-10-16 — A collision is skipped silently

- **Section affected:** Constraints
- **Original said:** nothing on reporting.
- **What changed:** a skipped `SetX` raises no generator diagnostic; FCI-003's analyzer reports it at the assignment, where the author needs it.
- **Why:** the generator's model is location-free and equatable for incremental caching; a diagnostic needs a location.
- **Discovery Log:** n/a (detail, not intent)
