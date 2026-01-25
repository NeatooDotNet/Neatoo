# KnockOff Upgrade Build Errors

**Status:** In Progress
**Priority:** High
**Created:** 2026-01-22
**Last Updated:** 2026-01-22

---

## Problem

After upgrading KnockOff dependency, the Neatoo solution has 47 build errors in Person.DomainModel.Tests. The errors indicate breaking API changes in KnockOff:

1. **CS1656**: "Cannot assign to 'OnCall' because it is a 'method group'" (27 occurrences)
2. **CS1593**: "Delegate 'Func<T>' does not take N arguments" (16 occurrences)
3. **CS1061**: Missing `WasCalled` and `CallCount` properties on interceptors (4 occurrences)

These errors suggest the KnockOff API has changed:
- `OnCall` may now be a method instead of a property
- Delegate signatures for property interceptors have changed
- Call tracking API (`WasCalled`, `CallCount`) may have been replaced

## Solution

Use the knockoff-usage skill to guide fixing all build errors by:
1. Identifying the correct new KnockOff API patterns
2. Updating test code to match the new API
3. Documenting any skill shortcomings or gaps encountered

This is a test of the knockoff-usage skill's ability to handle real-world migration scenarios.

---

## Plans

---

## Tasks

- [ ] Identify KnockOff API changes from build errors
- [ ] Fix CS1656 errors (OnCall assignments)
- [ ] Fix CS1593 errors (delegate signature mismatches)
- [ ] Fix CS1061 errors (missing WasCalled/CallCount)
- [ ] Verify all tests build successfully
- [ ] Document any knockoff skill gaps or shortcomings

---

## Progress Log

**2026-01-22**: Created todo. Build shows 47 errors across Person.DomainModel.Tests project. Launching neatoo-ddd-architect agent with knockoff skill to fix errors.

---

## Skill Shortcomings / Gaps

*(To be filled in by the architect agent during the fix process)*

---

## Results / Conclusions

*(To be filled when work completes)*
