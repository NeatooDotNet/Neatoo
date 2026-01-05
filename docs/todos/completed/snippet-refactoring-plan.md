# Documentation Snippet Refactoring Plan

**Status:** ✅ COMPLETE
**Created:** 2026-01-03
**Completed:** 2026-01-03

## Problem Statement

The current documentation embeds full, compilable code examples where focused snippets would communicate concepts more effectively. Readers must visually parse through class definitions, constructors, interfaces, and boilerplate to find the 2-5 lines that demonstrate the actual concept.

### Current Pattern (Problem)

```csharp
<!-- snippet: docs:aggregates-and-entities:entitybase-basic -->
/// <summary>
/// Basic EntityBase example showing automatic state tracking.
/// </summary>
public partial interface IOrder : IEntityBase
{
    Guid Id { get; set; }
    string? Status { get; set; }
    decimal Total { get; set; }
}

[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services)
    {
        RuleManager.AddValidation(
            t => t.Total <= 0 ? "Total must be greater than zero" : "",
            t => t.Total);
    }

    public partial Guid Id { get; set; }
    public partial string? Status { get; set; }     // IsModified tracked automatically
    public partial decimal Total { get; set; }      // IsSavable updated on change

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
        Status = "New";
    }
}
<!-- /snippet -->
```

**Issue:** 33 lines to demonstrate "EntityBase tracks state automatically." The concept is buried.

### Target Pattern (Goal)

Documentation should show focused snippets with minimal distraction:

```csharp
// EntityBase provides automatic state tracking
public partial string? Status { get; set; }     // IsModified tracked automatically
public partial decimal Total { get; set; }      // IsSavable updated on change
```

Full examples remain in sample files for those who need compilable references.

---

## Complete Snippet Audit

### Summary Statistics

| Category | Count | Target Action |
|----------|-------|---------------|
| ✅ Good (≤15 lines or complete examples) | 16 | Keep as-is |
| ⚠️ Refactor (16-40 lines) | 20 | Add nested micro-snippets |
| ❌ Too Large (>40 lines, not review examples) | 10 | Break up into focused snippets |

**Note:** Complete examples at the end of docs are intentionally large - they serve as "putting it all together" references.

---

### aggregates-and-entities.md Snippets

| Snippet ID | Lines | Status | Action |
|------------|-------|--------|--------|
| `entitybase-basic` | 33 | ⚠️ | Add nested regions for state-tracking, constructor |
| `interface-requirement` | 9 | ✅ | Keep |
| `aggregate-root-class` | 29 | ⚠️ | Add nested regions for structure elements |
| `partial-properties` | 33 | ⚠️ | Already has nested `non-partial-properties` (6 lines ✅) |
| `non-partial-properties` | 6 | ✅ | Keep (good micro-snippet) |
| `data-annotations` | 27 | ⚠️ | Add nested regions for attribute examples |
| `value-object` | 27 | ⚠️ | Add nested regions for POCO pattern, Fetch |
| `validatebase-criteria` | 40 | ⚠️ | Add nested regions for inline rule, declaration |
| `child-entity` | 39 | ⚠️ | Add nested regions for parent access pattern |
| `complete-example` | **157** | ✅ | **Keep at end of doc as review** |

### validation-and-rules.md Snippets

