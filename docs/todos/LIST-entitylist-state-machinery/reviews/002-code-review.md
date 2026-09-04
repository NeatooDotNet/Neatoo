# LIST-002 — Code Review (Step 5, per-plan, findings-only)

**Date:** 2026-08-21 (run retroactively — see close-out audit V1)
**Reviewer:** `code-reviewer`
**Object:** commit `7b1d7c1`
**Budget:** deep
**Outcome:** **One veto-tier finding — silent data loss. Fixed.**

---

## Direction

Sound. `SetItem` genuinely composes `RemoveItem`'s "leaving" and `InsertItem`'s "joining"
semantics. Guard placement, the self-replacement escape hatch, the paused-branch trust boundary,
and the end-of-method announce all mirror the two established methods. The reviewer checked
`v0.32.0.md` against the code point by point and found the migration note accurate on every claim.

## V1 (veto) — the incoming item could be live *and* queued for deletion

`InsertItem`'s live branch has a re-add / intra-aggregate-move step: if the incoming item already
has a `ContainingList`, it calls `RemoveFromDeletedList` on that list, and if `item.IsDeleted` it
calls `UnDelete()` (`EntityListBase.cs:268-277`). **`SetItem`'s live branch had no equivalent.**

So `list[i] = item`, where `item` is currently sitting in `this.DeletedList` — because it was
removed or displaced earlier and not yet saved — left the item swapped into the live slot, still in
`DeletedList`, still `IsDeleted == true`. The canonical `[Update]` loop drives off
`this.Union(DeletedList)` filtered on `IsDeleted`, so **the next save would DELETE a row the
user's own collection shows as live.** Silent data loss.

None of the plan's eight tests, and no Acceptance bullet, covered "replace with an item currently
awaiting deletion" — the exact question the brief asked as Q5 and the plan never answered.

**Disposition: fixed.** Verified first — the new test
`SetItem_ReplacingWithAnItemAwaitingDeletion_ResurrectsIt` was written before any code change and
failed on `a.IsDeleted`, exactly as predicted. The fix mirrors `InsertItem`: `RemoveFromDeletedList`
on the incoming item's old list, then `UnDelete()` if it was flagged — placed **before** the
displaced item is queued, so resurrecting the incoming item and queueing the outgoing one do not
interfere.

Revert verification: gating out only `SetItem`'s re-add step fails exactly one test, the new one.

## Callouts — all recorded, none actioned

**C1 — mid-mutation notification.** Independently traced and confirmed to match the Discovery Log
entry already recorded from the close-out audit: `MarkDeleted()` fires while the displaced item is
still subscribed, so the mark can raise a list-level `IsModified` before the slot is swapped.
Byte-for-byte the same shape as `RemoveItem`. Inherited, correctly dispositioned as recorded-not-changed.

*New nuance the reviewer surfaced while re-tracing:* because `oldWasModified` is captured **before**
`MarkDeleted()` runs, replacing an unmodified-persisted item with another unmodified-persisted item
skips both branches of the end-of-method recalculation, leaving `_cachedChildrenModified` `true`
from the transient flip — violating its own doc comment ("children only, not `DeletedList`"). The
reviewer could not construct a currently-reachable sequence where this produces a wrong *public*
`IsModified`, because `DeletedList.Any()` masks it until `FactoryComplete(Update)` fully
recalculates. The one path it could not rule out: moving the item to a *different* list in the same
aggregate while still in this list's `DeletedList`, since `RemoveFromDeletedList` does no
recalculation and no notification. **Carried forward** to whatever eventually reorders
`RemoveItem`/`SetItem` together.

**C2 — mixed equality semantics.** The duplicate guard uses `Contains` (`EqualityComparer<I>.Default`)
while the self-replacement guard uses `ReferenceEquals`. These agree unless an entity type overrides
`Equals`, which is not idiomatic here. The reviewer separately confirmed `ReferenceEquals` is the
**correct** choice for self-replacement — it is exactly what makes the documented
identity-preserving-refresh migration note true: a distinct, logically-equal instance is
deliberately not a no-op, and its row is deleted. That is the disclosed tradeoff.

**C3 — paused branch leaves the displaced item's `ContainingList` stale.** `RemoveItem`'s paused
branch has the identical gap already. Not new; no coverage either way. Matches the same gap LIST-004's
review flagged from the other direction.

## Verified, no findings

- Q2: a paused `SetItem`'s incoming item lands in exactly the state a paused `InsertItem`'s would;
  `Delete()` only inspects pause state at call time, so no new interaction with LIST-004.
- Q4: throwing when the incoming item is already at a different index is correct `InsertItem` parity.
- Q6: all three guards, the mark-and-queue, and the `MarkModified` step are inside `if (!IsPaused)`;
  only `base.SetItem` and identity conferral run unconditionally.
- Logs check out, including that the revert log's five named failures match the plan's claim exactly.
- `git show 09a3425` confirmed the `AreSame` change is property-*value* comparison, not list-item
  identity — so it does not interact with either guard.
