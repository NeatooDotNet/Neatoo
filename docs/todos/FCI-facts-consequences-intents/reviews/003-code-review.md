# FCI-003 code review

**2026-10-16: APPROVED.** No veto. The rule-lambda and `Execute` tests match the real RuleManager and RuleBase API; writes in local functions, nested lambdas, through child interfaces and shared-rule interfaces are caught; the Design sweep keeps each region's meaning; all verb callers are awaited.

Callouts, all taken:

1. **High, AC-3, fixed.** Compound assignment and `++`/`--` were not reported, which hid two writes in RuleBasics' `docs-rule-order` rules. The analyzer now registers every compound kind and the increments; the fix writes `SetX(X op v)` and `SetX(X + 1)`; `EarlyTraceRule`/`LateTraceRule` are `AsyncRuleBase` with awaited writes. Pinned by `CompoundAssignmentsAndIncrements_InVerb_AreReported`, `Fix_CompoundAssignmentAndIncrement_KeepTheValue`.
2. **Medium, AC-3, fixed.** A write in a private helper a rule calls was silent. A non-public helper is now a context when a rule lambda, a method-group rule registration, a verb, or another such helper in the same file calls it. Pinned by `HelperCalledFromRuleOrVerb_IsReported_HelperCalledOnlyFromOperation_IsNot`.
3. **Medium, AC-3, fixed.** Overrides of Neatoo lifecycle virtuals (`FactoryComplete`, `OnDeserialized`) were reported as verbs. An override of any member Neatoo declares is now exempt. Pinned by `OverrideOfNeatooLifecycleHook_IsNotReported`.
4. **High observed, AC-6, fixed.** Three swept lines inside block-bodied lambdas were over-indented and rendered so into the guides and the skill. Re-indented; mdsnippets re-run.

Could not verify, resolved: the logs predated three new tests; the full suite was re-run after every change (fresh `003-*.log`). Batch Fix All over two writes in one lambda is now pinned by `FixAll_TwoWritesInOneAddActionLambda_BothAwaited`.

Theoretical, not triaged: writes in list-class methods; writes through an interface that does not extend IValidateBase; a public helper called only from operations reported as a verb (Scope's literal definition); C# 14 `t?.X = v` reason text; a comment on the right-hand side dropped by the fix; SubmitDraftOrders' "async rules finish here" prose (rides the Punchlist row on prose).
