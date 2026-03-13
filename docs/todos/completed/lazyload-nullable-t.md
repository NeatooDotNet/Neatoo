# LazyLoad<T> Nullable Reference Type Support

**Status:** Complete
**Priority:** Medium
**Created:** 2026-03-12
**Last Updated:** 2026-03-12

## Problem

`LazyLoad<T>` has a `where T : class` constraint, which prevents using nullable reference types like `LazyLoad<IOrderItemList?>`. With C# nullable reference types enabled, `where T : class` requires a non-nullable reference type. Users need to express that a lazy-loaded child may legitimately be null.

## Solution

Relax the generic constraint from `where T : class` to `where T : class?` in:
- `LazyLoad<T>` class declaration
- `ILazyLoadFactory` interface methods
- `LazyLoadFactory` implementation methods

This is a non-breaking change — existing code using `LazyLoad<SomeClass>` continues to compile, while `LazyLoad<SomeClass?>` becomes valid.

## Files to Change

- `src/Neatoo/LazyLoad.cs` — class constraint
- `src/Neatoo/ILazyLoadFactory.cs` — interface and implementation constraints

## Documentation to Update

- `skills/neatoo/references/lazy-loading.md` — note nullable T support
- `src/Design/Design.Domain/PropertySystem/LazyLoadProperty.cs` — update design comments

## Plans

_(none — straightforward change)_

## Tasks

- [x] Change constraints in source code
- [x] Verify build and all tests pass
- [x] Update skill documentation
- [x] Update Design.Domain comments

## Progress Log

- 2026-03-12: Changed `where T : class` to `where T : class?` in LazyLoad.cs and ILazyLoadFactory.cs. Build passes, all 2087 tests pass. Updated skill reference and Design.Domain comments. Copied skill to ~/.claude/skills/neatoo/.

## Results / Conclusions

Changed the generic constraint from `where T : class` to `where T : class?` in 3 locations (LazyLoad<T> class, ILazyLoadFactory interface, LazyLoadFactory implementation). This is a non-breaking change that allows nullable reference types like `LazyLoad<IOrderItemList?>`. All existing tests continue to pass.
