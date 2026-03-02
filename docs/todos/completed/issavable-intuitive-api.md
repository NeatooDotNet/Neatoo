# Make IsSavable More Intuitive

**Status:** Complete
**Priority:** High
**Created:** 2026-02-24
**Last Updated:** 2026-03-01

---

### 2026-03-01 (Architect Verification)
- Independent verification: VERIFIED
- All builds pass (0 errors, 0 warnings across both solutions)
- All tests pass (2144 passed, 0 failed, 1 skipped)
- All design acceptance criteria met (IEntityRoot exists, 5 Design.Tests pass)
- Developer deviations verified correct (ReadmeSamples.cs IOrder is a child; UnitTest.Demo IPersonObject has no root behavior)
- Plan status updated to Verified
- Ready for documentation step

---

## Problem

`IsSavable` on `EntityBase` includes a `!IsChild` check, making it **always false** for child entities:

```csharp
public virtual bool IsSavable => this.IsModified && this.IsValid && !this.IsBusy && !this.IsChild;
```

This creates a trap where developers naturally use `IsSavable` in save cascade logic to check whether child entities need persisting — but it always returns `false` for children, silently skipping saves.

### Real-World Impact (zTreatment)

In zTreatment, `VisitHub.SaveChildren()` and `Visit.Update()` were checking `IsSavable` on child entities to decide whether to cascade saves. Because `IsSavable` includes `!IsChild` (always `false` for children), child saves were silently skipped. The fix required changing to `IsModified`/`IsNew`:

- `VisitHub.cs` SaveChildren: `ConsultationEntity.IsSavable` -> `ConsultationEntity.IsModified`
- `Visit.cs` InsertCore: `TreatmentEntity.IsSavable` -> `TreatmentEntity.IsNew`
- `Visit.cs` Update: `TreatmentEntity.IsSavable` -> `TreatmentEntity.IsNew` / `TreatmentEntity.IsModified`

### CSLA Comparison

The `!IsChild` check in `IsSavable` is **not from CSLA**. CSLA's `IsSavable` does not check `IsChild`:

```csharp
// CSLA BusinessBase.IsSavable
public virtual bool IsSavable
{
  get
  {
    var result = IsDirty && IsValid && !IsBusy;
    if (result)
    {
      if (IsDeleted)
        result = BusinessRules.HasPermission(ApplicationContext,
          AuthorizationActions.DeleteObject, this);
      else if (IsNew)
        result = BusinessRules.HasPermission(ApplicationContext,
          AuthorizationActions.CreateObject, this);
      else
        result = BusinessRules.HasPermission(ApplicationContext,
          AuthorizationActions.EditObject, this);
    }
    return result;
  }
}
```

Two differences from Neatoo:

1. **No `!IsChild` check** — CSLA keeps "has pending changes" (`IsSavable`) separate from "can initiate its own save" — the latter is enforced by throwing `InvalidOperationException` in `Save()` if called on a child. This means `IsSavable` can be `true` on a CSLA child object (it just means "this object needs saving"), and the root-only enforcement is a separate concern. Neatoo merged both concepts into one property by adding `!IsChild`, which is the source of the confusion.

2. **Authorization included** — CSLA's `IsSavable` checks whether the current user has permission to perform the specific operation (create, edit, or delete) based on the object's state. This makes `IsSavable` a complete "can the UI enable the Save button?" answer. Neatoo's `IsSavable` does not include authorization — authorization is handled separately at the factory level via RemoteFactory's `[AspAuthorize]`/`[AuthorizeFactory]` attributes. This means Neatoo's `IsSavable` can be `true` even when the user lacks permission to save, and the factory call would fail at runtime.

### CSLA's Root vs Child DataPortal Architecture

CSLA has **two separate DataPortal paths** that make root vs child operations explicit at the call site:

```csharp
// Root operations — uses DataPortal<T> internally
DataPortal.Update(entity);      // invokes DataPortal_Update() on the object
DataPortal.Insert(entity);      // invokes DataPortal_Insert() on the object

// Child operations — uses Server.ChildDataPortal internally
DataPortal.UpdateChild(child);  // invokes Child_Update() on the object
DataPortal.CreateChild<T>();    // invokes Child_Create() on the object
DataPortal.FetchChild<T>();     // invokes Child_Fetch() on the object
```

