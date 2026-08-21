# Code Review Record — ISNEW-007 — 2026-08-21

**Reviewer:** code-reviewer agent (opted-in). Findings-only, no grade.

## Direction & shape

Clean and faithful to the ISNEW-001 canonical: `AddressList.Fetch`/`Update` mirror
`OrderItemList` step for step; `Address.Fetch`/`Insert`/`Update` mirror `OrderItem`
(parent-id-in-signature included); `Employee` delegates to the list factory exactly as
`Order` does; the `PauseAllActions()` wrapper and the phantom `ProcessDeletedAddresses`
helper are gone. Framework claims were verified against `src/Neatoo` source and the emitted
factories, not the diff's comments. Interface-first holds; only repositories are stubbed.

## Veto-tier findings — all fixed

1. **The ENTITY DUALITY block described a role the code could no longer perform.** After the
   rework only `[Remote] [Delete]` survived, so the generated root-role `Save` threw
   `NotImplementedException` on insert and update, and `IAddressOnlyRepository.Insert/Update`
   were dead members.
2. **The surviving `[Remote] [Delete]` punched a hole in the aggregate boundary.** Because its
   signature carries no parent identity, the generator emitted a **public**
   `Save(IAddress target)` on `IAddressFactory` — a consumer holding a child out of
   `employee.Addresses` could delete the row outside the aggregate's save flow. The canonical
   `IOrderItemFactory` exposes nothing public.

   **Fix (both):** the standalone-root role was removed entirely, along with
   `IAddressOnlyRepository` and its mock/registration, restoring the internal-only factory
   surface. In its place, Address carries a `NO STANDALONE-ROOT OPERATIONS` block giving the
   two decisive reasons (public-Save hole; `IAddress : IEntityBase` already declares
   "child, not root", so a true dual-use type needs a second root interface). This resolves
   callout (a) as well — the half-true "routes on IsDeleted/IsNew" phrasing evaporates with
   the removed role. It also supersedes test-review finding 3, which asked for coverage of a
   role that should not exist.

   *Note:* an earlier attempt during this loop went the opposite way — completing the
   standalone role with `[Remote]` Fetch/Insert/Update. That would have widened the hole
   (public Save able to insert and update children too). Reverted before it landed.

3. **`FactoryOperations/RemoteBoundary.cs` — unswept file teaching the contradicted rule by
   name.** It stated that a child's persistence methods are "called by the parent's
   persistence code, NOT through the factory" (about Address specifically) and that "Address
   as root: can be fetched/saved independently". **Fixed in place:** the block now states the
   canonical rule (child persistence runs through the child factory's Save, coordinated by
   the list's `[Update]`), spells out the root-vs-child split as interface + signature +
   visibility, points at Address's rejected-pattern block, and re-frames `DualUseEntity` as
   demonstrating the remote/local boundary rather than root-vs-child.

## Callout-tier findings

- (a) Resolved by veto 1/2 fix (see above).
- (b) Two load idioms (`LoadValue` vs plain assignment) taught in the same file set without
  reconciliation — **fixed**: `Employee.Fetch` now carries the canonical sentence explaining
  both are clean while paused and why `LoadValue` is shown for the root.
- (c) `Employee.Delete` iterates `Addresses` only, not `DeletedList` — same shape as
  `Order.Delete`, consistent with the canonical and covered by FK cascade in a real schema.
  **Accepted; queued to ISNEW-006** with the other root-delete work.
- (d) Dead `MockAddressOnlyRepository` members — **fixed** (mock deleted with the role).

## Build & test after fixes

Design.Tests rebuilt and re-run: 0 errors, 113/113 passed. Full-solution logs unchanged from
the gate pre-flight (the fix round touched Design.Domain comments/ops and Design.Tests only).

Standing note repeated from ISNEW-001: `src/Neatoo.sln` excludes Design.Tests, so
"full sln green" never covers the Design projects on its own.
