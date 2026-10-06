# Consistency audit: neatoo + mudneatoo skills

Read-only audit, 2026-10-05. No repo file edited, nothing built or run.

**Root for all sites:** `C:\Users\KeithVoels\source\repos\neatoodotnet\Neatoo\skills\`

Path legend used in the tables:

| Prefix | Means |
|---|---|
| `N:` | `neatoo\` (e.g. `N:SKILL.md`) |
| `N/r:` | `neatoo\references\` |
| `M:` | `mudneatoo\` |
| `M/r:` | `mudneatoo\references\` |

Source used for STALE checks: `Neatoo\src\Neatoo\`, `Neatoo\src\Neatoo.BaseGenerator\`, `Neatoo\src\Neatoo.Blazor.MudNeatoo\`, `Neatoo\src\samples\`, `Neatoo\src\Examples\Person\`, and (for NF0105 / `FactoryOperation` / `LazyLoad` only) `RemoteFactory\src\`.

**Kinds.** *prose* and *hand-written code* are edited in the skill file. *generated snippet* is edited in the named `src\samples\*.cs` region, then `dotnet mdsnippets` re-renders the skill.

**Snippet freshness.** A script compared all 90 `<!-- snippet: -->` placeholders in both skills against the `#region` blocks in `src\samples\`. 89 match text and line anchors exactly; 1 is un-rendered (row ST-1). So every generated-snippet finding below is a problem in the sample source, not a stale render.

**Dependency fact that constrains the S7 fix.** `Neatoo\Directory.Packages.props:30` pins `Neatoo.RemoteFactory` 1.6.1. The samples compile against that, so a snippet cannot show RemoteFactory 1.9 `[Remote, Execute]` semantics until the pin moves to 1.9 or later.

## Counts

| Class | Rows |
|---|---|
| CONTRADICTS S1 | 4 |
| CONTRADICTS S2 | 2 |
| CONTRADICTS S3 | 2 |
| CONTRADICTS S4 | 1 |
| CONTRADICTS S5 | 3 |
| CONTRADICTS S6 | 5 |
| CONTRADICTS S7 | 3 |
| CONTRADICTS S8 | 3 |
| CONTRADICTS S9 | 4 |
| CONTRADICTS S10 | 2 |
| CONTRADICTS S11 | 2 |
| CONTRADICTS S12 | 2 |
| CONTRADICTS S13 | 3 |
| **CONTRADICTS total** | **36** |
| INTERNAL | 28 |
| STALE | 17 |
| UNRULED | 10 |
| **All rows** | **91** |

Omissions (a settled point never stated at all) are listed separately at the end and are not counted.

---

## CONTRADICTS S1 (tier and `[Remote]`)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S1-1 | `N/r:lazy-loading.md:126` | "**Do NOT create `EntityLazyLoad<T>` in `[Fetch]` or `[Create]`** — these only run server-side." | CONTRADICTS S1. Also INTERNAL with `N/r:entities.md:681` ("Client code calls `itemFactory.Create()`") | prose | "a factory method body runs on one tier and the loader delegate is not serialized; the constructor runs on both tiers" |
| S1-2 | Root aggregates whose persistence operations take a server-only `[Service]` repository but are `public` with no `[Remote]`. 6 sites: `N/r:base-classes.md:129-154`; `N/r:entities.md:481-501` and `:513-516`; `N/r:source-generation.md:39-55`; hand-written `N/r:entities.md:611-612` and `:639-641` | "`[Insert]` / `public async Task InsertAsync([Service] ISkillEmployeeRepository repository)`" | CONTRADICTS S1 | generated snippets `edit-base-sample` (`BaseClassSamples.cs`), `entities-cascade-insert` and `entities-cascade-update` (`EntitiesSamples.cs`), `api-generator-save-factory` (`ApiReferenceSamples.cs`); plus hand-written code | Make root Fetch/Insert/Update/Delete `[Remote] ... internal`; say once that samples run under `NeatooFactory.Logical` |
| S1-3 | Child `[Create]` kept `public`. `N/r:entities.md:673, 681, 691, 707-709, 724, 734`. Generated snippets that depend on it (external code calls a child factory `Create()` or `Fetch()`): `N/r:entities.md:321, 411, 560`; `N/r:collections.md:35, 115, 118, 238, 301, 306, 352`; `N/r:validation.md:413` | "`[Create]` stays `public` because client code needs to create objects before adding them to collections." | CONTRADICTS S1 read literally ("Child entity operations are `internal`"). Needs the author to confirm whether child `[Create]` is exempt. Also INTERNAL with `N:SKILL.md:120` and `M:SKILL.md:316` (`Items.AddItem()` idiom); see U-9 | prose + hand-written code; dependent generated snippets `entities-child-state`, `entities-parent-property`, `entities-cascade-correct-external` (`EntitiesSamples.cs`), `collections-add-item`, `collections-parent-cascade`, `collections-iteration` (`CollectionsSamples.cs`), `skill-coll-new-vs-existing-removal`, `skill-coll-add-existing-marks-modified` (`CollectionSamples.cs`), `validation-cascade` (`ValidationSamples.cs`) | Either write the exemption down, or make child `[Create]` internal and show list `AddItem()` as the only creation path |
| S1-4 | `N/r:pitfalls.md:15` | "Adding `[Remote]` to child entity factory methods \| Unnecessary—child methods are called from server" | CONTRADICTS S1 (weaker than "never") | prose | "Never: child operations are `internal` and reached only from the parent's factory method" |

Related rows filed under other classes: ST-14 (`entities.md:733`, NF0105 stated backwards), ST-15 (`entities.md:729`, dual-use entities), U-4 (lazy-loaded child fetched through `[Remote]`), S7-2 (commands "execute on the server").

## CONTRADICTS S2 (Create / Fetch / Execute)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S2-1 | `[Fetch]` whose parameters are the object's data rather than an identifier; tests "fetch" objects that never existed. 11 sites: `N/r:base-classes.md:129-136`, `:268-275`; `N/r:entities.md:191`, `:249`, `:279`, `:481-486`; `N/r:testing.md:242`, `:278`; `N/r:collections.md:306`, `:352`; hand-written `N/r:entities.md:712-713` | "`[Fetch]` / `public void Fetch(int id, string name, string email, decimal salary)`" and "`factory.Fetch(1, "ORD-001", DateTime.Today)`" | CONTRADICTS S2 | generated snippets `edit-base-sample`, `readonly-base-sample` (`BaseClassSamples.cs`); `entities-modification-state`, `entities-persistence-state`, `entities-savable`, `entities-cascade-insert` (`EntitiesSamples.cs`); `test-change-tracking` (`TestingPatternsTests.cs`); `skill-coll-new-vs-existing-removal`, `skill-coll-add-existing-marks-modified` (`CollectionSamples.cs`); plus one hand-written block | `Fetch(key)` that loads through a `[Service]` (mock) repository; a child `Fetch` takes the persisted row |
| S2-2 | `N:SKILL.md:55`, `:130`; `N/r:domain-logic-placement.md:28-32`, `:542` | "`[Execute]` / `[Fetch]` on a plain `[Factory]` class or a static command"; "(normalize, stage forward, seed)"; "The seam's `Open` / `[Fetch]` (3)" | CONTRADICTS S2: a load-time seam that may seed (create) or return existing is offered as a `[Fetch]` | prose | "`[Fetch]` only when the object always exists; a seam that may create or load is a static `[Remote, Execute]`" |

