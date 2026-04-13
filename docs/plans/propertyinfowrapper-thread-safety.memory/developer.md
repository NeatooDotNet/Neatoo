# Developer Pre-Implementation Review — PropertyInfoWrapper Thread Safety

**Date:** 2026-04-12
**Reviewer:** neatoo-developer
**Review type:** Pre-implementation plan grading (not Step 5 code review)

## Composite Grade: B+

The plan correctly diagnoses the bug, chooses a defensible fix, and enumerates existing regression cases well. It is let down by an under-specified stress-test design (no cache-reset between iterations, no seed, no per-test fresh wrapper semantics stated), an acceptance criterion that refers to a scenario number that does not exist, and a vague "audit adjacent caches" step that any implementer will interpret differently. With the must-fixes below it is a clean Grade A.

## Dimensional Grades

| Dimension | Grade | One-sentence reasoning |
|-----------|-------|------------------------|
| Bug understanding | A | Root cause correct: static `PropertyInfoList<T>.PropertyInfos` holds the wrapper, `ContainsKey`+`set_Item` on `Dictionary<,>` is non-atomic, `_version` check throws on race, corrupted dictionary stays broken. |
| Scope discipline | B | Scope is right (wrapper only, plus regression tests + audit), but the `customAttributes` list is included "for consistency" without grading the value — and the audit step is loose enough to creep. |
| Business rules / testable assertions | B+ | Rules 1-6 are unambiguous and traced. Missing: an explicit rule for "reflection invoked at most once per `TAttr`" which Scenario 2 implicitly expects — a junior could skip enforcing it. Rule 2 says "references to `List<Attribute>` instances" (plural) but Rule 5 says "same collection reference" (singular) — contradiction on first read. |
| Test strategy | C+ | Biggest weakness. The plan does not address three concrete reproducibility issues: (a) whether each `Parallel.For` iteration resets the wrapper cache or shares one wrapper across 640k calls (if shared, the first race corrupts once and every subsequent call throws — proving nothing about the fix's correctness on a fresh cache), (b) no `Random` seed for Scenario 2's 8-type pick (flaky), (c) no mention of whether `Parallel.For`'s `MaxDegreeOfParallelism = 64` on a CI box with 2 cores actually races — thread-count != core-count doesn't guarantee interleaving on cold cache without the starting gate, which the plan mentions but does not specify how to construct. |
| Fix design correctness | A- | The lock closes every race. All reads and writes of `customAttribute` / `customAttributes` are inside the lock. Returning `this.customAttributes` from inside the lock and enumerating outside is safe because the list is written once, never mutated after population — but the plan does not state this reasoning, which is the sort of thing a reviewer should want written down. Shape shown in Design section is correct. |
| Implementation steps | B | Order is right (test first, verify failure, fix, verify pass). Step 7 ("audit adjacent caches") is too vague — no search pattern, no concrete target list beyond the todo's list. Step 2 adds the sanity test but doesn't name the DI entry point (`IPropertyInfoList<T>`) to keep an implementer from taking the long way around. |
| Acceptance criteria | B- | Third bullet references "Scenario 2 of the design's test design section" but the design section has no numbered scenarios — it has a bulleted list. The test scenarios table numbers 1-7 does have a Scenario 2 about multi-type lookup, which is NOT the shared-wrapper sanity check. This will confuse the implementer. |
| Risks section | A- | Names real risks (lock contention, test flakiness, reproducer sufficiency, audit scope creep, defensive list-cache hardening). The risk about flakiness suggests raising iteration count "or run tests in a loop with `[TestMethod]` helpers" — vague. |

## Must-Fix Changes

1. **[Must-fix] Design section, "Test design" bullets, reproducer semantics:** Specify exactly what each of the three stress tests resets between iterations. The corruption is persistent — once the dictionary is corrupted, every `set_Item` throws forever. If the test reuses one wrapper across all 640k calls, then on unmodified code the test reliably throws (proving corruption is reachable), and on fixed code it reliably passes (proving serialization works). But the value of Scenario 2's "reflection invoked at most once per type" check depends on a **fresh** wrapper. State explicitly: "Each test scenario constructs exactly one `PropertyInfoWrapper` instance shared by all threads for that test method, cold cache. Verification of the fix is: zero exceptions across all 640k calls AND for Scenario 2, the number of underlying `PropertyInfo.GetCustomAttribute<T>()` invocations equals 8 (one per type) — this requires a test-only subclass or a counter harness. The plan currently says 'reflection invoked at most once per type' as an expectation but does not show how to measure it."

2. **[Must-fix] Acceptance Criteria, third bullet:** Reads "Shared-wrapper sanity test (Scenario 2 of the design's test design section)". There is no numbered Scenario 2 in the design's test design section — the numbered scenarios are in the Test Scenarios table, where Scenario 2 is the multi-type concurrent lookup, NOT the shared-wrapper sanity check. Fix: rename to "Shared-wrapper sanity test (Design section / Test design / 'Shared-wrapper sanity check' bullet)" and add it as its own numbered scenario (Scenario 8) in the Test Scenarios table so every AC bullet maps to a numbered scenario.

3. **[Must-fix] Implementation Steps, Step 7 "Audit adjacent caches":** Specify the concrete Grep/search the implementer must run. Example list: (a) `private (static )?Dictionary<` in `src/Neatoo/` files where the enclosing type is registered Singleton (`AddNeatooServices.cs`) or referenced by a Singleton, (b) `private (static )?List<` used as a nullable lazy cache, (c) `private (static )?bool [a-zA-Z]+ = false;` flags paired with a populate method (the `isRegistered` pattern). Also: concretely list the files to check — `RuleManager`, `AttributeToRule`, `RuleMetadataCache` if any, `PropertyManager`, `ValidatePropertyManager`. An audit without a target list will be half-done.

## Should-Fix Changes

1. **[Should-fix] Business Rules:** Add a rule: "Rule 7: WHEN `GetCustomAttribute<TAttr>()` is called N times concurrently for the same `TAttr` against a cold-cache wrapper, THEN `PropertyInfo.GetCustomAttribute<TAttr>()` is invoked exactly once across all callers." This is what "exactly-once reflection" — the selling point of the lock-vs-ConcurrentDictionary choice — actually buys. Without it, the choice of `lock` over `ConcurrentDictionary.GetOrAdd` is not testable.

2. **[Should-fix] Design section, "Code shape":** Add a one-line comment above the second `return this.customAttributes;` explaining that callers enumerate outside the lock but this is safe because the list is assigned once and never mutated after population. This is the kind of invariant that disappears in a future "small refactor" if undocumented.

3. **[Should-fix] Test design bullets:** Specify that Scenario 2's `Random` instance must be seeded (e.g., `new Random(12345)`), and that Scenario 2 must NOT use a shared `Random` across threads (shared `Random` is itself not thread-safe and will throw — confusing failure mode). Use `ThreadLocal<Random>` seeded per-thread-id, or `Random.Shared` (which is thread-safe on .NET 6+) with a wrapper that derives per-thread seeds.

4. **[Should-fix] Rule 2 wording:** "return references to `List<Attribute>` instances containing the complete attribute set" — "instances" is plural and contradicts Rule 5 ("same collection reference"). Rewrite: "return a reference to the same `List<Attribute>` instance containing the complete attribute set." Remove the plural.

5. **[Should-fix] Implementation Steps, Step 1:** "Document the failure (paste the first failure output into the developer memory file during Step 5 code review)." Move this instruction to Step 4 or add a sub-step under Step 1 for "capture failure output to developer memory file" — waiting until Step 5 means the orchestrator may lose the evidence if the test is edited.

6. **[Should-fix] Implementation Steps, Step 2:** The sanity test resolves "two `EntityProperty<P>` instances of the same type through the DI container" — but the actual sharing is via `PropertyInfoList<T>`, not `EntityProperty<P>`. Simpler and more direct: resolve `IPropertyInfoList<TSomeType>` twice from two separate scopes (or one scope, doesn't matter — it's Singleton), call `GetPropertyInfo("SomeProp")` on both, assert `AreSame` on the returned `IPropertyInfo` — and then cast to `PropertyInfoWrapper` and assert `AreSame` on that too. The current phrasing via `EntityProperty<P>` adds indirection and will require the implementer to set up more infrastructure than needed.

## Nice-to-Have Changes

1. **[Nice-to-have] Risks section, test flakiness bullet:** Replace "run tests in a loop with `[TestMethod]` helpers" with a concrete tactic: mark the test `[DataTestMethod]` with `[DataRow(1)]` through `[DataRow(5)]` so each test run executes the race 5 times and a single transient miss still fails the build.

2. **[Nice-to-have] Test design bullets:** Note explicitly that the three concurrent scenarios should be marked `[TestCategory("Stress")]` or similar so they can be filtered out of fast-feedback dev loops if they turn out slow (10k x 64 = 640k reflection lookups is not free, even with a cached result).

3. **[Nice-to-have] Risks section:** Add a risk: "If the `PropertyInfoWrapper` field initializer (`customAttribute = new()`) is ever moved to be set inside the lock on first call, the current design assumes the dictionary exists at construction time. Keep the field initializer where it is." Prevents a well-meaning "simplification" later that reintroduces a race (re-assigning the dictionary reference inside the lock).

4. **[Nice-to-have] Overview:** State the blast radius more concretely — "until process restart, every entity construction of any type sharing the corrupted dictionary throws" is already in the todo; the plan elides this and a first-time reader may miss why this is a High priority patch-release bug.

## What the Plan Got Right

- **Correct root cause.** Sharing via static `PropertyInfoList<T>.PropertyInfos` on Singleton-lifetime registration. Confirmed by reading the code.
- **Correct fix choice.** Single instance `lock` matches the existing `lockRegisteredProperties` pattern in `PropertyInfoList<T>`; idiomatic for this codebase.
- **Rejected alternatives are named and dismissed with reasons** — this blocks future "why didn't you use `ConcurrentDictionary`" reviewers.
- **Test-first ordering.** Step 1 requires the regression test to fail against unmodified code before the fix is written. This is the right discipline — without it, the tests prove nothing.
- **Distinction between corrupting cache and merely redundant cache.** Correctly identifies that `customAttributes` (List) is not broken; the plan hardens it defensively with honest framing in the Risks section.
- **Shared-wrapper sanity test is a good instinct.** Codifies the invariant that makes thread safety necessary; a future refactor making wrappers per-scope would fail it and signal the contract change.
- **Business rules 3-6 are traced to existing tests** with file and line numbers — excellent traceability.
- **Risks section names real risks**, not generic ones (lock contention, flakiness, reproducer sufficiency, audit scope creep, defensive hardening cost).
- **No expansion into rewriting `PropertyInfoList<T>` or changing DI lifetimes.** Scope is tight.
- **Release notes step is included** with specific instructions on what the entry must name (exception, stack site, contract affirmed).

## Baseline Failure Output (pre-fix)

**Date captured:** 2026-04-12
**Command:** `dotnet test src/Neatoo.UnitTest/Neatoo.UnitTest.csproj --filter "FullyQualifiedName~PropertyInfoWrapperTests"`
**Against:** `PropertyInfoWrapper.cs` with virtual seams added (`ReflectCustomAttribute`, `ReflectAllCustomAttributes`) but **no lock**.

All 3 concurrent scenarios fail deterministically against the unfixed code:

- **Scenario 1 (`GetCustomAttribute_ConcurrentSingleType_NoCorruption`):**
  `Assert.AreEqual failed. Expected:<1>. Actual:<4>. Reflection for TestDescriptionAttribute must be invoked exactly once; observed 4.`
  Interpretation: 4 threads all saw `ContainsKey==false` before any wrote, each invoked reflection. Cold-cache race confirmed.

- **Scenario 2 (`GetCustomAttribute_ConcurrentMultiType_ReflectsOncePerType`):**
  `Assert.AreEqual failed. Expected:<0>. Actual:<64>. First: InvalidOperationException: Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.`
  Interpretation: **Exact production stack trace reproduced.** 64 threads hit `Dictionary.set_Item` corruption; all subsequent calls also threw until the test aborted.

- **Scenario 3 (`GetCustomAttributes_ConcurrentAccess_NoCorruption`):**
  `Assert.AreEqual failed. Expected:<1>. Actual:<9>. All threads must observe the same cached list reference.`
  Interpretation: 9 distinct `List<Attribute>` references returned — multiple threads racing through the null check, each building a fresh list, different consumers pinned different references.

Scenarios 1 & 3 use reflection-invocation counts rather than hoping for rare `Dictionary` corruption — deterministic race indicators. Scenario 2 additionally reproduces the exact production exception.

**Results summary:** `Failed: 3, Passed: 40, Skipped: 0, Total: 43` (Scenario 8 DI-sharing sanity test passes on current code as expected — the wrapper is shared by design; that's what makes thread-safety necessary.)

## Adjacent Cache Audit

**Date:** 2026-04-12
**Scope:** `src/Neatoo/**/*.cs` (excluding tests, Neatoo.Analyzers, Neatoo.BaseGenerator).

### Search A — unsynchronized lazy `Dictionary` / mutable dictionary fields

| Location | Type | Enclosing lifetime | Verdict |
|---|---|---|---|
| `Internal/PropertyInfoWrapper.cs:32` `customAttribute` | `Dictionary<Type, Attribute?>` | Singleton-reachable (shared via `PropertyInfoList<T>.PropertyInfos` static) | **Fixed in this change** (instance `lock`) |
| `Internal/ValidatePropertyManager.cs:99` `_createPropertyMethodPropertyType` | `static ConcurrentDictionary<Type, MethodInfo>` | Reachable from all scopes | **Safe** — already `ConcurrentDictionary` |
| `Internal/AsyncTasks.cs:11` `_tasks` | `Dictionary<Guid, Task>` | Per-entity instance (Transient) | **Safe** — all mutations go through `lock (_lockObject)` on every read/write path; confirmed by reading `AddTask`, `SequenceCompleted`, and exception paths |
| `Internal/PropertyInfoList.cs:12` `PropertyInfos` | `static IDictionary<string, IPropertyInfo>` | Singleton (per-T) | **Safe post-registration** — populated once under `lockRegisteredProperties`; read paths (`GetPropertyInfo`, `HasProperty`, `Properties`) call `RegisterProperties()` first which takes the lock, establishing a happens-before edge. After `isRegistered = true`, the dict is effectively immutable, so `TryGetValue` outside the lock is safe. Cross-referenced: `docs/todos/remove-inconsistent-locks.md` discusses inconsistent locking but not this specific path — still safe by the happens-before argument |
| `Rules/RuleManager.cs:374` `Rules` | `Dictionary<uint, IRule>` | Per-entity (Transient) | **Safe** — not shared across threads at framework level; per-entity state, caller is responsible for threading |

### Search B — unsynchronized lazy nullable `List`/`IEnumerable` caches

| Location | Type | Enclosing lifetime | Verdict |
|---|---|---|---|
| `Internal/PropertyInfoWrapper.cs:62` `customAttributes` | `List<Attribute>?` | Singleton-reachable | **Fixed in this change** (defensive hardening; did not corrupt in production but produced redundant reflection under contention) |

No other `private (static )?List<.*>?` lazy-populate patterns found in Neatoo core.

### Search C — `isRegistered`-style one-time-init flags

| Location | Flag | Lock-guarded? | Verdict |
|---|---|---|---|
| `Internal/PropertyInfoList.cs:13` `isRegistered` | `private static bool` | Yes — reads + write inside `lock (lockRegisteredProperties)` | **Safe** |
| `EntityListBase.cs:51` `_cachedChildrenModified`, `ValidateListBase.cs:60` `_cachedIsBusy`, `Internal/ValidateProperty.cs:122` `_isReadOnly` | `private bool` | Per-instance Transient state, not shared across threads at framework level | **Safe in practice** — not singleton-reachable, not a one-time-init pattern |

### Other singleton candidates inspected

- `Rules/Rules/AttributeToRule.cs` (registered `AddSingleton<IAttributeToRule, AttributeToRule>`): **stateless** — no fields; `GetRule` is a pure switch over the attribute type, `CreateTriggerProperty` creates a fresh lambda each call. Safe.
- `Internal/PropertyManager*.cs`, `Internal/EntityPropertyManager*.cs`, `Internal/ValidatePropertyManager.cs`: the factories are Transient-resolved via `CreateValidatePropertyManager` / `CreateEntityPropertyManager` delegates; each entity gets its own manager. `_createPropertyMethodPropertyType` (the only singleton-reachable cache) is `ConcurrentDictionary`. Safe.

### Conclusion

**No additional fixes required.** The only unsynchronized singleton-reachable lazy caches in Neatoo core were the two `PropertyInfoWrapper` fields addressed by this change. `PropertyInfoList<T>` was architecturally close to the same mistake but was already lock-guarded on registration and has correct happens-before on the read path. `AsyncTasks` uses explicit locking on every path. No new todos filed.

## Verdict

To reach Grade A: fix the three must-fixes (reproducer semantics including fresh-wrapper rule and reflection-count measurement, the broken Scenario 2 reference in acceptance criteria, and the vague audit step), then address the should-fix items around Rule 2's contradictory wording, the missing "exactly-once reflection" rule, and the `Random` seeding discipline. None of these require architectural rethinking — they are specification tightening. The core approach (instance `lock` over both caches, test-first, audit adjacent) is sound and should not be changed.
