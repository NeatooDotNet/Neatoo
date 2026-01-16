# EntityDictionaryBase Support

**Status:** Pending
**Priority:** Medium
**Created:** 2026-01-15

---

## Problem

Currently, Neatoo only provides `EntityListBase<T>` for child entity collections. When domain models need dictionary-style access to children (e.g., keyed by LocationId), developers must choose between:

1. Using `Dictionary<TKey, TEntity>` - loses Neatoo state propagation
2. Using `EntityListBase<T>` with linear search - works but exposes list interface, not dictionary
3. Building computed Dictionary views from the list - works but creates new dictionary on each access

Real-world example from zTreatment:
```csharp
// SymptomsAssessment has areas keyed by LocationId
// Currently using Dictionary<long, ISymptomsArea> _areas - no state propagation
// Converting to EntityListBase<ISymptomsArea> with a computed Dictionary property for API compatibility
```

---

## Solution

Add `EntityDictionaryBase<TKey, TEntity>` to Neatoo that:

1. Extends the same state propagation infrastructure as EntityListBase
2. Provides dictionary semantics (indexer, ContainsKey, Keys, Values, TryGetValue)
3. Generates proper factory interface via source generation
4. Supports JSON serialization for RemoteFactory

Example usage:
```csharp
public interface ISymptomsAreaDictionary : IEntityDictionaryBase<long, ISymptomsArea>
{
}

[Factory]
internal class SymptomsAreaDictionary : EntityDictionaryBase<long, ISymptomsArea>, ISymptomsAreaDictionary
{
    // Key extracted from entity.LocationId automatically or via configuration
}
```

---

## Tasks

- [ ] Design IEntityDictionaryBase<TKey, TEntity> interface
- [ ] Implement EntityDictionaryBase<TKey, TEntity> with state propagation
- [ ] Add key extraction strategy (attribute or delegate)
- [ ] Update source generator to handle dictionary types
- [ ] Add JSON serialization support for RemoteFactory
- [ ] Add unit tests for state propagation
- [ ] Add integration tests for serialization round-trip
- [ ] Document usage patterns

---

## Related

- Similar to .NET's KeyedCollection<TKey, TItem> concept
- State propagation should mirror EntityListBase behavior
