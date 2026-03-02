# Stub Overrides Reference

Stub overrides are `protected override` methods and properties you define in a standalone stub's partial class. They provide reusable default behavior that can be superseded per-test with `Return()`/`Call()`/`Get()`/`Set()`.

**Standalone patterns only.** Inline stubs (patterns 5-9) generate the entire class -- no partial available for overrides.

---

## Base Class Pattern

KnockOff generates a base class with `virtual protected` members suffixed with underscore (`_`). You override these to provide defaults:

<!-- snippet: stub-overrides-basic -->
```cs
[KnockOff]
public partial class StubOverridesRepoStub : IStubOverridesRepo { }

// Stub overrides provide default behavior
public partial class StubOverridesRepoStub
{
    // Protected override method with underscore suffix
    // This is the fallback when no Return is configured
    protected override User? GetUserById_(int id)
    {
        return new User { Id = id, Name = "Default User" };
    }

    protected override bool IsActive_(int userId)
    {
        return true; // Default: users are active
    }

    protected override decimal GetBalance_(int userId)
    {
        return 100.00m; // Default test balance
    }
}
```
<!-- endSnippet -->

The compiler enforces signature correctness -- typos or wrong parameter types produce CS0115 errors.

---

## Method Stub Overrides

### Return/Call Supersedes

`Return()` and `Call()` take priority over stub overrides per-test:

<!-- snippet: stub-overrides-ref-return-supersedes -->
```cs
// Default behavior from override
service.GetUserById(1); // "Default User"

// Supersede with Return for this test
stub.GetUserById.Call(id => new User { Id = id, Name = "Override" });
service.GetUserById(1); // "Override"
```
<!-- endSnippet -->

### When Chains

Stub override stubs support the full When chain API:

<!-- snippet: stub-overrides-ref-when -->
```cs
stub.GetUserById.When(42).Return(new User { Id = 42, Name = "SPECIAL" });

service.GetUserById(42); // "SPECIAL" (When matched)
service.GetUserById(1);  // "Default User" (stub override)
```
<!-- endSnippet -->

Priority: When > Sequences > Return/Call > **Stub Override**

---

## Mixed Stubs

Override some methods, configure others:

<!-- snippet: stub-overrides-ref-mixed -->
```cs
// Methods WITH override use it as default
service.WithOverride("test");    // "[User: test]"

// Methods WITHOUT override need configuration or return default
stub.WithoutOverride.Call((input) => $"[Configured: {input}]");
service.WithoutOverride("test"); // "[Configured: test]"
```
<!-- endSnippet -->

---

## Overloaded Methods

Each overload gets its own virtual method in the base class:

<!-- snippet: stub-overrides-overloads -->
```cs
public partial class StubOverrideFormatterStub
{
    // Override only the overloads you need
    protected override string Format_(string input) => input.ToUpperInvariant();

    // Override other overloads with custom logic
    protected override string Format_(string input, bool uppercase)
        => uppercase ? input.ToUpperInvariant() : input.ToLowerInvariant();
}
```
<!-- endSnippet -->

Override only some overloads -- unoverridden ones use the interceptor path (Return/default).

---

## Strict Mode

Stub overrides **bypass strict mode** -- they ARE the configuration:

<!-- snippet: stub-overrides-ref-strict -->
```cs
var stub = new StubOverridesRepoStub();
stub.Strict = true;

IStubOverridesRepo service = stub;

// Stub overrides bypass strict mode -- they ARE the configuration
var user = service.GetUserById(1); // Works -- override IS the config
Assert.Equal("Default User", user!.Name);
```
<!-- endSnippet -->

---

## Applicable Patterns

| Pattern | Stub Overrides? |
|---------|:-:|
| 1. Standalone | Yes |
| 2. Generic Standalone | Yes |
| 3. Standalone Class | Yes |
| 4. Generic Standalone Class | Yes |
| 5-9. Inline patterns | No |

---

## Not Supported

- **Generic methods** -- excluded from base class pattern. Use `Of<T>()` instead.
- **Inline stubs** -- entire class is generated, no partial for overrides.
- **Indexer overrides** -- see separate design (not yet supported).

---

## Reset Behavior

`Reset()` clears tracking but **preserves** Return/Call/Get/Set configuration:

<!-- snippet: stub-overrides-reset -->
```cs
// Reset clears tracking state but preserves Return configuration
stub.GetBalance.Reset();
stub.GetBalance.Verify(Called.Never);
```
<!-- endSnippet -->

---

## Quick Reference

| Task | Code |
|------|------|
| Define override | `protected override string Process_(string input) => result;` |
| Supersede with Return | `stub.Process.Return(value)` |
| Supersede with callback | `stub.Process.Call((input) => result)` |
| When chain override | `stub.Process.When("x").Return("y")` |
| Strict mode bypass | Override IS the configuration |
| Reset | `stub.Process.Reset()` clears counts, preserves config |
