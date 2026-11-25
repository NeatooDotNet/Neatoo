## Close-Out Audit — 2026-11-24

**Grade: C**

**What the grade rests on.** Every Required criterion, AC-1 to AC-9, holds at the arc's head (fae71f0), and so do the two built Optional criteria, AC-10 and AC-11. The one unbuilt criterion, AC-12, is Optional and parked as #103. On those facts alone the grade would be B, because AC-12 is not built. The grade is C for one container reason. Four test-reviewer tech-debt lines have no recorded disposition anywhere: not in a plan, not in the todo, not on an issue, not in a PR body. Each exists only in its review file. SKILL Step 5 says "A plan reaches Done when the gate closes and every finding has a recorded disposition — in the plan, the todo, or an issue", and the test-reviewer says the orchestrator dispositions tech-debt. A finding whose only disposition is the session is the C clause in `close-out-audit.md`. The four lines are named under Callouts, first entry. No veto-tier finding is open. The build and every suite are green.

### Acceptance Criteria Trace

| AC | Word | Evidence (file:method:line) | Holds? | If not: parked as? |
|---|---|---|---|---|
| AC-1 | Required | Every write goes through one report point: `ValidateProperty.SetPrivateValue` → `ReportToOwner` (`src/Neatoo/Internal/ValidateProperty.cs:261`, `:277-283`). The owner's end is wired in the `ValidateBase` ctor (`src/Neatoo/ValidateBase.cs:213-216`) to `AddChildTask` (`:557-574`), which registers on the object first and then passes the task up. The other entry points: `RunRules(propertyName)` registers itself (`:914-925`); `RunRules(RunRulesFlag)` registers its rules outside its own wait (`:978-986`); a lazy load is reported by `LazyLoadValidateProperty.ReportLoadInFlight` (`:117-143`, `:331-337`) and the entity twin (`LazyLoadEntityProperty.cs:35-61`); an object that is busy when it is attached is taken up by `SetParent` (`ValidateBase.cs:273-287`). Generated code registers nothing (`PropertyGenerator.cs`). Tests: `FrameworkRegistersTests` (9 methods), `WaitsAwaitTasksTests.RunRulesWithAFlag_NotAwaited_IsInTheObjectsRunningTasks`, `AnItemAlreadyBusyWhenItIsAddedToAList_HasItsWorkTakenUpByTheListsOwner`, `AwaitableSetterGenerationTests.GeneratedSetterAndSetName_RegisterNothing_TheOwnerRegisters` | Yes | — |
| AC-2 | Required | `ValidateBase.WaitForTasks()` is `await RunningTasks.AllDone` (`ValidateBase.cs:662-665`). The property-manager scan is gone. A grep of `src/Neatoo` and `src/Neatoo.Blazor.MudNeatoo` for `while (…IsBusy`, `Task.Delay` and `Task.Yield` finds nothing. The list's `WaitForItems` (`ValidateListBase.cs:479-494`) uses `IsBusy` only to pick which items' waits to await, and then awaits their tasks. Test: `OnOneThread_WaitForTasks_ReturnsAnIncompleteTask_AndCompletesWhenTheWorkDoes` | Yes | — |
| AC-3 | Required | Same seam as AC-2, on a thread with a synchronization context (`TestInfrastructure/OneThreadAtATime.cs`). Tests: `WaitsAwaitTasksTests.OnOneThread_WaitForTasks_…`, which covers four entry points; `PropertyTasksTests.WaitForTasks_ReturnsControlWhileARuleStartedByRunRulesIsInFlight_…` and `…WhileAWriteThroughThePropertyIsInFlight_…`, which hit the 5 s limit in `003-red.log`; three MudNeatoo tests that hang the host on `main` (`007-red-main.log`, 3 aborted runs) | Yes | — |
| AC-4 | Required | `IsBusy => RunningTasks.IsRunning` (`ValidateBase.cs:172`). `AddChildTask` accounts self-first (`:557-566`). A property's write and rule run await the property's own accounting before they complete (`ValidateProperty.cs:420-438`, `RuleManager.cs:707-740`). List busy is read from the items (`ValidateListBase.cs:77`). Tests: `WhenTheRootsWaitCompletes_NothingOnItOrBelowItIsBusy` (root, child, list, item and their properties), `FrameworkRegistersTests.SaveWithAToken_OnARoot_SucceedsAfterAWriteToAChildWithAnAsyncRule` (red with `SaveOperationException` in `004-red.log`), `ABusyItemAddedToAList_WhoseRuleThenThrows_IsReportedByTheOwnersWaitOnce` | Yes | — |
| AC-5 | Required | `WaitForTasks(token)` is `RunningTasks.WaitForCompletion(token)` (`ValidateBase.cs:681-684`). That is `AllDone` plus a cancellable wait that gives back its observer slot (`AsyncTasks.cs:154-195`). Tests: `TheTwoWaits_WaitForTheSameWork`, `ATokenWait_Cancelled_Throws_AndTheWorkStillFinishes`, `AListsTokenWait_WaitsForItsBusyItem_AndCanBeCancelled`, `AsyncTasksFaultRecoveryTests.CancelledWait_DoesNotTakeTheFault_TheNextWaitReportsIt`. The legacy todo was moved to `docs/todos/completed/` with its closing line | Yes | — |
| AC-6 | Required | Property busy is the child's busy plus the property's own tracker (`ValidateProperty.cs:76`, `:90`). `TrackTask` (`:164-188`) adds a completion-only task and announces the rising edge. `AnnounceIdle` runs from the tracker's callback (`:108-147`). The rule manager hands each trigger property the rule run (`RuleManager.cs:707-720`). Tests: `PropertyTasksTests` (15 methods, among them overlapping writes, a throwing rule leaving nothing busy, a deserialized object, and a property created on first use) | Yes | — |
| AC-7 | Required | The tracker starts a clean sequence after a closed one and carries an unobserved fault forward (`AsyncTasks.cs:62-75`). It hands a fault once (`:115-142`, `:266-290`). It reports any exception from the callback (`:241-253`). A thrown rule's message carries its rule id, so a clean run clears it (`RuleManager.cs:776`, `:806`). Tests: `AsyncTasksFaultRecoveryTests` (6), `AsyncRuleFaultTests` (6), `FrameworkRegistersTests.ARuleThatThrowsAfterASetterWrite_SurfacesOnce_CarryingTheExceptionOnce`, `InputWriteIsWaitedForTests.AWriteThroughAnInput_WhoseRuleThrows_…` | Yes | — |
| AC-8 | Required | No MudNeatoo source changed. The inputs' `EntityProperty.SetValue` reaches `ReportToOwner` (above). Tests: `Neatoo.Blazor.MudNeatoo.Tests.InputWriteIsWaitedForTests`, 5 methods (entity, child-to-root, `Setter`, disabled-while-busy, throwing rule). "Not before" is asserted (`Assert.IsFalse(wait.IsCompleted, …)`) | Yes | — |
| AC-9 | Required | `docs/guides/async.md:131-160` and `:392-408` (handler rule); `docs/guides/blazor.md:517`; `docs/guides/collections.md:439`; `docs/reference/api.md:256`; `skills/neatoo/references/{properties,pitfalls,lazy-loading}.md`; `skills/mudneatoo/references/property-change-events.md:51`; `src/Design/CLAUDE-DESIGN.md:350-391`; Design.Domain comments (`LazyLoadProperty.cs`, `StateProperties.cs`, `ErrorPatterns.cs`); `docs/release-notes/v0.37.0.md` (6 compile breaks, 9 behavior changes, every Punchlist migration row). I ran my own search without word boundaries for `poll`, cached busy, `Interlocked`, execution id, marked busy and `AddChildTask` over the guides, the reference, the skills, `CLAUDE-DESIGN.md` and Design.Domain. Its only hits are the correct `protected` row in `api.md:256` and git-ignored `Generated/` files that the build does not compile | Yes | — |
| AC-10 | Optional | `NeatooPropertyChangedInvocation.InvokeAll` (`src/Neatoo/Internal/NeatooPropertyChangedInvocation.cs:17-39`) at every raise whose task is used: `ValidateProperty.cs:412`, `:422`; `ValidatePropertyManager.cs:139`; `ValidateBase.cs:445`; `ValidateListBase.cs:331`. Tests: `EverySubscriberIsAwaitedTests`, 4 methods for AC-10 (red in `006-red.log`) plus the null-subscriber case (`006-red-null.log`) | Yes | — |
| AC-11 | Optional | The object's idle check runs in `OnFullSequenceComplete` (`ValidateBase.cs:219-223`). The tracker awaits that callback before it completes `AllDone` (`AsyncTasks.cs:241-290`), on the thread that added the task (`:102-106`). Test: `EverySubscriberIsAwaitedTests.TheIdleAnnouncement_IsRaisedBeforeTheWaitCompletes` | Yes | — |
| AC-12 | Optional | Not built. The reason is in `plans/006-optional-criteria.md` Plan Amendments: `MarkUnmodified` refuses while busy, and that needs a ruling. The Discovery Log entry is WAIT-006 · Queue | No | #103 |

