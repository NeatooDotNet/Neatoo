## Code Review — WAIT-008 — 2026-11-24

**Verdict: APPROVED** (no veto-tier finding; four callouts on AC-9, Required, listed in full below)
**Logs:** build PASSED — `reviews/008-build.log` (0 errors, 97 warnings, all CS4014/CS8622/CS8629/MSTEST0032 in test projects), `reviews/008-design-build.log` (0 errors, 0 warnings); tests 2,344 passed, 0 failed, 2 skipped — `reviews/008-test.log` (MudNeatoo 38, BaseGenerator 51, Analyzers 23, Person 55, UnitTest 1,942 + 2 skipped; UnitTest.Demo has no tests, as on earlier plans), `reviews/008-design-test.log` (Design.Tests 235). No failures to name.

The owner's ruling on the lazy-load change is still pending. This verdict covers the prose against the code. It does not cover that ruling.

### Direction & shape

The prose lands where the code is. I checked each behavioral sentence the diff adds against the product code (`git diff origin/main...HEAD -- src/Neatoo src/Neatoo.BaseGenerator`). These claims hold:

- **Tracking.** The owner registers every write, rule run and lazy load. The setter path is `ValidateProperty.SetPrivateValue` → `ReportToOwner` → `ValidatePropertyManager.TaskStarted` → `ValidateBase.AddChildTask`. `RunRules(propertyName)` and `RunRules(RunRulesFlag)` register themselves. `AddChildTask` registers on the object first, then on the parent.
- **Waiting.** `WaitForTasks()` is `RunningTasks.AllDone` and nothing else. `WaitForTasks(token)` is the same wait with cancellation.
- **Busy.** On an object, busy is `RunningTasks.IsRunning`. On a property it is `ValueAsBase?.IsBusy || HasTasksInFlight`. On a list it is `this.Any(item => item.IsBusy)`.
- **Faults.** A rule's fault is reported once, in an `AggregateException`: `AsyncTasks.SequenceCompleted` calls `SetException(new AggregateException(...))`, and the observer and unobserved-fault bookkeeping limits it to one report. The exception message now carries the rule id.
- **Handlers.** `InvokeAll` awaits every subscriber.

The release notes' "before" column also checks out against `origin/main`. Main's `AsyncTasks.AddTask` returned the faulted completion task for good. Main's `ValidateProperty.IsBusy` had the `??` / `||` precedence slip, so a property holding a child ignored its own work. Main's `OnValueNeatooPropertyChanged` skipped tracking a second overlapping write. Main's `WaitForTasks(token)` did not call `PropertyManager.WaitForTasks()`.

The Compile breaks table matches the public and protected signature diff, with nothing missing and nothing invented. The gaps are in three behavior statements, and in the reach of the handler rule (callouts 1 to 4). The Design.Domain comment edits are accurate. The two `ValidateListBaseTests.cs` edits change comments only, with no assertion touched, and the `EntityPropertyManager.cs` edit is comment-only. No hand-typed C# was added to a guide or a skill: the diff adds no fenced block and no snippet placeholder, and the 396 placeholders all have their `endSnippet`.

### Veto-tier

None.

### Callouts

**1. The migration advice to use `TrackTask(task)` makes the property busy, but not its object.** `docs/release-notes/v0.37.0.md:125` (Compile breaks, first row) and `:157-158` (Behavior change 5). Confidence: High. `Affects: AC-9`. `Reachable by:` a consumer with a custom property class who follows the migration row.

Behavior change 5 says a custom property class that reports `IsBusy` without a task "no longer makes its object busy or holds its wait open. Track the work with `TrackTask(task)`." That advice does not do what it promises:

- `ValidateProperty.TrackTask` (`src/Neatoo/Internal/ValidateProperty.cs:164-188`) adds the task only to the property's own tracker (`_tasks`).
- It never calls `_reportToOwner`.
- The object's `IsBusy` and `WaitForTasks()` read only `RunningTasks` (`ValidateBase.cs:172`, `:662-665`).

