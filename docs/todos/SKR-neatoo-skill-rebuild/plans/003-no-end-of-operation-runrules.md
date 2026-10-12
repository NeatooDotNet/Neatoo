# Stop teaching end-of-operation RunRules

**Plan #:** 003
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-5
**Status:** Draft
**Last Updated:** 2026-10-11 (drafted from the stub after recon)
**Plan-review opt-in:** Yes — the replacement shape (derived values loaded from the row or assigned in the operation) becomes doctrine copied into user code; Pass A against D18 and D6
**Code-review opt-in:** Yes — examples are what gets copied
**Branch:** skr-003-no-end-of-operation-runrules — cut from the arc at Step 2
**PR:** —

---

## Scope

Apply D18 across Design.Domain, the skill and the user docs: running rules at the end of a `[Create]` or `[Fetch]` is the user's rare decision, not a step. Persisted data is assumed valid; a derived value an operation needs is loaded from the row (as `OrderItem.LineTotal`, `WorkOrderTask.Cost` and `WorkOrder.TotalCost` already are) or assigned inside the operation. The D6 gate — `RunRules(All)` at the top of a root's `[Insert]`/`[Update]` — is a different thing and stays.

In code: `WorkOrder.Create` stops calling `RunRules` and sets the derived values its known state implies; `WorkOrder.Fetch` loads the root's derived values from the row instead of re-running `Status`/`Budget` rules, and the row carries them; `CommonGotchas` gotcha 1 ("rules don't fire during `[Create]`, so call `RunRules` at the end") becomes "assign the derived value in the operation", its `CreateWithRunRules` operation and summary-table row follow, and the gotcha-1 tests pin the new shape. Gotcha 4 (`PauseAllActions` outside an operation) is a different case and is reworded only where it generalizes to operations. One place in the skill says when a user might still call `RunRules` after an operation (an object returned already showing its broken rules) and that it is rare.

In prose: the ~12 lines in `SKILL.md`, `rules-lifecycle.md`, `validation.md`, `testing.md`, `collections.md`, `blazor.md`, `domain-logic-placement.md`, `pitfalls.md`, `docs/guides/async.md`, `business-rules.md`, `collections.md`, `validation.md`, `docs/reference/api.md` and `CLAUDE-DESIGN.md:156` that teach the habit are rewritten to D18; the regions re-render. Close #98 as moot (pulled from the todo punchlist). The plan does not reorganize the skill; plan 004 does.

---

## Intent

- A reader writing a `[Create]` or `[Fetch]` never sees "call `RunRules` at the end" as a step anywhere in the example set, the skill or the docs (trap T10).
- A reader who needs a derived value after an operation sees the two shapes that give it: load it from the row, or assign it in the operation.
- A reader who wants an object returned already invalid sees that `RunRules` after an operation is for that, and that it is rare.

---

## Framework & Architectural Alignment

- D18 (2026-10-09): loaded data is assumed valid; derived values are loaded or assigned inside the operation; end-of-operation `RunRules` is the user's rare decision. The 2026-10-09 job-example miss (the orchestrator and the plan-reviewer both added a validate step by habit) is why this plan exists: the habit is reached for even with the fact present.
- D6 stands: the root's `[Insert]`/`[Update]` run `RunRules(All)` and throw on an invalid aggregate (`Order.cs`, `Employee.cs`, `Product.cs`, `SavePatterns.cs`, `WorkOrder.cs`). Those calls are a gate at the top of a write, not a step at the end of a read, and are not touched.
- #98's mechanism stays stated: a root rule that writes a child after the child's `[Fetch]` finished marks the child modified — one more reason the derived values come from the row. The sentence in `rules-lifecycle.md` survives the rewrite.
- `.claude/rules/design-snippets.md`: regions tight, unique, pinned by a test; the `skill-create-with-run-rules` name goes away with the shape.
- Memory `feedback-no-validate-step`: the orchestrator does not add a validate step; the reviewers are briefed not to.

---

## Constraints & Invariants

