# Design.App and Design.Server in CI

**Plan #:** 002
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4, AC-1 (AC-4 as narrowed 2026-10-11: Neatoo-owned composition only)
**Status:** In Progress
**Last Updated:** 2026-10-11 (plan review round 2 APPROVED; implementation started)
**Plan-review opt-in:** Yes — two new projects in CI, the first Blazor code in the example set, and a composition the skill will tell people to copy
**Code-review opt-in:** Yes — examples are what gets copied
**Branch:** skr-002-design-app-server — cut from the arc at Step 2
**PR:** —

---

## Scope

Create `src/Design/Design.App` (Blazor WebAssembly with MudBlazor and MudNeatoo by project reference) and `src/Design/Design.Server` (ASP.NET Core host for the client and the RemoteFactory endpoint) over Design.Domain, and add both to `Design.sln` so the existing CI steps build and test them. They supply the Neatoo-owned regions the skill needs:

- **Neatoo on each tier:** what a Neatoo application registers on the client and on the server — `AddNeatooServices` with the factory mode and the domain assembly, MudBlazor — and, on the server, why that call stands alone beside `UseNeatoo()` (RemoteFactory#117: adding `AddNeatooAspNetCore` after it breaks every remote Save).
- **UI contract (T1, T8):** a save handler that awaits `WaitForTasks()`, reads `IsSavable`, and reassigns the `Save()` result; and a ViewModel over the live `WorkOrder` aggregate whose page binds to the aggregate's business facts (`CanApprove`, `IsHeld`, `IsOverBudget`, `HasUnschedulableTasks`) and decides nothing itself (D16).
- **One edit page over `Order`** with its item list, as the first page plan 006 builds on.
- **`WorkOrder.ShowHoldBanner` renamed to a business fact** (`IsHeld`): the property was the `ShowTreatmentPanel` smell (D16, N5) inside the example set's own `skill-visibility-rules` region; the region is renamed to say "derived facts", and the three prose sites that teach `ShowHoldBanner` follow the re-render (plan review V1; user 2026-10-11).
- **In-memory implementations of Design.Domain's `[Service]` interfaces in Design.Infrastructure**, registered singleton by Design.Server so rows survive requests. Design.Tests keeps its own mocks (plan review C4; user 2026-10-11).
- **A client/server test pair in Design.Tests** built from the two composition seams, with the keyed `HttpClient` re-registered over an `HttpMessageHandler` that forwards each request to the server container's `HandleRemoteDelegateRequest` (one layer below `Neatoo.UnitTest/ClientServerContainer.cs`'s stub, so RemoteFactory's real `MakeRemoteDelegateRequest` runs and only a Remote-mode seam reaches it; round 2 callout 1) — so the server bullet exercises serialization (which is what C1 breaks) and the handler bullet can tell a reassigned result from a dropped one (C2, C3).

The RemoteFactory-owned half of the composition — the endpoint, `UseNeatoo`, the keyed `HttpClient`, the `IsServerRuntime` trim switch — must exist for the app to run, but gets no `skill-*` region here: RemoteFactory's own `Design.Client.Blazor` and `Design.Server` already demonstrate it, with composition tests, and the Neatoo skill points at the RemoteFactory skill for it (owner split, 2026-10-09; both skills are assumed loaded together, user 2026-10-11). Plan 006 writes the mudneatoo text and adds any further pages it needs.

---

## Intent

- A reader told "set up the client and server" sees the two Neatoo lines per tier in compiled code, and follows a pointer for the rest.
- A reader writing a Save button copies a handler that never validates first, because there is nothing to validate (T1).
- A reader building a screen with derived values or visibility copies a ViewModel that holds the aggregate and binds to its facts, with no `if` deciding business state (T8).
- The Design set has real Blazor code compiled in CI, so mudneatoo can stop hand-writing it (AC-6, plan 006).

---

## Framework & Architectural Alignment

- Owner split (todo, 2026-10-09): RemoteFactory owns the transport and tier composition; Neatoo owns `AddNeatooServices`, MudNeatoo, and the UI contract. RemoteFactory's `src/Design/Design.Server/Program.cs` and `ServerServices.cs` are its examples of `AddNeatooAspNetCore` / `UseNeatoo`; this plan does not restate them in regions.
- RemoteFactory's composition-seam pattern (`ServerServices.AddDesignServer`, pinned by `DesignServerCompositionTests`): the server's registrations live behind one method that `Program.cs` and the test both call, so the test cannot drift from the host. Design.Server follows it, and Design.App gets the same seam taking the base address, because `WebAssemblyHostBuilder.HostEnvironment` cannot be reached from a test (C3).
- RemoteFactory#117 (filed from this review): `AddNeatooRemoteFactory` registers `IServiceAssemblies` and the interface converter last-wins, so `AddNeatooServices(Server)` followed by `AddNeatooAspNetCore` drops Neatoo.dll from type resolution and every remote Save fails on the server. Design.Server calls `AddNeatooServices(Server)` and `UseNeatoo()` only — `AddNeatooServices` already registers what `UseNeatoo` needs; `AddNeatooAspNetCore` adds only `IAspAuthorize`, unused here — and the region's header states the hazard until #117 ships.
- D16: entities expose business facts; the UI binds. D20 / model statement 7 and memory note `feedback-no-validate-step`: the save shape is `WaitForTasks()` → `IsSavable` → `Save()` and reassign; no `RunRules`. T13: `Save()` returns a new instance.
- D6: the server's `[Insert]`/`[Update]` gate stays the safety net; the client handler does not duplicate it.
- D24: no LazyLoad in anything built here.
- `.claude/rules/design-snippets.md` and `.claude/rules/docs.md`: regions `skill-*`, unique, tight, each pinned by a test. This plan extends both rules' "single source" from Design.Domain and Design.Tests to the whole `src/Design` directory, names the comment-style snippet marker for `.razor` markup (where `#region` is text and would render), and adds the one exception: a Razor page-binding region is pinned by the build, since the repo has no bUnit (C6). The marker's syntax is confirmed against `dotnet mdsnippets` output before the rule is written.
- `Directory.Build.props` sets `AnalysisMode=all` with warnings as errors and does not suppress CA1515, so a public type in an exe project fails the build (C5). Design.App and Design.Server keep their types `internal` and declare `InternalsVisibleTo("Design.Tests")`, as Design.Domain does.
- Interface-first: the ViewModel and the page reference `IOrder`, `IWorkOrder`, never concretes.

---

## Constraints & Invariants

- Design.App and Design.Server target net9.0, like Design.Domain and Design.Tests, with the Blazor WebAssembly packages pinned to their latest listed 9.0.x through `VersionOverride` (RemoteFactory's Design projects do the same). No existing project's target framework changes.
- Existing Design.Tests stay green; the only change to an existing test is the `ShowHoldBanner` → `IsHeld` rename in `WorkOrderAggregateTests` (identifier only, assertions intact).
- The ViewModel and save handler are plain C# in Design.App, so Design.Tests can pin them without bUnit; the Razor pages are pinned by compiling (no bUnit — todo Out of Scope).
- No business decision lives in the ViewModel or the page: every value the page shows or hides on comes from a business fact on the aggregate. The ViewModel may name a screen element only by mapping such a fact one-to-one, never by combining or deciding (D16).
- Design.Infrastructure's implementations are in-memory and registered singleton; they exist so the server composition can serve every factory in Design.Domain except the LazyLoad demos (D24; round 2 callout 2), not to be realistic persistence.
- `Design.sln` builds with 0 warnings; `dotnet mdsnippets` reports no duplicate or missing regions.

---

## Steps

1. Rename `WorkOrder.ShowHoldBanner` to `IsHeld` (interface, entity, rule, test, region name, and the three prose sites that re-render or name it); the rule's comment says the UI binds a fact, not a show/hide decision.
2. Fill Design.Infrastructure with in-memory implementations of every `[Service]` interface Design.Domain's operations take — except `ILazyLoadChildRepository` and `ILazyLoadParentRepository` (D24: nothing new on LazyLoad) — plus `IShippingRateService`'s real implementation already in Design.Domain; update `CLAUDE.md`'s "empty project" line.
3. Create Design.Server as an ASP.NET Core host over Design.Domain: one composition method that `Program.cs` calls and the tests call — `AddNeatooServices(Server)`, `AddDesignDomainRules()`, Design.Infrastructure's services as singletons — then `UseNeatoo()` and the hosted-WASM middleware. The composition's header states the #117 hazard and why `AddNeatooAspNetCore` is absent.
4. Create Design.App as a Blazor WebAssembly client over Design.Domain with MudBlazor and MudNeatoo: one composition method taking the base address — `AddNeatooServices(Remote)`, `AddDesignDomainRules()`, the keyed `HttpClient`, MudBlazor — which `Program.cs` calls with `HostEnvironment.BaseAddress`; plus the trim configuration.
5. Add an Order edit page with its item list, bound through MudNeatoo, and a save handler with the T1 shape.
6. Add a ViewModel over the live `WorkOrder` aggregate and a page bound to it: approval and the derived facts come from the aggregate's rules; the ViewModel holds the aggregate, exposes the verb, and saves.
7. Wrap the Neatoo-owned parts in `skill-*` regions: Neatoo registration on each tier, the save handler, the ViewModel, and the page's binding (comment-style marker in `.razor`, confirmed against `dotnet mdsnippets`).
8. Add Design.App and Design.Server to `Design.sln`; reference both from Design.Tests; build the client/server test pair from the two seams with the keyed `HttpClient` forwarding into the server container; write the tests that pin the Acceptance bullets. Update `.claude/rules/design-snippets.md`, `.claude/rules/docs.md` and `CLAUDE.md`'s Project Map.
9. Build, test, run `dotnet mdsnippets`; confirm no duplicate or missing regions.

---

## Acceptance

- [ ] Through the client/server pair, an `IOrder` fetched on the client, modified and saved crosses to the server composition, is serialized both ways, and lands in Design.Infrastructure's store `[integration]` · Must
- [ ] The client composition runs in Remote mode: the same `[Remote]` call observed above leaves the client container through the keyed `HttpClient` (the forwarding handler sees it), which only a Remote-mode seam does; a Server-mode seam would take the local path and fail (it cannot resolve the repository on the client, and `Calls` stays 0) `[integration]` · Must
- [ ] The save handler, run through the pair, saves a savable order and holds the server's returned instance afterwards (not the one it sent); given an invalid order it does not call `Save()` and leaves the messages on the entity; it calls no `RunRules` `[integration]` · Must
- [ ] The WorkOrder ViewModel's approve action and the facts the page binds to (`CanApprove`, `IsHeld`, `IsOverBudget`, `HasUnschedulableTasks`) follow the aggregate's rules: changing the aggregate changes what the ViewModel exposes, with no logic in the ViewModel deciding it `[integration]` · Must
- [ ] No property on `WorkOrder` is named for a screen element; `WorkOrderAggregateTests` keeps every assertion under the renamed fact `[explicit-skip: rename, verified by grep and the unchanged test count]` · Must
- [ ] Design.App's pages compile in `Design.sln` `[explicit-skip: no bUnit; pinned by the build]` · Must
- [ ] Every new Neatoo-owned shape has a `skill-*` region and `dotnet mdsnippets` reports no duplicate or missing regions `[explicit-skip: tooling]` · Must
- [ ] `Design.sln` builds with 0 errors and 0 warnings and every Design.Tests test passes, locally and in the CI steps that already build and test `Design.sln` `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

Walked 2026-10-11.

- `Design.sln`: Design.Domain, Design.Infrastructure, Design.Tests, Neatoo, Neatoo.BaseGenerator. Design.Domain and Design.Tests are single-target net9.0 (overriding `Directory.Build.props`'s `net9.0;net10.0`). MudNeatoo multi-targets net9.0/net10.0 with MudBlazor 9.0.0.
- `Directory.Packages.props`: `Microsoft.AspNetCore.Components.WebAssembly`, `.DevServer`, `.Server` at 10.0.3 (net10.0 only); `Neatoo.RemoteFactory` and `Neatoo.RemoteFactory.AspNetCore` 1.9.0. NuGet lists 9.0.20 for all three WebAssembly packages (registration index and package page, 2026-10-11).
- CI (`.github/workflows/build.yml`): installs `9.0.x` explicitly but builds `Neatoo.sln`, which includes net10.0 Person projects, and has passed (last green run 2026-10-09) — the runner image carries .NET 10. Runs on PRs to `main` only, so plan PRs into `skr-arc` get no CI; the arc's PR to `main` does. Already builds and tests `Design.sln`, so new projects in it are picked up with no workflow change.
- `Examples/Person`: `Person.App/Program.cs` calls `AddNeatooServices(NeatooFactory.Remote, ...)` and registers the keyed `HttpClient`; `Person.Server/Program.cs` calls `AddNeatooServices(NeatooFactory.Server, ...)` and maps `/api/neatoo` by hand to `HandleRemoteDelegateRequest` — it does not use `Neatoo.RemoteFactory.AspNetCore`. Person.App sets `IsServerRuntime=false` with `Trim="true"` and roots MudNeatoo for full trim.
- `src/Neatoo/AddNeatooServices.cs`: registers Neatoo's services and calls `AddNeatooRemoteFactory(portalServer, assemblies)`. RemoteFactory's `AddNeatooAspNetCore` also calls `AddNeatooRemoteFactory(NeatooFactory.Server, ...)` and adds `IAspAuthorize` and the logging `HandleRemoteDelegateRequest`. Calling both is order-dependent and Neatoo-first breaks remote Save (plan review C1, RemoteFactory#117; verified in `AddRemoteFactoryServices.cs:62,65` at v1.9.0 and `AddNeatooServices.cs:114-122`).
- RemoteFactory `src/Design`: `Design.Client.Blazor`, `Design.Server` (`ServerServices.AddDesignServer` seam, `UseNeatoo`), `DesignServerCompositionTests` in its Design.Tests, which references Design.Server directly. Its Design projects use `VersionOverride="9.0.13"` for net9.0.
- `WorkOrder` (`Aggregates/WorkOrderAggregate/WorkOrder.cs`): `CanApprove` rule (region `skill-can-x-rule`), visibility rules (`skill-visibility-rules`), entity verb (`skill-entity-verb`); tests in `AggregateTests/WorkOrderAggregateTests.cs`.
- `skills/mudneatoo`: `SKILL.md` (core pattern, field types, page structure, child collections, metadata, display-only, view/edit mode) and `references/aggregate-reactive-vm.md` (ViewModel over a live aggregate) are the blocks plans 002 and 006 will source.

---

## Punchlist

-

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Order round-trips through the client/server pair | Must | `[integration]` | `CompositionTests/ClientServerPairTests.Order_CreatedOnTheClient_SavesThroughTheServerAndFetchesBack` | `[integration]` — client scope from `AddDesignClient`, server scope from `AddDesignServer`, request serialized through `ForwardingHandler` into `HandleRemoteDelegateRequest`; asserts the row in `InMemoryOrderRepository.Store`, a Fetch back, then modifies the fetched graph (root and child) and saves again — the `[Update]` path — asserting the store row changed |
| Client runs in Remote mode (observed [Remote] call) | Must | `[integration]` | `ClientServerPairTests.Client_IsInRemoteMode_TheCallLeavesThroughTheKeyedHttpClient` | `[integration]` — `ServerLink.Calls` goes 0 → 1 on a `[Remote]` Fetch; a Server-mode seam would run locally and leave it 0 |
| Save handler shape, through the pair | Must | `[integration]` | `ViewModelTests/OrderEditViewModelTests.Save_SavableOrder_SavesAndHoldsTheReturnedInstance` (one round trip, `!ReferenceEquals(sent, vm.Order)`, row stored), `Save_InvalidOrder_DoesNotCallSaveAndKeepsTheMessages` (0 calls, same instance, message present); `Load` exercised after the save (second round trip); `grep -rn RunRules src/Design/Design.App --include=*.cs --include=*.razor` → one hit, the DID NOT DO THIS comment | `[integration]` |
| WorkOrder ViewModel follows the aggregate | Must | `[integration]` | `ViewModelTests/WorkOrderViewModelTests.HoldBanner_FollowsTheAggregatesIsHeldFact` (entity fact + ViewModel mapping), `Approve_FollowsTheAggregatesAdmissionAndStatus` (entity + ViewModel), `OverBudget_IsTheAggregatesFact_TheViewModelExposesTheEntity` (entity only — the ViewModel has no over-budget member; the page binds the entity), `HasUnschedulableTasks_FollowsTheChildrensRule_OnTheClient` (entity only, same reason), `Save_SavableWorkOrder_CrossesToTheServerAndHoldsTheReturnedInstance` (WorkOrder through the pair: Save reassigns, store row, Load back) | `[integration]` — client scope of the pair |
| No screen-element property on WorkOrder | Must | `[explicit-skip]` | `grep -nE "Show[A-Z]|Hide[A-Z]|Visible|Enabled" WorkOrderAggregate/*.cs` → only the comment saying there is none; `WorkOrderAggregateTests` has 6 `[TestMethod]` on `skr-arc` and 6 now, assertions unchanged (rename only) | skip honoured |
| Pages compile | Must | `[explicit-skip]` | `OrderEdit.razor`, `WorkOrderPage.razor`, `Home.razor` compile in `Design.sln` (`reviews/002-build.log`, 0 errors) | skip honoured |
| Regions and mdsnippets | Must | `[explicit-skip]` | `dotnet mdsnippets` exit 0, no missing-snippet lines; 160 markers in `src/Design` (regions + Razor `begin-snippet`), 0 duplicates. New: `skill-client-composition`, `skill-server-composition`, `skill-save-handler`, `skill-viewmodel-over-aggregate`, `skill-page-order-edit`, `skill-page-binds-facts`, `skill-client-server-pair`, `skill-derived-facts` (renamed), and `-test` partners for pair, save handler, ViewModel | skip honoured |
| Build and tests green | Must | `[explicit-skip]` | `reviews/002-build.log`: 0 errors, 0 warnings (Design.sln incl. App, Server, Infrastructure); `reviews/002-test.log`: 196 passed, 0 failed. CI: no workflow change needed — the existing Design.sln steps pick the projects up; runs on the arc PR to `main` | skip honoured |

---

## Gate Record

-

---

## Plan Amendments

### 2026-10-11 — Drafted from the stub

- **Section affected:** Scope
- **Original said:** the regions cover the whole tier composition: both `Program.cs` files, the keyed `HttpClient`, the `IsServerRuntime` trim switch and the endpoint. Open: net9.0 vs net10.0 with CI on 9.0 only; no bUnit.
- **What changed:** the RemoteFactory half of the composition gets no regions here (owner split; RemoteFactory's Design set already demonstrates and tests it). net9.0 with `VersionOverride` 9.0.20 keeps every Design project on one framework; CI is not a constraint (it already builds net10.0). Pages are pinned by the build; the ViewModel and save handler by Design.Tests.
- **Why:** pre-flight recon, 2026-10-11.

### 2026-10-11 — Amended for the first plan review (`reviews/002-plan-review.md`, CONCERNS)

- **Section affected:** Scope, Alignment, Constraints, Steps, Acceptance
- **Original said:** the page binds "the visibility rules" (`ShowHoldBanner`); Step 1 left the `AddNeatooServices` / `AddNeatooAspNetCore` combination to be settled at the keyboard; server services had no named home; tests were in-process against a Server-mode container; no client seam; regions in Razor and CA1515 unaddressed.
- **What changed:** V1 — `ShowHoldBanner` renamed to `IsHeld` (user: rename to a business fact), new Step 1 and a new Must bullet. C1 — the combination is answered and the hazard filed as RemoteFactory#117 (user: file on RF); Design.Server is `AddNeatooServices(Server)` + `UseNeatoo()` only. C2/C3 — a client/server test pair with a serializing stub `IMakeRemoteDelegateRequest`, and a client seam taking the base address; the first three bullets now run through the pair. C4 — in-memory implementations in Design.Infrastructure, singleton (user's choice). C5 — `internal` + `InternalsVisibleTo`. C6 — comment-style marker for `.razor`, rule files extended, the build-pinned exception stated. Theoretical: `docs.md` added to Step 8; 003's `RunRules` change in `WorkOrder.Create/Fetch` noted as a dependency for the ViewModel test's post-Fetch assertions (D18) — the test asserts after a property change, not after Fetch alone.
- **Why:** plan review round 1; three choices were the user's and are recorded with the finding.
- **Discovery Log:** 2026-10-11 / SKR-002 (second entry)

### 2026-10-11 — Amended for plan review round 2 (APPROVED, two Must callouts)

- **Section affected:** Scope, Constraints, Steps 2 and 8, Acceptance bullet 2
- **What changed:** the test pair stubs the keyed `HttpClient`'s handler rather than `IMakeRemoteDelegateRequest`, so RemoteFactory's real remote path runs and only a Remote-mode client seam reaches the server container (a Server-mode seam takes the local path and the bullet fails). Design.Infrastructure excludes the two LazyLoad demo repositories (D24).
- **Why:** round 2 callouts 1 and 2; both mechanical.

---

## Notes

- Skills consulted at drafting: `skills/mudneatoo` and `skills/neatoo` read from the repo (not loaded via the Skill tool, per `CLAUDE.md`); RemoteFactory's `src/Design` read directly.
