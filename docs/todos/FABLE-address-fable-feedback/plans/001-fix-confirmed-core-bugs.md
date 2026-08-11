# Fix Confirmed + High-Confidence Core Defects

**Plan #:** 001
**Date:** 2026-08-11
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-11
**Plan-review opt-in:** (declared at draft time)
**Code-review opt-in:** (declared at draft time)

---

## Scope

Work through the "Verified" and "High-confidence — core" defects in FableFeedback.md Appendix A: the `IsBusy` operator-precedence bug, post-deserialization double event subscription (entity and list), the list `FactoryComplete` stale-cache hole, the list converter's missing `$ref` emission, the `AsyncTasks` completion race and unobservable async-rule exceptions, per-invocation `expression.Compile()` in trigger properties, the shadowed-static `CreateProperty` hazard, the mutable shared `RuleBase.None`, the internal `PropertyReadOnlyException`, and the dead/inert surface (`RunRulesFlag`, no-op generated `GetRuleId`, unreachable generator paths). Each item is re-verified against current code before fixing; each fix gets a regression test. This plan does NOT touch MudNeatoo (plan 004) or RemoteFactory (plan 003), and does not revisit the `NoWarn` policy beyond what individual fixes require.
