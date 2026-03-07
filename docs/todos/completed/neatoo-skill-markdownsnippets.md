# Replace Inline Code in Neatoo SKILL.md with MarkdownSnippets

**Status:** Complete
**Priority:** Medium - Reduces maintenance burden and ensures code accuracy
**Created:** 2026-02-01
**Last Updated:** 2026-02-01
**Effort:** Small (2-4 hours) - Many snippets already exist in reference docs

---

## Problem

The main `skills/neatoo/SKILL.md` file contains many inline C# code blocks that should use MarkdownSnippets placeholders instead. This creates several issues:

1. **Sync risk**: Inline code can drift from actual Neatoo behavior
2. **No compilation**: These samples don't compile, so errors can slip in
3. **Inconsistency**: Reference docs (`references/*.md`) correctly use snippets, but SKILL.md doesn't

**Inline code locations in SKILL.md:**
- Lines 51-54: Properties example
- Lines 62-86: Factory Methods example
- Lines 99-111: Validation example
- Lines 119-145: Authorization example
- Lines 163-171: Service injection example
- Lines 185-194: Remote execution example
- Lines 204-217: Testing example
- Lines 221-228: SuppressFactory example

**Already correct:**
- Lines 13-29: Quick Start uses `<!-- snippet: skill-quickstart -->`

## Solution

Replace all inline C# code blocks in SKILL.md with MarkdownSnippets placeholders. Either:
1. Reference existing snippets from the samples projects
2. Create new region markers in sample code for SKILL.md-specific examples

The goal is that all C# in SKILL.md comes from compiled, tested sample code.

---

## Plans

### Snippet Mapping

Analysis of existing snippets in reference docs (`references/*.md`) and samples:

| Section | Lines | Action | Snippet Name | Notes |
|---------|-------|--------|--------------|-------|
| Properties | 51-54 | Create | `skill-properties-basic` | Minimal example showing partial properties |
| Factory Methods | 62-86 | Reuse | `remotefactory-factory-methods` | Already exists in samples |
| Validation | 99-111 | Reuse | `validation-basic` | Already exists, shows RuleManager.AddValidation |
| Authorization | 119-145 | Reuse | `authorization-interface`, `authorization-implementation` | Already exists in references/authorization.md |
| Service injection | 163-171 | Reuse | `remotefactory-service-injection` | Already exists in samples |
| Remote execution | 185-194 | Reuse | `remotefactory-remote-attribute` | Already exists in samples |
| Testing | 204-217 | Create | `skill-testing-pattern` | No existing testing snippets |
| SuppressFactory | 221-228 | Create | `skill-suppress-factory` | Source generation suppression example |

**Summary:** 5 of 8 inline code blocks can reuse existing snippets. Only 3 new snippets needed.

---

## Tasks

### Analysis
- [x] Inventory all inline code blocks in SKILL.md
- [x] Check if equivalent snippets already exist in samples
- [x] Identify which new regions need to be created (see Snippet Mapping above)

### Sample Code Updates
- [x] Add region markers to existing samples for reuse
- [x] Create new sample code where needed (properties, validation, authorization, testing sections)
- [x] Verify all samples compile

### SKILL.md Updates
- [x] Replace properties example with snippet placeholder
- [x] Replace factory methods example with snippet placeholder
- [x] Replace validation example with snippet placeholder
- [x] Replace authorization example with snippet placeholder
- [x] Replace service injection example with snippet placeholder
- [x] Replace remote execution example with snippet placeholder
- [x] Replace testing example with snippet placeholder
- [x] Replace SuppressFactory example with snippet placeholder

### Verification
- [x] Run mdsnippets to expand placeholders
- [x] Verify SKILL.md renders correctly
- [x] Confirm all code examples are accurate

---

## Acceptance Criteria

- [x] All inline C# code blocks replaced with snippet placeholders
- [x] `dotnet mdsnippets` expands all placeholders without errors
- [x] Snippet source links are valid and clickable
- [x] No functionality or clarity lost from original inline examples
- [x] New sample classes follow `Skill*` naming convention (e.g., `SkillValidProduct`)

---

## Progress Log

**2026-02-01:** Created todo after identifying that SKILL.md has inline code blocks while reference docs correctly use MarkdownSnippets. The skill was created with inline code for quick reference purposes, but this creates sync risk.

**2026-02-01:** Completed analysis phase. Reviewed reference docs and found 5 of 8 inline code blocks can reuse existing snippets. Only 3 new snippets needed: `skill-properties-basic`, `skill-testing-pattern`, and `skill-suppress-factory`.

**2026-02-01:** Implementation complete. Created new snippets in sample files following `Skill*` naming convention. Updated SKILL.md to use snippet placeholders. Ran `dotnet mdsnippets` to expand placeholders. All samples compile, all 20 tests pass.

---

## Results / Conclusions

All inline code blocks in SKILL.md have been replaced with MarkdownSnippets placeholders. The following snippets were created:

| Snippet Name | File Location | Purpose |
|-------------|---------------|---------|
| `skill-properties-basic` | QuickStartSamples.cs | Minimal partial property example |
| `skill-factory-methods` | FactorySamples.cs | Full factory method pattern |
| `skill-validation` | ValidationSamples.cs | Constructor validation pattern |
| `skill-authorization` | AuthorizationSamples.cs | Authorization interface + implementation + usage |
| `skill-service-injection` | FactorySamples.cs | [Service] parameter injection |
| `skill-remote-execution` | FactorySamples.cs | Aggregate root with [Remote] |
| `skill-remote-child` | FactorySamples.cs | Child entity without [Remote] |
| `skill-testing-pattern` | TestingPatternsTests.cs | DI setup and factory usage pattern |
| `skill-suppress-factory` | SourceGenerationSamples.cs | [SuppressFactory] usage |

**Note:** The `skill-suppress-factory` example uses `Getter<T>()/Setter()` pattern instead of partial properties because `[SuppressFactory]` suppresses both factory and property generation. This is the correct pattern for classes with this attribute.

**All samples compile and tests pass.**
