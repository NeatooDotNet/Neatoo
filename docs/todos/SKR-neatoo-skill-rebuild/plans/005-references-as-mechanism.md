# References reorganized as mechanism

**Plan #:** 005
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-2, AC-7
**Status:** Done
**Last Updated:** 2026-10-12 (drafted from the stub after SKR-004 merged)
**Plan-review opt-in:** No — no ruling is new and no API changes; the Design changes are Razor regions over pages that exist and one input-model page. The decisions this plan makes are listed under Notes for the user
**Code-review opt-in:** No — prose plus rendered regions; the test-reviewer's map check and 007's fresh-session test are the gates that fit
**Branch:** skr-005-references-as-mechanism — cut from the arc at Step 2
**PR:** —

---

## Scope

Make each `skills/neatoo/references/` file a mechanism reference under the decisions in the new `SKILL.md`:

- **Receive** the sections 004 removed, by the move list in 004's Notes — rewritten against the model, not pasted: the old text carries "Value Object = `ValidateBase`", the placement-ladder "rung" vocabulary and a "Validate" phase, all of which the new `SKILL.md` contradicts.
- **Split `domain-logic-placement.md`**: the gesture/load/seam branches of its decision tree are `SKILL.md`'s "Logic is placed by who drives it" and RemoteFactory's; the reference keeps the rule-shape branches (which rule for which trigger) and the Neatoo patterns — derived values, facts, chained rules, async rules via a command, cross-property validation, child triggers, the parent orchestrator, the verb — with no "rung" vocabulary.
- **Replace every hand-written non-WRONG block** in `skills/neatoo/references/` (seven Razor blocks in `blazor.md` and `domain-logic-placement.md`), and label WRONG the three anti-pattern `@code` blocks that sit under an Anti-Pattern heading without the label with a compiled region, or a pointer to a rendered page block where one already shows the binding. `ShowTreatmentPanel` / `ShowReviewAlert` / `ShowExtensionWarning` go with them (screen-named properties on the entity).
- **Remove what the new `SKILL.md` contradicts**: "value object" on `ValidateBase` (`base-classes.md:9,26`, `blazor.md:82`), LazyLoad teaching outside `lazy-loading.md` (two `pitfalls.md` rows and one parenthetical).
- **Add `references/pains.md`**: each known framework pain with its issue or pending analyzer, and the workaround the skill teaches until it closes. `SKILL.md`'s Known pains section and index point at it.
- **Rename** `skill-value-object` / `skill-value-object-interface` to `skill-input-model` / `skill-input-model-interface` and update every render site (`base-classes.md`, three docs).

