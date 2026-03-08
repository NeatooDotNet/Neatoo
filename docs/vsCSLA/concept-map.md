# CSLA → Neatoo Concept Map

Side-by-side mapping of every major CSLA concept to its Neatoo equivalent. Status indicates whether the Neatoo skill documents the concept.

**Important:** Neatoo + RemoteFactory together cover what CSLA does as one framework. Both skills should be assumed loaded together.

## Base Classes & Stereotypes

| CSLA | Neatoo | Notes | Skill Status |
|------|--------|-------|:---:|
| `BusinessBase<T>` | `EntityBase<T>` | Persistent entity with CRUD lifecycle | Documented |
| `ReadOnlyBase<T>` | Standard class with `[Factory]` + `[Fetch]` via RemoteFactory | No base class needed — just a POCO with factory methods | **Needs correction** |
| `BusinessListBase<T,C>` | `EntityListBase<I>` | Parameterized on interface, not concrete | Documented |
| `ReadOnlyListBase<T,C>` | `ValidateListBase<I>` | No deletion tracking | Documented |
| `CommandBase<T>` | Static class with `[Factory]` + `[Execute]` | No base class needed | Documented |
| `DynamicListBase<T>` | *Does not exist* | No self-saving lists; children save through aggregate cascade | **Gap** |
| `CriteriaBase` | `ValidateBase<T>` when validation needed; method parameters or POCO otherwise | CriteriaBase existed because serialization wasn't out-of-the-box in early .NET. Use `ValidateBase<T>` when you need rules and `IsValid` on the criteria object. | **Needs correction** |
| EditableRoot / EditableChild stereotypes | `IEntityRoot` / `IEntityBase` interfaces | Interface-first design replaces stereotypes | Documented |

## Data Portal / Factory

Neatoo's equivalent of CSLA's DataPortal is split into the **RemoteFactory** — a separate skill/library. Method signatures are the contract (no reflection runtime link-up).

| CSLA | Neatoo | Notes | Skill Status |
|------|--------|-------|:---:|
| `DataPortal.Create<T>()` | `factory.Create()` | Generated from `[Create]` method | Documented (RemoteFactory skill) |
| `DataPortal.Fetch<T>()` | `factory.Fetch()` / `factory.FetchAsync()` | Generated from `[Fetch]` method | Documented (RemoteFactory skill) |
| `DataPortal.Update()` / `Save()` | `entity.Save()` / `factory.SaveAsync()` | Routes to `[Insert]`/`[Update]`/`[Delete]` based on state | Documented |
| `DataPortal.Delete<T>()` | `entity.Delete()` then `Save()` | Deferred deletion pattern | Documented |
| `DataPortal.CreateChild<T>()` | `childFactory.Create()` | No separate child factory — same factory, framework sets `IsChild` on list add | Documented |
| `DataPortal_Create` | `[Create] public void Create()` | Method attribute instead of convention | Documented |
| `DataPortal_Fetch` | `[Fetch] public void Fetch(...)` | Method attribute | Documented |
| `DataPortal_Insert` | `[Insert] public void Insert(...)` | Method attribute. Parent/child relationship is explicit in method parameters (e.g., child `[Insert]` takes parent ID). More explicit than CSLA. | Documented |
| `DataPortal_Update` | `[Update] public void Update(...)` | Method attribute | Documented |
| `DataPortal_Delete` | `[Delete] public void Delete(...)` | Method attribute | Documented |
| `[RunLocal]` | No `[Remote]` attribute (default is local) | Inverse: Neatoo methods are local unless marked `[Remote]` | Documented (RemoteFactory skill) |
| `[Transactional]` | *Not needed* | Scoped DI handles transaction boundaries naturally | Not applicable |
| DataPortal channels (HTTP, gRPC, local) | `NeatooFactory` enum: `Server`, `Remote`, `Logical` | Configured at DI registration | **Gap** |
| `ApplicationContext` / `ClientContext` | Correlation context (RemoteFactory) | Some similarities but not a direct equivalent | Documented (RemoteFactory skill) |

## Properties: Object-Per-Property Architecture

This is a **major architectural difference** from CSLA. CSLA keeps multiple property bags correlated through the base class. Neatoo has an `IValidateProperty<T>` / `IEntityProperty` object per property, accessed via `entity["PropName"]`. This enables direct binding to property-level metadata:

- `entity["Name"].IsValid`
- `entity["Name"].PropertyMessages`
- `entity["Name"].IsBusy`
- `entity["Name"].IsReadOnly`
- `entity["Name"].IsModified`

