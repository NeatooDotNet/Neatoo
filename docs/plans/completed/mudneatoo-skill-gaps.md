# MudNeatoo Skill: Add Missing Namespace, ReadOnly, and View/Edit Documentation

**Date:** 2026-03-18
**Related Todo:** [MudNeatoo Skill Gaps](../todos/mudneatoo-skill-gaps.md)
**Status:** Complete
**Last Updated:** 2026-03-18 (documentation step complete)

---

## Overview

The `skills/mudneatoo/SKILL.md` is missing documentation that caused an agent to decompile the MudNeatoo DLL to discover basic usage information. This plan adds three documentation sections and one optional tip to close those gaps. This is a documentation-only change -- no source code modifications.

---

## Difficulty & Risk Assessment

**Difficulty:** Low
**Risk:** Low
**Justification:** All changes are additive documentation in a single markdown file. No source code, no tests, no build impact. The content is factual (verified from source code) and the insertion points are clear. The only risk is placing content in an awkward location within the existing skill structure, which is mitigated by the design below.

---

## Business Rules

These are content-correctness assertions for the skill documentation. WHEN an agent reads the skill, THEN it should be able to answer these questions without decompiling anything.

1. WHEN an agent needs to add `@using` directives for MudNeatoo, THEN the skill lists the three required namespaces: `Neatoo.Blazor.MudNeatoo.Components`, `Neatoo.Blazor.MudNeatoo.Validation`, and `Neatoo.Blazor.MudNeatoo.Extensions`.
2. WHEN an agent needs to know which namespace contains `MudNeatooTextField` and other input components, THEN the skill states `Neatoo.Blazor.MudNeatoo.Components`.
3. WHEN an agent needs to know which namespace contains `NeatooValidationSummary`, THEN the skill states `Neatoo.Blazor.MudNeatoo.Validation`.
4. WHEN an agent needs to know which namespace contains `EntityPropertyExtensions` (e.g., `GetValidationFunc<T>`), THEN the skill states `Neatoo.Blazor.MudNeatoo.Extensions`.
5. WHEN an agent asks whether `ReadOnly` can be overridden on a MudNeatoo component that wraps a MudBlazor input with a `ReadOnly` parameter, THEN the skill clearly states that `ReadOnly` is hardcoded to `EntityProperty.IsReadOnly` with no exposed `[Parameter]` -- it cannot be overridden. The skill also notes that `MudNeatooSlider` is an exception: MudBlazor's `MudSlider` has no `ReadOnly` parameter, so the slider uses only `Disabled="@EntityProperty.IsBusy"`.
6. WHEN an agent asks how `IsReadOnly` is determined, THEN the skill states it is set from `propertyInfo.IsPrivateSetter` -- a property with a `private set` or get-only (no setter) is read-only. The skill also notes that `internal set` is NOT treated as read-only (`PropertyInfoWrapper` checks `SetMethod?.IsPrivate`, and `internal` setters are not private).
7. WHEN an agent needs to build a page with view/edit mode switching, THEN the skill documents the pattern: use plain MudBlazor display components (`MudText`, `MudChip`, etc.) for view mode and MudNeatoo components for edit mode, controlled by conditional rendering (`@if (isEditing)`).
8. WHEN an agent looks at the "All MudBlazor parameters pass through" statement, THEN the skill clarifies that `ReadOnly` is the exception -- it does NOT pass through.

---

## Approach

Add four documentation additions to `skills/mudneatoo/SKILL.md`:

1. **"Namespaces & @using Directives" section** -- Insert immediately after the "Required package" line and before "The Core Pattern" section. This is the first thing an agent needs when setting up a page.

2. **ReadOnly clarification** -- Add a "ReadOnly Behavior" subsection within or immediately after the "IEntityProperty Metadata" section, where `IsReadOnly` is already listed. Clarify the hardcoded binding and private-setter rule. Also add a parenthetical note to the "All MudBlazor parameters pass through" line.

3. **"View/Edit Mode Pattern" section** -- Insert after "Display-Only Binding" section (which already shows read-only display). This is the natural place since view/edit mode is an extension of the display-only vs edit-mode concept.

