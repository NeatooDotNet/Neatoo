# Design Source of Truth Improvements

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-01
**Last Updated:** 2026-02-01

---

## Problem

A Claude Code expert review of the Design Source of Truth implementation identified gaps that reduce its effectiveness. While the current implementation scored 9/10, several areas would substantially improve Claude Code's ability to reason about the Neatoo framework.

## Solution

Address the identified gaps in priority order, focusing on documentation that helps Claude understand runtime behavior, error handling, and integration points.

---

## Plans

---

## Tasks

### Priority 1: Threading and Serialization Guarantees
- [x] Add "Threading Guarantees" section to CLAUDE-DESIGN.md
  - Document RuleManager thread safety (or lack thereof)
  - Document LoadValue/SetValue synchronization requirements
  - Document async rule interaction with synchronous code
- [x] Add "Serialization Considerations" section to CLAUDE-DESIGN.md
  - What state survives serialization (IsModified, DeletedList, rule state?)
  - JSON serialization pitfalls (circular Parent references)
  - Client-server state transfer behavior

### Priority 2: Common Gotchas File
- [x] Create `src/Design/Design.Domain/CommonGotchas.cs` with wrong→right code examples
  - Gotcha 1: Assuming rules fire during [Create] (they're paused)
  - Gotcha 2: DeletedList behavior for IsNew=true items (discarded, not tracked)
  - Gotcha 3: Method-injected [Service] unavailable on client
  - Gotcha 4: PauseAllActions breaks rule calculations
  - Gotcha 5: IsModified includes child modifications (use IsSelfModified)
- [x] Add tests for each gotcha in Design.Tests

### Priority 3: RemoteFactory Integration Details
- [x] Expand RemoteFactory section in CLAUDE-DESIGN.md
  - How [Remote] methods work in Blazor WebAssembly
  - Factory method behavior: server vs. client invocation
  - HTTP proxy generation details
  - PrivateAssets="all" pattern explanation

### Priority 4: Performance Implications
- [x] Add `PERFORMANCE:` comments to key files
  - Why EntityListBase stores DeletedList separately vs. marking items
  - IsModified aggregation cost vs. individual property tracking
  - Memory overhead of state tracking
  - Rule execution performance considerations

### Priority 5: DI and Service Registration Deep-Dive
- [x] Expand `src/Design/Design.Domain/DI/ServiceRegistration.cs`
  - How factory discovers [Factory] classes
  - DI container assumptions (IServiceProvider)
  - Custom rule registration
  - Scoped vs. transient lifetime considerations

### Priority 6: Error Handling Strategy
- [x] Create `src/Design/Design.Domain/ErrorHandling/ErrorPatterns.cs`
  - Exceptions factory methods can throw
  - Validation failures vs. exceptions distinction
  - Rule exception behavior
  - Error boundary patterns

### Priority 7: Rule Trigger Decision Tree
- [x] Add decision matrix to `src/Design/Design.Domain/Rules/RuleBasics.cs`
  - Fluent API vs. class-based rules: when to use each
  - Single vs. multiple property triggers
  - Async rule interaction patterns
  - Rule execution order guarantees

---

## Progress Log

**2026-02-01**: Created todo based on Claude Code expert review of Design Source of Truth. Original review scored implementation 9/10 but identified 7 improvement areas.

**2026-02-01**: Completed all 7 priorities:
- Added Threading Guarantees and Serialization Considerations sections to CLAUDE-DESIGN.md
- Created CommonGotchas.cs with 5 documented gotchas and wrong/right code examples
- Added 13 comprehensive tests for gotcha behaviors in GotchaTests/CommonGotchaTests.cs
- Added RemoteFactory Deep Dive section to CLAUDE-DESIGN.md with Blazor WASM patterns
- Added PERFORMANCE comments to AllBaseClasses.cs and RuleBasics.cs
- Expanded ServiceRegistration.cs with factory discovery, DI assumptions, and lifetime documentation
- Created ErrorHandling/ErrorPatterns.cs with validation vs exception patterns
- Added rule trigger decision matrix to RuleBasics.cs
- All 84 tests pass

---

## Results / Conclusions

All identified gaps have been addressed. The Design Source of Truth now includes:

1. **Threading documentation** - Clear guidance on single-threaded model, async rule behavior, and synchronization context
2. **Serialization documentation** - What state survives serialization, circular reference handling, client-server transfer patterns
3. **CommonGotchas.cs** - 5 documented gotchas with wrong/right code examples and comprehensive test coverage
4. **RemoteFactory deep dive** - How [Remote] methods work in Blazor WASM, HTTP proxy generation, PrivateAssets pattern
5. **Performance comments** - Memory overhead, DeletedList design rationale, rule execution considerations
6. **DI deep dive** - Factory discovery, container assumptions, custom rule registration, lifetime guidance
7. **Error handling patterns** - Validation failures vs exceptions, rule exception behavior, error boundaries
8. **Rule decision matrix** - When to use fluent vs class-based rules, trigger patterns, execution order guarantees

Files modified/created:
- `src/Design/CLAUDE-DESIGN.md` - Threading, Serialization, RemoteFactory sections
- `src/Design/Design.Domain/CommonGotchas.cs` - New file
- `src/Design/Design.Domain/ErrorHandling/ErrorPatterns.cs` - New file
- `src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs` - PERFORMANCE comments
- `src/Design/Design.Domain/Rules/RuleBasics.cs` - PERFORMANCE comments, decision matrix
- `src/Design/Design.Domain/DI/ServiceRegistration.cs` - DI deep dive
- `src/Design/Design.Tests/GotchaTests/CommonGotchaTests.cs` - New test file
- `src/Design/Design.Tests/TestInfrastructure.cs` - Mock repositories for gotcha tests
