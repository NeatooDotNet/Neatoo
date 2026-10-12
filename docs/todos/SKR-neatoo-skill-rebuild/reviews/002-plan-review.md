# Plan Review — SKR-002 — Design.App and Design.Server in CI

**Reviewer:** plan-reviewer
**Draft reviewed:** `3ff6a1b` on `skr-002-design-app-server` (nothing implemented)

---

## Round 1 — 2026-10-11

**Verdict: CONCERNS**

### Pass A — documented requirements

Docs consulted: `docs/VisionAlignment.md` §0 (D6, D16, D18, D20, D24), `todo.md` (Goal, AC, T1/T8, Out of Scope, Dismissed, Discovery Log), `.claude/rules/design-snippets.md`, `.claude/rules/docs.md`, `CLAUDE.md`.

**Veto-tier**

- **V1 — D16 contradicted by the T8 example.** `VisionAlignment.md:37`: a property named for a screen element (`ShowTreatmentPanel`) is a code smell; it belongs in a ViewModel or read model. The plan points the page at "the visibility rules" (`skill-visibility-rules`, `WorkOrder.cs:56-68`), whose main property is `ShowHoldBanner` — named for a screen element, with the comment "A show/hide decision is domain state. The UI binds `@if (ShowHoldBanner)`". Constraint "every value the page shows or hides on comes from a property of the aggregate" also rules out the ViewModel, which is where D16 says such a value belongs. As written the compiled T8 example would endorse the D16 smell. Plan 005 renames `ShowTreatmentPanel`; nothing covers `ShowHoldBanner`, which is also taught in `docs/guides/business-rules.md:688-703`, `parent-child.md:356-363`, `skills/neatoo/references/domain-logic-placement.md:113-133` and asserted at `WorkOrderAggregateTests.cs:119,126`. Options: (1) page binds only facts (`CanApprove`, `IsOnHold`, `IsOverBudget`, `HasUnschedulableTasks`) and the constraint is reworded so the ViewModel may name a screen element by mapping a fact; (2) rename the property to a business fact — changes an existing test, user's call.

### Pass B — codebase

Files examined: Neatoo `AddNeatooServices.cs`, `NeatooJsonConverterFactory.cs`, `NeatooBaseJsonTypeConverter.cs`; RemoteFactory at tag v1.9.0 (unchanged through 1.10.2) `AddRemoteFactoryServices.cs`, `RemoteFactory.AspNetCore/ServiceCollectionExtensions.cs`, `WebApplicationExtensions.cs`, `ServiceAssemblies.cs`, `NeatooRuntime.cs`, `MakeRemoteDelegateRequest.cs`; RF `src/Design` server/client/tests; Design `WorkOrder.cs`, `IWorkOrderInterfaces.cs`, generated `OrderFactory`/`WorkOrderFactory`, `TestInfrastructure.cs`, `DomainRegistration.cs`, `ServiceRegistration.cs`, csprojs, `Design.sln`; Person App/Server; MudNeatoo csproj; `Directory.Build.props`, `Directory.Packages.props`, `mdsnippets.json`; `Neatoo.UnitTest/ClientServerContainer.cs`; NuGet cache nuspecs (MudBlazor 9.0.0, WASM 9.0.13, RF 1.9.0); two offline scratch builds.

**Reality check (direction fits).** WorkOrder already has `CanApprove`, `IsOverBudget`, `HasUnschedulableTasks`, the `Approve` verb, `Create`, `[Remote] Fetch` and Save through `IWorkOrderFactory`; Step 4 needs no domain code apart from V1. MudBlazor 9.0.0 ships `lib/net9.0`. `VersionOverride` is already used (`Neatoo.csproj`). Restore experiment: mixed 9.0.x transitive versions resolve to the highest with no NU1605. Build experiment: an MSTest net9.0 project referencing a net9.0 BlazorWebAssembly project and a Web project builds and runs tests using both; no wwwroot/_framework or DevServer in test output. `dotnet build` never runs ILLink, so trim settings are not exercised by CI.

**Veto-tier:** none beyond V1.

**Callouts (all Must-affecting, AC-4)**

