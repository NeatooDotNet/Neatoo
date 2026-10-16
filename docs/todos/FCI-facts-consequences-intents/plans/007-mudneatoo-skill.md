# MudNeatoo skill: components and verb-bound controls

**Plan #:** 007
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-7
**Status:** Done
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (the shape is #113's item 1 and the architecture page's)
**Code-review opt-in:** Yes (a public component parameter, a new test project, and the skill Claude loads)
**Branch:** fci-007-mudneatoo-skill
**PR:** —

---

## Scope

- Every MudNeatoo input gains an optional `Setter` (`Func<T, Task>`). When it is set, a change calls it instead of `EntityProperty.SetValue`; reading the value, messages, busy and read-only state is unchanged (#113 item 1).
- A new test project renders the components with bUnit and pins both write paths.
- Design.App gains a TrainingPlan page whose interval rows are components that refresh themselves. The page subscribes to nothing on the aggregate's graph. The minutes slider reads `Minutes` and writes `SlideMinutes`; the effort slider is a plain fact binding; the nudge writes the next mark.
- skills/mudneatoo replaces "the page owns the subscription" with the component as the unit of refresh, teaches the verb-bound control, and states per component which parameters are forwarded (#94).
- Boundary components, the render throttle and the slider label gaps stay with FCI-009.

## Intent

- A fresh session binds a fact to a MudNeatoo input and an intent to the same input's `Setter`, and never hand-writes a control to call a verb.
- A screen re-renders by component, not by redrawing the page on every graph notification.

## Framework & Architectural Alignment

- VisionAlignment statement 8 and D27–D28: a control writes a fact, or calls the verb that carries the intent. MudNeatoo already awaits the property; the `Setter` is awaited the same way.
- Design snippets rule: page regions are pinned by the build; the ViewModel and the component behaviour are pinned by tests.

## Constraints & Invariants

- No existing component behaviour changes when `Setter` is absent.
- NuGet versions from the registration endpoint only (bUnit 2.11.3).

## Steps

1. `Setter` on the eleven inputs.
2. `Neatoo.Blazor.MudNeatoo.Tests` with bUnit: a change with a `Setter` calls it and does not write the property; without one the property is written.
3. Design.App: TrainingPlan ViewModel, the page, an interval row component; the ViewModel tested in Design.Tests.
4. skills/mudneatoo rewrite; sync `~/.claude/skills/mudneatoo`.

## Acceptance

- [x] A MudNeatoo input with a `Setter` calls it on change, awaits it, and leaves the write to it `[integration]`
- [x] Without a `Setter`, every input still writes through `EntityProperty.SetValue` `[integration]`
- [x] The TrainingPlan ViewModel creates the plan, adds intervals and approves through the aggregate `[integration]`
- [x] The Design.App page binds facts, a verb-bound slider and a nudge, with rows that refresh themselves and no page subscription `[explicit-skip: page region pinned by the build]`
- [x] skills/mudneatoo teaches the component as the unit of refresh and the verb-bound control from compiled regions `[explicit-skip: prose over compiled regions]`
- [x] skills/mudneatoo states which parameters each component forwards (#94) `[explicit-skip: prose checked against the component sources]`
- [x] Build and tests green; mdsnippets clean; installed copy matches `[explicit-skip: meta-bullet]`

## Current State (Pre-Flight)

- All eleven inputs write in a private `OnValueChanged` that awaits `EntityProperty.SetValue`; each subscribes to its own property wrapper's `PropertyChanged`.
- No project renders a component; Design.Tests references Design.App and so MudNeatoo. No bUnit in `Directory.Packages.props`.
- Design.App's Employee, Order and WorkOrder pages subscribe to the root's `NeatooPropertyChanged` and redraw the page; the skill teaches that as the page structure.
- The skill's forwarding sentence already says "a curated set"; it does not say which.

## Test Evidence

Flip check: the `Setter` branch disabled in all eleven inputs failed exactly the twelve with-setter tests and nothing else (`reviews/007-flip-setter.log`); restored, all pass.

Further flips, each restored: a fire-and-forget `Setter` call failed exactly the eleven awaits tests (`007-flip-await.log`); the two components without their subscriptions failed all four component tests (`007-flip-subscription.log`); a row that kept its first subscription failed only the follow test (`007-flip-follow.log`).

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| An input with a `Setter` calls it, awaits it, leaves the write to it | Required | `[integration]` | `Neatoo.Blazor.MudNeatoo.Tests.SetterTests.*_WithASetter_CallsIt_AndLeavesTheWriteToIt` and `.*_AwaitsItsSetter` (all eleven inputs each) | ✓ (bUnit render) |
| Without a `Setter`, every input writes `SetValue` | Required | `[integration]` | `SetterTests.*_WithoutASetter_WritesTheProperty` (all eleven) | ✓ |
| The TrainingPlan ViewModel creates, adds, approves through the aggregate | Required | `[integration]` | `Design.Tests.ViewModelTests.TrainingPlanViewModelTests` (4 tests, client scope), including the setter delegate the row passes | ✓ |
| The page binds facts, a verb-bound slider and a nudge; rows refresh themselves; no page subscription | Required | `[integration]` | `Design.Tests.ComponentTests.TrainingPlanComponentTests` (bUnit): a row refreshes for a sibling's slide, a row follows the interval it is given, the summary refreshes for the plan's facts, the Approve button follows `CanApprove`; the page's bindings pinned by the build | ✓ |
| Skill teaches the unit of refresh and the verb-bound control from compiled regions | Required | `[explicit-skip]` | skills/mudneatoo SKILL.md "Verb-Bound Controls" and "The Component Is the Unit of Refresh"; references updated; `007-mdsnippets.log` | n/a |
| Skill states what each component forwards (#94) | Required | `[explicit-skip]` | the forwarding table in SKILL.md, checked against each component's declared parameters | n/a |
| Build and tests green; mdsnippets clean; installed copy matches | Required | `[explicit-skip]` | `reviews/007-build.log`, `007-design-build.log` (0 NEATOO011/012), `007-test.log` (all green), installed skills equal the working tree | ✓ |

## Gate Record

- **Test review round 1:** APPROVED with two callouts, both fixed and flip-checked: every input's awaited setter is tested, and the row and summary are rendered with bUnit in Design.Tests. Tech-debt dismissed: the test project is single-target like every test project in the repo. See `reviews/007-test-review.md`.
- **Code review:** APPROVED with four callouts, all fixed: the row's subscription code is a quoted region, the row's comment says what it re-renders for, the stale blazor.md phrase, and a Home link to the screen. Two theoretical items taken: the slider's missing read-only binding in the "automatically" list, and keying rows on their item. See `reviews/007-code-review.md`.

## Plan Amendments

- The test project is the first to add a package: bunit 2.11.3, from the NuGet registration endpoint. CLAUDE.md's project map and the design-snippets rule now name it.
- Read-only is unchanged with a `Setter`: ten inputs still bind `ReadOnly` to `IsReadOnly`, which a private setter sets, so a verb-bound text, numeric or picker field needs a public setter today. Separating "private setter" from `MarkReadOnly` is a framework decision, parked for Keith.
- #94 closes from the arc PR into main.
- Design.Tests also takes bunit, to render the TrainingPlan row and summary. The design-snippets rule keeps the page-binding exception for pages only.
