# H3: Add Generator Diagnostics

**Priority:** High
**Category:** Code Bug
**Effort:** Low
**Status:** Completed
**Completed:** 2025-12-31
**File:** `src/Neatoo.BaseGenerator/BaseGenerator.cs` lines 46-50

---

## Problem Statement

The source generator has an empty catch block that silently swallows exceptions:

```csharp
try
{
    // Generator logic...
}
catch (Exception)
{
    // Silent catch - no logging, no diagnostics
}
```

This makes debugging generator issues extremely difficult because:
1. Build succeeds even when generator fails
2. No indication of what went wrong
3. Generated code silently missing

---

## Current Code

```csharp
// BaseGenerator.cs around line 46-50
catch (Exception)
{
    // Empty - exceptions are silently ignored
}
```

---

## Proposed Fix

Report exceptions as compiler diagnostics:

```csharp
catch (Exception ex)
{
    // Report as a warning so builds don't fail but issue is visible
    context.ReportDiagnostic(Diagnostic.Create(
        new DiagnosticDescriptor(
            id: "NEATOO001",
            title: "Source Generator Error",
            messageFormat: "Neatoo source generator encountered an error: {0}",
            category: "Neatoo.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "An exception occurred during source generation. Check the exception message for details."),
        Location.None,
        ex.Message));

    // Optionally include stack trace in debug builds
    #if DEBUG
    context.ReportDiagnostic(Diagnostic.Create(
        new DiagnosticDescriptor(
            id: "NEATOO002",
            title: "Source Generator Stack Trace",
            messageFormat: "{0}",
            category: "Neatoo.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true),
        Location.None,
        ex.StackTrace));
    #endif
}
```

---

## Diagnostic IDs to Define

| ID | Severity | Description |
|----|----------|-------------|
| NEATOO001 | Warning | Generator exception occurred |
| NEATOO002 | Info | Stack trace (debug only) |
| NEATOO003 | Error | Critical generator failure |

---

## Implementation Tasks

- [x] Create diagnostic descriptor constants in a dedicated class
- [x] Replace empty catch with diagnostic reporting
- [x] Test that diagnostics appear in build output (builds succeed, diagnostics are reported as warnings)
- [ ] Test that diagnostics appear in IDE error list (requires manual testing in VS)
- [x] Add documentation for diagnostic codes (in GeneratorDiagnostics.cs XML comments)
- [x] Consider adding a `GeneratorDiagnostics.cs` file for all diagnostic definitions

---

## Diagnostic Descriptor Class

```csharp
// src/Neatoo.BaseGenerator/Diagnostics/GeneratorDiagnostics.cs
internal static class GeneratorDiagnostics
{
    public static readonly DiagnosticDescriptor GeneratorException = new(
        id: "NEATOO001",
        title: "Source Generator Error",
        messageFormat: "Neatoo source generator error: {0}",
        category: "Neatoo.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GeneratorStackTrace = new(
        id: "NEATOO002",
        title: "Source Generator Stack Trace",
        messageFormat: "{0}",
        category: "Neatoo.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}
```

---

## Testing

1. Intentionally cause a generator exception
2. Verify warning appears in build output
3. Verify warning appears in VS Error List
4. Verify build still succeeds (warning, not error)

---

## Files to Modify

| File | Action |
|------|--------|
| `src/Neatoo.BaseGenerator/BaseGenerator.cs` | Modify catch block |
| `src/Neatoo.BaseGenerator/Diagnostics/GeneratorDiagnostics.cs` | Create |
