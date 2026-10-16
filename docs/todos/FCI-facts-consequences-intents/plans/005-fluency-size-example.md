# Compiled fluency-size aggregate in Design.Domain

**Plan #:** 005
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-5, AC-11
**Status:** Draft
**Last Updated:** 2026-10-16
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** —
**PR:** —

---

## Scope

A Design.Domain aggregate the size of zTreatment's fluency tab, built rules-first in a domain of its own: a root holding an approved total and a child list; a slider-shaped fact per child; a fit rule across siblings that converges on the equality short-circuit; the scoped anchor a gesture sets for one fit; a lock that is an intent written as an ordered write; an admission fact on a meta trigger; a next-mark fact the UI writes. Every rule and verb uses the awaited `SetX`. Design.Tests pin each behaviour. Optional, AC-11: the open finishes inside a non-remote `[Execute]` seam that calls the `[Remote]` Fetch and settles client-side inside the operation.
