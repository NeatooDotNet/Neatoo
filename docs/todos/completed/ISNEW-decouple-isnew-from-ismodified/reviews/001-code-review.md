# Code Review Record — ISNEW-001 — 2026-08-21

**Reviewer:** code-reviewer agent (opted-in per plan header). Findings-only, no grade.

## Direction & shape

Clean. The reworked OrderAggregate matches the Person canonical (adapted to the
repository-tuple shape), mirrored consistently in `SaveDemoItemList`/`FetchDemoItemList`.
Interface-first respected; tests use real factories with only external repositories stubbed.
The reviewer verified the load-bearing framework claims directly against `src/Neatoo` and
the generated factories (not the diff's own comments), including the retired-Pattern-3
"explicit PauseAllActions resumes early" claim.

## Veto-tier findings and dispositions

1. **`SavePatterns.cs` — "parent list's DeletedList is cleared after child [Delete]"** (the
   cascade misconception surviving in the reworked file). **FIXED** — block rewritten:
   no state changes after [Delete]; lifecycle hooks fire on the single target; canonical
   deleted children never get a [Delete] at all.
2. **`SavePatterns.cs` — Save-routing pseudocode contradicted by generated code.**
   **FIXED** — replaced with the actual emitted ordering (`IsDeleted` → `IsNew` → Update),
   with the three consequences spelled out: IsModified never consulted by routing (the
   `EntityBase.Save()` IsSavable gate is what stops unmodified saves), IsDeleted wins over
   IsNew (created-then-deleted routes to [Delete], not a no-op), no short-circuit. Matching
   corrections in the [Update]/[Delete] section headers and `AllBaseClasses.cs:298,309`.
   Routing consequence for the flip recorded in the ISNEW-004 stub.
3. **`Entities/` (Employee/Address/AddressList) — third demo aggregate with the identical
   broken lifecycle, unswept.** **DEFERRED with record** — queued as ISNEW-007 (full aggregate
   rework incl. the DID-NOT-DO blocks that reject the now-canonical pattern; flagged to run
   early). Deferral recorded in the Discovery Log and as a Plan Amendment scoping Acceptance
   bullet #4 to the swept surface.

## Callout-tier findings and dispositions

- (a) NF0105 claim in `FetchPatterns.cs` contradicted by compiling `[Remote] internal`
  methods — **fixed** (parenthetical dropped; real rationale stated).
- (b) `OrderItem.cs` SetParent mechanism wrong (Parent flows at list-assignment, not per
  paused add) — **fixed**.
- (c) Root `[Delete]`'s direct repository child deletes read as oversight — **fixed**
  (intent comments added in both files).
- (d) `IsNew || IsModified` save guard deviates from Person's unconditional saves —
  **recorded in plan Notes as deliberate** (repository writes aren't free; pinned by
  exact-count assertion).
- (e) Design.Tests not in `src/Neatoo.sln` — **recorded in plan Notes for the close-out
  audit**: full coverage claims need both log pairs.

## Build & test after fixes

Design.Tests rebuilt and re-run post-fix: 0 errors, 110/110 passed
(`001-design-build.log` / `001-design-test.log`, overwritten per loop protocol).
Full-solution logs unchanged (no library or test-logic changes in the fix round —
comment-only in Design.Domain plus todo/plan records).

## Verification (reviewer follow-up pass, 2026-08-21)

**Verdict: all three veto-tier findings closed, all five callouts dispositioned. No new
findings. Clear to mark Done.**

- Vetoes 1+2: grep for surviving old claims across SavePatterns.cs/AllBaseClasses.cs returns
  zero matches; replacement routing block checked line-for-line against
  `OrderItemFactory.g.cs` `LocalSave`, including the no-[Delete] NotImplementedException
  annotation.
- Veto 3: deferral trail confirmed complete across all four artifacts (ISNEW-007 stub, Index
  row, Discovery Log entry, Plan Amendment); bullet #4 honest. (Reviewer's optional
  tightening — qualify the bullet text itself — applied: bullet now names the swept surface
  inline.)
- Callouts (a)-(e): all confirmed dispositioned as recorded above.
- Log freshness independently verified via file mtimes (post-fix logs; identical test
  duration was coincidence).
