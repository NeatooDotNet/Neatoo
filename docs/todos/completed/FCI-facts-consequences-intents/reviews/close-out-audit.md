## Close-Out Audit — 2026-10-16

**Grade: B**

Every Required criterion (AC-1 to AC-9) traces to code, tests and prose on `fci-arc`. AC-10 and AC-11 are Optional, not built, and recorded as `proposed` Parked rows. That is allowed while an unattended loop runs; SKILL.md Core Principle 8 and "The stop is where issues are approved" cover it. No veto-tier finding. The close-out build, the Design build and every test assembly are green.

### Acceptance Criteria Trace
| AC | Word | Evidence (file:method:line) | Holds? | If not: parked as? |
|---|---|---|---|---|
| AC-1 | Required | `docs/VisionAlignment.md:201` (statement 8), `:53-56` (D27–D30); `skills/neatoo/SKILL.md:19` (model statement 3), `:31` (first habit row), `:106-118`; `skills/mudneatoo/SKILL.md:236-238`, `:276`, `:282`; `references/aggregate-reactive-vm.md:3` ("derived values live in the aggregate, as rules"). No "who is driving it" is left in docs, skills or src outside history. | Yes. The MudNeatoo skill names consequences as "rules", not by the word (Medium). | — |
| AC-2 | Required | `src/Neatoo.BaseGenerator/Generators/PropertyGenerator.cs:84 GenerateAwaitableSetter` (class; `SetPrivateValue` for a private setter, `SetValue` otherwise; task tracked; Task returned), `:122` (interface, public-set only, skips get-only and an existing member); `Extractors/PropertyExtractor.cs HasMemberInHierarchy / InterfaceDeclaresMember / InterfaceDeclaresReadOnlyProperty`. Tests: `AwaitableSetterGenerationTests` (8), `AwaitableSetterTests` (5). | Yes | — |
| AC-3 | Required | `src/Neatoo.Analyzers/AwaitPropertyWriteAnalyzer.cs` (NEATOO011, `DiagnosticSeverity.Warning`; `IsFactoryOperation:253`, initializer exemption, `WriteContext`); `src/Neatoo.CodeFixes/AwaitPropertyWriteCodeFixProvider.cs:209` (`AddAction`→`AddActionAsync`). Tests: `AwaitPropertyWriteAnalyzerTests` (18). `close-out-design-build.log` has 0 NEATOO011. | Yes | — |
| AC-4 | Required | `src/Neatoo/ValidateBase.cs:314 RaiseIfChanged` records the name; `:355`, `:368 RunMetaPropertyRules` (does nothing while paused, task tracked, sync fault rethrown); `EntityBase.cs:269-279` feeds the entity's meta names through it. `src/Neatoo.Analyzers/AsyncRuleOnIsBusyAnalyzer.cs:70 IsIsBusyTrigger` (NEATOO012, Error, IsBusy and IsSavable). Tests: `MetaStateTriggerTests` (8), `AsyncRuleOnIsBusyAnalyzerTests` (5), `AsyncTasksCompletionCallbackTests`. | Yes | — |
| AC-5 | Required | `src/Design/Design.Domain/Aggregates/TrainingPlanAggregate/TrainingPlan.cs:96 Fit`, `:124 AnchorFit`, `:154 AddInterval`, `:168 Approve`, `:84-87` (CanApprove on `IsValid`); `Interval.cs:58 SlideMinutes`, `:72 LockMinutes`. Interface-first: concretes are internal, `ITrainingPlan : IEntityRoot`, `IInterval : IEntityBase`. Tests: `TrainingPlanTests` (16). The Neatoo and MudNeatoo skills quote its regions. | Yes | — |
| AC-6 | Required | `skills/neatoo/SKILL.md:42`, `:182` (awaited `Set{Name}`, NEATOO011 and NEATOO012), `:359` (`[Remote]` is per call, #87); `references/domain-logic-placement.md:1195-1196` (#114: a child may write its parent; writing a sibling through the parent is the anti-pattern; the paused parent); `references/entities.md:240`, `:249-267` and `pitfalls.md:31` (#88 sticky, region `skill-test-modified-is-sticky` pinned by `WritingTheOriginalValueBack_LeavesThePlanModified`); `rules-lifecycle.md:240-264` (safe and sync-only meta triggers, the loop symptom); #112's principle is at SKILL.md:108. No assigning sync `AddAction` in docs or skills (grep). | Yes | — |
| AC-7 | Required | `src/Neatoo.Blazor.MudNeatoo/Components/*.razor.cs` (11 inputs; for example `MudNeatooSlider.razor.cs:126` `Setter`, `:133-143`); `Design.App/Components/IntervalRow.razor:24` (`Setter="@Interval.SlideMinutes"`), `PlanSummary.razor`, `OrderItemEditor.razor`, `WorkOrderTaskEditor.razor`. Every page subscribes only to its root's own `PropertyChanged`. `skills/mudneatoo/SKILL.md:105-121` (forwarding table, #94), `:236-278` (verb-bound control), `:280-406` (unit of refresh). Tests: `SetterTests` (33), `TrainingPlanComponentTests` (4), `RowComponentTests` (5), `TrainingPlanViewModelTests` (4). | Yes, see Callout 3 | — |
| AC-8 | Required | `~/.claude/skills/RemoteFactory/SKILL.md:371` and `references/class-factory.md:368` carry FCI-001's wording verbatim; `SKILL.md:50` carries the restated pointer. RemoteFactory#178 MERGED, #173 CLOSED (2026-10-16, `gh`). | Yes | — |
| AC-9 | Required | `reviews/008-fresh-session.md` Run 2 tables; `008-runs/run.sh`; `run2/probe-tools.txt` (5 refusals, 1 skill read); no `NeatooPropertyChanged +=` in any run-2 answer (grep); `run2/out6.md:588` and `out7.md:898` spot-checked. The installed neatoo and mudneatoo match `skills/` now (`diff -rq`). | Yes | — |
| AC-10 | Optional | Not built: no boundary, throttle, `ValueLabelFormat` or `Converter` in the diff. | Not built | Parked row 5, `proposed` (AC-10; plans/009) |
| AC-11 | Optional | Not built: no non-remote-seam open in Design. | Not built | Parked row 4, `proposed` (AC-11) |

### Veto-Tier Findings
None.

### Callouts (Required in full; ≤ 5 Optional)

**Required-affecting**

1. **Standing orders. The loop did not stop on an out-of-scope test failure, or on a DECISION NEEDED verdict.** High.
   - **Where.** `reviews/006-test-review.md` round 1 is DECISION NEEDED on `AsyncFlowTests_Property_PropertyChangedEvents`, which failed in `006-test.log`. FCI-006 serves AC-1 and AC-6, both Required.
   - **The rule.** The Standing orders (todo.md:12, Keith) say such a failure "is a `proposed` Parked row and stops the loop". SKILL.md's Unattended stop conditions list "a `DECISION NEEDED` verdict on a plan serving a Required criterion".
   - **What happened.** The row was parked (Parked row 6), but #120, #121 and #122 were merged and the close-out reached afterwards. No `**Blocked:**` line was written, and nothing records Keith ruling.
   - **Why it is not veto-tier.** No deliverable contradicts a rule, the test is untouched, the close-out suite is green, and the decision is still open for Keith.
   - Affects: AC-6. Reachable by: observed. `006-test.log` shows 1 failed, and the merge times of PR #120 to #122 come after it.
2. **Standing orders. A public API change beyond the three decided ones is recorded nowhere for Keith's review.** Medium.
   - **The change.** The `Setter` parameter on all eleven MudNeatoo inputs (for example `MudNeatooSlider.razor.cs:126`).
   - **The rule.** The Standing orders reserve "any public API change beyond the three decided above" for Keith. The header's three decided changes do not include it.
   - **What authorizes it.** The opening stub (8ffc82e, plan 007 Scope: "this plan ships that parameter") and Keith's own #113.
   - **The gap.** `reviews/007-code-review.md` (governance note) asked that it be named in the arc PR's review list. It is not in Recommendations Taken or the Discovery Log, so nothing in the todo carries it to the Step 8 PR body (container item 12).
   - Affects: AC-7. Reachable by: Keith's end review of the arc PR, which the Standing orders make the acknowledgment.
3. **A MudNeatoo reference still teaches a page that subscribes to `NeatooPropertyChanged`.** High on the text, Medium on the consequence.
   - **The conflict.** These lines contradict SKILL.md:406, "A page never subscribes to the root's `NeatooPropertyChanged`", the no-escape rule FCI-008 added after run 1 fell into F6:
     - `skills/mudneatoo/references/property-change-events.md:128`: "If you render a view-only page … subscribe to … `NeatooPropertyChanged` (for any descendant state) yourself".
     - `:33` and `:48`: `NeatooPropertyChanged` "for graph-wide awareness".
     - `skills/mudneatoo/SKILL.md:793`, in the references index: "how a page or container component subscribes to deep-graph changes".
     - `references/aggregate-reactive-vm.md:3`: "the page binds the aggregate and subscribes to it" (corrected later at `:205`).
   - **Why the runs did not catch it.** No run-1 or run-2 session read `property-change-events.md` (tool ledgers), so AC-9's evidence does not cover it.
   - Affects: AC-7 (and AC-9's F6). Reachable by: a session building a view-only page over an aggregate with children, sent to `property-change-events.md` by the SKILL.md index line.
4. **Recommendations Taken is incomplete against the review files.** High.
   - **The rule.** The Standing orders say every recommendation taken is listed there for Keith's end review.
   - **Taken and fixed, but not listed:**
     - FCI-008 round 1: the S6 F3 wording, and the T2 "server gate's shape" wording.
     - FCI-008 round 2 tech-debt: transcript impermanence stated, run 1's probe ledger moved to `run1/`, run 1's command line noted, and the test count described honestly. The Gate Record lists all four. Recommendations Taken line 111 names only the isolation wording, `OnRemove`, the loose references and the mojibake.
     - FCI-005 round 2: the Count-flip line corrected.
     - FCI-006, both reviews: the mdsnippets render logged (`006-mdsnippets.log`).
     - FCI-007 code review: the governance note (Callout 2).
   - Affects: AC-9 (the FCI-008 rows), AC-5, AC-6, AC-7. Reachable by: Keith's end review of Recommendations Taken.
5. **Container item 6. Three Punchlist rows are closed without a commit, a PR or an issue.** High.
   - **The rows.** Row 1 (the changed-meta-property lock, AC-4) says "locked; 0 of 25…", though its commit is 128142f in #119. Row 4 (AC-6) cites `reviews/006-prose-grep.log`. Row 5 (AC-6) cites skill section names. The work is present in each case; the record is missing.
   - Affects: AC-4, AC-6. Reachable by: the Step 8 check that the Punchlist is all `[x]` or parked.

**Optional-affecting**

6. **Container item 3. Plan 009's status is "Parked", which is not a plan status.** High.
   - **Where.** The Plan Index row and the plan header (`plans/009-mudneatoo-components.md:7`).
   - **The rule.** SKILL.md:230 allows Draft · In Progress · Done · Abandoned · Retired. Step 8 requires every plan to be Done, Abandoned or Retired. The parked work itself is properly a Parked row.
   - Affects: AC-10. Reachable by: the Step 8 verification.

### Could not verify
- Whether Keith was asked about the AsyncFlowTests flake (Callout 1) or the `Setter` (Callout 2) outside the record. Nothing on disk, in commits or in the PR bodies says so.
- Container item 12, that the arc PR body lists the decisions that touched Keith's sentences: the arc PR does not exist yet.
- Container item 7, that issue bodies open with `Origin:`: every Parked row is `proposed` with no number, so there was no issue to sample.
- The run-2 stream-json transcripts. The record keeps them in a session scratchpad, so I relied on the committed answers and ledgers, which the FCI-008 test review re-derived.
- That FCI-006's main 60-run log ran at main (7b1bfe6). The worktree is gone, as the test review already says.

### Theoretical (not triaged)
- Parked row 2 is a validation triggered only on a meta name, which drops its messages. It bears on Required AC-4, and Core Principle 8 says Required work is not parked. AC-4's sentence still holds, and `rules-lifecycle.md:264` documents the limitation.
- `reviews/008-fresh-session.md` Observation 3 says the `[Create]` `RunRules(All)` wording is "still open for Keith's wording call", carried over from SKR-007 observation 2. It traces to no Parked row in FCI. It serves no FCI criterion.
- The Dismissed row "104 CS4014 warnings" is now 106. FCI-004's `AsyncTasksCompletionCallbackTests.cs` added two of the same kind.

### Build & Test
- Build: PASSED — `reviews/close-out-build.log` (0 errors, 89 warnings: CS4014 106 lines, CS8622, CS8629, MSTEST0032, all kinds from before FCI; 0 NEATOO011/012); `reviews/close-out-design-build.log` (0 warnings, 0 errors).
- Tests: 2,265 passed, 0 failed — `reviews/close-out-test.log`: BaseGenerator 50, MudNeatoo 33, Analyzers 23, Person 55, UnitTest 1869 (2 skipped, both from before FCI), Design.Tests 235. Demo reports no tests (dismissed, FCI-002). The run was under the Blame hang detector.
- mdsnippets: `reviews/close-out-mdsnippets.log`, no missing or duplicate snippets. The working tree was clean.
- Sacred tests: the only changes to existing test files are four added `await`s before verbs that now return `Task` (WorkOrderAggregateTests ×2, OrderEditViewModelTests, WorkOrderViewModelTests). PR #117 reports them. No assertion changed. Every Test Evidence method cited across FCI-002 to FCI-008 exists (grep).

### Container
- Plans issued / cap: 9 / 14
- Status cells over budget: 0. One status word is outside the vocabulary (Callout 6).
- Untraced deferrals: None. Every "parked for Keith", "pre-existing" and "not built" in a plan body traces to a Parked or Dismissed row.
- Prose over budget: None. The Goal is 126 words; the one Discovery Log entry is 37 words.
- Open PRs into the arc: None. #115 to #122 are MERGED into `fci-arc`, and `origin/fci-arc` is level with the local branch.
- Required parked without demotion: None
- Dispositions living only in a PR body: None. The `Setter` governance note lives only in `reviews/007-code-review.md` (Callout 2).

### Parked (as the todo lists them)
- proposed · test runs default to a hang timeout · origin: FCI-004
- proposed · a validation triggered only on a meta name drops its messages · origin: reviews/004-code-review.md
- proposed · decision · MetaState compare-and-reset race under concurrent writes · origin: reviews/005-code-review.md callout 4
- proposed · AC-11 (Optional) · open finishing inside a non-remote seam; LoadValue-in-a-rule smell · origin: FCI-005
- proposed · AC-10 (Optional) · FCI-009: boundaries, throttle, slider label, Converter, ValidateBase binding · origin: plans/009
- proposed · decision · AsyncFlowTests_Property_PropertyChangedEvents flake · origin: FCI-006
- proposed · decision · a Setter does not lift read-only · origin: FCI-007

### Read report
- Read beyond the brief:
  - The skill's own files: iterative-todo SKILL.md (proposed-row semantics, the status vocabulary, the Unattended stop conditions) and the SKR-007 fresh-session observations.
  - GitHub, read-only: issues #112, #114, #87, #88, #94; RemoteFactory#173 and #178; the PR bodies of #117 and #120.
  - Repository history: the opening stub 8ffc82e.
  - Installed copies: the skills under `~/.claude/skills` (diff and grep).
  - The bunit registration endpoint: 2.11.3 is the latest.
  - Code: the component, analyzer, generator, ValidateBase, EntityBase and AsyncTasks sources.
- Named but unused: the per-plan flip logs, which the plan reviewers verified; the run-1 answers beyond grep; and the per-plan build and test logs beyond their CS4014 counts.
