# Guides, skills and release notes

**Plan #:** 008
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-9
**Status:** In Progress
**Last Updated:** 2026-11-24
**Plan-review opt-in:** No (the rulings D31 to D36 and the merged plans state what the docs must say)
**Code-review opt-in:** Yes (an unanchored read of the prose against the code is the only check a docs plan gets)
**Branch:** wait-008-docs-and-release-notes
**PR:** —

---

## Scope

Brings `docs/guides`, the API reference, `skills/neatoo`, `skills/mudneatoo`, `src/Design/CLAUDE-DESIGN.md` and the Design.Domain comments into line with the code on tracking, waiting, busy and faults, and writes the release notes with the migration for the removed and changed members. It changes no behavior and no test.

---

## Intent

What a reader must come away knowing, because the code now behaves this way:

- **One record of async work.** Every async operation on an object is a task in that object's running tasks, whoever started it: a property setter, `Set{Name}`, `IValidateProperty.SetValue` (every MudNeatoo input), `RunRules(propertyName)`, `RunRules(RunRulesFlag)` awaited or not, a meta rule, a lazy load. The owner registers it; callers and generated code register nothing. A task passes up through `Parent` to the root.
- **`WaitForTasks()` awaits those tasks and nothing else.** `WaitForTasks(token)` is the same wait with cancellation; cancelling stops the wait, not the work. When the wait completes, nothing on the object or below it is busy, and whoever listens for `IsBusy` has already been told.
- **`IsBusy` reads tasks.** On an object it is "its running tasks are running". On a property it is "this property has work in flight, or the object it holds is busy", which is what an input binds for one field. On a list it is "an item is busy", read from the items.
- **A rule that throws surfaces once, and the object recovers (D34).** The awaiter of the write or of `RunRules` gets the exception. The next `WaitForTasks()` on the object throws it once, in an `AggregateException`, to the waits that were waiting. After that the object tracks and waits as before. The trigger property keeps the message, so the object stays invalid and unsavable until the rule runs clean. A rule that throws before its first `await` still throws from the setter.
- **Every `NeatooPropertyChanged` subscriber is awaited.** A write is not over while any handler of it is running. So a handler must not await `WaitForTasks()`, `RunRules(RunRulesFlag)` or `Save(token)` on the object where the write was made or on any parent of it, which includes its own object: it would wait for itself. `RunRules(propertyName)` does not wait on the running tasks, so a handler may await it.
- **Neatoo objects are single-threaded**, like a UI application. The tracker accounts for a finished task on the synchronization context that added it.
- **What is gone or changed**, for the migration notes: every row the todo's Punchlist carries for WAIT-002 to WAIT-006, checked against the diff of public and protected signatures between `main` and the arc.

---

## Framework & Architectural Alignment

- D31 to D36 in `docs/VisionAlignment.md` are the source for every statement above.
- Code samples in guides and skills come only from compiled regions in `src/Design/` through `dotnet mdsnippets`. No C# is typed into a guide or a skill.
- DDD terms are used and not explained.
- LazyLoad is pending removal (D24): its passages are corrected where they are wrong about waiting, and not extended.

---

## Constraints & Invariants

- No product code and no test changes. A Design.Domain comment may change; a Design.Domain or Design.Tests region changes only if a guide needs a sample that does not exist, and that is reported.
- The release tag is Keith's. This plan sets the version and writes the notes; it tags nothing.
- `docs/release-notes/v0.36.1.md` is history: only its "has not been measured" sentence is corrected, by pointing at what measured it.

---

## Steps

1. Guides, API reference, skills, `CLAUDE-DESIGN.md` and Design.Domain comments: correct every passage the Intent contradicts, and add the handler rule.
2. Release notes for the next version, with the migration section, and the version number.
3. Regenerate snippets, build, run the suites, copy the two skills to the user's skills folder.

---

## Acceptance

