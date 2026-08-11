# Address Fable Feedback

**ID:** FABLE
**Type:** Bug + Enhancement (mixed arc)
**Status:** In Progress
**Priority:** High
**Created:** 2026-08-11
**Last Updated:** 2026-08-11

---

## Goal

Address the findings in the 2026-08-11 framework assessment captured in [docs/FableFeedback.md](../../FableFeedback.md): fix the confirmed and high-confidence defects, close the analyzer gap for silent failure modes, harden the RemoteFactory boundary layer, rebuild or shrink MudNeatoo, bring the human-facing docs up to the standard of the skills/Design corpus, and reposition the framework's public pitch (RemoteFactory as the standalone headline, entity layer as the opt-in edit-graph tier). Success means the "What to do, in order" list in FableFeedback.md is worked through — each item either done or explicitly declined with a recorded reason.

## Acceptance Criteria

- [ ] Every defect in FableFeedback.md Appendix A is fixed, or recorded as declined/deferred with a reason
- [ ] Analyzers exist for the silent failure modes named in the feedback (missing `partial`/`[Factory]` silent-skip, trigger-path mismatch, non-partial-property serialization loss), with analyzer tests
- [ ] RemoteFactory boundary hardening landed: securable endpoint, event-relay auth posture decided, type-resolution allow-list, error contract, real-HTTP test suite
- [ ] MudNeatoo components share a base class, re-subscribe on parameter change, and have bUnit coverage — or the library's scope is deliberately reduced with the decision recorded
- [ ] Human docs cover the topics the feedback lists as missing (lazy loading, authorization, testing, trimming) and the guide reading order is pedagogical, not alphabetical
- [ ] Positioning updated: RemoteFactory-first framing, "when not to use Neatoo" guidance, vsCSLA material linked for evaluators

## Out of Scope

- zTreatment application changes (framework consumers pick up fixes via package updates)
- New framework features not named in the feedback
- The 1.0 release itself (this todo makes 1.0 credible; cutting it is separate)

---

## Plan Index

The initial split mirrors the priority order in FableFeedback.md ("What to do, in order"). Expect this index to grow as plans are drafted.

| # | File | Title | Status |
|---|------|-------|--------|
| 001 | [001-fix-confirmed-core-bugs](./plans/001-fix-confirmed-core-bugs.md) | Fix confirmed + high-confidence core defects | Draft |
| 002 | [002-analyzer-suite-for-silent-failures](./plans/002-analyzer-suite-for-silent-failures.md) | Analyzers for silent failure modes | Draft |
| 003 | [003-remotefactory-boundary-hardening](./plans/003-remotefactory-boundary-hardening.md) | RemoteFactory endpoint security, error contract, HTTP tests | Draft |
| 004 | [004-mudneatoo-rebuild](./plans/004-mudneatoo-rebuild.md) | MudNeatoo base class, lifecycle fixes, bUnit tests | Draft |
| 005 | [005-docs-for-humans](./plans/005-docs-for-humans.md) | Port skill/Design content to human docs; fix Person example | Draft |
| 006 | [006-reposition-and-cleanup](./plans/006-reposition-and-cleanup.md) | RemoteFactory-first positioning; remove dead surface | Draft |

---

## Discovery Log

(empty — no implementation yet)

---

## Skipped Steps

- Step 1 reconnaissance — satisfied by the five-agent audit that produced FableFeedback.md itself; its Appendix A/B are the recon record.
- Step 1 ID candidate ballot — user requested a simple capture-only todo; ID `FABLE` chosen to match the feedback document (uniqueness verified against `docs/todos/` and `docs/todos/completed/`).

---

## Sibling Todos

(none)

---

## Close-Out Audit

(not started)

---

## Docs & Retro

(not started)

---

## Results / Conclusions

(not started)
