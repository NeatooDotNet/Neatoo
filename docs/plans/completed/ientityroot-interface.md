# IEntityRoot Interface — Architectural Plan

**Date:** 2026-03-01
**Related Todo:** [Make IsSavable More Intuitive](../todos/issavable-intuitive-api.md)
**Status:** Documentation Complete
**Last Updated:** 2026-03-01

---

## Overview

Introduce `IEntityRoot : IEntityBase` that adds `IsSavable` and `Save()` to the public interface. Child entities expose only `IEntityBase`, which no longer exposes `IsSavable` or `Save()`. The user signals root vs child by choosing which interface their entity interface extends. Concrete `EntityBase<T>` is unchanged.

---

## Approach

1. Remove `IsSavable` from `IEntityMetaProperties` and `Save()` from `IEntityBase`
2. Create new `IEntityRoot : IEntityBase` that adds `IsSavable` and `Save()`
3. Update existing entity interfaces: roots extend `IEntityRoot`, children extend `IEntityBase`
4. `EntityBase<T>` concrete class keeps `IsSavable` and `Save()` as concrete members (classes should be `internal`)
5. No RemoteFactory changes needed

---

## Design

### Interface Hierarchy (After Change)

```
IValidateMetaProperties          IFactorySaveMeta (RemoteFactory)
       |                                |
IEntityMetaProperties ------------------+
  (IsChild, IsModified, IsSelfModified, IsMarkedModified)
  (NO IsSavable — moved to IEntityRoot)
       |
IValidateBase + IEntityMetaProperties + IFactorySaveMeta
       |
  IEntityBase
  (Root, ModifiedProperties, Delete, UnDelete, indexer)
  (NO Save — moved to IEntityRoot)
  (NO IsSavable — moved to IEntityRoot)
       |
  IEntityRoot : IEntityBase
  (IsSavable, Save(), Save(CancellationToken))
```

### User-Facing Pattern

```csharp
// Aggregate root — extends IEntityRoot, exposes IsSavable and Save()
public interface IOrder : IEntityRoot
{
    string OrderNumber { get; set; }
    IOrderLineList Lines { get; }
}

// Child entity — extends IEntityBase only, no IsSavable, no Save()
public interface IOrderLine : IEntityBase
{
    string ProductName { get; set; }
    decimal Price { get; set; }
}
```

### Concrete Class (Unchanged)

```csharp
// EntityBase<T> still implements IEntityRoot (so it has IsSavable and Save())
// But this doesn't matter because:
// 1. Entity classes should be internal
// 2. Users interact through the interface (IEntityBase or IEntityRoot)
// 3. The interface is the access control mechanism
[Factory]
public abstract class EntityBase<T> : ValidateBase<T>, IEntityBase, IEntityRoot
    where T : EntityBase<T>
{
    // IsSavable and Save() remain as concrete members
    // No behavior change
}
```

---

## Detailed Changes

### File: `src/Neatoo/IMetaProperties.cs`

**Change:** Remove `IsSavable` from `IEntityMetaProperties`.

Before:
```csharp
public interface IEntityMetaProperties : IFactorySaveMeta
{
    bool IsChild { get; }
    bool IsModified { get; }
    bool IsSelfModified { get; }
    bool IsMarkedModified { get; }
    bool IsSavable { get; }  // REMOVE THIS
}
```

After:
```csharp
public interface IEntityMetaProperties : IFactorySaveMeta
{
    bool IsChild { get; }
    bool IsModified { get; }
    bool IsSelfModified { get; }
    bool IsMarkedModified { get; }
    // IsSavable moved to IEntityRoot
}
```

### File: `src/Neatoo/EntityBase.cs`

**Change 1:** Remove `Save()` and `IsSavable` from `IEntityBase` interface.

Before:
```csharp
public interface IEntityBase : IValidateBase, IEntityMetaProperties, IFactorySaveMeta
{
    IValidateBase? Root { get; }
    IEnumerable<string> ModifiedProperties { get; }
    void Delete();
    void UnDelete();
    Task<IEntityBase> Save();
    Task<IEntityBase> Save(CancellationToken token);
    new IEntityProperty this[string propertyName] { get; }
}
```

After:
```csharp
public interface IEntityBase : IValidateBase, IEntityMetaProperties, IFactorySaveMeta
{
    IValidateBase? Root { get; }
    IEnumerable<string> ModifiedProperties { get; }
    void Delete();
    void UnDelete();
    new IEntityProperty this[string propertyName] { get; }
    // Save() and IsSavable moved to IEntityRoot
}
```

**Change 2:** Create new `IEntityRoot` interface (in `IEntityBase.cs` or a new file).

```csharp
/// <summary>
/// Defines the interface for aggregate root entities that can initiate save operations.
/// </summary>
/// <remarks>
/// Use IEntityRoot for aggregate roots that own the Save() operation.
/// Use IEntityBase for child entities within an aggregate.
/// The user signals root vs child by choosing which interface their entity interface extends.
/// </remarks>
public interface IEntityRoot : IEntityBase
{
    /// <summary>
    /// Gets a value indicating whether this entity can be saved.
    /// </summary>
    /// <value>
    /// <c>true</c> if the entity is modified, valid, and not busy; otherwise, <c>false</c>.
    /// </value>
    bool IsSavable { get; }

    /// <summary>
    /// Persists the entity asynchronously using the configured factory.
    /// </summary>
    Task<IEntityBase> Save();

    /// <summary>
    /// Persists the entity asynchronously with cancellation support.
    /// </summary>
    Task<IEntityBase> Save(CancellationToken token);
}
```

**Change 3:** `EntityBase<T>` implements `IEntityRoot` in addition to `IEntityBase`.

Before:
```csharp
public abstract class EntityBase<T> : ValidateBase<T>, INeatooObject, IEntityBase, IEntityBaseInternal, IEntityMetaProperties
```