Out: mudneatoo (006); RF-owned rows that `pitfalls.md` still states (correct today, pointer-only cleanup belongs with 007's sync); `lazy-loading.md`'s content (D24, its own todo).

---

## Intent

- A reader who follows any `SKILL.md` link lands on mechanism that agrees with the model — no reference re-teaches the ladder, calls `ValidateBase` a value object, or shows a screen-named property.
- Every block a reader copies from `skills/neatoo` compiles and is tested, or is labelled WRONG.
- The sections `SKILL.md` gave up exist somewhere, once, in the reference its pointer names.
- A known pain is findable by symptom with its issue number and today's workaround.

---

## Framework & Architectural Alignment

- `SKILL.md` (004) is the decision layer; references carry mechanism and never restate a decision differently.
- Rulings applied: D16 (facts, not screen names; the ViewModel may map one-to-one), D17/D25 (record = value object; `ValidateBase` = input model), D18 as clarified (no routine validate step; operations compute what they need), D19, D23, D24 (no LazyLoad teaching).
- `.claude/rules/design-snippets.md`: Razor regions use `@* begin-snippet: name *@` … `@* end-snippet *@`; a page-binding region is pinned by the build, the ViewModel it binds by a test. `.claude/rules/skills-neatoo.md`: no "see link" to repository source.
- Ownership split: RF-owned material is pointed at, never newly added.

---

## Constraints & Invariants

- No region is renamed except the two `skill-value-object*` regions; no region another file renders is moved or narrowed without updating that file.
- Sacred tests unchanged; no test in `Design.Tests` loses an assertion.
- `dotnet mdsnippets` clean (no missing, no duplicate) across `skills/`, `docs/`, `README.md`.
- Old-section content is rewritten to the model; anything RF owns in it (factory attributes, `[Remote]` placement) becomes a one-line pointer.
- No ruling IDs in skill prose.

---

## Steps

1. **Design.** Add tight Razor regions for the bindings the seven blocks showed, in Design.App pages that already exist (WorkOrderPage: a derived value, a fact, the verb button; OrderEdit: the property metadata binding through MudNeatoo). Add one small Design.App component that binds an input model (`ValidateBase`) through the manual metadata pattern — the shape `blazor.md` teaches because MudNeatoo cannot bind `IValidateProperty` (#95). Rename the two `skill-value-object*` regions. Build, test, `dotnet mdsnippets`.
2. **`domain-logic-placement.md`.** Replace the decision tree with the rule-shape table (trigger → rule shape) and a pointer to `SKILL.md`'s placement decision; drop "rung" vocabulary; swap each RIGHT Razor block for its region or a pointer; receive "The Aggregate Is a Graph", "Designing Rules for Open Mutation" with the placement-by-scope table, and `Parent` (rule / factory method / verb, cast, rules of use) with `skill-parent-in-child-rule`, `skill-parent-in-child-update`, `skill-parent-in-update-test`.
3. **`entities.md`.** Receive the state-property table (no `IsDirty`), save routing, and the edit-then-save phases with the persisting-operation vs business-method table and "what the save needs must be state" — as two phases (set state, rules react live; save), no "Validate" phase.
4. **`base-classes.md`.** Fix the DDD mapping (record = value object; `ValidateBase` = input model); receive the base-class quick reference corrected the same way; add the smallest complete entity (`skill-quick-start-interface`, `skill-quick-start`) as the copyable start. **`properties.md`** receives `skill-partial-properties` if it does not already render the same shape.
5. **`blazor.md`.** Swap its four Razor blocks (`:31`/`:41` thin-binding pair, `:58` Mode 1, `:69` Mode 2) for regions; the WRONG half of the pair stays, the RIGHT half becomes a pointer to the page block; fix `:82`.
6. **`pitfalls.md`.** Drop the two LazyLoad rows and the LazyLoad parenthetical; receive the troubleshooting quick checks.
7. **`pains.md`** (new): Neatoo#103, #96, #89, #95, #93, RemoteFactory#117, the three pending analyzers, the `Save()` result; each with symptom, issue, workaround. Point `SKILL.md`'s Known pains and index at it; close the todo's Neatoo#103 row.
8. `dotnet mdsnippets`; grep sweep (below); fill Test Evidence; gate.

---

## Acceptance

- [x] Every fenced block in `skills/neatoo/references/` outside `lazy-loading.md` (pending removal) is a filled snippet, labelled WRONG, or non-C# (XML/csproj, a diagram) `[explicit-skip: awk sweep for fenced blocks outside placeholders]` · Must
- [x] Each section on 004's move list exists in its target reference, once, rewritten to the model `[explicit-skip: move-list walk]` · Must
- [x] No reference calls `ValidateBase` a value object, uses "rung"/"ladder" vocabulary, names a screen element on an entity, or teaches building on LazyLoad outside `lazy-loading.md` `[explicit-skip: grep]` · Must
- [x] The input-model binding a page uses where MudNeatoo cannot bind compiles in Design.App, and the property object it binds carries the validity and messages the page shows `[unit]` · Must
- [x] `pains.md` lists each pain with its issue or analyzer and a workaround; `SKILL.md` links it `[explicit-skip: read + grep]` · Should
- [x] `skill-value-object*` renamed with every render site updated; `dotnet mdsnippets` clean `[explicit-skip: tooling]` · Must

---

## Current State (Pre-Flight)

Walked 2026-10-12 on `skr-arc` at `987251f`.

- References: 14 files, 6,137 lines. Hand-written non-WRONG blocks: seven Razor (`blazor.md:41,58,69`; `domain-logic-placement.md:75,133,595,630`); three anti-pattern `@code` blocks without a WRONG label (`domain-logic-placement.md:307,507,647`); one hand-drawn tree (`domain-logic-placement.md:13`); XML in `blazor.md:260`, `trimming.md:69,81`; diagrams in `entities.md:455`, `rules-lifecycle.md:110`. WRONG C# blocks: `domain-logic-placement.md:290`, `entities.md:676,690`, `pitfalls.md:38`.
- "rung" vocabulary: `domain-logic-placement.md:11–36, 756`. Screen-named entity properties: `blazor.md:43–48`. "Value object" on `ValidateBase`: `base-classes.md:9,26`, `blazor.md:82`. LazyLoad outside its file: `pitfalls.md` two rows and the `[Remote]` row's parenthetical.
- Sections 003 rewrote (`blazor.md` Form Submission, `validation.md` Validation Before Save, `rules-lifecycle.md`) agree with the new `SKILL.md`; no change needed.
- Open issues with a skill-facing pain: #103, #96, #95, #93, #89; RemoteFactory#117. #97 is LazyLoad (D24) and is not a pain to teach around.
- `skill-value-object` renders in `skills/neatoo/references/base-classes.md`, `docs/getting-started.md`, `docs/guides/validation.md`, `docs/reference/api.md`.

---

## Punchlist

- [x] Neatoo#103 in the pains reference · `references/pains.md` · done when listed with symptom and workaround · AC-7 · Should (pulled from the todo; the inline half landed in 004)

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Evidence | Confirmed |
|---|---|---|---|---|
| Fenced blocks: snippet, WRONG, or non-C# | Must | `[explicit-skip]` | awk sweep (fenced blocks outside `<!-- snippet -->` pairs) over `references/*.md` and `SKILL.md`: 17 blocks, each WRONG-labelled (`blazor.md:31`; `domain-logic-placement.md:32,72,257,274,475,572,604`; `entities.md:722,736`; `pitfalls.md:36`) or non-C# (`blazor.md:259` csproj XML; `trimming.md:69,81` XML; `entities.md:501`, `rules-lifecycle.md:110` diagrams); the one exception is `lazy-loading.md:251` (pending removal, out of scope). The seven RIGHT Razor blocks became pointers to the rendered page (`blazor.md` `#a-viewmodel-over-a-live-aggregate`) or the new `skill-input-model-manual-binding` region (`blazor.md:61`); the three unlabelled anti-pattern `@code` blocks now open with a WRONG comment. | skip confirmed |
| Move list landed, once, rewritten | Must | `[explicit-skip]` | 004 Notes table walked row by row: Quick Start → `base-classes.md` (the complete entity `skill-entity-crud` already renders there; the four quick-start regions stay in `docs/getting-started.md`, see Amendments); Interface-First → `collections.md` + `SKILL.md`; placement ladder → `SKILL.md` decision + `domain-logic-placement.md:9` Which Rule Shape (smell tables kept, rung numbers removed); Three-Phase + boundary table + save-needs-state → `entities.md:249,269` (two phases, no Validate phase); Aggregate Is a Graph → `domain-logic-placement.md:838`; Open Mutation + scope table → `:844`; Parent (three regions, cast, rules of use) → `:855`; Base Class Quick Reference → `base-classes.md:5` The Shapes (record = value object, `ValidateBase` = input model); Key Properties → `entities.md:234`; factory methods → RF pointer (`base-classes.md:18`); save routing, cascading → already in `entities.md`; partial properties → already in `properties.md:5`; server gate, validation, rules-don't-fire, child triggers, IsNew vs IsModified → already rendered (Notes table); Testing → `testing.md`; Troubleshooting → `pitfalls.md:93-94`. Each appears once; `SKILL.md:332` pointer updated. | skip confirmed |
| No value-object/rung/screen-name/LazyLoad contradictions | Must | `[explicit-skip]` | grep over `references/*.md` + `SKILL.md`: "value object" only on the `record` (`base-classes.md:14,32,616,618`) and `SKILL.md`'s record rows; "rung"/"ladder" empty; `Show[A-Z]` only the ViewModel's one-to-one `ShowHoldBanner` (`blazor.md:407,449`) and the comment saying the entity never names one (`domain-logic-placement.md:95`); LazyLoad outside its file: `pains.md:26` and `SKILL.md:310,330` (being removed), `properties.md:342`, `rules-lifecycle.md:19` and `source-generation.md:182` (what generated code does today — description, not instruction; removed with the D24 surface). Dn grep empty. | skip confirmed |
| Input-model manual binding compiles and behaves | Must | `[unit]` | `Design.App/Pages/InputModelPage.razor` (region `skill-input-model-manual-binding`, pinned by the build: `reviews/005-build.log` 0/0). `Design.Tests/BaseClassTests/ValidateBaseTests.cs:72` `PropertyObject_CarriesWhatTheInputModelPageBinds`: the same `model[nameof(Name)]` object the page binds — empty name → `IsValid` false, "Name is required" in `PropertyMessages`, not busy; "Ada" → valid, no messages. Real factory from the DI scope, nothing mocked. Tier amended from `[integration]`: no tier crossing is involved. | unit |
| Pains reference | Should | `[explicit-skip]` | `references/pains.md`: six open defects (Neatoo#103, #96, #89, #95, #93; RemoteFactory#117), each with symptom, issue link and workaround; three missing analyzers with mistake, effect and fix; LazyLoad noted as removed rather than fixed. `SKILL.md` Known pains links it (`:303`) and the index lists it (`:329`). | skip confirmed |
| Rename, mdsnippets clean | Must | `[explicit-skip]` | `#region skill-input-model` (`AllBaseClasses.cs:93`), `#region skill-input-model-interface` (`IBaseClassInterfaces.cs:12`); placeholders renamed in `base-classes.md`, `docs/getting-started.md`, `docs/guides/validation.md`, `docs/reference/api.md`; grep for `snippet: skill-value-object -->` / `-interface -->` empty. `reviews/005-mdsnippets.log`: exit 0, no missing/duplicate line beyond the config echo. | skip confirmed |

---

## Gate Record

- **Test review, round 1 (2026-10-12): CLEAN** — `reviews/005-test-review.md`. No veto, no Must- or Should-cover. Sacred tests: one added method, nothing else. Build 0/0, 202 passing, mdsnippets clean. Tech-debt (bullet 3 cell omitted `rules-lifecycle.md:19`, a generator description): cell corrected. Theoretical (the input-model page's busy bar and re-render handler are unreachable while `DemoInputModel` has no async rule): not triaged.
- Code review: not opted in.

---

## Plan Amendments

### 2026-10-12 — Drafted from the stub

- **Section affected:** Scope, Steps, Acceptance
- **Original said:** receive 004's sections; split `domain-logic-placement.md`; rename `ShowTreatmentPanel`; remove contradictions; pains reference (#96, #97, analyzers, `Save()` result).
- **What changed:** the receive list is 004's Notes table; the seven RIGHT Razor blocks join (AC-2 covers Razor that carries C# expressions — the rule is "never hand-write C#"); `ShowTreatmentPanel` is removed with its block, not renamed, since the page region already shows facts; #97 dropped from pains (D24: removed, not worked around); #95, #93, #89 added; the `skill-value-object*` rename traced from 004.
- **Why:** pre-flight recon.

### 2026-10-12 — Implementation amendments

- **Section affected:** Steps 1, 4, 6; Acceptance bullets 1, 3, 4
- **Original said:** Step 1 adds Razor regions to WorkOrderPage/OrderEdit for the seven RIGHT blocks; Step 4 adds the quick-start regions to `base-classes.md` and `skill-partial-properties` to `properties.md`; bullet 4 `[integration]`.
- **What changed:** the RIGHT blocks became pointers to the page block `blazor.md` already renders (`skill-page-binds-facts` shows every binding they showed), so no new page regions; the one new region is the input-model page's manual binding. Quick-start regions not added: `skill-entity-crud` in `base-classes.md` is already the complete entity, and the four quick-start regions stay rendered in `docs/getting-started.md`. `skill-partial-properties` not added: `properties.md:5` renders the same shape. The DDD-concept definition lines in `base-classes.md` went with the head rewrite (global doc guideline). Bullet 1 exempts `lazy-loading.md` (D24, pending removal); bullet 3 reads "teaches building on LazyLoad" — generator descriptions in `properties.md` and `source-generation.md` stay until the surface is removed. Bullet 4 is `[unit]`: the test exercises the property object in one DI scope; nothing crosses a tier.
- **Why:** each Step's "if not already rendered" check came back rendered; the tier tag described a crossing that does not happen.

---

## Notes

Decisions made at drafting, for the user to overrule:

1. Razor RIGHT blocks count under AC-2. Replaced by regions, or a pointer to the rendered page block where it already shows the same binding.
2. Pains get their own file (`pains.md`) rather than rows in `pitfalls.md`: a pain is a framework defect with an issue that will close; a pitfall is a mistake that stays one.
3. RF-owned rows in `pitfalls.md` stay as they are (correct today); turning them into pointers waits for the RF skill sync (007).
