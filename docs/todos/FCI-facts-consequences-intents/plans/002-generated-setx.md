# Generated awaitable SetX on class and interface

**Plan #:** 002
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-2
**Status:** Draft
**Last Updated:** 2026-10-16
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** —
**PR:** —

---

## Scope

The base generator emits `Task SetX(T value)` for every settable partial property: on the class with the setter's semantics, including the private-set bypass of `IsReadOnly`, returning the whole cascade's Task; and on the generated partial `I{ClassName}` interface for public-set properties only. Generator tests pin the shape; integration tests pin that awaiting it waits for the nested cascade, async rules included, and that an awaited write re-entering the rule manager from inside a rule completes in order. Does not add the analyzer or change any existing rule.
