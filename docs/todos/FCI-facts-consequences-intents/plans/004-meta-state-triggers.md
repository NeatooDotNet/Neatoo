# Meta-state triggers and the IsBusy analyzer

**Plan #:** 004
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4
**Status:** Done
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (decided by Keith in the design session, D29)
**Code-review opt-in:** Yes (framework behaviour every rule sees; a new error-severity analyzer)
**Branch:** fci-004-meta-state-triggers
**PR:** #118

---

## Scope

When `IsValid`, `IsSelfValid`, `IsBusy` or `IsModified` changes outside a pause, the object runs the rules triggered by that name, so `CanApprove = Status == Pending && IsValid` is an ordinary rule. The other meta properties the object already announces (`IsSelfModified`, `IsSavable`, `IsDeleted`) trigger the same way. An analyzer refuses an async rule whose trigger names `IsBusy`, the one loop that exists by construction. Integration tests pin that a meta trigger fires once per flip, is skipped while paused, and that a sync `IsBusy` rule converges. Does not add loop detection; termination stays the author's, tested.

## Intent

- Admission facts that read validity are rules, so the button binds a fact a rule maintains.
- The loop that cannot terminate is a compile error, not a hang.

## Framework & Architectural Alignment

- VisionAlignment D29. Meta changes are already detected and announced in one place per class; the trigger rides that point.
- Rules are paused inside factory operations; meta triggers obey the same pause.

## Constraints & Invariants

- A meta trigger runs only rules triggered by that name; an object with none pays one scan of its rules.
- A meta rule's task is tracked like a forked setter's, so `IsBusy` and `WaitForTasks` cover it, and a synchronous fault surfaces as a setter's does.
- Re-entrant changes are each handled once: the baseline is reset before the rules run.
- NEATOO011 does not ask a rule triggered on `IsBusy` to await its write, because NEATOO012 forbids that rule being async.
- Every existing test stays green.

## Steps

1. Record which meta properties changed where they are announced, reset the baseline, then run the rules each name triggers, unless paused.
2. NEATOO012 (error): an async rule registration, or an `AsyncRuleBase` trigger, that names `IsBusy`.
3. Exempt `IsBusy`-triggered rule lambdas from NEATOO011.
4. Integration tests for the triggers; analyzer tests for NEATOO012 and the exemption.

## Acceptance

- [x] A rule triggered on `IsValid` runs once when validity flips and maintains an admission fact `[integration]`
- [x] A rule triggered on `IsModified` runs when the entity first becomes modified `[integration]`
- [x] No meta rule runs while the object is paused `[integration]`
- [x] A sync rule triggered on `IsBusy` runs once per flip and the object settles `[integration]`
- [x] NEATOO012 reports `AddActionAsync` and `AsyncRuleBase` triggered on `IsBusy`, and not a sync one `[unit]`
- [x] NEATOO011 is silent on a sync `IsBusy`-triggered rule's write `[unit]`
- [x] Build and all tests green `[explicit-skip: meta-bullet]`

## Current State (Pre-Flight)

- `ValidateBase.CheckIfMetaPropertiesChanged` (`ValidateBase.cs:330-337`) raises `IsValid`, `IsSelfValid`, `IsBusy` through `RaiseIfChanged` (`:313-320`) and resets `MetaState`. `EntityBase`'s override (`EntityBase.cs:269-280`) raises `IsModified`, `IsSelfModified`, `IsSavable`, `IsDeleted` when not paused, then calls base. It is called after a child change's rules (`ValidateBase.cs:391`), after `RunRules(name)` (`:855`), and when the task sequence completes (`:212-216`).
- `ValidateBase.RunRules(string)` runs the rule manager, recalculates validity and re-checks meta state. `IRuleManager.Rules` exposes the rules; `TriggerProperty.IsMatch` compares the path string, so `t => t.IsValid` matches "IsValid". Trigger names are not validated at registration.
- `AsyncTasks.AddTask` throws a completed task's exception and ignores a completed task, as the generated setter relies on.

## Test Evidence

