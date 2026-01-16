# Analyzer: Partial Class Requirement

**Status:** Pending
**Priority:** Medium
**Created:** 2026-01-16

---

## Problem

Classes inheriting from `ValidateBase<T>` or `EntityBase<T>` must be `partial` for the source generator to create `InitializePropertyBackingFields()`. Without `partial`:

- Code compiles without errors
- Properties silently fail at runtime (return defaults, no modification tracking, no rules)
- Difficult to diagnose

---

## Solution

Create analyzer NEATOO011 that:
1. Detects classes inheriting from `ValidateBase<T>` or `EntityBase<T>`
2. Reports error if class is not `partial`
3. Provides code fix to add `partial` keyword

---

## Tasks

- [ ] Create `PartialClassRequirementAnalyzer` in `Neatoo.BaseGenerator`
- [ ] Detect inheritance from ValidateBase/EntityBase (handle generic base classes)
- [ ] Report diagnostic NEATOO011 with clear message
- [ ] Create `PartialClassRequirementCodeFixProvider`
- [ ] Code fix adds `partial` keyword to class declaration
- [ ] Add unit tests for analyzer
- [ ] Add unit tests for code fix
- [ ] Document in troubleshooting.md

---

## Implementation Notes

### Diagnostic

```csharp
public static readonly DiagnosticDescriptor Rule = new(
    id: "NEATOO011",
    title: "Neatoo entity class must be partial",
    messageFormat: "Class '{0}' inherits from {1} and must be declared as partial",
    category: "Neatoo.Usage",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Classes inheriting from ValidateBase<T> or EntityBase<T> must be partial for the source generator to create property backing fields.");
```

### Detection Logic

```csharp
// Check if class inherits from ValidateBase<T> or EntityBase<T>
var baseType = classSymbol.BaseType;
while (baseType != null)
{
    var name = baseType.OriginalDefinition.ToDisplayString();
    if (name == "Neatoo.ValidateBase<T>" || name == "Neatoo.EntityBase<T>")
    {
        // Check if class is partial
        if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            // Report diagnostic
        }
        break;
    }
    baseType = baseType.BaseType;
}
```

### Code Fix

Add `partial` keyword before `class`:

```csharp
var newModifiers = classDeclaration.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
var newClassDeclaration = classDeclaration.WithModifiers(newModifiers);
```

---

## Progress Log

### 2026-01-16
- Created todo after identifying that non-partial classes silently fail
- This is a breaking change in 10.10.0 that should have compile-time detection

---

## Results / Conclusions

*To be completed when implemented.*
