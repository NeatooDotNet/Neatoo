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
- **Every `NeatooPropertyChanged` subscriber is awaited.** A write is not over while any handler of it is running. So a handler must not await `WaitForTasks()`, `RunRules(RunRulesFlag)` or `Save(token)` on its own object or a parent: it would wait for itself. `RunRules(propertyName)` does not wait on the running tasks, so a handler may await it.
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

- [ ] No guide, reference, skill or Design.Domain comment names a removed member or describes cached or flag-based busy `[explicit-skip: documentation; checked by search]`
- [ ] The guides and skills state tracking, waiting, busy and faults as the Intent does `[explicit-skip: documentation; checked by the code review]`
- [ ] The async and Blazor guides say what a `NeatooPropertyChanged` handler must not await `[explicit-skip: documentation]`
- [ ] The release notes carry every migration row from the todo's Punchlist `[explicit-skip: documentation]`
- [ ] `dotnet mdsnippets` leaves every placeholder filled; build and tests green `[explicit-skip: meta-bullet]`
- [ ] `skills/neatoo` and `skills/mudneatoo` are copied to the user's skills folder `[explicit-skip: environment step]`

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

No behavioral bullet. Evidence is recorded in the Gate Record: the searches, the snippet run, the suites and the code review.

---

## Gate Record

---

## Plan Amendments

### 2026-11-24 — A failed lazy load and a wait in progress: pending Keith's ruling

- **Section affected:** Intent, Acceptance (the guides and skills on lazy loading; the migration notes)
- **Original said:** LazyLoad passages are corrected where they are wrong about waiting.
- **What changed:** writing the docs showed that the code changed a documented behavior. On `main`, a fire-and-forget load that fails while the parent's `WaitForTasks()` is waiting throws the loader's exception from that wait. On the arc the wait completes cleanly and the failure is in `HasLoadError` only. `skills/neatoo/references/lazy-loading.md` documented the throw. The docs agent rewrote that sentence and the Design.Domain comment to match the code; those two edits stand in the branch as a description of the code as it is, and are not merged.
- **Why:** WAIT-004 reports a load in flight to its owner as a task that finishes when the property stops loading, whatever the outcome. The only test on the case, `ParentIsValid_AfterExplicitChildLoadFailure`, accepts both outcomes, so no gate saw it. The todo's Out of Scope says LazyLoad's documented waiting keeps working, so which side is right is Keith's to rule: keep the change and note it in the migration, or restore the throw.
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
