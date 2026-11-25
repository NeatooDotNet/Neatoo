## Code Review — WAIT-004 — [2026-11-24]

**Verdict: APPROVED**
**Logs:** build PASSED, 0 warnings (`reviews/004-build.log`, `reviews/004-design-build.log`); tests 2,080 passed, 2 skipped, 0 failed (`reviews/004-test.log`: Neatoo.UnitTest 1,918 + 2 skipped, BaseGenerator 51, MudNeatoo 33, Analyzers 23, Person 55), Design.Tests 235 passed (`reviews/004-design-test.log`). The two skipped tests (`FatClientValidate_Deserialize_SharedDictionaryReference`, `AsyncFlowTests_CheckAllRules`) were already skipped in `003-test.log`. Totals move 1,911 → 1,920 and 50 → 51, which matches the nine new integration tests and one new generator test. No test was removed.

### Direction & shape
The deliverable landed where D32 puts it. A property reports its write from one place, `ValidateProperty.SetPrivateValue` (`src/Neatoo/Internal/ValidateProperty.cs:261`, via `ReportToOwner` at :277). The setter, `Set{Name}`, `IValidateProperty.SetValue` and `Setter` all pass through that point, and so does the `ObjectInvalid` setter. The report path is given to the property at its single attach site (`ValidatePropertyManager.Attach`, :114-122, reached from both `Register` and the deserialization `SetProperties`, :275/:303). The delegate is late-bound through `ReportTaskStarted`, so binding order does not matter. The entity registers each `RunRules(propertyName)` run itself (`ValidateBase.cs:884-896`). That one seam also covers the meta-rule path and the parent's `ChildNeatooPropertyChanged` run, and through the latter a list's Count change, which was unregistered before.

`AddChildTask` now registers with the object itself before passing the task up (`ValidateBase.cs:539-547`). The ordering claim holds:
- `WhenFinished` uses `ExecuteSynchronously`, so continuations run in registration order.
- The write task completes only after the property's own accounting (`ValidateProperty.cs:420-441`).
- So the chain is property, then object, then parent.

The generator change and the four call sites that stopped registering match Step 5. The exception de-duplication in `AsyncTasks.SequenceCompleted` (:208-220) is per-sequence and by reference, which is the case the plan describes: the write and the rule run inside it are both open in one sequence. `RunRules(RunRulesFlag)` remains unregistered, so it does not wait for itself (Constraint 3). No reflection was added. Nothing is `new`ed in a base class (D36).

For WAIT-005, the remaining unregistered paths are lazy-load edge cases (callouts below) and a few theoretical entry points.

### Veto-tier
None.

### Callouts

- **`src/Neatoo/Internal/LazyLoadEntityProperty.cs:205-228` and `LazyLoadValidateProperty.cs:289-311` · High (traced, not run) · `Affects: AC-1`, AC-3 · `Reachable by:` assigning a new wrapper to a lazy property (the generated lazy setter calls `LoadValue`, `PropertyGenerator.cs:35`) while the current wrapper's load is in flight, when the new wrapper is pre-loaded or never loaded.**
  - **The mechanism.** The reported `_loadInFlight` task completes only when this property receives the wrapper's falling `IsLoading`. But `ValidateProperty.LoadValue` (:290-299) unsubscribes `PassThruValuePropertyChanged` from the old wrapper when it is replaced, and `HandleNonNullValue` (:354-357) does the same. The old wrapper's `finally { _isLoading = false; OnPropertyChanged(IsLoading) }` (RemoteFactory `LazyLoad.LoadAsyncCore`) never reaches the property.
  - **The result.** The task stays in the owner's and every ancestor's `RunningTasks` for good:
    - `IsBusy` stays true, so `IsSavable` is false and `Save()` throws `IsBusy`.
    - `WaitForTasks()` never completes, because it awaits `RunningTasks.AllDone` before the property manager.
    - `Save(token)` hangs until the token is cancelled.
  - **Before this plan.** `WaitForTasks()` reached the new wrapper's `WaitForTasks()` and completed, so this is new behavior.
  - **Not affected.** If the new wrapper later loads, its falling `IsLoading` completes the old task. Setting the property to null also completes it, because `HandleNullValue` leaves the `PropertyChanged` subscription in place.
  - **Scope.** No test replaces a wrapper mid-load. LazyLoad is pending removal (D24), but WAIT-005's "wait is the running tasks alone" depends on this task ending.
