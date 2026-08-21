# EntityListBase State Machinery — Notification, Replacement, and Paused-Path Defects

**ID:** LIST (assigned 2026-08-21; unique across active and completed todos; registered in `docs/todos/_ids.md`. Plans referenced as `LIST-NNN`.)
**Type:** Bug
**Status:** Not Started
**Priority:** Medium
**Created:** 2026-08-21
**Last Updated:** 2026-08-21

**Sibling of:** [ISNEW](../ISNEW-decouple-isnew-from-ismodified/todo.md) — every item here was
found by an ISNEW gate but does not advance that todo's Goal, so it was carved out at close
rather than dropped or left hanging in a completed container.

---

## Goal

Close the defects in `EntityListBase` / `ValidateListBase` state machinery that the ISNEW arc
surfaced but deliberately did not fix: a stale meta-state baseline that swallows the user's
next edit notification after a save carrying deletions, a `SetItem` that silently orphans the
row it replaces, a paused `Delete()` that drops a child, and a set of guards and notifications
that are load-bearing but asserted nowhere. Two of these are user-visible bugs (items 1 and 5
in the deferred table below); the rest are holes that let a future change reopen a fixed
defect with a green suite.

## Acceptance Criteria

- [ ] After a save that carried child deletions, editing a child again makes the aggregate
      savable — on a local/fat-client save, not only a remote one
- [ ] Replacing a persisted child in a list results in that child's row being deleted, not
      silently orphaned
- [ ] `Delete()` on a child inside a paused window either routes correctly or is prevented
- [ ] The list's `IsModified` transitions raise `PropertyChanged` so bindings and parents see them
- [ ] The pause-guard asymmetry in `HandlePropertyChanged` is asserted, so adding a guard
      "for symmetry" fails a test instead of silently reopening the ISNEW-003 defect
- [ ] `FactoryComplete` on a never-paused list is either correct or explicitly dispositioned
- [ ] The remaining test-infrastructure debt inherited from ISNEW is closed or accepted

## Out of Scope

- `IsNew`/`IsModified` semantics — settled by ISNEW and not reopened here
- The canonical aggregate lifecycle patterns in Design.Domain — settled by ISNEW-001/007

---

## Plan Index

| # | File | Title | Status |
|---|------|-------|--------|
| 001 | [001-metastate-baseline-and-paused-delete](./plans/001-metastate-baseline-and-paused-delete.md) | Meta-state baseline, paused delete, notification holes, inherited test debt | Draft (stub) |
| 002 | [002-setitem-replaced-item](./plans/002-setitem-replaced-item.md) | SetItem: identity + replaced-item persistence | Draft (stub) |

*Both stubs were written as ISNEW-008 and ISNEW-009 and carry their original provenance lines;
cross-references to those IDs in ISNEW's records point here.*

---

## Discovery Log

### 2026-08-21 — LIST (carve-out from ISNEW)
- **Finding:** ISNEW closed with two Draft plans in its container. They do not advance ISNEW's
  Goal (which is met and fully accepted), and the workflow requires every plan in a completing
  todo to be terminal.
- **Decision:** Re-split — carved both plans into this sibling todo, renumbered 008→001 and
  009→002; ISNEW's Index rows are Retired tombstones pointing here.
- **Index changes:** LIST-001 and LIST-002 created from ISNEW-008 and ISNEW-009.
- **Follow-up:** LIST-001

---

## Skipped Steps

- Step 1 reconnaissance — unnecessary; every item arrived with a gate's diagnosis, file
  citation, and reachability analysis attached (see the ISNEW review records).

---

## Sibling Todos

- [ISNEW — Decouple IsNew from IsModified](../ISNEW-decouple-isnew-from-ismodified/todo.md) —
  the arc that surfaced all of this work. Its `reviews/003-code-review.md`,
  `reviews/003-test-review.md`, and `reviews/004-test-review.md` hold the original findings.

---

## Close-Out Audit

_Not yet run._

## Docs & Retro

_Filled at Step 8._

## Results / Conclusions

_Filled at Step 8._