- [x] No guide, reference, skill or Design.Domain comment names a removed member or describes cached or flag-based busy `[explicit-skip: documentation; checked by search]`
- [x] The guides and skills state tracking, waiting, busy and faults as the Intent does `[explicit-skip: documentation; checked by the code review]`
- [x] The async and Blazor guides say what a `NeatooPropertyChanged` handler must not await `[explicit-skip: documentation]`
- [x] The release notes carry every migration row from the todo's Punchlist `[explicit-skip: documentation]`
- [x] `dotnet mdsnippets` leaves every placeholder filled; build and tests green `[explicit-skip: meta-bullet]`
- [x] `skills/neatoo` and `skills/mudneatoo` are copied to the user's skills folder `[explicit-skip: environment step]`

---

## Current State (Pre-Flight)

- 36 files under `docs/`, `skills/`, `README.md` and `src/Design/CLAUDE-DESIGN.md` mention waiting or busy. Known stale passages: `docs/reference/api.md` (public `AddChildTask` twice, `AddMarkedBusy` / `RemoveMarkedBusy`, the property's `Task` row); `docs/guides/properties.md` "tracked per property, not per entity"; `docs/guides/collections.md` (cached busy, "same algorithm applies to IsBusy"); `docs/guides/blazor.md` near line 102 ("a handler"); `docs/guides/async.md` Error Handling (says nothing about once, or recovery) and CancellationToken Support; `src/Design/CLAUDE-DESIGN.md` (the `AddMarkedBusy` flow, `Interlocked` execution ids).
- The version is 0.36.1. Breaking releases before 1.0 have been minor bumps (0.31.0, 0.33.0, 0.34.0), so the next is 0.37.0.
- The legacy cancellation todo was already moved to `docs/todos/completed/` by WAIT-005; the stub's line about moving it is dropped.
- The public and protected signature diff between `main` and the arc was listed at pre-flight and is the check on the migration section.

---

## Punchlist

---

## Test Evidence

The Acceptance bullets are documentation checks; their evidence is in the Gate Record: the searches, the snippet run and the code review.

One behavior is pinned here, because Keith's ruling on a failed lazy load (see Plan Amendments) changed what a test must claim. Both are in `Neatoo.UnitTest.Integration.Concepts.EntityBase.LazyLoadExplicitLoadPropagationTests`:

| Test | Claimed before | Claims now | Justification |
|---|---|---|---|
| `ParentIsValid_AfterExplicitChildLoadFailure` | The parent's `WaitForTasks()` either completes or throws `InvalidOperationException`; either way the parent is invalid | The wait completes without throwing; the parent is invalid | Keith, 2026-11-24: "Keep the change" |
| `ParentWaitForTasks_InProgressWhenTheLoadFails_CompletesWithoutThrowing` | (new) | A wait in progress when a fire-and-forget load fails completes without throwing; `HasLoadError` is set and the parent is invalid | The same ruling. On `main` this case throws: `reviews/008-lazy-failure-probe.log` |

Full runs: `reviews/008-build.log`, `008-test.log` (2,110 passed, 2 skipped, 0 failed), `008-design-build.log`, `008-design-test.log` (235 passed).

---

## Gate Record

- Searches (2026-11-24): the docs agent's search for removed members and cached busy over the in-scope paths left three hits, each correct (the `protected` `AddChildTask` row, "incremental caching" for `IsValid`, and a denial in `CLAUDE-DESIGN.md`). An independent search with no word boundaries over every `.md`, `.cs` and `.razor` file found, beyond history under `docs/plans` and `docs/completed`, three stale comments in product and test code, now corrected. `dotnet mdsnippets` leaves no placeholder unfilled and changes nothing on a second run.
- Round 1 (2026-11-24): code review APPROVED, no veto, four callouts, all taken — `reviews/008-code-review.md`:
  - The migration advice named `TrackTask(task)` alone, which makes the property busy and not its object. The notes now name `ReportToOwner(task)` beside it.
  - The notes said the trigger property "still" keeps its message until the rule runs clean. On `main` a clean run did not clear it. That fix (WAIT-002) is now behavior change 6.
  - A handler that throws after an `await` now faults the write whichever position it subscribed in. Added to behavior change 4.
  - The self-wait rule named the handler's own object and its parents and missed the object written to: a root handler, or a parent's rule on a child's property, that awaits the child's wait also waits for itself. Reworded in the guides, the reference, both skills, `CLAUDE-DESIGN.md` and the notes.
  - It confirmed the compile-breaks table against the signature diff, that no C# was typed into a guide or skill, and the account of the lazy-load change.
- Round 1, test review (2026-11-24): APPROVED, no veto, no callout — `reviews/008-test-review.md`. The changed test is stricter than before and pins the already-failed case; the new test pins the wait-in-progress case, the one that differs from `main`. Its note that the changed test's comment could make the new one look like a duplicate was taken.
- Round 2 (2026-11-24): code review APPROVED, no veto, no callout. All four round 1 corrections hold against the code, and the widened self-wait rule is not too wide (siblings, descendants and `RunRules(propertyName)` stay allowed). Its three small notes were taken: the lazy-load note is behavior change 9, not 8; behavior change 7 now names a lazy load; the comment edit is committed. The gate is closed.
- Skills: `skills/neatoo` and `skills/mudneatoo` copied to the user's skills folder; `diff -rq` reports no difference. Before the copy, only the nine files this arc changed differed.
- Final runs on the merged tree: `reviews/008-build.log`, `008-test.log` (2,110 passed, 2 skipped, 0 failed), `008-design-build.log`, `008-design-test.log` (235 passed).

---

## Plan Amendments

### 2026-11-24 — A failed lazy load and a wait in progress: ruled, the change is kept

- **Section affected:** Intent, Acceptance (the guides and skills on lazy loading; the migration notes)
- **Original said:** LazyLoad passages are corrected where they are wrong about waiting.
- **What changed:** writing the docs showed that the code changed a documented behavior. On `main`, a fire-and-forget load that fails while the parent's `WaitForTasks()` is waiting throws the loader's exception from that wait. On the arc the wait completes cleanly and the failure is in `HasLoadError` only. `skills/neatoo/references/lazy-loading.md` documented the throw. The docs agent rewrote that sentence and the Design.Domain comment to match the code; those two edits stand in the branch as a description of the code as it is, and are not merged.
- **Why:** WAIT-004 reports a load in flight to its owner as a task that finishes when the property stops loading, whatever the outcome. The only test on the case, `ParentIsValid_AfterExplicitChildLoadFailure`, accepts both outcomes, so no gate saw it. The todo's Out of Scope says LazyLoad's documented waiting keeps working, so which side is right is Keith's to rule: keep the change and note it in the migration, or restore the throw.
- **Ruling:** Keith, 2026-11-24: "Keep the change". A failed load never faults a wait; `HasLoadError` is the one channel. The agent's two edits stand, the migration notes carry it as behavior change 9, and the test is rewritten to the ruling: `ParentIsValid_AfterExplicitChildLoadFailure` no longer accepts a throw, and `ParentWaitForTasks_InProgressWhenTheLoadFails_CompletesWithoutThrowing` is new.
- **Discovery Log:** 2026-11-24 / WAIT-008

### 2026-11-24 — The handler rule names `RunRules(RunRulesFlag)`

- **Section affected:** Intent
- **Original said:** a handler must not await `WaitForTasks()`, `RunRules()` or `Save(token)`.
- **What changed:** the rule names `RunRules(RunRulesFlag)`. `RunRules(propertyName)` does not wait on the object's running tasks, and Design.Domain's `WorkOrderTaskList.HandleNeatooPropertyChanged` awaits it on siblings, which stays the supported pattern.
- **Why:** found by the docs agent against the code.
- **Discovery Log:** n/a (wording)

### 2026-11-24 — Two comment-only edits outside the docs

- **Section affected:** Constraints ("no product code and no test changes")
- **What changed:** one comment in `src/Neatoo/Internal/EntityPropertyManager.cs` and two in `src/Neatoo.UnitTest/Unit/Core/ValidateListBaseTests.cs` said list or manager busy is a cached flag recalculated on resume. They now say it is read from the items. No code line and no assertion changed.
- **Why:** the search for cached busy found them.
- **Discovery Log:** n/a

---

## Notes