## CONTRADICTS S3 (baseline inside a factory operation)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S3-1 | `LoadValue` or `PauseAllActions` shown or recommended inside a factory operation. 8 sites: hand-written `N/r:rules-lifecycle.md:58-59`, `N/r:properties.md:203-204`; generated `N/r:lazy-loading.md:77-80`, `N/r:properties.md:382`; prose `N/r:properties.md:169`, `:229`, `N/r:validation.md:574`, `N/r:rules-lifecycle.md:19` (soft: describes `LoadValue` without naming a factory method) | "`this["Quantity"].LoadValue(data.Quantity);`"; "`using (PauseAllActions())` / `this["Id"].LoadValue(id);`"; "Sets value (Fetch escape hatch, bypasses IsReadOnly)" | CONTRADICTS S3 | hand-written code; generated snippets `skill-lazyload-constructor-pattern` (`LazyLoadSamples.cs:95-98`; the same pattern sits un-rendered at `LazyLoadSamples.cs:41-45`) and `properties-load-value` (`PropertiesSamples.cs`); prose | Plain assignment (`Quantity = data.Quantity;`), drop the `PauseAllActions` wrapper; prose: "plain assignment inside a factory method is already a baseline load" |
| S3-2 | `N/r:entities.md:154-166` | "`base.FactoryComplete(operation); // ALWAYS call base first`" then "`// Logic after Create — e.g., set defaults` / `Status = "Draft";`" | CONTRADICTS S3 and S11. Verified: `EntityBase.cs:594-621` and `ValidateBase.cs:986-989` resume actions inside `base.FactoryComplete`, so the assignment after it is a live edit: rules run and the new object reports `IsModified=true`. Also INTERNAL with `N/r:pitfalls.md:13` ("set defaults in `Create()`") | generated snippet `skill-factory-complete` (`SkillGapSamples.cs`) | Set defaults inside `[Create]`; keep the `FactoryComplete` example to work that sets no state |

## CONTRADICTS S4 (self-persistence)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S4-1 | `M/r:aggregate-reactive-vm.md:99-104` | "`Plan = (IPlan)await _plan.Save(); // Setter unsubscribes old, subscribes new`" | Possible CONTRADICTS S4: in `N:SKILL.md:170` and `:225-238` `Plan` is a child of `Visit`; here external code saves it. Also INTERNAL with `M:SKILL.md:259`: no `WaitForTasks()` or `IsSavable` check before the save | hand-written code | Save through the root (or rename the example aggregate) and add `await WaitForTasks()` |

No other S4 contradiction found; `N/r:entities.md:433-669` and `N/r:pitfalls.md:20-21` agree with S4.

## CONTRADICTS S5 (rules over exceptions)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S5-1 | `N/r:domain-logic-placement.md:373-374` | "`if (Status != "Pending")` / `throw new InvalidOperationException($"Cannot approve from status '{Status}'");`" | CONTRADICTS S5 | hand-written code | Remove the throw; the `CanApprove` rule (or a validation rule on `Status`) is the gate |
| S5-2 | `N:SKILL.md:76`; `N/r:domain-logic-placement.md:541` | "An admission check before a verb runs \| The seam refuses (3); the ViewModel mirrors *by reading*…"; "A guard in the ViewModel that duplicates one the seam already enforces" | CONTRADICTS S5 (a factory method that checks and refuses) | prose | "Admission is an entity `CanX` rule or a validation rule; the seam does not check and return" |
| S5-3 | `N/r:base-classes.md:219-227` | "`catch` / `{ return false; }`" | CONTRADICTS S5 (soft: an application failure is swallowed into a `bool`) | generated snippet `command-base-sample` (`BaseClassSamples.cs`) | Let the exception propagate |

## CONTRADICTS S6 (rules that call the server)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S6-1 | `N/r:domain-logic-placement.md:190-220` (heading `:190`, `:192`, `:204`, code `:206-219`) | "## Pattern 4: Async Rules — the Client-Resident Case Only"; "**Do not use a rule to fetch.** A rule that calls a remote service fires a server round-trip from inside a property setter: on every keystroke…" | CONTRADICTS S6. Two supporting claims are also STALE: "on every keystroke" (MudNeatoo text and numeric fields are `Immediate="false"` and commit on blur: `MudNeatooTextField.razor:12`, `.razor.cs:116`) and "no cancellation" (`AddActionAsync` token overloads, `RuleManager.cs:556-592`; INTERNAL with `N/r:validation.md:531-555`). The section's only positive example (`:194-202`) is a synchronous `AddAction` | prose + hand-written code | Rewrite Pattern 4: an async rule takes an injected `[Remote, Execute]` command delegate, never a server-only service; trigger on field commit |
| S6-2 | `N:SKILL.md:80`, `:61`; `N/r:domain-logic-placement.md:23-25` | "Rule (1) **only** if it never leaves the browser; otherwise a seam (3) the ViewModel invokes"; "…and it would round-trip → NOT a rule. A seam the ViewModel invokes (rung 3)" | CONTRADICTS S6 | prose | "Rule (1), with the server call encapsulated in an injected command" |
| S6-3 | A rule or entity constructor takes a service directly instead of an injected command. 7 sites: generated `N/r:validation.md:105-119`, `:134-155`, `:192-199`; `N/r:shared-rules.md:32-52`, `:77-84`; hand-written `N/r:domain-logic-placement.md:164-171`, `:551-587` | "`IValidationUniquenessService uniquenessService) : base(services)`"; "`var isUnique = await _validationService.IsEmailUniqueAsync(target.Email);`" | CONTRADICTS S6 (uniqueness and eligibility lookups are server data) | generated snippets `validation-async-rule` (`ValidationSamples.cs`), `skill-async-rule-class`, `skill-rule-registration` (`SkillGapSamples.cs`), `shared-rule-class`, `shared-rule-entity-usage` (`SharedRulesSamples.cs`); plus hand-written code | Replace the service with an `[Execute]` delegate in the samples and the two hand-written blocks |
| S6-4 | `N/r:shared-rules.md:122`; `N/r:validation.md:100` | "The rule needs injected services (repository, external service, etc.)"; "For rules that need to call services or databases:" | CONTRADICTS S6 | prose | "needs an injected command (`[Execute]` delegate); never a repository or EF" |
| S6-5 | `N/r:blazor.md:265` | "**Show validation early** -- Display errors as user types, not just on submit" | CONTRADICTS S6 (commit, not keystroke). Also STALE against `Immediate="false"` in `MudNeatooTextField.razor:12` and `MudNeatooNumericField.razor:12` | prose | "Display errors when the field commits" |