4. **MudBlazor 9.x tip** -- Add a brief "MudBlazor 9.x Compatibility" note at the end, before the "Reference Documentation" section. This is optional context, not a MudNeatoo-specific issue.

---

## Design

### Section 1: Namespaces & @using Directives

Location: After `**Required package:** \`Neatoo.Blazor.MudNeatoo\`` (line 12), before `## The Core Pattern` (line 13).

Content:

```markdown
## Namespaces & @using Directives

Add these `@using` directives to your `_Imports.razor` or individual `.razor` files:

```razor
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using Neatoo.Blazor.MudNeatoo.Extensions
```

| Namespace | Contains |
|-----------|----------|
| `Neatoo.Blazor.MudNeatoo.Components` | All MudNeatoo input components (`MudNeatooTextField`, `MudNeatooSelect`, etc.) |
| `Neatoo.Blazor.MudNeatoo.Validation` | `NeatooValidationSummary` |
| `Neatoo.Blazor.MudNeatoo.Extensions` | `EntityPropertyExtensions` (`GetValidationFunc<T>`, `GetErrorText`, `HasErrors`) |
```

### Section 2: ReadOnly Behavior

Location: After the "IEntityProperty Metadata" table (after line 316), add a subsection.

Content:

```markdown
### ReadOnly Behavior

`ReadOnly` is **not** a pass-through parameter on MudNeatoo components that wrap a MudBlazor input with a `ReadOnly` parameter. These components hardcode `ReadOnly="@EntityProperty.IsReadOnly"` in their Razor templates. You cannot override it via a component parameter.

**Exception:** `MudNeatooSlider` does not bind `ReadOnly`. MudBlazor's `MudSlider` has no `ReadOnly` parameter -- the slider uses only `Disabled="@EntityProperty.IsBusy"`.

`IsReadOnly` is determined by `propertyInfo.IsPrivateSetter`:
- Property with `private set` or get-only (no setter) -> `IsReadOnly = true`
- Property with public setter -> `IsReadOnly = false`
- Property with `internal set` -> `IsReadOnly = false` (not treated as read-only; `PropertyInfoWrapper` checks `SetMethod?.IsPrivate`, and `internal` setters are not private)

This means read-only state is controlled entirely by the domain model's property declaration. To make a property read-only, declare it with a `private set` or as get-only.
```

Also update the "pass through" line (line 78):

From: `All MudBlazor parameters pass through -- \`Variant\`, \`Margin\`, ...`

To: `All MudBlazor parameters pass through -- \`Variant\`, \`Margin\`, ... -- **except \`ReadOnly\`** on components that wrap MudBlazor inputs with a \`ReadOnly\` parameter (hardcoded to \`EntityProperty.IsReadOnly\`; see ReadOnly Behavior below).`

### Section 3: View/Edit Mode Pattern

Location: After the "Display-Only Binding" section (after line 348), before "Reference Documentation".

Content:

```markdown
## View/Edit Mode Pattern

Since most MudNeatoo components bind `ReadOnly` from `IsReadOnly` automatically (with no override), the pattern for view/edit mode switching is conditional rendering -- not toggling a ReadOnly parameter.

Use plain MudBlazor display components in view mode and MudNeatoo components in edit mode:

```razor
@if (isEditing)
{
    <MudNeatooTextField T="string"
                        EntityProperty="@entity[nameof(IPatient.FirstName)]" />
    <MudNeatooTextField T="string"
                        EntityProperty="@entity[nameof(IPatient.LastName)]" />

    <MudButton OnClick="Save" Disabled="@(!entity.IsSavable)">Save</MudButton>
    <MudButton OnClick="CancelEdit" Variant="Variant.Text">Cancel</MudButton>
}
else
{
    <MudText Typo="Typo.body1">@entity.FirstName @entity.LastName</MudText>
    <MudButton OnClick="StartEdit" Variant="Variant.Text">Edit</MudButton>
}
```

This is intentional -- ReadOnly state belongs to the domain model (via private setters), not the UI. View/edit toggling is a UI concern handled with Razor conditionals.
```

