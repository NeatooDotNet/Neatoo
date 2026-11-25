## Test Review — WAIT-005 — [2026-11-24] — Round 1

**Verdict: CONCERNS**
**Logs:** build PASSED (0 errors, 97 warnings, the same count as `004-build.log`) — `reviews/005-build.log`; design build PASSED (0 warnings) — `reviews/005-design-build.log`; tests 2,090 passed, 0 failed, 2 skipped (`FatClientValidate_Deserialize_SharedDictionaryReference`, `AsyncFlowTests_CheckAllRules`) — `reviews/005-test.log`; design tests 235 passed, 0 failed — `reviews/005-design-test.log`. Red run `reviews/005-red.log`: 7 run, 1 failed (`RunRulesWithAFlag_NotAwaited_IsInTheObjectsRunningTasks`, at `WaitsAwaitTasksTests.cs:88`, the "running tasks hold the rules" assert). That matches the map.

Sacred tests: `git diff wait-arc...HEAD` touches one test file, the new `WaitsAwaitTasksTests.cs`. No existing test was changed, so the plan's claim holds.

### Veto-tier

- **Bullet 5, "the root, its child, its list, the list's item and their properties are not busy".** `WhenTheRootsWaitCompletes_NothingOnItOrBelowItIsBusy` checks all four objects and the list. It checks only two properties: the child's `Value` and the root's `Child`. It never checks the list item's `Value`, the property whose write is still in flight when the child's gate opens. It also never checks the root's `Children` or `Value`. So the bullet's "the list item's properties" part is unpinned, and the item is the case where the work goes through the list path. Fix shape: in the same test, assert `item[nameof(IRegisteredChild.Value)].IsBusy` and `root[nameof(IRegisteredRoot.Children)].IsBusy` are false. That is two lines, at the same tier (`[integration]`).
  - On the question asked: the test does pin todo AC-4 as written, because AC-4 names the object and its descendants and the test asserts every descendant object. The gap is only against the plan's stricter bullet, which adds "their properties".

### Callouts (≤ 5 Optional)

- **An awaited `RunRules()` whose async rule throws, or is cancelled by its token, now has its rules task in the tracker.** Location: `src/Neatoo/ValidateBase.cs`, `RunRules(RunRulesFlag, CancellationToken?)` (the `AddChildTask(rules)` at line 974). Two things about this outcome have no test: the Constraint that the awaiter still gets the rules' own exception, and the Note that the next `WaitForTasks()` reports it too. The cancelled-token outcome isn't covered either. My greps found no object-level test for `RunRules` with a faulting or cancelled rule. `Affects: AC-2`. `Reachable by:` any consumer `await entity.RunRules()` or `RunRules(flag, token)` on an object whose async rule throws or is cancelled. Suggested tier: `[integration]`, `RegisteredRoot` with `Value = "throw"`.
- **`ValidateListBase.WaitForTasks(CancellationToken)` was rewritten onto `WaitForItems`, and no test calls it.** Location: `src/Neatoo/ValidateListBase.cs`, `WaitForTasks(CancellationToken)`. Bullet 6 pins only the parameterless list wait. `Affects: AC-5`. `Reachable by:` `EntityLazyLoad.WaitForTasks(token)` when the lazy value is a list (`src/Neatoo/EntityLazyLoad.cs:79`), and by consumers through `IValidateMetaProperties`. Suggested tier: `[integration]`, the bullet 6 shape with a token, plus a cancelled-token case.
- **A root's rising `IsBusy` notification for a write on a child or list item now comes only from the parent chain in `AddChildTask`.** Location: `src/Neatoo/ValidateBase.cs`, `AddChildTask` (the `wasRunning` check). Bullet 7's test covers a write to the root's own property. Nothing checks that the root announces busy once and idle once when the write is on its child. `Affects: AC-4`. `Reachable by:` the generated setter on any child or list item under a root whose `IsBusy` a UI binds. Suggested tier: `[integration]`, bullet 7's shape with the write on `root.Child`.

### Tech-debt (untiered, ≤ 5, one line each)

- `src/Neatoo.UnitTest/AsyncFlowTests/AsyncFlowTests.cs:77` keeps a commented-out `CollectionAssert.Contains(propertyChangedCalls, "IsBusy")`. It predates this plan.
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs:24-25` still says the object's wait reaches lazy loads through `PropertyManager.WaitForTasks()`. That is no longer true. It belongs to WAIT-008 (AC-9).
- `ValidatePropertyManager.WaitForTasks()` stays public, but the object's wait no longer calls it (plan Notes).

### Could not verify

- I could not show that the new rising-edge check in `AddChildTask` is what makes bullet 7 pass. Its only pin, `AnObject_AnnouncesBusyOnceAndIdleOnce_ForAWriteThroughTheProperty`, was green before the change through the old property-manager path. It has never been seen red with the new check removed. I did not run that mutation.

### Theoretical (not triaged)

- `WaitForItems` has a "go round again" branch for when an item's finished work starts more work. No test reaches it, and I traced no input that does.
- The AC-3 test opens the gate before its first await. A wait that completed one tick early, before the work finished, would not be caught. The implementation (`RunningTasks.AllDone`) makes that implausible.

### Judgement on the six tests that were green before the change

The evidence is honest. Bullets 1, 3, 4, 5, 6 and 7 each claim behavior a caller already saw after WAIT-003 and WAIT-004, and this plan has to keep that behavior while it removes the flag path. A test that is green before and after is the right evidence for that kind of claim. The things only this plan delivers are these:

- **Un-awaited `RunRules()` is registered.** This one has the red run.
- **The token wait no longer differs from the plain wait.** The old difference was `PropertyManager.WaitForTasks()` over busy properties. After WAIT-004 the only work that path still caught was the un-awaited `RunRules()` above, which is red-proven, and custom properties that report busy without a task, which the Notes say are deliberately no longer waited for.
- **The rising notification moved to `AddChildTask`.** Bullet 7 pins the behavior, but the test has not been seen red against the new line (see Could not verify).

AC-2's "polls no flag" is structural, and the diff bears it out: neither the object's waits nor the list's loop on a busy flag any more. `WaitForItems` reads `IsBusy` only to choose which items' waits to await.

On AC-3: `OnOneThread_WaitForTasks_ReturnsAnIncompleteTask_AndCompletesWhenTheWorkDoes` pins it as written. It runs under `OneThreadAtATime` with a `[Timeout]` and `WaitAsync(Limit)`, and it covers all four ways the bullet names to start work. A wait that polled and never returned would fail at `WaitAsync(Limit)`.

### Read report

- Read beyond the brief:
  - `src/Neatoo/EntityLazyLoad.cs` callers: to find a live caller for the list's token wait.
  - `AsyncFlowTests.cs` and `ValidateBaseAsyncTests.cs` greps: for existing `IsBusy` notification coverage.
  - `004-build.log`: to compare the warning count.
- Named but unused: `docs/VisionAlignment.md` D33. No finding depended on its wording.
