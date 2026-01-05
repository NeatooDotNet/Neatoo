# CancellationToken Support for Neatoo

**Priority:** Medium | **Category:** Feature Enhancement
**Created:** 2026-01-04
**Status:** Complete
**Completed:** 2026-01-04

## Implementation Summary

All phases have been implemented and tested:
- [x] Phase 1: Core Infrastructure (AsyncTasks, WaitForTasks)
- [x] Phase 2: Validation Layer (RunRules with Cancellation)
- [x] Phase 3: Entity Layer (Save Operations)
- [x] Phase 4: API Surface (Fluent Rules)
- [x] Unit Tests (19 new tests)
- [x] All 1594 tests passing

---

## Overview

Add comprehensive, unified `CancellationToken` support across all async operations in Neatoo. Currently, cancellation tokens exist in some places but are either not used effectively or missing entirely.

**Design Philosophy:** Cancellation is for stopping, not recovering. When validation is cancelled, the object is marked invalid via `MarkInvalid()`. Recovery requires calling `RunRules(RunRulesFlag.All)` to clear and re-validate.

---

## Current State Analysis

### Where CancellationToken Exists (But Is Ineffective)

| Location | Issue |
|----------|-------|
| `RuleBase.RunRule(CancellationToken?)` | Token passed but only checked **after** execution completes |
| `RuleManager.RunRules(CancellationToken?)` | Token passed to rules but no early exit from rule loop |
| Fluent rules (`WhenAsync`, `ThenAsync`) | Accept token but don't pass it to user `ExecuteFunc` delegates |

### Where CancellationToken Is Missing

| Location | Impact |
|----------|--------|
| `AsyncTasks.AddTask()` | Cannot cancel pending property setter async operations |
| `WaitForTasks()` | Cannot timeout waiting for async operations |
| `Save()` operations | Cannot cancel long-running save operations |

### Design Decisions

1. **Validation State After Cancellation**
   - Use existing `MarkInvalid("Validation cancelled")` mechanism
   - Object stays invalid until `RunRules(RunRulesFlag.All)` is called
   - Rationale: Cancellation typically means shutdown/close - no recovery needed. Safe default prevents saving half-validated objects.

2. **AsyncTasks Cancellation**
   - Support cancellation only for waiting, not for running tasks
   - Running tasks complete to avoid inconsistent entity state
   - `WaitForTasks(token)` throws `OperationCanceledException` if cancelled

3. **Save Operation Safety**
   - Only allow cancellation before persistence operations
   - Never cancel during `Insert`/`Update`/`Delete` (no rollback mechanism)

---

## Implementation Plan

### Phase 1: Core Infrastructure (AsyncTasks, WaitForTasks)

**Goal:** Add cancellation support to the async task management layer.

#### Task 1.1: Update AsyncTasks.cs

- [ ] Add `WaitForCompletion(CancellationToken? token = null)` method
- [ ] Register cancellation callback to cancel the wait
- [ ] Ensure running tasks complete (cancellation only affects waiting)

```csharp
public async Task WaitForCompletion(CancellationToken? token = null)
{
    if (token == null)
    {
        await AllDone;
        return;
    }

    var tcs = new TaskCompletionSource<bool>();
    using var registration = token.Value.Register(() => tcs.TrySetCanceled());

    var completedTask = await Task.WhenAny(AllDone, tcs.Task);
    if (completedTask == tcs.Task)
    {
        token.Value.ThrowIfCancellationRequested();
    }

    await AllDone; // Propagate any exceptions
}
```

#### Task 1.2: Update IValidateBase/ValidateBase

- [ ] Add `WaitForTasks(CancellationToken token)` overload
- [ ] Call `AsyncTasks.WaitForCompletion(token)`

```csharp
public async Task WaitForTasks(CancellationToken token)
{
    await this.AsyncTasks.WaitForCompletion(token);
}
```

---

### Phase 2: Validation Layer (RunRules with Cancellation)

