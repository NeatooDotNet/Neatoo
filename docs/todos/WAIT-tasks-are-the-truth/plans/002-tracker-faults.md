# The tracker faults once, then recovers

**Plan #:** 002
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-7
**Status:** Draft
**Last Updated:** 2026-11-24
**Plan-review opt-in:** decided at Step 2
**Code-review opt-in:** decided at Step 2
**Branch:** cut from wait-arc at Step 2
**PR:** —

---

## Scope

Changes the task tracker so that a faulted sequence reports its exceptions to the wait that observes it and the next task starts a clean sequence, and so that an exception of any type from the completion callback still completes the wait. It adds the tests for an async rule's fault coming out of WaitForTasks, including the one that is commented out today. It does not change what registers tasks or how waits are composed.
