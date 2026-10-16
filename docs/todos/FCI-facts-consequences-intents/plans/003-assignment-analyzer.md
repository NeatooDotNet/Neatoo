# Assignment analyzer, code fix, Design sweep

**Plan #:** 003
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3
**Status:** Draft
**Last Updated:** 2026-10-16
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** —
**PR:** —

---

## Scope

A Neatoo analyzer warns on assignment to a Neatoo partial property inside a rule body, `AddAction`/`AddValidation` lambdas and `RuleBase`/`AsyncRuleBase` `Execute` overrides alike, and inside a verb, meaning a public or internal instance method of the entity that is not a factory operation; it stays silent inside factory operations and constructors. Its code fix rewrites the assignment to `await t.SetX(v)` and makes the enclosing lambda or method async. src/Design is swept to the awaited form, so Design.Domain's multi-write verbs become `async Task`. Existing tests are not rewritten: test projects suppress the diagnostic, because tests that exercise the forking setter are sacred.
