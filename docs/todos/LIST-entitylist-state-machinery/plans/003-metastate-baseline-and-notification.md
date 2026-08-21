# Stale Meta-State Baseline After a Save Carrying Deletions

**Plan #:** 003
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md) (re-split out of the original LIST-001)
**Status:** Done
**Last Updated:** 2026-08-21
**Plan-review opt-in:** No — the defect is reproduced and isolated by a controlled probe (see
Current State), so there is no direction question left for a reviewer to catch. The risk that
remains is coverage honesty, which the Step 5 test gate owns.
**Code-review opt-in:** Yes — notification machinery, user-visible behavior change.

---

## Scope

The headline user-visible defect of this todo. After a save that carried child deletions, the
list is left with a meta-state baseline saying `IsModified = true` while the real value is
`false`. The user's next child edit therefore compares true-against-true and **announces
nothing** — the list's value silently self-heals, but no `PropertyChanged` is raised, so parents
and bindings never learn there is unsaved work again.

This plan fixes the baseline at the point it goes stale, and closes the notification coverage
gap that let it hide — including retiring a standing NOTE that asserts the opposite of what the
code now does.

This plan does **not** touch `SetItem` (LIST-002), the paused `Delete()` seam (LIST-004), or the
inherited test debt (LIST-005). It does not change what gets persisted, only what gets announced
and what the baseline records.

## Intent

After a save, a list tells the truth about itself and keeps telling the truth. A child edit
following a save that carried deletions raises the same notification it would have raised after
a save that carried none — the two cases become indistinguishable to a parent, a binding, or an
unsaved-changes guard.

## Framework & Architectural Alignment

- Meta-state notification is a baseline-comparison design: `ResetMetaState()` snapshots, and
  `CheckIfMetaPropertiesChanged()` compares-then-snapshots, raising via `RaiseIfChanged`. Any
  code path that changes a meta property outside that pairing leaves the baseline lying. The fix
  belongs at the mutation site, using the existing seam — not a new notification mechanism.
- Resume is deliberately silent: after a factory operation the post-factory state *is* the new
  baseline, so `ResumeAllActions` snapshots without announcing. That is correct and is preserved.
  The Update branch is different in kind: it mutates state **after** the resume has already
  snapshotted, which is precisely why it needs the comparing form rather than the snapshotting one.
- ISNEW settled that `IsModified` is the aggregating state and `IsNew`/`IsDeleted` are per-object
  routing state. This plan changes only when `IsModified` transitions are *announced*.

## Constraints & Invariants

- `IsNew`/`IsModified` semantics settled by ISNEW are not reopened.
- What the Update branch *persists* is untouched: `DeletedList` still clears, deleted children
  still get `ContainingList` nulled. Only the baseline and the notification change.
- Resume must stay silent (see Alignment). The fix must not make `FactoryComplete(Fetch)` or
  `FactoryComplete(Create)` start announcing — a fetch is not news.
- Existing tests are sacred. `FactoryComplete_Update_RecalculatesCache` asserts the post-save
  *value*; that assertion must keep passing untouched.

## Current State

Walked 2026-08-21. The defect is **reproduced and isolated**, not inferred.

- `EntityListBase.IsModified => _cachedChildrenModified || this.DeletedList.Any()`
  (`src/Neatoo/EntityListBase.cs:64`).
- `ResetMetaState()` (`:185-189`) snapshots `EntityMetaState = (IsModified, IsSelfModified)`;
  `CheckIfMetaPropertiesChanged()` (`:172-179`) compares against that snapshot, raises via
  `RaiseIfChanged`, then delegates to base which re-snapshots.
- `EntityListBase.FactoryComplete` (`:444-461`) runs in this order: `base.FactoryComplete` →
  `ResumeAllActions` → `ResetMetaState()` **while `DeletedList` is still populated**, so the
  baseline records `IsModified = true`; then the Update branch clears `DeletedList` (`:455`) and
  recalculates `_cachedChildrenModified` (`:459`). Nothing calls `CheckIfMetaPropertiesChanged()`
  afterwards, so the baseline is never corrected and the transition is never announced.
- `HandlePropertyChanged` (`ValidateListBase.cs:368-408`) ends with an **unguarded**
  `CheckIfMetaPropertiesChanged()`, which is why the *value* later self-heals even though the
  *notification* was lost.

**Probe result (throw-away, deleted after recording).** A `ProbeList` fetched with two persisted
children, one removed, then completed through a local `FactoryStart(Update)` /
`FactoryComplete(Update)` pair:

| case | `IsModified` after edit | notifications raised |
|---|---|---|
| save carried a deletion | `True` (self-healed) | `[]` — **nothing announced** |
| control: save carried no deletion | `True` | `[IsModified]` |

Identical code path; the only difference is whether a deletion was in flight. Two consequences
for this plan:

1. The stub's assumption that a **non-remote save harness was gating work is wrong** — the unit
   tier already provides one. `FactoryStart(Update)`/`FactoryComplete(Update)` on a plain
   `EntityListBase` is a local save, no `[Remote]` fixture and no serialization involved. The
   `[Remote]` SaveLifecycle fixtures hide the defect because `OnDeserialized` rebuilds the
   baseline; they are not needed to expose it.
2. The standing NOTE in `EntityListBaseTests` (~line 820) — "`EntityListBase.IsModified` is
   computed (uses `Any()`) and ... does not raise `PropertyChanged`" — is **disproven by the
   control**, not merely stale. Lists do announce `IsModified`.

## Steps

1. Correct the baseline at the site that invalidates it: after the Update branch has cleared
   `DeletedList` and recalculated, compare-and-announce through the existing meta-state seam
   rather than leaving the snapshot the resume took.
