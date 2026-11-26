## Test Review — WAIT-002 — [2026-11-24] — Round 1

**Verdict: APPROVED**
**Logs:** build PASSED, 0 errors (`reviews/002-build.log`); design build PASSED, 0 errors, 0 warnings (`reviews/002-design-build.log`); tests 2347 passed, 0 failed, 2 skipped (`reviews/002-test.log`: Neatoo.UnitTest 1891/0/2, MudNeatoo 33, BaseGenerator 50, Analyzers 23, Person 55; Neatoo.UnitTest.Demo reports "No test is available", same as before); design tests 235 passed, 0 failed (`reviews/002-design-test.log`). Both skipped tests (`FatClientValidate_Deserialize_SharedDictionaryReference`, `AsyncFlowTests_CheckAllRules`) were already `[Ignore]`d on `wait-arc`.

### Veto-tier
None.

Per bullet (all Required):
- **A task added after a fault is tracked.** `AddTask_AfterAFaultedSequence_IsTracked_AndItsWaitCompletesCleanly` asserts `IsRunning`, an incomplete wait, and a clean completion. Unit tier. Fails on the old `AddTask`, which returned the faulted task without tracking it.
- **An observed fault is not thrown again.** `Fault_ObservedByAWaitInProgress_IsNotThrownByALaterWait` reads `AllDone` before the fault, then a later `AllDone` must complete cleanly. Unit tier. Can fail.
- **An unobserved fault is thrown by the next wait, once.** `Fault_NobodyWasWaitingFor_IsThrownByTheNextWait_Once` never reads `AllDone` before the fault, then asserts a single throw carrying "unobserved" and a clean second wait. Unit tier. It fails if the hand-off does not clear the source (the second wait throws) and if the fault is not recorded. It reaches the "nobody was waiting" state by timing, not by construction; see Tech-debt.
- **An unobserved fault is reported with the next sequence.** In `Fault_NotObservedBeforeTheNextSequence_IsReportedWithThatSequence`, nothing reads `AllDone` before the second `AddTask`, so the fault stays unobserved. Discarding `AddTask`'s return value is not an observation. The test asserts that the second sequence's wait throws "carried". Unit tier. Fails without the carry-forward.
- **A callback that throws any exception faults the wait.** `CompletionCallback_ThatThrowsAnyException_FaultsTheWait` uses a non-`AggregateException` and asserts an `AggregateException` that carries it within 5 s. On the old code the wait hung and `WaitAsync` timed out, so the test can fail. Unit tier.
- **A thrown async rule surfaces once, the property is invalid, a later write is tracked.** `AsyncRuleFaultTests` covers each part:
  - "surfaces": `_SurfacesFromWaitForTasks` checks the type and message.
  - "once": `_IsNotThrownByASecondWait`.
  - "invalid with the message": `_LeavesTheTriggerPropertyInvalidWithTheMessage` checks object and property `IsValid` and `PropertyMessages.Single()`.
  - "tracked and waited for": `AfterAFault_ALaterWriteIsTrackedAndWaitedFor`.
  - "valid once the rule runs clean" (amendment): `_TheObjectIsValidOnceTheRuleRunsClean`.
  - Integration tier, real DI and a real `ValidateBase`.
  - `AfterAFault_ALaterWriteIsTrackedAndWaitedFor` is not vacuous. `WaitForTasks()` awaits `RunningTasks.AllDone`, then `PropertyManager.WaitForTasks()`. For a string property, the second either completes synchronously or spins synchronously (`ValidateProperty.WaitForTasks` returns the completed `ValueAsBase?.WaitForTasks() ?? Task.CompletedTask`). So `IsCompleted == false` can only come from an incomplete `AllDone`, which means the rule's task is tracked. On the old code `AllDone` was the faulted task, so `IsCompleted` was true.
- **An entity takes its tracker from the registered factory.** `AreSame(supplied.Single(), created.Tracker)` over a fresh container with `CreateAsyncTasks` registered after `AddNeatooServices`:
  - `AsyncRuleFaultTests.Entity_TakesItsTrackerFromTheRegisteredFactory` covers a `ValidateBase` object.
  - `TrackerFromServicesTests.EntityBase_TakesItsTrackerFromTheRegisteredFactory` covers an `EntityBase` object.
  - `_WithNothingRegisteredOverIt_TakesTheFrameworkTracker` covers the default registration path.
  - Integration tier. Both base classes are proven.

Sacred tests: `git diff wait-arc...HEAD --stat -- src/Neatoo.UnitTest src/Design` shows only additions: 3 new test files, plus a 1-line forwarding member in each of `CustomEntityPropertyEntity.cs` and `CustomPropertyType.cs`. `AsyncTasksTests.cs` and `AsyncTasksCompletionCallbackTests.cs` are unchanged. No test was removed, weakened or re-expected.

### Callouts (≤ 5 Optional)
None.

### Tech-debt (untiered, ≤ 5, one line each)
- `AsyncTasksFaultRecoveryTests.UntilIdle` polls `IsRunning`, which clears before the sequence closes. `IsRunning` is set false in `SequenceCompleted`'s first lock, and `_allDoneClosed` is set in its second. If the test reads `AllDone` (or calls `AddTask`) inside that window, the "unobserved" tests take the observed or same-sequence path and pass without exercising the hand-off or the carry-forward. The window is narrow, and both tests can still fail on the main path.
- `AsyncRuleFaultTests.Entity_TakesItsTrackerFromTheRegisteredFactory` exercises a `ValidateBase` object, not an entity. The name says entity.

### Could not verify
- `002-test.log` and `002-design-test.log` are at minimal verbosity and list no test names. That the 11 cited tests ran is inferred from the green totals, not confirmed by name.

### Theoretical (not triaged)
- The `AddNeatooServices` comment says a consumer can register `IAsyncTasks` itself after the call to replace the tracker. No test registers `IAsyncTasks`. The code path is the default `CreateAsyncTasks` delegate, which the default-registration test does exercise.

### Read report
- Read beyond the brief: `ValidateBase.WaitForTasks` and `ValidatePropertyManager` / `ValidateProperty.WaitForTasks`, to judge whether `wait.IsCompleted` could be false for a reason other than tracking. Also `git grep` on `wait-arc` for the two skipped tests, to confirm they were already ignored.
- Named but unused: `docs/VisionAlignment.md` D34/D36 (the plan's bullets were sufficient); `IAsyncTasks.cs` (not needed to judge the tests).
