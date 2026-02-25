# Make IsSavable More Intuitive

**Status:** In Progress
**Priority:** High
**Created:** 2026-02-24
**Last Updated:** 2026-02-24

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

## Solution

Make the API more intuitive so developers don't fall into this trap. Possible approaches (to be evaluated in planning):

1. **Rename or split** — e.g., `IsSavable` for the root-only check, `NeedsSave` or `HasPendingChanges` for the child-safe check
2. **Analyzer diagnostic** — Warn when `IsSavable` is accessed on a known child entity type
3. **Documentation + Design samples** — Add a prominent warning in Design.Domain showing the correct cascade pattern
4. **API change** — Remove `!IsChild` from `IsSavable` and add a separate `IsRoot` or `CanInitiateSave` property (aligns with CSLA's approach)
5. **Explicit child factory operations** — Add child-specific factory methods (like CSLA's `DataPortal.UpdateChild`) so the root-vs-child distinction is explicit at the call site, not hidden in a property
6. **Combination** — Multiple approaches together

**Note:** CSLA research suggests the deeper issue may not be `IsSavable` itself but the lack of explicit root-vs-child factory operations. Options 4 and 5 address the architectural root cause.

---

## Plans

_(To be created during architect review)_

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

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project builds successfully
- [ ] Design project tests pass

**Verification results:**
- Design build: [Pending]
- Design tests: [Pending]

---

## Results / Conclusions

[What was learned? What decisions were made?]
