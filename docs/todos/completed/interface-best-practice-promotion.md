# Interface Best Practice Promotion

Move interface usage from "anti-pattern/pitfall" framing to "strongly encouraged best practice."

## Problem Statement

Interface usage is currently documented primarily in negative terms:
- **Skill `pitfalls.md` Section 14**: "Casting to Concrete Types" - frames interfaces as avoiding anti-patterns
- **Skill `entities.md` line 560**: Mentions as a pitfall
- **Documentation `aggregates-and-entities.md`**: "Why Interfaces Are Required (Not Optional)" - mixed positive/negative framing

The guidance is correct, but the framing focuses on "what not to do" rather than "what to do." Developers should see interfaces as the recommended approach from the start, not discover them while reading about mistakes to avoid.

## Desired Outcome

Interfaces should be presented as a **strongly encouraged best practice** with the following messaging:

1. **Lead with positive guidance**: "Define an interface for every entity"
2. **Explain benefits first**: Testability, clean APIs, client-server support
3. **Move anti-patterns to supporting material**: Reference pitfalls for "what to avoid"

## Task List

### Documentation Changes

- [x] **Create `docs/best-practices.md`**
  - New file: Central location for Neatoo best practices
  - Interface usage as the first/primary best practice
  - Lead with positive framing and benefits
  - Include code examples showing the pattern
  - Cross-reference troubleshooting.md for anti-patterns

- [x] **Update `docs/aggregates-and-entities.md` (lines 178-241)**
  - Changed section title to "Interface-First Design (Best Practice)"
  - Lead with "Define a public interface for every Neatoo entity"
  - Restructured to present benefits before anti-patterns
  - Moved anti-pattern examples to subsection, reference troubleshooting.md

- [x] **Update `docs/index.md`**
  - Added link to best-practices.md in "I Want To..." table
  - Added to "Getting Started" section in Documentation Index

### Skill Changes

- [x] **Create `~/.claude/skills/neatoo/best-practices.md`**
  - Interface-first design as primary best practice
  - Positive framing: "Define interfaces for all entities"
  - Quick-reference benefits table
  - Code examples showing correct pattern
  - Reference pitfalls.md for anti-patterns

- [x] **Update `~/.claude/skills/neatoo/SKILL.md`**
  - Added best-practices.md to the reference files table (first entry)
  - Changed pitfalls.md description to reference best-practices.md

- [x] **Update `~/.claude/skills/neatoo/pitfalls.md` Section 14**
  - Retitled to "14. Not Using Interface-First Design"
  - Added opening paragraph referencing best-practices.md
  - Kept anti-pattern examples, framed as violations of best practice
  - Updated Quick Checklist to reference best-practices.md

- [x] **Update `~/.claude/skills/neatoo/entities.md` line 559-560**
  - Changed to "Not using interface-first design - See best-practices.md"

### Sample Code

- [x] **Verify samples exist** in `docs/samples/` that demonstrate interface pattern
  - Existing snippets reused: `docs:aggregates-and-entities:interface-requirement`, `docs:aggregates-and-entities:class-declaration`
  - No new snippets needed

### Verification

- [x] Run `.\scripts\extract-snippets.ps1 -Verify` to ensure docs sync - Passed (82 snippets verified)
- [x] Build samples to verify code compiles - Passed (0 errors, 0 warnings)
- [x] Review messaging consistency across docs and skill - Complete

## Key Messaging Points

### Primary Message (Best Practices)
> **Interface-First Design**: Define a public interface for every Neatoo entity. The interface is your API contract; all code outside the entity class works with interfaces.

### Benefits to Emphasize
| Benefit | Description |
|---------|-------------|
| **Testability** | Mock dependencies, stub entities in unit tests |
| **Clean API** | Interface defines what consumers can do; implementation details hidden |
| **Client-server** | RemoteFactory generates transfer code from interfaces |
| **Encapsulation** | Internal class prevents direct instantiation; factory pattern enforced |

### Secondary Message (Pitfalls Reference)
> Casting to concrete types breaks this design. See [pitfalls.md](pitfalls.md) Section 14 for anti-patterns to avoid.

## Files Summary

| File | Action | Key Change |
|------|--------|------------|
| `docs/best-practices.md` | Create | New best practices document with interfaces as #1 |
| `docs/aggregates-and-entities.md` | Edit | Reframe interface section with positive framing |
| `docs/index.md` | Edit | Link to best-practices.md |
| `skill/best-practices.md` | Create | Skill version of best practices |
| `skill/SKILL.md` | Edit | Add best-practices.md to reference table |
| `skill/pitfalls.md` | Edit | Retitle Section 14, add reference to best practices |
| `skill/entities.md` | Edit | Update pitfalls list to reference best practices |
