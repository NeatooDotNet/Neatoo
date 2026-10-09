# Rebuild the Neatoo skill around the model and placement decisions

**ID:** SKR
**Type:** Enhancement
**Status:** In Progress
**Priority:** High
**Created:** 2026-10-09
**Last Updated:** 2026-10-09
**Initial split:** 7 plans
**Plan cap:** 11
**Arc branch:** skr-arc

---

## Goal

Someone building zCRM, human or Claude, opens `skills/neatoo` and can decide three things for any type, operation or piece of logic: which shape it takes, which attribute it gets, and which tier it runs on. Then they can build it correctly. `SKILL.md` leads with the model (`docs/VisionAlignment.md` section 2 under rulings D1–D19), then the type, operation, tier and placement decisions, then links to mechanism references. Every code block is compiled and tested. Design.App and Design.Server join Design.Domain as snippet sources, for tier composition and for Blazor.

## Acceptance Criteria

- [ ] **AC-1** · Must — `SKILL.md` leads with the model, then the type, operation, tier and placement decisions and the UI contract, consistent with D1–D19.
- [ ] **AC-2** · Must — Every C# block in `skills/neatoo` comes from a compiled `src/Design` region, except blocks labelled WRONG.
- [ ] **AC-3** · Must — Design.Domain has a tested example of each type choice: aggregate, input model, read model, context, static command, class-level `[Execute]`, domain-service interface factory, and an entity reused by a job.
- [ ] **AC-4** · Must — Design.App and Design.Server build and test in CI and supply the tier-composition snippets.
- [ ] **AC-5** · Must — No skill, doc or Design.Domain text teaches running rules at the end of `[Create]` or `[Fetch]` as a routine step.
- [ ] **AC-6** · Should — mudneatoo's Blazor blocks come from Design.App.
- [ ] **AC-7** · Should — The skill names each known framework pain with its issue or analyzer.
- [ ] **AC-8** · Should — The installed copies in `~/.claude/skills` match `skills/` at close.

## Out of Scope

- The analyzers (collection-reference trigger, `LoadValue` inside an operation, discarded `Save()` result): their own todo; the skill links to them as they land.
- The RemoteFactory repository and skill (deferred; RemoteFactory#112 filed).
- Restructuring the user docs beyond the D18 edits AC-5 requires.
- Fixing MudNeatoo components or adding bUnit coverage beyond what AC-6's snippets need.

---

## Plan Index

| # | File | Title (≤ 8 words) | Serves | Status | PR |
|---|------|-------|--------|--------|----|
| 001 | [001-type-choice-examples](./plans/001-type-choice-examples.md) | Design.Domain example per type choice | AC-3 | Draft | — |
| 002 | [002-design-app-server](./plans/002-design-app-server.md) | Design.App and Design.Server in CI | AC-4 | Draft | — |
| 003 | [003-no-end-of-operation-runrules](./plans/003-no-end-of-operation-runrules.md) | Stop teaching end-of-operation RunRules | AC-5 | Draft | — |
| 004 | [004-skill-model-and-decisions](./plans/004-skill-model-and-decisions.md) | SKILL.md: model, decisions, placement, UI contract | AC-1, AC-2 | Draft | — |
| 005 | [005-references-as-mechanism](./plans/005-references-as-mechanism.md) | References reorganized as mechanism | AC-1, AC-2, AC-7 | Draft | — |
| 006 | [006-mudneatoo-from-design-app](./plans/006-mudneatoo-from-design-app.md) | mudneatoo blocks sourced from Design.App | AC-6 | Draft | — |
| 007 | [007-sync-and-verify](./plans/007-sync-and-verify.md) | Installed skills synced and verified | AC-8 | Draft | — |

---

## Punchlist

- [ ] `CLAUDE.md` "Central Pillar" and "The Rules" wording softened to strongly recommended (D19) · `CLAUDE.md` · done when neither word "pillar" nor "must be internal" remains · AC-1 · Must
- [ ] Close #98 as moot under D18 · GitHub · done when closed with a comment naming D18 · AC-5 · Must
- [ ] mudneatoo block count corrected in the audit README (49, not 14) · `docs/todos/consistency-audit/README.md` · done when the line reads 49 · AC-6 · Should

## Dismissed

- SKR · Analyzer for a concrete type where the interface belongs · D19: interface-first is recommended, not required

---

## Discovery Log

---

## Skipped Steps

- 

---

## Sibling Todos

- 

---

## Close-Out Audit

---

## Follow-on

---

## Docs & Retro

---

## Results / Conclusions
