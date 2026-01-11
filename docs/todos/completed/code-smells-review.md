# Code Smells Review

Code review of Neatoo library identifying code smells and areas for improvement.

**Created:** 2026-01-10
**Status:** Code Review Complete - Documentation Tasks Remaining

---

## High Priority

---

### Meta State Pattern Duplication → Extract IMetaStateNotifier Service

**Files:**
- `src/Neatoo/ValidateBase.cs:278-296`
- `src/Neatoo/EntityBase.cs:215-238`
- `src/Neatoo/ValidateListBase.cs:272-288`
- `src/Neatoo/EntityListBase.cs:140-159`

The same `RaiseIfChanged` pattern is duplicated across 4 classes. Following Neatoo's extensibility principle, this should be a DI-registered service so users can customize notification behavior (logging, batching, throttling, telemetry).

**Design:**
```csharp
public interface IMetaStateNotifier
{
    void RaiseIfChanged<T>(T cachedValue, T currentValue, string propertyName,
                           Action<string> raisePropertyChanged,
                           Action<NeatooPropertyChangedEventArgs> raiseNeatooPropertyChanged);
}
```

- State caching stays in each class (shape varies per class)
- Notification behavior is centralized and swappable via DI

**Tasks:**
- [ ] Create `IMetaStateNotifier` interface
- [ ] Create `MetaStateNotifier` default implementation in `Neatoo.Internal`
- [ ] Register in `AddNeatooServices`
- [ ] Inject via `IValidateBaseServices<T>`
- [ ] Refactor ValidateBase to use IMetaStateNotifier
- [ ] Refactor EntityBase to use IMetaStateNotifier
- [ ] Refactor ValidateListBase to use IMetaStateNotifier
- [ ] Refactor EntityListBase to use IMetaStateNotifier
- [ ] Ensure all existing tests pass after refactoring

---

### Document Extensibility Principle

**Draft created:** `docs/architecture/extensibility-principle.md`

Add extensibility principle to main documentation (advanced section). This principle should be prominent so users understand Neatoo's philosophy of full DI extensibility.

- [ ] Add "Architecture" or "Advanced" section to main docs
- [ ] Move/incorporate extensibility-principle.md content
- [ ] Add examples of customizing services
- [ ] Document which services can be replaced and why users might want to

---

## Medium Priority

### Thread Safety in IsBusy Checks → Remove Inconsistent Lock + Document Threading Model

**Files:**
- `src/Neatoo/Internal/ValidateProperty.cs:58-67`
- `src/Neatoo/Internal/ValidatePropertyManager.cs:43`

**Issue:** `IsBusy` uses `_isMarkedBusyLock` but `IsSelfBusy` is read/written without synchronization. The lock is inconsistent and unnecessary.

**Decision:** Neatoo assumes single async flow per entity instance. Entities must not be shared across concurrent operations. This matches typical usage: factory creates → use → save → discard.

**Threading model by environment:**
- Blazor WASM: Single-threaded ✅
- Blazor Server components: Sync context per circuit ✅
- Blazor Server background work: No sync context - use `InvokeAsync` ⚠️
- ASP.NET Core server (RemoteFactory): No sync context, but safe if single async flow ✅

**Tasks:**
- [ ] Remove `_isMarkedBusyLock` from ValidateProperty (lock is inconsistent/unnecessary)
- [ ] Remove `_propertyBagLock` from ValidatePropertyManager if also unnecessary
- [ ] Document threading model in advanced docs section

---

### Document Threading Model (Advanced Topic)

Add threading/synchronization documentation explaining:
- Neatoo entities must not be accessed concurrently from multiple async flows
- Blazor Server background work requires `InvokeAsync` to access entities
- ASP.NET Core server-side is safe within a single async operation flow

**Reference:** [ASP.NET Core Blazor synchronization context](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context)

- [ ] Create threading model documentation
- [ ] Add examples of safe vs unsafe usage patterns
- [ ] Document Blazor Server `InvokeAsync` requirement for background work

---

### Bug: RunRulesFlag.Messages and NoMessages Don't Work

**Discovered during:** Complex flag conditionals refactoring

