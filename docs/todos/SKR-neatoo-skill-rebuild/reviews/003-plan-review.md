# Plan Review — SKR-003 — Stop teaching end-of-operation RunRules

**Reviewer:** plan-reviewer
**Draft reviewed:** `e5fe93d` on `skr-003-no-end-of-operation-runrules` (first draft: remove `RunRules` from `WorkOrder.Create`/`Fetch`, persist derived values in the row)

---

## Round 1 — 2026-10-11

**Verdict: APPROVED** (no veto-tier; six Must callouts)

### Pass A

Docs consulted: VisionAlignment §0 (D6, D18, D20), todo (AC-5, T10, Out of Scope, Dismissed), CLAUDE.md (project and global), `design-snippets.md`, `docs.md`, #98.

No veto. The draft applied D18 as then written and kept the D6 gate separate. Every other `RunRules(All)` in skill, docs, README and CLAUDE-DESIGN is a D6 gate: `SKILL.md:88,106,617`; skill `entities.md:487,522`; docs `entities.md:458,899,1097,1115`; `remote-factory.md:171`; `validation.md:716`; `api.md:1525,1560`; `getting-started.md:259,277`; `README.md:183,201`; `CLAUDE-DESIGN.md:195,517`.

### Pass B

Files examined: `WorkOrder.cs`, `WorkOrderTask.cs`, `WorkOrderTaskList.cs`, `CommonGotchas.cs`, `OrderItem.cs`; framework `ValidateBase.cs:770-890`, `EntityPropertyManager.cs:30-60,180-210`, `ValidatePropertyManager.cs:305-330`; tests `WorkOrderAggregateTests.cs`, `WorkOrderViewModelTests.cs`, `CommonGotchaTests.cs`, `TestInfrastructure.cs:442-490`, `AggregateLifecycleTests.cs:188-206`, `AggregateCoverageGapTests.cs:206-229`; `OrderEditViewModel.cs`, `WorkOrderPage.razor`, `InMemoryWorkOrderRepository.cs`; `RunRules` grep across skill/docs/CLAUDE-DESIGN/README; `Person.cs`, `PersonPhoneList.cs`.

**Reality check.** Assigning the private-set partials inside a paused `Fetch` is legal and clean: `EntityPropertyManager.cs:43` skips modified-marking while paused, no rule fires, and `FactoryComplete` resumes without running rules (`ValidateBase.cs:798-806`, `ValidatePropertyManager.cs:316+`), so the child-writing rules (`WorkOrder.cs:91-99`, `:103-111`) cannot fire. `Summary` is the four-trigger rule at `:117-122`. `WorkOrderViewModelTests.cs:59` holds either way.

**Q3 (gotcha-1 tests):** `Gotcha1_RunRulesInsideFactoryMethod_WorksWhilePaused` (`:72`) pins "`RunRules` has no `IsPaused` guard inside an operation", not "a created object's derived values are right" as the draft's Constraints said; the fact is also pinned by `AggregateLifecycleTests.cs:188-206` and `Gotcha4_RunRulesWhilePaused_WorksCorrectly`. Regions and render sites: `skill-computed-gap-rule` (`CommonGotchas.cs:70` → `rules-lifecycle.md:42`); `skill-create-without-run-rules` (`:83` → `rules-lifecycle.md:60`); `skill-create-run-rules` (`:97` → `SKILL.md:726`, `rules-lifecycle.md:82`, `business-rules.md:511`); test region `skill-run-rules-forces` (`CommonGotchaTests.cs:52` → `async.md:229`, `business-rules.md:799`, `validation.md:447`, `api.md:157`, skill `validation.md:483`); `skill-create-with-run-rules` (`WorkOrder.cs:138` → `domain-logic-placement.md:555`); the `:72` test is in no region.

**Q4:** Gotcha 4 is a separate case (restores what a user-chosen pause suppressed, outside any operation); `:363` already says so. Leave it.

**Q5:** Spot-checked 14 inventory lines; all teach the habit. Missing lines in callout 3.

**Q6:** `Person.cs:106,137` are D6 gates; `PersonPhoneList.cs:36` a list verb. Nothing in Person contradicts D18; no snippet renders from `Examples/`.

### Callouts (all Must)

1. **A WorkOrder test fails after Step 1.** `SeedWorkOrder` (`TestInfrastructure.cs:453-465`) sets only `Status` and `Budget`; once `Fetch` loads `CanApprove` from the row, `WorkOrderAggregateTests.cs:162` fails; the seed's `TotalCost` 0 vs task `Cost 60` is inconsistent. **Disposition: moot** — the re-draft keeps `Fetch`'s re-run; the new test seeds through Save.
2. **`HasUnschedulableTasks` left out of the row and of bullet 2.** A root derived value today's targeted `RunRules` does not compute either (existing gap); after a Load, `WorkOrderPage.razor:36` shows no banner. **Disposition: amended** — Step 1 makes `Fetch` produce it; bullet 1 names it.
3. **Habit text missing from the inventory.** Assertion messages in rendered regions (`WorkOrderAggregateTests.cs:55` → `domain-logic-placement.md:572`; `:162` → `SKILL.md:470`); prose `skills/neatoo/references/validation.md:645` (T10 word for word), `rules-lifecycle.md:118` (diagram), `docs/guides/business-rules.md:759-761`, `docs/guides/blazor.md:173`. **Disposition: amended** — added to Current State; the two assertion messages are correct doctrine after the clarification and stay.
4. **The rare case is already stated in four rendered places** (`skill-run-rules-after-create` region → skill and docs `blazor.md`, `collections.md`; the Blazor prose describes a real case: `Order.Create` leaves required fields empty, so an empty form is `IsSavable` and Save hits the server gate). "Exactly one place" too strong. **Disposition: amended** — stated where it applies; `rules-lifecycle.md` gets the doctrine sentence pointing at the region.
5. **`Summary` null after Create** if Create only assigns `Status` and `CanApprove`; the over-budget alert would read "Over budget: ". **Disposition: moot** — Create keeps `RunRules(All)`.
6. **Persisting derived facts makes the row a second home for each formula**; `Summary` and `CanApprove` as columns; stale after a formula change. Advisory alternative: assign from loaded inputs. **Disposition: moot** — the re-draft keeps the re-run.

### Theoretical (not triaged)

- `ValidateBase.cs:777`: the `PauseAllActions` XML doc example says "Rules run automatically when disposed", contradicting gotcha 4 and `ResumeAllActions`. Framework code, out of scope.
- `WorkOrder.Fetch` had no region, so the root shape did not render. **Taken up:** Step 1 adds one.
- The #98 close comment said "moot" while Current State said the plan supplies all three asks. **Taken up:** #98 is fulfilled and closed as done.

### Read report

Beyond the brief: `design-snippets.md`; `TestInfrastructure.cs` (`MockWorkOrderRepository`); `AggregateLifecycleTests.cs:188-206`; `AggregateCoverageGapTests.cs:206-229`; `OrderEditViewModel.cs`; the framework pause/resume code; the placeholder grep across all markdown. Named but unused: the memory note; Plan Index rows other than 003.

### Orchestrator note

While this review was in flight the user corrected the plan's reading of D18: "You asked about adding a runrules at the end of every fetch and I said no. When did I say you cannot use RunRules in fetch?" and, of `WorkOrder.Fetch`'s targeted re-run, "That's a great case to use runrules." The plan was re-drafted around that (Amendments, 2026-10-11 second entry); D18's row and T10 were clarified; the memory note records the over-correction. Round 2 reviews the re-draft.