After:
```csharp
public abstract class EntityBase<T> : ValidateBase<T>, INeatooObject, IEntityBase, IEntityRoot, IEntityBaseInternal, IEntityMetaProperties
```

### File: `src/Neatoo/EntityListBase.cs`

**Change:** `EntityListBase<I>` no longer needs `IsSavable` on `IEntityMetaProperties`. But it currently implements `IEntityMetaProperties` which currently includes `IsSavable`. After removing `IsSavable` from `IEntityMetaProperties`, the existing `IsSavable => false` property on `EntityListBase` will become a concrete member that isn't part of any interface.

**Decision:** Remove `IsSavable` from `EntityListBase<I>` entirely since it won't be on any interface. Or keep it for internal convenience. Given that `IEntityListBase` extends `IEntityMetaProperties`, and `IEntityMetaProperties` no longer has `IsSavable`, the `IsSavable` property on `EntityListBase` can be removed.

Also: `IEntityListBase` extends `IEntityMetaProperties`. After the change, `IEntityMetaProperties` no longer has `IsSavable`, so `IEntityListBase` consumers lose access to `IsSavable` on lists -- which is correct (lists are never savable).

### File: `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs`

**Impact:** The serialization code at line 312-321 serializes all properties from `IEntityMetaProperties`. After removing `IsSavable` from `IEntityMetaProperties`, it will no longer be serialized. This is correct -- `IsSavable` is a computed property (derived from `IsModified`, `IsValid`, `IsBusy`, `IsChild`) and does not need to be serialized. The deserialization side does not read `IsSavable` -- it reads the settable properties on `EntityBase<>` to restore state.

### File: `src/Neatoo/LazyLoad.cs`

**Change:** `LazyLoad<T>` currently implements `IEntityMetaProperties.IsSavable` via delegation. After removing `IsSavable` from `IEntityMetaProperties`, this property becomes orphaned.

```csharp
// Before (line 253-254)
public bool IsSavable => (_value as IEntityMetaProperties)?.IsSavable ?? false;
```

This should be removed or changed to implement `IEntityRoot.IsSavable` if `LazyLoad<T>` implements `IEntityRoot`. Need to verify the `LazyLoad<T>` interface list.

---

## Tensions and Concerns

### TENSION 1: `Person.cs` Uses `this.IsSavable` Inside Insert/Update (MEDIUM)

In `src/Examples/Person/Person.DomainModel/Person.cs` lines 92 and 117:
```csharp
[Insert]
public async Task<PersonEntity?> Insert(...)
{
    await RunRules(token: cancellationToken);
    if(!this.IsSavable) { return null; }  // Uses IsSavable on concrete class
    ...
}
```

This code accesses `IsSavable` on the concrete `EntityBase<T>` class (not through the interface). **This will still work** because `EntityBase<T>` retains `IsSavable` as a concrete member. No change needed.

However, this is a **code smell**: the Insert method is called by Save(), which already checked IsSavable before calling Insert. The re-check is redundant. But this is an existing pattern, not introduced by our change.

### TENSION 2: All Existing Sample/Test Entity Interfaces Use `IEntityBase` For Everything (HIGH)

Every entity interface in the codebase extends `IEntityBase`, regardless of whether it's a root or child:

**Root entities using `IEntityBase` (should change to `IEntityRoot`):**
- `IPerson : IEntityBase` (Person.DomainModel)
- `IOrder : IEntityBase` (ReadmeSamples.cs)
- `IPersonObject : IEntityBase` (Neatoo.UnitTest.Demo)

**Child entities using `IEntityBase` (correct, stay as is):**
- `IPersonPhone : IEntityBase`
- `ISkillFactoryOrderItem : IEntityBase`
- `ICollectionOrderItem : IEntityBase`
- `IEntitiesOrderItem : IEntityBase`
- (and 10+ more in samples)

**Root entities without interfaces (Design.Domain):**
The Design.Domain project uses concrete classes directly (Order, Employee, DemoEntity) without user-defined interfaces. These classes are public and extend `EntityBase<T>`. Since `EntityBase<T>` will implement `IEntityRoot`, they automatically get `IsSavable` and `Save()`.

**Impact:** After the change, root entity interfaces that extend `IEntityBase` will lose `IsSavable` and `Save()`. They must be updated to extend `IEntityRoot`. This is a **breaking change** for existing users.

### TENSION 3: `IsSavable` on `IEntityBase` Referenced by Tests via `nameof` (LOW)

In `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/EntityBaseTests.cs` line 177:
```csharp
CollectionAssert.Contains(propertyChangedNames, nameof(IEntityBase.IsSavable));
```

After the change, `IsSavable` is no longer on `IEntityBase`. This becomes `nameof(IEntityRoot.IsSavable)` or just `"IsSavable"`.

### TENSION 4: Blazor UI Binding Uses `Person.IsSavable` (LOW)

In `src/Examples/Person/Person.App/Pages/Home.razor` line 84:
```csharp
Disabled="@(!Person.IsSavable)"
```

Where `Person` is typed as `IPerson`. After the change, `IPerson` must extend `IEntityRoot` for this to compile.

### TENSION 5: `EntityListBase<I>` MetaState Caching Includes IsSavable (LOW — pure dead code removal)

`EntityListBase` caches `IsSavable` in `EntityMetaState` and checks for changes. But `IsSavable` was always `false` for every child in the list — all it was doing was faithfully propagating `false`. This isn't losing functionality, it's removing a lie. Clean it up entirely.

### TENSION 6: Design.Domain Entities Are Public Without Interfaces (INFORMATIONAL)

The Design.Domain project uses public concrete entity classes (e.g., `Order : EntityBase<Order>`) without user-defined entity interfaces. Since `EntityBase<T>` implements `IEntityRoot`, the Design.Domain entities automatically expose `IsSavable` and `Save()` through the concrete class and through `IEntityRoot`. This is consistent with the design intent.

