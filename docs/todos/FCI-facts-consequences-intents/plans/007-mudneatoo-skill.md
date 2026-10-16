# MudNeatoo skill: components and verb-bound controls

**Plan #:** 007
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-7
**Status:** Draft
**Last Updated:** 2026-10-16
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** —
**PR:** —

---

## Scope

Replace skills/mudneatoo's page pattern, where the page subscribes to the root and redraws everything, with the component as the unit of refresh: each control already refreshes from its own property wrapper, so the page does not subscribe. Teach the verb-bound control, which reads a fact and writes an intent, which needs #113's `Setter` parameter on the inputs; this plan ships that parameter. Correct the pass-through claim in #94. Blocks from Design.App regions. Boundary components, the throttle and the slider gaps stay with FCI-009.
