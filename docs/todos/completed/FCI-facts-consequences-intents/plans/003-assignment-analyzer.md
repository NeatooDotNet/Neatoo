# Assignment analyzer, code fix, Design sweep

**Plan #:** 003
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3
**Status:** Done
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (the rule and its scope were decided by Keith in the design session, D28)
**Code-review opt-in:** Yes (new analyzer and code fix every consumer runs; public API change to Design verbs)
**Branch:** fci-003-assignment-analyzer
**PR:** #117

---

## Scope

A Neatoo analyzer warns on assignment to a Neatoo partial property inside a rule body, `AddAction`/`AddValidation` lambdas and `RuleBase`/`AsyncRuleBase` `Execute` overrides alike, and inside a verb, meaning a public instance method of the entity that is not a factory operation; it stays silent inside factory operations and constructors. Its code fix rewrites the assignment to `await t.SetX(v)` and makes the enclosing lambda async, turning `AddAction` into `AddActionAsync`. src/Design is swept to the awaited form, so Design.Domain's multi-write verbs become `async Task`. Existing tests are not rewritten to the awaited form: Neatoo.UnitTest suppresses the diagnostic, because tests that exercise the forking setter are sacred.

## Intent

- The standard "domain code awaits its writes" is mechanical: the compiler names every place it is broken.
- The fix is one keystroke where the context allows it.

## Framework & Architectural Alignment

- VisionAlignment D28. NEATOO010 is the model: same project, same Neatoo-base-class test, generated code skipped.
- Factory operations assign, because the object is paused (D28, statement 5).

## Constraints & Invariants

- A warning, never an error; a consumer with `TreatWarningsAsErrors` opts in by their own setting.
- No diagnostic inside a factory operation, a constructor's own statements, an object initializer, or generated code.
- The fix is offered only where it compiles: the receiver has the matching `SetX`, the statement is an expression statement, and the enclosing lambda is async already or an `AddAction` lambda; a sync method is left to the author, because making it `async Task` changes its signature.
- Where `SetX` is missing, the message says why: the interface is not partial, or a member named `SetX` already exists.
- Design tests change only where a verb's new `Task` must be awaited; each is reported in the PR.

## Steps

1. Analyzer: find the three contexts, find writes to Neatoo properties in them, report with the reason `SetX` is missing when it is.
2. Code fix: the awaited write, the async lambda, `AddAction` to `AddActionAsync`.
3. A new analyzer test project, tests over stubs for every context and every exemption, and code-fix tests.
4. Sweep src/Design to zero NEATOO011: awaited writes in rules, `async Task` verbs, interfaces made partial where a write crosses types, callers awaited.
5. Suppress NEATOO011 in Neatoo.UnitTest.

## Acceptance

- [x] Assignment to a Neatoo property in an `AddAction` lambda, an `Execute` override and a verb is reported `[unit]`
- [x] Assignment in a factory operation, a constructor's own statements, an object initializer, or to a non-Neatoo property is not reported `[unit]`
- [x] The message names the missing `SetX`'s reason when it does not exist `[unit]`
- [x] The fix rewrites a write in an async lambda and converts an `AddAction` lambda to `AddActionAsync` `[unit]`
- [x] Design.Domain and Design.App build with no NEATOO011 and every Design test passes `[integration]`
- [x] Build and all tests green `[explicit-skip: meta-bullet]`

## Current State (Pre-Flight)

- `src/Neatoo.Analyzers/ConstructorPropertyAssignmentAnalyzer.cs` is NEATOO010: syntax walk of constructors, `ClassOrBaseClassIsNeatooBaseClass`, error severity, no tests anywhere. `src/Neatoo.CodeFixes/ConstructorPropertyAssignmentCodeFixProvider.cs` pairs it.
- Design.Domain references both as analyzers; Design.Domain, Design.App, Design.Infrastructure, Design.Server and Neatoo.UnitTest set `TreatWarningsAsErrors` false. The Neatoo package ships the analyzers to consumers.
- Design has 19 `AddAction` and 3 `AddActionAsync` registrations, 13 rule classes (`RuleBase`/`AsyncRuleBase`), and verbs including `Order.Submit`, `WorkOrder.Approve`, `WorkOrderTask.ApplyParentDiscount`. No Design interface is partial.
- Neatoo.BaseGenerator.Tests already carries Microsoft.CodeAnalysis.CSharp and .Workspaces; the analyzer tests get their own project beside it.