## CONTRADICTS S7 (commands on RemoteFactory 1.9+)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S7-1 | `N/r:base-classes.md:202-229` | "/// The delegate is injected via DI and always executes on the server."; "`[Execute]` / `internal static async Task<bool> _SendEmail(`" | CONTRADICTS S7 | generated snippet `command-base-sample` (`BaseClassSamples.cs#L204-L233`) | `[Remote, Execute]` + `private static`; comment "crosses to the server because of `[Remote]`". Needs the RemoteFactory pin raised to 1.9 or later first |
| S7-2 | `N:SKILL.md:276`; `N/r:base-classes.md:12`, `:187-189`; `N/r:pitfalls.md:85` | "Command \| Static class with `[Execute]` \| Stateless server-side operation"; "Use for operations that execute on the server without persistence state." | CONTRADICTS S7 (and S1) | prose | "`[Remote, Execute]` when it needs the server; a bare `[Execute]` runs on the calling tier" |
| S7-3 | `N:SKILL.md:37`, `:55`, `:277`; `N/r:domain-logic-placement.md:16`, `:34` | "needs `[Service]`s, or runs at load or fetch … \| `[Execute]` / `[Fetch]` on a plain `[Factory]` class or a static command" | CONTRADICTS S7 by omission: a seam said to need server services is never marked `[Remote]`. The text `[Remote, Execute]` does not occur in either skill (grep) | prose | Write the mechanism as `[Remote, Execute]` |

## CONTRADICTS S8 (read model)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S8-1 | `N/r:base-classes.md:13`, `:14`, `:22`, `:234-243`; `N/r:pitfalls.md:86` | "`ValidateBase<T>` with `[Fetch]` only \| Read Model"; "Read-only models \| `ValidateBase` with only `[Fetch]` methods" | CONTRADICTS S8. Also INTERNAL with `N:SKILL.md:56` and `:278` | prose | "Plain `[Factory]` class, `[Fetch]` only, no Neatoo base" |
| S8-2 | `N/r:base-classes.md:251-284` | "/// Employee summary read model using ValidateBase." | CONTRADICTS S8 (the same block also hits S9 and S2) | generated snippet `readonly-base-sample` (`BaseClassSamples.cs#L239-L273`) | Plain `[Factory]` class with `[Fetch]`; drop `ValidateBase` and `ValidateListBase` |
| S8-3 | `N/r:base-classes.md:245` | "Use `ValidateBase<T>` only when validation rules are needed, `IsValid`, property change notifications, or change tracking on the read model." | CONTRADICTS S8 in part (wider than "needs rules") | prose | "`ValidateBase` only when the object needs rules" |

## CONTRADICTS S9 (interface-first)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S9-1 | Public concrete with no interface. 12 classes: `N:SKILL.md:19`; `N/r:base-classes.md:38`, `:88`, `:257`; `N/r:validation.md:13`, `:36`, `:263`; `N/r:properties.md:13`, `:259`; `N/r:entities.md:467`; `N/r:source-generation.md:19`, `:91` | "`public partial class Product : EntityBase<Product>`" | CONTRADICTS S9 | generated snippets `skill-quickstart` (`QuickStartSamples.cs`); `validate-base-sample`, `edit-base-sample`, `readonly-base-sample` (`BaseClassSamples.cs`); `validation-basic`, `validation-attributes`, `validation-properties` (`ValidationSamples.cs`); `properties-partial-declaration`, `properties-read-only` (`PropertiesSamples.cs`); `entities-cascade-insert` (`EntitiesSamples.cs`); `api-generator-save-factory`, `api-attributes-suppressfactory` (`ApiReferenceSamples.cs`) | `public interface IX : IEntityRoot` (or `IEntityBase`) + `internal partial class X : …, IX` |
| S9-2 | Public concrete that does have an interface. 3 classes: `N/r:base-classes.md:178`; `N/r:collections.md:12`; `N/r:lazy-loading.md:36` | "`public class SkillEmployeeAddressList : EntityListBase<ISkillEmployeeAddress>, ISkillEmployeeAddressList`" | CONTRADICTS S9 (concrete must be `internal`) | generated snippets `editable-list-base-sample` (`BaseClassSamples.cs`), `collections-entity-list-definition` (`CollectionsSamples.cs`), `skill-lazyload-constructor-pattern` (`LazyLoadSamples.cs`) | `internal` |
| S9-3 | Concrete used where the interface belongs: list type arguments `N/r:base-classes.md:281`, `N/r:collections.md:404`; concrete list as property type `N/r:entities.md:476`; `new ConcreteList()` at `N/r:base-classes.md:93`, `N/r:entities.md:471`, `N/r:collections.md:82`, `:146`, `:190`. (Un-rendered but in the same sample: `EntitiesSamples.cs:279` `EntityListBase<EntitiesCascadeItem>`) | "`public class SkillEmployeeSummaryList : ValidateListBase<SkillEmployeeSummary>`"; "`public partial EntitiesCascadeItemList Items { get; set; }`" | CONTRADICTS S9 | generated snippets `readonly-base-sample`, `edit-base-sample`, `collections-validate-list-definition`, `collections-remove-validate`, `collections-validation`, `collections-run-rules`, `entities-cascade-insert` | List on the child interface; property typed as the list interface |
| S9-4 | `N/r:pitfalls.md:34`, `:59` | "`public class EmployeeEditRule : RuleBase<Employee>`" | CONTRADICTS S9 (soft: a public rule over a concrete entity; with an `internal` entity this is CS0060) | hand-written code | `internal class EmployeeEditRule` |

## CONTRADICTS S10 (waiting on rules)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S10-1 | `N/r:pitfalls.md:9`, `:89` | "Always `await entity.RunRules()` before checking `IsValid`"; "Check validity \| `await RunRules()` first" | CONTRADICTS S10. Also INTERNAL with `N:SKILL.md:122` and `N/r:validation.md:608` ("Always `await WaitForTasks()` before checking validation state or saving") | prose | "`await entity.WaitForTasks()` before reading `IsValid` when async rules may be in flight" |
| S10-2 | `RunRules` called after a property set, before reading validity. 5 snippets, 8 call sites: `N/r:validation.md:494-495`, `:246-247` (soft); `N/r:collections.md:152`, `:171`; `N/r:testing.md:26-28`, `:33`, `:177`, `:187` | "`// Re-run all rules before save` / `await order.RunRules(RunRulesFlag.All);`"; "`// Set invalid data and run rules to trigger validation`" | CONTRADICTS S10. `test-validation` is also INTERNAL with itself: its second test (`N/r:testing.md:205`) asserts straight after the set | generated snippets `validation-before-save`, `validation-run-rules` (`ValidationSamples.cs`); `collections-validation` (`CollectionsSamples.cs`); `test-real-vs-mock`, `test-validation` (`TestingPatternsTests.cs`) | `await x.WaitForTasks()` or nothing. (`N/r:collections.md:156`, `:205` and `N/r:blazor.md:165` are genuine forced runs and are not findings) |

