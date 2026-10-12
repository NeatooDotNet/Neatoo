# Code Review — SKR-003 — Stop teaching end-of-operation RunRules as a step

**Reviewer:** code-reviewer (opt-in, per-plan)
**Logs:** `003-build.log` (0 errors, 0 warnings), `003-test.log`

---

## Round 1 — 2026-10-11 (HEAD `a8f9299`, 197 tests)

**Verdict: CLEAN** (five Must callouts, none blocking)

### Direction & shape

- `WorkOrder.cs`: `Create` keeps `RunRules(All)` and says why it is safe (no children yet); `Fetch` keeps its two targeted calls, now in `skill-fetch-computes-derived`; `HasUnschedulableTasks` persisted like `TotalCost` (row, `MapTo`, `Fetch`).
- `CommonGotchas.cs`: gotcha 1 still states the gotcha and still shows `CreateWithRunRules`; only the remedy text and summary row 1 changed. Gotcha 4 untouched.
- No step, no ban; D6 gates unchanged. Rare validate case stated in skill and docs `blazor.md` as the `[Create]`'s own choice.
- No Dn/Tn IDs in `.cs` comments; DDD terms used, not explained. No old placeholders left.
- **Update path for `HasUnschedulableTasks` complete:** a task's `Hours` change runs the task's `IsSchedulable` rule → fires the root rule triggered by `Tasks![0].IsSchedulable` while the root is unpaused → root property modified if the value changed → `IsSelfModified` → `MapTo`. Trigger-less changes (removing a task) are covered by the gate's `RunRules(All)` at `:222` before the `IsSelfModified` check at `:231`.

### Veto-tier

None.

### Callouts (all AC-5 · Must)

1. **The new test passed with `RunRules(All)` too, and the comments overstated the cause.** Same-value writes are no-ops (`ValidateProperty.HandleNonNullValue` `isDiff` at `:240`, `OnPropertyChanged(Value)` only at `:294`; `EntityPropertyManager.cs:43-46`). With consistent saved data the `Discount` orchestrator and the `IsOnHold` re-run write what was loaded. "A root rule that wrote them would mark them modified" is true only for value-changing writes; same wording in rules-lifecycle.md, SKILL.md, business-rules.md, CLAUDE-DESIGN.md. **Triage: fixed both ways** — wording now "a root rule that changes a child's value marks that child"; and `Fetch_TargetedRunRules_DoesNotWriteTheFinishedChildren` added (divergent task `Discount`), mutation-checked.
2. **`business-rules.md` showed `CreateWithRunRules` twice**, the second under "Forcing a run after `Create`" — a lead-in written for the old caller-side test that now reads like the T10 habit. **Triage: fixed** — replaced by a pointer sentence to Rule Execution Order.
3. **`collections.md` (skill and docs) lead-ins contradicted their snippet**: the prose said `RunRules()` is for an operation's derived values, the rendered `skill-run-rules-after-create` test calls it after `Create` to read `IsValid` — the rare validate case; `testing.md:77` said a test never validates while the skill renders that test. **Triage: fixed** — lead-ins name the rare case.
4. **`rules-lifecycle.md` `RunRulesFlag.All` row gave the wrong reason the D6 gate is safe** ("no finished child can be written"): pausing does not cascade, children are unpaused at the top of `Update` and the `Discount` rule writes them; the gate is harmless because the save re-baselines them. **Triage: fixed.**
5. **The "whose rules write children" qualifier was dropped** at `rules-lifecycle.md:104` and docs `validation.md:476`, reading as a ban on `All` in any root `[Fetch]`. **Triage: fixed.**

### Theoretical (not triaged)

- Plan Index row 003 said Draft. **Fixed.**
- `skill-run-rules-forces` renders nowhere (allowed by the amendment).
- docs `async.md` "RunRules with Async" now shows a sync-rule `Create`. For plan 005.

### Read report

Beyond the brief: `WorkOrderTask.cs`; `ValidateProperty.cs:160-370`; `EntityPropertyManager.cs:30-70`; `ValidateBase.cs:780-790, 879-905, 974-989`; `ValidatePropertyManager.cs:308-320`; `AggregateCoverageGapTests.cs:195-230`; #98 body and close comment; `todo.md:1-110`.

---

## Round 2 — 2026-10-11 (HEAD `8b697b2`, 198 tests)

**Verdict: CLEAN** (one new minor callout)

- 1–5 resolved, each checked at every listed site. The new test tells the forms apart: under `All` the orchestrator writes 0 over the loaded 0.25 — a real change — and the asserts fail; targeted calls never fire the `Discount` rule. Its comment presents the mismatched row as one a rule change left behind, consistent with "persisted data is assumed valid".
- **New (AC-5 · Must):** the second test sits inside `skill-fetch-computes-derived-test`, so `business-rules.md`'s snippet shows both tests under a singular lead-in. **Triage: fixed** — lead-in made plural with a clause for the second test.
- Theoretical: `CommonGotchas.cs:51` over 80 columns. **Wrapped.**
