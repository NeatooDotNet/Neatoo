## Close-Out Audit — 2026-10-12

**Grade: C**

The grade comes from one container item: a deferral that was dropped (veto-tier, below). Every Acceptance Criterion traces to code or artifacts and holds, and the build and tests are green.

### Acceptance Criteria Trace
| AC | Word | Evidence (file:method:line) | Holds? | If not: accepted gap? |
|---|---|---|---|---|
| AC-1 | Must | `skills/neatoo/SKILL.md:13` `## The model` (statements 1–4, then the RF pointer at `:23`); trap table `:29–40`, one row each for T1, T2, T7, T8, T9, T10, T11-Neatoo, T12, T16 and T17; RF pointer block `:42` lists the eleven questions with no answers; decisions `:44–304` (placement table `:104`, UI contract `:127`, interface-first `:286`, two rule shapes `:290`, duality `:294`, record vs input model `:298`, server gate `:302`). Read against D1–D19 (plus D20, D21, D23–D26): D1/D21 `:38,292`; D2 `:32`; D4/D20 `:33`; D6 `:304` ("the framework does not run it for you"); D16 `:34`; D17/D25 `:39,300`; D18 as clarified `:36`; D19 `:288`; D23 `:296`; D24 `:315`. No contradiction found. | Yes | — |
| AC-2 | Must | Fenced-block sweep of `skills/neatoo/**` (run during this audit): every block outside a `<!-- snippet -->` pair is WRONG-labelled (`blazor.md:31`; `domain-logic-placement.md:32,72,257,274,475,572,604`; `entities.md:722,736`; `pitfalls.md:36`) or not C# (`blazor.md:259`, `trimming.md:69,81` XML; `entities.md:501`, `rules-lifecycle.md:110` diagrams). One exception: `lazy-loading.md:251`, a hand-written Razor block. The todo's Out of Scope gives the lazy-load surface, `lazy-loading.md` included, to its own todo, and D24 forbids building a region for it. | Yes, with the Out of Scope carve-out for `lazy-loading.md:251` | — |
| AC-3 | Must | Aggregate: `Order`/`WorkOrder` (`AggregateTests/*`). Input model: `AllBaseClasses.cs:93` `#region skill-input-model`, pinned by `ValidateBaseTests.PropertyObject_CarriesWhatTheInputModelPageBinds`. Read model: `ReadModels/EmployeeDirectory.cs:41-113`, pinned by `EmployeeDirectoryTests` (3 methods). Entity reused by a job: `Jobs/SubmitDraftOrders.cs:51-116` `_Run`; it calls no `RunRules`, decides on `IsSavable` / `!IsValid` / skip, and reassigns the result. Pinned by `SubmitDraftOrdersTests.Run_ValidInvalidAndUnchangedOrders_SavesReportsAndSkipsWithoutThrowing` (read: asserts saved, rejected with message, unchanged). Rule with an interface-factory domain service: `Rules/DomainServiceRule.cs:49` `ShippingQuoteRule(IShippingRateService)` over `Services/ShippingRateService.cs:26-57`, pinned by `ShippingQuoteRuleTests` (2 methods). | Yes | — |
| AC-4 | Must | `Design.App/ClientServices.cs:27-56` `AddDesignClient`: `AddNeatooServices(Remote)` and MudBlazor. The keyed `HttpClient` sits outside the region (`:62-67`) and the comment points at the RF skill. `Design.Server/ServerServices.cs:38-61` `AddDesignServer`: `AddNeatooServices(Server)`, with the #117 hazard in the header. Both render in `references/blazor.md:528,563`. UI contract: `skill-save-handler` (`SKILL.md:131`, `blazor.md:291`), `skill-viewmodel-over-aggregate` (`blazor.md:366`), `skill-page-binds-facts`. MudNeatoo: the `skill-mud-*` regions. CI: `Design.sln` holds Design.App and Design.Server, and `.github/workflows/build.yml:88-96` builds and tests `Design.sln`. The pair is pinned by `ClientServerPairTests.Order_CreatedOnTheClient_SavesThroughTheServerAndFetchesBack` and `Client_IsInRemoteMode_TheCallLeavesThroughTheKeyedHttpClient` (read: `Calls` goes 0 → 1). | Yes | — |
| AC-5 | Must | Grep over `skills/`, `docs/guides`, `docs/reference`, `docs/getting-started.md`, `README.md`, `CLAUDE-DESIGN.md` and Design.Domain for end-of-operation or all-factory-method `RunRules` prescriptions. The only hits negate the habit (`rules-lifecycle.md:104`, `pitfalls.md:26`) or are unrelated (`AsyncRules.cs:256`, about recovery after cancellation; `ServiceRegistration.cs:227`). `WorkOrder.cs:153-193` `Fetch` runs trigger-named `RunRules` only, pinned by `WorkOrderAggregateTests.Fetch_TargetedRunRules_DoesNotWriteTheFinishedChildren`. | Yes | — |
| AC-6 | Should | `skills/mudneatoo`: 25 placeholders, all filled. 22 render from Design.App (`EmployeeEdit.razor` ×15, `InputModelPage.razor` ×2, `OrderEdit.razor`, `WorkOrderPage.razor`, two ViewModels, `_Imports.razor`). The 3 Design.Domain ones are C# rules, not Blazor. Every other fenced block is WRONG-labelled (sweep: `SKILL.md:60`, `aggregate-reactive-vm.md:9`, `anti-patterns.md` ×9). | Yes | — |
| AC-7 | Should | `SKILL.md:306-315` Known pains (Neatoo#103, the three pending analyzers, RemoteFactory#117, LazyLoad being removed) and `references/pains.md:7-24` (#103, #96, #89, #95, #93, RF#117, three analyzers). Covers every pain issue open at the time 005 was planned. #109 was filed at close (see callouts). | Yes | — |
| AC-8 | Should | `diff -rq skills/neatoo ~/.claude/skills/neatoo` and `diff -rq skills/mudneatoo ~/.claude/skills/mudneatoo`, run during this audit: no output, exit 0 for both. | Yes | — |
| AC-9 | Must | As narrowed 2026-10-12 (Discovery Log, user). `reviews/007-fresh-session.md`: five headless sessions, scored row by row against T1–T20. No Neatoo-owned row and no RF-owned row fallen into. Spot-checked: out1:540-566 (T1 shape, in-flight guard, reassign); every `RunRules` in out1/3/4/5/5b is a form `[Create]` or the D6 gate (out1:93,122,144; out5:92,123,136). Test review CLEAN (`007-test-review.md`). | Yes | — |
| AC-10 | Should | `SKILL.md` is 337 lines rendered. `## The model` at `:13`; trap table at `:25`. | Yes | — |

### Veto-Tier Findings

1. **Container item 5: an untraced deferral. Plan 005 deferred the RF-owned `pitfalls.md` rows to 007, and nothing picked them up.** Confidence: High.
   - **What 005 said:** `plans/005-references-as-mechanism.md:27` ("RF-owned rows that `pitfalls.md` still states (correct today, pointer-only cleanup belongs with 007's sync)") and `:145` Notes 3 ("turning them into pointers waits for the RF skill sync (007)").
   - **What 007 did:** its Scope, Steps, Punchlist and Amendments never mention `pitfalls.md`, and it closed Done.
   - **What else records it:** no todo Punchlist, Dismissed or Follow-on row.
   - **The rows are still there:** `skills/neatoo/references/pitfalls.md:15-19` (constructor `[Service]`, `[Remote]` on child operations, `[Fetch]` parameters, the Guid key, the root mapping child rows) still state RemoteFactory's answers, against the todo's "nothing is copied" ownership line (`todo.md:59`).
   - **Why it was never done:** the condition it waited for (the rebuilt RF skill) moved to the RF sibling on 2026-10-12, so the work has no owner in SKR.
   - **What must change:** give it a home, as a todo Follow-on row (drafted below) or a Punchlist row, or do the edit.

### Callouts (Must in full; ≤ 5 Should/Could)

Must-affecting: none.

1. **Container item 7: mudneatoo's EntityLazyLoad guidance was removed, though Out of Scope reserves it for the LazyLoad todo, and no Discovery Log entry authorizes the removal.** Confidence: Medium.
   - **Evidence:** `todo.md:69` lists "mudneatoo's `EntityLazyLoad`" among the lazy-load surface that is "its own todo". Plan 006 (Scope, `plans/006-mudneatoo-from-design-app.md:25`) removed the EntityLazyLoad databinding section, Anti-Pattern 9 and the LazyLoad triggers, leaving one sentence (`skills/mudneatoo/SKILL.md:466`). The neatoo skill took the other path and kept `lazy-loading.md` as pending removal.
   - **In its favour:** the same Out of Scope line also says SKR "copies none of it into the rebuilt skill". Plan 006 records the change in its Scope and Amendments, so the drift is not silent.
   - **Affects:** AC-6 (Should).
   - **Reachable by:** a developer on today's Neatoo binding `EntityLazyLoad` on a MudNeatoo page who consults mudneatoo. They get the "being removed" sentence. The binding still exists in neatoo's `lazy-loading.md:244-265`.
   - **Suggested disposition:** accept, with a one-line Discovery Log entry.
2. **Pains: Neatoo#109 is not named.** It was filed at 2026-10-12 20:12 CDT, after every plan had merged. Confidence: Medium.
   - **The pain:** `EntityListBase` has no tracked move. A reorder is invisible to change tracking, so the root needs a manual `MarkModified()`, and a future `MoveItem` override would queue a DELETE.
   - **Evidence:** neither `references/pains.md` nor `references/collections.md` mentions `Move` or reordering (grep).
   - **Affects:** AC-7 (Should).
   - **Reachable by:** a developer reordering children in an aggregate list. zTreatment CUST-004 hit it, per the issue.
   - **Suggested disposition:** Follow-on.

### Theoretical (not triaged)
- `CLAUDE.md:111` and `docs/guides/remote-factory.md:56` say a created object is `IsSavable = true` with no qualifier, and `Order.Create` is now a counterexample. This was the 004 plan reviewer's theoretical, noted "(005/007 wording)" in 004's amendments and not taken up.
- #94 reads as addressed by 006 (mudneatoo no longer says Min/Max pass through) but is still open. #88 (setting a value back does not clear `IsModified`) is not mentioned in `skills/neatoo`. Both carry the `skill` label and are gaps rather than framework pains, so they fall outside AC-7.
- Plan headers 004–007 still read `PR: —`, and their `Last Updated` predates implementation. The Plan Index carries the merged PRs, so container item 9 holds.
- The 007 test review's note that S5's prompt weakens the T7 evidence stands. It is not re-raised here.

### Build & Test
- **Build:** PASSED, both solutions.
  - `reviews/final-build.log` (`Neatoo.sln`): 0 errors, 88 warnings, all in `Neatoo.UnitTest` and `Person.DomainModel.Tests`. The arc's diff touches neither project nor any framework project; it changes only `src/Design/**`, `skills/`, `docs/`, `.claude/rules/`, `CLAUDE.md` and `README.md`.
  - `reviews/final-design-build.log` (`Design.sln`): 0 errors, 0 warnings.
- **Tests:** 2,158 passed, 0 failed, 2 skipped.
  - `reviews/final-test.log`: 1,952 passed (42 BaseGenerator, 55 Person.DomainModel, 1,855 UnitTest). The 2 skipped are `AsyncFlowTests_CheckAllRules` and `FatClientValidate_Deserialize_SharedDictionaryReference`, both in `Neatoo.UnitTest`, which the arc does not touch.
  - `reviews/final-design-test.log`: 206 passed.
- **Sacred tests:** every assertion removed in the `Design.Tests` diff is accounted for:
  - the `ShowHoldBanner` → `IsHeld` rename;
  - the in-scope `AggregateLifecycleTests` Arrange;
  - `AddThenRemoveNewItem` moved verbatim in intent to `SaveAggregateLifecycleTests.cs:140`, with the user's go-ahead.

### Container
- **Plans issued / cap:** 7 / 11.
- **Status cells over budget:** 0.
- **Untraced deferrals:** plan 005 → 007, the RF-owned `pitfalls.md` rows (the veto above).
- **Prose over budget:** Goal 107 words, within budget. All seven Discovery Log entries exceed 60 words. The worst is "2026-10-11 — SKR-003 D18 clarified" at 163.
- **Open PRs into the arc:** None. #100–#108 are all MERGED into `skr-arc`, and #98 is closed.
- **Must accepted as gap without demotion:** None.
- **Test Evidence:** every behavioural Acceptance bullet on the seven plans has a row, and every cited test method exists (grep). The one renamed method, `AddThenRemoveNewItem_LeavesAggregateCleanButSavable`, is recorded in 004. No `MISSING` rows. Every Gate Record has at most two rounds.

### Follow-on (draft for Step 8)
- Turn `references/pitfalls.md:15-19` (RF-owned rows) into pointers once the RF skill rebuild lands (from 005, not traced) · AC-1 · Must.
- Neatoo#109 tracked move: a `pains.md` row with the `MarkModified()` workaround · AC-7 · Should.
- Out of Scope: move the RemoteFactory pin 1.9.0 → 1.10.3. The same task withdraws "`[Execute]` must return `Task<T>`" (`ApproveEmployee.cs:50,170`, rendered into `references/base-classes.md`).
- Out of Scope: remove the lazy-load surface (D24), including `lazy-loading.md:251`, the last hand-written non-WRONG block in `skills/neatoo`.
- Out of Scope: the three analyzers. The skill links each as it lands (`SKILL.md:311-313`, `pains.md:20-24`).
- RF sibling Todo 2: its own fresh-session test against the rebuilt RF skill (Discovery Log 2026-10-12).
- Doc debt: unqualified "after `[Create]`: `IsSavable = true`" at `CLAUDE.md:111` and `docs/guides/remote-factory.md:56`.
- Accepted at the 002 code review: Design.Infrastructure's singleton stores are unsynchronized (header note).
- Optional, per 007 Scope ("if wanted"): a repeatable `claude plugin eval` suite.

### Read report
- **Read beyond the brief:**
  - GitHub, read-only: `gh issue list` (which found #109), `gh issue view 94/109`, `gh pr view` for #100–#108, #98's state.
  - Files: `skills/neatoo/references/pitfalls.md` and `pains.md`; `lazy-loading.md:240-265`; `AsyncRules.cs:235-275`; `ClientServices.cs` and `ServerServices.cs`; `SubmitDraftOrders.cs` and its test.
  - Diffs and sweeps: `git diff main..skr-arc` of `Design.Tests` for removed assertions; a fenced-block sweep of both skills; spot reads of `007-runs/out1.md` and `out5.md`.
- **Named but unused:**
  - The 001–006 plan, test and code review files. Only grepped for verdicts and dispositions; the plans' Gate Records were relied on.
  - `src/Design/CLAUDE-DESIGN.md` and `.claude/rules/docs.md` (loaded, nothing turned on them).
  - VisionAlignment beyond §0.

---

## Close-Out Audit (Re-Audit) — 2026-10-12

**Grade: A**

Re-audit after PR #110 (`747b4e6`, merged into `skr-arc` at `aa09e89`). `gh pr list --state open --base skr-arc` is empty. The previous entry's veto is resolved by a cited change. Both callouts are disposed: one fixed, one accepted by the user and recorded. Every Acceptance Criterion still traces and holds.

### Previous findings
| Previous finding | Change | Verified |
|---|---|---|
| **Veto:** 005 → 007 untraced deferral, RF-owned rows in `pitfalls.md` | `skills/neatoo/references/pitfalls.md:15-19` now point at the RemoteFactory skill for the RF answers. They keep only the Neatoo half: `:15`, the rule that reaches the server (command delegate or interface-factory domain service → `validation.md`); `:19`, the list coordinating with `DeletedList` (→ `entities.md`). Discovery Log, "SKR close-out audit (grade C)": Fix. | Yes. I read the diff `16ed47d..aa09e89` and the rows; no RF answer is restated. |
| **Callout 1:** mudneatoo LazyLoad removal without a log entry (AC-6, Should) | Accepted by the user and recorded: Discovery Log, "SKR close-out audit (grade C)" ("Out of Scope already says SKR copies none of it"). | Yes. Container item 7 is now satisfied by an authorizing entry. |
| **Callout 2:** Neatoo#109 unnamed (AC-7, Should) | `references/pains.md:12`, an Open Defects row: symptom, the issue link, and the workaround (`Move`, then `MarkModified()` on the root; not `Remove` then `Insert`). | Yes. It matches the issue body. |
| **Theoretical:** plan headers `PR: —` | Plans 004–007 now read `**PR:** #105`–`#108`. | Yes. See Theoretical below for a rendering side effect. |

**User correction, outside the findings:** validating what an operation hands back is now "the developer's choice; the framework never makes it", not "rare". Discovery Log, "'Rare' is the wrong word".
- **Where it changed:** `SKILL.md:36`; `collections.md`, `blazor.md`, `rules-lifecycle.md` and `testing.md`; the docs guides; `Order.cs:114-119`, `CommonGotchas.cs:52` and `OrderEditViewModel.cs:20`; VisionAlignment D18 (`:39`) and D26.
- **Grep:** `rare validate|the rare case` outside `docs/todos` returns nothing. Of the remaining whole-word "rare" hits, `SKILL.md:100` and `VisionAlignment.md:185` are about `LoadValue` and `MarkModified`, and `VisionAlignment.md:39` records the clarification. None is about validation.
- **Effect on the criteria:** AC-1's D18 read and AC-5 still hold against the revised D18 and D26.
- **Re-render:** mdsnippets re-rendered every snippet that embeds `Order.cs` (line ranges shifted by one across `entities.md`, `collections.md` and the docs).

### Acceptance Criteria Trace
| AC | Word | Evidence (file:method:line) | Holds? | If not: accepted gap? |
|---|---|---|---|---|
| AC-1 | Must | Unchanged from the previous entry, except `SKILL.md:36`, reworded to match D18 as clarified 2026-10-12. | Yes | — |
| AC-2 | Must | Unchanged. #110 added no fenced block. `lazy-loading.md:251` remains under the Out of Scope / D24 carve-out. | Yes, with the carve-out | — |
| AC-3 | Must | Unchanged. #110 touched only comments in `Order.cs` and `CommonGotchas.cs`. | Yes | — |
| AC-4 | Must | Unchanged. The `OrderEditViewModel.cs:20` change is comment-only. `Design.sln` builds 0/0 with 206 passing (regenerated logs). | Yes | — |
| AC-5 | Must | Re-grepped: no end-of-operation prescription. The validate case is now worded as the developer's choice everywhere. | Yes | — |
| AC-6 | Should | Unchanged. | Yes | — |
| AC-7 | Should | `SKILL.md:306-315` and `pains.md:7-25`, now including Neatoo#109. `gh issue list`: no pain issue filed since. | Yes | — |
| AC-8 | Should | `diff -rq` on both installed copies, run during this audit after the re-sync: no output, exit 0 for both. | Yes | — |
| AC-9 | Must | Unchanged. #110's skill edits are wording and pointers; no trap answer changed direction. | Yes | — |
| AC-10 | Should | `SKILL.md` still 337 lines; the table is at `:25`. | Yes | — |

### Veto-Tier Findings
None.

### Callouts (Must in full; ≤ 5 Should/Could)
None open. Prose budget (container item 8) is unchanged and listed under Container. It does not bear on any criterion.

### Theoretical (not triaged)
- Plans 004–007 lost the blank line between `**PR:** #10x` and `---`. In CommonMark that turns the header block into a setext H2 heading when rendered. The text is intact and plans 001–003 keep the blank line.
- `CLAUDE.md:111` and `docs/guides/remote-factory.md:56` still say `IsSavable = true` after `[Create]` with no qualifier. Carried from the previous entry; on the Follow-on draft.

### Build & Test
- **Build:** PASSED.
  - `reviews/final-design-build.log` (`Design.sln`, regenerated 20:30, around commit `747b4e6`): 0 errors, 0 warnings.
  - `reviews/final-build.log` (`Neatoo.sln`): unchanged from the previous entry, 0 errors and 88 warnings in untouched projects. #110 changed no file in any `Neatoo.sln` project; its only code files are three Design comment edits.
- **Tests:** 2,158 passed, 0 failed.
  - `reviews/final-design-test.log`: 206 passed, 0 failed.
  - `reviews/final-test.log`: 1,952 passed, 0 failed, 2 skipped (pre-existing).

### Container
- **Plans issued / cap:** 7 / 11.
- **Status cells over budget:** 0.
- **Untraced deferrals:** None. The 005 deferral was done in #110.
- **Prose over budget:** the seven older Discovery Log entries are still over 60 words. The worst is "2026-10-11 — SKR-003 D18 clarified" at 163. The two new entries are within budget per section.
- **Open PRs into the arc:** None. #110 is MERGED into `skr-arc`.
- **Must accepted as gap without demotion:** None.

### Follow-on (draft for Step 8)
- Out of Scope: move the RemoteFactory pin 1.9.0 → 1.10.3. The same task withdraws "`[Execute]` must return `Task<T>`" (`ApproveEmployee.cs:50,170`, rendered into `references/base-classes.md`).
- Out of Scope: remove the lazy-load surface (D24), including `lazy-loading.md:251`.
- Out of Scope: the three analyzers. The skill links each as it lands (`SKILL.md:311-313`, `pains.md`).
- RF sibling Todo 2: its fresh-session test against the rebuilt RF skill (Discovery Log 2026-10-12).
- Doc debt: unqualified "after `[Create]`: `IsSavable = true`" at `CLAUDE.md:111` and `docs/guides/remote-factory.md:56`.
- Accepted at the 002 code review: Design.Infrastructure's singleton stores are unsynchronized (header note).
- Optional, per 007 Scope: a repeatable `claude plugin eval` suite.

### Read report
- **Read beyond the brief:**
  - `git diff 16ed47d..aa09e89` over every file #110 touched.
  - A whole-word "rare" grep beyond the given pattern.
  - `gh issue list`, for pains filed since.
  - The `cat -A` read of plan 004's header, which found the setext side effect.
  - Log timestamps against the commit time.
- **Named but unused:** none.
