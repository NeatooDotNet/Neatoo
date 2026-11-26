# The rulings in VisionAlignment

**Plan #:** 001
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-9
**Status:** Done
**Last Updated:** 2026-11-24
**Plan-review opt-in:** No (prose; every ruling was decided by Keith in the session of 2026-11-24)
**Code-review opt-in:** No (no code)
**Branch:** wait-001-decision-record
**PR:** #143

---

## Scope

Records the wait-and-busy rulings as numbered decisions in `docs/VisionAlignment.md`: tasks are the only source of truth, the framework registers every reaction it starts, waits await tasks only and busy is read from tasks, an async rule that throws surfaces once and the object recovers, `Neatoo.Internal` is framework-only, and new framework parts are built the DI way. It names what each ruling replaces. It changes no code and no other document; the guides and skills follow in WAIT-008.

---

## Intent

- VisionAlignment is the ruled basis the code plans cite. They cannot be drafted against a design that exists only in a conversation.
- A reviewer checking WAIT-002 to WAIT-007 has one place that says what Keith decided, in his words where he gave them.

---

## Framework & Architectural Alignment

- VisionAlignment section 0: a ruling wins over the text below it; new rulings go in a dated table with what they supersede.
- No ruling ID leaks into the skills or `src/Design`; they live in VisionAlignment and in todos only.

---

## Constraints & Invariants

- Earlier rulings are not edited. D2 (a throw is an application failure) and D28 (domain code awaits its writes) stand; the new rulings say how they relate.
- A ruling records what Keith decided. A design detail he has not ruled on (how a list reads busy cheaply, adoption of in-flight work, duplicate exceptions) is not written as a ruling.

---

## Steps

1. Add a dated table of rulings after D30, each with what it supersedes.
2. Record Keith's two later decisions (the `Internal` namespace, the DI approach) and his approval of the architecture in the todo header.

---

## Acceptance

- [x] VisionAlignment carries the six rulings, each naming what it supersedes `[explicit-skip: prose, read against the session of 2026-11-24]`
- [x] The todo header attributes the `Internal` and DI decisions to Keith with the date `[explicit-skip: prose]`

---

## Current State (Pre-Flight)

- `docs/VisionAlignment.md` section 0 ends with the table "Rulings of 2026-10-16", D27 to D30 (lines 49 to 56).
- `WaitForTasks` appears in D20 (a job awaits it and reads `IsSavable`) and `IsBusy` in D29 (meta-state triggers). Neither conflicts with the new rulings.
- `ValidateBase` constructs its tracker with `new AsyncTasks()`; `IValidateBaseServices<T>` supplies the property manager, the property factory, the rule manager and the logger.

---

## Punchlist

---

## Test Evidence

No behavior. Both bullets are prose.

---

## Gate Record

- Skipped: a prose-only plan with no behavior. Recorded in the todo's Skipped Steps.

---

## Plan Amendments

---

## Notes

The rulings quote Keith where he gave words: the `Internal` namespace and the DI approach.
