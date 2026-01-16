# LoadValue Does Not Establish Parent-Child Relationships

**Status:** Complete
**Priority:** High
**Created:** 2026-01-15

---

## Problem

`LoadValue()` uses `quietly=true` when calling `SetPrivateValue()`, which prevents the `NeatooPropertyChanged` event from firing. This event is what triggers `SetParent()` in `ValidateBase._PropertyManager_NeatooPropertyChanged()`.

**Result:** Child objects assigned via `LoadValue()` never have their parent relationship established.

### Scope of Impact

**Affects ALL Neatoo base classes:**
- `ValidateBase<T>` - validation objects
- `EditBase<T>` - editable objects
- `EntityBase<T>` - persistable entities

**Affects ALL child types:**
- Single child objects (e.g., `Address` property)
- Child lists (`EntityListBase<T>`)
- Any type implementing `ISetParent`

**Occurs in ALL contexts where LoadValue is used:**
- **Constructors** - NEATOO010 analyzer recommends `LoadValue()` here
- **Factory Fetch methods** - Loading data from database (e.g., `PersonEntityBase.cs:40`)
- **Rules** - `RuleBase.LoadProperty()` uses `LoadValue()` internally
- **Any code** avoiding modification tracking

### Why `quietly=true` exists

Constructors run OUTSIDE factory pause. Without `quietly=true`, property assignments in constructors would trigger rules before the object is fully initialized.

**The conflict:**
- `quietly=true` is needed to prevent rule execution in constructors
- `quietly=true` also prevents `SetParent()` from being called
- `SetParent()` is structural, not related to modification tracking or rules

## Discovered Via

The NEATOO010 analyzer flags direct property assignments in constructors and suggests `LoadValue()`. When we converted `ChildObjList = new ChildObjList()` to `ChildObjListProperty.LoadValue(new ChildObjList())`, the parent-child relationship broke and tests started failing.

## Failing Tests

- `ValidateListBaseRuleTests_UniqueValue_Valid`
- `ValidateListBaseRuleTests_UniqueValue_Fixed`
- `ValidateListBaseRuleTests_UniqueValue_Removed_Fixed`

All fail because `ChildObj.ParentObj` returns `null` (parent never set).

## Current Workaround

`OnDeserialized()` explicitly loops through properties and calls `SetParent()` - but this only covers the serialization path, not constructors, factory methods, or rules.

## Root Cause Analysis

**Call flow comparison:**

Direct assignment (`Property = value`):
1. Generated setter calls `SetPrivateValue(value, false)` (quietly=false)
2. `HandleNonNullValue()` fires `OnValueNeatooPropertyChanged()`
3. `ValidateBase._PropertyManager_NeatooPropertyChanged()` receives event
4. Calls `child.SetParent(this)` ✓

LoadValue:
1. `LoadValue(value)` calls `SetPrivateValue(value, true)` (quietly=true)
2. `HandleNonNullValue()` skips `OnValueNeatooPropertyChanged()` because quietly=true
3. No event fires
4. `SetParent()` never called ✗

**Key insight from code:**
```csharp
// ValidateBase.cs line 390-404
private Task _PropertyManager_NeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    if (eventArgs.Property != null && eventArgs.Property == eventArgs.Source)
    {
        if (eventArgs.Property.Value is ISetParent child)
        {
            child.SetParent(this);  // SetParent is called UNCONDITIONALLY (no IsPaused check)
        }
        // ...
    }
    return this.ChildNeatooPropertyChanged(eventArgs);  // IsPaused check is HERE for rules
}
```

`SetParent` doesn't check `IsPaused` - only rule execution does. This means if we fire the event, `SetParent` will be called even when paused, but rules won't run.

## Possible Solutions

### Option 1: Decouple SetParent from NeatooPropertyChanged event

Add explicit `SetParent` call in `LoadValue` after setting value. Problem: Property doesn't know who the parent is - that's only known at `ValidateBase` level.

### Option 2: Add parent reference to property

Give `ValidateProperty` a reference to its owner (`ValidateBase`). Then `LoadValue` can call `SetParent` directly.

```csharp
public virtual void LoadValue(object? value)
{
    this.SetPrivateValue((T?)value, true);
    if (value is ISetParent setParent && this.Owner != null)
    {
        setParent.SetParent(this.Owner);
    }
}
```

Downside: Adds coupling/circular reference.

### Option 3: Fire a "structural change" event that only triggers SetParent

Add a separate event that `LoadValue` fires, which only triggers `SetParent` but not rules. More complex, adds another event type.

### Option 4: Have LoadValue fire full event, change rule execution to check a different flag

Instead of using `quietly` to suppress everything, have a separate "suppress rules" mechanism. `LoadValue` would fire events (so `SetParent` works) but set a flag that prevents rules from running.

### Option 5: Call SetParent in HandleNonNullValue regardless of quietly

Move `SetParent` call from event handler into `HandleNonNullValue` itself. But property still doesn't know parent...

### Option 6: After-the-fact parent establishment