However, the Design.Domain project should add interface examples showing the `IEntityRoot` vs `IEntityBase` distinction to serve as the authoritative reference.

### TENSION 7: `IsSavable` Contains `!IsChild` Check — Now Redundant? (DESIGN DECISION)

Currently: `public virtual bool IsSavable => this.IsModified && this.IsValid && !this.IsBusy && !this.IsChild;`

If only roots expose `IsSavable` through the interface, the `!IsChild` check becomes redundant (roots are never children). Options:

1. **Remove `!IsChild` from `IsSavable`** — Clean, but technically a behavior change if someone accesses `IsSavable` on the concrete class of a child entity (they shouldn't, but they could).
2. **Keep `!IsChild` for safety** — Belt and suspenders. No harm since it's always true for roots.

**Recommendation:** Keep `!IsChild` for now. It's a safety net and doesn't hurt. Can be removed in a follow-up if desired.

### TENSION 8: `IEntityListBase` Inherits `IEntityMetaProperties` — Losing `IsSavable` (LOW — same dead code)

`IEntityListBase : IValidateListBase, IEntityMetaProperties` currently exposes `IsSavable` through `IEntityMetaProperties`. After removing `IsSavable` from `IEntityMetaProperties`, lists no longer expose `IsSavable` through any interface. Same story as TENSION 5 — lists were never savable, `IsSavable` was always `false`. Removing it is correct.

The only usage of `IsSavable` on a list type is in `EntityListBase` itself (line 82) and in tests that access it on the concrete class. No code accesses it through the `IEntityListBase` interface. Safe.

### TENSION 9: `IEntityBase` Has `IEntityMetaProperties` In Its Inheritance — Diamond Problem (INFORMATIONAL)

After the change:
- `IEntityBase : IValidateBase, IEntityMetaProperties, IFactorySaveMeta`
- `IEntityRoot : IEntityBase` (adds `IsSavable`, `Save()`)
- `EntityBase<T> : ValidateBase<T>, IEntityBase, IEntityRoot, IEntityMetaProperties`

Since `IEntityRoot` extends `IEntityBase`, and `EntityBase<T>` implements both, there's no diamond problem. C# handles this cleanly. `IsSavable` on `IEntityRoot` is a new member that hides nothing (it's not on `IEntityBase` or `IEntityMetaProperties` anymore).

---

## Implementation Steps

### Phase 1: Framework Changes (src/Neatoo/)

1. Remove `IsSavable` from `IEntityMetaProperties` in `IMetaProperties.cs`
2. Create `IEntityRoot` interface (in `IEntityBase.cs` or new file)
3. Remove `Save()` from `IEntityBase` in `EntityBase.cs`
4. Add `IEntityRoot` to `EntityBase<T>`'s interface list
5. Clean up `EntityListBase<I>` — remove `IsSavable` property, clean up MetaState tuple
6. Clean up `LazyLoad<T>` — handle `IsSavable` removal from `IEntityMetaProperties`
7. Verify serialization still works (IsSavable no longer serialized, which is correct)

### Phase 2: Test Updates (src/Neatoo.UnitTest/)

1. Update `nameof(IEntityBase.IsSavable)` references to `nameof(IEntityRoot.IsSavable)` or `"IsSavable"`
2. Update test entity interfaces that represent roots to extend `IEntityRoot`
3. Ensure IsSavable assertions on children access the concrete class, not the interface
4. Update `EntityBaseStateTests` if needed (they access `IsSavable` on concrete class — should still work)

### Phase 3: Design Project Updates (src/Design/)

1. Add `IEntityRoot` usage examples to Design.Domain
2. Add entity interfaces to Order/Employee showing `IEntityRoot` vs `IEntityBase`
3. Update Design.Tests to verify the pattern
4. Update AllBaseClasses.cs documentation

### Phase 4: Samples & Examples Updates

1. Update `IPerson : IEntityBase` to `IPerson : IEntityRoot`
2. Update `IOrder : IEntityBase` to `IOrder : IEntityRoot` (ReadmeSamples)
3. Update all root entity interfaces in samples to `IEntityRoot`
4. Keep child entity interfaces as `IEntityBase`
5. Update Blazor page if needed (should work once IPerson extends IEntityRoot)

---

## Acceptance Criteria

- [ ] `IEntityBase` no longer exposes `IsSavable` or `Save()`
- [ ] `IEntityRoot : IEntityBase` exists with `IsSavable` and `Save()`
- [ ] `EntityBase<T>` implements `IEntityRoot`
- [ ] Root entity interfaces extend `IEntityRoot`; child interfaces extend `IEntityBase`
- [ ] All tests pass
- [ ] Design.sln builds and tests pass
- [ ] Neatoo.sln builds and tests pass
- [ ] Serialization unaffected (IsSavable computed, not stored)
- [ ] Person example builds and runs correctly

---

## Dependencies

- No RemoteFactory changes needed
- No source generator changes needed
- `IFactorySaveMeta` (RemoteFactory) unchanged — only has `IsNew` and `IsDeleted`

---

## Risks / Considerations

1. **Breaking change for existing users** — Any user interface extending `IEntityBase` for root entities must change to `IEntityRoot`. This is the intended effect but requires a major version bump.
2. **Serialization regression** — `IsSavable` will no longer be serialized. This should be fine since it's computed, but needs verification.
3. **EntityListBase cleanup** — `IsSavable` on lists was always `false` (dead code propagating a lie). Removal is pure cleanup with no functional impact, but MetaState tuple structure changes need care.
4. **`!IsChild` in `IsSavable` formula** — Recommend keeping for now as safety net.

---

## Architectural Verification

### Scope Table

