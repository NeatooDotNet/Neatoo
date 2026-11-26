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

---

## Test Review — WAIT-003 — [2026-11-24] — Round 2

Scope: `git diff 1fb0923..7bc4114`. That covers the round 1 callout, the four Acceptance bullets added at the gate, and the `OneThreadAtATime` rewrite.

**Verdict: APPROVED**
**Logs:** build PASSED, 0 errors, 97 warnings, the same count as round 1 (`reviews/003-build.log`); Design build PASSED, 0 errors (`reviews/003-design-build.log`); tests 2,068 passed, 0 failed, 2 skipped (`reviews/003-test.log`: MudNeatoo 33, BaseGenerator 50, Analyzers 23, Person.DomainModel 55, Neatoo.UnitTest 1,907 + the same 2 skipped); Design tests 235 passed, 0 failed (`reviews/003-design-test.log`). Red runs: `003-red-lazy.log` has the lazy test failing on `value.IsBusy` with the attach disabled. `003-red-round1.log` has exactly the five claimed tests failing, each on its pinning assertion: the callback thread (13 vs 14); the property's idle notification off the owning thread; both listener tests with "no exception was thrown"; and `AParentsWait...` on the child's own wait.

### Veto-tier
None.

The four new Required bullets are pinned at their declared tiers:
- Lazily created property: `Property_ThePropertyManagerCreatesOnFirstUse_IsBusyWhileItsAsyncRuleRuns`, through a `Getter`/`Setter` property, so `GetProperty` creates and attaches it. It asserts busy, an incomplete property wait, then idle on the property and the object. Red with the attach disabled.
- Owning thread:
  - `[unit]`: `AsyncTasksOwningThreadTests.CompletionCallback_RunsOnTheThreadThatAddedTheTask_WhenTheTaskFinishesOnAnother` drives a bare `AsyncTasks` and finishes the task on a pool thread with `Task.Run`.
  - `[integration]`: `BusyNotifications_AreRaisedOnTheThreadThatOwnsTheObject_WhenTheReplyArrivesOnAnother` asserts exactly two notifications each, busy and idle, for the property and the object, both on the owning thread. It is not vacuous.
- Listener throws as the property goes idle: the two `AListenerThatThrows...` tests pin all three parts. The awaited rule run or write faults with the listener's exception; the property's wait does not fault (it is awaited bare); and the property ends idle.
- Parent's wait and the idle child's fault: `AParentsWait_ReportsItsChildsFaultOnce_AndLeavesTheIdleChildsOwnWaitToReportIt` asserts the parent's first wait throws, its second does not, and the child's own wait throws. See the answers below.

Existing tests: `Property_RaisesIsBusyWhenItGoesBusyAndWhenItGoesIdle` now uses the shared `RecordBusyAtEachNotification` helper with the same two assertions. No assertion was removed anywhere in the diff. `AsyncFlowTests` did not change. Its helper moved from an exclusive scheduler to a dedicated thread with a synchronization context, which is still one thing at a time and is closer to the UI host the ruling describes. The plan's changed-tests row was updated to match, and the class is green.

### Callouts (≤ 5 Optional)
- An operation's own exception is reported in preference to a listener's idle exception. The path is the `catch` branches in `ValidateProperty.OnValueNeatooPropertyChanged` (`await accounted.WhenFinished(); throw;`) and in `RuleManager.RunRule` (`await Task.WhenAll(accounted).WhenFinished(); throw;`). `Affects: AC-6`. `Reachable by:` an async rule that throws on a property whose `PropertyChanged` listener, or an `IsBusy`-triggered rule, also throws as the property goes idle. `AfterAnAsyncRuleThrows...` exercises the branch with no listener. The precedence the second Plan Amendment states ("its own exception is the one reported") has no test. Suggested tier: `[integration]`, the "throw" value plus `ThrowWhenThePropertyGoesIdle`, asserting the awaited write surfaces "Rule failed" and not "Listener failed".