Add a method like `EstablishChildRelationships()` that mirrors what `OnDeserialized()` does. Callers of `LoadValue` with child objects would need to call this.

```csharp
// In constructor:
ChildObjListProperty.LoadValue(new ChildObjList());
this.EstablishChildRelationships(); // Loops through properties, calls SetParent
```

## Critical Insight: n! Risk

If `LoadValue` fired `NeatooPropertyChanged` in an unpaused state (DI constructor), it would cause:
1. `SetParent` called ✓
2. `RunRules` called ✗ (rules may not be configured yet)
3. `RaiseNeatooPropertyChanged` bubbles to parent ✗ (n! algorithm when building tree)

When `IsPaused=true` (factory), bubbling is prevented in `ChildNeatooPropertyChanged`:
```csharp
if (!this.IsPaused)
{
    await this.RunRules(...);
    await this.RaiseNeatooPropertyChanged(...);  // Bubbles up!
}
else
{
    this.ResetMetaState();  // No bubbling
}
```

**We cannot simply fire the event in unpaused state.** Must call `SetParent` directly.

## Why quietly=true Exists

Modification tracking in `EntityProperty.OnPropertyChanged` checks `IsPaused`:
```csharp
if (propertyName == nameof(Value))
{
    if (!this.IsPaused)  // Already checks IsPaused!
    {
        this.IsSelfModified = true;
    }
}
```

So `IsPaused=true` prevents modification tracking. But in DI constructors (`IsPaused=false`), we need `quietly=true` to skip `OnPropertyChanged(nameof(Value))`.

**The problem:** `quietly=true` skips BOTH:
- `OnPropertyChanged(nameof(Value))` - needed to skip for modification tracking
- `OnValueNeatooPropertyChanged()` - should NOT skip, needed for `SetParent`

These are currently coupled under the single `quietly` flag.

## Recommended Approach

**Option 7: Add ChangeReason to NeatooPropertyChangedEventArgs**

After brainstorming, we identified a cleaner solution that preserves architectural separation:

### Design

Add a `ChangeReason` enum to signal the *intent* of the change:

```csharp
public enum ChangeReason
{
    UserEdit,  // Normal property assignment - run rules
    Load       // Loading data - skip rules, but still do structural work (SetParent)
}

// In NeatooPropertyChangedEventArgs:
public ChangeReason Reason { get; init; } = ChangeReason.UserEdit;
```

### Implementation

1. **LoadValue fires NeatooPropertyChanged** with `Reason = ChangeReason.Load`
2. **ValidateBase._PropertyManager_NeatooPropertyChanged** always calls SetParent (unchanged)
3. **ValidateBase.ChildNeatooPropertyChanged** checks Reason before running rules:

```csharp
protected virtual async Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    if (!this.IsPaused && eventArgs.OriginalEventArgs.Reason != ChangeReason.Load)
    {
        await this.RunRules(eventArgs.FullPropertyName);
        await this.RaiseNeatooPropertyChanged(...);
        this.CheckIfMetaPropertiesChanged();
    }
    else
    {
        this.ResetMetaState();
    }
}
```

### Why This Is Better Than Option 2

| Concern | Option 2 (Owner reference) | Option 7 (ChangeReason) |
|---------|---------------------------|-------------------------|
| Property knows about ValidateBase | Yes (coupling) | No |
| Uses existing event infrastructure | No | Yes |
| Concept belongs in Neatoo | N/A | Yes - ChangeReason is a Neatoo concept |
| ValidateBase controls behavior | No - property calls SetParent | Yes - ValidateBase decides what to do |

### Key Insight

The event describes *what happened* (a Load), not *what to do about it* (skip rules). ValidateBase decides what "Load" means for its responsibilities:
- SetParent: always (structural)
- Rules: skip for Load (behavioral)
- UI PropertyChanged: always (binding)

This keeps PropertyManager as "just properties" and ValidateBase as the owner of parent-child and rule concerns.

## Tasks

- [x] Add `ChangeReason` enum to Neatoo namespace
- [x] Add `Reason` property to `NeatooPropertyChangedEventArgs`
- [x] Update `ValidateProperty.LoadValue()` to fire event with `Reason = Load`
- [x] Update `ValidateBase.ChildNeatooPropertyChanged()` to check Reason
- [x] Keep `quietly` parameter for PropertyChanged suppression (needed for modification tracking)
- [x] Verify all previously failing tests pass (ValidateListBaseRuleTests)
- [x] Verify factory Fetch methods still work (15 Fetch tests pass)
- [x] Verify parent-child relationships work (23 parent-related tests pass)
- [x] Verify `OnDeserialized` explicit `SetParent` loop is still needed (it is - see analysis)

---

## Progress Log

