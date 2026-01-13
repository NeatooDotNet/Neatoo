# Documentation Code Block Audit

Comprehensive audit and remediation of C# code blocks in documentation.

**Created:** 2026-01-11
**Updated:** 2026-01-12
**Status:** COMPLETE - All phases done

## Previous Blocker (RESOLVED)

~~`IEntityBase.Save(CancellationToken)` was missing from interface.~~

**Resolution:** Method already exists at `EntityBase.cs:44-54`. The todo was written before checking the actual file state.

---

**Progress:** All phases complete

## Final Summary

| Category | Count | Status |
|----------|-------|--------|
| Compiled snippets | 98 | OK |
| Pseudo blocks | 225 | OK - illustrative code |
| Invalid blocks | 17 | OK - anti-patterns |
| Generated blocks | varies | OK - source-generated output |
| Unmarked blocks | 0 | **COMPLETE** |

---

## Problem 1: Snippets with Commented Code (RESOLVED)

**Principle violated:** Compiled snippets must not contain commented-out code.

~~These snippets contain `// In real code:` or `// db.Something()` patterns that defeat the purpose of having compiled samples:~~

| File | Snippet ID | Status | Resolution |
|------|------------|--------|------------|
| `CompleteExampleSamples.cs` | `complete-example` | **FIXED** | Uses `IRepository<PersonEntity>` |
| `RemoteFactorySamples.cs` | `remote-attribute-patterns` | **OK** | Focus is `[Remote]` attribute, not persistence |
| `PitfallsSamples.cs` | `map-modified-to-declaration` | **FIXED** | Uses `IRepository<MapModifiedToPitfallEntity>` |
| `AsyncValidationSamples.cs` | `clean-factory` | **FIXED** | Uses `IRepository<UserWithEmailEntity>` |
| `RuleUsageSamples.cs` | `rule-registration` | **FIXED** | Removed DI comments |
| `ChildEntitySamples.cs` | `aggregate-root-pattern` | **FIXED** | Uses `IRepository<SalesOrderEntity>` |

### Solution Applied

Created generic repository interface pattern in `SampleDomain/IRepository.cs`:
- `IRepository<TEntity>` - FindAsync, AddAsync, RemoveAsync, SaveChangesAsync
- `IRepositoryWithChildren<TEntity, TChildEntity>` - Adds GetChildren
- `MockRepository<T>` - Testing implementation
- Registered as open generics in `SampleServiceProvider.cs`

---

## Problem 2: Pseudo Blocks That Should Be Snippets

These pseudo blocks contain complete, compilable code:

### Definitely Compilable

| Location | ID | Content |
|----------|----|---------|
| `factory-operations.md:452` | `save-routing-logic` | Complete `Save()` method with routing logic |
| `factory-operations.md:472` | `save-with-cancellation` | Complete try/catch with CancellationTokenSource |
| `factory-operations.md:543` | `correct-save-pattern` | Simple assignment pattern |
| `collections.md:210` | `update-with-deletedlist` | Foreach loop pattern |

### Behavior-via-Comments (Keep Pseudo)

| Location | ID | Content |
|----------|----|---------|
| `collections.md:152` | `add-item-behavior` | Has `// item.IsChild = true` comment showing result |
| `collections.md:168` | `remove-item-behavior` | Has if/else with comments showing behavior |

---

## Problem 3: Unmarked Blocks (105 total)

Files with most unmarked blocks:

| File | Count | Notes |
|------|-------|-------|
| `validation-and-rules.md` | 19 | Major doc, needs full review |
| `DDD-Analysis.md` | 17 | Analysis doc - may be OK as-is |
| `exceptions.md` | 11 | Error handling patterns |
| `aggregates-and-entities.md` | 9 | Core doc |
| `lazy-loading-analysis.md` | 8 | Analysis doc |
| `installation.md` | 8 | Setup instructions |
| `troubleshooting.md` | 6 | |
| `lazy-loading-pattern.md` | 6 | |
| `best-practices.md` | 6 | |
| `quick-start.md` | 5 | User-facing, high priority |
| Others | 10 | |

