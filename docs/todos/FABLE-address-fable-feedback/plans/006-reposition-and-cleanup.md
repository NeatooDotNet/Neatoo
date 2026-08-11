# Reposition and Cleanup

**Plan #:** 006
**Date:** 2026-08-11
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-11
**Plan-review opt-in:** (declared at draft time)
**Code-review opt-in:** (declared at draft time)

---

## Scope

Reposition the public pitch per FableFeedback.md: lead with RemoteFactory as the standalone product (usable with plain POCOs, no DDD buy-in), frame the Neatoo entity layer as the opt-in edit-graph tier, and add an honest "when not to use Neatoo" page codifying the edit-path/read-path split zTreatment already practices. Remove or make real the dead surface: inert `RunRulesFlag` values, the no-op generated `GetRuleId` path, the unreachable minimal-generation mode and NEATOO003/004 diagnostics, `Authorized.ToAuthorized()`/`ToBoolean()` NotImplemented stubs, and the commented-out `INotifyPropertyChangedAsync` file. This plan does NOT cut a 1.0 release or change versioning policy; it makes the story truthful at 0.x.