**Files:**
- `src/Neatoo/Rules/RuleBase.cs:207-210` - `PreviousMessages` never set
- `src/Neatoo/Rules/RuleManager.cs` - Should set `PreviousMessages` after running rule

**Issue:** `IRule.Messages` property always returns empty because `PreviousMessages` is never populated. This means:
- `RunRulesFlag.NoMessages` always matches (messages count is always 0)
- `RunRulesFlag.Messages` never matches

**Impact:** These flags are effectively broken and have been since they were created. Tests now document this behavior.

- [ ] Fix by having RuleManager set `PreviousMessages` on the rule after execution
- [ ] Or remove these flags if they're not needed

---

### ~~RuleManager.RunRule Method Size~~ → Resolved

**File:** `src/Neatoo/Rules/RuleManager.cs:419-515`

**Decision:** Method is cohesive - try/catch/finally structure naturally groups concerns. Extraction would just move code without reducing complexity.

**Action taken:** Removed dead code (redundant type check and unreachable else branch).

- [x] Analyzed method structure - decided extraction not beneficial
- [x] Removed dead code (redundant `if (r is IRule rule)` check)

---

### ~~Confusing Bitwise Logic in ValidateBase.RunRules~~ → Fixed

**File:** `src/Neatoo/ValidateBase.cs:747`

Replaced confusing bitwise expression with simple comparison and added comment:

```csharp
// Before (confusing):
if ((runRules | Neatoo.RunRulesFlag.Self) != Neatoo.RunRulesFlag.Self)

// After (clear):
// Run child property rules unless only Self flag is set
if (runRules != RunRulesFlag.Self)
```

- [x] Rewrite with clearer logic
- [x] Add comment explaining intent

---

## Low Priority

### ~~Magic Numbers~~ → Fixed Collision Issue + Documented Serialization Fragility

**Original code:** `src/Neatoo/Rules/RuleManager.cs:433`

```csharp
var uniqueExecIndex = rule.UniqueIndex + Random.Shared.Next(10000, 100000);
```

**Issues identified:**

1. **Collision risk:** If Rule A's `UniqueIndex + offset` equals Rule B's `UniqueIndex + offset`, one execution's `RemoveMarkedBusy` could remove another's marker.

2. **Rule Index Serialization Fragility:** Rules must be added in identical order on client/server for message tracking to work correctly.

**Fix applied:** Replaced random-based approach with atomic counter:
```csharp
private static long _nextExecId = 0;
var uniqueExecIndex = Interlocked.Increment(ref _nextExecId);
```

`UniqueIndex` is still used for `RuleMessage.RuleIndex` (serialization), but busy tracking now uses collision-free atomic IDs.

**Documentation tasks (serialization fragility):**
- [ ] Document rule index serialization contract in advanced docs
- [ ] Document that rules MUST be added in identical order on client/server
- [ ] Consider if there's a more robust identification scheme (rule type + property hash?)

- [x] Fixed collision issue with atomic counter
- [x] Updated comments explaining the approach

---

### ~~Redundant Type Check / Dead Code~~ → Fixed

**File:** `src/Neatoo/Rules/RuleManager.cs:421`

- [x] Removed redundant type check and dead else branch
- [x] Renamed parameter from `r` to `rule` for clarity

---

### ~~Dead/Unclear Comment~~ → Fixed

**File:** `src/Neatoo/Rules/RuleBase.cs:241`

Updated unclear comment to explain actual behavior:

```csharp
// Only set if not already assigned. This protects static rules (shared instances)
// from having their index overwritten when added to multiple RuleManagers.
```

- [x] Investigated and documented behavior with static rules
- [x] Updated comment with clear explanation

---

### ~~Await Redundancy~~ → Clarified (Not Redundant)

**File:** `src/Neatoo/Internal/ValidateProperty.cs:260-286`

**Analysis:**
- Second await is NOT redundant - handles case when `IsSelfBusy` is already true
- "Duplicate" `IsSelfBusy = false` is NOT redundant - must set false BEFORE `OnPropertyChanged` so listeners see correct `IsBusy` value; finally is failsafe for exceptions

**Changes made:**
- Added comments explaining the pattern (both the conditional await and the intentional double-assignment)

