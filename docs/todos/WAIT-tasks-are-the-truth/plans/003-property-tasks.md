# A property holds its work as tasks

**Plan #:** 003
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-6, AC-3
**Status:** Draft
**Last Updated:** 2026-11-24
**Plan-review opt-in:** decided at Step 2
**Code-review opt-in:** decided at Step 2
**Branch:** cut from wait-arc at Step 2
**PR:** —

---

## Scope

Replaces the property's busy bool and its list of busy ids with a task tracker on the property: each write's task and each async rule's task that names the property enter it, the property is busy while it is running, and waiting on the property awaits it. The rule manager hands the property a task where it handed it an id. The framework-use busy-marking members are removed. Overlapping writes and a faulted rule leave the property's busy state correct and announced. It does not change what the entity registers or how the entity waits.
