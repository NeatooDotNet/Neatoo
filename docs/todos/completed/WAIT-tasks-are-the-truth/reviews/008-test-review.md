## Test Review — WAIT-008 — 2026-11-24 — Round 1

**Verdict: APPROVED**
**Logs:** build PASSED, 0 errors (`reviews/008-build.log`: 18 warnings, all xUnit analyzer and CS8629 in the unchanged `Person.DomainModel.Tests/Integration Tests/PersonIntegrationTests.cs`); Design build PASSED, 0 warnings (`reviews/008-design-build.log`); tests 2,110 passed, 0 failed, 2 skipped (`reviews/008-test.log`: MudNeatoo 38, BaseGenerator 51, Analyzers 23, Person 55, Neatoo.UnitTest 1,943 + 2 skipped); Design.Tests 235 passed, 0 failed (`reviews/008-design-test.log`). All logs are stamped 22:09, the same minute as HEAD `000f448`, the commit that carries the test change.

### Veto-tier
None.

The Acceptance bullets are all `[explicit-skip]` documentation checks and are outside this review. The two test rows in Test Evidence are checked against the ruling (Plan Amendment 1, Parked row `ruled`, Keith 2026-11-24 "Keep the change"):

- **`ParentIsValid_AfterExplicitChildLoadFailure`: stronger, not weaker.** Before, a try/catch accepted either `InvalidOperationException` or no exception, so the test claimed nothing about the wait. Now it awaits `parent.WaitForTasks()` with no catch, and every assertion is kept (`HasLoadError`, the lazy child's `IsValid` false, the parent's `IsValid` false). Removing an accepted outcome narrows the claim, which is outcome 2 of "Tests Are Claims": the ruling changed the rule, and the test is rewritten to it. **What it actually exercises:** its loader is `() => throw ...`, which throws synchronously, so the load has already failed when `LoadAsync()` returns and the wait starts **after** the failure. That is probe case P2, which completes without throwing on both `main` and the arc (`reviews/008-lazy-failure-probe.log`). So this test pins the ruled behavior for an already-failed load, but it does not tell the arc from `main`. On `main` its old catch never fired either.
- **`ParentWaitForTasks_InProgressWhenTheLoadFails_CompletesWithoutThrowing`: really exercises the changed case.** The loader awaits a `TaskCompletionSource`. The test asserts `parent.IsBusy` and `!wait.IsCompleted` before it calls `SetException`, so the wait is in progress when the load fails. Then it awaits that same wait with a 5 s timeout. On `main` this is probe case P1, where the wait throws `InvalidOperationException`, so this test would fail on `main` and passes on the arc: it tells the two apart. It then asserts `!IsBusy`, `HasLoadError`, `!parent.IsValid`, and that a second wait also completes. That matches the Test Evidence row and the ruling ("a failed load never faults a wait; `HasLoadError` is the one channel").
- **The Test Evidence report is honest and complete.** `git diff wait-arc...HEAD` changes exactly two test files. `LazyLoadStatePropagationTests.cs` has the one rewritten test and the one added test, both reported. In `ValidateListBaseTests.cs` only two comments changed and no code line, assertion or expected value did; the third Plan Amendment reports this. No file under Design.Tests or any other test project changed. The other `.cs` edits (`EntityPropertyManager.cs` and four Design.Domain files) are comment-only.

### Callouts (≤ 5 Optional)
None.

### Tech-debt (untiered, ≤ 5, one line each)
- `ParentIsValid_AfterExplicitChildLoadFailure`'s new comment ("A failed load does not fault the parent's wait") reads as if it covers the changed case. Its synchronous loader makes it the after-failure case, the same on `main`, and the in-progress case is the new test's. A clause saying so would stop a later reader from deleting the new test as a duplicate.
- Probe case P3 (an awaited `LoadAsync()` throws the loader's exception, and the parent's wait after it completes) is the same on both branches and is not named by any test in this diff. Whether an existing test pins it was not checked, since it is out of this plan's scope.

### Could not verify
- `reviews/008-test.log` is the summary-verbosity output, so it does not list test names. I could not confirm from the log that the two named tests ran. They are `[TestMethod]`s in a `[TestClass]` inside `Neatoo.UnitTest`, which reports 0 failed.
- The probe log's source (the harness that produced P1 to P3) is not in the tree. I took its `main` versus arc outcomes as recorded and did not reproduce them.

### Theoretical (not triaged)
None.

### Read report
- Read beyond the brief: the `git diff wait-arc...HEAD -- src/` source diff (to confirm no unreported test change and that the Design.Domain and `EntityPropertyManager.cs` edits are comment-only); a grep of `src/Neatoo` lazy-load files for `HasLoadError` and the load-outcome comments.
- Named but unused: none.
