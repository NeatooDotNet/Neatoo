# Neatoo skill: facts first

**Plan #:** 006
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-6
**Status:** Done
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (prose over compiled regions; the doctrine is VisionAlignment's)
**Code-review opt-in:** Yes (the skill is the doctrine Claude loads; every line must match D27–D30)
**Branch:** fci-006-neatoo-skill
**PR:** #120

---

## Scope

Rewrite skills/neatoo's model statement 3 and its placement section around facts, consequences and intents.
- "Is this gesture a fact?" is the first question. The rule, verb, seam and read-model ladder is kept for where the work goes.
- Add rules as code in the cascade, the awaited `Set{Name}` standard with NEATOO011, and meta triggers with NEATOO012, all quoting the FCI-005 TrainingPlan regions.
- Soften domain-logic-placement's "a child never mutates through Parent" (#114). Answer #87 (`[Remote]` is per call) and #88 (modified is sticky).
- Work the two AC-6 punchlist rows: the prose beside the swept snippets, and the safe and sync-only meta triggers with the loop symptom.
- Sync the installed copy.

## Intent

- A fresh session placing logic asks first whether the gesture is a fact, and reaches for a property and a rule before a verb.
- Nothing in the skill forbids a shape the doctrine allows, or teaches a write the analyzers now flag.

## Framework & Architectural Alignment

- VisionAlignment statement 8 and D27–D30. Every C# block is an mdsnippets region from src/Design, pinned by a test; WRONG blocks are the only hand-written C#.
- No ruling IDs in skill prose; no "see link" references to repository source.

## Constraints & Invariants

- SKILL.md stays near 400 lines; mechanism goes to the references.
- The RemoteFactory skill owns `[Remote]` placement; this skill states only what Neatoo reasoning needs.

## Steps

1. Design: a test pinning that a write-back leaves the object modified; refresh stale comments beside swept regions.
2. SKILL.md: model statement 3, habit rows, the placement section with the TrainingPlan regions, rules as code, known pains.
3. References: domain-logic-placement (facts first, the worked example, the Parent bullet), rules-lifecycle and pitfalls (meta triggers, the loop symptom, NEATOO011/012), entities (sticky modified).
4. Prose sweep across docs/guides, docs/reference and the skill.
5. mdsnippets, Design build and test, sync `~/.claude/skills/neatoo`.

## Acceptance

- [x] SKILL.md states facts, consequences and intents in place of "logic is placed by who is driving it", with the first question being whether the gesture is a fact `[explicit-skip: prose; reviewed at the gate]`
- [x] Every new C# block in the skill renders from a compiled region pinned by a test `[integration]`
- [x] The skill teaches the awaited `Set{Name}` in rules and verbs, and names NEATOO011 and NEATOO012 `[explicit-skip: prose]`
- [x] The skill names the meta triggers that are safe for an async rule, the sync-only ones, and the loop symptom `[explicit-skip: prose]`
- [x] A child may write its parent or call a parent method; reaching through the parent into a sibling is the named anti-pattern (#114) `[explicit-skip: prose]`
- [x] Writing the original value back leaves the object modified (#88) `[integration]`
- [x] `[Remote]` is decided per call and composes inside local code (#87) `[explicit-skip: prose]`
- [x] No prose beside a swept snippet shows an assigning `AddAction` or a sync multi-write verb `[explicit-skip: grep evidence]`
- [x] Build and tests green; mdsnippets clean; installed copy matches `[explicit-skip: meta-bullet]`

## Current State (Pre-Flight)

- SKILL.md is 337 lines. Model statement 3 and "Logic is placed by who drives it" place logic by driver; nothing mentions `Set{Name}`, NEATOO011, NEATOO012 or meta triggers.
- domain-logic-placement.md line 941 forbids a child mutating through `Parent`.
- Prose in docs/guides/business-rules.md, docs/guides/properties.md, skills/neatoo/references/properties.md and domain-logic-placement.md says a rule writes "through the private setter" with `AddAction` beside snippets that now await `Set{Name}`; docs/guides/properties.md recommends `LoadProperty` inside a rule.
- A write sets `IsSelfModified` with no value comparison (`EntityPropertyManager.OnPropertyChanged`), so #88's answer holds.

## Test Evidence

The sticky-modified test pins existing framework behaviour (`EntityPropertyManager` marks a write modified with no comparison), so there is no product line to flip; it fails if a write-back ever clears `IsModified`. Every other new block in the skill quotes a FCI-005 region already pinned and flip-checked there.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| SKILL.md states facts, consequences, intents; first question is "is it a fact?" | Required | `[explicit-skip]` | prose: SKILL.md model statement 3, the first habit row, "A fact, a consequence, an intent" | n/a |
| Every new C# block renders from a compiled, tested region | Required | `[integration]` | `TrainingPlanTests` (all 16) pin the TrainingPlan regions; `TrainingPlanTests.WritingTheOriginalValueBack_LeavesThePlanModified` pins `skill-test-modified-is-sticky`; mdsnippets clean, placeholders balanced | ✓ |
| Awaited `Set{Name}`, NEATOO011 and NEATOO012 taught | Required | `[explicit-skip]` | prose: SKILL.md habit rows and placement section; rules-lifecycle "Rules Write Through Awaited Setters"; pitfalls rows | n/a |
| Safe and sync-only meta triggers, loop symptom | Required | `[explicit-skip]` | prose: rules-lifecycle "Rules on Meta State"; pitfalls row | n/a |
| Child may write its parent; sibling-through-parent is the anti-pattern (#114) | Required | `[explicit-skip]` | prose: domain-logic-placement Parent rules; SKILL.md placement section | n/a |
| Write-back leaves the object modified (#88) | Required | `[integration]` | `Design.Tests.AggregateTests.TrainingPlanTests.WritingTheOriginalValueBack_LeavesThePlanModified` | ✓ |
| `[Remote]` is per call (#87) | Required | `[explicit-skip]` | prose: SKILL.md "`[Remote]` is per call" | n/a |
| No prose beside a swept snippet shows an assigning `AddAction` | Required | `[explicit-skip]` | `reviews/006-prose-grep.log`: remaining hits are API lists, VisionAlignment history and the sync-only meta rows; no Design comment assigns in a lambda | ✓ |
| Build and tests green; mdsnippets clean; installed copy matches | Required | `[explicit-skip]` | `reviews/006-build.log`, `006-design-build.log` (0 NEATOO011/012), `006-test.log` (one flake, below), `006-test-rerun.log` (green); installed `~/.claude/skills/neatoo` equals the working tree | ✓ |

`006-test.log` has one failure, `AsyncFlowTests_Property_PropertyChangedEvents`. It fails 1 run in 30 on this branch (`006-asyncflow-30-runs.log`) and 2 in 60 on main (`006-asyncflow-main-60-runs.log`), so it predates FCI; it is untouched and parked as a proposed row.

## Gate Record

- **Test review round 1:** DECISION NEEDED, only on the pre-existing `AsyncFlowTests_Property_PropertyChangedEvents` flake; every Required bullet pinned. The flake is a proposed Parked decision row for Keith; the test is untouched. Its three tech-debt items were fixed: the broken `IsSavable` row, the missing void-verb grep (`006-prose-grep.log`), and the sibling wording. See `reviews/006-test-review.md`.
- **Code review:** APPROVED with seven callouts, all fixed, and four theoretical items taken where cheap. See `reviews/006-code-review.md`.

## Plan Amendments

- #112, #114, #87 and #88 close from the arc PR into main ("Closes" lines), since GitHub closes issues only on a merge into the default branch.

- The verb list carries the two cases statement 8 names that the first draft left out: several facts that land together, and a change the framework cannot track.
- `docs/guides/business-rules.md` reframes `LoadProperty` as the rare write without consequences instead of the loop-breaker. `LoadProperty` is real API; the sentence removed from `docs/guides/properties.md` was accurate, and was removed because D30 makes it the exception, not the default.
