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

### Why This Is a Trap

1. The name `IsSavable` reads as "does this entity have changes that need saving?" — a reasonable question during save cascades
2. The `!IsChild` condition is invisible unless you read the source code
3. There's no compiler warning, analyzer diagnostic, or runtime error — saves are just silently skipped
4. The correct pattern (checking `IsModified`/`IsNew` directly) is non-obvious

## Solution

Make the API more intuitive so developers don't fall into this trap. Possible approaches (to be evaluated in planning):

1. **Rename or split** — e.g., `IsSavable` for the root-only check, `NeedsSave` or `HasPendingChanges` for the child-safe check
2. **Analyzer diagnostic** — Warn when `IsSavable` is accessed on a known child entity type
3. **Documentation + Design samples** — Add a prominent warning in Design.Domain showing the correct cascade pattern
4. **API change** — Remove `!IsChild` from `IsSavable` and add a separate `IsRoot` or `CanInitiateSave` property
5. **Combination** — Multiple approaches together

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
