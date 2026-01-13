# Neatoo Extensibility Principle

## Core Philosophy

**Every behavior in Neatoo should be extensible through dependency injection.**

Neatoo deliberately avoids:
- `internal` classes that hide implementation
- Static methods that can't be swapped
- Sealed classes that prevent inheritance
- Private logic that locks users out

## The "Internal" Namespace

The `Neatoo.Internal` namespace contains **public classes** that are:
- Default implementations of interfaces
- Registered in DI as the standard behavior
- Fully replaceable by users who register their own implementations

The namespace name means "implementation detail you probably don't need to touch" - not "hidden from you."

## Why This Matters

Other frameworks frustrate users when they hit edge cases:
- "I need to customize X, but it's internal"
- "I can't extend Y because it's sealed"
- "Z uses a static method I can't intercept"

Neatoo takes the opposite approach: if you need to customize something, you can. Register your own implementation and Neatoo will use it.

## Protected Methods: The Exception

Some methods are `protected` to prevent **bypassing invariants**, not to prevent extension:

| Method | Why Protected |
|--------|---------------|
| `MarkUnmodified()` | Prevents external code from hiding modifications |
| `MarkDeleted()` | Must go through `Delete()` for consistency |
| `MarkNew()` / `MarkOld()` | Factory operations manage persistence state |
| `MarkAsChild()` | List operations manage child status |

These are accessible to derived classes but not to external callers. The goal is preventing misuse, not preventing extension.

## Service Registration Pattern

All Neatoo services follow this pattern:

<!-- pseudo:service-registration-pattern -->
```csharp
// Interface defines the contract
public interface ISomeService<T> { }

// Default implementation in Internal namespace
namespace Neatoo.Internal
{
    public class SomeService<T> : ISomeService<T> { }
}

// Registered in DI with sensible defaults
services.AddTransient(typeof(ISomeService<>), typeof(SomeService<>));

// Users can replace with their own implementation
services.AddTransient(typeof(ISomeService<>), typeof(MyCustomService<>));
```
<!-- /snippet -->

## Extending Neatoo

To customize any behavior:

1. **Identify the interface** - Find `ISomething` in the public API
2. **Implement your version** - Create a class implementing that interface
3. **Register it** - Replace the default in DI before calling `AddNeatooServices` or after with your override
4. **Done** - Neatoo uses your implementation

## Trade-offs

| Benefit | Cost |
|---------|------|
| Maximum extensibility | Larger public API surface |
| No "you can't do that" walls | More interfaces to understand |
| Full testability | Users can break things if careless |
| Honest API - nothing hidden | Must maintain backward compatibility |

## Guidance for Contributors

When adding new functionality to Neatoo:

1. **Define an interface** for the behavior
2. **Create a default implementation** in `Neatoo.Internal`
3. **Register in DI** via `AddNeatooServices`
4. **Inject via services pattern** (e.g., `IValidateBaseServices<T>`)
5. **Document** what the service does and when users might customize it

Avoid:
- `internal` keyword
- `static` methods for swappable behavior
- `sealed` classes (unless there's a security reason)
- Logic that can't be intercepted or replaced