After following the advice, the property reads busy, but its object does not, and the object's wait does not wait. Reaching the owner takes the separate protected `ReportToOwner(task)` (`ValidateProperty.cs:277`). That is what the framework's own lazy-load properties call (`LazyLoadValidateProperty.cs:122`, `LazyLoadEntityProperty.cs:40`); they keep `HasTasksInFlight` for the property's own busy state. One option: name both members, with `TrackTask` for the field and `ReportToOwner` for the object.

**2. The release notes misstate the "before" for a thrown rule's message, so the fix is not listed.** `docs/release-notes/v0.37.0.md:68-70`. Confidence: High. `Affects: AC-9`. `Reachable by:` an async rule that throws once and later runs clean.

The text reads: "the trigger property keeps its message, so the object is invalid and unsavable until the rule runs clean." It frames this as unchanged ("still"). On `main` it was not so:

- Main's `RuleManager.RunRule` catch block (`git show origin/main:src/Neatoo/Rules/RuleManager.cs`, about line 773) stored the exception message with no `RuleId`.
- `ClearMessagesForRule(rule.RuleId)` on the next clean run therefore never removed it.
- The object stayed invalid until `ClearAllMessages` or `RunRules(All)` cleared it.

The arc sets `RuleId` (`RuleManager.cs`, the catch in `RunRuleAndApplyMessages`), which is WAIT-002's Discovery Log entry. So the Behavior changes have a missing row: an object whose rule threw now becomes valid again when that rule next runs clean. `docs/guides/async.md:86` states the new behavior correctly ("its next clean run clears it"). Only the release notes have the before wrong.

**3. Behavior change 4 leaves out one consequence: a handler's exception now faults the write.** `docs/release-notes/v0.37.0.md:146-155`. Confidence: High. `Affects: AC-9`. `Reachable by:` a `NeatooPropertyChanged` subscriber, other than the last one, that throws after an `await` (a second UI subscriber, or a parent property beside a page handler).

Before, `NeatooPropertyChanged?.Invoke` returned only the last subscriber's task, so an earlier subscriber's async exception went unobserved. Now `NeatooPropertyChangedInvocation.InvokeAll` returns `Task.WhenAll` of every subscriber's task, and the path runs `ValidateProperty.OnValueNeatooPropertyChanged` → `ValidateBase.ChildNeatooPropertyChanged` → `RaiseNeatooPropertyChanged`. So that exception now:

- faults the write, which throws to the awaiter of `Set{Name}` or `SetValue`, and
- is reported once by the next `WaitForTasks()` on the object and on each parent.

The notes list two consequences, the self-wait and the delay. They miss this one. `docs/guides/async.md:396` says it for the awaiter ("an exception a handler throws reaches the awaiter of the write"), but not for `WaitForTasks()`.

**4. The "must not await" rule is drawn too narrowly: it misses the object where the write started.** Locations:

- `docs/guides/async.md:160`, `:98-104`
- `docs/guides/blazor.md:517`
- `docs/guides/properties.md:235`
- `docs/reference/api.md` (WaitForTasks row: "on this object or a descendant"; NeatooPropertyChanged row)
- `skills/neatoo/references/pitfalls.md` (new row)
- `skills/neatoo/references/properties.md:335`
- `skills/mudneatoo/references/property-change-events.md` (point 5)
- `src/Design/CLAUDE-DESIGN.md` ("Do not wait for yourself")

Confidence: Medium (traced in the code, not run). `Affects: AC-9`. `Reachable by:` a handler on a root's `NeatooPropertyChanged` that awaits `WaitForTasks()` on the child the change came from, or a parent rule triggered by `Child.X` that awaits the child's wait.

Every location says "on its own object (or the object that raised the event) or a parent". The write's task is registered on the object where the write started and on each ancestor (`ValidateBase.AddChildTask`). The root's handler runs inside that task:

1. The child property's write calls `OnValueNeatooPropertyChanged`.
2. That runs the child's `ChildNeatooPropertyChanged`, then the child's `RaiseNeatooPropertyChanged`.
3. The parent property's subscription passes it to the parent's `ChildNeatooPropertyChanged`, which raises it to the root's subscribers.
4. Each step is awaited, so the handler is part of the child's write.

