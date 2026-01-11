# MarkdownSnippets Migration

Migrating from custom `extract-snippets.ps1` to MarkdownSnippets tool.

## Migration Checklist

- [x] Phase 1: Setup MarkdownSnippets
  - [x] Install dotnet tool (`dotnet tool install MarkdownSnippets.Tool`)
  - [x] Create mdsnippets.json config
  - [x] Create .config/dotnet-tools.json (tool manifest)

- [x] Phase 2: Migrate C# Region Markers
  - [x] Convert `#region docs:{file}:{id}` to `#region {id}`
  - [x] Verify all regions have unique IDs across project (195 unique IDs)
  - [x] Renamed duplicates: `child-entity` -> `entity-child-entity`/`factory-child-entity`, `update-operation` -> `collections-update-operation`/`factory-update-operation`

- [x] Phase 3: Migrate Markdown Placeholders
  - [x] Convert `<!-- snippet: docs:{file}:{id} -->` to `snippet: {id}`
  - [x] MarkdownSnippets auto-removes old closing tags and generates new format

- [x] Phase 4: Verification
  - [x] Run MarkdownSnippets successfully (133 snippets extracted, 61 in docs)
  - [x] Create `verify-code-blocks.ps1` for pseudo/invalid validation
  - [ ] Add `pseudo:`/`invalid:` markers to 240 unmarked code blocks (FUTURE WORK)

- [x] Phase 5: Cleanup
  - [x] Copy skill files from `~/.claude/skills/neatoo/` to `.claude/skills/neatoo/`
  - [x] Archive old `extract-snippets.ps1` to `scripts/archive/`
  - [x] Update CLAUDE.md references

## Files Created/Modified

| File | Purpose |
|------|---------|
| `.config/dotnet-tools.json` | Tool manifest for MarkdownSnippets |
| `mdsnippets.json` | MarkdownSnippets configuration |
| `scripts/migrate-regions.ps1` | Migration script for C# region markers |
| `scripts/migrate-markdown.ps1` | Migration script for Markdown placeholders |
| `scripts/check-duplicate-ids.ps1` | Verify unique snippet IDs |
| `scripts/verify-code-blocks.ps1` | Verify all code blocks have markers |

## Commands Reference

```powershell
# Sync documentation with code snippets
dotnet mdsnippets

# Verify all code blocks have markers
pwsh scripts/verify-code-blocks.ps1

# Check if docs changed (CI verification)
dotnet mdsnippets && git diff --exit-code docs/ .claude/skills/
```

## Excluded Directories

Both MarkdownSnippets and verify-code-blocks.ps1 exclude:
- `todos/` - Planning documents with example snippets
- `release-notes/` - Version notes with inline examples

## Future Work: Code Block Markers

Analysis of 240 unmarked C# code blocks:

| Category | Count | Marker Type |
|----------|-------|-------------|
| Real gaps (should be compiled samples) | ~175 | `snippet: {id}` (after adding to samples) |
| Pseudo-code (illustrative, conceptual) | ~48 | `pseudo:{id}` |
| Invalid/anti-patterns (WRONG examples) | ~17 | `invalid:{id}` |
| Source-generated output | ~10 | `generated:{path}#L{start}-L{end}` |

See `docs/todos/documentation-sample-gaps.md` for prioritized list of files needing compiled samples.

### Marker Types

| Marker | Purpose |
|--------|---------|
| `snippet: {id}` | Compiled, tested code from `docs/samples/` |
| `pseudo:{id}` | Illustrative code, library internals, runtime state |
| `invalid:{id}` | Anti-patterns, WRONG examples |
| `generated:{path}#L{start}-L{end}` | Source-generated output with drift detection |

### Generated Snippets

For source-generated code (factory interfaces, getter/setter implementations), use the `generated:` marker with file path and line numbers:

```markdown
<!-- snippet: generated:Neatoo.Samples.DomainModel/Generated/Neatoo.Factory/PersonFactory.g.cs#L15-L22 -->
```csharp
public interface IPersonFactory
{
    IPerson Create();
    Task<IPerson> Fetch(int id);
}
```
<!-- /snippet -->
```

Line numbers serve as drift detection - when the generator changes, line mismatches signal review is needed.

## Notes

- MarkdownSnippets scans ALL code files and ALL markdown files, matching by snippet ID alone
- Snippet IDs must be globally unique across the project
- The tool auto-generates `<!-- snippet: {id} -->` and `<!-- endSnippet -->` tags
- `pseudo:`, `invalid:`, and `generated:` markers are manual (MarkdownSnippets ignores them)