### Veto-Tier Findings

None.

### Callouts (Required in full; ≤ 5 Optional)

**Required-affecting**

1. **Container: four tech-debt findings with no recorded disposition.** Confidence High. I grepped every plan, the todo, and the bodies of PRs #146, #147 and #150, and none of the four appears. *This is the item the grade rests on.*
   - `reviews/004-test-review.md:18`: `SaveWithAToken_OnARoot_SucceedsAfterAWriteToAChildWithAnAsyncRule` assumes MSTest runs with no synchronization context and does not assert it. `Affects: AC-4`. The 004 Gate Record names its two callouts and its open question, but not this line.
   - `reviews/004-test-review.md:19`: `GeneratedSetterAndSetName_RegisterNothing_TheOwnerRegisters` never asserts that `SetName` or `SetTotal` was generated. `Affects: AC-1`. Not in the 004 Gate Record.
   - `reviews/005-test-review.md:21`, carried into round 2 at `:89`: the commented-out `//CollectionAssert.Contains(propertyChangedCalls, "IsBusy")` at `src/Neatoo.UnitTest/AsyncFlowTests/AsyncFlowTests.cs:77`. It predates the arc. The SKILL's triage step 3 for a finding the plan did not cause is Park or Dismiss. `Affects: AC-4`. Neither 005 Gate Record round mentions it.
   - `reviews/008-test-review.md:20`: probe case P3 (an awaited `LoadAsync()` throws to its awaiter) is named by no test. Release-notes behavior change 9 states it. `Affects: AC-9`. The 008 Gate Record takes the neighbouring line (`:19`) and not this one.
   - `Reachable by:` Step 8 moves the folder to `completed/`. After that, these lines live only in review files that nobody triages again.
