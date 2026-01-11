# Unmarked C# Blocks in Documentation

Track progress adding markers to all C# code blocks in documentation.

## Status

- [x] Audit all main docs for unmarked blocks
- [x] Convert skill markers from `docs:{file}:{id}` to `{id}` format
- [x] Fix pseudo/generated/invalid markers to not use `snippet:` prefix
- [x] Add markers to meta-properties.md
- [x] Add markers to property-system.md
- [x] Add markers to remote-factory.md
- [x] Add markers to factory-operations.md
- [x] Add markers to testing.md
- [x] Add markers to mapper-methods.md
- [x] Add markers to collections.md
- [x] Update verify-code-blocks.ps1 to recognize new marker format
- [ ] Create missing sample code for `snippet` blocks
- [ ] Update docs-snippets skill with marker format clarification
- [ ] Add markers to remaining docs (aggregates-and-entities, best-practices, etc.)

## Marker Format

MarkdownSnippets processes `snippet:` markers. For non-compiled code, use plain HTML comments:

```markdown
<!-- pseudo:descriptive-id -->
```csharp
// Illustrative code
```
<!-- /pseudo -->

<!-- invalid:anti-pattern-id -->
```csharp
// WRONG - don't do this
```
<!-- /invalid -->

<!-- generated:path/to/file.g.cs#L10-L20 -->
```csharp
// Source-generated output
```
<!-- /generated -->
```

## Summary by File

| File | Pseudo | Snippet | Invalid | Generated | Total |
|------|--------|---------|---------|-----------|-------|
| meta-properties.md | 30 | 2 | 1 | 0 | 33 |
| property-system.md | 21 | 7 | 0 | 3 | 31 |
| remote-factory.md | 4 | 14 | 1 | 0 | 19 |
| factory-operations.md | 14 | 1 | 4 | 3 | 22 |
| testing.md | 8 | 2 | 2 | 0 | 12 |
| mapper-methods.md | 11 | 0 | 0 | 0 | 11 |
| collections.md | 4 | 4 | 2 | 0 | 10 |
| **Total** | **92** | **30** | **10** | **6** | **138** |

## Priority

1. **High**: remote-factory.md (14 snippets need sample code)
2. **High**: property-system.md (7 snippets need sample code)
3. **Medium**: collections.md (4 snippets need sample code)
4. **Medium**: meta-properties.md (2 snippets need sample code)
5. **Low**: Others (mostly pseudo markers needed)

---

## Detailed Audit by File

### meta-properties.md (33 blocks)

| Line | Description | Category |
|------|-------------|----------|
| 38 | `IsBusy` property signature | pseudo |
| 48 | Razor MudBlazor button example | pseudo |
| 61-64 | `WaitForTasks()` method signatures | pseudo |
| 67-70 | Simple await usage | pseudo |
| 73-85 | CancellationToken with try-catch | pseudo |
| 95-97 | `IsValid` property signature | pseudo |
| 104-109 | Razor conditional markup | pseudo |
| 115-117 | `IsSelfValid` property signature | pseudo |
| 125-146 | Person/phone validation scenario | **snippet** |
| 152-154 | `PropertyMessages` property signature | pseudo |
| 157-162 | PropertyMessages iteration | pseudo |
| 168-171 | `RunRules()` method signatures | pseudo |
| 185-194 | RunRules flag combinations | pseudo |
| 197-201 | Before-save validation pattern | pseudo |
| 209-212 | Clear methods signatures | pseudo |
| 220-222 | `IsNew` property signature | pseudo |
| 239-241 | `IsModified` property signature | pseudo |
| 254-256 | `IsSelfModified` property signature | pseudo |
| 267-269 | `IsMarkedModified` property signature | pseudo |
| 275-277 | `MarkModified()` method signature | pseudo |
| 285-301 | MarkModified state progression | **snippet** |
| 309-311 | `IsDeleted` property signature | pseudo |
| 314-317 | Delete/UnDelete method calls | pseudo |
| 326-328 | `IsChild` property signature | pseudo |
| 342-344 | `Parent` property signature | pseudo |
| 352-361 | Parent access with casting | pseudo |
| 367-369 | `Root` property signature | pseudo |
| 381-392 | Root property usage | pseudo |
| 398-404 | Cross-aggregate enforcement | **invalid** |
| 410-412 | `IsSavable` property signature | pseudo |
| 429-432 | `Save()` method signatures | pseudo |
| 440-446 | Entity-based save with cast | pseudo |
| 529-545 | PropertyChanged event handler | pseudo |
| 609-628 | Common UI patterns | pseudo |

### property-system.md (31 blocks)

| Line | Description | Category |
|------|-------------|----------|
| 9 | Partial property declaration | generated |
| 90 | IValidateProperty interface | **snippet** |
| 121 | IEntityProperty interface | **snippet** |
| 142 | Property indexer usage | **snippet** |
| 214 | Check modification example | pseudo |
| 226 | Clear modification example | pseudo |
| 237 | Property messages usage | pseudo |
| 253 | IPropertyMessage interface | **snippet** |
| 265 | Busy state checking | pseudo |
| 280 | Entity-level busy state | pseudo |
| 334 | Read-only state check | pseudo |
| 344 | PropertyManager usage | pseudo |
| 428 | Factory pause flow | pseudo |
| 507 | Bulk collection operations | pseudo |
| 524 | Checking pause state | pseudo |
| 541 | Resume behavior | pseudo |
| 559 | Direct resume pattern | pseudo |
| 577 | Child objects pausing | pseudo |
| 595 | Nested pause calls | pseudo |
| 615 | UI binding example | pseudo |
| 638 | PropertyChanged handler | pseudo |
| 658 | NeatooPropertyChanged delegate | **snippet** |
| 681 | NeatooPropertyChangedEventArgs | **snippet** |
| 715 | Event chain example | pseudo |
| 736 | Subscription example | pseudo |
| 755 | Nested property tracking | pseudo |
| 771 | Cross-item validation | **snippet** |
| 793 | Parent reacting to child | **snippet** |
| 813 | Blazor state management | pseudo |
| 852 | Event flow example | pseudo |
| 878 | Complete property access | pseudo |

