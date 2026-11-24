# The rulings in VisionAlignment

**Plan #:** 001
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-9
**Status:** Draft
**Last Updated:** 2026-11-24
**Plan-review opt-in:** decided at Step 2
**Code-review opt-in:** decided at Step 2
**Branch:** cut from wait-arc at Step 2
**PR:** —

---

## Scope

Records the wait-and-busy rulings as numbered decisions in docs/VisionAlignment.md: tasks are the only source of truth, the framework registers every reaction it starts, waits await tasks only, busy is read from tasks, an async rule that throws surfaces once and the object recovers, and framework-use members may be removed. It names what each ruling replaces. It changes no code and no other document; the guides and skills follow in WAIT-008.
