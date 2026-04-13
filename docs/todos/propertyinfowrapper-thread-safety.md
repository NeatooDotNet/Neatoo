# PropertyInfoWrapper Thread Safety — Concurrent Dictionary Corruption

**Status:** In Progress
**Priority:** High
**Created:** 2026-04-12
**Last Updated:** 2026-04-12

---

## Problem

Under concurrent server load, entity construction fails with:

```
System.InvalidOperationException: 'Operations that change non-concurrent collections must have
exclusive access. A concurrent update was performed on this collection and corrupted its state.
The collection's state is no longer correct.'
   at System.Collections.Generic.Dictionary`2.set_Item(TKey key, TValue value)
   at Neatoo.Internal.PropertyInfoWrapper.GetCustomAttribute[T]()
   at Neatoo.Internal.EntityProperty`1..ctor(IPropertyInfo propertyInfo)
   at Neatoo.Internal.DefaultFactory.CreateEntityProperty[P](IPropertyInfo propertyInfo)
   at Neatoo.Internal.EntityPropertyFactory`1.Create[TProperty](TOwner owner, String propertyName)
   at zTreatment.DomainModels.Visit.SymptomsAssessment.InitializePropertyBackingFields(...)
   at Neatoo.ValidateBase`1..ctor(IValidateBaseServices`1 services)
   at Neatoo.EntityBase`1..ctor(IEntityBaseServices`1 services)
```

Once the dictionary corrupts, the error recurs for every subsequent entity construction of any type whose wrapper shares the corrupted dictionary. The process stays broken until restart.

Reported from `zTreatment` production workload while loading symptoms for consultation 4964415.

---

## Analysis (Neatoo Team)

### Why the same wrapper is shared across threads

`PropertyInfoList<T>` (`src/Neatoo/Internal/PropertyInfoList.cs`) holds its wrappers in a **`static` field**:

```csharp
protected static IDictionary<string, IPropertyInfo> PropertyInfos { get; } = new Dictionary<string, IPropertyInfo>();
```

The list itself is registered as **singleton** in DI (`AddNeatooServices.cs:63`). `RegisterProperties()` populates `PropertyInfos` once per closed generic `T` inside a `lock`, and the wrappers are reused from that static dictionary forever.

The `CreatePropertyInfoWrapper` delegate is transient, but that is irrelevant — it is only invoked once per property inside the `RegisterProperties` lock. After that, every `EntityProperty<P>` constructor, on any thread, in any DI scope, for a given property of `T`, receives the **same** `PropertyInfoWrapper` instance.

### What breaks

`PropertyInfoWrapper.GetCustomAttribute<T>()` (`src/Neatoo/Internal/PropertyInfoWrapper.cs:23`):

```csharp
private Dictionary<Type, Attribute?> customAttribute = new();

