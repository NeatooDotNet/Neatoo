# MudNeatooTextField: Add UserAttributes escape hatch

**Status:** Complete
**Priority:** Medium
**Created:** 2026-04-13
**Last Updated:** 2026-04-13

---

## Problem

After the 0.29.0 upgrade the user-facing textarea still lacks two browser-level behaviors:

- **Spellcheck**: no way to set `spellcheck="true"` on the underlying `<textarea>`.
- **User drag-handle resize**: no way to apply `style="resize: vertical"` on the underlying `<textarea>`.

Initial hypothesis was that `MudTextField` exposed a `Spellcheck` parameter we were failing to forward. Verified against MudBlazor 9.0.0 installed package XML docs and current MudBlazor source (`MudTextField.razor`, `MudInput.razor`): **no `Spellcheck` parameter exists on either component.** However, `MudInput` does spread `@attributes="UserAttributes"` onto the native `<input>` and `<textarea>`, and `UserAttributes` is inherited from `MudComponentBase`.

So the real gap is: `MudNeatooTextField` does not forward `UserAttributes` to `MudTextField`, leaving consumers with no escape hatch for native HTML attributes or inline style.

## Solution

Add a single `UserAttributes` parameter to `MudNeatooTextField<T>` and forward it to `MudTextField`. This one parameter covers both requested use cases:

- `spellcheck` → `UserAttributes["spellcheck"] = "true"`
- drag-handle resize → `UserAttributes["style"] = "resize: vertical;"`

Release as 0.30.0 (minor — new parameter, no breaking changes).

---

## Requirements Review

**Verdict:** SKIPPED (per user — small API-surface addition, no business-rule impact)
**Reviewed:** —
**Summary:** —

---

## Plans

- [MudNeatooTextField UserAttributes parameter](../../plans/completed/mudneatoo-textfield-userattributes.md)

---

## Tasks

- [x] Create todo and plan
- [x] Implement `UserAttributes` parameter in `MudNeatooTextField.razor.cs` and `.razor`
- [x] Bump version to 0.30.0 in `Directory.Build.props`
- [x] Write release notes `docs/release-notes/v0.30.0.md`
- [x] Update `skills/mudneatoo/SKILL.md` with the new parameter and example
- [x] Build and run tests
- [x] Developer agent code review (Step 5) — Approved
- [x] Documenter agent review of skill + release notes (Step 7) — Approved

---

## Progress Log

### 2026-04-13
- Verified MudBlazor 9.0.0 has no `Spellcheck` parameter on `MudTextField` or `MudInput`.
- Verified `MudInput.razor` spreads `@attributes="UserAttributes"` onto the native `<textarea>`/`<input>`.
- Chose option 1: single `UserAttributes` parameter as the escape hatch.
- User opted to skip requirements review and architect validation — scope is a single pass-through parameter with no domain impact.
- Next: implement, then run developer + documenter agents.

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors, 474 pre-existing warnings (all in test projects).
- Tests: 2144 passed, 2 skipped (pre-existing), 0 failed.

---

## Results / Conclusions

- **MudBlazor fact established**: `MudTextField` and `MudInput` have no typed `Spellcheck` parameter in 9.0.0 or current `dev`. `UserAttributes` (inherited from `MudComponentBase`) is the supported mechanism — MudBlazor spreads it onto the native `<input>`/`<textarea>` via `@attributes="UserAttributes"`.
- **Single-parameter solution**: One `UserAttributes` pass-through handles both of the user's requested behaviors (spellcheck, drag-handle resize). No need for dedicated `Spellcheck` / `InputStyle` convenience wrappers.
- **Signature mirrors MudBlazor**: `Dictionary<string, object>?` default `null` matches `MudComponentBase.UserAttributes` exactly, so forwarding is one line with no conversion.
- **Opportunistic skill backfill**: `Lines`/`Sizing`/`MaxLines` from 0.29.0 had never been documented in `skills/mudneatoo/SKILL.md`. Added a "Multi-line text" section alongside the new `UserAttributes` section.
