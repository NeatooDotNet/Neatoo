# PropertyInfoWrapper Thread Safety — Plan

**Date:** 2026-04-12
**Related Todo:** [PropertyInfoWrapper Thread Safety — Concurrent Dictionary Corruption](../todos/propertyinfowrapper-thread-safety.md)
**Status:** Awaiting Code Review
**Last Updated:** 2026-04-12

---

## Overview

`PropertyInfoWrapper` instances are, by design, shared across all threads and all DI scopes in a running process (via `PropertyInfoList<T>.PropertyInfos`, a `static` dictionary on a singleton-lifetime list type). Property metadata is immutable at runtime, so this is the correct design. But the wrapper's two lazy caches — `customAttribute` (a `Dictionary<Type, Attribute?>`) and `customAttributes` (a `List<Attribute>?`) — are populated without synchronization. Concurrent entity construction racing on the dictionary corrupts it and produces `InvalidOperationException` from `Dictionary.set_Item` for every subsequent caller until process restart.

This plan makes both caches thread-safe by serializing access behind an instance-level `lock`, matching the pattern already established in `PropertyInfoList<T>.RegisterProperties`. It also adds concurrent regression tests that deterministically reproduce the corruption against the current code.

---

## Skills

- `~/.claude/skills/neatoo/SKILL.md` — framework patterns, `PropertyInfoWrapper` role in the property system, `PropertyInfoList<T>` lifetime, test conventions (no mocking Neatoo classes, MSTest `[TestClass]`/`[TestMethod]`, naming `MethodName_Scenario_ExpectedResult`)

---

## Business Rules (Testable Assertions)

1. WHEN N threads concurrently call `PropertyInfoWrapper.GetCustomAttribute<TAttr>()` on the same wrapper instance for the same or different `TAttr` types, THEN no call throws and the internal cache state remains consistent (no `InvalidOperationException` from `Dictionary`). — Source: NEW
2. WHEN N threads concurrently call `PropertyInfoWrapper.GetCustomAttributes()` on the same wrapper instance, THEN no call throws and all calls return references to `List<Attribute>` instances containing the complete attribute set. — Source: NEW
3. WHEN `GetCustomAttribute<TAttr>()` is called repeatedly on the same wrapper for the same `TAttr`, THEN all calls return the same `TAttr` instance (reference equality preserved). — Source: existing test `GetCustomAttribute_CalledTwice_ReturnsSameInstance` at `src/Neatoo.UnitTest/Unit/Core/PropertyInfoWrapperTests.cs:478`
4. WHEN `GetCustomAttribute<TAttr>()` is called on a property that does not have `TAttr`, THEN the result is `null` and subsequent calls do not re-invoke `PropertyInfo.GetCustomAttribute<TAttr>()`. — Source: existing test `GetCustomAttribute_NullResult_IsCached` at `src/Neatoo.UnitTest/Unit/Core/PropertyInfoWrapperTests.cs:512`
5. WHEN `GetCustomAttributes()` is called repeatedly on the same wrapper, THEN all calls return the same collection reference. — Source: existing test `GetCustomAttributes_CalledTwice_ReturnsSameCollection` at `src/Neatoo.UnitTest/Unit/Core/PropertyInfoWrapperTests.cs:549`
6. WHEN different `TAttr` types are requested on the same wrapper, THEN each type's result is cached independently and subsequent calls return the cached value per type. — Source: existing test `GetCustomAttribute_DifferentAttributeTypes_CachedSeparately` at `src/Neatoo.UnitTest/Unit/Core/PropertyInfoWrapperTests.cs:529`

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Concurrent single-type attribute lookup reproduces corruption on current code, passes after fix | 64 threads x 10,000 iterations, single shared wrapper, all threads request the same `TAttr`; cold cache at t=0 | Rule 1 | Current code: `InvalidOperationException` from `Dictionary.set_Item` within the first few hundred iterations. Fixed code: all 640,000 calls complete without exception; all return the same attribute instance. |
| 2 | Concurrent multi-type attribute lookup under contention | 64 threads x 10,000 iterations, single shared wrapper, each iteration randomly picks one of 8 `TAttr` types (some present on the property, some not); cold cache at t=0 | Rule 1, Rule 4, Rule 6 | Fixed code: all calls succeed; each attribute type returns the same cached instance (or `null`) across all threads; reflection invoked at most once per type. |
| 3 | Concurrent `GetCustomAttributes()` stress | 64 threads x 10,000 iterations, single shared wrapper, all threads call `GetCustomAttributes()`; cold cache at t=0 | Rule 2, Rule 5 | Fixed code: all calls succeed; all return the same `List<Attribute>` reference after first population; list contents match serial baseline. |
| 4 | Repeated calls return same instance (existing behavior preserved) | Single thread, repeated `GetCustomAttribute<T>()` | Rule 3 | `AreSame` holds across calls. Existing test `GetCustomAttribute_CalledTwice_ReturnsSameInstance` still passes. |
| 5 | Null result caching (existing behavior preserved) | Property without `TAttr`, repeated `GetCustomAttribute<TAttr>()` | Rule 4 | Returns `null`; reflection invoked at most once. Existing test `GetCustomAttribute_NullResult_IsCached` still passes. |
| 6 | `GetCustomAttributes()` returns same reference (existing behavior preserved) | Single thread, repeated `GetCustomAttributes()` | Rule 5 | `AreSame` across calls. Existing test `GetCustomAttributes_CalledTwice_ReturnsSameCollection` still passes. |
| 7 | Different attribute types cached separately (existing behavior preserved) | Single thread, interleaved calls for two `TAttr` types | Rule 6 | Each type returns its own cached instance; cross-type cache isolation holds. Existing test `GetCustomAttribute_DifferentAttributeTypes_CachedSeparately` still passes. |
| 8 | Shared-wrapper sanity — same `PropertyInfoWrapper` returned across DI resolutions | Resolve `IPropertyInfoList<TSomeTestType>` twice from DI; call `GetPropertyInfo("SomeProp")` on both | Underpins Rules 1, 2 (thread-safety is only needed because wrappers are shared) | Both resolutions return `AreSame` `IPropertyInfo` references; underlying `PropertyInfoWrapper` is also `AreSame`. Passes on both current and fixed code. If this test ever fails, the wrapper-sharing invariant is broken and the thread-safety contract scope has changed — a signal for review, not a silent pass. |

