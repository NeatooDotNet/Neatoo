# Design.Domain example per Neatoo type choice

**Plan #:** 001
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3
**Status:** Done
**Last Updated:** 2026-10-10 (implemented; both gates CLEAN round 1)
**Plan-review opt-in:** Yes — these examples become doctrine that `SKILL.md` points at and that gets copied; `plan-reviewer`, Pass A against D1–D19
**Code-review opt-in:** Yes — examples are what gets copied, so the shape matters more than in ordinary code
**Branch:** skr-001-type-choice-examples — cut from the arc at Step 2
**PR:** #101 → `skr-arc`

---

## Scope

Add to Design.Domain the Neatoo-owned type-choice examples the rebuilt `SKILL.md` will point at and that it does not yet have: a Neatoo entity reused by a server job with no person involved (D4, traps T1 and T7), and a rule that takes an interface-factory domain service (D1 as extended on 2026-10-09, trap T12). Give `Order` the verb the job needs; its existing validation rule is the one the verb can break. Rename `DemoValueObject` and its list to input-model names and make sure no Design.Domain or Design.Tests text calls `ValidateBase` a value object (D17, T16). Each new shape gets a Design.Tests test and `skill-*` regions. The RemoteFactory-owned shapes (context, class-level `[Execute]`, interface factory as a pattern) are the sibling todo's and are not built here; this plan defines only the small domain-service interface the rule needs to compile. It does not touch the skill files or the user docs by hand; snippets that re-render from the rename and from the new `IOrder` verb are accepted, and the two prose lines that name the old type are fixed. It also reverses the Design.Domain text that forbids a type being both root and child (D23; pulled from the todo punchlist).

---

## Intent

- A reader told "that is a job over entities" or "that rule needs a server answer" opens one compiled, tested file and copies its shape.
- The job example is T1's counter-example: after the verb, nothing validates; `WaitForTasks()` then `IsSavable` decides, and `RunRules` appears nowhere. It is also T7's: a background job reuses the entity and its rules instead of writing plain EF.
- The example set stops calling `ValidateBase` a value object.

---

## Framework & Architectural Alignment