| Feature/Pattern | Affected? | Status |
|---|---|---|
| `IEntityBase` interface | Yes — remove `IsSavable`, `Save()` | Needs Implementation |
| `IEntityRoot` interface | New interface | Needs Implementation |
| `IEntityMetaProperties` | Yes — remove `IsSavable` | Needs Implementation |
| `EntityBase<T>` class | Yes — add `IEntityRoot` to interface list | Needs Implementation |
| `EntityListBase<I>` | Yes — remove orphaned `IsSavable` | Needs Implementation |
| `IEntityListBase` | No change — inherits from `IEntityMetaProperties` which loses `IsSavable` | Verified implicitly |
| `LazyLoad<T>` | Yes — `IsSavable` orphaned | Needs Implementation |
| RemoteFactory/Generators | No change needed | Verified |
| Serialization | `IsSavable` no longer serialized (correct) | Needs Verification |
| Design.Domain entities | Need interfaces added | Needs Implementation (new code) |

### Design Project Verification

**Claim: Root entity interface extending IEntityRoot compiles**
- Failing code at: `src/Design/Design.Domain/Aggregates/OrderAggregate/IOrderInterfaces.cs:35`
- Error: `CS0246: The type or namespace name 'IEntityRoot' could not be found`
- Status: **Needs Implementation** — `IEntityRoot` must be created in `src/Neatoo/`

**Claim: IEntityRoot interface tests compile and pass**
- Failing code at: `src/Design/Design.Tests/AggregateTests/EntityRootInterfaceTests.cs`
- Tests exercise: `IEntityRoot` assignment, `IsSavable` on root, `Save()` on root, child not exposing `IsSavable`
- Status: **Needs Implementation** — depends on `IEntityRoot` creation

**Claim: Child entity interface extending IEntityBase compiles without IsSavable/Save()**
- Verified (existing code) — All child interfaces already extend `IEntityBase` (e.g., `IApiOrderItem : IEntityBase` in samples)
- After removing `IsSavable` from `IEntityBase`, child interfaces will no longer expose it — this is the desired outcome
- Status: **Needs Implementation** — must verify after `IsSavable` removal

**Claim: EntityBase<T> can implement both IEntityBase and IEntityRoot**
- Status: **Needs Implementation** — straightforward but must compile

### Breaking Changes: Yes

This is a **breaking change** for:
1. Any user interface extending `IEntityBase` for root entities that accesses `IsSavable` or `Save()` — must change to `IEntityRoot`
2. Any code accessing `IsSavable` through `IEntityMetaProperties` — must cast to `IEntityRoot`
3. Serialization format — `IsSavable` no longer included in JSON (was computed, not settable)

This requires a **major version bump** (0.14.x -> 1.0.0 or appropriate pre-1.0 version).

### Codebase Analysis

Files examined:
- `src/Neatoo/EntityBase.cs` — IEntityBase + EntityBase<T> definition, Save(), IsSavable
- `src/Neatoo/IEntityBase.cs` — Nearly empty (just namespace)
- `src/Neatoo/IMetaProperties.cs` — IValidateMetaProperties + IEntityMetaProperties
- `src/Neatoo/EntityListBase.cs` — IEntityListBase + EntityListBase<I>
- `src/Neatoo/ValidateBase.cs` — IValidateBase definition
- `src/Neatoo/LazyLoad.cs` — LazyLoad<T> IsSavable delegation
- `src/Neatoo/Exceptions.cs` — SaveOperationException, SaveFailureReason
- `src/Neatoo/InternalInterfaces.cs` — IEntityBaseInternal
- `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs` — Serialization
- `src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs` — All four base classes
- `src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs` — Aggregate root
- `src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItem.cs` — Child entity
- `src/Design/Design.Domain/Aggregates/OrderAggregate/OrderItemList.cs` — Entity list
- `src/Design/Design.Domain/Entities/Employee.cs` — Full CRUD entity
- `src/Design/Design.Domain/Entities/Address.cs` — Child entity
- `src/Design/Design.Domain/FactoryOperations/SavePatterns.cs` — Save patterns
- `src/Design/Design.Domain/DI/ServiceContracts.cs` — Service contracts
- `src/Design/CLAUDE-DESIGN.md` — Design guidance
- `src/Examples/Person/Person.DomainModel/Person.cs` — IPerson : IEntityBase, uses IsSavable inside Insert/Update
- `src/Examples/Person/Person.DomainModel/PersonPhone.cs` — IPersonPhone : IEntityBase (child)
- `src/Examples/Person/Person.DomainModel/PersonPhoneList.cs` — PersonPhoneList
- `src/Examples/Person/Person.App/Pages/Home.razor` — Blazor UI using Person.IsSavable
- `src/samples/*.cs` — 15+ sample files with entity interfaces
- `src/Neatoo.UnitTest/` — Test files referencing IsSavable

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Framework core changes | developer | Yes | Clean context for core interface work | None |
| Phase 2: Test updates | developer | No | Continues from Phase 1 context | Phase 1 |
| Phase 3: Design project updates | developer | No | Continues from Phase 2 context | Phase 2 |
| Phase 4: Samples & examples | developer | Yes | Independent scope, many files | Phase 1 |

**Parallelizable phases:** Phases 3 and 4 can run in parallel after Phase 2 completes.

**Notes:** Phase 1 is the critical path. All other phases depend on it.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-01

### Verdict: Approved

The plan is thorough. The core design is sound and well-motivated. Minor gaps exist (detailed below) but all are addressable during implementation without design changes.

### Concerns (Minor -- Addressed in Implementation Contract)

1. **IEntityRoot file location ambiguous**: Plan says "in `IEntityBase.cs` or a new file" without deciding. **Resolution:** Place `IEntityRoot` in `EntityBase.cs` alongside `IEntityBase` (where the interface is currently defined).

