# LIST-003 — Test Review (Step 5 gate)

**Date:** 2026-08-21
**Reviewer:** `test-reviewer`
**Object:** plan 003 + commit `35e5d1b`
**Budget:** tight
**Outcome:** Pass — no must-cover gaps; one should-cover, one nice-to-have. Both adopted.

---

## Verdict

No must-cover gaps. The reviewer did not take the Test Evidence map on trust — it hand-traced
`EntityListBase.HandlePropertyChanged`, `ResumeAllActions`, `ResetMetaState`, and
`ValidateListBase.InsertItem`/`FactoryStart`/`FactoryComplete` to confirm each new test exercises
a real state transition rather than a setup-guaranteed outcome, and independently confirmed all
four log claims (1838/0/2; one unit failure; one integration failure) match the logs.

Two findings of note that it verified rather than assumed:

- `HandlePropertyChanged_MetaCheckIsNotPauseGuarded` is an **effective** tripwire. The reviewer
  hand-simulated adding an `if (!IsPaused)` guard at both plausible sites — the `EntityListBase`
  cache-arithmetic line and the `ValidateListBase.HandlePropertyChanged` tail call — and confirmed
  the test fails in either case.
- `LocalSaveLifecycle` genuinely contains no `[Remote]`. All four textual hits for "[Remote]" in
  the fixture are inside doc comments describing its *absence*. It also checked for an
  assembly-level `[Parallelize]` or `.runsettings` that would make the shared static
  `LocalSaveStore` a race risk, and found none.

## Sacred tests

Confirmed via `git show 35e5d1b` that the only pre-existing content removed was the standing NOTE
(a single hunk, old 817-830), and that `FactoryComplete_Update_RecalculatesCache` is byte-identical
to before. The NOTE's removal is not a weakening: the control test directly disproves its claim.

## Findings and disposition

| # | Tier | Finding | Disposition |
|---|---|---|---|
| 1 | should-cover | The Acceptance bullet names both `Fetch` **and** `Create`, but only `Fetch` had a test; `Create` was covered "by construction" (the fix sits inside `if (Update)`). The reviewer agreed the reasoning is sound and the evidence row honest — but flagged it as a real gap, because structural unreachability is a property of the *current* guard. A refactor consolidating it the other way (`if (op != Fetch)`) would start announcing on Create and the Fetch test alone would not notice. | **Adopted.** Added `EntityListBaseTests.FactoryComplete_Create_AnnouncesNothing`, mirroring the Fetch test. |
| 2 | nice-to-have | The integration defect test did not assert `DeletedCount == 0` post-save, though the control asserts it as a precondition. Not load-bearing — `LocalSaveStore.DeletedLineIds` and `IsModified == false` cover it indirectly — but asymmetric. | **Adopted.** Added the assertion. |

Pre-existing tech-debt gaps: none beyond what sibling plans already own (LIST-002, LIST-004,
LIST-005) — the reviewer explicitly confirmed nothing was silently absorbed into this plan.

## Read report

Read beyond the brief: `ValidateListBase.cs` in full (needed to judge test vacuity independently —
the brief described the defect mechanism but not enough control flow); `IntegrationTestBase.cs`
(to confirm the local fixture does not implicitly route through a client/server split); greps for
`[Parallelize]`/`.runsettings` and for a project-level `test-reviewer` agent definition.

Named but unused: none — plan, todo, all four logs, and both code-target paths were used.

**Brief calibration for next time:** the `ValidateListBase.cs` pull was predictable and should have
been named as a code target with its question attached, not left to discovery.
