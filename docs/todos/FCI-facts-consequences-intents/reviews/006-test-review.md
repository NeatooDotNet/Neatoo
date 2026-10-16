# FCI-006 test review

**Round 1 (2026-10-16): DECISION NEEDED.** `AsyncFlowTests_Property_PropertyChangedEvents` failed in `006-test.log`. Is that failure accepted as the proposed Parked flake, or must it be fixed before FCI-006 is Done? Every Required bullet is otherwise pinned at its declared tier. No existing test changed: the only test diff from fci-arc is 15 added lines in `TrainingPlanTests.cs`, with nothing deleted.
- **Veto (Keith's call).** `Neatoo.UnitTest.AsyncFlowTests.AsyncFlowTests_Property_PropertyChangedEvents` failed in `006-test.log` (`CollectionAssert.Contains` at AsyncFlowTests.cs:167; 1868 passed, 1 failed, 2 skipped). It passed in `006-test-rerun.log` (1869 passed). The evidence matches how the plan describes it. The branch failed 1 run in 30 (`006-asyncflow-30-runs.log`: 29 passed, 1 failed). The `main-wt` worktree failed 2 runs in 60 (`006-asyncflow-main-60-runs.log`: 58 passed, 2 failed), and main (7b1bfe6) has none of the 18 FCI commits. `AsyncFlowTests.cs` was last changed in 7a0653e, and its failing assert follows a fixed `Task.Delay(5)` after `WaitForTasks()`, as the Parked row says. Calling it pre-existing is still Keith's decision, not this review's. The standing orders make an out-of-scope failure a `proposed` Parked row that stops the loop, and that row exists.
- **Required bullets.** `WritingTheOriginalValueBack_LeavesThePlanModified` is a real claim. `Create` leaves no intervals and 60 approved minutes, and 90 and 60 are both valid, so `CanApprove` stays true and `ApprovedMinutes` is the only thing written. A framework that compared writes against the baseline would report the plan unmodified and fail the test. The `IsFalse(plan.IsModified)` precondition rules out a modified Create. The test has no flip log, because the plan added no production line to flip. Every region newly placed in the skill comes from TrainingPlan, Interval or the interface file and is pinned by one of the 16 `TrainingPlanTests` (15 from FCI-005, plus the sticky test). That makes 14 regions and 15 placements, since `skill-admission-on-meta-state` is placed twice. Each placement's source line range matches its region's current lines. Placeholders are balanced in every changed file. No added fence in the skill or guides is hand-written C#. The explicit-skip prose sits where the evidence says, in these sections:
  - SKILL.md model statement 3, the first habit row, "A fact, a consequence, an intent", "Domain code awaits its writes", "A rule may trigger on meta state" and "`[Remote]` is per call".
  - rules-lifecycle "Rules on Meta State": safe and sync-only triggers, plus the symptom that `WaitForTasks()` never returns.
  - rules-lifecycle "Rules Write Through Awaited Setters".
  - The pitfalls rows for NEATOO011, NEATOO012 and the sticky write-back.
  - The domain-logic-placement Parent rule (#114).
  - The entities `IsModified` row.

  No skill prose still says logic is "placed by who drives it". The installed `~/.claude/skills/neatoo` is identical to `skills/neatoo` (`diff -r`).
- **Logs.** `006-build.log` and `006-design-build.log` both succeeded with 0 errors and 0 NEATOO011/012. Both warning sets are pre-existing kinds:
  - `006-build.log` has 89 warnings, mostly CS4014, plus CS8622, CS8629 and MSTEST0032.
  - `006-design-build.log` has 0 warnings.

  Design.Tests passed 222 of 222 in `006-test.log`, `006-test-rerun.log` and `006-design-test.log`.
- **Tech-debt.**
  - skills/neatoo/references/entities.md: the sticky prose and the `skill-test-modified-is-sticky` block were inserted *inside* the `IsSavable` table row, between the escaped pipes of `(IsModified \|\| IsNew)`. That breaks the row, and the installed copy carries the same break. Fix: restore the row and move the prose and block below the table.
  - `006-prose-grep.log` greps only for `AddAction`. I checked the bullet's other half, a sync multi-write verb, by grepping prose for `void` verbs, and found only API-table signatures.
  - domain-logic-placement's Parent bullet says a child does not "reach through the parent into a sibling", while rows 22, 400 and 1108 teach a child rule that *reads* its siblings through `Parent`. The bullet's example is a write. Saying "write" would avoid reading it as a ban on the sibling-read rule.
- **Could not verify.**
  - The main 60-run log ran in a scratch worktree that has since been removed (`main-wt`). That it was checked out at main (7b1bfe6) rests on its path name.
  - No mdsnippets log is in `reviews/`. "mdsnippets clean" rests on matching line ranges and balanced placeholders, not on a run.
- **Theoretical.** None.

**After round 1.** The decision is parked as a proposed row for Keith; the flake predates FCI and the test is untouched. Tech-debt fixed: the `IsSavable` row is whole again with the sticky prose below the table; `006-prose-grep.log` adds the void-verb grep (only API table rows remain); the sibling anti-pattern now says "write a sibling through the parent", and reading siblings through `Parent` is stated as fine. `006-mdsnippets.log` records the render.
