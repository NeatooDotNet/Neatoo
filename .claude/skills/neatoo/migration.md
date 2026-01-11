# Neatoo Migration Guide

## Overview

This guide covers breaking changes between Neatoo versions and how to migrate your code.

## Version History

| Version | Key Changes |
|---------|-------------|
| 10.6.0 | RemoteFactory upgrade with CancellationToken support |
| 10.5.0 | CancellationToken flows from client to server |
| 10.4.0 | Base layer collapse (EditBase removed) |
| 10.2.0 | Ordinal serialization for smaller payloads |
| 10.1.0 | C# record support for Value Objects |

## Migrate to 10.5.0+ (Save Reassignment)

### Summary

Always reassign the result of `Save()` - it returns a new instance after the client-server roundtrip.

<!-- snippet: save-reassignment -->
```csharp
/// <summary>
/// Migration pattern: Save() reassignment (v10.5+)
///
/// IMPORTANT: Always reassign the result of Save() to your variable.
/// Save() returns a new instance after the client-server roundtrip.
/// </summary>
public static class SaveReassignmentMigration
{
    public static async Task CorrectPattern(
        ISaveableItem entity,
        ISaveableItemFactory factory)
    {
        // v10.5+ CORRECT: Always reassign
        entity = await factory.Save(entity);

        // The returned entity has:
        // - Server-generated values (timestamps, computed fields)
        // - IsNew = false after Insert
        // - IsModified = false after successful save
    }

    // WRONG: Ignoring the return value (pre-v10.5 habit)
    // await factory.Save(entity);  // DON'T DO THIS
    //
    // The original 'entity' reference becomes stale after Save().
    // Server-side changes are only in the returned instance.
}
```
<!-- /snippet -->

## Migrate to 10.5.0+ (CancellationToken Support)

### Summary

CancellationToken now flows from client to server via HTTP connection state.

<!-- snippet: cancellation-token -->
```csharp
/// <summary>
/// Migration pattern: CancellationToken support (v10.5+)
///
/// Async operations now support CancellationToken for graceful cancellation.
/// The token flows from client to server via HTTP connection state.
/// </summary>
public static class CancellationTokenMigration
{
    public static async Task RunRulesWithCancellation(
        ISaveableItem entity,
        CancellationToken cancellationToken)
    {
        // v10.5+: RunRules supports CancellationToken
        await entity.RunRules(token: cancellationToken);

        // CancellationToken flows to:
        // - AsyncRuleBase.Execute() methods
        // - Server-side operations via HTTP connection
        // - Database queries (if passed through)
    }

    // For Save operations, cancellation is handled via HTTP:
    // - Client disconnection cancels server operation
    // - No explicit CancellationToken parameter needed on Save
}
```
<!-- /snippet -->

### Delegate Signature Changes

| Delegate | Change |
|----------|--------|
| `HandleRemoteDelegateRequest` | Added `CancellationToken` parameter |
| `MakeRemoteDelegateRequestHttpCall` | Added `CancellationToken` parameter |

### Migration Steps

1. Update NuGet packages to 10.5.0+
2. Update any custom delegate implementations to accept `CancellationToken`
3. Pass cancellation tokens to async operations in factory methods

## Migrate to 10.4.0 (Base Layer Collapse)

### Summary

`EditBase` was removed. All functionality is now in `EntityBase` and `ValidateBase`.

### Migration Steps

1. Replace `EditBase<T>` with `EntityBase<T>`
2. Replace `EditListBase<T>` with `EntityListBase<T>`
3. Update using statements

## Best Practices for Upgrades

1. **Read release notes** - Check for breaking changes
2. **Update incrementally** - One major version at a time
3. **Run tests** - Verify behavior after upgrade
4. **Check generated code** - Regenerate after package update
