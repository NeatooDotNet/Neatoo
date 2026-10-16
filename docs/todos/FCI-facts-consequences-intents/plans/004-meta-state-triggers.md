# Meta-state triggers and the IsBusy analyzer

**Plan #:** 004
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4
**Status:** Draft
**Last Updated:** 2026-10-16
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** —
**PR:** —

---

## Scope

When `IsValid`, `IsSelfValid`, `IsBusy` or `IsModified` changes outside a pause, the object runs the rules triggered by that name, so `CanApprove = Status == Pending && IsValid` is an ordinary rule. An analyzer refuses an async rule whose trigger names `IsBusy`, the one loop that exists by construction. Integration tests pin that a meta trigger fires once per flip, is skipped while paused, and that a sync `IsBusy` rule converges. Does not add loop detection; termination stays the author's, tested.
