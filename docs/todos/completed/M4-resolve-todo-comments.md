# M4: Resolve TODO Comments

**Priority:** Medium
**Category:** Technical Debt
**Effort:** Varies
**Status:** ✅ COMPLETE
**Completed:** 2026-01-04

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

## Implementation Tasks

- [x] Decide on MarkUnmodified busy behavior (TODO #1) - Throw if busy
- [x] Implement chosen solution for #1 - Added `InvalidOperationException` guard
- [x] Decide on Save auto-wait behavior (TODO #2) - Keep throwing, improve message
- [x] Implement chosen solution for #2 - Updated exception message in `Exceptions.cs`
- [x] Search for any other TODO comments - Found 4 design notes (see below)
- [x] Remove TODO comments once resolved
- [x] All tests pass (1,787 tests)

---

## Moved to Separate Plans

- **DisplayName serialization** (TODO #3) → [M4a-displayname-serialization.md](M4a-displayname-serialization.md)

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
| MarkUnmodified busy | EntityBase.cs:264 | Throw if busy | ✅ Done |
| WaitForTasks in Save | EntityBase.cs:426 | Keep throwing, improve message | ✅ Done |
| DisplayName serialization | EntityPropertyManager.cs:29 | Remove from serialization | ✅ Done → [M4a](completed/M4a-displayname-serialization.md) |

## Additional TODOs Found (Design Notes - Not Actionable)

These are design notes/future improvement ideas, not blocking issues:

| File | Line | Note | Type |
|------|------|------|------|
| `RuleBase.cs` | 58 | Replace OnRuleAdded with Factory Method | Design improvement |
| `EntityBase.cs` | 381 | Parent tracking for unassigned objects | Design note |
| `AsyncTasks.cs` | 9 | Add cancellation token | Feature request |
| `NeatooBaseJsonTypeConverter.cs` | 99 | Ugly code block | Code quality |

These can be addressed in future if needed, but don't represent incomplete functionality.

---

## Files to Modify

| File | Action |
|------|--------|
| `src/Neatoo/EntityBase.cs` | Resolve TODOs at lines 259 and 402 |
| `src/Neatoo/EntityPropertyManager.cs` | Add documentation comment at line 29 |
