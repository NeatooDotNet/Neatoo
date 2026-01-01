# H1: Fix RuleManager Assertion Logic

**Priority:** High
**Category:** Code Bug
**Effort:** Low
**Status:** Completed
**File:** `src/Neatoo/RuleManager.cs` lines 351-373
**Completed:** 2024-12-31

---

## Problem Statement

The assertion logic in `RuleManager.cs` was broken. The variable `setAtLeastOneProperty` was always `true` because:

1. It was initialized to `true` before the loop
2. It was set to `true` inside the loop
3. It was never set to `false`

This meant the `Debug.Assert()` always passed, defeating its purpose.

---

## Investigation Results

After initial fix attempt (changing init to `false`), tests revealed the assertion itself was incorrect:

1. The framework intentionally supports rules with trigger properties pointing to child objects
2. Comments in the code say "Allowing null trigger properties that may be on a child target"
3. When children are removed from lists, rules may reference properties that no longer exist
4. The `TryGetProperty` pattern already handles missing properties gracefully

The assertion was overly strict and conflicted with intentional framework behavior.

---

## Fix Applied

Removed the assertion entirely since:
1. The code already handles missing properties gracefully via `TryGetProperty`
2. Rules with child trigger properties are a supported scenario
3. The assertion was preventing legitimate operations (removing children from lists)

```csharp
// Before (broken - assertion always true)
var setAtLeastOneProperty = true;
// ... loops ...
Debug.Assert(setAtLeastOneProperty, "...");

// After (removed - the TryGetProperty pattern handles this gracefully)
// ... loops with TryGetProperty checks only ...
```

Also removed from:
- `Base.cs:WaitForTasks()` - similar broken assertion with unused `busyProperty` variable
- `ValidateBase.cs:RunRules()` - similar assertion

---

## Implementation Tasks

- [x] Read the full context of the assertion in `RuleManager.cs`
- [x] Verify the intended behavior with surrounding code
- [x] Initial fix: Changed initialization from `true` to `false`
- [x] Discovered assertion itself was incorrect for child property scenarios
- [x] Final fix: Removed the assertion entirely
- [x] Removed unused `setAtLeastOneProperty` variable
- [x] Verify existing tests pass (1620 passed, 1 skipped)

---

## Verification

Ran `dotnet test` - all 1620 tests pass. The 4 tests that were previously failing (due to the incorrect assertion in WaitForTasks/RunRules) now pass.

---

## Related

This fix is related to L3 (WaitForTasks assertion) - both were fixed together since they had the same root cause (incorrect assertions about busy state).
