# Requirements Documenter -- LazyLoad: Extend from RemoteFactory

Last updated: 2026-03-29
Current step: Documentation complete. Reporting to orchestrator.

## Key Context

The implementation renamed Neatoo's `LazyLoad<T>` to `EntityLazyLoad<T>` throughout the codebase to avoid namespace collision with `Neatoo.RemoteFactory.LazyLoad<T>`. The Neatoo subclass now inherits from `Neatoo.RemoteFactory.LazyLoad<T>`, retaining only meta-property interfaces. All public API names changed:

- `LazyLoad<T>` -> `EntityLazyLoad<T>`
- `ILazyLoadFactory` -> `IEntityLazyLoadFactory`
- `LazyLoadFactory` -> `EntityLazyLoadFactory`
- `CreateLazyLoad` -> `CreateEntityLazyLoad`

Internal names kept unchanged: `LazyLoadValidateProperty`, `LazyLoadEntityProperty`, `LazyLoadPropertyHelper`, `ILazyLoadProperty`.

No behavioral changes -- all 23 business rule assertions from the plan are preserved. This was a pure rename + inheritance refactoring.

## Mistakes to Avoid

None encountered in this run.

## User Corrections

None.

## Documentation Tracking

### Markdown files updated (skill behavioral contract references):

1. **`skills/neatoo/references/lazy-loading.md`** -- Updated all prose references from `LazyLoad<T>` to `EntityLazyLoad<T>`, `ILazyLoadFactory` to `IEntityLazyLoadFactory`, `CreateLazyLoad` to `CreateEntityLazyLoad`. Updated section headings. Added note about inheritance from `Neatoo.RemoteFactory.LazyLoad<T>`. Embedded code snippet auto-updated by `dotnet mdsnippets` from updated `src/samples/LazyLoadSamples.cs`.

2. **`skills/neatoo/references/pitfalls.md`** -- Updated 2 table rows referencing `LazyLoad<T>` to `EntityLazyLoad<T>`.

3. **`skills/neatoo/references/source-generation.md`** -- Updated 1 reference from `LazyLoad` to `EntityLazyLoad` in setter accessibility section.

4. **`skills/neatoo/SKILL.md`** -- Updated frontmatter `description` field: `LazyLoad` -> `EntityLazyLoad`, `ILazyLoadFactory` -> `IEntityLazyLoadFactory`. Updated reference listing for `lazy-loading.md`.

5. **`skills/mudneatoo/SKILL.md`** -- Updated LazyLoad Databinding Pattern section heading and description from `LazyLoad<T>` to `EntityLazyLoad<T>`.

### Files copied to ~/.claude/skills/:

- `~/.claude/skills/neatoo/SKILL.md`
- `~/.claude/skills/neatoo/references/lazy-loading.md`
- `~/.claude/skills/neatoo/references/pitfalls.md`
- `~/.claude/skills/neatoo/references/source-generation.md`
- `~/.claude/skills/mudneatoo/SKILL.md`

### MarkdownSnippets refresh:

Ran `dotnet mdsnippets` to update embedded code snippets in markdown files from the already-updated `src/samples/LazyLoadSamples.cs`. This refreshed the `skill-lazyload-constructor-pattern` snippet in `lazy-loading.md` to show `IEntityLazyLoadFactory` and `EntityLazyLoad<ISkillLazyChild>`.

### User-facing docs (`docs/`):

No changes needed -- `docs/index.md` and `docs/getting-started.md` do not reference LazyLoad types.

### Rules updated:

- 0 new rules added (no new behavioral contracts)
- 0 existing rules changed behavior
- 20+ existing rule documentation references updated to use new names (rename only, no behavioral change)

## Developer Deliverables

No additional .cs deliverables needed. The developer already updated:
- `src/samples/LazyLoadSamples.cs` (updated to use new names)
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` (updated to use new names)
- `src/Design/CLAUDE-DESIGN.md` (updated to use new names)

All .cs files already reflect the implementation. No further code changes required.