2. Confirm the fix stays inside `Update` — `Fetch` and `Create` completions must remain silent.
3. Pin the defect with the probe's two cases promoted to real tests, defect **and** control, so
   the pair distinguishes "the baseline was repaired" from "lists never announce".
4. Retire the disproven NOTE in `EntityListBaseTests` and put the notification tests it defers in
   its place, citing what replaced it.
5. Assert the `HandlePropertyChanged` pause-guard asymmetry, so a future "symmetry" guard fails a
   test instead of silently reopening the ISNEW-003 defect with a green suite.
6. Verify by revert that each new test fails without the fix; full-suite run.

## Acceptance

- After a local save that carried child deletions, editing a surviving child raises
  `PropertyChanged(IsModified)` on the list [unit]
- The control — the same edit after a save that carried no deletions — raises the same
  notification, so the pair isolates the baseline rather than the notification machinery [unit]
- A parent entity holding such a list becomes savable again after that edit, on a local save
  [integration]
- `FactoryComplete(Fetch)` and `FactoryComplete(Create)` announce nothing [unit]
- The `HandlePropertyChanged` pause-guard asymmetry is asserted in a test whose comment states
  why the asymmetry is deliberate [unit]
- The disproven NOTE is gone, replaced by the tests it deferred [unit]
- Reverting the fix fails the new tests; full solution suite green with no existing test's intent
  weakened [unit]

---

## Plan Amendments

**A1 (Step 3, the harness gap was not real).** The stub called a non-remote save harness the
*gating* work for this plan. At pre-flight it turned out the unit tier already is one:
`FactoryStart(Update)`/`FactoryComplete(Update)` on a plain `EntityListBase` **is** a local save,
with no `[Remote]` fixture and no serialization involved. The defect reproduced there in minutes.
A local fixture was still built — but for the *aggregate-level* bullet, not as a prerequisite.

**A2 (Step 3, new local fixture rather than modifying a shared one).** The aggregate-level bullet
needs a parent that saves locally. `Invoice` is `[Remote]` by explicit design ("so saves genuinely
cross the client/server boundary"), and `EntityPerson` has only `[Insert]`. Rather than alter
either shared fixture, `Integration/Aggregates/LocalSaveLifecycle/` was added: `LocalOrder` /
`LocalOrderLine` / `LocalOrderLineList` plus their own `LocalSaveStore` — the canonical shape with
no `[Remote]` anywhere. It has its own store because both stores are static, and sharing one would
couple two suites through mutable global state.

**A3 (the NOTE was disproven, not merely stale).** The control test raises `IsModified`, which
directly contradicts the NOTE's claim that `EntityListBase.IsModified` "does not raise
PropertyChanged". Removed and replaced with the four tests it deferred, with a comment recording
what replaced it and why.

## Test Evidence

| Acceptance bullet | Test | Tier | Status |
|---|---|---|---|
| Edit after a local save carrying deletions raises `PropertyChanged(IsModified)` | `EntityListBaseTests.SaveCarryingDeletions_ThenChildEdit_AnnouncesIsModified` | unit | Pinned — fails on revert |
| Control: same edit after a save carrying no deletions raises the same notification | `EntityListBaseTests.SaveWithoutDeletions_ThenChildEdit_AnnouncesIsModified` | unit | Pinned — passes on revert, which is what makes it a control |
| A parent holding such a list becomes savable again, on a local save | `LocalSaveLifecycleTests.LocalSaveCarryingDeletions_ThenChildEdit_MakesAggregateSavableAgain`, with `...LocalSaveWithoutDeletions_...` as its control | integration | Pinned — fails on revert at `saved.IsModified`, "The edit must reach the aggregate root" |
| `FactoryComplete(Fetch)` / `(Create)` announce nothing | `EntityListBaseTests.FactoryComplete_Fetch_AnnouncesNothing` | unit | Pinned (Fetch). `Create` shares the branch — the fix is inside `if (Update)` — so it is covered by construction, not by a separate test |
| `HandlePropertyChanged` pause-guard asymmetry asserted | `EntityListBaseTests.HandlePropertyChanged_MetaCheckIsNotPauseGuarded` | unit | Pinned |
| Disproven NOTE gone, replaced by the tests it deferred | `EntityListBaseTests` — NOTE replaced with a comment citing LIST-003 and the four tests | unit | Done |
| Revert fails the new tests; suite green, no intent weakened | Revert run: exactly 1 unit + 1 integration failure, both the defect tests. Solution run: 1838 passed / 0 failed / 2 skipped, plus Samples 254, BaseGenerator 42, Person.DomainModel 55 | unit | Pinned |

## Outcome

The defect is fixed at the site that caused it: `EntityListBase.FactoryComplete`'s Update branch
now compare-and-announces through `CheckIfMetaPropertiesChanged()` instead of leaving the
snapshot the resume took. One line of library change, extensively commented, plus six tests.

Two process notes worth carrying forward:

The **revert discipline paid twice**. The first revert attempt used a `sed` broad enough to also
revert a pre-existing `CheckIfMetaPropertiesChanged()` in `RemoveItem` (from ISNEW), which made
two tests fail and muddied attribution. Redone against that one call site only, the result was
exact: one unit test and one integration test fail, and nothing else — which is what proves the
control tests are genuine controls rather than co-dependent tests that happen to travel together.

The **`[Remote]`-only harness was the real bug-hider**, not any individual missing test. Six tests
now cover this area, but the reason nobody found the defect for so long is that every save fixture
in the suite deserialized its result and had its baselines rebuilt for free. `LocalSaveLifecycle`
exists so that stops being true; future save-path work should get a local case as a matter of
course.