---

## Approach

Serialize access to both `PropertyInfoWrapper` caches behind a single instance-level `lock` object. Reflection inside the lock is invoked at most once per attribute type (for `customAttribute`) and at most once total (for `customAttributes`). The lock is only contended during cache warm-up; once populated, readers still acquire the lock but the critical section is a dictionary lookup or reference read — microseconds, no reflection.

This matches the pattern already used in `PropertyInfoList<T>.RegisterProperties` (single `static` lock object guarding lazy population), so the fix is idiomatic for this codebase.

### Rejected alternatives (for completeness)

- **`ConcurrentDictionary` + `GetOrAdd`** — works but allows the reflection factory to run multiple times under contention. Rejected in favor of "exactly-once reflection" and consistency with the `RegisterProperties` pattern.
- **`Lazy<Attribute?>` per type in a `ConcurrentDictionary`** — strongest guarantee but higher per-type allocation. Not needed here.
- **Double-checked locking with `volatile`** — more complex than a simple lock; no measurable benefit given cache is populated quickly and contention is low post-warm-up.

---

## Domain Model Behavioral Design

Not applicable. This plan modifies a framework-internal reflection cache, not a domain model. No computed properties, visibility flags, reactive rules, classification properties, or validation rules are introduced or changed.

---

## Design

### File changes

- `src/Neatoo/Internal/PropertyInfoWrapper.cs` — modify `GetCustomAttribute<T>()` and `GetCustomAttributes()`; add instance-level `lock` object
- `src/Neatoo.UnitTest/Unit/Core/PropertyInfoWrapperTests.cs` — add concurrent regression tests (new `#region Thread Safety Tests` section)

### Code shape

```csharp
public class PropertyInfoWrapper : IPropertyInfo
{
    private readonly object cacheLock = new object();

    private Dictionary<Type, Attribute?> customAttribute = new();

    // Virtual seam: test-only subclass overrides this to count reflection invocations.
    // Production code calls this from inside the lock exactly once per attribute type.
    protected virtual Attribute? ReflectCustomAttribute(Type attrType)
        => this.PropertyInfo.GetCustomAttribute(attrType);

    public T? GetCustomAttribute<T>() where T : Attribute
    {
        lock (cacheLock)
        {
            if (!this.customAttribute.ContainsKey(typeof(T)))
            {
                this.customAttribute[typeof(T)] = ReflectCustomAttribute(typeof(T));
            }
            return (T?)this.customAttribute[typeof(T)];
        }
    }

    private List<Attribute>? customAttributes;

    public IEnumerable<Attribute> GetCustomAttributes()
    {
        lock (cacheLock)
        {
            if (this.customAttributes == null)
            {
                this.customAttributes = this.PropertyInfo.GetCustomAttributes().ToList();
            }
            // Safe to return while other threads hold references: the list is assigned exactly
            // once on first population and is never mutated afterward. Callers enumerate outside
            // the lock, but since the list reference is stable and its contents are immutable
            // post-assignment, no torn reads are possible.
            return this.customAttributes;
        }
    }
}
```