## CONTRADICTS S11 (IsNew / IsModified)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S11-1 | `N/r:entities.md:298-301` | "`IsSavable` returns `true` when: … `IsModified == true` (has changes)" | CONTRADICTS S11. Also INTERNAL with `N/r:entities.md:9` and `N:SKILL.md:290` | prose | "`IsModified` **or** `IsNew`" |
| S11-2 | `M/r:anti-patterns.md:213` | "Completely bypasses `IsSavable` (which includes `IsModified && !IsBusy`)" | CONTRADICTS S11. Also INTERNAL with `M:SKILL.md:257` | prose | "(`(IsModified \|\| IsNew) && IsValid && !IsBusy`)" |

See also S3-2 (the `FactoryComplete` snippet leaves a created object modified) and the installed-copy drift at the end (`!IsChild`).

## CONTRADICTS S12 (services)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S12-1 | `N:SKILL.md:234` | "`EnsureAuditList(auditListFactory);`" | CONTRADICTS S12 (a `[Service]` passed on as an ordinary method parameter) | hand-written code | Inline the body in the factory method |
| S12-2 | `N:SKILL.md:244-248` | "`this.Price = _priceService.GetPrice(this.Sku, currency);`" | CONTRADICTS S12 / S6 (ambiguous): a stored service of unknown origin used from an entity business method. Legal only if constructor-injected and present on both tiers | hand-written code | Show the constructor injection, or call a command delegate |

## CONTRADICTS S13 (placement)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| S13-1 | `M/r:aggregate-reactive-vm.md` (whole file; key lines `:3`, `:69-78`, `:145`) | "the VM often needs computed properties derived from the aggregate's state"; "`public double ProgressPercent =>` … `(double)_plan.CompletedTreatmentCount / _plan.ApprovedTreatments * 100`" | CONTRADICTS S13. Also INTERNAL with `N:SKILL.md:69` ("Not in: ViewModel or Razor arithmetic"), `N:SKILL.md:104`, `N/r:domain-logic-placement.md:537`, and `M/r:anti-patterns.md:414-448` (Anti-Pattern 8) | prose + hand-written code | Move the derived values to entity rules; cut the file down to "the VM re-raises for values it only reads", or delete it |
| S13-2 | `M/r:aggregate-reactive-vm.md:33-41`, `:119` | "`public bool IsApproved => ApprovedById.HasValue;`"; "Map triggers to partial properties, not expression-body properties." | CONTRADICTS S13: the ViewModel has to know how the entity derives its flags. Also INTERNAL with `N:SKILL.md:54` and `:94` (`CanX` exposed by a rule) | prose + hand-written code | Make `IsApproved` / `CanExtend` rule-computed partial properties so they notify |
| S13-3 | `N/r:domain-logic-placement.md:216-219` | "`public async Task LookUpCoverageAsync()` / `=> Coverage = await _coverageLookup.Execute(Patient.InsuranceId);`" | CONTRADICTS S13 (and S6): the lookup result now lives on the ViewModel and `Copay` is no longer set on the entity | hand-written code | Folded into the S6-1 rewrite |

---

## INTERNAL (two passages disagree)

