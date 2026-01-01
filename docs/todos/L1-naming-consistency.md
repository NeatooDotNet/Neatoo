# L1: Naming Consistency Fixes

**Priority:** Low
**Category:** Code Style
**Effort:** Low
**Status:** Not Started

---

## Problem Statement

Minor naming inconsistencies exist across the codebase that could be standardized.

---

## Issues Found

### 1. Field Naming in Property.cs

**Location:** `src/Neatoo/Property.cs`

Mixed conventions for protected vs private fields:

```csharp
// Private fields use underscore (correct)
private readonly object _isMarkedBusyLock = new object();
protected T? _value = default;  // Protected with underscore (inconsistent?)

// Protected property without underscore (different style)
protected List<ITriggerProperty> TriggerProperties { get; }
```

**Options:**
1. All private/protected fields use underscore `_field`
2. Protected members use no prefix, private use underscore
3. Convert protected fields to properties

**Recommendation:** Option 3 - Convert `_value` to a protected property for consistency:

```csharp
// Before
protected T? _value = default;

// After
protected T? InternalValue { get; set; } = default;
```

---

### 2. Delegate Naming

**Location:** Various

```csharp
// Missing delegate suffix/convention
public delegate CreatePropertyManager(IPropertyInfoList propertyInfoList);
```

**Recommendation:** Add `Delegate` suffix or use `Func<>`:

```csharp
// Option A: Add suffix
public delegate CreatePropertyManagerDelegate(IPropertyInfoList propertyInfoList);

// Option B: Use Func<>
public delegate IPropertyManager CreatePropertyManager(IPropertyInfoList propertyInfoList);
```

---

### 3. Event Handler Naming

**Location:** Various

Most event handlers follow the pattern `Handle{Event}` or `On{Event}`:

```csharp
// Inconsistent naming
private void PassThruValueNeatooPropertyChanged(...)  // Different pattern
protected virtual void OnPropertyChanged(...)  // Standard pattern
```

**Recommendation:** Standardize to `On{Event}` for overridable and `Handle{Event}` for private:

```csharp
// Private handler
private void HandleValueNeatooPropertyChanged(...)

// Overridable handler
protected virtual void OnPropertyChanged(...)
```

---

## Implementation Tasks

- [ ] Audit all field naming for consistency
- [ ] Decide on protected field convention
- [ ] Apply chosen convention to Property.cs
- [ ] Review delegate naming
- [ ] Review event handler naming
- [ ] Update any affected tests

---

## Breaking Change Assessment

| Change | Breaking? | Notes |
|--------|-----------|-------|
| Rename `_value` to property | Possibly | If derived classes access it |
| Rename private handlers | No | Internal only |
| Rename delegates | Yes | If used externally |

**Recommendation:** Only make non-breaking changes. Document conventions for new code.

---

## Coding Conventions to Document

Add to `CONTRIBUTING.md` or similar:

```markdown
## Naming Conventions

### Fields
- Private fields: `_camelCase` with underscore prefix
- Protected backing fields: Consider using auto-properties instead
- Constants: `PascalCase`

### Properties
- All properties: `PascalCase`
- Protected properties for subclass access preferred over protected fields

### Event Handlers
- Private handlers: `Handle{EventName}`
- Protected virtual handlers: `On{EventName}`
- Async handlers: `{Name}Async` suffix

### Delegates
- Use `Func<>` or `Action<>` when possible
- Custom delegates: Descriptive name without suffix
```

---

## Files to Review

| File | Issues |
|------|--------|
| `src/Neatoo/Property.cs` | `_value` field naming |
| Various | Delegate naming review |
| Various | Event handler naming review |
