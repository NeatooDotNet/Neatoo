# Bug: IsValid/IsSavable Stale During RunRules() in Factory Operations

**Status:** Complete
**Priority:** High
**Created:** 2026-03-03
**Last Updated:** 2026-03-03

## Problem

When `RunRules()` is called inside a factory operation (`[Insert]`, `[Update]`, `[Delete]`), `IsValid` and `IsSavable` return stale values. The common pattern `await RunRules(); if (!IsSavable) return;` is broken because `IsSavable` reads from `ValidatePropertyManager.IsValid`, which is cached and never recalculated while paused.

### Root Cause

1. `FactoryStart()` calls `PauseAllActions()` before the user's factory method runs
2. `RunRules()` executes rules correctly, rules set error messages on properties via `SetMessagesForRule()`
3. `SetMessagesForRule()` fires `PropertyChanged("IsValid")` on the `ValidateProperty`
4. `ValidatePropertyManager.Property_PropertyChanged()` returns early because `IsPaused == true`
5. `ValidatePropertyManager.IsValid` (cached, initialized to `true`) never gets recalculated
6. `this.IsSavable` checks `this.IsValid` which delegates to `PropertyManager.IsValid` -- still `true`
7. `FactoryComplete()` calls `ResumeAllActions()` which recalculates -- correct value, but too late

### Affected Code Path

```
User's [Insert] method
  -> await RunRules()
    -> RuleManager.RunRules()
      -> RequiredRule.Execute() finds empty field
        -> SetMessagesForRule() on ValidateProperty
          -> PropertyChanged("IsValid") fired on ValidateProperty
            -> ValidatePropertyManager.Property_PropertyChanged()
              -> if (this.IsPaused) return;  // <-- BUG: exits here
  -> if (!IsSavable)  // <-- reads stale cached true
```

### Impact

- The Person example (`Person.DomainModel`) uses this pattern in both Insert and Update
- The anti-pattern document (`business-rules-in-factory-methods-antipattern.md`) recommends this pattern
- Any entity using `[Required]` data annotations with RunRules inside factory methods is affected

## Solution

Add a `RecalculateValidity()` method to `ValidatePropertyManager` and call it from `ValidateBase` after `RunRules()` completes, ensuring cached `IsValid`/`IsSelfValid` are accurate regardless of paused state.

## Plans

- [IsValid Stale Cache Fix Plan](../plans/isvalid-stale-cache-fix.md)

## Tasks

- [ ] Add `RecalculateValidity()` to `ValidatePropertyManager`
- [ ] Call it from `ValidateBase.RunRules(RunRulesFlag)` after rule execution
- [ ] Call it from `ValidateBase.RunRules(string)` after rule execution
- [ ] Add Design project acceptance test
- [ ] Verify existing tests still pass

## Progress Log

- 2026-03-03: Bug confirmed, root cause analyzed, plan created, existing tests verified (4 fail, 3 pass)

## Results / Conclusions

Fix verified by architect. All 4 previously-failing acceptance tests now pass. Full test suite (1743 tests), Design.Tests (84 tests), and Person.DomainModel.Tests (55 tests) all pass with zero failures. Implementation matches the plan design exactly. No out-of-scope changes were made.