Several of these were also checked against source; the verification is in the Class column.

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| I-1 | `N/r:blazor.md:56`, `:64`, `:95-106`; `N/r:collections.md:397`; `M/r:anti-patterns.md:502` **vs** `M:SKILL.md:423`; `M/r:property-change-events.md:81-85` | "The entity implements `INotifyPropertyChanged`, so Blazor re-renders automatically when properties change" vs "Blazor does **not** auto-subscribe to `INotifyPropertyChanged`" | INTERNAL | prose | Keep the mudneatoo wording; rewrite the five "automatic" sites |
| I-2 | `M:SKILL.md:58`; `M/r:anti-patterns.md:122` **vs** `M/r:anti-patterns.md:167`; `N:SKILL.md:72`; `N/r:domain-logic-placement.md:14` | "Direct property assignment (`entity.Prop = value`) bypasses async rule pipeline — `SetValue()` is the correct async path" vs "ViewModel calls the entity setter; rules react" | INTERNAL + STALE. The generated setter is `XProperty.Value = value` (`PropertyGenerator.cs:60`), which calls `SetValue` (`ValidateProperty.cs:36-43`) and tracks the task in `RunningTasks`; nothing is bypassed, the caller just cannot await it | prose + hand-written comment | "the setter runs the same rules but returns no Task; a component should await `SetValue()`" |
| I-3 | `N:SKILL.md:310`; `M/r:aggregate-reactive-vm.md:24`; `M/r:anti-patterns.md:167`; `N/r:source-generation.md:95` **vs** `N/r:properties.md:3` | "The generator creates property implementations that call `Getter<T>()` and `Setter()` internally." vs "(The old `Getter<T>()`/`Setter()` pattern is deprecated.)" | INTERNAL + STALE. Both are `[Obsolete]` (`ValidateBase.cs:431`, `:447`); the generator emits `.Value` (`PropertyGenerator.cs:18-60`) | prose; generated snippet `api-attributes-suppressfactory` (`ApiReferenceSamples.cs`) | Say "backing `IValidateProperty<T>.Value`"; use a partial property in the snippet |
| I-4 | `N/r:domain-logic-placement.md:248-258` **vs** `N:SKILL.md:78`, `:351`; `N/r:validation.md:213`, `:216` | "`RuleManager.AddValidation(` … `t => t.EndDate,` / `t => t.StartDate);`" vs "**`AddValidation`/`AddValidationAsync` accept exactly one trigger property**" | INTERNAL + STALE. `RuleManager.cs:536-542` has a single one-trigger overload; Pattern 5 does not compile | hand-written code | Use a `RuleBase<T>` with two triggers |
| I-5 | `N:SKILL.md:79`, `:205` **vs** `N/r:domain-logic-placement.md:334` | "Override `HandleNeatooPropertyChanged` on the parent (1)"; "Sibling consistency in a list \| Root class \| Override `HandleNeatooPropertyChanged`" vs "override in EntityListBase subclass" | INTERNAL + STALE. Only `ValidateListBase.cs:350` declares it; an entity cannot override it | prose | "override on the list class" in both table rows |
| I-6 | `N/r:entities.md:535` **vs** `N/r:entities.md:659`; `N/r:pitfalls.md:21` | "`foreach (var deleted in Items.Deleted)`" vs "`foreach (var deleted in AreasList.DeletedList)`" | INTERNAL + STALE. `DeletedList` is `protected` (`EntityListBase.cs:108`); `Deleted` is a member the sample list declares at `EntitiesSamples.cs:282`, outside any rendered region | generated snippet `entities-cascade-update` (`EntitiesSamples.cs`); hand-written code; prose | Show the exposing member once (`public IReadOnlyList<I> Deleted => DeletedList;`) and use one name |
| I-7 | `N/r:properties.md:428-431` **vs** `N/r:rules-lifecycle.md:31-36`; `N/r:validation.md:382-386` | "// - All deferred events fire / // - Validation rules execute" vs "It does **NOT**: Run any rules" | INTERNAL + STALE (`ValidateBase.cs:798-806`: resume only clears the flag and resets meta state) | generated snippet `properties-suppress-events` (`PropertiesSamples.cs`) | Correct the comment in the sample |
| I-8 | `N/r:domain-logic-placement.md:164-171`, `:551-587`; `N/r:validation.md:509`, `:521`; `N/r:lazy-loading.md:53-61` **vs** `N/r:domain-logic-placement.md:204` | "Use `AddActionAsync` for async side-effects that compute or fetch values when a property changes." vs "**Do not use a rule to fetch.**" | INTERNAL (Pattern 3 and the class-based rule in the same file do what Pattern 4 forbids) | prose + hand-written code; generated snippets `async-action-rule` (`AsyncSamples.cs`), `skill-lazyload-constructor-pattern` | Resolved by the S6-1 rewrite |
| I-9 | `N/r:domain-logic-placement.md:124-131`, `:385-387` **vs** `N:SKILL.md:399`; `N/r:domain-logic-placement.md:301-314` | "`&& t.TreatmentPlan.IsApproved`" triggered by "`t => t.TreatmentPlan,`"; "`t.CanApprove = t.Status == "Pending" && t.IsValid,` / `t => t.Status`" | INTERNAL: the skill's own examples trigger on a reference (and read `IsValid` with no trigger), which the skill says never fires on child change | hand-written code | Trigger on `t => t.TreatmentPlan!.IsApproved`; drop `IsValid` from the rule or explain |
| I-10 | `M/r:aggregate-reactive-vm.md:156-157` **vs** `M:SKILL.md:214-250`, `:258`; `M/r:property-change-events.md:54` | "// WRONG: Subscribing to PropertyChanged in the Razor component" vs "**Subscribe to `PropertyChanged`** — so Blazor re-renders when `IsSavable` changes." | INTERNAL | prose + hand-written code | State when each applies (page with or without a ViewModel) |
| I-11 | `M:SKILL.md:188-252` **vs** `N:SKILL.md:58-59`; `N/r:domain-logic-placement.md:528` | "`@inject IPatientEditFactory PatientEditFactory`" (page fetches, saves, subscribes in `@code`) vs "Razor \| Binds." | INTERNAL (soft) | hand-written code | Say whether a page without a ViewModel is sanctioned |
| I-12 | `N:SKILL.md:54` **vs** `N:SKILL.md:128`, `:142` | "The verb sets state, rules validate, the caller saves." vs "A method that saves and returns its successor … is still inside the three phases." | INTERNAL; see U-7 | prose | Pick one |
| I-13 | `N:SKILL.md:157` **vs** `N/r:entities.md:499`, `:526`; `N/r:source-generation.md:61` | "Pass flags as parameters to `Save()` — Save's signature is fixed" vs "`Items[i] = await itemFactory.SaveAsync(Items[i], Id);`" | INTERNAL (entity `Save()` is fixed; a child factory `Save` takes the parent id) | prose | "the root's `Save()` signature is fixed" |
| I-14 | `M:SKILL.md:396`, `:457` **vs** `N/r:properties.md:193-249` | "read-only state is controlled entirely by the domain model's property declaration" vs "`MarkReadOnly()` is decided per-instance" | INTERNAL | prose | Add `MarkReadOnly()` to the mudneatoo text |
| I-15 | `N/r:properties.md:179` **vs** `M:SKILL.md:391-392` | "(which only tests `SetMethod?.IsPrivate`)" vs "Property with `private set` or get-only (no setter) -> `IsReadOnly = true`" | INTERNAL + STALE: `PropertyInfoWrapper.cs:12` is `!CanWrite \|\| SetMethod?.IsPrivate == true`; `properties.md` is the wrong one | prose | Correct `properties.md:179` |
| I-16 | `N/r:source-generation.md:71` **vs** `:78` | "`protected partial string Data { get; protected set; }` \| … \| `string Data { get; set; }`" vs "Non-public setters generate `get;` only on the interface declaration." | INTERNAL + STALE: `PropertyGenerator.cs:82` emits `get;` for any non-public setter. The declaration in the row is also not legal C# (the accessor modifier is not more restrictive than the property) | prose | `public partial string Data { get; protected set; }` and interface `{ get; }` |
| I-17 | `N/r:properties.md:253-275`, `:277-299` **vs** `N/r:properties.md:144-162`, `:245`; `N:SKILL.md:69`; `N/r:pitfalls.md:13`; `M/r:aggregate-reactive-vm.md:33` | "For calculated or read-only properties, declare the partial property with only a getter" and a plain non-partial getter under "while still using the backing field" vs "Private-set properties are set from within the entity, typically via `AddAction` rules" | INTERNAL: three recipes for a computed value; the non-partial getter neither notifies nor serializes, which the other passages warn about | prose; generated snippets `properties-read-only`, `properties-custom-getter` (`PropertiesSamples.cs`) | Name `private set` + `AddAction` as the recipe; caveat or remove the other two |
| I-18 | `N/r:properties.md:200` **vs** `:216` | "`internal void Fetch(int id, bool canEditSalary, [Service] IEmployeeRepository repo)`" vs "(server decides during Fetch, client respects)" | INTERNAL: the caller supplies the permission flag | hand-written code | Resolve the permission from a `[Service]` on the server |
| I-19 | `M:SKILL.md:94` **vs** `M/r:anti-patterns.md:399` | "All MudBlazor parameters pass through — `Variant`, `Margin`, … `Min`, `Max`, etc." vs "If a pass-through parameter is missing (like `Mask`)" | INTERNAL + STALE: each component declares a fixed set (`MudNeatooTextField.razor.cs` has no `Min`, `Max`, `Mask` or `Label`) | prose | "A curated set of parameters is forwarded" |
| I-20 | `M:SKILL.md:31`; `N/r:blazor.md:259` **vs** `N/r:blazor.md:68`, `:81`; `N/r:base-classes.md:26-28` | "Every MudNeatoo component takes an `EntityProperty` parameter — the `IEntityProperty` object" vs "backed by its own `IValidateProperty` object … MudNeatoo components … handle Mode 2 internally" | INTERNAL, source-verified: every component declares `public IEntityProperty EntityProperty`; a `ValidateBase` property is `ValidateProperty<T>` (`PropertyFactory.cs:12-15`), which is not an `IEntityProperty`. A `ValidateBase` object ("form data", `base-classes.md:26`) cannot be bound through MudNeatoo components and neither skill says so | prose | State the limitation in both skills |
| I-21 | `N/r:collections.md:377-381`; `N/r:pitfalls.md:17`, `:94`; `N/r:collections.md:372` **vs** the framework's own message | "Cross-aggregate transfer \| Remove → Save → Re-fetch → Add to new aggregate"; "`// THROWS: "item belongs to aggregate 'Order'"`" | INTERNAL + STALE: `EntityListBase.cs:246-255` says "…create a new child in the target aggregate and remove the original instead", and the quoted text is not the real message. A client cannot "re-fetch" a child when child `[Fetch]` is internal (S1/S2) | prose; generated snippet `skill-coll-cross-aggregate-error` (`CollectionSamples.cs`) | "Remove from the source; create a new child in the target" |
| I-22 | Heading says one thing, snippet shows another. 4 sites: `N/r:validation.md:7-24`, `:63-79`, `:255-283`; `N/r:entities.md:340-363` | "Add validation rules in the constructor using RuleManager:" (class has no rules); "Create reusable rule classes:" (inline lambda); "Inject services into factory methods:" (asserts `customer.Factory`) | INTERNAL | prose over generated snippets `validation-basic`, `validation-custom-rule`, `validation-properties`, `entities-factory-services` | Re-point each heading or swap the snippet |
| I-23 | `N:SKILL.md:282` **vs** `N/r:properties.md:371`, `:431`, `:443` | "**There is no `IsDirty` in Neatoo.**" vs "without triggering validation or marking dirty" | INTERNAL (terminology) | prose; generated comment | "modified" |
| I-24 | `N/r:domain-logic-placement.md:595` **vs** every testing snippet, e.g. `N/r:testing.md:16` | "`[TestMethod]`" vs "`[Fact]`" | INTERNAL (low) | hand-written code | Pick one |
| I-25 | `N/r:blazor.md:61`; `M:SKILL.md:428-430` **vs** `N/r:domain-logic-placement.md:525` | "`<MudText>@(order.IsModified ? "Unsaved changes" : "Saved")</MudText>`" vs "`@(condition ? "Label A" : "Label B")` ternary \| Domain `string` property (1)" | INTERNAL (low) | hand-written code | Remove the ternary or mark it presentational |
| I-26 | `N/r:shared-rules.md:132-133` | "// For rules specific to one entity type, inject the service and new the rule: / // RuleManager.AddRule(new HireDateRule()); // no DI needed" | INTERNAL (the comment contradicts itself) | generated snippet `shared-rule-entity-specific` (`SharedRulesSamples.cs`) | Fix the comment |
| I-27 | `N:SKILL.md:225-238` **vs** `N:SKILL.md:129`, `:139-140` | "`PendingAuditRecordsEntity!.Add(auditFactory.CreateApproved(...));`" inside `[Update]` vs "A persisting factory method … never calls a business method or reaches back into phase 1." | INTERNAL | hand-written code | Queue the record in phase 1, or carve out the exception in the text |
| I-28 | `N:SKILL.md:275` **vs** `N/r:base-classes.md:14` | "Validate Collection \| `ValidateListBase<I>` \| List of value objects" vs "`ValidateListBase<I>` \| Collection of Read Models" | INTERNAL (and S8) | prose | "List of value objects" |

