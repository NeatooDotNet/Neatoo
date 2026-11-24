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
- [ ] **AC-7** · Required — An async rule that throws surfaces from `WaitForTasks()` once, and the object then tracks and waits for later work.
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
| 001 | [001-decision-record](./plans/001-decision-record.md) | The rulings in VisionAlignment | AC-9 | Done | PRNUM001 |
| 002 | [002-tracker-faults](./plans/002-tracker-faults.md) | The tracker faults once, then recovers | AC-7 | Draft | — |
| 003 | [003-property-tasks](./plans/003-property-tasks.md) | A property holds its work as tasks | AC-6, AC-3 | Draft | — |
| 004 | [004-framework-registers](./plans/004-framework-registers.md) | The framework registers every reaction | AC-1 | Draft | — |
| 005 | [005-waits-await-tasks](./plans/005-waits-await-tasks.md) | Waits await tasks; busy reads tasks | AC-2, AC-3, AC-4, AC-5 | Draft | — |
| 006 | [006-optional-criteria](./plans/006-optional-criteria.md) | Subscribers, notification order, save in flight | AC-10, AC-11, AC-12 | Draft | — |
| 007 | [007-mudneatoo-and-design-app](./plans/007-mudneatoo-and-design-app.md) | MudNeatoo and Design.App on the new tracking | AC-8 | Draft | — |
| 008 | [008-docs-and-release-notes](./plans/008-docs-and-release-notes.md) | Guides, skills and release notes | AC-9 | Draft | — |

---

## Punchlist

## Dismissed

## Parked

- proposed · `ValidateProperty.LoadValue` called twice with the same instance subscribes twice · 0.36.1 design critique, 2026-11-24

---

## Recommendations Taken

[Every reviewer recommendation the loop took, for Keith's end review. One line each: plan, reviewer, what was recommended, what was done.]

---

## Discovery Log

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
