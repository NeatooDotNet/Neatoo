# RemoteFactory Record Support Update

**Date:** 2026-01-01
**RemoteFactory Version:** 10.1.1
**Commit:** `27760f8` - "10.1.1 - Record Support"
**Current Neatoo Version:** Uses RemoteFactory 10.0.1
**Breaking Changes:** No

---

## Summary

RemoteFactory 10.1.0 adds comprehensive C# record support, enabling records (particularly Value Objects) to work with the factory infrastructure including creation, fetching, service injection, and serialization round-trips through the client-server architecture.

---

## New Features in RemoteFactory 10.1.0

### 1. `[Create]` Attribute on Record Type Declarations

Records with primary constructors can now use `[Create]` directly on the type declaration instead of requiring an explicit constructor:

```csharp
// NEW - [Create] on type declaration
[Factory]
[Create]
public record Address(string Street, string City, string State, string ZipCode);

// Generated factory method:
// IAddressFactory.Create(string Street, string City, string State, string ZipCode)
```

**Previous approach (still supported):**
```csharp
[Factory]
public record Address
{
    public string Street { get; init; }
    public string City { get; init; }

    [Create]  // On explicit constructor
    public Address(string street, string city)
    {
        Street = street;
        City = city;
    }
}
```

### 2. Service Injection in Primary Constructor Parameters

The `[Service]` attribute works in positional record parameters:

```csharp
[Factory]
[Create]
public record RecordWithService(string Name, [Service] IMyService Service);

// Factory signature: IRecordWithServiceFactory.Create(string Name)
// Service is injected from DI, not passed as parameter
```

### 3. Fetch Operations on Records

Static `[Fetch]` methods work as expected with records:

```csharp
[Factory]
[Create]
public record Currency(string Code, string Name, string Symbol)
{
    [Fetch]
    public static Currency FetchByCode(string code)
        => new Currency(code, GetNameFor(code), GetSymbolFor(code));

    [Fetch]
    public static async Task<Currency> FetchByCodeAsync(
        string code,
        [Service] ICurrencyRepository repo)
    {
        return await repo.GetByCodeAsync(code);
    }
}
```

### 4. Remote Operations with Records

Records fully support `[Remote]` fetch operations with serialization round-trips:

```csharp
[Factory]
[Create]
public record RemoteRecord(string Name)
{
    [Fetch]
    [Remote]
    public static RemoteRecord FetchRemote(string name)
        => new RemoteRecord($"Remote-{name}");
}
```

### 5. New Diagnostics

| ID | Severity | Description |
|----|----------|-------------|
| NF0205 | Error | `[Create]` on type requires record with primary constructor |
| NF0206 | Error | `record struct` not supported (value types incompatible with RemoteFactory serialization) |

### 6. Serialization Support

Records serialize correctly through `NeatooJsonSerializer` including:
- All primitive types (string, int, long, double, decimal, bool, DateTime, Guid)
- Nullable properties
- Nested records
- Collections (List<T>, etc.)
- Value equality preserved after deserialization

---

## Record Constraints

| Record Type | Supported? | Notes |
|-------------|------------|-------|
| Positional record | Yes | Use `[Create]` on type |
| Record with explicit constructor | Yes | Use `[Create]` on constructor |
| `record struct` | No | NF0206 error - value types not supported |
| Generic record | No | Filtered by generator predicate |
| Abstract record | No | Filtered by generator predicate |
| Sealed record | Yes | Fully supported |

---

## What Neatoo Needs to Update

### 1. NuGet Package Version Update

**File:** `Directory.Packages.props`

Update RemoteFactory version from 10.0.1 to 10.1.1:

```xml
<PackageVersion Include="Neatoo.RemoteFactory" Version="10.1.1" />
```

### 2. CLAUDE.md Dependency Tracking

**File:** `CLAUDE.md`

Update the "Last Analyzed Commit" table:

```markdown
| Date | Commit | Description | Breaking? | Plan |
|------|--------|-------------|-----------|------|
| 2026-01-01 | `27760f8` | 10.1.0 - Record Support | No | `docs/todos/remotefactory-record-support-update.md` |
| 2025-12-31 | `b90ba4d` | Multi-target .NET 8.0, 9.0, 10.0 | No | N/A |
| 2025-12-30 | `9e62dda` | Remove Mapper Functionality | **YES** | `docs/todos/remotefactory-mapper-removal-plan.md` |
```

Update "Current Version":

```markdown
**Neatoo.RemoteFactory 10.1.0** (updated 2026-01-01)
```