**Goal:** Enable cancellation to short-circuit validation rule execution and mark object invalid.

#### Task 2.1: Update RuleManager.RunRules()

- [ ] Check `token.IsCancellationRequested` before each rule
- [ ] On cancellation, call `MarkInvalid("Validation cancelled")` and throw
- [ ] Re-throw `OperationCanceledException` so caller knows it was cancelled

```csharp
public async Task RunRules(string propertyName, CancellationToken? token = null)
{
    foreach (var rule in triggeredRules)
    {
        if (token?.IsCancellationRequested == true)
        {
            target.MarkInvalid("Validation cancelled");
            throw new OperationCanceledException(token.Value);
        }
        await rule.RunRule(token);
    }
}
```

#### Task 2.2: Update Individual Rule Execution

- [ ] Check cancellation before calling user code in `RuleBase.RunRule()`
- [ ] Check cancellation before calling user code in `AsyncRuleBase.RunRule()`
- [ ] Pass token to rule's `Execute()` method for user code to use

```csharp
// In AsyncRuleBase.RunRule()
public async Task<IRuleMessages> RunRule(CancellationToken? token = null)
{
    token?.ThrowIfCancellationRequested();

    // Pass token to Execute so user code can use it
    return await Execute(target, token);
}
```

---

### Phase 3: Entity Layer (Save Operations)

**Goal:** Add cancellation support to save operations with safety guarantees.

#### Task 3.1: Update ISavable/EntityBase

- [ ] Add `Save(CancellationToken token)` overload
- [ ] Only check cancellation BEFORE calling Insert/Update/Delete
- [ ] Never cancel during actual persistence operation

```csharp
public async Task<T> Save(CancellationToken token = default)
{
    await WaitForTasks(token); // Can be cancelled

    token.ThrowIfCancellationRequested(); // Pre-persistence check

    // NO cancellation checks during persistence
    if (IsNew) return await Insert();
    if (IsDeleted) return await Delete();
    return await Update();
}
```

#### Task 3.2: Update Generated Factory Methods

- [ ] Add `CancellationToken` parameter to `Insert()`, `Update()`, `Delete()` signatures
- [ ] Pass token through to any async operations in factory methods
- [ ] Document that factory methods should NOT check cancellation during DB operations

---

### Phase 4: API Surface (Fluent Rules)

**Goal:** Pass CancellationToken to user-provided delegates in fluent rules.

#### Task 4.1: Update Fluent Rule Delegates

Current delegates don't receive CancellationToken:
```csharp
// Current
.WhenAsync(async target => await CheckSomething(target))
.ThenAsync(async target => await DoSomething(target))
```

Add overloads that pass token:
```csharp
// Proposed
.WhenAsync(async (target, token) => await CheckSomething(target, token))
.ThenAsync(async (target, token) => await DoSomething(target, token))
```

#### Task 4.2: New Fluent Methods

- [ ] Add `WhenAsync(Func<T, CancellationToken, Task<bool>>)` overload
- [ ] Add `ThenAsync(Func<T, CancellationToken, Task<IRuleMessages>>)` overload
- [ ] Maintain backward compatibility with existing signatures

---

## Breaking Changes

### API Changes (Non-Breaking)

All changes add overloads; existing code continues to work:

| Existing Method | New Overload |
|-----------------|--------------|
| `WaitForTasks()` | `WaitForTasks(CancellationToken)` |
| `Save()` | `Save(CancellationToken)` |
| `RunRules()` | Already has token, behavior changes to check it |

### Behavioral Changes (Potentially Breaking)

| Change | Risk | Mitigation |
|--------|------|------------|
| `RunRules` calls `MarkInvalid` on cancellation | Low | Only affects code passing tokens |
| `RunRules` throws `OperationCanceledException` | Medium | Callers must handle if passing token |

---

## Testing Strategy

### Unit Tests

