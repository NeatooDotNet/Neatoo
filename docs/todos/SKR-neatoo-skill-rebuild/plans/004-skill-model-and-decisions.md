# SKILL.md: model, decisions, placement, UI contract

**Plan #:** 004
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-2
**Status:** Draft
**Last Updated:** 2026-10-09
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** skr-004-skill-model-and-decisions — cut from the arc at Step 2
**PR:** —

---

## Scope

Rewrite `skills/neatoo/SKILL.md` so it leads with the model (VisionAlignment section 2, rewritten under D1–D19), then four decision sections a reader uses in order: choosing a type (aggregate, input model, read model, context, static command, class-level `[Execute]`, domain-service interface factory, value object; when Neatoo, when plain RemoteFactory, D4), choosing an operation (D3, D11, D14), tier placement (what runs where and what decides: constructor vs `[Service]` injection, what ships to the browser, what registers on each tier), and logic placement (the rule / verb / seam / read model ladder by who drives it, and the UI contract: the ViewModel binds and adapts gestures, decides nothing, D16). Interface-first becomes strongly recommended with the `IsSavable` trap as the reason (D19). Every code block is a region from plans 001–003's Design projects. Mechanism content currently in `SKILL.md` moves to `references/` in plan 005; this plan leaves pointers. The frontmatter description is rewritten so the skill triggers on placement questions, not only on Neatoo type names.

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
