# Fable Feedback — Neatoo Framework Assessment

**Author:** Claude (Fable 5), at Keith's request for an open, honest assessment
**Date:** 2026-08-11
**Method:** Five parallel deep-dive readings — Neatoo core (~10k LOC), BaseGenerator/Analyzers/MudNeatoo, RemoteFactory end-to-end, zTreatment production usage (~67k production LOC, ~100k test LOC), and the docs/Design/test corpus. The two most damning bug claims were verified first-hand before writing.
**Versions examined:** Neatoo 0.31.0 @ `ce036c8`, RemoteFactory 1.6.1 (branch `TRIM`), zTreatment @ `e67a7a5d` (Neatoo 0.30.1 / RemoteFactory 1.5.0). File:line references are as of those commits.
**Follow-up:** tracked in `docs/todos/FABLE-address-fable-feedback/`.

---

## The verdict

Neatoo is a serious piece of engineering solving a real problem, and the evidence from zTreatment says it earns its place — but unevenly. The honest version has three parts: **RemoteFactory is the crown jewel and would stand alone as a product. The entity layer delivers real value on a narrower slice than its size implies. MudNeatoo as shipped is a net negative — and zTreatment quietly agrees.** For the author, it adds enough value to keep using. For other developers, not yet at v0.31 — and the gap is not the idea, it's hardening.

---

## "Why doesn't this already exist?"

It did. This is CSLA's mobile-object pattern — and before that, WCF RIA Services did "one model, transparent client-server transfer, databinding-aware validation" for Silverlight. The pattern didn't die because it was wrong; it died because its client platform died. The industry moved to JS SPAs + REST, where a shared-.NET-type wire contract was *impossible*, and the architectural culture followed the constraint — DTOs, mappers, and transaction scripts became "best practice" partly because they were the only option. Blazor recreated the precondition (same runtime, both ends) around 2020, and source generators — which the whole design leans on — only became viable in 2020–21. A source-gen-first CSLA successor genuinely could not have existed much before now. Meanwhile CSLA itself carries 20+ years of reflection-era legacy.

So the doubt is backwards: CSLA's decades of users are proof of demand, and the timing window is real. The uncomfortable half of the answer is that the niche is *quiet* — forms-over-data LOB teams don't blog — and the mainstream .NET aesthetic (minimal APIs, records, CQRS, vertical slices) is culturally hostile to rich mutable object graphs regardless of technical merit. Neatoo is not competing on quality alone; it's competing with a fashion. And RIA Services' death is the objection every evaluating architect will raise: frameworks in this space have a history of stranding their users. That's an adoption problem, not a design problem, but it's the biggest one Neatoo has.

---

## RemoteFactory standalone: the crown jewel