- [ ] `AsyncTasks_WaitForCompletion_CancellationToken_ThrowsOnCancel`
- [ ] `AsyncTasks_WaitForCompletion_RunningTasksComplete`
- [ ] `AsyncTasks_WaitForCompletion_CompletesNormallyWithoutToken`
- [ ] `RuleManager_RunRules_CancellationToken_ExitsEarly`
- [ ] `RuleManager_RunRules_CancellationToken_MarksInvalid`
- [ ] `RuleManager_RunRules_CancellationToken_ThrowsOperationCancelled`
- [ ] `ValidateBase_WaitForTasks_CancellationToken_Propagates`
- [ ] `AsyncRuleBase_Execute_ReceivesCancellationToken`

### Integration Tests

- [ ] `Entity_Save_CancellationToken_CancelsBeforePersistence`
- [ ] `Entity_Save_CancellationToken_NeverCancelsDuringPersistence`
- [ ] `Entity_CancelledValidation_IsInvalid`
- [ ] `Entity_CancelledValidation_RunRulesAllClears`
- [ ] `FluentRule_WhenAsync_CancellationToken_PassedToDelegate`

---

## Recovery Pattern

When validation is cancelled, the object is marked invalid. To recover:

```csharp
try
{
    await entity.RunRules(RunRulesFlag.All, cancellationToken);
}
catch (OperationCanceledException)
{
    // entity.IsValid == false
    // entity.ObjectInvalid == "Validation cancelled"
}

// Later, to recover:
await entity.RunRules(RunRulesFlag.All); // Clears ObjectInvalid and re-validates
```

---

## Documentation Updates

### Neatoo Documentation (`docs/`)

- [ ] Update `docs/validation-and-rules.md` with cancellation patterns
- [ ] Add "Cancellation Support" section to main documentation
- [ ] Document recovery pattern (RunRules.All clears cancelled state)
- [ ] Add examples showing proper cancellation usage
- [ ] Document `WaitForTasks(CancellationToken)` in async operations docs

### Neatoo Skill (`~/.claude/skills/neatoo/`)

- [ ] Add CancellationToken patterns to skill knowledge base
- [ ] Document when to use cancellation (shutdown, navigation, timeout)
- [ ] Add anti-patterns (don't cancel during persistence)
- [ ] Include recovery pattern guidance

---

## Files to Modify

### Source Code

| File | Changes |
|------|---------|
| `src/Neatoo/Internal/AsyncTasks.cs` | Add `WaitForCompletion(CancellationToken?)` |
| `src/Neatoo/ValidateBase.cs` | Add `WaitForTasks(CancellationToken)` overload |
| `src/Neatoo/Rules/RuleManager.cs` | Add early exit + `MarkInvalid` on cancellation |
| `src/Neatoo/Rules/RuleBase.cs` | Check cancellation before execute |
| `src/Neatoo/Rules/AsyncRuleBase.cs` | Check cancellation before execute, pass to Execute |
| `src/Neatoo/Rules/FluentValidation/*.cs` | Add token-accepting overloads |
| `src/Neatoo/EntityBase.cs` | Add `Save(CancellationToken)` |
| `src/Neatoo.Generator/FactoryGenerator.cs` | Add token to generated methods |

### Documentation

| File | Changes |
|------|---------|
| `docs/validation-and-rules.md` | Add cancellation patterns section |
| `docs/async-operations.md` | Create or update with WaitForTasks(token) |
| `~/.claude/skills/neatoo/*.md` | Add cancellation patterns and anti-patterns |

---

## Estimated Scope

| Phase | Complexity | Test Coverage |
|-------|------------|---------------|
| Phase 1 (AsyncTasks) | Low | 5-10 tests |
| Phase 2 (Validation) | Low | 8-12 tests |
| Phase 3 (Entity) | Low | 5-10 tests |
| Phase 4 (Fluent) | Medium | 10-15 tests |
| Documentation | Low | N/A |
| Skill Update | Low | N/A |

---

*Extracted from future-enhancements.md item #3 - 2026-01-04*
*Updated 2026-01-04: Simplified to use MarkInvalid instead of IsValidationPending*
