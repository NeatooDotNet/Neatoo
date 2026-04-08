# Change ValidateBase<T>.Logger from ILogger to ILogger<T>

**Date:** 2026-04-08
**Related Todo:** [Investigate Making ValidateBase<T>.Logger an ILogger<T>](../todos/validatebase-logger-ilogger-t.md)
**Status:** Complete
**Last Updated:** 2026-04-08

---

## Overview

Change the `Logger` property exposed to consumers from `ILogger` (category `"Neatoo.Trace"`) to `ILogger<T>` (category = the consumer's entity type). This lets consumers use `Logger` for application-level logging with proper type-categorized output, replacing a framework-only trace logger they couldn't meaningfully use.

---

## Business Requirements Context

[Architect fills in Step 3]

**Source:** [Todo Requirements Review](../todos/validatebase-logger-ilogger-t.md#requirements-review)

### Relevant Existing Requirements

[Architect fills in Step 3]

### Gaps

[Architect fills in Step 3]

### Contradictions

[Architect fills in Step 3]

### Recommendations for Architect

[Architect fills in Step 3]

---

## Business Rules (Testable Assertions)

[Architect fills in Step 3]

### Test Scenarios

[Architect fills in Step 3]

---

## Approach

Single `ILogger<T>` replaces the current `ILogger`. No separate framework trace logger needed because:

1. **Only 2 framework log calls** use `Services.Logger` — both in `EntityBase.FactoryComplete` (lines 561, 587). These are `Trace` level with structured EventIds (10400/10401) and already include the type name in the message. Switching the logger category from `"Neatoo.Trace"` to the entity's type doesn't lose filterability — consumers can filter by EventId or LogLevel.

2. **Serialization logging is independent** — `NeatooBaseJsonTypeConverter` and `NeatooListBaseJsonTypeConverter` create their own `"Neatoo.Trace"` loggers via `ILoggerFactory.CreateLogger("Neatoo.Trace")`. These are unaffected.

3. **Source generator doesn't reference Logger** — no generator changes needed.

---

## Domain Model Behavioral Design

N/A — this is a framework infrastructure change, not a domain model change. No computed properties, visibility flags, reactive rules, or validation rules are affected.

---

## Design

### Interface Change

```csharp
// IValidateBaseServices.cs — change ILogger to ILogger<T>
public interface IValidateBaseServices<T> where T : ValidateBase<T>
{
    ILogger<T> Logger { get; }  // was: ILogger Logger { get; }
}
```

### Services Implementation Change

```csharp
// ValidateBaseServices.cs — change property type and creation
public ILogger<T> Logger { get; protected set; }

// In constructors without ILoggerFactory:
this.Logger = (ILogger<T>)NullLoggerFactory.Instance.CreateLogger<T>();

// In constructors with ILoggerFactory:
this.Logger = loggerFactory?.CreateLogger<T>()
    ?? (ILogger<T>)NullLoggerFactory.Instance.CreateLogger<T>();
```

`NullLoggerFactory.Instance.CreateLogger<T>()` returns `ILogger` — the cast to `ILogger<T>` is safe because `NullLogger<T>` implements `ILogger<T>` and the `LoggerFactory.CreateLogger<T>()` extension method returns `ILogger<T>`. We should use `new NullLogger<T>()` as the fallback instead of casting.

```csharp
// Correct fallback:
this.Logger = new NullLogger<T>();

// With factory:
this.Logger = loggerFactory?.CreateLogger<T>() ?? new NullLogger<T>();
```

### EntityBaseServices Change

Same pattern — inherits `Logger` from `ValidateBaseServices<T>`, just update the constructor assignments:

```csharp
// EntityBaseServices.cs constructors that set Logger:
this.Logger = loggerFactory.CreateLogger<T>();
// and
this.Logger = loggerFactory?.CreateLogger<T>() ?? new NullLogger<T>();
```

### Consumer-Facing Change

```csharp
// ValidateBase.cs — change return type
protected ILogger<T> Logger => Services.Logger;  // was: ILogger Logger
```

### EntityBase FactoryComplete — No Change Needed

The `NeatooTraceLog` extension methods are defined on `ILogger`, and `ILogger<T>` implements `ILogger`, so the existing calls compile without modification:

```csharp
Logger.FactoryCompleteStarted(opName, typeName, ...);  // still works
Logger.FactoryCompleteFinished(opName, typeName, ...);  // still works
```

---

## Implementation Steps

1. **Change `IValidateBaseServices<T>.Logger`** from `ILogger` to `ILogger<T>`
2. **Update `ValidateBaseServices<T>`** — change property type to `ILogger<T>`, update all constructor assignments to use `CreateLogger<T>()` / `new NullLogger<T>()`
3. **Update `EntityBaseServices<T>`** — update constructor assignments that set `this.Logger`
4. **Update `ValidateBase<T>.Logger`** — change return type from `ILogger` to `ILogger<T>`
5. **Build and fix any compilation errors** — the `NeatooTraceLog` extension methods on `ILogger` should still work since `ILogger<T> : ILogger`
6. **Run all tests**

---

## Acceptance Criteria

- [ ] `ValidateBase<T>.Logger` is `ILogger<T>` where `T` is the consumer's entity type
- [ ] Framework trace logging in `EntityBase.FactoryComplete` still works (extension methods on `ILogger`)
- [ ] Serialization logging in JSON converters is unaffected (they use their own logger)
- [ ] All existing tests pass without modification
- [ ] No source generator changes needed
- [ ] Consumers can call `Logger.LogInformation(...)` and see output under their entity type's category

---

## Dependencies

- `Microsoft.Extensions.Logging.Abstractions` — already referenced, provides `NullLogger<T>` and `ILogger<T>`

---

## Risks / Considerations

1. **Breaking change for `IValidateBaseServices<T>` implementors** — Anyone who directly implements this interface (rather than inheriting `ValidateBaseServices<T>`) will need to update their `Logger` property. This is unlikely since the interface is infrastructure, but it is technically a public API change. Bump minor version.
2. **Log category change** — Framework trace messages in `FactoryComplete` will now appear under the entity's type category instead of `"Neatoo.Trace"`. The EventIds (10400/10401) still uniquely identify them. Anyone filtering by category `"Neatoo.Trace"` for these two messages would need to adjust. Low risk since the trace log feature is new (v0.27.0).
3. **`NullLogger<T>` vs cast** — Must use `new NullLogger<T>()` not cast from `NullLoggerFactory.Instance.CreateLogger("Neatoo.Trace")` — the latter returns `ILogger`, not `ILogger<T>`.
