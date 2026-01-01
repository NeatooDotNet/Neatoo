# M2: Add Value Object Base Type

**Priority:** Medium
**Category:** Missing DDD Feature
**Effort:** Medium
**Status:** Not Started

---

## Problem Statement

Currently, Value Objects in Neatoo are simple POCOs with `[Factory]` attribute (handled by RemoteFactory). This has several issues:

1. No enforced immutability
2. No built-in equality by value semantics
3. Easy to accidentally mutate
4. No clear distinction from entities in the codebase

---

## Current Approach

```csharp
// Current: Just a POCO
[Factory]
public partial class Money
{
    public decimal Amount { get; set; }  // Mutable!
    public string Currency { get; set; }  // Mutable!
}
```

---

## Proposed Solution

Provide guidance and optional base types for Value Objects using C# records:

### Option 1: Documentation-Only (Minimal)

Just document the pattern:

```csharp
// Recommended pattern using C# records
[Factory]
public partial record Money(decimal Amount, string Currency);
```

Records provide:
- Immutability by default (init-only setters)
- Value equality
- `with` expressions for copying with changes

### Option 2: Marker Interface

```csharp
// Neatoo provides marker interface
public interface IValueObject { }

// Usage
[Factory]
public partial record Money(decimal Amount, string Currency) : IValueObject;
```

Benefits:
- Clear intent documentation
- Enables analyzer rules
- Repository can exclude value objects from tracking

### Option 3: Base Record with Validation

```csharp
// Neatoo provides base record
public abstract record ValueObject
{
    protected ValueObject()
    {
        Validate();
    }

    /// <summary>
    /// Override to add validation. Throw if invalid.
    /// </summary>
    protected virtual void Validate() { }
}

// Usage
[Factory]
public partial record Money : ValueObject
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    protected override void Validate()
    {
        if (Amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(Amount));
        if (string.IsNullOrEmpty(Currency))
            throw new ArgumentException("Currency is required", nameof(Currency));
    }
}
```

---

## Recommended Approach

**Option 2 (Marker Interface)** with documentation for the record pattern.

This provides:
1. Minimal framework footprint
2. Clear intent
3. Works with existing RemoteFactory patterns
4. Allows future analyzer development

---

## Implementation

### IValueObject Interface

```csharp
// src/Neatoo/IValueObject.cs
namespace Neatoo;

/// <summary>
/// Marker interface for Value Objects.
/// Value Objects should be immutable and compared by value, not identity.
/// Use C# records for automatic value equality.
/// </summary>
/// <example>
/// [Factory]
/// public partial record Address(string Street, string City, string ZipCode) : IValueObject;
/// </example>
public interface IValueObject { }
```

### Documentation

Create `docs/value-objects.md` covering:

1. What is a Value Object?
2. When to use Value Objects vs Entities
3. Recommended patterns (records, immutability)
4. Integration with RemoteFactory
5. Examples (Money, Address, DateRange)

---

## Examples to Include

### Simple Value Object

```csharp
[Factory]
public partial record EmailAddress(string Value) : IValueObject
{
    // Validation in factory method
    [Create]
    public static EmailAddress Create(string value)
    {
        if (!IsValidEmail(value))
            throw new ArgumentException("Invalid email format");
        return new EmailAddress(value);
    }

    private static bool IsValidEmail(string value) =>
        !string.IsNullOrEmpty(value) && value.Contains('@');
}
```

### Multi-Property Value Object

```csharp
[Factory]
public partial record Address(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country) : IValueObject;
```

### Value Object with Behavior

```csharp
[Factory]
public partial record Money(decimal Amount, string Currency) : IValueObject
{
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return this with { Amount = Amount + other.Amount };
    }

    public Money MultiplyBy(decimal factor) =>
        this with { Amount = Amount * factor };
}
```

---

## Implementation Tasks

- [ ] Create `IValueObject` marker interface
- [ ] Create `docs/value-objects.md` documentation
- [ ] Add examples to samples folder
- [ ] Consider analyzer for common mistakes (mutable properties, etc.)
- [ ] Update quick-start to mention value objects

---

## Future Considerations

1. **Analyzer rules:**
   - Warn if IValueObject has mutable properties
   - Suggest using record instead of class

2. **Serialization:**
   - Ensure records serialize correctly with System.Text.Json
   - Test with RemoteFactory

---

## Files to Create/Modify

| File | Action |
|------|--------|
| `src/Neatoo/IValueObject.cs` | Create marker interface |
| `docs/value-objects.md` | Create documentation |
| `samples/ValueObjects/` | Create examples |