### Section 4: MudBlazor 9.x Compatibility Note

Location: Before "## Reference Documentation" (the final section).

Content:

```markdown
## MudBlazor 9.x Compatibility

MudBlazor 9.x renamed `ShowMessageBox` to `ShowMessageBoxAsync` on `IDialogService`. If you use message box dialogs alongside MudNeatoo forms, update your calls accordingly. This is a MudBlazor API change, not a MudNeatoo change.
```

---

## Implementation Steps

1. Add "Namespaces & @using Directives" section after line 12 of SKILL.md
2. Update the "pass through" line (line 78) to note the ReadOnly exception
3. Add "ReadOnly Behavior" subsection after the IEntityProperty Metadata table
4. Add "View/Edit Mode Pattern" section after "Display-Only Binding"
5. Add "MudBlazor 9.x Compatibility" note before "Reference Documentation"

---

## Acceptance Criteria

- [ ] SKILL.md contains a "Namespaces & @using Directives" section listing all three namespaces with a table describing what each contains
- [ ] SKILL.md contains `@using` directive code block showing exactly: `Neatoo.Blazor.MudNeatoo.Components`, `Neatoo.Blazor.MudNeatoo.Validation`, `Neatoo.Blazor.MudNeatoo.Extensions`
- [ ] The "All MudBlazor parameters pass through" line explicitly notes that `ReadOnly` is the exception
- [ ] SKILL.md contains a "ReadOnly Behavior" subsection explaining hardcoded binding, private-setter rule, the `MudNeatooSlider` exception, and the `internal set` behavior
- [ ] SKILL.md contains a "View/Edit Mode Pattern" section with a conditional rendering example using `@if (isEditing)`
- [ ] SKILL.md contains a "MudBlazor 9.x Compatibility" note about `ShowMessageBoxAsync`
- [ ] No source code files were modified

---

## Risks / Considerations

- **Line numbers will shift**: The design references current line numbers; the implementer should use content matching (old_string/new_string), not line numbers.
- **Skill file is loaded by agents**: Any formatting issues (broken markdown, missing code fence closers) would affect agent behavior. The implementer should verify the final markdown renders correctly.
- **MudBlazor 9.x note scope**: The todo noted this is "not a MudNeatoo skill issue" but the user wants it as an optional tip. Keeping it brief and clearly labeled as a MudBlazor change (not MudNeatoo) is important.

---

## Developer Review

**Reviewed:** 2026-03-18
**Verdict:** Approved

### Re-Review Summary

Both concerns from the initial review were adequately addressed:

1. **MudNeatooSlider exception** -- The plan now uses "components that wrap a MudBlazor input with a `ReadOnly` parameter" instead of "every component" and explicitly documents the Slider exception. Verified against `MudNeatooSlider.razor` source.

2. **`internal set` behavior** -- The plan now includes a third bullet documenting that `internal set` yields `IsReadOnly = false` with the technical reason (`SetMethod?.IsPrivate` is false for internal setters). Verified against `PropertyInfoWrapper.cs` line 12.

### Why This Plan Is Approved

This is a documentation-only change to a single markdown file with exact content specified. All factual claims were verified against source code. No ambiguity in scope, design, or insertion points.

**Files examined:**
- `skills/mudneatoo/SKILL.md` -- current content, insertion points verified
- `src/Neatoo/Internal/PropertyInfoWrapper.cs` -- `IsPrivateSetter` logic confirmed
- `src/Neatoo/Internal/ValidateProperty.cs` -- `IsReadOnly` set from `IsPrivateSetter` confirmed
- `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooSlider.razor` -- no `ReadOnly` binding, only `Disabled` confirmed

---

## Implementation Contract

**Created:** 2026-03-18
**Approved by:** neatoo-developer

### In Scope

