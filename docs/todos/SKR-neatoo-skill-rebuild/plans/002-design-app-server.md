# Design.App and Design.Server in CI

**Plan #:** 002
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4, AC-1 (AC-4 as narrowed 2026-10-11: Neatoo-owned composition only)
**Status:** Draft
**Last Updated:** 2026-10-11 (drafted from the stub after recon)
**Plan-review opt-in:** Yes — two new projects in CI, the first Blazor code in the example set, and a composition the skill will tell people to copy
**Code-review opt-in:** Yes — examples are what gets copied
**Branch:** skr-002-design-app-server — cut from the arc at Step 2
**PR:** —

---

## Scope

Create `src/Design/Design.App` (Blazor WebAssembly with MudBlazor and MudNeatoo by project reference) and `src/Design/Design.Server` (ASP.NET Core host for the client and the RemoteFactory endpoint) over Design.Domain, and add both to `Design.sln` so the existing CI steps build and test them. They supply the Neatoo-owned regions the skill needs:

- **Neatoo on each tier:** what a Neatoo application registers on the client and on the server — `AddNeatooServices` with the factory mode and the domain assembly, MudBlazor, and how that call combines with the RemoteFactory host on the server.
- **UI contract (T1, T8):** a save handler that awaits `WaitForTasks()`, reads `IsSavable`, and reassigns the `Save()` result; and a ViewModel over the live `WorkOrder` aggregate whose page binds to the aggregate's business facts (`CanApprove`, the visibility rules) and decides nothing itself (D16).
- **One edit page over `Order`** with its item list, as the first page plan 006 builds on.

The RemoteFactory-owned half of the composition — the endpoint, `UseNeatoo`, the keyed `HttpClient`, the `IsServerRuntime` trim switch — must exist for the app to run, but gets no `skill-*` region here: RemoteFactory's own `Design.Client.Blazor` and `Design.Server` already demonstrate it, with composition tests, and the Neatoo skill points at the RemoteFactory skill for it (owner split, 2026-10-09). Plan 006 writes the mudneatoo text and adds any further pages it needs.

---

## Intent

- A reader told "set up the client and server" sees the two Neatoo lines per tier in compiled code, and follows a pointer for the rest.
- A reader writing a Save button copies a handler that never validates first, because there is nothing to validate (T1).
- A reader building a screen with derived values or visibility copies a ViewModel that holds the aggregate and binds to its facts, with no `if` deciding business state (T8).
- The Design set has real Blazor code compiled in CI, so mudneatoo can stop hand-writing it (AC-6, plan 006).

---

## Framework & Architectural Alignment

- Owner split (todo, 2026-10-09): RemoteFactory owns the transport and tier composition; Neatoo owns `AddNeatooServices`, MudNeatoo, and the UI contract. RemoteFactory's `src/Design/Design.Server/Program.cs` and `ServerServices.cs` are its examples of `AddNeatooAspNetCore` / `UseNeatoo`; this plan does not restate them in regions.
- RemoteFactory's composition-seam pattern (`ServerServices.AddDesignServer`, pinned by `DesignServerCompositionTests`): the server's registrations live behind one method that `Program.cs` and the test both call, so the test cannot drift from the host. Design.Server follows it.
- D16: entities expose business facts; the UI binds. D20 / model statement 7 and memory note `feedback-no-validate-step`: the save shape is `WaitForTasks()` → `IsSavable` → `Save()` and reassign; no `RunRules`. T13: `Save()` returns a new instance.
- D6: the server's `[Insert]`/`[Update]` gate stays the safety net; the client handler does not duplicate it.
- D24: no LazyLoad in anything built here.
- `.claude/rules/design-snippets.md`: regions `skill-*`, unique, tight, each pinned by a test. This plan extends the rule's "single source" from Design.Domain and Design.Tests to the whole `src/Design` directory and says so in the rule file and `CLAUDE.md`.
- Interface-first: the ViewModel and the page reference `IOrder`, `IWorkOrder`, never concretes.