**Invariant (must not be violated by future refactors):** the `customAttribute` dictionary reference and the `customAttributes` list reference, once assigned, are never replaced. Only their contents are mutated, and only inside the lock, and only during the cold→warm transition. Any future change that reassigns either field outside the lock (for example, a well-meaning "clear cache" API) reintroduces the race.

### Single lock vs. two locks

Use one `cacheLock` for both caches. The two caches are accessed from different call sites (rule engines look up specific attribute types; list consumers enumerate all), and lock contention between them is near-zero in practice. A single lock is simpler and removes a coordination question.

### Lock scope

The lock is an **instance** field, not `static`. Each `PropertyInfoWrapper` has its own lock. Threads accessing different wrappers do not contend. Threads accessing the same wrapper serialize briefly inside the critical section — acceptable because the section is a dictionary lookup, not I/O.

### Test design

- **Framework:** MSTest (match existing test conventions).
- **Concurrency primitive:** `Parallel.For` with `MaxDegreeOfParallelism = 64` for the stress loop; `ManualResetEventSlim` as a starting gate (`gate.Wait()` at the top of each worker, `gate.Set()` after all threads have been scheduled) so all threads hit the wrapper simultaneously — maximizes race probability on cold cache.
- **Iteration count:** 10,000 per thread (640,000 total calls per scenario) — chosen empirically; the `Dictionary` corruption window is extremely narrow, so high iteration counts are needed to hit it reliably on current code.
- **Wrapper instance per scenario (explicit):** each test method constructs **exactly one** `PropertyInfoWrapper` instance at the start of the test, shared by all 64 threads for all 10,000 iterations. The wrapper begins with a cold cache (fresh `Dictionary`/`List` fields from the constructor). The wrapper is NOT reset between iterations — a single cold→warm transition per test method is what triggers the race. This matches the production path: one long-lived wrapper hit by many threads during warm-up.
- **Why one-wrapper-per-test (not one-per-iteration):** the corruption is persistent — once the `Dictionary` is corrupted, every subsequent `set_Item` throws forever. On unmodified code, the test should throw within the first few hundred iterations (first cold-cache race). On fixed code, the test should complete all 640k calls without exception. Resetting the wrapper between iterations would hide multi-thread-interleave corruption that only shows up after a long run; keeping one wrapper maximizes contention on the single `_version` field and most faithfully models production.
- **Reflection-count measurement (Scenario 2):** to verify "reflection invoked at most once per attribute type" (the core property that `lock` buys over `ConcurrentDictionary.GetOrAdd`), introduce a **test-only subclass** `CountingPropertyInfoWrapper : PropertyInfoWrapper` that overrides a virtual `ReflectCustomAttribute<T>()` seam (added to `PropertyInfoWrapper` for this purpose — see Design section below) and increments an atomic `ConcurrentDictionary<Type, int>` counter per invocation. After the stress run, assert every entry in the counter equals exactly 1. If `PropertyInfoWrapper` cannot be made subclassable without a larger refactor, alternate: wrap a custom `PropertyInfo`-derived type whose `GetCustomAttribute` increments a counter. Implementer picks the cleaner path and records the choice in the developer memory file. Scenarios 1 and 3 do not require counting — they only assert zero exceptions.
- **Random seeding (Scenario 2):** Scenario 2 picks 1 of 8 attribute types per iteration. Use a per-thread `Random` seeded deterministically: `new Random(12345 + Thread.CurrentThread.ManagedThreadId)` or pass a `ThreadLocal<Random>` that derives the seed from a fixed base. Do **not** share a single `Random` across threads — `System.Random` is not thread-safe and will itself throw, producing a confusing failure unrelated to the bug under test. Do **not** use `Random.Shared` without noting it — while thread-safe, it obscures reproducibility.
- **Reproducibility baseline:** before writing the fix, run the new tests against unmodified `PropertyInfoWrapper.cs` and confirm they fail. Capture the first failure output (exception type, message, stack) and paste it into the developer memory file in Step 1 immediately after the baseline run — waiting until Step 5 risks losing the evidence if the test is edited mid-flight. Without this baseline, we cannot claim the tests prove the fix works.
- **Shared-wrapper sanity check (Scenario 8 — see Test Scenarios table):** include one short test that resolves `IPropertyInfoList<TSomeTestType>` from the DI container twice, calls `GetPropertyInfo("SomeProp")` on each result, and asserts `Assert.AreSame` on the returned `IPropertyInfo` references (and, after casting, on the underlying `PropertyInfoWrapper`). This codifies the "shared by design" invariant directly at the `PropertyInfoList<T>` level — no `EntityProperty<P>` indirection needed, since the sharing happens in the list's static dictionary. A future refactor that accidentally makes wrappers per-scope would fail this test and signal that the thread-safety contract no longer applies.

