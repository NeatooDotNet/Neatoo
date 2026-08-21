# Test Review Record — ISNEW-003 — 2026-08-21

**Reviewer:** test-reviewer agent. **Gate: PASSES after the fix loop** (2 must-cover, 3
should/nice, all closed or dispositioned).

## must-cover findings and fixes

1. **The cached-modified recalculation was asserted only in the negative direction — and it
   is the exact seam ISNEW-004 flips.** Routing `FactoryComplete` through `ResumeAllActions`
   means `_cachedChildrenModified` is now recalculated on *every* factory completion, not
   just `Update`. Every existing assertion pointed at `false`, which the old stale cache also
   produced, so deleting the recalculation would have broken nothing. Worse, the reviewer
   spotted a **live behavior change shipped without an assertion**: because
   `EntityBase.IsModified` still includes `|| IsNew`, `InvoiceLineList.CreateWithStandardLines`
   (two new children added while paused) silently flipped `invoice.Lines.IsModified` from
   false to true. **Fixed:** a unit test pins the positive direction
   (`EntityListBaseTests.FactoryComplete_AfterPausedAddOfModifiedItem_ListReportsModified`),
   `Add_WhenPaused_DoesNotMarkModified` now also asserts the *resume* manufactures no dirt,
   and the rich-create integration test pins `Lines.IsModified` with the pre-flip expectation
   stated — that row is the ISNEW-004 baseline.
2. **`ListFactoryStateTests` repeated closed finding 002 #1 — no boundary proof.** Since the
   store is static and both containers share the process, dropping `[Remote]` would break no
   assertion; it bit hardest on the test cited as the *sole* support for the deserialization
   bullet, whose premise is the very thing it did not assert. **Fixed:** renamed to
   `FetchedChild_CrossesTheBoundary_AndKeepsIdentity` and it now asserts `RemoteCallCount`
   advanced (using the recorder added in the ISNEW-006 infra pass).

## should-cover / nice-to-have

3. **Busy half of the cache fix unpinned** — validity had a regression test, `IsBusy` did not,
   though it is a bare cache read feeding `IsSavable`. **Fixed:**
   `FactoryComplete_AfterPausedAddOfBusyItem_ListReportsBusy`, using the existing
   `MarkBusyForTest` helper. (Note the live add path *throws* on busy items while the paused
   path does not — the framework conceding the scenario is real.)
4. **Factory completion now swallows meta-property change notifications** — `ResumeAllActions`
   calls `ResetMetaState()` rather than `CheckIfMetaPropertiesChanged()`, so state changes
   during the paused window rebase silently instead of raising `PropertyChanged`. This
   matches what `OnDeserialized` already did (a defensible unification, and the pre-existing
   behavior for deserialization), but it is user-visible for anything bound across a fetch or
   save. **Routed to the opted-in code review** as the reviewer recommended, since the right
   answer may be a code change rather than a test.
5. **Overreaching comment** on `FetchedChild_IsMarkedAsChild` (claimed to distinguish
   server-side marking, which a client-side assertion cannot). **Fixed:** comment now points
   at the unit-level test that does pin the mechanism.

## Sacred tests

All three flipped tests verified as genuine characterization with subject and setup intact;
no other pre-existing test lost coverage. One accuracy note accepted: the "comments described
the mechanism" justification holds for two of the three — `Add_WhenPaused_DoesNotMarkAsChild`
had no rationale comment at all — and the load-bearing justification for all three is the
ISNEW-001 Discovery Log entry recording the gap as a defect before implementation began.

## Tech debt queued to ISNEW-006

- `HandlePropertyChanged` has no pause guard while `InsertItem`/`RemoveItem`/`SetItem` all do.
  That asymmetry is now load-bearing (it is why defect 1's window is narrow) but is asserted
  nowhere and documented only inside a test comment — adding a guard "for symmetry" would
  reopen the defect with a green suite.
- `EntityListBase.IsModified` raises no `PropertyChanged` (standing NOTE in
  `EntityListBaseTests`) — a real hole for Blazor bindings, interacting with finding 4.
- The paused `InsertItem` branch skips the duplicate-add, busy-item, and cross-aggregate
  guards the live path enforces — defensible for trusted input, unasserted either way.
- `ResumeAllActions` is guarded by `if (IsPaused)`, so `FactoryComplete` on a never-paused
  list silently skips recalculation; several unit tests drive `FactoryComplete` without
  `FactoryStart` and never touch the new path.

## Logs

`003-build.log` / `003-test.log`, `003-design-build.log` / `003-design-test.log`. **These
archived logs predate this plan's fix loop** (they show 2160 and 113) — the verified final
state for the whole arc is in `reviews/final-test.log` (2178) and `final-design-test.log` (129).