Business objects implement **separate methods** for each path:
- **Root:** `DataPortal_Insert()`, `DataPortal_Update()`, `DataPortal_Delete()`
- **Child:** `Child_Insert()`, `Child_Update()`, `Child_DeleteSelf()`

In modern CSLA (6+), the child path became the `IChildDataPortal<T>` interface with `UpdateChildAsync()`, `CreateChildAsync()`, `FetchChildAsync()`.

**Key design insight:** You never accidentally call `DataPortal.Update()` on a child because there's a dedicated `DataPortal.UpdateChild()` for that. The developer consciously chooses which path they're using. The ambiguity that caused the zTreatment bug simply can't happen in CSLA.

### Implications for Neatoo

In Neatoo's RemoteFactory pattern, there's a single `IFactory.Save(entity)` — no separate "child save" entry point. This means the root-vs-child distinction is invisible at the call site, which is the deeper architectural source of the confusion. The `IsSavable` issue may be a symptom of this larger design gap rather than the root cause.

### Why This Is a Trap

1. The name `IsSavable` reads as "does this entity have changes that need saving?" — a reasonable question during save cascades
2. The `!IsChild` condition is invisible unless you read the source code
3. There's no compiler warning, analyzer diagnostic, or runtime error — saves are just silently skipped
4. The correct pattern (checking `IsModified`/`IsNew` directly) is non-obvious
5. Developers familiar with CSLA would expect `IsSavable` to be `true` on modified children

## Solution — `IEntityRoot` Interface

**Chosen approach:** Introduce an `IEntityRoot` interface that extends `IEntityBase` and adds `IsSavable` and `Save()`. Child entities only expose `IEntityBase`, which has no `IsSavable` or `Save()`.

### Design decisions

- **`IEntityRoot : IEntityBase`** — adds `IsSavable` and `Save()`. Only aggregate roots implement this interface.
- **`IEntityBase`** — retains `IsModified`, `IsValid`, `IsBusy`, `IsNew`, `IsChild`, etc. No `IsSavable`, no `Save()`.
- **User signals root vs child** by choosing which interface their entity interface extends. No attributes, no inference, no RemoteFactory involvement.
- **Concrete `EntityBase<T>` unchanged** — keeps `IsSavable` and `Save()` as concrete members. This doesn't matter because all `EntityBase<T>` classes should be `internal` with only the public interface exposed.
- **`!IsChild` check in `IsSavable` becomes unnecessary** — if only roots expose it through the interface, the check is redundant.
- **Child factory methods have internal signatures** — child `[Insert]`/`[Update]` often need parent entity or parent ID parameters that outside consumers can't fulfill. Combined with `internal` access on entity classes, this prevents external callers from saving children even if they tried.

### Example

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

### Rejected approaches

1. ~~Rename or split~~ — Adding more properties doesn't fix the root cause
2. ~~Analyzer diagnostic~~ — Runtime/compile-time warning is weaker than removing the API entirely
3. ~~Documentation only~~ — Traps should be eliminated, not documented
4. ~~Remove `!IsChild` from `IsSavable`~~ — Still exposes `IsSavable` on children where it has no meaning
5. ~~Explicit child factory operations~~ — Goes the wrong direction; we want to hide child factories from consumers, not expose more of them

---

## Plans

- [IEntityRoot Interface — Architectural Plan](../plans/ientityroot-interface.md)

---

## Tasks

- [ ] Architect review: evaluate approaches and recommend solution
- [ ] Developer review: verify plan feasibility
- [ ] Implementation
- [ ] Update Design.Domain with correct cascade save patterns
- [ ] Update documentation/samples
- [ ] Architect verification

---

## Progress Log

