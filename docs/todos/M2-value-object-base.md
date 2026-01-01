# M2: Value Object Documentation

**Priority:** Low
**Category:** Documentation
**Effort:** Low
**Status:** Not Started

---

## Summary

C# records provide all Value Object functionality natively. Neatoo only needs documentation showing the recommended pattern with RemoteFactory.

---

## What C# Records Provide

| Feature | C# Record Support |
|---------|-------------------|
| Value equality (`==`, `Equals`) | ✓ Built-in |
| `GetHashCode` based on values | ✓ Built-in |
| Immutability | ✓ `init` setters by default |
| Copy with changes | ✓ `with` expressions |
| Deconstruction | ✓ Built-in |

**No Neatoo base class or marker interface needed.**

---

## Recommended Pattern

```csharp
[Factory]
public partial record Money(decimal Amount, string Currency);
```

### With Validation

```csharp
[Factory]
public partial record EmailAddress
{
    public string Value { get; init; }

    [Create]
    public static EmailAddress Create(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains('@'))
            throw new ArgumentException("Invalid email format", nameof(value));
        return new EmailAddress { Value = value };
    }
}
```

### With Behavior

```csharp
[Factory]
public partial record Money(decimal Amount, string Currency)
{
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return this with { Amount = Amount + other.Amount };
    }
}
```

---

## Implementation Tasks

- [ ] Create `docs/value-objects.md` with pattern examples
- [ ] Update quick-start to mention value objects

---

## Decided Against

| Proposal | Reason |
|----------|--------|
| `IValueObject` marker interface | No functional benefit; C# records are self-documenting |
| `ValueObject` base record with `Validate()` | Validation belongs in `[Create]` factory methods |
| Analyzer rules | Over-engineering; records already enforce immutability |

---

## Previous Analysis

Originally proposed adding base types and marker interfaces. After review, determined that C# records (available since C# 9/.NET 5) provide complete Value Object semantics. Neatoo's role is documentation only.
