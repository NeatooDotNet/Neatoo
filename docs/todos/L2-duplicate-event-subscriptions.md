# L2: Guard Against Duplicate Event Subscriptions

**Priority:** Low
**Category:** Edge Case Bug
**Effort:** Low
**Status:** Not Started

---

## Problem Statement

In `Property.cs`, the `OnDeserialized` method subscribes to event handlers without checking if already subscribed:

```csharp
public void OnDeserialized()
{
    if (this.Value is INotifyNeatooPropertyChanged neatooPropertyChanged)
    {
        neatooPropertyChanged.NeatooPropertyChanged += this.PassThruValueNeatooPropertyChanged;
    }
    if (this.Value is INotifyPropertyChanged notifyPropertyChanged)
    {
        notifyPropertyChanged.PropertyChanged += this.PassThruValuePropertyChanged;
    }
}
```

If `OnDeserialized` is called multiple times (e.g., during re-serialization scenarios), handlers would be duplicated.

---

## Risk Assessment

**Likelihood:** Low - `OnDeserialized` is typically called once by the JSON serializer

**Impact:** Medium - Duplicate handlers cause:
- Multiple event firings
- Memory leaks (handlers hold references)
- Performance degradation

---

## Proposed Fix

Add guard to prevent duplicate subscriptions:

### Option A: Unsubscribe First

```csharp
public void OnDeserialized()
{
    if (this.Value is INotifyNeatooPropertyChanged neatooPropertyChanged)
    {
        // Unsubscribe first (safe even if not subscribed)
        neatooPropertyChanged.NeatooPropertyChanged -= this.PassThruValueNeatooPropertyChanged;
        neatooPropertyChanged.NeatooPropertyChanged += this.PassThruValueNeatooPropertyChanged;
    }
    if (this.Value is INotifyPropertyChanged notifyPropertyChanged)
    {
        notifyPropertyChanged.PropertyChanged -= this.PassThruValuePropertyChanged;
        notifyPropertyChanged.PropertyChanged += this.PassThruValuePropertyChanged;
    }
}
```

### Option B: Track Subscription State

```csharp
private bool _eventHandlersSubscribed;

public void OnDeserialized()
{
    if (_eventHandlersSubscribed)
        return;

    _eventHandlersSubscribed = true;

    if (this.Value is INotifyNeatooPropertyChanged neatooPropertyChanged)
    {
        neatooPropertyChanged.NeatooPropertyChanged += this.PassThruValueNeatooPropertyChanged;
    }
    // ...
}
```

---

## Recommended Approach

**Option A** is recommended because:
1. Simpler - no additional state to track
2. Unsubscribe-then-subscribe is idempotent
3. Handles edge cases where flag could get out of sync

---

## Implementation Tasks

- [ ] Add unsubscribe before subscribe in `OnDeserialized`
- [ ] Add unit test for multiple deserialization calls
- [ ] Verify no side effects in existing tests

---

## Testing

```csharp
[TestMethod]
public void OnDeserialized_CalledMultipleTimes_NoExtraHandlers()
{
    // Arrange
    var property = CreatePropertyWithObservableValue();
    var eventCount = 0;

    // Simulate being a parent listening to property changes
    property.PropertyChanged += (s, e) => eventCount++;

    // Act - call OnDeserialized multiple times
    property.OnDeserialized();
    property.OnDeserialized();
    property.OnDeserialized();

    // Trigger a change on the value
    ((IObservableValue)property.Value).Change();

    // Assert - should only fire once, not three times
    Assert.AreEqual(1, eventCount);
}
```

---

## Files to Modify

| File | Action |
|------|--------|
| `src/Neatoo/Property.cs` | Add unsubscribe before subscribe |
| `src/Neatoo.UnitTest/Unit/Core/PropertyTests.cs` | Add test |
