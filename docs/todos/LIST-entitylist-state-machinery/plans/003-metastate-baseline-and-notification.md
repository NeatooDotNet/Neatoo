# Stale Meta-State Baseline After a Save Carrying Deletions

**Plan #:** 003
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md) (re-split out of the original LIST-001)
**Status:** Draft
**Last Updated:** 2026-08-21
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** Yes — notification machinery; user-visible defect.

---

## Scope

_Stub — Scope only; flesh out at Step 2._

The headline user-visible defect of this todo. On `FactoryComplete(Update)` the resume path
snapshots the meta-state baseline **while `DeletedList` is still populated**, so the baseline
records `IsModified = true`; the Update branch then clears `DeletedList`, making the real value
`false` — with nothing to announce the transition or correct the baseline. The list is left at
actual-`false` / baseline-`true`, so the user's *next* child edit compares true-against-true,
raises no `PropertyChanged`, and the parent's cached `IsModified` never refreshes. The aggregate
reports not-savable with real unsaved work in it.

Reachability confirmed by inspection at ISNEW close-out and re-walked 2026-08-21:
`EntityListBase.IsModified => _cachedChildrenModified || DeletedList.Any()`
(`EntityListBase.cs:64`); baseline snapshot is `ResetMetaState()` →
`EntityMetaState = (IsModified, IsSelfModified)` (`:185-189`), reached via
`base.FactoryComplete` → `ResumeAllActions` **before** the `DeletedList.Clear()` at `:455`.

**The suite cannot currently see this**, and that is the harder half of the plan: every
SaveLifecycle fixture is `[Remote]`, so the post-save graph is deserialized and `OnDeserialized`
rebuilds a correct baseline. The hole is open only on a **local / fat-client save**. Any fix
shipped without a non-remote save path in the harness would come with a test that passes
whether or not the fix is present.

Also folded in, same theme: the standing NOTE in `EntityListBaseTests` (~line 820) asserting
that `EntityListBase.IsModified` raises no `PropertyChanged` is **stale** — `_cachedChildrenModified`
is a cache and `CheckIfMetaPropertiesChanged` does `RaiseIfChanged(..., nameof(IsModified))`
(`EntityListBase.cs:172-179`). The NOTE must be retired and replaced with the notification tests
it defers, which are the same tests that pin this defect. Additionally, the
`HandlePropertyChanged` pause-guard asymmetry is load-bearing but asserted nowhere: adding a
guard "for symmetry" would silently reopen the ISNEW-003 defect with a green suite.

Likely fix is `CheckIfMetaPropertiesChanged()` at the end of the Update branch rather than
leaving the baseline where the resume put it — but the disposition is decided at draft, and the
non-remote harness gap is the gating work.
