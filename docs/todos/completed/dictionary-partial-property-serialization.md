# Bug: partial Dictionary<string, string> properties don't survive JSON bridge round-trip

## Summary

`partial` properties typed as `Dictionary<string, string>?` lose their values during Neatoo client-server serialization (RemoteFactory JSON bridge). The data is set on the server side but arrives as `null` on the client after the round-trip.

## Affected Pattern

```csharp
public partial Dictionary<string, string>? Data { get; set; }
```

## Observed In

zCRM domain models using `partial Dictionary<string, string>?` properties:

| Domain Model | Property |
|---|---|
| CrmTask | Data |
| Activity | Data |
| Notification | Extended |
| CareSummary | Profile |

## Current Workaround

Integration tests skip asserting these properties and defer coverage to DatabaseTests (which bypass the serialization layer):

```csharp
// Note: jsonb properties (Profile, Visits) don't survive Neatoo JSON bridge round-trip.
// Their persistence is covered by DatabaseTests.
```

## Expected Behavior

Dictionary properties should serialize and deserialize correctly through the RemoteFactory JSON bridge, the same as other partial property types (string, int, DateTime, enum, etc.).

## Plans

- [Fix Dictionary/Collection Serialization in ValidateProperty](../../plans/completed/dictionary-serialization-fix.md)

## Results / Conclusions

Fixed by manually deserializing `ValidateProperty<T>` / `EntityProperty<T>` in `NeatooBaseJsonTypeConverter.Read`, bypassing STJ's `[JsonConstructor]` path which cannot handle `$id`/`$ref` reference metadata in constructor parameters. The Dictionary special-case in `NeatooReferenceResolver.GetReference` was also removed, giving dictionaries normal reference IDs.

The fix is generic -- it works for all `T` types (primitives, collections, Neatoo child objects), not just Dictionary. The zCRM workaround comments can now be removed and Dictionary property assertions can be restored in integration tests.

## Status

**Status:** Complete
**Priority:** High
**Created:** 2026-02-26
**Last Updated:** 2026-02-26
