## Test Review — WAIT-003 — [2026-11-24] — Round 1

**Verdict: APPROVED**
**Logs:** build PASSED, 0 errors (`reviews/003-build.log`); Design build PASSED, 0 errors (`reviews/003-design-build.log`); tests 2,062 passed, 0 failed, 2 skipped (`reviews/003-test.log`: MudNeatoo 33, BaseGenerator 50, Analyzers 23, Person.DomainModel 55, Neatoo.UnitTest 1,901 + 2 skipped, `FatClientValidate_Deserialize_SharedDictionaryReference` and the `[Ignore]`d `AsyncFlowTests_CheckAllRules`); Design tests 235 passed, 0 failed (`reviews/003-design-test.log`). The red logs match the Test Evidence claims: `003-red.log` 6 failed and 2 passed of 8, the two `WaitForTasks_ReturnsControl...` tests failed at 5 s on the "looping, not awaiting" assertion; `003-red-awaited.log` shows both `WhenAnAwaited...` tests failing on `ValueProperty.IsBusy`.

### Veto-tier
None.

Every Required bullet has a test at `[integration]` (`PropertyTasksTests : IntegrationTestBase`, real DI, real `ValidateBase` fixtures, no mocks). The map is honest, row by row:
- Busy while its rule runs, not after: `Property_IsBusyWhileItsAsyncRuleRuns_AndNotAfter` asserts busy, then not busy on the property and the entity. It was green before the change, as the map says, because the old code already did this. The rule-only path (no write) is pinned separately by `WhenAnAwaitedRuleRunCompletes...`, whose `[true, false]` sequence can only come from the rule manager's `TrackTask`.
- Two overlapping writes: `Property_WithTwoOverlappingWrites_StaysBusyUntilBothHaveFinished`. Red on the old bool (`003-red.log`). Isolation below.
- After a rule throws: `AfterAnAsyncRuleThrows_NeitherThePropertyNorTheEntityIsBusy` was red on `target.IsBusy` (the old failsafe that cleared silently) and asserts both are idle now.
- Property wait suspends and completes: `Property_WaitForTasks_IsIncompleteWhileItsRuleRuns_AndCompletesWhenItDoes` was red on `wait.IsCompleted` (the old wait awaited only a child value).
- Entity wait after `RunRules(propertyName)` / after `IValidateProperty.SetValue`: the two `WaitForTasks_ReturnsControl...` tests, red at the limit and honest (below). The write test goes through `IValidateProperty.SetValue`, not the generated setter.
- Deserialized object: `Property_OnADeserializedObject_IsBusyWhileItsAsyncRuleRuns`. Without a tracker attached on the deserialize path, `TrackTask` returns completed and `value.IsBusy` would be false, so the test would catch a missing `Attach`.
- An awaited write or rule run leaves the property idle and has said so: both `WhenAnAwaited...` tests, with no property wait and an exact `CollectionAssert.AreEqual(new[] { true, false }, ...)`. Exactly two notifications, not just first and last. Red with the accounting awaits disabled.

Existing tests changed, judged against "Tests Are Claims":
- `ValidatePropertyTests.IsSelfBusy_InitialState_ReturnsFalse` was removed because its subject, `IsSelfBusy`, no longer exists (D31; D35 allows the removal). `IsBusy_InitialState_ReturnsFalse` (`src/Neatoo.UnitTest/Unit/Core/ValidatePropertyTests.cs:1189`) still pins the initial state. Justified, and not a weakening.
- `AsyncRuleFaultTests.ValidateBase_TakesItsTrackerFromTheRegisteredFactory` and `TrackerFromServicesTests.EntityBase_TakesItsTrackerFromTheRegisteredFactory`: the claim the names make ("takes its tracker from the registered factory") is kept by `CollectionAssert.Contains(supplied, created.Tracker)`. Only the "called exactly once" detail changed, and D36 changes it on purpose, because properties now take their trackers from the same factory. `Count >= 2` adds a claim that the properties took theirs from it too. Justified.
- `AsyncFlowTests`: in the diff, the six async tests changed only in their signatures (`async Task` to `Task ... => OneThreadAtATime.Run(async () => ...)`) and their closing braces. No assertion, setup line or expected value changed. The justification is the owner's ruling of 2026-11-24 and "Single-Threaded Object Model" (`src/Design/CLAUDE-DESIGN.md:350`), and the todo has dismissed the flake as #131. Running a single-threaded graph on one exclusive scheduler is the user's environment, not extra setup a user would not need. Justified.
- `UniqueNameRuleTests.Stubs.g.cs` is generated KnockOff output. All 516 removed lines belong to the `AddMarkedBusy` / `RemoveMarkedBusy` interceptors and their nested builders. It contains no assertions.

