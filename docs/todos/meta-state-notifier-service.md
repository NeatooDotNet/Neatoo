# Extract IMetaStateNotifier Service

Refactor duplicated meta state pattern into a DI-registered service for extensibility.

**Created:** 2026-01-10
**Status:** Not Started
**Origin:** code-smells-review.md

---

## Background

The same `RaiseIfChanged` pattern is duplicated across 4 classes:
- `ValidateBase.cs:278-296`
- `EntityBase.cs:215-238`
- `ValidateListBase.cs:272-288`
- `EntityListBase.cs:140-159`

Following Neatoo's extensibility principle, this should be a DI-registered service so users can customize notification behavior (logging, batching, throttling, telemetry).

## Design

```csharp
public interface IMetaStateNotifier
{
    void RaiseIfChanged<T>(T cachedValue, T currentValue, string propertyName,
                           Action<string> raisePropertyChanged,
                           Action<NeatooPropertyChangedEventArgs> raiseNeatooPropertyChanged);
}
```

- State caching stays in each class (shape varies per class)
- Notification behavior is centralized and swappable via DI

## Tasks

- [ ] Create `IMetaStateNotifier` interface
- [ ] Create `MetaStateNotifier` default implementation in `Neatoo.Internal`
- [ ] Register in `AddNeatooServices`
- [ ] Inject via `IValidateBaseServices<T>`
- [ ] Refactor ValidateBase to use IMetaStateNotifier
- [ ] Refactor EntityBase to use IMetaStateNotifier
- [ ] Refactor ValidateListBase to use IMetaStateNotifier
- [ ] Refactor EntityListBase to use IMetaStateNotifier
- [ ] Ensure all existing tests pass after refactoring