---

## Constraints & Invariants

- Design.App and Design.Server target net9.0, like Design.Domain and Design.Tests, with the Blazor WebAssembly packages pinned to their latest listed 9.0.x through `VersionOverride` (RemoteFactory's Design projects do the same). No existing project's target framework changes.
- Existing Design.Tests stay green and unchanged.
- The ViewModel and save handler are plain C# in Design.App, so Design.Tests can pin them without bUnit; the Razor pages are pinned by compiling (no bUnit — todo Out of Scope).
- No business decision lives in the ViewModel or the page: every value the page shows or hides on comes from a property of the aggregate.
- `Design.sln` builds with 0 warnings; `dotnet mdsnippets` reports no duplicate or missing regions.

---

## Steps

1. Create Design.Server as an ASP.NET Core host over Design.Domain: one composition method that `Program.cs` calls and the tests call, registering Neatoo for the server tier and the server-only services Design.Domain's operations take, plus the RemoteFactory endpoint and the hosted-WASM middleware. Settle how `AddNeatooServices` and RemoteFactory's ASP.NET Core registration combine (both reach `AddNeatooRemoteFactory`) and write the finding into the composition's header.
2. Create Design.App as a Blazor WebAssembly client over Design.Domain with MudBlazor and MudNeatoo: Neatoo for the client tier, the RemoteFactory client registrations, and the trim configuration.
3. Add an Order edit page with its item list, bound through MudNeatoo, and a save handler with the T1 shape.
4. Add a ViewModel over the live `WorkOrder` aggregate and a page bound to it: approval and visibility come from the aggregate's rules; the ViewModel holds the aggregate, exposes the verb, and saves.
5. Wrap the Neatoo-owned parts in `skill-*` regions: Neatoo registration on each tier, the save handler, the ViewModel, and the page's binding.
6. Add Design.App and Design.Server to `Design.sln` and reference them from Design.Tests; write the tests that pin the Acceptance bullets. Update `.claude/rules/design-snippets.md` and `CLAUDE.md`'s Project Map for the two new projects.
7. Build, test, run `dotnet mdsnippets`; confirm no duplicate or missing regions.

---

## Acceptance

- [ ] The server composition resolves and runs a Design.Domain aggregate operation end to end: an `IOrder` fetched, modified and saved through the services the composition registers `[integration]` · Must
- [ ] The client composition registers Neatoo in Remote mode and resolves the domain's factories `[integration]` · Must
- [ ] The save handler saves a savable order and reassigns the result; given an invalid order it does not call `Save()` and leaves the messages on the entity; it calls no `RunRules` `[integration]` · Must
- [ ] The WorkOrder ViewModel's approve action and the values the page binds to follow the aggregate's rules: changing the aggregate changes what the ViewModel exposes, with no logic in the ViewModel deciding it `[integration]` · Must
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
- `src/Neatoo/AddNeatooServices.cs`: registers Neatoo's services and calls `AddNeatooRemoteFactory(portalServer, assemblies)`. RemoteFactory's `AddNeatooAspNetCore` also calls `AddNeatooRemoteFactory(NeatooFactory.Server, ...)` and adds `IAspAuthorize` and the logging `HandleRemoteDelegateRequest`. Whether calling both is safe is the open question in Step 1.
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
| Server composition runs an aggregate operation | Must | `[integration]` | | |
| Client composition registers Remote mode | Must | `[integration]` | | |
| Save handler shape | Must | `[integration]` | | |
| WorkOrder ViewModel follows the aggregate | Must | `[integration]` | | |
| Pages compile | Must | `[explicit-skip]` | | |
| Regions and mdsnippets | Must | `[explicit-skip]` | | |
| Build and tests green | Must | `[explicit-skip]` | | |

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

---

## Notes

- Skills consulted at drafting: `skills/mudneatoo` and `skills/neatoo` read from the repo (not loaded via the Skill tool, per `CLAUDE.md`); RemoteFactory's `src/Design` read directly.
