# Analyzers for Silent Failure Modes

**Plan #:** 002
**Date:** 2026-08-11
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-11
**Plan-review opt-in:** (declared at draft time)
**Code-review opt-in:** (declared at draft time)

---

## Scope

Build out the analyzer surface FableFeedback.md identifies as the highest-leverage DX gap: diagnostics for `[Factory]` classes that silently get no generated code (missing `partial` on class or property), rule trigger expressions that can never match a property path (`t => t.Items` vs the required `t => t.Items![0].X` form), and non-partial properties that silently lose data during serialization round-trips. Also fix the known NEATOO010 defects (multi-file partial classes, syntactic-only matching, lambda-body false positives) and stand up an analyzer test project using `Microsoft.CodeAnalysis.Testing` — the analyzers currently have zero tests. This plan does NOT redesign the generator pipeline; generator-side dead code removal belongs to plan 001/006.
