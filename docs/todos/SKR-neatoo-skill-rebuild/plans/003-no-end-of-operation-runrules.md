# Stop teaching end-of-operation RunRules

**Plan #:** 003
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-5
**Status:** Draft
**Last Updated:** 2026-10-09
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** skr-003-no-end-of-operation-runrules — cut from the arc at Step 2
**PR:** —

---

## Scope

Apply D18 across the skill, the user docs and Design.Domain: running rules at the end of a `[Create]` or `[Fetch]` is the user's rare decision, not a step, because persisted data is assumed valid and a derived value an operation needs is assigned inside it. Rewrite the sites that teach it (`SKILL.md`, `rules-lifecycle.md`, `validation.md`, `blazor.md`, `domain-logic-placement.md`, `pitfalls.md`, `testing.md`; `docs/guides/async.md`, `blazor.md`, `business-rules.md`, `validation.md`, `docs/reference/api.md`; `CommonGotchas.cs` gotcha 1 and its summary table; `WorkOrder`'s `[Fetch]`), replacing the `CreateWithRunRules` and `Gotcha1_RulesFireAfterCreate_WithExplicitRunRules` regions with ones that assign derived values inside the operation, and keep one place that says when a user might still call `RunRules` after an operation. Close #98 as moot. It does not reorganize the skill; plan 004 does.

---

## Intent

## Framework & Architectural Alignment

## Constraints & Invariants

## Steps

## Acceptance

## Current State (Pre-Flight)

## Punchlist

## Test Evidence

## Gate Record

## Plan Amendments

## Notes
