# Investigate Making ValidateBase<T>.Logger an ILogger<T>

**Status:** Complete
**Priority:** Low
**Created:** 2026-04-08
**Last Updated:** 2026-04-08


---

## Problem

`ValidateBase<T>.Logger` is currently `ILogger` (non-generic), always created with the category `"Neatoo.Trace"`. Consumers cannot use it for their own application-level logging because:

1. The category is hardcoded to `"Neatoo.Trace"` — all consumer log messages would appear under the framework's trace category, making filtering impossible
2. The type is `ILogger`, not `ILogger<T>` — consumers lose the standard .NET convention of type-categorized loggers

If the logger were `ILogger<T>` (where `T` is the consumer's entity type), consumers could use it naturally for domain logging (e.g., `Logger.LogInformation("Order {Id} validated", Id)`) and the log output would be categorized under their type (e.g., `MyApp.Order`).

### Current Implementation

- `IValidateBaseServices<T>.Logger` → `ILogger` (line 41 of `IValidateBaseServices.cs`)
- `ValidateBaseServices<T>.Logger` → set via `ILoggerFactory.CreateLogger("Neatoo.Trace")` (lines 26, 35, 45, 58, 72)
- `EntityBaseServices<T>` → same pattern, inherits from `ValidateBaseServices<T>` (lines 64, 95)
- `ValidateBase<T>.Logger` → `protected ILogger Logger => Services.Logger;` (line 195)

## Solution

Investigate whether the Logger can be changed to `ILogger<T>` so that:
- Consumers get a logger categorized to their entity type
- Framework trace logging uses a separate internal logger (not exposed to consumers)
- The DI registration continues to work with `ILoggerFactory`

### Key Questions to Resolve

1. **Separation of concerns** — Should there be two loggers? One internal `ILogger` for Neatoo trace (property changes, rule execution) and one `ILogger<T>` for consumer use? Or should the consumer logger replace the trace logger entirely?
2. **DI impact** — `ILoggerFactory.CreateLogger<T>()` requires knowing `T` at service construction time. The services already have `T` as a type parameter, so this should work. Verify.
3. **Breaking change assessment** — Changing `ILogger` to `ILogger<T>` on the interface is a breaking change for anyone implementing `IValidateBaseServices<T>` directly (unlikely but possible).
4. **Source generator impact** — Does the generated code reference the Logger? If so, does it need updating?
5. **Serialization/deserialization** — `NeatooBaseJsonTypeConverter` creates its own `"Neatoo.Trace"` logger. This is separate and should remain unchanged.

---

## Requirements Review

**Reviewer:** [pending]
**Reviewed:** [pending]
**Verdict:** Pending

### Relevant Requirements Found

[Pending]

### Gaps

[Pending]

### Contradictions

[Pending]

### Recommendations for Architect

[Pending]

---

## Plans

- [Change ValidateBase<T>.Logger from ILogger to ILogger<T>](../plans/validatebase-logger-ilogger-t.md)

---

## Tasks

- [x] Implementation
- [x] Developer code review — approved
- [x] Architect verification — VERIFIED

---

## Progress Log

### 2026-04-08
- Created todo to investigate `ILogger<T>` for consumer logging
- Documented current implementation: `ILogger` with hardcoded `"Neatoo.Trace"` category in `ValidateBaseServices<T>` and `EntityBaseServices<T>`
- Identified key questions around separation of trace vs consumer logging, DI impact, and breaking changes

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors
- Tests: 2140 passed, 0 failed, 2 skipped (pre-existing)

---

## Results / Conclusions

- **One `ILogger<T>` is sufficient** — no need for a separate framework trace logger
- Only 2 framework log calls used `Services.Logger` (both in `EntityBase.FactoryComplete`), and they use structured EventIds so category change doesn't affect filterability
- Serialization loggers (`NeatooBaseJsonTypeConverter`, `NeatooListBaseJsonTypeConverter`) create their own independent `"Neatoo.Trace"` loggers — unaffected
- Source generator doesn't reference Logger — no generator changes needed
- Changed 4 files: `IValidateBaseServices.cs`, `ValidateBaseServices.cs`, `EntityBaseServices.cs`, `ValidateBase.cs`