Flip checks: with the meta rules not run, the IsValid, IsModified and IsBusy tests failed; with the pause guard removed, the pause test failed; with the NEATOO011 IsBusy exemption removed, its test failed; before the stubs declared IsBusy, both NEATOO012 tests found nothing and failed. Restored, all pass.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| IsValid trigger runs once per flip, maintains admission | Required | `[integration]` | `Neatoo.UnitTest.Integration.Concepts.MetaStateTriggers.MetaStateTriggerTests.IsValidTrigger_RunsOncePerFlip_AndMaintainsTheAdmissionFact` | ✓ |
| IsModified trigger runs on first modification | Required | `[integration]` | `MetaStateTriggerTests.IsModifiedTrigger_RunsWhenTheEntityFirstBecomesModified` | ✓ |
| No meta rule while paused | Required | `[integration]` | `MetaStateTriggerTests.MetaTriggers_DoNotRunWhilePaused` | ✓ |
| Sync IsBusy rule runs once per flip, settles | Required | `[integration]` | `MetaStateTriggerTests.SyncIsBusyTrigger_RunsOncePerFlip_AndSettles`, `.AsyncMetaRuleStartedWhenTheObjectGoesIdle_IsCoveredByWaitForTasks` | ✓ |
| NEATOO012 reports async IsBusy rules, not sync ones | Required | `[unit]` | `Neatoo.Analyzers.Tests.AsyncRuleOnIsBusyAnalyzerTests.AddActionAsync_TriggeredOnIsBusy_IsAnError`, `.AsyncRuleBase_TriggeredOnIsBusy_IsAnError`, `.SyncRulesOnIsBusy_AndAsyncRulesOnOtherMetaState_AreAllowed` (IsSavable included; the stub's RuleBase derives from AsyncRuleBase as Neatoo's does) | ✓ |
| NEATOO011 silent on a sync IsBusy rule's write | Required | `[unit]` | `AsyncRuleOnIsBusyAnalyzerTests.Neatoo011_IsSilent_OnASyncIsBusyRulesWrite`, `.Neatoo011_IsSilent_OnARuleClassOrMethodGroupTriggeredOnIsBusy` | ✓ |
| Meta rule tasks tracked; sync fault surfaces; other meta properties trigger (Constraints) | Required | `[integration]` | `MetaStateTriggerTests.AsyncMetaRuleOnAChild_IsTracked_SoTheParentIsBusyUntilItFinishes`, `.SynchronouslyFaultingMetaRule_SurfacesFromTheWrite`, `.OtherEntityMetaProperties_TriggerRules` (flip-checked: tracking removed and rethrow removed each fail one) | ✓ |
| Work added by the sequence-complete callback keeps it open (Constraints) | Required | `[unit]` | `Neatoo.UnitTest.Unit.Core.AsyncTasksCompletionCallbackTests.TaskAddedByCompletionCallback_KeepsTheSequenceOpenUntilItFinishes` (fails against the old AsyncTasks) | ✓ |
| Build and all tests green | Required | `[explicit-skip]` | `reviews/004-build.log`, `004-design-build.log`, `004-test.log` | ✓ |

## Gate Record

- Round 1 (2026-10-16): APPROVED, 1 callout and 2 tech-debt fixed — `reviews/004-test-review.md`. Code review APPROVED, 3 Required callouts fixed — `reviews/004-code-review.md`.
- Round 2 (2026-10-16): APPROVED, 2 tech-debt fixed, no leftovers — Done

## Plan Amendments

### 2026-10-16 — AsyncTasks completion window; IsSavable loops too

- **Section affected:** Constraints, Steps 1-2
- **Original said:** a meta rule's task is tracked like a forked setter's; NEATOO012 refuses async rules on IsBusy.
- **What changed:** work added while `AsyncTasks` runs its sequence-complete callback escaped `IsBusy` and `WaitForTasks` (pre-existing; meta rules made it reachable); the finishing sequence now stays open for it. NEATOO012 also refuses async rules on `IsSavable`, which carries `!IsBusy`: a test written that way hung the suite. Keith approved the AsyncTasks fix on a green suite (2026-10-16).
- **Why:** code review callout 2; the hang.
- **Discovery Log:** 2026-10-16 FCI-004