- **C1 — `AddNeatooServices` + `AddNeatooAspNetCore` is order-dependent, and one order breaks every remote Save.** RF 1.9.0 `AddNeatooRemoteFactory` uses `Add`, not `TryAdd`; `AddSingleton<IServiceAssemblies>(new ServiceAssemblies(assemblies))` and `AddTransient<NeatooJsonConverterFactory, NeatooInterfaceJsonConverterFactory>` are last-wins. Order A (Neatoo first, then `AddNeatooAspNetCore(domainAsm)`) undoes `AddNeatooServices.cs:121-122` (interface converter back beside Neatoo's) and replaces `IServiceAssemblies` with one lacking Neatoo.dll; `NeatooBaseJsonTypeConverter.cs:406` writes `$type` as a non-assembly-qualified `FullName`, `:185` resolves it via `localAssemblies.FindType` → `Type.GetType` from RemoteFactory.dll → null → `MakeGenericType` throws NRE. The server cannot deserialize any Neatoo entity the client sends: every `[Remote]` Save fails, Fetch works. Order B (AspNetCore first) nets to `AddNeatooServices` alone plus `IAspAuthorize`. `UseNeatoo` needs only `ICorrelationContext`, `HandleRemoteDelegateRequest`, `NeatooSerializationOptions`, all registered by `AddNeatooServices(Server)`; `IAspAuthorize` is needed only for `[AspAuthorize]`, which Design.Domain does not use. So `AddNeatooServices(Server)` + `UseNeatoo()` is sufficient. Whatever shape is chosen, the Neatoo region's header has to state this. Reachable by following RF's `AddDesignServer` seam and adding the Neatoo line first — the natural order with both skills loaded.
- **C2 — In-process tests cannot catch C1 and cannot tell whether the handler reassigns.** The local Save path returns the same instance (`OrderFactory.g.cs:352-369`), so a handler that drops `Save()`'s result passes "reassigns the result"; no serialization happens, so bullet 1 passes under Order A. `Neatoo.UnitTest/ClientServerContainer.cs:41-143` has the pattern: a stub `IMakeRemoteDelegateRequest` that serializes and dispatches to the server container's `HandleRemoteDelegateRequest`. Building that pair from the two composition seams closes both gaps. Design.Tests has no such harness today.
- **C3 — Client composition: no seam, and resolving a factory alone does not prove Remote mode.** Resolving a Remote-mode factory needs no host but needs the keyed `HttpClient` (`MakeRemoteDelegateRequestHttpCall` calls `GetRequiredKeyedService<HttpClient>(HttpClientKey)`); constructing an `HttpClient` makes no connection. Step 2 names no seam; `Person.App/Program.cs:14-21` composes inline on `HostEnvironment.BaseAddress`, which a test cannot call; RF's Client.Blazor has no client seam either. Resolution also succeeds in Server and Logical mode, so a resolve-only test passes under the wrong mode; an observable `[Remote]` call (C2) distinguishes Remote. The `IsServerRuntime` switch in Design.App's csproj does not reach the test process (`NeatooRuntime.IsServerRuntime` defaults true).
- **C4 — Server repositories have no home.** The only implementations of the ~25 `[Service]` interfaces are `internal` mocks in Design.Tests (`TestInfrastructure.cs:63-112,152,442`). Design.Server cannot reference Design.Tests (Tests will reference Server); Design.Infrastructure is empty; moving the mocks conflicts with "Design.Tests unchanged". The mocks are scoped — per request in a host, so a running Design.Server would lose rows between requests. The server also needs `IShippingRateService`, and both tiers need `AddDesignDomainRules()`.
- **C5 — CA1515 breaks the build.** `Directory.Build.props` sets `AnalysisMode=all`, `TreatWarningsAsErrors=True`, NoWarn omits CA1515. Scratch build of a net9.0 BlazorWebAssembly project under those settings: a public class gives `error CA1515` (CA1822, CA2007 also fire but are already in NoWarn). Design.Server, an exe, will do the same. Design.Tests needs visible ViewModel, handler and seam types: `internal` + `InternalsVisibleTo` as `Design.Domain.csproj` does, or Design.Domain's `AnalysisMode` opt-out.
- **C6 — Regions in Razor markup.** `design-snippets.md:7` names only `#region`; in `.razor` markup `#region` is text and renders on the page. The markup needs a comment-style marker and mdsnippets must be confirmed to parse it from `.razor`. The rule's "every snippet's behaviour is pinned by a test" is not met by the page-binding region (pinned by the build only); Step 6 rewrites the rule file but does not say it adds that exception.

### Theoretical (not triaged)

- With `PublishTrimmed=true` the trim analyzer runs at build on Design.App's own code under TreatWarningsAsErrors; bites only if that code uses reflection. `TrimMode`/`TrimmerRootAssembly` never exercised by CI.
- WorkOrder's `Create` and `Fetch` call `RunRules` (`WorkOrder.cs:145,177-178`). If plan 003 changes them, a ViewModel test asserting `CanApprove` after Create/Fetch depends on 003 assigning the derived value (D18).
- `.claude/rules/docs.md:2` also limits snippet sources to Design.Domain and Design.Tests; Step 6 does not update it.
- Double `RegisterFactories` leaves duplicate descriptors; matters only to code enumerating `IEnumerable<IFactorySave<T>>`.

### Read report

- Beyond the brief: RF v1.9.0 registration and serializer internals, Neatoo's JSON converters, generated factories, `TestInfrastructure.cs`, `DomainRegistration.cs`, `ClientServerContainer.cs`, `Directory.Build.props`, `mdsnippets.json`, NuGet nuspecs, two scratch builds (session scratchpad, not the repo).
- Named but unused: `src/Design/CLAUDE-DESIGN.md`.

### Orchestrator verification (2026-10-11)

- C1 confirmed in code: `AddRemoteFactoryServices.cs:62,65` (v1.9.0) use `AddSingleton`/`AddTransient`; `AddNeatooServices.cs:114-122` appends Neatoo.dll to the assemblies and `RemoveAll`s the converter factory after the call.
- V1 confirmed: `WorkOrder.cs:57-61`.

### Triage

See the plan's Amendments entry of 2026-10-11 and the todo's Discovery Log.
