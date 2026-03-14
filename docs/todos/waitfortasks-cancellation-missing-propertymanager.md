# WaitForTasks CancellationToken Missing PropertyManager.WaitForTasks

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-03-13
**Last Updated:** 2026-03-13

---

## Problem

`ValidateBase.WaitForTasks(CancellationToken)` only awaits `RunningTasks.WaitForCompletion(token)`. It does not await `PropertyManager.WaitForTasks()`, unlike the parameterless `WaitForTasks()` overload which does both.

This means the cancellation-token overload may return before property-level async tasks (validation rules, lazy loading) have completed.

Discovered during developer review of the LazyLoad auto-trigger plan. Pre-existing issue, not a regression.

**Current code** (`src/Neatoo/ValidateBase.cs` lines 681-684):
```csharp
public virtual async Task WaitForTasks(CancellationToken token)
{
    await this.RunningTasks.WaitForCompletion(token);
}
```

**Parameterless overload** (lines 663-668):
```csharp
public virtual async Task WaitForTasks()
{
    await this.RunningTasks.AllDone;
    await this.PropertyManager.WaitForTasks();
}
```

## Solution

The `CancellationToken` overload should also await `PropertyManager.WaitForTasks()` and (once the LazyLoad auto-trigger plan is complete) `WaitForLazyLoadChildren()`, consistent with the parameterless overload.

---

## Clarifications

---

## Requirements Review

**Reviewer:** [pending]
**Reviewed:** [pending]
**Verdict:** Pending

### Relevant Requirements Found

### Gaps

### Contradictions

### Recommendations for Architect

---

## Plans

---

## Tasks

- [ ] Investigate whether PropertyManager.WaitForTasks() supports cancellation
- [ ] Align CancellationToken overload with parameterless overload
- [ ] Verify no tests rely on current (incomplete) behavior

---

## Progress Log

### 2026-03-13
- Created from developer review concern during LazyLoad auto-trigger plan review
- Pre-existing inconsistency in `ValidateBase.cs` lines 663-684

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass
- [ ] All tests pass

**Verification results:**
- Build: [Pending]
- Tests: [Pending]

---

## Results / Conclusions

