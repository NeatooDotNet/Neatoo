# Tasks Are the Truth: One Way to Track, Wait and Read Busy

**ID:** WAIT
**Type:** Bug-Exposes-Fallacy
**Status:** In Progress
**Created:** 2026-11-24
**Last Updated:** 2026-11-24
**Initial split:** 8 plans
**Plan cap:** 12 — counts plan numbers issued, including Abandoned and Retired. Issuing a number above the cap is a stop-and-ask.
**Arc branch:** wait-arc — cut from main at Step 1; every plan and punchlist branch PRs into it; it PRs into main at Step 8.
**Related:** Closes #141. Adopts #129 under AC-11, #103 under AC-12, and the legacy todo `docs/todos/waitfortasks-cancellation-missing-propertymanager.md` under AC-5. Follows 0.36.1, which fixed #140. No other arc is open; LIST is not started and touches EntityListBase's state machinery, not its waiting. Decided by Keith, 2026-11-24: the redesign, in place of a point fix on the property, which he rejected as a patch; an async rule that throws surfaces from `WaitForTasks()` once and the object then recovers; the redesign is free to remove public members that are documented as framework use; the twelve criteria and their words; the whole redesign is done here, with no stopgap release. Also decided by Keith, 2026-11-24, after reading the old and new architecture side by side: the architecture "looks good"; "Code in the 'Internal' namespace is meant to be framework only, use at your own risk"; and "please use the existing DI heavy approach of Neatoo. It's extra work, not obviously useful, but it lets consumers expand the framework in major ways." The rulings are D31 to D36 in `docs/VisionAlignment.md`.
**Standing orders:** Unattended, like FCI (Keith, 2026-11-24). Claude merges every plan and punchlist PR into the arc and takes every reviewer recommendation that does not contradict a decision above, listing each under Recommendations Taken for Keith's end review. Keith reviews and merges the arc PR into main and owns the release tag. Claude stops for Keith, never decides: a DECISION NEEDED verdict, an out-of-scope test failure, a test-versus-code disagreement no documented intent covers, a red full suite after one fix attempt, a priority change, a Goal or Out of Scope edit, a plan number beyond the cap, and anything that touches a decision above. A parked finding is a `proposed` row, never an issue, until Keith approves it. A test change the documented intent justifies is made and reported in the PR.

---

## Goal

Neatoo tracks async work twice: as tasks in each entity's `RunningTasks`, and as busy flags on each property. `WaitForTasks()` bridges the two by polling the flags, and a flag cannot be awaited, so on one thread it freezes the page (#141). Work also reaches `RunningTasks` only when the code that started it registers it, which a MudNeatoo input and `RunRules(propertyName)` do not. This todo makes tasks the only source of truth. The framework registers every reaction it starts, `WaitForTasks()` awaits those tasks and nothing else, and busy state is read from the same tasks. The documented promise holds again: every async operation is tracked, so `IsBusy` and `WaitForTasks()` describe the whole object graph.

## Acceptance Criteria

- [ ] **AC-1** · Required — Every async reaction an entity starts is registered in its `RunningTasks` by the framework, whoever started it.
- [ ] **AC-2** · Required — `WaitForTasks()` awaits registered tasks only and polls no flag.
- [ ] **AC-3** · Required — On a single thread, `WaitForTasks()` returns an incomplete task while work is in flight and completes when that work does.
- [ ] **AC-4** · Required — When `WaitForTasks()` completes, `IsBusy` is false on the object and its descendants.
- [ ] **AC-5** · Required — `WaitForTasks(token)` waits for exactly what `WaitForTasks()` waits for.
- [ ] **AC-6** · Required — A property is busy exactly while a write or an async rule that involves it is in flight.
- [x] **AC-7** · Required — An async rule that throws surfaces from `WaitForTasks()` once, and the object then tracks and waits for later work.
- [ ] **AC-8** · Required — A write through a MudNeatoo input followed by `WaitForTasks()` on the entity completes when the rule does.
- [ ] **AC-9** · Required — The guides, the skills and Design.Domain describe tracking, waiting, busy and faults as the code behaves.
- [ ] **AC-10** · Optional — Raising `NeatooPropertyChanged` awaits every subscriber's task.
- [ ] **AC-11** · Optional — The falling `IsBusy` notification is raised before `WaitForTasks()` completes.
- [ ] **AC-12** · Optional — A save in flight makes the entity busy.