2. **Container: WAIT-002's Acceptance bullets are unticked on a Done plan.** `plans/002-tracker-faults.md:60-67` has eight `- [ ]` bullets. Every other plan ticks its bullets, and 002's own Test Evidence table pins each of them. Confidence High. `Affects: AC-7`. `Reachable by:` a reader of the plan, including Keith at the end review, sees a Done plan with nothing checked.
3. **Container: a dismissal recorded in a plan, not in the todo's Dismissed section.** `plans/003-property-tasks.md:134` says the third round-2 tidy-up line is "dismissed" (`reviews/003-test-review.md:76`, the public-concrete `LazilyCreatedPropertyObject` fixture). The SKILL's Dismiss is "one line in the todo's Dismissed section", and under the Standing orders that section and Recommendations Taken are what Keith reviews. The todo carries neither. Confidence High. `Affects: AC-6`. `Reachable by:` Keith's end review, which reads the todo.
4. **Design alignment: Keith's two in-arc rulings are not in VisionAlignment's ruling tables.** You asked where a later reader would find them. Both are recorded durably elsewhere:
   - The thread ruling: `src/Design/CLAUDE-DESIGN.md:350-352`, quoted with its date; "Not in this release: Threads" in `v0.37.0.md`; the Dismissed row; WAIT-003's test-change table.
   - The lazy-load ruling: behavior change 9, the rewritten `skills/neatoo/references/lazy-loading.md` passage, the `LazyLoadProperty.cs` comment, the test comment in `LazyLoadStatePropagationTests.cs`, the Dismissed row, the Discovery Log, and WAIT-008's Plan Amendments.

   Neither appears in `docs/VisionAlignment.md` §0. WAIT-001 wrote the convention down ("new rulings go in a dated table with what they supersede", `plans/001-decision-record.md:32`) and made that table "one place that says what Keith decided" (`:26`). The lazy-load ruling supersedes a documented behavior, and the todo's own Out of Scope sentence promises LazyLoad's "documented waiting keeps working". Confidence Medium: the convention is WAIT-001's, not a CLAUDE.md rule. `Affects: AC-9`. `Reachable by:` a later plan or reviewer who cites VisionAlignment as the ruled basis, as WAIT-002 to WAIT-008 each did.