- [x] Add "Namespaces & @using Directives" section after the "Required package" line (line 11) and before "## The Core Pattern" (line 13) in `skills/mudneatoo/SKILL.md`
- [x] Update the "All MudBlazor parameters pass through" line (line 78) to note the `ReadOnly` exception
- [x] Add "ReadOnly Behavior" subsection after the IEntityProperty Metadata table (after line 316)
- [x] Add "View/Edit Mode Pattern" section after "Display-Only Binding" (after line 348)
- [x] Add "MudBlazor 9.x Compatibility" note before "## Reference Documentation" (line 350)
- [x] Verify final markdown renders correctly (no broken fences, no orphaned headings)

### Explicitly Out of Scope

- No source code modifications (`.cs`, `.csproj`, `.razor`, `.sln`)
- No test changes
- No changes to any file other than `skills/mudneatoo/SKILL.md`
- No changes to `docs/` user-facing documentation

### Stop Conditions

If any of these occur, STOP and report:
- Any impulse to modify source code files
- Discovery that SKILL.md structure has changed since plan creation (line numbers shifted significantly, sections renamed/removed)
- Content in the plan contradicts what source code actually does

---

## Agent IDs

- **Architect:** [agent ID from Step 2]
- **Developer:** [agent ID from Step 4]

---

## Implementation Progress

**Started:** 2026-03-18
**Developer:** neatoo-developer

- [x] Step 1: Added "Namespaces & @using Directives" section (lines 13-27) with @using code block and namespace table
- [x] Step 2: Updated "All MudBlazor parameters pass through" line (line 94) to note ReadOnly exception
- [x] Step 3: Added "ReadOnly Behavior" subsection (lines 354-365) under IEntityProperty Metadata, documenting hardcoded binding, Slider exception, and internal set behavior
- [x] Step 4: Added "View/Edit Mode Pattern" section (lines 379-403) with conditional rendering example
- [x] Step 5: Added "MudBlazor 9.x Compatibility" note (lines 405-407) before Reference Documentation
- [x] Step 6: Verified final markdown -- all code fences closed, section hierarchy correct, no orphaned headings

---

## Completion Evidence

**Reported:** 2026-03-18

- **Tests Passing:** N/A -- documentation-only change, no source code or tests modified
- **All Contract Items:** Complete (6/6 checked)
- **File Modified:** `skills/mudneatoo/SKILL.md` only (grew from 353 lines to 412 lines)
- **No Source Code Modified:** Confirmed -- no `.cs`, `.csproj`, `.razor`, `.sln`, or other source files touched

### Acceptance Criteria Verification

- [x] SKILL.md contains "Namespaces & @using Directives" section with table (lines 13-27)
- [x] SKILL.md contains `@using` code block with all three namespaces (lines 17-21)
- [x] "All MudBlazor parameters pass through" line notes ReadOnly exception (line 94)
- [x] SKILL.md contains "ReadOnly Behavior" subsection with hardcoded binding, private-setter rule, Slider exception, and `internal set` behavior (lines 354-365)
- [x] SKILL.md contains "View/Edit Mode Pattern" section with `@if (isEditing)` example (lines 379-403)
- [x] SKILL.md contains "MudBlazor 9.x Compatibility" note about `ShowMessageBoxAsync` (lines 405-407)
- [x] No source code files were modified

---

## Architect Verification

**Verified:** 2026-03-18
**Verdict:** VERIFIED

**Build/Test Results:**
- N/A -- documentation-only change, no source code modified

**Design Match:** Yes

All four documentation additions match the plan design exactly:

1. **Namespaces section (lines 13-27)** -- Lists three namespaces with `@using` code block and table. Namespace structure verified against `src/Neatoo.Blazor.MudNeatoo/` which has `Components/`, `Validation/`, and `Extensions/` directories. Extension method names (`GetValidationFunc<T>`, `GetErrorText`, `HasErrors`) verified in `EntityPropertyExtensions.cs`.

2. **ReadOnly exception on pass-through line (line 94)** -- Correctly notes ReadOnly is not a pass-through parameter on components wrapping MudBlazor inputs with a ReadOnly parameter.