- [x] Analyzed - second await is necessary for IsSelfBusy=true case
- [x] Analyzed - try block assignment is necessary for notification ordering
- [x] Added comments explaining both patterns

---

### Inconsistent Private Field Naming → Deferred

**Files:** Various

| Convention | Example | Location |
|------------|---------|----------|
| `_camelCase` | `_value` | ValidateProperty.cs |
| `_PascalCase` | `_Property_NeatooPropertyChanged` | ValidatePropertyManager.cs |
| No prefix | `RuleMessages` | ValidateProperty.cs:337 |

**Decision:** Deferred - low value, high churn. Would require many renames across the codebase with minimal benefit. Existing code works correctly; inconsistency is cosmetic.

---

### ~~Deep Nesting in Event Handlers~~ → Acceptable

**File:** `src/Neatoo/Internal/ValidatePropertyManager.cs:132-174`

**Analysis:** Nesting is only 2 levels deep (not 4 as originally noted). The code is straightforward - guard clause, then conditional updates for IsValid/IsSelfValid. Extracting would add indirection without improving readability.

- [x] Reviewed - nesting is acceptable (2 levels, not 4)
- [x] No refactoring needed

---

### ~~Rule Addition Pattern Duplication~~ → Fixed

**File:** `src/Neatoo/Rules/RuleManager.cs`

Extracted `RegisterRule<TRule>` helper method. Pattern was repeated 8 times (6 fluent methods + AddRule + attribute scanning).

```csharp
private TRule RegisterRule<TRule>(TRule rule) where TRule : IRule
{
    this.Rules.Add(this._ruleIndex++, rule);
    rule.OnRuleAdded(this, this._ruleIndex);
    return rule;
}
```

- [x] Extracted to `RegisterRule<TRule>(TRule rule)` helper method
- [x] Updated all 8 occurrences

---

### ~~Reflection in Property Creation~~ → Necessary, Well-Cached

**File:** `src/Neatoo/Internal/ValidatePropertyManager.cs:95-130`

**Analysis:** Reflection is necessary here because:
- `CreateProperty<T>` is generic and T is only known at runtime via `propertyInfo.Type`
- Must use `MakeGenericMethod` to call with correct type argument
- Source generation can't help - property types are arbitrary user types

**Caching is already in place:**
- `_createPropertyMethod` - cached once for the `CreateProperty` MethodInfo
- `_createPropertyMethodPropertyType` - ConcurrentDictionary caches each `MakeGenericMethod` result by property type

Reflection happens only once per property type across the application lifetime.

- [x] Documented why reflection is necessary
- [x] Verified source generation cannot eliminate this
- [x] Confirmed caching is working correctly

---

## Completed

### Complex Flag Conditionals in RunRules

**File:** `src/Neatoo/Rules/RuleManager.cs:385-417`

Extracted complex conditional to `ShouldRunRule(IRule rule, RunRulesFlag flags)` static method.

- [x] Extract to `ShouldRunRule(IRule rule, RunRulesFlag flags)` method
- [x] Add XML documentation explaining flag combinations
- [x] Add unit tests for each flag combination (13 tests added)

**Bonus:** Discovered that `Messages` and `NoMessages` flags never worked - documented in bug above.

---

### Mutable Static State - `RuleMessages.None`

**Files:** `src/Neatoo/Rules/RuleMessage.cs:217`, `src/Neatoo/Rules/RuleMessage.cs:180`

`RuleMessages.None` and `IRuleMessages.None` were mutable `List<IRuleMessage>` exposed as static fields. Any code that added to this "empty" collection would corrupt global state.

- [x] Change `RuleMessages.None` to return a new instance or be read-only
- [x] Change `IRuleMessages.None` to return a new instance or be read-only
- [ ] Add unit test to verify None collections cannot be corrupted *(skipped - now returns new instance each time)*

**Fix applied:** Changed both from static fields to expression-bodied properties:
```csharp
public static IRuleMessages None => new RuleMessages();
public static RuleMessages None => new RuleMessages();
```

---

## Notes

- The mutable static state issue is the only one with potential correctness impact
- Most issues are maintainability/readability concerns
- Thread safety analysis may reveal the code is safe due to usage patterns
- Some refactoring (naming consistency) may not be worth the churn
