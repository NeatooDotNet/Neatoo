## Test Review — WAIT-006 — [2026-11-24] — Round 1

**Verdict: APPROVED**
**Logs:** build PASSED, 0 errors (`reviews/006-build.log`; 79 warnings, none in a file this plan changed). Design build PASSED, 0 warnings (`reviews/006-design-build.log`). Tests: 2,103 passed, 2 skipped, 0 failed across five assemblies (`reviews/006-test.log`: UnitTest 1,941 + MudNeatoo 33 + BaseGenerator 51 + Person.DomainModel 55 + Analyzers 23). Design.Tests: 235 passed, 0 failed (`reviews/006-design-test.log`). The red run (`reviews/006-red.log`) has 3 failed and 1 passed, as the plan says.

### Veto-tier
None.

- **Every bullet is pinned at `[integration]`.** All four tests use real Neatoo objects (`RegisteredRoot` through its generated factory, from `IntegrationTestBase` DI) and mock nothing.
  - `AWrite_IsNotOver_WhileAnEarlierSubscriberOfTheObjectIsStillRunning`: the first of two object subscribers is held on a gate, and the test asserts the write task has not completed. In the red run it failed for the right reason: `writing.IsCompleted` was true, because the multicast delegate returned the second subscriber's task.
  - `AWrite_IsNotOver_WhileItsRulesRun_WhenSomeoneElseAlsoListensToTheProperty`: the property's subscribers are the framework's property manager (which runs the gated rule) and then a consumer. The test asserts the write is pending and `IsBusy` is true, then that both clear once the gate is released. Red for the right reason.
  - `AWrite_ReportsWhatAnEarlierSubscriberThrew`: the earlier subscriber faults asynchronously and the later one completes at once. The test asserts that the write's task throws `InvalidOperationException` with the subscriber's message. Red for the right reason ("no exception was thrown").
  - `TheIdleAnnouncement_IsRaisedBeforeTheWaitCompletes`: see the AC-11 judgment below.
- **No sacred test was weakened.** `git diff wait-arc...HEAD` touches one test file, the new `EverySubscriberIsAwaitedTests.cs`. `FrameworkRegistersObjects.cs` and `OneThreadAtATime.cs` are unchanged. The plan's "No existing test was changed" holds.

**AC-11: is the test honest, and does it tell "before" from "at some point"?** Yes, on both counts.
- The handler records `wait.IsCompleted` at the moment idle is announced. The assertion is `AreEqual(false, …)`, so the test fails in both wrong cases. If idle were never announced, the value stays `null`. If idle were announced after the wait completed, it records `true`.
- The second case can actually occur on this harness:
  - `WaitForTasks()` is `async` over `RunningTasks.AllDone` (`ValidateBase.cs:662`), and AllDone's `TaskCompletionSource` is created without `RunContinuationsAsynchronously` (`AsyncTasks.cs:72`).
  - On the owning thread the captured context is `SynchronizationContext.Current` (`CreateCopy` returns `this`), so `SetResult` runs the `WaitForTasks` continuation inline.
  - So if `SequenceCompleted` completed AllDone before calling `OnFullSequenceComplete`, `wait` would already be complete when the handler ran, and the test would fail.
- The test is green because `AsyncTasks.cs:241` raises the callback before `SetResult` (line 287). That is WAIT-002/005's ordering, so the passing red run is consistent with "delivered earlier, pinned here". The test doesn't pass trivially.

**AC-12: the record.** The record is clear, and no test pretends to cover AC-12.
- The parking is recorded consistently in four places: the plan's Serves line, its Plan Amendment (with the `MarkUnmodified` reason), the todo's Parked list, and a Discovery Log entry (Queue).
- The todo's AC-12 checkbox is still unchecked.
- `#103` is an issue the todo already adopted (todo line 11), not a new issue, so the standing order that parked findings are `proposed` rows is not crossed.
- None of the four tests calls `Save`, and none claims AC-12.

**The one-thread move.** It hides nothing within the documented model.
- `SetPrivateValue` throws `this.Task.Exception` when the cascade has already faulted by the time the setter checks (`ValidateProperty.cs`, after `HandleNonNullValue`). That is the documented synchronous-fault path.
- On the pool, `Task.Yield` can resume the subscriber on another thread while the write is still on the caller's thread. The fault then sometimes surfaces from the setter, outside the `ThrowsExactlyAsync`. That is two threads running over one graph, which `CLAUDE-DESIGN.md` "Single-Threaded Object Model" and Keith's 2026-11-24 ruling exclude.
- On one thread the fault reliably takes the asynchronous path, which is the path the bullet is about. The assertion was not loosened: it is the same exact type and message.

### Callouts (≤ 5 Optional)
None.

The five call sites now use `InvokeAll`. Two of them are exercised directly with more than one subscriber: `ValidateBase.RaiseNeatooPropertyChanged` and `ValidateProperty.OnValueNeatooPropertyChanged`. The other three (`PassThruValueNeatooPropertyChanged`, `ValidatePropertyManager`, `ValidateListBase`) run the same helper. The single-subscriber branch runs in every existing test.

### Tech-debt (untiered, ≤ 5, one line each)
- The todo's Plans table row for 006 still lists `AC-10, AC-11, AC-12` under Serves, while the plan's own Serves says AC-12 is parked. Someone reading only the table could take AC-12 as delivered by 006.

### Could not verify
- The red run of `AWrite_ReportsWhatAnEarlierSubscriberThrew` was on the earlier plain-async form, not the committed `OneThreadAtATime.Run` form. Its stack frame is the method itself at line 92; the committed assert is inside a lambda at line 94. By reading the code, the committed form is also red on the old code: the multicast returns the second subscriber's completed task, the setter sees no fault, and the write completes cleanly. But no log shows that run.

### Theoretical (not triaged)
- Off the owning thread, one subscriber fault can surface either as an `AggregateException` thrown synchronously from the setter or as the write task's fault, nondeterministically. This is outside the single-threaded model by ruling.

### Read report
- **Read beyond the brief:**
  - `ValidateBase.WaitForTasks`, `AsyncTasks.SequenceCompleted` and `ValidateProperty.SetValue`/`SetPrivateValue`, to judge whether the AC-11 test can tell the two orderings apart and whether the one-thread move hides a fault path.
  - The "Single-Threaded Object Model" section of `src/Design/CLAUDE-DESIGN.md`, for the threading ruling.
- **Named but unused:** none.