- **The same lines · Medium (traced, not run) · `Affects: AC-1` · `Reachable by:` user code that starts `LoadAsync()` on a wrapper before assigning it to the property.**
  - **The mechanism.** The rising `IsLoading` fires before the property subscribes, so nothing is reported. The falling edge finds `_loadInFlight == null` and does nothing. The load is never in the owner's running tasks.
  - **Today.** This is harmless: `WaitForTasks()` still descends into the wrapper.
  - **Under WAIT-005's design.** `WaitForTasks()` would not wait for that load.
  - **Remedy.** One option: report when a wrapper that is already loading is attached.
  - **Callers.** No live caller in the repo does this. The Person example calls `LoadAsync()` on an already-assigned property (`Person.cs:119`, `:152`).

### Could not verify
- `004-test.log` is at minimal verbosity, so the nine `FrameworkRegistersTests` and `GeneratedSetterAndSetName_RegisterNothing_TheOwnerRegisters` are evidenced only by the count deltas above, not by name.
- The brief and plan Notes call an un-awaited `RunRules(RunRulesFlag)` "#89, out of scope". The todo's Out of Scope describes #89 as "whether `RunRules(Self)` drains its children". I could not confirm that #89 covers the un-awaited case. Its rule runs are tracked on trigger properties (`RuleManager.cs:713-719`) but are not reported to the entity, so WAIT-005 will meet it.
- `LazyLoadValidateProperty` (a ValidateBase owner) has no test of its own. It is checked only by being identical to the entity variant, which `ALazyLoadInProgress_IsInTheParentsRunningTasks` exercises.
- The plan's Test Evidence says "7 of the 9 tests failed" but names six. The seventh in `004-red.log` is `ABusyObjectAssignedToAParent_IsInTheParentsRunningTasks`, which became the guard test. Its red failure (`ChildObjectBusyException` from `HandleNonNullValue`) is the evidence that the guard's refusal predates the change. The two match; the plan's wording is the only gap.

### Theoretical (not triaged)
- An entity calling `RuleManager.RunRule(rule)` or `RuleManager.RunRules(name)` directly from a verb, un-awaited, bypasses `ValidateBase.RunRules(string)` and is not registered. No caller in the repo does this.
- A consumer-registered property manager that does not implement `IValidatePropertyManagerInternal` gets no report path, silently (`ValidateBase.cs:206`). So does a manager instance shared by two owners, where the second assignment of `TaskStarted` wins.
- An explicit `RunRules("X")` whose rule awaits `WaitForTasks()` or `RunRules(RunRulesFlag)` on its own target now waits for itself. This was already true for runs triggered by a setter write, whose write task was registered.
- Under a factory operation's pause, an awaited `RunRules(name)` with an async rule now makes the object's `IsBusy` rise through `RunningTasks`. The `CheckIfMetaPropertiesChanged` in `RunRulesFor`'s `finally` then raises `NeatooPropertyChanged("IsBusy")`, rising and falling, because `RaiseIfChanged` (`ValidateBase.cs:319`) does not check `IsPaused`. Paused parents only reset meta state.

### Read report
- Read beyond the brief:
  - `TaskFinishedExtensions.WhenFinished` (continuation options, for the ordering claim).
  - `EntityLazyLoad.cs`, and RemoteFactory `LazyLoad.cs` from the local checkout (`IsLoading` raise order, behavior only).
  - `ValidateProperty.LoadValue` and `HandleNonNullValue`/`HandleNullValue` subscription handling.
  - `ValidatePropertyManager.SetProperties`/`Register`/`Property_PropertyChanged`.
  - `ValidateListBase` `InsertItem`/`RemoveItem`/`ClearItems`/`HandleNeatooPropertyChanged`.
  - `EntityBase` `MarkUnmodified`/`ChildNeatooPropertyChanged`/`Save`/`Save(token)`.
  - `RuleManager.RunRule` and `AllRequiredRulesExecuted`.
  - The lazy branch of `PropertyGenerator.cs`.
  - The KnockOff stub diffs.
  - `003-test.log` (count baseline) and `004-red.log` in full.
  - The WAIT-008 plan's scope line (it covers the API reference, so the stale `AddChildTask` rows in `docs/reference/api.md:256`, `:1063` are already owned).
  - `docs/guides/async.md` on `RunRules` inside factory operations.
- Named but unused: `plans/003-property-tasks.md` (the round 2 callout in `reviews/003-code-review.md` was enough for the Punchlist row); `src/Design/CLAUDE-DESIGN.md` beyond the "Single-Threaded Object Model" subsection, where nothing in this change contradicts it.
