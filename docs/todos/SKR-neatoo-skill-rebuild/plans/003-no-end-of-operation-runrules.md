# Stop teaching end-of-operation RunRules as a step

**Plan #:** 003
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-5
**Status:** In Progress
**Last Updated:** 2026-10-11 (plan review round 2 APPROVED; implementation started)
**Plan-review opt-in:** Yes — the shape the skill points at for T10; Pass A against D18 as clarified and D6
**Code-review opt-in:** Yes — examples are what gets copied
**Branch:** skr-003-no-end-of-operation-runrules — cut from the arc at Step 2
**PR:** —

---

## Scope

Apply D18 as clarified on 2026-10-11 across Design.Domain, the skill and the user docs. The habit to stop teaching is `RunRules(RunRulesFlag.All)` prescribed as a routine step at the end of every factory operation ("this applies to all factory methods", "use at end of factory methods") and as a validate step: persisted data is assumed valid. What replaces it is the choice an operation makes for the derived values it needs: run the rules that compute them — targeted at the rules that write the object itself, never `All` from a root whose rules write children that have finished their own `[Fetch]` (#98) — or load them from the row. `RunRules` inside a `[Fetch]` or `[Create]` for that purpose is a normal tool ("that's a great case to use RunRules"), not a smell. The D6 gate — `RunRules(All)` at the top of a root's `[Insert]`/`[Update]` — is a different thing and stays.

In code: `WorkOrder.Fetch` keeps its targeted `RunRules(nameof(Status))`/`RunRules(nameof(Budget))` and becomes the T10 example, in a region; it also produces `HasUnschedulableTasks`, which neither the targeted re-run nor the row supplies today (plan review callout 2 — an existing gap the page would show after a Load). `WorkOrder.Create` keeps its `RunRules(All)` (no finished children exist yet) with a comment saying why `All` is safe there. `CommonGotchas` gotcha 1 keeps its statement (rules do not fire during an operation) and its `CreateWithRunRules` demonstration, but its remedy text changes from "call `RunRules` at the end of your factory method" to the choice above; the summary-table row follows. #98's three asks are fulfilled — the doctrine statement in the user docs with the `WorkOrder.Fetch` snippet, a test that a fetched work order with tasks is unmodified and carries its derived values, a `CLAUDE-DESIGN.md` mention — and #98 is closed as done, not moot.

In prose: the lines found at pre-flight (plus the six the plan review added) that prescribe the blanket step are rewritten; the rare case of running rules to *validate* a returned object stays stated where it applies (the Blazor save contract, the `skill-run-rules-after-create` region) and gets its doctrine sentence in `rules-lifecycle.md`; regions re-render. The plan does not reorganize the skill; plan 004 does.

---

## Intent

- A reader writing a `[Create]` or `[Fetch]` never sees "call `RunRules(All)` at the end" as a step; they see the question "what derived values does this operation need?" and the two ways to supply them (trap T10).
- A reader with a root whose rules write children sees why `All` from the root's `[Fetch]` marks the children modified, and the targeted form that does not (#98).
- A reader who wants an object returned already showing broken rules sees that `RunRules` after an operation does that and that it is rare — where they would hit the case, not as a step.

---

## Framework & Architectural Alignment

- D18 as clarified 2026-10-11 (`VisionAlignment.md` §0 row updated this plan): no routine end-of-operation `RunRules(All)`, no validate step; derived values computed inside the operation, `RunRules` a normal tool for that; targeted, not `All`, from a root with child-writing rules; or loaded from the row.
- D6 stands: the root's `[Insert]`/`[Update]` run `RunRules(All)` and throw. Those are a gate at the top of a write and are not touched (`Order.cs`, `WorkOrder.cs`, `Employee.cs`, `Product.cs`, `SavePatterns.cs`; the same sites in SKILL.md, docs, README, CLAUDE-DESIGN — all verified gates by the plan review).
- #98's mechanism, verified by the plan review against the framework: while paused, assignments do not mark modified and no rule fires (`EntityPropertyManager.cs:43`); `FactoryComplete` resumes without running rules; a root rule that writes a child after the child's `[Fetch]` finished writes it unpaused and marks it modified. `WorkOrder.Fetch` already avoids that with the targeted form.
- The 2026-10-09 job-example miss (a validate step added by habit) and the 2026-10-11 over-correction (this plan's first draft proposed removing `WorkOrder.Fetch`'s `RunRules`) are both in the memory note `feedback-no-validate-step`; the reviewers are briefed on both.
- `.claude/rules/design-snippets.md`: regions tight, unique, pinned by a test.

---

## Constraints & Invariants

- `RunRules` calls inside `[Create]`/`[Fetch]` are kept where they compute what the operation needs (`WorkOrder.Create`, `WorkOrder.Fetch`, gotcha 1's `CreateWithRunRules`); none is added as a blanket or validate step. The D6 gate calls and the in-rule calls (`WorkOrder.cs:108`, `WorkOrderTaskList.cs:39`) are unchanged.
- A fetched `WorkOrder` with tasks reports `IsModified == false` and the derived values it had when saved, including `HasUnschedulableTasks`.
- Existing Design.Tests stay green and unchanged except: the `MockWorkOrderRepository.SeedWorkOrder` may need `HasUnschedulableTasks`-relevant rows for the new test, and assertion *messages* inside rendered test regions that say "Create/Fetch ran the rules" are kept (they are now correct doctrine) or reworded only if they prescribe the blanket step. The `Gotcha1_RunRulesInsideFactoryMethod_WorksWhilePaused` test pins "`RunRules` has no `IsPaused` guard" and stays.
- Gotcha 4 (`PauseAllActions` outside an operation) is a different case and is not touched (plan review Q4).
- `Design.sln` builds with 0 warnings; `dotnet mdsnippets` reports no duplicate or missing regions.

---

## Steps

1. `WorkOrder.Fetch`: wrap in a `skill-*` region with a header that states the shape (targeted `RunRules` for the root's own derived values; children load theirs from their rows; `All` here would mark the finished tasks modified — #98). Make `Fetch` produce `HasUnschedulableTasks` by persisting it in `WorkOrderRow` as `TotalCost` is (both aggregate the children; `MapTo` writes it, `Fetch` loads it). Not by `RunRules("Tasks.IsSchedulable")`: `RunRules(string)` matches the trigger path exactly and the indexer is dropped from the path, so the string is undocumented and a wrong one matches nothing silently (plan review round 2). The comment says: re-run the root's own rules, load the child aggregates. `WorkOrder.Create`'s comment says why `All` is safe there (no children exist yet).
2. `CommonGotchas` gotcha 1: header, inline RIGHT text, `CreateWithRunRules`'s comment and the summary-table row restated as the choice (compute what you need: targeted `RunRules`, or load it; persisted data is assumed valid; `All` only when no finished child can be written). Region names that say the old shape (`skill-create-run-rules`, `skill-create-with-run-rules`) are renamed to say the new one; placeholders follow. The test region `skill-run-rules-forces` (caller-side `RunRules(All)` after Create "so Total is calculated" — T10's habit, computed outside the operation) stops rendering: its five placeholders (`async.md`, `business-rules.md`, `validation.md`, `api.md`, skill `validation.md`) switch to the `CreateWithRunRules` region, and the test keeps its region name only for the rare-validate prose if needed (round 2 callout 1).
3. Design.Tests: a `WorkOrderAggregateTests` test pins that a saved-then-fetched work order with tasks (one unschedulable) is unmodified and carries `CanApprove`, `IsHeld`, `IsOverBudget`, `Summary`, `HasUnschedulableTasks` and the tasks' `Cost`/`IsSchedulable` (#98's test). The gotcha-1 tests stay.
4. Skill and docs prose: every line in the pre-flight inventory and the plan review's additions is rewritten to the clarified D18; `rules-lifecycle.md` gets the doctrine paragraph (the choice, the #98 hazard, the rare validate case pointing at `skill-run-rules-after-create`); the Blazor save-contract statement stays as the concrete rare case; `CLAUDE-DESIGN.md:156` follows and gains the #98 mention in its factory-operation section; the user docs get #98's statement with the `WorkOrder.Fetch` snippet. Regions re-render.
5. Build, test, `dotnet mdsnippets`; grep confirms no "end of factory method"/"all factory methods" `RunRules` prescription remains; close #98 as done with a comment naming D18 and this plan.

---

## Acceptance

- [ ] A `WorkOrder` with tasks (one unschedulable) saved and fetched back reports `IsModified == false` and the same `CanApprove`, `IsHeld`, `IsOverBudget`, `Summary`, `HasUnschedulableTasks` and task `Cost`/`IsSchedulable` it had when saved `[integration]` · Must
- [ ] `WorkOrder.Fetch` runs no `RunRules(All)`; its `RunRules` calls run only rules that write the root itself (child aggregates are loaded from the row), and the shape is in a `skill-*` region pinned by the test above `[explicit-skip: verified by reading the region and the test]` · Must
- [ ] Gotcha 1 still pins "rules do not fire during an operation" and "`RunRules` has no `IsPaused` guard"; its remedy text and summary row no longer prescribe `RunRules` at the end of every factory method `[explicit-skip: existing tests unchanged; verified by grep]` · Must
- [ ] No skill, doc or Design.Domain text prescribes `RunRules(All)` as a routine end-of-operation step or a validate step; the rare validate case is stated where it applies and nowhere as a step `[explicit-skip: verified by grep of the pre-flight list plus the plan review's additions]` · Must
- [ ] #98's three asks delivered (doc statement with snippet, fetched-unmodified test, `CLAUDE-DESIGN.md` mention); #98 closed as done `[explicit-skip: GitHub]` · Must
- [ ] `Design.sln` builds with 0 errors and 0 warnings, every Design.Tests test passes, `dotnet mdsnippets` reports no duplicate or missing regions `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

Walked 2026-10-11; plan review round 1 added the items marked (PR).

- `RunRules` inside operations in Design.Domain (non-Generated): `WorkOrder.cs:147` (`Create`, `All`, region `skill-create-with-run-rules`, renders into `domain-logic-placement.md:555`), `:179-180` (`Fetch`: `RunRules(nameof(Status))`, `RunRules(nameof(Budget))`; no region), `CommonGotchas.cs:107` (`CreateWithRunRules`, region `skill-create-run-rules`, renders into `SKILL.md:726`, `rules-lifecycle.md:82`, `business-rules.md:511`). D6 gates (stay): `Order.cs:196,250`, `WorkOrder.cs:189,211`, `Employee.cs:191,246`, `Product.cs:77,95`, `SavePatterns.cs:249,288`. Inside rules (stay): `WorkOrder.cs:108`, `WorkOrderTaskList.cs:39`.
- `WorkOrder`'s derived root values: `CanApprove` (trigger `Status`), `IsHeld` (`IsOnHold`, `Status`), `IsOverBudget` (`TotalCost`, `Budget`; `:82-85`), `Summary` (four-trigger rule `:117-122`: `Status`, `IsOnHold`, `Budget`, `Discount`), `HasUnschedulableTasks` (trigger `Tasks![0].IsSchedulable`, `:65-67`). `Fetch`'s two targeted calls cover the first four; `HasUnschedulableTasks` is neither re-run nor in `WorkOrderRow` (PR callout 2). `WorkOrderRow` persists `TotalCost`; `WorkOrderTaskRow` persists `Cost`, `IsSchedulable`; `OrderItemRow` persists `LineTotal`.
- `MockWorkOrderRepository.SeedWorkOrder` (`TestInfrastructure.cs:453-465`) sets only `Status` and `Budget`; its task row has `Cost 60` while the root row's `TotalCost` is 0 (PR callout 1; irrelevant now that `Fetch` keeps its re-run, but the new test seeds through Save rather than the mock).
- `CommonGotchas.cs`: gotcha 1 header `:30-47` ("RIGHT: Call RunRules at the end of your factory method"), regions `skill-computed-gap-rule` (`:70` → `rules-lifecycle.md:42`), `skill-create-without-run-rules` (`:83` → `rules-lifecycle.md:60`), `skill-create-run-rules` (`:97`); gotcha 4 `:330-363` (`PauseAllActions`; `:363` already says it is a different case); summary table `:500` (row 1 "await RunRules() at end"), `:509` (row 4).
- Design.Tests: `CommonGotchaTests.cs` — `Gotcha1_RulesDoNotFireDuringCreate` (:37), `Gotcha1_RulesFireAfterCreate_WithExplicitRunRules` (:54, region `skill-run-rules-forces` → `async.md:229`, `business-rules.md:799`, `validation.md:447`, `api.md:157`, skill `validation.md:483`), `Gotcha1_RunRulesInsideFactoryMethod_WorksWhilePaused` (:72, no region; intent "no IsPaused guard", also pinned by `AggregateLifecycleTests.cs:188-206` and `Gotcha4_RunRulesWhilePaused_WorksCorrectly`); `AggregateCoverageGapTests.cs:206-229` (region `skill-run-rules-after-create`, the rare validate case, renders into skill and docs `blazor.md` and `collections.md`); `WorkOrderAggregateTests.cs:55` ("Create ran the rules…", in `skill-can-x-test` → `domain-logic-placement.md:572`) and `:162` ("Fetch re-ran the root's own rules…", in `skill-parent-in-update-test` → `SKILL.md:470`) — both messages are correct doctrine and stay.
- Prose prescribing the blanket step: `skills/neatoo/SKILL.md:724,745`; `references/rules-lifecycle.md:17-18,80-103,118 (diagram, PR),149`; `references/validation.md:481-506,645 (PR; T10 word for word)`; `references/testing.md:77`; `references/collections.md:228`; `references/domain-logic-placement.md:553`; `references/pitfalls.md:28`; `docs/guides/async.md:227`; `business-rules.md:509,759-761 (PR)`; `collections.md:439`; `validation.md:445,476`; `docs/reference/api.md:155`; `src/Design/CLAUDE-DESIGN.md:156`. The rare validate case, already stated where it applies: skill `blazor.md:150`, docs `blazor.md:173` (PR callout 4) and the `skill-run-rules-after-create` region.
- Every other `RunRules(All)` in skill, docs, README and CLAUDE-DESIGN is a D6 gate (PR, listed in `reviews/003-plan-review.md`). Nothing in `Examples/Person` contradicts D18 and no snippet renders from it (PR Q6).
- #98 (open): asks for the doctrine statement with a `WorkOrder.cs` snippet, a Design.Tests test that a fetched WorkOrder with tasks is not modified, and a `CLAUDE-DESIGN.md` mention.

---

## Punchlist

- [x] Close #98 · GitHub · done when closed as done with a comment naming D18 and the three deliverables · AC-5 · Must (pulled from the todo, 2026-10-11; reframed from "moot" to "done" after the D18 clarification) — closed 2026-10-11 (`48c1098`)

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Fetched WorkOrder: unmodified, derived values intact incl. HasUnschedulableTasks | Must | `[integration]` | `AggregateTests/WorkOrderAggregateTests.Fetch_RecomputesTheRootsDerivedValues_LoadsTheChildAggregates_AndIsNotModified` — saves through the aggregate (one task unschedulable, over budget), fetches back, asserts `IsModified == false` on root and child, `CanApprove`/`IsHeld`/`IsOverBudget`/`Summary` equal to the saved instance, `TotalCost` 200, `HasUnschedulableTasks` true, task `IsSchedulable`/`Cost` | `[integration]` — Server-mode DI, real factories, `MockWorkOrderRepository` |
| Fetch targeted, in a region, pinned | Must | `[explicit-skip]` | `WorkOrder.cs` region `skill-fetch-computes-derived`: `RunRules(nameof(Status))`, `RunRules(nameof(Budget))`, no `All`; `HasUnschedulableTasks` loaded from the row; pinned by the test above (region `skill-fetch-computes-derived-test`) | skip honoured |
| Gotcha 1 statement pinned; remedy text no longer prescribes | Must | `[explicit-skip]` | `CommonGotchaTests` unchanged (`Gotcha1_RulesDoNotFireDuringCreate`, `Gotcha1_RulesFireAfterCreate_WithExplicitRunRules`, `Gotcha1_RunRulesInsideFactoryMethod_WorksWhilePaused` all pass); `grep -n "at the end" CommonGotchas.cs` → no prescription; summary row 1 reads "Compute what the operation needs: RunRules, or load it" | skip honoured |
| No prose prescribes the step; rare case where it applies | Must | `[explicit-skip]` | grep over skills/neatoo, docs/guides, docs/reference, README, CLAUDE-DESIGN, Design.Domain for `at (the) end of (the|your|any|every) factory method|all factory methods|must call await RunRules` excluding lines that negate it → 0 hits (one unrelated comment in `ServiceRegistration.cs:227`); rare case stated at skill `blazor.md:150`, docs `blazor.md:173`, `rules-lifecycle.md` doctrine paragraph, `CommonGotchas.cs` header, all pointing at `skill-run-rules-after-create` | skip honoured |
| #98 delivered and closed | Must | `[explicit-skip]` | #98 CLOSED 2026-10-11 with a comment naming D18 and the three deliverables (`business-rules.md` statement + `skill-fetch-computes-derived` snippet and test; the test; `CLAUDE-DESIGN.md:156`) | skip honoured |
| Build, tests, mdsnippets green | Must | `[explicit-skip]` | `reviews/003-build.log`: 0 errors, 0 warnings; `reviews/003-test.log`: 197 passed, 0 failed; `dotnet mdsnippets` exit 0, no missing-snippet lines, 0 duplicate regions | skip honoured |

---

## Gate Record

-

---

## Plan Amendments

### 2026-10-11 — Drafted from the stub

- **Original said:** rewrite the sites, replace two regions, keep one rare-case statement, close #98 as moot.
- **What changed:** the how for `WorkOrder.Fetch`: persist the root's derived values in the row and remove the `RunRules` calls; `Create` assigns what its state implies.

### 2026-10-11 — Re-drafted after the user's D18 clarification (plan review round 1 APPROVED with six callouts)

- **Section affected:** all
- **Original said:** remove `RunRules` from `WorkOrder.Create`/`Fetch` and gotcha 1's remedy; the plan treated any `RunRules` inside a `[Create]`/`[Fetch]` as the habit.
- **What changed:** the user: "You asked about adding a runrules at the end of every fetch and I said no. When did I say you cannot use RunRules in fetch?" and, of `WorkOrder.Fetch`'s targeted re-run, "That's a great case to use runrules." D18's row in `VisionAlignment.md` is clarified; T10 is reworded; the memory note records the over-correction. The habit is the *blanket* `RunRules(All)` step and the *validate* step, not `RunRules` inside an operation. `WorkOrder.Fetch` keeps its re-run, becomes the T10 example and gains a region; `HasUnschedulableTasks` is added to what it produces (callout 2). `Create`'s `All` stays with a why (callout 5 moot: `Summary` is computed). Gotcha 1 keeps `CreateWithRunRules`; only the prescription changes (callout 3's missing prose lines added to the inventory; the "no IsPaused guard" test's real intent recorded). Callout 1 (seed) and 6 (persisting formulas) are moot with the re-run kept. Callout 4: the rare validate case is stated where it applies, not "exactly one place". #98 is fulfilled, not closed as moot.
- **Why:** the first draft over-corrected from the 2026-10-09 miss.
- **Discovery Log:** 2026-10-11 / SKR-003

---

## Notes

- Skills consulted at drafting: `skills/neatoo/references/rules-lifecycle.md`, `validation.md` read from the repo (not loaded via the Skill tool).
