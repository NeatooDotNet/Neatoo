# Pseudo to Snippet Conversion Plan

Convert pseudo-marked C# code blocks to compiled samples in `docs/samples/`.

**Created:** 2026-01-11
**Status:** In Progress

## Progress Summary

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| snippet markers (mdsnippets) | ~98 | 98 | 0 |
| pseudo markers | 137 | 122 | -15 |
| invalid markers | 13 | 12 | -1 |

**Note:** Snippet count from verify-code-blocks.ps1. 10 blocks converted to snippets, 2 duplicate pseudo blocks removed, 3 blocks already had compiled equivalents.

## Completed Conversions

### remote-factory.md (7 blocks)
- [x] `remote-attribute-patterns` - Entity with [Remote] operations
- [x] `aggregate-vs-child-patterns` - Aggregate root vs child patterns
- [x] `email-validation-rule` - Sync email validation rule
- [x] `unique-email-rule-async` - Async email uniqueness rule
- [x] `client-email-service` - Client HTTP service
- [x] `server-email-service` - Server database service
- [x] `authorization-pattern` - PersonAuth implementation

### testing.md (3 blocks)
- [x] `entity-unit-test-example` - TestableProduct unit test
- [x] `testing-null-parent` - Null parent handling test
- [x] `correct-real-neatoo-class` - TestPerson : ValidateBase

### collections.md (duplicates removed)
- [x] `custom-add-methods` - Removed, references `list-implementation`
- [x] `complete-personphonelist` - Removed, references existing snippets

---

## Remaining Work

| Priority | Count | Description |
|----------|-------|-------------|
| High | 0 | ~~Complete class/interface definitions~~ **DONE** |
| Medium | 20 | Complete usage examples |
| Low | 15 | Short fragments (optional) |
| Keep Pseudo | ~45 | API signatures, framework internals |

---

## High Priority: Complete Class Definitions

These are fully compilable classes that should be in samples.

### Rules and Validation

- [x] `pseudo:email-validation-rule` - Converted to snippet
- [x] `pseudo:unique-email-rule-async` - Converted to snippet

### Services

- [x] `pseudo:client-email-service` - Converted to snippet
- [x] `pseudo:server-email-service` - Converted to snippet

### Authorization

- [x] `pseudo:authorization-pattern` - Converted to snippet

### Entity Patterns

- [x] `pseudo:remote-attribute-patterns` - Converted to snippet
- [x] `pseudo:aggregate-vs-child-patterns` - Converted to snippet

### Collections

- [x] `pseudo:custom-add-methods` - **REMOVED** (duplicate of `list-implementation`)
- [x] `pseudo:complete-personphonelist` - **REMOVED** (duplicate of existing snippets)

### Testing

- [x] `pseudo:correct-real-neatoo-class` - Converted to snippet
- [x] `pseudo:entity-unit-test-example` - Converted to snippet
- [x] `pseudo:testing-null-parent` - Converted to snippet

---

## Medium Priority: Complete Usage Examples

These are complete working code that demonstrates patterns.

### Meta-Properties Usage

- [ ] `pseudo:isvalid-vs-isselfvalid-example` - `meta-properties.md:141-164`
  - 20+ lines showing IsValid vs IsSelfValid behavior
  - Complete working demonstration

- [ ] `pseudo:markmodified-state-progression` - `meta-properties.md:325-342`
  - Shows MarkModified state transitions
  - Complete usage example

- [ ] `pseudo:root-usage` - `meta-properties.md:435-447`
  - Shows Root property navigation
  - Complete example with order/line/detail

- [ ] `pseudo:common-ui-patterns` - `meta-properties.md:675-695`
  - Complete save workflow pattern
  - WaitForTasks + IsSavable check

### Property System

- [ ] `pseudo:complete-property-access` - `property-system.md:939-974`
  - Complete property manipulation example
  - Shows all property operations

- [ ] `pseudo:subscription-example` - `property-system.md:786-799`
  - Complete NeatooPropertyChanged event subscription
  - Shows subscribe/handler/unsubscribe pattern

- [ ] `pseudo:nested-property-tracking` - `property-system.md:806-815`
  - Shows tracking nested property changes
  - Complete event handler

- [ ] `pseudo:cross-item-validation` - `property-system.md:825-844`
  - Complete `HandleNeatooPropertyChanged` override
  - Re-validates siblings on property change

- [ ] `pseudo:blazor-state-management` - `property-system.md:870-895`
  - Complete Blazor component with NeatooPropertyChanged
  - Shows proper subscription/disposal pattern

### Collections

- [ ] `pseudo:intra-aggregate-moves` - `collections.md:234-249`
  - Complete example moving entity between lists
  - Shows DeletedList behavior

- [ ] `pseudo:update-with-deletedlist` - `collections.md:210-221`
  - Shows proper Update iteration pattern
  - Critical pattern for persistence

### Remote Factory

- [ ] `pseudo:serialization-behavior` - `remote-factory.md:140-160`
  - Shows JSON serialization structure
  - Complete example with state

- [ ] `pseudo:object-identity-demo` - `remote-factory.md:166-176`
  - Demonstrates object identity after remote ops
  - Complete example

- [ ] `pseudo:validation-failure-handling` - `remote-factory.md:386-398`
  - Complete validation failure handling
  - Shows PropertyMessages iteration

### Testing

- [ ] `pseudo:rule-testing-approach` - `testing.md:371-383`
  - Complete unit + integration test example
  - Shows both testing styles

- [ ] `pseudo:what-to-assert` - `testing.md:175-189`
  - Complete assertion patterns
  - Shows message checking

- [ ] `pseudo:mocking-ismodified` - `testing.md:240-246`
  - Complete mock setup for IsModified
  - Common pattern for rule tests