### remote-factory.md (19 blocks)

| Line | Description | Category |
|------|-------------|----------|
| 63-83 | `[Remote]` attribute patterns | **snippet** |
| 98-122 | Aggregate vs child patterns | **snippet** |
| 137-155 | Serialization behavior | pseudo |
| 161-169 | Object identity demo | **snippet** |
| 192-200 | Wrong vs correct Save | **invalid** |
| 208-220 | EmailValidationRule | **snippet** |
| 237-255 | UniqueEmailRule async | **snippet** |
| 260-272 | Client/Server services | **snippet** |
| 293-302 | Service injection | **snippet** |
| 310-328 | Insert with MapTo | **snippet** |
| 332-336 | Capturing return values | **snippet** |
| 344-354 | Exception handling | **snippet** |
| 360-371 | Validation failure handling | **snippet** |
| 379-395 | Authorization pattern | **snippet** |
| 414-417 | Custom endpoint path | pseudo |
| 421-427 | Client endpoint config | **snippet** |
| 452-460 | Validate before save | pseudo |
| 464-472 | Keep operations focused | pseudo |
| 476-490 | Concurrency handling | **snippet** |

### factory-operations.md (22 blocks)

| Line | Description | Category |
|------|-------------|----------|
| 37 | `[Remote]` Fetch example | pseudo |
| 112 | Create usage example | pseudo |
| 164 | Fetch usage example | pseudo |
| 240 | Generated Fetch overloads | generated |
| 325 | Factory validation anti-pattern | **invalid** |
| 370 | Generated MapModifiedTo | generated |
| 405 | Deletion workflow | pseudo |
| 417 | Service injection | pseudo |
| 436 | Save routing logic | pseudo |
| 454 | CancellationToken usage | pseudo |
| 476 | CORRECT save pattern | pseudo |
| 482 | WRONG save pattern | **invalid** |
| 515 | Entity-based save | pseudo |
| 529 | Casting anti-pattern | **invalid** |
| 734 | Command pattern | pseudo |
| 753 | Command usage | pseudo |
| 767 | AuthorizeFactory | pseudo |
| 782 | UI permission display | pseudo |
| 932 | Mapper declarations | pseudo |
| 944 | Factory callbacks | pseudo |
| 960 | Complete Order example | **snippet** |

### testing.md (12 blocks)

| Line | Description | Category |
|------|-------------|----------|
| 98-111 | TestableProduct unit test | **snippet** |
| 121-127 | RunRule signatures | pseudo |
| 171-184 | Assertion patterns | pseudo |
| 234-239 | Mock IEntityProperty | pseudo |
| 243-249 | Moq verification | pseudo |
| 288-303 | Null parent handling | **snippet** |
| 311-323 | WRONG: Execute wrapper | **invalid** |
| 325-329 | CORRECT: RunRule | pseudo |
| 333-337 | WRONG: Mock Neatoo | **invalid** |
| 339-345 | CORRECT: Real classes | pseudo |
| 351-362 | Unit vs integration | pseudo |

### mapper-methods.md (11 blocks)

All blocks are **pseudo** - illustrative patterns for MapFrom/MapTo/MapModifiedTo usage.

| Line | Description | Category |
|------|-------------|----------|
| 138 | Fetch with MapFrom | pseudo |
| 225 | Insert with MapTo | pseudo |
| 320 | Update with MapModifiedTo | pseudo |
| 349 | Property matching rules | pseudo |
| 362 | Type compatibility | pseudo |
| 376 | Child collections mapping | pseudo |
| 475 | Property exclusion rules | pseudo |
| 581 | Best practice: naming | pseudo |
| 592 | Best practice: MapModifiedTo | pseudo |
| 608 | Best practice: null handling | pseudo |
| 623 | Best practice: ID after insert | pseudo |

### collections.md (10 blocks)

| Line | Description | Category |
|------|-------------|----------|
| 123 | CORRECT/WRONG add patterns | **invalid** |
| 190 | Delete/Remove equivalence | pseudo |
| 206 | Update with DeletedList | pseudo |
| 228 | Intra-aggregate moves | pseudo |
| 246 | Cross-aggregate move error | **invalid** |
| 343 | Running rules on items | **snippet** |
| 412 | Custom add methods | **snippet** |
| 448 | Blazor UI binding | **snippet** |
| 480 | Pausing during bulk ops | pseudo |
| 493 | Complete PersonPhoneList | **snippet** |

---

## Next Steps

1. Start with **remote-factory.md** - highest priority (14 snippets needed)
2. Create sample code in `docs/samples/` for each snippet block
3. Add `#region` markers to sample code
4. Run `dotnet mdsnippets` to sync
5. Add pseudo/invalid/generated markers to remaining blocks
6. Run `pwsh scripts/verify-code-blocks.ps1` to verify all blocks have markers
