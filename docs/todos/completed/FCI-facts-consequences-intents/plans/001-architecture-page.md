# The architecture page in VisionAlignment

**Plan #:** 001
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-8
**Status:** Done
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (prose; every sentence was approved by Keith in the design session of 2026-10-15/16)
**Code-review opt-in:** No (no code)
**Branch:** fci-001-architecture-page
**PR:** #115

---

## Scope

Rewrite VisionAlignment section 2's statement 8 as facts, consequences and intents, add a fact row to the section 3 concept table beside the verb row, and record the page's decisions as rulings: the awaited `SetX` standard, meta-state triggers, the scoped anchor as the canonical evaporating intent, no new write path, no trigger passed into a rule. Draft the "where does business logic go" wording for RemoteFactory#173 from it. Does not touch the skills, which quote the page once the compiled example exists.

## Intent

- VisionAlignment is the ruled basis every skill plan cites; the skills cannot change before it does.
- desktopRemoteFactory gets a committed sentence to quote, not a chat message.

## Framework & Architectural Alignment

- VisionAlignment section 0: a ruling wins over the text below it; new rulings go in a dated table.
- No ruling IDs leak into skills or `src/Design`; they live here only.

## Constraints & Invariants

- Statements 1 to 7 and 9 are unchanged; RemoteFactory quotes 1 to 4 and 6 verbatim.
- Every claim about framework behaviour is one verified from source in the design session.

## Steps

1. Replace statement 8 with facts, consequences, intents, and keep the rule-verb-seam-read model ladder as where each one lives.
2. Add the fact row and rewrite the verb row in section 3; say the aggregate is held by rules, not a façade.
3. Add the 2026-10-16 rulings table.
4. Write the #173 wording into this plan's Notes.

## Acceptance

- [x] Statement 8 states facts, consequences and intents and the test "intent that survives as state is a property" `[explicit-skip: prose]`
- [x] Section 3 has a fact row and a verb row that says a verb survives only for what state cannot hold `[explicit-skip: prose]`
- [x] The 2026-10-16 rulings record the awaited write, meta triggers, the anchor, and the two things ruled out `[explicit-skip: prose]`

## Current State (Pre-Flight)

- `docs/VisionAlignment.md:191` statement 8 "Logic is placed by who is driving it"; `:195` the ownership sentence; `:234-236` Invariant, Derived value, Behaviour rows; `:243` Consistency boundary; rulings tables end at `:47` (D26).

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Statement 8 | Required | `[explicit-skip]` | prose, read in the diff | ✓ |
| Section 3 rows | Required | `[explicit-skip]` | prose, read in the diff | ✓ |
| 2026-10-16 rulings | Required | `[explicit-skip]` | prose, read in the diff | ✓ |

## Gate Record

- Test review skipped: prose-only plan with no behaviour to pin (Skipped Steps).

## Notes

**The wording for RemoteFactory#173**, for `SKILL.md`'s quick-reference row and `references/class-factory.md` rule 6:

> In the entity, not the factory: first as a property a later reader needs and the rules that follow from it, a verb only for intent the state cannot hold. The Neatoo skill places it.

For the restated sentence at the RF `SKILL.md`'s Neatoo pointer block, in place of "logic is placed by who is driving it": "a fact is a property, a consequence is a rule, an intent is a verb and survives only for what state cannot hold".
