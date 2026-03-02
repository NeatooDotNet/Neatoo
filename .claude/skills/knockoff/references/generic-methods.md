# Generic Methods Reference

Generic methods (methods with their own type parameters like `T Method<T>()`) use the `Of<T>()` pattern to configure and verify behavior per type argument.

---

## Of<T>() Pattern

Generic methods don't use `Return()` or `Call()` directly. You must first access a typed handler via `Of<T>()`:

```csharp
// WRONG: Cannot Return directly on a generic method handler
// stub.Convert.Return(42);

// CORRECT: Access typed handler first
stub.Convert.Of<int>().Call((value) => 42);
stub.Convert.Of<string>().Call((value) => "converted");
```

---

## Configuration

### Call(callback) -- Per-Type Behavior

<!-- snippet: generic-methods-return-constant -->
```cs
stub.GetById.Of<User>().Call((id) =>
    new User { Id = id, Name = "User" });
```
<!-- endSnippet -->

### Call(callback) -- Void Generic Methods

<!-- snippet: generic-methods-void -->
```cs
stub.Register.Of<string>().Call(() => called = true);

IGenericMixed service = stub;
service.Register<string>(); // called == true
```
<!-- endSnippet -->

---

## Multiple Type Parameters

Methods with multiple type parameters use `Of<T1, T2>()`:

<!-- snippet: generic-multi-param -->
```cs
// Configure for string -> int conversion
stub.Convert.Of<string, int>().Call((source) =>
    int.Parse(source));

// Configure for int -> string conversion
stub.Convert.Of<int, string>().Call((source) =>
    source.ToString());
```
<!-- endSnippet -->

---

## Verification

### Per-Type Verification

<!-- snippet: generic-methods-per-type-verify -->
```cs
stub.GetById.Of<User>().Verify(Called.Once);
stub.GetById.Of<Order>().Verify(Called.Once);
```
<!-- endSnippet -->

### Aggregate Verification (All Types)

<!-- snippet: generic-methods-aggregate-verify -->
```cs
stub.GetById.Verify(Called.Exactly(2)); // Total calls across ALL type arguments
```
<!-- endSnippet -->

---

## CalledTypeArguments

Track which type arguments were used at runtime:

<!-- snippet: generic-methods-called-types -->
```cs
var calledTypes = stub.GetById.CalledTypeArguments;
// calledTypes contains typeof(User) and typeof(string)
Assert.Equal(2, calledTypes.Count);
```
<!-- endSnippet -->

---

## Mixed Overloads (Generic + Non-Generic Same Name)

When a class has both a non-generic and generic method with the same name, they get **separate interceptors**:

<!-- snippet: generic-methods-mixed-overloads -->
```cs
// Given: void Process(string label) + void Process<T>(T item, string label)

// Non-generic: stub.Process
stub.Process.Call((label) => { });

// Generic: stub.ProcessGeneric.Of<T>()
stub.ProcessGeneric.Of<int>().Call((item, label) => { });
```
<!-- endSnippet -->

The generic overload gets a `Generic` suffix on the interceptor name.

---

## Reset

`Reset()` clears **all** typed handlers, call counts, and CalledTypeArguments:

<!-- snippet: generic-methods-reset -->
```cs
stub.GetById.Reset();

stub.GetById.Verify(Called.Never);
Assert.Empty(stub.GetById.CalledTypeArguments);
```
<!-- endSnippet -->

---

## Unconfigured Behavior

| Context | Behavior |
|---------|----------|
| Interface stub | Returns `default(T)` |
| Class stub (virtual) | Falls back to `base.Method<T>(args)` |
| Class stub (abstract) | Returns `default(T)` |
| Strict mode | Throws `StubException` |

---

## Stub Overrides NOT Supported

Generic methods are **excluded** from the stub override pattern (underscore-suffix base class). Use `Of<T>()` for all generic method configuration:

```csharp
// NO stub override for generic methods:
// protected override T Convert_<T>(object value) => ...  // NOT generated

// Use Of<T>() instead:
stub.Convert.Of<int>().Call((value) => 42);
```

---

## Quick Reference

| Task | Code |
|------|------|
| Configure return | `stub.Method.Of<T>().Call((args) => result)` |
| Configure void | `stub.Method.Of<T>().Call(() => { })` |
| Verify per-type | `stub.Method.Of<T>().Verify(Called.Once)` |
| Verify all types | `stub.Method.Verify(Called.Exactly(n))` |
| Track types used | `stub.Method.CalledTypeArguments` |
| Multi-type params | `stub.Method.Of<T1, T2>().Return(...)` |
| Reset all types | `stub.Method.Reset()` |
