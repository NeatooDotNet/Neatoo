# Code Review — SKR-002 — Design.App and Design.Server in CI

**Reviewer:** code-reviewer (opt-in, per-plan)
**Logs:** `002-build.log` (Design.sln incl. App, Server, Infrastructure: 0 errors, 0 warnings), `002-test.log`

---

## Round 1 — 2026-10-11 (HEAD `c5fa284`, 194 tests)

**Verdict: CONCERNS** (one veto-tier finding, mechanical fix)

### Direction & shape

The intent landed where it should. Each tier has one composition seam that both `Program.cs` and the tests call. The server region states the #117 hazard and omits `AddNeatooAspNetCore`. `UseNeatoo()`, the trim switch and the endpoint are present but outside any region. The test pair stubs the keyed `HttpClient`'s handler, as plan review round 2 asked, so the Remote-mode bullet is pinned honestly.

- Save handlers: both T1-shaped (`WaitForTasks` → `IsSavable` → reassigned `Save()`); no `RunRules`, no try/catch.
- D16 holds: `WorkOrderViewModel.ShowHoldBanner` is a one-to-one map of `IsHeld`; pages bind only facts and null guards.
- Interface-first holds. No LazyLoad. No ruling IDs crept back. Every remaining `ShowHoldBanner` is the ViewModel mapping or a comment naming the smell.
- Design.Infrastructure: five implementations spot-checked against interfaces and mocks — ids assigned, unknown rows fall back to placeholders like the mocks, no semantic drift; only the registration class is public.
- Plan-review dispositions all honoured.

### Veto-tier

- **`ClientServices.cs:41-43`: the keyed `HttpClient` registration sat inside `skill-client-composition`.** AC-4 as narrowed says the RemoteFactory half is pointed at, not restated; the plan's Scope lists the keyed `HttpClient` as RF-owned. The region wrapped the whole method, so the "set up the client" snippet taught RF's transport registration from the Neatoo skill. Reviewer's answer to the orchestrator's question: no, it does not count as "the one line RemoteFactory needs". **Triage: fixed** — moved into a private `AddRemoteFactoryTransport(baseAddress)` below the region; the in-region line is a pointer to the RemoteFactory skill.

### Callouts (all AC-4 · Must)

1. **After Save, the pages stayed subscribed to the stale instance** (`OrderEdit.razor`, `WorkOrderPage.razor`). The resubscribe branch ran only if the *old* instance raised `NeatooPropertyChanged`, which it never does after Save. MudNeatoo fields subscribe in `OnInitialized`, so they also stayed on the old wrappers. After the first Save the button stayed disabled, totals and banners stopped updating. Reachable by Save, then edit any field. **Triage: fixed** — page-level `SaveAsync` awaits `Vm.Save()` then `Resubscribe()`; bound section wrapped in `<div @key="Vm.Order">` / `<div @key="Vm.WorkOrder">` so the field components rebuild on the new instance; dead branch removed.
2. **`WorkOrderViewModel.AddTask` computes `Sequence = Tasks.Count + 1` in the ViewModel.** Picks a value the task's uniqueness rule checks; collides only in an app that can remove tasks. **Triage: dismissed as input defaulting** — comment added saying so; a verb on the aggregate is a Follow-on if ever wanted.
3. **Double-clicking Save inserts twice.** During the remote call the sent instance is still `IsSavable` (no busy flag) and `Vm.Order` is not yet reassigned. Outside the D20 shape; a framework pain (AC-7). **Triage: accepted** — todo punchlist row for AC-7 (skill names it; issue to file on Neatoo).
4. **Design.Infrastructure singletons hold per-operation state in unsynchronized collections.** Reachable by two concurrent Saves to a running Design.Server. **Triage: accepted** — "single-user demo store, not thread-safe" paragraph in the registration header.
5. **`OrderEditViewModel` comment said "the page's error boundary owns those", but Design.App had no `ErrorBoundary`.** **Triage: fixed** — `MainLayout.razor` wraps `@Body` in `<ErrorBoundary>` with an alert.

### Theoretical (not triaged)

- `skill-client-server-pair` re-registers the keyed `HttpClient`; if the skill ever sources it, the veto's question applies (it is a test-harness region and says why).
- `OnParametersSetAsync` has no cancellation; fast navigation during a Load can show the wrong order; returning to `/orders/new` replaces unsaved work.
- `Save`/`Load` in `skill-viewmodel-over-aggregate` were untested at round 1 (the test-reviewer's Should-cover closed this).
- Design.Infrastructure gave `InternalsVisibleTo` to Design.Server, which does not need it. **Removed.**

### Read report

Beyond the brief: `EntityBase.cs:470-533`; generated `OrderFactory` Save paths; `MudNeatooTextField` and a subscription grep across MudNeatoo; `WorkOrderTask.cs`; `App.razor`, `MainLayout.razor`, `index.html`; `TestInfrastructure.cs` mocks (spot-check); todo AC/Out of Scope/Dismissed/Discovery Log; VisionAlignment D6, D16, D18, D20, D24. Named but unused: `CLAUDE-DESIGN.md`; business-rules/parent-child prose (grep only).

---

## Round 2 — 2026-10-11 (HEAD `2f4fb12`, 196 tests)

**Verdict: CLEAN** (one new non-blocking callout)

- Veto resolved: `skill-client-composition` holds only the pointer; the keyed `HttpClient` is in a private method below `#endregion`. AC-4 satisfied.
- Callout 1 resolved in both pages: `SaveAsync` moves the subscription; `@key` rebuilds the subtree so old MudNeatoo fields dispose and new ones subscribe to the new wrappers; dead branch gone; if Save throws, `Vm.Order` is unchanged so skipping `Resubscribe` is correct. (`StateHasChanged()` after the `EventCallback` is redundant but harmless.)
- Callouts 2–5 and the IVT removal landed as described.
- **New callout (AC-4 · Must, Medium-High):** the `ErrorBoundary` never called `Recover()`; the layout outlives the page, so after one failed Save every page would keep showing the error until a reload. Reachable by a Save that throws, then navigating. **Triage: fixed** — `@ref` on the boundary and `Recover()` in the layout's `OnParametersSet` (build 0/0, 196 tests, logs refreshed).
- Theoretical: the page regions show `OnClick="SaveAsync"` while `SaveAsync`'s body is outside the region; plan 006 decides whether to widen the region or explain it in prose.
