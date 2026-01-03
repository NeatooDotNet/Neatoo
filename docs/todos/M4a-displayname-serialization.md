# M4a: Remove DisplayName from Serialization

**Priority:** Medium
**Category:** Performance / Technical Debt
**Effort:** Medium
**Status:** Completed
**Origin:** Split from [M4](M4-resolve-todo-comments.md) TODO #3
**Completed:** 2026-01-01

---

## Problem Statement

`DisplayName` is serialized during client-server transfer, but this information already exists in `[DisplayName]` attributes on properties. This adds unnecessary network overhead (~40 bytes per property).

**Original Location:** `src/Neatoo/Internal/EntityPropertyManager.cs:29`

```csharp
[JsonConstructor]
public EntityProperty(..., string displayName, ...)
{
    this.DisplayName = displayName; // TODO - Find a better way than serializing this
}
```

---

## Solution Implemented

`DisplayName` is now restored from reflection metadata in `OnDeserialized()` instead of being serialized.

### Changes Made

**1. Added `[JsonIgnore]` to `DisplayName` in `EntityProperty<T>`**
```csharp
[JsonIgnore]
public string DisplayName { get; private set; }
```

**2. Added `ApplyPropertyInfo()` method to `IEntityProperty` and `EntityProperty<T>`**
```csharp
// In IEntityProperty
void ApplyPropertyInfo(IPropertyInfo propertyInfo);

// In EntityProperty<T>
public void ApplyPropertyInfo(IPropertyInfo propertyInfo)
{
    var dnAttribute = propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
    DisplayName = dnAttribute?.DisplayName ?? propertyInfo.Name;
}
```

**3. Updated JSON constructor to use property name as fallback**
```csharp
[JsonConstructor]
public EntityProperty(string name, T value, bool isSelfModified, bool isReadOnly, IRuleMessage[] serializedRuleMessages)
    : base(name, value, serializedRuleMessages, isReadOnly)
{
    this.IsSelfModified = isSelfModified;
    // DisplayName is restored from reflection metadata in OnDeserialized via ApplyPropertyInfo
    this.DisplayName = name;
}
```

**4. Override `OnDeserialized()` in `EntityPropertyManager`**
```csharp
public override void OnDeserialized()
{
    base.OnDeserialized();

    // Restore DisplayName from reflection metadata for each property
    foreach (var kvp in PropertyBag)
    {
        var propertyInfo = PropertyInfoList.GetPropertyInfo(kvp.Key);
        if (propertyInfo != null)
        {
            kvp.Value.ApplyPropertyInfo(propertyInfo);
        }
    }
}
```

---

## Network Savings

| Aggregate Size | Previous Overhead | After Change |
|----------------|-------------------|--------------|
| 10 properties | ~400 bytes | 0 bytes |
| 50 properties | ~2 KB | 0 bytes |

---

## Implementation Plan (Completed)

### Phase 1: Create Tests (BEFORE changes)

Document current behavior with tests that will verify the change doesn't break functionality.

- [x] **1.1** Create `DisplayNameSerializationTests.cs` in `Neatoo.UnitTest/Integration/Concepts/Serialization/`
- [x] **1.2** Test: Entity with `[DisplayName]` attribute serializes and deserializes correctly
- [x] **1.3** Test: Entity without `[DisplayName]` uses property name as DisplayName
- [x] **1.4** Test: DisplayName survives full round-trip (serialize -> deserialize)
- [x] **1.5** Test: Child entities in aggregate preserve DisplayName after deserialization
- [x] **1.6** Run tests - all should pass with current implementation

### Phase 2: Implement Solution

- [x] **2.1** Add `ApplyPropertyInfo(IPropertyInfo)` to `IEntityProperty` interface
- [x] **2.2** Implement `ApplyPropertyInfo()` in `EntityProperty<T>`
- [x] **2.3** Add `[JsonIgnore]` attribute to `DisplayName` property
- [x] **2.4** Update JSON constructor to set `DisplayName = name` as temporary fallback
- [x] **2.5** Override `OnDeserialized()` in `EntityPropertyManager` to apply PropertyInfo
- [x] **2.6** Run Phase 1 tests - all should still pass

### Phase 3: Verify & Cleanup

- [x] **3.1** Run full test suite (1627 tests pass)
- [x] **3.2** Removed TODO comment from `EntityPropertyManager.cs`
- [x] **3.3** Verified serialized JSON no longer contains `displayName` field (test added)
- [x] **3.4** Update this document status to Completed

---

## Test Coverage

### Test Class: `DisplayNameSerializationTests`

Located at: `src/Neatoo.UnitTest/Integration/Concepts/Serialization/DisplayNameSerializationTests.cs`

| Test Method | Description |
|-------------|-------------|
| `DisplayName_FromAttribute_PreservedAfterSerialization` | Verifies `[DisplayName("Full Name")]` attribute value is preserved |
| `DisplayName_NoAttribute_UsesPropertyName` | Verifies property name is used when no attribute present |
| `DisplayName_ChildEntities_PreservedInAggregate` | Verifies child entity DisplayName preservation |
| `DisplayName_FullRoundTrip_Preserved` | Verifies multiple serialize/deserialize cycles work |
| `DisplayName_GuidProperty_UsesPropertyName` | Verifies Guid property uses property name |
| `SerializedJson_DoesNotContain_DisplayNameField` | Verifies JSON no longer contains displayName |

---

## Files Modified

| File | Action |
|------|--------|
| `src/Neatoo/IEntityProperty.cs` | Added `ApplyPropertyInfo()` method |
| `src/Neatoo/Internal/EntityPropertyManager.cs` | Added `[JsonIgnore]`, updated constructor, added `OnDeserialized()` override |
| `src/Neatoo.UnitTest/Integration/Concepts/Serialization/DisplayNameSerializationTests.cs` | Created new test file |
| `src/Neatoo.UnitTest/Unit/Core/EntityPropertyTests.cs` | Updated tests using JSON constructor (removed displayName parameter) |

---

## Related

- [M4: Resolve TODO Comments](M4-resolve-todo-comments.md) - Parent issue
- `src/Neatoo/Internal/NeatooBaseJsonTypeConverter.cs` - Serialization logic
- `src/Neatoo/Internal/PropertyInfoList.cs` - Property metadata lookup

---

*Created: 2026-01-01*
*Completed: 2026-01-01*
