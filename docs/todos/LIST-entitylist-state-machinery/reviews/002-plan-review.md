# LIST-002 — Plan Review (pre-implementation)

**Date:** 2026-08-21
**Reviewer:** `plan-reviewer`
**Object:** plan 002, draft, nothing implemented
**Budget:** deep
**Verdict:** **CONCERNS — two veto-tier findings.** Implementation is BLOCKED pending a decision.

---

## Veto 1 — two sacred tests encode the opposite expectation

The reviewer was asked adversarially whether the plan's premise ("replacing a persisted child
should DELETE its row") could destroy data for a legitimate identity-preserving swap. It found no
*documented* pattern that this breaks — the canonical refresh pattern in Design.Domain is a whole-list
re-fetch, not per-element indexer replacement — but it found something more concrete than a
hypothetical:

- `EntityListBaseTests.SetItem_ReplaceModifiedWithUnmodified_WhenOnlyModified_ListBecomesUnmodified`
  (`:1243-1265`) replaces a **persisted** modified item with a persisted unmodified one and asserts
  `list.IsModified == false` afterward.
- `LargeList_SetItem_UpdatesCacheCorrectly` (`:1518-1556`) has the same shape at its final assertion.

Verified independently: `IsModified => _cachedChildrenModified || DeletedList.Any()`. Under the
plan's Step 1 the displaced persisted item is queued into `DeletedList`, so `DeletedList.Any()`
stays true for the remainder of each test and **both assertions flip from pass to fail**.

The plan's Current State walk never mentions either test, and its Constraints declare "existing
tests are sacred" without addressing them. Under the global CLAUDE.md rule, an intent change to a
passing test requires stopping and asking rather than editing the test to fit.

**Status: ESCALATED TO THE USER. Not resolved by the orchestrator.**

## Veto 2 — internal contradiction in the plan (fixed)

Step 1 says give the displaced item "the same disposition `RemoveItem` gives" — but `RemoveItem`
deliberately **keeps** `ContainingList` set until `FactoryComplete(Update)` clears it via the
`DeletedList` loop (`EntityListBase.cs:331-332` comment, `:454-458` implementation). The plan's
Acceptance list nonetheless carried a `[unit]`-tagged, immediate bullet: *"The displaced item's
`ContainingList` no longer points at a list it is not in."* Faithful `RemoveItem` parity makes that
bullet unreachable.

**Disposition: fixed in the plan.** The bullet now carries an after-save qualifier and matches
`RemoveItem` parity, rather than silently requesting a different, unjustified immediate-clearing
disposition.

## Callouts

3. **Release-note scope.** The Constraints release-note bullet covers only the DELETE-queueing
   change. If Step 3 resolves toward adopting `InsertItem`'s aggregate-boundary guard, that is a
   *second* save-side break (silent success → thrown exception) not covered by the current framing.
   **Adopted:** bullet extended to cover whichever way Step 3 lands.
4. **Placement precedent.** `RemoveItem` captures its item reference *before* `base.RemoveItem`;
   `ValidateListBase.SetItem` unsubscribes the old handler and subscribes the new one around its own
   `base.SetItem`. The implementer should mirror that proven ordering rather than invent one.
   **Adopted** into Steps.

## Positive confirmations

- Current State verified accurate line-for-line against `EntityListBase.cs:361-410`.
- Composition of `RemoveItem` + `InsertItem` meanings is structurally safe; `MarkDeleted()` raising
  `PropertyChanged(IsModified)` is confirmed (`EntityBase.IsModified` includes `IsDeleted`).
- Step 4's "announce correctly, consistent with LIST-003" is well grounded:
  `ValidateListBase.SetItem` calls `CheckIfMetaPropertiesChanged()` *before* `EntityListBase.SetItem`'s
  post-base cache arithmetic runs, so the `IsModified` transition can go stale here exactly the way
  `RemoveItem`'s did before LIST-003.

## Read report

Read beyond the brief: `EntityListBaseTests.cs` (**not named in the brief** — read specifically to
test the premise empirically, and it is what surfaced Veto 1), `ValidateListBase.SetItem`/`RemoveItem`
tails, `EntityBase.IsModified`/`MarkDeleted`, and Design.Domain `EmployeeList.cs` / `CommonGotchas.cs`
(checked for a documented replace-to-refresh pattern; none exists).

**Brief calibration:** the adversarial question ("could this destroy data?") was answered by reading
the *test suite*, not the docs. Next time a plan proposes a behavior change, name the test files
covering the changed method as a code target — that is where the counter-evidence lives.