| Snippet ID | Lines | Status | Action |
|------------|-------|--------|--------|
| `age-validation-rule` | 20 | ⚠️ | Add nested regions for key patterns |
| `unique-email-rule` | 24 | ⚠️ | Add nested regions for async pattern |
| `rule-registration` | **63** | ❌ | Break into: interface, entity, rule impl snippets |
| `trigger-properties` | 19 | ⚠️ | Consider splitting constructor vs method approach |
| `returning-messages-single` | 11 | ✅ | Keep |
| `returning-messages-multiple` | 15 | ✅ | Keep |
| `returning-messages-conditional` | 14 | ✅ | Keep |
| `returning-messages-chained` | 12 | ✅ | Keep |
| `fluent-validation` | 5 | ✅ | **Keep (good micro-snippet)** |
| `fluent-validation-async` | 5 | ✅ | **Keep (good micro-snippet)** |
| `fluent-action` | 5 | ✅ | **Keep (good micro-snippet)** |
| `required-attribute` | 6 | ✅ | Keep (nested in wrapper) |
| `stringlength-attribute` | 12 | ✅ | Keep (nested in wrapper) |
| `minmaxlength-attribute` | 16 | ⚠️ | Consider splitting string vs collection |
| `range-attribute` | 20 | ⚠️ | Consider splitting by type (int, decimal, date) |
| `regularexpression-attribute` | 12 | ✅ | Keep |
| `emailaddress-attribute` | 6 | ✅ | Keep |
| `combining-attributes` | 13 | ✅ | Keep |
| `date-range-rule` | 20 | ⚠️ | Add nested region for key validation pattern |
| `parent-child-validation` | **82** | ❌ | Break into: rule class, entity with parent access |
| `complete-rule-example` | **41** | ✅ | Keep at end of doc as review |

### Other Sample Files (Not in main docs but compiled)

| Snippet ID | Lines | Status | Notes |
|------------|-------|--------|-------|
| `async-action-rule` | 33 | ⚠️ | Compile-only, docs use inline example |
| `pause-all-actions` | 42 | ❌ | Compile-only |
| `manual-execution` | 38 | ⚠️ | Compile-only |
| `load-property` | 42 | ❌ | Compile-only |
| `validation-messages` | 50 | ❌ | Compile-only |
| `ismodified-check` | 48 | ❌ | Compile-only |
| `aggregate-root-pattern` | 57 | ❌ | Shows [Remote] pattern |
| `child-entity-pattern` | 74 | ❌ | Shows no-[Remote] pattern |

---

### Good Patterns to Replicate

| File | Pattern | Why It Works |
|------|---------|--------------|
| `FluentRuleSamples.cs` | Nested regions inside constructor | 5-line micro-snippets extracted from compilable class |
| `DataAnnotationSamples.cs` | Nested regions in wrapper class | Each attribute = 6-16 line focused snippet |
| `RuleBaseSamples.cs` | Single-purpose rule classes | Self-contained, shows complete pattern |
| `non-partial-properties` | Nested region | 6 lines showing exact concept |

---

## Refactoring Strategy

### Principle: Compilable Wrapper + Micro-Snippets

```csharp
// Full compilable class (not shown in docs)
[Factory]
internal partial class SnippetWrapper : EntityBase<SnippetWrapper>, ISnippetWrapper
{
    public SnippetWrapper(IEntityBaseServices<SnippetWrapper> services) : base(services) { }

    #region docs:concept:micro-snippet-1
    // This small region appears in documentation
    [Required]
    public partial string? Name { get; set; }
    #endregion

    #region docs:concept:micro-snippet-2
    [Range(0, 100)]
    public partial int Percentage { get; set; }
    #endregion

    [Create]
    public void Create() { }
}
```

Documentation shows only the 2-3 lines from each micro-snippet, while the full class compiles and is tested.

---

## Task List

### Phase 1: Audit & Categorize ✅ COMPLETE

- [x] List all current snippets with line counts
- [x] Categorize each: keep / refactor to micro-snippet / remove from docs
- [x] Identify documentation sections that need snippet references updated

### Phase 2: Refactor Sample Files ✅ COMPLETE

**High Priority - Used in aggregates-and-entities.md:**

- [x] **EntityBaseSamples.cs** - Add nested regions
  - [x] `state-tracking-properties` (3 lines)
  - [x] `inline-validation-rule` (4 lines)
  - [x] `class-declaration` (2 lines)
  - [x] `entity-constructor` (1 line)
  - [x] `partial-property-declaration` (3 lines)
  - [x] `displayname-required` (3 lines)
  - [x] `emailaddress-validation` (3 lines)

- [x] **ValidateBaseSamples.cs** - Add nested regions
  - [x] `validatebase-declaration` (2 lines)
  - [x] `criteria-inline-rule` (9 lines)
  - [x] `criteria-date-properties` (2 lines)

