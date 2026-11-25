## Test Review — WAIT-007 — [2026-11-24] — Round 1

**Verdict: APPROVED**
**Logs:** build PASSED — `reviews/007-build.log` (0 errors; no warning names either new file), `reviews/007-design-build.log` (0 errors, 0 warnings); tests 2,109 passed, 2 skipped, 0 failed — `reviews/007-test.log` (MudNeatoo 38 = 33 existing + 5 new; BaseGenerator 51; Analyzers 23; Person 55; UnitTest 1,942 + 2 skipped); 235 passed, 0 failed — `reviews/007-design-test.log`. Red run `reviews/007-red-main.log`: the plain write, the child write and the throwing rule hang the test host on `main` (30-second blame timeout); the `Setter` test and the disabled test pass there, as the plan says.

### Veto-tier
None.

Each of the five behavioral bullets has a test in `src/Neatoo.Blazor.MudNeatoo.Tests/InputWriteIsWaitedForTests.cs` that renders the real `MudNeatooTextField` over a real `EntityBase` (`AsyncRuleTarget`), and each asserts what its bullet says:
- Input write: checks the entity is busy and the wait is incomplete while the gate is held, then that the wait completes after the gate is released, `IsBusy` is false and the value is written.
- Child's input: the same, checked on the root (busy, wait incomplete, then complete), then both root and child idle.
- `Setter`: the same over `SetName`. This passes on `main`, so it holds existing behavior in place rather than catching a regression. The bullet does not ask for a red run.
- Disabled: checks the field is enabled, then disabled while the rule runs, then enabled after the wait completes. It reads the rendered inner `MudTextField.Disabled` each time through `WaitForAssertion`, so a display that never changes fails it at the second or third check.
- Throwing rule: checks the first wait throws `AggregateException` with exactly one inner exception carrying the rule's message, the next wait is clean and the entity idle, and the next edit through the same field gives an incomplete wait that then completes with the value written.

### Callouts (≤ 5 Optional)
None. The plan has no Optional bullets and adds no product code, so there is no plan-introduced path to look for.

### Tech-debt (untiered, ≤ 5, one line each)
- `AWriteThroughAnInput_WhoseRuleThrows_...` asserts the change with `Assert.ThrowsAsync<InvalidOperationException>`, which also accepts subclasses, while the wait beside it uses `ThrowsExactlyAsync`. This is the loose-type pattern WAIT-003 and WAIT-005 tightened.
- `AsyncRuleTarget`'s summary says "Built directly (no factory)", but the class carries `[Factory]`. The comment and the attribute disagree.

### Could not verify
- That the three hangs on `main` are the #141 spin and not some other hang. The log gives only the blame timeout, and I did not open the dumps. The behavior fits the spin: the test bounds each wait with `WaitAsync(5s)`, so the call to `WaitForTasks()` must itself be blocking.
- "The MudNeatoo suite ran 20 more times with 0 failures": no log for those runs was provided.

### Theoretical (not triaged)
- The throwing test catches the change's fault from the dispatcher call. In a real page the same fault leaves MudTextField's DOM handler and goes into the renderer's event dispatch: the error UI on WASM, the end of the circuit on Server. No test sees that. Bullet 5's words are pinned, but the plan's Intent line "the page keeps working" claims more than any test shows.
- `SetterTests`' "without a Setter" tests check only that the value arrived, so they cannot tell `SetValue` from `LoadValue`. All eleven handlers call `SetValue` today (read, below), so this is not a gap now.
- The tests call `WaitForTasks()` from the test thread, not from inside a handler on the renderer's dispatcher, where a page's save handler calls it. The red run shows the freeze reproduces from the test thread anyway, and the single-thread case is AC-3's.

### Answers to the brief
- **One input for eleven.** The claim holds. All eleven `OnValueChanged` handlers in `src/Neatoo.Blazor.MudNeatoo/Components/*.razor.cs` are the same two branches: `await Setter(value)` or `await EntityProperty.SetValue(value)`. Each is wired to the inner component's change callback (`ValueChanged`, `DateChanged`, `TimeChanged`, `DateRangeChanged`). Every input binds `Disabled="@(EntityProperty.IsBusy || Disabled)"` and re-renders on the property's `IsBusy` notification. Where an input's write lands, at the property, is pinned per input by `SetterTests`. What happens after the property is written does not depend on which input wrote it, and the text-field tests pin that. AC-8 is pinned by the two together, with no real gap.
- **Raising the change.** The test calls the inner component's change callback on the renderer's dispatcher, the same seam `SetterTests` uses, and the same thread a DOM event reaches it on. It skips MudBlazor's text-to-value conversion and its `Validation` callback. Neither of those writes the property or changes the entity's wait. The one place the skipped renderer dispatch matters is the throwing case (Theoretical, first line).
- **"Not before".** Asserted. The wait is checked incomplete before the gate is released in the plain, child and `Setter` tests and in the next edit of the throwing test. The first wait in the throwing test is not checked that way, and its bullet does not ask for it.
- **Disabled.** The test shows the display follows busy: it sees enabled, then disabled, then enabled, read from the rendered inner component.
- **Tier.** I read it the same way. A bUnit render of the real component over a real entity, with no browser and no use-case catalogue declared, is `[integration]`.
- **Untouched files.** `git diff wait-arc...HEAD` touches only the two new test files, the plan and the todo's plan row. `SetterTests.cs`, `SetterTarget.cs`, `src/Neatoo.Blazor.MudNeatoo` and `src/Design` are unchanged. No existing test was weakened.

### Read report
- Read beyond the brief: `docs/VisionAlignment.md` D34 (the throwing bullet cites it); the Goal of `todo.md`, Dismissed, Parked and Recommendations Taken (to check nothing was raised again and to place #141's spin); `MudNeatooTextField.razor` (the `Disabled` binding).
- Named but unused: none.