2. **LazyLoad cleanup underspecified**: Plan says "handle IsSavable removal" but doesn't specify the outcome. **Resolution:** Remove `IsSavable` property from `LazyLoad<T>` entirely (do NOT implement `IEntityRoot` on LazyLoad). Remove `IsSavable_DelegatesToValue_WhenLoaded` and `IsSavable_BeforeLoad_ReturnsFalse` tests from `LazyLoadTests.cs`.

3. **Sample tests demonstrating "IsSavable is false on children"**: `EntitiesSamples.cs:677` and `ParentChildSamples.cs:330` access `IsSavable`/`Save()` on child entity interfaces (`IEntitiesOrderItem : IEntityBase`, `IParentChildLineItem : IEntityBase`). These CANNOT change to `IEntityRoot` since they ARE child entities. **Resolution:** Rewrite these assertions to demonstrate the new paradigm -- child interfaces don't expose `IsSavable` or `Save()`. Remove or restructure the `Save()` throws assertions.

4. **`ApiReferenceSamples.cs:1311`**: Casts to `IEntityMetaProperties` and accesses `.IsSavable`. Not mentioned in plan. **Resolution:** Remove `IsSavable` assertion from this sample or demonstrate `IEntityRoot` access instead.

5. **EntityBaseTests.cs scope underestimated**: TENSION 3 only mentions line 177. But lines 30, 36, 53, 55, 76, 93, 122, 156, 163, 168, 175 also access `IsSavable` through `IEntityPerson` (which inherits `IEntityBase`). **Resolution:** Change `IPersonEntity` to extend `IEntityRoot` (it is a root entity). Update all `nameof` references.

### What Looks Good

- Interface hierarchy design is clean
- 9 tensions comprehensively identified
- Serialization analysis verified correct (IsSavable has no setter, never deserialized)
- RemoteFactory/generator non-impact correctly assessed
- Design project verification files present and valid

---

## Implementation Contract

**Created:** 2026-03-01
**Approved by:** neatoo-developer

### Design Project Acceptance Criteria

These Design project files currently fail to compile. Implementation is done when they all compile and their tests pass.

- [ ] `src/Design/Design.Domain/Aggregates/OrderAggregate/IOrderInterfaces.cs:35` -- `IOrder : IEntityRoot` fails with CS0246 (IEntityRoot not found). Must compile after creating IEntityRoot.
- [ ] `src/Design/Design.Tests/AggregateTests/EntityRootInterfaceTests.cs` -- All 5 tests depend on IEntityRoot. Must compile and pass.

### In Scope

#### Phase 1: Framework Core Changes (`src/Neatoo/`)

- [ ] Remove `IsSavable` from `IEntityMetaProperties` in `src/Neatoo/IMetaProperties.cs`
- [ ] Remove `Save()` and `Save(CancellationToken)` from `IEntityBase` in `src/Neatoo/EntityBase.cs`
- [ ] Create `IEntityRoot : IEntityBase` interface in `src/Neatoo/EntityBase.cs` (same file as IEntityBase)
- [ ] Add `IEntityRoot` to `EntityBase<T>` class declaration
- [ ] Clean up `EntityListBase<I>` -- remove `IsSavable` property (line 82), update `EntityMetaState` tuple (remove `IsSavable` from tuple at lines 124, 181, 193)
- [ ] Clean up `LazyLoad<T>` -- remove `IsSavable` property (line 254)
- [ ] **Checkpoint:** `dotnet build src/Neatoo/Neatoo.csproj` succeeds

#### Phase 2: Test Updates (`src/Neatoo.UnitTest/`)

- [ ] Change `IPersonEntity : IPersonBase, IEntityBase` to `IPersonEntity : IPersonBase, IEntityRoot` in `src/Neatoo.UnitTest/Integration/Aggregates/Person/IPersonBase.cs`
- [ ] Update `nameof(IEntityBase.IsSavable)` at `EntityBaseTests.cs:177` to `nameof(IEntityRoot.IsSavable)` or `"IsSavable"`
- [ ] Update `nameof(IEntityPerson.IsSavable)` at `EntityBaseTests.cs:76,93,122` -- these will resolve automatically once IPersonEntity extends IEntityRoot
- [ ] Remove `IsSavable_DelegatesToValue_WhenLoaded` test from `LazyLoadTests.cs`
- [ ] Remove `IsSavable_BeforeLoad_ReturnsFalse` test from `LazyLoadTests.cs`
- [ ] Update `IsSavable_AlwaysFalse` test in `EntityListBaseTests.cs` -- now accesses concrete class (should still compile; verify)
- [ ] Verify `EntityBaseStateTests.cs` IsSavable tests still compile (access concrete class)
- [ ] **Checkpoint:** `dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj` passes

#### Phase 3: Design Project Updates (`src/Design/`)

- [ ] Verify `IOrderInterfaces.cs` compiles (IEntityRoot now exists)
- [ ] Verify `EntityRootInterfaceTests.cs` compiles and tests pass
- [ ] Update `AllBaseClasses.cs` documentation to include IEntityRoot
- [ ] **Checkpoint:** `dotnet build src/Design/Design.sln` succeeds and `dotnet test src/Design/Design.sln` passes

#### Phase 4: Samples & Examples Updates

- [ ] Update `IPerson : IEntityBase` to `IPerson : IEntityRoot` in `src/Examples/Person/Person.DomainModel/Person.cs`
- [ ] Update root entity interfaces in samples:
  - `IEntitiesOrder` (or equivalent) in `EntitiesSamples.cs`
  - `IParentChildOrder` (or equivalent) in `ParentChildSamples.cs`
  - All other root entity interfaces in `src/samples/`
- [ ] Rewrite child-entity IsSavable/Save() demonstrations:
  - `EntitiesSamples.cs:677` -- remove `item.IsSavable` assertion (child interface has no IsSavable)
  - `EntitiesSamples.cs:681` -- remove or restructure `item.Save()` throws test
  - `ParentChildSamples.cs:330` -- remove `item.IsSavable` assertion
  - `ParentChildSamples.cs:334` -- remove or restructure `item.Save()` throws test