**Optional-affecting**

5. **Container: #103 does not carry the finding it was parked with.** The Parked row says AC-12 needs a ruling because `MarkUnmodified` refuses while busy (`plans/006-optional-criteria.md` Plan Amendments). `gh issue view 103` (body and its one comment) has no `MarkUnmodified` and no `Origin:` line. The issue predates the arc, so an `Origin:` line is not expected, but its WAIT reference points to `docs/todos/WAIT-tasks-are-the-truth/`, and Step 8 moves that folder. Confidence High. `Affects: AC-12`. `Reachable by:` whoever picks up #103 starts from the issue, and that link breaks when the folder moves.

### Could not verify

- Behavior change 8's figures (13 µs for 1,000 items, 176 µs for 20,000) come from a throwaway Release benchmark that is not committed. The WAIT-005 code review could not verify them (round 2), and neither can I.
- The repeat-run claims have no logs: WAIT-003's `AsyncFlowTests_Property_PropertyChangedEvents` "0 in 30" and the unit suite's "0 failures in 30" after the round 1 fixes, WAIT-006's "0 failures in 25 runs", and WAIT-007's "ran 20 more times with 0 failures". `reviews/` holds no log for any of them.
- `final-test.log` and `final-design-test.log` are summary verbosity and name no passing test. I confirmed that every cited test method exists by grepping the source (77 cited names; the only miss is `IsSelfBusy_InitialState_ReturnsFalse`, which WAIT-003 removed and reported). I did not confirm from the final logs that each one ran.
- Container item 12 (every decision that touched a sentence of Keith's is listed in the arc PR body) cannot be checked until Step 8 opens that PR. The two candidates are the Dismissed rows on threads and on the lazy load.

### Theoretical (not triaged)

- Five raise sites discard `InvokeAll`'s task, as they did on `main`: `ValidateBase.RaiseIfChanged` (`ValidateBase.cs:337`, meta properties) and `ValidateListBase.cs:158`, `:190`, `:264` (`Count`) and `:405` (`RaiseIfChanged`). A consumer handler's async work on those notifications is in nobody's running tasks. The framework's own reaction, `RunRules(propertyName)`, registers itself. No live caller in the repo does async work there (`NeatooValidationSummary`, `WorkOrderTaskList` checked). Listed twice before (006 and 008 code reviews, Theoretical). It is beside `async.md:5`, "Every async operation is tracked".

### Build & Test

- Build: PASSED — `reviews/final-build.log` (0 errors, 97 warnings; the same 97 as `002-build.log`: CS4014 in `AsyncTasksTests` and `AsyncTasksFaultRecoveryTests`, CS8622, CS8629, MSTEST0032); `reviews/final-design-build.log` (0 errors, 0 warnings).
- Tests: 2,345 passed, 0 failed — `reviews/final-test.log`: 2,110 passed (Neatoo.UnitTest 1,943, Person.DomainModel.Tests 55, BaseGenerator.Tests 51, MudNeatoo.Tests 38, Analyzers.Tests 23), 2 skipped (`AsyncFlowTests_CheckAllRules` and `FatClientValidate_Deserialize_SharedDictionaryReference`, both `[Ignore]` on `origin/main`); `reviews/final-design-test.log`: Design.Tests 235. `Neatoo.UnitTest.Demo` reports "No test is available", as in every per-plan log since 002.
- Sacred tests: none weakened. Every existing test the arc changed is reported with its reason in a plan:
  - `ValidatePropertyTests.IsSelfBusy_InitialState_ReturnsFalse` is removed with its member (WAIT-003).
  - The two tracker-from-DI tests changed (WAIT-003).
  - `AsyncFlowTests` (six methods) now run on one thread with no assertion changed, under Keith's ruling (WAIT-003).
  - `AwaitableSetterGenerationTests.PublicSet_…` (WAIT-004).
  - `ParentIsValid_AfterExplicitChildLoadFailure` is stricter than before, under Keith's ruling (WAIT-008).
  - Two comments changed in `ValidateListBaseTests` (WAIT-008 Plan Amendments).

### Container

- Plans issued / cap: 8 / 12. Numbering 001–008 is monotonic, and the Plan Index matches `plans/`. None was Abandoned or Retired.
- Status cells over budget: 0. All eight cells read "Done".
- Every plan names its `AC-n` in Serves, and so does every Discovery Log entry.
- Gate Records: every gated plan has at most two rounds. WAIT-001's skipped gate is in Skipped Steps, and WAIT-007 opted out of code review because it is tests only.
- No DECISION NEEDED verdict was returned. Both stops (threads, lazy load) became rulings recorded under Dismissed.
- Test Evidence: no `MISSING` row. Every behavioral bullet on every Done plan has a row with an existing test. Red-run counts match the logs: 003-red 6/8, 004-red 7/9, 005-red 1/7, 006-red 3/4, 002-cancel-red 1/6, 007-red-main 3 aborted runs.
- Untraced deferrals: None. The deferral phrases in the plan bodies trace to the Punchlist (all seven rows `[x]`, with #146, #147 or #150), to the AC-12 Parked row, or to the Dismissed rows.
- Prose over budget: None. The Goal is 113 words. The longest Discovery Log entry is WAIT-008's, at 60 words.
- Open PRs into the arc: None (`gh pr list --state open --base wait-arc` is empty). #143 to #150 are all MERGED into `wait-arc`.
- Required parked without demotion: None.
- Dispositions living only in a PR body: None. Four findings have no disposition at all (Callout 1).
- Out of Scope was respected:
  - #131 was not touched; the flake it caused is dismissed under Keith's ruling.
  - #133, #96, #89 and the other MudNeatoo issues were not touched.
  - LazyLoad was not extended. Its documented waiting changed in one case, and Keith ruled on that.
- Version: `Directory.Build.props:16` is `0.37.0`. The release notes link to `../todos/completed/WAIT-tasks-are-the-truth/todo.md`, which becomes valid once Step 8 moves the folder.
- The two `proposed` Parked rows are the designed state under the Standing orders ("a parked finding is a `proposed` row, never an issue, until Keith approves it"). They are not criteria gaps and do not bear on the grade. Step 8 cannot pass while they remain `proposed`, so Keith rules on them first. I do not see it differently.

### Parked (as the todo lists them)

- proposed · `ValidateProperty.LoadValue` called twice with the same instance subscribes twice
- proposed · a list item whose busy changes while the item is paused does not raise the list's `IsBusy` notification
- #103 · AC-12 (Optional) not built: a save in flight is not made busy

### Read report

- Read beyond the brief:
  - Source: `src/Neatoo/Internal/AsyncTasks.cs`, `IAsyncTasks.cs`, `NeatooPropertyChangedInvocation.cs` and `TaskFinishedExtensions.cs` in full. `ValidateBase.cs`: ctor, `IsBusy`, `SetParent`, `RaiseIfChanged`, `RunMetaPropertyRules`, `ChildNeatooPropertyChanged`, `AddChildTask`, both waits, both `RunRules`. `ValidateProperty.cs` lines 60-300 and 400-440. `ValidatePropertyManager.cs` attach and report path. `ValidateListBase.cs` busy, raise sites, waits and `RunRules`. The `RuleManager.cs` diff. The `LazyLoadValidateProperty.cs` diff. The `AddNeatooServices.cs`, `IFactory.cs` and `IValidateBaseServices.cs` diffs. `WorkOrderTaskList.HandleNeatooPropertyChanged`. `NeatooValidationSummary.razor.cs`.
  - Tests: `WaitsAwaitTasksTests.cs` lines 1-140, `EverySubscriberIsAwaitedTests.cs`, the first 90 lines of `InputWriteIsWaitedForTests.cs`, the diffs of every existing test file the arc touched, and the `[Ignore]` state of both skipped tests on `origin/main`.
  - Process and issues: `SKILL.md` Steps 4-8, `~/.claude/agents/test-reviewer.md` on tech-debt, `gh issue view` 103, 141 and 129, `gh pr view` 143-150, the PR bodies of 146, 147 and 150, the `.gitignore` rule for `Generated/`, `docs/todos/_ids.md`, and `008-lazy-failure-probe.log`.
- Named but unused:
  - `reviews/000-analysis.md`: no criterion's trace turned on it.
  - The per-plan design logs, beyond the counts in the plans.
  - Most of the release notes' "What's fixed" prose: the WAIT-008 code review read it against the code in two rounds, and no finding of mine turned on it.