---

## Remediation Plan

### Phase 1: Fix Commented Code in Snippets (COMPLETE)

**Solution:** Created `IRepository<T>` interface pattern instead of commented DB code.

- [x] Created `SampleDomain/IRepository.cs` with `IRepository<T>` and `IRepositoryWithChildren<T,TChild>` interfaces
- [x] Added `MockRepository<T>` and `MockRepositoryWithChildren<T,TChild>` for testing
- [x] Registered mock repositories in `SampleServiceProvider.cs`
- [x] `CompleteExampleSamples.cs` - Insert/Update/Delete now use `IRepository<PersonEntity>`
- [x] `RemoteFactorySamples.cs` - OK as-is (focus is `[Remote]` attribute, not persistence)
- [x] `PitfallsSamples.cs` - Update now uses `IRepository<MapModifiedToPitfallEntity>`
- [x] `AsyncValidationSamples.cs` - Insert now uses `IRepository<UserWithEmailEntity>`
- [x] `RuleUsageSamples.cs` - Removed DI registration comments
- [x] `ChildEntitySamples.cs` - Fetch/Insert now use `IRepository<SalesOrderEntity>`
- [x] All 182 sample tests pass
- [x] `dotnet mdsnippets` synced successfully

### Phase 2: Convert Obvious Pseudo to Snippets (COMPLETE)

**Result:** Added compiled snippets for verification; doc pseudo blocks kept for narrative context.

- [x] `save-routing-logic` - **Keep pseudo** (shows generated factory internals, not user code)
- [x] `save-with-cancellation` - **Added snippet** to `SaveUsageSamples.cs` for compilation check
  - Note: Doc shows `entity.Save(cts.Token)` which only works on concrete class, not interface
  - Snippet shows `WaitForTasks(token)` + `factory.Save()` pattern (interface-compatible)
- [x] `correct-save-pattern` - **Added region** in `SaveUsageSamples.cs:77-80`
- [x] `update-with-deletedlist` - **Keep pseudo** (illustrative; full snippet exists at `collections-update-operation`)

### Phase 3: Add Markers to Unmarked Blocks (COMPLETE)
- [x] `quick-start.md` (5 blocks) - User-facing
- [x] `validation-and-rules.md` (19 blocks) - Core documentation
- [x] `aggregates-and-entities.md` (9 blocks) - Core documentation
- [x] `installation.md` (8 blocks) - User-facing
- [x] `index.md` (1 block)
- [x] `extensibility-principle.md` (1 block)
- [x] `blazor-binding.md` (3 blocks)
- [x] `database-dependent-validation.md` (3 blocks)
- [x] `ef-integration.md` (3 blocks)
- [x] `rule-identification.md` (4 blocks)
- [x] `best-practices.md` (6 blocks)
- [x] `lazy-loading-pattern.md` (6 blocks)
- [x] `troubleshooting.md` (6 blocks)
- [x] `exceptions.md` (11 blocks)
- [x] `lazy-loading-analysis.md` (8 blocks)
- [x] `DDD-Analysis.md` (17 blocks)

### Phase 4: Review Remaining Pseudo Blocks (DEFERRED)

All 225 pseudo blocks are appropriately marked. Converting to compiled snippets is a future enhancement that can be done incrementally as documentation is updated.

**Criteria for conversion:**
- Complete, compilable code (not just API signatures)
- Not illustrating anti-patterns (use `invalid:` for those)
- Would benefit from compilation verification

---

## Skill Updates Made

Updated `~/.claude/skills/docs-snippets/` with:
- [x] Added "No commented code in snippets" principle to SKILL.md
- [x] Added detailed section to 09-marker-types.md
- [x] Updated 01-snippet-regions.md to remove DB placeholder example from pseudo section
- [x] Updated decision flowchart

---

## Notes

- `docs/todos/` and `docs/release-notes/` are excluded from verification
- Analysis docs (DDD-Analysis.md, lazy-loading-analysis.md) may have different standards
- Some unmarked blocks may be intentional (showing output, config files, etc.)