- [ ] Update `ApiReferenceSamples.cs:1311` -- remove `entityMeta.IsSavable` assertion from IEntityMetaProperties demo
- [ ] Update comments in `RemoteFactoryIntegrationSamples.cs` referencing `IsSavable` on children
- [ ] Update `IPersonObject : IEntityBase` in `src/Neatoo.UnitTest.Demo/PersonObjectTests.cs` if it's a root
- [ ] **Checkpoint:** `dotnet build src/samples/` succeeds, `dotnet test src/samples/` passes
- [ ] **Checkpoint:** `dotnet build src/Examples/Person/Person.sln` succeeds, `dotnet test` on Person project passes

### Explicitly Out of Scope

- Removing `!IsChild` from the `IsSavable` formula on `EntityBase<T>` -- keep for safety
- RemoteFactory changes -- confirmed not needed
- Source generator changes -- confirmed not needed
- Version bumping -- separate task after implementation
- Documentation updates (CLAUDE.md, CLAUDE-DESIGN.md, skills) -- separate documentation step

### Verification Gates

1. **After Phase 1:** Framework compiles. `dotnet build src/Neatoo/Neatoo.csproj` succeeds.
2. **After Phase 2:** All unit/integration tests pass. `dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj` succeeds with zero failures.
3. **After Phase 3:** Design project compiles and tests pass. `dotnet build src/Design/Design.sln` and `dotnet test src/Design/Design.sln` succeed.
4. **After Phase 4:** Samples compile and tests pass. Examples compile and tests pass.
5. **Final:** `dotnet test src/Neatoo.sln` passes with zero failures. `dotnet build src/Design/Design.sln` succeeds. All Design project acceptance criteria compile.

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (test not related to IEntityRoot/IsSavable change)
- Serialization test fails (indicates IsSavable removal affects state transfer)
- RemoteFactory-generated code fails to compile (indicates unexpected generator dependency)
- Architectural contradiction discovered (e.g., framework code internally casts to IEntityBase and calls Save())
- Any test that was passing before starts failing for reasons unrelated to the IsSavable/IEntityRoot change

---

## Implementation Progress

**Started:** 2026-03-01
**Developer:** neatoo-developer

### Phase 1: Framework Core Changes -- COMPLETE

- [x] Removed `IsSavable` from `IEntityMetaProperties` in `src/Neatoo/IMetaProperties.cs`
- [x] Removed `Save()` and `Save(CancellationToken)` from `IEntityBase` in `src/Neatoo/EntityBase.cs`
- [x] Created `IEntityRoot : IEntityBase` interface in `src/Neatoo/EntityBase.cs`
- [x] Added `IEntityRoot` to `EntityBase<T>` class declaration
- [x] Cleaned up `EntityListBase<I>` -- removed `IsSavable` property, updated `EntityMetaState` tuple from 3-tuple to 2-tuple, updated `CheckIfMetaPropertiesChanged` and `ResetMetaState`
- [x] Cleaned up `LazyLoad<T>` -- removed orphaned `IsSavable` property
- [x] **Checkpoint:** `dotnet build src/Neatoo/Neatoo.csproj` -- 0 errors, 0 warnings

### Phase 2: Test Updates -- COMPLETE

- [x] Changed `IPersonEntity : IPersonBase, IEntityBase` to `IPersonEntity : IPersonBase, IEntityRoot` in `src/Neatoo.UnitTest/Integration/Aggregates/Person/IPersonBase.cs`
- [x] Changed `nameof(IEntityBase.IsSavable)` to `nameof(IEntityRoot.IsSavable)` in `EntityBaseTests.cs`
- [x] Changed `IEntityObject : IEntityBase` to `IEntityObject : IEntityRoot` in serialization `EntityObject.cs`
- [x] Removed `IsSavable` assertion from integration `EntityListBaseTests` (list.IsSavable was always false -- dead code)
- [x] Removed `IsSavable_AlwaysFalse` test from unit `EntityListBaseTests` (dead code)
- [x] Removed `IsSavable_DelegatesToValue_WhenLoaded` and `IsSavable_BeforeLoad_ReturnsFalse` tests from `LazyLoadTests.cs`
- [x] Removed `IsSavableValue` from `TestEntityValue` class
- [x] **Checkpoint:** `dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj` -- 1729 passed, 0 failed, 1 skipped

### Phase 3: Design Project Updates -- COMPLETE

- [x] Verified `IOrderInterfaces.cs` compiles (IOrder : IEntityRoot, IOrderItem : IEntityBase)
- [x] Added `IOrder` to `Order` class declaration: `Order : EntityBase<Order>, IOrder`
- [x] Added `#pragma warning disable CA1859` in `EntityRootInterfaceTests.cs` for intentional interface cast
- [x] Updated `AllBaseClasses.cs` documentation to include IEntityRoot
- [x] **Checkpoint:** `dotnet build src/Design/Design.sln` -- 0 errors; `dotnet test` -- 89 passed, 0 failed

### Phase 4: Samples & Examples Updates -- COMPLETE

