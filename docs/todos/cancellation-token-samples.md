# Add CancellationToken to All Samples and Examples

## Status: Partially Complete (Blocked)

**Blocked by:** RemoteFactory needs optional CancellationToken on generated factory methods
**Feature request:** `/home/keithvoels/neatoodotnet/RemoteFactory/docs/todos/optional-cancellation-token-factory-methods.md`

## Summary

Adding CancellationToken parameter to all async factory methods in samples and examples, demonstrating best practices for cancellation support.

## Current Blocker

When domain methods have **required** CancellationToken (no `= default`), the generated factory's SaveDelegate requires it too. This breaks `EntityBase.Save()` which internally calls the factory without a token.

**Workaround:** Add `= default` to domain method CancellationToken parameters.

**Proper fix:** RemoteFactory should generate factory methods with optional CancellationToken (`= default`) and pass it through only if the domain method accepts it.

## Current State (Person Example)

- `Person.cs`: Fetch and Insert have `= default`, Update and Delete do not yet
- Tests: Updated to pass CancellationToken but 4 integration tests fail due to the blocker
- Build: Passes
- Tests: 4 Person integration tests fail

## How to Continue

1. **After RemoteFactory implements optional CancellationToken:**
   - Update to new RemoteFactory version
   - Remove `= default` from Person.cs domain methods (or keep - either works)
   - Tests should pass

2. **Or continue with workaround:**
   - Add `= default` to Update and Delete in Person.cs
   - Tests will pass

## Completed Tasks

- [x] Added `Save(CancellationToken)` to `IEntityBase` interface
- [x] Identified all async factory methods needing CancellationToken
- [x] Updated to RemoteFactory 10.7.0 (fixed duplicate Save bug)
- [x] Updated `src/Examples/Person/` with CancellationToken on all async methods
- [x] Updated `src/Examples/Person/Person.Ef/PersonDbContext.cs` interface with CancellationToken
- [x] Updated all `docs/samples/` async factory methods with CancellationToken
- [x] All 1684 Neatoo tests pass
- [x] All 182 sample tests pass

## Files Updated

### Person Example
- `src/Examples/Person/Person.DomainModel/Person.cs` - Fetch, Insert, Update, Delete
- `src/Examples/Person/Person.Ef/PersonDbContext.cs` - Interface and implementation

### Documentation Samples
- `docs/samples/.../SaveOperationSamples.cs`
- `docs/samples/.../BusinessOperationSamples.cs`
- `docs/samples/.../ChildEntitySamples.cs`
- `docs/samples/.../CompleteExampleSamples.cs`
- `docs/samples/.../BestPracticesSamples.cs`
- `docs/samples/.../AsyncValidationSamples.cs`
- `docs/samples/.../PitfallsSamples.cs`
- `docs/samples/.../RuleUsageSamples.cs`

## Pattern Applied

```csharp
[Insert]
public async Task Insert([Service] IDbContext db, CancellationToken cancellationToken)
{
    await RunRules(token: cancellationToken);

    if (!IsSavable)
        return;

    db.Orders.Add(entity);
    await db.SaveChangesAsync(cancellationToken);
}
```

## Blocker Resolution

**Original blocker:** RemoteFactory bug created duplicate `Save`/`TrySave` methods when Insert, Update, and Delete all had CancellationToken parameters with different return types.

**Resolution:** Fixed in RemoteFactory 10.7.0. Updated Neatoo's `Directory.Packages.props` to use the new version.

## References

- Release note: `docs/release-notes/v10.8.0.md`
- RemoteFactory CancellationToken support: `RemoteFactory/docs/todos/completed/cancellation-token-support.md`
- Neatoo CancellationToken support: `Neatoo/docs/todos/completed/cancellation-token-support.md`