### Tech-debt (untiered, ≤ 5, one line each)
- `AParentsWait...`: the polling loop does not assert that it ended because both objects went idle. A deadline exit shows up one line later as a `TimeoutException` from `WaitAsync`, not a named failure.
- The listener tests use `Assert.ThrowsAsync<Exception>` plus `StringAssert.Contains(thrown.ToString(), ...)`, so the exception type the operation surfaces is not pinned.
- `LazilyCreatedPropertyObject` is a public concrete built on the non-DI `ValidateBaseServices`, unlike the file's interface-and-factory fixtures. Interface-first is recommended, not required.

### Could not verify
None. The logs are dated 18:41 to 18:46 and the commit 18:47, with a clean tree at `7bc4114`. That is consistent with "rerun on 7bc4114's code". I did not rebuild to confirm it.

### Theoretical (not triaged)
- The manager wait's new `IsCompletedSuccessfully` test also awaits a busy property's wait that has already faulted. I found no input that produces a busy property whose wait has already faulted at scan time: `AddTask` moves a held fault into the new, incomplete sequence. So that branch is not separately pinnable.
- `OneThreadAtATime.Post` falls back to the thread pool after the body finishes. This is test infrastructure only. A test whose work outlives its body loses the single-thread guarantee for that tail, and no current test asserts thread identity there.

### Answers to the brief's two questions
- **Does `AParentsWait...` pin more than documented intent supports?** No, but its child half rests on one reading, which I state here.
  - The parent half, "reports once", is AC-7 and the `IAsyncTasks` remarks applied to the parent's tracker. The parent's copy arrives through `AddChildTask`, its first wait takes it, and the second is clean.
  - The child half rests on the same remarks applied per tracker. `CreateAsyncTasks` is documented as "a task tracker for one entity or one property", and the remarks say a fault with nobody waiting goes "to the first wait after it". With the fix, the parent's scan never calls the idle child's wait, so the child's own wait is its tracker's first. Three more things support it: it is the behavior before this plan (the code review's callout: "a later `child.WaitForTasks()` no longer reports it, which it did before this plan"); it matches D33's direction, where an entity's wait is "the entity's registered tasks and nothing else"; and no ruling says a graph-level wait consumes a descendant's fault.
  - What no document settles is whether AC-7's "once" means once per tracker or once per graph. The test commits to per tracker. That is the only reading the written contract gives, so I do not raise it as a decision. If Keith ever rules once per graph, this is the test to revisit.
- **Is the polling loop on `IsBusy` an honest way to let both trackers close with nobody waiting?** Yes.
  - Any await on a wait would make that wait "a wait that held `AllDone` while it ran" and defeat the scenario, so something other than a wait has to observe the trackers closing.
  - The loop runs on the owning thread. Each `Task.Delay(5)` continuation comes back through the synchronization context, so the trackers' accounting, which now continues there, interleaves between polls rather than racing them.
  - Every way the loop can mislead fails red, not green. An exit at the deadline makes the next `WaitAsync(Limit)` throw `TimeoutException`, which fails `ThrowsExactlyAsync<AggregateException>`. An exit while the child is still busy would let the parent's scan take the child's fault, and the final assertion would fail, as it did in `003-red-round1.log`.
  - The only cost is the unnamed deadline exit listed under Tech-debt.

### Read report
- Read beyond the brief: `src/Neatoo/IAsyncTasks.cs` in full (the per-tracker contract and `CreateAsyncTasks`); `reviews/003-code-review.md` (the origin of the round 1 fixes and its account of the pre-plan child-wait behavior); the `AsyncRuleFaultObject` fixture the new parent wraps; `AsyncTasks.cs` `_unobservedFault` handling, to judge the theoretical faulted-wait branch; `002-tracker-faults.md` AC-7 wording, to check whether a graph case was ruled.
- Named but unused: none.