public T? GetCustomAttribute<T>() where T : Attribute
{
    if(!this.customAttribute.ContainsKey(typeof(T)))
    {
        this.customAttribute[typeof(T)] = this.PropertyInfo.GetCustomAttribute<T>();
    }
    return (T?) this.customAttribute[typeof(T)];
}
```

Concurrent entity constructions call this on the same wrapper instance. The non-atomic `ContainsKey` -> `set_Item` sequence on a plain `Dictionary<,>` permits two threads to both pass `ContainsKey` and both enter `set_Item`. `Dictionary<,>` detects concurrent structural mutation via its internal `_version` field and throws `InvalidOperationException`. Once the internal buckets and version counters are out of sync, every subsequent `set_Item` — from any thread, for any key — throws.

### Which caches are actually broken

`PropertyInfoWrapper` has two lazy caches. Only one causes corruption:

| Field | Type | Impact under contention |
|---|---|---|
| `customAttribute` | `Dictionary<Type, Attribute?>` | **Corrupts** — races on `set_Item` trigger `_version` mismatch |
| `customAttributes` | `List<Attribute>?` (nullable reference) | No corruption — reference assignment is atomic; worst case is redundant reflection |

The production stack trace implicates `GetCustomAttribute<T>()` specifically. `GetCustomAttributes()` is not broken but is adjacent enough to warrant consideration during the fix design.

### Why unit tests never caught this

- Existing `PropertyInfoWrapperTests` construct wrappers locally in each test — no sharing across threads
- `IntegrationTestBase` runs tests serially
- Dev-loop workloads (console app, local Blazor WASM) never race
- The race window is small; it takes sustained concurrent server traffic to trigger

---

## Requirements Review

**Verdict:** Pending
**Reviewed:**
**Summary:**

Business requirements source for this todo: `src/Design/Design.Domain/PropertySystem/` and related.

---

## Plans

- [PropertyInfoWrapper Thread Safety — Plan](../plans/propertyinfowrapper-thread-safety.md)

---

## Tasks

- [ ] Confirm thread-safety contract in `src/Design/Design.Domain/` — is thread-safe concurrent construction of entities a documented design requirement?
- [ ] Write a concurrent regression test that reproduces `Dictionary` corruption on a shared `PropertyInfoWrapper.GetCustomAttribute<T>()`. Test must fail deterministically (or near-deterministically) against current code, covering both cold-cache and post-corruption failure modes.
- [ ] Design the fix in the plan (no approach predetermined; evaluate options against the design project thread-safety contract and weigh against existing patterns in adjacent code)
- [ ] Implement fix in `PropertyInfoWrapper`
- [ ] Decide whether `customAttributes` (List cache) also gets hardened in the same change, or deferred — document the decision in the plan
- [ ] Concurrent regression test passes; run stress (1000+ iterations) to confirm stability
- [ ] Audit adjacent caches for the same shape:
  - `PropertyInfoList<T>` static state (`PropertyInfos` dictionary, `isRegistered` flag — these already sit behind `lockRegisteredProperties`, but re-verify read paths)
  - Other Neatoo internal caches with `private Dictionary<` / `private List<` lazily populated fields
  - Cross-reference `docs/todos/remove-inconsistent-locks.md`
- [ ] Full `dotnet build src/Neatoo.sln` and `dotnet test src/Neatoo.sln` green
- [ ] Release notes entry added (patch version — bug fix)

---

## Progress Log

### 2026-04-12
- Bug reported from zTreatment production workload (symptoms load for consultation 4964415)
- Outside team (zTreatment) drafted the initial todo with their own analysis and fix sketch
- Neatoo team re-analyzed: confirmed the corruption is real; corrected the root-cause explanation (sharing happens via `PropertyInfoList<T>.PropertyInfos` static field, not via the transient `CreatePropertyInfoWrapper` delegate)
- Scoped the broken surface: `customAttribute` dictionary is the only corrupting cache; `customAttributes` list is redundant-under-contention but not broken
- Rewrote task list; deferred fix design to the plan
- Implementation complete (per user request to skip to implementation):
  - Added virtual reflection seams (`ReflectCustomAttribute`, `ReflectAllCustomAttributes`) + instance `cacheLock` guarding both caches in `PropertyInfoWrapper.cs`
  - Added Thread Safety Tests region with Scenarios 1, 2, 3, 8 in `PropertyInfoWrapperTests.cs`
  - Verified tests fail deterministically against unfixed code (Scenario 1: 4x reflection for single type; Scenario 2: exact production `InvalidOperationException` reproduced; Scenario 3: 9 distinct list refs). Baseline captured in developer memory file
  - After fix: all 3 concurrent scenarios pass across 3 consecutive runs; full suite green (1793 Neatoo.UnitTest + 42 BaseGenerator + 254 Samples + 55 Person.DomainModel.Tests, 0 failures)
  - Adjacent-cache audit complete (developer memory file) — no additional fixes needed
  - Version bumped to 0.28.1; release notes added
- Next: Step 5 (developer code review) if running full workflow

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass (`dotnet build src/Neatoo.sln`)
- [ ] All tests pass (`dotnet test src/Neatoo.sln`)
- [ ] New concurrent regression test added and passing under stress
- [ ] Adjacent-cache audit completed with findings documented (fixes or new todos)
- [ ] Release notes entry merged

**Verification results:**
- Build: Pending
- Tests: Pending

---

## Results / Conclusions

[Pending]
