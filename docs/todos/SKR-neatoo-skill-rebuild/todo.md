# Rebuild the Neatoo skill around the model and placement decisions

**ID:** SKR
**Type:** Enhancement
**Status:** In Progress
**Priority:** High
**Created:** 2026-10-09
**Last Updated:** 2026-10-11 (SKR-003 Done, PR #104)
**Initial split:** 7 plans
**Plan cap:** 11
**Arc branch:** skr-arc

---

## Goal

Someone building zCRM, human or Claude, opens `skills/neatoo` and can decide three things for any type, operation or piece of logic: which shape it takes, which attribute it gets, and which tier it runs on. Then they can build it correctly. `SKILL.md` leads with the model (`docs/VisionAlignment.md` section 2 under rulings D1–D19), then the type, operation, tier and placement decisions, then links to mechanism references. Every code block is compiled and tested. Design.App and Design.Server join Design.Domain as snippet sources, for tier composition and for Blazor. The skill is organized around the questions a mainstream .NET developer asks, because those are what get reached for under load.

## Acceptance Criteria

- [ ] **AC-1** · Must — `SKILL.md` leads with the model, then answers every row of the habit-trap table at its decision point, with the type, operation, tier and placement decisions and the UI contract consistent with D1–D19.
- [ ] **AC-2** · Must — Every C# block in `skills/neatoo` comes from a compiled `src/Design` region, except blocks labelled WRONG.
- [ ] **AC-3** · Must — Design.Domain has a tested example of each Neatoo-owned type choice: aggregate, input model, read model, an entity reused by a job, and a rule that takes an interface-factory domain service.
- [ ] **AC-4** · Must — Design.App and Design.Server build and test in CI and supply the Neatoo-owned tier-composition snippets (`AddNeatooServices` per tier, MudNeatoo, the UI contract); the RemoteFactory half is pointed at, not restated (user, 2026-10-11).
- [ ] **AC-5** · Must — No skill, doc or Design.Domain text teaches running rules at the end of `[Create]` or `[Fetch]` as a routine step.
- [ ] **AC-6** · Should — mudneatoo's Blazor blocks come from Design.App.
- [ ] **AC-7** · Should — The skill names each known framework pain with its issue or analyzer.
- [ ] **AC-8** · Should — The installed copies in `~/.claude/skills` match `skills/` at close.
- [ ] **AC-9** · Must — A fresh session given only the two installed skills and the zCRM scenarios falls into none of the Neatoo-owned habit-trap rows; the RF-owned rows it falls into are reported to the RemoteFactory sibling (user, 2026-10-12).
- [ ] **AC-10** · Should — `SKILL.md` is at most about 400 lines, with the habit-trap table in the first screen after the model.

## Habit traps the skill must answer

The rubric for AC-1 and AC-9. Each row is a question a mainstream .NET developer (or model) asks by habit, Neatoo's answer, and the compiled example a reader copies instead. Sources: `docs/VisionAlignment.md` sections 1.3 and 2.1, Appendix A, RemoteFactory#112, and the 2026-10-09 job-example miss.

| # | Owner | The question the habit asks | Neatoo's answer | Compiled example | Plan |
|---|---|---|---|---|---|
| T1 | Neatoo | Where do I validate before Save? | Nowhere; the rules ran when the property was set. `WaitForTasks()`, read `IsSavable`. `RunRules` is not a validate step | job; Blazor save handler | 001, 002 |
| T2 | Neatoo | Where do I throw when input is bad? | A rule. Exceptions are application failures, never validation (D2) | `ErrorPatterns`; a validation rule | existing |
| T3 | RF | How do I load-or-create? | An `[Execute]` that calls Fetch and Create (D11) | RF skill: class-level `[Execute]` | RF sibling |
| T4 | RF | Can I `[Create]` from loaded data? | No. Create = new, Fetch = existing (D3) | RF skill | RF sibling |
| T5 | RF | How does the client call the repository? | It doesn't. A repository is `[Service]` on a server operation; a service the client calls is an interface factory (D12, RF#112) | RF skill: domain-service interface factory | RF sibling |
| T6 | RF | Where does my application service go? | Static command; class-level `[Execute]` when the result is the aggregate; context when the screen binds to it (D12, D13) | RF skill: command, context | RF sibling |
| T7 | Neatoo | Nobody edits this, so plain EF? | An entity is right with no person involved (D4) | job | 001 |
| T8 | Neatoo | Where do the screen's derived values and visibility go? | Rules on the entity expose business facts; the UI binds (D16) | `WorkOrder.CanApprove`; a Design.App page | 002 |
| T9 | Neatoo | How do I set a value in Fetch without marking it modified? | Plain assignment inside an operation is baseline; `LoadValue` is not for operations | `FetchPatterns` | existing |
| T10 | Neatoo | How do I get computed values after Fetch — call `RunRules(All)` at the end? | Rules do not fire during an operation, so compute what the operation needs: `RunRules(nameof(X))` for the rules that write the object, or load the value from the row. Not `All` from a root whose rules write children (#98), and never as a routine validate step: persisted data is assumed valid (D18, clarified 2026-10-11) | `WorkOrder` `[Fetch]`; `OrderItem` `[Fetch]` | 003 |
| T11 | both | The parent saves its children's rows? | Each object writes itself (RF); the list coordinates with `DeletedList` and `IsNew` routing (Neatoo, D10) | `Order` / `OrderItemList` | existing |
| T12 | Neatoo | A rule can't call the server? | It can, through an injected command or an interface-factory domain service (D1, C6 ruling) | `CheckUsernameAvailabilityRule`; rule with a domain service | existing, 001 |
| T13 | RF | `Save()` updates my object? | It returns a new instance; reassign it (analyzer pending) | RF skill | RF sibling |
| T14 | RF | Which services can a constructor take? | Both tiers. `[Service]` parameters are server-only | RF skill | RF sibling |
| T15 | RF | Should the child's `[Insert]` be `[Remote]`? | No; `internal`. The call is already on the server | RF skill | RF sibling |
| T16 | Neatoo | Value object, so `ValidateBase`? | A `record`; `[Factory]` only when construction needs a service or authorization; its constructor may throw. `ValidateBase` is an input model (D17, D25) | `EmployeeSummary`; input model | 001 |
| T17 | Neatoo | Can I call `Save()` on a child? | Not through a child-only interface: `IEntityBase` has none. A type that is both root and child extends `IEntityRoot` and is saved as a root only when fetched as one (D23) | `IOrderInterfaces` | existing |
| T18 | RF | Does `[Remote]` mean "runs on the server"? | RF skill | RF skill | RF sibling |
| T19 | RF | My command needs the server, so plain `[Execute]`? | RF skill | RF skill | RF sibling |
| T20 | RF | I'll return a DTO from a controller for this screen? | RF skill | RF skill | RF sibling |

Ownership (agreed 2026-10-09): each row lives in the skill of the framework that enforces it. The RemoteFactory skill owns model statements 1–4 and 6 and the RF rows; the Neatoo skill owns statements 5, 7, 8, 9 and the Neatoo rows. Each `SKILL.md` carries a pointer block naming the other's rows; nothing is copied. The RF rows are built by the sibling todo in the RemoteFactory repository.

Scenarios for AC-9 (plan 007): a Contact aggregate with a Save button; a contact search form; a duplicate-contact check; open-or-create today's visit; a nightly job over Contacts.

## Out of Scope

- The analyzers (collection-reference trigger, `LoadValue` inside an operation, discarded `Save()` result): their own todo; the skill links to them as they land.
- The RemoteFactory repository and skill: the sibling todo in that repository, run by the desktopRemoteFactory session, owns the RF rows, the RF model statements and RemoteFactory#112.
- Restructuring the user docs beyond the D18 edits AC-5 requires.
- Fixing MudNeatoo components or adding bUnit coverage beyond what AC-6's snippets need.
- Removing Neatoo's lazy-load surface (D24: `lazy-loading.md`, `LazyLoadProperty.cs`, Person's phone list, mudneatoo's `EntityLazyLoad`, #97): its own todo once RemoteFactory ships the removal. SKR builds nothing new on LazyLoad and copies none of it into the rebuilt skill.
- Moving the RemoteFactory pin from 1.9.0 to 1.10.3 (D8; 1.10.2 listed 2026-10-11 with the #113 fix; 1.10.3 listed 2026-10-12 with the #119 fix, which does not touch Neatoo's lists): its own task, with a full build and test. That task also withdraws "`[Execute]` must return `Task<T>`" (`ApproveEmployee.cs:50,170`, rendered into `references/base-classes.md`): from 1.10.2 a static `[Execute]` returns `Task` with no result or `Task<T>` with one, never `void`.

---

## Plan Index

| # | File | Title (≤ 8 words) | Serves | Status | PR |
|---|------|-------|--------|--------|----|
| 001 | [001-type-choice-examples](./plans/001-type-choice-examples.md) | Design.Domain example per Neatoo type choice | AC-3 | Done | #101 |
| 002 | [002-design-app-server](./plans/002-design-app-server.md) | Design.App and Design.Server in CI | AC-4, AC-1 | Done | #102 |
| 003 | [003-no-end-of-operation-runrules](./plans/003-no-end-of-operation-runrules.md) | Stop teaching end-of-operation RunRules as a step | AC-5 | Done | #104 |
| 004 | [004-skill-model-and-decisions](./plans/004-skill-model-and-decisions.md) | SKILL.md: model, habit traps, decisions, UI contract | AC-1, AC-2, AC-10 | Done | #105 |
| 005 | [005-references-as-mechanism](./plans/005-references-as-mechanism.md) | References reorganized as mechanism | AC-1, AC-2, AC-7 | Done | #106 |
| 006 | [006-mudneatoo-from-design-app](./plans/006-mudneatoo-from-design-app.md) | mudneatoo blocks sourced from Design.App | AC-6 | Done | #107 |
| 007 | [007-sync-and-verify](./plans/007-sync-and-verify.md) | Sync, then fresh-session test | AC-8, AC-9 | In Progress | — |

---

## Punchlist

- [x] `CLAUDE.md` "Central Pillar" and "The Rules" wording softened to strongly recommended (D19) · `CLAUDE.md` · PR #100 · AC-1 · Must
- [x] Design.Domain duality text reversed (D23): `RemoteBoundary.cs` "do not bolt a root role", `Address.cs` "NO STANDALONE-ROOT OPERATIONS", `IFactoryInterfaces.cs` "a root only" rewritten to the endorsed shape · `src/Design/Design.Domain` · done when no comment forbids a type being both root and child and the `DualUseEntity` demo shows both roles · AC-3 · Must — pulled onto SKR-001, PR #101
- [x] `CLAUDE-DESIGN.md:77,87` and `README.md:24` stop calling `ValidateBase` a value object (D17) · `src/Design/CLAUDE-DESIGN.md`, `README.md` · done when grep for "value object" near `ValidateBase` is empty · AC-3 · Must — pulled onto SKR-004 (the record value object lands there), 2026-10-12
- [x] Close #98 · GitHub · done when closed with a comment naming D18 · AC-5 · Must — pulled onto SKR-003; closed as done (not moot) after the D18 clarification, 2026-10-11
- [ ] mudneatoo block count corrected in the audit README (49, not 14) · `docs/todos/consistency-audit/README.md` · done when the line reads 49 · AC-6 · Should
- [x] Framework pain for the skill: `Save()` does not mark the entity busy, so a double-click on Save sends two Inserts (the sent instance stays `IsSavable` until the result is reassigned) · `skills/neatoo` pain list (plan 004/005) · done when the skill names it with Neatoo#103 (critical, filed 2026-10-11) · AC-7 · Should (SKR-002 code review callout 3) — pulled onto SKR-004 (inline) and SKR-005 (pains reference), 2026-10-12
- [x] Design.App Order page: an empty `New()` form is `IsSavable` (`Order.Create` runs no rules; two `[Required]` fields unset), so New → Save hits the `[Insert]` gate and throws into the `ErrorBoundary` — the rare validate case the Blazor prose describes, unapplied in the compiled example beside it · `Order.Create` (run rules so the form shows its errors) or `OrderEditViewModel` · done when the user decides which and the example follows · AC-4 · Must (SKR-003 plan review round 2, 2026-10-11) — user decided 2026-10-12: `Order.Create` runs its rules; pulled onto SKR-004

## Dismissed

- SKR · Analyzer for a concrete type where the interface belongs · D19: interface-first is recommended, not required

---

## Discovery Log

### 2026-10-12 — SKR-007 RF skill not rebuilt · serves AC-9, AC-8
- **Finding:** The RemoteFactory skill rebuild (desktopRemoteFactory's Todo 2) has not started and waits for EXSET (12 plans, 1 merged); no date. Both RF skill copies are stale (installed: pre-1.9.0; repo: post-1.9.0, most audit findings still open). AC-9 as written cannot be met inside SKR.
- **Decision:** Narrow (user). 007 runs now: neatoo and mudneatoo synced, the RF repo copy installed for the test; AC-9 gates on the Neatoo-owned rows; RF-owned rows the fresh session falls into go to desktopRemoteFactory as findings for Todo 2.
- **Index changes:** AC-9 narrowed. Cap unchanged, 7 of 11 issued.
- **Follow-up:** SKR-007; RF Todo 2's fresh-session test.

### 2026-10-09 — SKR · serves AC-1, AC-9
- **Finding:** The orchestrator and the plan-reviewer both added a validate-before-save step to the job example by habit, with the fact present in the skill; the todo guaranteed structure and compiled code but not answers keyed to the habit's questions, nor a fresh-session test.
- **Decision:** Re-split (Goal revised with the user)
- **Index changes:** AC-1 reworded; AC-9, AC-10 added; habit-trap table added as rubric; 002, 004, 007 scopes amended. Cap unchanged, 7 of 11 issued.
- **Follow-up:** SKR-004, SKR-007

### 2026-10-11 — SKR-002 · serves AC-4
- **Finding:** RemoteFactory's own `src/Design` (`Design.Client.Blazor`, `Design.Server`, `DesignServerCompositionTests`) already demonstrates and tests the endpoint, `UseNeatoo`, the keyed `HttpClient` and the `IsServerRuntime` trim switch. Restating them in Neatoo regions would duplicate and drift.
- **Decision:** Amend (user: Neatoo parts only; assume both skills are loaded together)
- **Index changes:** AC-4 narrowed to the Neatoo-owned composition; 002 regions only `AddNeatooServices` per tier, MudNeatoo, the save handler and the WorkOrder ViewModel/page. Cap unchanged, 7 of 11 issued.
- **Follow-up:** SKR-002

### 2026-10-12 — SKR-004 plan review · serves AC-1, AC-10, AC-4
- **Finding:** (1) The plan stated D18 as a ban ("never a validate step"), the mirror of the 2026-10-11 over-correction. (2) The ~400-line budget cannot hold a rendered block per trap row (~830 lines of regions). (3) T16 has no compiled `record`; the T1 example hands back an empty form that is `IsSavable` (the open Order-form row). (4) §2's "does a person edit it" test was superseded by D4 with no replacement stated.
- **Decision:** Amend. Three-leg D18 wording; blocks live in `references/`, `SKILL.md` renders four and points (user); `Order.Create` runs its rules (user — the `[Create]` decides); one `record` value object added (user); D26 ruled (user: Neatoo when rules or tracking something relies on — a person or a job; plain RF when nothing is tracked or validated).
- **Index changes:** AC-10 unchanged at ~400. Cap unchanged, 7 of 11 issued.
- **Follow-up:** SKR-004; 005 receives the removed sections by the list 004 records; 007 uses 004's row→line map.

### 2026-10-11 — SKR-003 D18 clarified · serves AC-5, AC-1
- **Finding:** Plan 003's first draft read D18 as "no `RunRules` inside a `[Create]`/`[Fetch]`" and proposed removing `WorkOrder.Fetch`'s targeted re-run in favour of persisting derived values. The user: the ruling was against prescribing `RunRules(All)` at the end of *every* operation and as a validate step; `RunRules` inside an operation to compute the derived values it needs is "a great case to use RunRules". The 2026-10-09 miss (validate step added by habit) and this over-correction are the same misunderstanding from opposite sides — the skill has to state the distinction, not a ban.
- **Decision:** Amend. D18's row in `VisionAlignment.md` clarified; T10 reworded (compute what the operation needs: targeted `RunRules` or load from the row; never `All` from a root with child-writing rules, #98; never as a validate step); plan 003 re-drafted; #98 fulfilled rather than closed as moot; memory note `feedback-no-validate-step` records both errors.
- **Index changes:** none. Cap unchanged, 7 of 11 issued.
- **Follow-up:** SKR-003; SKR-004 states the distinction at T1/T10.

### 2026-10-11 — SKR-002 plan review · serves AC-4, AC-1
- **Finding:** (1) `WorkOrder.ShowHoldBanner` is the D16/N5 screen-element smell inside the example set's own region, and the T8 page was about to bind it. (2) `AddNeatooServices(Server)` followed by RemoteFactory's `AddNeatooAspNetCore` drops Neatoo.dll from `IServiceAssemblies` and undoes Neatoo's converter override, so every remote Save fails on the server; the natural order with both skills loaded. (3) Design.Server's `[Service]` implementations had no home.
- **Decision:** Amend. Rename to `IsHeld`; Design.Server uses `AddNeatooServices(Server)` + `UseNeatoo()` and states why; RemoteFactory#117 filed; in-memory implementations go in Design.Infrastructure (user, all three, 2026-10-11). Tests run through a client/server pair so serialization is exercised.
- **Index changes:** none. Cap unchanged, 7 of 11 issued.
- **Follow-up:** SKR-002; the mudneatoo and docs prose that teach `ShowHoldBanner` re-render under 002.

### 2026-10-09 — SKR-001 · serves AC-3
- **Finding:** Three of plan 001's four shapes (context, class-level `[Execute]`, interface factory) are RemoteFactory doctrine; building them in Design.Domain would duplicate the RF skill and drift.
- **Decision:** Re-split (skills split by owner, agreed with the user)
- **Index changes:** AC-3 narrowed to Neatoo-owned shapes; trap table gains an Owner column; RF rows move to a sibling todo in the RemoteFactory repository; 001 re-drafted; 004 restates nothing RF-owned; 007 loads both skills. Cap unchanged, 7 of 11 issued.
- **Follow-up:** SKR-001, sibling

---

## Skipped Steps

- 

---

## Sibling Todos

- RemoteFactory skill rebuild, in the RemoteFactory repository (ID assigned there), run by the desktopRemoteFactory session. Owns T3–T6, T13–T15, T11's RF half, model statements 1–4 and 6, rulings D3/D8/D11–D15, RemoteFactory#112. SKR-007 waits on its skill being synced. Its branches land on its own arc, not this one.

---

## Close-Out Audit

---

## Follow-on

---

## Docs & Retro

---

## Results / Conclusions
