# Sync, then fresh-session test

**Plan #:** 007
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-8, AC-9
**Status:** Draft
**Last Updated:** 2026-10-09
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** skr-007-sync-and-verify — cut from the arc at Step 2
**PR:** —

---

## Scope

Copy `skills/neatoo` and `skills/mudneatoo` to `~/.claude/skills/`, confirm the copies are byte-identical, and run `dotnet mdsnippets` and the hand-written-block count one last time. Then test the skill on a fresh session, not on the orchestrator: launch a fresh-context agent from a directory that does not contain this repository's `CLAUDE.md` (so no doctrine leaks in beside the skill), give it only the installed skill and the five zCRM scenarios listed under the habit-trap table, and score its output row by row against that table. Every row it falls into becomes a punchlist fix to the skill; re-sync and re-run until it falls into none (AC-9). The orchestrator's own cold read is not evidence. A repeatable eval suite (`claude plugin eval`) is a sibling todo if wanted, not this plan.

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
