# LoadValue Does Not Establish Parent-Child Relationships

**Status:** In Progress
**Priority:** High
**Created:** 2026-01-15

---

## Problem

`LoadValue()` uses `quietly=true` when calling `SetPrivateValue()`, which prevents the `NeatooPropertyChanged` event from firing. This event is what triggers `SetParent()` in `ValidateBase._PropertyManager_NeatooPropertyChanged()`.

**Result:** Child objects/lists assigned via `LoadValue()` never have their parent relationship established.

**Why `quietly=true` exists:** Constructors run OUTSIDE factory pause. Without `quietly=true`, property assignments in constructors would trigger rules before the object is fully initialized.

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

**Option 2 (parent reference)** is the only safe approach:
1. Add `IValidateBase? Owner` property to `ValidateProperty`
2. Set `Owner` when property is registered with `PropertyManager`
3. In `LoadValue`, call `SetParent(Owner)` directly on new value if it implements `ISetParent`

This:
- Keeps `quietly=true` for modification tracking suppression
- Adds the missing `SetParent` call
- Avoids firing events that could bubble up (n! risk)
- Works in both factory (paused) and DI (unpaused) constructors

## Tasks

- [ ] Decide on approach
- [ ] Implement fix in `ValidateProperty.LoadValue()`
- [ ] Verify all failing tests pass
- [ ] Consider if `OnDeserialized` still needs explicit `SetParent` loop (might be redundant after fix)

---

## Progress Log

### 2026-01-15
- Created NEATOO010 analyzer for constructor property assignments
- Fixed analyzer violations using `LoadValue()`
- Discovered `LoadValue()` breaks parent-child relationships
- Investigated root cause: `quietly=true` prevents `SetParent` event
- Committed analyzer work with known issue documented
- Created this todo for investigation/fix

---

## Related Files

- `src/Neatoo/Internal/ValidateProperty.cs` - `LoadValue()` method (line 318-322)
- `src/Neatoo/ValidateBase.cs` - `_PropertyManager_NeatooPropertyChanged()` (line 390-404)
- `src/Neatoo/ValidateBase.cs` - `OnDeserialized()` (line 523-541)
- `src/Neatoo.UnitTest/Integration/Concepts/ValidateBase/ValidateListBaseRuleTests.cs` - failing tests
