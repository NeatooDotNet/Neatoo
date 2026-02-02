# Create Design Source of Truth Projects

**Status:** Complete
**Priority:** High
**Created:** 2026-01-31
**Last Updated:** 2026-01-31 (All Phases Complete)

---

## Problem

Design decisions for the Neatoo framework are being lost, forgotten, or contradicted because there's no authoritative source of truth:

- **Codebase**: API is hard to deduce from source generator implementation (complex Roslyn code)
- **User documentation**: Structured for users, not AI comprehension; falls behind
- **Sample projects**: User-focused, not structured for API deduction
- **CLAUDE.md**: Causes confusion during design changes (AI reverts to "what was")
- **Tests**: Test coverage validates behavior but doesn't explain design rationale

This leads to:
- Repeated proposals of previously-rejected designs
- Enhancements that miss critical existing functionality
- Losing track of why certain design decisions were made
- Inconsistent API evolution across patterns and base classes

## Solution

Create a new `src/Design/` directory with actual C# projects specifically designed for Claude Code to understand the Neatoo API. These projects will:

1. **Be the authoritative design reference** - Updated first, everything else flows from it
2. **Include extensive comments** - Not just "what" but "what we didn't do and why"
3. **Cover the full public API** - All base classes, factory operations, validation rules
4. **Be fully functional** - Compiles and tests pass, ensuring accuracy
5. **Capture design evolution** - Commented-out code showing rejected approaches

### Key Characteristics

- Heavy comments including `// DID NOT DO THIS BECAUSE XYZ`
- Commented-out code showing alternatives that were rejected
- Comments tying back to Neatoo generator internals where important
- `// GENERATOR BEHAVIOR:` comments showing what code is generated
- Separate solution (`src/Design/Design.sln`) to avoid noise in main solution

### Design Update Workflow

```
Design Code → Design Plan → Updated Codebase + Design Code → Skills/Samples → Documentation
```

---

## Plans

- [Design Source of Truth - Implementation Plan](../plans/design-source-of-truth-plan.md)

---

## Tasks

### Phase 1: Foundation
- [x] Create `src/Design/` directory structure
- [x] Create `Design.sln` solution with project reference to Neatoo
- [x] Create `Design.Domain` project with basic entity definitions
- [x] Create `Design.Infrastructure` project for persistence interfaces
- [x] Create `Design.Tests` project with MSTest

### Phase 2: Base Class Documentation
- [x] Create demonstrations for all four base classes side-by-side
- [x] Add extensive comments explaining when to use each class
- [x] Document what the generator produces for each base class

### Phase 3: Factory Operations Coverage
- [x] [Create] operation documentation
- [x] [Fetch] operation documentation
- [x] [Insert], [Update], [Delete] operations documentation
- [x] [Execute] command documentation
- [x] [Remote] attribute and client-server boundary

### Phase 4: Property System
- [x] Getter<T>/Setter pattern documentation
- [x] State properties (IsModified, IsNew, IsValid, etc.)
- [x] Change tracking behavior

### Phase 5: Validation and Rules
- [x] Rule system documentation (RuleBase<T>, AsyncRuleBase<T>)
- [x] RuleManager fluent API
- [x] Property validation patterns

### Phase 6: Entity Examples
- [x] Employee entity (EntityBase with full CRUD)
- [x] Address child entity
- [x] AddressList (EntityListBase)
- [x] ValueObjects (ValidateBase, ValidateListBase) - renamed from ReadModels
- [x] ApproveEmployee command ([Execute])

### Phase 6a: Aggregate Pattern (NEW - from architect recommendations)
- [x] OrderAggregate with Order (root), OrderItem (child), OrderItemList (collection)
- [x] Document DeletedList lifecycle with extensive comments
- [x] Document intra-aggregate move handling

### Phase 6b: Generator and DI Documentation (NEW - from architect recommendations)
- [x] `Generators/TwoGeneratorInteraction.cs` - Document both generators
- [x] `DI/ServiceRegistration.cs` - AddNeatooServices() patterns
- [x] `DI/ServiceContracts.cs` - IValidateBaseServices, IEntityBaseServices

### Phase 7: Testing
- [x] Create tests for each base class
- [x] Create tests for factory operations
- [x] Create tests for aggregate patterns (OrderAggregate, DeletedList)
- [x] Ensure all tests pass on all target frameworks (71 tests)

### Phase 8: Documentation & Finalization
- [x] Create `README.md` and `CLAUDE-DESIGN.md`
- [x] Update main `CLAUDE.md` to reference design projects
- [x] Re-evaluate relationship with existing documentation

### Comment Requirements
- [x] At least 10 "DID NOT DO THIS BECAUSE" comments
- [x] At least 10 "DESIGN DECISION" comments
- [x] At least 5 "GENERATOR BEHAVIOR" comments
- [x] At least 5 "COMMON MISTAKE" comments

---

## Progress Log

**2026-01-31**: Created todo based on KnockOff's design-source-of-truth pattern. Reviewed KnockOff's completed implementation for reference.

**2026-01-31**: Architect review completed. Key findings:
- All four base classes adequately covered
- API coverage checklist expanded with missing items (state transitions, lifecycle hooks, serialization hooks, service interfaces, generator interaction)
- Directory structure recommendations applied:
  - Added `Aggregates/OrderAggregate/` for complete aggregate pattern
  - Renamed `ReadModels/` to `ValueObjects/` for DDD accuracy
  - Added `Generators/` for two-generator documentation
  - Added `DI/` for service registration patterns
- DeletedList lifecycle documentation added
- Two-generator interaction documentation added
- Plan status: Under Review (Developer)

**2026-01-31**: Phases 1-6b completed. Design.Domain builds successfully after fixing:
- `[Factory]` attribute requirement on all 43 classes
- Rule class override errors (CS0506/CS0507)
- RuleMessages API (None, AsRuleMessages(), fluent If())
- Command method naming (_MethodName convention)
- AddNeatooServices API signature
- List bases don't have PauseAllActions

**2026-01-31**: Phase 7 (Testing) completed:
- Created 14 test files with 71 tests total
- Test coverage: BaseClassTests (4 files), AggregateTests (2 files), FactoryTests (3 files), PropertyTests (2 files), RuleTests (3 files)
- Fixed test issues related to factory operation pausing behavior and rule triggering
- All 71 tests passing

**2026-01-31**: Phase 8 (Documentation) completed:
- Created `src/Design/README.md` - Purpose explanation for humans
- Created `src/Design/CLAUDE-DESIGN.md` - Claude-specific guidance with patterns and quick reference
- Updated `CLAUDE.md` with Design Source of Truth section

---

## Results / Conclusions

The Design Source of Truth project is complete. The `src/Design/` directory now contains:

**Design.Domain** - 43 classes demonstrating all Neatoo patterns:
- All four base classes (EntityBase, ValidateBase, EntityListBase, ValidateListBase)
- All factory operations ([Create], [Fetch], [Insert], [Update], [Delete], [Execute])
- Complete aggregate pattern with DeletedList lifecycle
- Property system with LoadValue vs SetValue
- Validation rules (class-based and fluent)
- Two-generator interaction documentation
- DI service registration patterns

**Design.Tests** - 71 tests verifying all documented patterns

**Documentation** - README.md, CLAUDE-DESIGN.md, and updated CLAUDE.md

Key learnings documented:
- Factory operations run with PauseAllActions - rules don't fire during Create/Fetch
- DeletedList only tracks non-new items (IsNew=false)
- Rule triggers require actual property value changes
- [Factory] attribute must be explicit (not inherited)

