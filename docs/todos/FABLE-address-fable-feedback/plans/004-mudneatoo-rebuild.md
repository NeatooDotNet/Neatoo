# MudNeatoo Rebuild

**Plan #:** 004
**Date:** 2026-08-11
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-11
**Plan-review opt-in:** (declared at draft time)
**Code-review opt-in:** (declared at draft time)

---

## Scope

Rebuild the MudNeatoo input components on a shared base class that owns the subscription lifecycle (subscribe/re-subscribe in `OnParametersSet`, not `OnInitialized`-only), fix the per-component filter divergences (TextField missing `Value`; the never-firing `PropertyMessages` clause everywhere), route the four hand-rolled-validation components (CheckBox, Switch, Slider, RadioGroup) through the `MudForm` validation pipeline, tame `NeatooValidationSummary` render churn, and stand up bUnit coverage for all of it. Evaluate upstreaming zTreatment's `AggregateBoundary`/`SavableButton` boundary-component pattern as a first-class offering — possibly instead of expanding the per-input component set. An explicit decision to *shrink* MudNeatoo's scope is an acceptable outcome of this plan if recorded. This plan does NOT change the core property system's event names (that's plan 001 territory if needed).
