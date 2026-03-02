# Source Delegation Reference

Source delegation lets you delegate unconfigured calls to a real implementation. This enables partial stubbing -- configure specific methods while the rest fall through to the real object.

---

## Basic Usage

<!-- snippet: source-delegation-ref-basic -->
```cs
var stub = new SourceCalcStub();
var realCalculator = new RealCalc();

stub.Source(realCalculator);

ISourceCalc calc = stub;

// No methods configured -- all delegate to source
var r1 = calc.Add(2, 3);      // Returns 5 (from real implementation)
var r2 = calc.Subtract(10, 4); // Returns 6 (from real implementation)
```
<!-- endSnippet -->

---

## Partial Stubbing

Configure specific methods while delegating the rest:

<!-- snippet: source-delegation-ref-partial -->
```cs
var stub = new SourceCalcStub();
stub.Source(new RealCalc());

// Override just one method
stub.Add.Return(999);

ISourceCalc calc = stub;
calc.Add(2, 3);      // 999 (stub configuration wins)
calc.Subtract(10, 4); // 6 (delegates to source)
```
<!-- endSnippet -->

---

## Priority Chain

Source delegation sits below configuration but above defaults:

1. **When chains** (highest)
2. **Sequences**
3. **Return / Call** configuration
4. **Stub overrides** (standalone patterns only)
5. **Source delegation**
6. **Default value** (lowest) / StubException in strict mode

<!-- snippet: source-delegation-ref-priority -->
```cs
stub.Source(realCalculator);
stub.Divide.When(10, 2).Return(5);

ISourceCalc calc = stub;
calc.Divide(10, 2);  // 5 (When chain matched)
calc.Divide(20, 4);  // 5 (falls to source -- real implementation)
```
<!-- endSnippet -->

---

## Source(null) -- Remove Delegation

Pass `null` to remove the source:

<!-- snippet: source-delegation-ref-null -->
```cs
stub.Source(realCalculator);
ISourceCalc calc = stub;
calc.Add(2, 3); // 5 (from source)

stub.Source(null);
calc.Add(2, 3); // 0 (default -- no source, no configuration)
```
<!-- endSnippet -->

---

## Interface Hierarchy Support

When stubbing an interface that extends other interfaces, KnockOff generates **separate `Source()` overloads** for each interface in the hierarchy.

### Source(IStore) -- Full Delegation

```csharp
// Given: IStore : IReadableStore
var fullImpl = new InMemoryStore(); // implements IStore
stub.Source(fullImpl);
// ALL methods delegate
```

### Source(IReadableStore) -- Partial Delegation

```csharp
var readOnly = new ReadOnlyStore(); // implements IReadableStore only
stub.Source(readOnly);
// Only IReadableStore members delegate
// IStore-only members return defaults
```

### Partial Source + Configuration

<!-- snippet: source-partial-override -->
```cs
// Override specific member while source handles the rest
stub.GetById.Call((id) => new User { Id = id, Name = "Test User" });
```
<!-- endSnippet -->

---

## Reset Clears Source

`Reset()` on an interceptor clears its source reference:

<!-- snippet: source-clear -->
```cs
// Clear source to revert to smart defaults
stub.Source(null);
```
<!-- endSnippet -->

---

## Interface Stubs Only

Source delegation is **only available for interface stubs**. Class stubs (`[KnockOffBase<T>]`) do not have a `Source()` method because:

- The stub IS-A the class (inheritance, not delegation)
- Non-virtual members come from the base class directly
- Only virtual/abstract members are interceptable

---

## Quick Reference

| Task | Code |
|------|------|
| Set source | `stub.Source(implementation)` |
| Remove source | `stub.Source(null)` |
| Partial stub | `stub.Source(impl)` then `stub.Method.Return(value)` |
| Hierarchy partial | `stub.Source(baseInterfaceImpl)` |
| Reset clears source | `stub.Method.Reset()` removes source for that member |
