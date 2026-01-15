# L5: Internal Interface Pattern - Document Rationale and Consider Simplification

**Status**: Not Started
**Priority**: Low
**Category**: Code Quality / Documentation
**Created**: 2026-01-15

## Overview

The codebase uses separate internal interfaces (`IValidateBaseInternal`, `IEntityBaseInternal`, etc.) with runtime casts to separate public API from framework internals. This pattern should be documented, and we should evaluate reverting to the simpler `internal` method approach with `InternalsVisibleTo`.

## The Tension

Neatoo has internal operations that **must be protected** from external consumers:

| Internal Operation | Why Protected |
|-------------------|---------------|
| `SetPrivateValue()` | Bypasses IsReadOnly, could corrupt state |
| `MarkModified()` | Could fake modification status |
| `MarkAsChild()` | Could break aggregate boundaries |
| `ClearAllMessages()` | Could hide validation errors |
| `AddChildTask()` | Could break async coordination |

**Goal**: Prevent external code from bypassing Neatoo's protections while allowing:
1. Neatoo's own code to use these bypasses (necessary for framework operation)
2. Test frameworks (KnockOff, CastleProxy) to stub interfaces for unit testing

## Current Approach: Separate Internal Interfaces

```csharp
// Public interface - stubbable
public interface IEntityBase : IValidateBase { ... }

// Internal interface - framework only
internal interface IEntityBaseInternal : IValidateBaseInternal
{
    void MarkModified();
    void MarkAsChild();
}

// Usage in framework code (20+ casts throughout codebase)
if (item is IEntityBaseInternal entityInternal)
{
    entityInternal.MarkModified();
}
```

**Pros**:
- Public interfaces are clean and minimal
- External consumers literally cannot see internal members

**Cons**:
- 20+ runtime casts scattered through codebase
- Verbose pattern
- Runtime type checks have (minimal) performance cost
- Stubs that don't implement internal interface cause silent failures

## Proposed Simplification: Internal Methods + InternalsVisibleTo

```csharp
// Single interface with internal members
public interface IEntityBase : IValidateBase
{
    // Public members
    void Delete();
    Task<IEntityBase> Save();

    // Internal members - invisible to external consumers
    internal void MarkModified();
    internal void MarkAsChild();
}

// In AssemblyInfo.cs
[assembly: InternalsVisibleTo("Neatoo.UnitTest")]
[assembly: InternalsVisibleTo("KnockOff")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // CastleProxy
```

**Pros**:
- No runtime casts needed
- Cleaner framework code
- Compile-time safety (no silent failures)
- Same protection level (external code can't access internal members)

**Cons**:
- `InternalsVisibleTo` expands trust boundary to named assemblies
- Must maintain list of trusted assemblies

## Task List

- [ ] Document the internal interface pattern rationale in architecture docs
- [ ] Verify CastleProxy already has `InternalsVisibleTo` (check `DynamicProxyGenAssembly2`)
- [ ] Add `InternalsVisibleTo` for KnockOff assembly
- [ ] Evaluate: Revert to internal methods on interfaces (removes 20+ casts)
- [ ] If reverting: Update all interface definitions
- [ ] If reverting: Remove separate `*Internal` interfaces
- [ ] If reverting: Update framework code to call methods directly

## Decision Criteria

Revert to simpler approach if:
1. `InternalsVisibleTo` for KnockOff doesn't break anything
2. CastleProxy compatibility is confirmed
3. No external consumers depend on the current pattern

Keep current approach if:
1. There's a reason external test frameworks need to stub internal operations
2. The explicit separation provides documentation value worth the verbosity

## Related Documents

- [solution-b-internal-interfaces-design.md](solution-b-internal-interfaces-design.md) - Current design document
- [internal-interface-methods-analysis.md](internal-interface-methods-analysis.md) - Analysis of internal members
