# Rebuild the Neatoo skill around the model and placement decisions

**ID:** SKR
**Type:** Enhancement
**Status:** In Progress
**Priority:** High
**Created:** 2026-10-09
**Last Updated:** 2026-10-09 (Goal and criteria revised the same day; see Discovery Log)
**Initial split:** 7 plans
**Plan cap:** 11
**Arc branch:** skr-arc

---

## Goal

Someone building zCRM, human or Claude, opens `skills/neatoo` and can decide three things for any type, operation or piece of logic: which shape it takes, which attribute it gets, and which tier it runs on. Then they can build it correctly. `SKILL.md` leads with the model (`docs/VisionAlignment.md` section 2 under rulings D1–D19), then the type, operation, tier and placement decisions, then links to mechanism references. Every code block is compiled and tested. Design.App and Design.Server join Design.Domain as snippet sources, for tier composition and for Blazor. The skill is organized around the questions a mainstream .NET developer asks, because those are what get reached for under load.

## Acceptance Criteria

- [ ] **AC-1** · Must — `SKILL.md` leads with the model, then answers every row of the habit-trap table at its decision point, with the type, operation, tier and placement decisions and the UI contract consistent with D1–D19.
- [ ] **AC-2** · Must — Every C# block in `skills/neatoo` comes from a compiled `src/Design` region, except blocks labelled WRONG.
- [ ] **AC-3** · Must — Design.Domain has a tested example of each type choice: aggregate, input model, read model, context, static command, class-level `[Execute]`, domain-service interface factory, and an entity reused by a job.
- [ ] **AC-4** · Must — Design.App and Design.Server build and test in CI and supply the tier-composition snippets.
- [ ] **AC-5** · Must — No skill, doc or Design.Domain text teaches running rules at the end of `[Create]` or `[Fetch]` as a routine step.
- [ ] **AC-6** · Should — mudneatoo's Blazor blocks come from Design.App.
- [ ] **AC-7** · Should — The skill names each known framework pain with its issue or analyzer.
- [ ] **AC-8** · Should — The installed copies in `~/.claude/skills` match `skills/` at close.
- [ ] **AC-9** · Must — A fresh session given only the installed skill and the zCRM scenarios falls into none of the habit-trap rows.
- [ ] **AC-10** · Should — `SKILL.md` is at most about 400 lines, with the habit-trap table in the first screen after the model.

## Habit traps the skill must answer

The rubric for AC-1 and AC-9. Each row is a question a mainstream .NET developer (or model) asks by habit, Neatoo's answer, and the compiled example a reader copies instead. Sources: `docs/VisionAlignment.md` sections 1.3 and 2.1, Appendix A, RemoteFactory#112, and the 2026-10-09 job-example miss.

