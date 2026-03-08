# Neatoo Skill Gap Analysis

Gaps identified by using CSLA training knowledge as a feature catalog to find Neatoo capabilities that exist but aren't documented in the skill. When features are missing from the skill, Claude falls back to putting logic in the UI instead of using the framework.

## Methodology

Claude has extensive CSLA knowledge from training data. Neatoo + RemoteFactory together = CSLA (both skills should be assumed loaded). Each gap was verified against the Neatoo source code (`src/Neatoo/`) and reviewed with the framework author. Gaps represent either:

1. **Undocumented features** — the framework does it, but the skill doesn't say so → Claude writes UI code
2. **Undersold features** — the skill mentions it, but doesn't convey its significance → Claude doesn't use it
3. **Missing features** — the framework genuinely doesn't have this → document in pitfalls to prevent confusion

---

## High Impact Gaps

These cause Claude to write UI event handlers instead of using the framework.

### 1. Blazor Binding Mental Model — #1 Gap

**Problem:** Claude writes `OnXYZUpdated()` UI event handlers instead of using data binding. The skill doesn't explain the two binding modes.

**Neatoo reality:** Two binding modes exist, and the object-per-property architecture makes both work:

| Binding Mode | Syntax | Use When | Why It Updates |
|-------------|--------|----------|----------------|
| Display value | `@entity.Total` | Showing values, `@bind` | Entity implements `INotifyPropertyChanged`; cascading rules update properties which fire `PropertyChanged` |
| Property metadata | `entity["Total"]` | Need `.IsValid`, `.PropertyMessages`, `.IsBusy`, `.IsReadOnly` | Each `IValidateProperty` fires its own `PropertyChanged` for metadata changes |

MudNeatoo components handle the metadata case automatically — they bind to `IEntityProperty`, subscribe to `PropertyChanged`, and call `StateHasChanged`.

**Why this is #1:** Without this mental model, Claude writes event handlers for every computed property, conditional visibility check, and validation display — all things the framework handles through binding.

**Recommended skill fix:** Add to `blazor.md`, prominent section:

```markdown
## Two Binding Modes

Neatoo entities support two binding modes. Choose based on what you need:

| Need | Syntax | Example |
|------|--------|---------|
| Display a value | `@entity.PropertyName` | `<span>@order.Total</span>` |
| Bind for editing | `@bind-Value="entity.PropertyName"` | Standard Blazor binding |
| Show validation state | `entity["PropertyName"]` via MudNeatoo | `<MudNeatooTextField EntityProperty="entity["Name"]" />` |

Both modes auto-update: entity properties fire `INotifyPropertyChanged`,
and `IValidateProperty` objects fire `PropertyChanged` for metadata changes.

Do NOT write `OnPropertyChanged` event handlers for computed values.
If a rule writes to `Total`, `@entity.Total` updates automatically.
```

---

### 2. Object-Per-Property Architecture Undersold

**Problem:** The skill mentions `IValidateProperty<T>` and the string indexer but doesn't convey the architectural significance. CSLA correlates property bags through the base class. Neatoo gives each property its own object with metadata.

**Neatoo reality:** `entity["PropName"]` returns an `IValidateProperty<T>` / `IEntityProperty` with:
- `.Value` — the property value
- `.IsValid` — validation state for this property
- `.PropertyMessages` — validation messages for this property
- `.IsBusy` — async rule running state
- `.IsReadOnly` — structural read-only state
- `.IsModified` — dirty tracking for this property
- `PropertyChanged` event — fires when any metadata changes

This is what enables direct UI binding to validation state without routing through the entity.

**Recommended skill fix:** Add to `properties.md`:

```markdown
## Object-Per-Property Architecture

Each property has its own `IValidateProperty<T>` object with metadata:
- `entity["Name"].IsValid` — validation state
- `entity["Name"].PropertyMessages` — error messages
- `entity["Name"].IsBusy` — async rule running
- `entity["Name"].IsReadOnly` — read-only state
- `entity["Name"].IsModified` — dirty tracking

Each property object fires `PropertyChanged` when metadata changes.
MudNeatoo components bind to these objects directly.
```

---

### 3. DI-First Rule Architecture Undersold

**Problem:** The skill describes rules with the fluent API (`AddAction`/`AddValidation`) but undersells the class-based DI-first architecture. CSLA's `AddBusinessRules()` was a static method — no DI, required service locator. Neatoo rules are fundamentally different.

