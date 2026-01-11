# Documentation Sample Gaps

**Status:** Planning
**Created:** 2026-01-10
**Priority:** Medium

## Problem

The MarkdownSnippets migration revealed that ~175 code blocks in documentation are complete, compilable code that should come from `docs/samples/` but currently don't. These are gaps, not pseudo-code.

## Gap Analysis Summary

| Category | Count | % |
|----------|-------|---|
| Real gaps (compilable code) | ~175 | 73% |
| Pseudo-code (marked) | ~48 | 20% |
| Invalid/anti-patterns (marked) | ~17 | 7% |

## Files by Priority

### High Priority (15+ gaps each)

- [ ] **property-system.md** (~20 blocks)
  - Property access patterns
  - Getter/Setter examples
  - Type conversion examples
  - Meta-property demonstrations

- [ ] **validation-and-rules.md** (~17 blocks)
  - Rule implementation patterns
  - Trigger properties
  - Message returning patterns
  - Async rule examples

- [ ] **remote-factory.md** (~17 blocks)
  - Remote attribute usage
  - Child entity patterns
  - State transfer examples

- [ ] **factory-operations.md** (~15 blocks)
  - Factory operation examples
  - List factory patterns
  - Remote operations

### Medium Priority (5-14 gaps each)

- [ ] **collections.md** (~11 blocks)
  - Collection manipulation patterns
  - Parent-child setup
  - DeletedList iteration

- [ ] **exceptions.md** (~11 blocks)
  - Exception catching patterns
  - SaveOperationException handling

- [ ] **testing.md** (~11 blocks)
  - Testable entity examples
  - RunRule patterns

- [ ] **meta-properties.md** (~10 blocks)
  - Parent/Root property usage
  - Cross-aggregate enforcement

- [ ] **installation.md** (~8 blocks)
  - Program.cs setup
  - Blazor WASM configuration

- [ ] **aggregates-and-entities.md** (~8 blocks)
  - Interface patterns
  - Factory usage

### Lower Priority

- [ ] **best-practices.md** (~6 blocks)
- [ ] **ef-integration.md** (~3 blocks, but critical patterns)
- [ ] **database-dependent-validation.md** (~3 blocks)
- [ ] **quick-start.md** (~5 blocks)
- [ ] **blazor-binding.md** (~3 blocks)

## Critical Patterns to Address First

1. **Rule implementations with trigger properties** (validation-and-rules.md)
2. **DI setup patterns** - Program.cs for server and Blazor WASM (installation.md)
3. **EF integration** - Fetch/Insert/Update with MapTo/MapFrom (ef-integration.md)
4. **Collection manipulation** - add, remove, DeletedList patterns (collections.md)
5. **Property access** - type conversion, meta-properties (property-system.md)

## Approach

For each file:
1. Identify which blocks are truly compilable patterns
2. Create sample classes in `docs/samples/Neatoo.Samples.DomainModel/`
3. Add `#region {snippet-id}` markers
4. Add corresponding tests in `docs/samples/Neatoo.Samples.DomainModel.Tests/`
5. Replace inline code with `snippet: {id}` references
6. Run `dotnet mdsnippets` to sync

## Notes

- Some blocks may need to stay as pseudo-code if they show generated output or library internals
- Anti-pattern examples (WRONG) should use `invalid:` markers
- Focus on patterns that users copy-paste most often
