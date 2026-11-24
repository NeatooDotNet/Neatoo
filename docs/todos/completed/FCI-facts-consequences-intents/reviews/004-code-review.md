# FCI-004 code review

**2026-10-16: APPROVED.** No veto. The trigger sits at the single place each meta change is announced; the baseline is reset before rules run, so a nested change is handled once; paused changes are dropped and resume resets the baseline. Consumers with no meta-triggered rule pay one list allocation and one rule scan per changed name, and meet no new exception path.

Required callouts, all taken:

1. **High, AC-4, fixed.** NEATOO012 errored on a sync `RuleBase` triggered on `IsBusy`, because Neatoo's `RuleBase` derives from `AsyncRuleBase`; the test stub declared them unrelated, which hid it. The walk now stops at `RuleBase`, and the stub derives as Neatoo's does.
2. **High, AC-4, fixed.** Work started from `AsyncTasks.OnFullSequenceComplete` (a meta rule triggered by the object going idle) escaped `IsBusy` and `WaitForTasks`, and its late `SetResult` threw unobserved. Pre-existing; meta rules made it reachable. The finishing sequence now stays open for that work (Keith approved on a green suite). Pinned by `AsyncTasksCompletionCallbackTests`. Writing the integration test for it exposed that an async rule on `IsSavable` loops by construction (`IsSavable` carries `!IsBusy`); NEATOO012 now refuses it.
3. **Medium, AC-4, fixed.** The NEATOO011 exemption covered lambdas only; a `RuleBase` class and a method group registered with an `IsBusy` trigger are now exempt too.

Related note, parked as a proposal: a validation triggered only on a meta name has its messages dropped (they target the trigger property, which does not exist).

Theoretical, not triaged: a re-entrant check during `RaiseIfChanged`'s raise runs a meta rule twice (the duplicate notification predates this plan); names after a synchronous fault do not run; `changedMetaProperties` thread safety when an unawaited write overlaps the pool-thread completion (shared with `MetaState`); NEATOO012 misses `new TriggerProperty<T>(t => t.IsBusy)`, primary-constructor and `: this(...)` forms; a nested `x => x.IsBusy` in a rule body exempts it from NEATOO011.