- [x] Updated `IPerson : IEntityBase` to `IPerson : IEntityRoot` in `src/Examples/Person/Person.DomainModel/Person.cs`
- [x] Verified `IPersonPhone : IEntityBase` stays as-is (child entity)
- [x] Verified `IOrder : IEntityBase` in ReadmeSamples stays as-is (Order is a child within Customer aggregate)
- [x] All child entity interfaces (15+ in samples) correctly remain as `: IEntityBase`
- [x] Rewrote `EntitiesSamples.cs:677-682` -- removed `item.IsSavable` and `item.Save()` assertions, replaced with type-level enforcement comment; changed method from `async Task` to `void`
- [x] Rewrote `ParentChildSamples.cs:329-335` -- removed `item.IsSavable` and `item.Save()` assertions, replaced with type-level enforcement comment; changed method from `async Task` to `void`
- [x] Updated `ApiReferenceSamples.cs:1311` -- changed `entityMeta.IsSavable` to `((IEntityRoot)entityMeta).IsSavable` with explanatory comment
- [x] Updated comments in `RemoteFactoryIntegrationSamples.cs` -- replaced "IsSavable = false" comments with "IsSavable/Save() not accessible on child interface"
- [x] Left `IPersonObject : IEntityBase` in UnitTest.Demo as-is (no IsSavable/Save usage, conditionally a root)
- [x] **Checkpoint:** `dotnet build src/samples/` -- 0 errors; `dotnet test src/samples/` -- 245 passed, 0 failed
- [x] **Checkpoint:** `dotnet build Person.DomainModel` -- 0 errors; `dotnet test Person.DomainModel.Tests` -- 55 passed, 0 failed
- [x] **Checkpoint:** `dotnet build Person.App` -- 0 errors (Blazor page compiles with `Person.IsSavable`)

---

## Completion Evidence

**Completed:** 2026-03-01

### Test Results

**`dotnet test src/Neatoo.sln` (Full Solution):**
- Neatoo.UnitTest: 1729 passed, 0 failed, 1 skipped
- Samples: 245 passed, 0 failed
- Person.DomainModel.Tests: 55 passed, 0 failed
- Neatoo.BaseGenerator.Tests: 26 passed, 0 failed

**`dotnet build src/Design/Design.sln`:** 0 errors, 0 warnings
**`dotnet test src/Design/Design.sln`:** 89 passed, 0 failed

### Design Project Acceptance Criteria

- [x] `src/Design/Design.Domain/Aggregates/OrderAggregate/IOrderInterfaces.cs:35` -- `IOrder : IEntityRoot` compiles
- [x] `src/Design/Design.Tests/AggregateTests/EntityRootInterfaceTests.cs` -- All 5 tests pass

### All Contract Items Verified

All Phase 1-4 checklist items are checked. No stop conditions were triggered. No out-of-scope test failures occurred.

### Files Modified

**Framework Core (Phase 1):**
- `src/Neatoo/IMetaProperties.cs` -- Removed `IsSavable` from `IEntityMetaProperties`
- `src/Neatoo/EntityBase.cs` -- Removed `Save()` from `IEntityBase`, created `IEntityRoot : IEntityBase`, added `IEntityRoot` to `EntityBase<T>`
- `src/Neatoo/EntityListBase.cs` -- Removed `IsSavable` property, simplified `EntityMetaState` tuple
- `src/Neatoo/LazyLoad.cs` -- Removed orphaned `IsSavable` property

