# Reduce Design Source of Truth Scope

**Status:** In Progress
**Priority:** High
**Created:** 2026-02-01
**Last Updated:** 2026-02-01

---

## Problem

The Design Source of Truth project creates a parallel codebase of demonstration code that:

1. **Can silently diverge** - Design examples compile but aren't tested against actual implementation
2. **Creates maintenance burden** - Adding new features requires updating implementation AND design files AND CLAUDE-DESIGN.md
3. **Generates false confidence** - Claude trusts stale design files and implements against outdated contracts
4. **Scales poorly** - 50+ design files with ~25-45 hours per-release maintenance cost

The Claude Code expert assessment: "60% effective, questionable ROI... works until one of them breaks and you discover the other was wrong."

## Solution

Replace the full parallel codebase with curated snippets extracted from **real, tested code** using the MarkdownSnippets pattern. This ensures:

- **Single source of truth** - Snippets come from code that actually runs
- **Automatic sync** - If implementation changes, snippet extraction fails or shows the change
- **Reduced maintenance** - No parallel classes to maintain
- **Guaranteed accuracy** - Claude reads code that the tests validate

---

## Plans

---

## Tasks

### Phase 1: Identify High-Value Content to Preserve

- [ ] Audit `CommonGotchas.cs` - identify gotchas worth keeping as tested examples
- [ ] Audit `CLAUDE-DESIGN.md` - identify sections that add "why" value vs duplicating code
- [ ] Audit `AllBaseClasses.cs` - determine which examples need real test coverage
- [ ] Create list of design patterns that MUST remain documented (threading, serialization, `[Remote]` boundary)

### Phase 2: Create Real Test Coverage for Design Patterns

- [ ] Create `Design.Tests/GotchaValidationTests.cs` - tests that verify each documented gotcha
- [ ] Create `Design.Tests/PatternDemonstrationTests.cs` - tests showing correct usage patterns
- [ ] Add `#region` markers to test code for MarkdownSnippets extraction
- [ ] Verify all design examples are now exercised by real tests

### Phase 3: Migrate CLAUDE-DESIGN.md to MarkdownSnippets

- [ ] Add MarkdownSnippets to Design project
- [ ] Replace inline code examples with `snippet:` placeholders
- [ ] Configure snippet extraction from Design.Tests
- [ ] Run mdsnippets to verify extraction works
- [ ] Validate that CLAUDE-DESIGN.md renders correctly with extracted snippets

### Phase 4: Consolidate Design.Domain

- [ ] Remove `AllBaseClasses.cs` classes that now exist as tested examples
- [ ] Keep `CommonGotchas.cs` as documentation but reference tests for verification
- [ ] Remove or consolidate redundant design files
- [ ] Update directory structure to reflect reduced scope

### Phase 5: Update Documentation Hierarchy

- [ ] Document the new "source of truth" hierarchy in CLAUDE-DESIGN.md:
  - Level 1: Tests (most authoritative)
  - Level 2: Implementation with inline comments
  - Level 3: Design snippets extracted from tests
  - Level 4: CLAUDE-DESIGN.md (guide only)
- [ ] Update CLAUDE.md to reflect new approach
- [ ] Add CI check that mdsnippets extraction succeeds

### Phase 6: Validate Claude Code Effectiveness

- [ ] Test Claude Code comprehension with new structure
- [ ] Verify Claude can still answer key questions (base class selection, gotchas, factory patterns)
- [ ] Compare onboarding time: old design files vs new snippet-based approach
- [ ] Document any gaps and address them

---

## Success Criteria

1. **Zero parallel classes** - All code examples come from tested source
2. **Automated validation** - CI fails if snippets diverge from implementation
3. **Preserved value** - Claude can still quickly understand base classes, gotchas, and patterns
4. **Reduced maintenance** - Per-release update time drops from ~25-45 hours to ~5-10 hours
5. **No silent staleness** - Any implementation change that affects documentation is detectable

---

## Files Affected

**To Remove/Consolidate:**
- `src/Design/Design.Domain/BaseClasses/AllBaseClasses.cs` - Replace with tested examples
- Redundant design files in `FactoryOperations/`, `PropertySystem/`, etc.

**To Modify:**
- `src/Design/CLAUDE-DESIGN.md` - Convert to MarkdownSnippets
- `src/Design/Design.Domain/CommonGotchas.cs` - Add test references

**To Create:**
- `src/Design/Design.Tests/GotchaValidationTests.cs`
- `src/Design/Design.Tests/PatternDemonstrationTests.cs`
- MarkdownSnippets configuration

---

## Progress Log

---

## Results / Conclusions