---

## Implementation Steps

1. **Write concurrent regression tests first.** Add new `#region Thread Safety Tests` in `PropertyInfoWrapperTests.cs` covering Scenarios 1, 2, 3. Each test constructs one wrapper, one starting-gate `ManualResetEventSlim`, and runs `Parallel.For` with `MaxDegreeOfParallelism = 64`. Scenario 2 uses a `CountingPropertyInfoWrapper` subclass (or equivalent counter harness — see Test design) and asserts every per-type counter equals 1. Run the tests against the **unmodified** `PropertyInfoWrapper.cs` — tests must fail with `InvalidOperationException` from `Dictionary`. **Immediately** paste the first failure output (exception type, message, top of stack) into the developer memory file at `docs/plans/propertyinfowrapper-thread-safety.memory/developer.md` under a heading `## Baseline Failure Output (pre-fix)`. If tests pass against unmodified code, the reproducer is insufficient — raise iteration count or thread count until they fail deterministically before continuing to Step 2.
2. **Add Scenario 8 (shared-wrapper sanity test).** Resolve `IPropertyInfoList<TSomeTestType>` from the DI container twice, call `GetPropertyInfo("SomeProp")` on both, `Assert.AreSame` on the `IPropertyInfo` references, and cast to `PropertyInfoWrapper` and `Assert.AreSame` again. Do **not** route through `EntityProperty<P>` — the sharing happens at the `PropertyInfoList<T>` level, and going through `EntityProperty<P>` adds unnecessary DI setup. Run against current code — should pass (confirms the sharing invariant exists today).
3. **Modify `PropertyInfoWrapper.cs`:** add `private readonly object cacheLock = new object();`. Wrap the bodies of `GetCustomAttribute<T>()` and `GetCustomAttributes()` in `lock (cacheLock) { ... }`.
4. **Rerun concurrent regression tests.** All scenarios (1, 2, 3) must now pass. Run each test 3 times in a row to confirm stability.
5. **Rerun full `PropertyInfoWrapperTests` class.** All existing tests (Scenarios 4, 5, 6, 7 — existing caching, IsPrivateSetter, Type, etc.) must still pass.
6. **Rerun full Neatoo test suite:** `dotnet test src/Neatoo.sln`. Zero failures.
7. **Audit adjacent caches.** Run the following concrete searches against `src/Neatoo/**/*.cs` (not tests):
   - **Search A — unsynchronized lazy dictionaries:** `private (static )?(readonly )?Dictionary<` followed by nullable/lazy-populate usage. Ripgrep pattern: `private.*Dictionary<.*>.*=.*new\(\)` and inspect each hit for (i) whether the enclosing type is registered as Singleton in `AddNeatooServices.cs`, (ii) whether the dictionary is written inside a method without a lock. Singleton-reachable + unsynchronized write = same bug as `PropertyInfoWrapper`.
   - **Search B — unsynchronized lazy list/reference caches:** `private (static )?List<.*>\?` and `private (static )?IEnumerable<.*>\?`, looking for the `if (field == null) { field = Build(); } return field;` pattern.
   - **Search C — `isRegistered`-style one-time-init flags:** `private (static )?bool [a-zA-Z]+ = false;` paired with a populate method. These should already be lock-guarded (as in `PropertyInfoList<T>.RegisterProperties`), but verify every read path takes the lock or is safe by other means.
   - **Target files to check explicitly** (from the skim of the singleton surface and the `remove-inconsistent-locks.md` context): `src/Neatoo/Rules/RuleManager.cs`, `src/Neatoo/Rules/AttributeToRule.cs`, `src/Neatoo/Internal/PropertyInfoList.cs` (already partially locked — verify read paths through `GetPropertyInfo`/`HasProperty`/`Properties()` are safe after registration completes), `src/Neatoo/Internal/PropertyManager*.cs`, `src/Neatoo/Internal/ValidatePropertyManager.cs`, `src/Neatoo/Internal/EntityPropertyManager*.cs`. Add any newly discovered singleton-reachable cache types to this list.
   - **Output:** document every file searched, each hit's location, and a verdict (safe / needs-fix / ambiguous-deferred) in the developer memory file under `## Adjacent Cache Audit`. Cross-reference `docs/todos/remove-inconsistent-locks.md` where overlap exists.
   - If the audit finds new bugs, raise them to the orchestrator; do not silently fix them within this plan's scope.
