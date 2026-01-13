# Pseudo Block Recategorization Analysis

**Created:** 2026-01-12
**Status:** ANALYSIS COMPLETE - Awaiting decision

## Summary

Total pseudo blocks: ~150
Analysis based on docs-snippets skill guidelines.

## Marker Type Criteria

| Type | Criteria |
|------|----------|
| `snippet:` | Compilable C# - add to docs/samples |
| `pseudo:` | API signatures, hypothetical features, incomplete fragments |
| `invalid:` | Anti-patterns, intentionally wrong code |
| `generated:` | Source generator output |

## Categorization Results

### Category 1: Keep as `pseudo:` (API Signatures)

These are single-line or few-line API signatures for reference. Not meant to be compiled.

**Files:** meta-properties.md, property-system.md
**Example patterns:**
- `bool IsBusy { get; }`
- `Task WaitForTasks();`
- Interface definitions showing available members

**Count:** ~40 blocks
**Action:** Keep as `pseudo:`

### Category 2: Keep as `pseudo:` (Razor/Blazor Code)

Razor markup cannot be compiled in the C# samples project.

**Files:** meta-properties.md, blazor-binding.md, collections.md
**Example:**
```razor
<MudButton Disabled="@entity.IsBusy">Save</MudButton>
```

**Count:** ~15 blocks
**Action:** Keep as `pseudo:` (unless Blazor project added to samples)

### Category 3: Keep as `pseudo:` (Hypothetical/Planned Features)

Features not yet implemented in Neatoo.

**Files:** lazy-loading-pattern.md, lazy-loading-analysis.md
**Content:** Entire files describe planned lazy loading feature

**Count:** ~14 blocks
**Action:** Keep as `pseudo:` (document is clearly marked as "NOT YET IMPLEMENTED")

### Category 4: Keep as `pseudo:` (Internal Analysis)

Internal analysis documents, not user-facing documentation.

**Files:** DDD-Analysis.md
**Content:** Historical analysis with example patterns

**Count:** ~17 blocks
**Action:** Keep as `pseudo:` (analysis doc with illustrative examples)

### Category 5: Should Be `snippet:` (Compilable Code)

Complete, compilable code that should be in samples project.

**Files:** quick-start.md, factory-operations.md, validation-and-rules.md, exceptions.md, best-practices.md, installation.md

**Examples to convert:**
| File | ID | Content | Supporting Types Needed |
|------|----|---------|------------------------|
| quick-start.md | `qs-aggregate-root` | Complete Order entity | `OrderEntity`, `IOrderDbContext` |
| quick-start.md | `qs-interface-pattern` | IOrder interface | None |
| exceptions.md | `save-operation-exception` | Complete try/catch | `IPerson`, `IPersonFactory` |
| exceptions.md | `check-before-save` | Save workflow | Entity types |
| validation-and-rules.md | `pause-all-actions` | Using pattern | Entity type |
| best-practices.md | `factory-creation` | Factory usage | Entity types |

**Count:** ~50 blocks
**Action:** Add to samples project with supporting types

### Category 6: Already Correct - `invalid:` (Anti-patterns)

**Finding:** Anti-pattern blocks are already correctly marked with `invalid:` (17 blocks).

The pattern is:
- Pure anti-pattern code → `invalid:`
- Educational "WRONG then CORRECT" contrasts → `pseudo:` (acceptable as illustrative)

**Count:** 17 invalid blocks already exist
**Action:** No changes needed

---

## Recommended Approach

### Phase 1: Quick Marker Type Fixes
- [ ] Review pseudo blocks for anti-patterns → change to `invalid:`
- [ ] Review pseudo blocks for generated code → change to `generated:`

### Phase 2: Convert Priority Snippets (User-Facing Docs)
- [ ] quick-start.md - Create Order sample entity
- [ ] installation.md - Create setup examples
- [ ] best-practices.md - Create best practices samples

### Phase 3: Convert Reference Docs
- [ ] exceptions.md - Create exception handling samples
- [ ] factory-operations.md - Create operation samples
- [ ] validation-and-rules.md - Create rule samples

### Phase 4: Defer (Low Priority)
- Keep meta-properties.md as pseudo (API reference)
- Keep property-system.md as pseudo (API reference)
- Keep analysis docs as pseudo (internal)
- Keep lazy-loading docs as pseudo (not implemented)

---

## Decision Needed

How much conversion work is appropriate?

1. **Minimal:** Just fix marker types (pseudo → invalid/generated where needed)
2. **Moderate:** Convert user-facing docs (quick-start, installation, best-practices)
3. **Full:** Convert all compilable pseudo blocks to snippets

Each level increases quality but also effort.
