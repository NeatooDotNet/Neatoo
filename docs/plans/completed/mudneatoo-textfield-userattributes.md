# MudNeatooTextField UserAttributes parameter

**Date:** 2026-04-13
**Related Todo:** [MudNeatooTextField: Add UserAttributes escape hatch](../../todos/completed/mudneatoo-textfield-userattributes.md)
**Status:** Complete
**Last Updated:** 2026-04-13

---

## Overview

Add a single `UserAttributes` pass-through parameter to `MudNeatooTextField<T>` so callers can set arbitrary HTML attributes (`spellcheck`, inline `style`, etc.) on the underlying `<input>`/`<textarea>`. No behavioral logic. No domain-model impact.

---

## Skills

- `skills/mudneatoo/SKILL.md` — documents the MudNeatoo component wrappers; needs a note on the new parameter and an example.

---

## Business Rules (Testable Assertions)

This change has no domain business rules. It is a pure UI pass-through of a MudBlazor component parameter. The only correctness criterion is mechanical:

1. WHEN a consumer sets `UserAttributes` on `MudNeatooTextField`, THEN those attributes reach the rendered native `<input>` or `<textarea>` — Source: NEW (follows existing pass-through parameter convention already used for `Variant`, `Margin`, `Lines`, `Sizing`, `MaxLines`, etc.)

### Test Scenarios

No new automated tests. Verification is by build + existing test suite staying green. Manual verification:

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Consumer sets spellcheck | `UserAttributes=@(new() { ["spellcheck"] = "true" })` on a multiline field | Rule 1 | Rendered `<textarea>` has `spellcheck="true"` attribute |
| 2 | Consumer sets inline style | `UserAttributes=@(new() { ["style"] = "resize: vertical;" })` on a multiline field with `Sizing=Fixed` | Rule 1 | Rendered `<textarea>` has `style="resize: vertical;"` — user gets drag handle |
| 3 | No UserAttributes set | Parameter omitted entirely | Rule 1 | No change in rendered output vs. 0.29.0 — backward compatible |

Scenarios 1 and 2 are verified by the consuming application (zTreatment), not by unit tests in this repo.

---

## Approach

One new `[Parameter]` on `MudNeatooTextField<T>`: `Dictionary<string, object>? UserAttributes`. Forwarded to the child `MudTextField` via `UserAttributes="@UserAttributes"`. MudBlazor's `MudInput` already spreads `UserAttributes` onto the native element — no additional plumbing required.

This matches the shape of `UserAttributes` on `MudComponentBase` (`IReadOnlyDictionary<string, object?>` in source, but the public surface uses `Dictionary<string, object>`). We mirror the MudBlazor public type exactly to avoid surprising consumers.

---

## Domain Model Behavioral Design

Not applicable — this is a UI-layer pass-through parameter with no domain semantics.

---

## Design

### Files changed

| File | Change |
|------|--------|
| `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooTextField.razor.cs` | Add `[Parameter] public Dictionary<string, object>? UserAttributes { get; set; }` with XML summary |
| `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooTextField.razor` | Add `UserAttributes="@UserAttributes"` attribute on the `<MudTextField>` element |
| `Directory.Build.props` | `<Version>` 0.29.0 → 0.30.0 |
| `docs/release-notes/v0.30.0.md` | New release notes file following the v0.29.0 template |
| `skills/mudneatoo/SKILL.md` | Add `UserAttributes` to the component's parameter reference and an example showing `spellcheck` + `resize: vertical` |

### MudBlazor verification summary

- Installed `MudBlazor 9.0.0` XML docs: `MudComponentBase.UserAttributes` present; no `Spellcheck` property on `MudTextField`/`MudInput`.
- Current `MudBlazor/dev` source (`MudInput.razor`): native `<input>` and `<textarea>` both receive `@attributes="UserAttributes"`. Confirms the pass-through reaches the element.

### Parameter type choice: `Dictionary<string, object>` vs `IReadOnlyDictionary<string, object?>`

Use `Dictionary<string, object>` to match MudBlazor's public `UserAttributes` signature (`MudComponentBase.UserAttributes` is typed as `Dictionary<string, object>`). A more defensive `IReadOnlyDictionary<string, object?>` would require a cast or copy on forwarding and would diverge from every example in MudBlazor's own docs. Keep it identical to MudBlazor.

Default value: `null` (not an empty dictionary). Matches MudBlazor's default and avoids allocating an empty dictionary for every field.

---

## Implementation Steps

1. Edit `MudNeatooTextField.razor.cs`: add `[Parameter] public Dictionary<string, object>? UserAttributes { get; set; }` with an XML summary describing the escape-hatch semantics, mentioning spellcheck and inline style as example use cases.
2. Edit `MudNeatooTextField.razor`: add `UserAttributes="@UserAttributes"` on the `<MudTextField>` element. Keep alphabetical ordering unchanged from current file (append before `Class="@Class"` to stay at bottom, or place after `AdornmentColor`; pick whichever keeps the diff minimal).
3. Bump `<Version>` in `Directory.Build.props` from `0.29.0` to `0.30.0`.
4. Create `docs/release-notes/v0.30.0.md` following v0.29.0's structure.
5. Update `skills/mudneatoo/SKILL.md` to document `UserAttributes` and include an example combining `Sizing="InputSizing.Auto"`, `spellcheck`, and `style="resize: vertical"`.
6. `dotnet build src/Neatoo.sln`.
7. `dotnet test src/Neatoo.sln`.
8. Invoke `neatoo-developer` agent for code review (Step 5).
9. Invoke `business-requirements-documenter` agent to review skill and release-notes changes (Step 7).
10. On green review, mark todo Complete and move to `completed/`.

---

## Acceptance Criteria

- [ ] `UserAttributes` compiles and is forwarded to `MudTextField` in the `.razor` file.
- [ ] Version bumped to 0.30.0 in `Directory.Build.props`.
- [ ] Release notes `v0.30.0.md` exists and follows the established format.
- [ ] `skills/mudneatoo/SKILL.md` mentions `UserAttributes` with a working example.
- [ ] `dotnet build src/Neatoo.sln` succeeds (zero errors, zero warnings given `TreatWarningsAsErrors=true`).
- [ ] `dotnet test src/Neatoo.sln` passes — same count as before this change.
- [ ] Developer agent approves.
- [ ] Documenter agent approves skill + release-notes content.

---

## Dependencies

None. Pure additive change.

---

## Risks / Considerations

- **Nullability annotations.** `Dictionary<string, object>?` (not `Dictionary<string, object?>`). Matches MudBlazor's declaration; changing it would produce a compiler warning at the forwarding site.
- **No test coverage.** This is a thin pass-through and the project has no Blazor component test harness. Accepted risk: the downstream consuming application (zTreatment) will exercise it in its existing form pages.
- **Naming conflict with Razor attribute splatting.** `@attributes` and `UserAttributes` are distinct in MudBlazor's model. We are forwarding as a regular `[Parameter]`, not splatting — no conflict.
- **Skill example placement.** The skill already has several pass-through parameter examples; add the new example near the existing `Sizing`/`MaxLines` reference rather than creating a new section.