| CSLA | Neatoo | Notes | Skill Status |
|------|--------|-------|:---:|
| `RegisterProperty<T>()` | `partial` property declaration | Source generator handles registration | Documented |
| `GetProperty()` / `SetProperty()` | Generated `Getter<T>()` / `Setter()` | Implicit via partial property | Documented |
| `LoadProperty()` | `this["PropName"].LoadValue(value)` | No rules, no dirty marking | Documented |
| `ReadProperty()` | No equivalent | No per-property authorization bypass needed | N/A |
| `PropertyInfo<T>` | `IValidateProperty<T>` via `entity["PropName"]` | Full object-per-property with metadata | **Undersold** |
| `CanReadProperty()` / `CanWriteProperty()` | *Does not exist* | No per-property role-based authorization | **Gap** |
| `IsReadOnly` (per property) | `IValidateProperty.IsReadOnly` | Structural (from private setter), not dynamic role-based | Partially documented |
| Private backing fields | `IValidateProperty<T>` backing field (generated) | Named `{Name}Property` | Documented |

## Business Rules & Validation

CSLA rules are created in a static method (`AddBusinessRules`), requiring service locator for DI. Neatoo rules are **DI-first, async-first**:

- **Class-based rules:** Inherit from `RuleBase<T>` / `AsyncRuleBase<T>`, constructor-inject DI services, register via `RuleManager.AddRule(injectedRule)`
- **Inline rules:** `AddAction` / `AddValidation` fluent API for simple cases
- **No ceremony:** Rules operate directly on target `T` — no `IRuleContext`, no `LoadProperty`/`SetProperty`
- **Async by default:** `AddActionAsync` / `AddValidationAsync` are first-class

| CSLA | Neatoo | Notes | Skill Status |
|------|--------|-------|:---:|
| `BusinessRules.AddRule()` | `RuleManager.AddAction()` / `AddValidation()` / `AddRule()` | Fluent for inline, `AddRule` for DI-injected class-based rules | **Undersold** |
| `IBusinessRule.Execute(IRuleContext)` | `RuleBase<T>.Execute(T target)` | Strongly typed, operates on target directly, returns `IRuleMessages`. No `IRuleContext` ceremony. | **Undersold** |
| `CommonRules` (Required, MinLength, etc.) | `[Required]`, `[Range]`, etc. DataAnnotation attributes | Standard .NET attributes | Documented |
| Rule priority numbers | *Does not exist* | Rules execute in registration order | **Gap** |
| Rule short-circuiting | *Does not exist* | No short-circuit mechanism. Use `ElseIf` fluent builder for conditional logic. | **Gap** |
| Rule severity (Error/Warning/Information) | *Does not exist* | All messages are errors. No Warning/Information levels. | **Gap** |
| `AffectedProperties` | Properties set inside rule body trigger their own rules | Implicit via property change cascading | Implicit |
| `InputProperties` | Trigger property expressions | `t => t.Quantity`, `t => t.Items![0].LineTotal` | Documented |
| Async rules (`isAsync = true`) | `AddActionAsync()` / `AddValidationAsync()` | First-class async support, async by default | Documented |
| Per-object rules (no trigger) | *Not possible with inline API* | All rules require trigger properties. `RunningTasks.OnFullSequenceComplete` can be used for post-all-rules logic. `RunRules(RunRulesFlag.All)` for full revalidation. | **Gap** |
| `DataAnnotation` attributes | Same — `[Required]`, `[Range]`, `[EmailAddress]`, etc. | Source generator wires them into rule system | Documented |
| Class-based rules with DI | `AsyncRuleBase<T>` / `RuleBase<T>` with constructor injection | Register via `RuleManager.AddRule()`. Full DI access — CSLA required service locator. | **Undersold** |
| Static `AddBusinessRules()` | Constructor-based rule registration | Rules registered in entity constructor with full DI. CSLA's static method prevented DI. | **Undersold** |

## Authorization

Authorization is handled by **RemoteFactory**, not Neatoo core. The RemoteFactory skill has rich authorization documentation.

| CSLA | Neatoo | Notes | Skill Status |
|------|--------|-------|:---:|
| `AddObjectAuthorizationRules()` | `[AuthorizeFactory<T>]` attribute | Factory-level authorization with `CanCreate`/`CanRead`/`CanWrite` | Documented (RemoteFactory skill) |
| `CanCreateObject()` / `CanFetchObject()` etc. | `[AuthorizeFactory<T>]` + `[AspAuthorize]` with ASP.NET policies | Rich authorization at factory method level | Documented (RemoteFactory skill) |
| `CanReadProperty()` / `CanWriteProperty()` | *Does not exist* | No per-property role-based authorization | **Gap** |
| `IsReadOnly` (per property) | `IValidateProperty.IsReadOnly` | Structural (private setter), not dynamic role-based | Partially documented |