- No `RunRules` call remains inside a `[Create]` or `[Fetch]` in Design.Domain. `RunRules` calls inside `[Insert]`/`[Update]` (D6) and inside rules (`WorkOrder.cs:108`, `WorkOrderTaskList.cs:39`) stay.
- A fetched `WorkOrder` with tasks reports `IsModified == false` and the same derived values it had when saved; a created one reports `CanApprove == true` with `Status == "Pending"`.
- Existing Design.Tests stay green; the only existing tests changed are the gotcha-1 tests, which are rewritten to pin the replacement shape (their intent — a created object's derived values are right — is preserved) and any `WorkOrderAggregateTests` assertion that depended on `Create`'s `RunRules` (checked at pre-flight: none assert messages after Create).
- The gotcha-1 rewrite keeps the gotcha (rules do not fire during an operation) and changes only the remedy.
- `Design.sln` builds with 0 warnings; `dotnet mdsnippets` reports no duplicate or missing regions.

---

## Steps

1. `WorkOrder`: `Create` sets `Status = "Pending"` and `CanApprove = true` with no `RunRules`; the region is renamed to say what it shows (derived values assigned in the operation). `WorkOrderRow` gains the root's derived values (`CanApprove`, `IsHeld`, `IsOverBudget`, `Summary`), `MapTo` writes them, `Fetch` loads them and calls no `RunRules`; the Fetch comment says why (persisted data is valid; the row carries what the rules computed when it was saved; a rule that wrote a finished child would mark it modified — #98).
2. `CommonGotchas` gotcha 1: the remedy becomes "assign the derived value in the operation" with a `[Create]` that does so; `CreateWithRunRules` is removed; the header, the inline WRONG/RIGHT comments and the summary-table row follow. Gotcha 4's text is checked for sentences that generalize `RunRules` to operations and those are reworded; its `PauseAllActions` guidance stays.
3. Design.Tests: the gotcha-1 tests pin the new remedy (created object has its derived value, no `RunRules` called); a `WorkOrderAggregateTests` test pins that a fetched work order with tasks is unmodified and carries its derived values (#98's requested test); the created work order's `CanApprove` is pinned.
4. Skill and docs prose: every line found at pre-flight that teaches the habit is rewritten to D18; one paragraph in `rules-lifecycle.md` states the rare case and that it is the user's call; `CLAUDE-DESIGN.md:156` follows. Regions re-render.
5. Build, test, `dotnet mdsnippets`; grep confirms no "end of factory method"/"after `[Create]`" `RunRules` teaching remains; close #98 with a comment naming D18 and this plan.

---

## Acceptance

- [ ] A `WorkOrder` created through its factory reports `Status == "Pending"` and `CanApprove == true` without any `RunRules` call in `Create` `[integration]` · Must
- [ ] A `WorkOrder` with tasks saved and fetched back reports `IsModified == false` and the same `CanApprove`, `IsHeld`, `IsOverBudget`, `Summary` and task `Cost`/`IsSchedulable` it had when saved, with no `RunRules` call in `Fetch` `[integration]` · Must
- [ ] Gotcha 1's example `[Create]` yields its derived value without `RunRules`, and the gotcha's statement (rules do not fire during an operation) is still pinned `[integration]` · Must
- [ ] No `[Create]` or `[Fetch]` in Design.Domain contains a `RunRules` call; the D6 gate calls in `[Insert]`/`[Update]` are unchanged `[explicit-skip: verified by grep]` · Must
- [ ] No skill, doc or Design.Domain text teaches `RunRules` at the end of a `[Create]`/`[Fetch]` as a step; exactly one place states the rare case `[explicit-skip: verified by grep of the pre-flight list]` · Must
- [ ] #98 closed as moot with a comment naming D18 `[explicit-skip: GitHub]` · Must
- [ ] `Design.sln` builds with 0 errors and 0 warnings, every Design.Tests test passes, `dotnet mdsnippets` reports no duplicate or missing regions `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

Walked 2026-10-11.

- `RunRules` inside operations in Design.Domain (non-Generated): `WorkOrder.cs:147` (`Create`, region `skill-create-with-run-rules`), `:179-180` (`Fetch`: `RunRules(nameof(Status))`, `RunRules(nameof(Budget))` with a comment explaining the child-modification hazard), `CommonGotchas.cs:107` (`CreateWithRunRules`). D6 gates (stay): `Order.cs:196,250`, `WorkOrder.cs:189,211`, `Employee.cs:191,246`, `Product.cs:77,95`, `SavePatterns.cs:249,288`. Inside rules (stay): `WorkOrder.cs:108`, `WorkOrderTaskList.cs:39`.
- `WorkOrderRow` persists `TotalCost` but not `CanApprove`, `IsHeld`, `IsOverBudget`, `Summary`; `WorkOrderTaskRow` persists `Cost` and `IsSchedulable`; `OrderItemRow` persists `LineTotal`.
- `CommonGotchas.cs`: gotcha 1 header `:30-47` ("RIGHT: Call RunRules at the end of your factory method"), `CreateWithRunRules` `:99-107` (region `Gotcha1_...`? — check the exact region names at Step 2), gotcha 4 `:348-358` ("Call RunRules inside or after the paused block"), summary table `:500` (row 1 "await RunRules() at end") and `:509` (row 4).
- Design.Tests using `RunRules`: `GotchaTests/CommonGotchaTests.cs` — `Gotcha1_RulesFireAfterCreate_WithExplicitRunRules` (:54, region rendered into 5 doc/skill files), `Gotcha1_RunRulesInsideFactoryMethod_WorksWhilePaused` (:72, calls `CreateWithRunRules`), `Gotcha4_*` (:199, :220 — `PauseAllActions`, stay); `AggregateTests/AggregateCoverageGapTests.cs:221` (`address.RunRules()` in a test body, not an operation — stays).
- Prose teaching the habit (lines at pre-flight): `skills/neatoo/SKILL.md:724,730-745`; `references/rules-lifecycle.md:17-18,80-103,149`; `references/validation.md:481-506`; `references/testing.md:77`; `references/collections.md:228`; `references/blazor.md:150`; `references/domain-logic-placement.md:553`; `references/pitfalls.md:28`; `docs/guides/async.md:227-241`; `business-rules.md:509-519,803-811`; `collections.md:439`; `validation.md:445-476`; `docs/reference/api.md:155-169`; `src/Design/CLAUDE-DESIGN.md:156`. Legitimate `RunRules` API mentions (flags, cancellation, `PauseAllActions`) are not in scope.
- `WorkOrderAggregateTests` asserts after `Create` only on values the verb/rules set later; `WorkOrderViewModelTests` (SKR-002) asserts `CanApprove == true` after `New()` — must still hold with the assignment in `Create`.
- #98 (open): asks for the doctrine statement, the fetched-unmodified test and a `CLAUDE-DESIGN.md` mention — this plan supplies all three, with the row carrying the derived values instead of a targeted re-run.

---

## Punchlist

- [ ] Close #98 as moot under D18 · GitHub · done when closed with a comment naming D18 · AC-5 · Must (pulled from the todo, 2026-10-11)

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Created WorkOrder: Pending, CanApprove, no RunRules | Must | `[integration]` | | |
| Fetched WorkOrder: unmodified, derived values intact | Must | `[integration]` | | |
| Gotcha 1 remedy without RunRules | Must | `[integration]` | | |
| No RunRules in Create/Fetch; gates unchanged | Must | `[explicit-skip]` | | |
| No prose teaches the habit; one rare-case statement | Must | `[explicit-skip]` | | |
| #98 closed | Must | `[explicit-skip]` | | |
| Build, tests, mdsnippets green | Must | `[explicit-skip]` | | |

---

## Gate Record

-

---

## Plan Amendments

### 2026-10-11 — Drafted from the stub

- **Section affected:** Scope, Steps
- **Original said:** rewrite the sites, replace two regions, keep one rare-case statement, close #98; `WorkOrder`'s `[Fetch]` named without saying how.
- **What changed:** the how: `WorkOrderRow` carries the root's derived values and `Fetch` loads them (the shape the children and `OrderItem` already use); `Create` assigns what its known state implies. The pre-flight inventory found the D6 gates share the `RunRules(All)` call and must be distinguished in prose, not removed.
- **Why:** pre-flight recon.

---

## Notes

- Skills consulted at drafting: `skills/neatoo/references/rules-lifecycle.md`, `validation.md` read from the repo (not loaded via the Skill tool).