## Test Evidence

Flip checks: with the analyzer's report disabled, 8 of the first 11 analyzer tests failed (the 3 that passed are the not-reported and no-fix tests); with the initializer exemption removed, `ObjectInitializer_InsideVerb_IsNotReported` failed. The 7 tests added after round 1 were checked by tracing the analyzer, not by a recorded flip run. All 18 pass.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| AddAction lambda, Execute override, verb reported | Required | `[unit]` | `Neatoo.Analyzers.Tests.AwaitPropertyWriteAnalyzerTests.AddActionLambda_Assignment_IsReported`, `.AsyncLambdaAndValidationLambda_Assignments_AreReported`, `.ExecuteOverride_Assignment_IsReported`, `.Verb_Assignments_AreReported`, `.CompoundAssignmentsAndIncrements_InVerb_AreReported`, `.HelperCalledFromRuleOrVerb_IsReported_HelperCalledOnlyFromOperation_IsNot` | ✓ |
| Operation, constructor, initializer, non-Neatoo not reported | Required | `[unit]` | `AwaitPropertyWriteAnalyzerTests.OperationsConstructorsInitializersAndNonNeatooTargets_AreNotReported`, `.ObjectInitializer_InsideVerb_IsNotReported` (flip-checked: fails without the initializer exemption), `.OverrideOfNeatooLifecycleHook_IsNotReported` | ✓ |
| Message names why SetX is missing | Required | `[unit]` | `AwaitPropertyWriteAnalyzerTests.MissingSetter_MessageSaysWhy` | ✓ |
| Fix: async lambda and AddAction conversion | Required | `[unit]` | `AwaitPropertyWriteAnalyzerTests.Fix_AddActionLambda_BecomesAddActionAsyncWithAwaitedWrite`, `.Fix_StatementInAsyncLambda_IsAwaited`, `.Fix_AsyncVerb_IsAwaited`, `.Fix_NotOffered_ForSyncVerbOrMissingSetter`, `.Fix_BlockBodiedAddActionLambda_BecomesAsync`, `.FixAll_TwoWritesInOneAddActionLambda_BothAwaited`, `.Fix_CompoundAssignmentAndIncrement_KeepTheValue`; the fixed sources compile (`CompileErrors` asserted empty) | ✓ |
| Design builds with no NEATOO011, all Design tests pass | Required | `[integration]` | `reviews/003-design-build.log` (0 NEATOO011, `--no-incremental`), Design.Tests 206 passed in `003-test.log` | ✓ |
| Build and all tests green | Required | `[explicit-skip]` | `reviews/003-build.log`, `003-design-build.log`, `003-test.log` | ✓ |

## Gate Record

- Round 1 (2026-10-16): CONCERNS — 1 veto fixed (initializer exemption pinned), 2 callouts and 2 tech-debt fixed — `reviews/003-test-review.md`. Code review APPROVED, 4 callouts fixed — `reviews/003-code-review.md`.
- Round 2 (2026-10-16): APPROVED, no leftovers — Done

## Plan Amendments

### 2026-10-16 — The sweep ran through the code fix

- **Section affected:** Steps 2 and 4
- **Original said:** the fix rewrites one write; the sweep is by hand.
- **What changed:** the fix supports Fix All (batch), and `dotnet format analyzers --diagnostics NEATOO011` applied it across Design.Domain: 27 rule sites. Its tab indentation was converted back to the files' spaces. The three verbs, their interfaces and callers, and `CalculateTotalRule` (a writing `RuleBase`, now an `AsyncRuleBase`) were done by hand. Four Design test lines gained `await` before a verb that now returns `Task`; no assertion changed.
- **Why:** the fix is the tool a consumer would use; running it on Design exercised it on real code.
- **Discovery Log:** n/a (detail, not intent)