## N-Level Undo

| CSLA | Neatoo | Notes | Skill Status |
|------|--------|-------|:---:|
| `BeginEdit()` | *Does not exist* | No undo capability | **Gap** |
| `CancelEdit()` | *Does not exist* | Re-fetch to discard changes | **Gap** |
| `ApplyEdit()` | *Does not exist* | Not needed without undo | **Gap** |
| `EditLevel` tracking | *Does not exist* | — | **Gap** |

## State Properties

| CSLA | Neatoo | Notes | Skill Status |
|------|--------|-------|:---:|
| `IsDirty` | `IsModified` | **Different name** — skill explicitly warns about this | Documented |
| `IsSelfDirty` | `IsSelfModified` | Same rename | Documented |
| `IsValid` | `IsValid` | Same | Documented |
| `IsSelfValid` | `IsSelfValid` | Same | Documented |
| `IsSavable` | `IsSavable` | Same formula, only on `IEntityRoot` | Documented |
| `IsNew` | `IsNew` | Same | Documented |
| `IsDeleted` | `IsDeleted` | Same | Documented |
| `IsChild` | `IsChild` | Same | Documented |
| `IsBusy` | `IsBusy` | Same | Documented |
| `MarkNew()` | `MarkNew()` (protected) | Exposed via wrapper in tests | Partially documented |
| `MarkOld()` | `MarkOld()` (protected) | Exposed via wrapper in tests | Partially documented |
| `MarkDirty()` | `MarkModified()` / `DoMarkModified()` | Name change | Documented |
| `MarkClean()` | `MarkUnmodified()` / `DoMarkUnmodified()` | Name change | Documented |

## Serialization & Client-Server Transfer

| CSLA | Neatoo | Notes | Skill Status |
|------|--------|-------|:---:|
| `MobileFormatter` | System.Text.Json with custom converters | `NeatooBaseJsonTypeConverter` | Not documented (internal) |
| `[NonSerialized]` | No easy way to exclude partial properties | Non-partial properties are silently lost — this should be fixed | **Gap** |
| `OnDeserialized` callback | `OnDeserialized()` virtual method | Implements `IJsonOnDeserialized` | **Gap** |
| Partial properties and serialization | All partial properties serialize automatically | Non-partial properties are silently lost | Documented (pitfalls) |
| GraphMerger after save | *Does not exist and not wanted* | Save returns new instance. Reassign reference: `entity = await factory.SaveAsync(entity)` | Documented |

## Parent-Child Relationships

| CSLA | Neatoo | Notes | Skill Status |
|------|--------|-------|:---:|
| `Parent` property | `Parent` property | Same | Documented |
| `SetParent()` | Automatic on collection `Add()` | Framework handles it | Documented |
| `ChildChanged` event | `NeatooPropertyChanged` + child property triggers | More powerful: expression-based triggers | Documented |
| `FieldManager.UpdateChildren()` | Manual cascade in `[Insert]`/`[Update]` | Each parent calls `childFactory.SaveAsync()` on its children | Documented |
| `DeletedList` | `DeletedList` (on `EntityListBase`) | Same concept | Documented |
| Cross-aggregate transfer | Remove → Save → Re-fetch → Add | Same restriction, well documented | Documented |

## Events & Notifications

| CSLA | Neatoo | Notes | Skill Status |
|------|--------|-------|:---:|
| `PropertyChanged` (INotifyPropertyChanged) | `PropertyChanged` | Same. Each `IValidateProperty` also fires `PropertyChanged` for its metadata. | Documented |
| `ChildChanged` | `NeatooPropertyChanged` with child triggers | Expression-based child property triggers | Documented |
| `BusyChanged` | *No dedicated event* | `IsBusy` updates via `PropertyChanged` | **Gap** |
| `INotifyCollectionChanged` | Implemented on `ValidateListBase` / `EntityListBase` | Lists fire collection changed events | **Gap** (undocumented) |
| `Saved` event | *No equivalent* | Async/await handles post-save continuation naturally. No need for an event. | N/A |

## Testing

