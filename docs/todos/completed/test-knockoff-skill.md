# Test KnockOff Skill

**Status:** Complete
**Priority:** Medium
**Created:** 2026-01-22
**Last Updated:** 2026-01-22

---

## Problem

The KnockOff library was upgraded with API changes. Need to verify that the KnockOff skill documentation accurately reflects the new API and can guide developers through fixing build errors caused by API changes.

## Solution

Have the neatoo-ddd-architect use the KnockOff skill to validate the API changes in the `upgradeKnockOff` branch against the skill documentation.

---

## Plans

---

## Tasks

- [x] Review current test file changes against KnockOff skill documentation
- [x] Identify any API patterns not covered by the skill
- [x] Record skill shortcomings if unable to fix issues

---

## Progress Log

### 2026-01-22 - Initial Analysis

**Branch:** `upgradeKnockOff`

**Key API Changes Observed:**
1. `OnCall` callback signature changed - removed first `ko` parameter
   - Old: `stub.Method.OnCall = (ko, param1, param2) => ...`
   - New: `stub.Method.OnCall((param1, param2) => ...)`

2. `OnGet` property callback changed - removed `ko` parameter
   - Old: `stub.Property.OnGet = (ko) => value`
   - New: `stub.Property.OnGet = () => value`

3. Verification pattern changed
   - Old: `stub.Method.WasCalled` and `stub.Method.CallCount`
   - New: `stub.Method.Verify(Times.Once)`, `stub.Method.Verify(Times.Never)`

4. Indexer naming changed
   - Old: `stub.StringIndexer.OnGet`
   - New: `stub.Indexer.OnGet`

**Build Status:** Passes (migration already complete)
**Test Status:** All tests pass

### 2026-01-22 - Skill Validation Complete

**Files Reviewed:**
- `.claude/skills/knockoff-usage/SKILL.md` - Main skill file
- `.claude/skills/knockoff-usage/references/api-reference.md` - API reference
- `.claude/skills/knockoff-usage/references/methods.md` - Method interceptors
- `.claude/skills/knockoff-usage/references/properties.md` - Property interceptors
- `.claude/skills/knockoff-usage/references/patterns.md` - Stub patterns
- `.claude/skills/knockoff-usage/references/moq-migration.md` - Migration guide
- `src/Examples/Person/Person.DomainModel.Tests/UnitTests/PersonTests.cs`
- `src/Examples/Person/Person.DomainModel.Tests/UnitTests/UniqueNameRuleTests.cs`
- `src/Examples/Person/Person.DomainModel.Tests/UnitTests/UniquePhoneNumberRuleTests.cs`
- `src/Examples/Person/Person.DomainModel.Tests/UnitTests/UniquePhoneTypeRuleTests.cs`
- Generated stub files in `Generated/KnockOff.Generator/`

**What Works Well:**
- Stand-Alone, Inline Interface, and Inline Class patterns are well documented
- Method verification with Times constraints is documented correctly
- Indexer interception is documented in api-reference.md
- Moq migration guide is comprehensive
- The overall structure and organization is good

**Critical Issues Found:**
- Delegate stub pattern is completely missing
- OnGet/OnSet syntax in SKILL.md uses method-call syntax instead of property-assignment
- Two different OnCall syntaxes (method vs property) not distinguished

---

## Skill Shortcomings Found

### 1. Delegate Stub Pattern Not Documented

**Pattern Used in Tests:**
```csharp
[KnockOff<UniqueName.IsUniqueName>]  // IsUniqueName is a delegate type
public partial class UniqueNameRuleTests
{
    // ...
    var isUniqueStub = new Stubs.IsUniqueName();
    isUniqueStub.Interceptor.OnCall = (id, firstName, lastName, token) => Task.FromResult(true);
}
```

**Issue:** The skill documents three patterns (Stand-Alone, Inline Interface, Inline Class) but none cover stubbing **delegate types**. The actual API for delegate stubs differs:
- Delegates use `stub.Interceptor.OnCall = ...` (property assignment) rather than `stub.Method.OnCall(...)` (method call returning tracking object)
- The stub has an `Interceptor` property that contains `OnCall`, `Verify()`, and `LastCallArgs`
- Delegate stubs can be implicitly converted to the delegate type

**Recommendation:** Add a "Delegate Stub Pattern" section to `SKILL.md` and a new `references/delegates.md` file.

### 2. OnCall Property Assignment vs Method Call Distinction Not Explained

**Two syntaxes exist in the API:**