- [ ] `pseudo:verifying-dependency-calls` - `testing.md:252-258`
  - Complete mock verification patterns
  - Shows Verify/Times usage

---

## Low Priority: Short Fragments

These are short but could still be compiled for verification.

### Property Operations

- [ ] `pseudo:check-modification` - `property-system.md:223-232`
  - 5 lines checking IsModified

- [ ] `pseudo:clear-modification` - `property-system.md:236-243`
  - 4 lines clearing modification

- [ ] `pseudo:property-messages-usage` - `property-system.md:250-263`
  - Property message iteration

- [ ] `pseudo:busy-state-checking` - `property-system.md:282-294`
  - IsBusy checking pattern

- [ ] `pseudo:entity-level-busy` - `property-system.md:299-308`
  - Entity-level busy state

- [ ] `pseudo:property-indexer-usage` - `property-system.md:149-159`
  - Property indexer access

### Meta-Properties

- [ ] `pseudo:waitfortasks-usage` - `meta-properties.md:73-77`
  - 3 lines using WaitForTasks

- [ ] `pseudo:waitfortasks-cancellation` - `meta-properties.md:81-94`
  - Cancellation token usage

- [ ] `pseudo:propertymessages-iteration` - `meta-properties.md:177-183`
  - PropertyMessages foreach

- [ ] `pseudo:runrules-flag-combinations` - `meta-properties.md:209-219`
  - RunRulesFlag combinations

- [ ] `pseudo:runrules-before-save` - `meta-properties.md:223-228`
  - Pre-save validation pattern

- [ ] `pseudo:delete-undelete-calls` - `meta-properties.md:358-362`
  - Delete/UnDelete calls

- [ ] `pseudo:parent-access-casting` - `meta-properties.md:402-412`
  - Parent property casting

### Collections

- [ ] `pseudo:add-item-behavior` - `collections.md:152-158`
  - 4 lines showing add behavior

- [ ] `pseudo:remove-item-behavior` - `collections.md:169-181`
  - Remove behavior with IsNew check

- [ ] `pseudo:delete-remove-equivalence` - `collections.md:193-197`
  - 3 lines showing equivalence

---

## Keep as Pseudo: API Signatures

These are correctly marked and should remain pseudo.

### Interface/Type Signatures (meta-properties.md)
- `pseudo:isbusy-signature` - `bool IsBusy { get; }`
- `pseudo:waitfortasks-signatures` - method signatures
- `pseudo:isvalid-signature` - `bool IsValid { get; }`
- `pseudo:isselfvalid-signature` - `bool IsSelfValid { get; }`
- `pseudo:propertymessages-signature` - property signature
- `pseudo:runrules-signatures` - method signatures
- `pseudo:clear-messages-signatures` - method signatures
- `pseudo:isnew-signature` - `bool IsNew { get; }`
- `pseudo:ismodified-signature` - `bool IsModified { get; }`
- `pseudo:isselfmodified-signature` - property signature
- `pseudo:ismarkedmodified-signature` - property signature
- `pseudo:markmodified-signature` - method signature
- `pseudo:isdeleted-signature` - property signature
- `pseudo:ischild-signature` - property signature
- `pseudo:parent-signature` - property signature
- `pseudo:root-signature` - property signature
- `pseudo:issavable-signature` - property signature
- `pseudo:save-signatures` - method signatures

### Interface Definitions (property-system.md)
- `pseudo:ivalidateproperty-interface` - full interface
- `pseudo:ientityproperty-interface` - full interface
- `pseudo:ipropertymessage-interface` - interface
- `pseudo:neatoopropertychanged-delegate` - delegate definition
- `pseudo:neatoopropertychangedeventargs` - record definition

### Framework Internals
- `pseudo:getter-implementation` - internal getter
- `pseudo:factory-pause-flow` - internal framework flow
- `pseudo:partial-property-declaration` - single line declaration

### Razor Markup (special case)
- `pseudo:isbusy-razor-usage` - Razor syntax
- `pseudo:isvalid-razor-usage` - Razor syntax
- `pseudo:ui-binding-example` - Razor syntax
- `pseudo:blazor-ui-binding` - Razor component
- `pseudo:client-authorization-display` - Razor inject

---

## Duplicates to Consolidate

| Block 1 | Block 2 | Action |
|---------|---------|--------|
| `pseudo:custom-add-methods` | `pseudo:complete-personphonelist` | Keep complete version |
| `pseudo:service-injection` (x2) | Same ID in multiple files | Consolidate |
| `pseudo:propertychanged-handler` (x2) | Same ID in multiple files | Consolidate |
| `pseudo:entity-based-save` (x2) | Same ID in multiple files | Consolidate |

---

## Implementation Approach

### Phase 1: High Priority Classes
1. Create sample files in `docs/samples/Neatoo.Samples.DomainModel/`
2. Add `#region` markers with snippet IDs
3. Run `dotnet mdsnippets` to sync
4. Verify docs render correctly

### Phase 2: Usage Examples
1. Create demonstration classes that show the patterns
2. May need test classes in `docs/samples/Neatoo.Samples.DomainModel.Tests/`
3. Extract relevant portions with regions

### Phase 3: Consolidate Duplicates
1. Ensure single source of truth
2. Remove duplicate pseudo markers
3. Update all references

---

## Notes

- Some pseudo blocks may intentionally show patterns that don't compile in isolation (missing dependencies)
- Razor blocks require Blazor sample project or remain pseudo
- Test method blocks should go in test sample project
- Consider creating focused sample files (e.g., `RemoteFactoryPatterns.cs`, `ValidationRuleExamples.cs`)