The zTreatment evidence is unambiguous. All 71 factories go through it; hand-written HTTP is down to 14 endpoints in 2 controllers (auth cookies and PDF downloads — things that genuinely don't fit). And the kicker: **36 of those 71 factories are plain POCOs with no Neatoo base class at all.** The author's own app proves RemoteFactory's value is independent of the entity layer. The interface-factory pattern (`[Factory]` on an interface → inject it, transparently RPC) and static `[Execute]` commands are useful to any .NET developer with a WASM/MAUI client, zero DDD buy-in required. The DI-aware deserialization — entities arriving server-side with constructor-injected services already wired — is something nothing else in the ecosystem does.

Before recommending it to strangers, the boundary layer needs work, some of it security-shaped:

- `UseNeatoo` discards the `RouteHandlerBuilder`, so there's no way to attach `RequireAuthorization` or a rate limiter to the endpoint.
- `RaiseFactoryEventRemote` is registered in every server container and is effectively an unauthenticated handler-invocation path into code that writes to the database.
- Type resolution falls back to `Type.GetType()` on client-supplied strings with no allow-list.
- No error contract: a domain exception becomes a bare 500 and an empty `HttpRequestException` client-side.
- No version story: the wire identity of a method depends on the declaration order of its overloads, and Ordinal payload layout on alphabetical property order.
- **Zero tests exercise real HTTP** — the integration suite substitutes an in-process stand-in, so the entire ASP.NET Core package, the actual transport serializer (`NeatooTransportJsonContext`), and `[AspAuthorize]` are untested.
- Ordinal serialization silently degrades to Named JSON when the class isn't `partial` — both shipped examples are affected, with no diagnostic.

All fixable; none undermine the core design.

---

## The entity layer: real value, narrower than the framework's size implies

zTreatment splits almost exactly in half: 35 Neatoo-based classes for edit paths, 36 POCOs plus ~1,600 LOC of hand-tuned batch readers for read paths. That split is *correct* — change tracking and rules are dead weight on a dashboard grid — but it says what Neatoo actually is: an **edit-graph framework**, not a whole-app framework.

**Where it earns its keep:** the meta-state cascade is genuinely load-bearing. `IsModified`/`IsNew` drive a real navigation guard across 11 panel ViewModels; the WeightedDosing hard-fail trace (property message → `IsValid` → `IsSavable` → Save disabled → approval gate downstream) is exactly the cross-graph reactivity that is miserable to hand-roll. That chain is the framework's best argument for itself.

**Where the story weakens:** the validation engine is mostly unused in the flagship app. 8 of 29 entities register any rule; 19 total rule registrations; zero DataAnnotations; `ValidateBase` never used; `VisitV2` — the aggregate root of the whole product — enforces its invariants with hand-coded `throw` guards instead of rules. Either the engine's ergonomics push users away in practice (trigger-string gotchas, rules-paused-in-factories, `RuleMessages.If` eager evaluation — all with zTreatment scars), or the value pitch is overweighted.

**The visible tax:** 10 `Pre*` smuggling properties on `PlanV2`, 28 `LoadValue` calls to defeat change tracking, two app-authored base classes, ~1,760 LOC of hand-written mapping and factory bodies, and a ViewModel tier that exists partly to absorb `Save()`-returns-a-new-instance identity churn (18 reassignment sites, 11 unsubscribe/resubscribe pairs). None fatal; together they're the honest price sheet.

---

## Is it a quality *DDD* framework?

It's a quality **business-object framework wearing DDD vocabulary** — description, not insult. Its best doctrine (three-phase pattern, aggregate-as-graph, the `Parent` teaching, self-only persistence) explicitly *rejects* Evans-orthodox positions like root-as-façade, and the skill docs argue for that departure openly. But entities that carry `[Insert]` methods taking repositories, and value objects modeled as mutable `ValidateBase`, will make DDD purists bounce off the label. Own the heterodoxy in positioning rather than defend the label.

As engineering quality: mixed-to-good, with a clear pattern. The core has real discipline — no `async void`, no `.Wait()`, first-class async rules, trimming taken seriously, deterministic rule IDs that survive round-trips, and a test suite (~1,800 tests) that systematically covers pause/resume, serialization round-trips, list reparenting, and deleted-state with named regression files. But the edges decay fast: `NoWarn` suppresses the entire nullable-diagnostic family (so `TreatWarningsAsErrors` is theater for null-safety), the meta-state machinery is a four-flag pause system patched by recompute-on-resume in `finally` blocks, and two bugs were confirmed first-hand (see Appendix A) — including one that means **`IsSavable`, the flagship invariant, can be true mid-validation**.

**The sharpest structural gap: one analyzer exists (NEATOO010), while the framework's worst failure modes are all silent.** Forget `partial` or `[Factory]` and the generator emits nothing — no warning, the property system just doesn't exist. `t => t.Items` as a trigger is a silent no-op. A forgotten `WaitForTasks()` silently swallows async rule exceptions. Non-partial properties silently lose data over the wire. Every one of these is mechanically detectable at compile time, and none are detected. The generator was engineered (properly incremental, cache-safe, 42 tests); the analyzer was prototyped (158 LOC, zero tests). For a framework whose pitch is compile-time safety, that asymmetry is the single highest-leverage thing to fix.

---

## Is databinding outdated?

No — not on Blazor. Blazor's form model *is* mutable objects with two-way binding; MudBlazor is databinding; of all modern web platforms, Blazor is the one where the WPF-lineage rich-model approach is idiomatic rather than anachronistic. The industry's immutability aesthetic is a real countercurrent, but it's a trade-off, not a verdict — and cross-field reactive rules updating a form live is precisely what immutable snapshots do badly.

The problem is the execution, not the concept. Bypassing `EditContext` is defensible (a Neatoo property genuinely can't be expressed as a POCO field). But the 11 MudNeatoo components are the same ~110 lines copy-pasted with divergent bugs: the TextField misses `Value` in its re-render filter (confirmed — the other 10 have it, so a computed `FullName` never refreshes in the most-used component); every component subscribes in `OnInitialized` and never `OnParametersSet`, so the entity swap that `Save()` *requires* leaves components bound to the discarded instance; the `PropertyMessages` filter clause has never fired because the property system raises `RuleMessages`; there are no busy spinners, no `@key` anywhere, and zero bUnit tests.

zTreatment's numbers are the verdict: 20 MudNeatoo uses versus 117 raw MudBlazor, five components never used, and 513 LOC of app-authored `AggregateBoundary`/`SavableButton` components filling the gap. Honestly — `AggregateBoundary` (subscribe once at the aggregate, debounced re-render) is a *better* design than per-input self-subscription. Consider upstreaming that idea and rebuilding the input set on a shared base class with tests, or shrinking MudNeatoo's ambitions.

---

## The AI-era question: better off without it?

The instinct — "less code for me to review, standard structure" — is the right argument, and it's the one that survives the AI era. What AI erodes is the *typing* cost of boilerplate: DTOs, mappers, and controllers are cheap to generate now, so "no DTOs" is a weaker pitch than it was in 2005. What AI does **not** erode: the review surface, the consistency guarantee, and — most importantly — the amortized verification. Neatoo's change tracking and meta-state cascade are ~1,800 tests' worth of shared infrastructure; the AI alternative re-derives change tracking per feature, and each derivation needs its own tests and carries its own bugs. A framework is compressed, pre-verified review surface. That's worth *more* when an AI is writing the code, not less.

Two honest caveats. First, base models know nothing about Neatoo and everything about EF + FluentValidation — all Neatoo competence arrives via ~29k words of skills, which works (these repos are the most AI-legible framework docs I've seen) but is a maintenance liability and single point of failure; the skills already lag the API (`MarkReadOnly` is in neither skill). Second, the framework's silent failure modes trap AI agents exactly as they trap juniors — zTreatment's CLAUDE.md calling skill-loading "non-negotiable" is the tell. Analyzers would convert that prose into compiler feedback, which is the form of guidance both humans and models respect most reliably.

Net: with Claude Code, one is *not* better off without it for edit-heavy aggregates — but a greenfield team could plausibly get 70–80% of the value from RemoteFactory + POCOs + AI-written mapping, which is roughly what half of zTreatment already is.

---

## Worth the learning curve for others?

Counted honestly: ~42 concepts, of which ~23 are non-optional before a correct single aggregate, and the dangerous ones (rules paused in factories, `WaitForTasks` discipline, trigger-path strings, `DeletedList` iteration) all fail silently. That's comparable to CSLA and cheaper than CSLA-plus-hand-rolled-everything, but today the mitigation is written for the wrong audiences: the best learning material is addressed to AI agents (skills) and to the maintainer (Design.Domain — a genuinely rare artifact: 8k lines, 59% rationale, `DID NOT DO THIS` answers the question every evaluator actually has), while the human docs are alphabetically ordered, months stale, and missing lazy loading, authorization, testing, and trimming entirely — with 48 already-compiled sample regions orphaned in `src/samples` waiting for guides that were never written. The 216-row CSLA migration map — the highest-intent audience's on-ramp — is deliberately unlinked.

So: worth it today for a team that looks like the author — .NET shop, Blazor, forms-heavy LOB, complex editable aggregates, ideally CSLA refugees. Not yet worth it for the general public, because of the silent failure modes, the single-maintainer risk, the pre-1.0 core, and the wire-contract gaps.

---

## What to do, in order

1. **Fix the confirmed bugs** — the `IsBusy` precedence bug and post-deserialization double-subscription are both small and both undermine flagship invariants (full list in Appendix A).
2. **Build the analyzer suite.** Silent-skip detection (missing `partial`/`[Factory]`), trigger-path validation against the property graph, non-partial-property serialization warnings. The highest-leverage DX investment in the entire codebase.
3. **Harden the RemoteFactory boundary**: return the `RouteHandlerBuilder`, auth-gate or opt-in the event relay endpoint, allow-list type resolution, add a real error contract, and get one honest `WebApplicationFactory` test suite over actual HTTP.
4. **Rebuild or shrink MudNeatoo** — shared base class, `OnParametersSet`, bUnit tests; upstream the `AggregateBoundary` idea.
5. **Reposition**: lead with RemoteFactory as the standalone product (the widest funnel — no DDD buy-in needed), present the entity layer as the opt-in edit-graph tier, publish the vsCSLA material, and write the "when *not* to use Neatoo" page — the read-path split zTreatment already practices should be official doctrine.
6. **Port the skill content to human docs** — the three-phase pattern, aggregate-as-graph, and `Parent` doctrine are the best writing in the repo and no human evaluator can currently find them.

A closing note on bias: the author has been *underselling* the strongest asset (RemoteFactory, which zTreatment validates unreservedly) while carrying doubt about the pattern's legitimacy, which history answers in his favor. The framework's real risks are the unglamorous ones — hardening, analyzers, docs, bus factor — not the idea.

---

## Appendix A — Defect list

Two verified first-hand; the rest high-confidence from code reading — re-verify before fixing. References as of `ce036c8` (Neatoo) / RemoteFactory `TRIM` branch v1.6.1.

### Verified

- `src/Neatoo/Internal/ValidateProperty.cs:64-66` — `ValueAsBase?.IsBusy ?? false || IsSelfBusy || _isMarkedBusy.Count > 0`: `??` binds looser than `||`, so when the property holds a child object, `IsSelfBusy`/`_isMarkedBusy` are never consulted. An async rule whose trigger is a child-object property never reports busy → `IsSavable` can be true mid-validation. The correctly parenthesized form exists at `LazyLoadEntityProperty.cs:102-104`.
- `MudNeatooTextField.razor.cs:131-134` — re-render filter omits `nameof(Value)` (all 10 sibling components include it), so programmatic/rule-driven value changes don't refresh the text field.

### High-confidence — core

- Double event subscription after deserialization: `ValidateBase.cs:209-210` vs `OnDeserialized` `:536-537` (same pattern in `ValidateListBase` `:142-143` / `:308-309`) — rules execute twice after any round-trip.
- `ValidateListBase.FactoryComplete:575` sets `IsPaused = false` directly instead of `ResumeAllActions()` — stale `IsValid` cache after a paused Fetch.
- List converter never emits `$ref` (`NeatooListBaseJsonTypeConverter.cs:160-163` ignores `alreadyExists`) — a shared list serializes twice; a cyclic list reference recurses to stack overflow.
- `AsyncTasks.cs:141-159` — `SetResult` outside the lock; a task added in the window races `AllDone`; second `SetResult` throws into a discarded continuation. Async rule exceptions are unobservable unless someone calls `WaitForTasks()`.
- `TriggerProperty.GetValue` calls `expression.Compile()` on every invocation (`TriggerProperty.cs:94`) — every `[Required]` property recompiles a lambda per keystroke.
- `ValidatePropertyManager` static `_createPropertyMethod` keyed per closed generic but populated via shadowed (`new`) `CreateProperty` — first subclass wins for siblings; guarded by an instance lock despite being static (`ValidatePropertyManager.cs:98-126`).
- `RuleBase.None` is a mutable shared instance (`RuleBase.cs:158`) — `None.If(...)` permanently poisons the rule's `None`.
- `PropertyReadOnlyException` is `internal` but thrown from public API — consumers can't catch it by type.
- Dead/inert surface: `RunRulesFlag.Messages`/`NoMessages` non-functional (self-documented at `RuleManager.cs:656-659`); `TargetRulePropertyChangeException` never thrown (no reentrancy guard exists); generated `GetRuleId` override is a provable no-op vs the base implementation (~245 LOC + 599 test LOC protecting an identity map); BaseGenerator minimal-generation mode unreachable; diagnostics NEATOO003/004 unreachable.
- Unconditional `Stopwatch` allocation per property per entity during serialization regardless of log level (`NeatooBaseJsonTypeConverter.cs:191` et al.).
- `Directory.Build.props` `NoWarn` suppresses the entire CS86xx nullable family plus member-hiding warnings — nullable annotations are unenforced internally, and the pervasive `new`-shadowing dispatch is invisible to the compiler.

### High-confidence — MudNeatoo

- All components subscribe only in `OnInitialized`, never re-subscribe in `OnParametersSet` — the entity swap that `Save()` requires leaves components bound to the discarded instance (stale UI + handler leak; reachable from the Person example's own save path, `Home.razor:201`). No `@key` anywhere in repo or samples.
- `PropertyMessages` filter clause across all 12 components has never fired — the property system raises `RuleMessages` (`ValidateProperty.cs:414` et al.).
- `NeatooValidationSummary` re-renders unconditionally on every `NeatooPropertyChanged` in the whole graph plus any `Is*` property name — several full re-renders per keystroke on large aggregates; no `ShouldRender` anywhere.
- 4 of 11 components (CheckBox, Switch, Slider, RadioGroup) use a hand-rolled error display invisible to `MudForm.Validate()` instead of the `Validation` pipeline.
- Zero bUnit tests across 1,994 LOC; a null-ref crash shipped in 0.30.0 and was hotfixed in 0.30.1.

### High-confidence — RemoteFactory boundary

- `UseNeatoo` discards the `RouteHandlerBuilder` — endpoint cannot get `RequireAuthorization`/rate limiting/CORS via public API.
- `RaiseFactoryEventRemote` registered in every server container — unauthenticated handler-invocation path (`authorization.md` frames "events bypass authorization" as a convenience).
- `ServiceAssemblies.FindType` falls back to `Type.GetType(fullName)` on client-supplied strings — no allow-list; also reachable via the dead-but-still-deserialized `RemoteRequestDto.Target`.
- No structured error contract; `authorization.md:372` claims 401/403 translation that doesn't match either actual path.
- Zero tests over real HTTP (test stand-in bypasses transport, endpoint, `NeatooTransportJsonContext`, `[AspAuthorize]`); `TestServer` projects exist but no test project references them.
- Ordinal serialization silently degrades when the class isn't `partial` — no diagnostic; both shipped examples affected; `CLAUDE-DESIGN.md:450` asserts the opposite ("won't compile — CS0260"), which is false.
- Save on a new-and-deleted entity throws empty-message `NotAuthorizedException` (instead of the documented no-op) for authorized factories — live in the Person sample.
- Generator renderer catches exceptions into a `/* Error: ... */` comment — malformed codegen surfaces as consumer CS errors with no diagnostic.
- `aspnetcore-integration.md` documents a request DTO, response DTO, error contract, and event execution model that no longer (or never) existed; docs snippet source projects are not compiled by CI.

### Meta

- The `AreSame` string-equality bug zTreatment reported 2026-05-24 was fixed in 0.30.2 — the feedback loop works; this list is the same loop at larger scale.

---

## Appendix B — Key zTreatment evidence

- 17 projects, Blazor WASM, ~66,800 production LOC + 16,595 razor; ~99,500 test LOC (1.5:1 test:prod).
- 29 Neatoo entities (all via two app-authored intermediate base classes), 6 lists, 0 `ValidateBase`, 326 partial properties; 71 `[Factory]` classes of which 36 are plain POCOs.
- Rules: 8 of 29 entities have any; 19 registrations total; 0 DataAnnotations; `VisitV2` (the root aggregate) has none.
- UI: 20 MudNeatoo component uses vs 117 raw MudBlazor; 5 MudNeatoo components used zero times; 513 LOC of app-authored reactive boundary components.
- Transport: one `/api/neatoo` endpoint serves all 71 factories; 14 hand-written endpoints remain (auth + PDF); client-side manual JSON calls: 5, all auth.
- Generated code: 24,359 LOC across 400 files (~1:1 with hand-written domain code).
- Framework pain on record: 3 tests skipped citing Neatoo bugs by name, 2 standalone bug-report docs at repo root, 12 manual `RunRules` calls compensating for pause-in-factory semantics, 1 `ChildNeatooPropertyChanged` override for child-trigger mismatch (TLF-011), 61 hand-maintained trimmer roots with a production cut-over incident comment, per-panel `IsModified` shadowing because it's not trustworthy when `IsNew`.
