# SKILL.md: model, habit traps, decisions, UI contract

**Plan #:** 004
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-2, AC-10 (and AC-7 for the pains the skill names)
**Status:** Draft
**Last Updated:** 2026-10-12 (drafted from the stub after recon)
**Plan-review opt-in:** Yes — this is the deliverable the todo exists for; Pass A against D1–D25 as clarified, Pass B against the regions and the three plans' examples
**Code-review opt-in:** No — the deliverable is prose plus rendered regions; the test-reviewer's map check and plan 007's fresh-session test are the gates that fit
**Branch:** skr-004-skill-model-and-decisions — cut from the arc at Step 2
**PR:** —

---

## Scope

Rewrite `skills/neatoo/SKILL.md` from 863 lines to about 400, organized around the questions a mainstream .NET developer asks by habit, because those are what get reached for under load. The skill leads with the model (VisionAlignment section 2, statements 5, 7, 8, 9 in Neatoo's voice, 1–4 and 6 as the pointer to the RemoteFactory skill), then the habit-trap table from `todo.md` as the spine — every Neatoo-owned row (T1, T2, T7–T10, T12, T16, T17, and T11's Neatoo half) answered at its decision point, phrased against the question the habit asks, with the compiled example a reader copies instead; the RemoteFactory-owned rows (T3–T6, T13–T15, T18–T20, T11's RF half) appear as one pointer block that names the RF skill's row and nothing more. Then the Neatoo-owned decisions: when Neatoo and when plain RemoteFactory (statement 7, D4); baseline versus the user's work (statement 5, D18 as clarified: the operation computes what it needs, never a routine `RunRules(All)` step, never a validate step); logic placed by who drives it (statement 8, the ladder); the UI contract (statement 9, D16: the ViewModel binds and adapts gestures, decides nothing; the save handler's shape; the one allowed mapping); interface-first strongly recommended with the `IsSavable` trap as the reason (D19); the two rule shapes that reach the server (D1, D21, no preference); entity duality (D23); value object is a `record`, `ValidateBase` is an input model (D17, D25). Every code block is a region from `src/Design` (plans 001–003 supplied the missing shapes); the one exception is a short block labelled WRONG. Mechanism content (property system, `Parent`, three-phase save, lists, lazy load, testing, troubleshooting) leaves `SKILL.md` for `references/` in plan 005; this plan leaves one-line pointers so nothing is lost between the two PRs. The frontmatter description is rebuilt from the habit questions so the skill triggers on them, not only on type names, and drops the LazyLoad triggers (D24). The skill names the framework pains it must warn about with their issue (Neatoo#103 double-click Save; `[Remote]`/`AddNeatooAspNetCore` → RemoteFactory#117 as a pointer; the pending analyzers by name) — the pains list itself is plan 005's reference, this plan names them inline where the trap is.

Out of this plan: `references/*` (005), mudneatoo (006), the installed copy and the fresh-session test (007), any Design code change (if a trap needs an example that does not exist, that is a finding for the plan review, not a side quest).

---

## Intent

- A reader who asks any question in the trap table finds the answer within the first screen after the model, phrased against their question, with one compiled block to copy — not a prohibition.
- A reader who has to decide type, operation, tier or placement for a piece of zCRM logic finds the Neatoo-owned decision stated once, and a pointer when the decision is RemoteFactory's.
- A reader who copies a block gets code that compiles and is tested; the WRONG blocks are the only hand-written C# and are labelled.
- A fresh session given this skill and the RemoteFactory skill (plan 007) falls into none of the trap rows on the zCRM scenarios.

---

## Framework & Architectural Alignment

- VisionAlignment §2 statements 5, 7, 8, 9 are the Neatoo skill's model; 1–4 and 6 are RemoteFactory's (owner split, 2026-10-09; both skills assumed loaded together, 2026-10-11). The model is stated in the skill's words, not copied.
- Rulings applied in the text: D1/D21 (two rule shapes, no preference), D2 (rules over exceptions), D4 (an entity is right with no person), D6 (server gate stays, callers never mirror it), D16 (facts, not screen names), D17/D25 (record vs input model), D18 as clarified 2026-10-11 (compute what the operation needs; targeted not `All` from a root with child-writing rules; never a validate step), D19 (interface-first recommended), D20 (job shape), D23 (duality), D24 (no LazyLoad teaching; pending removal noted once).
- The three things the rebuilt skill must do that the current one does not (todo, 2026-10-09): answer the habit's questions, compiled examples over prose, tested on a fresh session. This plan does the first two; 007 the third.
- `.claude/rules/design-snippets.md`: every C# block is a region; WRONG blocks labelled; `dotnet mdsnippets` after edits.
- Memory `feedback-no-validate-step` and the 2026-10-11 over-correction: T1 and T10 are stated as one distinction (no validate step before Save; `RunRules` inside an operation for what it needs is normal), not as a rule in either direction.
- RemoteFactory skill sync (plan 007 dependency): the pointer block names RF rows by their trap number and topic, not by RF file name, so it survives the RF rebuild.

---

## Constraints & Invariants

- `SKILL.md` ≤ ~400 lines (AC-10); the trap table within the first screen after the model.
- Every C# block is a `<!-- snippet: skill-* -->` placeholder filled by `dotnet mdsnippets`; hand-written C# only in blocks labelled WRONG, and at most one per trap that needs it.
- Every Neatoo-owned trap row is answered (AC-1); every RF-owned row appears exactly once, in the pointer block, with no restated answer.
- No sentence in `SKILL.md` contradicts a ruling or a `references/` file that survives 005; where the old text taught a prohibition the new text states the model instead.
- No LazyLoad teaching; one sentence says it is being removed (D24) and points at the RF skill's replacement.
- The frontmatter `description` lists the habit questions and the type names; it is what makes the skill trigger.
- No ruling IDs (Dn) in the skill's prose: the skill states the doctrine, `VisionAlignment.md` is the record. Trap numbers (Tn) appear only in the table and the pointer block.
- `dotnet mdsnippets` reports no duplicate or missing regions after the rewrite; regions the old `SKILL.md` used and the new one drops stay in Design (005 decides their homes).

---

## Steps

1. Write the frontmatter: name, a description built from the habit questions (T1–T20's questions in plain words) plus the type and property names, minus LazyLoad; version bump.
2. Write the model: a one-screen statement of what Neatoo is (the editing session, statement 7), what an operation sets versus what the user does (5), who drives logic (8), what the UI does (9), and the pointer to the RemoteFactory skill for the seam, the attributes, `[Remote]` and self-writing (1–4, 6).
3. Write the habit-trap table: Neatoo-owned rows with question / answer / compiled example (region name and file); the RF-owned rows as a pointer block; T1 and T10 phrased as one distinction.
4. Write the decisions, one short section each with its region: Neatoo or plain RemoteFactory (job over entities, `skill-job-over-entities`); baseline vs user's work (`skill-fetch-computes-derived`, `skill-create-is-new-not-modified`); the ladder (rule / verb / seam / read model with `skill-can-x-rule`, `skill-entity-verb`, `skill-command`, `skill-read-model`); the UI contract (`skill-save-handler`, `skill-viewmodel-over-aggregate`, `skill-page-binds-facts`); interface-first and the `IsSavable` trap (`skill-aggregate-interfaces`, `skill-child-interface-no-save`); rules that reach the server (`skill-rule-with-command`, `skill-rule-with-domain-service`); duality (`skill-entity-both-roles`); record vs input model (`skill-value-object` — rename pending 005). The server gate (`skill-server-gate-refuses`) stays as the one thing callers never mirror.
5. Write the pains inline where the trap is: double-click Save (Neatoo#103), the `AddNeatooAspNetCore` order (RemoteFactory#117, pointer), the three pending analyzers by name, the `Save()` result (T13 pointer). Write the reference index: one line per `references/` file that will exist after 005, by the decision it serves.
6. Move nothing yet, delete the mechanism sections from `SKILL.md`, and leave a one-line pointer where each lived so 005 finds its content in git history and the reader finds the reference. Run `dotnet mdsnippets`; confirm every placeholder filled, no duplicates, line count.
7. Self-test against the trap table: for every row, the answer is findable from the question alone; record the line numbers in the Test Evidence map.

---

## Acceptance

- [ ] Every Neatoo-owned trap row (T1, T2, T7, T8, T9, T10, T11-Neatoo, T12, T16, T17) is answered in `SKILL.md` at a decision point, phrased against the question, with a filled compiled block beside it `[explicit-skip: table walk, line numbers recorded]` · Must
- [ ] Every RF-owned row (T3–T6, T11-RF, T13–T15, T18–T20) appears once, in the pointer block, with no restated answer `[explicit-skip: grep]` · Must
- [ ] Every C# block in `SKILL.md` is a filled `skill-*` snippet except blocks labelled WRONG; `dotnet mdsnippets` reports no missing or duplicate regions `[explicit-skip: tooling + grep for fenced blocks without a placeholder]` · Must
- [ ] `SKILL.md` leads with the model; the trap table starts within the first screen after it; total ≤ ~400 lines `[explicit-skip: line count]` · Should
- [ ] No sentence teaches a prohibition where the model gives the answer; no `RunRules` step before Save; no LazyLoad teaching; no ruling IDs in prose `[explicit-skip: read-through + grep]` · Must
- [ ] The frontmatter description contains the habit questions and triggers on them `[explicit-skip: read]` · Must
- [ ] The named pains carry their issue numbers (Neatoo#103, RemoteFactory#117) and the analyzers are named `[explicit-skip: grep]` · Should
- [ ] Each mechanism section removed from `SKILL.md` leaves a pointer to the `references/` file 005 will give it `[explicit-skip: diff walk]` · Must

---

## Current State (Pre-Flight)

Walked 2026-10-12.

- `skills/neatoo/SKILL.md`: 863 lines; sections Quick Start (:15), Interface-First Design (:193), Domain Logic First (:256, ladder/mirror/gesture/smell), Three-Phase Pattern (:328), Aggregate Is a Graph (:376), Rules for Open Mutation (:392), Parent (:416–534), Base Class Quick Reference (:535), Key Properties (:548), Core Patterns (:563–797: properties, factory methods, save routing, server gate, cascading, validation, rules-don't-fire, child triggers), Testing (:798), Reference Documentation (:802), Troubleshooting (:824), IsNew vs IsModified (:828). 17 snippets; 1 WRONG block. Frontmatter description names LazyLoad triggers (`EntityLazyLoad`, `IEntityLazyLoadFactory`, `IsLoaded`, `IsLoading`).
- Regions available in `src/Design`: 149 (`skill-*`), including the plans' new ones: `skill-job-over-entities`, `skill-rule-with-domain-service`, `skill-domain-service-interface-factory`, `skill-entity-both-roles`, `skill-verb-submit`, `skill-client-composition`, `skill-server-composition`, `skill-save-handler`, `skill-viewmodel-over-aggregate`, `skill-page-order-edit`, `skill-page-binds-facts`, `skill-derived-facts`, `skill-fetch-computes-derived`, `skill-create-computes-derived`, `skill-create-computes-what-it-needs`. Regions named `skill-value-object*` still exist under those names (rename in 005).
- `todo.md` trap table: T1–T20 with Owner column; T10 reworded 2026-10-11; T17 names `IOrderInterfaces`; T16 names `EmployeeSummary` (a `record`? — check it exists as a region or a type; `skill-value-object` is the `ValidateBase` input model).
- `VisionAlignment.md` §2: nine statements; §2.1 the incident clusters (the three largest: logic in the UI, placement among rule/verb/seam/read model, baseline state).
- `references/`: 14 files (base-classes, blazor, collections, domain-logic-placement, entities, lazy-loading, pitfalls, properties, rules-lifecycle, shared-rules, source-generation, testing, trimming, validation). 005 reorganizes them; this plan's reference index names the post-005 set and 005 reconciles.
- RemoteFactory skill: being rebuilt by the sibling todo (desktopRemoteFactory); not yet synced to `~/.claude/skills`. The pointer block names rows and topics, not file names.
- Open punchlist rows touching this plan: Neatoo#103 pain (AC-7) — named inline here, listed in 005's pains reference.

---

## Punchlist

- [ ] Neatoo#103 named inline at T1/the save handler · `SKILL.md` · done when the pain and issue appear beside the save-handler block · AC-7 · Should (pulled from the todo, 2026-10-12; the pains *reference* is 005's)

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Evidence | Confirmed |
|---|---|---|---|---|
| Every Neatoo-owned trap row answered with a block | Must | `[explicit-skip]` | | |
| RF rows once, pointer only | Must | `[explicit-skip]` | | |
| Every C# block a filled snippet (or WRONG) | Must | `[explicit-skip]` | | |
| Model first, table in first screen, ≤ ~400 lines | Should | `[explicit-skip]` | | |
| No prohibition-teaching, no validate step, no LazyLoad, no Dn | Must | `[explicit-skip]` | | |
| Frontmatter triggers on the questions | Must | `[explicit-skip]` | | |
| Pains carry issue numbers | Should | `[explicit-skip]` | | |
| Removed sections leave pointers | Must | `[explicit-skip]` | | |

---

## Gate Record

-

---

## Plan Amendments

### 2026-10-12 — Drafted from the stub

- **Section affected:** Scope, Steps, Acceptance
- **Original said:** the structure (model, trap table, decisions, pointer block, UI contract, interface-first, ~400 lines, frontmatter from the questions); mechanism moves to 005.
- **What changed:** the decisions list is enumerated with the region each one renders; pains are named inline with issue numbers (Neatoo#103, RemoteFactory#117) while the pains reference stays 005's; T1 and T10 are stated as one distinction after the 2026-10-11 D18 clarification; no Dn IDs in prose; code review not opted in (prose deliverable; the fresh-session test is the real gate).
- **Why:** pre-flight recon and the three plans that landed since the stub.

---

## Notes

- Skills consulted at drafting: `skills/neatoo/*` read from the repo (not loaded via the Skill tool, per `CLAUDE.md`); the RemoteFactory skill's current files for the pointer vocabulary.