### 2026-01-16 (Implementation Complete)
- Implemented Option 7 (ChangeReason in NeatooPropertyChangedEventArgs)
- Created `ChangeReason.cs` with `UserEdit` and `Load` values
- Added `Reason` property to `NeatooPropertyChangedEventArgs`
- Updated `ValidateProperty.LoadValue()` to fire `NeatooPropertyChanged` with `ChangeReason.Load`
- Updated `ValidateBase.ChildNeatooPropertyChanged()` to check `OriginalEventArgs.Reason`
- Fixed regression: Removed `OnPropertyChanged(nameof(Value))` from LoadValue - only `NeatooPropertyChanged` is needed
- All 1711 tests pass (1 skipped)
- Key findings:
  - `OnDeserialized` explicit SetParent loop is STILL needed - event handlers aren't subscribed during deserialization
  - Our fix helps with runtime LoadValue calls (factory Fetch) where handlers ARE subscribed
  - LoadValue correctly does NOT fire PropertyChanged (UI event) - only NeatooPropertyChanged (internal event)

### 2026-01-16 (continued)
- Brainstorming session to find cleaner solution
- Explored the two event chains (PropertyChanged vs NeatooPropertyChanged):
  - PropertyChanged: sync, for UI binding
  - NeatooPropertyChanged: async, for internal Neatoo logic (SetParent, rules, bubbling)
  - Two events justified - one sync, one async
- Added clarifying comment to ValidateBase.cs line 399-401 explaining PropertyChanged translation
- Identified Option 7: Add `ChangeReason` to `NeatooPropertyChangedEventArgs`
  - Event describes intent (Load vs UserEdit), not behavior (skip rules)
  - ValidateBase decides what each reason means for its responsibilities
  - Preserves architectural separation (property doesn't know about ValidateBase)
- Updated recommended approach to Option 7

### 2026-01-16
- Expanded problem scope documentation:
  - Affects ALL base classes (ValidateBase, EditBase, EntityBase)
  - Affects ALL child types (single objects AND lists)
  - Affects ALL LoadValue contexts (constructors, factory Fetch, rules)
- Added "Current Workaround" section noting OnDeserialized limitation

### 2026-01-15
- Created NEATOO010 analyzer for constructor property assignments
- Fixed analyzer violations using `LoadValue()`
- Discovered `LoadValue()` breaks parent-child relationships
- Investigated root cause: `quietly=true` prevents `SetParent` event
- Committed analyzer work with known issue documented
- Created this todo for investigation/fix

---

## Related Files

- `src/Neatoo/NeatooPropertyChangedEventArgs.cs` - Add `ChangeReason` property
- `src/Neatoo/Internal/ValidateProperty.cs` - `LoadValue()` method (line 318-322)
- `src/Neatoo/ValidateBase.cs` - `_PropertyManager_NeatooPropertyChanged()` (line 390-406)
- `src/Neatoo/ValidateBase.cs` - `ChildNeatooPropertyChanged()` (line 374-388)
- `src/Neatoo/ValidateBase.cs` - `OnDeserialized()` (line 523-541)
- `src/Neatoo/Rules/RuleBase.cs` - `LoadProperty()` helper methods (lines 259, 272, 287)
- `src/Neatoo.UnitTest/Integration/Concepts/ValidateBase/ValidateListBaseRuleTests.cs` - failing tests
- `src/Neatoo.UnitTest/Integration/Aggregates/Person/PersonEntityBase.cs` - factory Fetch example (line 40)
- `src/Neatoo.UnitTest/Integration/Aggregates/Person/PersonValidateBase.cs` - factory Fetch example (line 38)
- `src/Neatoo/ChangeReason.cs` - NEW: enum for UserEdit vs Load

---

## Results / Conclusions

**Solution:** Added `ChangeReason` enum to `NeatooPropertyChangedEventArgs` to distinguish between normal property edits (`UserEdit`) and loading data (`Load`).

**Key Files Changed:**
1. `src/Neatoo/ChangeReason.cs` - New enum with `UserEdit` and `Load` values
2. `src/Neatoo/NeatooPropertyChangedEventArgs.cs` - Added `Reason` property
3. `src/Neatoo/Internal/ValidateProperty.cs` - `LoadValue` now fires `NeatooPropertyChanged` with `ChangeReason.Load`
4. `src/Neatoo/ValidateBase.cs` - `ChildNeatooPropertyChanged` checks `Reason` to skip rules for Load operations

**Architectural Insight:**
- `NeatooPropertyChanged` triggers both **structural** (`SetParent`) and **behavioral** (rules) operations
- `ChangeReason` allows discriminating between operations that need both (UserEdit) vs only structural (Load)
- `OnDeserialized` still needs explicit `SetParent` loop because event handlers aren't subscribed during deserialization

**Test Coverage:**
- All 1711 tests pass
- ValidateListBaseRuleTests (originally failing) now pass
- Factory Fetch tests pass (15 tests)
- Parent-child relationship tests pass (23 tests)
- Deserialization tests pass (25 tests)

**Commits:**
- f90c022: feat: Add ChangeReason enum for property change intent signaling
- e7ca449: feat: Add Reason property to NeatooPropertyChangedEventArgs
- 77e71ae: feat: LoadValue fires NeatooPropertyChanged with ChangeReason.Load
- 28ced77: feat: ChildNeatooPropertyChanged checks ChangeReason to skip rules for Load
- e01904f: fix: LoadValue should not fire PropertyChanged to avoid UI updates during load
