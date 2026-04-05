# Requirements Documenter -- Rule ID Collision Fix

Last updated: 2026-04-04
Current step: Complete -- documentation updated, developer deliverables identified

## Key Context

Bug fix: `RuleIdGenerator` generated sequential ordinals (`1u, 2u, 3u...`) starting at `1u` per class. Derived classes collided with base class rule IDs. Fix replaced ordinals with FNV-1a hash-based IDs (`0xHHHHHHHHu`). The todo recommended Option A (offset ordinals) but Option B (hash-based IDs) was implemented -- cleaner, no cross-class analysis needed.

This is a patch-level bug fix (generator internals). No new API surface. No new behavioral contracts. The fix changes the internal ID scheme from ordinals to hashes.

## Mistakes to Avoid

- Do not rewrite v0.7.0 release notes history -- add a forward-reference note instead
- The CLAUDE-DESIGN.md line about `Interlocked.Increment` is about execution IDs, not source-generated rule IDs -- different concept, do not touch

## User Corrections

None.

## Documentation Tracking

### Markdown Files Updated

**Todo management:**
1. `docs/todos/rule-id-collision-in-inheritance.md` -- Updated status to Complete, added Completed date, added Resolution section documenting Option B, marked tasks done. Moved to `docs/todos/completed/`.
2. `docs/todos/index.md` -- Removed from Bugs table, added to Completed list.

**User-facing docs:**
3. `docs/release-notes/v0.7.0.md` -- Added "Subsequent Fix: Hash-Based IDs" section noting the collision bug and its resolution.
4. `docs/reference/api.md` -- Updated "RuleIdRegistry Generation" heading to "Rule ID Generation", replaced ordinal language with FNV-1a hash description, updated explanation paragraph.
5. `docs/guides/business-rules.md` -- Updated "Advanced: Stable Rule IDs" section to mention FNV-1a hashes and inheritance collision resistance.

**Skill behavioral contract refs:**
- No changes needed. `references/source-generation.md` and `references/validation.md` do not mention rule IDs or ordinals. The hash-based scheme is an internal implementation detail that doesn't change behavioral contracts visible to skill consumers.

### Rules Summary

- New rules added: 0 (this was a bug fix, not a new behavioral contract)
- Existing rules updated: 1 (rule ID generation scheme changed from ordinals to hashes)
- Outdated rules reconciled: 0

## Developer Deliverables

### 1. Source Generator Comment (RuleIdGenerator.cs)

**File:** `src/Neatoo.BaseGenerator/Generators/RuleIdGenerator.cs`
**Line 23:** XML comment says "Maps source expressions to deterministic ordinal IDs" -- should say "Maps source expressions to deterministic FNV-1a hash IDs"
**What to change:** Update the XML summary comment from "ordinal IDs" to "hash IDs"

### 2. Design.Domain FluentRules.cs Comment Accuracy Check

**File:** `src/Design/Design.Domain/Rules/FluentRules.cs`
**Lines 256-278:** The "CallerArgumentExpression and Rule IDs" comment block already describes hash-based IDs correctly (line 274: `Hash("t => t.Name == null ? \"Error\" : \"\"");`). No change needed -- this was already accurate before the fix.

### 3. Design.Domain RuleBasics.cs Comment

**File:** `src/Design/Design.Domain/Rules/RuleBasics.cs`
**Line 222:** Says "Rules are stored in Dictionary<uint, IRule> keyed by stable rule ID" -- this is still accurate. No change needed.

### 4. Sample Code for api.md Snippet

**File:** `src/samples/ApiReferenceSamples.cs` (the `api-generator-ruleid` snippet)
**What to check:** The snippet shows an `ApiRuleIdEntity` class. The rendered code in api.md shows the entity source, not the generated output. The generated output (which would show hash IDs) is not in the snippet. The surrounding text has been updated to say "FNV-1a hash constants". The snippet itself does not need updating -- it shows the entity definition, not the generated code.

**Net result: Only deliverable #1 requires a change.** The stale comment in `RuleIdGenerator.cs` line 23 should be updated.
