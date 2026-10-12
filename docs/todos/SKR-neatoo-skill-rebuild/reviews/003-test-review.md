# Test Review — SKR-003 — Stop teaching end-of-operation RunRules as a step

**Reviewer:** test-reviewer
**Logs:** `003-build.log` (0 errors, 0 warnings), `003-test.log`

---

## Round 1 — 2026-10-11 (HEAD `a8f9299`, 197 tests)

**Verdict: CONCERNS** (no veto, no Must-cover; one Should-cover)

### Veto-tier

None. `git diff skr-arc..HEAD -- src/Design/Design.Tests` shows only the one new test appended to `WorkOrderAggregateTests.cs`; `CommonGotchaTests.cs` unchanged.

### Must-cover

None — every Must bullet pinned at its declared tier.

- Bullet 1: the test has teeth — dropping `Fetch`'s targeted `RunRules` would break `CanApprove`/`IsOverBudget`/`Summary`; before this plan it would have failed on `HasUnschedulableTasks` (neither loaded nor matched by the two targeted calls).
- Bullet 2: region read — only targeted calls, no `All`, `HasUnschedulableTasks` loaded from the row.
- Bullet 3: all three gotcha-1 tests unchanged and green; `grep "at the end" CommonGotchas.cs` → 0; summary row 1 as stated.
- Bullet 4: the map's grep run — two lines, one negating the habit (`rules-lifecycle.md:104`), one unrelated (`ServiceRegistration.cs:227`); rare case stated at skill `blazor.md:150` and docs `blazor.md:173`; `skill-run-rules-forces` renders nowhere.
- Bullet 5: #98 CLOSED (COMPLETED); `skill-fetch-computes-derived` and its test render into `business-rules.md`; `CLAUDE-DESIGN.md:156` names #98.
- Bullet 6: logs green.

### Should-cover

1. **The new test could not tell targeted `RunRules` from `RunRules(All)`.** A child is marked modified only through a value-changing write (`ValidateProperty.HandleNonNullValue` `isDiff`, `AreSame` by value; `EntityPropertyManager.OnPropertyChanged`). With the graph saved through the aggregate (D6 gate ran `All` at Insert), `All` in `Fetch` writes the same values back and marks nothing. The test's name and comment claimed more than it could detect. Options: reword, or add a test seeding a task row whose `Discount` differs from the root's (would fail under `All`; caveat: inconsistent persisted data). **Triage: option (ii)** — `Fetch_TargetedRunRules_DoesNotWriteTheFinishedChildren` added; mutation-checked by the orchestrator (`Fetch` with `All` fails exactly this test); comment says divergence is what a changed rule leaves behind in old rows.

### Tech-debt

- Prose said flatly that `All` from a root "marks its just-fetched children modified"; true only when a root rule *changes* a child's value. Passed to code review. **Reworded everywhere.**

### Read report

Beyond the brief: `WorkOrderTask.cs`; `ValidateProperty.cs:150-367`, `EntityPropertyManager.cs:30-69`; `ValidateBase.cs:836-905`; #98 body; the todo's Dismissed section. Named but unused: VisionAlignment D18 row, todo T10 (the brief's calibration was enough); `003-plan-review.md` bodies (grep only).

---

## Round 2 — 2026-10-11 (HEAD `8b697b2`, 198 tests)

**Verdict: CLEAN**

- Should-cover 1 closed. The seed uses the unchanged `MockWorkOrderRepository` helpers; root `Discount` 0, task `Discount` 0.25. The targeted calls run only the four root rules, none of which writes a task. Under `All` the `Discount` orchestrator (`WorkOrder.cs:91-99`) would write 0 over 0.25 — a real `isDiff` on an unpaused task — and both `AreEqual(0.25m, …)` and `IsFalse(Tasks[0].IsModified)` catch it. The orchestrator's mutation check agrees. The seed's inconsistency (row `Cost` 60 vs `Discount` 0.25; root `TotalCost` 0) does not weaken the test: targeted `Fetch` recomputes none of those.
- Map rows 100–101 honest: row 1 pins the values and the hazard; row 2 pins targeted-not-`All`. Both tests inside `skill-fetch-computes-derived-test`.
- Sacred tests intact: Design.Tests has only additions (+63 lines, all in `WorkOrderAggregateTests.cs`). The tech-debt wording is fixed.
- No findings at any tier.