### 3. Documentation Updates

**File:** `docs/todos/M2-value-object-base.md`

Update the Value Objects section to show the new `[Create]` on type syntax:

**Current example:**
```csharp
[Factory]
public partial record Address(string Street, string City, string State, string ZipCode);
```

**Updated example:**
```csharp
[Factory]
[Create]
public record Address(string Street, string City, string State, string ZipCode)
{
    public string SingleLine => $"{Street}, {City}, {State} {ZipCode}";
}
```

Also add examples of:
- Service injection in primary constructor
- Fetch operations on records
- Remote fetch operations

---

## Implementation Tasks

### Phase 1: Update Dependencies

- [ ] Update `Directory.Packages.props` to reference RemoteFactory 10.1.0
- [ ] Run `dotnet restore` to verify package resolves
- [ ] Run full test suite to verify no regressions

### Phase 2: Update CLAUDE.md

- [ ] Add new entry to "Last Analyzed Commit" table
- [ ] Update "Current Version" to 10.1.0
- [ ] Add "Record Support (10.1.0)" section to Breaking Change Notes (non-breaking feature)

### Phase 3: Documentation Updates

- [ ] Update `docs/todos/M2-value-object-base.md` with `[Create]` on type syntax
- [ ] Add service injection example to M2-value-object-base.md
- [ ] Add fetch operations example to M2-value-object-base.md
- [ ] Consider creating `docs/value-objects.md` as referenced in M2 tasks

### Phase 4: Optional - Add Neatoo Examples

- [ ] Consider adding record Value Object examples to integration tests
- [ ] Consider adding record examples to sample projects

---

## Example: Complete Value Object with RemoteFactory

```csharp
// Value Object with creation and fetching
[Factory]
[Create]
public record Currency(string Code, string Name, string Symbol)
{
    // Computed property
    public string Display => $"{Name} ({Symbol})";

    // Fetch by code (local)
    [Fetch]
    public static Currency FetchByCode(string code)
        => code switch
        {
            "USD" => new Currency("USD", "US Dollar", "$"),
            "EUR" => new Currency("EUR", "Euro", "E"),
            "GBP" => new Currency("GBP", "British Pound", "P"),
            _ => throw new ArgumentException($"Unknown currency: {code}")
        };

    // Async fetch from database (remote)
    [Fetch]
    [Remote]
    public static async Task<Currency> FetchFromDatabaseAsync(
        string code,
        [Service] ICurrencyRepository repo)
    {
        return await repo.GetByCodeAsync(code);
    }
}

// Generated factory interface:
public interface ICurrencyFactory
{
    Currency Create(string Code, string Name, string Symbol);
    Currency FetchByCode(string code);
    Task<Currency> FetchFromDatabaseAsync(string code);
}
```

---

## Testing Verified in RemoteFactory

The following test scenarios are verified by RemoteFactory's test suite:

### Unit Tests (RecordTests.cs)
- [x] Create positional record via primary constructor
- [x] Service injection in primary constructor
- [x] Sync and async fetch operations
- [x] Explicit constructor with `[Create]`
- [x] Sealed records
- [x] Records with default parameter values
- [x] Records with additional init properties
- [x] Nested records
- [x] Records with collections
- [x] Records with nullable properties
- [x] Complex records with multiple data types
- [x] Value equality verification

### Serialization Tests (RecordSerializationTests.cs)
- [x] Remote fetch serialization round-trip
- [x] Complex record serialization
- [x] Collection property serialization
- [x] Nullable property serialization
- [x] Nested record serialization
- [x] Value equality preserved after serialization

### Diagnostic Tests (RecordDiagnosticTests.cs)
- [x] NF0205 for `[Create]` on non-record class
- [x] NF0205 for `[Create]` on record without primary constructor
- [x] NF0206 for `record struct`
- [x] Valid scenarios produce no diagnostics

---

## Notes

1. **No code changes required in Neatoo** - This is purely a dependency update and documentation enhancement.

2. **Backward compatible** - Existing code using `[Create]` on explicit constructors continues to work.

3. **Recommended for Value Objects** - The `[Create]` on type syntax is cleaner and more idiomatic for Value Objects.

4. **`record struct` not supported** - Due to serialization and reference tracking limitations, only reference type records (`record` or `record class`) are supported. This aligns with DDD Value Object patterns where reference semantics are typically preferred.

---

## Related Documents

- `docs/todos/M2-value-object-base.md` - Value Objects documentation task
- `docs/todos/remotefactory-mapper-removal-plan.md` - Previous RemoteFactory update plan