3. **ReadOnly Behavior subsection (lines 354-365)** -- All claims verified against source:
   - Hardcoded `ReadOnly="@EntityProperty.IsReadOnly"` confirmed in 10 of 11 components (all except Slider)
   - MudNeatooSlider exception confirmed: uses only `Disabled="@EntityProperty.IsBusy"`, no ReadOnly binding
   - `IsPrivateSetter` logic at `PropertyInfoWrapper.cs:12`: `!propertyInfo.CanWrite || propertyInfo.SetMethod?.IsPrivate == true`
   - `IsReadOnly` assignment at `ValidateProperty.cs:22`: `this.IsReadOnly = propertyInfo.IsPrivateSetter`
   - `internal set` behavior correctly documented: `SetMethod?.IsPrivate` returns false for internal setters

4. **View/Edit Mode Pattern section (lines 379-403)** -- Conditional rendering pattern with `@if (isEditing)` using MudNeatoo components for edit mode and plain MudBlazor components for view mode.

5. **MudBlazor 9.x Compatibility note (lines 405-407)** -- Brief, correctly scoped as a MudBlazor change, not a MudNeatoo change.

**Markdown well-formedness:** 32 code fence markers (all paired), section hierarchy is clean, no orphaned headings.

**Source files examined for verification:**
- `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooTextField.razor` -- ReadOnly binding at line 11
- `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooNumericField.razor` -- ReadOnly binding at line 11
- `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooSelect.razor` -- ReadOnly binding at line 11
- `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooDatePicker.razor` -- ReadOnly binding at line 9
- `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooDateRangePicker.razor` -- ReadOnly binding at line 9
- `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooTimePicker.razor` -- ReadOnly binding at line 9
- `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooCheckBox.razor` -- ReadOnly binding at line 11
- `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooSwitch.razor` -- ReadOnly binding at line 11
- `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooRadioGroup.razor` -- ReadOnly binding at line 9
- `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooAutocomplete.razor` -- ReadOnly binding at line 13
- `src/Neatoo.Blazor.MudNeatoo/Components/MudNeatooSlider.razor` -- No ReadOnly binding, Disabled only at line 14
- `src/Neatoo.Blazor.MudNeatoo/Extensions/EntityPropertyExtensions.cs` -- GetValidationFunc<T>, GetErrorText, HasErrors methods confirmed
- `src/Neatoo.Blazor.MudNeatoo/Validation/NeatooValidationSummary.razor` -- exists in Validation namespace
- `src/Neatoo/Internal/PropertyInfoWrapper.cs` -- IsPrivateSetter logic at line 12
- `src/Neatoo/Internal/ValidateProperty.cs` -- IsReadOnly = propertyInfo.IsPrivateSetter at line 22

**Issues Found:** None

---

## Documentation

**Agent:** neatoo-developer
**Completed:** 2026-03-18

### Updates Made

- `skills/mudneatoo/SKILL.md`: All four documentation additions (namespaces, ReadOnly behavior, view/edit mode pattern, MudBlazor 9.x compatibility) were completed during implementation.
- `docs/todos/skill-documentation-gaps.md`: Checked off the "MudNeatoo Component Namespace Gap" item -- resolved by the Namespaces & @using Directives section added in this todo.

### Items Reviewed (No Update Needed)

- `docs/guides/blazor.md`: Has a "Read-Only Properties" section (lines 205-235) that describes general ReadOnly binding behavior. The additional detail about hardcoded binding, IsPrivateSetter mechanism, Slider exception, and internal set behavior is appropriate for the skill documentation (agent consumption) rather than the user-facing guide. No update needed.
- `docs/todos/mudblazor-skill-documentation-gaps.md`: Covers MudBlazor setup topics (Material Design Icons font, WASM vs Server, asset fingerprinting, index.html). No overlap with this todo's scope. No update needed.

### Manual Step Required

- **Copy skill to installed location:** The user must run `cp skills/mudneatoo/SKILL.md ~/.claude/skills/mudneatoo/SKILL.md` (and copy the `references/` directory if changed) to sync the installed skill. Per CLAUDE.md: "Edit skills here in `skills/mudneatoo/`, then copy to `~/.claude/skills/mudneatoo/`."