The root re-raises with itself as `Source` (`ValidateBase.cs:465`). So a root handler that awaits `child.WaitForTasks()` waits for itself, and no sentence forbids it. Under the api.md wording ("from a rule or a NeatooPropertyChanged handler on this object or a descendant"), a handler on the root that waits on a child even reads as allowed. The same applies to a list's `HandleNeatooPropertyChanged` that awaits the changed item's `WaitForTasks()`. The full rule is "the object where the write started, or any parent of it". Ruling D33 and the WAIT-006 punchlist row are silent on this. The gap comes from the docs' wording, not from a decision.

### Could not verify

- That each object accounts for a finished task before its parent (`CLAUDE-DESIGN.md` flow, "child before parent"). I read `AddChildTask`'s order. That the continuations also run in that order rests on WAIT-004's test for #146, which I did not re-trace.
- The list `IsBusy` read costs (13 µs at 1,000 items, 176 µs at 20,000) come from `plans/005-waits-await-tasks.md` Plan Amendments. They match it, and I did not re-measure them.
- Acceptance bullet 6. `skills/neatoo` and `skills/mudneatoo` differ from `~/.claude/skills/`: nine files, including `source-generation.md`, which this diff does not touch. The bullet is still unchecked, so the copy has not happened yet.

### The lazy-load account (checked, not a finding)

The amendment and the Parked row match `reviews/008-lazy-failure-probe.log`. In P1 on `main`, the wait in progress threw `InvalidOperationException`, the loader's own exception unwrapped, by way of `PropertyManager.WaitForTasks` → `EntityLazyLoad.WaitForTasks` → `LoadTask`. On the arc the same wait completes cleanly, because `ReportLoadInFlight` reports a `TaskCompletionSource` that the code completes with `TrySetResult` whatever the outcome. P2, P3 and the waits after them match on both. Two additions make the account complete:

- On `main`, `WaitForTasks(token)` never waited for a lazy load at all, so it never threw either. On the arc it waits and does not throw.
- The comment in `ParentIsValid_AfterExplicitChildLoadFailure` (`src/Neatoo.UnitTest/Integration/Concepts/EntityBase/LazyLoadStatePropagationTests.cs:142`, "WaitForTasks propagates the exception from the faulted _loadTask") is stale under the arc's behavior. It belongs to the same ruling.

### Theoretical (not triaged)

