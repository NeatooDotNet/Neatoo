## Code Review — WAIT-002 — 2026-11-24

**Verdict: APPROVED**
**Logs:** build PASSED — `reviews/002-build.log` (0 errors; 78 warnings, all MSTEST analyzer warnings in test projects), `reviews/002-design-build.log` (0 warnings, 0 errors); tests 2,287 passed, 0 failed, 2 skipped (pre-existing: `FatClientValidate_Deserialize_SharedDictionaryReference`, `AsyncFlowTests_CheckAllRules`) — `reviews/002-test.log`; Design.Tests 235 passed, 0 failed — `reviews/002-design-test.log`.

### Direction & shape
The work landed in the right place. The tracker is behind a public `IAsyncTasks` (`src/Neatoo/IAsyncTasks.cs`). `IValidateBaseServices<T>.CreateAsyncTasks()` supplies it, and the `CreateAsyncTasks` delegate is registered in `AddNeatooServices` the way the property-manager delegates are. `ValidateBase` no longer constructs its tracker (`ValidateBase.cs:126,213`). That satisfies D36. The default `new AsyncTasks()` exists only in the services' convenience constructors, which the plan's Framework Alignment allows. Fault recovery is contained in `Internal/AsyncTasks.cs`. Two flags (`_allDoneObserved`, `_allDoneClosed`) and a carried `_unobservedFault` implement D34's "surfaces once, then recovers". No existing tracker test changed: the diff touches no existing test file except `CustomEntityPropertyEntity.cs`, where the wrapper forwards the new member. Both amendments, the rule id on exception messages and the single DI constructor, are recorded in Amendments and the Discovery Log.

**Interleavings walked (`AsyncTasks.cs`), all correct under the lock discipline:**
- A wait takes `AllDone` while the sequence runs, then it faults. `Observed` is set (:110-113). At close (:218-227) the completion source is dropped and `SetException` is called on the captured local (:237). The waiter throws once, and later waits get `Task.CompletedTask` (:105-108).
- Nobody waits, it faults, then two waits arrive. `_unobservedFault` is set (:230). The first wait is handed the faulted task, and the fault and the source are cleared (:114-119). The second wait gets a completed task.
- Nobody waits, it faults, then a task is added before any wait. `AddTask` sees `_allDoneClosed`, moves the fault into `_exceptions` and starts a new source (:61-73). The new sequence reports it once.
- A task is added from inside `OnFullSequenceComplete`. `_allDoneClosed` is still false, so the task joins the current source. The second lock sees `_tasks.Count > 0`, carries the exceptions forward and returns (:212-216).
- A task is added between the final lock and `SetException`/`SetResult`. `_allDoneClosed` is true, so a new source is created. The old source is completed through the captured local, and an unobserved fault moves to the new sequence. No double delivery.
- Two tasks fault in one sequence. Both are added to `_exceptions` in the first lock (:171-174) and reported together.

**DI constructor selection:** checked against MS.DI's longest-satisfiable-constructor rule, with every other satisfiable constructor's parameter types required to be a subset of the chosen one's. In `ValidateBaseServices<T>`, the new 6-parameter constructor (`Internal/ValidateBaseServices.cs:86-96`) is the unique longest, and every other constructor's types are a subset of it. In `EntityBaseServices<T>`, the new 7-parameter constructor (`EntityBaseServices.cs:104-114`) is likewise unique and a superset. `IFactorySave<T>?` and `ILoggerFactory?` have defaults, so this constructor is chosen whether or not either is registered, and no ambiguity is introduced. `TrackerFromServicesTests` exercises the no-logging, no-save-factory container.

**RuleManager (`Rules/RuleManager.cs:772-788`):** stamping `rule.RuleId` changes which messages survive in two more ways, beyond "the next clean run clears it". Neither contradicts D2 or D34:
1. A thrown rule's exception message now replaces that rule's own earlier message on the trigger property. Before, the two coexisted.
2. A second throwing rule on the same property no longer evicts the first rule's exception message. Before, both carried `RuleId` 0 and `SetMessagesForRule` (`ValidateProperty.cs:408`) removed the other.

**Skill snippet:** `CustomPropertyType.cs:158` and `skills/neatoo/references/properties.md:473` match. The region is `#region` :135 to `#endregion` :179, matching the regenerated `L135-L179`.

### Veto-tier
None.

### Callouts
- `Internal/AsyncTasks.cs:110-113, 222-227`; `ValidateBase.cs:671-674` — Confidence: Medium. `Affects: AC-7`. `Reachable by:` a caller of `WaitForTasks(token)` whose token fires while an async rule is in flight (a component disposed, or a timeout CTS).
  - `WaitForCompletion` reads `AllDone`, which marks the sequence observed. It then abandons the task on cancellation (`AsyncTasks.cs:149-154`).
  - When the rule then throws, the close drops the source as "observed". The fault reaches no wait, and the next `WaitForTasks()` completes cleanly.
  - The trigger property still carries the message, so the object is invalid but nothing throws. AC-7's "surfaces once" becomes "surfaces zero times" on this path.
  - The same applies to any reader of `AllDone` that does not await it.
- `src/Neatoo/IValidateBaseServices.cs:57`; `ValidateBase.cs:126`; `AsyncTasks.cs:46` — Confidence: Medium. `Affects: AC-9`. `Reachable by:` a consumer upgrading who has their own services wrapper (the skill teaches one, `skill-custom-property-services`), who calls `RunningTasks.AddTask(t, bool)`, or who types `RunningTasks` as `AsyncTasks`. Their code no longer compiles.
  - These are source-breaking changes: a required interface member was added, a protected member's type changed, and a public parameter was removed.
  - Plan 008's scope names "the migration for the removed members". Nothing in the todo or the Discovery Log records these three for it.

### Could not verify
- `002-test.log` is minimal verbosity. The 11 new tests are inside the totals but are not named individually.

### Theoretical (not triaged)
- Pre-existing race in `SequenceCompleted`, kept as it was; thread safety is Out of Scope. A task added in `OnFullSequenceComplete` can complete on the pool before the outer call reaches its second lock (:207). Both calls then close the sequence, and the second `SetResult`/`SetException` on the same source throws inside an unobserved continuation. The outer exceptions are lost, or are left in `_unobservedFault` beside a source that already succeeded.
- A fault carried into the next sequence (:64-68) leaves the old source's faulted task never awaited, so `TaskScheduler.UnobservedTaskException` fires for it on GC.

### Read report
- Read beyond the brief: `Internal/ValidateProperty.cs:395-445` (`SetMessagesForRule` semantics); `ValidateBase.cs:640-676, 950-972` (`AllDone` consumers); `plans/008-docs-and-release-notes.md` Scope.
- Named but unused: `docs/VisionAlignment.md` D2 and D35 (read only the D34 and D36 rows); the plan's Current State (background only).
