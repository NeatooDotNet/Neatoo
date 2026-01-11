# Bug: RunRulesFlag.Messages and NoMessages Don't Work

The Messages and NoMessages flags in RunRulesFlag never match because PreviousMessages is never set.

**Created:** 2026-01-10
**Status:** Not Started
**Origin:** code-smells-review.md (discovered during ShouldRunRule refactoring)

---

## Problem

**Files:**
- `src/Neatoo/Rules/RuleBase.cs:207-210` - `PreviousMessages` never set
- `src/Neatoo/Rules/RuleManager.cs` - Should set `PreviousMessages` after running rule

**Issue:** `IRule.Messages` property always returns empty because `PreviousMessages` is never populated. This means:
- `RunRulesFlag.NoMessages` always matches (messages count is always 0)
- `RunRulesFlag.Messages` never matches

**Impact:** These flags are effectively broken and have been since they were created. Tests in `RuleManagerTests.cs` document this behavior.

## Options

1. **Fix it:** Have RuleManager set `PreviousMessages` on the rule after execution
2. **Remove it:** Delete these flags if they're not needed

## Tasks

- [ ] Decide: fix or remove
- [ ] If fix: Update RuleManager to set PreviousMessages after rule execution
- [ ] If remove: Delete Messages/NoMessages from RunRulesFlag enum
- [ ] Update tests accordingly