## Out of Scope

- The meta-state compare-and-reset race under concurrent threads (#131), and thread safety beyond what exists today.
- A default hang timeout for test runs (#133). Tests written here that can hang carry their own timeout.
- `RunRules(RunRulesFlag.All)` not clearing `ObjectInvalid` (#96), and whether `RunRules(Self)` drains its children (#89).
- Removing LazyLoad (D24). Its documented waiting keeps working; nothing new is built on it.
- The other MudNeatoo issues (#128, #136, #130, #124, #113, #93, #95) and the `IMetaStateNotifier` refactor.
- A stopgap release for #141.

---

## Plan Index

| # | File | Title (≤ 8 words) | Serves | Status | PR |
|---|------|-------|--------|--------|----|
| 001 | [001-decision-record](./plans/001-decision-record.md) | The rulings in VisionAlignment | AC-9 | Done | #143 |
| 002 | [002-tracker-faults](./plans/002-tracker-faults.md) | The tracker faults once, then recovers | AC-7 | Done | #144 |
| 003 | [003-property-tasks](./plans/003-property-tasks.md) | A property holds its work as tasks | AC-6, AC-3 | Done | #145 |
| 004 | [004-framework-registers](./plans/004-framework-registers.md) | The framework registers every reaction | AC-1 | Done | #146 |
| 005 | [005-waits-await-tasks](./plans/005-waits-await-tasks.md) | Waits await tasks; busy reads tasks | AC-2, AC-3, AC-4, AC-5 | Done | #147 |
| 006 | [006-optional-criteria](./plans/006-optional-criteria.md) | Subscribers, notification order (save in flight parked as #103) | AC-10, AC-11 | In Progress | — |
| 007 | [007-mudneatoo-and-design-app](./plans/007-mudneatoo-and-design-app.md) | MudNeatoo and Design.App on the new tracking | AC-8 | Draft | — |
| 008 | [008-docs-and-release-notes](./plans/008-docs-and-release-notes.md) | Guides, skills and release notes | AC-9 | Draft | — |

---

## Punchlist

- [ ] The migration notes name three compile breaks from WAIT-002: forward `IValidateBaseServices.CreateAsyncTasks` in a services wrapper; `AsyncTasks.AddTask(Task, bool)` is now `AddTask(Task)`; `RunningTasks` is typed `IAsyncTasks` · the release notes (WAIT-008) · done when the migration section lists all three · AC-9
- [ ] The migration notes name the breaks from WAIT-003: `IValidateProperty.AddMarkedBusy` and `RemoveMarkedBusy` are removed, with `IsSelfBusy` and `IsMarkedBusy` on `ValidateProperty`; `IFactory` gains `CreateAsyncTasks()`; `DefaultFactory` is registered transient · the release notes (WAIT-008) · done when the migration section lists them · AC-9
- [x] (#146) A write to a child registers with the parent chain before the child's own tracker, so with no synchronization context the root's waits complete before the child has accounted, and `Save(token)` on the root then throws `SaveOperationException` (IsBusy). Since WAIT-003 this happens every time, where it was a race before (traced by the code reviewer, not run) · the generated setter and `Set{Name}`, and `ValidateBase.AddChildTask`, `Setter`, `ObjectInvalid` and the meta-rule registration · done when an object accounts before its parent and a test saves a root with a token after a child write with an async rule · AC-4 · for WAIT-004, which rebuilds registration
- [ ] The migration notes name the breaks from WAIT-004: `AddChildTask` is off `IValidateBase` and `protected` on `ValidateBase`; generated setters and `Set{Name}` no longer register, so consumers rebuild; an awaited `RunRules(propertyName)` that throws is now also reported by the next `WaitForTasks()`, as an awaited `Set{Name}` already was · the release notes (WAIT-008) · done when the migration section lists them · AC-9
- [x] (#147) `RunRules(RunRulesFlag)` called and not awaited: its rule runs are busy on their trigger properties but not in the object's running tasks, so a wait on running tasks alone would not wait for them. It cannot register itself, because it awaits the running tasks inside · `ValidateBase.RunRules(RunRulesFlag)` and `RuleManager.RunRules(RunRulesFlag)` · done when the wait in WAIT-005 covers it, with a test, or it is ruled awaited-only · AC-1 · for WAIT-005
- [ ] The migration notes name the changes from WAIT-005: an object's `IsBusy` and `WaitForTasks()` are its running tasks alone, so a custom property that reports busy without tracking a task no longer makes its object busy; `ValidatePropertyManager.IsBusy` has no setter; an awaited `RunRules()` that throws is also reported by the next `WaitForTasks()` · the release notes (WAIT-008) · done when the migration section lists them · AC-9
- [ ] The guides say what a `NeatooPropertyChanged` handler must not do: await `WaitForTasks()`, `RunRules()` or `Save(token)` on its own object or a parent. Every handler's task is now part of the write, so such a handler waits for itself, whatever order it subscribed in (`docs/guides/blazor.md` near line 102 reads as allowing it) · the async and Blazor guides, and the migration notes · done when both say so · AC-9 · for WAIT-008 (from the WAIT-006 code review)

## Dismissed

- `AsyncFlowTests_Property_PropertyChangedEvents` failing intermittently, proposed as a fix to `CheckIfMetaPropertiesChanged` · it is #131, already out of scope, and Keith ruled on 2026-11-24: "Neatoo is not multi-threaded capable. Like a UI application. Neatoo is not threadsafe." The test ran one object on several pool threads at once; it now runs one thing at a time (WAIT-003)

## Parked

- proposed · `ValidateProperty.LoadValue` called twice with the same instance subscribes twice · 0.36.1 design critique, 2026-11-24
- proposed · a list item whose busy changes while the item itself is paused does not raise the list's `IsBusy` notification; the list's `IsBusy` value is right and its owner is unaffected. Reached by an awaited `RunRules` with an async rule inside a factory operation on an item already in a list · WAIT-005 · `reviews/005-code-review.md` Round 2, Theoretical
- #103 · AC-12 (Optional) not built: registering the save as the object's work makes `MarkUnmodified` refuse when a local save with an async `[Insert]` or `[Update]` completes, so it needs a ruling on that guard and on the sent instance's state after a remote save · WAIT-006 · `plans/006-optional-criteria.md` Plan Amendments

---

## Recommendations Taken

[Every reviewer recommendation the loop took, for Keith's end review. One line each: plan, reviewer, what was recommended, what was done.]

- WAIT-002 · code-reviewer · a wait cancelled by its token could swallow a rule's fault, so it surfaced zero times · fixed in the tracker, with a test seen red first.
- WAIT-002 · code-reviewer · three compile breaks for consumers were recorded nowhere for the migration notes · punched to the Punchlist for WAIT-008.
- WAIT-002 · test-reviewer · a test named for an entity tested a ValidateBase object, and two tests could pass through a different path in a narrow window · renamed; the two tests now wait for the sequence to close.
- WAIT-003 · test-reviewer · no test made a property created on first use go busy · test added, seen red with the attach disabled on that path.
- WAIT-003 · code-reviewer (veto) · an exception thrown while a property went idle landed in the property's tracker, not in the awaited write or rule run · the property hands it to the operation that ended the work; two tests seen red first.
- WAIT-003 · code-reviewer · the manager's wait took and dropped the fault an idle child held · only busy properties are asked for their wait, and a faulted wait is awaited; test seen red first.
- WAIT-003 · code-reviewer (listed as theoretical) · this plan had moved the property's idle notification onto a pool-thread continuation · the tracker now accounts for a task on the thread that added it, for objects and properties alike, which also settles the row parked earlier the same day; two tests seen red first.
- WAIT-003 · test-reviewer (round 2) · no test pinned that a rule's own exception is reported over a listener's · two tests added, seen red with that branch weakened; the unnamed deadline exit and the loose exception type it listed were tightened too.
- WAIT-003 · code-reviewer (round 2) · with the tracker accounting inline, a root accounts before its child and `Save(token)` on the root throws while a child write is in flight · punched to the Punchlist for WAIT-004, which rebuilds registration; the arc is not released between plans.
- WAIT-004 · test-reviewer · the protected generated setter and the ValidateBase lazy-load report had no test, and the "once" tests had never been seen red · the generator test covers the protected setter, the lazy-load report has its own test, and the "once" tests were run red with de-duplication disabled.
- WAIT-004 · code-reviewer · a lazy value replaced while it loaded left its owner busy for good, and a lazy value already loading when assigned was never reported · the report now follows the value the property holds; two tests seen red first.
- WAIT-004 · code-reviewer (could not verify) · an un-awaited `RunRules(RunRulesFlag)` is busy on its properties but not in the object's running tasks · punched to the Punchlist for WAIT-005.
- WAIT-005 · test-reviewer (veto) · a bullet claimed "their properties" and the test checked two of four · two assertions added; its three callouts taken as tests, and the rising check in `AddChildTask` seen red.
- WAIT-005 · code-reviewer · a `ValidateListBase` takes an item that is already busy and its owner never heard of the work · a parent now takes up the work of an object that is busy when attached; test seen red first. This corrects WAIT-004's "a list refuses a busy item".
- WAIT-005 · code-reviewer · list `IsBusy` read every item on every notification · measured (an edit cost up to 137 times more at 20,000 items), then fixed: the value is still read from the items, but the notification check reads them only when busy may have changed.
- WAIT-005 · test-reviewer (round 2) · three paths had no test: a cancellation between rules, a busy item whose rule throws, and the list's own `IsBusy` notification · three tests added, two seen red with the code they pin disabled; loose exception assertions tightened.
- WAIT-006 · code-reviewer · a subscriber that returns null now faulted the write, where the old invocation absorbed it · null is treated as finished again; test seen red first.
- WAIT-006 · code-reviewer · a handler that awaits its own object's wait now deadlocks whatever order it subscribed in, and a guide reads as allowing it · punched to the Punchlist for WAIT-008 (the guides and the migration notes).
- WAIT-006 · test-reviewer (tech-debt) · the todo's plan row still listed AC-12 as served · the row now says it is parked.

---

## Discovery Log

### 2026-11-24 — WAIT-002 · serves AC-7
- **Finding:** DI fell back to a constructor without the tracker factory when no logging was registered.
- **Decision:** Amend
- **Follow-up:** n/a

### 2026-11-24 — WAIT-002 · serves AC-7
- **Finding:** The message recorded when a rule throws carried no rule id, so the rule's next clean run never cleared it.
- **Decision:** Amend
- **Follow-up:** n/a

### 2026-11-24 — WAIT-003 · serves AC-6
- **Finding:** The property's tracker finished its accounting on another thread after an awaited write or rule run had returned, so the idle notification raced the caller's next write.
- **Decision:** Amend
- **Follow-up:** n/a

### 2026-11-24 — WAIT-003 · serves AC-6
- **Finding:** The tracker accounted for a finished task in a `ContinueWith` continuation, which ignores the synchronization context, so completion callbacks and idle notifications ran on a pool thread.
- **Decision:** Amend
- **Follow-up:** n/a

### 2026-11-24 — WAIT-004 · serves AC-1
- **Finding:** A property refuses a busy child and a list refuses a busy item, so a parent is never handed work already in flight; the planned adoption step had nothing to do.
- **Decision:** Amend
- **Follow-up:** n/a

### 2026-11-24 — WAIT-005 · serves AC-4
- **Finding:** A `ValidateListBase` takes an item that is already busy, so WAIT-004's reason for dropping adoption held for entity lists only; and list `IsBusy` read from the items on every notification cost up to 137 times more per edit.
- **Decision:** Amend
- **Follow-up:** n/a

### 2026-11-24 — WAIT-006 · serves AC-12
- **Finding:** Making a save in flight busy means registering it, and `MarkUnmodified` then refuses when a local save with an async operation completes. It needs a ruling on saving, and the criterion is Optional.
- **Decision:** Queue
- **Follow-up:** parked as #103, on the todo's Parked list

---

## Skipped Steps

- Step 1 reconnaissance agents · the analysis for #140 and #141 on 2026-11-24 already mapped these seams, the documented intent and the test inventory; it is `reviews/000-analysis.md`.
- WAIT-001 Step 5 gate · a prose-only plan with no behavior to test.

---

## Sibling Todos

---

## Close-Out Audit

---

## Docs & Retro

---

## Results / Conclusions
