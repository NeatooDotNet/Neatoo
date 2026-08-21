# Test Review Record — ISNEW-002 — 2026-08-21

**Reviewer:** test-reviewer agent. **Gate: PASSES after the fix loop** (3 must-cover findings
raised, all closed; should-covers 4-6 folded into the same pass).

## must-cover findings and fixes

1. **The client/server boundary was never asserted.** Because `SaveLifecycleStore` is static
   and visible to both containers, every test would have passed identically with the
   operations running in-process — dropping `[Remote]` broke no assertion. That left the
   plan's central claim (assertions against the state that crossed the wire) unverified, and
   it matters directly for the flip: design.md relies on `IsMarkedModified` riding the wire.
   **Fixed:** `AreNotSame` assertions on the root and a child confirm the client holds
   deserialized instances after `Save()`.
2. **`CreatedUntouched_IsSavable_AndSaveInserts` did not test its bullet.** It set `Customer`
   before asserting, so `IsSavable` came from ordinary property dirt, not the `IsNew` weld —
   the test would have passed post-flip even if the new `|| IsNew` term in `IsSavable` were
   forgotten, which is the exact regression it exists to catch. The aggregate could not
   express the real case because `Customer` is `[Required]`. **Fixed:** added a rich
   `[Create]` overload (`CreateForCustomer`) that populates the root *and* factory-built
   children inside the paused factory op, giving a valid aggregate with
   `IsSelfModified=false`; the renamed
   `RichCreate_Untouched_IsSavableFromIsNewAlone_AndSaveInserts` now pins savability to the
   weld alone. This also closed should-cover #2 (the motivating rich-create-with-children
   case) and #1 (pre-save create-state assertions).
3. **A user-attached new child on an otherwise-clean fetched root was not isolated.** The
   existing test bundled a modify, a remove, and an add, so savability was satisfied by the
   modified child regardless of the attach. If ISNEW-004 changes `IsModified` but misses the
   `InsertItem` exemption removal, the suite would stay green while consumer saves throw
   `NotModified` — the silent-data-loss path design.md calls mandatory. **Fixed:**
   `FetchedRoot_AddOneNewChild_IsModifiedAndSavable_AndChildInserts` isolates it.

## should-cover — folded in

4. Second-save guard: `SavedAggregate_SecondSave_ThrowsNotModified` (also pins that `MarkOld`
   crossed the wire — a stale `IsNew` would duplicate the insert).
5. Removal-only change on a fetched root: `FetchedRoot_RemoveOneChild_...`.
6. Update-path insert Id writeback asserted on the client copy.

## Tech debt queued to ISNEW-006

- The two-container harness offers no way to prove a call went remote; a call counter on
  `MakeRemoteDelegateRequest` exposed through `ClientServerTestBase` would close it for every
  two-container test at once (the instance-identity assertions used here are a per-test
  proxy). `TwoContainerMetaStateTests` has the same blind spot.
- Neatoo.UnitTest has no explicit parallelization policy, yet several fixtures depend on
  implicit sequential execution (static containers, `SaveLifecycleStore`).
- Root/child `[Delete]` paths on the SaveLifecycle aggregate remain uncovered (design.md
  wants created-then-deleted routing anchored for the flip).

## Verified clean by the reviewer

Store isolation (`Reset()` first in `[TestInitialize]`, no parallelization configured); id
ranges cannot collide (invoices from 1, seeded lines 100-101, inserted lines 102+, separate
counters and dictionaries, `GetLines` ordered); every assertion loop preceded by an exact
count guard; routing assertions use exact-match rather than contains; the `IsSelfModified`
header guard pinned in both directions; fetch preconditions would catch paused child-property
assignment dirtying the parent.

## Sacred tests

None touched — ISNEW-002's entire surface is new files.

## Logs

`002-build.log` / `002-test.log` (full sln: 42 + 254 + 55 + 1802 passed, 2 pre-existing
skips; Neatoo.UnitTest 1793 → 1802 = 9 new tests), `002-design-build.log` /
`002-design-test.log` (Design.Tests 113/113).