8. **Add release notes entry** under `docs/release-notes/` (next patch version — bug fix). Entry must name the exception, the stack site, and the public contract being affirmed (thread-safe attribute lookup on shared wrappers).

---

## Acceptance Criteria

- [ ] New concurrent regression tests added at `src/Neatoo.UnitTest/Unit/Core/PropertyInfoWrapperTests.cs`, covering Scenarios 1, 2, 3
- [ ] Regression tests verified to fail against unmodified code (failure output captured to developer memory file), then pass after the fix (stability: 3 consecutive clean runs)
- [ ] Scenario 2 reflection-count assertion passes: every attribute type in the counter dictionary equals exactly 1 after the stress run (proves "exactly-once reflection" — the lock's value over `ConcurrentDictionary.GetOrAdd`)
- [ ] Scenario 8 (shared-wrapper sanity — defined in the Test Scenarios table) added and passes on both current and fixed code
- [ ] All existing `PropertyInfoWrapperTests` still pass
- [ ] `dotnet build src/Neatoo.sln` succeeds with `TreatWarningsAsErrors=true`
- [ ] `dotnet test src/Neatoo.sln` green
- [ ] Adjacent-cache audit documented (findings, fixes-or-new-todos decisions) in the developer memory file
- [ ] Release notes entry added under `docs/release-notes/`
- [ ] No behavioral changes to caching contract: same attribute instance returned on repeat calls (Rule 3); null cached (Rule 4); same list reference returned (Rule 5); different attribute types cached independently (Rule 6)

---

## Dependencies

- No external dependencies. Fix is contained entirely in `Neatoo` core and its unit test project.
- No Neatoo.BaseGenerator, Neatoo.Analyzers, or RemoteFactory changes required.
- Verification consumers (zTreatment) do not need coordination — they consume the NuGet package; shipping a patch version is sufficient.

---

## Risks / Considerations

- **Lock contention under extreme load.** Serializing all attribute lookups through a single instance lock creates a bottleneck if the same wrapper is hit by thousands of threads. In practice the critical section is a dictionary lookup on a populated cache — nanoseconds — so this is not a realistic concern for the production workload described in the todo. If profiling later shows contention, switch to `ConcurrentDictionary<Type, Attribute?>` with `GetOrAdd` — that is a self-contained follow-up change.
- **Test flakiness on slow CI hardware.** Concurrent regression tests that depend on racing threads can hide real failures if the CI box is single-core or slow. Mitigate by setting `MaxDegreeOfParallelism` explicitly and using a `ManualResetEventSlim` starting gate so all threads launch simultaneously rather than staggering. If flakiness still occurs, raise iteration count or run tests in a loop with `[TestMethod]` helpers.
- **Verifying the regression test actually reproduces the bug.** If the test passes against unmodified `PropertyInfoWrapper.cs`, the test is insufficient. Step 1 of the implementation requires confirmation that the test fails against the current code before the fix is written. This is a non-negotiable prerequisite.
- **Adjacent caches might reveal deeper issues.** The audit step (task list in todo) may find similar patterns elsewhere. If the audit uncovers new bugs, file them as separate todos; do not expand this plan's scope.
- **The `List<Attribute>?` cache field was never corrupting on current code.** Hardening it here is defensive, not bug-fixing. The cost is negligible (one added `lock`), and the consistency benefit (both caches follow the same rule) outweighs the alternative of leaving mismatched concurrency stories in one file.