- **The "Before 0.37.0: Not tracked" rows** in "Every write is tracked" (`SetValue`, un-awaited `RunRules`) are accurate about running tasks. On a multi-threaded host, though, main's `WaitForTasks()` did wait for that work, by spinning on the property flags. A reader may take "Not tracked" to mean "not waited for".
- **A `NeatooPropertyChanged` raised for a meta property** (`ValidateBase.RaiseIfChanged`, `:337`) or for a list's `Count` (`ValidateListBase.cs:158`, `:190`, `:264`) is not awaited. A handler's async work on those events is in nobody's running tasks. This is unchanged from `main`, and it sits beside async.md's "every async operation is a task".
- **`docs/guides/blazor.md:456` (unchanged)**: "`IsBusy` ... recalculate and cascade" is a little stale beside the new step 2. Busy is read, not recalculated.
- **A rule that awaits `RunRules(RunRulesFlag)` on its own target** re-runs itself and recurses without bound. That is worse than "waits for itself", but the guidance not to do it is right.
- **The self-wait statements** (`async.md:160`, the handler section) are supported by the code, and they were already true on `main`. There, a write through the generated setter or `Set{Name}` was registered on the target and every ancestor (main's `PropertyGenerator` and `AddChildTask`), so a rule or handler inside it awaiting that wait waited for itself. Writes through `SetValue` and `RunRules(propertyName)` hung too, through main's flag-poll loop rather than the task wait. What is new is that a later subscriber no longer rescues a handler, which Behavior change 4 says.

### Read report

- **Read beyond the brief:**
  - `src/Neatoo/EntityBase.cs` (`Save`, `Save(token)`), `src/Neatoo/EntityListBase.cs` (busy refusal) and `src/Neatoo/EntityLazyLoad.cs` (`WaitForTasks`).
  - `src/Neatoo/Exceptions.cs` (`ChildObjectBusyException`).
  - Main's `RuleManager.cs` catch block and main's `AsyncTasks.cs`.
  - `LazyLoadStatePropagationTests.cs`.
  - `reviews/007-red-main.log`, which confirms the three hangs on main that the release notes cite.
  - `plans/005-waits-await-tasks.md` (the measurements).
  - `WorkOrderTaskList.cs` (the sibling `RunRules(propertyName)` pattern).
  - The MudNeatoo components' `Disabled` bindings.
- **Named but unused:** none. I read the VisionAlignment D31 to D36 rows. No finding turned on them beyond the note in callout 4.

---

## Code Review — WAIT-008 — Round 2 — 2026-11-24

**Verdict: APPROVED** (all four Round 1 callouts addressed and verified against the code; no veto-tier finding; no new callout)
**Logs:** build PASSED — `reviews/008-build.log` (0 errors), `reviews/008-design-build.log` (0 errors, 0 warnings). Tests: 2,110 passed, 0 failed, 2 skipped — `reviews/008-test.log` (MudNeatoo 38, BaseGenerator 51, Analyzers 23, Person 55, UnitTest 1,943 + 2 skipped; one more than Round 1, which is the new lazy-load test). Design.Tests 235 passed — `reviews/008-design-test.log`. No failures to name.

A correction to Round 1's header: its "2,344 passed" was the main suite (2,109) plus Design.Tests (235). Nothing failed in either round.

Object under review: `git diff 1792c82..000f448`.

### Round 1 callouts, verified

1. **`TrackTask` alone — addressed.** `v0.37.0.md:130` and Behavior change 5 (`:166-170`) now name both protected members. The code agrees:
   - `TrackTask(task)` puts the task on the property's own tracker (`ValidateProperty.cs:164-188`).
   - `ReportToOwner(task)` (`:277-283`) calls `_reportToOwner`. That is `ValidatePropertyManager.ReportTaskStarted`, which calls `TaskStarted`, which is `ValidateBase.AddChildTask`. The object registers the task and passes it up through its parents, so it is busy and its `WaitForTasks()` waits.
   - This is the pair the framework's lazy-load properties use.

2. **The rule's message — addressed.** The fault section (`:74-89`) now separates the wait from the message, and Behavior change 6 (`:172-174`) is new. The "before" is right:
   - Main's catch block stored the exception message with `RuleId` 0.
   - A registered rule's id is never 0: `RuleBase.cs:244-246` treats 0 as unassigned.
   - So `ClearMessagesForRule(rule.RuleId)` on a clean run never removed the message. Only clearing all messages did, which `RunRules(All)` also does through `ClearAllMessages`.

3. **A throwing handler — addressed.** It is now the third consequence under Behavior change 4 (`:163-165`). It matches `NeatooPropertyChangedInvocation.InvokeAll`, which returns `Task.WhenAll` of every subscriber's task, against main's `?.Invoke`, which returned only the last one's. The "after an `await`" qualifier is right too: a subscriber that throws before returning a task stops the subscribers after it, on both branches, and that throw is synchronous.

4. **The self-wait rule — addressed.** Every place Round 1 listed now draws the rule over the object written to and everything above it:
   - `async.md:160`, and `async.md:396-403` with the new paragraph naming the child
   - `blazor.md:515-517` and `properties.md:235`
   - both rows in `api.md`
   - `pitfalls.md`, the skill's `properties.md` and mudneatoo's point 5
   - `CLAUDE-DESIGN.md` and Behavior change 4

   A search for the old wordings ("own object or a parent", "or on a parent of it", "object that raised the event", "on that entity or a parent") finds none left. The only hit is the unrelated `Source` definition at `properties.md:228`.

   **Is it too wide?** I checked the cases the new wording forbids and the ones it leaves allowed:
   - **Forbidden, rightly.** The write's task is registered on the object written to and every ancestor (`ValidateBase.AddChildTask`). The handler and the parent rule triggered by `t => t.Child.X` both run inside that task: the parent runs `ChildNeatooPropertyChanged`, then `RunRules("Child.X")`, then `RaiseNeatooPropertyChanged`, and all of it is awaited inside the child property's `OnValueNeatooPropertyChanged`. Every object the rule now names has the handler inside its running tasks.
   - **Allowed, rightly.** The rule names no sibling and nothing below the written object. A sibling's tracker and a descendant's tracker do not hold the write, so waiting on them is safe. `RunRules(propertyName)` on a sibling stays allowed, as it should.
   - **The `api.md` row.** It now reads from the waited object's side ("part of a write to this object or to anything below it, wherever that rule or handler lives"), which is the same rule.

   One narrow over-reach is listed under Theoretical. It is not worth a change.

### Behavior change 9 and the lazy-load tests

Behavior change 9 (`v0.37.0.md:181-186`) matches `reviews/008-lazy-failure-probe.log` and the code:
- **The wait in progress.** On `main` it threw the loader's exception (P1). On the arc it completes, because `ReportLoadInFlight` and `EndLoadReportIfNotLoading` report a `TaskCompletionSource` that only `TrySetResult` completes.
- **A wait started after the failure.** It completed on both branches (P2).
- **The parent.** It reads `IsValid == false` while the error stands (P1, both branches).
- **An awaited `LoadAsync()`.** It still throws (P3).

The test change matches the plan's Test Evidence table:
- `ParentIsValid_AfterExplicitChildLoadFailure` drops its `catch (InvalidOperationException)`. It now claims one outcome where it accepted two, so it is stricter, not weakened.
- `ParentWaitForTasks_InProgressWhenTheLoadFails_CompletesWithoutThrowing` drives the exact P1 case: the parent is busy, the wait is incomplete, the load fails, and the wait completes within a 5-second bound. On `main` its `await wait` would throw, so it pins the ruled behavior.

**Should the notes mention that `WaitForTasks(token)` on `main` never waited for a lazy load?** It is not needed for change 9, which is about throwing, and `WaitForTasks(token)` on `main` never threw a load's failure. The consumer-visible part is that `WaitForTasks(token)` and `Save(token)` now wait for a load in flight. "What's fixed" already covers that: "`WaitForTasks(token)` skipped property-level work that `WaitForTasks()` covered", and the "lazy load in flight" row of the tracking table says the same. Behavior change 7 names only "a rule still running". Adding "or a load" there would make it explicit. That is one option, not a gap.

### Veto-tier

None.

### Callouts

None.

### Could not verify

- `LazyLoadStatePropagationTests.cs` has an uncommitted comment-only edit in the working tree, which is not in 000f448. It adds three lines to `ParentIsValid_AfterExplicitChildLoadFailure` saying the loader throws synchronously, which is accurate. The test logs were written at 22:09, so they may predate it. Because it is a comment, the result cannot differ.
- Acceptance bullet 6, the copy of the skills to `~/.claude/skills/`, still stands as in Round 1. I did not re-check it.

### Theoretical (not triaged)

- **A narrow over-reach in the self-wait rule.** A `NeatooPropertyChanged` raised for a meta property (`ValidateBase.RaiseIfChanged`, `:337`) or for a list's `Count` (`ValidateListBase.cs:158`, `:190`, `:264`) is not awaited by whoever raises it. A handler on those events that awaits the wait would therefore not deadlock. The docs' "must not" covers them anyway. That errs on the safe side: the same handler's work is untracked, so it is not a pattern to invite.
- **A stale change number in the tracking files.** The plan's first amendment (`plans/008-docs-and-release-notes.md:120`, "behavior change 8") and the todo's Discovery Log ("migration note 8") give the lazy-load note as number 8. In the release notes it is 9; 8 is the list `IsBusy` row.

### Read report

- **Read beyond the brief:** `src/Neatoo/Rules/RuleBase.cs` (rule id assignment); the working-tree diff of `LazyLoadStatePropagationTests.cs`.
- **Named but unused:** none.
