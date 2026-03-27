# Requirements Documenter -- Private Property Setters

Last updated: 2026-03-23
Current step: Documentation complete, reporting to orchestrator

## Key Context

- Private setter support is a new feature: source generator respects `private set` on partial properties
- `IValidateProperty.SetPrivateValue(object?, bool)` added as public interface method (was already `public virtual` on concrete type)
- 13 business rules established in plan; all SATISFIED per verification
- Design.Domain already has `PrivateSetPropertyDemo` with DESIGN DECISION, GENERATOR BEHAVIOR, and COMMON MISTAKE comment markers -- developer and architect handled this during implementation
- Current Neatoo version is 0.23.2; this feature would be 0.24.0 (minor: new feature, no breaking changes)

## Mistakes to Avoid

- Do NOT modify .cs files (Design projects, samples, framework source)
- Skill source lives in `skills/neatoo/` -- edit there, then copy to `~/.claude/skills/neatoo/`
- docs/guides/ uses MarkdownSnippets for code samples; private setter section uses inline code since no sample snippet exists yet (listed as Developer Deliverable)

## User Corrections

(None)

## Documentation Tracking

### Markdown Files Updated

**Skill Behavioral Contract References (2 files):**

1. `skills/neatoo/references/properties.md` -- Added "Private Setter Properties" section before "Read-Only Properties". Documents generated behavior, setting patterns, indexer behavior table, MudNeatoo integration, protected/internal setters, serialization, and `IValidateProperty.SetPrivateValue`. Also updated `IsReadOnly` description in Object-Per-Property table. Copied to `~/.claude/skills/neatoo/references/properties.md`.

2. `skills/neatoo/references/source-generation.md` -- Added "Setter Accessibility" section before "Suppressing Generation". Documents the generator's handling of private/protected/internal setters with a behavior table. Copied to `~/.claude/skills/neatoo/references/source-generation.md`.

**User-Facing Docs (3 files):**

3. `docs/guides/properties.md` -- Added "Private Setter Properties" section after "Read-Only Properties". Documents the pattern, AddAction usage, indexer behavior, and protected/internal setter behavior. Uses inline code (no snippet sample exists yet).

4. `docs/release-notes/v0.24.0.md` -- Created release notes for private setter support and IValidateProperty.SetPrivateValue API addition.

5. `docs/release-notes/index.md` -- Updated current version to 0.24.0 and added entry to highlights and all-releases tables.

**Plan File (1 file):**

6. `docs/plans/private-property-setters.md` -- Updated status from "Verified" to "Requirements Documented".

### Rules Categorization

| Rule | Category | Action |
|------|----------|--------|
| 1 (private set generates private accessor) | NEW (GAP-1) | Added to skill refs and user docs |
| 2 (private set interface is get-only) | NEW (GAP-1) | Added to skill refs and user docs |
| 3 (private set uses SetPrivateValue) | NEW (GAP-2) | Added to skill refs and user docs |
| 4 (protected set preserves accessor) | NEW (GAP-3) | Added to skill refs and user docs |
| 5 (internal set preserves accessor) | NEW (GAP-3) | Added to skill refs and user docs |
| 6 (LazyLoad with private set) | NEW | Added to source-generation.md table |
| 7 (get-only unchanged) | Existing | No update needed (already documented) |
| 8 (internal set triggers rules) | NEW (GAP-2) | Added to skill refs and user docs |
| 9 (SetValue throws on private set) | Existing (Requirement 4) | Added explicit table to skill refs |
| 10 (LoadValue bypasses) | Existing (Requirement 2) | Added explicit table to skill refs |
| 11 (MudNeatoo ReadOnly binding) | Existing (Requirement 9) | Documented in skill refs |
| 12 (IsReadOnly serialization) | Existing (Requirement 6) | Documented in skill refs |
| 13 (SetPrivateValue on IValidateProperty) | NEW | Added to skill refs, release notes |

**Summary:** 8 new rules added, 5 existing rules referenced/enhanced, 0 outdated rules reconciled.

## Developer Deliverables

### 1. Sample Code for docs/guides/properties.md snippet

**File:** `src/samples/PropertiesSamples.cs` (or new file)
**What to add:** A `PrivateSetterPropertyDemo` sample class and corresponding test methods demonstrating:
- Entity with `public partial decimal ComputedTotal { get; private set; }` and writable `Quantity`/`UnitPrice`
- `AddAction` rule computing `ComputedTotal`
- Test asserting that setting Quantity/UnitPrice recomputes ComputedTotal
- Test asserting `entity["ComputedTotal"].IsReadOnly == true`
- Test asserting `entity["ComputedTotal"].SetValue(x)` throws `PropertyReadOnlyException`

**Snippet names:** `properties-private-setter-declaration`, `properties-private-setter-usage`
**Contract documented:** Rules 1-3, 8-9 (generator behavior + runtime behavior of private-set properties)

**Note:** Once the sample is created, update `docs/guides/properties.md` to use `<!-- snippet: ... -->` references instead of the current inline code block.

### 2. Design.Domain code comments verification

**File:** `src/Design/Design.Domain/PropertySystem/PropertyBasics.cs`
**Status:** Already adequate. The developer added comprehensive DESIGN DECISION, GENERATOR BEHAVIOR, and COMMON MISTAKE markers covering the private setter pattern. No additional comments needed.

### 3. Design.Tests verification

**File:** `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs`
**Status:** Developer reported 8 integration tests created but blocked by pre-existing Design.Tests build issue (NF0105 errors). No additional tests needed from a requirements perspective -- the tests were designed and written.