| CSLA | Neatoo | Notes | Skill Status |
|------|--------|-------|:---:|
| Mock DataPortal | `NeatooFactory.Logical` — no mocking needed | All operations run locally in tests | Documented |
| `DataPortal.ProxyFactory` | DI registration with `NeatooFactory.Logical` | Simpler — just a DI enum | Documented |
| Test with real objects | **Core principle** — never mock Neatoo | Extensively documented | Documented |

## Blazor / UI Integration

Two binding modes exist — this is the **#1 skill gap** causing Claude to write event handlers instead of using the framework:

| Binding Mode | Syntax | Use When | UI Updates |
|-------------|--------|----------|------------|
| Display value | `@entity.Total` | Showing values, `@bind` | Entity implements `INotifyPropertyChanged`; cascading rules update properties which fire `PropertyChanged` |
| Property metadata | `entity["Total"]` | Need `.IsValid`, `.PropertyMessages`, `.IsBusy`, `.IsReadOnly` | Each `IValidateProperty` fires its own `PropertyChanged` for metadata changes |

| CSLA | Neatoo | Notes | Skill Status |
|------|--------|-------|:---:|
| `CslaValidationMessages` | `NeatooValidationSummary` / `NeatooValidationMessage` | MudBlazor-specific components | Documented |
| `EditContext` integration | MudNeatoo components bind to `IEntityProperty` | Bypass `EditForm`/`EditContext` entirely | **Gap** |
| Re-rendering on property change | MudNeatoo subscribes to `IEntityProperty.PropertyChanged`, calls `StateHasChanged` | Automatic for MudNeatoo components; manual for custom components | **Gap** |
| Post-save UI update | Replace entity reference, call `StateHasChanged` | `Save()` returns new instance — old references are stale | **Gap** |

## Configuration & DI

| CSLA | Neatoo | Notes | Skill Status |
|------|--------|-------|:---:|
| `services.AddCsla()` | `services.AddNeatooServices(NeatooFactory.X, assemblies)` | Enum selects mode | Partially documented |
| Channel configuration | `NeatooFactory` enum: `Server` / `Remote` / `Logical` | Three modes | **Gap** |
| Multi-assembly scanning | `params Assembly[]` parameter | Pass multiple assemblies | **Gap** |

## Features CSLA Has That Neatoo Does Not

| CSLA Feature | Neatoo Status | Workaround |
|-------------|--------------|------------|
| N-Level Undo | Not implemented | Re-fetch to discard changes |
| Per-property authorization | Not implemented | Use factory-level `[AuthorizeFactory<T>]`; use `IsReadOnly` for structural read-only |
| Rule severity (Warning/Info) | Not implemented | All messages are errors |
| Rule priority | Not implemented | Control via registration order |
| Rule short-circuiting | Not implemented | Use `ElseIf` fluent builder for conditional rule logic |
| DynamicListBase (self-saving list) | Not implemented | Save through aggregate root cascade |
| GraphMerger | Not implemented (intentionally) | `SaveAsync` returns new instance; reassign reference |
| `BusyChanged` event | Not implemented | Subscribe to `PropertyChanged` for `IsBusy` changes |
| `Saved` event | Not implemented | Async/await continuation replaces the need for this event |

## Features Neatoo Has That CSLA Does Not

| Neatoo Feature | CSLA Equivalent | Advantage |
|---------------|----------------|-----------|
| Source-generated partial properties | `RegisterProperty<T>()` boilerplate | Zero boilerplate |
| Source-generated factories | Manual factory or DataPortal convention | Type-safe, no string-based method names |
| Object-per-property (`IValidateProperty<T>`) | Property bags correlated through base class | Direct binding to `entity["Name"].IsValid`, `.PropertyMessages`, `.IsBusy`, `.IsReadOnly` |
| Expression-based child property triggers | `ChildChanged` event + string matching | Type-safe, compile-time checked |
| Parent-as-orchestrator pattern | Manual `ChildChanged` handlers | Same mechanism as same-object rules |
| `AddAction` / `AddActionAsync` fluent rules | `BusinessRules.AddRule(new ...)` | Inline lambdas, less ceremony |
| DI-first class-based rules | Static `AddBusinessRules()` + service locator | Constructor injection, full DI access, no service locator |
| Interface-first design (IEntityRoot/IEntityBase) | EditableRoot/EditableChild markers | Compile-time enforcement of save boundaries |
| `[Service]` method injection | Service locator in DataPortal methods | Clean DI, server-only services don't break client |
| DI-first, async-first architecture | Retrofit onto sync-first design | No legacy sync baggage |
| Explicit method signatures as contract | Reflection runtime link-up | More explicit what's being saved and why |
