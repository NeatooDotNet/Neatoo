# AsyncTasks Design Rationale

## Overview

Document the design rationale behind the `AsyncTasks` class for advanced documentation. This is not an artificial complexity - it solves a real C# language constraint.

## The Core Problem: Property Setters Can't Be Async

In C#, property setters cannot be `async`. This is a language limitation:

```csharp
// This is NOT valid C#
public string FirstName
{
    get => _firstName;
    async set  // ERROR: Property setters cannot be async
    {
        _firstName = value;
        await ValidateAsync();
    }
}
```

But Neatoo needs to support async validation rules that:
- Check database for uniqueness
- Call external services
- Perform I/O operations

## The Fire-and-Forget Pattern

Neatoo's solution is the "fire-and-forget with rendezvous" pattern:

```csharp
// 1. Property setter looks synchronous
person.FirstName = "John";   // Triggers async UniqueNameRule internally
person.Email = "a@b.com";    // Triggers async EmailValidationRule

// 2. Rules run concurrently in background
// 3. Before checking validity or saving, await completion
await person.WaitForTasks();
Assert.IsTrue(person.IsValid);
```

## Why AsyncTasks Is Necessary

The `AsyncTasks` class tracks these "orphaned" async operations:

| Requirement | Why It's Needed |
|-------------|-----------------|
| Track dynamically-added tasks | Don't know upfront how many properties will change |
| Know when ALL are done | Including cascading rules that trigger more rules |
| Aggregate exceptions | Multiple rules might fail simultaneously |
| Provide `IsBusy` | UI needs to show loading state during validation |

## Alternatives Considered

### 1. Synchronous Validation Only
**Rejected**: Can't do database lookups without blocking UI thread.

### 2. Explicit Async Setters
```csharp
await person.SetFirstNameAsync("John");
```
**Rejected**: Awkward API, breaks data binding, not idiomatic C#.

### 3. Validate Only on Save
**Rejected**: Poor UX - users don't see validation errors until they click Save.

### 4. Polling on IsBusy
```csharp
while (IsBusy) await Task.Delay(10);
```
**Rejected**: Wasteful, timing issues, no exception aggregation.

### 5. External Library (Nito.AsyncEx, etc.)
**Rejected**: Adds dependency, still needs custom wrapper for exception aggregation.

## The Pattern Flow

```
┌─────────────────┐
│  Property Set   │  person.FirstName = "John"
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Setter Method  │  Returns Task from SetPrivateValue()
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  AsyncTasks     │  Tracks the task, sets IsBusy = true
│  .AddTask()     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Rules Execute  │  Async rules run in background
│  (background)   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  WaitForTasks() │  Caller awaits completion
│  .AllDone       │  IsBusy = false, exceptions thrown
└─────────────────┘
```

## Documentation Topics to Cover

1. **Why validation is async** - Database checks, external services
2. **The C# constraint** - Property setters can't be async
3. **The pattern** - Fire-and-forget with WaitForTasks rendezvous
4. **IsBusy binding** - How UI can show loading state
5. **Exception handling** - Aggregated exceptions from multiple rules
6. **Best practices** - Always call WaitForTasks before checking IsValid
7. **Thread safety** - AsyncTasks handles concurrent rule execution

## Code Reference

- `src/Neatoo/Internal/AsyncTasks.cs` - The implementation
- `src/Neatoo/Base.cs:269-287` - Setter method using AsyncTasks
- `src/Neatoo/Base.cs:359-381` - WaitForTasks implementation

## Key Insight for Documentation

Emphasize that this is **not artificial complexity**. It's a pragmatic solution to:
1. A real C# language constraint (async setters don't exist)
2. A real UX requirement (immediate validation feedback)
3. A real technical requirement (async database/service calls)

The ~130 lines of AsyncTasks code enable a clean API surface where:
- Property setters look normal
- Async validation "just works"
- Callers have a clean await point
