# The Flip: IsModified / IsSavable / Attach-Marking

**Plan #:** 004
**Date:** 2026-08-21
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-21
**Plan-review opt-in:** TBD at draft (public API semantics change — expect Yes)
**Code-review opt-in:** TBD at draft (expect Yes)

---

## Scope

_Stub — Scope only; flesh out at Step 2._

The core semantic change per [design.md](../design.md) Decided Design: `EntityBase.IsModified`
drops the `IsNew` term; `IsSavable` and the `Save()` guard admit `IsNew`; `MarkNew()` stays
pure; dirty-on-create becomes opt-in via `MarkModified()` in the `[Create]` body;
attach-marking lands (un-paused `InsertItem`/`SetItem` and entity-child property assignment
mark the attached item modified, replacing the weld's child-flow job). Update the pinned
tests named in design.md, add the new state-transition and serialization round-trip coverage,
and write the code-level Why comments at the changed seams. Depends on the verified baseline
(ISNEW-001…003).

Routing note carried from the ISNEW-001 code review: generated Save routing is `IsDeleted`
first, then `IsNew`, else Update — IsModified is never consulted, and a created-then-deleted
root routes to `[Delete]` (no silent no-op). The flip's Save()/IsSavable changes must keep
that path sane (a new+deleted entity is IsModified=true via IsDeleted, hence savable, hence
routed to a [Delete] for a row that never existed — decide whether that stays app-tolerated
or warrants a routing/guard change, coordinating with RemoteFactory if so).
