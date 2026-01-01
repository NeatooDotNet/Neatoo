# M4: Resolve TODO Comments

**Priority:** Medium
**Category:** Technical Debt
**Effort:** Varies
**Status:** Not Started

---

## Problem Statement

Several TODO comments in the codebase indicate incomplete or uncertain implementations that need resolution.

---

## TODO Items Found

### 1. EntityBase.cs Line 259 - MarkUnmodified Busy Check

**Location:** `src/Neatoo/EntityBase.cs:259`

```csharp
protected virtual void MarkUnmodified()
{
    // TODO : What if busy??
    this.PropertyManager.MarkSelfUnmodified();
}
```

**Decision Needed:** What should happen if `MarkUnmodified()` is called while the object is busy (async validation running)?

**Options:**
1. Throw exception (safest, explicit)
2. Wait for tasks to complete then mark unmodified
3. Mark unmodified anyway (could cause inconsistency)
4. Queue the operation for when busy completes

**Recommendation:** Option 1 - Throw `InvalidOperationException` with clear message. The caller should await `WaitForTasks()` first.

```csharp
protected virtual void MarkUnmodified()
{
    if (IsBusy)
        throw new InvalidOperationException(
            "Cannot mark unmodified while busy. Call WaitForTasks() first.");

    PropertyManager.MarkSelfUnmodified();
}
```

---

### 2. EntityBase.cs Line 402 - WaitForTasks in Save

**Location:** `src/Neatoo/EntityBase.cs:402`

```csharp
if (this.IsBusy)
{
    // TODO await this.WaitForTasks(); ??
    throw new SaveOperationException(SaveFailureReason.IsBusy);
}
```

**Decision Needed:** Should `Save()` automatically wait for async tasks, or throw immediately?

**Options:**
1. Throw immediately (current behavior) - explicit, fast-fail
2. Auto-wait for tasks - convenient, but hides timing issues
3. Configurable via parameter - flexible

**Recommendation:** Keep throwing but improve the exception message:

```csharp
if (IsBusy)
{
    throw new SaveOperationException(SaveFailureReason.IsBusy)
    {
        HelpLink = "Consider calling await WaitForTasks() before Save()"
    };
}
```

Document that callers should use:
```csharp
await entity.WaitForTasks();
await entity.Save();
```

---

### 3. EntityPropertyManager.cs Line 29 - DisplayName Serialization

**Location:** `src/Neatoo/EntityPropertyManager.cs:29`

```csharp
[JsonConstructor]
public EntityProperty(..., string displayName, ...)
{
    this.DisplayName = displayName; // TODO - Find a better way than serializing this
}
```

**Issue:** `DisplayName` is serialized but could be derived from metadata.

**Options:**
1. Keep serializing (simple, works)
2. Derive from `[Display]` attribute on deserialization
3. Use a display name resolver service

**Recommendation:** Keep serializing for now. The overhead is minimal and it ensures consistent behavior. Add a note explaining why:

```csharp
// DisplayName is serialized to ensure consistent display across client/server
// even if metadata/resources differ between environments
this.DisplayName = displayName;
```

---

## Implementation Tasks

- [ ] Decide on MarkUnmodified busy behavior (TODO #1)
- [ ] Implement chosen solution for #1
- [ ] Decide on Save auto-wait behavior (TODO #2)
- [ ] Implement chosen solution for #2
- [ ] Document DisplayName serialization decision (TODO #3)
- [ ] Search for any other TODO comments
- [ ] Add tests for new behavior
- [ ] Remove TODO comments once resolved

---

## Search for Additional TODOs

Run this command to find all TODO comments:

```powershell
Get-ChildItem -Path "src/Neatoo" -Recurse -Include "*.cs" |
    Select-String -Pattern "TODO" |
    Format-Table Path, LineNumber, Line
```

---

## Resolution Tracking

| TODO | File:Line | Decision | Status |
|------|-----------|----------|--------|
| MarkUnmodified busy | EntityBase.cs:259 | Throw if busy | Pending |
| WaitForTasks in Save | EntityBase.cs:402 | Keep throwing, improve message | Pending |
| DisplayName serialization | EntityPropertyManager.cs:29 | Keep, add comment | Pending |

---

## Files to Modify

| File | Action |
|------|--------|
| `src/Neatoo/EntityBase.cs` | Resolve TODOs at lines 259 and 402 |
| `src/Neatoo/EntityPropertyManager.cs` | Add documentation comment at line 29 |
