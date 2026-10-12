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

Rewrite `skills/neatoo/SKILL.md` from 863 lines to about 400, organized around the questions a mainstream .NET developer asks by habit, because those are what get reached for under load. The skill leads with the model (VisionAlignment section 2, statements 5, 7, 8, 9 in Neatoo's voice, 1–4 and 6 as the pointer to the RemoteFactory skill), then the habit-trap table from `todo.md` as the spine — every Neatoo-owned row (T1, T2, T7–T10, T12, T16, T17, and T11's Neatoo half) answered at its decision point, phrased against the question the habit asks, with the compiled example a reader copies instead; the RemoteFactory-owned rows (T3–T6, T13–T15, T18–T20, T11's RF half) appear as one pointer block that names the RF skill's row and nothing more. Then the Neatoo-owned decisions: when Neatoo and when plain RemoteFactory (statement 7, D4); baseline versus the user's work (statement 5, D18 as clarified — three legs: no validate step before Save; `RunRules` inside an operation for the derived values it needs; and the rare, caller-decided validation of an object an operation handed back unvalidated, stated where it applies); logic placed by who drives it (statement 8, the ladder); the UI contract (statement 9, D16: the ViewModel binds and adapts gestures, decides nothing; the save handler's shape; the one allowed mapping); interface-first strongly recommended with the `IsSavable` trap as the reason (D19); the two rule shapes that reach the server (D1, D21, no preference); entity duality (D23); value object is a `record`, `ValidateBase` is an input model (D17, D25). Code blocks: `SKILL.md` renders only the blocks for the three largest incident clusters — the save handler, the page binding facts, `Fetch` computing what it needs, and the admission rule (`skill-save-handler`, `skill-page-binds-facts`, `skill-fetch-computes-derived`, `skill-can-x-rule`); every other trap row and decision points at the `references/` file that already renders its region (never at a source file). The one hand-written exception is a short block labelled WRONG. (User, 2026-10-12: blocks in references, `SKILL.md` points; the ~400 budget stands.) Mechanism content (property system, `Parent`, three-phase save, lists, lazy load, testing, troubleshooting) leaves `SKILL.md` for `references/` in plan 005; this plan leaves one-line pointers so nothing is lost between the two PRs. The frontmatter description is rebuilt from the habit questions so the skill triggers on them, not only on type names, and drops the LazyLoad triggers (D24). The skill names the framework pains it must warn about with their issue (Neatoo#103 double-click Save; `[Remote]`/`AddNeatooAspNetCore` → RemoteFactory#117 as a pointer; the pending analyzers by name) — the pains list itself is plan 005's reference, this plan names them inline where the trap is.

Two small Design changes are in this plan because the plan review found the traps they serve have no honest example (user, 2026-10-12): `Order.Create` runs its rules so the form it hands back shows its required fields and `IsSavable` is false until they are filled — the rare validate case, decided by that operation (closes the todo's Order-form punchlist row); and one `record` value object with a constructor that throws on bad input, used by an existing aggregate and tested (region `skill-value-object-record`), so T16 has a compiled record. The "when Neatoo" test is ruled as D26 (rules or tracking something relies on — a person or a job; plain RemoteFactory when nothing is tracked or validated) and written into VisionAlignment §2. Out of this plan: `references/*` (005), mudneatoo (006), the installed copy and the fresh-session test (007), any other Design change.

---

## Intent

- A reader who asks any question in the trap table finds the answer within the first screen after the model, phrased against their question, with one compiled block to copy — not a prohibition.
- A reader who has to decide type, operation, tier or placement for a piece of zCRM logic finds the Neatoo-owned decision stated once, and a pointer when the decision is RemoteFactory's.
- A reader who copies a block gets code that compiles and is tested; the WRONG blocks are the only hand-written C# and are labelled.
- A fresh session given this skill and the RemoteFactory skill (plan 007) falls into none of the trap rows on the zCRM scenarios.

---

## Framework & Architectural Alignment

- VisionAlignment §2 statements 5, 7, 8, 9 are the Neatoo skill's model; 1–4 and 6 are RemoteFactory's (owner split, 2026-10-09; both skills assumed loaded together, 2026-10-11). The model is stated in the skill's words, not copied.
- Rulings applied in the text: D1/D21 (two rule shapes, no preference), D2 (rules over exceptions), D4 (an entity is right with no person), D6 (server gate stays, callers never mirror it), D16 (facts, not screen names), D17/D25 (record vs input model), D18 as clarified 2026-10-11 (compute what the operation needs; targeted not `All` from a root with child-writing rules; no *routine* validate step — the rare caller-decided one is the user's call and stays stated where it applies), D19 (interface-first recommended), D20 (job shape), D23 (duality), D24 (no LazyLoad teaching; pending removal noted once).
- The three things the rebuilt skill must do that the current one does not (todo, 2026-10-09): answer the habit's questions, compiled examples over prose, tested on a fresh session. This plan does the first two; 007 the third.
- `.claude/rules/design-snippets.md`: every C# block is a region; WRONG blocks labelled; `dotnet mdsnippets` after edits.
- Memory `feedback-no-validate-step` and the 2026-10-11 over-correction: T1 and T10 are stated as one distinction with three legs (no validate step before Save; `RunRules` inside an operation for what it needs is normal; the rare caller-decided validation of an unvalidated object is the user's call), not as a rule in either direction. The plan's first draft dropped "routine" twice and read as a ban — the plan review caught it.
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

1. Design, first: `Order.Create` runs its rules (no children exist yet, so `All`) with a comment saying this operation decides its form must not come back unvalidated; the tests that assumed a new `Order` is valid are updated for the new expectation (in scope: they test the changed behaviour), and `OrderEditViewModelTests` asserts the empty form is not savable and shows its messages. Add a `record` value object (an `EmailAddress` or similar) whose constructor throws on bad input, used by an existing aggregate, pinned by a test; region `skill-value-object-record`. Build, test, `dotnet mdsnippets`.
2. Write the frontmatter: name; a description under the 1,024-character cap (check what Claude Code does with a longer one before relying on words placed late) that leads with the habit questions in compact form — not all twenty verbatim; the Neatoo-owned ones plus "type, operation, tier, placement" — then the type and property names, minus LazyLoad; version bump.
3. Write the model: one screen — Neatoo is the editing session (statement 7) with the D26 test for when Neatoo and when plain RemoteFactory; what an operation sets is baseline, the rest is the user's work (5); logic placed by who drives it (8); the UI binds and adapts gestures, decides nothing (9); the pointer to the RemoteFactory skill for the seam, the attributes, `[Remote]` and self-writing (1–4, 6).
4. Write the habit-trap table: Neatoo-owned rows as question / answer / where the compiled example renders (`references/<file>.md#<anchor>`, never a source path), with T1 and T10 as one three-leg distinction; T2 answered by the verb-does-not-throw pair (`skill-entity-verb` + `skill-can-x-rule`) and `skill-validation-attributes-and-rules`, not `skill-rule-that-throws`; T11-Neatoo by `skill-list-update`; T12 by both `skill-rule-command` and `skill-rule-with-command` (the delegate's command and the rule that takes it) plus `skill-rule-with-domain-service`; T16 by `skill-value-object-record` with `skill-value-object` (the input model) as the contrast. The RF-owned rows as one pointer block, questions phrased as RemoteFactory's VisionAlignment §4 phrases them so they resolve once the RF skill syncs.
5. Write the decisions, one short section each, prose with the region named and its reference linked; render inline only the four blocks the Scope names. Decisions: Neatoo or plain RemoteFactory (D26; `skill-job-over-entities`); baseline vs user's work (`skill-fetch-computes-derived` inline, `skill-create-is-new-not-modified`); the ladder (`skill-can-x-rule` inline, `skill-entity-verb`, `skill-job-over-entities` as the seam — it shows cross-row server work; `skill-command` shows a side effect — and `skill-read-model`); the UI contract (`skill-save-handler` inline, `skill-viewmodel-over-aggregate`, `skill-page-binds-facts` inline); interface-first and the `IsSavable` trap (`skill-aggregate-interfaces`, `skill-child-interface-no-save`); rules that reach the server (the two shapes); duality (`skill-entity-both-roles`); record vs input model. The server gate (`skill-server-gate-refuses`) stays as the one thing callers never mirror.
6. Write the pains inline where the trap is: double-click Save (Neatoo#103) at the save handler; the `AddNeatooAspNetCore` order (RemoteFactory#117) as a pointer in the composition line; the pending analyzers by name; the `Save()` result (T13 pointer). Write the reference index: one line per `references/` file by the decision it serves, with `lazy-loading.md` listed as "pending removal, do not copy" (D24; its removal is its own todo).
7. Remove the mechanism sections from `SKILL.md` and leave a one-line pointer where each lived. Record in this plan's Notes the removed sections with their line ranges at the base commit (`3877443`) and the target reference for each, including the sections no reference carries today (Three-Phase Pattern and its two tables, Aggregate Is a Graph, Rules for Open Mutation with the placement-by-scope table, Parent's rules of use and its three regions, Quick Start's four regions, Key Properties, Base Class Quick Reference, Save Routing), so 005 moves them rather than rediscovers them. Fix the two `references/domain-logic-placement.md` pointers that name deleted `SKILL.md` sections. Trace the `skill-value-object*` rename to 005 explicitly. Run `dotnet mdsnippets`; confirm every placeholder filled, no duplicates; line count.
8. Self-test against the trap table: for every row, the answer is findable from the question alone; record `SKILL.md` line numbers per row in the Test Evidence map (007's input).

---

## Acceptance

- [ ] A new `Order` from `Order.Create` is not savable and carries its required-field messages before any edit; filling the required fields makes it savable `[integration]` · Must
- [ ] The `record` value object rejects bad input in its constructor and is used by an aggregate; a valid one round-trips through that aggregate `[integration]` · Must
- [ ] Every Neatoo-owned trap row (T1, T2, T7, T8, T9, T10, T11-Neatoo, T12, T16, T17) is answered in `SKILL.md` at a decision point, phrased against the question, with its compiled example rendered inline or linked to the `references/` file that renders it `[explicit-skip: table walk, line numbers recorded]` · Must
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
- [ ] Design.App Order form: `Order.Create` runs its rules so the empty form is not savable · `Order.cs`, tests · done when the New → Save path cannot reach the gate · AC-4 · Must (pulled from the todo, 2026-10-12; user: the `[Create]` decides)

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Evidence | Confirmed |
|---|---|---|---|---|
| New Order not savable until required fields filled | Must | `[integration]` | | |
| Record value object rejects bad input, round-trips | Must | `[integration]` | | |
| Every Neatoo-owned trap row answered, example inline or linked | Must | `[explicit-skip]` | | |
| RF rows once, pointer only | Must | `[explicit-skip]` | | |
| Every C# block a filled snippet (or WRONG) | Must | `[explicit-skip]` | | |
| Model first, table in first screen, ≤ ~400 lines | Should | `[explicit-skip]` | | |
| No prohibition-teaching, no routine validate step, no LazyLoad, no Dn | Must | `[explicit-skip]` | | |
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

### 2026-10-12 — Amended for the plan review (`reviews/004-plan-review.md`, CONCERNS)

- **Section affected:** Scope, Alignment, Steps, Acceptance, Punchlist
- **Original said:** "never a validate step" (twice); every trap row and decision renders its block inline within ~400 lines; Design changes out of scope; statement 7 "in Neatoo's voice" with no replacement for its superseded test; T2/T11/T12/T16 regions unnamed or wrong; Step 6 left recovery to git history; frontmatter grew by twenty questions.
- **What changed:** V1 — D18 stated as three legs (no validate step before Save; `RunRules` inside an operation for what it needs; the rare caller-decided validation stays where it applies). C1 — `Order.Create` runs its rules (user: the `[Create]` decides); the todo's Order-form row pulled onto this plan. C2 — blocks live in references, `SKILL.md` renders four and points (user); budget stands. C3 — T16 gets a `record` value object region (user); T11 uses `skill-list-update`; T2 uses the verb/admission pair; T12 renders both the command and the rule. C4 — Step 7 records every removed section, its base-commit lines and target reference, and fixes the two `domain-logic-placement.md` pointers. C5 — description under 1,024 characters, questions compact. C6 — D26 ruled (user: rules or tracking something relies on) and written into VisionAlignment §0 and §2. C7 — T12 both regions. Should 1 — rename traced to 005 in Step 7. Should 2 — `lazy-loading.md` indexed as pending removal. Theoretical: the seam rung uses `skill-job-over-entities` as its primary.
- **Why:** plan review round 1; four decisions were the user's and are recorded with the finding.
- **Discovery Log:** 2026-10-12 / SKR-004

---

## Notes

- Skills consulted at drafting: `skills/neatoo/*` read from the repo (not loaded via the Skill tool, per `CLAUDE.md`); the RemoteFactory skill's current files for the pointer vocabulary.
