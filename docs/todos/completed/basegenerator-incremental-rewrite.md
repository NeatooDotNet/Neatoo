# BaseGenerator Incremental Source Generator Rewrite

**Status:** Complete
**Priority:** Critical
**Created:** 2026-01-16
**Completed:** 2026-01-17

---

## Problem

The current `PartialBaseGenerator` stores `SemanticModel` in the `SemanticTargetResult` struct and passes it directly from the transform phase to the Execute phase. This **breaks Roslyn's incremental generator caching** and causes excessive recompilation.

### The Anti-Pattern (Current Code)

```csharp
// Line 12-69: SemanticTargetResult stores SemanticModel
internal readonly struct SemanticTargetResult : IEquatable<SemanticTargetResult>
{
    public SemanticModel? SemanticModel { get; }  // <-- THE PROBLEM

    public bool Equals(SemanticTargetResult other)
    {
        // Line 50: ReferenceEquals means ANY semantic change invalidates cache
        return ReferenceEquals(SemanticModel, other.SemanticModel) ...
    }
}

// Line 279: Transform stores SemanticModel
return SemanticTargetResult.Success(classDeclaration, context.SemanticModel);

// Line 355: Execute uses stored SemanticModel
var semanticModel = result.SemanticModel;
var classNamedSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
```

### Why This Is Wrong

1. **SemanticModel instances change frequently** - Every keystroke can create a new SemanticModel
2. **ReferenceEquals fails** - Even if the actual semantic content is identical, a new instance fails the equality check
3. **Cache invalidation** - Roslyn's incremental generator thinks the input changed and re-runs Execute
4. **Excessive regeneration** - This defeats the entire purpose of incremental generators

### The Correct Pattern

Roslyn incremental generators must:
1. **Transform phase**: Extract ONLY the data needed from SemanticModel into **value-equatable records/POCOs**
2. **Execute phase**: Use that cached data directly OR reconstruct semantic info from `SourceProductionContext.Compilation` if absolutely needed

---

## Solution

Rewrite `PartialBaseGenerator` to follow the incremental generator best practice:

1. Create equatable data models that capture all necessary information from SemanticModel
2. Transform phase extracts data into these models (no SemanticModel reference stored)
3. Execute phase uses cached data models to generate code

### Detection Strategy Simplification

**Key insight**: All Neatoo classes have `[Factory]` attribute (from RemoteFactory). This is how we find them.

For Pipeline 2 (non-factory Neatoo classes), these are classes that:
- Inherit from `ValidateBase<T>` or `EntityBase<T>`
- Do NOT have `[Factory]` attribute
- Still need minimal generation (backing fields + InitializePropertyBackingFields)

---

## Tasks

### Phase 1: Design Data Models

- [x] Design `NeatooClassInfo` record to replace `SemanticTargetResult`
  - Class name, namespace, accessibility
  - Type parameters (for generic classes)
  - Base type info (ValidateBase<T> or EntityBase<T> and the type argument)
  - Whether directly inheriting Neatoo base
  - Interface info (if I{ClassName} partial interface exists)

- [x] Design `PropertyInfo` record for partial properties
  - Property name, type, accessibility
  - Has setter (bool)
  - Needs interface declaration (bool)

- [x] Design `RuleExpressionInfo` for GetRuleId generation
  - Constructor rule invocations
  - Validation attribute rules

- [x] Design `MapperMethodInfo` for MapModifiedTo
  - Method declaration info
  - Property mappings needed

### Phase 2: Implement Transform Phase

- [x] Implement `ExtractNeatooClassInfo()` - called in transform
  - Extract all needed data from SemanticModel
  - Return `NeatooClassInfo` record (or error record)

- [x] Update `GetSemanticTargetForGeneration()` to return data model
  - Call `ExtractNeatooClassInfo()`
  - Return equatable record, NOT SemanticModel

- [x] Update `GetNonFactoryNeatooClass()` similarly

### Phase 3: Implement Execute Phase

- [x] Rewrite `Execute()` to use `NeatooClassInfo`
  - No SemanticModel.GetDeclaredSymbol() calls
  - All data comes from the cached record

- [x] Rewrite `ExecuteMinimalGeneration()` similarly

- [x] Rewrite `AddPartialProperties()` to use `PropertyInfo` records
  - No SemanticModel access

- [x] Rewrite `AddMapModifiedToMethod()` to use `MapperMethodInfo`
  - Pre-extract property matching in transform

- [x] Rewrite `GenerateGetRuleIdMethod()` to use `RuleExpressionInfo`
  - Already mostly syntax-based, just cleanup

- [x] Rewrite `GenerateInitializePropertyBackingFields()`
  - Use pre-extracted type parameter and base class info

### Phase 4: Remove Dependencies

- [x] Delete `SemanticTargetResult` struct
- [x] Remove `SemanticModel` property from `PartialBaseText`
- [x] Clean up `GetBaseClassDeclarationSyntax()` - may not be needed
- [x] Clean up `UsingStatements()` - extract usings in transform phase

### Phase 5: Testing & Verification

- [x] Ensure all existing tests pass
- [x] Add incremental caching test (verify no regeneration on unrelated changes)
- [x] Test with Person.DomainModel example
- [x] Verify generated output is identical to before

---

## Data Model Design

### NeatooClassInfo (primary result from transform)

