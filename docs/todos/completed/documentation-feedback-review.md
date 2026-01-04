# Documentation Feedback Review

**Status:** Completed
**Completed:** 2026-01-03

## Summary

Addressed inline feedback comments across documentation files to improve clarity, structure, and technical accuracy.

## Completed Items

### Critical Fixes
- [x] Fix MapTo/MapFrom references - now shows manual implementation (removed features)
- [x] Add inline rule examples to Order and PersonSearchCriteria samples
- [x] Remove confusing DTO from Value Objects section

### Content Improvements
- [x] Clarify interface pattern (recommended not required, enables unit testing, auto-generated)
- [x] Add DI introduction with table explaining constructor vs `[Service]` injection
- [x] Soften factory language ("Preferred" instead of "Never")
- [x] Add fluent rule example to Quick Example in index.md
- [x] Clarify Neatoo package includes RemoteFactory as dependency

### Structural Reorganization
- [x] Move Key Features table to top of index.md for evaluators
- [x] Move Entity State Properties table up in aggregates-and-entities.md
- [x] Simplify Aggregate Root vs Child Entity section - focus on `[Remote]` explanation
- [x] Restructure validation-and-rules.md with TOC and "Inline Rules" category
- [x] Add "Action Rules (Transformations)" section documenting computed values

### Quick-Start Improvements
- [x] Add "Learn More" links after each step
- [x] Remove verbose Key Concepts section (details now in linked docs)

### CLAUDE.md Updates
- [x] Add "Documentation Philosophy" section
- [x] Add "When to Explain Why" guidelines

## Files Modified

- `docs/aggregates-and-entities.md`
- `docs/index.md`
- `docs/quick-start.md`
- `docs/validation-and-rules.md`
- `CLAUDE.md`
- `src/Neatoo.Documentation.Samples/AggregatesAndEntities/EntityBaseSamples.cs`
- `src/Neatoo.Documentation.Samples/AggregatesAndEntities/ValidateBaseSamples.cs`
- `src/Neatoo.Documentation.Samples/AggregatesAndEntities/ValueObjectSamples.cs`

## Verification

- Build: Succeeded
- Tests: 149 passed