## STALE (does not match current source)

| ID | Site(s) | Quote | Class | Kind | Minimal fix |
|---|---|---|---|---|---|
| ST-1 | `N/r:entities.md:28-29` | "`<!-- snippet: entities-lifecycle-new -->` / `<!-- endSnippet -->`" (empty) | STALE: the region exists (`src\samples\EntitiesSamples.cs:403-421`) and is rendered in `docs\guides\entities.md:171`; the skill placeholder was never filled | generated snippet `entities-lifecycle-new` (`EntitiesSamples.cs`) | Re-run `dotnet mdsnippets` |
| ST-2 | `N:SKILL.md:211` | "a `Parent` reference populated automatically by the Neatoo source generator" | STALE: set at runtime (`ValidateBase.cs:403-406` via `ISetParent.SetParent`) | prose | "set by the property system at runtime" |
| ST-3 | `N:SKILL.md:253` | "(effectively `object?` / `IBase?` at the use site)" | STALE: `public IValidateBase? Parent` (`ValidateBase.cs:160`); `IBase` has 0 matches in `src\Neatoo` | prose | "`IValidateBase?`" |
| ST-4 | `N/r:validation.md:285` | "## BrokenRules Collection" | STALE: `BrokenRules` has 0 matches in `src\Neatoo`; the member is `PropertyMessages` | prose | "## PropertyMessages" |
| ST-5 | `N/r:entities.md:172` | "it resumes paused actions … and subscribes lazy-load properties" | STALE: `FactoryComplete` only resumes and marks state (`ValidateBase.cs:986-989`, `EntityBase.cs:594-625`); `SubscribeToLazyLoadProperties` has 0 matches. Also INTERNAL with `N/r:lazy-loading.md:128` | prose | Delete the lazy-load clause |
| ST-6 | `N/r:properties.md:516` | "Neatoo tracks why a property changed (user edit, rule, load):" | STALE: `ChangeReason` has only `UserEdit` and `Load` (`ChangeReason.cs:19-31`) | prose | "(user edit or load)" |
| ST-7 | `N/r:source-generation.md:7` | "`IEntityProperty<T>` for `EntityBase` subclasses. `IEntityProperty<T>` adds … `LoadValue()`" | STALE: the generated accessor is always `IValidateProperty<T>` (`PropertyGenerator.cs:18`); `LoadValue` is on `IValidateProperty` (`IValidateProperty.cs:80`) | prose | Correct both statements |
| ST-8 | `N/r:blazor.md:267-312` | "Isolate EF Core in a separate infrastructure project and use `PrivateAssets="all"` on the project reference. See the Person example" | STALE: `Person.DomainModel.csproj:21` references `Person.Dal` with no `PrivateAssets` and never references `Person.Ef`; project names differ. Also a repo-file pointer, which `.claude\rules\skills-neatoo.md` forbids | prose | Describe the Dal-interface / Ef-implementation split the example really uses, or drop the pointer |
| ST-9 | `N/r:properties.md:553` | "Authoritative sample: `src/Design/Design.Domain/PropertySystem/CustomPropertyType.cs`." | STALE against the skill rule "No 'see link' file references to repository source files" (the file itself exists) | prose | Embed the code or drop the pointer |
| ST-10 | `N/r:entities.md:174` | "`FactoryOperation` values: `Create`, `Fetch`, `Insert`, `Update`, `Delete`." | STALE (incomplete): also `None`, `Execute` (`RemoteFactory\src\RemoteFactory\FactoryOperation.cs:8-16`) | prose | Add the two values, or say "the persistence values" |
| ST-11 | `N/r:source-generation.md:103` | "Generated code is output to `Generated/Neatoo.BaseGenerator/` within each project. This folder is excluded from git." | STALE (conditional): only when the project sets `EmitCompilerGeneratedFiles` and `CompilerGeneratedFilesOutputPath` (`Samples.csproj:13-14`) | prose | "when the project opts in with…" |
| ST-12 | `N/r:domain-logic-placement.md:319`, `:327` | "**Event args inspection** — `ChangeReason`, `Source`, `FullPropertyName`"; "`if (args.OriginalEventArgs.Reason == ChangeReason.UserEdit)`" | STALE (low confidence): an entity does not raise `NeatooPropertyChanged` for `Load` or while paused (`ValidateBase.cs:385-396`), so the check is always true. List path not traced | prose + hand-written code | Drop `ChangeReason` from the reasons |
| ST-13 | `M/r:property-change-events.md:15-17` | "`IsModified`, `IsSelfModified` (from `EntityListBase`; `EntityBase` tracks its children the same way)" | STALE (incomplete): `EntityBase.cs:273-276` also raises `IsSavable` and `IsDeleted`; line `:56` relies on `IsSavable` | prose | List all four |
| ST-14 | `N/r:entities.md:733` | "**No `[Remote]` on internal methods.** `[Remote]` + `internal` triggers NF0105 diagnostic error." | STALE (reversed): `RemoteFactory\src\Generator\DiagnosticDescriptors.cs:50` "NF0105: [Remote] cannot be used with public methods"; release notes v0.21.0 "NF0105 flipped". Also INTERNAL with `N/r:lazy-loading.md:73-75` and `N/r:properties.md:198-200`, which show `[Remote]` `[Fetch]` `internal` | prose | "`[Remote]` requires `internal`; child methods are `internal` without `[Remote]`" |
| ST-15 | `N/r:entities.md:729` | "Entities that can serve as both aggregate roots and children (entity duality pattern) must keep all factory methods `public` with `[Remote]`" | STALE (`[Remote] public` is now the NF0105 error). The topic itself is not ruled; see the UNRULED cross-reference | prose | Remove until the author rules on dual-use entities |
| ST-16 | `N/r:base-classes.md:210` | "// [Execute] generates a delegate: SendEmailCommand.SendEmail" | STALE (low): the class is `SkillSendEmailCommand` | generated snippet `command-base-sample` (`BaseClassSamples.cs`) | Fix the comment |
| ST-17 | `M:SKILL.md:98` | "forwards a `UserAttributes` (`Dictionary<string, object>?`) parameter" | STALE (trivial): non-nullable, defaults to `new()` (`MudNeatooTextField.razor.cs:105`) | prose | Drop the `?` |

