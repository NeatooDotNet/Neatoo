# Subscribers, notification order, save in flight

**Plan #:** 006
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-10, AC-11, AC-12
**Status:** Draft
**Last Updated:** 2026-11-24
**Plan-review opt-in:** decided at Step 2
**Code-review opt-in:** decided at Step 2
**Branch:** cut from wait-arc at Step 2
**PR:** —

---

## Scope

Covers the three Optional criteria: raising NeatooPropertyChanged awaits every subscriber's task; the falling IsBusy notification is raised before WaitForTasks completes, with the flaky test of #129 made deterministic; and a save in flight registers as work so the entity is busy and a second Save is refused (#103). Each is built if it is cheap and in reach once WAIT-005 is in, and parked otherwise.
