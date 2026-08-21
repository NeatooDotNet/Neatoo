# LIST-001 — Test Review (Step 5 gate)

**Date:** 2026-08-21
**Reviewer:** `test-reviewer`
**Object:** plan 001 + commit `390234d`
**Budget:** tight
**Outcome:** Pass with corrections. No must-cover gaps; three findings, all adopted.

Note: the gate initially went idle without reporting (it wrote its analysis as plain text rather
than sending it). It was asked to report and to state what it actually read versus reconstructed;
the second brief also carried a correction to the first (see Finding 2).

---

## Finding 1 — the disposition test does not pin what its row claimed (adopted)

`FactoryComplete_WhenNeverPaused_LeavesLiveMaintainedStateIntact` is **not vacuous**, but it
**cannot fail for the reason it names**. The reviewer worked the arithmetic: the recalculation the
guard skips — `_cachedIsValid = !this.Any(c => !c.IsValid)` — computes exactly the value live
maintenance already produced, so guarded and unguarded agree *by construction* whenever the live
path is correct. Deleting the `if (IsPaused)` guard outright leaves the test green. No fixture can
separate the two without introducing a separate live-maintenance bug.

The plan's own Constraints explicitly sanctioned a characterization test here, so the test itself
stays — but the Test Evidence row labelled it **Pinned**, which overstated it.

**Disposition: adopted.** The row now reads *"NOT pinned — documented, not regression-proof"* with
the reasoning, and the test's own comment now states plainly what it does not do.

## Finding 2 — the `ValidateListBase`-tier test is NOT redundant (keep)

Confirmed real tier value. `TestValidateList : ValidateListBase<TestValidateItem>` has **no**
`EntityListBase` in its chain, while all three pre-existing pins use `EntityPersonList`, an
`EntityListBase` subclass. `ValidateListBase` is directly instantiable and is what a
validation-only or read-model list uses; that path had zero coverage of `FactoryComplete`'s
recalculation before this test.

**But the test's own comment overstated the mechanism.** It argued an `EntityListBase` override
could mask a `ValidateListBase` regression — there is no such override: `EntityListBase.ResumeAllActions`
only adds `_cachedChildrenModified` before calling base, and the `_cachedIsValid`/`_cachedIsBusy`
recalculation lives solely in the base class. (`IsModified` is genuinely `EntityListBase`-exclusive
and absent from `IValidateMetaProperties` — that half of the reasoning was sound.)

**Disposition: adopted.** Comment rewritten to state the real justification — covering a directly
used tier — rather than a masking risk that does not exist.

## Finding 3 — `IsBusy` was asymmetrically undercovered at that tier (adopted)

`_cachedIsBusy` is recalculated by the identical, adjacent, unoverridden line
(`ValidateListBase.cs:552`) and skipped while paused by the same `if (!IsPaused)` (`:146`), but only
the Invalid half got a `ValidateListBase`-tier pin. By the same rationale that justified adding
Invalid, Busy was undercovered.

**Disposition: adopted.** Added `FactoryComplete_AfterPausedAddOfBusyItem_ValidateListReportsBusy`.

## Finding 4 — evidence log did not match the tree (adopted, re-run)

`001-revert-verification.log` named `...AddOfInvalidItem_ListReportsInvalid` at line 566, but the
method at that line is now `...AddOfInvalidItem_ValidateListReportsInvalid` — the log was captured
just before a cosmetic rename. Act line and assertion text matched, so it was the same test body,
but as cited the evidence did not literally match the tree it supports.

**Disposition: adopted — re-run rather than annotated.** The regenerated log names 7 failures, all
matching current source: the 3 pre-existing pins, the 2 new `ValidateListBase`-tier pins, and the
2 LIST-003 silence tests (which depend on the resume recalculation to have a modified child to be
silent about).

A second lesson from the re-run: the first attempt used a `sed` broad enough to hit **both**
`ResumeAllActions()` call sites in `ValidateListBase` — `FactoryComplete` *and* the
deserialization resume at `:315` — which added two unrelated `FatClient*` deserialization
failures. This is the same over-broad-`sed` mistake made once already in LIST-003. Line-targeted
edits only when a call appears more than once.

## Read report

The reviewer read the plan, todo Discovery Log, the full `390234d` diff, the relevant regions of
`ValidateListBase.cs` / `EntityListBase.cs` / `ValidateBase.cs`, both existing pin test files, the
FABLE-001 plan (to confirm the A3 deferral actually landed with correct citations), all three logs,
and `IMetaProperties.cs`. Named but unused: ISNEW `reviews/004-code-review.md` V2 — the code
comment at `EntityListBase.cs:380-385` already quotes it.
