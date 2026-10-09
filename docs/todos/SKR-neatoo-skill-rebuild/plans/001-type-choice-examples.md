# Design.Domain example per type choice

**Plan #:** 001
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3
**Status:** Draft
**Last Updated:** 2026-10-09
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** skr-001-type-choice-examples — cut from the arc at Step 2
**PR:** —

---

## Scope

Add to Design.Domain the type choices the rebuilt SKILL.md will point at and that it does not yet have: a context (a plain `[Factory]` instance a screen binds to, with `[Fetch]` and `[Execute]`, D13), a class-level static `[Execute]` that gets-or-makes an aggregate (D11, D12), an interface factory that is a domain service the client calls (D12; never named or shaped as a repository), and a Neatoo entity reused by a server job with no person involved (D4). Rename `DemoValueObject` and its list to an input model, since `ValidateBase` is not a value object (D17), and check that every existing `ValidateBase` example reads as one. Each new type gets a Design.Tests test and `skill-*` regions. It does not touch the skill files, the docs, or the aggregates already there.

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