### 2026-02-24
- Created todo based on zTreatment bug investigation
- Root cause: `IsSavable` includes `!IsChild`, always false for child entities
- Identified in `EntityBase.cs:153`: `public virtual bool IsSavable => this.IsModified && this.IsValid && !this.IsBusy && !this.IsChild;`
- Researched CSLA: `!IsChild` is NOT from CSLA. CSLA's `IsSavable` doesn't check `IsChild` — it enforces root-only saves by throwing in `Save()` instead. This favors option 4 (remove `!IsChild` from `IsSavable`, enforce elsewhere).
- Deeper CSLA research: CSLA has **two separate DataPortal paths** — `DataPortal.Update()` for roots and `DataPortal.UpdateChild()` for children, with separate method conventions (`DataPortal_Update` vs `Child_Update`). Modern CSLA (6+) uses `IChildDataPortal<T>`. This makes root-vs-child explicit at the call site. Neatoo's single `IFactory.Save()` lacks this distinction, which is the deeper architectural issue. Added option 5 (explicit child factory operations).
- CSLA's `IsSavable` also includes **authorization checks** (create/edit/delete permissions based on object state). Neatoo's `IsSavable` doesn't — authorization lives at the factory level via `[AspAuthorize]`/`[AuthorizeFactory]`. This means Neatoo's `IsSavable` can be `true` when the user can't actually save. Consider whether `IsSavable` should incorporate authorization awareness.

### 2026-03-01 (Architect Review)
- Deep codebase analysis completed — examined 20+ files across framework, Design, examples, and samples
- Created architectural plan at `docs/plans/ientityroot-interface.md`
- Identified 9 tensions/concerns with the `IEntityRoot` approach
- Key findings:
  - `IsSavable` is on `IEntityMetaProperties` (not directly on `IEntityBase`) — affects `IEntityListBase` and `LazyLoad<T>` too
  - Every entity interface in samples/examples extends `IEntityBase` — all roots must change to `IEntityRoot` (breaking change)
  - `Person.cs` uses `this.IsSavable` inside Insert/Update methods (concrete class access — still works)
  - Blazor UI binds to `Person.IsSavable` — requires `IPerson : IEntityRoot`
  - Serialization writes all `IEntityMetaProperties` properties via reflection — `IsSavable` will stop being serialized (correct, it's computed)
  - `EntityListBase` caches `IsSavable` in MetaState — becomes dead code after removal
- Design project verification code written:
  - `src/Design/Design.Domain/Aggregates/OrderAggregate/IOrderInterfaces.cs` — `IOrder : IEntityRoot` (fails: CS0246)
  - `src/Design/Design.Tests/AggregateTests/EntityRootInterfaceTests.cs` — tests exercising the pattern (fails: depends on IEntityRoot)
- Plan status: Under Review (Developer)

### 2026-03-01 (Design Decision)
- Decided on `IEntityRoot` interface approach after discussion
- Key insights: (1) child factory methods have signatures outside consumers can't fulfill (need parent entity/ID), (2) `internal` on entity classes limits access to the domain assembly, (3) the user choosing `IEntityRoot` vs `IEntityBase` on their interface is the explicit signal — no RemoteFactory involvement needed
- Rejected all other approaches — this eliminates the trap at the interface level rather than working around it
- Concrete `EntityBase<T>` unchanged — `IsSavable`/`Save()` stay as concrete members since entity classes should be `internal`

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] Design project builds successfully
- [x] Design project tests pass

**Verification results:**
- Design build: PASS (0 errors, 0 warnings)
- Design tests: PASS (89 passed, 0 failed)

---

## Results / Conclusions

Introduced `IEntityRoot : IEntityBase` interface with `IsSavable` and `Save()`. Child entities expose only `IEntityBase` — no `IsSavable`, no `Save()`. The user signals root vs child by choosing which interface their entity interface extends.

Key outcomes:
- The `IsSavable` trap is eliminated — child interfaces don't expose it, so developers can't misuse it
- `EntityListBase` dead code removed — `IsSavable` was always false on every child in the list
- `LazyLoad<T>` orphaned `IsSavable` removed
- 2144 tests pass, zero failures
- Design project has authoritative example at `Design.Domain/Aggregates/OrderAggregate/IOrderInterfaces.cs`
