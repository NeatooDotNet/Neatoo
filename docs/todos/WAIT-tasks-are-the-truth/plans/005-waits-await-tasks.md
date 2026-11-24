# Waits await tasks; busy reads tasks

**Plan #:** 005
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-2, AC-3, AC-4, AC-5
**Status:** Draft
**Last Updated:** 2026-11-24
**Plan-review opt-in:** decided at Step 2
**Code-review opt-in:** decided at Step 2
**Branch:** cut from wait-arc at Step 2
**PR:** —

---

## Scope

Makes WaitForTasks on an entity the entity's registered tasks and nothing else, with the token overload the same wait plus cancellation, and removes the property manager's polling loop and the list's. Entity and list busy state is read from the trackers, and the cached busy flags go. It adopts the legacy cancellation todo. It does not touch event invocation, save or MudNeatoo.
