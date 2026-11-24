# The framework registers every reaction

**Plan #:** 004
**Date:** 2026-11-24
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1
**Status:** Draft
**Last Updated:** 2026-11-24
**Plan-review opt-in:** decided at Step 2
**Code-review opt-in:** decided at Step 2
**Branch:** cut from wait-arc at Step 2
**PR:** —

---

## Scope

Moves registration from the caller to the owner: a property reports each task it starts tracking to its entity, which registers it and passes it up through its parents, so a write through the setter, Set{Name}, IValidateProperty.SetValue, RunRules(propertyName), a meta rule or a lazy load is registered the same way. Generated code and the hand-written setters stop registering. An object attached while busy has its in-flight work adopted. It does not change how waits are composed.
