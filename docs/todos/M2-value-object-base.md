# M2: Value Objects and Input Objects Documentation

**Priority:** Medium
**Category:** Documentation
**Effort:** Low
**Status:** Not Started

---

## Summary

Document the distinction between Value Objects (immutable C# records) and Input Objects (ValidateBase for user input). C# records provide all Value Object functionality natively; Neatoo provides ValidateBase for input capture and validation.

---

## Key Distinction

| Concern | Input Object (ValidateBase) | Value Object (Record) |
|---------|----------------------------|----------------------|
| Purpose | Capture user input | Store validated data |
| Mutable | Yes (user editing) | No (immutable) |
| Validation state | Yes (errors, INotifyDataErrorInfo) | No (always valid) |
| UI binding | Yes (INotifyPropertyChanged) | No |
| When valid | Can be invalid | Always valid |
| Neatoo base | `ValidateBase<T>` | None (C# record) |

---

## The Flow

```
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│   User Input    │  →   │  Input Object   │  →   │  Value Object   │
│   (Form/UI)     │      │  (ValidateBase) │      │  (Record)       │
└─────────────────┘      └─────────────────┘      └─────────────────┘
                         - Mutable                 - Immutable
                         - Shows errors            - Always valid
                         - Binds to UI             - Stored in Entity
```

1. User fills out form → bound to `ValidateBase` Input Object
2. Validation rules run as user types, errors displayed
3. When valid and user submits → convert to immutable Value Object (record)
4. Value Object stored in Entity
5. Entity saved to database

---

## Input Objects (ValidateBase)

Use `ValidateBase<T>` when capturing user input that requires:
- UI binding (form fields)
- Real-time validation feedback
- Error display

### Example: Address Input

```csharp
public class AddressInput : ValidateBase<AddressInput>
{
    public string Street { get => Getter<string>(); set => Setter(value); }
    public string City { get => Getter<string>(); set => Setter(value); }
    public string State { get => Getter<string>(); set => Setter(value); }
    public string ZipCode { get => Getter<string>(); set => Setter(value); }

    // Validation rules via attributes or RuleManager
    [Required]
    public string Street { get => Getter<string>(); set => Setter(value); }

    // Convert to Value Object when valid
    public Address ToValueObject() => new Address(Street, City, State, ZipCode);
}
```

---

## Value Objects (C# Records)

Use C# records for immutable data that:
- Is already validated
- Is stored within Entities
- Is fetched from database
- Needs value equality

**Requires RemoteFactory 10.1.1+** for `[Create]` on type syntax.

### What C# Records Provide

| Feature | C# Record Support |
|---------|-------------------|
| Value equality (`==`, `Equals`) | ✓ Built-in |
| `GetHashCode` based on values | ✓ Built-in |
| Immutability | ✓ `init` setters by default |
| Copy with changes | ✓ `with` expressions |

**No Neatoo base class needed.**

### Basic Syntax: `[Create]` on Type

RemoteFactory 10.1.1 allows `[Create]` directly on record type declarations:

```csharp
[Factory]
[Create]
public record Address(string Street, string City, string State, string ZipCode);

// Generated factory:
// IAddressFactory.Create(string Street, string City, string State, string ZipCode)
```

### Example: Money with Behavior

```csharp
[Factory]
[Create]
public record Money(decimal Amount, string Currency)
{
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return this with { Amount = Amount + other.Amount };
    }
}
```

### Example: Service Injection in Primary Constructor

```csharp
[Factory]
[Create]
public record AuditedValue(string Value, [Service] IAuditService Audit)
{
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}

// Generated factory:
// IAuditedValueFactory.Create(string Value) - IAuditService injected from DI
```

### Example: Fetch Operations on Records

```csharp
[Factory]
[Create]
public record Currency(string Code, string Name, string Symbol)
{
    public string Display => $"{Name} ({Symbol})";

    // Local fetch
    [Fetch]
    public static Currency FetchByCode(string code)
        => code switch
        {
            "USD" => new Currency("USD", "US Dollar", "$"),
            "EUR" => new Currency("EUR", "Euro", "€"),
            "GBP" => new Currency("GBP", "British Pound", "£"),
            _ => throw new ArgumentException($"Unknown currency: {code}")
        };

    // Remote fetch with service injection
    [Fetch]
    [Remote]
    public static async Task<Currency> FetchFromDatabaseAsync(
        string code,
        [Service] ICurrencyRepository repo)
    {
        return await repo.GetByCodeAsync(code);
    }
}

// Generated factory:
// ICurrencyFactory.Create(string Code, string Name, string Symbol)
// ICurrencyFactory.FetchByCode(string code)
// ICurrencyFactory.FetchFromDatabaseAsync(string code)
```

### Record Constraints

| Record Type | Supported? | Notes |
|-------------|------------|-------|
| Positional record | Yes | Use `[Create]` on type |
| Record with explicit constructor | Yes | Use `[Create]` on constructor |
| `record struct` | No | NF0206 error - value types not supported |
| Generic record | No | Not supported |
| Abstract record | No | Not supported |

---

## When to Use Each

| Scenario | Use |
|----------|-----|
| Form capturing user input | `ValidateBase<T>` (Input Object) |
| Storing validated data in Entity | C# record (Value Object) |
| Fetching lookup/reference data | C# record with `[Factory]` + `[Fetch]` |
| Search criteria / filters | `ValidateBase<T>` |
| Immutable domain concepts (Money, DateRange) | C# record |

---

## Complete Example: Address Workflow

### 1. Input Object (Form Binding)

```csharp
public class AddressInput : ValidateBase<AddressInput>
{
    [Required(ErrorMessage = "Street is required")]
    public string Street { get => Getter<string>(); set => Setter(value); }

    [Required(ErrorMessage = "City is required")]
    public string City { get => Getter<string>(); set => Setter(value); }

    [Required(ErrorMessage = "State is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Use 2-letter state code")]
    public string State { get => Getter<string>(); set => Setter(value); }

    [Required(ErrorMessage = "ZIP code is required")]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid ZIP code")]
    public string ZipCode { get => Getter<string>(); set => Setter(value); }

    public Address ToValueObject() => new Address(Street, City, State, ZipCode);
}
```

### 2. Value Object (Immutable Storage)

```csharp
[Factory]
[Create]
public record Address(string Street, string City, string State, string ZipCode)
{
    public string SingleLine => $"{Street}, {City}, {State} {ZipCode}";
}
```

### 3. Entity Using Value Object

```csharp
public class Customer : EntityBase<Customer>
{
    public string Name { get => Getter<string>(); set => Setter(value); }
    public Address ShippingAddress { get => Getter<Address>(); set => Setter(value); }

    public void UpdateAddress(AddressInput input)
    {
        if (!input.IsValid)
            throw new InvalidOperationException("Address input is not valid");
        ShippingAddress = input.ToValueObject();
    }
}
```

### 4. Blazor Form

```razor
<EditForm Model="@addressInput">
    <MudTextField @bind-Value="addressInput.Street" Label="Street" />
    <MudTextField @bind-Value="addressInput.City" Label="City" />
    <MudTextField @bind-Value="addressInput.State" Label="State" />
    <MudTextField @bind-Value="addressInput.ZipCode" Label="ZIP Code" />

    <MudButton OnClick="Save" Disabled="@(!addressInput.IsValid)">Save</MudButton>
</EditForm>

@code {
    private AddressInput addressInput = new();

    private void Save()
    {
        customer.UpdateAddress(addressInput);
    }
}
```

---

## Implementation Tasks

- [ ] Create `docs/value-objects.md` with this content
- [ ] Update `docs/aggregates-and-entities.md` to reference value objects doc
- [ ] Add example to samples folder

---

## Decided Against

| Proposal | Reason |
|----------|--------|
| `IValueObject` marker interface | No functional benefit; C# records are self-documenting |
| `ValueObject` base record | Validation belongs in Input Objects, not Value Objects |
| Analyzer rules | Over-engineering for documentation-only feature |