1. **For method interceptors:** `stub.Method.OnCall((args) => result)` - This is a METHOD that returns a tracking object
2. **For delegate interceptors:** `stub.Interceptor.OnCall = (args) => result` - This is a PROPERTY assignment

The skill documentation shows only the method syntax. Developers encountering delegate stubs will be confused when the familiar `stub.Interceptor.OnCall((args) => ...)` syntax doesn't compile.

**Test code examples:**
```csharp
// Delegate stub - property assignment
isUniqueStub.Interceptor.OnCall = (id, firstName, lastName, token) => Task.FromResult(true);

// Interface method stub - method call
personDbContextStub.FindPerson.OnCall((token) => Task.FromResult<PersonEntity?>(personEntity));
```

**Recommendation:** Clarify this distinction in the skill and reference files.

### 3. OnGet Property Syntax is Property Assignment, Not Method

**The skill shows:**
```csharp
stub.Property.OnGet(() => value);  // Looks like a method call
```

**Actual API (from tests):**
```csharp
stub.Property.OnGet = () => value;  // Property assignment
```

**Test code examples:**
```csharp
firstNameProp.IsModified.OnGet = () => isModified;
personStub.FirstName.OnGet = () => firstName;
phoneStub.PhoneNumber.OnGet = () => "1234567890";
```

**Issue:** The skill documentation shows `OnGet(callback)` syntax but the actual API uses `OnGet = callback` syntax. This will cause compilation errors for developers following the documentation.

**Locations to fix:**
- `SKILL.md` lines 116, 119 - shows `stub.Property.OnGet(() => ...)` should be `stub.Property.OnGet = () => ...`
- `references/properties.md` line 98 - shows `stub.Timestamp.OnGet = () => DateTime.UtcNow;` which IS correct
- `references/api-reference.md` line 173-174 shows assignment syntax which IS correct

The main SKILL.md is inconsistent with the reference docs.

### 4. OnSet Property Syntax Inconsistency

**Same issue as OnGet:** The skill shows `stub.Property.OnSet((value) => ...)` but actual API is `stub.Property.OnSet = (value) => ...`

**Test code example:**
```csharp
stub.Name.OnSet = (value) => setValues.Add(value);
```

### 5. Missing Indexer OnGet Signature with Key Parameter

**The skill shows indexer OnGet as:**
```csharp
stub.Indexer.OnGet = (k) => new User { Id = k, Name = "FromCallback" };
```

This IS documented correctly in `references/api-reference.md`, but the main skill file doesn't show that indexer OnGet takes a key parameter.

**Test code example:**
```csharp
personStub.Indexer.OnGet = (propName) => propName switch
{
    nameof(IPerson.FirstName) => firstNameProp,
    nameof(IPerson.LastName) => lastNameProp,
    _ => new Stubs.IEntityProperty()
};
```

### Summary of Required Skill Updates

1. **Add Delegate Stub Pattern** - New section in SKILL.md and new reference file
2. **Fix OnGet/OnSet Syntax** - Change method-call syntax to property-assignment syntax in SKILL.md
3. **Add Interceptor Property Documentation** - Document that delegate stubs use `stub.Interceptor` property
4. **Clarify Two OnCall Syntaxes** - Method interceptors use method calls; delegate interceptors use property assignment

---

## Results / Conclusions

**Validation Status:** COMPLETE - Issues Found

**Overall Assessment:** The KnockOff skill documentation is approximately 80% accurate for the common use cases (interface stubs with methods and properties). However, there are critical gaps that would cause confusion and compilation errors:

1. **Delegate stubs are not documented at all.** This is a significant gap since the tests demonstrate this pattern is used in production code.

2. **OnGet/OnSet syntax is inconsistent.** The main SKILL.md shows method-call syntax `OnGet(() => value)` but the actual API uses property-assignment `OnGet = () => value`. The reference docs are correct, but developers reading only SKILL.md will get compilation errors.

3. **Two OnCall syntaxes exist.** Method interceptors use `stub.Method.OnCall((args) => ...)` which returns a tracking object. Delegate interceptors use `stub.Interceptor.OnCall = ...` which is property assignment. This distinction is not explained.

**Recommendations:**
1. Fix OnGet/OnSet syntax in SKILL.md (lines 116, 119)
2. Add a "Delegate Stub Pattern" section to SKILL.md
3. Create `references/delegates.md` for detailed delegate stub documentation
4. Add a note explaining the two OnCall syntaxes

**Next Steps:** Create a follow-up todo to implement these skill documentation improvements.