## UNRULED (normative claim the settled list does not decide)

Quotes are verbatim.

| ID | Site(s) | Quote | Why a reader could be misled | Kind | Minimal fix |
|---|---|---|---|---|---|
| U-1 | `N/r:rules-lifecycle.md:156` (block `:154-171`); also `N/r:validation.md:359`, `N/r:properties.md:402` | "`PauseAllActions()` returns `IDisposable`. Use for batch updates where intermediate rule execution is unnecessary:" | S3 only covers factory operations. In source, a paused setter skips modification tracking (`EntityPropertyManager.cs:43`), so a consumer's "batch update" under pause leaves a fetched entity unmodified and unsavable | prose + hand-written code; generated snippets `validation-pause-actions` (`ValidationSamples.cs`), `properties-suppress-events` (`PropertiesSamples.cs`) | Rule on whether consumers may pause at all; if not, remove all three sections |
| U-2 | `N/r:entities.md:580` | "**Reassign after save** — `factory.SaveAsync()` returns a new instance, so always reassign: `Items[i] = await itemFactory.SaveAsync(Items[i])`" | True across the wire. Inside a server-side cascade the call is local; whether it returns a new instance there was not verified | prose; generated snippets `entities-cascade-insert`, `entities-cascade-update` | Rule, then state when a new instance comes back |
| U-3 | `N/r:lazy-loading.md:96` (loader at `:48-51`) | "loader executes with correct `this.Id` → child loaded via `[Remote]` call." | A lazily loaded child needs a child `[Fetch]` the client can call; S1 says child operations are never `[Remote]` | prose; generated snippet `skill-lazyload-constructor-pattern` (child at `LazyLoadSamples.cs:37-39`) | Rule on lazy children |
| U-4 | `N:SKILL.md:166`, `:179` | "Neatoo rejects that encapsulation boundary. The aggregate is a graph whose nodes are all directly addressable by any consumer:"; "**The root is a coordinator, not a gate.**" | A large design stance with no ruling. Compatible with S4 as written | prose | Confirm or soften |
| U-5 | `N:SKILL.md:130` | "An orchestration seam — an `[Execute]`, or a `[Fetch]` that shapes the graph it returns — is not a persisting factory method, and the line above does not bind it." | Lets a `[Fetch]` hand back a graph carrying staged, unsaved changes; S2 says Fetch gets existing data, S3 says assignments in a factory operation are baseline | prose | Rule on whether a `[Fetch]` may return a modified graph |
| U-6 | `N:SKILL.md:128` | "A method that saves and returns its successor, or hands a validated value to a command, is still inside the three phases." | Lets an entity business method call `Save()` or an `[Execute]`; rung 2 (`:54`) says "the caller saves" (I-12) | prose | Rule |
| U-7 | `N/r:base-classes.md:93`; `N/r:entities.md:471` **vs** `N/r:lazy-loading.md:114-119` | "`AddressesProperty.LoadValue(new SkillEmployeeAddressList());`" vs "`public void Create([Service] IPersonPhoneList emptyPhoneList)`" | Two ways to initialise a child list; the first news up a concrete list in the constructor (S9) | generated snippets `edit-base-sample`, `entities-cascade-insert`; hand-written code | Rule on the one way |
| U-8 | `N:SKILL.md:120`; `M:SKILL.md:316` **vs** `N/r:entities.md:681` | "Adding items to child collections (e.g., `order.Items.AddItem()`, `plan.PendingAuditRecordsEntity.Add(...)`) is also phase 1" vs "Client code calls `itemFactory.Create()` before `order.Items.Add(item)`" | Two ways to create a child; tied to S1-3 | prose + hand-written code | Rule |
| U-9 | `M/r:anti-patterns.md:454`; also `M:SKILL.md:275`, `N/r:lazy-loading.md:147`, `:212` | "Awaiting `LoadAsync()` with `await` in `OnInitializedAsync()`, which blocks rendering until data arrives. Use fire-and-forget instead." | A discarded task hides a load failure unless the page binds `HasLoadError`; S5 calls an exception an application failure | prose + hand-written code | Rule; if kept, require the error branch |
| U-10 | `N/r:lazy-loading.md:241` | "**Eager loading in the parent's `[Fetch]` method is preferred** for most cases — it keeps data access visible and avoids N+1 query problems." | A preference with no ruling (low) | prose | Confirm |