### Callouts (≤ 5 Optional)
- Tracker attach on lazy property creation: `ValidatePropertyManager.GetProperty` → `Attach` (`src/Neatoo/Internal/ValidatePropertyManager.cs:167`). `Affects: AC-6`. `Reachable by:` `ValidateBase.Getter<P>` / `Setter<P>` (`src/Neatoo/ValidateBase.cs:501`, `:517`) for a property the generated code did not register, and `ObjectInvalid`. No test makes a lazily created property go busy. Register is covered by every new test and `SetProperties` by the deserialized test. Suggested tier: `[integration]`, a `Setter<P>`-backed property with an async rule is busy while the rule runs and idle after.

### Tech-debt (untiered, ≤ 5, one line each)
- `Property_RaisesIsBusyWhenItGoesBusyAndWhenItGoesIdle` writes out the `RecordBusyAtEachNotification` helper inline. The exact-sequence `WhenAnAwaited...` tests now subsume its first/last check.
- `PropertyTasksTests.cs:266-267`: no blank line between `WhenAnAwaitedWriteCompletes...` and the next `[TestMethod]`.
- `ValidatePropertyManager.Property_PropertyChanged` still recomputes a cached manager `IsBusy` from property notifications (`:184`). That belongs to WAIT-005; noted only because the new property notification feeds it.

### Could not verify
None.

### Theoretical (not triaged)
- `AfterAnAsyncRuleThrows...` awaits `ValueProperty.WaitForTasks()` after the entity's wait throws and before it asserts. So it does not pin that the entity's wait alone leaves everything idle. That is AC-4 (WAIT-005), not this bullet.
- Intent's "busy notification raised ... including after a fault" has no Acceptance bullet. The fault and clean paths share one non-faulting continuation in `TrackTask`, so there is no separate path to pin.
- `LazyLoadValidateProperty` / `LazyLoadEntityProperty` now include `HasTasksInFlight` and `WaitForOwnTasks()`. The existing lazy-load tests pass, and LazyLoad is pending removal (D24), so nothing new is owed.

### Answers to the brief's two questions
- **Do the `WaitForTasks_ReturnsControl...` tests honestly pin "returns control while work is in flight"?** Yes. They measure whether the call returns while the gate is held, and the gate is released only after the call has returned. A wait that spins synchronously, which is the old `while (IsBusy)` over a property wait that completed synchronously, or one that blocks (`.Wait()` / `.Result`), never returns from the call on any thread, so both fail at the limit (`003-red.log`). Whether a wait suspends depends on the tasks it awaits, not on the thread, so a pass on a pool thread implies a pass on one thread. Making the call from the pool is also what keeps the red case bounded: on one exclusive scheduler, a spinning wait would starve the `Task.Delay` continuation and the test would hang instead of failing, which the todo's Out of Scope (#133) forbids. The test thread is suspended in `WhenAny` while the pool thread runs the call, so nothing touches the graph concurrently. AC-3's literal "on a single thread" is shared with WAIT-005. The plan's bullet does not say it.
- **Does `Property_WithTwoOverlappingWrites_StaysBusyUntilBothHaveFinished` isolate the write from the rule?** Yes. The only async rule is on the parent, triggered by the path `Child.X`. `RuleManager.RunRule` hands the run to a trigger property only through `Target.TryGetProperty(triggerProperty.PropertyName)`, and `ValidateBase.TryGetProperty` (`src/Neatoo/ValidateBase.cs:585`) does an exact `PropertyManager.HasProperty` lookup. It does not resolve dotted paths, so the child's `X` is never handed the rule's task. The child has no rules of its own, and `X` holds a string (`ValueAsBase` is null). That leaves `X`'s own tracker, fed only by the write's cascade task in `OnValueNeatooPropertyChanged`, as the source of `x.IsBusy`. The red run confirms it discriminates: the old bool, cleared by the first write, failed this exact assertion.

### Read report
- Read beyond the brief: `src/Neatoo/Rules/TriggerProperty.cs` and `ValidateBase.TryGetProperty`, to settle whether the child-path rule reaches `X`; `ValidatePropertyManager.GetProperty` / `SetProperties` / `Register` to map the attach paths to tests; `ValidateBase.Getter/Setter`, to confirm the lazy-creation path has a live caller; `Neatoo.UnitTest.csproj` target framework (net9.0 only, so one UnitTest line in the log is complete).
- Named but unused: Plan Amendments' 2,000-iteration probe (throwaway, not in the logs), D32 to D34 beyond a skim.
