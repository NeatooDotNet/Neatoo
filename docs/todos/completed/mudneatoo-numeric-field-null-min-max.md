# MudNeatooNumericField: Nullable Min/Max/Step Passed as default(T) to MudBlazor

- **Status:** Completed
- **Priority:** High
- **Created:** 2026-04-06
- **Completed:** 2026-04-13 (v0.29.0)

## Problem

`MudNeatooNumericField<T>` declares `Min`, `Max`, and `Step` as `T?` (nullable), defaulting to `null`. These are passed directly to MudBlazor's `MudNumericField<T>` which expects `T` (non-nullable). MudBlazor's own defaults are `T.MinValue` / `T.MaxValue` via `IMinMaxValue<T>`.

When the caller does not set `Min`/`Max`/`Step`, Blazor converts `null` to `default(T)`. For `int`, this means `Max=0` — which silently clamps every value to 0, making the field reset to 0 on any edit.

**File:** `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooNumericField.razor`

## Root Cause

```razor
Min="@Min"    <!-- Min is T? = null → passed as default(T) = 0 -->
Max="@Max"    <!-- Max is T? = null → passed as default(T) = 0 -->
Step="@Step"  <!-- Step is T? = null → passed as default(T) = 0 -->
```

MudBlazor's `MudNumericField` treats `Max=0` as "clamp to 0", not "no max set".

## Impact

Any `MudNeatooNumericField<int>` (or other numeric type) where the caller does not explicitly set `Max` will silently clamp all values to 0. This manifests as the field resetting to 0 when the user edits it.

Discovered in zTreatment's consultation plan panel — the field loaded correctly but reset to 0 on any user interaction. Took extensive debugging to isolate because the symptom (value goes to 0) appeared to be a rendering/re-render issue.

## Fix

Conditionally omit `Min`/`Max`/`Step` when they are null so MudBlazor uses its own defaults (`T.MinValue`/`T.MaxValue`). This likely requires a `@attributes` dictionary or splitting the render into conditional branches.

Also audit all other MudNeatoo components for the same pattern — any `T?` parameter passed to a MudBlazor `T` parameter has this bug.
