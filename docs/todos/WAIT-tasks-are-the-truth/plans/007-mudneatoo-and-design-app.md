# MudNeatoo and Design.App on the new tracking

**Plan #:** 007
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-8
**Status:** Draft
**Last Updated:** 2026-11-24
**Plan-review opt-in:** decided at Step 2
**Code-review opt-in:** decided at Step 2
**Branch:** cut from wait-arc at Step 2
**PR:** —

---

## Scope

Proves the reported case at the component level: a MudNeatoo input writes a property whose async rule is in flight, a handler awaits WaitForTasks on the entity, and it completes when the rule does. It moves the MudNeatoo inputs and the Design.App pages and ViewModels off any removed member, and checks their busy display against the new busy state. It adds no new MudNeatoo feature.