```csharp
internal readonly record struct NeatooClassInfo(
    // Identity
    string ClassName,
    string Namespace,
    string ClassDeclarationText,  // modifiers + class + name + type params

    // Type info
    EquatableArray<string> TypeParameters,
    string? NeatooBaseTypeArgument,  // e.g., "Person" from ValidateBase<Person>
    bool IsDirectlyInheritingNeatooBase,
    bool NeedsCastToTypeParameter,

    // Interface
    bool HasPartialInterface,

    // Partial properties
    EquatableArray<PartialPropertyInfo> Properties,

    // Rules (for GetRuleId)
    EquatableArray<string> RuleExpressions,

    // Mapper method info
    EquatableArray<MapperMethodInfo> MapperMethods,

    // Using directives
    EquatableArray<string> UsingDirectives,

    // Error handling
    string? ErrorMessage,
    string? StackTrace
)
{
    public bool IsSuccess => ErrorMessage == null;
    public bool IsError => ErrorMessage != null;
}
```

### PartialPropertyInfo

```csharp
internal readonly record struct PartialPropertyInfo(
    string Name,
    string Type,
    string Accessibility,
    bool HasSetter,
    bool NeedsInterfaceDeclaration
);
```

### MapperMethodInfo

```csharp
internal readonly record struct MapperMethodInfo(
    string MethodDeclaration,  // partial method signature
    string ParameterName,
    EquatableArray<PropertyMapping> Mappings
);

internal readonly record struct PropertyMapping(
    string ClassPropertyName,
    string ParameterPropertyName,
    string ClassPropertyType,
    string ParameterPropertyType,
    bool NeedsNullCheck,
    bool NeedsTypeCast
);
```

### EquatableArray<T>

Need an `IEquatable<>` wrapper for collections:

```csharp
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    private readonly T[] _items;

    public EquatableArray(IEnumerable<T> items) => _items = items.ToArray();

    public bool Equals(EquatableArray<T> other)
    {
        if (_items.Length != other._items.Length) return false;
        for (int i = 0; i < _items.Length; i++)
        {
            if (!_items[i].Equals(other._items[i])) return false;
        }
        return true;
    }

    // IReadOnlyList implementation...
}
```

---

## Key Files to Modify

| File | Changes |
|------|---------|
| `src/Neatoo.BaseGenerator/BaseGenerator.cs` | Complete rewrite |
| `src/Neatoo.BaseGenerator/Models/` | New folder for data models |
| `src/Neatoo.BaseGenerator/EquatableArray.cs` | New - equatable collection wrapper |

---

## Progress Log

### 2026-01-16
- Created this todo document
- Analyzed current implementation in detail
- Identified all SemanticModel usage points
- Designed data model structure

### 2026-01-17
- Implemented complete rewrite following Clean Architecture approach
- Created new folder structure: Models/, Extractors/, Generators/
- Created EquatableArray<T> wrapper with structural equality for caching
- Created data model records (NeatooClassInfo, PartialPropertyInfo, etc.)
- Created extractor classes to extract data during transform phase
- Created generator classes to emit code during execute phase
- Rewrote BaseGenerator.cs from 1076 lines to ~190 lines
- Fixed netstandard2.0 compatibility issues (IsExternalInit polyfill, ToHashSet)
- All tests pass: BaseGenerator.Tests (26/26), Neatoo.UnitTest (1722/1722)
- Generated output verified identical to previous implementation

---

## References

- [Roslyn Incremental Generators Best Practices](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md)
- Current implementation: `src/Neatoo.BaseGenerator/BaseGenerator.cs`
- Generated example: `src/Examples/Person/Person.DomainModel/Generated/Neatoo.BaseGenerator/...`

---

## Results / Conclusions

### Implementation Summary

The BaseGenerator was completely rewritten to follow Roslyn incremental generator best practices:

1. **No SemanticModel stored in pipeline results** - All data is extracted into value-equatable records during the transform phase
2. **Proper caching** - `EquatableArray<T>` provides structural equality for collections, enabling Roslyn's incremental caching
3. **Clean separation of concerns**:
   - `Models/` - Immutable data records
   - `Extractors/` - Transform phase logic (accesses SemanticModel)
   - `Generators/` - Execute phase logic (uses cached data only)

### Files Created

| File | Purpose |
|------|---------|
| `Models/EquatableArray.cs` | ImmutableArray wrapper with structural equality |
| `Models/NeatooClassInfo.cs` | Primary data model for classes |
| `Models/PartialPropertyInfo.cs` | Property metadata |
| `Models/RuleExpressionInfo.cs` | Rule expressions for GetRuleId |
| `Models/MapperMethodInfo.cs` | Mapper method metadata |
| `Models/PropertyMapping.cs` | Property mapping for MapModifiedTo |
| `Extractors/NeatooClassExtractor.cs` | Main extraction orchestrator |
| `Extractors/PropertyExtractor.cs` | Partial property extraction |
| `Extractors/RuleExpressionExtractor.cs` | Rule expression extraction |
| `Extractors/MapperMethodExtractor.cs` | Mapper method extraction |
| `Extractors/UsingDirectivesExtractor.cs` | Using directive extraction |
| `Generators/SourceGenerator.cs` | Main code generation orchestrator |
| `Generators/PropertyGenerator.cs` | Property code generation |
| `Generators/MapperGenerator.cs` | MapModifiedTo generation |
| `Generators/RuleIdGenerator.cs` | GetRuleId generation |
| `Generators/InitializerGenerator.cs` | InitializePropertyBackingFields generation |
| `IsExternalInit.cs` | Polyfill for netstandard2.0 record struct support |

### Key Improvements

1. **Incremental caching now works correctly** - SemanticModel is no longer stored, preventing cache invalidation on every keystroke
2. **Code is more maintainable** - Clean separation into small, focused files
3. **Error handling preserved** - Same diagnostic reporting behavior
4. **Generated output identical** - No changes to the code that consuming projects see