| # | The question the habit asks | Neatoo's answer | Compiled example | Plan |
|---|---|---|---|---|
| T1 | Where do I validate before Save? | Nowhere; the rules ran when the property was set. `WaitForTasks()`, read `IsSavable`. `RunRules` is not a validate step | job; Blazor save handler | 001, 002 |
| T2 | Where do I throw when input is bad? | A rule. Exceptions are application failures, never validation (D2) | `ErrorPatterns`; a validation rule | existing |
| T3 | How do I load-or-create? | An `[Execute]` that calls Fetch and Create (D11) | class-level `[Execute]` | 001 |
| T4 | Can I `[Create]` from loaded data? | No. Create = new, Fetch = existing (D3) | `FetchPatterns` | existing |
| T5 | How does the client call the repository? | It doesn't. A repository is `[Service]` on a server operation; a service the client calls is an interface factory (D12, RF#112) | domain-service interface factory | 001 |
| T6 | Where does my application service go? | Static command; class-level `[Execute]` when the result is the aggregate; context when the screen binds to it (D12, D13) | `Commands/`; context | 001 |
| T7 | Nobody edits this, so plain EF? | An entity is right with no person involved (D4) | job | 001 |
| T8 | Where do the screen's derived values and visibility go? | Rules on the entity expose business facts; the UI binds (D16) | `WorkOrder.CanApprove`; a Design.App page | 002 |
| T9 | How do I set a value in Fetch without marking it modified? | Plain assignment inside an operation is baseline; `LoadValue` is not for operations | `FetchPatterns` | existing |
| T10 | How do I re-run rules after Fetch so computed values populate? | You don't. Assign the derived value inside the operation (D18) | 003's replacement regions | 003 |
| T11 | The parent saves its children's rows? | Each object writes itself; the list coordinates (D10) | `Order` / `OrderItemList` | existing |
| T12 | A rule can't call the server? | It can, through an injected command (D1) | `CheckUsernameAvailabilityRule` | existing |
| T13 | `Save()` updates my object? | It returns a new instance; reassign it (analyzer pending) | `SavePatterns` | existing |
| T14 | Which services can a constructor take? | Both tiers. `[Service]` parameters are server-only | `ServiceInjectionDemo` | existing |
| T15 | Should the child's `[Insert]` be `[Remote]`? | No; `internal`. The call is already on the server | `OrderItem` | existing |
| T16 | Value object, so `ValidateBase`? | A `record`. `ValidateBase` is an input model (D17) | `EmployeeSummary`; input model | 001 |
| T17 | Can I call `Save()` on a child? | No. `IEntityBase` has none; only `IEntityRoot` does | `IOrderInterfaces` | existing |

Scenarios for AC-9 (plan 007): a Contact aggregate with a Save button; a contact search form; a duplicate-contact check; open-or-create today's visit; a nightly job over Contacts.

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
| 002 | [002-design-app-server](./plans/002-design-app-server.md) | Design.App and Design.Server in CI | AC-4, AC-1 | Draft | — |
| 003 | [003-no-end-of-operation-runrules](./plans/003-no-end-of-operation-runrules.md) | Stop teaching end-of-operation RunRules | AC-5 | Draft | — |
| 004 | [004-skill-model-and-decisions](./plans/004-skill-model-and-decisions.md) | SKILL.md: model, habit traps, decisions, UI contract | AC-1, AC-2, AC-10 | Draft | — |
| 005 | [005-references-as-mechanism](./plans/005-references-as-mechanism.md) | References reorganized as mechanism | AC-1, AC-2, AC-7 | Draft | — |
| 006 | [006-mudneatoo-from-design-app](./plans/006-mudneatoo-from-design-app.md) | mudneatoo blocks sourced from Design.App | AC-6 | Draft | — |
| 007 | [007-sync-and-verify](./plans/007-sync-and-verify.md) | Sync, then fresh-session test | AC-8, AC-9 | Draft | — |

---

## Punchlist

- [ ] `CLAUDE.md` "Central Pillar" and "The Rules" wording softened to strongly recommended (D19) · `CLAUDE.md` · done when neither word "pillar" nor "must be internal" remains · AC-1 · Must
- [ ] Close #98 as moot under D18 · GitHub · done when closed with a comment naming D18 · AC-5 · Must
- [ ] mudneatoo block count corrected in the audit README (49, not 14) · `docs/todos/consistency-audit/README.md` · done when the line reads 49 · AC-6 · Should

## Dismissed

- SKR · Analyzer for a concrete type where the interface belongs · D19: interface-first is recommended, not required

---

## Discovery Log

### 2026-10-09 — SKR · serves AC-1, AC-9
- **Finding:** The orchestrator and the plan-reviewer both added a validate-before-save step to the job example by habit, with the fact present in the skill; the todo guaranteed structure and compiled code but not answers keyed to the habit's questions, nor a fresh-session test.
- **Decision:** Re-split (Goal revised with the user)
- **Index changes:** AC-1 reworded; AC-9, AC-10 added; habit-trap table added as rubric; 002, 004, 007 scopes amended. Cap unchanged, 7 of 11 issued.
- **Follow-up:** SKR-004, SKR-007

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
