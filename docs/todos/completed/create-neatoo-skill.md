# Create Neatoo Skill for Claude Code

**Status:** In Progress
**Priority:** High
**Created:** 2026-01-26
**Last Updated:** 2026-01-26

**Files Created:**
- `skills/neatoo/SKILL.md`
- `skills/neatoo/references/*.md` (11 files including pitfalls.md)
- `skills/neatoo/samples/Neatoo.Skills.Domain/` (9 sample files)
- `skills/neatoo/samples/Neatoo.Skills.Tests/` (2 test files, 20 passing tests)

---

## Problem

Claude Code needs comprehensive guidance when working with Neatoo. Currently there's no structured skill that provides:
- Which Neatoo base class maps to which DDD concept
- How to use properties, validation, entities, collections
- Factory patterns ([Factory], [Create], [Fetch], [Remote], [Service])
- Authorization patterns
- Testing guidance (no mocking Neatoo classes)
- Blazor integration patterns

Without this, Claude must rediscover patterns each session or rely on scattered documentation.

## Solution

Create a comprehensive Neatoo skill at `/skills/neatoo/` with:
- SKILL.md with auto-triggers for Neatoo-related work
- Reference documents for each topic area
- Sample projects with MarkdownSnippets integration to keep skill docs in sync with working code

**Key design decisions:**
- Neatoo + RemoteFactory = Neatoo (present all factory features as Neatoo features)
- Assume DDD expertise - no DDD tutorials
- Two sample projects: domain model + tests (separation of concerns)
- MarkdownSnippets extracts regions from samples into reference docs

---

## Plans

---

## Tasks

### Structure Setup
- [x] Create `/skills/neatoo/` directory structure
- [x] Update `mdsnippets.json` to include skills directory (already works - no changes needed)
- [x] Create `skills/neatoo/samples/Neatoo.Skills.Domain/` project
- [x] Create `skills/neatoo/samples/Neatoo.Skills.Tests/` project
- [x] Verify MarkdownSnippets works with skill samples

### SKILL.md
- [x] Create SKILL.md with auto-trigger conditions
- [x] Define trigger phrases (ValidateBase, EditBase, [Factory], etc.)
- [x] Add summary of each reference document
- [x] Add "when to use which base class" quick reference

### Reference Documents
- [x] `references/base-classes.md` - Neatoo-to-DDD mapping
- [x] `references/properties.md` - Getter/Setter, change tracking
- [x] `references/validation.md` - ValidateBase, rules, attributes
- [x] `references/entities.md` - EditBase, lifecycle, persistence, Save routing
- [x] `references/collections.md` - EditableListBase, parent-child
- [x] `references/factory.md` - [Factory], CRUD ops, [Remote], [Service]
- [x] `references/authorization.md` - [AuthorizeFactory<T>], CanCreate/CanFetch
- [x] `references/source-generation.md` - What gets generated, Generated/ folder
- [x] `references/blazor.md` - Blazor-specific patterns
- [x] `references/testing.md` - No mocking Neatoo, integration patterns

### Sample Code
- [x] Domain model samples with region markers for each reference doc
- [x] Test samples demonstrating proper testing patterns
- [x] Verify all samples compile and tests pass (20 tests passing)
- [x] Run mdsnippets to sync samples into reference docs

### Validation
- [ ] Test auto-triggers work correctly
- [ ] Verify skill provides accurate guidance
- [ ] Ensure samples match current Neatoo patterns

---

## Progress Log

**2026-01-26:** Created todo. Explored existing MarkdownSnippets setup and RemoteFactory repository to understand full scope. Agreed on structure: SKILL.md + 10 reference docs + 2 sample projects (domain + tests).

**2026-01-26:** Completed initial structure:
- Created directory structure at `skills/neatoo/`
- Created both sample projects (Domain + Tests) - both build and tests pass
- Added projects to solution under "Skills" folder
- Created SKILL.md with comprehensive auto-triggers and quick reference
- Created all 10 reference documents with snippet placeholders
- Verified MarkdownSnippets scans skills directory (no config changes needed)

**2026-01-26:** Fixed authorization.md and testing.md - replaced inline C# code blocks with snippet placeholders. All reference docs now consistently use `<!-- snippet: name -->` for code examples.

**2026-01-26:** Created comprehensive sample code:
- Domain samples: BaseClassSamples.cs, PropertySamples.cs, ValidationSamples.cs, FactorySamples.cs, AuthorizationSamples.cs, EntitySamples.cs, CollectionSamples.cs, SourceGenerationSamples.cs, BlazorSamples.cs
- Test samples: SkillTestBase.cs (DI setup, mock services), TestingPatternsTests.cs (20 passing tests)
- Created `references/pitfalls.md` documenting gotchas discovered during sample creation

**Key gotchas discovered and documented:**
- `CommandBase` doesn't exist - use static classes with `[Execute]`
- `ReadOnlyBase`/`ReadOnlyListBase` don't exist - use ValidateBase with only `[Fetch]`
- Multiple `[AuthorizeFactory<>]` attributes not supported - combine into one interface
- `NeatooPropertyChanged` delegate takes 1 arg, not 2 (sender, args)
- `ChangeReason` enum values: `UserEdit`, `Load` (not `DataLoad`)
- Property is `Reason` not `ChangeReason` on event args
- Validation rules are async - use `await RunRules()` before checking validity

**Updated SKILL.md** to reflect accurate base class names (EntityBase, ValidateBase, EntityListBase, ValidateListBase) and corrected examples.

**Next:** Run mdsnippets to sync samples into reference docs.

---

## Results / Conclusions