Cross-references already counted elsewhere: ST-15 (dual-use entities: "must keep all factory methods `public` with `[Remote]`", `N/r:entities.md:729`), S1-3 (child `[Create]` public), S3-2 (whether `FactoryComplete` is a sanctioned place for logic), I-18 (field-level authorization through `MarkReadOnly()` in `[Fetch]`).

---

## Omissions (a settled point is never stated; not counted above)

| ID | Point | Evidence | Minimal fix |
|---|---|---|---|
| O-1 | S9 | The interface-first rule appears in neither skill. Grep for `IEntityRoot`, `interface-first`, `matched public interface`, `IEntityListBase<` across `skills\` returns two hits, neither a rule: `N:SKILL.md:290` and a code comment at `N/r:entities.md:333`. `N/r:base-classes.md:288-292` ("Inheritance Guidelines") omits it | Copy the "Central Pillar: Interface-First Design" block from the repo `CLAUDE.md` into `N:SKILL.md` and `N/r:base-classes.md` |
| O-2 | S2 | Nothing says Create is `new`, Fetch takes a key, or that create-or-load is a static `[Execute]`. The nearest text is `N/r:entities.md:682` | Add a short block to `N/r:entities.md` |
| O-3 | S6 | No example of a rule that takes an injected `[Execute]` delegate. `M:SKILL.md:42` ("Calls `SetValue()` on change") does not say the text and numeric fields commit on blur | Add the example; document `Immediate="false"` |
| O-4 | S7 | `[Remote, Execute]` occurs nowhere in either skill (grep) | Covered by S7-1 to S7-3 |
| O-5 | S12 | Only `N/r:pitfalls.md:14` touches it; "never passed as an ordinary method parameter" is not stated | One sentence in `N:SKILL.md` under the factory-method boundary table |
| O-6 | S1 | The neatoo skill never defines `[Remote]` as "client entry point, not 'runs on the server'"; it defers to the RemoteFactory skill (`N:SKILL.md:314`) | One sentence next to `N:SKILL.md:314` |

## Other observation (no class)

`N/r:source-generation.md:27-28`, generated snippet `api-generator-save-factory` (`ApiReferenceSamples.cs`): "`public void DoMarkNew() => MarkNew();` / `public void DoMarkOld() => MarkOld();`". Test scaffolding that exposes protected routing state publicly is rendered into a skill example. Move the two lines outside the region.

---

## Repo copy vs installed copy

Installed at `C:\Users\KeithVoels\.claude\skills\neatoo\` and `...\mudneatoo\`. Same file set on both sides (15 + 4).

| File | Difference |
|---|---|
| `mudneatoo\SKILL.md` | **Content differs.** The installed copy lacks the "`MudNeatooTextField` escape hatch: `UserAttributes`" and "Multi-line text" sections (repo lines 96-126), and its line 226 still reads "`IsValid && (IsModified \|\| IsNew) && !IsBusy && !IsChild`" (`IsChild` has 0 matches in `src\Neatoo`). Line endings also differ (repo CRLF, installed LF) |
| `mudneatoo\references\aggregate-reactive-vm.md`, `anti-patterns.md`, `property-change-events.md` | Line endings only (repo LF, installed CRLF) |
| `neatoo\references\base-classes.md`, `blazor.md`, `lazy-loading.md`, `pitfalls.md`, `rules-lifecycle.md`, `shared-rules.md`, `source-generation.md`, `testing.md`, `trimming.md` | Line endings only (repo LF, installed CRLF) |
| `neatoo\SKILL.md`, `neatoo\references\collections.md`, `domain-logic-placement.md`, `entities.md`, `properties.md`, `validation.md` | Byte-identical |

## Files read

Read whole, top to bottom (19 of 19): `neatoo\SKILL.md`; `neatoo\references\` `base-classes.md`, `blazor.md`, `collections.md`, `domain-logic-placement.md`, `entities.md`, `lazy-loading.md`, `pitfalls.md`, `properties.md`, `rules-lifecycle.md`, `shared-rules.md`, `source-generation.md`, `testing.md`, `trimming.md`, `validation.md`; `mudneatoo\SKILL.md`; `mudneatoo\references\` `aggregate-reactive-vm.md`, `anti-patterns.md`, `property-change-events.md`. None left unfinished.

Installed copies were compared byte-for-byte and with line endings normalised; they were not separately read.

Source files read whole for verification: `EntityBase.cs`, `ValidateBase.cs`, `IValidateProperty.cs`, `IEntityProperty.cs`, `Internal\ValidateProperty.cs`, `Internal\PropertyInfoWrapper.cs`, `Internal\DefaultPropertyFactory.cs`, `Rules\RunRulesFlag.cs`, `Rules\TriggerProperty.cs`, `ChangeReason.cs`, `NeatooPropertyChangedEventArgs.cs`, `EntityLazyLoad.cs`, `Neatoo.BaseGenerator\Generators\PropertyGenerator.cs`, `MudNeatooTextField.razor` and `.razor.cs`, `Directory.Packages.props`. Read in part or by targeted grep only: `Rules\RuleManager.cs` (lines 395-619), `EntityListBase.cs` (lines 1-130, 205-354), `Internal\EntityPropertyManager.cs` (lines 1-285), `Rules\RuleBase.cs`, `Rules\RuleMessage.cs`, `ValidateListBase.cs`, the other MudNeatoo components (parameter lists), the Person `.csproj` files, and the RemoteFactory files named above. Absence claims ("0 matches") come from a grep for the exact identifier across `src\Neatoo\*.cs`.

`trimming.md`: both tables were checked row by row against source (14 of 14 annotated type parameters, 11 of 11 suppression sites with their warning codes) and match. Not verified: the byte-size figures at `:100-105` and the statement about RemoteFactory's generated `FactoryServiceRegistrar` at `:61`.

Not verified anywhere in this audit: claims about RemoteFactory's generated factory interface visibility (`N/r:entities.md:691-692`), "Async action rules execute in registration order" (`N/r:validation.md:637`), and the MudBlazor 9 rename (`M:SKILL.md:461`).