**Neatoo reality:**
- Inherit from `RuleBase<T>` / `AsyncRuleBase<T>`
- Constructor-inject any DI services
- Register via `RuleManager.AddRule(injectedRule)` in the entity constructor
- Rule operates directly on target `T` — no `IRuleContext`, no `LoadProperty`/`SetProperty`
- Async by default
- Fluent API (`AddAction`/`AddValidation`) is shorthand for simple cases

**Recommended skill fix:** Strengthen the class-based rules section in `validation.md` to emphasize:
- Constructor injection (contrast with CSLA's service locator)
- Direct target manipulation (no `IRuleContext` ceremony)
- That the fluent API is shorthand, not the primary mechanism for complex rules

---

### 4. Save Returns New Instance — UI Implications

**Current skill state:** `entities.md` documents "Reassign after save" but doesn't address the Blazor UI implication.

**Risk:** Claude will call `Save()` without replacing the bound entity reference, leaving the UI bound to a stale object.

**Recommended skill fix:** Add to `blazor.md`:

```markdown
## Handling Save in Blazor

`Save()` returns a new entity instance. The old reference is stale.
After save, replace the bound reference so the UI re-binds:

    entity = await factory.SaveAsync(entity);
    StateHasChanged(); // Re-render with new instance
```

---

### 5. No N-Level Undo

**CSLA pattern:** `BeginEdit()`, `CancelEdit()`, `ApplyEdit()` for multi-level undo with `EditLevel` tracking.

**Neatoo reality:** No undo capability. Zero references to `BeginEdit`, `CancelEdit`, `ApplyEdit` in the codebase.

**Workaround:** Re-fetch the entity to discard changes.

**Recommended skill fix:** Add to `pitfalls.md`:

```
| Expecting BeginEdit/CancelEdit/ApplyEdit | Neatoo has no n-level undo. To discard changes, re-fetch the entity. | Re-fetch from factory to reset state |
```

---

### 6. No Per-Property Authorization (CanRead/CanWrite)

**CSLA pattern:** `CanReadProperty()`, `CanWriteProperty()` with role-based authorization rules per property.

**Neatoo reality:** `IValidateProperty.IsReadOnly` exists but is structural (set from `PropertyInfo.IsPrivateSetter`), not dynamic or role-based. No `CanReadProperty` / `CanWriteProperty` system. Authorization is at factory operation level via `[AuthorizeFactory<T>]` and `[AspAuthorize]` (documented in RemoteFactory skill).

**Recommended skill fix:** Add to `pitfalls.md`:

```
| Expecting per-property authorization (CanRead/CanWrite) | Neatoo has `IsReadOnly` on properties (structural, from private setter) but no dynamic role-based per-property authorization. | Use `[AuthorizeFactory<T>]` for operation-level auth (see RemoteFactory skill). For dynamic read-only, set `IsReadOnly` in rules or factory methods. |
```

---

### 7. No Rule Severity Levels (Warning/Information)

**CSLA pattern:** Rules could return `RuleSeverity.Error`, `RuleSeverity.Warning`, or `RuleSeverity.Information`.

**Neatoo reality:** `IRuleMessage` has only `RuleId`, `PropertyName`, and `Message`. No `Severity` property. All rule messages are errors that make `IsValid = false`.

**Recommended skill fix:** Add to `pitfalls.md`:

```
| Expecting rule severity levels (Warning/Information) | All Neatoo rule messages are errors. There are no warning or informational severity levels. Any non-empty message makes `IsValid = false`. | For advisory messages, use a separate domain property (e.g., `WarningMessage`) computed via `AddAction`. |
```

---

## Medium Impact Gaps

These affect less common scenarios but will cause confusion when encountered.

### 8. INotifyCollectionChanged — Exists but Undocumented

**Verified:** `ValidateListBase` implements `INotifyCollectionChanged`. `EntityListBase` inherits it.

**Risk:** Claude may not know lists support collection binding for UI.

**Recommended fix:** Add one line to `collections.md`:
> Both `EntityListBase` and `ValidateListBase` implement `INotifyCollectionChanged` for UI list binding.

---

### 9. All Rules Require Trigger Properties

**CSLA pattern:** Object-level rules could validate the entire object without being tied to a specific property.

**Neatoo reality:** `AddValidation` takes exactly one trigger property. `AddAction` takes 1–3 or an array. `AddRule` with class-based rules also requires trigger properties. There is no "validate the whole object on any change" mechanism. `RunningTasks.OnFullSequenceComplete` can be used for post-all-rules logic.

**Recommended fix:** Add to `validation.md`:
> All rules (AddAction, AddValidation, AddRule) require at least one trigger property. For full revalidation, call `RunRules(RunRulesFlag.All)` explicitly.

---

### 10. NeatooFactory Modes Undocumented

**Verified:** `NeatooFactory` enum: `Server`, `Remote`, `Logical`.

| Mode | Use When |
|------|----------|
| `Server` | Server process in 3-tier architecture |
| `Remote` | Client process in 3-tier architecture (generates HTTP calls) |
| `Logical` | Single-tier apps, unit tests (all operations local) |

**Current skill state:** Only `Logical` is mentioned (in testing docs).

**Recommended fix:** Add NeatooFactory modes to `source-generation.md` or a new `setup.md` reference:
> `services.AddNeatooServices(NeatooFactory.Server, assemblies)` — three modes: `Server` (3-tier server), `Remote` (3-tier client), `Logical` (single-tier/tests).

---

### 11. FactoryComplete Lifecycle Hook Undocumented

**Verified:** `FactoryComplete(FactoryOperation)` is a virtual method on `ValidateBase`, `EntityBase`, and `EntityListBase`. Called after factory operations complete.

**Current skill state:** Mentioned in passing but not explained as a usable hook.

**Recommended fix:** Add to `entities.md`:
> Override `FactoryComplete(FactoryOperation)` to run logic after Create/Fetch/Insert/Update/Delete completes. The framework calls this automatically.

---

### 12. OnDeserialized Hook Undocumented

**Verified:** `ValidateBase`, `ValidateListBase`, and property managers implement `IJsonOnDeserialized` with virtual `OnDeserialized()` method.

**Recommended fix:** Add to `properties.md`:
> Override `OnDeserialized()` to restore transient state after JSON deserialization (e.g., re-subscribe to events, recalculate derived values that aren't serialized).

---

### 13. ReadOnlyBase and CriteriaBase Incorrectly Mapped

**Current skill state:** `base-classes.md` maps ReadOnlyBase to "ValidateBase with [Fetch] only."

**Corrected mappings:**
- **ReadOnlyBase** → Standard class with `[Factory]` + `[Fetch]` via RemoteFactory. No base class needed.
- **CriteriaBase** → `ValidateBase<T>` when validated criteria with rules and `IsValid` are needed. Otherwise just method parameters or a POCO. CriteriaBase existed because serialization wasn't out-of-the-box in early .NET.

**Recommended fix:** Update `base-classes.md` with corrected mappings.

---

## Low Impact Gaps

These are edge cases or already have obvious workarounds.

| # | Gap | Status | Workaround | Fix Priority |
|---|-----|--------|------------|:---:|
| 14 | Rule priority / short-circuiting | Does not exist | Registration order controls execution; use `ElseIf` fluent builder | Low |
| 15 | DynamicListBase | Does not exist | Save through aggregate cascade | Low |
| 16 | BusyChanged event | Does not exist | Subscribe to `PropertyChanged` for `IsBusy` | Low |
| 17 | DbContext scoping | Normal DI lifetime rules apply | Standard `AddDbContext` scoped registration | Low |
| 18 | Multi-assembly domain models | `AddNeatooServices` takes `params Assembly[]` | Pass multiple assemblies | Low |
| 19 | Property serialization exclusion | No easy way to exclude partial properties | Needs framework fix | Low |

---

## Resolved (Already in Skill)

These CSLA concepts are already well-documented in the Neatoo skill:

- `IsDirty` → `IsModified` naming difference (explicit warning in SKILL.md)
- DataPortal → RemoteFactory with `[Create]`/`[Fetch]`/`[Insert]`/`[Update]`/`[Delete]`
- `LoadProperty` → `LoadValue` (properties.md)
- `ChildChanged` → child property triggers with `AddAction` (SKILL.md, domain-logic-placement.md)
- Rules during Fetch/Create → `PauseAllActions()` wraps factory operations (pitfalls.md)
- DeletedList behavior (collections.md)
- Aggregate save cascading (entities.md)
- Parent-as-orchestrator (SKILL.md, domain-logic-placement.md)
- Testing without mocking Neatoo (testing.md)
- Domain logic placement / thin UI principle (blazor.md, domain-logic-placement.md)
- Authorization via `[AuthorizeFactory<T>]` and `[AspAuthorize]` (RemoteFactory skill)

---

## Two-Skill Relationship

**Neatoo + RemoteFactory = CSLA.** The Neatoo skill covers domain model, validation, properties, and collections. The RemoteFactory skill covers factory operations, authorization, client-server transfer, and lifecycle hooks. Both skills should be assumed loaded together. Consider noting this relationship in both skills.