- Rulings D1 (a rule may call the server through an injected command; extended 2026-10-09 to an interface-factory domain service, which on the client resolves to the generated proxy and is therefore a wrapped server call, not a server-only service), D2 (no catch-and-report, no drop-out), D4 (an entity is right with no person involved; list the broken rules rather than throw), D6 (the root's `[Insert]`/`[Update]` gate stays as the safety net), D17 (`ValidateBase` is an input model), D18 (loaded data is assumed valid; no `RunRules` after Fetch or before Save), D19 (interface-first strongly recommended; Design.Domain follows it).
- Model statement 4: a job that takes a server-only `[Service]` is `[Remote, Execute]`, because a static `[Execute]` with no `[Remote]` is a local-only delegate registered unguarded on the client (plan review C1). On the server `[Remote]` does nothing extra.
- Model statement 7 and the memory note `feedback-no-validate-step`: the job's shape is apply verb, `WaitForTasks()`, `if (IsSavable) Save() else report messages`.
- RemoteFactory skill: an interface factory's implementation carries no `[Factory]`; in `NeatooFactory.Server` mode the interface resolves to the implementation, so a test observes the rule's behaviour, not the proxy (plan review C4). The implementation registers in `DesignTestServices` only, never in `AddDesignDomainRules`, because a client-side `AddScoped<IService, Impl>` after `AddNeatooServices` would replace the generated proxy (C6).
- `.claude/rules/design-snippets.md`: regions `skill-*`, unique, tight, each pinned by a test. Region *names* containing `value-object` are kept this plan so existing placeholders keep rendering; plans 004 and 005 rename them when they rewrite the skill, and the docs follow (C5).

---

## Constraints & Invariants

- Existing Design.Tests stay green and unchanged except for the rename.
- The job calls no `RunRules` and catches nothing; an entity the verb leaves unchanged is skipped because it is not savable, never because `Save()` threw `NotModified`.
- The job separates three outcomes: `IsSavable` is saved through the root's own `Save()` and the result is reassigned (T13); `!IsValid` is reported with its rule messages; `!IsModified` is skipped silently (user, 2026-10-10). It returns the report.
- The domain-service interface factory is named and documented as a service, never as a repository; it exposes nothing persistence-shaped.
- `AsyncRules.cs`'s header is rewritten so the two rule examples read as one doctrine: a rule may take a command delegate or an interface-factory domain service (D1, D21); neither is a server-only service. No preference between the two is stated (user, 2026-10-10).
- `Design.sln` builds with 0 warnings; `dotnet mdsnippets` reports no duplicate or missing regions.

---

## Steps

1. Give `Order` a Submit verb that sets `Status`, the trigger of its existing validation rule, so an order with no items becomes invalid at once and the job never reaches `Save()` for it (review P-B1). The verb goes on `IOrder`; the `skill-aggregate-interfaces` re-render is accepted. State in its comment that a verb sets state and never persists.
2. Add the job under `Jobs/`: a `[Remote, Execute] private static` command that takes a set of order keys, fetches each through the factory, applies the verb, awaits `WaitForTasks()`, saves those that are savable and reports the rest with their messages. Its header states D4, T1 and T7, and says why `[Remote]` is on it.
3. Add a domain-service interface factory under `Services/`: a small `[Factory]` interface with one or two related calls, a server implementation without `[Factory]`, and a rule on a new small demo type (not `AsyncRulesDemo`, so no existing test's `IsValid` changes and no existing region re-renders; review P-B3) that takes the interface in its constructor and writes a message from its answer. Rewrite the `AsyncRules.cs` header so the command-delegate rule and this one read as one doctrine, with no preference stated.
4. Rename `DemoValueObject` / `IDemoValueObject` / `DemoValueObjectList` / `IDemoValueObjectList` to input-model names; re-point every reference including `DesignTestServices`; rewrite the surrounding comments and `ServiceContracts.cs`'s `MyValueObject` comment so `ValidateBase` is an input model; fix the two doc prose lines that name the old type. Keep the existing region names.
5. Register the service implementation in `DesignTestServices` (the job uses the already-registered `IOrderRepository`; review P-B4). Reverse the duality text at `RemoteBoundary.cs`, `Address.cs` and `IFactoryInterfaces.cs` to the endorsed shape (D23) and make the `DualUseEntity` demo show both roles.
6. Write one Design.Tests class per new shape pinning the Acceptance bullets; wrap each shape in a `skill-*` region sized for the skill.
7. Build, test, run `dotnet mdsnippets`; confirm no duplicate or missing regions; confirm the re-rendered snippets are only those the rename, the `IOrder` verb and the duality demo touch.

---

## Acceptance

- [x] Given one order the verb leaves valid, one it leaves invalid and one it leaves unchanged, the job saves the first, reports the second with its rule messages, skips the third, and throws nothing `[integration]` · Must
- [x] The job's source contains no `RunRules` call `[explicit-skip: verified by grep, pinned by review]` · Must
- [x] A rule taking the domain service produces the message that reflects the service's answer `[integration]` · Must
- [x] No type named `*ValueObject*` derives from `ValidateBase`, and no Design.Domain or Design.Tests comment calls `ValidateBase` a value object `[explicit-skip: rename and prose, verified by grep excluding Generated/]` · Must
- [x] Every new shape has a `skill-*` region and `dotnet mdsnippets` reports no duplicate or missing regions `[explicit-skip: tooling]` · Must
- [x] `Design.sln` builds with 0 errors and 0 warnings and every Design.Tests test passes `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

Walked 2026-10-10 before the first edit.

- `Order.cs:58-62`: the only validation rule is `Items?.Count == 0 && Status != "Draft"` → "Order must have at least one item", triggered by `t => t.Status`, synchronous. `Create` sets `Status = "Draft"` (`:111`). `[Remote][Fetch]` at `:140`, `[Remote][Insert]` `:187`, `[Remote][Update]` `:242` (D6 gate: `RunRules(All)` then `SaveOperationException` at `:250-253`), `[Remote][Delete]` `:279`. No verb exists. `OrderRow.Status` is a string.
- `IOrderInterfaces.cs:68-109`: `IOrder : IEntityRoot` inside region `skill-aggregate-interfaces`, which renders into `SKILL.md`, `references/collections.md`, `docs/guides/collections.md`, `docs/guides/entities.md`, `docs/reference/api.md`. Adding `Submit()` re-renders all five.
- `TestInfrastructure.cs:149-168`: `MockOrderRepository` is scoped, has `Store`, `SeedOrder()` (two default items) and `SeedOrder(params OrderItemRow[])`; an empty call seeds an order with no items. No mock change needed.
- `ApproveEmployee.cs:101-121`: command shape to copy — `[Remote][Execute] private static Task<T> _Name(..., [Service] ...)`, throws `InvalidOperationException` on not-found, returns a `sealed record`.
- `AsyncRules.cs:95-115`: header's DESIGN DECISION reads "The rule depends on a command delegate, never on a server-only service", with a DID NOT DO THIS on injecting `IUsernameRepository`. `CheckUsernameAvailabilityRule : AsyncRuleBase<AsyncRulesDemo>` takes `UsernameAvailability.IsAvailable` (`:143-151`), inside region `skill-rule-with-command`. `AddDesignDomainRules` registers `ICheckUsernameAvailabilityRule` and `IUniqueCodeRule` transient.
- No `[Factory]` interface exists in Design.Domain. `DI/DomainRegistration.cs` is the one registration extension.
- `DemoValueObject` (`AllBaseClasses.cs:103`, `ValidateBase`, rules on `Name`, `Create()`, `Create(string)`, internal `Fetch(string)`), `DemoValueObjectList` (`:342`, parameter `valueObjectFactory` `:354,360`), `IDemoValueObject` (`IBaseClassInterfaces.cs:16`), `IDemoValueObjectList` (`:57`). References: `TestInfrastructure.cs:48`, `ValidateBaseTests.cs:18,24`, `ValidateListBaseTests.cs:18-26` (generated factory names), `ServiceContracts.cs:68` (`MyValueObject` comment), comments at `AllBaseClasses.cs:18,53,63,78,82`. Prose naming the type: `docs/guides/collections.md:7`, `docs/guides/validation.md:160`. Regions `skill-value-object`, `skill-value-object-interface`, `skill-validate-list`, `skill-validate-list-interface`, `skill-test-services` render into `docs/getting-started.md`, `docs/guides/collections.md`, `docs/guides/validation.md`, `docs/reference/api.md`, `skills/neatoo/references/base-classes.md`, `collections.md`, `testing.md`.
- Duality text: `RemoteBoundary.cs:226-259` (DESIGN DECISION "do not bolt a root role onto a child-only type"; `DualUseEntity` has the root role only, `:264-320`), `Address.cs:165-199` ("NO STANDALONE-ROOT OPERATIONS ... a hard rule", REJECTED PATTERN, two reasons), `IFactoryInterfaces.cs:160-163` ("A root only").

---

## Punchlist

- [x] Design.Domain duality text reversed (D23) · `RemoteBoundary.cs`, `Address.cs`, `IFactoryInterfaces.cs` · done when no comment forbids both roles and `DualUseEntity` shows both · AC-3 · Must (pulled from the todo, 2026-10-10) — `11acfb8`

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Job saves valid, reports invalid, skips unchanged | Must | `[integration]` | `JobTests/SubmitDraftOrdersTests.Run_ValidInvalidAndUnchangedOrders_SavesReportsAndSkipsWithoutThrowing` (plus `Run_UnknownOrder_Throws` for the D2 edge) | `[integration]` — real `IOrderFactory`, `MockOrderRepository`, Server-mode DI via `DesignTestServices` |
| No `RunRules` in the job | Must | `[explicit-skip]` | `grep -rn RunRules src/Design/Design.Domain/Jobs` → one hit, the DESIGN DECISION comment at `SubmitDraftOrders.cs:16` saying why there is none | skip honoured |
| Rule message reflects the service | Must | `[integration]` | `ServiceTests/ShippingQuoteRuleTests.Rule_DestinationServed_QuotesFromTheService`, `Rule_DestinationNotServed_ReportsTheServiceAnswer` | `[integration]` — `IShippingRateService` resolves to `ShippingRateService` in Server mode |
| No `ValidateBase` value object | Must | `[explicit-skip]` | `grep -rniE "value object\|ValueObject" src/Design --include=*.cs` excluding `Generated/` → no hits | skip honoured |
| Regions and mdsnippets | Must | `[explicit-skip]` | `dotnet mdsnippets` exit 0, no missing-snippet lines; 150 regions in `src/Design` excluding `Generated/`, 0 duplicates. New regions: `skill-verb-submit`, `skill-job-over-entities`, `skill-domain-service-interface-factory`, `skill-rule-with-domain-service`, `skill-entity-both-roles`; `-test` partners on the last three of job, rule and both-roles (`skill-verb-submit` is pinned by the job test, `skill-domain-service-interface-factory` by the rule tests) | skip honoured |
| Build and tests green | Must | `[explicit-skip]` | `reviews/001-build.log`: 0 errors, 0 warnings; `reviews/001-test.log`: 187 passed, 0 failed | skip honoured |
| Duality punchlist row (D23) | Must | — | `FactoryTests/DualUseEntityTests.RootRole_FetchByIdAndSave_WritesThroughTheRepository`, `ChildRole_FetchFromRowAndSave_WritesIntoTheRow` | `[integration]` — pins both roles of one class |

---

## Gate Record

- **2026-10-10, round 1 — test-reviewer: CLEAN** (`reviews/001-test-review.md`). Every Must bullet pinned at its declared tier; explicit-skip grep claims verified true; sacred tests rename-only; logs green. One untiered wording nit in the Test Evidence map, fixed inline. Two theoretical items listed, not triaged.
- **2026-10-10, round 1 — code-reviewer: CLEAN** (`reviews/001-code-review.md`). No veto-tier. Three Must-affecting callouts, all comment text: the job's `[Remote]` reason, the duality block's "harmless" `Save()` claim (T17), and ruling/trap IDs plus owner quotes in the example set. All three fixed the same day; build 0/0, 187 tests, `mdsnippets` clean after the fixes; logs refreshed.
- **Closing bar:** all six Acceptance bullets Must and pinned; no gaps accepted. Gate closed after round 1.

---

## Plan Amendments

### 2026-10-09 — Re-drafted after the owner split and the first review

- **Section affected:** all
- **Original said:** four new shapes (context, class-level `[Execute]`, interface factory, job) plus the rename; the job ran `RunRules(All)` before saving (Notes question); the context had an instance `[Execute]` verb.
- **What changed:** context, class-level `[Execute]` and the interface-factory pattern moved to the RemoteFactory sibling (owner split). The job runs no rules: the verb already triggered them (user, 2026-10-09). The context's verb, where it is built, is a plain instance method calling the held aggregate's verb (user's choice (b) on V1). A rule may take an interface-factory domain service (user's ruling on C6). Review callouts C1–C6 folded into Alignment and Constraints.
- **Why:** the first review (`reviews/001-plan-review.md`) and the split agreed the same day.
- **Discovery Log:** 2026-10-09 / SKR-001

### 2026-10-10 — `InternalsVisibleTo("Design.Tests")` on Design.Domain

- **Section affected:** Step 5 / Step 6 (duality demo and its test)
- **Original said:** make the `DualUseEntity` demo show both roles and pin each with a test.
- **What changed:** the generator emits the child-role operations (`internal`, no `[Remote]`) as `internal` members of `IDualUseEntityFactory`, so Design.Tests could not call `Fetch(row)` / `Save(entity, row)`. Design.Domain now declares `<InternalsVisibleTo Include="Design.Tests" />`, matching `Person.DomainModel`. The alternatives — a parent/list for `DualUseEntity`, or making `Address` the both-roles example — were put to the user, who chose this one.
- **Why:** the child role is reached only by a list's `[Update]` on the server; `internal` is the correct visibility, and the test needs to see it.
- **Discovery Log:** not logged — a mechanical choice recorded here only.

### 2026-10-10 — Child-role `Save` is synchronous

- **Section affected:** Step 6
- **What changed:** the test's `await _factory.Save(entity, row)` became a plain call; a local (non-`[Remote]`) operation on a synchronous method generates a synchronous factory member. The root-role `Save()` stays `await`ed.
- **Why:** generator behaviour, not a design choice.

---

## Notes

- Skills consulted at drafting: the RemoteFactory skill's `class-factory.md`, `interface-factory.md`, `static-factory.md`, `service-injection.md`; `skills/neatoo` read from the repo (not loaded via the Skill tool, per `CLAUDE.md`).
- The first review's theoretical item about a plain `[Factory]` class holding an `IOrder` across the wire no longer applies; no context is built here.
