# Replace Quoted Identifiers with nameof()

Exhaustive check for string literals that reference identifiers and can be replaced with `nameof()` for refactoring safety.

**Created:** 2026-01-11
**Status:** Pending
**Priority:** Low

---

## Problem

String literals referencing identifiers (parameter names, property names, method names) are fragile:
- Renaming via IDE refactoring doesn't update the string
- No compile-time error if the identifier no longer exists
- Easy to introduce typos

## Example Fixed

```csharp
// Before (fragile)
[CallerArgumentExpression("func")] string? sourceExpression = null

// After (refactoring-safe)
[CallerArgumentExpression(nameof(func))] string? sourceExpression = null
```

## Task List

- [ ] Search for `CallerArgumentExpression("` patterns
- [ ] Search for `nameof(` usages to understand current patterns
- [ ] Check `ArgumentNullException.ThrowIfNull` calls for string parameter names
- [ ] Check `ArgumentException` and similar for parameter name strings
- [ ] Check attribute usages with string parameters referencing members
- [ ] Check logging/debug strings that reference member names
- [ ] Review and update any findings

## Search Commands

```bash
# Find CallerArgumentExpression with string literals
grep -r 'CallerArgumentExpression("' --include="*.cs"

# Find ArgumentNullException patterns
grep -r 'ArgumentNullException.*"' --include="*.cs"

# Find nameof patterns for reference
grep -r 'nameof(' --include="*.cs" | head -50
```