**Tests (Phase 2):**
- `src/Neatoo.UnitTest/Integration/Aggregates/Person/IPersonBase.cs` -- `IPersonEntity : IEntityRoot`
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/EntityBaseTests.cs` -- `nameof(IEntityRoot.IsSavable)`
- `src/Neatoo.UnitTest/Integration/Concepts/Serialization/EntityObject.cs` -- `IEntityObject : IEntityRoot`
- `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/EntityListBaseTests.cs` -- Removed dead `IsSavable` assertion
- `src/Neatoo.UnitTest/Unit/Core/EntityListBaseTests.cs` -- Removed `IsSavable_AlwaysFalse` test
- `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` -- Removed IsSavable tests, cleaned TestEntityValue

**Design Project (Phase 3):**
- `src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs` -- Added `IOrder` to class declaration
- `src/Design/Design.Tests/AggregateTests/EntityRootInterfaceTests.cs` -- Added pragma for CA1859
- `src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs` -- Updated documentation

**Samples & Examples (Phase 4):**
- `src/Examples/Person/Person.DomainModel/Person.cs` -- `IPerson : IEntityRoot`
- `src/samples/EntitiesSamples.cs` -- Rewrote child-entity test (removed IsSavable/Save assertions)
- `src/samples/ParentChildSamples.cs` -- Rewrote child-entity test (removed IsSavable/Save assertions)
- `src/samples/ApiReferenceSamples.cs` -- Changed IEntityMetaProperties.IsSavable to IEntityRoot cast
- `src/samples/RemoteFactoryIntegrationSamples.cs` -- Updated child IsSavable comments

### Deviation from Plan

1. **`IOrder : IEntityBase` in ReadmeSamples.cs was NOT changed to IEntityRoot.** The plan listed this as a root entity, but code inspection reveals Order is a child within the Customer aggregate (contained in OrderList, no [Remote] operations). Leaving as IEntityBase is correct.

2. **`IPersonObject : IEntityBase` in UnitTest.Demo was NOT changed.** The plan says "if it's a root." It has no IsSavable/Save usage and no [Remote] factory operations. Leaving as IEntityBase avoids unnecessary churn.

---

## Documentation

**Agent:** neatoo-developer
**Completed:** 2026-03-01

### Expected Deliverables

- [x] Update `src/Design/CLAUDE-DESIGN.md` — Update IsSavable definition, add IEntityRoot
- [x] Update `src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs` — Add IEntityRoot to documentation (completed during Phase 3 implementation)
- [x] Update `CLAUDE.md` (project root) — Update IsSavable definition in State Properties
- [x] Add `IEntityRoot` to terminology section
- [x] Update MarkdownSnippets samples for IEntityRoot usage pattern — N/A, no snippet placeholders reference IsSavable/Save() directly; sample code was already updated in Phase 4
- [x] Skill updates: RemoteFactory skill — N/A, no references to IsSavable, Save(), IEntityBase, or IEntityRoot
- [x] Skill updates: KnockOff skill — N/A, no references to IsSavable, Save(), IEntityBase, or IEntityRoot
- [x] Sample updates: Yes — all root entity interfaces already updated in Phase 4

### Files Updated

1. **`CLAUDE.md`** (project root):
   - Added "Entity Interfaces: Root vs Child" subsection to Neatoo Terminology with `IEntityRoot` and `IEntityBase` descriptions, code example, and "why this exists" rationale
   - Added `IsSavable` to State Properties with root-only note
   - Added "Root vs child interfaces" entry to Design Source of Truth key files list

2. **`src/Design/CLAUDE-DESIGN.md`**:
   - Added "Key Design Decision: IEntityRoot vs IEntityBase" section with full rationale, code example, and EntityListBase note
   - Added "Root vs child interfaces" row to Key Files by Topic table
   - Updated EntityBase description in "The Four Base Classes" to include IEntityRoot/IEntityBase distinction
   - Updated "Adding a Child Entity" section to describe interface pattern instead of `IsSavable=false` behavior
   - Updated Quick Reference state properties table: `IsSavable` now shows "EntityBase (IEntityRoot only)"

3. **`src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs`** — Already updated during Phase 3 implementation (interface hierarchy diagram, IEntityRoot/IEntityBase documentation, EntityListBase design decision comment)

4. **`src/Design/Design.Domain/Aggregates/OrderAggregate/IOrderInterfaces.cs`** — Already created during Phase 3 implementation (authoritative example of IEntityRoot vs IEntityBase)

### Skills Checked (No Changes Needed)

- `.claude/skills/RemoteFactory/SKILL.md` and all reference files — no references to IsSavable, Save(), IEntityBase, or IEntityRoot
- `.claude/skills/knockoff/SKILL.md` and all reference files — no references to IsSavable, Save(), IEntityBase, or IEntityRoot

### Release Notes Guidance

Release notes (separate task with version bumping) should cover:
- **Breaking change:** `IsSavable` removed from `IEntityMetaProperties` and `IEntityBase`. `Save()` removed from `IEntityBase`. Both moved to new `IEntityRoot : IEntityBase` interface.
- **Migration:** Root entity interfaces must change from `IEntityBase` to `IEntityRoot` to access `IsSavable` and `Save()`. Child entity interfaces remain `IEntityBase`.
- **Removed:** `IsSavable` removed from `EntityListBase` (was always false -- dead code).
- **Removed:** `IsSavable` removed from `LazyLoad<T>` (orphaned after interface change).
- **Serialization:** `IsSavable` no longer serialized (was computed, never deserialized -- no functional impact).
- **Version:** Major version bump required (breaking interface change).

---

## Architect Verification

**Verified by:** neatoo-architect
**Date:** 2026-03-01
**Verdict:** VERIFIED

### Independent Build Results

| Solution | Result |
|----------|--------|
| `dotnet build src/Neatoo.sln` | 0 errors, 0 warnings |
| `dotnet build src/Design/Design.sln` | 0 errors, 0 warnings |

### Independent Test Results

| Test Project | Passed | Failed | Skipped |
|-------------|--------|--------|---------|
| Neatoo.UnitTest | 1729 | 0 | 1 |
| Samples | 245 | 0 | 0 |
| Person.DomainModel.Tests | 55 | 0 | 0 |
| Neatoo.BaseGenerator.Tests | 26 | 0 | 0 |
| Design.Tests | 89 | 0 | 0 |
| **Total** | **2144** | **0** | **1** |

The 1 skipped test (`AsyncFlowTests_CheckAllRules`) is pre-existing and unrelated to this change.

### Design Verification Checklist

- [x] `IEntityRoot` exists in `src/Neatoo/EntityBase.cs` and extends `IEntityBase`
- [x] `IEntityRoot` has `IsSavable`, `Save()`, and `Save(CancellationToken)`
- [x] `IsSavable` removed from `IEntityMetaProperties` in `src/Neatoo/IMetaProperties.cs`
- [x] `Save()` and `Save(CancellationToken)` removed from `IEntityBase`
- [x] `EntityBase<T>` implements `IEntityRoot` (line 116: `class EntityBase<T> : ValidateBase<T>, INeatooObject, IEntityBase, IEntityRoot, IEntityBaseInternal, IEntityMetaProperties`)
- [x] `IsSavable` removed from `EntityListBase<I>` (line 78: comment confirms removal)
- [x] `IsSavable` removed from `LazyLoad<T>` (line 253: comment confirms removal)
- [x] Design project acceptance criteria compile and pass:
  - `IOrderInterfaces.cs:35` -- `IOrder : IEntityRoot` compiles
  - `EntityRootInterfaceTests.cs` -- All 5 tests pass (RootInterface_ExposesIsSavable, ChildInterface_DoesNotExposeIsSavable, RootInterface_ExposessSave, OrderImplementsIEntityRoot, OrderItemImplementsIEntityBase_NotIEntityRoot)
- [x] `AllBaseClasses.cs` updated with IEntityRoot documentation
- [x] `IPerson : IEntityRoot` in `src/Examples/Person/Person.DomainModel/Person.cs`

### Developer Deviations -- Verified Correct

1. **`IOrder : IEntityBase` in `ReadmeSamples.cs` left unchanged.** Verified: `Order` is a child entity contained in `OrderList` (`IEntityListBase<IOrder>`) which is a property of `Customer`. Customer has the `[Remote]` and `[Insert]` operations. Order is not an aggregate root. Keeping `IEntityBase` is correct.

2. **`IPersonObject : IEntityBase` in `UnitTest.Demo/PersonObjectTests.cs` left unchanged.** Verified: No code accesses `IPersonObject.IsSavable` or `IPersonObject.Save()`. No `[Remote]` factory operations on `PersonObject`. This is a minimal test fixture with no root behavior. Keeping `IEntityBase` is correct.