- [x] **ValueObjectSamples.cs** - Add nested regions
  - [x] `value-object-declaration` (2 lines)
  - [x] `value-object-properties` (2 lines)
  - [x] `value-object-fetch` (6 lines)

- [x] **ChildEntitySamples.cs** - Add nested regions
  - [x] `parent-access-property` (2 lines)
  - [x] `remote-fetch` (4 lines)
  - [x] `remote-insert` (3 lines)
  - [x] `child-fetch-no-remote` (3 lines)

- [x] **CompleteExampleSamples.cs** - Keep at end of doc as review
  - [x] Keep embedded in docs (serves as "putting it all together" reference)

**Medium Priority - Used in validation-and-rules.md:**

- [x] **RuleUsageSamples.cs** - Break up large snippets
  - [x] `rule-interface-definition` (3 lines)
  - [x] `entity-rule-injection` (8 lines)
  - [x] `rule-manager-addrule` (2 lines)
  - [x] `parent-child-rule-class` (18 lines)
  - [x] `parent-access-in-rule` (3 lines)

- [x] **RuleBaseSamples.cs** - Already good
  - [x] `complete-rule-example`: Keep as-is (review example at end of doc)

**Already Good - No Changes:**

- [x] `FluentRuleSamples.cs` - Nested regions working (5 lines each)
- [x] `DataAnnotationSamples.cs` - Nested regions working (6-16 lines each)
- [x] Return message snippets - Already focused (11-15 lines)

### Phase 3: Update Documentation ✅ COMPLETE

- [x] **aggregates-and-entities.md**
  - [x] Replace `entitybase-basic` (33 lines) with `state-tracking-properties` (3 lines)
  - [x] Replace `aggregate-root-class` (29 lines) with focused snippets
  - [x] Keep `complete-example` (157 lines) at END of doc as review/reference
  - [x] Add contextual intro text before each micro-snippet

- [x] **validation-and-rules.md**
  - [x] Replace `rule-registration` (63 lines) with focused snippets
  - [x] Replace `parent-child-validation` (82 lines) with focused snippets
  - [x] Verify all data annotation snippets remain ≤15 lines

- [ ] **Other docs using snippets** (future work)
  - [ ] Review factory-operations.md
  - [ ] Review collections.md
  - [ ] Review property-system.md

### Phase 4: Verify & Test ✅ COMPLETE

- [x] Run `dotnet build` - samples still compile
- [x] Run `dotnet test` - 148/149 tests pass (1 pre-existing failure unrelated to refactoring)
- [x] Run `.\scripts\extract-snippets.ps1 -Update` - snippets synced
- [x] Visual review: doc snippets now 3-10 lines (target achieved)

---

## Example Refactoring: EntityBase Section

### Before (Current)

```markdown
### EntityBase<T>

<!-- snippet: docs:aggregates-and-entities:entitybase-basic -->
[33 lines of full class definition]
<!-- /snippet -->
```

### After (Target)

```markdown
### EntityBase<T>

EntityBase provides state tracking properties for persistence:

<!-- snippet: docs:aggregates-and-entities:entitybase-state-tracking -->
```csharp
public partial string? Status { get; set; }     // IsModified tracked automatically
public partial decimal Total { get; set; }      // IsSavable updated on change
```
<!-- /snippet -->

Constructor pattern:

<!-- snippet: docs:aggregates-and-entities:entitybase-constructor -->
```csharp
public Order(IEntityBaseServices<Order> services) : base(services) { }
```
<!-- /snippet -->

See [EntityBaseSamples.cs](../src/Neatoo.Documentation.Samples/AggregatesAndEntities/EntityBaseSamples.cs) for complete example.
```

---

## Success Criteria

1. **No documentation snippet exceeds 15 lines** (target: 3-10 lines)
2. **Each snippet demonstrates exactly one concept**
3. **Sample files remain compilable and tested**
4. **Documentation reads as quick-reference**, not tutorial
5. **"Complete example" links exist** for those who need full context

---

## Related Documents

- [Documentation Samples Project](completed/documentation-samples-project.md)
- [Documentation Feedback Review](completed/documentation-feedback-review.md)

---

*Created from analysis of current documentation structure - January 3, 2026*
