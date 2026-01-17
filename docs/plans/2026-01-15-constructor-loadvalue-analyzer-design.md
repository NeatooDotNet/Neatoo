# Constructor LoadValue Analyzer Design

## Problem

When properties are assigned in constructors using normal setters (`Name = "value"`), the assignment is tracked as a modification because the constructor runs outside of the factory pause mechanism. This causes `IsModified = true` unexpectedly after Fetch operations.

This bug is extremely difficult to diagnose. The correct pattern is to use `LoadValue()` in constructors:

<!-- pseudo:loadvalue-constructor-pattern -->
```csharp
// Wrong - tracks modification
public EntityObject(IEntityBaseServices<EntityObject> services) : base(services)
{
    Required = 1;  // IsModified becomes true
}

// Correct - no modification tracking
public EntityObject(IEntityBaseServices<EntityObject> services) : base(services)
{
    RequiredProperty.LoadValue(1);  // IsModified stays false
}
```
<!-- /snippet -->

## Solution

Add a Roslyn analyzer to the `Neatoo.BaseGenerator` project that:
1. Detects simple assignments to partial properties inside constructors
2. Reports an error diagnostic
3. Offers a code fix to transform to `LoadValue()`

## Diagnostic

| Property | Value |
|----------|-------|
| ID | `NEATOO001` |
| Title | Use LoadValue() in constructors |
| Message | Property '{0}' should be set using {0}Property.LoadValue() in constructors to avoid unintended modification tracking |
| Severity | Error |
| Category | Usage |

## Scope

**Flags:**
- Simple assignments (`=`) to partial properties
- Inside ANY constructor body (including `[Create]` constructors)
- In classes inheriting from `ValidateBase` or `EntityBase`

**Does not flag:**
- Assignments in methods (Fetch, Update, etc.) - factory pause is active
- Compound assignments (`+=`, `++`, etc.) - rare in constructors, can add later
- Non-partial properties
- Classes not inheriting from Neatoo base classes

## Implementation

### Files

Add to `Neatoo.BaseGenerator` project:
- `Analyzers/ConstructorPropertyAssignmentAnalyzer.cs`
- `Analyzers/ConstructorPropertyAssignmentCodeFixProvider.cs`

### Analyzer Flow

1. Register for `ConstructorDeclaration` syntax nodes
2. Get containing class, verify it inherits from Neatoo base class (bail if not)
3. Walk constructor body finding `SimpleAssignmentExpression` nodes
4. For each, check if left side matches a partial property name
5. Report diagnostic for matches

### Reused Logic

From existing `BaseGenerator.cs`:
- `ClassOrBaseClassIsNeatooBaseClass()` - inheritance check
- Partial property detection pattern (properties with `partial` keyword)

### Code Fix Transformation

<!-- pseudo:code-fix-transformation -->
```csharp
// Input
Name = "value";
Count = 42;
Child = someObject;

// Output
NameProperty.LoadValue("value");
CountProperty.LoadValue(42);
ChildProperty.LoadValue(someObject);
```
<!-- /snippet -->

## Test Cases

### Analyzer Tests

| Test | Input | Expected |
|------|-------|----------|
| Flags simple assignment | `Name = "value"` in ctor | Error |
| Flags `[Create]` constructor | `Name = "value"` in `[Create]` ctor | Error |
| Ignores methods | `Name = "value"` in `Fetch()` | No error |
| Ignores non-Neatoo classes | Regular class constructor | No error |
| Ignores non-partial properties | `public string Foo { get; set; }` | No error |
| Ignores compound assignments | `Count++` in ctor | No error |
| Ignores LoadValue calls | `NameProperty.LoadValue()` | No error |

### Code Fix Tests

| Test | Input | Output |
|------|-------|--------|
| Simple assignment | `Name = "value"` | `NameProperty.LoadValue("value")` |
| Complex expression | `Name = GetDefault()` | `NameProperty.LoadValue(GetDefault())` |
| Multiple assignments | Fix all in constructor | All transformed |

## Why Error Severity?

1. The bug is extremely hard to diagnose (`IsModified = true` with no obvious cause)
2. There is always a guaranteed workaround (`LoadValue()`)
3. The pattern should be enforced consistently - no exceptions
