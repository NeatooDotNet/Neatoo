# Expose IsLoaded Check Without Triggering Load

**Status:** Not Started
**Priority:** High
**Created:** 2026-01-16

---

## Problem

When checking if a lazy-loaded child needs saving, accessing the property triggers the load:

```csharp
// This triggers lazy load even though we just want to check!
if (History.IsNew || History.IsModified)
{
    History = (IConsultationHistory)await History.Save();
}
```

The `IsLoaded` property exists on `IValidateProperty<T>` but is only accessible via the internal property object (e.g., `HistoryProperty.IsLoaded`), not through a clean public API.

---

## Solution

Expose a way to check if a property is loaded without triggering the load. Options:

### Option A: Indexer Returns Property Metadata

Already exists: `entity["PropertyName"]` returns `IValidateProperty`. Could use:

```csharp
if (this["History"].IsLoaded && (History.IsNew || History.IsModified))
{
    // Only access History if already loaded
}
```

**Status:** This already works today via the indexer.

### Option B: IsLoaded Helper Method

Add a helper method on the entity:

```csharp
if (IsLoaded(nameof(History)) && (History.IsNew || History.IsModified))
```

### Option C: Conditional Access Pattern

```csharp
if (HistoryProperty.IsLoaded && (History.IsNew || History.IsModified))
```

Requires exposing `{PropertyName}Property` publicly (currently internal/protected).

---

## Tasks

- [ ] Verify Option A works (indexer approach)
- [ ] Decide if additional API is needed
- [ ] Document the pattern for checking before accessing lazy properties
- [ ] Add example to lazy loading documentation

---

## Progress Log

### 2026-01-16
- Created todo from real-world usage scenario
- Identified that indexer approach may already solve this

---
